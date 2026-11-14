using System;
using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using BilgiAdasi.Order.Market;
using BilgiAdasi.Player;
using BilgiAdasi.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Şehirdeki pazar tezgâhı (sahne bağlantı noktası; modeli Mustafa'da). E → pazar ekranı açılır (UI modu).
    /// Ne satılacağı tezgâhın kendi kataloğundan (MarketCatalog) gelir ve kurallardan (MarketRuleSet) geçer:
    /// görevle aç/kapat, siparişin ürününü satma, stok. Pazar açıkken sipariş alınır/biterse liste kendiliğinden yenilenir.
    /// Para aktif siparişin en ucuz malzemesine yetmezse Usta keseye borç koyar (oyun kilitlenmez). Esc/B ile kapanır.
    /// </summary>
    public sealed class MarketStall : WorldInteractable, IMarketCommands, IMarketState, IMarketInfo
    {
        [Tooltip("Boşsa ortak \"Pazar\" ipucu. Birden çok tezgâhta ayırt etmek için: \"Element pazarı\", \"Alet pazarı\".")]
        [SerializeField] private string promptOverride = "";
        [Tooltip("Pazar ekranının başlığı (boşsa ekranın varsayılanı: \"Pazar\").")]
        [SerializeField] private string marketTitle = "";
        [Tooltip("Başlığın altındaki tek satır (boşsa ekranın varsayılanı).")]
        [SerializeField] private string marketSubtitle = "";
        [Tooltip("Bu tezgâhın satış listesi. İkinci pazar = başka bir katalog.")]
        [SerializeField] private MarketCatalog catalog;
        [Tooltip("Borç miktarı buradan okunur.")]
        [SerializeField] private EconomyData economy;
        [SerializeField] private OrderTexts texts;

        private readonly MarketRuleSet rules = MarketRuleSet.CreateDefault();
        private readonly List<MarketOffer> offers = new List<MarketOffer>();
        private readonly List<MarketItem> items = new List<MarketItem>();
        private readonly HashSet<string> orderProducts = new HashSet<string>();
        private readonly HashSet<string> orderReactants = new HashSet<string>();
        // Kayıt sistemi yokken (test sahnesi) satış adetleri burada tutulur.
        private readonly Dictionary<string, int> localSold = new Dictionary<string, int>();

        private PlayerInventory user;
        private PlayerInteractor controllingInteractor;
        private InputAction cancel;
        private bool open;

        public override bool CanInteract => base.CanInteract && !open && catalog != null && economy != null;

        public string MarketTitle => marketTitle;
        public string MarketSubtitle => marketSubtitle;

        public override string GetPrompt(PlayerInteractor interactor) =>
            !string.IsNullOrEmpty(promptOverride) ? promptOverride : texts != null ? texts.MarketPrompt : string.Empty;

        private static SaveData Data => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;
        private string CatalogId => catalog != null ? catalog.name : string.Empty;

        private void OnEnable()
        {
            GameEvents.ActiveOrderChanged += OnActiveOrderChanged;
            GameEvents.TaskCompleted += OnTaskCompleted;
            CacheOrderProducts(GameEvents.ActiveOrder);
        }

        private void OnDisable()
        {
            GameEvents.ActiveOrderChanged -= OnActiveOrderChanged;
            GameEvents.TaskCompleted -= OnTaskCompleted;
            CloseMarket();
        }

        public override void Interact(PlayerInteractor interactor)
        {
            var view = ViewLocator.Get<IMarketView>();
            if (view == null || interactor.Inventory == null) return;

            user = interactor.Inventory;
            open = true;
            interactor.SetControlEnabled(false);
            controllingInteractor = interactor;
            GameInput.SetMode(InputMode.UI);
            cancel = GameInput.Controls.UI.Cancel;
            cancel.performed += OnCancel;
            ViewLocator.Register<IMarketCommands>(this);

            Rebuild();
            GiveLoanIfBroke();
            Show(view);
        }

        public void Buy(SpeciesData species)
        {
            if (!open || species == null) return;

            // Satırın hâlâ satışta olduğunu doğrula (liste bu arada değişmiş olabilir).
            Rebuild();
            var offer = FindOffer(species);
            if (offer == null)
            {
                Show(ViewLocator.Get<IMarketView>());
                return;
            }

            if (!user.CanAdd(species))
            {
                Notice(texts != null ? texts.InventoryFull : null);
                GameEvents.RaiseSound(SoundId.Wrong);
                return;
            }
            if (!user.TrySpend(offer.Price))
            {
                GameEvents.RaiseSound(SoundId.Wrong);
                Notice(texts != null ? texts.NotEnoughMoney : null);
                // Siparişin malzemesi alınamıyorsa (ör. 3 akçeyle 5 akçelik propan) Usta yine borç verir.
                if (orderReactants.Contains(offer.Key)) GiveLoan();
                else GiveLoanIfBroke();
                Show(ViewLocator.Get<IMarketView>());
                return;
            }
            user.Add(species, 1);
            GameEvents.RaiseSound(SoundId.CoinSingle);
            AddSold(offer.Key);
            Rebuild();
            Show(ViewLocator.Get<IMarketView>());
        }

        public void CloseMarket()
        {
            if (!open) return;
            open = false;
            if (GameInput.TryGetControls(out _)) cancel.performed -= OnCancel;
            ViewLocator.Unregister<IMarketCommands>(this);
            ViewLocator.Get<IMarketView>()?.Close();
            GameInput.SetMode(InputMode.Player);
            if (controllingInteractor != null) controllingInteractor.SetControlEnabled(true);
            controllingInteractor = null;
            user = null;
        }

        // ---------- IMarketState ----------

        public bool IsTaskCompleted(string taskId)
        {
            var data = Data;
            return data != null && data.IsTaskCompleted(taskId);
        }

        public bool IsActiveOrderProduct(string key) => orderProducts.Contains(key);

        public int SoldCount(string key)
        {
            var data = Data;
            if (data != null) return data.GetMarketSold(CatalogId, key);
            return localSold.TryGetValue(key, out int n) ? n : 0;
        }

        // ---------- Olaylar ----------

        private void OnActiveOrderChanged(TaskData task)
        {
            CacheOrderProducts(task);
            RefreshIfOpen();
        }

        private void OnTaskCompleted(string _) => RefreshIfOpen();

        private void OnCancel(InputAction.CallbackContext _) => CloseMarket();

        private void RefreshIfOpen()
        {
            if (!open) return;
            Rebuild();
            Show(ViewLocator.Get<IMarketView>());
        }

        // ---------- İç ----------

        /// <summary>Katalogdan satırları üretir ve kurallardan geçirir.</summary>
        private void Rebuild()
        {
            offers.Clear();
            if (catalog == null) return;
            var entries = catalog.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || e.Species == null) continue;
                offers.Add(new MarketOffer(e.Key, i, e.Price, e.UnlimitedStock, e.Stock,
                    e.AvailableAfter != null ? e.AvailableAfter.Id : null,
                    e.RemovedAfter != null ? e.RemovedAfter.Id : null));
            }
            rules.Apply(offers, this);
        }

        private void Show(IMarketView view)
        {
            if (view == null || user == null) return;
            items.Clear();
            var entries = catalog.Entries;
            for (int i = 0; i < offers.Count; i++)
            {
                var s = entries[offers[i].SourceIndex].Species;
                items.Add(new MarketItem(s, offers[i].Price, user.CountOf(s)));
            }
            view.Open(items, user.Money);
        }

        private MarketOffer FindOffer(SpeciesData species)
        {
            for (int i = 0; i < offers.Count; i++)
                if (offers[i].Key == species.Formula) return offers[i];
            return null;
        }

        private void CacheOrderProducts(TaskData task)
        {
            orderProducts.Clear();
            orderReactants.Clear();
            if (!(task is OrderTask order)) return;
            var steps = order.Steps;
            for (int i = 0; i < steps.Count; i++)
            {
                var products = steps[i].TargetProducts;
                for (int j = 0; j < products.Count; j++)
                    if (products[j].Species != null) orderProducts.Add(products[j].Species.Formula);
                var reaction = steps[i].Reaction;
                if (reaction == null) continue;
                for (int j = 0; j < reaction.Reactants.Count; j++)
                    if (reaction.Reactants[j].Species != null) orderReactants.Add(reaction.Reactants[j].Species.Formula);
            }
            // Siparişin herhangi bir adımında girdi olan madde gizlenmez; yoksa görev kilitlenir
            // (cirak_0: H2 → H2, denk_4: 2. adım CaO ister).
            for (int i = 0; i < steps.Count; i++)
            {
                var reaction = steps[i].Reaction;
                if (reaction == null) continue;
                var reactants = reaction.Reactants;
                for (int j = 0; j < reactants.Count; j++)
                    if (reactants[j].Species != null) orderProducts.Remove(reactants[j].Species.Formula);
            }
        }

        private void AddSold(string key)
        {
            var data = Data;
            if (data != null) data.AddMarketSold(CatalogId, key, 1);
            else localSold[key] = (localSold.TryGetValue(key, out int n) ? n : 0) + 1;
        }

        /// <summary>
        /// Usta yalnızca işi yürütmek için borç verir: aktif siparişin bu tezgâhta satılan bir malzemesine kese
        /// yetmiyorsa. Sipariş yoksa ya da tezgâh sipariş malzemesi satmıyorsa (alet pazarı) borç yok.
        /// </summary>
        private void GiveLoanIfBroke()
        {
            if (offers.Count == 0 || orderReactants.Count == 0) return;
            int cheapest = int.MaxValue;
            for (int i = 0; i < offers.Count; i++)
                if (orderReactants.Contains(offers[i].Key) && offers[i].Price < cheapest) cheapest = offers[i].Price;
            if (cheapest == int.MaxValue || user.Money >= cheapest) return;
            GiveLoan();
        }

        private void GiveLoan()
        {
            user.AddMoney(economy.LoanAmount);
            if (texts != null) Notice(string.Format(texts.LoanGiven, string.Empty, economy.LoanAmount));
        }

        private static void Notice(string text)
        {
            if (!string.IsNullOrEmpty(text)) ViewLocator.Get<INoticeView>()?.ShowNotice(text);
        }
    }
}
