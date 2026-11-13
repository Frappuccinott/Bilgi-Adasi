using BilgiAdasi.Chemistry;
using BilgiAdasi.Order;
using NUnit.Framework;
using UnityEngine;

namespace BilgiAdasi.Tests
{
    /// <summary>Seviye 0 (Çıraklık): kendine eşit "ısıtma" tepkimesi; iki H₂ ile terazi eşit, eksik/fazla ya da yabancı maddeyle değil.</summary>
    public class ApprenticeshipTests
    {
        private PrototypeChemistrySeed chem;
        private ReactionData heat;
        private OrderTask task;

        [SetUp]
        public void SetUp()
        {
            chem = PrototypeChemistrySeed.Build();
            var h2 = chem.Species["H2"];
            heat = ReactionData.Create("R_isitma", ReactionType.Other, new[] { new SpeciesAmount(h2, 2) }, new[] { new SpeciesAmount(h2, 2) });
            task = OrderTask.Create("cirak_0", "Câbir Usta", new string[0], new string[0], "", "", OrderMode.Exact,
                new[] { new OrderStep(heat, new[] { new SpeciesAmount(h2, 2) }, true) }, new SpeciesData[0]);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(task);
            Object.DestroyImmediate(heat);
            chem.DestroyAll();
        }

        [Test]
        public void IsitmaTepkimesi_Denk()
        {
            Assert.IsTrue(AtomMath.IsBalanced(heat));
        }

        [Test]
        public void IkiHidrojen_TeraziEsit_BirIleDegil()
        {
            var s = new OrderSession(task);
            s.Pour(chem.Species["H2"]);
            Assert.IsFalse(s.IsSatisfied, "tek kavanoz: sol kefe 2 H, sağ 4 H");
            s.Pour(chem.Species["H2"]);
            Assert.IsTrue(s.IsSatisfied);
            Assert.AreEqual(4, s.Rows[0].Left);
            Assert.AreEqual(4, s.Rows[0].Right);
            s.Pour(chem.Species["H2"]);
            Assert.IsFalse(s.IsSatisfied, "üç kavanoz: fazla");
        }

        [Test]
        public void YabanciMadde_DengeSayilmaz()
        {
            var s = new OrderSession(task);
            s.Pour(chem.Species["H2"]);
            s.Pour(chem.Species["H2"]);
            s.Pour(chem.Species["O2"]);
            Assert.IsFalse(s.IsSatisfied);
        }
    }
}
