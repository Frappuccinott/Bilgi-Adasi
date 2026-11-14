using BilgiAdasi.Player;
using NUnit.Framework;
using UnityEngine;

namespace BilgiAdasi.Tests
{
    /// <summary>Boy uzama tablosu (CLAUDE.md §7): köy başına +0.01, sonraki sınıfın boyunu geçmez, çıraklık sayılmaz.</summary>
    public class GrowthTests
    {
        private GrowthSettings settings;

        [SetUp]
        public void SetUp() => settings = ScriptableObject.CreateInstance<GrowthSettings>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(settings);

        [Test]
        public void Grade10_Start_Is166() => Assert.AreEqual(1.66f, settings.HeightFor(10, 0), 1e-4f);

        [Test]
        public void Grade10_TwoNeighborhoods_Is168() => Assert.AreEqual(1.68f, settings.HeightFor(10, 2), 1e-4f);

        [Test]
        public void Grade10_ClampedToGrade11() => Assert.AreEqual(1.71f, settings.HeightFor(10, 20), 1e-4f);

        [Test]
        public void Grade12_DoesNotGrowPerNeighborhood() => Assert.AreEqual(1.75f, settings.HeightFor(12, 5), 1e-4f);

        [Test]
        public void Apprenticeship_DoesNotCount()
        {
            Assert.IsFalse(settings.Counts("ciraklik"));
            Assert.IsTrue(settings.Counts("denklestirme"));
            Assert.IsFalse(settings.Counts(null));
        }
    }
}
