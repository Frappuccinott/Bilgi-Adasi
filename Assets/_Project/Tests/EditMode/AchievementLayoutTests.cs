using BilgiAdasi.Core;
using NUnit.Framework;

namespace BilgiAdasi.Tests
{
    /// <summary>Başarım ağacı yerleşimi (saf C#): ana dal düz gider, yan dallar altta boş satıra iner, çakışma yok.</summary>
    public class AchievementLayoutTests
    {
        [Test]
        public void Chain_StaysOnFirstRow_SideBranchesGoBelow()
        {
            // 0 kök → 1 → 2 → 4 (ana dal); 3 = 1'in ikinci çocuğu; 5 = kökün ikinci çocuğu
            int[] parents = { -1, 0, 1, 1, 2, 0 };
            var cols = new int[parents.Length];
            var rows = new int[parents.Length];
            AchievementLayout.Compute(parents, cols, rows);

            Assert.AreEqual(new[] { 0, 1, 2, 2, 3, 1 }, cols);
            Assert.AreEqual(0, rows[0]);
            Assert.AreEqual(0, rows[1]);
            Assert.AreEqual(0, rows[2]);
            Assert.AreEqual(1, rows[3]); // 2 ile aynı sütun, bir alt satır
            Assert.AreEqual(0, rows[4]); // 2'nin tek (ilk) çocuğu: ana dal devam eder
            Assert.AreEqual(1, rows[5]);
        }

        [Test]
        public void NoTwoNodesShareACell()
        {
            int[] parents = { -1, 0, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 0 };
            var cols = new int[parents.Length];
            var rows = new int[parents.Length];
            AchievementLayout.Compute(parents, cols, rows);

            for (int a = 0; a < parents.Length; a++)
                for (int b = a + 1; b < parents.Length; b++)
                    Assert.IsFalse(cols[a] == cols[b] && rows[a] == rows[b], $"{a} ve {b} aynı hücrede");
        }

        [Test]
        public void ParentAfterChild_IsTreatedAsRoot()
        {
            int[] parents = { 1, -1 };
            var cols = new int[2];
            var rows = new int[2];
            AchievementLayout.Compute(parents, cols, rows);
            Assert.AreEqual(0, cols[0]);
            Assert.AreEqual(0, cols[1]);
            Assert.AreNotEqual(rows[0], rows[1]);
        }
    }
}
