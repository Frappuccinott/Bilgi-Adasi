using UnityEngine;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// Adım hissi (CLAUDE.md §4.4). PlayerMotor'un adım döngüsüne bağlıdır (ses ile aynı ritim):
    /// - Ayak yere bastığında kamera hafifçe çöker (≤ 0.025 m) ve başını ~0.3° eğer, adım ortasında yükselir.
    /// - İki adımda bir küçük yan salınım (konum; roll yok).
    /// - İnişte küçük çökme; dururken çok yavaş nefes salınımı.
    /// Pitch ofseti PlayerLook'un yazdığı dönüşün üstüne eklenir (LateUpdate, CinemachineBrain'den önce).
    /// </summary>
    public sealed class HeadBob : MonoBehaviour
    {
        private const float BlendSharpness = 12f;
        private const float DipRecoverSharpness = 9f;
        private const float SwayRatio = 0.5f;

        [SerializeField] private PlayerMovementSettings settings;
        [SerializeField] private PlayerMotor motor;
        [Tooltip("Sallanacak transform (kamera pivotu).")]
        [SerializeField] private Transform target;

        private Vector3 basePosition;
        private Vector3 bobOffset;
        private float bobPitch;
        private float dip;
        private float breathTime;

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
            bobOffset = Vector3.zero;
            bobPitch = 0f;
            if (target != null) target.localPosition = basePosition;
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

            Vector3 desiredOffset = Vector3.zero;
            float desiredPitch = 0f;

            if (settings.HeadBobEnabled)
            {
                float speedRatio = settings.WalkSpeed > 0f ? motor.HorizontalSpeed / settings.WalkSpeed : 0f;
                float walkWeight = motor.IsGrounded ? Mathf.Clamp01(speedRatio) : 0f;

                if (walkWeight > 0.01f)
                {
                    float phase = motor.StepCycle * Mathf.PI;
                    float strike = 1f - Mathf.Abs(Mathf.Sin(phase)); // tam adımda 1 (ayak yere basar), ortada 0
                    desiredOffset.y = -strike * settings.HeadBobAmplitude * walkWeight;
                    desiredOffset.x = Mathf.Sin(phase) * settings.HeadBobAmplitude * SwayRatio * walkWeight;
                    desiredPitch = strike * settings.HeadBobPitch * walkWeight;
                }

                // Nefes: hareket azaldıkça belirginleşir.
                float idleWeight = 1f - Mathf.Clamp01(speedRatio * 3f);
                if (idleWeight > 0.01f && settings.BreathingRate > 0f)
                {
                    breathTime += dt;
                    float breath = Mathf.Sin(breathTime * settings.BreathingRate * Mathf.PI * 2f);
                    desiredOffset.y += breath * settings.BreathingAmplitude * idleWeight;
                    desiredPitch += -breath * settings.BreathingPitch * idleWeight;
                }
            }

            float blend = 1f - Mathf.Exp(-BlendSharpness * dt);
            bobOffset = Vector3.Lerp(bobOffset, desiredOffset, blend);
            bobPitch = Mathf.Lerp(bobPitch, desiredPitch, blend);
            dip = Mathf.Lerp(dip, 0f, 1f - Mathf.Exp(-DipRecoverSharpness * dt));

            target.localPosition = basePosition + bobOffset + Vector3.up * dip;
            // PlayerLook bu karede mutlak pitch yazdı; üstüne küçük ofset eklenir (birikmez).
            target.localRotation *= Quaternion.Euler(bobPitch, 0f, 0f);
        }
    }
}
