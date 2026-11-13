using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Desk;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace BilgiAdasi.Tests
{
    /// <summary>Masa soruları (03 §4–5): her tip veriden üretilir ve doğru kontrol edilir; Lonca havuzu tekrar etmez.</summary>
    public class DeskQuestionTests
    {
        private PrototypeChemistrySeed chem;
        private readonly List<Object> created = new List<Object>();

        [SetUp]
        public void SetUp() => chem = PrototypeChemistrySeed.Build();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
            chem.DestroyAll();
        }

        private T Keep<T>(T o) where T : Object
        {
            created.Add(o);
            return o;
        }

        private static int IndexOf(PaperRow row, string option) => System.Array.IndexOf(row.Options, option);

        // ---------- boşluk doldurma ----------

        private FillBlankQ FillBlank() => Keep(FillBlankQ.Create("fb", "", "",
            new[] { "sentez", "analiz", "yanma", "oksijen", "buharlaşma" },
            new List<FillBlankQ.Sentence>
            {
                new FillBlankQ.Sentence { text = "Birleşme: {0}.", answers = new[] { "sentez" } },
                new FillBlankQ.Sentence { text = "{0} ile yanan tepkime {1}.", answers = new[] { "oksijen", "yanma" } },
            }));

        [Test]
        public void FillBlank_DogruKelimeler_Gecer()
        {
            var q = FillBlank().Build(new Random(3));
            Assert.AreEqual(3, q.Rows.Count);
            q.Select(0, IndexOf(q.Rows[0], "sentez"));
            q.Select(1, IndexOf(q.Rows[1], "oksijen"));
            q.Select(2, IndexOf(q.Rows[2], "yanma"));
            Assert.AreEqual(PaperVerdict.Correct, q.Evaluate());
        }

        [Test]
        public void FillBlank_BosSatir_EksikSayilir_YanlisSatirLekelenir()
        {
            var q = FillBlank().Build(new Random(3));
            q.Select(0, IndexOf(q.Rows[0], "sentez"));
            Assert.AreEqual(PaperVerdict.Incomplete, q.Evaluate());

            q.Select(1, IndexOf(q.Rows[1], "oksijen"));
            q.Select(2, IndexOf(q.Rows[2], "buharlaşma"));
            Assert.AreEqual(PaperVerdict.Wrong, q.Evaluate());
            Assert.IsFalse(q.Rows[1].Wrong);
            Assert.IsTrue(q.Rows[2].Wrong);

            q.Select(2, IndexOf(q.Rows[2], "yanma"));
            Assert.IsFalse(q.Rows[2].Wrong, "seçim değişince leke kalkar");
        }

        [Test]
        public void FillBlank_SatirMetni_DigerBosluguSecimleDoldurur()
        {
            var q = FillBlank().Build(null);
            q.Select(1, IndexOf(q.Rows[1], "oksijen"));
            StringAssert.Contains("oksijen ile yanan tepkime", q.RowLabel(2));
            StringAssert.Contains(FillBlankPaper.Empty, q.RowLabel(2));
        }

        // ---------- çoktan seçmeli ----------

        [Test]
        public void MultipleChoice_KarisikSiklar_DogruSikIzlenir()
        {
            var data = Keep(MultipleChoiceQ.Create("mc", "", "", new[] { "CO2 ve su", "H2 ve O2", "su", "N2" }, new[] { 0 }));
            for (int seed = 0; seed < 10; seed++)
            {
                var q = data.Build(new Random(seed));
                Assert.AreEqual(1, q.Rows.Count);
                q.Select(0, IndexOf(q.Rows[0], "su"));
                Assert.AreEqual(PaperVerdict.Wrong, q.Evaluate());
                q.Select(0, IndexOf(q.Rows[0], "CO2 ve su"));
                Assert.AreEqual(PaperVerdict.Correct, q.Evaluate());
            }
        }

        [Test]
        public void MultipleChoice_CokluKanit_HerKartAyriKontrol()
        {
            var data = Keep(MultipleChoiceQ.Create("kanit", "", "", new[] { "gaz", "renk", "şekil" }, new[] { 0, 1 }, true));
            var q = data.Build(new Random(1));
            Assert.AreEqual(3, q.Rows.Count);
            for (int i = 0; i < q.Rows.Count; i++) q.Select(i, 0); // hepsine "Kanıt"
            Assert.AreEqual(PaperVerdict.Wrong, q.Evaluate());
            for (int i = 0; i < q.Rows.Count; i++)
                Assert.AreEqual(q.Rows[i].Label == "şekil", q.Rows[i].Wrong);
        }

        // ---------- doğru / yanlış + düzeltme ----------

        [Test]
        public void TrueFalse_YSecilinceDuzeltmeAcilir_VeKontrolEdilir()
        {
            var data = Keep(TrueFalseQ.Create("tf", "", "", new List<TrueFalseQ.Statement>
            {
                new TrueFalseQ.Statement { text = "Analiz ayrıştırır.", isTrue = true },
                new TrueFalseQ.Statement
                {
                    text = "Atomlar kaybolur.", isTrue = false,
                    corrections = new[] { "Atomlar kaybolmaz.", "Yalnız yanmada kaybolur." }, correctionIndex = 0
                }
            }));
            var q = data.Build(new Random(5));
            Assert.AreEqual(3, q.Rows.Count);
            Assert.IsFalse(q.IsVisible(2), "Y seçilmeden düzeltme görünmez");

            q.Select(0, 0); // D
            q.Select(1, 1); // Y
            Assert.IsTrue(q.IsVisible(2));
            Assert.AreEqual(PaperVerdict.Incomplete, q.Evaluate());

            q.Select(2, IndexOf(q.Rows[2], "Yalnız yanmada kaybolur."));
            Assert.AreEqual(PaperVerdict.Wrong, q.Evaluate());
            q.Select(2, IndexOf(q.Rows[2], "Atomlar kaybolmaz."));
            Assert.AreEqual(PaperVerdict.Correct, q.Evaluate());
        }

        [Test]
        public void TrueFalse_DogruIfadeyeY_Yanlis()
        {
            var data = Keep(TrueFalseQ.Create("tf2", "", "", new List<TrueFalseQ.Statement>
            {
                new TrueFalseQ.Statement { text = "Yanmada O2 girer.", isTrue = true }
            }));
            var q = data.Build(null);
            q.Select(0, 1);
            Assert.AreEqual(PaperVerdict.Wrong, q.Evaluate());
            Assert.IsTrue(q.Rows[0].Wrong);
        }

        // ---------- sınıflandırma ----------

        [Test]
        public void Classify_TepkimeKartlari_TureGoreKutu()
        {
            var data = Keep(ClassifyQ.Create("cl", "", "", new[] { "Sentez", "Analiz", "Yanma" }, new List<ClassifyQ.Card>
            {
                new ClassifyQ.Card { reaction = chem.Reactions["R_water"], box = 0 },
                new ClassifyQ.Card { reaction = chem.Reactions["R_lime1"], box = 1 },
                new ClassifyQ.Card { text = "Fener yandı.", box = 2 },
            }));
            var q = data.Build(new Random(9));
            for (int i = 0; i < q.Rows.Count; i++)
            {
                string label = q.Rows[i].Label;
                int box = label.Contains("CaCO") ? 1 : label.StartsWith("Fener") ? 2 : 0;
                q.Select(i, box);
            }
            Assert.AreEqual(PaperVerdict.Correct, q.Evaluate());
        }

        // ---------- denkleştirme ----------

        private static void SetCoefficients(PaperQuestion q, params int[] values)
        {
            for (int i = 0; i < values.Length; i++) q.Select(i, values[i] - 1);
        }

        [Test]
        public void Balance_Su_DogruSadeVeSadelesmeyen()
        {
            var data = Keep(BalanceEquationQ.Create("be", "", "", chem.Reactions["R_water"]));
            var q = data.Build(null);
            Assert.AreEqual(3, q.Rows.Count);
            Assert.AreEqual(PaperVerdict.Wrong, q.Evaluate(), "hepsi 1: dengesiz");

            SetCoefficients(q, 2, 1, 2);
            Assert.AreEqual(PaperVerdict.Correct, q.Evaluate());

            SetCoefficients(q, 4, 2, 4);
            Assert.AreEqual(PaperVerdict.NotMinimal, q.Evaluate());
        }

        [Test]
        public void Balance_Fotosentez_Glikoz()
        {
            Assert.IsTrue(AtomMath.IsBalanced(chem.Reactions["R_photosynthesis"]));
            Assert.IsTrue(AtomMath.IsBalanced(chem.Reactions["R_respiration"]));
            var q = Keep(BalanceEquationQ.Create("ph", "", "", chem.Reactions["R_photosynthesis"])).Build(null);
            SetCoefficients(q, 6, 6, 1, 6);
            Assert.AreEqual(PaperVerdict.Correct, q.Evaluate());
            SetCoefficients(q, 6, 6, 1, 5);
            Assert.AreEqual(PaperVerdict.Wrong, q.Evaluate());
        }

        [Test]
        public void Step_DonguselKayar()
        {
            var q = Keep(BalanceEquationQ.Create("st", "", "", chem.Reactions["R_water"])).Build(null);
            q.Step(0, -1);
            Assert.AreEqual(BalancePaper.MaxCoefficient - 1, q.Rows[0].Selected, "1'den geri: 9");
            q.Step(0, 1);
            Assert.AreEqual(0, q.Rows[0].Selected);
        }

        // ---------- Lonca havuzu ----------

        [Test]
        public void GuildPool_TekrarYok_YerineBaskasi_BitinceFalse()
        {
            var items = new List<string> { "a", "b", "c", "d", "e", "f" };
            var pool = new GuildExamPool<string>(items, new Random(42));
            var drawn = pool.Draw(4);
            Assert.AreEqual(4, drawn.Count);
            CollectionAssert.AllItemsAreUnique(drawn);
            Assert.AreEqual(2, pool.Remaining);

            Assert.IsTrue(pool.TryReplace(out var r1));
            CollectionAssert.DoesNotContain(drawn, r1);
            Assert.IsTrue(pool.TryReplace(out var r2));
            Assert.AreNotEqual(r1, r2);
            Assert.IsFalse(pool.TryReplace(out _), "havuz bitti: aynı soru kalır");
        }
    }
}
