using UnityEngine;

namespace BilgiAdasi.Player
{
    /// <summary>Hareket, bakış ve rahatlık ayarları (CLAUDE.md §4). Play mode'da canlı değiştirilebilir.</summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Oyuncu/Hareket Ayarları", fileName = "PlayerMovementSettings")]
    public sealed class PlayerMovementSettings : ScriptableObject
    {
        [Header("Hareket (m/s, m/s²)")]
        [SerializeField] private float walkSpeed = 3.8f;
        [SerializeField] private float sprintSpeed = 5.6f;
        [SerializeField] private float crouchSpeed = 2.0f;
        [Tooltip("Geri giderken hız çarpanı.")]
        [Range(0.3f, 1f)] [SerializeField] private float backwardMultiplier = 0.7f;
        [Tooltip("Yana giderken hız çarpanı.")]
        [Range(0.3f, 1f)] [SerializeField] private float strafeMultiplier = 0.85f;
        [Tooltip("Yüksek = tam hıza hemen çıkar (oturaklı). 45 → ~0.08 sn.")]
        [SerializeField] private float groundAcceleration = 45f;
        [Tooltip("Yüksek = bırakınca hemen durur, kaymaz. 55 → ~0.07 sn.")]
        [SerializeField] private float groundDeceleration = 55f;
        [Range(0f, 1f)] [SerializeField] private float airControl = 0.3f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float slopeLimit = 45f;
        [SerializeField] private float stepOffset = 0.3f;

        [Header("Eğim")]
        [Tooltip("En dik yokuş yukarı (slopeLimit) çıkarken hız çarpanı.")]
        [Range(0.3f, 1f)] [SerializeField] private float uphillMultiplier = 0.65f;
        [Tooltip("En dik yokuş aşağı inerken hız çarpanı.")]
        [Range(1f, 1.5f)] [SerializeField] private float downhillMultiplier = 1.1f;

        [Header("Zıplama ve iniş")]
        [SerializeField] private bool allowJump = true;
        [SerializeField] private float jumpHeight = 1.0f;
        [Tooltip("Kenardan düştükten sonra hâlâ zıplanabilen süre (sn).")]
        [SerializeField] private float coyoteTime = 0.1f;
        [Tooltip("Yere değmeden hemen önce basılan zıplama hatırlanır (sn).")]
        [SerializeField] private float jumpBufferTime = 0.12f;
        [Tooltip("Bu düşüş hızının (m/s) üstündeki inişler 'sert' sayılır. Normal zıplama inişi ~6.3.")]
        [SerializeField] private float hardLandingSpeed = 7.5f;
        [Tooltip("Sert inişten sonra kısa süre hız çarpanı.")]
        [Range(0.2f, 1f)] [SerializeField] private float hardLandingSpeedMultiplier = 0.6f;
        [SerializeField] private float hardLandingDuration = 0.2f;

        [Header("Boy ve eğilme (m)")]
        [Tooltip("Ayakta boy. 10. sınıf: 1.66 (CLAUDE.md §7). PlayerGrowth değiştirir.")]
        [SerializeField] private float standingHeight = 1.66f;
        [Tooltip("Göz, başın tepesinden bu kadar aşağıda (1.66 boy → 1.56 göz).")]
        [SerializeField] private float eyeOffsetFromTop = 0.10f;
        [SerializeField] private float crouchHeight = 1.10f;
        [Tooltip("Eğilip kalkma hızı (m/s).")]
        [SerializeField] private float crouchTransitionSpeed = 4f;

