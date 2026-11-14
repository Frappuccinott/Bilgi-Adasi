using System;
using BilgiAdasi.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// Simya Traktörü (16-traktor-tarla-mekanikleri §1). Bakıp Use (F / X) → binilir, dururken yine Use → inilir.
    /// Sürüş kinematik (CharacterController, devrilme yok), iki akslı bisiklet modeli: dönüş hızı = v / dingil × tan(sapma).
    /// W/S veya sol çubuk: gaz / fren-geri, A/D: direksiyon, Shift / L3: hızlı (alet inikken yok).
    /// Kamera birinci şahıs, koltukta; bakış gövdeye göre serbest (±yawLimit). Roll yok, motor titreşimi kameraya verilmez.
    /// Bisikletle aynı desen ama ayrı sınıf: çalışan bisiklet koduna dokunulmaz.
    /// Alet (M3) ve parsel (M4) bu sınıfı SpeedCap ve olaylarla kullanır; traktör onları bilmez.
    /// </summary>
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class Tractor : WorldInteractable
    {
        private enum DriveState { Parked, Mounting, Driving, Dismounting }

        private const float GroundStickVelocity = -2f;
        private const float InputDeadzone = 0.05f;
        private const float SlopeHoldSpeed = 0.3f;
        private const float WallNormalMaxY = 0.5f;
        private const float FallLimit = 50f;
        private const float GroundProbeUp = 1.5f;
        private const float GroundProbeDown = 4f;

        [SerializeField] private TractorSettings settings;
        [SerializeField] private TractorRig rig;
        [Tooltip("Sürücünün ayak (kök) noktası. Göz = bu nokta + oyuncunun göz yüksekliği. Koltuğun üstünde.")]
        [SerializeField] private Transform seatAnchor;
        [Tooltip("Engel yoklaması ve inme yeri kontrolü için layer'lar (Player hariç).")]
        [SerializeField] private LayerMask obstacleMask = ~0;
        [SerializeField] private string saveId = "tractor";
        [Tooltip("Motor sesi (döngü). Boş bırakılabilir; perde ve ses yüke göre değişir.")]
        [SerializeField] private AudioSource engineSource;
        [SerializeField] private float engineIdleVolume = 0.25f;
        [SerializeField] private float engineMaxVolume = 0.6f;

        private CharacterController controller;
        private readonly RaycastHit[] probeHits = new RaycastHit[8];
        private readonly Collider[] overlapHits = new Collider[8];

        private DriveState state = DriveState.Parked;
        private PlayerInteractor rider;
        private PlayerMotor riderMotor;
        private PlayerLook riderLook;
        private Transform riderTransform;

        private float speed;
        private float yaw;
        private float steer;       // −1..1
        private float steerAngle;  // ön teker sapması (°)
        private float engineLoad;  // 0..1
        private float verticalVelocity;
        private float visualPitch;
        private float visualHeight;
        private bool gamepadFastToggled;
        private bool dismountRequested;
        private bool isQuitting;
        private Collider wallHit;
        private Vector3 groundNormal = Vector3.up;
        private float extraRearReach;

        private float blend;
        private Vector3 blendFrom;
        private Vector3 dismountTarget;
        private int mountFrame = -1;

        private IInteractPromptView promptView;
        private bool promptShown;
        private IRiderArmsView armsView;

        private Vector3 homePosition;
        private float homeYaw;

        /// <summary>Biri bindi (true) / indi (false).</summary>
        public event Action<bool> DrivenChanged;

        /// <summary>Sürücü varken Jump (Space / A) basıldı. Alet takma/indirme bunu dinler (M3); traktör kendisi bir şey yapmaz.</summary>
        public event Action ImplementButtonPressed;

        public bool IsDriven => state != DriveState.Parked;
        public bool IsDriving => state == DriveState.Driving;
        public float Speed => speed;
        public float Yaw => yaw;
        public TractorRig Rig => rig;

        /// <summary>Dışarıdan konan hız sınırı (m/s). Alet inikken iş hızı; yoksa sonsuz.</summary>
        public float SpeedCap { get; set; } = float.PositiveInfinity;

        /// <summary>Takılı aletin arka ucu kadar ek engel yoklaması (m).</summary>
        public float ExtraRearReach { get => extraRearReach; set => extraRearReach = Mathf.Max(0f, value); }

        public override bool CanInteract => base.CanInteract && state == DriveState.Parked && settings != null;

        private static SaveData Data => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponent<CharacterController>();
            if (rig == null) rig = GetComponentInChildren<TractorRig>();
            if (seatAnchor == null) seatAnchor = transform.Find("SeatAnchor");

            foreach (var col in GetComponentsInChildren<Collider>())
                if (col != controller) Physics.IgnoreCollision(controller, col);

            if (settings != null)
            {
                controller.slopeLimit = settings.SlopeLimit;
                controller.stepOffset = settings.StepOffset;
                controller.radius = settings.ColliderRadius;
                controller.height = Mathf.Max(settings.ColliderHeight, settings.ColliderRadius * 2f);
                controller.center = new Vector3(0f, controller.height * 0.5f, settings.ColliderCenterZ);
            }
            yaw = transform.eulerAngles.y;
            homePosition = transform.position;
            homeYaw = yaw;
        }

        private void Start()
        {
            var data = Data;
            bool resume = false;
            if (data != null && data.TryGetPose(saveId, out var pose))
            {
                if (HasGroundBelow(pose.position))
                {
                    PlaceAt(pose.position, pose.yaw);
                    resume = pose.ridden;
                }
                else
                {
                    Debug.LogWarning($"[Tractor] Kayıttaki konumun altında zemin yok {pose.position}; park yerinde başlıyor.", this);
                    data.SetPose(saveId, homePosition, homeYaw, false);
                }
            }
            if (settings == null) return;
            UpdateGrounding(0f, true);
            UpdateRig(0f);

            if (resume)
            {
                var player = FindAnyObjectByType<PlayerInteractor>(); // yalnızca açılışta bir kez
                if (player != null) Mount(player, true);
            }
        }

        private void OnEnable() => GameEvents.BeforeSave += OnBeforeSave;

        private void OnApplicationQuit()
        {
            isQuitting = true;
            if (state != DriveState.Parked) SavePose(true); // üstündeyken kapandı: açılışta oyuncu traktörde başlar
        }

        private void OnDisable()
        {
            GameEvents.BeforeSave -= OnBeforeSave;
            if (state == DriveState.Parked) return;
            // Çıkışta / sahne boşalırken oyuncuya dokunma (kapanmış GameInput'tan yeni Controls yaratılmasın).
            bool tearingDown = isQuitting || !gameObject.scene.isLoaded;
            EndDrive(riderTransform != null ? riderTransform.position : transform.position,
                riderTransform != null && !tearingDown, tearingDown);
        }

        private void OnBeforeSave(SaveData _)
        {
            if (state != DriveState.Parked) SavePose(true);
        }

        // ---------- Etkileşim ----------

        public override PromptAction TriggerAction => PromptAction.Use;

        public override string GetPrompt(PlayerInteractor interactor) => settings.MountPrompt;

        public override void Interact(PlayerInteractor interactor) => Mount(interactor, false);

        private void Mount(PlayerInteractor interactor, bool instant)
        {
            if (state != DriveState.Parked || interactor == null) return;
            var motor = interactor.GetComponent<PlayerMotor>();
            var look = interactor.GetComponent<PlayerLook>();
            if (motor == null || look == null) return;

            rider = interactor;
            riderMotor = motor;
            riderLook = look;
            riderTransform = interactor.transform;

            rider.SetRiding(true);
            riderMotor.Controller.enabled = false;
            riderMotor.Teleported += OnRiderTeleported;
            riderLook.BeginAnchoredYaw(instant ? 0f : Mathf.DeltaAngle(yaw, riderTransform.eulerAngles.y),
                settings.YawLimit, settings.RecenterDelay, settings.RecenterSpeed);

            var a = GameInput.Controls.Player;
            a.Use.performed += OnUsePerformed;
            a.Sprint.performed += OnSprintPerformed;
            a.Jump.performed += OnJumpPerformed;
            GameInput.SchemeChanged += OnSchemeChanged;
            promptView = ViewLocator.Get<IInteractPromptView>();
            armsView = FindArmsView();
            if (rig != null && rig.HasGrips) armsView?.BeginGrip(rig.GripLeft, rig.GripRight, transform, instant);

            speed = 0f;
            verticalVelocity = 0f;
            dismountRequested = false;
            gamepadFastToggled = false;
            mountFrame = Time.frameCount;
            blendFrom = riderTransform.position;
            blend = 0f;
            state = DriveState.Mounting;
            if (instant)
            {
                blend = 1f;
                state = DriveState.Driving;
                PlaceRider(seatAnchor.position);
            }
            if (engineSource != null && engineSource.clip != null && !engineSource.isPlaying) engineSource.Play();
            DrivenChanged?.Invoke(true);
        }

        private void OnUsePerformed(InputAction.CallbackContext ctx)
        {
            if (state != DriveState.Driving || Time.frameCount == mountFrame || !rider.ControlEnabled) return;
            dismountRequested = true;
        }

        private void OnSprintPerformed(InputAction.CallbackContext ctx)
        {
            if (ctx.control.device is Gamepad) gamepadFastToggled = !gamepadFastToggled;
        }

        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            if (state != DriveState.Driving || !rider.ControlEnabled) return;
            ImplementButtonPressed?.Invoke();
        }

        private void OnSchemeChanged(ControlScheme scheme)
        {
            if (promptShown) promptView?.ShowPrompt(settings.DismountPrompt, ControlGlyphs.For(PromptAction.Use));
        }

        private void OnRiderTeleported() => EndDrive(Vector3.zero, false);

        // ---------- Döngü ----------

        private void Update()
        {
            if (state == DriveState.Parked || settings == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (transform.position.y < homePosition.y - FallLimit)
            {
                PlaceAt(homePosition, homeYaw);
                speed = verticalVelocity = 0f;
                if (riderTransform != null) PlaceRider(seatAnchor.position);
                return;
            }

            switch (state)
            {
                case DriveState.Mounting:
                    blend += dt / Mathf.Max(0.01f, settings.MountDuration);
                    Simulate(Vector2.zero, false, dt, true);
                    PlaceRider(Vector3.Lerp(blendFrom, seatAnchor.position, Mathf.SmoothStep(0f, 1f, blend)));
                    if (blend >= 1f) state = DriveState.Driving;
                    break;

                case DriveState.Driving:
                    bool canDrive = rider.ControlEnabled && !dismountRequested;
                    var a = GameInput.Controls.Player;
                    Vector2 input = canDrive ? Vector2.ClampMagnitude(a.Move.ReadValue<Vector2>(), 1f) : Vector2.zero;
                    bool keyboardFast = a.Sprint.IsPressed() && !(a.Sprint.activeControl?.device is Gamepad);
                    bool fast = (keyboardFast || gamepadFastToggled) && canDrive;
                    Simulate(input, fast, dt, !canDrive);
                    PlaceRider(seatAnchor.position);
                    if (dismountRequested && Mathf.Abs(speed) <= settings.DismountMaxSpeed && controller.isGrounded)
                        BeginDismount();
                    break;

                case DriveState.Dismounting:
                    blend += dt / Mathf.Max(0.01f, settings.MountDuration);
                    Simulate(Vector2.zero, false, dt, true);
                    PlaceRider(Vector3.Lerp(blendFrom, dismountTarget, Mathf.SmoothStep(0f, 1f, blend)));
                    if (blend >= 1f) EndDrive(dismountTarget, true);
                    break;
            }

            if (state == DriveState.Parked) return;
            UpdateGrounding(dt);
            UpdateRig(dt);
            riderLook.YawRecenter = speed > SlopeHoldSpeed;
            riderLook.ExtraFov = settings.SpeedFovBoost * Mathf.Clamp01(speed / Mathf.Max(0.1f, settings.FastSpeed));
            UpdatePrompt();
            UpdateAudio();
        }

        private void Simulate(Vector2 input, bool fast, float dt, bool brakeAll)
        {
            bool grounded = controller.isGrounded;
            if (!grounded) groundNormal = Vector3.up;

            // --- Hız ---
            float throttle = input.y;
            if (throttle <= 0.1f) gamepadFastToggled = false;
            float forwardMax = Mathf.Min(fast ? settings.FastSpeed : settings.CruiseSpeed, SpeedCap);

            float target, rate;
            if (brakeAll)
            {
                target = 0f;
                rate = settings.BrakeDeceleration;
            }
            else if (throttle > InputDeadzone)
            {
                if (speed < -InputDeadzone) { target = 0f; rate = settings.BrakeDeceleration; }
                else
                {
                    target = forwardMax * throttle;
                    rate = speed < target ? settings.Acceleration : settings.CoastDeceleration;
                }
            }
            else if (throttle < -InputDeadzone)
            {
                if (speed > InputDeadzone) { target = 0f; rate = settings.BrakeDeceleration * -throttle; }
                else
                {
                    target = Mathf.Max(-settings.ReverseSpeed, -SpeedCap) * -throttle;
                    rate = speed > target ? settings.Acceleration : settings.CoastDeceleration;
                }
            }
            else
            {
                target = 0f;
                rate = settings.CoastDeceleration;
            }
            if (grounded) speed = Mathf.MoveTowards(speed, target, rate * dt);

            // Motor yükü: gaz ve hız (volan, duman, ses).
            float load = brakeAll ? 0f : Mathf.Max(Mathf.Abs(throttle), Mathf.Abs(speed) / Mathf.Max(0.1f, settings.FastSpeed));
            engineLoad = Mathf.MoveTowards(engineLoad, Mathf.Clamp01(load), 2f * dt);

            if (grounded && groundNormal.y < 0.999f && Mathf.Abs(speed) > SlopeHoldSpeed)
            {
                Vector3 along = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
                speed -= settings.SlopeAcceleration * along.y * dt;
            }
            speed = Mathf.Clamp(speed, -settings.ReverseSpeed, settings.FastSpeed);

            // --- Direksiyon ---
            float steerTarget = input.x;
            bool returning = Mathf.Abs(steerTarget) < Mathf.Abs(steer) || steerTarget * steer < 0f;
            steer = Mathf.MoveTowards(steer, steerTarget, (returning ? settings.SteerReturn : settings.SteerResponse) * dt);
            float speed01 = Mathf.Clamp01(Mathf.Abs(speed) / Mathf.Max(0.1f, settings.FastSpeed));
            steerAngle = steer * Mathf.Lerp(settings.MaxSteerAngleSlow, settings.MaxSteerAngleFast, speed01);
            float yawRate = speed / Mathf.Max(0.1f, settings.WheelBase) * Mathf.Tan(steerAngle * Mathf.Deg2Rad);
            yaw = Mathf.Repeat(yaw + yawRate * Mathf.Rad2Deg * dt, 360f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            LimitByObstacle(dt);

            if (grounded && verticalVelocity < 0f) verticalVelocity = GroundStickVelocity;
            verticalVelocity += settings.Gravity * dt;

            Vector3 dir = grounded ? Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized : transform.forward;
            Vector3 before = transform.position;
            float intended = speed * dt;
            wallHit = null;
            var flags = controller.Move((dir * speed + Vector3.up * verticalVelocity) * dt);

            // Dik engele sürtünce hız, gerçekten alınan yola iner (dik çarpışta durur, sıyırınca az kaybeder).
            if ((flags & CollisionFlags.Sides) != 0 && wallHit != null)
            {
                Vector3 moved = transform.position - before;
                moved.y = 0f;
                float progressed = Vector3.Dot(moved, transform.forward);
                if (Mathf.Abs(progressed) < Mathf.Abs(intended) * 0.8f)
                {
                    float slid = moved.magnitude / dt;
                    if (slid < Mathf.Abs(speed)) speed = Mathf.Sign(speed) * slid;
                }
            }
        }

        private void LimitByObstacle(float dt)
        {
            float travel = Mathf.Abs(speed) * dt;
            if (travel <= 0f) return;

            Vector3 dir = speed > 0f ? transform.forward : -transform.forward;
            float reach = speed > 0f ? settings.FrontReach : settings.RearReach + extraRearReach;
            float offset = Mathf.Max(0f, reach - settings.ProbeRadius);
            float probeY = Mathf.Max(settings.ProbeHeight, settings.StepOffset + 0.05f + settings.ProbeRadius);
            Vector3 origin = transform.position + Vector3.up * probeY;

            int count = Physics.SphereCastNonAlloc(origin, settings.ProbeRadius, dir, probeHits, offset + travel,
                obstacleMask, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = probeHits[i];
                if (hit.distance <= 0f) continue;
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (riderTransform != null && hit.collider.transform.IsChildOf(riderTransform)) continue;
                if (hit.normal.y > WallNormalMaxY) continue;
                if (hit.distance < nearest) nearest = hit.distance;
            }
            if (float.IsPositiveInfinity(nearest)) return;

            float allowed = nearest - offset;
            if (allowed >= travel) return;
            speed = allowed <= 0.01f ? 0f : Mathf.Sign(speed) * allowed / dt;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.collider.transform.IsChildOf(transform)) return;
            if (hit.normal.y > 0.5f)
            {
                if (hit.point.y < transform.position.y + 0.5f) groundNormal = hit.normal;
            }
            else wallHit = hit.collider;
        }

        /// <summary>Model zemine otursun: ön ve arka aksın altındaki zemine göre eğim ve yükseklik (yalnızca görünüm).</summary>
        private void UpdateGrounding(float dt, bool instant = false)
        {
            float hf = GroundHeightAt(settings.FrontContactZ);
            float hr = GroundHeightAt(settings.RearContactZ);
            float span = Mathf.Max(0.1f, settings.FrontContactZ - settings.RearContactZ);
            float t = (hf - hr) / span;
            float targetPitch = -Mathf.Atan(t) * Mathf.Rad2Deg;
            float targetHeight = hr + (0f - settings.RearContactZ) * t; // kökteki (z=0) yükseklik
            float k = instant ? 1f : 1f - Mathf.Exp(-settings.WheelGroundSharpness * dt);
            visualPitch = Mathf.Lerp(visualPitch, targetPitch, k);
            visualHeight = Mathf.Lerp(visualHeight, targetHeight, k);
        }

        private float GroundHeightAt(float localZ)
        {
            float maxH = settings.MaxGroundProbe;
            Vector3 origin = transform.TransformPoint(new Vector3(0f, maxH + 0.15f, localZ));
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, probeHits, 2f * maxH + 0.3f, obstacleMask,
                QueryTriggerInteraction.Ignore);
            float best = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = probeHits[i];
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (riderTransform != null && hit.collider.transform.IsChildOf(riderTransform)) continue;
                if (hit.point.y > best) best = hit.point.y;
            }
            if (float.IsNegativeInfinity(best)) return 0f;
            return Mathf.Clamp(best - (transform.position.y - controller.skinWidth), -maxH, maxH);
        }

        private void UpdateRig(float dt)
        {
            if (rig == null) return;
            float engine = state == DriveState.Parked ? -1f : engineLoad;
            rig.UpdatePose(speed, steerAngle, steerAngle * settings.SteeringWheelRatio, engine,
                settings.IdleFlywheelRps, settings.MaxFlywheelRps, visualPitch, visualHeight, dt);
        }

        private void PlaceRider(Vector3 position)
        {
            // Bakış ofsetini PlayerLook bu karede güncelledi (çalışma sırası 50); roll yok.
            riderTransform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw + riderLook.YawOffset, 0f));
        }

        // ---------- İnme ----------

        private void BeginDismount()
        {
            dismountRequested = false;
            if (!TryFindDismountSpot(out dismountTarget)) return; // yer yok: sürmeye devam
            speed = 0f;
            blendFrom = riderTransform.position;
            blend = 0f;
            state = DriveState.Dismounting;
            armsView?.EndGrip();
            HidePrompt();
        }

        private bool TryFindDismountSpot(out Vector3 spot)
        {
            float side = settings.DismountSideOffset;
            float radius = riderMotor.Controller.radius;
            float height = riderMotor.StandingHeight;
            Vector3 fromEye = seatAnchor.position + Vector3.up * 1.2f;

            for (int i = 0; i < 4; i++)
            {
                Vector3 local = i switch
                {
                    0 => new Vector3(-side, 0f, -0.6f),                        // sol basamak (sürücünün inişi)
                    1 => new Vector3(side, 0f, -0.6f),                         // sağ
                    2 => new Vector3(0f, 0f, -(settings.RearReach + extraRearReach + 0.6f)), // arka
                    _ => new Vector3(0f, 0f, settings.FrontReach + 0.6f)       // ön
                };
                Vector3 probe = transform.TransformPoint(local) + Vector3.up * 1.0f;
                if (!Physics.Raycast(probe, Vector3.down, out var ground, 2.2f, obstacleMask, QueryTriggerInteraction.Ignore)) continue;
                if (ground.normal.y < 0.7f || ground.collider.transform.IsChildOf(transform)) continue;

                Vector3 point = ground.point;
                Vector3 bottom = point + Vector3.up * (radius + 0.05f);
                Vector3 top = point + Vector3.up * Mathf.Max(radius + 0.05f, height - radius);
                // Traktörün kendi gövde hacmi ve oyuncu engel sayılmaz (Play 14.11: F ile inilemiyordu, dört yön de
                // traktörün kutusuna değdiği için reddediliyordu).
                if (IsBlocked(bottom, top, radius)) continue;
                if (Physics.Linecast(fromEye, point + Vector3.up * 1f, out var block, obstacleMask, QueryTriggerInteraction.Ignore)
                    && !block.collider.transform.IsChildOf(transform)) continue;

                spot = point;
                return true;
            }
            spot = default;
            return false;
        }

        private bool IsBlocked(Vector3 bottom, Vector3 top, float radius)
        {
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlapHits, obstacleMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var t = overlapHits[i].transform;
                if (t.IsChildOf(transform)) continue;
                if (riderTransform != null && t.IsChildOf(riderTransform)) continue;
                return true;
            }
            return false;
        }

        private void EndDrive(Vector3 riderPosition, bool teleportRider, bool tearingDown = false)
        {
            if (state == DriveState.Parked) return;
            state = DriveState.Parked;

            if (GameInput.TryGetControls(out var c))
            {
                c.Player.Use.performed -= OnUsePerformed;
                c.Player.Sprint.performed -= OnSprintPerformed;
                c.Player.Jump.performed -= OnJumpPerformed;
            }
            GameInput.SchemeChanged -= OnSchemeChanged;
            if (riderMotor != null) riderMotor.Teleported -= OnRiderTeleported;
            if (tearingDown)
            {
                if (!isQuitting) SavePose(true); // sahne değişimi; çıkışta OnApplicationQuit yazdı
                rider = null;
                riderMotor = null;
                riderLook = null;
                riderTransform = null;
                promptView = null;
                promptShown = false;
                armsView = null;
                return;
            }
            HidePrompt();
            armsView?.EndGrip();
            armsView = null;

            if (riderLook != null)
            {
                riderLook.ExtraFov = 0f;
                riderLook.EndAnchoredYaw();
            }
            if (riderMotor != null && teleportRider)
                riderMotor.Teleport(riderPosition, riderTransform.rotation);
            if (rider != null) rider.SetRiding(false);

            rider = null;
            riderMotor = null;
            riderLook = null;
            riderTransform = null;
            promptView = null;

            speed = 0f;
            engineLoad = 0f;
            dismountRequested = false;
            UpdateGrounding(0f, true);
            UpdateRig(0f);
            if (engineSource != null) engineSource.Stop();

            SavePose(false);
            DrivenChanged?.Invoke(false);
        }

        // ---------- Yardımcılar ----------

        /// <summary>
        /// Kayıtlı kol görünümü. Kayıtta yok edilmiş eski bir kol rig'i kalmışsa (Play 14.11: ViewLocator'da Destroy
        /// edilmiş RiderArmsIK duruyordu, eller tutmuyordu) oyuncunun kendi kollarına düşer. Yalnızca binerken çağrılır.
        /// </summary>
        private IRiderArmsView FindArmsView()
        {
            var view = ViewLocator.Get<IRiderArmsView>();
            if (view is UnityEngine.Object o && o != null) return view;
            var own = rider != null ? rider.GetComponentInChildren<RiderArmsIK>(true) : null;
            return own != null ? own : FindAnyObjectByType<RiderArmsIK>();
        }

        private void UpdatePrompt()
        {
            bool show = state == DriveState.Driving && rider.ControlEnabled && !dismountRequested
                        && Mathf.Abs(speed) < settings.PromptMaxSpeed;
            if (show == promptShown) return;
            promptShown = show;
            if (show) promptView?.ShowPrompt(settings.DismountPrompt, ControlGlyphs.For(PromptAction.Use));
            else promptView?.HidePrompt();
        }

        private void HidePrompt()
        {
            if (!promptShown) return;
            promptShown = false;
            promptView?.HidePrompt();
        }

        private void UpdateAudio()
        {
            if (engineSource == null) return;
            engineSource.volume = Mathf.Lerp(engineIdleVolume, engineMaxVolume, engineLoad);
            engineSource.pitch = 0.85f + 0.4f * engineLoad;
        }

        private bool HasGroundBelow(Vector3 position)
        {
            int count = Physics.RaycastNonAlloc(position + Vector3.up * GroundProbeUp, Vector3.down, probeHits,
                GroundProbeUp + GroundProbeDown, obstacleMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var t = probeHits[i].collider.transform;
                if (t.IsChildOf(transform)) continue;
                if (riderTransform != null && t.IsChildOf(riderTransform)) continue;
                return true;
            }
            return false;
        }

        private void PlaceAt(Vector3 position, float yawDegrees)
        {
            controller.enabled = false;
            yaw = yawDegrees;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            controller.enabled = true;
        }

        private void SavePose(bool driven)
        {
            var save = SaveSystem.Instance;
            if (save == null || save.Data == null || string.IsNullOrEmpty(saveId)) return;
            if (!HasGroundBelow(transform.position)) return;
            save.Data.SetPose(saveId, transform.position, yaw, driven);
        }
    }
}
