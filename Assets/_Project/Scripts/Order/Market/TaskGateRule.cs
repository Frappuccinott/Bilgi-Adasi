using System.Collections.Generic;

namespace BilgiAdasi.Order.Market
{
    /// <summary>
    /// Görevle aç / kapat: "şu görev bitince açılır" henüz bitmediyse ya da "şu görev bitince kalkar" bittiyse satır çıkar.
    /// </summary>
    public sealed class TaskGateRule : IMarketRule
    {
        public void Apply(List<MarketOffer> offers, IMarketState state)
        {
            for (int i = offers.Count - 1; i >= 0; i--)
            {
                var o = offers[i];
                bool locked = !string.IsNullOrEmpty(o.AvailableAfterTaskId) && !state.IsTaskCompleted(o.AvailableAfterTaskId);
                bool removed = !string.IsNullOrEmpty(o.RemovedAfterTaskId) && state.IsTaskCompleted(o.RemovedAfterTaskId);
                if (locked || removed) offers.RemoveAt(i);
            }
        }
    }
}
