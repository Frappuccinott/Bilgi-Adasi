using BilgiAdasi.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// Birinci şahıs bakış (CLAUDE.md §4.3). Yatay dönüş gövdeyi, dikey dönüş kamera pivotunu döndürür.
    /// Fare: ham delta, yumuşatma yok. Gamepad: eğri + hafif ivmelenme. Roll yok.
    /// </summary>
    public sealed class PlayerLook : MonoBehaviour
    {
        [SerializeField] private PlayerMovementSettings settings;
        [Tooltip("Dikey dönen kamera pivotu (CinemachineCamera bunun üzerinde).")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private CinemachineCamera playerCamera;

        private float pitch;
        private float gamepadRamp;

        public float Pitch => pitch;

        private void Start()
        {
            pitch = NormalizeAngle(cameraPivot.localEulerAngles.x);
            ApplyFieldOfView();
        }

        private void Update()
        {
            if (settings == null || cameraPivot == null) return;

            var look = GameInput.Controls.Player.Look;
            Vector2 value = look.ReadValue<Vector2>();
            bool fromGamepad = look.activeControl?.device is Gamepad;

            float yawDelta, pitchDelta;
            if (fromGamepad)
            {
                float magnitude = Mathf.Clamp01(value.magnitude);
                if (magnitude > 0f)
                {
                    gamepadRamp = settings.GamepadRampTime > 0f
                        ? Mathf.MoveTowards(gamepadRamp, 1f, Time.deltaTime / settings.GamepadRampTime)
                        : 1f;
                    float scale = settings.GamepadResponse.Evaluate(magnitude) / magnitude * gamepadRamp;
                    yawDelta = value.x * scale * settings.GamepadYawSpeed * Time.deltaTime;
                    pitchDelta = value.y * scale * settings.GamepadPitchSpeed * Time.deltaTime;
                }
                else
                {
                    gamepadRamp = 0f;
                    yawDelta = pitchDelta = 0f;
                }
            }
            else
            {
                gamepadRamp = 0f;
                // Fare deltası zaten kare başına; deltaTime ile çarpılmaz.
                yawDelta = value.x * settings.MouseSensitivity;
                pitchDelta = value.y * settings.MouseSensitivity;
            }

            transform.Rotate(0f, yawDelta, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch - pitchDelta, -settings.PitchLimit, settings.PitchLimit);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        /// <summary>Ayarlar ekranından FOV değişince çağrılır.</summary>
        public void ApplyFieldOfView()
        {
            if (playerCamera != null && settings != null) playerCamera.Lens.FieldOfView = settings.FieldOfView;
        }

        private static float NormalizeAngle(float angle) => angle > 180f ? angle - 360f : angle;
    }
}
