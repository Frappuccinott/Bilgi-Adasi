using BilgiAdasi.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// CharacterController tabanlı yürüme (CLAUDE.md §4.2): vektörel hızlanma/yavaşlama,
    /// %30 havada kontrol, sabit yerçekimi. Sprint: klavyede basılı tut, gamepad'de L3 aç/kapa.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        private const float GroundStickVelocity = -2f;
        private const float MoveInputDeadzone = 0.01f;

        [SerializeField] private PlayerMovementSettings settings;

        private CharacterController controller;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private bool gamepadSprintToggled;

        public PlayerMovementSettings Settings => settings;
        public CharacterController Controller => controller;
        /// <summary>Yatay hız büyüklüğü (m/s). Kafa sallanması bunu kullanır.</summary>
        public float HorizontalSpeed => horizontalVelocity.magnitude;
        public bool IsGrounded => controller.isGrounded;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (settings != null)
            {
                controller.slopeLimit = settings.SlopeLimit;
                controller.stepOffset = settings.StepOffset;
            }
        }

        private void OnEnable()
        {
            GameInput.Controls.Player.Sprint.performed += OnSprintPerformed;
        }

        private void OnDisable()
        {
            GameInput.Controls.Player.Sprint.performed -= OnSprintPerformed;
            // İstasyona geçerken kaymasın: hız sıfırlanır.
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        private void OnSprintPerformed(InputAction.CallbackContext ctx)
        {
            if (ctx.control.device is Gamepad) gamepadSprintToggled = !gamepadSprintToggled;
        }

        private void Update()
        {
            if (settings == null) return;
            float dt = Time.deltaTime;
            var actions = GameInput.Controls.Player;

            Vector2 input = Vector2.ClampMagnitude(actions.Move.ReadValue<Vector2>(), 1f);
            bool hasInput = input.sqrMagnitude > MoveInputDeadzone;
            if (!hasInput) gamepadSprintToggled = false; // durunca gamepad sprint'i kapanır

            bool keyboardSprint = actions.Sprint.IsPressed() && !(actions.Sprint.activeControl?.device is Gamepad);
            float speed = keyboardSprint || gamepadSprintToggled ? settings.SprintSpeed : settings.WalkSpeed;

            Vector3 desired = (transform.forward * input.y + transform.right * input.x) * speed;

            bool grounded = controller.isGrounded;
            float rate = hasInput ? settings.GroundAcceleration : settings.GroundDeceleration;
            if (!grounded) rate *= settings.AirControl;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desired, rate * dt);

            if (grounded && verticalVelocity < 0f) verticalVelocity = GroundStickVelocity;
            verticalVelocity += settings.Gravity * dt;

            Vector3 motion = horizontalVelocity + Vector3.up * verticalVelocity;
            var flags = controller.Move(motion * dt);

            // Duvara çarpınca yatay hız birikmesin.
            if ((flags & CollisionFlags.Sides) != 0)
                horizontalVelocity = Vector3.ProjectOnPlane(controller.velocity, Vector3.up);
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
