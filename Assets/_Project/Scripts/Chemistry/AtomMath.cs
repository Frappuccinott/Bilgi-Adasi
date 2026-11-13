using System;
using System.Collections.Generic;

namespace BilgiAdasi.Chemistry
{
    /// <summary>
    /// Atom sayımı ve denge hesabı. Saf C#: UnityEngine API'si kullanılmaz.
    /// Sonuç sözlükleri çağıran tarafından verilir (tekrar kullanılır, gereksiz ayırma yok).
    /// </summary>
    public static class AtomMath
    {
        /// <summary>result'ı temizler ve element → toplam atom sayısıyla doldurur.</summary>
        public static void Count(IReadOnlyList<SpeciesAmount> items, Dictionary<ElementData, int> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            result.Clear();
            AddTo(items, 1, result);
        }

        /// <summary>items × multiplier atomlarını result'a ekler (temizlemez).</summary>
        public static void AddTo(IReadOnlyList<SpeciesAmount> items, int multiplier, Dictionary<ElementData, int> result)
        {
            if (items == null) return;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.Species == null || item.Amount == 0) continue;
                var composition = item.Species.Composition;
                for (int c = 0; c < composition.Count; c++)
                {
                    var ec = composition[c];
                    if (ec.Element == null) continue;
                    result.TryGetValue(ec.Element, out int current);
                    result[ec.Element] = current + ec.Count * item.Amount * multiplier;
                }
            }
        }

        /// <summary>İki sayım her elementte eşit mi? (Eksik anahtar = 0)</summary>
        public static bool Equal(IReadOnlyDictionary<ElementData, int> a, IReadOnlyDictionary<ElementData, int> b)
        {
            foreach (var pair in a)
            {
                b.TryGetValue(pair.Key, out int other);
                if (pair.Value != other) return false;
            }
            foreach (var pair in b)
            {
                a.TryGetValue(pair.Key, out int other);
                if (pair.Value != other) return false;
            }
            return true;
        }

        /// <summary>result[e] = left[e] − right[e]; sıfır olanlar yazılmaz. Köpürmede fazla (+) / eksik (−) atomlar için.</summary>
        public static void Difference(IReadOnlyDictionary<ElementData, int> left, IReadOnlyDictionary<ElementData, int> right,
            Dictionary<ElementData, int> result)
        {
            result.Clear();
            foreach (var pair in left)
            {
                right.TryGetValue(pair.Key, out int r);
                int d = pair.Value - r;
                if (d != 0) result[pair.Key] = d;
            }
            foreach (var pair in right)
            {
                if (left.ContainsKey(pair.Key)) continue;
                if (pair.Value != 0) result[pair.Key] = -pair.Value;
            }
        }

        /// <summary>Tepkimenin girenleri ile ürünleri atomca eşit mi?</summary>
        public static bool IsBalanced(ReactionData reaction)
        {
            var left = new Dictionary<ElementData, int>();
            var right = new Dictionary<ElementData, int>();
            Count(reaction.Reactants, left);
            Count(reaction.Products, right);
            return Equal(left, right);
        }

        public static int Gcd(int a, int b)
        {
            while (b != 0) (a, b) = (b, a % b);
            return Math.Abs(a);
        }

        /// <summary>Katsayılar en küçük tam sayılar mı? (4H₂ + 2O₂ → 4H₂O → false)</summary>
        public static bool IsMinimal(IReadOnlyList<int> coefficients)
        {
            int g = 0;
            for (int i = 0; i < coefficients.Count; i++) g = Gcd(g, coefficients[i]);
            return g == 1;
        }

        /// <summary>
        /// Purchase modu: hedef ürün adedi için her girenden gereken miktar.
        /// ölçek = hedef / ürün katsayısı. Ölçek tam sayı değilse false döner.
        /// </summary>
        public static bool RequiredForPurchase(ReactionData reaction, SpeciesData product, int targetAmount,
            Dictionary<SpeciesData, int> required)
        {
            required.Clear();
            int productCoef = 0;
            for (int i = 0; i < reaction.Products.Count; i++)
                if (reaction.Products[i].Species == product) productCoef = reaction.Products[i].Amount;

            if (productCoef <= 0 || targetAmount % productCoef != 0) return false;

            int scale = targetAmount / productCoef;
            for (int i = 0; i < reaction.Reactants.Count; i++)
            {
                var r = reaction.Reactants[i];
                required[r.Species] = r.Amount * scale;
            }
            return true;
        }
    }
}
