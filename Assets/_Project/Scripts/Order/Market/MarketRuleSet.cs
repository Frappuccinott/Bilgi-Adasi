using System.Collections.Generic;

namespace BilgiAdasi.Order.Market
{
    /// <summary>
    /// Kuralları sırayla uygular. Sıra önemlidir: önce görünürlük (görev, sipariş), sonra stok.
    /// </summary>
    public sealed class MarketRuleSet
    {
        private readonly List<IMarketRule> rules = new List<IMarketRule>();

        public MarketRuleSet(params IMarketRule[] rules)
        {
            if (rules != null) this.rules.AddRange(rules);
        }

        /// <summary>Prototip pazarının kuralları.</summary>
        public static MarketRuleSet CreateDefault() => new MarketRuleSet(
            new TaskGateRule(),
            new HideOrderProductRule(),
            new StockRule());

        public MarketRuleSet Add(IMarketRule rule)
        {
            if (rule != null) rules.Add(rule);
            return this;
        }

        /// <summary>Listeyi yerinde süzer; satılacak satırlar kalır.</summary>
        public void Apply(List<MarketOffer> offers, IMarketState state)
        {
            if (offers == null || state == null) return;
            for (int i = 0; i < rules.Count; i++) rules[i].Apply(offers, state);
        }
    }
}
