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

        [Header("Açılış: ada (göz açılınca kare kare sinematik)")]
        [Tooltip("Sırayla oynanır; her kare kamerayı from → to arasında yavaşça kaydırır. Boşsa doğrudan uyanışa geçilir.")]
        [SerializeField] private IntroShot[] introShots = new IntroShot[0];
        [Tooltip("Sağ alttaki ipuçları: kareyi geç / bütün açılışı atla.")]
        [SerializeField] private string nextWord = "Devam";
        [SerializeField] private string skipWord = "Atla";

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
        [TextArea(1, 2)] [SerializeField] private string doneNotice = "Kavanozlar geri geldi: ne girerse o çıkar. Tezgâha bak; köyden biri seni arıyor.";

        public OrderTask Task => task;
        public IntroShot[] IntroShots => introShots;
        public string NextWord => nextWord;
        public string SkipWord => skipWord;
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

        public void SetIntro(IntroShot[] shots) => introShots = shots;
#endif
    }

    /// <summary>Açılış sinematiğinin bir karesi: üst satır (yer adı), cümle ve kameranın yavaş kayışı (dünya koordinatı).</summary>
    [System.Serializable]
    public sealed class IntroShot
    {
        [Tooltip("Küçük altın üst satır: yerin adı.")]
        [SerializeField] private string kicker;
        [TextArea(2, 3)] [SerializeField] private string text;
        [Min(1f)] [SerializeField] private float seconds = 5f;
        [SerializeField] private Vector3 fromPosition, toPosition;
        [Tooltip("Kameranın baktığı nokta (başta → sonda).")]
        [SerializeField] private Vector3 fromLook, toLook;
        [Range(30f, 80f)] [SerializeField] private float fieldOfView = 55f;

        public string Kicker => kicker;
        public string Text => text;
        public float Seconds => seconds;
        public float FieldOfView => fieldOfView;

        /// <summary>Kareye göre (0–1) kamera konumu ve bakışı; kayış yumuşak başlar ve yumuşak biter.</summary>
        public void Evaluate(float t, out Vector3 position, out Quaternion rotation)
        {
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            position = Vector3.Lerp(fromPosition, toPosition, e);
            var look = Vector3.Lerp(fromLook, toLook, e);
            var dir = look - position;
            rotation = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir, Vector3.up) : Quaternion.identity;
        }

        public IntroShot() { }

        public IntroShot(string kicker, string text, Vector3 fromPos, Vector3 toPos, Vector3 fromLook, Vector3 toLook,
            float seconds = 5f, float fov = 55f)
        {
            this.kicker = kicker;
            this.text = text;
            fromPosition = fromPos;
            toPosition = toPos;
            this.fromLook = fromLook;
            this.toLook = toLook;
            this.seconds = seconds;
            fieldOfView = fov;
        }
    }
}
