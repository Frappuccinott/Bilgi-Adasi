using System;
using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// TEST YER TUTUCUSU — Ustanın Sorgusu kutusu (alt orta, parşömen). Mustafa'nınkiyle silinir.
    /// Fare: tıkla. Klavye/gamepad: Move (W/S, sol çubuk) ile seç, Interact/Jump (E, Space / X, A) ile onayla.
    /// Kazanı olan sahnede kendiliğinden kurulur (sahneye elle konmaz).
    /// </summary>
    public sealed class PlaceholderReasoningView : RegisteredView<IReasoningView>, IReasoningView
    {
        private const int MaxOptions = 3;
        private static readonly string[] Letters = { "A)  ", "B)  ", "C)  " };

        // Çalışma anında kurulur; Play'de yeniden derlemeden sağ çıksın diye serileştirilir.
        [SerializeField, HideInInspector] private GameObject panel;
        [SerializeField, HideInInspector] private TMP_Text speakerLabel;
        [SerializeField, HideInInspector] private TMP_Text bodyLabel;
        [SerializeField, HideInInspector] private Button[] options = new Button[MaxOptions];
        [SerializeField, HideInInspector] private TMP_Text[] optionLabels = new TMP_Text[MaxOptions];
        [SerializeField, HideInInspector] private Button continueButton;
        [SerializeField, HideInInspector] private TMP_Text continueLabel;

        private Action<int> onChosen;
        private Action onContinue;
        private int visibleOptions;
        private int selected;
        private float shownAt;
        private float nextMoveAt;
        private InputAction move, interact, jump;

        // ---------- kurulum ----------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Ensure(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode _) => Ensure(scene);

        private static void Ensure(Scene scene)
        {
            if (FindAnyObjectByType<OrderController>() == null) return;
            // Sahnede tasarlanmış görünüm (Figma) varsa yer tutucu kurulmaz.
            bool needReasoning = ViewLocator.Get<IReasoningView>() == null;
            bool needBook = ViewLocator.Get<IDiscoveryBookView>() == null;
            if (!needReasoning && !needBook) return;
            var canvas = PlaceholderLearningCanvas.Get(scene);
            if (needReasoning && canvas.GetComponentInChildren<PlaceholderReasoningView>(true) == null)
                new GameObject("ReasoningView", typeof(RectTransform)).AddComponent<PlaceholderReasoningView>().Build(canvas);
            if (needBook && canvas.GetComponentInChildren<PlaceholderDiscoveryBookView>(true) == null)
                new GameObject("DiscoveryBookView", typeof(RectTransform)).AddComponent<PlaceholderDiscoveryBookView>().Build(canvas);
        }

        private void Build(RectTransform canvas)
        {
            var rt = (RectTransform)transform;
            rt.SetParent(canvas, false);
            PlaceholderUi.Fill(rt, 0, 0, 0, 0);

            var p = PlaceholderUi.Panel(rt, "Panel", PlaceholderUi.Parchment, new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(1400, 470));
            panel = p.gameObject;

            speakerLabel = PlaceholderUi.Text(p, "", 32, PlaceholderUi.Wood, TextAlignmentOptions.TopLeft);
            Place(speakerLabel.rectTransform, 36, 24, 1328, 44);
            speakerLabel.fontStyle = FontStyles.Bold;

            bodyLabel = PlaceholderUi.Text(p, "", 32, PlaceholderUi.Ink, TextAlignmentOptions.TopLeft);
            Place(bodyLabel.rectTransform, 36, 76, 1328, 150);
            bodyLabel.textWrappingMode = TextWrappingModes.Normal;

            for (int i = 0; i < MaxOptions; i++)
            {
                int index = i;
                options[i] = PlaceholderUi.MakeButton(p, "", () => Choose(index), PlaceholderUi.WoodLight, 62);
                Place((RectTransform)options[i].transform, 36, 222 + i * 72, 1328, 62);
                optionLabels[i] = options[i].GetComponentInChildren<TMP_Text>();
                optionLabels[i].alignment = TextAlignmentOptions.MidlineLeft;
                optionLabels[i].fontSize = 28;
                optionLabels[i].textWrappingMode = TextWrappingModes.Normal;
            }

            continueButton = PlaceholderUi.MakeButton(p, "", Continue, PlaceholderUi.WoodLight, 62);
            Place((RectTransform)continueButton.transform, 1124, 388, 240, 62);
            continueLabel = continueButton.GetComponentInChildren<TMP_Text>();

            panel.SetActive(false);
        }

        /// <summary>Panelin sol üst köşesine göre yerleştirir.</summary>
        private static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        // ---------- IReasoningView ----------

        public void ShowQuestion(string speaker, string question, IReadOnlyList<string> optionTexts, Action<int> chosen)
        {
            onChosen = chosen;
            onContinue = null;
            Open(speaker, question);
            visibleOptions = 0;
            for (int i = 0; i < MaxOptions; i++)
            {
                bool on = optionTexts != null && i < optionTexts.Count && !string.IsNullOrEmpty(optionTexts[i]);
                options[i].gameObject.SetActive(on);
                if (!on) continue;
                optionLabels[i].text = Letters[i] + optionTexts[i];
                visibleOptions++;
            }
            continueButton.gameObject.SetActive(false);
            Select(0);
        }

        public void ShowMessage(string speaker, string text, string continueWord, Action continued)
        {
            onChosen = null;
            onContinue = continued;
            Open(speaker, text);
            visibleOptions = 0;
            for (int i = 0; i < MaxOptions; i++) options[i].gameObject.SetActive(false);
            continueButton.gameObject.SetActive(true);
            continueLabel.text = continueWord;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }

        public void Hide()
        {
            onChosen = null;
            onContinue = null;
            if (panel != null) panel.SetActive(false);
            Listen(false);
        }

        private void Open(string speaker, string text)
        {
            panel.SetActive(true);
            speakerLabel.text = speaker;
            bodyLabel.text = text;
            shownAt = Time.unscaledTime;
            Listen(true);
        }

        // ---------- girdi ----------

        private void Listen(bool on)
        {
            if (on && move == null)
            {
                move = GameInput.FindAction("Player/Move");
                interact = GameInput.FindAction("Player/Interact");
                jump = GameInput.FindAction("Player/Jump");
                interact.performed += OnSubmit;
                jump.performed += OnSubmit;
            }
            else if (!on && move != null)
            {
                if (GameInput.TryGetControls(out _))
                {
                    interact.performed -= OnSubmit;
                    jump.performed -= OnSubmit;
                }
                move = interact = jump = null;
            }
        }

        protected override void OnDisable()
        {
            Listen(false);
            base.OnDisable();
        }

        private void Update()
        {
            if (move == null || visibleOptions == 0) return;
            float y = move.ReadValue<Vector2>().y;
            if (Mathf.Abs(y) < 0.5f)
            {
                nextMoveAt = 0f;
                return;
            }
            if (Time.unscaledTime < nextMoveAt) return;
            nextMoveAt = Time.unscaledTime + 0.25f;
            Select((selected + (y < 0 ? 1 : visibleOptions - 1)) % visibleOptions);
        }

        private void Select(int index)
        {
            selected = Mathf.Clamp(index, 0, Mathf.Max(0, visibleOptions - 1));
            if (EventSystem.current != null && visibleOptions > 0)
                EventSystem.current.SetSelectedGameObject(options[selected].gameObject);
        }

        private void OnSubmit(InputAction.CallbackContext _)
        {
            // Açılışta basılı kalan tuş (kazanı yakan E) hemen seçim yapmasın.
            if (Time.unscaledTime - shownAt < 0.35f) return;
            if (onChosen != null && visibleOptions > 0) Choose(selected);
            else if (onContinue != null) Continue();
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
    }

    /// <summary>Öğrenme yer tutucuları için ortak ekran tuvali (1920×1080).</summary>
    internal static class PlaceholderLearningCanvas
    {
        private const string Name = "_Placeholder_LearningUI";

        public static RectTransform Get(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == Name) return (RectTransform)root.transform;

            var go = new GameObject(Name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(go, scene);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return (RectTransform)go.transform;
        }
    }
}
