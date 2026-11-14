using System;
using System.Collections;
using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using BilgiAdasi.Player;
using BilgiAdasi.UI;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Tarla görevi akışı (FieldTask; Sv2 Tohum Nefes Alır). NeighborhoodRunner sıradaki görev tarla görevi olunca Assign eder.
    /// Mantık sırası:
    /// 1) Usta haberi verir, Elif derdini anlatır, ders notu açılır (LearningDirector).
    /// 2) Plan soruları (Hesap Ustası: kaç tohum, kaç kese) — pazara gitmeden hesap yapılır.
    /// 3) Tarla sürülmüş görünür, tohum parası keseye girer; oyuncu pazardan kese alıp ekim sandığına döker (SeedBox).
    ///    Az gelirse Elif kaç filiz çıkacağını söyler, daha ister.
    /// 4) Ekim öncesi soru (toprak neden sürüldü), sonra ekici traktöre takılır.
    /// 5) Oyuncu traktörle tarlada tur atar: ekicinin geçtiği parsel ekilir (FieldPlots.SowAt). Gereken tur bitince
    ///    kalan parseller sıra sıra ekilir (ekim animasyonu).
    /// 6) Ekim sonrası soru (tohum nasıl nefes alır) → Elif → görev biter (TaskCompleted → kayıt) → sorgu + Keşif Defteri.
    /// Kayıt: dökülen kese bayrakla (ekim:&lt;görev&gt;:&lt;n&gt;), plan soruları bayrakla (plan:&lt;görev&gt;); tarla durumu ilerlemeden.
    /// </summary>
    public sealed class FieldTaskDirector : MonoBehaviour
    {
        private enum Phase { Idle, Offering, Lesson, Questions, Planting, Driving, Sowing }

        [SerializeField] private DialogueDirector dialogue;
        [SerializeField] private SeedBox seedBox;
        [SerializeField] private FieldPlots plots;
        [Tooltip("Ekim için sürülecek traktör. Boşsa traktörsüz: sandık dolunca tarla kendiliğinden ekilir.")]
        [SerializeField] private Tractor tractor;
        [Tooltip("Traktöre takılacak ekici (Alet_Ekici). Ekim bitince yerine döner.")]
        [SerializeField] private Transform seeder;
        [Tooltip("Ekicinin tohum bıraktığı nokta, pimden geriye (m).")]
        [SerializeField] private float seederDropBack = 1.3f;
        [Tooltip("Tur sayılırken traktör tarla sınırının bu kadar dışına çıkabilir (m).")]
        [SerializeField] private float lapMargin = 4f;
        [Tooltip("Sahnedeki bütün tarla görevleri (yüklemede tarla durumu buradan: biten son görevin bitiş durumu).")]
        [SerializeField] private List<FieldTask> knownTasks = new List<FieldTask>();
        [SerializeField] private string masterName = "Câbir Usta";
        [SerializeField] private Sprite masterPortrait;
        [Tooltip("Haber, önceki konuşma/sorgu bittikten bu kadar sonra gelir (sn).")]
        [SerializeField] private float offerDelay = 1.5f;

        private Phase phase = Phase.Idle;
        private FieldTask task;
        private int planted;
        private PlayerInteractor user;
        private readonly List<DialogueLine> lines = new List<DialogueLine>();
        private readonly List<FieldQuestion> pending = new List<FieldQuestion>();
        private Coroutine offerRoutine;

        // tur sayacı
        private int lapQuadrants;    // bu turda bulunulan çeyrekler (4 bit; hepsi = 1 tur)
        private int laps;

        // ekicinin traktörden önceki yeri
        private Transform seederParent;
        private Vector3 seederPos;
        private Quaternion seederRot;
        private bool seederAttached;
        private Transform hitch;

        /// <summary>Görev bitti (kayıt GameManager'da). Runner köy ilerlemesini günceller.</summary>
        public event Action<FieldTask> TaskFinished;

        public FieldTask ActiveTask => phase == Phase.Idle ? null : task;
        public bool CanPour => phase == Phase.Planting && task != null;
        public string PourPrompt => task != null ? task.PourPrompt : string.Empty;

        private static SaveData Data => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;
        private static FieldTaskDirector Instance;

        private void Awake()
        {
            if (dialogue == null) dialogue = FindAnyObjectByType<DialogueDirector>();
            if (tractor != null) hitch = FindChild(tractor.transform, "Traktor_Baglanti");
        }

        private UnityEngine.InputSystem.InputAction hintAction;

        private void OnEnable()
        {
            Instance = this;
            hintAction = GameInput.FindAction("Player/OpenHint");
            if (hintAction != null) hintAction.performed += OnHint;
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
            if (hintAction != null) hintAction.performed -= OnHint;
            hintAction = null;
        }

        /// <summary>H / Y: ustanın defterinde görevin ipucu (siparişteki gibi; açılırsa "defter açmadan" sayılmaz).</summary>
        private void OnHint(UnityEngine.InputSystem.InputAction.CallbackContext _)
        {
            if (task == null || (phase != Phase.Planting && phase != Phase.Driving)) return;
            var view = ViewLocator.Get<IHintBookView>();
            if (view == null || string.IsNullOrWhiteSpace(task.HintText)) return;
            if (view.IsHintOpen) { view.HideHint(); return; }
            view.ShowHint(task.HintText);
            GameEvents.RaiseHintOpened(task.Id);
        }

        private void Start()
        {
            // Tarla, biten son tarla görevinin bitiş durumunda başlar (Assign başlangıç durumunu ayrıca kurar).
            var data = Data;
            if (plots == null || data == null) return;
            for (int i = knownTasks.Count - 1; i >= 0; i--)
                if (knownTasks[i] != null && data.IsTaskCompleted(knownTasks[i].Id)) { plots.SetState(knownTasks[i].EndState); break; }
        }

        // ---------- Runner ----------

        /// <summary>Sıradaki görev bu tarla görevi: haber, önceki konuşma bitince verilir.</summary>
        public void Assign(FieldTask fieldTask, bool immediate)
        {
            if (fieldTask == null || (task == fieldTask && phase != Phase.Idle)) return;
            task = fieldTask;
            planted = SavedPlanted();
            phase = Phase.Offering;
            if (offerRoutine != null) StopCoroutine(offerRoutine);
            offerRoutine = StartCoroutine(OfferWhenFree(immediate ? 0.3f : offerDelay));
        }

        /// <summary>Geliştirici atlaması: görevi bırak.</summary>
        public void ClearTask()
        {
            if (offerRoutine != null) StopCoroutine(offerRoutine);
            offerRoutine = null;
            if (phase == Phase.Questions) { ViewLocator.Get<IReasoningView>()?.Hide(); Unblock(); }
            DetachSeeder();
            if (task != null && phase != Phase.Offering && phase != Phase.Lesson) GameEvents.RaiseActiveOrderChanged(null);
            phase = Phase.Idle;
            task = null;
            pending.Clear();
            if (seedBox != null) seedBox.SetFill(0f, true);
        }

        /// <summary>Duraklatma menüsü / HUD görev kartı.</summary>
        public CurrentTaskInfo GetCurrentInfo(string neighborhoodTitle, int done, int total)
        {
            if (task == null || phase == Phase.Idle || phase == Phase.Offering) return null;
            var info = new CurrentTaskInfo
            {
                Title = task.GiverName,
                Quote = task.GiverLines != null && task.GiverLines.Length > 0 ? task.GiverLines[0] : null,
                WantedRich = task.WantedText,
                Place = string.IsNullOrEmpty(task.Place) ? neighborhoodTitle : task.Place,
                Done = done,
                Total = total,
                HasDelivery = false
            };
            // adımlar: 0 tohum al · 1 sandığa dök · 2 traktörle tur · 3 sorular
            int inHand = user != null && user.Inventory != null ? user.Inventory.CountOf(task.SeedBag) : 0;
            int reached = phase == Phase.Driving || phase == Phase.Sowing ? 2
                : planted >= task.RequiredBags ? 2 : inHand > 0 ? 1 : 0;
            if (phase == Phase.Sowing) reached = 3;
            var steps = task.StepTexts;
            for (int i = 0; steps != null && i < steps.Length; i++)
            {
                if (string.IsNullOrEmpty(steps[i])) continue;
                string text = steps[i];
                if (i == 2 && (phase == Phase.Driving || phase == Phase.Sowing))
                    text += "  (" + Mathf.Min(laps, task.RequiredLaps) + " / " + task.RequiredLaps + ")";
                info.Steps.Add(new CurrentTaskInfo.Step(text, i < reached));
            }
            return info;
        }

        // ---------- Haber ve ders notu ----------

        private IEnumerator OfferWhenFree(float delay)
        {
            yield return new WaitForSeconds(delay);
            // Açık konuşma / ders notu / sorgu bitsin. LearningDirector.IsBusy takılı kalabiliyor (Play 14.11: açık hiçbir
            // şey yokken true kaldı); konuşma ve istasyon dışındaki beklemeye üst sınır.
            float waited = 0f;
            while (IsSomethingOpen() && (waited < 15f || DialogueBusy() || StationBase.Active != null || GameInput.Mode == InputMode.UI))
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            offerRoutine = null;

            user = FindAnyObjectByType<PlayerInteractor>(); // haber anında bir kez
            lines.Clear();
            foreach (var l in task.MasterOfferLines) Add(masterName, l, masterPortrait);
            foreach (var l in task.GiverLines) Add(task.GiverName, l, task.GiverPortrait);
            var lessons = LearningDirector.Instance;
            if (lessons != null && lessons.HasLesson(task.Id) && !string.IsNullOrWhiteSpace(lessons.LessonIntroLine))
                Add(masterName, lessons.LessonIntroLine, masterPortrait);
            if (dialogue != null && lines.Count > 0) dialogue.Play(lines, ShowLesson, user);
            else ShowLesson();
        }

        // Pazar, Esc menüsü, harita, defter gibi bir ekran açıkken (UI modu) haber gelmez (Play 14.11: pazarın üstüne açılıyordu).
        private static bool IsSomethingOpen() =>
            LearningDirector.IsBusy || PredictionDirector.IsBusy || StationBase.Active != null || DialogueBusy()
            || GameInput.Mode == InputMode.UI;

        private static bool DialogueBusy()
        {
            var d = Instance != null ? Instance.dialogue : null;
            return d != null && d.IsPlaying;
        }

        private void ShowLesson()
        {
            phase = Phase.Lesson;
            var lessons = LearningDirector.Instance;
            if (lessons != null && lessons.ShowLesson(task.Id, AfterLesson)) return;
            AfterLesson();
        }

        /// <summary>Ders notundan sonra plan soruları (pazara gitmeden hesap). Kayıttan devamda tekrar sorulmaz.</summary>
        private void AfterLesson()
        {
            if (task == null) return;
            var data = Data;
            bool planned = data != null && data.HasFlag(PlanFlag);
            if (planned || planted > 0) { Begin(); return; }
            AskStage(FieldQuestionStage.Plan, () =>
            {
                Data?.SetFlag(PlanFlag);
                Begin();
            });
        }

        private string PlanFlag => "plan:" + task.Id;

        private void Begin()
        {
            if (task == null) return;
            phase = Phase.Planting;
            if (plots != null) plots.AnimateTo(task.StartState);
            // Alınmış görev: Esc menüsündeki "Ders Notu" bu görevin notunu açar (LearningDirector), pazar yenilenir.
            GameEvents.RaiseActiveOrderChanged(task);
            PayAdvance();
            if (seedBox != null) seedBox.SetFill(Fill(), true);
            if (planted >= task.RequiredBags) StartDriving(); // kayıttan devam: tohum zaten dökülmüş
        }

        private void PayAdvance()
        {
            if (task.Advance <= 0 || user == null || user.Inventory == null) return;
            var data = Data;
            if (data != null)
            {
                if (data.IsAdvancePaid(task.Id)) return;
                data.MarkAdvancePaid(task.Id);
            }
            user.Inventory.AddMoney(task.Advance);
        }

        // ---------- Ekim sandığı ----------

        public void Pour(PlayerInteractor interactor)
        {
            if (!CanPour || interactor == null || interactor.Inventory == null) return;
            if (dialogue != null && dialogue.IsPlaying) return;
            user = interactor;
            var inv = interactor.Inventory;
            lines.Clear();

            int bags = inv.SelectedSpecies == task.SeedBag ? inv.SelectedCount : 0;
            if (bags <= 0)
            {
                Add(task.ElifSpeaker, task.NoSeedLine, task.GiverPortrait);
                dialogue?.Play(lines, null, interactor);
                return;
            }
            inv.RemoveSelected(bags);
            planted += bags;
            SavePlanted();
            if (seedBox != null) seedBox.SetFill(Fill(), false);
            GameEvents.RaiseSound(SoundId.Pour);

            int required = task.RequiredBags;
            if (planted < required)
            {
                int seeds = planted * task.SeedsPerBag;
                int sprouts = Mathf.RoundToInt(seeds * task.Germination);
                Add(task.ElifSpeaker, string.Format(task.TooFewLine, planted, seeds, sprouts), task.GiverPortrait);
                dialogue?.Play(lines, null, interactor);
                return;
            }
            if (planted == required) Add(task.ElifSpeaker, task.ExactLine, task.GiverPortrait);
            else Add(task.ElifSpeaker, string.Format(task.TooManyLine, planted - required), task.GiverPortrait);
            phase = Phase.Questions; // sandık kapanır
            Action next = () => AskStage(FieldQuestionStage.BeforeSowing, StartDriving);
            if (dialogue != null) dialogue.Play(lines, () => next(), interactor);
            else next();
        }

        private float Fill() => task == null ? 0f : Mathf.Clamp01(planted / (float)Mathf.Max(1, task.RequiredBags));

        private int SavedPlanted()
        {
            var data = Data;
            if (data == null || task == null) return 0;
            int n = 0;
            while (data.HasFlag(PlantedFlag(n + 1))) n++;
            return n;
        }

        private void SavePlanted()
        {
            var data = Data;
            if (data == null) return;
            for (int i = 1; i <= planted; i++) data.SetFlag(PlantedFlag(i));
        }

        private string PlantedFlag(int n) => "ekim:" + task.Id + ":" + n;

        // ---------- Traktörle ekim ----------

        private void StartDriving()
        {
            if (task == null) return;
            if (tractor == null || plots == null)
            {
                // Traktörsüz sahne: tarla kendiliğinden ekilir.
                phase = Phase.Sowing;
                drivingDone = true;
                if (plots != null) plots.SowTo(task.EndState, AfterSowing);
                else AfterSowing();
                return;
            }
            phase = Phase.Driving;
            drivingDone = false;
            laps = 0;
            lapQuadrants = 0;
            AttachSeeder();
            plots.BeginDriveSow(task.EndState);
            if (seedBox != null) seedBox.SetFill(0f, false); // tohum ekiciye geçti
            lines.Clear();
            foreach (var l in task.DriveLines) Add(task.ElifSpeaker, l, task.GiverPortrait);
            if (dialogue != null && lines.Count > 0) dialogue.Play(lines, null, user);
        }

        // ---------- Tarladaki kişilerle konuşma (FieldNpc) ----------

        /// <summary>Konuşma tablosunun görevi: süren görev, yoksa sahnedeki ilk tarla görevi.</summary>
        private FieldTask TalkTask => task != null ? task : knownTasks.Count > 0 ? knownTasks[0] : null;

        /// <summary>Konuşulabilir mi: soru kartı, ders notu ya da başka konuşma açık değilken.</summary>
        public bool CanTalk => TalkTask != null && phase != Phase.Questions && phase != Phase.Lesson && !DialogueBusy()
                               && GameInput.Mode == InputMode.Player;

        public string TalkPrompt(FieldNpcRole role)
        {
            var t = TalkTask;
            if (t == null) return string.Empty;
            // Her kare çağrılır (crosshair NPC'deyken): metni önbellekle, her karede yeni string üretme.
            if (talkCache == null || role != talkRole || t != talkTask)
            {
                talkRole = role;
                talkTask = t;
                talkCache = string.Format(t.TalkPrompt, t.NpcName(role));
            }
            return talkCache;
        }

        private string talkCache;
        private FieldNpcRole talkRole;
        private FieldTask talkTask;

        /// <summary>0 görev gelmedi · 1 haber/ders notu/hesap · 2 tohum al/dök · 3 traktörle tur · 4 ekim sonrası · 5 bitti.</summary>
        private int TalkStage()
        {
            var t = TalkTask;
            var data = Data;
            if (task == null)
                return t != null && data != null && data.IsTaskCompleted(t.Id) ? 5 : 0;
            switch (phase)
            {
                case Phase.Offering:
                case Phase.Lesson: return 1;
                case Phase.Planting: return 2;
                case Phase.Driving: return 3;
                case Phase.Sowing: return 4;
                case Phase.Questions: return planted <= 0 ? 1 : drivingDone ? 4 : 2;
                default: return 0;
            }
        }

        public void Talk(FieldNpcRole role, PlayerInteractor interactor)
        {
            var t = TalkTask;
            if (t == null || !CanTalk) return;
            string line = t.TalkLine(role, TalkStage());
            if (string.IsNullOrWhiteSpace(line)) return;
            string speaker = t.NpcName(role);
            Sprite portrait = role == FieldNpcRole.Giver ? t.GiverPortrait : PortraitBook.Resolve(speaker, null);
            lines.Clear();
            Add(speaker, line, portrait);
            dialogue?.Play(lines, null, interactor);
        }

        // ---------- HUD sipariş kartı: adım rehberi (Emin'in kartı; OrderController siparişsiz anda kartı kapatabilir,
        // bu yüzden görev boyunca yarım saniyede bir yeniden gösterilir) ----------

        private readonly List<OrderGuideStep> guide = new List<OrderGuideStep>(5);
        private readonly List<Chemistry.SpeciesAmount> noProducts = new List<Chemistry.SpeciesAmount>();
        private readonly List<Chemistry.SpeciesData> seedList = new List<Chemistry.SpeciesData>(1);
        private float cardTimer;
        private int cardKey = -1;
        private bool cardShown;
        private bool drivingDone;

        private void RefreshCard(float dt)
        {
            var view = ViewLocator.Get<IOrderCardView>();
            bool visible = task != null && phase != Phase.Idle && phase != Phase.Offering && phase != Phase.Lesson;
            if (!visible || view == null)
            {
                if (cardShown) view?.ClearOrder();
                cardShown = false;
                cardKey = -1;
                return;
            }
            var data = Data;
            int inHand = user != null && user.Inventory != null ? user.Inventory.CountOf(task.SeedBag) : 0;
            int required = task.RequiredBags;
            bool planDone = planted > 0 || phase != Phase.Questions || (data != null && data.HasFlag(PlanFlag));
            bool pourDone = planted >= required;
            // Elde kese varsa sıradaki adım "sandığa dök" (Play 14.11: 12 kese alınınca kart ilerlemiyordu; kaç kese
            // gerektiğini kart değil, döküldüğünde Elif söyler). Döküp eli boşalınca az geldiyse yeniden "pazardan al".
            bool buyDone = pourDone || inHand > 0;
            int key = (planDone ? 1 : 0) | (buyDone ? 2 : 0) | (pourDone ? 4 : 0) | (drivingDone ? 8 : 0)
                      | (planted << 4) | (Mathf.Min(laps, 15) << 12);
            if (key != cardKey)
            {
                cardKey = key;
                guide.Clear();
                bool current = false;
                AddGuide(0, null, planDone, ref current);
                AddGuide(1, null, buyDone, ref current);
                AddGuide(2, Format(2, planted, required), pourDone, ref current);
                AddGuide(3, Format(3, Mathf.Min(laps, task.RequiredLaps), task.RequiredLaps), drivingDone, ref current);
                AddGuide(4, null, false, ref current);
                seedList.Clear();
                if (task.SeedBag != null) seedList.Add(task.SeedBag);
                cardTimer = 0f;
            }
            cardTimer -= dt;
            if (cardTimer > 0f) return;
            cardTimer = 0.5f;
            view.ShowOrder(new OrderCardInfo(task.GiverName, noProducts, seedList, 0, 1, task.Advance, false, guide, task.WantedText));
            cardShown = true;
        }

        private string Format(int i, int a, int b)
        {
            var d = task.GuideDetails;
            return d != null && i < d.Length && !string.IsNullOrEmpty(d[i]) ? string.Format(d[i], a, b) : null;
        }

        private void AddGuide(int i, string formatted, bool done, ref bool currentTaken)
        {
            var steps = task.GuideSteps;
            if (steps == null || i >= steps.Length || string.IsNullOrEmpty(steps[i])) return;
            var state = GuideState.Done;
            if (!done)
            {
                state = currentTaken ? GuideState.Pending : GuideState.Current;
                currentTaken = true;
            }
            string detail = null;
            if (state == GuideState.Current)
            {
                var d = task.GuideDetails;
                detail = formatted ?? (d != null && i < d.Length ? d[i] : null);
            }
            guide.Add(new OrderGuideStep(steps[i], detail, state));
        }

        private void Update()
        {
            RefreshCard(Time.deltaTime);
            if (phase != Phase.Driving || tractor == null || plots == null) return;
            float dt = Time.deltaTime;
            if (!tractor.IsDriving || Mathf.Abs(tractor.Speed) < 0.2f) return;

            Vector3 drop = tractor.transform.position - tractor.transform.forward * DropDistance();
            plots.SowAt(drop, dt);

            // Bütün parseller ekildiyse turu beklemeye gerek yok: ekim bitti (Play 14.11: tarla dolduğu hâlde 2. tur
            // sayılmıyordu).
            if (plots.CellCount > 0 && plots.SownCount >= plots.CellCount)
            {
                if (laps < task.RequiredLaps) { laps = task.RequiredLaps; LapNotice(); }
                FinishDriving();
                return;
            }

            // Tur: tarlanın dört çeyreğinin (merkeze göre) hepsinde bulunmak = 1 tur. Yön ve sıra fark etmez;
            // eski yönlü açı toplamı ters yönde dönünce ya da sıra sıra ekerken (merkezden geçişte ±180° sıçrama)
            // birbirini götürüyordu (Play 14.11). Yalnız tarlanın içinde ya da kenarında sayılır.
            var fb = plots.FieldBounds;
            Vector3 p = tractor.transform.position;
            bool near = p.x > fb.min.x - lapMargin && p.x < fb.max.x + lapMargin
                        && p.z > fb.min.z - lapMargin && p.z < fb.max.z + lapMargin;
            if (!near) return;
            int quadrant = (p.x >= fb.center.x ? 1 : 0) | (p.z >= fb.center.z ? 2 : 0);
            lapQuadrants |= 1 << quadrant;
            if (lapQuadrants != 0b1111) return;
            lapQuadrants = 1 << quadrant; // yeni tur bulunduğu çeyrekten başlar
            laps++;
            LapNotice();
            if (laps >= task.RequiredLaps) FinishDriving();
        }

        private void LapNotice()
        {
            ViewLocator.Get<INoticeView>()?.ShowNotice(string.Format(task.LapNotice, Mathf.Min(laps, task.RequiredLaps), task.RequiredLaps));
            GameEvents.RaiseSound(SoundId.Correct);
        }

        private float DropDistance()
        {
            if (hitch == null) return seederDropBack + 1.9f;
            float hitchBack = -tractor.transform.InverseTransformPoint(hitch.position).z;
            return hitchBack + seederDropBack;
        }

        private void FinishDriving()
        {
            phase = Phase.Sowing;
            drivingDone = true;
            lines.Clear();
            Add(task.ElifSpeaker, task.LapsDoneLine, task.GiverPortrait);
            if (dialogue != null && lines.Count > 0) dialogue.Play(lines, null, user);
            plots.FinishDriveSow(AfterSowing);
        }

        private void AttachSeeder()
        {
            if (seeder == null || tractor == null || seederAttached) return;
            seederParent = seeder.parent;
            seederPos = seeder.position;
            seederRot = seeder.rotation;
            var anchor = hitch != null ? hitch : tractor.transform;
            seeder.SetParent(tractor.transform, true);
            seeder.SetPositionAndRotation(anchor.position, tractor.transform.rotation);
            seederAttached = true;
            tractor.ExtraRearReach = seederDropBack + 1.2f;
            foreach (var c in seeder.GetComponentsInChildren<Collider>()) c.enabled = false;
        }

        private void DetachSeeder()
        {
            if (!seederAttached || seeder == null) return;
            seeder.SetParent(seederParent, true);
            seeder.SetPositionAndRotation(seederPos, seederRot);
            foreach (var c in seeder.GetComponentsInChildren<Collider>()) c.enabled = true;
            seederAttached = false;
            if (tractor != null) tractor.ExtraRearReach = 0f;
        }

        private void AfterSowing()
        {
            if (task == null) return;
            AskStage(FieldQuestionStage.AfterSowing, FinishTask);
        }

        // ---------- Saha soruları ----------

        private Action afterQuestions;

        /// <summary>Bu aşamanın sorularını sırayla sorar (Ustanın Sorgusu kartı); bitince onDone.</summary>
        private void AskStage(FieldQuestionStage stage, Action onDone)
        {
            pending.Clear();
            for (int i = 0; i < task.Questions.Count; i++)
                if (task.Questions[i].Stage == stage) pending.Add(task.Questions[i]);
            if (pending.Count == 0 || ViewLocator.Get<IReasoningView>() == null) { onDone?.Invoke(); return; }
            // Soru kartı açılırken açık konuşma (ör. Elif'in "İki tur tamam") kapanır: iki panel üst üste binmesin (Play 14.11).
            if (dialogue != null && dialogue.IsPlaying) dialogue.Finish();
            var before = phase;
            phase = Phase.Questions;
            afterQuestions = () =>
            {
                if (phase == Phase.Questions) phase = before == Phase.Questions ? Phase.Planting : before;
                onDone?.Invoke();
            };
            Block();
            AskNext(0);
        }

        private void AskNext(int index)
        {
            var view = ViewLocator.Get<IReasoningView>();
            if (view == null || task == null || index >= pending.Count)
            {
                view?.Hide();
                Unblock();
                afterQuestions?.Invoke();
                return;
            }
            var q = pending[index];
            view.ShowQuestion(q.Speaker, q.Question, q.Options, chosen =>
            {
                GameEvents.RaiseSound(chosen == q.CorrectIndex ? SoundId.Correct : SoundId.Wrong);
                view.ShowMessage(q.Speaker, q.ReplyFor(chosen), task.ContinueWord, () => AskNext(index + 1));
            });
        }

        private void FinishTask()
        {
            var done = task;
            DetachSeeder();
            phase = Phase.Idle;
            task = null;
            lines.Clear();
            foreach (var l in done.FinishLines) Add(done.ElifSpeaker, l, done.GiverPortrait);
            if (dialogue != null && lines.Count > 0) dialogue.Play(lines, () => Complete(done), user);
            else Complete(done);
        }

        private void Complete(FieldTask done)
        {
            GameEvents.RaiseActiveOrderChanged(null);
            GameEvents.RaiseTaskCompleted(done.Id);       // kayıt + otomatik kayıt (GameManager)
            LearningDirector.Instance?.Play(done.Id);     // Ustanın Sorgusu + Keşif Defteri
            TaskFinished?.Invoke(done);
        }

        // ---------- Yardımcılar ----------

        private bool blocked;

        private void Block()
        {
            if (blocked) return;
            blocked = true;
            if (user == null) user = FindAnyObjectByType<PlayerInteractor>();
            if (user != null) user.SetControlEnabled(false);
            GameInput.SetCursorFree(true);
        }

        private void Unblock()
        {
            if (!blocked) return;
            blocked = false;
            if (user != null && StationBase.Active == null) user.SetControlEnabled(true);
            GameInput.SetCursorFree(GameInput.Mode != InputMode.Player);
        }

        private void Add(string speaker, string text, Sprite portrait)
        {
            if (!string.IsNullOrWhiteSpace(text)) lines.Add(new DialogueLine(speaker, text, portrait));
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
