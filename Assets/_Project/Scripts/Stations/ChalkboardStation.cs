using BilgiAdasi.Player;
using UnityEngine;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Karatahta: aktif görevin denklemini yakından okuma (yazıyı OrderController → IChalkboardView verir).
    /// Girişte istasyon kamerası tahta yazısının ortasına, yatay ve tam karşıdan bakacak şekilde hizalanır;
    /// tahta sahnede taşınsa ya da döndürülse de kamera eğik kalmaz.
    /// </summary>
    public sealed class ChalkboardStation : StationBase
    {
        [Tooltip("Tahta yazısının RectTransform'u. Boşsa çocuklardaki ChalkboardText kullanılır.")]
        [SerializeField] private RectTransform boardFace;
        [Tooltip("Kameranın tahtaya uzaklığı (m). 0: CameraPoint'in sahnedeki uzaklığı korunur.")]
        [SerializeField, Min(0f)] private float viewDistance;

        private const float FallbackDistance = 1.3f;

        protected override void Awake()
        {
            base.Awake();
            if (boardFace == null)
            {
                var board = GetComponentInChildren<ChalkboardText>(true);
                if (board != null) boardFace = board.transform as RectTransform;
            }
        }

        protected override void OnEnter() => AlignCamera();

        private void AlignCamera()
        {
            var cam = StationCamera;
            if (cam == null || boardFace == null) return;

            // Okuma yönü: yazının ileri ekseni (TMP önden bu yönde bakılınca okunur), yataya indirgenmiş — eğim yok.
            Vector3 forward = Vector3.ProjectOnPlane(boardFace.forward, Vector3.up);
            if (forward.sqrMagnitude < 1e-4f) return;
            forward.Normalize();

            Vector3 center = boardFace.TransformPoint(boardFace.rect.center);
            float distance = viewDistance;
            if (distance <= 0f)
            {
                distance = Vector3.Dot(center - cam.transform.position, forward);
                if (distance < 0.3f) distance = FallbackDistance;
            }
            cam.transform.SetPositionAndRotation(center - forward * distance, Quaternion.LookRotation(forward, Vector3.up));
        }
    }
}
