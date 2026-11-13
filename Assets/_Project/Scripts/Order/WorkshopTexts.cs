using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Atölye ekleri için oyuncuya görünen metinler: eşyayı elde taşıma / bırakma, kilitli dolap, Atom Görüşü,
    /// Hata Dedektifi. Kodda metin yazılmaz; bu asset'ten okunur (Resources/WorkshopTexts).
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Metin/Atölye ekleri", fileName = "WorkshopTexts")]
    public sealed class WorkshopTexts : ScriptableObject
    {
        public const string ResourcePath = "WorkshopTexts";

        [Header("Elde taşıma / bırakma")]
        [Tooltip("{0}: formül (zengin), {1}: adet")]
        [SerializeField] private string dropPrompt = "Bırak: {0} ×{1}";
        [Tooltip("{0}: formül (zengin), {1}: adet")]
        [SerializeField] private string pickPrompt = "Al: {0} ×{1}";
        [SerializeField] private string inventoryFull = "Elin dolu. Önce bir yere bırak.";

        [Header("Kilitli dolap")]
        [SerializeField] private string cabinetPrompt = "Kilitli dolap";
        [TextArea(2, 4)]
        [SerializeField] private string cabinetNotice =
            "Dolap kilitli. İçinde cıva, kezzap ve zırnık var: Usta izni olmadan dokunulmaz.\nGerçek hayatta tehlikeli maddeler yalnızca öğretmen gözetiminde, laboratuvarda kullanılır.";

        [Header("Atom Görüşü")]
        [SerializeField] private string atomTitle = "Atom Görüşü";
        [SerializeField] private string leftoverLabel = "artan";
        [Tooltip("Sayacın altındaki not.")]
        [SerializeField] private string atomFooter = "Atomlar kaybolmadı, yalnızca yeniden dizildi.";

        [Header("Hata Dedektifi (tahta)")]
        [SerializeField] private string huntPrompt = "Tahtaya bak";
        [TextArea(1, 3)]
        [SerializeField] private string huntNotice = "Usta tahtaya hatalı bir denklem bırakmış. Tahtaya bakıp hatayı bul.";
        [SerializeField] private string huntTitle = "Hata Dedektifi";
        [SerializeField] private string huntQuestion = "Hangi elementler dengesiz? İşaretle, sonra kontrol et.";
        [SerializeField] private string huntCheck = "Kontrol et";
        [SerializeField] private string huntCorrect = "Doğru buldun.";
        [TextArea(1, 3)]
        [SerializeField] private string huntWrong = "Tam değil. Tebeşirle her elementin iki taraftaki sayısını yazdım; yeniden bak.";
        [SerializeField] private string huntContinue = "Devam";
        [SerializeField] private string huntDone = "Hata Dedektifi tamam. Şimdi siparişe dön.";
        [Tooltip("{0}: seç, {1}: işaretle/onayla, {2}: çık (tuş simgeleri)")]
        [SerializeField] private string huntControls = "{0} seç   {1} işaretle   {2} çık";
        [Tooltip("{0}: element, {1}: sol sayı, {2}: sağ sayı")]
        [SerializeField] private string huntCount = "{1} / {2}";
        [SerializeField] private string roundFormat = "Tur {0} / {1}";

        public string DropPrompt => dropPrompt;
        public string PickPrompt => pickPrompt;
        public string InventoryFull => inventoryFull;
        public string CabinetPrompt => cabinetPrompt;
        public string CabinetNotice => cabinetNotice;
        public string AtomTitle => atomTitle;
        public string LeftoverLabel => leftoverLabel;
        public string AtomFooter => atomFooter;
        public string HuntPrompt => huntPrompt;
        public string HuntNotice => huntNotice;
        public string HuntTitle => huntTitle;
        public string HuntQuestion => huntQuestion;
        public string HuntCheck => huntCheck;
        public string HuntCorrect => huntCorrect;
        public string HuntWrong => huntWrong;
        public string HuntContinue => huntContinue;
        public string HuntDone => huntDone;
        public string HuntControls => huntControls;
        public string HuntCount => huntCount;
        public string RoundFormat => roundFormat;

        private static WorkshopTexts cached;

        /// <summary>Resources/WorkshopTexts (yoksa varsayılan değerlerle geçici bir örnek).</summary>
        public static WorkshopTexts Get()
        {
            if (cached != null) return cached;
            cached = Resources.Load<WorkshopTexts>(ResourcePath);
            if (cached == null) cached = CreateInstance<WorkshopTexts>();
            return cached;
        }
    }
}
