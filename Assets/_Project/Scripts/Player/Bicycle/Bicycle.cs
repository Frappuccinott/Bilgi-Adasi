using System;
using BilgiAdasi.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// Şehirde duran bisiklet. Bakıp Use (F / X) → binilir, yine Use → inilir (hızlıysa önce frenler).
    /// Sürüş kinematik (CharacterController): W/S veya sol çubuk pedal/fren (dururken geri itme), A/D gidon,
    /// Shift / L3 hızlı pedal. Bakış serbest (gidona göre ±yawLimit), bırakınca gidiyorken öne döner.
    /// Kamera birinci şahısta kalır; yatış yalnızca modelde (BicycleRig). Konum indikten sonra kaydedilir.
    /// PlayerLook'tan sonra çalışır: aynı karede bakış ofseti hazırdır, sürücü pozu tek seferde yazılır.
    /// </summary>
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class Bicycle : WorldInteractable
    {
        private enum RideState { Parked, Mounting, Riding, Dismounting }

        private const float GroundStickVelocity = -2f;
        private const float InputDeadzone = 0.05f;
        private const float SlopeHoldSpeed = 0.3f;
        private const float WallNormalMaxY = 0.5f;
        private const float RiderLiftSharpness = 10f;

        [SerializeField] private BicycleSettings settings;
        [SerializeField] private BicycleRig rig;
        [Tooltip("Sürücünün ayak (kök) noktası. Göz = bu nokta + oyuncunun göz yüksekliği. Yatmaz (Visual dışında).")]
        [SerializeField] private Transform seatAnchor;
        [Tooltip("Engel yoklaması ve inme yeri kontrolü için layer'lar (Player hariç).")]
        [SerializeField] private LayerMask obstacleMask = ~0;
        [Tooltip("Kayıt anahtarı. Sahnede birden çok bisiklet varsa her biri farklı olmalı.")]
        [SerializeField] private string saveId = "bicycle";
        [Tooltip("Tekerlek yuvarlanma sesi (döngü). Boş bırakılabilir.")]
        [SerializeField] private AudioSource rollingSource;
        [SerializeField] private float rollingMaxVolume = 0.6f;

        private CharacterController controller;
        private readonly RaycastHit[] probeHits = new RaycastHit[8];

        private RideState state = RideState.Parked;
        private PlayerInteractor rider;
        private PlayerMotor riderMotor;
        private PlayerLook riderLook;
        private Transform riderTransform;

        private float speed;
        private float momentum;
        private float visualPitch;
        private float visualHeight;
        private float curbClimbRemaining;
        private Collider wallHit; // son Move'da değilen dik yüzey (yoksa null)
        private bool wasGrounded = true;
        private bool isQuitting;
        private float launchVertical; // yerdeyken eğim boyunca hızın dikey payı (m/s)
        private float riderLift;
        private float frontContactZ = 0.58f;
        private float rearContactZ = -0.47f;
        private float yaw;
        private float steer;
        private float steerAngle;
        private float lean;
        private float stand;     // 0 oturuyor, 1 ayakta (hızlı pedal)
        private float rockPhase; // ayakta sallanma ritmi (radyan)
        private float rock;      // ayakta sallanma açısı (°, sağ pozitif; yalnızca model)
        private float verticalVelocity;
        private bool pedaling;
        private bool gamepadFastToggled;
        private bool dismountRequested;
        private Vector3 groundNormal = Vector3.up;

        private float blend;
        private Vector3 blendFrom;
        private Vector3 dismountTarget;
        private int mountFrame = -1;

        private IInteractPromptView promptView;
        private bool promptShown;
        private IRiderArmsView armsView;
        private IRiderBodyView bodyView;

        // Sahnedeki park yeri: kayıttaki konum geçersizse ya da bisiklet dünyanın altına düşerse buraya döner.
        private Vector3 homePosition;
        private float homeYaw;
        private const float FallLimit = 50f;       // park yerinin bu kadar altına düşerse geri dön
        private const float GroundProbeUp = 1.5f;  // zemin yoklaması: noktanın üstünden başlar
        private const float GroundProbeDown = 4f;

        /// <summary>Biri bisiklete binince / inince (ses, HUD, görev için).</summary>
        public event Action<bool> RiddenChanged;

        public bool IsRidden => state != RideState.Parked;
        public float Speed => speed;
        public override bool CanInteract => base.CanInteract && state == RideState.Parked && settings != null;

        private static SaveData Data => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponent<CharacterController>();
            if (rig == null) rig = GetComponentInChildren<BicycleRig>();
            if (seatAnchor == null) seatAnchor = transform.Find("SeatAnchor");

            // Bakma/çarpma hacimleri bisikletin kendi kapsülüne takılmasın.
            foreach (var col in GetComponentsInChildren<Collider>())
                if (col != controller) Physics.IgnoreCollision(controller, col);

            if (settings != null)
            {
                controller.slopeLimit = settings.SlopeLimit;
                controller.stepOffset = settings.StepOffset;
                SetColliderHeight(settings.ParkedHeight);
            }
            yaw = transform.eulerAngles.y;
            homePosition = transform.position;
            homeYaw = yaw;
            if (rig != null && rig.TryGetWheelContactZ(transform, out float front, out float rear))
            {
                frontContactZ = front;
                rearContactZ = rear;
            }
        }

        private void Start()
        {
            var data = Data;
            bool resumeRide = false;
            if (data != null && data.TryGetPose(saveId, out var pose))
            {
                // Kayıt sahneler arasında ortak: başka sahnede (ya da düşerken) kaydedilmiş konumun altında zemin
                // olmayabilir. O zaman park yerinde kalınır ve bisiklete binilmiş başlanmaz.
                if (HasGroundBelow(pose.position))
                {
                    PlaceAt(pose.position, pose.yaw);
                    resumeRide = pose.ridden;
                }
                else
                {
                    Debug.LogWarning($"[Bicycle] Kayıttaki konumun altında zemin yok {pose.position}; park yerinde başlıyor.", this);
                    data.SetPose(saveId, homePosition, homeYaw, false);
                }
            }
            if (settings == null) return;
            // Kaldırım kenarına park edilmişse teker yere otursun.
            UpdateWheelGrounding(0f, true);
            if (rig != null) rig.UpdatePose(0f, false, 0f, 0f, visualPitch, visualHeight, 0f);

            // Oyun bisikletteyken kapandıysa oyuncu bisikletin üstünde başlar (PlayerSpawn oyuncuyu Awake'te yaratır).
            if (resumeRide)
            {
                var player = FindAnyObjectByType<PlayerInteractor>(); // yalnızca açılışta bir kez
                if (player != null) Mount(player, true);
            }
        }

        private void OnDisable()
        {
            if (state == RideState.Parked) return;
            // Çıkışta / sahne boşalırken oyuncuya dokunma: SetRiding(false) PlayerMotor'u açar, OnEnable'ı
            // kapanmış GameInput'tan yeni (dispose edilmeyen) bir Controls örneği yaratırdı.
            bool tearingDown = isQuitting || !gameObject.scene.isLoaded;
            EndRide(riderTransform != null ? riderTransform.position : transform.position,
                riderTransform != null && !tearingDown, tearingDown);
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
            if (state != RideState.Parked) SavePose(true); // üstündeyken kapandı: açılışta oyuncu bisiklette başlar
        }

        // ---------- Etkileşim ----------

        /// <summary>Bisiklete F ile binilir/inilir (gamepad X).</summary>
        public override PromptAction TriggerAction => PromptAction.Use;

        public override string GetPrompt(PlayerInteractor interactor) => settings.MountPrompt;

        public override void Interact(PlayerInteractor interactor) => Mount(interactor, false);

        /// <summary>instant: geçiş yok, oyuncu doğrudan selede ve bisikletin yönüne bakar (kayıttan devam).</summary>
        private void Mount(PlayerInteractor interactor, bool instant)
        {
            if (state != RideState.Parked || interactor == null) return;
            var motor = interactor.GetComponent<PlayerMotor>();
            var look = interactor.GetComponent<PlayerLook>();
            if (motor == null || look == null) return;

            rider = interactor;
            riderMotor = motor;
            riderLook = look;
            riderTransform = interactor.transform;

            rider.SetRiding(true);                // hareket, kafa sallanması ve ışın kapanır; crosshair kalır
            riderMotor.Controller.enabled = false; // bisikletin kapsülü taşır
            riderMotor.Teleported += OnRiderTeleported;
            riderLook.BeginAnchoredYaw(instant ? 0f : Mathf.DeltaAngle(yaw, riderTransform.eulerAngles.y),
                settings.YawLimit, settings.RecenterDelay, settings.RecenterSpeed);

            var a = GameInput.Controls.Player;
            a.Use.performed += OnUsePerformed;
            a.Sprint.performed += OnSprintPerformed;
            GameInput.SchemeChanged += OnSchemeChanged;
            promptView = ViewLocator.Get<IInteractPromptView>();
            // Eller gidona uzanır (kollar varsa); sürerken gidonla birlikte döner ve yatar.
            armsView = ViewLocator.Get<IRiderArmsView>();
            if (rig != null && rig.HasGrips) armsView?.BeginGrip(rig.GripLeft, rig.GripRight, transform, instant);
            // Gövde seleye oturur, ayaklar pedallara basar.
            bodyView = ViewLocator.Get<IRiderBodyView>();
            if (rig != null) bodyView?.BeginRide(rig, instant);

            SetColliderHeight(settings.RidingHeight);
            speed = 0f;
            momentum = 0f;
            riderLift = 0f;
            stand = rock = 0f;
            wasGrounded = true;
            launchVertical = 0f;
            verticalVelocity = 0f;
            dismountRequested = false;
            gamepadFastToggled = false;
            mountFrame = Time.frameCount;
            blendFrom = riderTransform.position;
            blend = 0f;
            state = RideState.Mounting;
            if (instant)
            {
                blend = 1f;
                state = RideState.Riding;
                PlaceRider(seatAnchor.position);
            }
            RiddenChanged?.Invoke(true);
        }

        private void OnUsePerformed(InputAction.CallbackContext ctx)
        {
            // Binme basışı aynı karede inmeye dönmesin; konuşma sırasında inilmez.
            if (state != RideState.Riding || Time.frameCount == mountFrame || !rider.ControlEnabled) return;
            dismountRequested = true;
        }

        private void OnSprintPerformed(InputAction.CallbackContext ctx)
        {
            if (ctx.control.device is Gamepad) gamepadFastToggled = !gamepadFastToggled;
        }

        private void OnSchemeChanged(ControlScheme scheme)
        {
            if (promptShown) promptView?.ShowPrompt(settings.DismountPrompt, ControlGlyphs.For(PromptAction.Use));
        }

        /// <summary>Oyuncu dışarıdan ışınlandı (doğma, geliştirici kısayolu): olduğu yerde bisikletten ayrılır.</summary>
        private void OnRiderTeleported() => EndRide(Vector3.zero, false);

        // ---------- Döngü ----------

        private void Update()
        {
            if (state == RideState.Parked || settings == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Zeminden taşıp boşluğa düştüyse (harita kenarı, delik) park yerine dön; sürücü de selede gelir.
            if (transform.position.y < homePosition.y - FallLimit)
            {
                PlaceAt(homePosition, homeYaw);
                speed = momentum = verticalVelocity = 0f;
                if (riderTransform != null) PlaceRider(seatAnchor.position);
                return;
            }

            switch (state)
            {
                case RideState.Mounting:
                    blend += dt / Mathf.Max(0.01f, settings.MountDuration);
                    Simulate(Vector2.zero, false, dt);
                    PlaceRider(Vector3.Lerp(blendFrom, seatAnchor.position, Mathf.SmoothStep(0f, 1f, blend)));
                    if (blend >= 1f) state = RideState.Riding;
                    break;

                case RideState.Riding:
                    bool canDrive = rider.ControlEnabled && !dismountRequested;
                    var a = GameInput.Controls.Player;
                    Vector2 input = canDrive ? Vector2.ClampMagnitude(a.Move.ReadValue<Vector2>(), 1f) : Vector2.zero;
                    bool keyboardFast = a.Sprint.IsPressed() && !(a.Sprint.activeControl?.device is Gamepad);
                    bool brakeAll = !canDrive; // inmek isteniyor ya da konuşma var: fren
                    bool fast = (keyboardFast || gamepadFastToggled) && canDrive;
                    Simulate(input, fast, dt, brakeAll);
                    UpdateStanding(fast, dt);
                    PlaceRider(seatAnchor.position);
                    if (dismountRequested && Mathf.Abs(speed) <= settings.DismountMaxSpeed && controller.isGrounded)
                        BeginDismount();
                    break;

                case RideState.Dismounting:
                    blend += dt / Mathf.Max(0.01f, settings.MountDuration);
                    Simulate(Vector2.zero, false, dt, true);
                    UpdateStanding(false, dt);
                    PlaceRider(Vector3.Lerp(blendFrom, dismountTarget, Mathf.SmoothStep(0f, 1f, blend)));
                    if (blend >= 1f) EndRide(dismountTarget, true);
                    break;
            }

            if (state == RideState.Parked) return; // EndRide bu karede bitirdi
            riderLift = Mathf.Lerp(riderLift, 0f, 1f - Mathf.Exp(-RiderLiftSharpness * dt));
            UpdateWheelGrounding(dt);
            if (rig != null) rig.UpdatePose(speed, pedaling, steerAngle, lean + rock, visualPitch, visualHeight, dt);
            riderLook.YawRecenter = Mathf.Abs(speed) > SlopeHoldSpeed;
            riderLook.ExtraFov = settings.SpeedFovBoost * Mathf.Clamp01(speed / Mathf.Max(0.1f, settings.FastSpeed));
            UpdatePrompt();
            UpdateAudio();
        }

        private void Simulate(Vector2 input, bool fast, float dt, bool brakeAll = false)
        {
            bool grounded = controller.isGrounded;
            if (!grounded) groundNormal = Vector3.up;

            if (grounded && !wasGrounded)
            {
                // İniş: kamera hafif çöker, sert inişte biraz hız gider.
                float impact = -verticalVelocity;
                // Yere yapışma hızı (−2) civarı inişler zemin titremesidir: sarsıntı vermesin.
                if (impact > -GroundStickVelocity + 1f)
                {
                    riderLift -= Mathf.Min(settings.MaxLandingDip, impact * settings.LandingDipPerSpeed);
                    if (impact > settings.HardLandingSpeed) speed *= 1f - settings.HardLandingSpeedLoss;
                }
            }
            else if (!grounded && wasGrounded && curbClimbRemaining <= 0f
                     && Mathf.Abs(launchVertical - verticalVelocity) > settings.MinLaunchVertical)
            {
                // Rampanın ucundan çıkış: eğim boyunca giden hızın dikey payı korunur (yay çizerek uçar/düşer).
                verticalVelocity = launchVertical;
            }
            wasGrounded = grounded;

            // --- Hız ---
            float throttle = input.y;
            if (throttle <= 0.1f) gamepadFastToggled = false;
            pedaling = throttle > InputDeadzone && speed >= 0f && !brakeAll;

            // Momentum (gittikçe hızlanma): seyir hızına ulaşıp pedala devam ettikçe hedef hız yavaşça yükselir.
            // Pedal bırakılınca yavaş, frende hızlı söner; duvara takılıp hız düşerse de söner.
            float fastMul = fast ? settings.FastAccelerationMultiplier : 1f;
            float baseTarget = Mathf.Lerp(settings.CruiseSpeed, settings.MomentumSpeed, momentum);
            bool braking = brakeAll || (throttle < -InputDeadzone && speed > InputDeadzone);
            if (pedaling && speed >= baseTarget * throttle * 0.9f)
                momentum = Mathf.MoveTowards(momentum, 1f, fastMul * throttle * dt / Mathf.Max(0.1f, settings.MomentumBuildTime));
            else if (braking || !pedaling || speed < baseTarget * 0.6f)
                momentum = Mathf.MoveTowards(momentum, 0f, (braking ? settings.MomentumBrakeDecay : settings.MomentumDecay) * dt);

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
                    target = (baseTarget + (fast ? settings.FastSpeedBonus : 0f)) * throttle;
                    // İvme eğrisi: kalkışta ağır ilk tur, sonra güçlü, hedefe yaklaştıkça yumuşak.
                    rate = speed < target
                        ? settings.PedalAcceleration * fastMul * settings.AccelerationFactor(Mathf.Clamp01(speed / Mathf.Max(0.1f, target)))
                        : settings.CoastDeceleration;
                }
            }
            else if (throttle < -InputDeadzone)
            {
                if (speed > InputDeadzone) { target = 0f; rate = settings.BrakeDeceleration * -throttle; }
                else { target = settings.PushBackSpeed * throttle; rate = settings.PushAcceleration; }
            }
            else
            {
                target = 0f;
                rate = speed < 0f ? settings.PushAcceleration : settings.CoastDeceleration;
            }
            if (grounded) speed = Mathf.MoveTowards(speed, target, rate * dt);

            // Eğim: yokuş yavaşlatır, iniş hızlandırır. Dururken ve pedal yokken bisiklet kendi kaymaz.
            if (grounded && groundNormal.y < 0.999f && (Mathf.Abs(speed) > SlopeHoldSpeed || pedaling))
            {
                Vector3 along = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
                speed -= settings.SlopeAcceleration * along.y * dt;
            }
            speed = Mathf.Clamp(speed, -settings.PushBackSpeed, settings.MaxDownhillSpeed);

            // --- Gidon (bisiklet modeli: dönüş hızı = v / dingil * tan(açı)) ---
            float steerTarget = input.x;
            bool returning = Mathf.Abs(steerTarget) < Mathf.Abs(steer) || steerTarget * steer < 0f;
            steer = Mathf.MoveTowards(steer, steerTarget, (returning ? settings.SteerReturn : settings.SteerResponse) * dt);
            float speed01 = Mathf.Clamp01(Mathf.Abs(speed) / Mathf.Max(0.1f, settings.CruiseSpeed));
            steerAngle = steer * Mathf.Lerp(settings.MaxSteerAngleSlow, settings.MaxSteerAngleFast, speed01);
            float yawRate = speed / Mathf.Max(0.1f, settings.WheelBase) * Mathf.Tan(steerAngle * Mathf.Deg2Rad); // rad/sn
            yaw = Mathf.Repeat(yaw + yawRate * Mathf.Rad2Deg * dt, 360f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // --- Yatış: merkezkaç dengesi, yalnızca model ---
            float targetLean = Mathf.Clamp(Mathf.Atan(speed * yawRate / 9.81f) * Mathf.Rad2Deg, -settings.MaxLean, settings.MaxLean);
            lean = Mathf.Lerp(lean, targetLean, 1f - Mathf.Exp(-settings.LeanSharpness * dt));

            // --- Engel: tekerlek uçları kapsülün dışında, ileri/geri yokla ---
            LimitByObstacle(dt);

            // --- Dikey (kaldırıma çıkarken kapsül kenarı geçene kadar havada tutulur) ---
            if (curbClimbRemaining > 0f)
            {
                verticalVelocity = 0f;
                curbClimbRemaining = Mathf.Abs(speed) < 0.1f ? 0f : curbClimbRemaining - Mathf.Abs(speed) * dt;
            }
            else
            {
                if (grounded && verticalVelocity < 0f) verticalVelocity = GroundStickVelocity;
                verticalVelocity += settings.Gravity * dt;
            }

            Vector3 dir = grounded ? Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized : transform.forward;
            if (grounded) launchVertical = dir.y * speed;
            Vector3 before = transform.position;
            float intended = speed * dt;
            wallHit = null;
            var flags = controller.Move((dir * speed + Vector3.up * verticalVelocity) * dt);

            // CC'nin kendi küçük basamak çıkışı (zemin ekleri, eşikler): kamera ve model tek karede sıçramasın.
            float rise = transform.position.y - before.y;
            float slopeRise = Mathf.Abs(intended) * 0.85f + 0.005f; // ~40° eğime kadar normal
            if (grounded && rise > slopeRise)
            {
                riderLift -= rise - slopeRise;
                visualHeight -= rise - slopeRise;
            }

            // Yalnızca gerçek yan engel (dik yüzey) hızı keser. Zemin ekleri ve dönüşte kapsülün sürtündüğü
            // yatık yüzeyler de Sides bayrağı verir; onlar hızı sıfırlamamalı (sağa dönerken ani durma buydu).
            if ((flags & CollisionFlags.Sides) != 0 && wallHit != null)
            {
                Vector3 moved = transform.position - before;
                moved.y = 0f;
                float progressed = Vector3.Dot(moved, transform.forward);
                bool blocked = Mathf.Abs(progressed) < Mathf.Abs(intended) * 0.8f;
                if (!blocked) return;

                if (grounded && Mathf.Abs(speed) > 0.2f && TryClimbCurb(Mathf.Sign(speed)))
                {
                    speed *= 1f - settings.CurbSpeedLoss; // tümsek hissi: biraz hız kaybı
                }
                else
                {
                    // Duvar boyunca kayma korunur: hız, gerçekten alınan yatay yola iner (dik çarpışta durur, sıyırınca az kaybeder).
                    float slid = moved.magnitude / dt;
                    if (slid < Mathf.Abs(speed))
                    {
                        LogSuddenStop("yan engel", wallHit, speed, slid);
                        speed = Mathf.Sign(speed) * slid;
                    }
                }
            }
        }

        /// <summary>Hız bir karede belirgin düşerse sebebini yazar (yalnızca editör / development build).</summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogSuddenStop(string reason, Collider obstacle, float from, float to)
        {
            if (Mathf.Abs(from) - Mathf.Abs(to) < 1.5f) return;
            Debug.Log($"[Bisiklet] Ani yavaşlama ({reason}): {Mathf.Abs(from):0.0} → {Mathf.Abs(to):0.0} m/s, engel: " +
                      (obstacle != null ? obstacle.name : "-"), obstacle != null ? obstacle : this);
        }

        /// <summary>
        /// Kapsülün önünde (geri giderken arkasında) maxCurbHeight'tan alçak bir basamak varsa bisikleti üstüne kaldırır.
        /// Kamera ve model sıçramaz: sürücü ve model yükseklik farkını yumuşakça kapatır.
        /// </summary>
        private bool TryClimbCurb(float direction)
        {
            float maxH = settings.MaxCurbHeight;
            Vector3 dir = transform.forward * direction;
            Vector3 origin = transform.position + dir * (controller.radius + 0.12f) + Vector3.up * (maxH + 0.1f);
            if (!Physics.Raycast(origin, Vector3.down, out var hit, maxH + 0.25f, obstacleMask, QueryTriggerInteraction.Ignore)) return false;
            if (hit.normal.y < 0.7f || hit.collider.transform.IsChildOf(transform)) return false;

            float groundY = transform.position.y - controller.skinWidth;
            float h = hit.point.y - groundY;
            if (h <= 0.02f || h > maxH) return false;

            float startY = transform.position.y;
            controller.Move(Vector3.up * (h + 0.02f));
            float lifted = transform.position.y - startY;
            if (lifted < h * 0.9f) return false; // tavan/engel: çıkılamadı

            curbClimbRemaining = controller.radius + 0.08f;
            verticalVelocity = 0f;
            riderLift -= lifted;   // kamera yumuşakça yükselsin
            visualHeight -= lifted; // model olduğu yerde kalsın, sonra otursun
            return true;
        }

        /// <summary>Model tekerleri yere otursun: ön/arka teker altındaki zemine göre öne/arkaya eğim ve yükseklik.</summary>
        private void UpdateWheelGrounding(float dt, bool instant = false)
        {
            float targetPitch, targetHeight, sharpness;
            if (controller.isGrounded || curbClimbRemaining > 0f || instant)
            {
                float hf = GroundHeightAt(frontContactZ);
                float hr = GroundHeightAt(rearContactZ);
                float t = (hf - hr) / Mathf.Max(0.1f, frontContactZ - rearContactZ);
                targetPitch = -Mathf.Atan(t) * Mathf.Rad2Deg; // burun yukarı: X etrafında negatif
                targetHeight = hf - frontContactZ * t;
                sharpness = settings.WheelGroundSharpness;
            }
            else
            {
                // Havada: burun gidiş yönünü izler (çıkarken yukarı, düşerken aşağı). Geri giderken kuyruk izler.
                float flight = Mathf.Atan2(verticalVelocity, Mathf.Max(1f, Mathf.Abs(speed))) * Mathf.Rad2Deg;
                if (speed < 0f) flight = -flight;
                targetPitch = Mathf.Clamp(-flight, -settings.MaxAirPitch, settings.MaxAirPitch);
                targetHeight = 0f;
                sharpness = settings.AirPitchSharpness;
            }
            float k = instant ? 1f : 1f - Mathf.Exp(-sharpness * dt);
            visualPitch = Mathf.Lerp(visualPitch, targetPitch, k);
            visualHeight = Mathf.Lerp(visualHeight, targetHeight, k);
        }

        private float GroundHeightAt(float localZ)
        {
            float maxH = settings.MaxCurbHeight;
            Vector3 origin = transform.TransformPoint(new Vector3(0f, maxH + 0.15f, localZ));
            if (!Physics.Raycast(origin, Vector3.down, out var hit, 2f * maxH + 0.3f, obstacleMask, QueryTriggerInteraction.Ignore)) return 0f;
            if (hit.collider.transform.IsChildOf(transform)) return 0f;
            return Mathf.Clamp(hit.point.y - (transform.position.y - controller.skinWidth), -maxH, maxH);
        }

        private void LimitByObstacle(float dt)
        {
            float travel = Mathf.Abs(speed) * dt;
            if (travel <= 0f) return;

            Vector3 dir = speed > 0f ? transform.forward : -transform.forward;
            float reach = speed > 0f ? settings.FrontReach : settings.RearReach;
            float offset = Mathf.Max(0f, reach - settings.ProbeRadius);
            // Küre kaldırım yüksekliğinin üstünden geçer: kaldırım engel değil, TryClimbCurb çıkar.
            float probeY = Mathf.Max(settings.ProbeHeight, settings.MaxCurbHeight + 0.05f + settings.ProbeRadius);
            Vector3 origin = transform.position + Vector3.up * probeY;

            int count = Physics.SphereCastNonAlloc(origin, settings.ProbeRadius, dir, probeHits, offset + travel,
                obstacleMask, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            Collider nearestCollider = null;
            for (int i = 0; i < count; i++)
            {
                var hit = probeHits[i];
                if (hit.distance <= 0f) continue;                     // başlangıçta içinde: takılıp kalmasın
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.normal.y > WallNormalMaxY) continue;          // rampa/yokuş engel değil
                if (hit.distance < nearest)
                {
                    nearest = hit.distance;
                    nearestCollider = hit.collider;
                }
            }
            if (float.IsPositiveInfinity(nearest)) return;

            float allowed = nearest - offset;
            if (allowed >= travel) return;
            float limited = allowed <= 0.01f ? 0f : Mathf.Sign(speed) * allowed / dt;
            LogSuddenStop("teker önünde engel", nearestCollider, speed, limited);
            speed = limited;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.collider.transform.IsChildOf(transform)) return;
            if (hit.normal.y > 0.5f)
            {
                if (hit.point.y < transform.position.y + controller.radius) groundNormal = hit.normal;
            }
            else
            {
                wallHit = hit.collider; // dik yüzey: duvar ya da kaldırım yüzü
            }
        }

        private void PlaceRider(Vector3 position)
        {
            // Bakış ofsetini PlayerLook bu karede güncelledi (çalışma sırası 50); roll yok.
            // Baş: virajda ve ayakta sallanırken yana kayar, ayaktayken yükselip gidona eğilir (yalnızca konum).
            Vector3 head = new Vector3(
                Mathf.Sin(lean * Mathf.Deg2Rad) * settings.CameraLeanShift + Mathf.Sin(rock * Mathf.Deg2Rad) * settings.CameraRockShift,
                stand * settings.StandRise,
                stand * settings.StandForward);
            riderTransform.SetPositionAndRotation(position + Vector3.up * riderLift + transform.TransformDirection(head),
                Quaternion.Euler(0f, yaw + riderLook.YawOffset, 0f));
        }

        /// <summary>
        /// Hızlı pedalda sürücü ayağa kalkar ve bisiklet pedal ritmiyle sağa-sola sallanır (GTA tarzı).
        /// Ritim teker dönüşünden gelir ama maxRockFrequency'yi geçmez (velosipedde teker çok hızlı döner).
        /// </summary>
        private void UpdateStanding(bool fast, float dt)
        {
            bool standing = fast && pedaling && controller.isGrounded && speed > settings.RockMinSpeed;
            stand = Mathf.Lerp(stand, standing ? 1f : 0f, 1f - Mathf.Exp(-settings.StandSharpness * dt));
            if (stand < 0.001f)
            {
                stand = 0f;
                rock = 0f;
                rockPhase = 0f; // her kalkış aynı yönden başlasın
                return;
            }
            float radius = rig != null ? rig.WheelRadius : 0.35f;
            float frequency = Mathf.Min(settings.MaxRockFrequency, Mathf.Abs(speed) / (2f * Mathf.PI * Mathf.Max(0.05f, radius)));
            rockPhase = Mathf.Repeat(rockPhase + frequency * 2f * Mathf.PI * dt, 2f * Mathf.PI);
            rock = stand * settings.RockAngle * Mathf.Sin(rockPhase);
        }

        // ---------- İnme ----------

        private void BeginDismount()
        {
            dismountRequested = false;
            if (!TryFindDismountSpot(out dismountTarget)) return; // yer yok: sürmeye devam
            speed = 0f;
            blendFrom = riderTransform.position;
            blend = 0f;
            state = RideState.Dismounting;
            armsView?.EndGrip(); // eller gidonu bırakır
            bodyView?.EndRide(); // gövde seleden kalkar
            HidePrompt();
        }

        private bool TryFindDismountSpot(out Vector3 spot)
        {
            float side = settings.DismountSideOffset;
            float radius = riderMotor.Controller.radius;
            float height = riderMotor.StandingHeight;
            Vector3 fromEye = transform.position + Vector3.up * 1f;

            for (int i = 0; i < 4; i++)
            {
                Vector3 local = i switch
                {
                    0 => new Vector3(side, 0f, -0.1f),                      // sağ
                    1 => new Vector3(-side, 0f, -0.1f),                     // sol
                    2 => new Vector3(0f, 0f, -(settings.RearReach + 0.45f)), // arka
                    _ => new Vector3(0f, 0f, settings.FrontReach + 0.45f)    // ön
                };
                Vector3 probe = transform.TransformPoint(local) + Vector3.up * 0.6f;
                if (!Physics.Raycast(probe, Vector3.down, out var ground, 1.6f, obstacleMask, QueryTriggerInteraction.Ignore)) continue;
                if (ground.normal.y < 0.7f || ground.collider.transform.IsChildOf(transform)) continue;

                Vector3 point = ground.point;
                Vector3 bottom = point + Vector3.up * (radius + 0.05f);
                Vector3 top = point + Vector3.up * Mathf.Max(radius + 0.05f, height - radius);
                if (Physics.CheckCapsule(bottom, top, radius, obstacleMask, QueryTriggerInteraction.Ignore)) continue;
                if (Physics.Linecast(fromEye, point + Vector3.up * 1f, obstacleMask, QueryTriggerInteraction.Ignore)) continue;

                spot = point;
                return true;
            }
            spot = default;
            return false;
        }

        /// <summary>
        /// Sürüşü bitirir. teleportRider: oyuncu verilen noktaya konur (aksi hâlde zaten ışınlandı).
        /// tearingDown: çıkış/sahne boşalması — yalnızca abonelikler bırakılır, oyuncu ve görünüm güncellenmez.
        /// </summary>
        private void EndRide(Vector3 riderPosition, bool teleportRider, bool tearingDown = false)
        {
            if (state == RideState.Parked) return;
            state = RideState.Parked;

            if (GameInput.TryGetControls(out var c))
            {
                c.Player.Use.performed -= OnUsePerformed;
                c.Player.Sprint.performed -= OnSprintPerformed;
            }
            GameInput.SchemeChanged -= OnSchemeChanged;
            if (riderMotor != null) riderMotor.Teleported -= OnRiderTeleported;
            if (tearingDown)
            {
                // Kayıt OnApplicationQuit'te yapıldı; sahne değişiminde son konum burada yazılır.
                if (!isQuitting) SavePose(true); // sahne değişimi: yeni sahnede bisiklette devam
                rider = null;
                riderMotor = null;
                riderLook = null;
                riderTransform = null;
                promptView = null;
                promptShown = false;
                armsView = null; // kollar da sahneyle birlikte gidiyor
                bodyView = null;
                return;
            }
            HidePrompt();
            armsView?.EndGrip();
            armsView = null;
            bodyView?.EndRide();
            bodyView = null;

            // Sahne kapanırken oyuncu bisikletten önce yok edilmiş olabilir.
            if (riderLook != null)
            {
                riderLook.ExtraFov = 0f;
                riderLook.EndAnchoredYaw();
            }
            if (riderMotor != null && teleportRider)
                riderMotor.Teleport(riderPosition, riderTransform.rotation); // CharacterController'ı açar
            if (rider != null) rider.SetRiding(false);

            rider = null;
            riderMotor = null;
            riderLook = null;
            riderTransform = null;
            promptView = null;

            speed = 0f;
            momentum = 0f;
            lean = 0f;
            stand = rock = rockPhase = 0f;
            pedaling = false;
            dismountRequested = false;
            SetColliderHeight(settings.ParkedHeight);
            curbClimbRemaining = 0f;
            riderLift = 0f;
            UpdateWheelGrounding(0f, true);
            if (rig != null) rig.UpdatePose(0f, false, steerAngle, 0f, visualPitch, visualHeight, 0f);
            if (rollingSource != null) rollingSource.volume = 0f;

            SavePose(false);
            RiddenChanged?.Invoke(false);
        }

        // ---------- Yardımcılar ----------

        private void UpdatePrompt()
        {
            bool show = state == RideState.Riding && rider.ControlEnabled && !dismountRequested
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
            if (rollingSource == null) return;
            float t = controller.isGrounded ? Mathf.Clamp01(Mathf.Abs(speed) / Mathf.Max(0.1f, settings.FastSpeed)) : 0f;
            rollingSource.volume = t * rollingMaxVolume;
            rollingSource.pitch = 0.8f + 0.4f * t;
            if (t > 0.01f && !rollingSource.isPlaying && rollingSource.clip != null) rollingSource.Play();
        }

        private void SetColliderHeight(float height)
        {
            controller.height = Mathf.Max(height, controller.radius * 2f);
            controller.center = new Vector3(0f, controller.height * 0.5f, 0f);
        }

        /// <summary>Noktanın hemen altında (bisiklet ve sürücü dışında) zemin var mı? Yalnızca açılış ve kayıtta.</summary>
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

        private void SavePose(bool ridden)
        {
            var save = SaveSystem.Instance;
            if (save == null || save.Data == null || string.IsNullOrEmpty(saveId)) return;
            // Havada / düşerken kaydetme: açılışta oyuncu boşlukta başlardı.
            if (!HasGroundBelow(transform.position)) return;
            save.Data.SetPose(saveId, transform.position, yaw, ridden);
            save.Save();
        }
    }
}
