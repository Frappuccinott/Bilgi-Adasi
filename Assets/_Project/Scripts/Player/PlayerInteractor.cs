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
        private bool riding;
        private string shownPrompt;

        /// <summary>Oyuncunun envanteri ve kesesi (aynı objede ya da üstünde).</summary>
        public Order.PlayerInventory Inventory { get; private set; }

        private void Awake()
        {
            Inventory = GetComponentInParent<Order.PlayerInventory>();
        }

        /// <summary>Oyuncu kontrolü açılıp kapanınca (istasyon girişi/çıkışı) tetiklenir.</summary>
        public event Action<bool> ControlEnabledChanged;

        public bool ControlEnabled => controlEnabled;
        /// <summary>Bisiklette: yürüme ve ışın kapalı, bakış ve crosshair açık. Interact'i bisiklet dinler.</summary>
        public bool IsRiding => riding;

        private void OnEnable()
        {
            GameInput.Controls.Player.Interact.performed += OnInteractPerformed;
            GameInput.SchemeChanged += OnSchemeChanged;
            ViewLocator.Changed += OnViewsChanged;
            promptView = ViewLocator.Get<IInteractPromptView>();
        }

        private void OnDisable()
        {
            if (GameInput.TryGetControls(out var c)) c.Player.Interact.performed -= OnInteractPerformed;
            GameInput.SchemeChanged -= OnSchemeChanged;
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
            if (!controlEnabled || riding || rayOrigin == null) return;

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
            else if (current != null) RefreshPrompt(false);
        }

        /// <summary>İpucu metni duruma göre değişebilir (eldeki madde); değişince yeniden çizilir.</summary>
        private void RefreshPrompt(bool force)
        {
            string text = current.GetPrompt(this);
            if (!force && ReferenceEquals(text, shownPrompt)) return;
            shownPrompt = text;
            promptView?.ShowPrompt(text, ControlGlyphs.For(PromptAction.Interact));
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
                RefreshPrompt(true);
            }
            else
            {
                shownPrompt = null;
                promptView?.HidePrompt();
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext ctx)
        {
            if (!controlEnabled || riding || current == null || !current.CanInteract) return;
            // Hedef seçili kalır: aynı kazana art arda dökülebilsin. İpucu bir sonraki karede tazelenir.
            current.Interact(this);
        }

        /// <summary>İstasyonlar çağırır: false → hareket, bakış, ışın ve crosshair kapanır.</summary>
        public void SetControlEnabled(bool enabled)
        {
            if (controlEnabled == enabled) return;
            controlEnabled = enabled;
            // Bisikletteyken yürüme ve kafa sallanması kapalı kalır (konuşma bitince geri açılmasın).
            if (motor != null) motor.enabled = enabled && !riding;
            if (look != null) look.enabled = enabled;
            if (headBob != null) headBob.enabled = enabled && !riding;

            if (!enabled)
            {
                SetCurrent(null);
                lastCollider = null;
                lastHitInteractable = null;
            }
            promptView?.SetCrosshairVisible(enabled);
            ControlEnabledChanged?.Invoke(enabled);
        }

        /// <summary>Bicycle çağırır: true → yürüme, kafa sallanması ve etkileşim ışını kapanır; bakış ve crosshair kalır.</summary>
        public void SetRiding(bool value)
        {
            if (riding == value) return;
            riding = value;
            if (motor != null) motor.enabled = controlEnabled && !riding;
            if (headBob != null) headBob.enabled = controlEnabled && !riding;
            if (riding)
            {
                SetCurrent(null);
                lastCollider = null;
                lastHitInteractable = null;
            }
        }

        private void OnSchemeChanged(ControlScheme scheme)
        {
            if (current != null) RefreshPrompt(true);
        }

        private void OnViewsChanged(Type type)
        {
            if (type != typeof(IInteractPromptView)) return;
            promptView = ViewLocator.Get<IInteractPromptView>();
            promptView?.SetCrosshairVisible(controlEnabled);
            if (current != null) RefreshPrompt(true);
        }
    }
}
