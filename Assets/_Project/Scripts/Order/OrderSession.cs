using System.Collections.Generic;
using BilgiAdasi.Chemistry;

namespace BilgiAdasi.Order
{
    /// <summary>Terazideki bir element satırı: sol kefe / sağ kefe / artan (yalnızca Purchase).</summary>
    public readonly struct AtomRow
    {
        public readonly ElementData Element;
        public readonly int Left;
        public readonly int Right;
        public readonly int Leftover;

        public AtomRow(ElementData element, int left, int right, int leftover)
        {
            Element = element;
            Left = left;
            Right = right;
            Leftover = leftover;
        }

        public bool IsEqual => Left == Right;
    }

    /// <summary>
    /// Bir siparişin saf mantığı (02-siparis-mekanigi.md §3). MonoBehaviour değildir; EditMode testleri vardır.
    /// Kazan içeriği, terazi satırları ve denge durumu burada tutulur. Malzeme envanterden gelir (pazar/raf);
    /// kazana giren geri alınamaz: yalnızca ateş (tepkime ya da köpürme) ya da boşaltma ile kazan boşalır.
    /// Tepkimenin girenleri dışında bir madde varsa denge sayılmaz (ürünü dökerek "dengeleme" hilesi kapanır).
    /// </summary>
    public sealed class OrderSession
    {
        private readonly OrderTask task;

        private readonly List<SpeciesData> cauldronOrder = new List<SpeciesData>();
        private readonly Dictionary<SpeciesData, int> cauldron = new Dictionary<SpeciesData, int>();
        private readonly Dictionary<SpeciesData, int> required = new Dictionary<SpeciesData, int>();

        private readonly List<SpeciesAmount> contents = new List<SpeciesAmount>();
        private readonly List<SpeciesAmount> used = new List<SpeciesAmount>();
        private readonly List<SpeciesAmount> extra = new List<SpeciesAmount>();
        private readonly Dictionary<ElementData, int> left = new Dictionary<ElementData, int>();
        private readonly Dictionary<ElementData, int> right = new Dictionary<ElementData, int>();
        private readonly Dictionary<ElementData, int> leftover = new Dictionary<ElementData, int>();
        private readonly List<ElementData> elementOrder = new List<ElementData>();
        private readonly List<AtomRow> rows = new List<AtomRow>();

        public OrderSession(OrderTask task)
        {
            this.task = task;
            StartStep(0);
        }

        public OrderTask Task => task;
        public int StepIndex { get; private set; }
        public OrderStep Step => task.Steps[StepIndex];
        public bool HasNextStep => StepIndex + 1 < task.Steps.Count;
        public bool IsPurchase => task.Mode == OrderMode.Purchase;
        public int FailCount { get; private set; }

        /// <summary>Kazandaki türler, eklenme sırasıyla.</summary>
        public IReadOnlyList<SpeciesAmount> Contents => contents;
        public IReadOnlyList<AtomRow> Rows => rows;
        /// <summary>Tepkimeye girecek kısım (Exact: kazanın tamamı; Purchase: gereken kadarı).</summary>
        public IReadOnlyList<SpeciesAmount> Used => IsPurchase ? used : contents;
        /// <summary>Purchase: tepkimeden artan maddeler (Atom Görüşü'nde "artan").</summary>
        public IReadOnlyList<SpeciesAmount> Extra => extra;
        public bool IsEmpty => contents.Count == 0;
        /// <summary>Kazanda bu adımın tepkimesine girmeyen bir madde var.</summary>
        public bool HasForeign { get; private set; }
        /// <summary>Exact: atomlar eşit. Purchase: her girenden en az gereken kadar. İkisinde de yabancı madde yok.</summary>
        public bool IsSatisfied { get; private set; }

        public int CountInCauldron(SpeciesData s) => s != null && cauldron.TryGetValue(s, out int n) ? n : 0;

        public bool IsReactant(SpeciesData s)
        {
            var reactants = Step.Reaction.Reactants;
            for (int i = 0; i < reactants.Count; i++)
                if (reactants[i].Species == s) return true;
            return false;
        }

        /// <summary>Kazana bir kavanoz dökülür (geri alınamaz).</summary>
        public void Pour(SpeciesData s)
        {
            if (s == null) return;
            if (!cauldron.ContainsKey(s)) cauldronOrder.Add(s);
            cauldron[s] = CountInCauldron(s) + 1;
            Recalculate();
        }

