using UnityEngine;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// Adım hissi (CLAUDE.md §4.4): çok hafif dikey sallanma (≤ 0.025 m) + yarım frekansta küçük yan salınım,
    /// inişte küçük kamera çökmesi. Ayarlardan kapatılabilir. Göz yüksekliği CameraRoot'ta (PlayerMotor);
    /// bu bileşen yalnızca pivotun yerel konumuna küçük bir ofset ekler.
    /// </summary>
    public sealed class HeadBob : MonoBehaviour
    {
        private const float ReturnSharpness = 10f;
        private const float DipRecoverSharpness = 9f;
        private const float SwayRatio = 0.5f;

        [SerializeField] private PlayerMovementSettings settings;
        [SerializeField] private PlayerMotor motor;
        [Tooltip("Sallanacak transform (kamera pivotu).")]
        [SerializeField] private Transform target;

        private Vector3 basePosition;
        private Vector3 bobOffset;
        private float phase;
        private float dip;

        private void Awake()
        {
            if (target != null) basePosition = target.localPosition;
        }

        private void OnEnable()
        {
            if (motor != null) motor.Landed += OnLanded;
        }

        private void OnDisable()
        {
            if (motor != null) motor.Landed -= OnLanded;
            dip = 0f;
        }

        private void OnLanded(float impactSpeed)
        {
            if (settings == null || !settings.HeadBobEnabled) return;
            dip = -Mathf.Min(settings.LandingDipMax, impactSpeed * settings.LandingDipPerSpeed);
        }

        private void LateUpdate()
        {
            if (settings == null || motor == null || target == null) return;
            float dt = Time.deltaTime;

            Vector3 desired = Vector3.zero;
            if (settings.HeadBobEnabled && motor.IsGrounded)
            {
                float speedRatio = settings.WalkSpeed > 0f ? motor.HorizontalSpeed / settings.WalkSpeed : 0f;
                if (speedRatio > 0.05f)
                {
                    // Adım frekansı hızla artar ama koşuda ikiye katlanmaz (daha sakin).
                    float frequency = settings.HeadBobFrequency * Mathf.Lerp(1f, 1.35f, Mathf.Clamp01(speedRatio - 1f));
                    // |sin| her π'de bir tepe yapar → π / adım.
                    phase += dt * frequency * Mathf.PI;
                    float amplitude = settings.HeadBobAmplitude * Mathf.Clamp01(speedRatio);
                    // |sin| → her adımda bir iniş; yan salınım iki adımda bir tur.
                    desired.y = -Mathf.Abs(Mathf.Sin(phase)) * amplitude;
                    desired.x = Mathf.Sin(phase) * amplitude * SwayRatio;
                }
            }

            bobOffset = Vector3.Lerp(bobOffset, desired, 1f - Mathf.Exp(-ReturnSharpness * 2f * dt));
            dip = Mathf.Lerp(dip, 0f, 1f - Mathf.Exp(-DipRecoverSharpness * dt));

            target.localPosition = basePosition + bobOffset + Vector3.up * dip;
        }
    }
}