        [Header("Bakış")]
        [Tooltip("Fare: piksel başına derece. Ham girdi, yumuşatma yok.")]
        [SerializeField] private float mouseSensitivity = 0.08f;
        [SerializeField] private float gamepadYawSpeed = 180f;
        [SerializeField] private float gamepadPitchSpeed = 120f;
        [Tooltip("Çubuk eğimi (0–1) → hız oranı (0–1).")]
        [SerializeField] private AnimationCurve gamepadResponse = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 2f, 2f));
        [Tooltip("Gamepad'de tam hıza çıkma süresi (sn). Hafif ivmelenme.")]
        [SerializeField] private float gamepadRampTime = 0.15f;
        [SerializeField] private float pitchLimit = 80f;
        [Range(60f, 90f)] [SerializeField] private float fieldOfView = 70f;
        [Tooltip("Koşarken FOV artışı (derece). Küçük tutulur.")]
        [Range(0f, 8f)] [SerializeField] private float sprintFovBoost = 4f;
        [Tooltip("FOV geçiş keskinliği. Yüksek = hızlı.")]
        [SerializeField] private float fovSharpness = 6f;

        [Header("Adım ve kafa sallanması")]
        [Tooltip("Bir adımda alınan yol (m). 3.8 m/s yürüyüşte ~2 adım/sn.")]
        [SerializeField] private float strideLength = 1.9f;
        [SerializeField] private bool headBobEnabled = true;
        [Tooltip("Adım başına dikey iniş, en fazla 0.025 m (CLAUDE.md §4.4).")]
        [Range(0f, 0.025f)] [SerializeField] private float headBobAmplitude = 0.025f;
        [Tooltip("Adım başına kamera başı eğimi (derece). Roll değil, yalnızca aşağı-yukarı.")]
        [Range(0f, 1f)] [SerializeField] private float headBobPitch = 0.3f;
        [Tooltip("İnişte kamera çökmesi üst sınırı (çok küçük tutulur).")]
        [Range(0f, 0.05f)] [SerializeField] private float landingDipMax = 0.035f;
        [Tooltip("Düşüş hızı (m/s) başına çökme (m).")]
        [SerializeField] private float landingDipPerSpeed = 0.006f;

        [Header("Nefes (dururken)")]
        [Range(0f, 0.01f)] [SerializeField] private float breathingAmplitude = 0.004f;
        [Tooltip("Saniyedeki nefes sayısı.")]
        [SerializeField] private float breathingRate = 0.25f;
        [Range(0f, 0.5f)] [SerializeField] private float breathingPitch = 0.12f;

        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        public float CrouchSpeed => crouchSpeed;
        public float BackwardMultiplier => backwardMultiplier;
        public float StrafeMultiplier => strafeMultiplier;
        public float GroundAcceleration => groundAcceleration;
        public float GroundDeceleration => groundDeceleration;
        public float AirControl => airControl;
        public float Gravity => gravity;
        public float SlopeLimit => slopeLimit;
        public float StepOffset => stepOffset;

        public float UphillMultiplier => uphillMultiplier;
        public float DownhillMultiplier => downhillMultiplier;

        public bool AllowJump => allowJump;
        public float JumpHeight => jumpHeight;
        public float CoyoteTime => coyoteTime;
        public float JumpBufferTime => jumpBufferTime;
        public float HardLandingSpeed => hardLandingSpeed;
        public float HardLandingSpeedMultiplier => hardLandingSpeedMultiplier;
        public float HardLandingDuration => hardLandingDuration;

        public float StandingHeight => standingHeight;
        public float EyeOffsetFromTop => eyeOffsetFromTop;
        public float CrouchHeight => crouchHeight;
        public float CrouchTransitionSpeed => crouchTransitionSpeed;

        public float MouseSensitivity { get => mouseSensitivity; set => mouseSensitivity = Mathf.Max(0.001f, value); }
        public float GamepadYawSpeed => gamepadYawSpeed;
        public float GamepadPitchSpeed => gamepadPitchSpeed;
        public AnimationCurve GamepadResponse => gamepadResponse;
        public float GamepadRampTime => gamepadRampTime;
        public float PitchLimit => pitchLimit;
        public float FieldOfView { get => fieldOfView; set => fieldOfView = Mathf.Clamp(value, 60f, 90f); }
        public float SprintFovBoost => sprintFovBoost;
        public float FovSharpness => fovSharpness;

        public float StrideLength => strideLength;
        public bool HeadBobEnabled { get => headBobEnabled; set => headBobEnabled = value; }
        public float HeadBobAmplitude => headBobAmplitude;
        public float HeadBobPitch => headBobPitch;
        public float LandingDipMax => landingDipMax;
        public float LandingDipPerSpeed => landingDipPerSpeed;

        public float BreathingAmplitude => breathingAmplitude;
        public float BreathingRate => breathingRate;
        public float BreathingPitch => breathingPitch;
    }
}
