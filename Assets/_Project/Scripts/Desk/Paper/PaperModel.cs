using System;
using System.Collections.Generic;
using System.Text;
using BilgiAdasi.Chemistry;

namespace BilgiAdasi.Desk
{
    /// <summary>Satırdaki seçeneklerin kâğıtta nasıl durduğu (görünüm ipucu; kontrol mantığını etkilemez).</summary>
    public enum RowStyle
    {
        /// <summary>Kısa seçenekler yan yana (D / Y, kutu adları, Kanıt / Değil).</summary>
        Chips,
        /// <summary>Tek değer, ‹ › ile değişir (kelime bankası, katsayı).</summary>
        Cycle,
        /// <summary>Uzun seçenekler alt alta (çoktan seçmeli şıklar, düzeltme cümleleri).</summary>
        List
    }

    /// <summary>Kâğıttaki bir cevap satırı: oyuncu seçeneklerden birini seçer. Serbest metin yok (03 §1).</summary>
    public sealed class PaperRow
    {
        public string Label;
        public string[] Options;
        public int Correct;
        public int Selected = -1;
        public RowStyle Style;
        /// <summary>Koşullu satır (Y'den sonra düzeltme): ParentRow'da ParentOption seçiliyse görünür.</summary>
        public int ParentRow = -1;
        public int ParentOption = -1;
        /// <summary>Son kontrolde yanlış bulundu (mürekkep lekesi). Seçim değişince kalkar.</summary>
        public bool Wrong;

        public PaperRow(string label, string[] options, int correct, RowStyle style)
        {
            Label = label;
            Options = options ?? Array.Empty<string>();
            Correct = correct;
            Style = style;
        }
    }

    public enum PaperVerdict
    {
        Correct,
        /// <summary>Bazı satırlar boş: hata sayılmaz.</summary>
        Incomplete,
        /// <summary>Yanlış: leke, hata sayılır.</summary>
        Wrong,
        /// <summary>Denklem atomca doğru ama katsayılar sadeleşebiliyor: hata sayılmaz, tekrar denenir (03 §4).</summary>
        NotMinimal
    }

    /// <summary>
    /// Kâğıda açılmış bir soru (çalışma anı modeli). Saf C#: UnityEngine API'si kullanılmaz, EditMode testli.
    /// QuestionData.Build üretir; denetleyici seçimleri değiştirir, Evaluate kontrol eder; view yalnızca okur.
    /// </summary>
    public class PaperQuestion
    {
        public readonly string Id;
        public readonly string Prompt;
        public readonly string Hint;
        public readonly List<PaperRow> Rows = new List<PaperRow>();

        public PaperQuestion(string id, string prompt, string hint)
        {
            Id = id;
            Prompt = prompt;
            Hint = hint;
        }

        /// <summary>Kâğıttaki soru metni (denklemde katsayılar seçime göre değişir).</summary>
        public virtual string PromptText => Prompt;

        /// <summary>Satırın kâğıttaki metni (boşluk doldurmada cümle, seçilen kelimeyle).</summary>
        public virtual string RowLabel(int row) => Rows[row].Label;

        public bool IsVisible(int row)
        {
            var r = Rows[row];
            return r.ParentRow < 0 || Rows[r.ParentRow].Selected == r.ParentOption;
        }

        public bool IsComplete
        {
            get
            {
                for (int i = 0; i < Rows.Count; i++)
                    if (IsVisible(i) && Rows[i].Selected < 0) return false;
                return true;
            }
        }

        public void Select(int row, int option)
        {
            if (row < 0 || row >= Rows.Count) return;
            var r = Rows[row];
            if (option < -1 || option >= r.Options.Length) return;
            r.Selected = option;
            r.Wrong = false;
        }

        /// <summary>Seçeneği bir ileri/geri kaydırır (RB/LB, tekerlek). Boşsa ilk/son seçenek.</summary>
        public void Step(int row, int delta)
        {
            if (row < 0 || row >= Rows.Count || delta == 0) return;
            var r = Rows[row];
            int n = r.Options.Length;
            if (n == 0) return;
            int next = r.Selected < 0 ? (delta > 0 ? 0 : n - 1) : ((r.Selected + delta) % n + n) % n;
            Select(row, next);
        }

        /// <summary>Görünür satırları kontrol eder, yanlışları işaretler.</summary>
        public virtual PaperVerdict Evaluate()
        {
            if (!IsComplete) return PaperVerdict.Incomplete;
            bool ok = true;
            for (int i = 0; i < Rows.Count; i++)
            {
                var r = Rows[i];
                r.Wrong = IsVisible(i) && r.Selected != r.Correct;
                if (r.Wrong) ok = false;
            }
            return ok ? PaperVerdict.Correct : PaperVerdict.Wrong;
        }

