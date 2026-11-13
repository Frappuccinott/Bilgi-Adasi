using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Order;
using NUnit.Framework;

namespace BilgiAdasi.Tests
{
    /// <summary>Hata Dedektifi: işaretlenen küme = AtomMath'e göre dengesiz elementler (D-3'ün iki turu).</summary>
    public sealed class ErrorHuntTests
    {
        private ElementData c, h, o;
        private SpeciesData ch4, o2, co2, h2o, c3h8;

        [SetUp]
        public void SetUp()
        {
            c = ElementData.Create("C", "Karbon", UnityEngine.Color.gray);
            h = ElementData.Create("H", "Hidrojen", UnityEngine.Color.white);
            o = ElementData.Create("O", "Oksijen", UnityEngine.Color.red);
            ch4 = SpeciesData.Create("CH4", "Metan", new ElementCount(c, 1), new ElementCount(h, 4));
            o2 = SpeciesData.Create("O2", "Oksijen", new ElementCount(o, 2));
            co2 = SpeciesData.Create("CO2", "Karbondioksit", new ElementCount(c, 1), new ElementCount(o, 2));
            h2o = SpeciesData.Create("H2O", "Su", new ElementCount(h, 2), new ElementCount(o, 1));
            c3h8 = SpeciesData.Create("C3H8", "Propan", new ElementCount(c, 3), new ElementCount(h, 8));
        }

        private static bool Check(List<SpeciesAmount> re, List<SpeciesAmount> pr, params ElementData[] marks)
        {
            var order = new List<ElementData>();
            var l = new Dictionary<ElementData, int>();
            var r = new Dictionary<ElementData, int>();
            ErrorHuntLogic.Count(re, pr, order, l, r);
            return ErrorHuntLogic.IsCorrect(order, l, r, new HashSet<ElementData>(marks));
        }

        [Test]
        public void Round1_MethaneCopy_HAndOUnbalanced()
        {
            var re = new List<SpeciesAmount> { new SpeciesAmount(ch4, 1), new SpeciesAmount(o2, 1) };
            var pr = new List<SpeciesAmount> { new SpeciesAmount(co2, 1), new SpeciesAmount(h2o, 1) };
            Assert.IsTrue(Check(re, pr, h, o));
            Assert.IsFalse(Check(re, pr, h));        // eksik
            Assert.IsFalse(Check(re, pr, c, h, o));  // fazla (C dengeli)
            Assert.IsFalse(Check(re, pr));
        }

        [Test]
        public void Round2_Propane_CAndOUnbalanced()
        {
            var re = new List<SpeciesAmount> { new SpeciesAmount(c3h8, 1), new SpeciesAmount(o2, 5) };
            var pr = new List<SpeciesAmount> { new SpeciesAmount(co2, 1), new SpeciesAmount(h2o, 4) };
            Assert.IsTrue(Check(re, pr, c, o));
            Assert.IsFalse(Check(re, pr, h, o));
        }

        [Test]
        public void Order_ReactantElementsFirst()
        {
            var order = new List<ElementData>();
            ErrorHuntLogic.Count(new List<SpeciesAmount> { new SpeciesAmount(ch4, 1) },
                new List<SpeciesAmount> { new SpeciesAmount(h2o, 1) }, order,
                new Dictionary<ElementData, int>(), new Dictionary<ElementData, int>());
            CollectionAssert.AreEqual(new[] { c, h, o }, order);
        }
    }
}
