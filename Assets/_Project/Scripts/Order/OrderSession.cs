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

    public enum AddResult
    {
        Added,
        NotOnShelf,
        NotEnoughMoney
    }

    /// <summary>
    /// Bir siparişin saf mantığı (02-siparis-mekanigi.md §3). MonoBehaviour değildir; EditMode testleri vardır.
    /// Kazan içeriği, raf, para, terazi satırları ve denge durumu burada tutulur.
    /// </summary>
    public sealed class OrderSession
    {
        private readonly OrderTask task;

        private readonly List<SpeciesData> shelf = new List<SpeciesData>();
        private readonly List<SpeciesData> cauldronOrder = new List<SpeciesData>();
        private readonly Dictionary<SpeciesData, int> cauldron = new Dictionary<SpeciesData, int>();
        private readonly Dictionary<SpeciesData, int> owned = new Dictionary<SpeciesData, int>();
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
            Money = task.Budget;
            for (int i = 0; i < task.Shelf.Count; i++) AddToShelf(task.Shelf[i]);
            if (IsPurchase)
                for (int i = 0; i < task.Prices.Count; i++) AddToShelf(task.Prices[i].Species);
            StartStep(0);
        }

        public OrderTask Task => task;
        public int StepIndex { get; private set; }
        public OrderStep Step => task.Steps[StepIndex];
        public bool HasNextStep => StepIndex + 1 < task.Steps.Count;
        public bool IsPurchase => task.Mode == OrderMode.Purchase;
        public int Money { get; private set; }
        public int FailCount { get; private set; }
        /// <summary>Sakin eksiği bir kez bedava verdi mi (Purchase).</summary>
        public bool ShortageForgiven { get; private set; }

        public IReadOnlyList<SpeciesData> ShelfItems => shelf;
        /// <summary>Kazandaki türler, eklenme sırasıyla.</summary>
        public IReadOnlyList<SpeciesAmount> Contents => contents;
        public IReadOnlyList<AtomRow> Rows => rows;
        /// <summary>Exact: atomlar eşit. Purchase: her girenden en az gereken kadar var.</summary>
        public bool IsSatisfied { get; private set; }

        public int CountInCauldron(SpeciesData s) => cauldron.TryGetValue(s, out int n) ? n : 0;
        public int OwnedCount(SpeciesData s) => owned.TryGetValue(s, out int n) ? n : 0;

        public int PriceOf(SpeciesData s)
        {
            for (int i = 0; i < task.Prices.Count; i++)
                if (task.Prices[i].Species == s) return task.Prices[i].Price;
            return 0;
        }

        /// <summary>Raftan kazana bir kavanoz. Purchase'ta elde yoksa satın alınır.</summary>
        public AddResult Add(SpeciesData s)
        {
            if (s == null || !shelf.Contains(s)) return AddResult.NotOnShelf;

            if (IsPurchase && OwnedCount(s) <= CountInCauldron(s))
            {
                int price = PriceOf(s);
                if (Money < price) return AddResult.NotEnoughMoney;
                Money -= price;
                owned[s] = OwnedCount(s) + 1;
            }

            if (!cauldron.ContainsKey(s)) cauldronOrder.Add(s);
            cauldron[s] = CountInCauldron(s) + 1;
            Recalculate();
            return AddResult.Added;
        }

        /// <summary>Kazandan bir kavanoz çıkar; 0 olursa rafa döner. Purchase'ta akçe geri gelmez (kavanoz elde kalır).</summary>
        public bool Remove(SpeciesData s)
        {
            int n = CountInCauldron(s);
            if (n <= 0) return false;
            if (n == 1)
            {
                cauldron.Remove(s);
                cauldronOrder.Remove(s);
            }
            else
            {
                cauldron[s] = n - 1;
            }
            Recalculate();
            return true;
        }

        /// <summary>Dengesizken ateş yakıldı (köpürme). Hata sayısını döndürür.</summary>
        public int RegisterFail() => ++FailCount;

        /// <summary>Purchase: eksikleri tamamlamanın akçe bedeli.</summary>
        public int ShortageCost()
        {
            int cost = 0;
            foreach (var pair in required)
            {
                int missing = pair.Value - CountInCauldron(pair.Key);
                if (missing > 0) cost += missing * PriceOf(pair.Key);
            }
            return cost;
        }

        /// <summary>
        /// Purchase: para eksiği karşılamıyorsa sakin bir kez eksiği bedava verir (oyun kilitlenmez).
        /// Verdiyse true.
        /// </summary>
        public bool TryForgiveShortage()
        {
            if (!IsPurchase || IsSatisfied || ShortageForgiven || Money >= ShortageCost()) return false;
            foreach (var pair in required)
            {
                int missing = pair.Value - CountInCauldron(pair.Key);
                if (missing <= 0) continue;
                if (!cauldron.ContainsKey(pair.Key)) cauldronOrder.Add(pair.Key);
                cauldron[pair.Key] = CountInCauldron(pair.Key) + missing;
                owned[pair.Key] = OwnedCount(pair.Key) + missing;
            }
            ShortageForgiven = true;
            Recalculate();
            return true;
        }

        /// <summary>Zincir görev: adım ürünleri rafa düşer, kazan boşalır, sonraki adım başlar.</summary>
        public void AdvanceStep()
        {
            if (!HasNextStep) return;
            var products = Step.TargetProducts;
            for (int i = 0; i < products.Count; i++) AddToShelf(products[i].Species);
            StartStep(StepIndex + 1);
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

            BuildElementOrder();
            Recalculate();
        }

        private void AddToShelf(SpeciesData s)
        {
            if (s != null && !shelf.Contains(s)) shelf.Add(s);
        }

        /// <summary>Terazi satır sırası: önce hedef ürünlerdeki elementler, sonra raftakiler.</summary>
        private void BuildElementOrder()
        {
            elementOrder.Clear();
            var targets = Step.TargetProducts;
            for (int i = 0; i < targets.Count; i++) AddElements(targets[i].Species);
            for (int i = 0; i < shelf.Count; i++) AddElements(shelf[i]);
        }

        private void AddElements(SpeciesData s)
        {
            if (s == null) return;
            var comp = s.Composition;
            for (int i = 0; i < comp.Count; i++)
                if (comp[i].Element != null && !elementOrder.Contains(comp[i].Element))
                    elementOrder.Add(comp[i].Element);
        }

        private void Recalculate()
        {
            contents.Clear();
            used.Clear();
            extra.Clear();
            for (int i = 0; i < cauldronOrder.Count; i++)
            {
                var s = cauldronOrder[i];
                int n = cauldron[s];
                contents.Add(new SpeciesAmount(s, n));

                if (IsPurchase && required.TryGetValue(s, out int need))
                {
                    int u = n < need ? n : need;
                    if (u > 0) used.Add(new SpeciesAmount(s, u));
                    if (n > u) extra.Add(new SpeciesAmount(s, n - u));
                }
                else if (IsPurchase)
                {
                    extra.Add(new SpeciesAmount(s, n)); // tepkimede yeri yok → hepsi artan
                }
            }

            AtomMath.Count(IsPurchase ? used : contents, left);
            AtomMath.Count(Step.TargetProducts, right);
            AtomMath.Count(extra, leftover);

            rows.Clear();
            for (int i = 0; i < elementOrder.Count; i++)
            {
                var e = elementOrder[i];
                left.TryGetValue(e, out int l);
                right.TryGetValue(e, out int r);
                leftover.TryGetValue(e, out int o);
                rows.Add(new AtomRow(e, l, r, o));
            }

            IsSatisfied = contents.Count > 0 && AtomMath.Equal(left, right);
        }
    }
}
