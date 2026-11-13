using System;
using System.Collections.Generic;
using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Esc menüsü + başarım ağacı — Figma: Ekranlar (v2) / Ekran/Duraklatma ve Keşif Defteri ve Başarılar / Başarılar/Kimya.
    /// Ana sayfa: karartılmış oyun görüntüsü, sol üstte "Duraklatıldı / Atölye / konum", solda metin menü (odaktaki öğe
    /// altın, başında çizgi), sol altta tuş ipuçları.
    /// Başarılar: ders sekmeleri (renk noktası + sayaç, seçili altı altın çizgi), dallanan ağaç (daire düğüm: tamam = altın
    /// dolu + onay, sıradaki = altın halka, açık = kâğıt halka, gizli = kesik halka; bağlar dirsekli), sağda seçili düğüm
    /// kartı, altta lejant. Sekme değiştirme yeni aksiyon gerektirmez: sekme satırında ← → (UI Navigate) ya da fare.
    /// </summary>
    public sealed class FigmaPauseMenu : FigmaView<IPauseMenuView>, IPauseMenuView
    {
        [Header("Etiketler — ana sayfa")]
        [SerializeField] private string pausedWord = "Duraklatıldı";
        [SerializeField] private string placeTitle = "Atölye";
        [SerializeField] private string placeSubtitle = "Kimya şehri, Etkileşim ilçesi";
        [SerializeField] private string resumeWord = "Devam Et";
        [SerializeField] private string achievementsWord = "Başarılar";
        [SerializeField] private string quitWord = "Oyundan Çık";
        [SerializeField] private string selectWord = "Seç";
        [SerializeField] private string browseWord = "Gez";

        [Header("Etiketler — başarılar")]
        [SerializeField] private string backWord = "Geri";
        [SerializeField] private string tabsWord = "Ders";
        [SerializeField] private string learnedWord = "Öğrendiğin";
        [SerializeField] private string doneWord = "Tamamlandı";
        [SerializeField] private string nextWord = "Sıradaki";
        [SerializeField] private string openWord = "Açık";
        [SerializeField] private string lockedWord = "Kilitli";

        [Header("Ağaç ölçüleri (Figma: sütun ~300; satır ad + alt satır sığacak kadar)")]
        [SerializeField] private Vector2 step = new Vector2(300f, 74f);
        [SerializeField] private Vector2 treePadding = new Vector2(40f, 40f);
        [SerializeField] private float nodeRadius = 14f;
        [SerializeField] private float lineWidth = 3f;

        [Header("Etiketler — şu anki görev")]
        [SerializeField] private string currentTaskWord = "Şu anki görev";
        [SerializeField] private string wantedWord = "İstenen";
        [SerializeField] private string deliverWord = "Kapıya götür, teslim et";
        [SerializeField] private string orderWord = "Sipariş";

        [Header("Ayarlar")]
        [SerializeField] private string settingsWord = "Ayarlar";
        [Tooltip("Varsayılan değerler buradan (Ayarlar → Varsayılana dön).")]
        [SerializeField] private Player.PlayerMovementSettings movementSettings;

        private GameObject overlay, mainPage, treePage, settingsPage;
        private FigmaSettingsPanel settingsPanel;
        private Button resumeButton, achievementsButton, settingsButton;
        private TextMeshProUGUI achievementsCount;
        private RectTransform tabBar, viewport, content;
        private TextMeshProUGUI detailCode, detailTitle, detailHeader, detailBody;
        private GameObject detailCard;
        private readonly List<Button> tabButtons = new List<Button>();
        private readonly List<Button> nodeButtons = new List<Button>();
        private readonly List<HintSlot> hints = new List<HintSlot>();
        private AchievementMenuInfo current;
        private int currentTab;

        private struct HintSlot
        {
            public TextMeshProUGUI Label;
            public RectTransform Root;
            public PromptAction Action;
        }

        private void Awake() => Build();

        protected override void OnEnable()
        {
            base.OnEnable();
            GameInput.SchemeChanged += OnScheme;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameInput.SchemeChanged -= OnScheme;
        }

        // ---------- IPauseMenuView ----------

        public void Open(AchievementMenuInfo achievements)
        {
            current = achievements;
            achievementsCount.text = achievements != null ? $"{achievements.UnlockedCount}/{achievements.TotalCount}" : string.Empty;
            overlay.SetActive(true);
            OnScheme(GameInput.LastScheme);
            ShowMain(resumeButton);
        }

        public void Close()
        {
            overlay.SetActive(false);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        public bool TryGoBack()
        {
            if (treePage.activeSelf)
            {
                ShowMain(achievementsButton);
                return true;
            }
            if (settingsPage.activeSelf)
            {
                ShowMain(settingsButton);
                return true;
            }
            return false;
        }

        // ---------- Sayfalar ----------

        private void ShowSettingsPage()
        {
            mainPage.SetActive(false);
            settingsPage.SetActive(true);
            settingsPanel.Show();
        }

        private void ShowMain(Selectable select)
        {
            treePage.SetActive(false);
            settingsPage.SetActive(false);
            mainPage.SetActive(true);
            Select(select);
        }

        private void ShowTreePage()
        {
            mainPage.SetActive(false);
            treePage.SetActive(true);
            BuildTabs();
            int count = current?.Tabs.Count ?? 0;
            ShowTab(Mathf.Clamp(currentTab, 0, Mathf.Max(0, count - 1)));
            Select(tabButtons.Count > currentTab ? tabButtons[currentTab] : null);
        }

        private void ShowTab(int index)
        {
            currentTab = index;
            var tabs = current?.Tabs;
            for (int i = 0; i < tabButtons.Count; i++)
                tabButtons[i].GetComponent<TabItem>().SetActive(i == index);
            var tab = tabs != null && index < tabs.Count ? tabs[index] : null;
            BuildTree(tab);
            content.anchoredPosition = Vector2.zero;
            ShowDetail(FirstFocus(tab));
        }

        private static int FirstFocus(AchievementTabInfo tab)
        {
            if (tab == null || tab.Nodes.Count == 0) return -1;
            // İlk "sıradaki" düğüm; yoksa ilk düğüm.
            for (int i = 0; i < tab.Nodes.Count; i++)
                if (StateOf(tab, i) == NodeLook.Next) return i;
            return 0;
        }

        private static void Select(Selectable s)
        {
            var es = EventSystem.current;
            if (es == null || s == null) return;
            // Önce boşalt: aynı obje zaten seçiliyse OnSelect gelmez ve odak görünümü kaybolurdu.
            es.SetSelectedGameObject(null);
            es.SetSelectedGameObject(s.gameObject);
        }

        private void OnScheme(ControlScheme scheme)
        {
            foreach (var h in hints) UiKit.SetKeyCap(h.Label, h.Root, ControlGlyphs.For(h.Action, scheme), 44f);
        }

        private void Update()
        {
            if (!treePage.activeSelf) return;
            var es = EventSystem.current;
            var selected = es != null ? es.currentSelectedGameObject : null;
            if (selected != null && selected.transform.parent == content) KeepVisible((RectTransform)selected.transform);
        }

        private void KeepVisible(RectTransform node)
        {
            Vector2 view = viewport.rect.size;
            Vector2 pos = content.anchoredPosition;
            float left = node.anchoredPosition.x - treePadding.x;
            float right = node.anchoredPosition.x + node.sizeDelta.x + treePadding.x;
            float top = -node.anchoredPosition.y - step.y;
            float bottom = -node.anchoredPosition.y + step.y;
            if (left + pos.x < 0f) pos.x = -left;
            else if (right + pos.x > view.x) pos.x = view.x - right;
            if (top - pos.y < 0f) pos.y = top;
            else if (bottom - pos.y > view.y) pos.y = bottom - view.y;
            pos.x = Mathf.Clamp(pos.x, Mathf.Min(0f, view.x - content.sizeDelta.x), 0f);
            pos.y = Mathf.Clamp(pos.y, 0f, Mathf.Max(0f, content.sizeDelta.y - view.y));
            content.anchoredPosition = pos;
        }

        // ---------- Sekmeler ----------

        private void BuildTabs()
        {
            Clear(tabBar);
            tabButtons.Clear();
            var tabs = current?.Tabs;
            if (tabs == null) return;
            var th = Theme;
            for (int i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                int index = i;
                var rt = UiKit.Rect(tabBar, "Tab_" + tab.Title);
                var hit = rt.gameObject.AddComponent<Image>();
                hit.color = new Color(0, 0, 0, 0);
                var le = rt.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 55f;
                var row = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
                row.spacing = 10f;
                row.padding = new RectOffset(0, 0, 0, 8);
                row.childAlignment = TextAnchor.MiddleLeft;
                row.childControlWidth = row.childControlHeight = true;
                row.childForceExpandWidth = row.childForceExpandHeight = false;

                var dot = UiKit.Img(rt, "Dot", tab.Color.a > 0f ? tab.Color : th.Gold, UiKit.Disc);
                var dle = dot.gameObject.AddComponent<LayoutElement>();
                dle.preferredWidth = dle.preferredHeight = 12f;
                var name = UiKit.Text(rt, "Name", th.HeadingFont, 36f, th.Paper, TextAlignmentOptions.Left, FontStyles.Bold);
                name.text = tab.Title;
                name.textWrappingMode = TextWrappingModes.NoWrap;
                var count = UiKit.Text(rt, "Count", th.BodyFont, 24f, th.Paper, TextAlignmentOptions.Left);
                count.text = tab.ComingSoon ? (string.IsNullOrEmpty(tab.ComingSoonLabel) ? lockedWord.ToLowerInvariant() : tab.ComingSoonLabel)
                    : $"{tab.UnlockedCount}/{tab.Nodes.Count}";
                count.textWrappingMode = TextWrappingModes.NoWrap;

                var underline = UiKit.Img(rt, "Underline", th.Gold);
                var urt = underline.rectTransform;
                underline.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                urt.anchorMin = Vector2.zero;
                urt.anchorMax = new Vector2(1f, 0f);
                urt.pivot = new Vector2(0.5f, 0f);
                urt.offsetMin = Vector2.zero;
                urt.offsetMax = new Vector2(0f, 3f);

                var b = rt.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => ShowTab(index));
                var item = rt.gameObject.AddComponent<TabItem>();
                item.Init(name, count, underline, th, tab.ComingSoon, () => { if (currentTab != index) ShowTab(index); });
                tabButtons.Add(b);
            }
            // Sekme satırı yalnızca yatay gezinir; aşağı = ağacın ilk düğümü (ağaç kurulunca bağlanır).
            for (int i = 0; i < tabButtons.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                if (i > 0) nav.selectOnLeft = tabButtons[i - 1];
                if (i < tabButtons.Count - 1) nav.selectOnRight = tabButtons[i + 1];
                tabButtons[i].navigation = nav;
            }
        }

        private void LinkTabsDown(Selectable target)
        {
            foreach (var b in tabButtons)
            {
                var nav = b.navigation;
                nav.selectOnDown = target;
                b.navigation = nav;
            }
        }

        // ---------- Ağaç ----------

        private enum NodeLook { Done, Next, Open, Secret }

        private static NodeLook StateOf(AchievementTabInfo tab, int i)
        {
            var n = tab.Nodes[i];
            if (n.State == AchievementState.Unlocked) return NodeLook.Done;
            if (n.State == AchievementState.Secret) return NodeLook.Secret;
            int p = n.ParentIndex;
            bool parentDone = p < 0 || (p < tab.Nodes.Count && tab.Nodes[p].State == AchievementState.Unlocked);
            return parentDone ? NodeLook.Next : NodeLook.Open;
        }

        private Vector2 NodeCenter(in AchievementNodeInfo n) =>
            new Vector2(treePadding.x + n.Column * step.x, -(treePadding.y + n.Row * step.y));

        private void BuildTree(AchievementTabInfo tab)
        {
            Clear(content);
            nodeButtons.Clear();
            var nodes = tab?.Nodes;
            if (nodes == null || nodes.Count == 0)
            {
                content.sizeDelta = Vector2.zero;
                LinkTabsDown(null);
                return;
            }
            var th = Theme;
            int maxCol = 0, maxRow = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                maxCol = Mathf.Max(maxCol, nodes[i].Column);
                maxRow = Mathf.Max(maxRow, nodes[i].Row);
            }
            content.sizeDelta = new Vector2(maxCol * step.x + step.x + 2f * treePadding.x, maxRow * step.y + 2f * treePadding.y);

            // Bağlar önce (düğümlerin altında): ebeveynin sağından yatay, ortada dirsek, çocuğa yatay.
            for (int i = 0; i < nodes.Count; i++)
            {
                int p = nodes[i].ParentIndex;
                if (p < 0 || p >= nodes.Count) continue;
                bool done = nodes[i].State == AchievementState.Unlocked;
                var color = done ? th.Gold : UiKit.WithAlpha(th.Paper, 0.35f);
                Vector2 a = NodeCenter(nodes[p]) + new Vector2(nodeRadius, 0f);
                Vector2 b = NodeCenter(nodes[i]) - new Vector2(nodeRadius, 0f);
                float midX = a.x + Mathf.Max(16f, (b.x - a.x) * 0.5f);
                Line(a, new Vector2(midX, a.y), color);
                Line(new Vector2(midX, a.y), new Vector2(midX, b.y), color);
                Line(new Vector2(midX, b.y), b, color);
            }

            for (int i = 0; i < nodes.Count; i++) nodeButtons.Add(MakeNode(tab, i, th));
            LinkTabsDown(nodeButtons[FirstFocus(tab)]);
        }

        private void Line(Vector2 a, Vector2 b, Color color)
        {
            var img = UiKit.Img(content, "Link", color);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = (a + b) * 0.5f;
            rt.sizeDelta = new Vector2(Mathf.Abs(b.x - a.x) + lineWidth, Mathf.Abs(b.y - a.y) + lineWidth);
        }

        private Button MakeNode(AchievementTabInfo tab, int index, UiTheme th)
        {
            var n = tab.Nodes[index];
            var look = StateOf(tab, index);
            bool isRoot = n.ParentIndex < 0;
            float r = isRoot ? nodeRadius * 1.3f : nodeRadius;

            var rt = UiKit.Rect(content, "Node_" + n.Id);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = NodeCenter(n) - new Vector2(r, 0f);
            rt.sizeDelta = new Vector2(step.x - 36f, Mathf.Max(2f * r, step.y - 4f));
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);

            // Odak halesi
            var halo = UiKit.Img(rt, "Halo", UiKit.WithAlpha(th.GoldLight, 0.9f), UiKit.Ring);
            UiKit.Place(halo.rectTransform, new Vector2(0f, 0.5f), new Vector2(-6f, 0f), new Vector2(2f * r + 12f, 2f * r + 12f));
            halo.gameObject.SetActive(false);

            Color goldOrPaper = look == NodeLook.Done || look == NodeLook.Next ? th.Gold : th.Paper;
            Sprite circle = look == NodeLook.Done ? UiKit.Disc : look == NodeLook.Secret ? UiKit.RingDashed : UiKit.Ring;
            float alpha = look == NodeLook.Open ? 0.75f : look == NodeLook.Secret ? 0.45f : 1f;
            var disc = UiKit.Img(rt, "Circle", UiKit.WithAlpha(goldOrPaper, alpha), circle);
            UiKit.Place(disc.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(2f * r, 2f * r));
            if (look == NodeLook.Done)
            {
                var chk = UiKit.Img(disc.transform, "Check", th.InkDeep, UiKit.Check);
                UiKit.Fill(chk.rectTransform, 3, 3, 3, 3);
            }

            // Etiket "künye"si: arkası zemin renginde, bağ çizgileri yazının altında kalır.
            var chip = UiKit.Img(rt, "LabelChip", UiKit.WithAlpha(th.InkDark, 0.94f), UiKit.Round8);
            var chipRt = chip.rectTransform;
            chipRt.anchorMin = chipRt.anchorMax = chipRt.pivot = new Vector2(0f, 0.5f);
            chipRt.anchoredPosition = new Vector2(2f * r + 8f, 0f);
            var col = UiKit.Rect(chip.transform, "Label");
            UiKit.Fill(col, 8f, 3f, 10f, 4f);
            var vl = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.MiddleLeft;
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            vl.spacing = -2f;
            Color titleColor = look == NodeLook.Next ? th.GoldLight : UiKit.WithAlpha(th.Paper, look == NodeLook.Done ? 1f : alpha);
            var title = UiKit.Text(col, "Title", th.HeadingFont, isRoot ? 36f : 28f, titleColor, TextAlignmentOptions.Left, FontStyles.Bold);
            title.text = n.Title;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.gameObject.AddComponent<LayoutElement>().minHeight = title.fontSize * 1.2f;
            var sub = UiKit.Text(col, "Sub", th.BodyFont, 22f, UiKit.WithAlpha(th.Paper, 0.6f), TextAlignmentOptions.Left);
            sub.text = look == NodeLook.Done ? n.Header : look == NodeLook.Next ? nextWord.ToLowerInvariant() : lockedWord.ToLowerInvariant();
            sub.textWrappingMode = TextWrappingModes.NoWrap;
            float maxW = step.x - 36f - (2f * r + 8f) - 10f;
            float textW = Mathf.Max(title.GetPreferredValues(n.Title).x, sub.GetPreferredValues(sub.text).x);
            chipRt.sizeDelta = new Vector2(Mathf.Min(maxW, textW + 18f), title.fontSize * 1.2f + 22f * 1.2f + 7f);

            var b = rt.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => ShowDetail(index));
            rt.gameObject.AddComponent<NodeItem>().Init(index, halo.gameObject, ShowDetail);
            return b;
        }

        private void ShowDetail(int index)
        {
            var tabs = current?.Tabs;
            var tab = tabs != null && currentTab < tabs.Count ? tabs[currentTab] : null;
            if (tab == null || index < 0 || index >= tab.Nodes.Count)
            {
                detailCard.SetActive(false);
                return;
            }
            var n = tab.Nodes[index];
            var look = StateOf(tab, index);
            detailCode.text = tab.Title;
            detailTitle.text = n.Title;
            detailHeader.text = look == NodeLook.Done ? $"{n.Header} · {doneWord}" : look == NodeLook.Next ? nextWord : tab.LockedLabel;
            detailBody.text = string.IsNullOrEmpty(n.Description) ? "—" : n.Description;
            detailCard.SetActive(true);
        }

        private static void Clear(RectTransform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        // ---------- Kurulum ----------

        private void Build()
        {
            var th = Theme;
            // Opak zemin: koyu ahşap masa (marka: "arayüz dünyanın parçası — ahşap"). Oyun görüntüsü ve HUD görünmez.
            var ov = UiKit.Img(transform, "PauseOverlay", th.WoodDark, null, true);
            UiKit.Fill(ov.rectTransform);
            overlay = ov.gameObject;
            var grain = UiKit.Img(ov.transform, "WoodGrain", UiKit.WithAlpha(th.Wood, 0.55f), UiKit.WoodGrain);
            UiKit.Fill(grain.rectTransform);
            var vignette = UiKit.Img(ov.transform, "Vignette", UiKit.WithAlpha(th.InkDeep, 0.9f), UiKit.Vignette);
            UiKit.Fill(vignette.rectTransform);
            var shade = UiKit.Img(ov.transform, "LeftShade", UiKit.WithAlpha(th.InkDeep, 0.45f), UiKit.FadeLeft);
            UiKit.Fill(shade.rectTransform, 0, 0, 840, 0);
            if (th.Seal != null)
            {
                // Sağ altta büyük, soluk mühür kabartması
                var mark = UiKit.Img(ov.transform, "SealMark", new Color(1f, 1f, 1f, 0.06f), th.Seal);
                UiKit.Place(mark.rectTransform, new Vector2(1f, 0f), new Vector2(120f, -140f), new Vector2(620f, 620f));
                mark.rectTransform.localEulerAngles = new Vector3(0f, 0f, -12f);
                mark.preserveAspect = true;
            }

            BuildMain(ov.rectTransform, th);
            BuildTreePage(ov.rectTransform, th);
            BuildSettingsPage(ov.rectTransform, th);

            treePage.SetActive(false);
            settingsPage.SetActive(false);
            overlay.SetActive(false);
        }

        private void BuildMain(RectTransform parent, UiTheme th)
        {
            var page = UiKit.Fill(UiKit.Rect(parent, "Main"));
            mainPage = page.gameObject;

            var head = UiKit.Rect(page, "Header");
            UiKit.Place(head, new Vector2(0f, 1f), new Vector2(120f, -120f), new Vector2(800f, 170f));
            var hv = head.gameObject.AddComponent<VerticalLayoutGroup>();
            hv.childControlWidth = hv.childControlHeight = true;
            hv.childForceExpandHeight = false;
            hv.spacing = 0f;
            UiKit.Text(head, "Paused", th.BodyFont, 28f, th.GoldLight, TextAlignmentOptions.Left, FontStyles.Bold).text = pausedWord;
            UiKit.Text(head, "Place", th.HeadingFont, 72f, th.Paper, TextAlignmentOptions.Left, FontStyles.Bold).text = placeTitle;
            UiKit.Text(head, "Where", th.BodyFont, 28f, UiKit.WithAlpha(th.Paper, 0.8f)).text = placeSubtitle;

            var menu = UiKit.Rect(page, "Menu");
            UiKit.Place(menu, new Vector2(0f, 1f), new Vector2(120f, -400f), new Vector2(600f, 320f));
            var mv = menu.gameObject.AddComponent<VerticalLayoutGroup>();
            mv.childControlWidth = mv.childControlHeight = true;
            mv.childForceExpandHeight = false;
            mv.spacing = 6f;
            resumeButton = MenuItem(menu, resumeWord, th, () => ViewLocator.Get<IPauseMenuCommands>()?.Resume(), out _);
            achievementsButton = MenuItem(menu, achievementsWord, th, ShowTreePage, out achievementsCount);
            settingsButton = MenuItem(menu, settingsWord, th, ShowSettingsPage, out _);
            MenuItem(menu, quitWord, th, () => ViewLocator.Get<IPauseMenuCommands>()?.QuitGame(), out _);

            var bar = HintBar(page, th, new Vector2(0f, 0f), new Vector2(120f, 52f));
            AddHint(bar, th, PromptAction.Cancel, resumeWord);
            AddHint(bar, th, PromptAction.Confirm, selectWord);
            AddHint(bar, th, PromptAction.UiVertical, browseWord);

            BuildTaskCard(page, th);
        }

        // ---------- Şu anki görev kartı (Figma: Ekran/Duraklatma → Görev parşömeni) ----------

        private GameObject taskCard;
        private TextMeshProUGUI taskTitle, taskQuote, taskWanted, taskPlace, taskProgress;
        private RectTransform taskSteps, taskDots;
        private GameObject taskQuoteGo;

        public void ShowCurrentTask(CurrentTaskInfo task)
        {
            if (taskCard == null) return;
            taskCard.SetActive(task != null);
            if (task == null) return;
            var th = Theme;
            taskTitle.text = task.Title;
            taskQuote.text = string.IsNullOrEmpty(task.Quote) ? string.Empty : "“" + task.Quote + "”";
            taskQuoteGo.SetActive(!string.IsNullOrEmpty(task.Quote));
            taskWanted.text = task.WantedRich;

            Clear(taskSteps);
            for (int i = 0; i < task.Steps.Count; i++) StepRow(taskSteps, th, task.Steps[i].Text, task.Steps[i].Done);
            StepRow(taskSteps, th, deliverWord, false, task.ReadyToDeliver);

            Clear(taskDots);
            for (int i = 0; i < task.Total; i++)
            {
                bool done = i < task.Done;
                var dot = UiKit.Img(taskDots, "Dot", done ? th.Gold : UiKit.WithAlpha(th.PaperShade, 0.9f), done ? UiKit.Disc : UiKit.Ring);
                var le = dot.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = le.preferredHeight = 16f;
            }
            taskPlace.text = task.Place;
            taskProgress.text = task.Total > 0 ? $"{orderWord} {Mathf.Min(task.Done + 1, task.Total)} / {task.Total}" : string.Empty;
        }

        private void StepRow(RectTransform parent, UiTheme th, string text, bool done, bool highlight = false)
        {
            var row = UiKit.Rect(parent, "Step");
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 16f;
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = hl.childControlHeight = true;
            hl.childForceExpandWidth = hl.childForceExpandHeight = false;

            // Onay kutusu: kâğıt kare, mürekkep çerçeve; bitmişse çimen yeşili onay
            var box = UiKit.Img(row, "Box", th.PaperShade, UiKit.Round4);
            var ble = box.gameObject.AddComponent<LayoutElement>();
            ble.preferredWidth = ble.preferredHeight = 30f;
            ble.minWidth = 30f;
            var face = UiKit.Img(box.transform, "Face", done ? new Color(0.184f, 0.541f, 0.227f) : th.Paper, UiKit.Round4);
            UiKit.Fill(face.rectTransform, 2.5f, 2.5f, 2.5f, 2.5f);
            if (done)
            {
                var chk = UiKit.Img(face.transform, "Check", th.Paper, UiKit.Check);
                UiKit.Fill(chk.rectTransform, 1, 1, 1, 1);
            }
            var t = UiKit.Text(row, "Text", th.BodyFont, 30f, done ? th.PaperShade : highlight ? th.WaxMid : th.Ink, TextAlignmentOptions.MidlineLeft,
                done ? FontStyles.Strikethrough : highlight ? FontStyles.Bold : FontStyles.Normal);
            t.text = text;
            t.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private void BuildTaskCard(RectTransform page, UiTheme th)
        {
            // Kâğıt parşömen + içte ince mühür rengi çizgi (Figma "Çerçeve çizgisi", 16 px içeride)
            var card = UiKit.PaperCard(page, "CurrentTask", th, 6f, out var paper, 10f);
            UiKit.Place(card, new Vector2(1f, 0.5f), new Vector2(-120f, 0f), new Vector2(760f, 0f));
            card.Find("Frame").GetComponent<Image>().color = th.PaperMid;
            taskCard = card.gameObject;
            var inner = UiKit.Img(paper, "InnerRule", UiKit.WithAlpha(th.WaxMid, 0.55f), UiKit.Round4);
            UiKit.Fill(inner.rectTransform, 16, 16, 16, 16);
            inner.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var hollow = UiKit.Img(inner.transform, "Hollow", th.Paper, UiKit.Round4);
            UiKit.Fill(hollow.rectTransform, 1.5f, 1.5f, 1.5f, 1.5f);

            var col = paper.gameObject.AddComponent<VerticalLayoutGroup>();
            col.padding = new RectOffset(64, 64, 52, 52);
            col.spacing = 14f;
            col.childControlWidth = col.childControlHeight = true;
            col.childForceExpandWidth = true;
            col.childForceExpandHeight = false;

            UiKit.Text(paper, "Label", th.BodyFont, 26f, th.WaxMid, TextAlignmentOptions.Left).text = currentTaskWord;
            taskTitle = UiKit.Text(paper, "Title", th.HeadingFont, 54f, th.InkDark, TextAlignmentOptions.Left, FontStyles.Bold);
            taskQuote = UiKit.Text(paper, "Quote", th.HeadingFont, 30f, th.Ink, TextAlignmentOptions.TopLeft, FontStyles.Italic);
            taskQuoteGo = taskQuote.gameObject;
            Rule(paper, th);

            var wanted = UiKit.Rect(paper, "Wanted");
            var wl = wanted.gameObject.AddComponent<HorizontalLayoutGroup>();
            wl.spacing = 24f;
            wl.childAlignment = TextAnchor.MiddleLeft;
            wl.childControlWidth = wl.childControlHeight = true;
            wl.childForceExpandWidth = wl.childForceExpandHeight = false;
            var wword = UiKit.Text(wanted, "Word", th.BodyFont, 26f, th.PaperShade, TextAlignmentOptions.MidlineLeft);
            wword.text = wantedWord;
            taskWanted = UiKit.Text(wanted, "Formula", th.BodyFont, 48f, th.InkDark, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            taskWanted.textWrappingMode = TextWrappingModes.NoWrap;

            taskSteps = UiKit.Rect(paper, "Steps");
            var sl = taskSteps.gameObject.AddComponent<VerticalLayoutGroup>();
            sl.spacing = 12f;
            sl.childControlWidth = sl.childControlHeight = true;
            sl.childForceExpandWidth = true;
            sl.childForceExpandHeight = false;
            Rule(paper, th);

            var foot = UiKit.Rect(paper, "Village");
            var fl = foot.gameObject.AddComponent<HorizontalLayoutGroup>();
            fl.spacing = 16f;
            fl.childAlignment = TextAnchor.MiddleLeft;
            fl.childControlWidth = fl.childControlHeight = true;
            fl.childForceExpandWidth = fl.childForceExpandHeight = false;
            taskPlace = UiKit.Text(foot, "Place", th.BodyFont, 26f, th.PaperShade, TextAlignmentOptions.MidlineLeft);
            taskPlace.textWrappingMode = TextWrappingModes.NoWrap;
            taskDots = UiKit.Rect(foot, "Dots");
            var dl = taskDots.gameObject.AddComponent<HorizontalLayoutGroup>();
            dl.spacing = 16f;
            dl.childAlignment = TextAnchor.MiddleLeft;
            dl.childControlWidth = dl.childControlHeight = true;
            dl.childForceExpandWidth = dl.childForceExpandHeight = false;
            taskProgress = UiKit.Text(foot, "Progress", th.BodyFont, 28f, th.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            taskProgress.textWrappingMode = TextWrappingModes.NoWrap;

            card.gameObject.AddComponent<CardSizer>().Init(paper, 6f);
            taskCard.SetActive(false);
        }

        private static void Rule(RectTransform parent, UiTheme th)
        {
            var r = UiKit.Img(parent, "Rule", UiKit.WithAlpha(th.PaperShade, 0.6f));
            var le = r.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = le.minHeight = 1.5f;
        }

        private void BuildSettingsPage(RectTransform parent, UiTheme th)
        {
            var page = UiKit.Fill(UiKit.Rect(parent, "Settings"));
            settingsPage = page.gameObject;
            var bar = HintBar(page, th, new Vector2(0f, 0f), new Vector2(120f, 52f));
            settingsPanel = page.gameObject.AddComponent<FigmaSettingsPanel>();
            settingsPanel.Init(th, movementSettings, (action, word) => AddHint(bar, th, action, word));
        }

        private Button MenuItem(RectTransform parent, string label, UiTheme th, UnityEngine.Events.UnityAction onClick, out TextMeshProUGUI count)
        {
            var rt = UiKit.Rect(parent, "Item_" + label);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 71f;
            var row = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 16f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;

            var dash = UiKit.Img(rt, "Dash", th.GoldLight);
            var dle = dash.gameObject.AddComponent<LayoutElement>();
            dle.preferredWidth = 24f;
            dle.preferredHeight = 3f;
            var text = UiKit.Text(rt, "Label", th.HeadingFont, 44f, th.Paper, TextAlignmentOptions.Left, FontStyles.Bold);
            text.text = label;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            count = UiKit.Text(rt, "Count", th.BodyFont, 28f, UiKit.WithAlpha(th.Paper, 0.7f), TextAlignmentOptions.Left);
            count.textWrappingMode = TextWrappingModes.NoWrap;

            var b = rt.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(onClick);
            rt.gameObject.AddComponent<MenuFocus>().Init(text, dash, th);
            return b;
        }

        private RectTransform HintBar(RectTransform parent, UiTheme th, Vector2 anchor, Vector2 pos)
        {
            var bar = UiKit.Rect(parent, "Hints");
            UiKit.Place(bar, anchor, pos, new Vector2(0f, 44f));
            var row = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 36f;
            row.childAlignment = anchor.x > 0.5f ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            bar.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            return bar;
        }

        private void AddHint(RectTransform bar, UiTheme th, PromptAction action, string word)
        {
            var slot = UiKit.Rect(bar, "Hint_" + action);
            var row = slot.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 10f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var key = UiKit.KeyCap(slot, th, 44f, out var keyRoot);
            var w = UiKit.Text(slot, "Word", th.BodyFont, 28f, th.Paper, TextAlignmentOptions.Left);
            w.text = word;
            w.textWrappingMode = TextWrappingModes.NoWrap;
            hints.Add(new HintSlot { Label = key, Root = keyRoot, Action = action });
        }

        private void BuildTreePage(RectTransform parent, UiTheme th)
        {
            var page = UiKit.Fill(UiKit.Rect(parent, "Achievements"));
            treePage = page.gameObject;
            var dim = UiKit.Img(page, "Dim", UiKit.WithAlpha(th.InkDeep, 0.35f));
            UiKit.Fill(dim.rectTransform);

            var title = UiKit.Text(page, "Title", th.HeadingFont, 72f, th.Paper, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            UiKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(120f, -62f), new Vector2(800f, 100f));
            title.text = achievementsWord;

            tabBar = UiKit.Rect(page, "Tabs");
            UiKit.Place(tabBar, new Vector2(0f, 1f), new Vector2(120f, -170f), new Vector2(1680f, 55f));
            var tl = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            tl.spacing = 44f;
            tl.childAlignment = TextAnchor.MiddleLeft;
            tl.childControlWidth = tl.childControlHeight = true;
            tl.childForceExpandWidth = tl.childForceExpandHeight = false;
            var rule = UiKit.Img(page, "TabRule", UiKit.WithAlpha(th.Paper, 0.25f));
            UiKit.Place(rule.rectTransform, new Vector2(0f, 1f), new Vector2(120f, -226f), new Vector2(1680f, 1f));

            var vp = UiKit.Img(page, "Viewport", new Color(0, 0, 0, 0), null, true);
            viewport = vp.rectTransform;
            UiKit.Place(viewport, new Vector2(0f, 1f), new Vector2(120f, -250f), new Vector2(1200f, 720f));
            vp.gameObject.AddComponent<RectMask2D>();
            content = UiKit.Rect(viewport, "Content");
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0f, 1f);
            var scroll = vp.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.inertia = false;

            // Seçili düğüm kartı (Figma: x 1360, y 300, 440 geniş)
            var card = UiKit.PaperCard(page, "Detail", th, 4f, out var paper);
            UiKit.Place(card, new Vector2(0f, 1f), new Vector2(1360f, -300f), new Vector2(440f, 0f));
            detailCard = card.gameObject;
            var col = paper.gameObject.AddComponent<VerticalLayoutGroup>();
            col.padding = new RectOffset(34, 30, 30, 34);
            col.spacing = 8f;
            col.childControlWidth = col.childControlHeight = true;
            col.childForceExpandWidth = true;
            col.childForceExpandHeight = false;
            detailCode = UiKit.Text(paper, "Course", th.BodyFont, 24f, th.WaxMid, TextAlignmentOptions.Left, FontStyles.Bold);
            detailTitle = UiKit.Text(paper, "Title", th.HeadingFont, 44f, th.InkDark, TextAlignmentOptions.Left, FontStyles.Bold);
            detailHeader = UiKit.Text(paper, "State", th.BodyFont, 26f, th.PaperShade);
            var line = UiKit.Img(paper, "Rule", th.PaperShade);
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = 2f;
            var learned = UiKit.Text(paper, "LearnedLabel", th.BodyFont, 28f, th.WaxMid, TextAlignmentOptions.Left, FontStyles.Bold);
            learned.text = learnedWord;
            detailBody = UiKit.Text(paper, "Body", th.HeadingFont, 30f, th.Ink, TextAlignmentOptions.TopLeft, FontStyles.Italic);
            card.gameObject.AddComponent<CardSizer>().Init(paper, 4f);

            // Lejant (Figma: x 120, y 998)
            var legend = UiKit.Rect(page, "Legend");
            UiKit.Place(legend, new Vector2(0f, 0f), new Vector2(120f, 53f), new Vector2(900f, 29f));
            var ll = legend.gameObject.AddComponent<HorizontalLayoutGroup>();
            ll.spacing = 34f;
            ll.childAlignment = TextAnchor.MiddleLeft;
            ll.childControlWidth = ll.childControlHeight = true;
            ll.childForceExpandWidth = ll.childForceExpandHeight = false;
            LegendItem(legend, th, UiKit.Disc, th.Gold, doneWord);
            LegendItem(legend, th, UiKit.Ring, th.Gold, nextWord);
            LegendItem(legend, th, UiKit.Ring, UiKit.WithAlpha(th.Paper, 0.75f), openWord);
            LegendItem(legend, th, UiKit.RingDashed, UiKit.WithAlpha(th.Paper, 0.45f), lockedWord);

            var bar = HintBar(page, th, new Vector2(1f, 0f), new Vector2(-120f, 44f));
            AddHint(bar, th, PromptAction.UiHorizontal, tabsWord);
            AddHint(bar, th, PromptAction.UiVertical, browseWord);
            AddHint(bar, th, PromptAction.Cancel, backWord);
        }

        private static void LegendItem(RectTransform parent, UiTheme th, Sprite sprite, Color color, string word)
        {
            var rt = UiKit.Rect(parent, "Legend_" + word);
            var row = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 10f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var icon = UiKit.Img(rt, "Icon", color, sprite);
            var le = icon.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = 18f;
            var t = UiKit.Text(rt, "Word", th.BodyFont, 24f, th.Paper, TextAlignmentOptions.Left);
            t.text = word;
            t.textWrappingMode = TextWrappingModes.NoWrap;
        }

        // ---------- Odak davranışları ----------

        /// <summary>Menü öğesi: odakta altın yazı + baştaki çizgi (Figma UI/Menü öğesi Durum=Odak).</summary>
        private sealed class MenuFocus : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
        {
            private TextMeshProUGUI label;
            private Image dash;
            private UiTheme theme;

            public void Init(TextMeshProUGUI l, Image d, UiTheme th)
            {
                label = l;
                dash = d;
                theme = th;
                Set(false);
            }

            private void Set(bool focused)
            {
                if (label == null) return;
                label.color = focused ? theme.GoldLight : theme.Paper;
                dash.color = focused ? theme.GoldLight : new Color(0, 0, 0, 0);
            }

            public void OnSelect(BaseEventData _) => Set(true);
            public void OnDeselect(BaseEventData _) => Set(false);
            // Sayfa gizlenirken seçim başka sayfaya geçer; kapalı objeye OnDeselect gitmez, odak izi kalmasın.
            private void OnDisable() => Set(false);
            public void OnPointerEnter(PointerEventData _) => EventSystem.current?.SetSelectedGameObject(gameObject);
        }

        /// <summary>Ders sekmesi: seçili sekme parlak + altın alt çizgi; gamepad odağı sekmeyi değiştirir.</summary>
        private sealed class TabItem : MonoBehaviour, ISelectHandler
        {
            private TextMeshProUGUI nameText, count;
            private Image underline;
            private UiTheme theme;
            private bool soon;
            private Action onFocus;

            public void Init(TextMeshProUGUI n, TextMeshProUGUI c, Image u, UiTheme th, bool comingSoon, Action focus)
            {
                nameText = n;
                count = c;
                underline = u;
                theme = th;
                soon = comingSoon;
                onFocus = focus;
            }

            public void SetActive(bool active)
            {
                float a = active ? 1f : soon ? 0.4f : 0.7f;
                nameText.color = UiKit.WithAlpha(theme.Paper, a);
                count.color = UiKit.WithAlpha(theme.Paper, a * 0.75f);
                underline.enabled = active;
            }

            public void OnSelect(BaseEventData _) => onFocus?.Invoke();
        }

        /// <summary>Ağaç düğümü: gamepad seçimi ya da fare üstüne gelince kart güncellenir, hale görünür.</summary>
        private sealed class NodeItem : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
        {
            private int index;
            private GameObject halo;
            private Action<int> onFocus;

            public void Init(int i, GameObject h, Action<int> focus)
            {
                index = i;
                halo = h;
                onFocus = focus;
            }

            public void OnSelect(BaseEventData _)
            {
                halo.SetActive(true);
                onFocus?.Invoke(index);
            }

            public void OnDeselect(BaseEventData _) => halo.SetActive(false);
            private void OnDisable() { if (halo != null) halo.SetActive(false); }

            public void OnPointerEnter(PointerEventData _) => EventSystem.current?.SetSelectedGameObject(gameObject);
        }
    }
}
