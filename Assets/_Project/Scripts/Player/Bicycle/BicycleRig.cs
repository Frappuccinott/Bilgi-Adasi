using UnityEngine;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// Bisikletin görünümü: tekerlek dönüşü, gidon, pedal (aynakol) ve yatış. Mantık bilmez; Bicycle her karede UpdatePose çağırır.
    /// Model değişince: eski parçaları (_Placeholder) sil, yeni modeli Visual altına koy, aşağıdaki parçaları yeniden bağla.
    /// Dönüşler parçanın başlangıç (dinlenme) dönüşüne göre uygulanır; eksenler parçanın YEREL eksenidir,
    /// modelin ekseni farklıysa yalnızca eksen alanını değiştir (ters dönüyorsa ekseni eksi yap).
    /// </summary>
    public sealed class BicycleRig : MonoBehaviour
    {
        [Header("Parçalar (boş bırakılan çizilmez)")]
        [Tooltip("Ön teker; pivotu göbekte olmalı.")]
        [SerializeField] private Transform frontWheel;
        [Tooltip("Arka teker; pivotu göbekte olmalı.")]
        [SerializeField] private Transform rearWheel;
        [Tooltip("Gidon + çatal + ön teker grubu; pivotu furş borusunda.")]
        [SerializeField] private Transform steering;
        [Tooltip("Aynakol (pedal kolları); pivotu orta göbekte.")]
        [SerializeField] private Transform crank;
        [Tooltip("Pedallar aynakolun çocuğu olmalı; ters döndürülerek yere paralel tutulur.")]
        [SerializeField] private Transform pedalLeft;
        [SerializeField] private Transform pedalRight;
        [Tooltip("Virajda yatan kök. Pivotu yerde olmalı (genelde Visual).")]
        [SerializeField] private Transform leanRoot;

        [Header("Eksenler (parçanın yerel ekseni)")]
        [SerializeField] private Vector3 wheelAxis = Vector3.right;
        [SerializeField] private Vector3 steerAxis = Vector3.up;
        [SerializeField] private Vector3 crankAxis = Vector3.right;
        [SerializeField] private Vector3 pedalAxis = Vector3.right;
        [SerializeField] private Vector3 leanAxis = Vector3.forward;
        [Tooltip("Kaldırımda öne/arkaya eğilme ekseni (leanRoot'un yerel ekseni; + burun aşağı).")]
        [SerializeField] private Vector3 pitchAxis = Vector3.right;

        [Header("Ölçüler")]
        [Tooltip("Teker yarıçapı (m). Dönüş hızı buradan: açısal hız = hız / yarıçap.")]
        [SerializeField] private float wheelRadius = 0.34f;
        [Tooltip("Vites oranı: aynakolun bir turunda teker kaç tur döner.")]
        [SerializeField] private float gearRatio = 2.6f;
        [Tooltip("Pedal bırakılınca aynakolun durma hızı (°/sn²).")]
        [SerializeField] private float crankStopRate = 720f;

        private Quaternion frontRest, rearRest, steerRest, crankRest, pedalLeftRest, pedalRightRest, leanRest;
        private Vector3 leanRestPosition;
        private float wheelAngle;
        private float crankAngle;
        private float crankRate;

        public float WheelRadius => wheelRadius;

        private void Awake() => CacheRestPose();

        /// <summary>Parçaların başlangıç dönüşlerini saklar. Parça referansı çalışırken değişirse yeniden çağır.</summary>
        public void CacheRestPose()
        {
            frontRest = Rest(frontWheel);
            rearRest = Rest(rearWheel);
            steerRest = Rest(steering);
            crankRest = Rest(crank);
            pedalLeftRest = Rest(pedalLeft);
            pedalRightRest = Rest(pedalRight);
            leanRest = Rest(leanRoot);
            leanRestPosition = leanRoot != null ? leanRoot.localPosition : Vector3.zero;
        }

        /// <summary>Teker göbeklerinin bisiklet kökündeki ileri (Z) konumu: kaldırımda tekerin altındaki zemin buradan yoklanır.</summary>
        public bool TryGetWheelContactZ(Transform root, out float front, out float rear)
        {
            front = rear = 0f;
            if (frontWheel == null || rearWheel == null || root == null) return false;
            front = root.InverseTransformPoint(frontWheel.position).z;
            rear = root.InverseTransformPoint(rearWheel.position).z;
            return front - rear > 0.1f;
        }

        /// <param name="speed">İleri hız (m/s, geri negatif).</param>
        /// <param name="pedaling">Pedal çevriliyor mu (bırakınca aynakol durur, teker döner: serbest dişli).</param>
        /// <param name="steerAngle">Gidon açısı (°, sağ pozitif).</param>
        /// <param name="leanAngle">Yatış (°, sağa yatış pozitif).</param>
        /// <param name="pitchAngle">Öne/arkaya eğim (°, burun aşağı pozitif) — kaldırımda tekerler yere otursun.</param>
        /// <param name="heightOffset">Modelin yerden yüksekliği (m), eğimle birlikte.</param>
        public void UpdatePose(float speed, bool pedaling, float steerAngle, float leanAngle, float pitchAngle, float heightOffset, float dt)
        {
            float radius = Mathf.Max(0.05f, wheelRadius);
            float wheelRate = speed / radius * Mathf.Rad2Deg;
            wheelAngle = Mathf.Repeat(wheelAngle + wheelRate * dt, 360f);

            float targetCrankRate = pedaling && speed > 0f ? wheelRate / Mathf.Max(0.1f, gearRatio) : 0f;
            crankRate = pedaling ? targetCrankRate : Mathf.MoveTowards(crankRate, 0f, crankStopRate * dt);
            crankAngle = Mathf.Repeat(crankAngle + crankRate * dt, 360f);

            if (frontWheel != null) frontWheel.localRotation = frontRest * Quaternion.AngleAxis(wheelAngle, wheelAxis);
            if (rearWheel != null) rearWheel.localRotation = rearRest * Quaternion.AngleAxis(wheelAngle, wheelAxis);
            if (steering != null) steering.localRotation = steerRest * Quaternion.AngleAxis(steerAngle, steerAxis);
            if (crank != null) crank.localRotation = crankRest * Quaternion.AngleAxis(crankAngle, crankAxis);
            if (pedalLeft != null) pedalLeft.localRotation = pedalLeftRest * Quaternion.AngleAxis(-crankAngle, pedalAxis);
            if (pedalRight != null) pedalRight.localRotation = pedalRightRest * Quaternion.AngleAxis(-crankAngle, pedalAxis);
            // Sağa yatış: üst +X'e gider → ileri eksen etrafında negatif açı.
            if (leanRoot != null)
            {
                leanRoot.localPosition = leanRestPosition + Vector3.up * heightOffset;
                leanRoot.localRotation = leanRest * Quaternion.AngleAxis(pitchAngle, pitchAxis) * Quaternion.AngleAxis(-leanAngle, leanAxis);
            }
        }

        private static Quaternion Rest(Transform t) => t != null ? t.localRotation : Quaternion.identity;
    }
}
