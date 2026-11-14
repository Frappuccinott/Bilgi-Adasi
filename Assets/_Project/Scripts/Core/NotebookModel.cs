using System;
using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>Defterin üç ayracı (Figma: Keşif Defteri ve Başarılar / Defter/4–7, Ders Notu/1–2).</summary>
    public enum NotebookTab
    {
        MasterNotes = 0, // Ustanın Notları
        Discoveries = 1, // Keşiflerim
        Mastery = 2      // Ustalık
    }

    public enum MasteryState
    {
        Done,
        Next,
        Locked
    }

    /// <summary>Bir Keşif Defteri kaydının "Öteki derslerde" notu (saha notu): ders, başlık, cümle.</summary>
    [Serializable]
    public sealed class FieldNote
    {
        [Tooltip("Ders şeridinin adıyla aynı: Matematik, Fizik, Biyoloji …")]
        [SerializeField] private string subject = "";
        [Tooltip("Kısa başlık: \"Matematik, oran\".")]
        [SerializeField] private string heading = "";
        [TextArea(1, 3)] [SerializeField] private string text = "";

        public string Subject => subject;
        public string Heading => heading;
        public string Text => text;

        public FieldNote() { }

        public FieldNote(string subject, string heading, string text)
        {
            this.subject = subject;
            this.heading = heading;
            this.text = text;
        }
    }

    /// <summary>Defterin kenarındaki ders şeridi.</summary>
    [Serializable]
    public sealed class NotebookSubject
    {
        [SerializeField] private string name = "";
        [SerializeField] private Color color = Color.gray;
        [Tooltip("Bu dersin kendi kayıtları (Keşiflerim) ve ders notları var: yalnızca Kimya (prototip).")]
        [SerializeField] private bool primary;
        [Tooltip("Kilitli: şerit soluk, sayfası kilit metni.")]
        [SerializeField] private bool locked;

        public string Name => name;
        public Color Color => color;
        public bool Primary => primary;
        public bool Locked => locked;

        public NotebookSubject() { }

        public NotebookSubject(string name, Color color, bool primary, bool locked)
        {
            this.name = name;
            this.color = color;
            this.primary = primary;
            this.locked = locked;
        }
    }

    /// <summary>Köyün defterdeki başlığı ("ciraklik" → "Atölye", "denklestirme" → "Denkleştirme Köyü").</summary>
    [Serializable]
    public struct NotebookGroupTitle
    {
        [SerializeField] private string neighborhoodId;
        [SerializeField] private string title;

        public string NeighborhoodId => neighborhoodId;
        public string Title => title;

        public NotebookGroupTitle(string neighborhoodId, string title)
        {
            this.neighborhoodId = neighborhoodId;
            this.title = title;
        }
    }

    /// <summary>Defterin sabit kelimeleri (kodda metin yok). LearningData'da durur.</summary>
    [Serializable]
    public sealed class NotebookTexts
    {
        [Header("Ayraçlar")]
        [SerializeField] private string masterNotesTab = "Ustanın Notları";
        [SerializeField] private string discoveriesTab = "Keşiflerim";
        [SerializeField] private string masteryTab = "Ustalık";

        [Header("Ders şeritleri")]
        [SerializeField] private List<NotebookSubject> subjects = new List<NotebookSubject>();
        [SerializeField] private List<NotebookGroupTitle> groupTitles = new List<NotebookGroupTitle>();
        [Tooltip("Başlığı listede olmayan köy için. {0} = köy adı.")]
        [SerializeField] private string groupFormat = "{0} Köyü";

        [Header("Keşiflerim")]
        [Tooltip("{0} = yazılan sayfa sayısı.")]
        [SerializeField] private string pagesWritten = "{0} sayfa yazıldı";
        [Tooltip("{0} = görev adı.")]
        [SerializeField] private string notWritten = "{0}, yazılmadı";
        [Tooltip("Sağ sayfanın soluk satırı. {0} = sakin ve yer, {1} = köy, {2} = görev sırası.")]
        [SerializeField] private string placeFormat = "{0}. {1}, {2}. görev";
        [SerializeField] private string otherSubjects = "Öteki derslerde";
        [SerializeField] private string emptyDiscovery = "Bu sayfa henüz boş. Görevi bitirince kendi cümlenle yazacaksın.";
        [Tooltip("Öteki ders şeridi: {0} = ders adı.")]
        [SerializeField] private string fieldNotesTitle = "{0} notları";
        [SerializeField] private string fieldNotesEmpty = "Bu derste henüz notun yok. Kimya görevlerinde rastladıkça buraya düşer.";
        [SerializeField] private string lockedSubject = "Bu dersin şehri henüz sisli. Kapısı açılınca defterin bu bölümü de dolar.";

        [Header("Ustanın Notları")]
        [Tooltip("{0} = açılmış not sayısı.")]
        [SerializeField] private string notesOpened = "{0} not açıldı";
        [SerializeField] private string lessonLocked = "Görevi alınca açılır";
        [SerializeField] private string noLesson = "Ders notu yok";
        [Tooltip("Köyün hiç ders notu yoksa tek satır.")]
        [SerializeField] private string lessonsLater = "Bu köyün ders notları henüz yazılmadı";
        [SerializeField] private string lessonKicker = "Ders notu";
        [Tooltip("{0} = köy, {1} = görev sırası.")]
        [SerializeField] private string lessonDetail = "{0}, {1}. görev";
        [SerializeField] private string inThisNote = "Bu notta";
        [Tooltip("Ders notunun sayfa sırasına göre bölüm adları (Bu notta tablosu).")]
        [SerializeField] private string[] sectionLabels = { "Konu", "Yöntem", "Çözümlü örnek", "Bu görevde" };
        [SerializeField] private string readHint = "Enter ile notu baştan oku. Görev bittikten sonra da burada durur.";
        [SerializeField] private string notesEmpty = "Usta henüz not bırakmadı. Tezgâhtan ilk siparişi al.";

        [Header("Ustalık")]
        [SerializeField] private string masteryTitle = "Ustalık";
        [SerializeField] private string masterySubtitle = "Câbir Usta’nın çırağı";
        [SerializeField] private string gradeLabel = "Sınıf";
        [SerializeField] private string heightLabel = "Boy";
        [Tooltip("{0} = boy (m).")]
        [SerializeField] private string heightFormat = "{0:0.00} m";
        [SerializeField] private string sealedLabel = "Mühürlü görev";
        [SerializeField] private string achievementsLabel = "Başarım";
        [SerializeField] private string villagesTitle = "Köyler";
        [SerializeField] private string villageKicker = "Köy";
        [SerializeField] private string villageDetail = "Kimya, Etkileşim";
        [SerializeField] private string stateDone = "Mühürlü";
        [SerializeField] private string stateNext = "Sıradaki";
        [SerializeField] private string stateLocked = "Henüz değil";
        [SerializeField] private string noFails = "Köpürmeden.";
        [Tooltip("{0} = hata sayısı.")]
        [SerializeField] private string failFormat = "{0} köpürme.";
        [SerializeField] private string villageRule = "Beş görevin beşi de mühürlenince köy mührü verilir.";

        [Header("Alt satır ve HUD")]
        [SerializeField] private string tabsWord = "Ayraç";
        [SerializeField] private string subjectWord = "Ders";
        [SerializeField] private string selectWord = "Seç";
        [SerializeField] private string readWord = "Oku";
        [SerializeField] private string backWord = "Geri";
        [SerializeField] private string closeWord = "Kapat";
        [SerializeField] private string hudWord = "Defter";
        [SerializeField] private string newPageWord = "Deftere yeni sayfa";

        public string MasterNotesTab => masterNotesTab;
        public string DiscoveriesTab => discoveriesTab;
        public string MasteryTab => masteryTab;
        public IReadOnlyList<NotebookSubject> Subjects => subjects;
        public string PagesWritten => pagesWritten;
        public string NotWritten => notWritten;
        public string PlaceFormat => placeFormat;
        public string OtherSubjects => otherSubjects;
        public string EmptyDiscovery => emptyDiscovery;
        public string FieldNotesTitle => fieldNotesTitle;
        public string FieldNotesEmpty => fieldNotesEmpty;
        public string LockedSubject => lockedSubject;
        public string NotesOpened => notesOpened;
        public string LessonLocked => lessonLocked;
        public string NoLesson => noLesson;
        public string LessonsLater => lessonsLater;
        public string LessonKicker => lessonKicker;
        public string LessonDetail => lessonDetail;
        public string InThisNote => inThisNote;
        public string SectionLabel(int page) => sectionLabels != null && page >= 0 && page < sectionLabels.Length ? sectionLabels[page] : string.Empty;
        public string ReadHint => readHint;
        public string NotesEmpty => notesEmpty;
        public string MasteryTitle => masteryTitle;
        public string MasterySubtitle => masterySubtitle;
        public string GradeLabel => gradeLabel;
        public string HeightLabel => heightLabel;
        public string HeightFormat => heightFormat;
        public string SealedLabel => sealedLabel;
        public string AchievementsLabel => achievementsLabel;
        public string VillagesTitle => villagesTitle;
        public string VillageKicker => villageKicker;
        public string VillageDetail => villageDetail;
        public string StateDone => stateDone;
        public string StateNext => stateNext;
        public string StateLocked => stateLocked;
        public string NoFails => noFails;
        public string FailFormat => failFormat;
        public string VillageRule => villageRule;
        public string TabsWord => tabsWord;
        public string SubjectWord => subjectWord;
        public string SelectWord => selectWord;
        public string ReadWord => readWord;
        public string BackWord => backWord;
        public string CloseWord => closeWord;
        public string HudWord => hudWord;
        public string NewPageWord => newPageWord;

        public string TabTitle(NotebookTab tab) =>
            tab == NotebookTab.MasterNotes ? masterNotesTab : tab == NotebookTab.Discoveries ? discoveriesTab : masteryTab;

        public string GroupTitle(string neighborhoodId, string neighborhoodTitle)
        {
            for (int i = 0; i < groupTitles.Count; i++)
                if (groupTitles[i].NeighborhoodId == neighborhoodId) return groupTitles[i].Title;
            return string.Format(groupFormat, neighborhoodTitle);
        }

#if UNITY_EDITOR
        public void SetSubjects(IEnumerable<NotebookSubject> list) => subjects = new List<NotebookSubject>(list);
        public void SetGroupTitles(IEnumerable<NotebookGroupTitle> list) => groupTitles = new List<NotebookGroupTitle>(list);
#endif
    }

    // ---------- Anlık görüntü (denetleyici kurar, view çizer) ----------

    /// <summary>Defterin bir satırı: Keşiflerim'de bir kayıt, Ustanın Notları'nda bir ders notu, öteki derste bir saha notu.</summary>
    public sealed class NotebookRow
    {
        public string Code;          // "K-1", "1"
        public string Title;         // "Kuru Çeşme", "Atomlar kaybolmaz"
        public bool Enabled;         // yazıldı / açıldı
        public string Kicker;        // sağ sayfa üst etiketi (bakır)
        public string Subtitle;      // soluk satır
        public string Body;          // oyuncunun cümlesi / notun özeti
        public string Equation;      // zengin metin
        public string IconFormula;   // UiTheme.IconFor için
        public string Footer;        // "Bu sayfa … işe yarayacak"
        public string FieldSubject;  // saha notu satırı: hangi ders
        public readonly List<FieldNote> FieldNotes = new List<FieldNote>();
        public LessonNote Lesson;    // Ustanın Notları: okunacak not
    }

    public sealed class NotebookGroup
    {
        public string Title;
        public string Count;         // "3/5"
        public readonly List<NotebookRow> Rows = new List<NotebookRow>();
    }

    public sealed class MasteryTask
    {
        public string Title;
        public string Detail;
        public MasteryState State;
    }

    public sealed class MasteryVillage
    {
        public string Title;
        public int Done;
        public readonly List<MasteryTask> Tasks = new List<MasteryTask>();
    }

    /// <summary>Defterin açıldığı anki hâli (NotebookDirector kurar). View yalnızca çizer.</summary>
    public sealed class NotebookModel
    {
        public NotebookTexts Texts;
        public LessonTexts LessonTexts;
        public readonly List<NotebookGroup> Discoveries = new List<NotebookGroup>();
        public int WrittenCount;
        public readonly List<NotebookGroup> Lessons = new List<NotebookGroup>();
        public int OpenedLessonCount;
        /// <summary>Öteki dersler: ders adı → o derse düşen saha notları (köy köy).</summary>
        public readonly Dictionary<string, List<NotebookGroup>> FieldNotesBySubject = new Dictionary<string, List<NotebookGroup>>();
        public readonly List<KeyValuePair<string, string>> MasteryStats = new List<KeyValuePair<string, string>>();
        public readonly List<MasteryVillage> Villages = new List<MasteryVillage>();
        /// <summary>Açılınca seçili gelecek kayıt (yeni yazılan sayfa) — Keşiflerim'de kod, yoksa boş.</summary>
        public string FocusCode;
    }
}
