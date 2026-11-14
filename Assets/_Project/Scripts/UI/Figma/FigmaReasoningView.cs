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
    /// Ustanın Sorgusu (IReasoningView) — konuşma balonuyla aynı dil: alttan karartma, altın konuşmacı adı, kâğıt
    /// rengi soru; altında üç seçenek kâğıt şeridi (1/2/3 mum mührü), seçili olanın altın çerçevesi. Seçimden sonra
    /// ustanın cevabı ve defter notu aynı yerde, sağ altta "[E] Devam".
    /// Fare: tıkla. Klavye/gamepad: Move ↑↓ (W/S, sol çubuk) seç, Interact/Jump (E, Space / X, A) onayla;
    /// klavyede 1/2/3 doğrudan seçer. Açıkken envanter gezinmesi kilitli (GameInput.LockInventory).
    /// </summary>
    public sealed class FigmaReasoningView : FigmaView<IReasoningView>, IReasoningView
    {
        private const int MaxOptions = 3;
        private static readonly string[] Letters = { "1", "2", "3" };

        [SerializeField] private float left = 160f;
        [SerializeField] private float textWidth = 1300f;
        [SerializeField] private float inputGrace = 0.35f;

        private GameObject root;
        private RectTransform column, hintRoot, hintKeyRoot;
        private TextMeshProUGUI speaker, body, hintKey, hintWord;
        private readonly Button[] options = new Button[MaxOptions];
        private readonly TextMeshProUGUI[] optionTexts = new TextMeshProUGUI[MaxOptions];
        private readonly Image[] optionFrames = new Image[MaxOptions];

        private Action<int> onChosen;
        private Action onContinue;
        private bool holding; // FigmaDialogueView.Hold: soru açıkken mini harita / defter / envanter çekilir
        private int visible, selected;
        private float shownAt, nextMoveAt;
        private InputAction move, interact, jump, slotKey;

        private void Awake() => Build();

        protected override void OnEnable()
        {
            base.OnEnable();
            GameInput.SchemeChanged += OnScheme;
        }

        protected override void OnDisable()
        {
            Listen(false);
            SetHolding(false);
            GameInput.SchemeChanged -= OnScheme;
            base.OnDisable();
        }

        // ---------- IReasoningView ----------

        public void ShowQuestion(string speakerName, string question, IReadOnlyList<string> optionList, Action<int> chosen)
        {
            onChosen = chosen;
            onContinue = null;
            Open(speakerName, question);
            visible = 0;
            for (int i = 0; i < MaxOptions; i++)
            {
                bool on = optionList != null && i < optionList.Count && !string.IsNullOrEmpty(optionList[i]);
                options[i].gameObject.SetActive(on);
                if (!on) continue;
                optionTexts[i].text = optionList[i];
                visible++;
            }
            hintRoot.gameObject.SetActive(false);
            Select(0);
        }

        public void ShowMessage(string speakerName, string text, string continueWord, Action continued)
        {
            onChosen = null;
            onContinue = continued;
            Open(speakerName, text);
            visible = 0;
            for (int i = 0; i < MaxOptions; i++) options[i].gameObject.SetActive(false);
            hintWord.text = continueWord;
            hintRoot.gameObject.SetActive(true);
            OnScheme(GameInput.LastScheme);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        public void Hide()
        {
            onChosen = null;
            onContinue = null;
            if (root != null) root.SetActive(false);
            Listen(false);
            SetHolding(false);
        }

        private void SetHolding(bool on)
        {
            if (holding == on) return;
            holding = on;
            FigmaDialogueView.Hold(on);
        }

        private void Open(string speakerName, string text)
        {
            speaker.text = speakerName;
            body.text = text;
            root.SetActive(true);
            SetHolding(true);
            shownAt = Time.unscaledTime;
            Listen(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(column);
        }

        // ---------- Girdi ----------

        private void Listen(bool on)
        {
            if (on && move == null)
            {
                move = GameInput.FindAction("Player/Move");
                interact = GameInput.FindAction("Player/Interact");
                jump = GameInput.FindAction("Player/Jump");
                slotKey = GameInput.FindAction("Player/SelectSlot");
                interact.performed += OnSubmit;
                jump.performed += OnSubmit;
                slotKey.performed += OnNumberKey;
                GameInput.LockInventory(true); // soru açıkken envanterde gezinilmez
            }
            else if (!on && move != null)
            {
                if (GameInput.TryGetControls(out _))
                {
                    interact.performed -= OnSubmit;
                    jump.performed -= OnSubmit;
                    slotKey.performed -= OnNumberKey;
                }
                GameInput.LockInventory(false);
                move = interact = jump = slotKey = null;
            }
        }

        /// <summary>Klavyede 1/2/3 doğrudan o şıkkı seçer (envanter kilitliyken aynı tuşlar).</summary>
        private void OnNumberKey(InputAction.CallbackContext ctx)
        {
            if (onChosen == null || visible == 0 || Time.unscaledTime - shownAt < inputGrace) return;
            if (!(ctx.control is UnityEngine.InputSystem.Controls.KeyControl key)) return;
            int index = key.keyCode - Key.Digit1;
            if (index < 0 || index >= visible) return;
            Select(index);
            Choose(index);
        }

        private void Update()
        {
            if (move == null || visible == 0) return;
            float y = move.ReadValue<Vector2>().y;
            if (Mathf.Abs(y) < 0.5f)
            {
                nextMoveAt = 0f;
                return;
            }
            if (Time.unscaledTime < nextMoveAt) return;
            nextMoveAt = Time.unscaledTime + 0.25f;
            Select((selected + (y < 0f ? 1 : visible - 1)) % visible);
        }

        private void OnSubmit(InputAction.CallbackContext _)
        {
            if (Time.unscaledTime - shownAt < inputGrace) return;
            if (onChosen != null && visible > 0) Choose(selected);
            else if (onContinue != null) Continue();
        }

        private void Select(int index)
        {
            selected = Mathf.Clamp(index, 0, Mathf.Max(0, visible - 1));
            var th = Theme;
            for (int i = 0; i < MaxOptions; i++) optionFrames[i].color = i == selected ? th.Gold : th.Wood;
            if (visible > 0) EventSystem.current?.SetSelectedGameObject(options[selected].gameObject);
        }

        private void Choose(int index)
        {
            var cb = onChosen;
            onChosen = null;
            cb?.Invoke(index);
        }

        private void Continue()
        {
            var cb = onContinue;
            onContinue = null;
            cb?.Invoke();
        }

        private void OnScheme(ControlScheme _)
        {
            if (hintKey != null) UiKit.SetKeyCap(hintKey, hintKeyRoot, ControlGlyphs.For(PromptAction.Interact), 40f);
        }

        // ---------- Kurulum ----------

        private void Build()
        {
            var th = Theme;
            var rootRt = UiKit.Fill(UiKit.Rect(transform, "Reasoning"));
            root = rootRt.gameObject;

            // Ekranın tamamı: arkaya tıklanmasın; mesajdayken tıklama devam ettirir.
            var catcher = UiKit.Img(rootRt, "ClickCatcher", new Color(0, 0, 0, 0), null, true);
            UiKit.Fill(catcher.rectTransform);
            catcher.gameObject.AddComponent<Button>().onClick.AddListener(() => { if (onContinue != null) Continue(); });
            var fade = UiKit.Img(rootRt, "BottomFade", UiKit.WithAlpha(th.InkDeep, 0.9f), UiKit.FadeUp);
            var frt = fade.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(1f, 0f);
            frt.pivot = new Vector2(0.5f, 0f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = new Vector2(0f, 620f);

            // Sütun: konuşmacı, soru/metin, seçenekler — alttan yukarı büyür
            column = UiKit.Rect(rootRt, "Column");
            column.anchorMin = column.anchorMax = new Vector2(0f, 0f);
            column.pivot = new Vector2(0f, 0f);
            // Envanter çubuğunun (altta ~150 px) üstünde kalır; sorgu sırasında envanter açık durur.
            column.anchoredPosition = new Vector2(left, 190f);
            column.sizeDelta = new Vector2(textWidth, 0f);
            var col = column.gameObject.AddComponent<VerticalLayoutGroup>();
            col.spacing = 12f;
            col.childControlWidth = col.childControlHeight = true;
            col.childForceExpandWidth = true;
            col.childForceExpandHeight = false;
            column.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            speaker = UiKit.Text(column, "Speaker", th.BodyFont, 24f, th.GoldLight, TextAlignmentOptions.Left, FontStyles.Bold);
            body = UiKit.Text(column, "Body", th.BodyFont, 32f, th.Paper);
            foreach (var t in new[] { speaker, body })
            {
                var sh = t.gameObject.AddComponent<Shadow>();
                sh.effectColor = UiKit.WithAlpha(th.InkDeep, 0.6f);
                sh.effectDistance = new Vector2(0f, -2f);
            }

            for (int i = 0; i < MaxOptions; i++)
            {
                int index = i;
                var opt = UiKit.Rect(column, "Option_" + Letters[i]);
                opt.gameObject.AddComponent<LayoutElement>().minHeight = 64f;
                var frame = UiKit.Img(opt, "Frame", th.Wood, UiKit.Round8, true);
                UiKit.Fill(frame.rectTransform);
                optionFrames[i] = frame;
                var paper = UiKit.Img(frame.transform, "Paper", th.Paper, UiKit.Round8);
                UiKit.Fill(paper.rectTransform, 4, 4, 4, 4);
                var row = opt.gameObject.AddComponent<HorizontalLayoutGroup>();
                row.padding = new RectOffset(14, 22, 10, 10);
                row.spacing = 16f;
                row.childAlignment = TextAnchor.MiddleLeft;
                row.childControlWidth = row.childControlHeight = true;
                row.childForceExpandWidth = false;
                row.childForceExpandHeight = false;
                frame.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

                var wax = UiKit.Img(opt, "Wax", th.Wax, UiKit.Disc);
                var wl = wax.gameObject.AddComponent<LayoutElement>();
                wl.preferredWidth = wl.preferredHeight = 40f;
                var letter = UiKit.Text(wax.transform, "Letter", th.HeadingFont, 24f, th.Paper, TextAlignmentOptions.Center, FontStyles.Bold);
                UiKit.Fill(letter.rectTransform);
                letter.text = Letters[i];
                optionTexts[i] = UiKit.Text(opt, "Text", th.BodyFont, 28f, th.Ink, TextAlignmentOptions.MidlineLeft);
                optionTexts[i].gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

                var btn = opt.gameObject.AddComponent<Button>();
                btn.targetGraphic = frame;
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => Choose(index));
                options[i] = btn;
                var hover = opt.gameObject.AddComponent<OptionHover>();
                hover.Init(() => Select(index));
            }

            // Sağ alt: tuş kapağı + "Devam"
            hintRoot = UiKit.Rect(rootRt, "Hint");
            UiKit.Place(hintRoot, new Vector2(1f, 0f), new Vector2(-72f, 56f), new Vector2(0f, 40f));
            var hrow = hintRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            hrow.spacing = 10f;
            hrow.childAlignment = TextAnchor.MiddleRight;
            hrow.childControlWidth = hrow.childControlHeight = true;
            hrow.childForceExpandWidth = hrow.childForceExpandHeight = false;
            hintRoot.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            hintKey = UiKit.KeyCap(hintRoot, th, 40f, out hintKeyRoot);
            hintWord = UiKit.Text(hintRoot, "Word", th.BodyFont, 26f, th.Paper, TextAlignmentOptions.Left);
            hintWord.textWrappingMode = TextWrappingModes.NoWrap;

            root.SetActive(false);
        }

        /// <summary>Fare üstüne gelince seçimi taşır (gamepad ile aynı vurgu).</summary>
        private sealed class OptionHover : MonoBehaviour, IPointerEnterHandler
        {
            private Action onEnter;
            public void Init(Action enter) => onEnter = enter;
            public void OnPointerEnter(PointerEventData _) => onEnter?.Invoke();
        }
    }
}