        /// <summary>Kazan boşalır (boşaltma kolu, köpürme ya da tepkime sonrası). Malzeme gider.</summary>
        public void Clear()
        {
            cauldron.Clear();
            cauldronOrder.Clear();
            Recalculate();
        }

        /// <summary>Dengesizken ateş yakıldı (köpürme). Hata sayısını döndürür.</summary>
        public int RegisterFail() => ++FailCount;

        /// <summary>Zincir görev: sonraki adım başlar, kazan boşalır.</summary>
        public void AdvanceStep()
        {
            if (HasNextStep) StartStep(StepIndex + 1);
        }

        /// <summary>Zincir: ara ürün elden gitti, sipariş ilk adımdan yeniden başlar (hata sayısı korunur).</summary>
        public void RestartFromFirstStep() => StartStep(0);

        /// <summary>
        /// Bu adımın girenlerinden önceki adımların ürünü olanlar (ara ürün; ör. CaO) ve tepkimedeki miktarı.
        /// İlk adımda boş.
        /// </summary>
        public void GetIntermediates(List<SpeciesAmount> into)
        {
            into.Clear();
            if (StepIndex == 0) return;
            var reactants = Step.Reaction.Reactants;
            for (int i = 0; i < reactants.Count; i++)
            {
                var s = reactants[i].Species;
                bool made = false;
                for (int p = 0; p < StepIndex && !made; p++)
                {
                    var products = Task.Steps[p].TargetProducts;
                    for (int k = 0; k < products.Count; k++)
                        if (products[k].Species == s) { made = true; break; }
                }
                if (made) into.Add(reactants[i]);
            }
        }

        private void StartStep(int index)
        {
            StepIndex = index;
            cauldron.Clear();
            cauldronOrder.Clear();

            required.Clear();
            if (IsPurchase && Step.TargetProducts.Count > 0)
            {
                var target = Step.TargetProducts[0];
                AtomMath.RequiredForPurchase(Step.Reaction, target.Species, target.Amount, required);
            }
            Recalculate();
        }

        private void Recalculate()
        {
            contents.Clear();
            used.Clear();
            extra.Clear();
            HasForeign = false;
            bool enough = true;

            for (int i = 0; i < cauldronOrder.Count; i++)
            {
                var s = cauldronOrder[i];
                int n = cauldron[s];
                contents.Add(new SpeciesAmount(s, n));
                if (!IsReactant(s)) HasForeign = true;

                if (!IsPurchase) continue;
                if (required.TryGetValue(s, out int need))
                {
                    int u = n < need ? n : need;
                    if (u > 0) used.Add(new SpeciesAmount(s, u));
                    if (n > u) extra.Add(new SpeciesAmount(s, n - u));
                }
                else
                {
                    extra.Add(new SpeciesAmount(s, n)); // tepkimede yeri yok → hepsi artan
                }
            }

            if (IsPurchase)
                foreach (var pair in required)
                    if (CountInCauldron(pair.Key) < pair.Value) enough = false;

            AtomMath.Count(IsPurchase ? used : contents, left);
            AtomMath.Count(Step.TargetProducts, right);
            AtomMath.Count(extra, leftover);

            // Terazi satır sırası: önce hedef ürünlerdeki elementler, sonra kazandakiler.
            elementOrder.Clear();
            var targets = Step.TargetProducts;
            for (int i = 0; i < targets.Count; i++) AddElements(targets[i].Species);
            for (int i = 0; i < contents.Count; i++) AddElements(contents[i].Species);

            rows.Clear();
            for (int i = 0; i < elementOrder.Count; i++)
            {
                var e = elementOrder[i];
                left.TryGetValue(e, out int l);
                right.TryGetValue(e, out int r);
                leftover.TryGetValue(e, out int o);
                rows.Add(new AtomRow(e, l, r, o));
            }

            IsSatisfied = contents.Count > 0 && !HasForeign && (IsPurchase ? enough : AtomMath.Equal(left, right));
        }

        private void AddElements(SpeciesData s)
        {
            if (s == null) return;
            var comp = s.Composition;
            for (int i = 0; i < comp.Count; i++)
                if (comp[i].Element != null && !elementOrder.Contains(comp[i].Element))
                    elementOrder.Add(comp[i].Element);
        }
    }
}
