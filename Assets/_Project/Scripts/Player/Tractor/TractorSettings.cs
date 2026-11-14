using UnityEngine;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// Simya Traktörü sürüş ayarları (16-traktor-tarla-mekanikleri §1). Play mode'da canlı ayarlanabilir.
    /// Ağır, yavaş ama tepkili: bırakınca ~1 sn'de durur, kaymaz.
    /// </summary>
    [CreateAssetMenu(menuName = "BilgiAdasi/Traktör Ayarları", fileName = "TractorSettings")]
    public sealed class TractorSettings : ScriptableObject
    {
        [Header("Hız (m/s)")]
        [SerializeField] private float cruiseSpeed = 3.0f;
        [Tooltip("Sprint (Shift / L3) ile en yüksek hız. Alet inikken kullanılmaz.")]
        [SerializeField] private float fastSpeed = 4.0f;
        [Tooltip("Alet indirilmişken hız sınırı (A2 sorusundaki 1,6 m/s).")]
        [SerializeField] private float workSpeed = 1.6f;
        [SerializeField] private float reverseSpeed = 1.5f;
        [SerializeField] private float acceleration = 1.8f;
        [SerializeField] private float brakeDeceleration = 4.0f;
        [Tooltip("Gaz bırakılınca yavaşlama.")]
        [SerializeField] private float coastDeceleration = 3.0f;
        [Tooltip("Yokuşta hızın değişimi (yerçekimi payı).")]
        [SerializeField] private float slopeAcceleration = 3.0f;

        [Header("Direksiyon")]
        [Tooltip("Ön ve arka aks arası (m). Modelden: 2.45.")]
        [SerializeField] private float wheelBase = 2.45f;
        [SerializeField] private float maxSteerAngleSlow = 32f;
        [SerializeField] private float maxSteerAngleFast = 18f;
        [Tooltip("Direksiyonun tam sapmaya gitme hızı (1/sn, 0–1 aralığında).")]
        [SerializeField] private float steerResponse = 2.2f;
        [Tooltip("Bırakınca ortaya dönüş hızı (1/sn).")]
        [SerializeField] private float steerReturn = 2.8f;
        [Tooltip("Direksiyon simidi, ön teker sapmasının kaç katı döner.")]
        [SerializeField] private float steeringWheelRatio = 4f;

        [Header("Bakış ve kamera")]
        [Tooltip("Gövdeye göre serbest bakış sınırı (°).")]
        [SerializeField] private float yawLimit = 120f;
        [SerializeField] private float recenterDelay = 1.5f;
        [SerializeField] private float recenterSpeed = 60f;
        [SerializeField] private float speedFovBoost = 2f;

        [Header("Gövde (CharacterController)")]
        [SerializeField] private float colliderRadius = 1.0f;
        [SerializeField] private float colliderHeight = 2.4f;
        [Tooltip("Kapsülün traktör yerelindeki ileri kayması (ön ve arka aksın ortası).")]
        [SerializeField] private float colliderCenterZ = 0.3f;
        [SerializeField] private float slopeLimit = 30f;
        [SerializeField] private float stepOffset = 0.3f;
        [SerializeField] private float gravity = -20f;

        [Header("Engel yoklaması")]
        [SerializeField] private float probeRadius = 0.5f;
        [SerializeField] private float probeHeight = 0.8f;
        [Tooltip("Kökten ön tamponun ucuna (m).")]
        [SerializeField] private float frontReach = 2.3f;
        [Tooltip("Kökten arka ucun sonuna (m), alet takılı değilken.")]
        [SerializeField] private float rearReach = 1.6f;

        [Header("Model zemine oturma")]
        [SerializeField] private float frontContactZ = 1.55f;
        [SerializeField] private float rearContactZ = -0.9f;
        [SerializeField] private float maxGroundProbe = 0.4f;
        [SerializeField] private float wheelGroundSharpness = 8f;

        [Header("Binme / inme")]
        [SerializeField] private float mountDuration = 0.6f;
        [Tooltip("İnerken oyuncunun traktörün yanına konacağı uzaklık (m).")]
        [SerializeField] private float dismountSideOffset = 1.9f;
        [SerializeField] private float dismountMaxSpeed = 0.3f;
        [SerializeField] private float promptMaxSpeed = 0.5f;

        [Header("Motor (görünüm)")]
        [Tooltip("Volanın rölantide saniyedeki turu.")]
        [SerializeField] private float idleFlywheelRps = 1.2f;
        [SerializeField] private float maxFlywheelRps = 4f;

        [Header("Metinler")]
        [SerializeField] private string mountPrompt = "Traktöre bin";
        [SerializeField] private string dismountPrompt = "Traktörden in";

        public float CruiseSpeed => cruiseSpeed;
        public float FastSpeed => fastSpeed;
        public float WorkSpeed => workSpeed;
        public float ReverseSpeed => reverseSpeed;
        public float Acceleration => acceleration;
        public float BrakeDeceleration => brakeDeceleration;
        public float CoastDeceleration => coastDeceleration;
        public float SlopeAcceleration => slopeAcceleration;
        public float WheelBase => wheelBase;
        public float MaxSteerAngleSlow => maxSteerAngleSlow;
        public float MaxSteerAngleFast => maxSteerAngleFast;
        public float SteerResponse => steerResponse;
        public float SteerReturn => steerReturn;
        public float SteeringWheelRatio => steeringWheelRatio;
        public float YawLimit => yawLimit;
        public float RecenterDelay => recenterDelay;
        public float RecenterSpeed => recenterSpeed;
        public float SpeedFovBoost => speedFovBoost;
        public float ColliderRadius => colliderRadius;
        public float ColliderHeight => colliderHeight;
        public float ColliderCenterZ => colliderCenterZ;
        public float SlopeLimit => slopeLimit;
        public float StepOffset => stepOffset;
        public float Gravity => gravity;
        public float ProbeRadius => probeRadius;
        public float ProbeHeight => probeHeight;
        public float FrontReach => frontReach;
        public float RearReach => rearReach;
        public float FrontContactZ => frontContactZ;
        public float RearContactZ => rearContactZ;
        public float MaxGroundProbe => maxGroundProbe;
        public float WheelGroundSharpness => wheelGroundSharpness;
        public float MountDuration => mountDuration;
        public float DismountSideOffset => dismountSideOffset;
        public float DismountMaxSpeed => dismountMaxSpeed;
        public float PromptMaxSpeed => promptMaxSpeed;
        public float IdleFlywheelRps => idleFlywheelRps;
        public float MaxFlywheelRps => maxFlywheelRps;
        public string MountPrompt => mountPrompt;
        public string DismountPrompt => dismountPrompt;
    }
}
