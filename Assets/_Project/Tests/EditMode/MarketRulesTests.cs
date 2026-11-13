using System.Collections.Generic;
using BilgiAdasi.Order.Market;
using NUnit.Framework;

namespace BilgiAdasi.Tests
{
    /// <summary>Pazar kuralları (saf C#): görevle aç/kapat, siparişin ürününü satmama, stok ve kural sırası.</summary>
    public class MarketRulesTests
    {
        /// <summary>Elle ayarlanan sahte oyun durumu.</summary>
        private sealed class FakeState : IMarketState
        {
            public readonly HashSet<string> Completed = new HashSet<string>();
            public readonly HashSet<string> OrderProducts = new HashSet<string>();
            public readonly Dictionary<string, int> Sold = new Dictionary<string, int>();

            public bool IsTaskCompleted(string taskId) => Completed.Contains(taskId);
            public bool IsActiveOrderProduct(string key) => OrderProducts.Contains(key);
            public int SoldCount(string key) => Sold.TryGetValue(key, out int n) ? n : 0;
        }

        private FakeState state;

        [SetUp]
        public void SetUp() => state = new FakeState();

        private static MarketOffer Offer(string key, bool unlimited = true, int stock = 0,
            string after = null, string removedAfter = null, int price = 1) =>
            new MarketOffer(key, 0, price, unlimited, stock, after, removedAfter);

        private static List<string> Keys(List<MarketOffer> offers)
        {
            var keys = new List<string>();
            foreach (var o in offers) keys.Add(o.Key);
            return keys;
        }

        // ---------- TaskGateRule ----------

        [Test]
        public void TaskGate_NoTasks_AlwaysListed()
        {
            var offers = new List<MarketOffer> { Offer("O2") };
            new TaskGateRule().Apply(offers, state);
            Assert.AreEqual(1, offers.Count);
        }

        [Test]
        public void TaskGate_AvailableAfter_HiddenUntilTaskDone()
        {
            var offers = new List<MarketOffer> { Offer("N2", after: "denk_3") };
            new TaskGateRule().Apply(offers, state);
            Assert.AreEqual(0, offers.Count);

            state.Completed.Add("denk_3");
            offers.Add(Offer("N2", after: "denk_3"));
            new TaskGateRule().Apply(offers, state);
            Assert.AreEqual(1, offers.Count);
        }

        [Test]
        public void TaskGate_RemovedAfter_HiddenOnceTaskDone()
        {
            var offers = new List<MarketOffer> { Offer("CH4", removedAfter: "denk_2") };
            new TaskGateRule().Apply(offers, state);
            Assert.AreEqual(1, offers.Count);

            state.Completed.Add("denk_2");
            new TaskGateRule().Apply(offers, state);
            Assert.AreEqual(0, offers.Count);
        }

        [Test]
        public void TaskGate_OpenWindow_OnlyBetweenTheTwoTasks()
        {
            var rule = new TaskGateRule();
            List<MarketOffer> Fresh() => new List<MarketOffer> { Offer("H2", after: "denk_1", removedAfter: "denk_4") };

            var o = Fresh(); rule.Apply(o, state); Assert.AreEqual(0, o.Count, "önce kapalı");
            state.Completed.Add("denk_1");
            o = Fresh(); rule.Apply(o, state); Assert.AreEqual(1, o.Count, "arada açık");
            state.Completed.Add("denk_4");
            o = Fresh(); rule.Apply(o, state); Assert.AreEqual(0, o.Count, "sonra kalkar");
        }

        // ---------- HideOrderProductRule ----------

        [Test]
        public void HideOrderProduct_RemovesOnlyActiveOrderProducts()
        {
            state.OrderProducts.Add("H2O");
            var offers = new List<MarketOffer> { Offer("H2"), Offer("H2O"), Offer("O2") };
            new HideOrderProductRule().Apply(offers, state);
            CollectionAssert.AreEqual(new[] { "H2", "O2" }, Keys(offers));
        }

        [Test]
        public void HideOrderProduct_NoOrder_KeepsAll()
        {
            var offers = new List<MarketOffer> { Offer("H2O"), Offer("O2") };
            new HideOrderProductRule().Apply(offers, state);
            Assert.AreEqual(2, offers.Count);
        }

        // ---------- StockRule ----------

        [Test]
        public void Stock_Unlimited_NeverRemoved()
        {
            state.Sold["O2"] = 999;
            var offers = new List<MarketOffer> { Offer("O2") };
            new StockRule().Apply(offers, state);
            Assert.AreEqual(1, offers.Count);
        }

        [Test]
        public void Stock_Limited_RemainingIsStockMinusSold()
        {
            state.Sold["CaCO3"] = 2;
            var offers = new List<MarketOffer> { Offer("CaCO3", unlimited: false, stock: 5) };
            new StockRule().Apply(offers, state);
            Assert.AreEqual(1, offers.Count);
            Assert.AreEqual(3, offers[0].Remaining);
        }

        [Test]
        public void Stock_SoldOut_Removed()
        {
            state.Sold["CaCO3"] = 5;
            var offers = new List<MarketOffer> { Offer("CaCO3", unlimited: false, stock: 5) };
            new StockRule().Apply(offers, state);
            Assert.AreEqual(0, offers.Count);
        }

        [Test]
        public void Stock_ZeroStock_RemovedEvenIfNothingSold()
        {
            var offers = new List<MarketOffer> { Offer("N2", unlimited: false, stock: 0) };
            new StockRule().Apply(offers, state);
            Assert.AreEqual(0, offers.Count);
        }

        // ---------- MarketRuleSet ----------

        [Test]
        public void RuleSet_Default_AppliesAllRulesAndKeepsOrder()
        {
            state.OrderProducts.Add("H2O");
            state.Sold["C3H8"] = 1;
            var offers = new List<MarketOffer>
            {
                Offer("H2"),
                Offer("O2"),
                Offer("H2O"),                                // siparişin ürünü
                Offer("CH4", after: "denk_2"),               // görev bitmedi
                Offer("C3H8", unlimited: false, stock: 1),   // tükendi
                Offer("CaCO3", unlimited: false, stock: 3),
            };
            MarketRuleSet.CreateDefault().Apply(offers, state);
            CollectionAssert.AreEqual(new[] { "H2", "O2", "CaCO3" }, Keys(offers));
        }

        [Test]
        public void RuleSet_CustomRule_AddedWithoutTouchingOthers()
        {
            var set = MarketRuleSet.CreateDefault().Add(new HalfPriceRule());
            var offers = new List<MarketOffer> { Offer("O2", price: 4) };
            set.Apply(offers, state);
            Assert.AreEqual(2, offers[0].Price);
        }

        [Test]
        public void RuleSet_NullInputs_DoNotThrow()
        {
            var set = MarketRuleSet.CreateDefault();
            Assert.DoesNotThrow(() => set.Apply(null, state));
            Assert.DoesNotThrow(() => set.Apply(new List<MarketOffer>(), null));
        }

        /// <summary>Örnek: yeni kural yalnızca yeni bir sınıftır.</summary>
        private sealed class HalfPriceRule : IMarketRule
        {
            public void Apply(List<MarketOffer> offers, IMarketState s)
            {
                foreach (var o in offers) o.Price /= 2;
            }
        }
    }
}
