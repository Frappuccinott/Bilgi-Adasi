using BilgiAdasi.Chemistry;
using BilgiAdasi.Player;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Kazan (karar 13.11: istasyon kamerası yok, birinci şahıstan). Elde madde varken E → bir kavanoz dökülür.
    /// Envanter doluyken kazanda bekleyen ürün varsa E → ürünü al. Akış OrderController'da (aynı objede).
    /// </summary>
    [RequireComponent(typeof(OrderController))]
    public sealed class CauldronStation : WorldInteractable
    {
        private OrderController controller;

        // İpucu önbelleği: durum değişmedikçe aynı string döner (her karede ayırma olmasın).
        private string cachedPrompt;
        private SpeciesData cachedSpecies;
        private bool cachedPending;

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponent<OrderController>();
        }

        public override string GetPrompt(PlayerInteractor interactor)
        {
            var texts = controller.Texts;
            if (texts == null) return string.Empty;
            bool pendingNow = controller.HasPendingProduct;
            var s = interactor.Inventory != null ? interactor.Inventory.SelectedSpecies : null;
            if (cachedPrompt != null && pendingNow == cachedPending && s == cachedSpecies) return cachedPrompt;

            cachedPending = pendingNow;
            cachedSpecies = s;
            cachedPrompt = pendingNow ? texts.TakeProductPrompt
                : s == null ? texts.EmptyHandPrompt
                : string.Format(texts.PourPrompt, s.FormulaRich);
            return cachedPrompt;
        }

        public override void Interact(PlayerInteractor interactor)
        {
            if (controller.HasPendingProduct) controller.TakePending(interactor.Inventory);
            else controller.Pour(interactor.Inventory);
        }
    }
}
