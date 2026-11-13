using BilgiAdasi.Chemistry;
using BilgiAdasi.Order;
using NUnit.Framework;
using UnityEngine;

namespace BilgiAdasi.Tests
{
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

        private static void AddN(OrderSession s, SpeciesData sp, int n)
        {
            for (int i = 0; i < n; i++) Assert.AreEqual(AddResult.Added, s.Add(sp));
        }

        [Test]
        public void Order1_Water_BalancesOnlyWithCorrectCounts()
        {
            var s = new OrderSession(tasks[0]);
            Assert.IsFalse(s.IsSatisfied);

            AddN(s, S("H2"), 1);
            AddN(s, S("O2"), 1);
            Assert.IsFalse(s.IsSatisfied, "1 H2 + 1 O2 dengede olmamalı");

            AddN(s, S("H2"), 1);
            Assert.IsTrue(s.IsSatisfied, "2 H2 + 1 O2 → 2 H2O dengede olmalı");

            AddN(s, S("O2"), 1);
            Assert.IsFalse(s.IsSatisfied, "fazla O2 dengeyi bozmalı");
        }

        [Test]
        public void Remove_ToZero_ReturnsJarToShelf()
        {
            var s = new OrderSession(tasks[0]);
            AddN(s, S("H2"), 1);
            Assert.AreEqual(1, s.Contents.Count);
            Assert.IsTrue(s.Remove(S("H2")));
            Assert.AreEqual(0, s.Contents.Count);
            Assert.IsFalse(s.Remove(S("H2")));
        }

        [Test]
        public void NotOnShelf_IsRejected()
        {
            var s = new OrderSession(tasks[0]);
            Assert.AreEqual(AddResult.NotOnShelf, s.Add(S("CH4")));
        }

        [TestCase(1, "CH4", 1, "O2", 2)]
        [TestCase(2, "C3H8", 1, "O2", 5)]
        public void Combustion_Orders_Balance(int index, string fuel, int fuelN, string ox, int oxN)
        {
            var s = new OrderSession(tasks[index]);
            AddN(s, S(fuel), fuelN);
            AddN(s, S(ox), oxN - 1);
            Assert.IsFalse(s.IsSatisfied);
            AddN(s, S(ox), 1);
            Assert.IsTrue(s.IsSatisfied);
        }

        [Test]
        public void Order4_Chain_StepProductGoesToShelf()
        {
            var s = new OrderSession(tasks[3]);
            Assert.AreEqual(AddResult.NotOnShelf, s.Add(S("CaO")), "CaO başta rafta olmamalı");

            AddN(s, S("CaCO3"), 1);
            Assert.IsTrue(s.IsSatisfied);
            Assert.IsTrue(s.HasNextStep);

            s.AdvanceStep();
            Assert.AreEqual(1, s.StepIndex);
            Assert.AreEqual(0, s.Contents.Count, "yeni adımda kazan boş");

            AddN(s, S("CaO"), 1);
            AddN(s, S("H2O"), 1);
            Assert.IsTrue(s.IsSatisfied);
            Assert.IsFalse(s.HasNextStep);
        }

        [Test]
        public void Order5_Purchase_ExactBuy_SatisfiesWithMoneyLeft()
        {
            var s = new OrderSession(tasks[4]);
            Assert.AreEqual(20, s.Money);

            AddN(s, S("N2"), 2);   // 6 akçe
            AddN(s, S("H2"), 6);   // 12 akçe
            Assert.IsTrue(s.IsSatisfied);
            Assert.AreEqual(2, s.Money);
        }

        [Test]
        public void Order5_Purchase_ExcessIsLeftoverAndStillSatisfied()
        {
            var s = new OrderSession(tasks[4]);
            AddN(s, S("N2"), 2);
            AddN(s, S("H2"), 7);   // 1 fazla
            Assert.IsTrue(s.IsSatisfied);
            Assert.AreEqual(0, s.Money);

            int hLeftover = 0;
            foreach (var row in s.Rows)
                if (row.Element.Symbol == "H") hLeftover = row.Leftover;
            Assert.AreEqual(2, hLeftover, "1 fazla H2 = 2 artan H atomu");
        }

        [Test]
        public void Order5_Purchase_RemoveDoesNotRefund_ReAddIsFree()
        {
            var s = new OrderSession(tasks[4]);
            AddN(s, S("N2"), 1);
            Assert.AreEqual(17, s.Money);
            s.Remove(S("N2"));
            Assert.AreEqual(17, s.Money, "çıkarınca akçe geri gelmez");
            AddN(s, S("N2"), 1);
            Assert.AreEqual(17, s.Money, "elde olan kavanoz tekrar bedava eklenir");
        }

        [Test]
        public void Order5_Purchase_OutOfMoney_ShortageForgivenOnce()
        {
            var s = new OrderSession(tasks[4]);
            AddN(s, S("N2"), 5);   // 15 akçe, 3 fazla N2
            AddN(s, S("H2"), 2);   // 4 akçe → kalan 1
            Assert.AreEqual(AddResult.NotEnoughMoney, s.Add(S("H2")));
            Assert.IsFalse(s.IsSatisfied);

            Assert.IsTrue(s.TryForgiveShortage());
            Assert.IsTrue(s.IsSatisfied);
            Assert.IsFalse(s.TryForgiveShortage(), "yalnızca bir kez");
        }
    }
}
