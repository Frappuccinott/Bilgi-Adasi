using System;
using BilgiAdasi.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// Kamera merkezinden ışın atar (2.2 m), bakılan IInteractable'ı vurgular ve ipucunu gösterir.
    /// İstasyona girince hareket, bakış ve ipucu kapanır; çıkınca geri açılır.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private float range = 2.2f;
        [Tooltip("Işının çarpabileceği layer'lar. Player layer'ı hariç tutulmalı (duvarlar ışını keser).")]
        [SerializeField] private LayerMask rayMask = ~0;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerLook look;
        [SerializeField] private HeadBob headBob;

        private Collider lastCollider;
        private IInteractable lastHitInteractable;
        private IInteractable current;
        private IInteractPromptView promptView;
        private bool controlEnabled = true;

        /// <summary>Oyuncu kontrolü açılıp kapanınca (istasyon girişi/çıkışı) tetiklenir.</summary>
        public event Action<bool> ControlEnabledChanged;

        public bool ControlEnabled => controlEnabled;

        private void OnEnable()
        {
            GameInput.Controls.Player.Interact.performed += OnInteractPerformed;
            GameInput.DeviceChanged += OnDeviceChanged;
            ViewLocator.Changed += OnViewsChanged;
            promptView = ViewLocator.Get<IInteractPromptView>();
        }

        private void OnDisable()
        {
            GameInput.Controls.Player.Interact.performed -= OnInteractPerformed;
            GameInput.DeviceChanged -= OnDeviceChanged;
            ViewLocator.Changed -= OnViewsChanged;
            SetCurrent(null);
        }

        private void Start()
        {
            GameInput.SetMode(InputMode.Player);
            promptView?.SetCrosshairVisible(true);
        }

        private void Update()
        {
            if (!controlEnabled || rayOrigin == null) return;

            IInteractable found = null;
            if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out var hit, range, rayMask, QueryTriggerInteraction.Ignore))
            {
                // GetComponentInParent yalnızca bakılan collider değişince çağrılır (her karede değil).
                if (hit.collider != lastCollider)
                {
                    lastCollider = hit.collider;
                    lastHitInteractable = hit.collider.GetComponentInParent<IInteractable>();
                }
                found = lastHitInteractable;
            }
            else
            {
                lastCollider = null;
                lastHitInteractable = null;
            }

            if (found != null && !found.CanInteract) found = null;
            if (!ReferenceEquals(found, current)) SetCurrent(found);
        }

        private void SetCurrent(IInteractable target)
        {
            if (target != null && !target.CanInteract) target = null;
            if (ReferenceEquals(target, current)) return;

            current?.SetHighlighted(false);
            current = target;

            if (current != null)
            {
                current.SetHighlighted(true);
                promptView?.ShowPrompt(current.PromptText, GameInput.LastDevice);
            }
            else
            {
                promptView?.HidePrompt();
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext ctx)
        {
            if (!controlEnabled || current == null || !current.CanInteract) return;
            var target = current;
            SetCurrent(null);
            target.Interact(this);
        }

        /// <summary>İstasyonlar çağırır: false → hareket, bakış, ışın ve crosshair kapanır.</summary>
        public void SetControlEnabled(bool enabled)
        {
            if (controlEnabled == enabled) return;
            controlEnabled = enabled;
            if (motor != null) motor.enabled = enabled;
            if (look != null) look.enabled = enabled;
            if (headBob != null) headBob.enabled = enabled;

            if (!enabled)
            {
                SetCurrent(null);
                lastCollider = null;
                lastHitInteractable = null;
            }
            promptView?.SetCrosshairVisible(enabled);
            ControlEnabledChanged?.Invoke(enabled);
        }

        private void OnDeviceChanged(InputDeviceKind device)
        {
            if (current != null) promptView?.ShowPrompt(current.PromptText, device);
        }

        private void OnViewsChanged(Type type)
        {
            if (type != typeof(IInteractPromptView)) return;
            promptView = ViewLocator.Get<IInteractPromptView>();
            promptView?.SetCrosshairVisible(controlEnabled);
            if (current != null) promptView?.ShowPrompt(current.PromptText, GameInput.LastDevice);
        }
    }
}
