using System;
using System.Collections.Generic;
using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Ustanın Sorgusu (04-gorev-plani §2.3): başarıdan sonra 3 açıklama, biri doğru, ikisi tipik yanılgı.
    /// Yanlışta görev geri alınmaz; usta yanılgıyı tek cümleyle düzeltir.
    /// </summary>
    [Serializable]
    public sealed class ReasoningPrompt
    {
        [TextArea(1, 3)] [SerializeField] private string question = "";
        [SerializeField] private string[] options = new string[3];
        [SerializeField] private int correctIndex;
        [Tooltip("Her seçenek için ustanın tek cümlelik cevabı.")]
        [SerializeField] private string[] replies = new string[3];

        public bool IsEmpty => string.IsNullOrWhiteSpace(question) || options == null || options.Length == 0;
        public string Question => question;
        public IReadOnlyList<string> Options => options;
        public int CorrectIndex => correctIndex;

        public string ReplyFor(int index) =>
            replies != null && index >= 0 && index < replies.Length ? replies[index] : string.Empty;

        public ReasoningPrompt() { }

        public ReasoningPrompt(string question, string[] options, int correctIndex, string[] replies)
        {
            this.question = question;
            this.options = options;
            this.correctIndex = correctIndex;
            this.replies = replies;
        }
    }

    /// <summary>Bir görevin öğrenme katmanı: ders notu (görevden önce) + sorgu (isteğe bağlı) + Keşif Defteri kaydı.</summary>
    [Serializable]
    public sealed class LearningEntry
    {
        [Tooltip("TaskData.Id ile aynı: denk_1 …")]
        [SerializeField] private string taskId = "";
        [Tooltip("Görevden önce ustanın konu anlatımı (13-ders-notu-plani). Sayfası yoksa gösterilmez.")]
        [SerializeField] private LessonNote lesson = new LessonNote();
        [SerializeField] private ReasoningPrompt reasoning = new ReasoningPrompt();
        [Tooltip("Keşif Defteri kayıt numarası: K-1")]
        [SerializeField] private string discoveryId = "";
        [TextArea(2, 4)] [SerializeField] private string discoveryText = "";
        [Tooltip("Öğrenilen konunun kısa adı (müfredat dili): \"Yanma tepkimeleri\". Görev sonu bildiriminde ve başarım kartının altında görünür. Boşsa ders notunun adı.")]
        [SerializeField] private string topic = "";

        [Header("Defter sayfası (Keşiflerim)")]
        [Tooltip("Görevin defterdeki adı: \"Kuru Çeşme\".")]
        [SerializeField] private string title = "";
        [Tooltip("Sakin ve yer: \"Muhtar Yusuf, meydan çeşmesi\".")]
        [SerializeField] private string placeLine = "";
        [Tooltip("Sayfadaki denklem (zengin metin, alt indisli).")]
        [SerializeField] private string equation = "";
        [Tooltip("Denklemin yanındaki kavanoz ikonu: UiTheme.IconFor formülü (H2O).")]
        [SerializeField] private string iconFormula = "";
        [Tooltip("Sayfanın altındaki soluk not: bu kayıt nerede işe yarar.")]
        [SerializeField] private string usage = "";
        [SerializeField] private List<FieldNote> fieldNotes = new List<FieldNote>();

        public string TaskId => taskId;
        public string Title => title;
        public string PlaceLine => placeLine;
        public string Equation => equation;
        public string IconFormula => iconFormula;
        public string Usage => usage;
        public IReadOnlyList<FieldNote> FieldNotes => fieldNotes;
        public LessonNote Lesson => lesson;
        public ReasoningPrompt Reasoning => reasoning;
        public string DiscoveryId => discoveryId;
        public string DiscoveryText => discoveryText;
        /// <summary>Öğrenilen konu: elle girilen ad, yoksa ders notunun adı, o da yoksa boş.</summary>
        public string Topic => !string.IsNullOrWhiteSpace(topic) ? topic
            : lesson != null && !string.IsNullOrWhiteSpace(lesson.Title) ? lesson.Title : string.Empty;

        public LearningEntry() { }

        public LearningEntry(string taskId, ReasoningPrompt reasoning, string discoveryId, string discoveryText,
            LessonNote lesson = null)
        {
            this.taskId = taskId;
            this.reasoning = reasoning ?? new ReasoningPrompt();
            this.discoveryId = discoveryId;
            this.discoveryText = discoveryText;
            this.lesson = lesson ?? new LessonNote();
        }

#if UNITY_EDITOR
        /// <summary>Editör veri üreticisi için: öğrenilen konunun kısa adı.</summary>
        public LearningEntry WithTopic(string topicName)
        {
            topic = topicName ?? string.Empty;
            return this;
        }

        /// <summary>Editör veri üreticisi için: defter sayfasının alanları.</summary>
        public LearningEntry WithPage(string pageTitle, string place, string richEquation, string icon, string usageNote,
            params FieldNote[] notes)
        {
            title = pageTitle ?? string.Empty;
            placeLine = place ?? string.Empty;
            equation = richEquation ?? string.Empty;
            iconFormula = icon ?? string.Empty;
            usage = usageNote ?? string.Empty;
            fieldNotes = new List<FieldNote>(notes ?? new FieldNote[0]);
            return this;
        }
#endif
    }

    /// <summary>
    /// Ustanın Sorgusu + Keşif Defteri verisi (04-gorev-plani §2.1, §2.3, §4). Oyuncuya görünen bütün metin burada.
    /// Resources'ta durur: LearningDirector sahneye dokunmadan kendini kurar ve bu asset'i yükler.
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Öğrenme (Sorgu + Keşif Defteri)", fileName = "LearningData")]
    public sealed class LearningData : ScriptableObject
    {
        public const string ResourcePath = "LearningData";

        [Header("Metinler")]
        [SerializeField] private string masterName = "Câbir Usta";
        [SerializeField] private string bookTitle = "Keşif Defteri";
        [Tooltip("{0} = kayıt numarası (K-1), {1} = kayıt metni.")]
        [TextArea(1, 3)] [SerializeField] private string writtenNote = "Keşif Defteri'ne yazıldı ({0}):\n{1}";
        [Tooltip("Kısa HUD notu. {0} = kayıt numarası.")]
        [SerializeField] private string writtenNotice = "Keşif Defteri'ne yeni sayfa: {0}";
        [SerializeField] private string continueWord = "Devam";
        [SerializeField] private string closeWord = "Kapat";
        [SerializeField] private string emptyBook = "Defter henüz boş. Gördüğünü yaz: her siparişten sonra bir sayfa eklenir.";
        [Tooltip("Tepkime bittikten sonra sorgunun açılmasına kadar bekleme (sn).")]
        [SerializeField] private float delayAfterReaction = 0.6f;

        [Header("Ders Notu (görevden önce)")]
        [SerializeField] private LessonTexts lessonTexts = new LessonTexts();

        [Header("Defter (ayraçlar, şeritler, Ustalık)")]
        [SerializeField] private NotebookTexts notebookTexts = new NotebookTexts();

        [Header("Görevler")]
        [SerializeField] private List<LearningEntry> entries = new List<LearningEntry>();

        public string MasterName => masterName;
        public string BookTitle => bookTitle;
        public string WrittenNote => writtenNote;
        public string WrittenNotice => writtenNotice;
        public string ContinueWord => continueWord;
        public string CloseWord => closeWord;
        public string EmptyBook => emptyBook;
        public float DelayAfterReaction => delayAfterReaction;
        public LessonTexts LessonTexts => lessonTexts;
        public NotebookTexts NotebookTexts => notebookTexts;
        public IReadOnlyList<LearningEntry> Entries => entries;

        public LearningEntry Find(string taskId)
        {
            if (string.IsNullOrEmpty(taskId)) return null;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null && entries[i].TaskId == taskId) return entries[i];
            return null;
        }

#if UNITY_EDITOR
        /// <summary>Editör veri üreticisi için.</summary>
        public void SetEntries(IEnumerable<LearningEntry> list) => entries = new List<LearningEntry>(list);
#endif
    }
}
