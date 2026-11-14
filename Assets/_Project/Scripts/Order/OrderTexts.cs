using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Atölyenin görevden bağımsız metinleri: etkileşim ipuçları ve kısa notlar (kodda sabit Türkçe metin olmaz).
    /// "{0}" madde formülü (alt indisli), "{1}" adet ya da akçe.
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Metin/Sipariş Metinleri", fileName = "OrderTexts")]
    public sealed class OrderTexts : ScriptableObject
    {
        [Header("Notlar")]
        [TextArea(2, 4)] [SerializeField] private string sealNote =
            "Kavanozun içindekini değiştiremezsin. Kaç kavanoz koyacağını değiştirirsin.";
        [TextArea(1, 3)] [SerializeField] private string notEnoughMoney = "Keseden yeterince akçe kalmadı.";
        [TextArea(1, 3)] [SerializeField] private string inventoryFull = "Envanterde yer yok.";
        [TextArea(1, 3)] [SerializeField] private string loanGiven = "Usta keseye {1} akçe koydu: \"Sonra ödersin.\"";
        [TextArea(1, 3)] [SerializeField] private string noOrder = "Bekleyen sipariş yok. Tezgâha bir bak.";
        [TextArea(1, 3)] [SerializeField] private string readyToDeliver = "Sipariş hazır. Tezgâhta bekleyen sahibine götür.";
        [TextArea(1, 3)] [SerializeField] private string productWaiting = "Envanterde yer yok; ürün kazanda bekliyor.";
        [TextArea(1, 3)] [SerializeField] private string foamLost = "Kazan köpürdü, içindekiler gitti. Teraziye bak, baştan dök.";
        [TextArea(1, 3)] [SerializeField] private string drained = "Kazan boşaltıldı.";
        [TextArea(1, 3)] [SerializeField] private string nextStep = "Bir adım tamam. Sıradaki adım için kazanı yeniden doldur.";
        [SerializeField] private string currencyName = "akçe";

        [Header("İpuçları (crosshair)")]
        [SerializeField] private string pourPrompt = "Kazana dök: {0}";
        [SerializeField] private string emptyHandPrompt = "Kazan (elinde madde yok)";
        [SerializeField] private string takeProductPrompt = "Ürünü al";
        [SerializeField] private string firePrompt = "Ateşi yak";
        [SerializeField] private string drainPrompt = "Kazanı boşalt";
        [SerializeField] private string takeShelfPrompt = "Al: {0} ×{1}";
        [SerializeField] private string placeShelfPrompt = "Rafa koy: {0} ×{1}";
        [SerializeField] private string emptyShelfPrompt = "Boş raf";
        [SerializeField] private string marketPrompt = "Pazar";
        [SerializeField] private string toolNotPourable = "Alet kazana dökülmez.";

        [Header("Sipariş kartı: adım rehberi")]
        [SerializeField] private string guideCollect = "Malzemeleri al";
        [Tooltip("Kart girenleri gösteriyorsa sonuna adları eklenir.")]
        [SerializeField] private string guideCollectDetail = "Pazardan ya da raftan";
        [Tooltip("Kart girenleri saklıyorsa (oyuncu kendisi bulur).")]
        [SerializeField] private string guideCollectHidden = "Hangi maddeler? Ürünün atomlarına bak";
        [SerializeField] private string guidePour = "Kazana dök";
        [SerializeField] private string guidePourDetail = "Elindeki kavanozla kazana bak. Dökülen geri alınmaz";
        [SerializeField] private string guideBalance = "Teraziyi dengele";
        [SerializeField] private string guideBalanceDetail = "Sol kefedeki atomlar sağ kefedekilere eşit olsun";
        [SerializeField] private string guidePurchase = "Yeterince koy";
        [SerializeField] private string guidePurchaseDetail = "İstenen ürün kadar malzeme olsun";
        [SerializeField] private string guideFire = "Ateşi yak";
        [SerializeField] private string guideFireDetail = "Ocağa bak; terazi dengedeyse tepkime başlar";
        [SerializeField] private string guideReacting = "Tepkime sürüyor…";
        [SerializeField] private string guideNextStage = "Ürün sonraki aşamanın malzemesi";
        [SerializeField] private string guideDeliver = "Kapıya götür";
        [Tooltip("{0} = sipariş veren.")]
        [SerializeField] private string guideDeliverDetail = "{0} kapıda bekliyor; ürünü elinde tut";
        [SerializeField] private string guideMasterTakes = "Usta bakıyor";
        [SerializeField] private string guideMasterTakesDetail = "Ürün sende kalır";

        public string SealNote => sealNote;
        public string NotEnoughMoney => notEnoughMoney;
        public string InventoryFull => inventoryFull;
        public string LoanGiven => loanGiven;
        public string NoOrder => noOrder;
        public string ReadyToDeliver => readyToDeliver;
        public string ProductWaiting => productWaiting;
        public string FoamLost => foamLost;
        public string Drained => drained;
        public string NextStep => nextStep;
        public string CurrencyName => currencyName;

        public string PourPrompt => pourPrompt;
        public string EmptyHandPrompt => emptyHandPrompt;
        public string TakeProductPrompt => takeProductPrompt;
        public string FirePrompt => firePrompt;
        public string DrainPrompt => drainPrompt;
        public string TakeShelfPrompt => takeShelfPrompt;
        public string PlaceShelfPrompt => placeShelfPrompt;
        public string EmptyShelfPrompt => emptyShelfPrompt;
        public string MarketPrompt => marketPrompt;
        public string ToolNotPourable => toolNotPourable;

        public string GuideCollect => guideCollect;
        public string GuideCollectDetail => guideCollectDetail;
        public string GuideCollectHidden => guideCollectHidden;
        public string GuidePour => guidePour;
        public string GuidePourDetail => guidePourDetail;
        public string GuideBalance => guideBalance;
        public string GuideBalanceDetail => guideBalanceDetail;
        public string GuidePurchase => guidePurchase;
        public string GuidePurchaseDetail => guidePurchaseDetail;
        public string GuideFire => guideFire;
        public string GuideFireDetail => guideFireDetail;
        public string GuideReacting => guideReacting;
        public string GuideNextStage => guideNextStage;
        public string GuideDeliver => guideDeliver;
        public string GuideDeliverDetail => guideDeliverDetail;
        public string GuideMasterTakes => guideMasterTakes;
        public string GuideMasterTakesDetail => guideMasterTakesDetail;
    }
}
