using System;
using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Ana menü — Esc menüsüyle aynı marka dili: koyu ahşap masa (damar + vinyet), solda Alegreya başlık ve
    /// italik alt başlık, altın kısa çizgi; menü maddeleri büyük başlık fontu, odakta altın çizgi + altın yazı.
    /// Fare (üstüne gelince odak) ve gamepad/klavye (EventSystem yön gezinmesi) ile kullanılır. Kayıt varken
    /// "Oyna" kâğıt bir onay kartı açar. Görünüm yalnızca IMainMenuView; ne yapılacağı denetleyiciden gelir.
    /// </summary>
    public sealed class FigmaMainMenuView : FigmaView<IMainMenuView>, IMainMenuView
    {
        [SerializeField] private float left = 160f;

        private RectTransform menuColumn;
        private TextMeshProUGUI titleText, subtitleText;
        private Button playButton, continueButton, quitButton;
        private TextMeshProUGUI playLabel, continueLabel, quitLabel;
        private GameObject confirmRoot;
        private TextMeshProUGUI confirmText, confirmYesLabel, confirmNoLabel;
        private Button confirmYes, confirmNo;
        private Action onPlay, onContinue, onQuit, confirmAction, cancelAction;
        private bool busy;

        private void Awake() => Build();

        public void Show(MenuTexts texts, bool canContinue, Action play, Action cont, Action quit)
        {
            onPlay = play;
            onContinue = cont;
            onQuit = quit;
            titleText.text = texts.Title;
            subtitleText.text = texts.Subtitle;
            playLabel.text = texts.Play;
            continueLabel.text = texts.ContinueGame;
            quitLabel.text = texts.Quit;
            confirmText.text = texts.NewGameWarning;
            confirmYesLabel.text = texts.ConfirmNewGame;
            confirmNoLabel.text = texts.Cancel;

            continueButton.interactable = canContinue;
            continueButton.GetComponent<MenuFocus>().SetDisabled(!canContinue);
            // Devam edilebiliyorsa varsayılan odak "Devam et": en sık seçilen.
            Select(canContinue ? continueButton : playButton);
        }

        public void AskNewGame(Action confirm, Action cancel)
        {
            confirmAction = confirm;
            cancelAction = cancel;
            confirmRoot.SetActive(true);
            SetMenuInteractable(false);
            Select(confirmNo);   // yanlışlıkla ilerlemeyi silmesin: varsayılan Vazgeç
        }

        public void SetBusy(bool value)
        {
            busy = value;
            SetMenuInteractable(!value);
        }

        private void SetMenuInteractable(bool on)
        {
            playButton.interactable = on;
            quitButton.interactable = on;
            continueButton.interactable = on && !continueButton.GetComponent<MenuFocus>().Disabled;
        }

        private void CloseConfirm()
        {
            confirmRoot.SetActive(false);
            SetMenuInteractable(true);
            Select(playButton);
        }

        private static void Select(Selectable s)
        {
            var es = EventSystem.current;
            if (es == null || s == null) return;
            es.SetSelectedGameObject(null);
            es.SetSelectedGameObject(s.gameObject);
        }

        private void Update()
        {
            // Gamepad/klavye: seçim kaybolursa (fare boşluğa tıkladı) ilk geçerli maddeye dön.
            var es = EventSystem.current;
            if (es == null || busy || es.currentSelectedGameObject != null) return;
            if (confirmRoot.activeSelf) Select(confirmNo);
            else Select(continueButton.interactable ? continueButton : playButton);
        }

        // ---------- Kurulum ----------

        private void Build()
        {
            var th = Theme;
            var root = UiKit.Fill(UiKit.Rect(transform, "MainMenu"));
            var bg = UiKit.Img(root, "Table", th.WoodDark, null, true);
            UiKit.Fill(bg.rectTransform);
            var grain = UiKit.Img(bg.transform, "WoodGrain", UiKit.WithAlpha(th.Wood, 0.55f), UiKit.WoodGrain);
            UiKit.Fill(grain.rectTransform);
            var vignette = UiKit.Img(bg.transform, "Vignette", UiKit.WithAlpha(th.InkDeep, 0.9f), UiKit.Vignette);
            UiKit.Fill(vignette.rectTransform);
            var shade = UiKit.Img(bg.transform, "LeftShade", UiKit.WithAlpha(th.InkDeep, 0.5f), UiKit.FadeLeft);
            UiKit.Fill(shade.rectTransform, 0, 0, 760, 0);
            if (th.Seal != null)
            {
                // Sağda büyük, soluk mühür kabartması (Esc menüsüyle aynı)
                var mark = UiKit.Img(bg.transform, "SealMark", new Color(1f, 1f, 1f, 0.07f), th.Seal);
                UiKit.Place(mark.rectTransform, new Vector2(1f, 0.5f), new Vector2(-180f, 0f), new Vector2(720f, 720f));
                mark.rectTransform.localEulerAngles = new Vector3(0f, 0f, -12f);
                mark.preserveAspect = true;
            }

            // Başlık
            titleText = UiKit.Text(root, "Title", th.HeadingFont, 120f, th.Paper, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
            UiKit.Place(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(left, -150f), new Vector2(1100f, 150f));
            titleText.rectTransform.pivot = new Vector2(0f, 1f);
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            var rule = UiKit.Img(root, "Rule", th.GoldLight);
            UiKit.Place(rule.rectTransform, new Vector2(0f, 1f), new Vector2(left + 4f, -318f), new Vector2(120f, 4f));
            rule.rectTransform.pivot = new Vector2(0f, 1f);
            subtitleText = UiKit.Text(root, "Subtitle", th.HeadingFont, 38f, UiKit.WithAlpha(th.Paper, 0.78f),
                TextAlignmentOptions.TopLeft, FontStyles.Italic);
            UiKit.Place(subtitleText.rectTransform, new Vector2(0f, 1f), new Vector2(left, -340f), new Vector2(1100f, 60f));
            subtitleText.rectTransform.pivot = new Vector2(0f, 1f);

            // Menü maddeleri
            menuColumn = UiKit.Rect(root, "Menu");
            menuColumn.anchorMin = menuColumn.anchorMax = menuColumn.pivot = new Vector2(0f, 0f);
            menuColumn.anchoredPosition = new Vector2(left, 170f);
            menuColumn.sizeDelta = new Vector2(600f, 0f);
            var col = menuColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            col.spacing = 8f;
            col.childControlWidth = col.childControlHeight = true;
            col.childForceExpandWidth = true;
            col.childForceExpandHeight = false;
            menuColumn.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            continueButton = MenuItem(menuColumn, "Continue", th, () => { if (!busy) onContinue?.Invoke(); }, out continueLabel);
            playButton = MenuItem(menuColumn, "Play", th, () => { if (!busy) onPlay?.Invoke(); }, out playLabel);
            quitButton = MenuItem(menuColumn, "Quit", th, () => { if (!busy) onQuit?.Invoke(); }, out quitLabel);

            BuildConfirm(root, th);
        }

        private void BuildConfirm(RectTransform root, UiTheme th)
        {
            var scrim = UiKit.Img(root, "ConfirmScrim", UiKit.WithAlpha(th.InkDeep, 0.72f), null, true);
            UiKit.Fill(scrim.rectTransform);
            confirmRoot = scrim.gameObject;

            var card = UiKit.PaperCard(scrim.transform, "ConfirmCard", th, 6f, out var paper);
            UiKit.Place(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 300f));
            confirmText = UiKit.Text(paper, "Text", th.HeadingFont, 38f, th.InkDark, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Fill(confirmText.rectTransform, 40f, 36f, 40f, 120f);

            var row = UiKit.Rect(paper, "Buttons");
            UiKit.Place(row, new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(560f, 64f));
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = true;
            confirmNo = CardButton(row, "No", th, () => { var c = cancelAction; CloseConfirm(); c?.Invoke(); }, out confirmNoLabel);
            confirmYes = CardButton(row, "Yes", th, () => { var c = confirmAction; confirmRoot.SetActive(false); c?.Invoke(); },
                out confirmYesLabel);
            confirmRoot.SetActive(false);
        }

        private static Button MenuItem(RectTransform parent, string name, UiTheme th, UnityEngine.Events.UnityAction onClick,
            out TextMeshProUGUI label)
        {
            var rt = UiKit.Rect(parent, "Item_" + name);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 84f;
            var row = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 18f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;

            var dash = UiKit.Img(rt, "Dash", th.GoldLight);
            var dle = dash.gameObject.AddComponent<LayoutElement>();
            dle.preferredWidth = 30f;
            dle.preferredHeight = 4f;
            label = UiKit.Text(rt, "Label", th.HeadingFont, 58f, th.Paper, TextAlignmentOptions.Left, FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;

            var b = rt.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(onClick);
            rt.gameObject.AddComponent<MenuFocus>().Init(label, dash, th);
            return b;
        }

        /// <summary>Onay kartı düğmesi: ahşap düğme (harita "Kapat" ile aynı), odakta altın çerçeve.</summary>
        private static Button CardButton(RectTransform parent, string name, UiTheme th, UnityEngine.Events.UnityAction onClick,
            out TextMeshProUGUI label)
        {
            var rt = UiKit.Rect(parent, "Button_" + name);
            var outline = UiKit.Img(rt, "Outline", Color.white, UiKit.Round8, true);
            UiKit.Fill(outline.rectTransform);
            var inner = UiKit.Img(outline.transform, "Inner", th.Wood, UiKit.Round8);
            UiKit.Fill(inner.rectTransform, 4, 4, 4, 4);
            label = UiKit.Text(inner.transform, "Label", th.BodyFont, 30f, th.Paper, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Fill(label.rectTransform);

            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = outline;
            var colors = b.colors;
            colors.normalColor = th.InkDark;
            colors.highlightedColor = th.GoldLight;
            colors.selectedColor = th.GoldLight;
            colors.pressedColor = th.GoldDark;
            colors.disabledColor = UiKit.WithAlpha(th.InkDark, 0.5f);
            colors.fadeDuration = 0.08f;
            b.colors = colors;
            b.onClick.AddListener(onClick);
            rt.gameObject.AddComponent<HoverSelect>();
            return b;
        }

        /// <summary>Menü maddesi odağı: odakta altın yazı + altın kısa çizgi; kapalı madde soluk.</summary>
        private sealed class MenuFocus : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
        {
            private TextMeshProUGUI label;
            private Image dash;
            private UiTheme theme;

            public bool Disabled { get; private set; }

            public void Init(TextMeshProUGUI l, Image d, UiTheme th)
            {
                label = l;
                dash = d;
                theme = th;
                Set(false);
            }

            public void SetDisabled(bool disabled)
            {
                Disabled = disabled;
                Set(false);
            }

            private void Set(bool focused)
            {
                if (label == null) return;
                label.color = Disabled ? UiKit.WithAlpha(theme.Paper, 0.32f) : focused ? theme.GoldLight : theme.Paper;
                dash.color = focused && !Disabled ? theme.GoldLight : new Color(0f, 0f, 0f, 0f);
            }

            public void OnSelect(BaseEventData _) => Set(true);
            public void OnDeselect(BaseEventData _) => Set(false);
            private void OnDisable() => Set(false);

            public void OnPointerEnter(PointerEventData _)
            {
                var s = GetComponent<Selectable>();
                if (s != null && s.IsInteractable()) EventSystem.current?.SetSelectedGameObject(gameObject);
            }
        }

        private sealed class HoverSelect : MonoBehaviour, IPointerEnterHandler
        {
            public void OnPointerEnter(PointerEventData _)
            {
                var s = GetComponent<Selectable>();
                if (s != null && s.IsInteractable()) EventSystem.current?.SetSelectedGameObject(gameObject);
            }
        }
    }
}
