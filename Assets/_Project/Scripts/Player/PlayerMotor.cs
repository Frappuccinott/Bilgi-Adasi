using System;
using BilgiAdasi.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// CharacterController tabanlı hareket (CLAUDE.md §4.2).
    /// - Yürüme: yüksek hızlanma/yavaşlama → kaymaz, oturaklı. Havada %30 kontrol.
    /// - Sprint: klavyede Shift basılı, gamepad'de L3 aç/kapa (durunca kapanır).
    /// - Zıplama: Space / A. Kenar toleransı (coyote) + erken basma tamponu.
    /// - Eğilme: Ctrl basılı ya da C / B aç/kapa. Tavan varsa kalkmaz.
    /// Boy ve göz yüksekliği bu bileşende; PlayerGrowth SetStandingHeight ile değiştirir.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        private const float GroundStickVelocity = -2f;
        private const float MoveInputDeadzone = 0.01f;
        private const float CrouchedEpsilon = 0.05f;

        [SerializeField] private PlayerMovementSettings settings;
        [Tooltip("Göz yüksekliğinde duran kamera kökü. Boşsa 'CameraRoot' adlı çocuk bulunur.")]
        [SerializeField] private Transform cameraRoot;

        private CharacterController controller;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;

        private bool gamepadSprintToggled;
        private bool crouchToggled;
        private float standingHeight;
        private float currentHeight;

        private float lastGroundedTime = float.NegativeInfinity;
        private float jumpPressedTime = float.NegativeInfinity;
        private bool wasGrounded = true;

        /// <summary>Yere iniş; parametre çarpma hızı (m/s, pozitif). Kamera çökmesi için.</summary>
        public event Action<float> Landed;
        public event Action Jumped;

        public PlayerMovementSettings Settings => settings;
        public CharacterController Controller => controller;
        public float HorizontalSpeed => horizontalVelocity.magnitude;
        public bool IsGrounded => controller.isGrounded;
        public bool IsCrouching => currentHeight < standingHeight - CrouchedEpsilon;
        public float StandingHeight => standingHeight;
        public float CurrentHeight => currentHeight;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (cameraRoot == null) cameraRoot = transform.Find("CameraRoot");

            standingHeight = settings != null ? settings.StandingHeight : controller.height;
            currentHeight = standingHeight;
            if (settings != null)
            {
                controller.slopeLimit = settings.SlopeLimit;
                controller.stepOffset = settings.StepOffset;
            }
            ApplyHeight(currentHeight);
        }

        private void OnEnable()
        {
            var a = GameInput.Controls.Player;
            a.Sprint.performed += OnSprintPerformed;
            a.Jump.performed += OnJumpPerformed;
            a.CrouchToggle.performed += OnCrouchTogglePerformed;
        }

        private void OnDisable()
        {
            var a = GameInput.Controls.Player;
            a.Sprint.performed -= OnSprintPerformed;
            a.Jump.performed -= OnJumpPerformed;
            a.CrouchToggle.performed -= OnCrouchTogglePerformed;
            // İstasyona geçerken kaymasın.
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            jumpPressedTime = float.NegativeInfinity;
        }

        private void OnSprintPerformed(InputAction.CallbackContext ctx)
        {
            if (ctx.control.device is Gamepad) gamepadSprintToggled = !gamepadSprintToggled;
        }

        private void OnJumpPerformed(InputAction.CallbackContext ctx) => jumpPressedTime = Time.time;

        private void OnCrouchTogglePerformed(InputAction.CallbackContext ctx) => crouchToggled = !crouchToggled;

        private void Update()
        {
            if (settings == null) return;
            float dt = Time.deltaTime;
            float now = Time.time;
            var a = GameInput.Controls.Player;

            // --- Yer teması ve iniş ---
            bool grounded = controller.isGrounded;
            if (grounded)
            {
                if (!wasGrounded && verticalVelocity < 0f) Landed?.Invoke(-verticalVelocity);
                lastGroundedTime = now;
            }
            wasGrounded = grounded;

            // --- Girdi ---
            Vector2 input = Vector2.ClampMagnitude(a.Move.ReadValue<Vector2>(), 1f);
            bool hasInput = input.sqrMagnitude > MoveInputDeadzone;
            if (!hasInput) gamepadSprintToggled = false;

            bool keyboardSprint = a.Sprint.IsPressed() && !(a.Sprint.activeControl?.device is Gamepad);
            bool sprinting = (keyboardSprint || gamepadSprintToggled) && hasInput;
            if (sprinting) crouchToggled = false; // koşmaya başlayınca ayağa kalkar

            // --- Eğilme ---
            bool wantCrouch = a.Crouch.IsPressed() || crouchToggled;
            UpdateHeight(wantCrouch ? settings.CrouchHeight : standingHeight, dt);
            bool crouching = IsCrouching;

            // --- Yatay hız ---
            float speed = crouching ? settings.CrouchSpeed : sprinting ? settings.SprintSpeed : settings.WalkSpeed;
            Vector3 desired = (transform.forward * input.y + transform.right * input.x) * speed;

            float rate = hasInput ? settings.GroundAcceleration : settings.GroundDeceleration;
            if (!grounded) rate *= settings.AirControl;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desired, rate * dt);

            // --- Dikey hız ---
            bool jumpBuffered = now - jumpPressedTime <= settings.JumpBufferTime;
            bool canCoyote = now - lastGroundedTime <= settings.CoyoteTime;
            if (settings.AllowJump && jumpBuffered && canCoyote && verticalVelocity <= 0.01f)
            {
                if (crouching)
                {
                    // Eğilmişken zıplama önce ayağa kaldırır (tavan izin veriyorsa).
                    crouchToggled = false;
                    jumpPressedTime = float.NegativeInfinity;
                }
                else
                {
                    verticalVelocity = Mathf.Sqrt(2f * -settings.Gravity * settings.JumpHeight);
                    jumpPressedTime = float.NegativeInfinity;
                    lastGroundedTime = float.NegativeInfinity;
                    Jumped?.Invoke();
                }
            }
            else if (grounded && verticalVelocity < 0f)
            {
                verticalVelocity = GroundStickVelocity;
            }
            verticalVelocity += settings.Gravity * dt;

            // --- Hareket ---
            var flags = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);

            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
            if ((flags & CollisionFlags.Sides) != 0)
            {
                // Duvara sürtünürken hız birikmesin; duvar boyunca kayma korunur.
                Vector3 actual = controller.velocity;
                actual.y = 0f;
                if (actual.sqrMagnitude < horizontalVelocity.sqrMagnitude) horizontalVelocity = actual;
            }
        }

        private void UpdateHeight(float targetHeight, float dt)
        {
            if (targetHeight > currentHeight && !HasHeadroom(targetHeight)) targetHeight = currentHeight;
            if (Mathf.Approximately(targetHeight, currentHeight)) return;

            currentHeight = Mathf.MoveTowards(currentHeight, targetHeight, settings.CrouchTransitionSpeed * dt);
            ApplyHeight(currentHeight);
        }

        /// <summary>Başın üstünde targetHeight'a kadar boşluk var mı?</summary>
        private bool HasHeadroom(float targetHeight)
        {
            float radius = controller.radius * 0.95f;
            Vector3 bottom = transform.position + Vector3.up * (currentHeight - radius);
            Vector3 top = transform.position + Vector3.up * (targetHeight - radius);
            int mask = ~(1 << gameObject.layer);
            return !Physics.CheckCapsule(bottom, top, radius, mask, QueryTriggerInteraction.Ignore);
        }

        private void ApplyHeight(float height)
        {
            controller.height = height;
            controller.center = new Vector3(0f, height * 0.5f, 0f);
            if (cameraRoot != null)
            {
                float eyeOffset = settings != null ? settings.EyeOffsetFromTop : 0.1f;
                var p = cameraRoot.localPosition;
                cameraRoot.localPosition = new Vector3(p.x, height - eyeOffset, p.z);
            }
        }

        /// <summary>Ayakta boyu değiştirir (PlayerGrowth). Eğilmişse kalkınca uygulanır.</summary>
        public void SetStandingHeight(float height)
        {
            bool wasStanding = !IsCrouching;
            standingHeight = height;
            if (wasStanding && HasHeadroom(height))
            {
                currentHeight = height;
                ApplyHeight(height);
            }
        }

        /// <summary>Oyuncuyu bir noktaya ışınlar (doğma, geliştirici kısayolu).</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, rotation.eulerAngles.y, 0f));
            controller.enabled = true;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }
    }
}
