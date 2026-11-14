using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Ana menü arka planı: haritanın üzerinde kuş bakışı, kesintisiz 360° dönen kamera (Figma Ekran/Giriş'teki
    /// "Oyun görüntüsü"). Pivotun çevresinde sabit hızla döner; yükseklik ve yarıçap çok yavaş "nefes alır".
    /// Roll yok (LookRotation, dünya yukarısı). Menü solda durduğu için pivot ekranın sağına kaydırılır (yawOffset).
    /// Zamandan bağımsız (unscaled): menü duraklasa da döner. Kurulum 20 sahnede kurar.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class MenuFlyover : MonoBehaviour
    {
        [Header("Yörünge")]
        [Tooltip("Dönülen nokta (dünya). Kurulum 20: harita merkezi, zemin yüksekliğinde.")]
        [SerializeField] private Vector3 pivot = new Vector3(-5f, 0f, 12f);
        [SerializeField, Min(1f)] private float radius = 55f;
        [Tooltip("Pivotun üstünde kamera yüksekliği (m).")]
        [SerializeField, Min(1f)] private float height = 64f;
        [Tooltip("Bakılan nokta pivotun ne kadar üstünde (m).")]
        [SerializeField] private float lookHeight = 0f;
        [Tooltip("Bir tam tur (360°) kaç saniye.")]
        [SerializeField, Min(5f)] private float secondsPerTurn = 120f;
        [SerializeField] private float startAngle = 210f;
        [Tooltip("Pozitif: saat yönünün tersine (yukarıdan bakınca).")]
        [SerializeField] private bool counterClockwise = true;

        [Header("Kompozisyon")]
        [Tooltip("Kamerayı sola çevirir (derece): pivot ekranın sağına kayar, soldaki menü haritayı örtmez.")]
        [SerializeField, Range(-30f, 30f)] private float yawOffset = -10f;

        [Header("Nefes (çok yavaş, baş döndürmez)")]
        [SerializeField, Min(0f)] private float heightBreath = 4f;
        [SerializeField, Min(0f)] private float radiusBreath = 6f;
        [Tooltip("Nefes döngüsü (sn). Turla aynı olmasın ki her tur biraz farklı görünsün.")]
        [SerializeField, Min(5f)] private float breathPeriod = 47f;

        private float angle;

        private void OnEnable()
        {
            angle = startAngle;
            Apply(0f);
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            angle = Mathf.Repeat(angle + (counterClockwise ? 1f : -1f) * 360f / secondsPerTurn * dt, 360f);
            Apply(Time.unscaledTime);
        }

        private void Apply(float time)
        {
            float b = Mathf.Sin(time * Mathf.PI * 2f / breathPeriod);
            float r = radius + radiusBreath * b;
            float h = height + heightBreath * Mathf.Sin(time * Mathf.PI * 2f / (breathPeriod * 1.618f));
            float a = angle * Mathf.Deg2Rad;
            var pos = pivot + new Vector3(Mathf.Sin(a) * r, h, Mathf.Cos(a) * r);
            var look = Quaternion.LookRotation(pivot + Vector3.up * lookHeight - pos, Vector3.up);
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yawOffset, 0f) * look);
        }

        /// <summary>Kurulum (editör) için: yörünge merkezi ve boyu.</summary>
        public void Configure(Vector3 center, float orbitRadius, float orbitHeight)
        {
            pivot = center;
            radius = orbitRadius;
            height = orbitHeight;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            angle = startAngle;
            Apply(0f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.82f, 0.25f, 0.8f);
            const int seg = 64;
            Vector3 prev = pivot + new Vector3(0f, height, radius);
            for (int i = 1; i <= seg; i++)
            {
                float t = i / (float)seg * Mathf.PI * 2f;
                var p = pivot + new Vector3(Mathf.Sin(t) * radius, height, Mathf.Cos(t) * radius);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
            Gizmos.DrawWireSphere(pivot, 2f);
        }
#endif
    }
}
