using System;
using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>Sorunun görevdeki yeri (mantık sırası).</summary>
    public enum FieldQuestionStage
    {
        /// <summary>Ders notundan hemen sonra, pazara gitmeden: hesap yapılır (kaç tohum, kaç kese).</summary>
        Plan,
        /// <summary>Tohum sandığa dökülünce, ekimden önce (toprak neden sürüldü).</summary>
        BeforeSowing,
        /// <summary>Ekim bitince, tohum toprağa girmişken (tohum nasıl nefes alır).</summary>
        AfterSowing
    }

    /// <summary>Tarladaki bir saha sorusu (çoktan seçmeli): Matematik ya da Biyoloji, cevaba göre ustanın yanıtı.</summary>
    [Serializable]
    public sealed class FieldQuestion
    {
        [Tooltip("Görevin hangi anında sorulur.")]
        [SerializeField] private FieldQuestionStage stage;
        [Tooltip("Soruyu soran (Hesap Ustası / Hayat Ustası).")]
        [SerializeField] private string speaker = "";
        [TextArea(2, 4)] [SerializeField] private string question = "";
        [SerializeField] private string[] options = new string[3];
        [SerializeField] private int correctIndex;
        [Tooltip("Her şıkkın yanıtı (yanlışta neden yanlış olduğu; ipucu, tipik hata).")]
        [TextArea(1, 4)] [SerializeField] private string[] replies = new string[3];

        public FieldQuestionStage Stage => stage;
        public string Speaker => speaker;
        public string Question => question;
        public IReadOnlyList<string> Options => options;
        public int CorrectIndex => correctIndex;
        public string ReplyFor(int i) => replies != null && i >= 0 && i < replies.Length ? replies[i] : string.Empty;

        public FieldQuestion() { }

        public FieldQuestion(FieldQuestionStage stage, string speaker, string question, string[] options, int correctIndex, string[] replies)
        {
            this.stage = stage;
            this.speaker = speaker;
            this.question = question;
            this.options = options;
            this.correctIndex = correctIndex;
            this.replies = replies;
        }
    }

    /// <summary>
    /// Tarla görevi (15-bereket-tarlasi-mat-biyo, 16-traktor): Matematik + Biyoloji. Bu sürüm yürüyerek oynanır:
    /// pazardan tohum kesesi al → tarladaki ekim sandığına dök → saha soruları → parseller değişir → defter.
    /// Doğru kese sayısı (Sv2 Tohum Nefes Alır: 120 filiz ÷ 0,8 = 150 tohum = 15 kese) oyuncuya söylenmez;
    /// ders notu yöntemi anlatır, sandık az gelirse kaç filiz çıkacağını söyler.
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Tarla görevi", fileName = "bereket_")]
    public sealed class FieldTask : TaskData
    {
        [Header("Tohum")]
        [Tooltip("Pazarda satılan tohum kesesi (envanter maddesi, alet olarak işaretli).")]
        [SerializeField] private SpeciesData seedBag;
        [Min(1)] [SerializeField] private int seedsPerBag = 10;
        [Range(0.05f, 1f)] [SerializeField] private float germination = 0.8f;
        [Min(1)] [SerializeField] private int targetSprouts = 120;
        [Tooltip("Görev alınınca keseye bir kez konan akçe (tohum parası).")]
        [Min(0)] [SerializeField] private int advance = 15;

        [Header("Parseller (Tarla_P<satır><sütun>_<Durum>)")]
        [Tooltip("Görev başlarken tarlanın durumu (Elif sürmüş olur).")]
        [SerializeField] private string startState = "Surulmus";
        [Tooltip("Görev bitince tarlanın durumu.")]
        [SerializeField] private string endState = "Ekili";

        [Header("Konuşmalar")]
        [Tooltip("Görev gelince ustanın haberi (Elif'in satırları TaskData.giverLines).")]
        [TextArea(1, 3)] [SerializeField] private string[] masterOfferLines = new string[0];
        [Tooltip("Sandık: {0} kese, {1} tohum, {2} beklenen filiz.")]
        [TextArea(1, 3)] [SerializeField] private string tooFewLine = "";
        [TextArea(1, 3)] [SerializeField] private string exactLine = "";
        [Tooltip("{0} fazla kese.")]
        [TextArea(1, 3)] [SerializeField] private string tooManyLine = "";
        [TextArea(1, 3)] [SerializeField] private string noSeedLine = "";
        [Tooltip("Sorular bitince, parseller değişirken.")]
        [TextArea(1, 3)] [SerializeField] private string[] finishLines = new string[0];

        [Header("Traktörle ekim")]
        [Tooltip("Ekici takılı traktörle tarlada atılacak tur (tarla merkezinin çevresinde tam dönüş).")]
        [Min(1)] [SerializeField] private int requiredLaps = 2;
        [Tooltip("Sandık dolunca: ekici traktöre takıldı, tarlada tur at.")]
        [TextArea(1, 3)] [SerializeField] private string[] driveLines = new string[0];
        [Tooltip("Tur bildirimi: {0} biten tur, {1} gereken.")]
        [SerializeField] private string lapNotice = "Tur {0} / {1}";
        [Tooltip("Turlar bitince (kalan parseller ekilirken).")]
        [TextArea(1, 3)] [SerializeField] private string lapsDoneLine = "";

        [Header("Sipariş kartı rehberi (HUD; Emin'in adım rehberi)")]
        [Tooltip("Adımlar: 0 plan soruları · 1 tohum al · 2 sandığa dök · 3 traktörle tur · 4 ekim sonrası soru.")]
        [SerializeField] private string[] guideSteps = new string[5];
        [Tooltip("Sıradaki adımın kısa açıklaması. 2: {0} dökülen kese · 3: {0} biten tur, {1} gereken.")]
        [SerializeField] private string[] guideDetails = new string[5];

        [Header("Tarladaki kişiler (Elif, Hesap Ustası, Hayat Ustası)")]
        [Tooltip("Sırayla: veren (Elif), Hesap Ustası, Hayat Ustası.")]
        [SerializeField] private string[] npcNames = { "Çiftçi Elif", "Hesap Ustası", "Hayat Ustası" };
        [Tooltip("Etkileşim ipucu; {0} kişinin adı.")]
        [SerializeField] private string talkPrompt = "{0} ile konuş";
        [Tooltip("Aşamaya göre söz (6): 0 görev gelmedi · 1 ders notu/hesap · 2 tohum al/dök · 3 traktörle tur · 4 ekim sonrası · 5 görev bitti.")]
        [TextArea(1, 3)] [SerializeField] private string[] giverTalk = new string[6];
        [TextArea(1, 3)] [SerializeField] private string[] mathTalk = new string[6];
        [TextArea(1, 3)] [SerializeField] private string[] biologyTalk = new string[6];

        [Header("Saha soruları (sırayla; her biri kendi anında)")]
        [SerializeField] private List<FieldQuestion> questions = new List<FieldQuestion>();

        [Header("Kart ve ipuçları")]
        [SerializeField] private string place = "Bereket Tarlası";
        [SerializeField] private string wantedText = "";
        [SerializeField] private string[] stepTexts = new string[3];
        [SerializeField] private string pourPrompt = "Tohumları sandığa dök";
        [SerializeField] private string continueWord = "Devam";
        [SerializeField] private string elifSpeaker = "Çiftçi Elif";

        public SpeciesData SeedBag => seedBag;
        public int SeedsPerBag => seedsPerBag;
        public float Germination => germination;
        public int TargetSprouts => targetSprouts;
        public int Advance => advance;
        public string StartState => startState;
        public string EndState => endState;
        public string[] MasterOfferLines => masterOfferLines;
        public string TooFewLine => tooFewLine;
        public string ExactLine => exactLine;
        public string TooManyLine => tooManyLine;
        public string NoSeedLine => noSeedLine;
        public string[] FinishLines => finishLines;
        public IReadOnlyList<FieldQuestion> Questions => questions;
        public string NpcName(FieldNpcRole r) => npcNames != null && (int)r < npcNames.Length ? npcNames[(int)r] : string.Empty;
        public string TalkPrompt => talkPrompt;

        /// <summary>Kişinin o aşamadaki sözü; boşsa bir önceki dolu aşamanınki.</summary>
        public string TalkLine(FieldNpcRole r, int stage)
        {
            var arr = r == FieldNpcRole.Giver ? giverTalk : r == FieldNpcRole.Math ? mathTalk : biologyTalk;
            if (arr == null) return string.Empty;
            for (int i = Mathf.Min(stage, arr.Length - 1); i >= 0; i--)
                if (!string.IsNullOrWhiteSpace(arr[i])) return arr[i];
            return string.Empty;
        }

        public string[] GuideSteps => guideSteps;
        public string[] GuideDetails => guideDetails;
        public int RequiredLaps => requiredLaps;
        public string[] DriveLines => driveLines;
        public string LapNotice => lapNotice;
        public string LapsDoneLine => lapsDoneLine;
        public string Place => place;
        public string WantedText => wantedText;
        public string[] StepTexts => stepTexts;
        public string PourPrompt => pourPrompt;
        public string ContinueWord => continueWord;
        public string ElifSpeaker => elifSpeaker;

        /// <summary>Hedefe yetecek en az kese: ⌈hedef ÷ olasılık ÷ kese başına tohum⌉.</summary>
        public int RequiredBags => Mathf.CeilToInt(targetSprouts / germination / seedsPerBag - 0.0001f);

#if UNITY_EDITOR
        public void EditorSetCommon(string taskId, string giver, string[] lines, string hint, string realLife) =>
            SetCommon(taskId, giver, lines, new string[0], hint, realLife);

        public void EditorSetTalk(string[] names, string prompt, string[] giver, string[] math, string[] biology)
        {
            npcNames = names; talkPrompt = prompt; giverTalk = giver; mathTalk = math; biologyTalk = biology;
        }

        public void EditorSetGuide(string[] steps, string[] details)
        {
            guideSteps = steps; guideDetails = details;
        }

        public void EditorSetDriving(int laps, string[] drive, string notice, string done)
        {
            requiredLaps = laps; driveLines = drive; lapNotice = notice; lapsDoneLine = done;
        }

        public void EditorSet(SpeciesData bag, int perBag, float p, int target, int advanceMoney, string start, string end,
            string[] master, string few, string exact, string many, string noSeed, string[] finish,
            List<FieldQuestion> qs, string placeName, string wanted, string[] steps, string pour)
        {
            seedBag = bag; seedsPerBag = perBag; germination = p; targetSprouts = target; advance = advanceMoney;
            startState = start; endState = end; masterOfferLines = master; tooFewLine = few; exactLine = exact;
            tooManyLine = many; noSeedLine = noSeed; finishLines = finish; questions = qs; place = placeName;
            wantedText = wanted; stepTexts = steps; pourPrompt = pour;
        }
#endif
    }
}
