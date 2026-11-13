using UnityEngine;

namespace BilgiAdasi.Desk
{
    /// <summary>Masanın görevden bağımsız metinleri (kodda sabit Türkçe metin olmaz). {0}/{1} sayılar.</summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Metin/Masa Metinleri", fileName = "DeskTexts")]
    public sealed class DeskTexts : ScriptableObject
    {
        [Header("Kâğıt")]
        [SerializeField] private string submitWord = "Gönder";
        [SerializeField] private string continueWord = "Devam";
        [SerializeField] private string fromLabel = "Gönderen";
        [SerializeField] private string questionCounter = "Soru {0} / {1}";
        [SerializeField] private string masterName = "Câbir Usta";

        [Header("Kontrol notları")]
        [TextArea(1, 2)] [SerializeField] private string incomplete = "Önce her satırı işaretle.";
        [TextArea(1, 2)] [SerializeField] private string wrong = "Mürekkep lekesi! Lekeli satırlara yeniden bak.";
        [TextArea(1, 2)] [SerializeField] private string wrongEquation = "Mürekkep lekesi! Her elementi iki tarafta ayrı say.";
        [TextArea(1, 2)] [SerializeField] private string notMinimal = "Doğru, ama daha küçük sayılarla da olur.";
        [TextArea(1, 2)] [SerializeField] private string correct = "Doğru.";
        [TextArea(1, 2)] [SerializeField] private string replaced = "Kâtip o soruyu çekti, yerine başka bir soru koydu.";

        [Header("Mühür")]
        [SerializeField] private string masterSeal = "Ustanın mührü basıldı.";
        [SerializeField] private string guildSeal = "Loncanın mührü basıldı.";

        [Header("İpucu")]
        [SerializeField] private string hintTitle = "Ustanın notu";
        [TextArea(1, 2)] [SerializeField] private string hintLocked = "Ustanın defteri iki hatadan sonra açılır.";

        [Header("Masa ve güvercin")]
        [TextArea(1, 2)] [SerializeField] private string pigeonArrived = "Pencereye bir güvercin kondu. Masada bir mektup var.";
        [TextArea(1, 2)] [SerializeField] private string guildArrived = "Loncadan mühürlü bir zarf geldi. Masada seni bekliyor.";
        [SerializeField] private string currentTaskStep = "Masadaki mektubu cevapla";

        public string SubmitWord => submitWord;
        public string ContinueWord => continueWord;
        public string FromLabel => fromLabel;
        public string QuestionCounter => questionCounter;
        public string MasterName => masterName;
        public string Incomplete => incomplete;
        public string Wrong => wrong;
        public string WrongEquation => wrongEquation;
        public string NotMinimal => notMinimal;
        public string Correct => correct;
        public string Replaced => replaced;
        public string MasterSeal => masterSeal;
        public string GuildSeal => guildSeal;
        public string HintTitle => hintTitle;
        public string HintLocked => hintLocked;
        public string PigeonArrived => pigeonArrived;
        public string GuildArrived => guildArrived;
        public string CurrentTaskStep => currentTaskStep;
    }
}