        /// <summary>Fisher–Yates; dizinin kopyası değil kendisi karışır.</summary>
        public static void Shuffle<T>(IList<T> list, Random rng)
        {
            if (rng == null) return;
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    /// <summary>
    /// Boşluk doldurma: cümlelerde {0}, {1} … boşluklar; her boşluk bir satır, seçenekler kelime bankası (03 §4).
    /// Satır metni cümlenin tamamıdır: diğer boşluklar seçilen kelimeyle, bu satırın boşluğu altı çizili görünür.
    /// </summary>
    public sealed class FillBlankPaper : PaperQuestion
    {
        public const string Empty = "______";

        private readonly List<string> sentences = new List<string>();
        // satır → (cümle, cümledeki boşluk no)
        private readonly List<int> rowSentence = new List<int>();
        private readonly List<int> rowBlank = new List<int>();
        private readonly StringBuilder sb = new StringBuilder(256);

        public FillBlankPaper(string id, string prompt, string hint) : base(id, prompt, hint) { }

        /// <summary>Cümle ekler; answers[k] = {k} boşluğunun bankadaki doğru kelime sırası.</summary>
        public void AddSentence(string sentence, int[] answers, string[] bank)
        {
            int s = sentences.Count;
            sentences.Add(sentence ?? string.Empty);
            for (int k = 0; k < answers.Length; k++)
            {
                Rows.Add(new PaperRow(sentence, bank, answers[k], RowStyle.Cycle));
                rowSentence.Add(s);
                rowBlank.Add(k);
            }
        }

        public override string RowLabel(int row)
        {
            int s = rowSentence[row];
            string text = sentences[s];
            sb.Clear();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                int close = c == '{' ? text.IndexOf('}', i + 1) : -1;
                if (close < 0 || !int.TryParse(text.Substring(i + 1, close - i - 1), out int blank))
                {
                    sb.Append(c);
                    continue;
                }
                int r = RowOf(s, blank);
                string word = r >= 0 && Rows[r].Selected >= 0 ? Rows[r].Options[Rows[r].Selected] : Empty;
                if (r == row) sb.Append("<u><b>").Append(word).Append("</b></u>");
                else sb.Append(word);
                i = close;
            }
            return sb.ToString();
        }

        private int RowOf(int sentence, int blank)
        {
            for (int i = 0; i < Rows.Count; i++)
                if (rowSentence[i] == sentence && rowBlank[i] == blank) return i;
            return -1;
        }
    }

    /// <summary>
    /// Denkleştirme (03 §4): her terimin katsayısı bir satır (1–9). Kontrol atom eşitliği + en küçük tam sayılar;
    /// atomca doğru ama sadeleşebiliyorsa NotMinimal (hata sayılmaz).
    /// </summary>
    public sealed class BalancePaper : PaperQuestion
    {
        public const int MaxCoefficient = 9;

        private static string[] coefficientOptions;

        private readonly List<SpeciesData> species = new List<SpeciesData>();
        private readonly int reactantCount;
        private readonly List<SpeciesAmount> left = new List<SpeciesAmount>();
        private readonly List<SpeciesAmount> right = new List<SpeciesAmount>();
        private readonly Dictionary<ElementData, int> leftAtoms = new Dictionary<ElementData, int>();
        private readonly Dictionary<ElementData, int> rightAtoms = new Dictionary<ElementData, int>();
        private readonly List<int> coefficients = new List<int>();

        public BalancePaper(string id, string prompt, string hint, ReactionData reaction) : base(id, prompt, hint)
        {
            if (coefficientOptions == null)
            {
                coefficientOptions = new string[MaxCoefficient];
                for (int i = 0; i < MaxCoefficient; i++) coefficientOptions[i] = (i + 1).ToString();
            }
            reactantCount = reaction.Reactants.Count;
            for (int i = 0; i < reaction.Reactants.Count; i++) AddTerm(reaction.Reactants[i]);
            for (int i = 0; i < reaction.Products.Count; i++) AddTerm(reaction.Products[i]);
        }

        private void AddTerm(SpeciesAmount term)
        {
            species.Add(term.Species);
            int correct = Math.Max(0, Math.Min(MaxCoefficient, term.Amount) - 1);
            // Başlangıç: hepsi 1 (Selected 0); doğru cevap ayrıca Evaluate'te atomdan hesaplanır.
            Rows.Add(new PaperRow(term.Species != null ? term.Species.FormulaRich : "?", coefficientOptions, correct, RowStyle.Cycle) { Selected = 0 });
        }

        public int Coefficient(int row) => Rows[row].Selected + 1;

        public override string PromptText
        {
            get
            {
                left.Clear();
                right.Clear();
                for (int i = 0; i < Rows.Count; i++)
                    (i < reactantCount ? left : right).Add(new SpeciesAmount(species[i], Coefficient(i)));
                return Prompt + "\n<size=120%>" + EquationWithAllCoefficients() + "</size>";
            }
        }

        // Kâğıtta katsayı 1 de yazılır: oyuncu kutuyu görsün.
        private string EquationWithAllCoefficients()
        {
            var b = new StringBuilder(128);
            for (int i = 0; i < Rows.Count; i++)
            {
                if (i == reactantCount) b.Append(FormulaFormatter.Arrow);
                else if (i > 0) b.Append(FormulaFormatter.Plus);
                b.Append("<b>").Append(Coefficient(i)).Append("</b>");
                b.Append(species[i] != null ? species[i].FormulaRich : "?");
            }
            return b.ToString();
        }

        public override PaperVerdict Evaluate()
        {
            left.Clear();
            right.Clear();
            coefficients.Clear();
            for (int i = 0; i < Rows.Count; i++)
            {
                Rows[i].Wrong = false;
                coefficients.Add(Coefficient(i));
                (i < reactantCount ? left : right).Add(new SpeciesAmount(species[i], Coefficient(i)));
            }
            AtomMath.Count(left, leftAtoms);
            AtomMath.Count(right, rightAtoms);
            if (!AtomMath.Equal(leftAtoms, rightAtoms)) return PaperVerdict.Wrong;
            return AtomMath.IsMinimal(coefficients) ? PaperVerdict.Correct : PaperVerdict.NotMinimal;
        }
    }
}
