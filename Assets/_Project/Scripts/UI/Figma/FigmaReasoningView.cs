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
    /// Ustanın Sorgusu / tahmin sorusu (IReasoningView) — normal konuşmayla birebir aynı çerçeve (DialogueFrame):
    /// alttan karartma, portre, altın konuşan adı, kâğıt rengi soru. Altında seçenekler: kâğıt tuş kapağında numara +
    /// kâğıt rengi metin; seçili olan koyu şerit, ince altın çerçeve ve altın yazı. Sağ altta "[E] Seç" (soru) ya da
    /// "[E] Devam" (cevap). Fare: tıkla / üstüne gel. Klavye/gamepad: Move ↑↓ (W/S, ok tuşları, sol çubuk) ya da D-pad ↑↓ seç,
    /// Interact/Confirm (E, Enter, Space / X, A) onayla; klavyede 1/2/3 doğrudan seçer. Açıkken envanter gezinmesi kilitli.
    /// </summary>
    public sealed class FigmaReasoningView : FigmaView<IReasoningView>, IReasoningView
    {
        private const int MaxOptions = 3;

        [SerializeField] private string chooseWord = "Seç";
        [SerializeField] private float inputGrace = 0.35f;

        private GameObject root;
        private DialogueFrame frame;
        private readonly Button[] options = new Button[MaxOptions];
        private readonly TextMeshProUGUI[] optionTexts = new TextMeshProUGUI[MaxOptions];
        private readonly Image[] optionBorders = new Image[MaxOptions];
        private readonly Image[] optionBands = new Image[MaxOptions];

        private Action<int> onChosen;
        private readonly int[] choiceMap = new int[MaxOptions]; // ekrandaki şık → verideki sıra
        private Action onContinue;
        private bool holding; // FigmaDialogueView.Hold: soru açıkken mini harita / defter / envanter çekilir
        private int visible, selected;
        private float shownAt, nextMoveAt;
        private InputAction move, interact, jump, slotKey, dpadUp, dpadDown;

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
            // Şıklar her soruda karışık sırada (Play 14.11: verideki doğru cevap çoğunlukla ilk şıktı). Seçilen şık
            // çağırana verinin kendi sırasıyla döner (choiceMap), denetleyiciler değişmez.
            visible = 0;
            for (int i = 0; optionList != null && i < optionList.Count && visible < MaxOptions; i++)
                if (!string.IsNullOrEmpty(optionList[i])) choiceMap[visible++] = i;
            for (int i = visible - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (choiceMap[i], choiceMap[j]) = (choiceMap[j], choiceMap[i]);
            }
            for (int i = 0; i < MaxOptions; i++)
            {
                bool on = i < visible;
                options[i].transform.parent.gameObject.SetActive(on);
                if (on) optionTexts[i].text = optionList[choiceMap[i]];
            }
            frame.HintWord.text = chooseWord;
            Open(speakerName, question);
            Select(0);
        }

        public void ShowMessage(string speakerName, string text, string continueWord, Action continued)
        {
            onChosen = null;
            onContinue = continued;
            visible = 0;
            for (int i = 0; i < MaxOptions; i++) options[i].transform.parent.gameObject.SetActive(false);
            frame.HintWord.text = continueWord;
            Open(speakerName, text);
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
            frame.SetSpeaker(speakerName, null);
            frame.Body.text = text;
            OnScheme(GameInput.LastScheme);
            root.SetActive(true);
            SetHolding(true);
            shownAt = Time.unscaledTime;
            Listen(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(frame.Column);
        }

        // ---------- Girdi ----------

        private void Listen(bool on)
        {
            if (on && move == null)
            {
                move = GameInput.FindAction("Player/Move");
                interact = GameInput.FindAction("Player/Interact");
                jump = GameInput.FindAction("Player/Confirm"); // Enter, Space / A
                slotKey = GameInput.FindAction("Player/SelectSlot");
                // Gamepad D-pad ↑/↓ Player map'inde defter/harita tuşudur; soru açıkken onlar açılmaz, seçim taşınır.
                dpadUp = GameInput.FindAction("Player/OpenBook");
                dpadDown = GameInput.FindAction("Player/OpenMap");
                interact.performed += OnSubmit;
                jump.performed += OnSubmit;
                slotKey.performed += OnNumberKey;
                dpadUp.performed += OnDpadUp;
                dpadDown.performed += OnDpadDown;
                GameInput.LockInventory(true); // soru açıkken envanterde gezinilmez
            }
            else if (!on && move != null)
            {
                if (GameInput.TryGetControls(out _))
                {
                    interact.performed -= OnSubmit;
                    jump.performed -= OnSubmit;
                    slotKey.performed -= OnNumberKey;
                    dpadUp.performed -= OnDpadUp;
                    dpadDown.performed -= OnDpadDown;
                }
                GameInput.LockInventory(false);
                move = interact = jump = slotKey = dpadUp = dpadDown = null;
            }
        }

        private void OnDpadUp(InputAction.CallbackContext ctx) => StepDpad(ctx, -1);
        private void OnDpadDown(InputAction.CallbackContext ctx) => StepDpad(ctx, 1);

        private void StepDpad(InputAction.CallbackContext ctx, int dir)
        {
            if (onChosen == null || visible == 0) return;
            if (!(ctx.control.parent is UnityEngine.InputSystem.Controls.DpadControl)) return; // N / M / Tab / Select değil
            Select((selected + dir + visible) % visible);
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
            for (int i = 0; i < MaxOptions; i++)
            {
                bool on = i == selected;
                optionBorders[i].enabled = on;
                optionBands[i].enabled = on;
                optionTexts[i].color = on ? th.GoldLight : th.Paper;
            }
            if (visible > 0) EventSystem.current?.SetSelectedGameObject(options[selected].gameObject);
        }

        private void Choose(int index)
        {
            var cb = onChosen;
            onChosen = null;
            cb?.Invoke(index >= 0 && index < visible ? choiceMap[index] : index);
        }

        private void Continue()
        {
            var cb = onContinue;
            onContinue = null;
            cb?.Invoke();
        }

        private void OnScheme(ControlScheme _)
        {
            if (frame != null) UiKit.SetKeyCap(frame.HintKey, frame.HintKeyRoot, ControlGlyphs.For(PromptAction.Interact), 40f);
        }

        // ---------- Kurulum ----------

        private void Build()
        {
            var th = Theme;
            frame = DialogueFrame.Build(transform, "Reasoning", th);
            root = frame.Root.gameObject;
            // Cevapta ekrana tıklamak devam ettirir; soruda yalnızca seçenekler tıklanır.
            frame.Catcher.onClick.AddListener(() => { if (onContinue != null) Continue(); });

            // Seçenekler sütunda metnin altında; aralarında biraz boşluk.
            var gap = UiKit.Rect(frame.Column, "OptionsGap");
            gap.gameObject.AddComponent<LayoutElement>().preferredHeight = 4f;
            for (int i = 0; i < MaxOptions; i++)
            {
                int index = i;
                // Satır tam genişlik tutucu; seçenek içeriği kadar (seçili şerit yazıyı sarar, referanstaki gibi).
                var holder = UiKit.Rect(frame.Column, "OptionRow_" + (i + 1));
                var hl = holder.gameObject.AddComponent<HorizontalLayoutGroup>();
                hl.childAlignment = TextAnchor.UpperLeft;
                hl.childControlWidth = hl.childControlHeight = true;
                hl.childForceExpandWidth = hl.childForceExpandHeight = false;
                var opt = UiKit.Rect(holder, "Option_" + (i + 1));
                var row = opt.gameObject.AddComponent<HorizontalLayoutGroup>();
                row.padding = new RectOffset(10, 18, 6, 6);
                row.spacing = 14f;
                row.childAlignment = TextAnchor.MiddleLeft;
                row.childControlWidth = row.childControlHeight = true;
                row.childForceExpandWidth = false;
                row.childForceExpandHeight = false;
                var hit = opt.gameObject.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f);

                // Seçili: ince altın çerçeve + koyu mürekkep şerit (yalnızca seçiliyken görünür)
                var border = UiKit.Img(opt, "Border", UiKit.WithAlpha(th.GoldDark, 0.8f), UiKit.Round8);
                border.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                UiKit.Fill(border.rectTransform);
                optionBorders[i] = border;
                var band = UiKit.Img(opt, "Band", UiKit.WithAlpha(th.InkDeep, 0.88f), UiKit.Round8);
                band.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                UiKit.Fill(band.rectTransform, 2f, 2f, 2f, 2f);
                optionBands[i] = band;

                var key = UiKit.KeyCap(opt, th, 34f, out var keyRoot);
                UiKit.SetKeyCap(key, keyRoot, "<b>[" + (i + 1) + "]</b>", 34f);
                optionTexts[i] = UiKit.Text(opt, "Text", th.BodyFont, 27f, th.Paper, TextAlignmentOptions.MidlineLeft);
                DialogueFrame.Shade(optionTexts[i], th);

                var btn = opt.gameObject.AddComponent<Button>();
                btn.targetGraphic = hit;
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => Choose(index));
                options[i] = btn;
                opt.gameObject.AddComponent<OptionHover>().Init(() => Select(index));
            }
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
