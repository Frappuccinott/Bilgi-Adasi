using System.Collections.Generic;

namespace BilgiAdasi.Order.Market
{
    /// <summary>
    /// Siparişin ürününü satma: alınmış bir siparişin istediği madde pazardan çıkar (oyuncu üretmek zorunda).
    /// Katalogda ayrıca işaretlemek gerekmez; otomatiktir.
    /// </summary>
    public sealed class HideOrderProductRule : IMarketRule
    {
        public void Apply(List<MarketOffer> offers, IMarketState state)
        {
            for (int i = offers.Count - 1; i >= 0; i--)
                if (state.IsActiveOrderProduct(offers[i].Key)) offers.RemoveAt(i);
        }
    }
}
