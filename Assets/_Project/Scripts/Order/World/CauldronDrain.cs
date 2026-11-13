using BilgiAdasi.Player;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>Kazanın boşaltma kolu: E → kazan boşalır, malzeme gider (hata sayılmaz). Dökülen geri alınamaz.</summary>
    public sealed class CauldronDrain : WorldInteractable
    {
        [Tooltip("Kazanın OrderController'ı (üst objede).")]
        [SerializeField] private OrderController controller;
        [Tooltip("Çekilince dönen kol parçası (pivotu menteşede). Boş bırakılabilir.")]
        [SerializeField] private Transform lever;

        private float pulledUntil;

        protected override void Awake()
        {
            base.Awake();
            if (controller == null) controller = GetComponentInParent<OrderController>();
        }

        public override bool CanInteract =>
            base.CanInteract && controller != null && controller.CanPour && !controller.Session.IsEmpty;

        public override string GetPrompt(PlayerInteractor interactor) =>
            controller.Texts != null ? controller.Texts.DrainPrompt : string.Empty;

        public override void Interact(PlayerInteractor interactor)
        {
            controller.Drain();
            pulledUntil = Time.time + 0.5f;
        }

        private void Update()
        {
            if (lever == null) return;
            bool pulled = Time.time < pulledUntil;
            // Kol dinlenme konumundaysa her karede transform yazılmaz.
            if (!pulled && lever.localRotation == Quaternion.identity) return;
            float target = pulled ? 50f : 0f;
            var rot = Quaternion.Euler(target, 0f, 0f);
            lever.localRotation = Quaternion.Slerp(lever.localRotation, rot, 1f - Mathf.Exp(-12f * Time.deltaTime));
        }
    }
}
