using BilgiAdasi.Chemistry;
using BilgiAdasi.Order;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Seviye 0 — Çıraklık, "İlk Kural" (04-gorev-plani §4, 01 §1): sınıfta uyuyakalma kartları → atölye kapısında
    /// uyanış (Câbir Usta) → tahmin → kazana iki H₂, ısıt (tepkime yok, terazi eşit kalır) → Sorgu + K-0.
    /// Oyuncuya görünen bütün metin burada.
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Çıraklık (Seviye 0)", fileName = "Ciraklik")]
    public sealed class ApprenticeshipData : ScriptableObject
    {
        [Tooltip("Seviye 0 görevi (cirak_0): kazana iki H₂, ısıt. Tepkimesi kendine eşit (2H₂ → 2H₂).")]
        [SerializeField] private OrderTask task;

        [Header("Açılış: sınıf (kararmış ekranda kartlar)")]
        [TextArea(1, 3)] [SerializeField] private string[] classroomCards = new string[0];
        [Tooltip("Bir kartın ekranda kalma süresi (sn). Onay tuşu geçer.")]
        [SerializeField] private float cardSeconds = 3.2f;

        [Header("Uyanış: atölye kapısı")]
        [SerializeField] private string masterName = "Câbir Usta";
        [TextArea(2, 4)] [SerializeField] private string[] wakeLines = new string[0];

        [Header("Tahmin (görevden önce)")]
        [TextArea(1, 3)] [SerializeField] private string predictionQuestion = "";
        [SerializeField] private string[] predictionOptions = new string[3];
        [SerializeField] private int predictionCorrect;
        [Tooltip("Her seçenek için ustanın tek cümlelik cevabı. Görev geri alınmaz; yanlış tahmin de denenerek görülür.")]
        [SerializeField] private string[] predictionReplies = new string[3];
        [SerializeField] private string continueWord = "Devam";

        [Header("Hediye ve notlar")]
        [SerializeField] private SpeciesData giftSpecies;
        [Min(0)] [SerializeField] private int giftCount = 2;
        [TextArea(1, 2)] [SerializeField] private string giftNotice = "Usta eline iki kavanoz H<sub>2</sub> verdi. Kazana dök, ateşi yak.";
        [TextArea(1, 2)] [SerializeField] private string doneNotice = "Kavanozlar geri geldi: ne girerse o çıkar. Kapıya bak; köyden biri seni arıyor.";

        public OrderTask Task => task;
        public string[] ClassroomCards => classroomCards;
        public float CardSeconds => cardSeconds;
        public string MasterName => masterName;
        public string[] WakeLines => wakeLines;
        public string PredictionQuestion => predictionQuestion;
        public string[] PredictionOptions => predictionOptions;
        public int PredictionCorrect => predictionCorrect;
        public string ReplyFor(int i) => predictionReplies != null && i >= 0 && i < predictionReplies.Length ? predictionReplies[i] : string.Empty;
        public string ContinueWord => continueWord;
        public SpeciesData GiftSpecies => giftSpecies;
        public int GiftCount => giftCount;
        public string GiftNotice => giftNotice;
        public string DoneNotice => doneNotice;

#if UNITY_EDITOR
        public void Set(OrderTask cirak, string[] cards, string[] wake, string question, string[] options, int correct,
            string[] replies, SpeciesData gift, int count)
        {
            task = cirak;
            classroomCards = cards;
            wakeLines = wake;
            predictionQuestion = question;
            predictionOptions = options;
            predictionCorrect = correct;
            predictionReplies = replies;
            giftSpecies = gift;
            giftCount = count;
        }
#endif
    }
}
