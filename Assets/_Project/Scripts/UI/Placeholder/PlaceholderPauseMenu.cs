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
    /// TEST YER TUTUCUSU — Esc menüsü: ana sayfa (Devam / Başarımlar / Oyundan çık) ve sekmeli, dallanan başarım ağacı.
    /// Ağaç: sütun = derinlik, satır = yerleşim (otomatik ya da asset'te elle); ebeveyn → çocuk dirsekli çizgiler.
    /// Tür renkleri/simgeleri sekme asset'inden (AchievementTree) gelir; bu view'ın kendi görünümü (renk, boyut, font,
    /// sprite) Inspector'dan değişir — kod değişmez. Gamepad: yön tuşlarıyla gezilir, görünür alan seçimi takip eder.
    /// HUD Canvas'ı altına (son kardeş) konması yeter. Mustafa'nın menüsü IPauseMenuView'ı uygulayıp bunun yerine geçebilir.
    /// </summary>
    public sealed class PlaceholderPauseMenu : RegisteredView<IPauseMenuView>, IPauseMenuView
    {
        [Header("Etiketler")]
        [SerializeField] private string pausedTitle = "Duraklatıldı";
        [SerializeField] private string resumeWord = "Devam et";
        [SerializeField] private string achievementsWord = "Başarımlar";
        [SerializeField] private string quitWord = "Oyundan çık";
        [SerializeField] private string backWord = "Geri";

        [Header("Görünüm — renkler")]
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color panelColor = PlaceholderUi.Wood;
        [SerializeField] private Color viewportColor = PlaceholderUi.Dim;
        [SerializeField] private Color buttonColor = PlaceholderUi.Parchment;
        [SerializeField] private Color titleColor = PlaceholderUi.Parchment;
        [SerializeField] private Color inkColor = PlaceholderUi.Ink;
        [SerializeField] private Color lockedNodeColor = PlaceholderUi.WoodLight;
        [SerializeField] private Color secretNodeColor = PlaceholderUi.Dim;
        [SerializeField] private Color unlockedNodeColor = PlaceholderUi.Parchment;
        [SerializeField] private Color lockedLineColor = PlaceholderUi.WoodLight;
        [Tooltip("Ders sekmesinin rengi yoksa seçili sekme rengi.")]
        [SerializeField] private Color activeTabColor = PlaceholderUi.Gold;

        [Header("Görünüm — font ve sprite (boş = varsayılan)")]
        [SerializeField] private TMP_FontAsset font;
        [Tooltip("Paneller (9-slice önerilir).")]
        [SerializeField] private Sprite panelSprite;
        [Tooltip("Düğüm çerçevesi (9-slice önerilir).")]
        [SerializeField] private Sprite nodeSprite;
        [SerializeField] private Sprite buttonSprite;

        [Header("Görünüm — ağaç ölçüleri")]
        [SerializeField] private Vector2 nodeSize = new Vector2(210, 84);
        [SerializeField] private Vector2 step = new Vector2(260, 124);
        [SerializeField] private float padding = 40f;
        [SerializeField] private float lineWidth = 5f;
        [SerializeField] private float nodeFontSize = 20f;
        [SerializeField] private Vector2 treePanelSize = new Vector2(1760, 960);

        private GameObject overlay, mainPage, treePage;
        private Button resumeButton, achievementsButton, backButton;
        private TMP_Text achievementsLabel, treeTitle, detailText, backHint, mainHint;
        private RectTransform viewport, content, tabBar;
        private readonly List<Button> nodeButtons = new List<Button>();
        private readonly List<Button> tabButtons = new List<Button>();
        private AchievementMenuInfo current;
        private int currentTab;

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
            achievementsLabel.text = $"{achievementsWord}  <size=75%>{achievements.UnlockedCount}/{achievements.TotalCount}</size>";
            OnScheme(GameInput.LastScheme);
            overlay.SetActive(true);
            ShowMain(resumeButton);
        }

        public void Close()
        {
            overlay.SetActive(false);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        public bool TryGoBack()
        {
            if (!treePage.activeSelf) return false;
            ShowMain(achievementsButton);
            return true;
        }

        // ---------- Sayfalar ----------

        private void ShowMain(Button select)
        {
            treePage.SetActive(false);
            mainPage.SetActive(true);
            Select(select);
        }

        private void ShowTreePage()
        {
            mainPage.SetActive(false);
            treePage.SetActive(true);
            BuildTabs();
            ShowTab(Mathf.Clamp(currentTab, 0, Mathf.Max(0, (current?.Tabs.Count ?? 1) - 1)), true);
        }

        private void ShowTab(int index, bool selectFirstNode)
        {
            currentTab = index;
            var tabs = current?.Tabs;
            var tab = tabs != null && index < tabs.Count ? tabs[index] : null;
            // Seçili sekme dersin rengini alır (asset'teki Accent Color); renk yoksa activeTabColor.
            for (int i = 0; i < tabButtons.Count && tabs != null && i < tabs.Count; i++)
            {
                var accent = tabs[i].Color.a > 0f ? tabs[i].Color : activeTabColor;
                tabButtons[i].GetComponent<Image>().color = i == index ? accent : buttonColor;
                tabButtons[i].GetComponentInChildren<TMP_Text>().color = i == index ? Color.white : inkColor;
            }

            if (tab == null)
            {
                treeTitle.text = $"<b>{achievementsWord}</b>";
                BuildTree(null);
                return;
            }
            string soon = tab.ComingSoon ? $"   <size=70%><i>{tab.ComingSoonLabel}</i></size>" : string.Empty;
            treeTitle.text = $"<b>{achievementsWord}</b> — {tab.Title}   <size=75%>{tab.UnlockedCount}/{tab.Nodes.Count}</size>{soon}";
            BuildTree(tab);
            content.anchoredPosition = Vector2.zero;
            if (nodeButtons.Count > 0) ShowDetail(0);
            Select(selectFirstNode && nodeButtons.Count > 0 ? nodeButtons[0] : tabButtons.Count > index ? tabButtons[index] : backButton);
        }

        private static void Select(Selectable s)
        {
            var es = EventSystem.current;
            if (es != null && s != null) es.SetSelectedGameObject(s.gameObject);
        }

        private void OnScheme(ControlScheme scheme)
        {
            string glyph = ControlGlyphs.For(PromptAction.Cancel, scheme);
            if (backHint != null) backHint.text = backWord + "  " + glyph;
            if (mainHint != null) mainHint.text = resumeWord + "  " + glyph;
        }

        private void Update()
        {
            if (!treePage.activeSelf) return;
            var es = EventSystem.current;
            var selected = es != null ? es.currentSelectedGameObject : null;
            if (selected == null || selected.transform.parent != content) return;
            KeepVisible((RectTransform)selected.transform);
        }

        // Gamepad'le seçilen düğüm görünür alanın dışındaysa içeriği kaydırır.
        private void KeepVisible(RectTransform node)
        {
            Vector2 view = viewport.rect.size;
            Vector2 pos = content.anchoredPosition; // x ≤ 0 sola, y ≥ 0 yukarı kaydırır
            float left = node.anchoredPosition.x, top = -node.anchoredPosition.y;
            float right = left + nodeSize.x + padding, bottom = top + nodeSize.y + padding;
            left -= padding;
            top -= padding;

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
            for (int i = 0; i < tabs.Count; i++)
            {
                int index = i;
                var tab = tabs[i];
                string label = tab.ComingSoon ? $"{tab.Title} <size=70%>({tab.ComingSoonLabel})</size>" : tab.Title;
                var b = PlaceholderUi.MakeButton(tabBar, $"{label}  <size=70%>{tab.UnlockedCount}/{tab.Nodes.Count}</size>",
                    () => ShowTab(index, true), buttonColor, 56);
                Style(b);
                var le = b.GetComponent<LayoutElement>();
                le.preferredWidth = 360;
                le.flexibleWidth = 0;
                if (tab.Icon != null) AddIcon((RectTransform)b.transform, tab.Icon, 40);
                tabButtons.Add(b);
            }
        }

        // ---------- Ağaç ----------

        private void BuildTree(AchievementTabInfo tab)
        {
            Clear(content);
            nodeButtons.Clear();
            var nodes = tab?.Nodes;
            if (nodes == null || nodes.Count == 0)
            {
                detailText.text = string.Empty;
                content.sizeDelta = Vector2.zero;
                return;
            }

            int maxCol = 0, maxRow = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                maxCol = Mathf.Max(maxCol, nodes[i].Column);
                maxRow = Mathf.Max(maxRow, nodes[i].Row);
            }
            content.sizeDelta = new Vector2(maxCol * step.x + nodeSize.x + 2f * padding, maxRow * step.y + nodeSize.y + 2f * padding);

            // Önce çizgiler (düğümlerin altında kalsın)
            for (int i = 0; i < nodes.Count; i++)
            {
                int p = nodes[i].ParentIndex;
                if (p < 0 || p >= nodes.Count) continue;
                var color = nodes[i].State == AchievementState.Unlocked ? nodes[i].Color : lockedLineColor;
                Vector2 from = NodePos(nodes[p]) + new Vector2(nodeSize.x, -nodeSize.y * 0.5f);
                Vector2 to = NodePos(nodes[i]) + new Vector2(0f, -nodeSize.y * 0.5f);
                float midX = from.x + (to.x - from.x) * 0.5f;
                Line(new Vector2(from.x, from.y), new Vector2(midX, from.y), color);
                Line(new Vector2(midX, from.y), new Vector2(midX, to.y), color);
                Line(new Vector2(midX, to.y), new Vector2(to.x, to.y), color);
            }

            for (int i = 0; i < nodes.Count; i++) nodeButtons.Add(MakeNode(nodes[i], i));
        }

        private Vector2 NodePos(in AchievementNodeInfo n) =>
            new Vector2(padding + n.Column * step.x, -(padding + n.Row * step.y));

        private void Line(Vector2 a, Vector2 b, Color color)
        {
            var go = new GameObject("Line", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(content, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = (a + b) * 0.5f;
            rt.sizeDelta = new Vector2(Mathf.Abs(b.x - a.x) + lineWidth, Mathf.Abs(b.y - a.y) + lineWidth);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private Button MakeNode(in AchievementNodeInfo n, int index)
        {
            bool unlocked = n.State == AchievementState.Unlocked;
            var go = new GameObject("Node_" + n.Id, typeof(RectTransform), typeof(Image), typeof(Button), typeof(NodeHover));
            var rt = (RectTransform)go.transform;
            rt.SetParent(content, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = NodePos(n);
            rt.sizeDelta = nodeSize;
            var frame = go.GetComponent<Image>();
            frame.color = unlocked ? n.Color : panelColor;
            ApplySprite(frame, nodeSprite);

            var inner = PlaceholderUi.Panel(rt, "Inner", unlocked ? unlockedNodeColor
                : n.State == AchievementState.Secret ? secretNodeColor : lockedNodeColor,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            PlaceholderUi.Fill(inner, 5, 5, 5, 5);
            ApplySprite(inner.GetComponent<Image>(), nodeSprite);

            var icon = AddIcon(inner, n.Icon, 44);
            icon.color = n.Icon != null ? (unlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f))
                : unlocked ? n.Color : secretNodeColor;

            var label = PlaceholderUi.Text(inner, n.Title, nodeFontSize, unlocked ? inkColor : titleColor, TextAlignmentOptions.MidlineLeft);
            PlaceholderUi.Fill(label.rectTransform, 60, 4, 6, 4);
            label.overflowMode = TextOverflowModes.Ellipsis;
            if (font != null) label.font = font;

            var b = go.GetComponent<Button>();
            PlaceholderUi.SetHighlight(b);
            b.onClick.AddListener(() => ShowDetail(index));
            go.GetComponent<NodeHover>().Init(index, ShowDetail);
            return b;
        }

        private Image AddIcon(RectTransform parent, Sprite sprite, float size)
        {
            var icon = PlaceholderUi.Panel(parent, "Icon", Color.white, new Vector2(0f, 0.5f), new Vector2(8, 0), new Vector2(size, size)).GetComponent<Image>();
            icon.rectTransform.pivot = new Vector2(0f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(8, 0);
            icon.sprite = sprite;
            icon.preserveAspect = true;
            return icon;
        }

        private void ShowDetail(int index)
        {
            var tabs = current?.Tabs;
            if (tabs == null || currentTab >= tabs.Count) return;
            var tab = tabs[currentTab];
            if (index < 0 || index >= tab.Nodes.Count) return;
            var n = tab.Nodes[index];
            string hex = ColorUtility.ToHtmlStringRGB(n.Color);
            string state = n.State == AchievementState.Unlocked ? $"<color=#{hex}>{n.Header}</color>" : tab.LockedLabel;
            detailText.text = $"<b>{n.Title}</b>   <size=75%>{state}</size>\n<size=85%>{n.Description}</size>";
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

        private static void ApplySprite(Image img, Sprite sprite)
        {
            if (sprite == null) return;
            img.sprite = sprite;
            img.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        }

        private void Style(Button b)
        {
            ApplySprite(b.GetComponent<Image>(), buttonSprite);
            var t = b.GetComponentInChildren<TMP_Text>();
            t.color = inkColor;
            if (font != null) t.font = font;
        }

        /// <summary>Gamepad seçimi ve fare üstüne gelince açıklamayı gösterir.</summary>
        private sealed class NodeHover : MonoBehaviour, ISelectHandler, IPointerEnterHandler
        {
            private int index;
            private Action<int> onFocus;

            public void Init(int i, Action<int> focus)
            {
                index = i;
                onFocus = focus;
            }

            public void OnSelect(BaseEventData _) => onFocus?.Invoke(index);
            public void OnPointerEnter(PointerEventData _) => onFocus?.Invoke(index);
        }

        // ---------- Kurulum ----------

        private void Build()
        {
            var root = (RectTransform)transform;

            var ov = PlaceholderUi.Panel(root, "PauseOverlay", overlayColor, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            PlaceholderUi.Fill(ov, 0, 0, 0, 0);
            ov.GetComponent<Image>().raycastTarget = true; // arkadaki HUD'a tıklanmasın
            overlay = ov.gameObject;

            // Ana sayfa
            var main = PlaceholderUi.Panel(ov, "Main", panelColor, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 470));
            ApplySprite(main.GetComponent<Image>(), panelSprite);
            mainPage = main.gameObject;
            var title = PlaceholderUi.Text(main, "<b>" + pausedTitle + "</b>", 44, titleColor, TextAlignmentOptions.Top);
            PlaceholderUi.Fill(title.rectTransform, 0, 24, 0, 0);
            var list = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
            var listRt = (RectTransform)list.transform;
            listRt.SetParent(main, false);
            PlaceholderUi.Fill(listRt, 40, 110, 40, 80);
            var layout = list.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 14;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            resumeButton = PlaceholderUi.MakeButton(listRt, resumeWord, () => ViewLocator.Get<IPauseMenuCommands>()?.Resume(), buttonColor, 72);
            achievementsButton = PlaceholderUi.MakeButton(listRt, achievementsWord, ShowTreePage, buttonColor, 72);
            achievementsLabel = achievementsButton.GetComponentInChildren<TMP_Text>();
            var quit = PlaceholderUi.MakeButton(listRt, quitWord, () => ViewLocator.Get<IPauseMenuCommands>()?.QuitGame(), lockedNodeColor, 72);
            Style(resumeButton);
            Style(achievementsButton);
            Style(quit);
            mainHint = PlaceholderUi.Text(main, "", 24, titleColor, TextAlignmentOptions.Bottom);
            PlaceholderUi.Fill(mainHint.rectTransform, 0, 0, 0, 24);

            // Ağaç sayfası: başlık, sekme çubuğu, kaydırılabilir ağaç, açıklama, geri
            var tree = PlaceholderUi.Panel(ov, "Achievements", panelColor, new Vector2(0.5f, 0.5f), Vector2.zero, treePanelSize);
            ApplySprite(tree.GetComponent<Image>(), panelSprite);
            treePage = tree.gameObject;
            treeTitle = PlaceholderUi.Text(tree, "", 38, titleColor, TextAlignmentOptions.TopLeft);
            PlaceholderUi.Fill(treeTitle.rectTransform, 32, 18, 32, 0);

            var bar = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabBar = (RectTransform)bar.transform;
            tabBar.SetParent(tree, false);
            tabBar.anchorMin = new Vector2(0f, 1f);
            tabBar.anchorMax = new Vector2(1f, 1f);
            tabBar.pivot = new Vector2(0.5f, 1f);
            tabBar.offsetMin = new Vector2(24, -138);
            tabBar.offsetMax = new Vector2(-24, -76);
            var hl = bar.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 10;
            hl.childControlHeight = true;
            hl.childControlWidth = true;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = true;

            var vp = PlaceholderUi.Panel(tree, "Viewport", viewportColor, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            PlaceholderUi.Fill(vp, 24, 150, 24, 190);
            vp.GetComponent<Image>().raycastTarget = true;
            vp.gameObject.AddComponent<RectMask2D>();
            viewport = vp;

            var c = new GameObject("Content", typeof(RectTransform));
            content = (RectTransform)c.transform;
            content.SetParent(vp, false);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;

            var scroll = vp.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = vp;
            scroll.content = content;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.inertia = false;

            var detail = PlaceholderUi.Panel(tree, "Detail", buttonColor, new Vector2(0f, 0f), new Vector2(24, 24),
                new Vector2(treePanelSize.x - 24 - 24 - 280 - 16, 150));
            ApplySprite(detail.GetComponent<Image>(), panelSprite);
            detail.pivot = Vector2.zero;
            detail.anchoredPosition = new Vector2(24, 24);
            detailText = PlaceholderUi.Text(detail, "", 28, inkColor, TextAlignmentOptions.TopLeft);
            PlaceholderUi.Fill(detailText.rectTransform, 18, 12, 18, 12);

            var backRt = PlaceholderUi.Panel(tree, "BackHolder", new Color(0, 0, 0, 0), new Vector2(1f, 0f), Vector2.zero, new Vector2(280, 150));
            backRt.pivot = new Vector2(1f, 0f);
            backRt.anchoredPosition = new Vector2(-24, 24);
            backButton = PlaceholderUi.MakeButton(backRt, backWord, () => TryGoBack(), lockedNodeColor, 150);
            PlaceholderUi.Fill((RectTransform)backButton.transform, 0, 0, 0, 0);
            Style(backButton);
            backHint = backButton.GetComponentInChildren<TMP_Text>();

            if (font != null)
                foreach (var t in GetComponentsInChildren<TMP_Text>(true)) t.font = font;

            treePage.SetActive(false);
            overlay.SetActive(false);
        }
    }
}
