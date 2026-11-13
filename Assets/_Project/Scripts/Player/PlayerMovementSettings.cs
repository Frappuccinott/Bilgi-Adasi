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
        [SerializeField] private float groundAcceleration = 14f;
        [SerializeField] private float groundDeceleration = 18f;
        [Range(0f, 1f)] [SerializeField] private float airControl = 0.3f;
        [SerializeField] private float gravity = -20f;
        [Tooltip("Varsayılan kapalı. Açılırsa input asset'e Jump aksiyonu (klavye + gamepad) eklenmeli.")]
        [SerializeField] private bool allowJump = false;
        [SerializeField] private float jumpHeight = 0.9f;
        [SerializeField] private float slopeLimit = 45f;
        [SerializeField] private float stepOffset = 0.3f;

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
        [Tooltip("En fazla 0.025 m.")]
        [Range(0f, 0.025f)] [SerializeField] private float headBobAmplitude = 0.02f;
        [Tooltip("Yürüme hızında saniyedeki adım salınımı.")]
        [SerializeField] private float headBobFrequency = 1.8f;

        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        public float GroundAcceleration => groundAcceleration;
        public float GroundDeceleration => groundDeceleration;
        public float AirControl => airControl;
        public float Gravity => gravity;
        public bool AllowJump => allowJump;
        public float JumpHeight => jumpHeight;
        public float SlopeLimit => slopeLimit;
        public float StepOffset => stepOffset;

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
    }
}
