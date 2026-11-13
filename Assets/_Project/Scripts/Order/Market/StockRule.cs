using System.Collections.Generic;

namespace BilgiAdasi.Order.Market
{
    /// <summary>
    /// Stok: sınırlı satırlarda kalan = stok − satılan; kalan 0 olunca satır çıkar. Sınırsız satırlara dokunmaz.
    /// </summary>
    public sealed class StockRule : IMarketRule
    {
        public void Apply(List<MarketOffer> offers, IMarketState state)
        {
            for (int i = offers.Count - 1; i >= 0; i--)
            {
                var o = offers[i];
                if (o.UnlimitedStock) continue;
                int left = o.Stock - state.SoldCount(o.Key);
                o.Remaining = left < 0 ? 0 : left;
                if (o.Remaining == 0) offers.RemoveAt(i);
            }
        }
    }
}
