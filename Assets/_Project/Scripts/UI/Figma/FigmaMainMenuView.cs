using System;
using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Ana menü — Figma "Ekranlar (v2) / Ekran/Giriş" birebir. Arka plan yok: arkada canlı oyun görüntüsü
    /// (MenuFlyover kuş bakışı tur) görünür, üstünde iki degrade scrim (sol + alt). Solda Logo/Yazı (Alegreya 120,
    /// turuncu çizgi), altında UI/Menü öğesi listesi (Normal / Odak / Pasif), "Devam Et"in altında kayıt özeti
    /// (köy, 5 nokta, 3/5); en altta güvenlik notu, sağ altta cihaza göre tuş ipuçları (Seç / Geri).
    /// Ayarlar → FigmaSettingsPanel (Esc menüsündekiyle aynı sayfa), Emeği Geçenler → Figma Ekran/Emeği Geçenler.
    /// Görünüm yalnızca IMainMenuView; ne yapılacağı denetleyiciden gelir.
    /// </summary>
    public sealed class FigmaMainMenuView : FigmaView<IMainMenuView>, IMainMenuView
    {
        [Tooltip("Ayarlar sayfasının varsayılanları (Esc menüsüyle aynı asset). Boşsa sayfa yine çalışır.")]
        [SerializeField] private PlayerMovementSettings movementSettings;

        // Figma ölçüleri (1920×1080 referans, sol üst köşeden)
        private const float Left = 120f;
        private const float LogoTop = 150f;
        private const float MenuTop = 470f;

        private struct HintSlot
        {
            public TextMeshProUGUI Label;
            public RectTransform Root;
            public PromptAction Action;
        }

        private readonly List<HintSlot> hints = new List<HintSlot>();
        private GameObject mainPage, settingsPage, creditsPage;
        private FigmaSettingsPanel settingsPanel;
        private TextMeshProUGUI logoText, creditsLogo, disclaimerText, hintSelectWord, hintBackWord, creditsBackWord;
        private Button continueButton, newGameButton, settingsButton, creditsButton, quitButton;
        private TextMeshProUGUI continueLabel, newGameLabel, settingsLabel, creditsLabel, quitLabel;
        private GameObject summaryRow;
        private TextMeshProUGUI summaryPlace, summaryCount;
        private readonly Image[] summaryDots = new Image[5];
        private RectTransform creditsColumn;
        private TextMeshProUGUI creditsNote, creditsTeaser;
        private GameObject confirmRoot;
        private TextMeshProUGUI confirmText, confirmYesLabel, confirmNoLabel;
        private Button confirmYes, confirmNo;
        private Action onNewGame, onContinue, onQuit, confirmAction, cancelAction;
        private MenuTexts texts;
        private bool busy;

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

        // ---------- IMainMenuView ----------

        public void Show(MenuTexts t, bool canContinue, MainMenuSaveSummary summary, Action newGame, Action cont, Action quit)
        {
            texts = t;
            onNewGame = newGame;
            onContinue = cont;
            onQuit = quit;

            logoText.text = creditsLogo.text = t.Title;
            continueLabel.text = t.ContinueGame;
            newGameLabel.text = t.NewGame;
            settingsLabel.text = t.Settings;
            creditsLabel.text = t.Credits;
            quitLabel.text = t.Quit;
            disclaimerText.text = t.Disclaimer;
            hintSelectWord.text = t.HintSelect;
            hintBackWord.text = creditsBackWord.text = t.HintBack;
            confirmText.text = t.NewGameWarning;
            confirmYesLabel.text = t.ConfirmNewGame;
            confirmNoLabel.text = t.Cancel;

            ShowSummary(canContinue ? summary : null);
            FillCredits(t);

            continueButton.interactable = canContinue;
            continueButton.GetComponent<MenuFocus>().SetDisabled(!canContinue);
            OnScheme(GameInput.LastScheme);
            ShowMain(canContinue ? continueButton : newGameButton);
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

        public bool TryGoBack()
        {
            if (busy) return false;
            if (confirmRoot.activeSelf)
            {
                var c = cancelAction;
                CloseConfirm();
                c?.Invoke();
                return true;
            }
            if (settingsPage.activeSelf)
            {
                ShowMain(settingsButton);
                return true;
            }
            if (creditsPage.activeSelf)
            {
                ShowMain(creditsButton);
                return true;
            }
            return false;
        }

        // ---------- Sayfalar ----------

        private void ShowMain(Selectable select)
        {
            settingsPage.SetActive(false);
            creditsPage.SetActive(false);
            mainPage.SetActive(true);
            Select(select);
        }

        private void ShowSettings()
        {
            if (busy) return;
            mainPage.SetActive(false);
            settingsPage.SetActive(true);
            settingsPanel.Show();
            OnScheme(GameInput.LastScheme);
        }

        private void ShowCredits()
        {
            if (busy) return;
            mainPage.SetActive(false);
            creditsPage.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void ShowSummary(MainMenuSaveSummary s)
        {
            summaryRow.SetActive(s != null && s.Total > 0);
            if (!summaryRow.activeSelf || texts == null) return;
            var th = Theme;
            summaryPlace.text = string.Format(texts.PlaceFormat, s.Place);
            summaryCount.text = string.Format(texts.ProgressFormat, s.Done, s.Total);
            for (int i = 0; i < summaryDots.Length; i++)
            {
                bool on = i < summaryDots.Length && i < s.Total;
                summaryDots[i].gameObject.SetActive(on);
                bool done = i < s.Done;
                summaryDots[i].sprite = done ? UiKit.Disc : UiKit.RingThin;
                summaryDots[i].color = done ? th.Gold : UiKit.WithAlpha(th.Paper, 0.6f);
            }
        }

        private void SetMenuInteractable(bool on)
        {
            newGameButton.interactable = on;
            settingsButton.interactable = on;
            creditsButton.interactable = on;
            quitButton.interactable = on;
            continueButton.interactable = on && !continueButton.GetComponent<MenuFocus>().Disabled;
        }

        private void CloseConfirm()
        {
            confirmRoot.SetActive(false);
            SetMenuInteractable(true);
            Select(newGameButton);
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
            // Gamepad/klavye: seçim kaybolursa (fare boşluğa tıkladı) ilk geçerli maddeye dön (yalnızca ana sayfada).
            var es = EventSystem.current;
            if (es == null || busy || es.currentSelectedGameObject != null || !mainPage.activeSelf) return;
            if (confirmRoot.activeSelf) Select(confirmNo);
            else Select(continueButton.interactable ? continueButton : newGameButton);
        }

        private void OnScheme(ControlScheme scheme)
        {
            for (int i = 0; i < hints.Count; i++)
                UiKit.SetKeyCap(hints[i].Label, hints[i].Root, ControlGlyphs.For(hints[i].Action, scheme), 44f);
        }

        // ---------- Kurulum ----------

        private void Build()
        {
            var th = Theme;
            var root = UiKit.Fill(UiKit.Rect(transform, "MainMenu"));

            // Scrim sol: #24170F 88% → 62% (30%) → 0 (55%), soldan sağa
            var scrimLeft = UiKit.Img(root, "ScrimLeft", th.ScrimInk,
                UiKit.Gradient(false, new Vector2(0f, 0.88f), new Vector2(0.30f, 0.62f), new Vector2(0.55f, 0f), new Vector2(1f, 0f)));
            UiKit.Fill(scrimLeft.rectTransform);
            // Scrim alt: %72'ye kadar saydam, en altta %70
            var scrimBottom = UiKit.Img(root, "ScrimBottom", th.ScrimInk,
                UiKit.Gradient(true, new Vector2(0f, 0f), new Vector2(0.72f, 0f), new Vector2(1f, 0.7f)));
            UiKit.Fill(scrimBottom.rectTransform);

            mainPage = UiKit.Fill(UiKit.Rect(root, "Main")).gameObject;
            var main = (RectTransform)mainPage.transform;

            logoText = Logo(main, th, new Vector2(0f, 1f), new Vector2(Left, -LogoTop));

            // Menü (Figma "Menü": x 120, y 470, dikey boşluk 4)
            var menu = UiKit.Rect(main, "Menu");
            UiKit.Place(menu, new Vector2(0f, 1f), new Vector2(Left, -MenuTop), new Vector2(700f, 0f));
            var col = menu.gameObject.AddComponent<VerticalLayoutGroup>();
            col.spacing = 4f;
            col.childAlignment = TextAnchor.UpperLeft;
            col.childControlWidth = col.childControlHeight = true;
            col.childForceExpandWidth = col.childForceExpandHeight = false;
            menu.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            continueButton = MenuItem(menu, "Continue", th, () => { if (!busy) onContinue?.Invoke(); }, out continueLabel);
            BuildSummary(menu, th);
            newGameButton = MenuItem(menu, "NewGame", th, () => { if (!busy) onNewGame?.Invoke(); }, out newGameLabel);
            settingsButton = MenuItem(menu, "Settings", th, ShowSettings, out settingsLabel);
            creditsButton = MenuItem(menu, "Credits", th, ShowCredits, out creditsLabel);
            quitButton = MenuItem(menu, "Quit", th, () => { if (!busy) onQuit?.Invoke(); }, out quitLabel);

            // Güvenlik notu (x 120, y 1008; Alegreya Sans italik 22, %60)
            disclaimerText = UiKit.Text(main, "Disclaimer", th.BodyItalicFont, 22f, UiKit.WithAlpha(th.Paper, 0.6f),
                TextAlignmentOptions.TopLeft, th.HasBodyItalic ? FontStyles.Normal : FontStyles.Italic);
            UiKit.Place(disclaimerText.rectTransform, new Vector2(0f, 0f), new Vector2(Left, 1080f - 1008f - 26f), new Vector2(1300f, 26f));
            disclaimerText.textWrappingMode = TextWrappingModes.NoWrap;

            // Tuş ipuçları (sağ alt: sağdan 120, y 996, 44 yükseklik, aralık 36)
            var bar = HintBar(main, new Vector2(1f, 0f), new Vector2(-Left, 1080f - 996f - 44f));
            hintSelectWord = AddHint(bar, th, PromptAction.Confirm, 0.92f, false);
            hintBackWord = AddHint(bar, th, PromptAction.Cancel, 0.92f, false);

            BuildSettings(root, th);
            BuildCredits(root, th);
            BuildConfirm(root, th);
        }

        /// <summary>Logo/Yazı (arayüz): Alegreya 120 (Black ağırlığı), harf aralığı −1.2 px, gölge 0 4 16; altında 140×6 turuncu çizgi.</summary>
        private static TextMeshProUGUI Logo(RectTransform parent, UiTheme th, Vector2 anchor, Vector2 pos)
        {
            var logo = UiKit.Rect(parent, "Logo");
            UiKit.Place(logo, anchor, pos, new Vector2(545f, 130f));
            var text = UiKit.Text(logo, "Text", th.HeadingBlackFont, 120f, th.Paper, TextAlignmentOptions.TopLeft,
                th.HasHeadingBlack ? FontStyles.Normal : FontStyles.Bold);
            UiKit.Place(text.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(545f, 120f));
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.characterSpacing = -1f;   // −1.2 px / 120 px = −1 em/100
            text.alignment = TextAlignmentOptions.Left;   // CSS leading:1 → satır kutusunda dikey ortalı
            UiKit.TextShadow(text, UiKit.WithAlpha(th.ShadowInk, 0.6f), 4f, 16f);
            var rule = UiKit.Img(logo, "Rule", th.LogoAccent, UiKit.Round4);
            UiKit.Place(rule.rectTransform, new Vector2(0f, 1f), new Vector2(0f, -124f), new Vector2(140f, 6f));
            return text;
        }

        /// <summary>Kayıt özeti: sol 40, aralık 14, alt 18; köy (Alegreya italik 28, %85), 5 nokta (12 px, aralık 6), 3/5.</summary>
        private void BuildSummary(RectTransform parent, UiTheme th)
        {
            var row = UiKit.Rect(parent, "SaveSummary");
            summaryRow = row.gameObject;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(40, 0, 0, 18);
            h.spacing = 14f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

            summaryPlace = UiKit.Text(row, "Place", th.HeadingItalicFont, 28f, UiKit.WithAlpha(th.Paper, 0.85f),
                TextAlignmentOptions.Left, th.HasHeadingItalic ? FontStyles.Normal : FontStyles.Italic);
            summaryPlace.textWrappingMode = TextWrappingModes.NoWrap;
            summaryPlace.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;

            var dots = UiKit.Rect(row, "Dots");
            var dl = dots.gameObject.AddComponent<HorizontalLayoutGroup>();
            dl.spacing = 6f;
            dl.childAlignment = TextAnchor.MiddleLeft;
            dl.childControlWidth = dl.childControlHeight = true;
            dl.childForceExpandWidth = dl.childForceExpandHeight = false;
            var dle = dots.gameObject.AddComponent<LayoutElement>();
            dle.preferredWidth = 84f;
            dle.preferredHeight = 12f;
            for (int i = 0; i < summaryDots.Length; i++)
            {
                summaryDots[i] = UiKit.Img(dots, "Dot" + (i + 1), th.Gold, UiKit.Disc);
                var le = summaryDots[i].gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = le.preferredHeight = 12f;
            }

            summaryCount = UiKit.Text(row, "Count", th.BodyFont, 24f, UiKit.WithAlpha(th.Paper, 0.6f), TextAlignmentOptions.Left);
            summaryCount.textWrappingMode = TextWrappingModes.NoWrap;
            summaryRow.SetActive(false);
        }

        private void BuildSettings(RectTransform root, UiTheme th)
        {
            var page = UiKit.Fill(UiKit.Rect(root, "Settings"));
            settingsPage = page.gameObject;
            var dim = UiKit.Img(page, "Dim", UiKit.WithAlpha(th.ShadowInk, 0.8f), null, true);
            UiKit.Fill(dim.rectTransform);
            var bar = HintBar(page, new Vector2(0f, 0f), new Vector2(Left, 52f));
            settingsPanel = page.gameObject.AddComponent<FigmaSettingsPanel>();
            settingsPanel.Init(th, movementSettings, (action, word) =>
            {
                var w = AddHint(bar, th, action, 1f, false);
                w.text = word;
            });
            settingsPage.SetActive(false);
        }

        /// <summary>Figma Ekran/Emeği Geçenler: karartma %80, ortada logo (y 110) ve jenerik (y 330, aralık 34), alt degrade, "Devam edecek".</summary>
        private void BuildCredits(RectTransform root, UiTheme th)
        {
            var page = UiKit.Fill(UiKit.Rect(root, "Credits"));
            creditsPage = page.gameObject;
            var dim = UiKit.Img(page, "Dim", UiKit.WithAlpha(th.ShadowInk, 0.8f), null, true);
            UiKit.Fill(dim.rectTransform);

            creditsLogo = Logo(page, th, new Vector2(0.5f, 1f), new Vector2(0f, -110f));

            creditsColumn = UiKit.Rect(page, "Column");
            UiKit.Place(creditsColumn, new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(1400f, 0f));
            var v = creditsColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 34f;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            creditsColumn.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var fade = UiKit.Img(page, "BottomFade", th.ShadowInk, UiKit.Gradient(true, new Vector2(0f, 0f), new Vector2(1f, 0.9f)));
            fade.rectTransform.anchorMin = new Vector2(0f, 0f);
            fade.rectTransform.anchorMax = new Vector2(1f, 0f);
            fade.rectTransform.pivot = new Vector2(0.5f, 0f);
            fade.rectTransform.anchoredPosition = Vector2.zero;
            fade.rectTransform.sizeDelta = new Vector2(0f, 200f);

            creditsTeaser = UiKit.Text(page, "Teaser", th.HeadingItalicFont, 34f, th.GoldLight, TextAlignmentOptions.Center,
                th.HasHeadingItalic ? FontStyles.Normal : FontStyles.Italic);
            UiKit.Place(creditsTeaser.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 1080f - 960f - 46f), new Vector2(1200f, 46f));
            creditsTeaser.textWrappingMode = TextWrappingModes.NoWrap;

            var bar = HintBar(page, new Vector2(1f, 0f), new Vector2(-Left, 1080f - 990f - 44f));
            bar.GetComponent<HorizontalLayoutGroup>().spacing = 36f;
            creditsBackWord = AddHint(bar, th, PromptAction.Cancel, 0.9f, true);
            creditsPage.SetActive(false);
        }

        private void FillCredits(MenuTexts t)
        {
            var th = Theme;
            for (int i = creditsColumn.childCount - 1; i >= 0; i--) Destroy(creditsColumn.GetChild(i).gameObject);
            foreach (var e in t.CreditEntries)
            {
                var block = UiKit.Rect(creditsColumn, "Entry");
                var v = block.gameObject.AddComponent<VerticalLayoutGroup>();
                v.spacing = 8f;
                v.childAlignment = TextAnchor.UpperCenter;
                v.childControlWidth = v.childControlHeight = true;
                v.childForceExpandWidth = true;
                v.childForceExpandHeight = false;
                if (!string.IsNullOrEmpty(e.role))
                {
                    var role = UiKit.Text(block, "Role", th.BodyFont, 24f, UiKit.WithAlpha(th.GoldLight, 0.85f), TextAlignmentOptions.Center);
                    role.text = e.role;
                }
                var name = UiKit.Text(block, "Name", th.HeadingFont, 38f, th.Paper, TextAlignmentOptions.Center);   // Alegreya Bold
                name.text = e.name;
            }
            creditsNote = UiKit.Text(creditsColumn, "Note", th.BodyRegularFont, 26f, UiKit.WithAlpha(th.Paper, 0.6f), TextAlignmentOptions.Center);
            creditsNote.text = t.CreditsNote;
            creditsNote.gameObject.SetActive(!string.IsNullOrEmpty(t.CreditsNote));
            creditsTeaser.text = t.CreditsTeaser;
        }

        private void BuildConfirm(RectTransform root, UiTheme th)
        {
            var scrim = UiKit.Img(root, "ConfirmScrim", UiKit.WithAlpha(th.ShadowInk, 0.72f), null, true);
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

        private static RectTransform HintBar(RectTransform parent, Vector2 anchor, Vector2 pos)
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

        /// <summary>Tuş kapağı (44) + kelime (Alegreya Sans Medium 26). Figma: aralık 12 (Emeği Geçenler'de 8 + gölge).</summary>
        private TextMeshProUGUI AddHint(RectTransform bar, UiTheme th, PromptAction action, float alpha, bool compact)
        {
            var slot = UiKit.Rect(bar, "Hint_" + action);
            var row = slot.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = compact ? 8f : 12f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var key = UiKit.KeyCap(slot, th, 44f, out var keyRoot);
            var w = UiKit.Text(slot, "Word", th.BodyFont, 26f, UiKit.WithAlpha(th.Paper, alpha), TextAlignmentOptions.Left);
            w.textWrappingMode = TextWrappingModes.NoWrap;
            if (compact) UiKit.TextShadow(w, UiKit.WithAlpha(th.ShadowInk, 0.7f), 2f, 8f);
            hints.Add(new HintSlot { Label = key, Root = keyRoot, Action = action });
            return w;
        }

        /// <summary>UI/Menü öğesi: 28×3 odak çizgisi (yalnızca odakta), aralık 18, Alegreya Bold 46, gölge 0 2 8; yükseklik 71.</summary>
        private static Button MenuItem(RectTransform parent, string name, UiTheme th, UnityEngine.Events.UnityAction onClick,
            out TextMeshProUGUI label)
        {
            var rt = UiKit.Rect(parent, "Item_" + name);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 71f;
            var row = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 18f;
            row.padding = new RectOffset(0, 0, 4, 4);
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;

            var dash = UiKit.Img(rt, "Dash", th.GoldLight);
            var dle = dash.gameObject.AddComponent<LayoutElement>();
            dle.preferredWidth = 28f;
            dle.preferredHeight = 3f;
            label = UiKit.Text(rt, "Label", th.HeadingFont, 46f, th.Paper, TextAlignmentOptions.Left);   // Alegreya Bold (gerçek ağırlık)
            label.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.TextShadow(label, UiKit.WithAlpha(th.ShadowInk, 0.55f), 2f, 8f);

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

        /// <summary>Menü öğesi durumları (Figma UI/Menü öğesi): Normal %78 kâğıt, Odak altın + çizgi, Pasif %35 kâğıt.</summary>
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
                label.color = Disabled ? UiKit.WithAlpha(theme.Paper, 0.35f)
                    : focused ? theme.GoldLight : UiKit.WithAlpha(theme.Paper, 0.78f);
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
