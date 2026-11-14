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
    /// Keşif Defteri (IDiscoveryBookView) — Esc menüsünden açılır; marka dili: karartılmış oyunun üstünde ahşap
    /// cilt, içinde kâğıt sayfa. Başlık Alegreya, her kayıt "K-n" mum rozeti + mürekkep metin; uzun liste kayar
    /// (fare tekeri / UI ↑↓). Altta "Kapat" ve Esc/B tuş ipucu. Kapatınca menüye dönülür (çağıran yönetir).
    /// </summary>
    public sealed class FigmaDiscoveryBook : FigmaView<IDiscoveryBookView>, IDiscoveryBookView
    {
        [SerializeField] private Vector2 size = new Vector2(1180f, 860f);
        [SerializeField] private float scrollStep = 0.12f;

        private GameObject root;
        private TextMeshProUGUI title, empty, closeLabel, escKey;
        private RectTransform list, escKeyRoot;
        private ScrollRect scroll;
        private Button closeButton;
        private readonly List<GameObject> rows = new List<GameObject>();
        private Action closed;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake() => Build();

        protected override void OnEnable()
        {
            base.OnEnable();
            GameInput.SchemeChanged += OnScheme;
        }

        protected override void OnDisable()
        {
            GameInput.SchemeChanged -= OnScheme;
            base.OnDisable();
        }

        public void Open(string bookTitle, IReadOnlyList<string> pages, string emptyText, string closeWord, Action onClosed)
        {
            closed = onClosed;
            title.text = bookTitle;
            closeLabel.text = closeWord;
            for (int i = 0; i < rows.Count; i++) Destroy(rows[i]);
            rows.Clear();
            for (int i = 0; i < pages.Count; i++) rows.Add(MakeRow(pages[i]));
            empty.gameObject.SetActive(pages.Count == 0);
            empty.text = emptyText;
            root.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(list);
            scroll.verticalNormalizedPosition = 1f;
            OnScheme(GameInput.LastScheme);
            EventSystem.current?.SetSelectedGameObject(closeButton.gameObject);
        }

        public void Close()
        {
            if (!IsOpen) return;
            root.SetActive(false);
            var cb = closed;
            closed = null;
            cb?.Invoke();
        }

        private UnityEngine.InputSystem.InputAction navAction;

        private void Update()
        {
            if (!IsOpen) return;
            // Gamepad/klavye: UI ↑↓ seçim olmadığı için kaydırmaya çevrilir.
            // Her kare string ile aramamak için ilk seferde önbellekle.
            var nav = navAction ??= GameInput.FindAction("UI/Navigate");
            float y = nav != null && nav.enabled ? nav.ReadValue<Vector2>().y : 0f;
            if (Mathf.Abs(y) > 0.5f)
                scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + Mathf.Sign(y) * scrollStep * Time.unscaledDeltaTime * 8f);
        }

        private void OnScheme(ControlScheme _)
        {
            if (escKey != null) UiKit.SetKeyCap(escKey, escKeyRoot, ControlGlyphs.For(PromptAction.Cancel), 40f);
        }

        // "<b>K-1</b>   metin" → rozet + metin
        private GameObject MakeRow(string page)
        {
            var th = Theme;
            string id = string.Empty, text = page;
            const string open = "<b>", close = "</b>";
            if (page.StartsWith(open))
            {
                int end = page.IndexOf(close, StringComparison.Ordinal);
                if (end > 0)
                {
                    id = page.Substring(open.Length, end - open.Length);
                    text = page.Substring(end + close.Length).TrimStart();
                }
            }

            var row = UiKit.Rect(list, "Page_" + id);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 18f;
            h.childAlignment = TextAnchor.UpperLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            var badge = UiKit.Img(row, "Badge", th.Wax, UiKit.Round8);
            var bl = badge.gameObject.AddComponent<LayoutElement>();
            bl.preferredWidth = 76f;
            bl.preferredHeight = 40f;
            var bt = UiKit.Text(badge.transform, "Id", th.HeadingFont, 24f, th.Paper, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Fill(bt.rectTransform);
            bt.text = id;

            var body = UiKit.Text(row, "Text", th.BodyFont, 29f, th.Ink, TextAlignmentOptions.TopLeft);
            body.text = text;
            body.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            return row.gameObject;
        }

        private void Build()
        {
            var th = Theme;
            var rootRt = UiKit.Fill(UiKit.Rect(transform, "DiscoveryBook"));
            root = rootRt.gameObject;
            var scrim = UiKit.Img(rootRt, "Scrim", UiKit.WithAlpha(th.InkDeep, 0.72f), null, true);
            UiKit.Fill(scrim.rectTransform);

            // Cilt: sert gölge + ahşap kapak; sayfa: kâğıt
            var book = UiKit.Rect(rootRt, "Book");
            UiKit.Place(book, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var shadow = UiKit.Img(book, "Shadow", UiKit.WithAlpha(th.InkDeep, 0.6f), UiKit.Round8);
            UiKit.Fill(shadow.rectTransform, 0, 12, 0, -12);
            var cover = UiKit.Img(book, "Cover", th.WoodDark, UiKit.Round8);
            UiKit.Fill(cover.rectTransform);
            var grain = UiKit.Img(cover.transform, "Grain", UiKit.WithAlpha(th.Wood, 0.45f), UiKit.WoodGrain);
            UiKit.Fill(grain.rectTransform);
            var page = UiKit.Img(cover.transform, "Page", th.Paper, UiKit.Round8);
            UiKit.Fill(page.rectTransform, 22, 22, 22, 22);

            title = UiKit.Text(page.transform, "Title", th.HeadingFont, 56f, th.InkDark, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            UiKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(60f, -34f), new Vector2(800f, 70f));
            var rule = UiKit.Img(page.transform, "Rule", UiKit.WithAlpha(th.Ink, 0.25f));
            UiKit.Place(rule.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(size.x - 160f, 2f));

            // Kayan liste
            var viewport = UiKit.Rect(page.transform, "Viewport");
            UiKit.Fill(viewport, 60, 140, 60, 120);
            viewport.gameObject.AddComponent<RectMask2D>();
            var vimg = viewport.gameObject.AddComponent<Image>();
            vimg.color = new Color(0, 0, 0, 0);
            list = UiKit.Rect(viewport, "List");
            list.anchorMin = new Vector2(0f, 1f);
            list.anchorMax = new Vector2(1f, 1f);
            list.pivot = new Vector2(0.5f, 1f);
            list.offsetMin = list.offsetMax = Vector2.zero;
            var v = list.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 22f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = list;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            scroll.viewport = viewport;

            empty = UiKit.Text(viewport, "Empty", th.BodyFont, 30f, UiKit.WithAlpha(th.Ink, 0.6f), TextAlignmentOptions.Center);
            UiKit.Fill(empty.rectTransform);

            // Alt: Esc ipucu (sol) + Kapat düğmesi (sağ)
            var hint = UiKit.Rect(page.transform, "Hint");
            UiKit.Place(hint, new Vector2(0f, 0f), new Vector2(60f, 40f), new Vector2(300f, 40f));
            var hrow = hint.gameObject.AddComponent<HorizontalLayoutGroup>();
            hrow.spacing = 10f;
            hrow.childAlignment = TextAnchor.MiddleLeft;
            hrow.childControlWidth = hrow.childControlHeight = true;
            hrow.childForceExpandWidth = hrow.childForceExpandHeight = false;
            escKey = UiKit.KeyCap(hint, th, 40f, out escKeyRoot);

            var btnRt = UiKit.Rect(page.transform, "Close");
            UiKit.Place(btnRt, new Vector2(1f, 0f), new Vector2(-60f, 34f), new Vector2(200f, 58f));
            var outline = UiKit.Img(btnRt, "Outline", th.InkDark, UiKit.Round8, true);
            UiKit.Fill(outline.rectTransform);
            var inner = UiKit.Img(outline.transform, "Inner", th.Wood, UiKit.Round8);
            UiKit.Fill(inner.rectTransform, 3, 3, 3, 3);
            closeLabel = UiKit.Text(inner.transform, "Label", th.BodyFont, 28f, th.Paper, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Fill(closeLabel.rectTransform);
            closeButton = btnRt.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = outline;
            var colors = closeButton.colors;
            colors.selectedColor = colors.highlightedColor = th.Gold;
            closeButton.colors = colors;
            closeButton.onClick.AddListener(Close);

            root.SetActive(false);
        }
    }
}
