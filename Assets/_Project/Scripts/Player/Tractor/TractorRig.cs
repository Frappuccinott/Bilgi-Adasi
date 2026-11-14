using UnityEngine;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// Traktörün görünümü (mantık bilmez): teker dönüşü, ön aks sapması, direksiyon simidi, volan, gövde eğimi, duman.
    /// Model değişince yalnızca bu bileşenin referansları yeniden bağlanır (Kurulum → 23).
    /// Hareketler hıza bağlı olduğu için kodla döner; pişirilmiş klip yok (16-traktor §0).
    /// </summary>
    public sealed class TractorRig : MonoBehaviour
    {
        [Header("Parçalar (Traktor.fbx)")]
        [SerializeField] private Transform rearWheels;
        [Tooltip("Ön tekerler ön aksın çocuğu olmalı: aks sapar, tekerler kendi ekseninde döner.")]
        [SerializeField] private Transform frontWheels;
        [SerializeField] private Transform frontAxle;
        [SerializeField] private Transform steeringWheel;
        [SerializeField] private Transform flywheel;
        [Tooltip("Eğim (öne/arkaya) bu objeye uygulanır; genelde modelin kökü (Visual).")]
        [SerializeField] private Transform pitchRoot;

        [Header("Sürücü noktaları")]
        [SerializeField] private Transform gripLeft;
        [SerializeField] private Transform gripRight;
        [SerializeField] private Transform seat;
        [SerializeField] private Transform footLeft;
        [SerializeField] private Transform footRight;

        [Header("Eksenler (parçanın yerelinde)")]
        [SerializeField] private Vector3 wheelAxis = Vector3.right;
        [SerializeField] private Vector3 axleSteerAxis = Vector3.up;
        [Tooltip("Direksiyon mili ekseni (simidin yereli). Simit yataydan 65° dik, sürücüye dönük (harita_tarla.py STEER_TILT).")]
        [SerializeField] private Vector3 steeringWheelAxis = new Vector3(0f, 0.423f, -0.906f);
        [SerializeField] private Vector3 flywheelAxis = Vector3.right;

        [Header("Ölçüler")]
        [SerializeField] private float rearWheelRadius = 0.88f;
        [SerializeField] private float frontWheelRadius = 0.48f;

        [Header("Duman (isteğe bağlı)")]
        [SerializeField] private ParticleSystem smoke;
        [SerializeField] private float smokeIdleRate = 3f;
        [SerializeField] private float smokeMaxRate = 14f;

        private Quaternion rearRest, frontRest, axleRest, steeringRest, flywheelRest, pitchRest;
        private Vector3 pitchRestPosition;
        private float rearAngle, frontAngle, flywheelAngle;
        private bool cached;

        public Transform GripLeft => gripLeft;
        public Transform GripRight => gripRight;
        public bool HasGrips => gripLeft != null && gripRight != null;
        public Transform Seat => seat;
        public Transform FootLeft => footLeft;
        public Transform FootRight => footRight;

        private void Awake() => CacheRestPose();

        public void CacheRestPose()
        {
            if (cached) return;
            if (rearWheels != null) rearRest = rearWheels.localRotation;
            if (frontWheels != null) frontRest = frontWheels.localRotation;
            if (frontAxle != null) axleRest = frontAxle.localRotation;
            if (steeringWheel != null) steeringRest = steeringWheel.localRotation;
            if (flywheel != null) flywheelRest = flywheel.localRotation;
            if (pitchRoot != null)
            {
                pitchRest = pitchRoot.localRotation;
                pitchRestPosition = pitchRoot.localPosition;
            }
            cached = true;
        }

        /// <param name="speed">İleri hız (m/s, geri negatif).</param>
        /// <param name="steerAngle">Ön teker sapması (°, sağ pozitif).</param>
        /// <param name="steeringWheelAngle">Simidin dönüşü (°).</param>
        /// <param name="engine01">Motor yükü 0–1 (volan ve duman); motor kapalıysa negatif.</param>
        /// <param name="pitchAngle">Gövde eğimi (°, burun yukarı negatif).</param>
        /// <param name="heightOffset">Gövdenin zemine göre yükseklik farkı (m).</param>
        public void UpdatePose(float speed, float steerAngle, float steeringWheelAngle, float engine01,
            float idleRps, float maxRps, float pitchAngle, float heightOffset, float dt)
        {
            if (!cached) CacheRestPose();
            float deg = speed * Mathf.Rad2Deg * dt;
            rearAngle = Mathf.Repeat(rearAngle + deg / Mathf.Max(0.05f, rearWheelRadius), 360f);
            frontAngle = Mathf.Repeat(frontAngle + deg / Mathf.Max(0.05f, frontWheelRadius), 360f);

            if (rearWheels != null) rearWheels.localRotation = rearRest * Quaternion.AngleAxis(rearAngle, wheelAxis);
            if (frontWheels != null) frontWheels.localRotation = frontRest * Quaternion.AngleAxis(frontAngle, wheelAxis);
            if (frontAxle != null) frontAxle.localRotation = axleRest * Quaternion.AngleAxis(steerAngle, axleSteerAxis);
            if (steeringWheel != null)
                steeringWheel.localRotation = steeringRest * Quaternion.AngleAxis(steeringWheelAngle, steeringWheelAxis);

            bool running = engine01 >= 0f;
            float rps = running ? Mathf.Lerp(idleRps, maxRps, engine01) : 0f;
            flywheelAngle = Mathf.Repeat(flywheelAngle + rps * 360f * dt, 360f);
            if (flywheel != null) flywheel.localRotation = flywheelRest * Quaternion.AngleAxis(flywheelAngle, flywheelAxis);

            if (pitchRoot != null)
            {
                pitchRoot.localRotation = pitchRest * Quaternion.AngleAxis(pitchAngle, Vector3.right);
                pitchRoot.localPosition = pitchRestPosition + Vector3.up * heightOffset;
            }

            if (smoke != null)
            {
                var emission = smoke.emission;
                emission.rateOverTime = running ? Mathf.Lerp(smokeIdleRate, smokeMaxRate, engine01) : 0f;
                if (running && !smoke.isPlaying) smoke.Play();
            }
        }
    }
}
