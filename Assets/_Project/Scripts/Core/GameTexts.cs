using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Görevlere bağlı olmayan, oyuncuya görünen ortak metinler (CLAUDE.md §8: metin kodda sabit yazılmaz).
    /// {0} gibi yer tutucular string.Format ile doldurulur.
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Ortak Metinler", fileName = "GameTexts")]
    public sealed class GameTexts : ScriptableObject
    {
        [Header("Kişiler")]
        [SerializeField] private string masterName = "Câbir Usta";
        [SerializeField] private string realLifeTitle = "Gerçek hayatta";
        [SerializeField] private string discoveryTitle = "Keşif Defteri";
        [SerializeField] private string hintTitle = "Ustanın Notları";

        [Header("Kazan")]
        [TextArea(1, 3)] [SerializeField] private string noOrder = "Kazan boş bekliyor. Köye in; kimin derdi varsa dinle, siparişi buraya getir.";
        [TextArea(1, 3)] [SerializeField] private string sealNote = "Kavanozun içindekini değiştiremezsin. Kaç kavanoz koyacağını değiştirirsin.";
        [TextArea(1, 3)] [SerializeField] private string foamNote = "Kazan köpürdü! Terazilere bak: yanıp sönen atomlar fazla ya da eksik.";
        [TextArea(1, 3)] [SerializeField] private string balancedNote = "Teraziler dengede. Ocak kendiliğinden tutuştu…";
        [TextArea(1, 3)] [SerializeField] private string hintGlowNote = "Ustanın defteri parlıyor. (H)";
        [TextArea(1, 3)] [SerializeField] private string noHintYet = "Defterde bu sipariş için henüz bir not yok. Önce kendin dene.";
        [TextArea(1, 3)] [SerializeField] private string stepDone = "Adım tamam. Yeni ürün rafa kondu: {0}";
        [TextArea(1, 3)] [SerializeField] private string leftover = "Artan madde: {0}";

        [Header("Sipariş kartı")]
        [SerializeField] private string cardWanted = "İstenen";
        [SerializeField] private string cardReactants = "Girenler";
        [SerializeField] private string cardStep = "Adım {0}/{1}";
        [SerializeField] private string cardPurse = "Kese: {0} akçe";
        [SerializeField] private string leftScaleTitle = "GİREN";
        [SerializeField] private string rightScaleTitle = "ÇIKAN";
        [TextArea(1, 3)] [SerializeField] private string controlsKeyboard = "Tık / teker ↑: kavanoz ekle   Teker ↓ / −: çıkar   F: ateşi yak   H: defter   Esc: çık";
        [TextArea(1, 3)] [SerializeField] private string controlsGamepad = "Sol çubuk: kavanoz seç   A / RB: ekle   LB: çıkar   Y: ateşi yak   X: defter   B: çık";

        [Header("Satın alma")]
        [SerializeField] private string priceLabel = "{0} akçe";
        [TextArea(1, 3)] [SerializeField] private string noRefund = "Satın alınan geri satılmaz. Hesabı baştan yap.";
        [TextArea(1, 3)] [SerializeField] private string outOfMoney = "Bir dahakine dikkat. Eksiği benden olsun.";
        [TextArea(1, 3)] [SerializeField] private string notEnoughMoney = "Kesede o kadar akçe yok.";

        [Header("Sorgu ve defter")]
        [TextArea(1, 3)] [SerializeField] private string discoveryWritten = "Keşif Defteri'ne yazıldı ({0}):\n{1}";
        [SerializeField] private string continueLabel = "Devam";

        [Header("Köy")]
        [TextArea(1, 3)] [SerializeField] private string residentWaitTurn = "Şimdi değil evladım, önce başkalarının derdi var. Sonra uğra.";
        [TextArea(1, 3)] [SerializeField] private string orderAccepted = "Sipariş alındı: {0}. Atölyedeki kazana git.";
        [TextArea(1, 3)] [SerializeField] private string neighborhoodDone = "{0} ayağa kalktı!";
        [SerializeField] private string talkPrompt = "Konuş";
        [SerializeField] private string objectiveActive = "{0}: {1} için atölyedeki kazana git";
        [SerializeField] private string objectiveNext = "{0}: {1} ile konuş";

        public string MasterName => masterName;
        public string RealLifeTitle => realLifeTitle;
        public string DiscoveryTitle => discoveryTitle;
        public string HintTitle => hintTitle;
        public string NoOrder => noOrder;
        public string SealNote => sealNote;
        public string FoamNote => foamNote;
        public string BalancedNote => balancedNote;
        public string HintGlowNote => hintGlowNote;
        public string NoHintYet => noHintYet;
        public string StepDone => stepDone;
        public string Leftover => leftover;
        public string CardWanted => cardWanted;
        public string CardReactants => cardReactants;
        public string CardStep => cardStep;
        public string CardPurse => cardPurse;
        public string LeftScaleTitle => leftScaleTitle;
        public string RightScaleTitle => rightScaleTitle;
        public string ControlsKeyboard => controlsKeyboard;
        public string ControlsGamepad => controlsGamepad;
        public string PriceLabel => priceLabel;
        public string NoRefund => noRefund;
        public string OutOfMoney => outOfMoney;
        public string NotEnoughMoney => notEnoughMoney;
        public string DiscoveryWritten => discoveryWritten;
        public string ContinueLabel => continueLabel;
        public string ResidentWaitTurn => residentWaitTurn;
        public string OrderAccepted => orderAccepted;
        public string NeighborhoodDone => neighborhoodDone;
        public string TalkPrompt => talkPrompt;
        public string ObjectiveActive => objectiveActive;
        public string ObjectiveNext => objectiveNext;
    }
}
