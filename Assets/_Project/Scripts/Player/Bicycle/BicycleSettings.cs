using UnityEngine;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// Bisiklet sürüş ayarları. Play mode'da canlı ayarlanabilir.
    /// Sürüş kinematiktir: devrilme yok, yatış yalnızca modelde (kamera roll yok, CLAUDE.md §4.3).
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Oyuncu/Bisiklet Ayarları", fileName = "BicycleSettings")]
    public sealed class BicycleSettings : ScriptableObject
    {
        [Header("Hız (m/s, m/s²)")]
        [Tooltip("Pedala yeni basınca ulaşılan seyir hızı.")]
        [SerializeField] private float cruiseSpeed = 5f;
        [Tooltip("Durmadan pedal çevirdikçe seyir hızı buna kadar çıkar (gittikçe hızlanma).")]
        [SerializeField] private float momentumSpeed = 7.5f;
        [Tooltip("Seyir hızından momentum hızına kesintisiz pedalla kaç saniyede çıkılır.")]
        [SerializeField] private float momentumBuildTime = 8f;
        [Tooltip("Pedal bırakılınca momentumun sönme hızı (birim/sn, 1 = tamamı).")]
        [SerializeField] private float momentumDecay = 0.2f;
        [Tooltip("Frende momentumun sönme hızı (birim/sn).")]
        [SerializeField] private float momentumBrakeDecay = 1f;
        [Tooltip("Hızlı pedal (Shift / L3): hıza eklenen.")]
        [SerializeField] private float fastSpeedBonus = 2f;
        [Tooltip("Hızlı pedalda ivme ve momentum birikimi çarpanı.")]
        [SerializeField] private float fastAccelerationMultiplier = 1.3f;
        [Tooltip("Kalkıştaki ivme (m/s²). Asıl ivme = bu × eğri(hız / o anki hedef hız).")]
        [SerializeField] private float pedalAcceleration = 3.2f;
        [Tooltip("x: hız / hedef hız (0–1), y: ivme çarpanı. Kalkışta güçlü, hedefe yaklaştıkça yumuşar.")]
        [SerializeField] private AnimationCurve accelerationBySpeed = new AnimationCurve(
            new Keyframe(0f, 0.55f, 0f, 1.5f),   // ilk tur pedal ağır: yavaş kalkış
            new Keyframe(0.25f, 1f, 0f, 0f),
            new Keyframe(1f, 0.25f, -1f, 0f));
        [SerializeField] private float brakeDeceleration = 7f;
        [Tooltip("Pedal bırakılınca yuvarlanma direnci.")]
        [SerializeField] private float coastDeceleration = 0.6f;
        [Tooltip("Dururken S / çubuk geri: bisikleti geri itme hızı.")]
        [SerializeField] private float pushBackSpeed = 1f;
        [SerializeField] private float pushAcceleration = 3f;
        [Tooltip("Eğimde hız değişimi (sin(eğim) başına m/s²). Yokuş yavaşlatır, iniş hızlandırır.")]
        [SerializeField] private float slopeAcceleration = 4f;
        [SerializeField] private float maxDownhillSpeed = 10f;

        [Header("Gidon")]
        [Tooltip("Tekerlek göbekleri arası (m). Dönüş yarıçapını belirler.")]
        [SerializeField] private float wheelBase = 1.05f;
        [Tooltip("Yavaşken en büyük gidon açısı (°).")]
        [SerializeField] private float maxSteerAngleSlow = 35f;
        [Tooltip("Seyir hızında en büyük gidon açısı (°).")]
        [SerializeField] private float maxSteerAngleFast = 10f;
        [Tooltip("Gidonun tam kırılma hızı (girdi birimi/sn).")]
        [SerializeField] private float steerResponse = 4f;
        [Tooltip("Bırakınca ortaya dönüş hızı (girdi birimi/sn).")]
        [SerializeField] private float steerReturn = 6f;

        [Header("Yatış (yalnızca model)")]
        [SerializeField] private float maxLean = 8f;
        [SerializeField] private float leanSharpness = 6f;

        [Header("Fizik")]
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float slopeLimit = 40f;
        [Header("Kaldırım")]
        [Tooltip("Bisikletin çıkabildiği en yüksek basamak/kaldırım (m). Engel yoklaması bunun üstünden geçer.")]
        [SerializeField] private float maxCurbHeight = 0.3f;
        [Tooltip("Kaldırıma çarpınca kaybedilen hız oranı (0–1).")]
        [SerializeField] private float curbSpeedLoss = 0.15f;
        [Tooltip("Model tekerlerin yere oturması (öne/arkaya eğilme) için yumuşatma.")]
        [SerializeField] private float wheelGroundSharpness = 18f;
        [Tooltip("Park hâlinde çarpışma kapsülü boyu (sürücüsüz).")]
        [SerializeField] private float parkedHeight = 1.1f;
        [Tooltip("Sürerken çarpışma kapsülü boyu (sürücüyle).")]
        [SerializeField] private float ridingHeight = 1.75f;
        [Tooltip("Tekerleklerin duvara girmemesi için ileri/geri yoklama küresi.")]
        [SerializeField] private float probeRadius = 0.25f;
        [SerializeField] private float probeHeight = 0.55f;
        [Tooltip("Merkezden ön tekerin ucuna (m).")]
        [SerializeField] private float frontReach = 0.95f;
        [Tooltip("Merkezden arka tekerin ucuna (m).")]
        [SerializeField] private float rearReach = 0.85f;

        [Header("Havada (rampa, kenar)")]
        [Tooltip("Rampadan çıkarken eğimin dikey hızı korunur: bu değerin (m/s) altındaki farklar yok sayılır (titreme olmasın).")]
        [SerializeField] private float minLaunchVertical = 0.5f;
        [Tooltip("Havada modelin burnu gidiş yönünü izler; en fazla eğim (°).")]
        [SerializeField] private float maxAirPitch = 35f;
        [SerializeField] private float airPitchSharpness = 5f;
        [Tooltip("İnişte kamera çökmesi: düşüş hızı (m/s) başına metre.")]
        [SerializeField] private float landingDipPerSpeed = 0.012f;
        [SerializeField] private float maxLandingDip = 0.08f;
        [Tooltip("Bu düşüş hızının (m/s) üstündeki inişte hız kaybı oranı.")]
        [SerializeField] private float hardLandingSpeed = 6f;
        [SerializeField] private float hardLandingSpeedLoss = 0.15f;

        [Header("Binme / inme")]
        [SerializeField] private float mountDuration = 0.35f;
        [Tooltip("Bu hızın üstünde inme isteği önce frenler, durunca indirir.")]
        [SerializeField] private float dismountMaxSpeed = 1.2f;
        [Tooltip("İnilecek yer: bisikletin yanına uzaklık (m).")]
        [SerializeField] private float dismountSideOffset = 0.8f;
        [Tooltip("Bu hızın altında ekranda 'in' ipucu görünür.")]
        [SerializeField] private float promptMaxSpeed = 0.5f;

        [Header("Bakış (sürerken)")]
        [Tooltip("Gidona göre en fazla sağa/sola bakış (°).")]
        [SerializeField] private float yawLimit = 110f;
        [Tooltip("Bakış girdisi bu kadar süre yoksa (ve gidiyorsa) bakış öne döner (sn).")]
        [SerializeField] private float recenterDelay = 1.2f;
        [SerializeField] private float recenterSpeed = 90f;
        [Tooltip("Hızlı pedalda FOV artışı (°). Hızla orantılı.")]
        [SerializeField] private float speedFovBoost = 5f;

        [Header("Ayakta pedal (hızlı pedal, GTA tarzı sallanma)")]
        [Tooltip("Hızlı pedalda sürücü ayağa kalkma geçiş hızı (1/sn).")]
        [SerializeField] private float standSharpness = 5f;
        [Tooltip("Ayaktayken göz kaç m yükselir.")]
        [SerializeField] private float standRise = 0.07f;
        [Tooltip("Ayaktayken göz kaç m öne gelir (gidona eğilme).")]
        [SerializeField] private float standForward = 0.08f;
        [Tooltip("Ayakta pedalda bisikletin pedal ritmiyle sağa-sola sallanması (°, yalnızca model).")]
        [SerializeField] private float rockAngle = 6f;
        [Tooltip("Sallanmanın en yüksek ritmi (tam salınım/sn). Pedal daha hızlı dönse de bundan hızlı sallanmaz.")]
        [SerializeField] private float maxRockFrequency = 1.3f;
        [Tooltip("Sallanmanın başladığı en düşük hız (m/s).")]
        [SerializeField] private float rockMinSpeed = 1.5f;

        [Header("Kamera (roll yok, yalnızca konum)")]
        [Tooltip("Virajda baş, yatış yönüne kayar: yatış açısının sinüsü × bu (m). 0 = kapalı.")]
        [SerializeField] private float cameraLeanShift = 0.45f;
        [Tooltip("Ayakta sallanırken baş bunun kadar yana gider: sallanma açısının sinüsü × bu (m).")]
        [SerializeField] private float cameraRockShift = 0.25f;

        [Header("Metin")]
        [SerializeField] private string mountPrompt = "Bisiklete bin";
        [SerializeField] private string dismountPrompt = "Bisikletten in";

        public float CruiseSpeed => cruiseSpeed;
        public float MomentumSpeed => momentumSpeed;
        public float MomentumBuildTime => momentumBuildTime;
        public float MomentumDecay => momentumDecay;
        public float MomentumBrakeDecay => momentumBrakeDecay;
        public float FastSpeedBonus => fastSpeedBonus;
        public float FastAccelerationMultiplier => fastAccelerationMultiplier;
        /// <summary>Düz yolda ulaşılabilen en yüksek hız (FOV ve ses buna oranlanır).</summary>
        public float FastSpeed => momentumSpeed + fastSpeedBonus;
        public float PedalAcceleration => pedalAcceleration;
        public float AccelerationFactor(float speed01) => accelerationBySpeed != null && accelerationBySpeed.length > 0
            ? Mathf.Max(0.05f, accelerationBySpeed.Evaluate(speed01))
            : 1f;
        public float BrakeDeceleration => brakeDeceleration;
        public float CoastDeceleration => coastDeceleration;
        public float PushBackSpeed => pushBackSpeed;
        public float PushAcceleration => pushAcceleration;
        public float SlopeAcceleration => slopeAcceleration;
        public float MaxDownhillSpeed => maxDownhillSpeed;
        public float WheelBase => wheelBase;
        public float MaxSteerAngleSlow => maxSteerAngleSlow;
        public float MaxSteerAngleFast => maxSteerAngleFast;
        public float SteerResponse => steerResponse;
        public float SteerReturn => steerReturn;
        public float MaxLean => maxLean;
        public float LeanSharpness => leanSharpness;
        public float Gravity => gravity;
        public float SlopeLimit => slopeLimit;
        /// <summary>
        /// CharacterController'ın kendi basamak çıkışı: zemin ekleri ve alçak eşikler (sıçrama Bicycle'da yumuşatılır).
        /// Bundan yüksek kaldırımları Bicycle.TryClimbCurb çıkar.
        /// </summary>
        public float StepOffset => Mathf.Min(0.12f, maxCurbHeight);
        public float MaxCurbHeight => maxCurbHeight;
        public float CurbSpeedLoss => curbSpeedLoss;
        public float WheelGroundSharpness => wheelGroundSharpness;
        public float ParkedHeight => parkedHeight;
        public float RidingHeight => ridingHeight;
        public float ProbeRadius => probeRadius;
        public float ProbeHeight => probeHeight;
        public float FrontReach => frontReach;
        public float RearReach => rearReach;
        public float MinLaunchVertical => minLaunchVertical;
        public float MaxAirPitch => maxAirPitch;
        public float AirPitchSharpness => airPitchSharpness;
        public float LandingDipPerSpeed => landingDipPerSpeed;
        public float MaxLandingDip => maxLandingDip;
        public float HardLandingSpeed => hardLandingSpeed;
        public float HardLandingSpeedLoss => hardLandingSpeedLoss;
        public float MountDuration => mountDuration;
        public float DismountMaxSpeed => dismountMaxSpeed;
        public float DismountSideOffset => dismountSideOffset;
        public float PromptMaxSpeed => promptMaxSpeed;
        public float YawLimit => yawLimit;
        public float RecenterDelay => recenterDelay;
        public float RecenterSpeed => recenterSpeed;
        public float SpeedFovBoost => speedFovBoost;
        public float StandSharpness => standSharpness;
        public float StandRise => standRise;
        public float StandForward => standForward;
        public float RockAngle => rockAngle;
        public float MaxRockFrequency => maxRockFrequency;
        public float RockMinSpeed => rockMinSpeed;
        public float CameraLeanShift => cameraLeanShift;
        public float CameraRockShift => cameraRockShift;
        public string MountPrompt => mountPrompt;
        public string DismountPrompt => dismountPrompt;
    }
}
