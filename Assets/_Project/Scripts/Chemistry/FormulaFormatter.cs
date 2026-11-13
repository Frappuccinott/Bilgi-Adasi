using System.Collections.Generic;
using System.Text;

namespace BilgiAdasi.Chemistry
{
    /// <summary>Formül ve denklem metinlerini TextMeshPro için alt indisli üretir. Saf C#.</summary>
    public static class FormulaFormatter
    {
        public const string Arrow = " → ";
        public const string Plus = " + ";

        private static readonly StringBuilder Builder = new StringBuilder(128);

        /// <summary>"Ca(OH)2" → "Ca(OH)&lt;sub&gt;2&lt;/sub&gt;". Harf ya da ')' sonrasındaki rakamlar alt indis olur.</summary>
        public static string ToRich(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return string.Empty;
            var sb = new StringBuilder(plain.Length + 16);
            bool inSub = false;
            for (int i = 0; i < plain.Length; i++)
            {
                char ch = plain[i];
                bool isDigit = ch >= '0' && ch <= '9';
                if (isDigit && !inSub && i > 0 && (char.IsLetter(plain[i - 1]) || plain[i - 1] == ')'))
                {
                    sb.Append("<sub>");
                    inSub = true;
                }
                else if (!isDigit && inSub)
                {
                    sb.Append("</sub>");
                    inSub = false;
                }
                sb.Append(ch);
            }
            if (inSub) sb.Append("</sub>");
            return sb.ToString();
        }

        /// <summary>Ders kitabı biçiminde denklem: "CH<sub>4</sub> + 2O<sub>2</sub> → CO<sub>2</sub> + 2H<sub>2</sub>O". Katsayı 1 yazılmaz.</summary>
        public static string Equation(ReactionData reaction) => Equation(reaction.Reactants, reaction.Products);

        public static string Equation(IReadOnlyList<SpeciesAmount> reactants, IReadOnlyList<SpeciesAmount> products)
        {
            Builder.Clear();
            AppendSide(reactants);
            Builder.Append(Arrow);
            AppendSide(products);
            return Builder.ToString();
        }

        /// <summary>
        /// Katsayısız iskelet denklem (denkleştirilecek hali): "H<sub>2</sub> + O<sub>2</sub> → H<sub>2</sub>O".
        /// showReactants false ise sol tarafın yerine unknownSide yazılır (sipariş kartı girenleri saklıyorsa).
        /// </summary>
        public static string Skeleton(ReactionData reaction, bool showReactants, string unknownSide)
        {
            Builder.Clear();
            if (showReactants) AppendSide(reaction.Reactants, false);
            else Builder.Append(unknownSide);
            Builder.Append(Arrow);
            AppendSide(reaction.Products, false);
            return Builder.ToString();
        }

        private static void AppendSide(IReadOnlyList<SpeciesAmount> side, bool coefficients = true)
        {
            bool first = true;
            for (int i = 0; i < side.Count; i++)
            {
                var term = side[i];
                if (term.Species == null || term.Amount <= 0) continue;
                if (!first) Builder.Append(Plus);
                if (coefficients && term.Amount != 1) Builder.Append(term.Amount);
                Builder.Append(term.Species.FormulaRich);
                first = false;
            }
        }
    }
}
