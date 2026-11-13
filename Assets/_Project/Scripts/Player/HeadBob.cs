using UnityEngine;

namespace BilgiAdasi.Player
{
    /// <summary>Çok hafif kafa sallanması (≤ 0.025 m). Ayarlardan kapatılabilir. Durunca yumuşakça sıfıra döner.</summary>
    public sealed class HeadBob : MonoBehaviour
    {
        private const float ReturnSpeed = 8f;

        [SerializeField] private PlayerMovementSettings settings;
        [SerializeField] private PlayerMotor motor;
        [Tooltip("Sallanacak transform (kamera pivotu). Yalnızca yerel Y konumu değişir.")]
        [SerializeField] private Transform target;

        private float baseY;
        private float phase;

        private void Start()
        {
            if (target != null) baseY = target.localPosition.y;
        }

        private void LateUpdate()
        {
            if (settings == null || motor == null || target == null) return;

            float offset = 0f;
            if (settings.HeadBobEnabled && motor.enabled && motor.IsGrounded)
            {
                float speedRatio = settings.WalkSpeed > 0f ? motor.HorizontalSpeed / settings.WalkSpeed : 0f;
                if (speedRatio > 0.05f)
                {
                    phase += Time.deltaTime * settings.HeadBobFrequency * speedRatio * Mathf.PI * 2f;
                    offset = Mathf.Sin(phase) * settings.HeadBobAmplitude * Mathf.Min(speedRatio, 1f);
                }
            }

            var p = target.localPosition;
            float y = offset != 0f ? baseY + offset : Mathf.Lerp(p.y, baseY, Time.deltaTime * ReturnSpeed);
            target.localPosition = new Vector3(p.x, y, p.z);
        }

        /// <summary>Büyüme göz yüksekliğini değiştirince taban yükseklik güncellenir.</summary>
        public void SetBaseHeight(float localY) => baseY = localY;
    }
}
