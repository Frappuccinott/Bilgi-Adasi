using BilgiAdasi.Player;

namespace BilgiAdasi.Order
{
    /// <summary>Kazan istasyonu (sipariş). Akış OrderController'da; tepkime/köpürme sürerken çıkılamaz.</summary>
    public sealed class CauldronStation : StationBase
    {
        private OrderController controller;

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponent<OrderController>();
        }

        protected override bool CanExit() => controller == null || !controller.IsBusy;
    }
}
