using System.Collections.Generic;
using BilgiAdasi.Chemistry;

namespace BilgiAdasi.Order
{
    // Sipariş istasyonu görünüm arayüzleri. Mustafa'nın view'ları bunları uygular ve
    // RegisteredView<IArayüz>'den türeyerek kendini kaydeder. OrderController yalnızca bu arayüzleri tanır.
    // Görünümden gelen tıklamalar IOrderCommands üzerinden denetleyiciye döner.

    /// <summary>Raftaki bir kavanoz.</summary>
    public readonly struct ShelfItem
    {
        public readonly SpeciesData Species;
        /// <summary>Şu an kazanda kaç tane.</summary>
        public readonly int InCauldron;
        /// <summary>Purchase: elde (satın alınmış) kaç tane. Exact'te sınırsız → -1.</summary>
        public readonly int Owned;
        /// <summary>Purchase: birim fiyat. Exact'te 0.</summary>
        public readonly int Price;

        public ShelfItem(SpeciesData species, int inCauldron, int owned, int price)
        {
            Species = species;
            InCauldron = inCauldron;
            Owned = owned;
            Price = price;
        }
    }

    /// <summary>Sipariş kartında gösterilecek bilgi.</summary>
    public readonly struct OrderCardInfo
    {
        public readonly string GiverName;
        public readonly IReadOnlyList<SpeciesAmount> Products;
        /// <summary>Kart girenleri gösteriyorsa (Görev 1–2) adları; katsayı verilmez. Göstermiyorsa null.</summary>
        public readonly IReadOnlyList<SpeciesData> Reactants;
        public readonly int StepIndex;
        public readonly int StepCount;
        public readonly bool IsPurchase;
        public readonly int Money;
        public readonly int Budget;
        public readonly string CurrencyName;

        public OrderCardInfo(string giverName, IReadOnlyList<SpeciesAmount> products, IReadOnlyList<SpeciesData> reactants,
            int stepIndex, int stepCount, bool isPurchase, int money, int budget, string currencyName)
        {
            GiverName = giverName;
            Products = products;
            Reactants = reactants;
            StepIndex = stepIndex;
            StepCount = stepCount;
            IsPurchase = isPurchase;
            Money = money;
            Budget = budget;
            CurrencyName = currencyName;
        }
    }

    /// <summary>Görünümden denetleyiciye komutlar (düğme, sürükle-bırak, tıklama).</summary>
    public interface IOrderCommands
    {
        /// <summary>Raftan kazana bir kavanoz (sürükle-bırak ya da tıkla).</summary>
        void AddToCauldron(SpeciesData species);
        void Increase(SpeciesData species);
        void Decrease(SpeciesData species);
        /// <summary>Fare tekeri / RB-LB bu türü değiştirir.</summary>
        void Select(SpeciesData species);
        /// <summary>Kavanoz etiketine tıklandı → mühür notu.</summary>
        void TouchLabel(SpeciesData species);
        void FireStove();
        void ToggleHint();
        void CloseRealLifeCard();
    }

    /// <summary>İstasyon paneli açık/kapalı (oyuncu kazana girince açılır).</summary>
    public interface IOrderStationView
    {
        void SetOpen(bool open);
    }

    public interface IOrderCardView
    {
        void ShowOrder(OrderCardInfo info);
        /// <summary>Kısa bilgi/replik: "Keseden yeterince akçe kalmadı", teslim repliği…</summary>
        void ShowMessage(string text);
    }

    public interface IShelfView
    {
        void ShowShelf(IReadOnlyList<ShelfItem> items, SpeciesData selected);
        /// <summary>Etikete tıklandı: mühür titrer. note null değilse not metni de gösterilir (yalnızca ilk sefer).</summary>
        void PlaySealShake(SpeciesData species, string note);
    }

    public interface ICauldronView
    {
        void ShowContents(IReadOnlyList<SpeciesAmount> contents, SpeciesData selected);
        void SetStoveLit(bool lit);
        void PlayFoam(float duration);
        void PlayReaction(float duration);
    }

    /// <summary>İki terazi + atom sayaçları. Satır başına: sol / sağ / artan.</summary>
    public interface IScaleView
    {
        void ShowAtoms(IReadOnlyList<AtomRow> rows, bool balanced);
        /// <summary>Köpürme: Left &gt; Right olan elementler fazla (kırmızı), Left &lt; Right eksik (kesikli boş daire).</summary>
        void ShowFoamDifference(IReadOnlyList<AtomRow> rows);
        void ClearFoamDifference();
    }
}
