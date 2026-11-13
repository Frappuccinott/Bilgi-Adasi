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
        [Tooltip("Yüksek = tam hıza hemen çıkar (oturaklı). 45 → ~0.08 sn.")]
        [SerializeField] private float groundAcceleration = 45f;
        [Tooltip("Yüksek = bırakınca hemen durur, kaymaz. 55 → ~0.07 sn.")]
        [SerializeField] private float groundDeceleration = 55f;
        [Range(0f, 1f)] [SerializeField] private float airControl = 0.3f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float slopeLimit = 45f;
        [SerializeField] private float stepOffset = 0.3f;

        [Header("Zıplama")]
        [SerializeField] private bool allowJump = true;
        [SerializeField] private float jumpHeight = 1.0f;
        [Tooltip("Kenardan düştükten sonra hâlâ zıplanabilen süre (sn).")]
        [SerializeField] private float coyoteTime = 0.1f;
        [Tooltip("Yere değmeden hemen önce basılan zıplama hatırlanır (sn).")]
        [SerializeField] private float jumpBufferTime = 0.12f;

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

        [Header("Rahatlık")]
        [SerializeField] private bool headBobEnabled = true;
        [Tooltip("Dikey sallanma, en fazla 0.025 m.")]
        [Range(0f, 0.025f)] [SerializeField] private float headBobAmplitude = 0.02f;
        [Tooltip("Yürüme hızında saniyedeki adım sayısı.")]
        [SerializeField] private float headBobFrequency = 1.9f;
        [Tooltip("İnişte kamera çökmesi üst sınırı (çok küçük tutulur).")]
        [Range(0f, 0.05f)] [SerializeField] private float landingDipMax = 0.035f;
        [Tooltip("Düşüş hızı (m/s) başına çökme (m).")]
        [SerializeField] private float landingDipPerSpeed = 0.006f;

        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        public float CrouchSpeed => crouchSpeed;
        public float GroundAcceleration => groundAcceleration;
        public float GroundDeceleration => groundDeceleration;
        public float AirControl => airControl;
        public float Gravity => gravity;
        public float SlopeLimit => slopeLimit;
        public float StepOffset => stepOffset;

        public bool AllowJump => allowJump;
        public float JumpHeight => jumpHeight;
        public float CoyoteTime => coyoteTime;
        public float JumpBufferTime => jumpBufferTime;

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

        public bool HeadBobEnabled { get => headBobEnabled; set => headBobEnabled = value; }
        public float HeadBobAmplitude => headBobAmplitude;
        public float HeadBobFrequency => headBobFrequency;
        public float LandingDipMax => landingDipMax;
        public float LandingDipPerSpeed => landingDipPerSpeed;
    }
}
