using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Kazan istasyonu (sipariş). Fare: imlecin altındaki hedef. Gamepad: Navigate (sol çubuk / D-pad) ile
    /// hedefler arasında gezinme. Click/Increase = ekle, Decrease = çıkar, FireStove = ateş, OpenHint = defter.
    /// Mantık OrderController'da; bu sınıf yalnızca girdiyi hedeflere çevirir.
    /// </summary>
    public sealed class CauldronStation : StationBase
    {
        [SerializeField] private OrderController controller;
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private float rayDistance = 8f;
        [Tooltip("Gamepad'de hedefler arası geçiş bekleme süresi (sn).")]
        [SerializeField] private float navigateRepeat = 0.22f;

        private static readonly List<StationTarget> Ordered = new List<StationTarget>();
        private static Camera cachedCamera;

        private StationTarget focused;
        private float nextNavigateTime;

        public override bool CanInteract => base.CanInteract && !Dialogue.IsOpen;

        protected override void OnEnter()
        {
            var s = GameInput.Controls.Station;
            s.Click.performed += OnClick;
            s.Increase.performed += OnIncrease;
            s.Decrease.performed += OnDecrease;
            s.FireStove.performed += OnFire;
            s.OpenHint.performed += OnHint;
            if (controller != null)
            {
                controller.OrderFinished += OnOrderFinished;
                controller.BeginSession();
            }
        }

        protected override void OnExit()
        {
            var s = GameInput.Controls.Station;
            s.Click.performed -= OnClick;
            s.Increase.performed -= OnIncrease;
            s.Decrease.performed -= OnDecrease;
            s.FireStove.performed -= OnFire;
            s.OpenHint.performed -= OnHint;
            SetFocus(null);
            if (controller != null)
            {
                controller.OrderFinished -= OnOrderFinished;
                controller.EndSession();
            }
        }

        protected override bool CanExit() => controller == null || (!controller.Busy && !Dialogue.IsOpen);

        private void Update()
        {
            if (!IsActive || Dialogue.IsOpen) return;
            if (GameInput.LastDevice == InputDeviceKind.Gamepad) UpdateGamepadFocus();
            else UpdatePointerFocus();
        }

        private void UpdatePointerFocus()
        {
            if (cachedCamera == null) cachedCamera = Camera.main;
            if (cachedCamera == null) return;
            Vector2 point = GameInput.Controls.Station.Point.ReadValue<Vector2>();
            Ray ray = cachedCamera.ScreenPointToRay(point);
            StationTarget hit = null;
            if (Physics.Raycast(ray, out var info, rayDistance, targetMask, QueryTriggerInteraction.Collide))
                hit = info.collider.GetComponentInParent<StationTarget>();
            SetFocus(hit);
        }

        private void UpdateGamepadFocus()
        {
            Vector2 nav = GameInput.Controls.Station.Navigate.ReadValue<Vector2>();
            if (Mathf.Abs(nav.x) < 0.5f && Mathf.Abs(nav.y) < 0.5f)
            {
                nextNavigateTime = 0f;
                if (focused == null || !focused.isActiveAndEnabled) Step(0);
                return;
            }
            if (Time.unscaledTime < nextNavigateTime) return;
            nextNavigateTime = Time.unscaledTime + navigateRepeat;
            Step(Mathf.Abs(nav.x) >= Mathf.Abs(nav.y) ? (nav.x > 0 ? 1 : -1) : (nav.y > 0 ? -1 : 1));
        }

        /// <summary>Hedefleri ekranda soldan sağa sıralar ve odağı delta kadar kaydırır.</summary>
        private void Step(int delta)
        {
            if (cachedCamera == null) cachedCamera = Camera.main;
            Ordered.Clear();
            var active = StationTarget.Active;
            for (int i = 0; i < active.Count; i++) Ordered.Add(active[i]);
            if (Ordered.Count == 0)
            {
                SetFocus(null);
                return;
            }
            if (cachedCamera != null) Ordered.Sort(CompareScreenX);
            int index = focused != null ? Ordered.IndexOf(focused) : -1;
            index = index < 0 ? 0 : (index + delta + Ordered.Count) % Ordered.Count;
            SetFocus(Ordered[index]);
        }

        private static int CompareScreenX(StationTarget a, StationTarget b)
        {
            float ax = cachedCamera.WorldToScreenPoint(a.transform.position).x;
            float bx = cachedCamera.WorldToScreenPoint(b.transform.position).x;
            return ax.CompareTo(bx);
        }

        private void SetFocus(StationTarget target)
        {
            if (focused == target) return;
            if (focused != null) focused.SetFocused(false);
            focused = target;
            if (focused != null) focused.SetFocused(true);
        }

        private void OnClick(InputAction.CallbackContext _)
        {
            if (controller == null || focused == null || Dialogue.IsOpen) return;
            switch (focused.Kind)
            {
                case StationTargetKind.Jar: controller.AddJar(focused.Species); break;
                case StationTargetKind.Stove: controller.Fire(); break;
                case StationTargetKind.HintBook: controller.OpenHint(); break;
            }
        }

        private void OnIncrease(InputAction.CallbackContext _)
        {
            if (controller != null && focused != null && focused.Kind == StationTargetKind.Jar && !Dialogue.IsOpen)
                controller.AddJar(focused.Species);
        }

        private void OnDecrease(InputAction.CallbackContext _)
        {
            if (controller != null && focused != null && focused.Kind == StationTargetKind.Jar && !Dialogue.IsOpen)
                controller.RemoveJar(focused.Species);
        }

        private void OnFire(InputAction.CallbackContext _)
        {
            if (controller != null && !Dialogue.IsOpen) controller.Fire();
        }

        private void OnHint(InputAction.CallbackContext _)
        {
            if (controller != null && !Dialogue.IsOpen) controller.OpenHint();
        }

        private void OnOrderFinished() => Exit();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            Ordered.Clear();
            cachedCamera = null;
        }
    }
}
