using System.Collections.Generic;
using BilgiAdasi.Chemistry;

namespace BilgiAdasi.Order
{
    // Atölye (sipariş) görünüm arayüzleri. Mustafa'nın view'ları bunları uygular ve
    // RegisteredView<IArayüz>'den türeyerek kendini kaydeder. Denetleyiciler yalnızca bu arayüzleri tanır.
    // Kazan, terazi ve raf 3D'dir (dünyada); envanter, sipariş kartı ve pazar ekranı HUD'dadır.

    /// <summary>Sipariş kartında (HUD) gösterilecek bilgi.</summary>
    public readonly struct OrderCardInfo
    {
        public readonly string GiverName;
        /// <summary>Bu adımın hedef ürünleri (sağ kefe).</summary>
        public readonly IReadOnlyList<SpeciesAmount> Products;
        /// <summary>Kart girenleri gösteriyorsa (Görev 1–2) adları; katsayı verilmez. Göstermiyorsa null.</summary>
        public readonly IReadOnlyList<SpeciesData> Reactants;
        public readonly int StepIndex;
        public readonly int StepCount;
        public readonly int Reward;
        /// <summary>Ürün hazır, kapıda teslim bekliyor.</summary>
        public readonly bool ReadyToDeliver;

        public OrderCardInfo(string giverName, IReadOnlyList<SpeciesAmount> products, IReadOnlyList<SpeciesData> reactants,
            int stepIndex, int stepCount, int reward, bool readyToDeliver)
        {
            GiverName = giverName;
            Products = products;
            Reactants = reactants;
            StepIndex = stepIndex;
            StepCount = stepCount;
            Reward = reward;
            ReadyToDeliver = readyToDeliver;
        }
    }

    /// <summary>HUD'daki sipariş kartı. Aktif sipariş boyunca görünür.</summary>
    public interface IOrderCardView
    {
        void ShowOrder(OrderCardInfo info);
        /// <summary>Aktif sipariş yok: kart gizlenir.</summary>
        void ClearOrder();
    }

    /// <summary>Ekranın altındaki 6 slotluk envanter + kese.</summary>
    public interface IInventoryView
    {
        void ShowInventory(IReadOnlyList<InventorySlot> slots, int selected, int money);
    }

    /// <summary>3D kazan görselleri: sıvı, döküm, köpük, ateş.</summary>
    public interface ICauldronView
    {
        /// <summary>Kazandaki maddeler (eklenme sırasıyla). Boşsa sıvı görünmez.</summary>
        void ShowContents(IReadOnlyList<SpeciesAmount> contents);
        /// <summary>Bir kavanoz döküldü (sıçrama).</summary>
        void PlayPour(SpeciesData species);
        /// <summary>Kazan boşaltıldı (kol).</summary>
        void PlayDrain();
        void SetStoveLit(bool lit);
        void PlayFoam(float duration);
        void PlayReaction(float duration);
    }

    /// <summary>3D terazi: sol kefe kazandaki atomlar, sağ kefe siparişin ürünleri. Satır başına: sol / sağ / artan.</summary>
    public interface IScaleView
    {
        void ShowAtoms(IReadOnlyList<AtomRow> rows, bool balanced);
        /// <summary>Köpürme: Left &gt; Right olan elementler fazla, Left &lt; Right eksik.</summary>
        void ShowFoamDifference(IReadOnlyList<AtomRow> rows);
        void ClearFoamDifference();
    }

    /// <summary>
    /// Atom Görüşü (04-gorev-plani §2.4): tepkime anında kazanın üstünde girenlerin atomları ayrılır ve ürünler olarak
    /// yeniden dizilir; altta element sayacı. Purchase'ta eşsiz kalan moleküller "artan" olarak yerinde kalır.
    /// Listeler çağrıdan sonra değişir: view kopyalar.
    /// </summary>
    public interface IAtomVisionView
    {
        void Play(IReadOnlyList<SpeciesAmount> reactants, IReadOnlyList<SpeciesAmount> leftovers, IReadOnlyList<SpeciesAmount> products);
    }

    /// <summary>Pazarda satılan bir kalem.</summary>
    public readonly struct MarketItem
    {
        public readonly SpeciesData Species;
        public readonly int Price;
        /// <summary>Envanterde bu maddeden kaç tane var.</summary>
        public readonly int Owned;

        public MarketItem(SpeciesData species, int price, int owned)
        {
            Species = species;
            Price = price;
            Owned = owned;
        }
    }

    /// <summary>Pazar tezgâhı ekranı (UI modu). Düğmeler IMarketCommands'ı çağırır.</summary>
    public interface IMarketView
    {
        void Open(IReadOnlyList<MarketItem> items, int money);
        void Close();
    }

    public interface IMarketCommands
    {
        void Buy(SpeciesData species);
        void CloseMarket();
    }
}
