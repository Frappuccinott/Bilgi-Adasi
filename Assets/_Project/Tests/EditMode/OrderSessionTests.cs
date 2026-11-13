using BilgiAdasi.Chemistry;
using BilgiAdasi.Order;
using NUnit.Framework;
using UnityEngine;

namespace BilgiAdasi.Tests
{
    /// <summary>Sipariş mantığı (karar 13.11): dökme geri alınmaz, yabancı madde dengeyi bozar, Clear malzemeyi siler.</summary>
    public class OrderSessionTests
    {
        private PrototypeChemistrySeed chem;
        private OrderTask[] tasks;

        [SetUp]
        public void SetUp()
        {
            chem = PrototypeChemistrySeed.Build();
            tasks = DenklestirmeSeed.Build(f => chem.Species[f], id => chem.Reactions[id]);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var t in tasks) Object.DestroyImmediate(t);
            chem.DestroyAll();
        }

        private SpeciesData S(string f) => chem.Species[f];

        private static void PourN(OrderSession s, SpeciesData sp, int n)
        {
            for (int i = 0; i < n; i++) s.Pour(sp);
        }

        [Test]
        public void Order1_Water_BalancesOnlyWithCorrectCounts()
        {
            var s = new OrderSession(tasks[0]);
            Assert.IsFalse(s.IsSatisfied);
            PourN(s, S("H2"), 1);
            PourN(s, S("O2"), 1);
            Assert.IsFalse(s.IsSatisfied, "1 H2 + 1 O2 dengede değil");
            PourN(s, S("H2"), 1);
            Assert.IsTrue(s.IsSatisfied, "2 H2 + 1 O2 → 2 H2O");
            PourN(s, S("H2"), 1);
            Assert.IsFalse(s.IsSatisfied, "fazla H2 geri alınamaz, denge bozulur");
        }

        [Test]
        public void ProductPouredDirectly_IsForeign_NotBalanced()
        {
            var s = new OrderSession(tasks[0]);
            PourN(s, S("H2O"), 2); // atomlar eşit ama ürün dökülmüş: hile
            Assert.IsTrue(s.HasForeign);
            Assert.IsFalse(s.IsSatisfied);
        }

        [Test]
        public void Clear_EmptiesCauldron()
        {
            var s = new OrderSession(tasks[0]);
            PourN(s, S("H2"), 2);
            s.Clear();
            Assert.IsTrue(s.IsEmpty);
            Assert.AreEqual(0, s.CountInCauldron(S("H2")));
        }

        [Test]
        public void Order3_Propane_Balances()
        {
            var s = new OrderSession(tasks[2]);
            PourN(s, S("C3H8"), 1);
            PourN(s, S("O2"), 5);
            Assert.IsTrue(s.IsSatisfied);
        }

        [Test]
        public void Order4_Chain_SecondStepUsesCaO()
        {
            var s = new OrderSession(tasks[3]);
            PourN(s, S("CaCO3"), 1);
            Assert.IsTrue(s.IsSatisfied);
            Assert.IsTrue(s.HasNextStep);
            s.AdvanceStep();
            Assert.IsTrue(s.IsEmpty);
            PourN(s, S("CaO"), 1);
            PourN(s, S("H2O"), 1);
            Assert.IsTrue(s.IsSatisfied);
        }

        [Test]
        public void Order5_Purchase_ExcessAllowedAsLeftover()
        {
            var s = new OrderSession(tasks[4]);
            PourN(s, S("N2"), 2);
            PourN(s, S("H2"), 5);
            Assert.IsFalse(s.IsSatisfied, "6 H2 gerekli");
            PourN(s, S("H2"), 2);
            Assert.IsTrue(s.IsSatisfied, "fazla H2 artan olarak kabul");
            bool leftover = false;
            foreach (var r in s.Rows) if (r.Leftover > 0) leftover = true;
            Assert.IsTrue(leftover);
        }

        [Test]
        public void FailCount_Increments()
        {
            var s = new OrderSession(tasks[0]);
            Assert.AreEqual(1, s.RegisterFail());
            Assert.AreEqual(2, s.RegisterFail());
        }
    }
}
