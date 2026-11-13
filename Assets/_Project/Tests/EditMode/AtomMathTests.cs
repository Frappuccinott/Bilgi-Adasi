using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using NUnit.Framework;

namespace BilgiAdasi.Tests
{
    public class AtomMathTests
    {
        private PrototypeChemistrySeed seed;

        [SetUp]
        public void SetUp() => seed = PrototypeChemistrySeed.Build();

        [TearDown]
        public void TearDown() => seed.DestroyAll();

        [TestCase("R_water")]
        [TestCase("R_methane")]
        [TestCase("R_propane")]
        [TestCase("R_lime1")]
        [TestCase("R_lime2")]
        [TestCase("R_ammonia")]
        [TestCase("R_co")]
        [TestCase("R_waterSplit")]
        public void PrototypeReactions_AreBalanced(string id)
        {
            Assert.IsTrue(AtomMath.IsBalanced(seed.Reactions[id]), id + " dengede değil");
        }

        [Test]
        public void Count_SumsAtomsTimesAmount()
        {
            // 2 CH4 + 1 O2 → C 2, H 8, O 2
            var items = new List<SpeciesAmount>
            {
                new SpeciesAmount(seed.Species["CH4"], 2),
                new SpeciesAmount(seed.Species["O2"], 1),
            };
            var result = new Dictionary<ElementData, int>();
            AtomMath.Count(items, result);

            Assert.AreEqual(2, result[seed.Elements["C"]]);
            Assert.AreEqual(8, result[seed.Elements["H"]]);
            Assert.AreEqual(2, result[seed.Elements["O"]]);
        }

        [Test]
        public void Cauldron_WithWrongAmounts_IsNotBalanced()
        {
            // Kazana 1 H2 + 1 O2, hedef 2 H2O → dengesiz
            var left = new Dictionary<ElementData, int>();
            var right = new Dictionary<ElementData, int>();
            AtomMath.Count(new[] { new SpeciesAmount(seed.Species["H2"], 1), new SpeciesAmount(seed.Species["O2"], 1) }, left);
            AtomMath.Count(new[] { new SpeciesAmount(seed.Species["H2O"], 2) }, right);

            Assert.IsFalse(AtomMath.Equal(left, right));

            var diff = new Dictionary<ElementData, int>();
            AtomMath.Difference(left, right, diff);
            Assert.AreEqual(-2, diff[seed.Elements["H"]]); // 2 H eksik
            Assert.IsFalse(diff.ContainsKey(seed.Elements["O"])); // O eşit
        }

        [Test]
        public void Equal_TreatsMissingKeyAsZero()
        {
            var a = new Dictionary<ElementData, int> { { seed.Elements["H"], 2 }, { seed.Elements["O"], 0 } };
            var b = new Dictionary<ElementData, int> { { seed.Elements["H"], 2 } };
            Assert.IsTrue(AtomMath.Equal(a, b));
        }

        [Test]
        public void Gcd_Works()
        {
            Assert.AreEqual(2, AtomMath.Gcd(4, 6));
            Assert.AreEqual(5, AtomMath.Gcd(0, 5));
            Assert.AreEqual(3, AtomMath.Gcd(-9, 6));
        }

        [Test]
        public void IsMinimal_DetectsReducibleCoefficients()
        {
            Assert.IsFalse(AtomMath.IsMinimal(new[] { 4, 2, 4 }));
            Assert.IsTrue(AtomMath.IsMinimal(new[] { 2, 1, 2 }));
            Assert.IsTrue(AtomMath.IsMinimal(new[] { 1, 5, 3, 4 }));
        }

        [Test]
        public void RequiredForPurchase_Ammonia4()
        {
            // N2 + 3H2 → 2NH3, hedef 4 NH3 → 2 N2 + 6 H2
            var required = new Dictionary<SpeciesData, int>();
            bool ok = AtomMath.RequiredForPurchase(seed.Reactions["R_ammonia"], seed.Species["NH3"], 4, required);

            Assert.IsTrue(ok);
            Assert.AreEqual(2, required[seed.Species["N2"]]);
            Assert.AreEqual(6, required[seed.Species["H2"]]);
        }

        [Test]
        public void RequiredForPurchase_NonIntegerScale_ReturnsFalse()
        {
            var required = new Dictionary<SpeciesData, int>();
            Assert.IsFalse(AtomMath.RequiredForPurchase(seed.Reactions["R_ammonia"], seed.Species["NH3"], 3, required));
        }
    }

    public class FormulaFormatterTests
    {
        [TestCase("H2O", "H<sub>2</sub>O")]
        [TestCase("Ca(OH)2", "Ca(OH)<sub>2</sub>")]
        [TestCase("C3H8", "C<sub>3</sub>H<sub>8</sub>")]
        [TestCase("CaCO3", "CaCO<sub>3</sub>")]
        [TestCase("CO", "CO")]
        public void ToRich_SubscriptsDigits(string plain, string rich)
        {
            Assert.AreEqual(rich, FormulaFormatter.ToRich(plain));
        }

        [Test]
        public void Equation_HidesCoefficientOne()
        {
            var seed = PrototypeChemistrySeed.Build();
            try
            {
                Assert.AreEqual(
                    "CH<sub>4</sub> + 2O<sub>2</sub> → CO<sub>2</sub> + 2H<sub>2</sub>O",
                    FormulaFormatter.Equation(seed.Reactions["R_methane"]));
            }
            finally
            {
                seed.DestroyAll();
            }
        }
    }
}
