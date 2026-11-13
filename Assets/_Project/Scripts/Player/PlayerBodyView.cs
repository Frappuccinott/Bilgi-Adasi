using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// Oyuncunun görünen gövdesi (baş ve kollar hariç; kollar kameradaki ayrı rig). Oyuncunun çocuğu bir sarmalayıcıya
    /// eklenir; Animator modelde (Ogrenci_Govde_Rig).
    /// Yerde: yürüme/çömelme karışımı (Hiz, Comel), tempo PlayerMotor'un adım döngüsüyle aynı (adım sesiyle senkron).
    /// Havada: zıplama pozu. Bisiklette: kalça seleye oturur, ayaklar pedallara iki kemik IK ile basar.
    /// Aşağı bakınca gövde biraz geri kayar (kamera göğsün içine girmesin). İstasyon modunda gizlenir.
    /// Kollar (RiderArmsIK, 10000) ve Cinemachine'den sonra çalışır.
    /// </summary>
    [DefaultExecutionOrder(10001)]
    public sealed class PlayerBodyView : RegisteredView<IRiderBodyView>, IRiderBodyView
    {
        private static readonly int StateGround = Animator.StringToHash("Yer");
        private static readonly int StateAir = Animator.StringToHash("Havada");
        private static readonly int StateBike = Animator.StringToHash("Bisiklet");
        private static readonly int ParamSpeed = Animator.StringToHash("Hiz");
        private static readonly int ParamCrouch = Animator.StringToHash("Comel");
        private static readonly int ParamTempo = Animator.StringToHash("Tempo");

        [Header("Bağlantılar")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerLook look;
        [Tooltip("Kalça kemiği (Kalca): bisiklette seleye oturan nokta.")]
        [SerializeField] private Transform hips;
        [SerializeField] private Transform thighLeft, shinLeft, footLeft;
        [SerializeField] private Transform thighRight, shinRight, footRight;
        [Tooltip("İstasyonda gizlenen görünür parçalar (gölge başı hariç).")]
        [SerializeField] private Renderer[] bodyRenderers;

        [Header("Yerde")]
        [Tooltip("Bu hızda (m/s) yürüme klibi tam ağırlık alır.")]
        [SerializeField] private float fullWalkSpeed = 1.2f;
        [Tooltip("Bu süre havada kalınca zıplama pozu (sn). Basamaktan inerken titremesin.")]
        [SerializeField] private float airDelay = 0.12f;
        [SerializeField] private float stateFade = 0.15f;
        [Tooltip("Aşağı bakınca gövde bu kadar geri kayar (m).")]
        [SerializeField] private float lookDownBackOffset = 0.16f;
        [SerializeField] private float lookDownStart = 25f;
        [SerializeField] private float lookDownEnd = 70f;

        [Header("Bisiklet")]
        [SerializeField] private float mountDuration = 0.35f;
        [SerializeField] private float dismountDuration = 0.3f;
        [Tooltip("Ayak bileğinin pedalın üstündeki yüksekliği (m): taban + pedal kalınlığı.")]
        [SerializeField] private float ankleAbovePedal = 0.11f;
        [Tooltip("Ayak bileği pedalın bu kadar gerisinde (m): pedala ayak ortasıyla değil parmak dibiyle basılır.")]
        [SerializeField] private float ankleBehindPedal = 0.07f;
        [Tooltip("Pedalda ayak ucu aşağı (°).")]
        [SerializeField] private float toeDown = 12f;
        [Tooltip("Diz ipucu: uyluğa göre gövde uzayında (sağ bacak; sol aynalanır). X dışa, Y yukarı, Z ileri (m).")]
        [SerializeField] private Vector3 kneeHint = new Vector3(0.08f, 0.35f, 0.8f);

        private AnimatedPoseCache legPose;
        private Quaternion footLeftRest, footRightRest; // kök uzayında (ayak yere paralel)
        private int currentState;
        private float airTime;
        private bool hidden;

        private BicycleRig ride;
        private bool riding;
        private float rideWeight;
        private Transform pedalLeft, pedalRight;

        private void Awake()
        {
            if (motor == null) motor = GetComponentInParent<PlayerMotor>();
            if (look == null) look = GetComponentInParent<PlayerLook>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            legPose = new AnimatedPoseCache(new[] { thighLeft, shinLeft, footLeft, thighRight, shinRight, footRight });
            // Bağlama pozu ayakta, ayak tabanı yere paralel: pedalda bu yön esas alınır.
            Quaternion inv = Quaternion.Inverse(transform.rotation);
            footLeftRest = inv * footLeft.rotation;
            footRightRest = inv * footRight.rotation;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ride = null;
            riding = false;
            rideWeight = 0f;
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        // ---------- IRiderBodyView ----------

        public void BeginRide(BicycleRig rig, bool instant)
        {
            if (rig == null || rig.Seat == null) return;
            ride = rig;
            riding = true;
            // Modelde pedal adları kenara göre değil; sol ayak bisikletin solundaki (yerel x < 0) pedala basar.
            Transform a = rig.PedalLeft, b = rig.PedalRight;
            bool aIsLeft = a != null && b != null && rig.transform.InverseTransformPoint(a.position).x <
                           rig.transform.InverseTransformPoint(b.position).x;
            pedalLeft = aIsLeft ? a : b;
            pedalRight = aIsLeft ? b : a;
            if (instant) rideWeight = 1f;
            SetState(StateBike, instant ? 0f : stateFade);
        }

        public void EndRide()
        {
            riding = false;
            SetState(StateGround, stateFade);
        }

        // ---------- Döngü ----------

        private void Update()
        {
            if (animator == null || motor == null) return;
            float dt = Time.deltaTime;
            if (riding) return; // durum BeginRide'da

            bool grounded = !motor.isActiveAndEnabled || motor.IsGrounded;
            airTime = grounded ? 0f : airTime + dt;
            SetState(airTime > airDelay ? StateAir : StateGround, stateFade);

            float speed = motor.isActiveAndEnabled ? motor.HorizontalSpeed : 0f;
            var s = motor.Settings;
            float crouch = s != null ? Mathf.InverseLerp(motor.StandingHeight, s.CrouchHeight, motor.CurrentHeight) : 0f;
            // Yürüme klibi 1 sn = 2 adım: tempo = saniyedeki adım / 2 → adım sesiyle aynı ritim.
            float stride = s != null ? Mathf.Max(0.1f, s.StrideLength) : 1.9f;
            animator.SetFloat(ParamSpeed, Mathf.Clamp01(speed / Mathf.Max(0.1f, fullWalkSpeed)), 0.08f, dt);
            animator.SetFloat(ParamCrouch, crouch);
            animator.SetFloat(ParamTempo, Mathf.Max(0.5f, speed / (2f * stride)));
        }

        private void SetState(int state, float fade)
        {
            if (state == currentState || animator == null) return;
            currentState = state;
            if (fade <= 0f) animator.Play(state, 0, 0f);
            else animator.CrossFadeInFixedTime(state, fade, 0);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            UpdateVisibility();

            float duration = riding ? mountDuration : dismountDuration;
            rideWeight = Mathf.MoveTowards(rideWeight, riding ? 1f : 0f, dt / Mathf.Max(0.01f, duration));
            if (ride == null || ride.Seat == null) rideWeight = 0f;

            // Yerde: oyuncunun çocuğu, aşağı bakınca biraz geri.
            float pitch = look != null ? look.Pitch : 0f;
            float back = lookDownBackOffset * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(lookDownStart, lookDownEnd, pitch));
            transform.SetLocalPositionAndRotation(new Vector3(0f, 0f, -back), Quaternion.identity);

            if (rideWeight <= 0f)
            {
                if (legPose.HasWritten) legPose.Sync(); // son IK pozu kalmasın
                if (!riding) ride = null;
                return;
            }

            float w = Mathf.SmoothStep(0f, 1f, rideWeight);
            legPose.Sync();

            // Kalça seleye: kök dönüşü selenin (bisikletle yatan) dönüşü; kalça kemiği sele noktasına.
            Transform seat = ride.Seat;
            Vector3 hipLocal = transform.InverseTransformPoint(hips.position);
            Quaternion seatRot = seat.rotation;
            // Aşağı bakınca göğüs kameranın önünü kapatmasın: yerdekinin yarısı kadar geri (kalça seleden pek kopmaz).
            Vector3 seatPos = seat.position - seatRot * hipLocal - seat.forward * (back * 0.5f);
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, seatPos, w),
                Quaternion.Slerp(transform.rotation, seatRot, w));

            Vector3 up = seat.up, fwd = seat.forward;
            Leg(thighLeft, shinLeft, footLeft, footLeftRest, pedalLeft, -1f, up, fwd, w);
            Leg(thighRight, shinRight, footRight, footRightRest, pedalRight, 1f, up, fwd, w);
            legPose.Record();
        }

        private void Leg(Transform thigh, Transform shin, Transform foot, Quaternion footRest, Transform pedal, float side,
            Vector3 up, Vector3 fwd, float w)
        {
            if (pedal == null) return;
            Vector3 ankle = pedal.position + up * ankleAbovePedal - fwd * ankleBehindPedal;
            Vector3 hint = thigh.position + transform.rotation * new Vector3(kneeHint.x * side, kneeHint.y, kneeHint.z);
            TwoBoneIK.Solve(thigh, shin, foot, ankle, hint, w);
            Quaternion target = transform.rotation * Quaternion.AngleAxis(toeDown, Vector3.right) * footRest;
            foot.rotation = Quaternion.Slerp(foot.rotation, target, w);
        }

        private void UpdateVisibility()
        {
            bool hide = GameInput.Mode == InputMode.Station;
            if (hide == hidden || bodyRenderers == null) return;
            hidden = hide;
            foreach (var r in bodyRenderers)
                if (r != null) r.enabled = !hide;
        }
    }
}
