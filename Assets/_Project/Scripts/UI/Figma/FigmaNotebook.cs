using System;
using System.Collections.Generic;
using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Defter (INotebookView + ILessonView) — Figma: Keşif Defteri ve Başarılar / Defter/2–7, Ders Notu/1–2.
    /// Karartılmış oyunun üstünde açık defter: ahşap cilt, iki kâğıt sayfa, sırt gölgesi, kurdele; üstte üç ayraç
    /// (Ustanın Notları · Keşiflerim · Ustalık), sağ kenarda ders şeritleri. Açılış: alttan eğik yükselir → kapak açılır →
    /// yazı belirir (0,7 sn); kapanış 0,4 sn (gerçek zaman, oyun duruyorken de akar).
    /// Girdi (UI modu): Q / E (LB / RB) ayraç, ↑ ↓ seçim, ← → ders şeridi ya da notta sayfa, Enter / A oku-ileri,
    /// Esc / B geri-kapat. Fareyle: satıra, ayraca, şeride tıklanır; notta sol sayfa geri, sağ sayfa ileri.
    /// Görev öncesi Ders Notu (ILessonView): defter Ustanın Notları'nda, notun içinde açılır; son açılışta Enter görevi başlatır.
    /// </summary>
    public sealed class FigmaNotebook : FigmaView<INotebookView>, INotebookView, ILessonView
    {
        [Header("Yerleşim (Figma: defter 1560×920, sol sütun 92/86 600 px, sağ sütun 850/86 620 px)")]
        [SerializeField] private Vector2 bookSize = new Vector2(1560f, 920f);
        [SerializeField] private float bookOffsetY = -16f;
        [SerializeField] private Vector2 leftColumn = new Vector2(92f, 600f);
        [SerializeField] private Vector2 rightColumn = new Vector2(850f, 620f);
        [SerializeField] private float columnTop = 86f;
        [SerializeField] private float columnBottom = 90f;
        [SerializeField] private int sortingOrder = 40;
        [Tooltip("Ekran 16:9 değilse defter + ayraç + alt satır sığsın diye küçültülür (yatay/dikey pay, px).")]
        [SerializeField] private Vector2 fitMargin = new Vector2(340f, 150f);

        [Header("Canlandırma (sn, gerçek zaman)")]
        [SerializeField] private float riseTime = 0.2f;
        [SerializeField] private float coverTime = 0.25f;
        [SerializeField] private float settleTime = 0.25f;
        [SerializeField] private float closeTime = 0.4f;
        [SerializeField] private float riseDistance = 700f;
        [SerializeField] private float riseTilt = -6f;
        [Tooltip("Linear renk uzayında 0,72 gözle zayıf kalıyor; Figma'daki %55–70 karartmaya bu yakın.")]
        [SerializeField] private float scrimAlpha = 0.86f;
        [SerializeField] private float inputGuardSeconds = 0.3f;

        private enum Phase { Hidden, Opening, Open, Closing }

        private GameObject root;
        private RectTransform rootRect, bookRect, leftCol, rightCol, footerRoot, coverFlap, ribbonsRoot;
        private CanvasGroup contentGroup;
        private Image scrim;
        private TextMeshProUGUI coverTitle;
        private readonly Image[] tabImages = new Image[3];
        private readonly TextMeshProUGUI[] tabLabels = new TextMeshProUGUI[3];
        private readonly List<Image> ribbonImages = new List<Image>();
        private readonly List<TextMeshProUGUI> ribbonLabels = new List<TextMeshProUGUI>();
        private readonly List<Hint> hints = new List<Hint>();

        private struct Hint
        {
            public GameObject Root;
            public TextMeshProUGUI Key1, Key2, Label;
            public RectTransform Key1Root, Key2Root;
        }

        // durum
        private Phase phase = Phase.Hidden;
        private float phaseTime;
        private float bookScale = 1f;
        private NotebookModel model;
        private NotebookTab tab;
        private int subject;
        private readonly int[] selection = new int[3];
        private bool navArmed = true;
        private float openedAt;
        private Action closed;

        // not okuma (Ustanın Notları içinde)
        private LessonNote reading;
        private LessonTexts lessonTexts;
        private int spread;
        private string finishWord;
        private Action lessonFinished;
        private bool lessonOnly;      // ILessonView'dan açıldı: son açılışta Enter defteri kapatır
        private bool lessonFromIndex; // içindekilerden açıldı: sonda içindekilere döner

        private InputAction navigate, submit, cancel, prevTab, nextTab;
        private readonly List<SelectableRow> rows = new List<SelectableRow>();

        private struct SelectableRow
        {
            public int Index;
        }

        public bool IsOpen => root != null && root.activeSelf && phase != Phase.Hidden;

        private int SpreadCount => reading == null ? 0 : (reading.Pages.Count + 1) / 2;

        private NotebookTexts Texts => model != null ? model.Texts : null;

        private void Awake() => Build();

        protected override void OnEnable()
        {
            base.OnEnable();
            ViewLocator.Register<ILessonView>(this);
            GameInput.SchemeChanged += OnScheme;
            if (GameInput.TryGetControls(out _))
            {
                navigate = GameInput.FindAction("UI/Navigate");
                submit = GameInput.FindAction("UI/Submit");
                cancel = GameInput.FindAction("UI/Cancel");
                prevTab = GameInput.FindAction("UI/PrevTab");
                nextTab = GameInput.FindAction("UI/NextTab");
                submit.performed += OnSubmit;
                cancel.performed += OnCancel;
                prevTab.performed += OnPrevTab;
                nextTab.performed += OnNextTab;
                // Yön olayla okunur: D-pad'e hızlı dokunuşta basma + bırakma aynı karede gelse de kaçmaz
                // (her karede ReadValue ile okumak bu basışları atlıyordu: "1–2 kez basmak gerekiyor").
                navigate.performed += OnNavigate;
                navigate.canceled += OnNavigate;
            }
        }

        protected override void Start()
        {
            base.Start();
            ViewLocator.Register<ILessonView>(this);
        }

        protected override void OnDisable()
        {
            ViewLocator.Unregister<ILessonView>(this);
            GameInput.SchemeChanged -= OnScheme;
            if (submit != null && GameInput.TryGetControls(out _))
            {
                submit.performed -= OnSubmit;
                cancel.performed -= OnCancel;
                prevTab.performed -= OnPrevTab;
                nextTab.performed -= OnNextTab;
                navigate.performed -= OnNavigate;
                navigate.canceled -= OnNavigate;
            }
            base.OnDisable();
        }

        // ---------- INotebookView ----------

        public void OpenBook(NotebookModel notebook, NotebookTab startTab, Action onClosed)
        {
            model = notebook;
            closed = onClosed;
            lessonOnly = false;
            reading = null;
            pendingLesson = null;
            tab = startTab;
            subject = PrimarySubject();
            Array.Clear(selection, 0, selection.Length);
            if (!string.IsNullOrEmpty(model?.FocusCode)) selection[(int)NotebookTab.Discoveries] = FindDiscovery(model.FocusCode);
            else selection[(int)NotebookTab.Discoveries] = FirstEnabled(model?.Discoveries);
            selection[(int)NotebookTab.MasterNotes] = FirstEnabled(model?.Lessons);
            selection[(int)NotebookTab.Mastery] = CurrentVillage();
            BeginOpen();
        }

        // ---------- ILessonView ----------

        public void Open(LessonNote lesson, LessonTexts texts, string finish, Action onFinished)
        {
            model = ViewLocator.Get<INotebookSource>()?.BuildModel() ?? new NotebookModel();
            lessonTexts = texts;
            closed = null;
            lessonOnly = true;
            lessonFromIndex = false;
            lessonFinished = onFinished;
            finishWord = finish;
            reading = lesson;
            pendingLesson = lesson;
            spread = 0;
            tab = NotebookTab.MasterNotes;
            subject = PrimarySubject();
            Array.Clear(selection, 0, selection.Length);
            selection[(int)NotebookTab.MasterNotes] = FindLesson(lesson);
            selection[(int)NotebookTab.Discoveries] = FirstEnabled(model.Discoveries);
            selection[(int)NotebookTab.Mastery] = CurrentVillage();
            BeginOpen();
        }

        public void Close()
        {
            if (!IsOpen || phase == Phase.Closing) return;
            phase = Phase.Closing;
            phaseTime = 0f;
        }

        private void FinishClose()
        {
            phase = Phase.Hidden;
            root.SetActive(false);
            Clear(leftCol);
            Clear(rightCol);
            var lessonCb = lessonOnly ? lessonFinished : null;
            var cb = closed;
            lessonFinished = null;
            closed = null;
            reading = null;
            lessonOnly = false;
            lessonCb?.Invoke();
            cb?.Invoke();
        }

        private void BeginOpen()
        {
            root.SetActive(true);
            FitBook();
            phase = Phase.Opening;
            phaseTime = 0f;
            openedAt = Time.unscaledTime;
            // Yön basılı değilse hemen kurulu: ilk basış yutulmasın. Basılıysa (D-pad ↑ ile açıldı) bırakınca kurulur.
            navArmed = navigate == null || !navigate.enabled || navigate.ReadValue<Vector2>().sqrMagnitude < 0.09f;
            EventSystem.current?.SetSelectedGameObject(null);
            if (Texts != null) coverTitle.text = Texts.DiscoveriesTab;
            Render();
            ApplyAnimation(0f);
        }

        // ---------- canlandırma ----------

        private void Update()
        {
            if (phase == Phase.Hidden) return;
            phaseTime += Time.unscaledDeltaTime;
            if (phase == Phase.Opening)
            {
                float total = riseTime + coverTime + settleTime;
                ApplyAnimation(Mathf.Clamp01(phaseTime / total));
                if (phaseTime >= total) phase = Phase.Open;
            }
            else if (phase == Phase.Closing)
            {
                float k = Mathf.Clamp01(phaseTime / closeTime);
                ApplyClosing(k);
                if (k >= 1f) FinishClose();
            }
        }

        /// <summary>t: 0 → 1 açılış. 0–rise: defter alttan eğik yükselir, kararma; rise–cover: kapak açılır; sonra yazı belirir.</summary>
        private void ApplyAnimation(float t)
        {
            float total = riseTime + coverTime + settleTime;
            float time = t * total;
            float rise = Ease(Mathf.Clamp01(time / riseTime));
            float cover = Ease(Mathf.Clamp01((time - riseTime) / coverTime));
            float settle = Ease(Mathf.Clamp01((time - riseTime - coverTime) / settleTime));

            bookRect.anchoredPosition = new Vector2(0f, bookOffsetY - riseDistance * (1f - rise));
            bookRect.localRotation = Quaternion.Euler(0f, 0f, riseTilt * (1f - rise));
            bookRect.localScale = Vector3.one * bookScale;
            SetScrim(scrimAlpha * rise);
            coverFlap.gameObject.SetActive(cover < 1f);
            coverFlap.localScale = new Vector3(1f - cover, 1f, 1f);
            contentGroup.alpha = settle;
            footerRoot.gameObject.SetActive(settle > 0.01f);
        }

        private void ApplyClosing(float k)
        {
            float e = Ease(k);
            contentGroup.alpha = 1f - Mathf.Clamp01(k * 3f);
            bookRect.anchoredPosition = new Vector2(0f, bookOffsetY - riseDistance * e);
            bookRect.localRotation = Quaternion.Euler(0f, 0f, riseTilt * e);
            SetScrim(scrimAlpha * (1f - e));
            footerRoot.gameObject.SetActive(false);
        }

        private static float Ease(float x) => 1f - (1f - x) * (1f - x);

        private void SetScrim(float a)
        {
            var c = scrim.color;
            c.a = a;
            scrim.color = c;
        }

        private void FitBook()
        {
            Canvas.ForceUpdateCanvases();
            var r = rootRect.rect;
            if (r.width <= 0f || r.height <= 0f) return;
            bookScale = Mathf.Min(1f, r.width / (bookSize.x + fitMargin.x), r.height / (bookSize.y + fitMargin.y));
            bookRect.localScale = Vector3.one * bookScale;
            float f = Mathf.Max(bookScale, 0.8f);
            footerRoot.localScale = new Vector3(f, f, 1f);
        }

        // ---------- girdi ----------

        private bool Ready => phase == Phase.Open && Time.unscaledTime - openedAt >= inputGuardSeconds;

        /// <summary>
        /// Yön girdisi (UI/Navigate) her değişimde: bırakınca (merkeze dönünce) yeniden kurulur, eşiği geçen ilk değer bir adım atar.
        /// Defteri açan basış (D-pad ↑) açılırken basılı kalsa da bırakılınca kurulur; açılış canlandırmasında adım atılmaz.
        /// </summary>
        private void OnNavigate(InputAction.CallbackContext ctx)
        {
            if (phase == Phase.Hidden) return;
            var v = ctx.ReadValue<Vector2>();
            if (v.sqrMagnitude < 0.09f)
            {
                navArmed = true;
                return;
            }
            if (!navArmed || !Ready) return;
            if (Mathf.Abs(v.x) >= 0.5f && Mathf.Abs(v.x) > Mathf.Abs(v.y))
            {
                navArmed = false;
                if (reading != null) TurnSpread(v.x > 0f ? 1 : -1);
                else if (tab != NotebookTab.Mastery) StepSubject(v.x > 0f ? 1 : -1);
            }
            else if (Mathf.Abs(v.y) >= 0.5f)
            {
                navArmed = false;
                if (reading == null) StepSelection(v.y > 0f ? -1 : 1);
            }
        }

        private void OnSubmit(InputAction.CallbackContext _)
        {
            if (Ready) Confirm();
        }

        private void OnCancel(InputAction.CallbackContext _)
        {
            if (Ready) Back();
        }

        private void OnPrevTab(InputAction.CallbackContext _)
        {
            if (Ready) StepTab(-1);
        }

        private void OnNextTab(InputAction.CallbackContext _)
        {
            if (Ready) StepTab(1);
        }

        private void Confirm()
        {
            if (reading != null)
            {
                if (spread < SpreadCount - 1) TurnSpread(1);
                else FinishReading();
                return;
            }
            if (tab == NotebookTab.MasterNotes)
            {
                var row = SelectedRow();
                if (row != null && row.Enabled && row.Lesson != null)
                {
                    reading = row.Lesson;
                    lessonTexts = model.LessonTexts;
                    lessonFromIndex = true;
                    spread = 0;
                    Render();
                }
            }
        }

        private void Back()
        {
            if (reading != null)
            {
                if (spread > 0)
                {
                    TurnSpread(-1);
                    return;
                }
                // Görev öncesi notun ilk sayfasında geri yok: önce okunmalı.
                if (lessonOnly && !lessonFromIndex) return;
                reading = null;
                if (lessonOnly)
                {
                    Close();
                    return;
                }
                Render();
                return;
            }
            // Görev öncesi kipte öteki ayraca geçen oyuncu Esc ile nota döner.
            if (lessonOnly && !lessonFromIndex)
            {
                SelectTab(NotebookTab.MasterNotes);
                return;
            }
            Close();
        }

        // Görev öncesi (ya da menüden) açılan not: Ustanın Notları ayracına dönülünce yine bu not açılır.
        private LessonNote pendingLesson;

        private void FinishReading()
        {
            if (lessonOnly && !lessonFromIndex)
            {
                Close();
                return;
            }
            reading = null;
            lessonFromIndex = false;
            Render();
        }

        private void StepTab(int delta) => SelectTab((NotebookTab)(((int)tab + delta + 3) % 3));

        private void SelectTab(NotebookTab t)
        {
            tab = t;
            if (lessonOnly)
            {
                // Görev öncesi not: Ustanın Notları'na dönünce kaldığı açılıştan sürer.
                lessonFromIndex = false;
                reading = tab == NotebookTab.MasterNotes ? pendingLesson : null;
            }
            else reading = null;
            Render();
        }

        private void StepSubject(int delta)
        {
            var subjects = Texts?.Subjects;
            if (subjects == null || subjects.Count == 0) return;
            subject = Mathf.Clamp(subject + delta, 0, subjects.Count - 1);
            selection[(int)tab] = 0;
            Render();
        }

        private void StepSelection(int delta)
        {
            int count = rows.Count;
            if (count == 0) return;
            int s = Mathf.Clamp(selection[(int)tab] + delta, 0, count - 1);
            if (s == selection[(int)tab]) return;
            selection[(int)tab] = s;
            Render();
        }

        private void TurnSpread(int delta)
        {
            int target = Mathf.Clamp(spread + delta, 0, Mathf.Max(0, SpreadCount - 1));
            if (target == spread) return;
            spread = target;
            Render();
        }

        // ---------- veri yardımcıları ----------

        private int PrimarySubject()
        {
            var subjects = Texts?.Subjects;
            if (subjects == null) return 0;
            for (int i = 0; i < subjects.Count; i++)
                if (subjects[i].Primary) return i;
            return 0;
        }

        private NotebookSubject CurrentSubject =>
            Texts != null && Texts.Subjects.Count > 0 ? Texts.Subjects[Mathf.Clamp(subject, 0, Texts.Subjects.Count - 1)] : null;

        private static int FirstEnabled(List<NotebookGroup> groups)
        {
            if (groups == null) return 0;
            int i = 0, last = 0;
            foreach (var g in groups)
                foreach (var r in g.Rows)
                {
                    if (r.Enabled) last = i;
                    i++;
                }
            return last;
        }

        private int FindDiscovery(string code)
        {
            int i = 0;
            foreach (var g in model.Discoveries)
                foreach (var r in g.Rows)
                {
                    if (r.Code == code) return i;
                    i++;
                }
            return 0;
        }

        private int FindLesson(LessonNote lesson)
        {
            int i = 0;
            foreach (var g in model.Lessons)
                foreach (var r in g.Rows)
                {
                    if (r.Lesson == lesson) return i;
                    i++;
                }
            return 0;
        }

        private int CurrentVillage()
        {
            if (model == null) return 0;
            for (int i = 0; i < model.Villages.Count; i++)
            {
                var v = model.Villages[i];
                if (v.Done < v.Tasks.Count) return i;
            }
            return Mathf.Max(0, model.Villages.Count - 1);
        }

        /// <summary>Şu anki ayraç + ders şeridine göre sol sayfadaki gruplar.</summary>
        private List<NotebookGroup> CurrentGroups()
        {
            var subj = CurrentSubject;
            if (model == null || subj == null || subj.Locked) return null;
            if (tab == NotebookTab.Discoveries)
            {
                if (subj.Primary) return model.Discoveries;
                return model.FieldNotesBySubject.TryGetValue(subj.Name, out var g) ? g : null;
            }
            if (tab == NotebookTab.MasterNotes) return subj.Primary ? model.Lessons : null;
            return null;
        }

        private NotebookRow SelectedRow()
        {
            var groups = CurrentGroups();
            if (groups == null) return null;
            int target = selection[(int)tab], i = 0;
            foreach (var g in groups)
                foreach (var r in g.Rows)
                {
                    if (i == target) return r;
                    i++;
                }
            return null;
        }

        // ---------- çizim ----------

        private void Render()
        {
            var th = Theme;
            var texts = Texts;
            // ayraçlar
            for (int i = 0; i < 3; i++)
            {
                bool on = (int)tab == i;
                tabImages[i].color = on ? th.Paper : th.PaperMid;
                tabLabels[i].text = texts != null ? texts.TabTitle((NotebookTab)i) : string.Empty;
                tabLabels[i].font = on ? th.HeadingFont : th.BodyFont;
                tabLabels[i].color = on ? th.InkDeep : th.Ink;
            }
            // Ayraçlar yan yana, yazı genişliğine göre (Figma: ilk x 90, aralık ~24).
            float x = 90f;
            for (int i = 0; i < 3; i++)
            {
                var rt = tabImages[i].rectTransform;
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                rt.anchoredPosition = new Vector2(x, (int)tab == i ? 46f : 40f);
                x += LayoutUtility.GetPreferredWidth(rt) + 24f;
            }
            RenderRibbons();

            Clear(leftCol);
            Clear(rightCol);
            rows.Clear();
            if (reading != null) RenderLesson();
            else if (tab == NotebookTab.Mastery) RenderMastery();
            else RenderIndex();
            Fit(leftCol, leftColumn.y);
            Fit(rightCol, rightColumn.y);
            RenderFooter();
        }

        private void RenderRibbons()
        {
            var subjects = Texts?.Subjects;
            int count = subjects?.Count ?? 0;
            for (int i = 0; i < ribbonImages.Count; i++)
            {
                bool show = i < count && tab != NotebookTab.Mastery;
                ribbonImages[i].gameObject.SetActive(show);
                if (!show) continue;
                var s = subjects[i];
                bool on = i == subject;
                var c = s.Color;
                ribbonImages[i].color = s.Locked ? Color.Lerp(c, Theme.WoodDark, 0.55f) : on ? c : Color.Lerp(c, Theme.PaperMid, 0.35f);
                var rt = ribbonImages[i].rectTransform;
                rt.sizeDelta = new Vector2(on ? 214f : 190f, 53f);
                ribbonLabels[i].text = s.Name;
                ribbonLabels[i].color = s.Locked ? Theme.Paper : Theme.InkDeep;
            }
        }

        // ----- içindekiler + kayıt (Ustanın Notları, Keşiflerim) -----

        private void RenderIndex()
        {
            var th = Theme;
            var texts = Texts;
            var subj = CurrentSubject;
            if (texts == null || subj == null) return;

            // Sol başlık: ders adı + sayaç
            var head = Row(leftCol, "Head", 16f, TextAnchor.LowerLeft);
            Label(head, subj.Name, th.HeadingFont, 56f, th.InkDeep, FontStyles.Bold, true);
            string counter = !subj.Primary || subj.Locked ? string.Empty
                : tab == NotebookTab.Discoveries ? string.Format(texts.PagesWritten, model.WrittenCount)
                : string.Format(texts.NotesOpened, model.OpenedLessonCount);
            if (!string.IsNullOrEmpty(counter)) Label(head, counter, th.BodyFont, 26f, th.PaperShade, FontStyles.Normal, true);

            if (subj.Locked)
            {
                Para(rightCol, "Locked", texts.LockedSubject, th.BodyFont, 31f, th.PaperShade, FontStyles.Italic, 12f);
                return;
            }

            var groups = CurrentGroups();
            if (groups == null || groups.Count == 0)
            {
                string empty = tab == NotebookTab.MasterNotes ? (subj.Primary ? texts.NotesEmpty : texts.FieldNotesEmpty) : texts.FieldNotesEmpty;
                Para(rightCol, "Empty", empty, th.BodyFont, 31f, th.PaperShade, FontStyles.Italic, 12f);
                return;
            }
            if (!subj.Primary) Para(leftCol, "Sub", string.Format(texts.FieldNotesTitle, subj.Name), th.BodyFont, 26f, th.PaperShade, FontStyles.Normal, 0f);

            int index = 0;
            int selected = Mathf.Clamp(selection[(int)tab], 0, CountRows(groups) - 1);
            selection[(int)tab] = selected;
            NotebookRow current = null;
            foreach (var g in groups)
            {
                var gh = Row(leftCol, "Group", 12f, TextAnchor.LowerLeft);
                Label(gh, g.Title, th.HeadingFont, 30f, th.Copper, FontStyles.Bold, true);
                if (!string.IsNullOrEmpty(g.Count)) Label(gh, g.Count, th.BodyFont, 26f, th.PaperShade, FontStyles.Normal, true);
                foreach (var r in g.Rows)
                {
                    bool sel = index == selected;
                    if (sel) current = r;
                    ListRow(r, sel, index);
                    rows.Add(new SelectableRow { Index = index });
                    index++;
                }
            }
            if (current == null) return;
            if (tab == NotebookTab.MasterNotes) RenderNoteDetail(current);
            else if (subj.Primary) RenderDiscoveryDetail(current);
            else RenderFieldDetail(current, subj);
        }

        private static int CountRows(List<NotebookGroup> groups)
        {
            int n = 0;
            foreach (var g in groups) n += g.Rows.Count;
            return Mathf.Max(1, n);
        }

        private void ListRow(NotebookRow r, bool selected, int index)
        {
            var th = Theme;
            var row = Row(leftCol, "Row_" + r.Code, 18f, TextAnchor.MiddleLeft);
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(14, 10, 5, 5);
            var bg = row.gameObject.AddComponent<Image>();
            bg.color = selected ? UiKit.WithAlpha(th.Gold, 0.22f) : new Color(0f, 0f, 0f, 0f);
            bg.raycastTarget = true;
            if (selected)
            {
                var bar = UiKit.Img(row, "Bar", th.Copper);
                var le = bar.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = le.minWidth = 4f;
                le.preferredHeight = 40f;
            }
            var code = Label(row, r.Code, th.BodyFont, 26f, r.Enabled ? th.Copper : th.PaperShade, FontStyles.Bold, true);
            code.GetComponent<LayoutElement>().minWidth = 52f;
            Label(row, r.Title, r.Enabled ? th.HeadingFont : th.BodyFont, r.Enabled ? 31f : 29f,
                r.Enabled ? th.InkDeep : th.PaperShade, r.Enabled ? FontStyles.Italic : FontStyles.Normal, false);
            AddClick(row.gameObject, () =>
            {
                if (selection[(int)tab] == index) Confirm();
                else
                {
                    selection[(int)tab] = index;
                    Render();
                }
            });
        }

        private void RenderDiscoveryDetail(NotebookRow r)
        {
            var th = Theme;
            var texts = Texts;
            var head = Row(rightCol, "Head", 14f, TextAnchor.LowerLeft);
            Label(head, r.Kicker, th.BodyFont, 28f, th.Copper, FontStyles.Bold, true);
            Label(head, r.Title, th.HeadingFont, 46f, th.InkDeep, FontStyles.Bold, false);
            Para(rightCol, "Place", r.Subtitle, th.BodyFont, 26f, th.PaperShade, FontStyles.Normal, 0f);
            Para(rightCol, "Writing", r.Body, th.BodyFont, 33f, r.Enabled ? th.InkDeep : th.PaperShade, FontStyles.Italic, 12f);
            if (!string.IsNullOrEmpty(r.Equation))
            {
                var eq = Row(rightCol, "Equation", 18f, TextAnchor.MiddleLeft);
                var icon = th.IconFor(r.IconFormula);
                if (icon != null)
                {
                    var img = UiKit.Img(eq, "Icon", Color.white, icon);
                    img.preserveAspect = true;
                    var le = img.gameObject.AddComponent<LayoutElement>();
                    le.preferredWidth = le.preferredHeight = 64f;
                }
                Label(eq, r.Equation, th.HeadingFont, 38f, th.Ink, FontStyles.Bold, true);
            }
            if (r.FieldNotes.Count > 0)
            {
                Rule(rightCol);
                Para(rightCol, "Other", texts.OtherSubjects, th.HeadingFont, 30f, th.Copper, FontStyles.Bold, 0f);
                foreach (var n in r.FieldNotes) BarNote(rightCol, n.Heading, n.Text, SubjectColor(n.Subject), th.Ink);
            }
            if (!string.IsNullOrEmpty(r.Footer)) Para(rightCol, "Footer", r.Footer, th.BodyFont, 26f, th.PaperShade, FontStyles.Normal, 0f);
        }

        private void RenderFieldDetail(NotebookRow r, NotebookSubject subj)
        {
            var th = Theme;
            var head = Row(rightCol, "Head", 14f, TextAnchor.LowerLeft);
            Label(head, r.Code, th.BodyFont, 28f, th.Copper, FontStyles.Bold, true);
            Label(head, r.Title, th.HeadingFont, 46f, th.InkDeep, FontStyles.Bold, false);
            BarNote(rightCol, r.Kicker, r.Body, subj.Color, th.Ink);
        }

        private void RenderNoteDetail(NotebookRow r)
        {
            var th = Theme;
            var texts = Texts;
            var kick = Row(rightCol, "Kicker", 14f, TextAnchor.LowerLeft);
            Label(kick, r.Kicker, th.BodyFont, 28f, th.Copper, FontStyles.Bold, true);
            Label(kick, r.Subtitle, th.BodyFont, 28f, th.PaperShade, FontStyles.Normal, true);
            Para(rightCol, "Title", r.Title, th.HeadingFont, 52f, r.Enabled ? th.InkDeep : th.PaperShade, FontStyles.Bold, -8f);
            if (!r.Enabled || r.Lesson == null) return;
            Para(rightCol, "Body", r.Body, th.BodyFont, 31f, th.InkDeep, FontStyles.Normal, 12f);
            Rule(rightCol);
            Para(rightCol, "InThis", texts.InThisNote, th.HeadingFont, 30f, th.Copper, FontStyles.Bold, 0f);
            for (int p = 0; p < r.Lesson.Pages.Count; p++)
            {
                string title = FirstOf(r.Lesson.Pages[p], LessonBlockKind.Title);
                if (string.IsNullOrEmpty(title)) title = FirstOf(r.Lesson.Pages[p], LessonBlockKind.Equation);
                if (string.IsNullOrEmpty(title)) title = FirstOf(r.Lesson.Pages[p], LessonBlockKind.Subhead);
                if (string.IsNullOrEmpty(title)) continue;
                var line = Row(rightCol, "Section", 0f, TextAnchor.MiddleLeft);
                var l = Label(line, texts.SectionLabel(p), th.BodyFont, 26f, th.PaperShade, FontStyles.Bold, true);
                l.GetComponent<LayoutElement>().preferredWidth = 210f;
                Label(line, title, th.HeadingFont, 30f, th.InkDeep, FontStyles.Italic, false);
            }
            Rule(rightCol);
            Para(rightCol, "Hint", texts.ReadHint, th.BodyFont, 26f, th.PaperShade, FontStyles.Normal, 0f);
        }

        private static string FirstOf(LessonPage page, LessonBlockKind kind)
        {
            if (page == null) return null;
            foreach (var b in page.Blocks)
                if (b != null && b.Kind == kind && !string.IsNullOrEmpty(b.Text)) return b.Text;
            return null;
        }

        private Color SubjectColor(string name)
        {
            var subjects = Texts?.Subjects;
            if (subjects != null)
                foreach (var s in subjects)
                    if (s.Name == name) return s.Color;
            return Theme.Copper;
        }

        // ----- Ustalık -----

        private void RenderMastery()
        {
            var th = Theme;
            var texts = Texts;
            if (texts == null) return;
            var head = Row(leftCol, "Head", 16f, TextAnchor.LowerLeft);
            Label(head, texts.MasteryTitle, th.HeadingFont, 56f, th.InkDeep, FontStyles.Bold, true);
            Label(head, texts.MasterySubtitle, th.BodyFont, 26f, th.PaperShade, FontStyles.Normal, true);
            foreach (var kv in model.MasteryStats)
            {
                var line = Row(leftCol, "Stat", 0f, TextAnchor.LowerLeft);
                var l = Label(line, kv.Key, th.BodyFont, 29f, th.Ink, FontStyles.Normal, true);
                l.GetComponent<LayoutElement>().preferredWidth = 270f;
                Label(line, kv.Value, th.HeadingFont, 34f, th.InkDeep, FontStyles.Bold, true);
            }
            Rule(leftCol);
            Para(leftCol, "Villages", texts.VillagesTitle, th.HeadingFont, 30f, th.Copper, FontStyles.Bold, 0f);

            int selected = Mathf.Clamp(selection[(int)NotebookTab.Mastery], 0, Mathf.Max(0, model.Villages.Count - 1));
            selection[(int)NotebookTab.Mastery] = selected;
            for (int i = 0; i < model.Villages.Count; i++)
            {
                var v = model.Villages[i];
                int index = i;
                var row = Row(leftCol, "Village", 14f, TextAnchor.MiddleLeft);
                row.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(14, 10, 8, 8);
                var bg = row.gameObject.AddComponent<Image>();
                bg.color = i == selected ? UiKit.WithAlpha(th.Gold, 0.22f) : new Color(0f, 0f, 0f, 0f);
                var t = Label(row, v.Title, th.HeadingFont, 30f, th.InkDeep, FontStyles.Italic, true);
                t.GetComponent<LayoutElement>().preferredWidth = 290f;
                var seals = Row(row, "Seals", 8f, TextAnchor.MiddleLeft);
                foreach (var task in v.Tasks) Seal(seals, task.State, 26f);
                Label(row, v.Done + "/" + v.Tasks.Count, th.BodyFont, 26f, th.PaperShade, FontStyles.Normal, true);
                rows.Add(new SelectableRow { Index = i });
                AddClick(row.gameObject, () =>
                {
                    selection[(int)NotebookTab.Mastery] = index;
                    Render();
                });
            }
            if (model.Villages.Count == 0) return;

            var village = model.Villages[selected];
            var kick = Row(rightCol, "Kicker", 14f, TextAnchor.LowerLeft);
            Label(kick, texts.VillageKicker, th.BodyFont, 28f, th.Copper, FontStyles.Bold, true);
            Label(kick, texts.VillageDetail, th.BodyFont, 28f, th.PaperShade, FontStyles.Normal, true);
            Para(rightCol, "Title", village.Title, th.HeadingFont, 48f, th.InkDeep, FontStyles.Bold, -8f);
            foreach (var task in village.Tasks)
            {
                var row = Row(rightCol, "Task", 18f, TextAnchor.MiddleLeft);
                Seal(row, task.State, 28f);
                var col = UiKit.Rect(row, "Text");
                var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
                v.childControlWidth = v.childControlHeight = true;
                v.childForceExpandWidth = true;
                v.childForceExpandHeight = false;
                col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                Para(col, "Name", task.Title, th.HeadingFont, 32f, task.State == MasteryState.Locked ? th.PaperShade : th.InkDeep, FontStyles.Bold, 0f);
                Para(col, "Detail", task.Detail, th.BodyFont, 26f, th.PaperShade, FontStyles.Normal, 0f);
            }
            Rule(rightCol);
            var legend = Row(rightCol, "Legend", 22f, TextAnchor.MiddleLeft);
            LegendItem(legend, MasteryState.Done, texts.StateDone);
            LegendItem(legend, MasteryState.Next, texts.StateNext);
            LegendItem(legend, MasteryState.Locked, texts.StateLocked);
            Para(rightCol, "Rule", texts.VillageRule, th.BodyFont, 26f, th.PaperShade, FontStyles.Normal, 0f);
        }

        private void LegendItem(RectTransform parent, MasteryState state, string label)
        {
            var r = Row(parent, "Item", 8f, TextAnchor.MiddleLeft);
            Seal(r, state, 24f);
            Label(r, label, Theme.BodyFont, 26f, Theme.Ink, FontStyles.Normal, true);
        }

        private void Seal(RectTransform parent, MasteryState state, float size)
        {
            var th = Theme;
            var sprite = state == MasteryState.Done ? UiKit.Disc : state == MasteryState.Next ? UiKit.Ring : UiKit.RingDashed;
            var color = state == MasteryState.Done ? th.WaxMid : state == MasteryState.Next ? th.Gold : th.PaperShade;
            var img = UiKit.Img(parent, "Seal", color, sprite);
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = le.minWidth = le.minHeight = size;
        }

        // ----- Ders notu okuma -----

        private void RenderLesson()
        {
            Fill(leftCol, spread * 2);
            Fill(rightCol, spread * 2 + 1);
        }

        private void Fill(RectTransform column, int pageIndex)
        {
            if (reading == null || pageIndex >= reading.Pages.Count || reading.Pages[pageIndex] == null) return;
            var blocks = reading.Pages[pageIndex].Blocks;
            for (int i = 0; i < blocks.Count; i++)
                if (blocks[i] != null) MakeBlock(column, blocks[i]);
        }

        private void MakeBlock(RectTransform column, LessonBlock b)
        {
            var th = Theme;
            switch (b.Kind)
            {
                case LessonBlockKind.Kicker:
                {
                    var row = Row(column, "Kicker", 14f, TextAnchor.LowerLeft);
                    Label(row, b.Text, th.BodyFont, 28f, th.Copper, FontStyles.Bold, true);
                    if (!string.IsNullOrEmpty(b.Detail)) Label(row, b.Detail, th.BodyFont, 28f, th.PaperShade, FontStyles.Normal, true);
                    break;
                }
                case LessonBlockKind.Title:
                    Para(column, "Title", b.Text, th.HeadingFont, 52f, th.InkDeep, FontStyles.Bold, -8f);
                    break;
                case LessonBlockKind.Subhead:
                    Para(column, "Subhead", b.Text, th.HeadingFont, 30f, th.Copper, FontStyles.Bold, 0f);
                    break;
                case LessonBlockKind.Body:
                    Para(column, "Body", b.Text, th.BodyFont, 31f, th.InkDeep, FontStyles.Normal, 12f);
                    break;
                case LessonBlockKind.Equation:
                    Para(column, "Equation", b.Text, th.HeadingFont, 40f, th.Ink, FontStyles.Bold, 0f);
                    break;
                case LessonBlockKind.Footnote:
                    Para(column, "Footnote", b.Text, th.BodyFont, 26f, th.PaperShade, FontStyles.Normal, 0f);
                    break;
                case LessonBlockKind.Rule:
                    Rule(column);
                    break;
                case LessonBlockKind.Steps:
                    for (int i = 0; i < b.Items.Count; i++)
                    {
                        var row = Row(column, "Step_" + (i + 1), 18f, TextAnchor.UpperLeft);
                        var num = Label(row, (i + 1).ToString(), th.HeadingFont, 34f, th.Copper, FontStyles.Bold, true);
                        num.GetComponent<LayoutElement>().preferredWidth = 30f;
                        num.margin = new Vector4(0f, -7f, 0f, 0f);
                        var t = Label(row, b.Items[i], th.BodyFont, 30f, th.InkDeep, FontStyles.Normal, false);
                        t.lineSpacing = 10f;
                    }
                    break;
                case LessonBlockKind.Note:
                    BarNote(column, b.Detail, b.Text, th.Copper, th.InkDeep);
                    break;
                case LessonBlockKind.Warning:
                    BarNote(column, b.Detail, b.Text, th.WaxMid, th.InkDeep);
                    break;
                case LessonBlockKind.Counts:
                    MakeCounts(column, b);
                    break;
            }
        }

        private static readonly float[] CountColumns = { 130f, 170f, 170f, 150f };

        private void MakeCounts(RectTransform column, LessonBlock b)
        {
            var th = Theme;
            var lt = lessonTexts ?? model?.LessonTexts;
            if (lt == null) return;
            var box = UiKit.Rect(column, "Counts");
            var v = box.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = false;
            v.childForceExpandHeight = false;
            var head = Row(box, "Header", 0f, TextAnchor.LowerLeft);
            Cell(head, lt.ElementHeader, th.BodyFont, 26f, th.PaperShade, FontStyles.Bold, 0);
            Cell(head, lt.LeftHeader, th.BodyFont, 26f, th.PaperShade, FontStyles.Bold, 1);
            Cell(head, lt.RightHeader, th.BodyFont, 26f, th.PaperShade, FontStyles.Bold, 2);
            Cell(head, string.Empty, th.BodyFont, 26f, th.PaperShade, FontStyles.Normal, 3);
            for (int i = 0; i < b.Counts.Count; i++)
            {
                var c = b.Counts[i];
                var row = Row(box, "Row_" + c.Element, 0f, TextAnchor.MiddleLeft);
                Cell(row, c.Element, th.HeadingFont, 32f, th.InkDeep, FontStyles.Bold, 0);
                Cell(row, c.Left.ToString(), th.HeadingFont, 32f, th.InkDeep, FontStyles.Normal, 1);
                Cell(row, c.Right.ToString(), th.HeadingFont, 32f, th.InkDeep, FontStyles.Normal, 2);
                Cell(row, c.Balanced ? lt.EqualWord : lt.NotEqualWord, th.BodyFont, 28f, c.Balanced ? th.Copper : th.WaxMid, FontStyles.Bold, 3);
            }
        }

        private static void Cell(RectTransform row, string text, TMP_FontAsset font, float size, Color color, FontStyles style, int col)
        {
            var t = Label(row, text, font, size, color, style, true);
            t.GetComponent<LayoutElement>().preferredWidth = CountColumns[col];
        }

        // ----- alt satır -----

        private void RenderFooter()
        {
            var texts = Texts;
            var lt = lessonTexts ?? model?.LessonTexts;
            for (int i = 0; i < hints.Count; i++) hints[i].Root.SetActive(false);
            if (texts == null) return;
            int h = 0;
            if (reading != null && lt != null)
            {
                SetHint(h++, PromptAction.UiHorizontal, null, string.Format(lt.PageFormat, spread + 1, Mathf.Max(1, SpreadCount)));
                bool last = spread >= SpreadCount - 1;
                string go = !last ? lt.NextWord : lessonOnly && !lessonFromIndex ? finishWord : texts.BackWord;
                SetHint(h++, PromptAction.Confirm, null, go);
                SetHint(h++, PromptAction.PrevTab, PromptAction.NextTab, texts.TabsWord);
            }
            else
            {
                SetHint(h++, PromptAction.PrevTab, PromptAction.NextTab, texts.TabsWord);
                if (tab != NotebookTab.Mastery) SetHint(h++, PromptAction.UiHorizontal, null, texts.SubjectWord);
                SetHint(h++, PromptAction.UiVertical, null, texts.SelectWord);
                if (tab == NotebookTab.MasterNotes) SetHint(h++, PromptAction.Confirm, null, texts.ReadWord);
                if (!(lessonOnly && !lessonFromIndex)) SetHint(h++, PromptAction.Cancel, null, texts.CloseWord);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(footerRoot);
        }

        private void SetHint(int i, PromptAction a, PromptAction? b, string label)
        {
            if (i >= hints.Count) return;
            var hint = hints[i];
            hint.Root.SetActive(true);
            UiKit.SetKeyCap(hint.Key1, hint.Key1Root, ControlGlyphs.For(a), 44f);
            hint.Key2Root.gameObject.SetActive(b.HasValue);
            if (b.HasValue) UiKit.SetKeyCap(hint.Key2, hint.Key2Root, ControlGlyphs.For(b.Value), 44f);
            hint.Label.text = label;
        }

        private void OnScheme(ControlScheme _)
        {
            if (IsOpen) RenderFooter();
        }

        // ---------- küçük yapı taşları ----------

        private void Fit(RectTransform column, float width)
        {
            column.localScale = Vector3.one;
            column.sizeDelta = new Vector2(width, bookSize.y - columnTop - columnBottom);
            LayoutRebuilder.ForceRebuildLayoutImmediate(column);
            float available = column.sizeDelta.y;
            float needed = LayoutUtility.GetPreferredHeight(column);
            if (needed <= available || needed <= 0f) return;
            float s = Mathf.Max(0.72f, available / needed);
            column.localScale = new Vector3(s, s, 1f);
            column.sizeDelta = new Vector2(width / s, available / s);
            LayoutRebuilder.ForceRebuildLayoutImmediate(column);
        }

        private static void Clear(RectTransform column)
        {
            for (int i = column.childCount - 1; i >= 0; i--)
            {
                var child = column.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private void Rule(RectTransform parent)
        {
            var r = UiKit.Img(parent, "Rule", UiKit.WithAlpha(Theme.Copper, 0.35f));
            var le = r.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = le.minHeight = 2f;
        }

        private void BarNote(RectTransform parent, string head, string body, Color accent, Color ink)
        {
            var th = Theme;
            var row = Row(parent, "Note", 16f, TextAnchor.UpperLeft);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            // Çizgi metin boyunca uzasın ama satır sütunun boş yerini kaplamasın.
            row.gameObject.AddComponent<LayoutElement>().flexibleHeight = 0f;
            var bar = UiKit.Img(row, "Bar", accent);
            var ble = bar.gameObject.AddComponent<LayoutElement>();
            ble.preferredWidth = ble.minWidth = 6f;
            var col = UiKit.Rect(row, "Text");
            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            if (!string.IsNullOrEmpty(head)) Para(col, "Head", head, th.BodyFont, 26f, accent == th.Copper || accent == th.WaxMid ? accent : th.Ink, FontStyles.Bold, 0f);
            Para(col, "Body", body, th.BodyFont, 29f, ink, FontStyles.Italic, 8f);
        }

        private static RectTransform Row(RectTransform parent, string name, float spacing, TextAnchor align)
        {
            var row = UiKit.Rect(parent, name);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            return row;
        }

        private static TextMeshProUGUI Label(RectTransform row, string text, TMP_FontAsset font, float size, Color color,
            FontStyles style, bool fit)
        {
            var t = UiKit.Text(row, "Text", font, size, color, TextAlignmentOptions.TopLeft, style);
            t.text = text;
            var le = t.gameObject.AddComponent<LayoutElement>();
            if (fit) t.textWrappingMode = TextWrappingModes.NoWrap;
            else le.flexibleWidth = 1f;
            return t;
        }

        private static TextMeshProUGUI Para(RectTransform column, string name, string text, TMP_FontAsset font, float size,
            Color color, FontStyles style, float lineSpacing)
        {
            var t = UiKit.Text(column, name, font, size, color, TextAlignmentOptions.TopLeft, style);
            t.text = text;
            t.lineSpacing = lineSpacing;
            return t;
        }

        private void AddClick(GameObject go, Action action)
        {
            if (go.GetComponent<Graphic>() == null)
            {
                var hit = go.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f);
            }
            var b = go.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            b.onClick.AddListener(() =>
            {
                if (Ready) action();
            });
        }

        // ---------- kurulum ----------

        private void Build()
        {
            var th = Theme;
            var rootRt = UiKit.Fill(UiKit.Rect(transform, "Notebook"));
            root = rootRt.gameObject;
            rootRect = rootRt;
            var canvas = root.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            root.AddComponent<GraphicRaycaster>();
            scrim = UiKit.Img(rootRt, "Scrim", UiKit.WithAlpha(th.InkDeep, 0f), null, true);
            UiKit.Fill(scrim.rectTransform);

            var book = UiKit.Rect(rootRt, "Book");
            UiKit.Place(book, new Vector2(0.5f, 0.5f), new Vector2(0f, bookOffsetY), bookSize);
            bookRect = book;

            // Ayraçlar: cildin arkasında, üst kenardan taşar (Figma: x 90 / 330 / 540, y −38).
            float[] tabX = { 90f, 330f, 540f };
            for (int i = 0; i < 3; i++)
            {
                var tab = UiKit.Img(book, "Tab_" + i, th.PaperMid, UiKit.Round8, true);
                UiKit.Place(tab.rectTransform, new Vector2(0f, 1f), new Vector2(tabX[i], 38f), new Vector2(0f, 76f));
                var row = tab.gameObject.AddComponent<HorizontalLayoutGroup>();
                row.padding = new RectOffset(26, 26, 12, 34);
                row.childControlWidth = row.childControlHeight = true;
                tab.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                var label = UiKit.Text(tab.transform, "Label", th.BodyFont, 28f, th.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                tabImages[i] = tab;
                tabLabels[i] = label;
                int index = i;
                AddClick(tab.gameObject, () => SelectTab((NotebookTab)index));
            }

            // Ders şeritleri: cildin arkasında sağ kenardan taşar (Figma: x 1536, y 130 + 74·i).
            ribbonsRoot = UiKit.Rect(book, "Ribbons");
            UiKit.Fill(ribbonsRoot);
            for (int i = 0; i < 6; i++)
            {
                var r = UiKit.Img(ribbonsRoot, "Ribbon_" + i, th.Copper, UiKit.Round4, true);
                var rt = r.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(bookSize.x - 24f, -(130f + 74f * i));
                rt.sizeDelta = new Vector2(166f, 53f);
                var label = UiKit.Text(r.transform, "Label", th.BodyFont, 24f, th.InkDeep, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                UiKit.Fill(label.rectTransform, 40f, 0f, 8f, 0f);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                ribbonImages.Add(r);
                ribbonLabels.Add(label);
                int index = i;
                AddClick(r.gameObject, () =>
                {
                    if (reading != null || tab == NotebookTab.Mastery) return;
                    subject = index;
                    selection[(int)tab] = 0;
                    Render();
                });
            }

            var shadow = UiKit.Img(book, "Shadow", UiKit.WithAlpha(th.InkDeep, 0.55f), UiKit.Round8);
            UiKit.Fill(shadow.rectTransform, 0, 14, 0, -14);
            var cover = UiKit.Img(book, "Cover", th.WoodDark, UiKit.Round8);
            UiKit.Fill(cover.rectTransform);
            var grain = UiKit.Img(cover.transform, "Grain", UiKit.WithAlpha(th.Wood, 0.45f), UiKit.WoodGrain);
            UiKit.Fill(grain.rectTransform);

            float half = bookSize.x * 0.5f;
            var leftPage = UiKit.Img(cover.transform, "PageLeft", th.Paper, UiKit.Round8, true);
            UiKit.Fill(leftPage.rectTransform, 22f, 22f, half + 2f, 30f);
            var rightPage = UiKit.Img(cover.transform, "PageRight", th.Paper, UiKit.Round8, true);
            UiKit.Fill(rightPage.rectTransform, half + 2f, 22f, 22f, 30f);
            AddClick(leftPage.gameObject, () =>
            {
                if (reading != null) Back();
            });
            AddClick(rightPage.gameObject, () =>
            {
                if (reading != null) Confirm();
            });
            Spine(leftPage.transform, th, false);
            Spine(rightPage.transform, th, true);

            var ribbon = UiKit.Img(book, "Bookmark", th.WaxMid);
            UiKit.Place(ribbon.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -22f), new Vector2(30f, 50f));

            var content = UiKit.Fill(UiKit.Rect(book, "Content"));
            contentGroup = content.gameObject.AddComponent<CanvasGroup>();
            leftCol = Column(content, "LeftColumn", leftColumn);
            rightCol = Column(content, "RightColumn", rightColumn);

            // Kapak kanadı: sağ yarıyı örter, sırttan sola doğru açılır (Figma: Defter/3 Kapak açılıyor).
            coverFlap = UiKit.Rect(book, "CoverFlap");
            coverFlap.anchorMin = new Vector2(0.5f, 0f);
            coverFlap.anchorMax = new Vector2(1f, 1f);
            coverFlap.pivot = new Vector2(0f, 0.5f);
            coverFlap.offsetMin = new Vector2(-6f, -6f);
            coverFlap.offsetMax = new Vector2(6f, 6f);
            var flap = UiKit.Img(coverFlap, "Leather", th.WoodDark, UiKit.Round8);
            UiKit.Fill(flap.rectTransform);
            var flapGrain = UiKit.Img(flap.transform, "Grain", UiKit.WithAlpha(th.Wood, 0.5f), UiKit.WoodGrain);
            UiKit.Fill(flapGrain.rectTransform);
            var plate = UiKit.Img(flap.transform, "Label", th.PaperMid, UiKit.Round4);
            UiKit.Place(plate.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 140f));
            coverTitle = UiKit.Text(plate.transform, "Title", th.HeadingFont, 44f, th.InkDeep, TextAlignmentOptions.Center, FontStyles.Italic);
            UiKit.Fill(coverTitle.rectTransform, 10f, 10f, 10f, 10f);

            // Alt satır: tuş ipuçları (en çok beş grup).
            footerRoot = UiKit.Rect(rootRt, "Footer");
            UiKit.Place(footerRoot, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(0f, 48f));
            var frow = footerRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            frow.spacing = 36f;
            frow.childAlignment = TextAnchor.MiddleCenter;
            frow.childControlWidth = frow.childControlHeight = true;
            frow.childForceExpandWidth = frow.childForceExpandHeight = false;
            footerRoot.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            for (int i = 0; i < 5; i++)
            {
                var g = UiKit.Rect(footerRoot, "Hint_" + i);
                var h = g.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 10f;
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = h.childControlHeight = true;
                h.childForceExpandWidth = h.childForceExpandHeight = false;
                var hint = new Hint { Root = g.gameObject };
                hint.Key1 = UiKit.KeyCap(g, th, 44f, out hint.Key1Root);
                hint.Key2 = UiKit.KeyCap(g, th, 44f, out hint.Key2Root);
                hint.Label = UiKit.Text(g, "Label", th.BodyFont, 28f, th.Paper, TextAlignmentOptions.MidlineLeft);
                hint.Label.textWrappingMode = TextWrappingModes.NoWrap;
                hints.Add(hint);
            }

            root.SetActive(false);
        }

        private RectTransform Column(RectTransform parent, string name, Vector2 xAndWidth)
        {
            var col = UiKit.Rect(parent, name);
            col.anchorMin = col.anchorMax = col.pivot = new Vector2(0f, 1f);
            col.anchoredPosition = new Vector2(xAndWidth.x, -columnTop);
            col.sizeDelta = new Vector2(xAndWidth.y, bookSize.y - columnTop - columnBottom);
            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 16f;
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return col;
        }

        private static void Spine(Transform page, UiTheme th, bool rightSide)
        {
            var s = UiKit.Img(page, "Spine", UiKit.WithAlpha(th.InkDeep, 0.22f), UiKit.FadeLeft);
            var rt = s.rectTransform;
            rt.anchorMin = new Vector2(rightSide ? 0f : 1f, 0f);
            rt.anchorMax = new Vector2(rightSide ? 0f : 1f, 1f);
            rt.pivot = new Vector2(rightSide ? 0f : 1f, 0.5f);
            rt.sizeDelta = new Vector2(70f, 0f);
            rt.anchoredPosition = Vector2.zero;
            if (!rightSide) rt.localScale = new Vector3(-1f, 1f, 1f);
        }
    }
}
