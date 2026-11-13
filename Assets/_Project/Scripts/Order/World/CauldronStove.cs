using BilgiAdasi.Player;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>Kazanın ocağı: E → ateşi yak. Terazi dengedeyse tepkime, değilse köpürme (malzeme gider).</summary>
    public sealed class CauldronStove : WorldInteractable
    {
        [Tooltip("Kazanın OrderController'ı (üst objede).")]
        [SerializeField] private OrderController controller;

        protected override void Awake()
        {
            base.Awake();
            if (controller == null) controller = GetComponentInParent<OrderController>();
        }

        public override bool CanInteract => base.CanInteract && controller != null && !controller.IsBusy;

        public override string GetPrompt(PlayerInteractor interactor) =>
            controller.Texts != null ? controller.Texts.FirePrompt : string.Empty;

        public override void Interact(PlayerInteractor interactor) => controller.Fire(interactor.Inventory);
    }
}
