using System.Collections;
using TMPro;
using UnityEngine;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Hikâye kartı — marka dili: kararmayla aynı en koyu mürekkep (InkDeep) + vinyet, ortada Alegreya italik,
    /// kâğıt renginde tek cümle; altında ince mühür çizgisi. Satır değişince yazı yumuşakça söner/yanar.
    /// HUD'da kararmanın da üstünde (son kardeş) durur. Zaman ölçeğinden bağımsız.
    /// </summary>
    public sealed class FigmaStoryCard : FigmaView<IStoryCardView>, IStoryCardView
    {
        [SerializeField] private float fadeSeconds = 0.6f;
        [SerializeField] private float textFadeSeconds = 0.35f;

        private CanvasGroup group;
        private CanvasGroup textGroup;
        private TextMeshProUGUI label;
        private Coroutine routine;

        private void Awake() => Build();

        public void Show(string text)
        {
            if (routine != null) StopCoroutine(routine);
            gameObject.SetActive(true);
            routine = StartCoroutine(ShowRoutine(text));
        }

        public void ShowImmediate(string text)
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            gameObject.SetActive(true);
            label.text = text;
            textGroup.alpha = 1f;
            group.alpha = 1f;
            group.blocksRaycasts = true;
        }

        public void Hide()
        {
            if (!isActiveAndEnabled) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(HideRoutine());
        }

        private IEnumerator ShowRoutine(string text)
        {
            group.blocksRaycasts = true;
            if (group.alpha < 0.99f)
            {
                label.text = text;
                textGroup.alpha = 1f;
                yield return Fade(group, 1f, fadeSeconds);
            }
            else
            {
                yield return Fade(textGroup, 0f, textFadeSeconds);
                label.text = text;
                yield return Fade(textGroup, 1f, textFadeSeconds);
            }
            routine = null;
        }

        private IEnumerator HideRoutine()
        {
            yield return Fade(group, 0f, fadeSeconds);
            group.blocksRaycasts = false;
            routine = null;
        }

        private static IEnumerator Fade(CanvasGroup g, float target, float duration)
        {
            float start = g.alpha;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                g.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            g.alpha = target;
        }

        private void Build()
        {
            var th = Theme;
            var root = UiKit.Fill(UiKit.Rect(transform, "StoryCard"));
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            var fill = UiKit.Img(root, "Fill", th.InkDeep, null, true);
            UiKit.Fill(fill.rectTransform);
            var vig = UiKit.Img(root, "Vignette", Color.black, UiKit.Vignette);
            UiKit.Fill(vig.rectTransform);

            var textRoot = UiKit.Rect(root, "Text");
            UiKit.Place(textRoot, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1300f, 260f));
            textGroup = textRoot.gameObject.AddComponent<CanvasGroup>();
            label = UiKit.Text(textRoot, "Label", th.HeadingFont, 46f, th.Paper, TextAlignmentOptions.Center, FontStyles.Italic);
            UiKit.Fill(label.rectTransform);

            var rule = UiKit.Img(root, "Rule", UiKit.WithAlpha(th.Gold, 0.55f));
            UiKit.Place(rule.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -130f), new Vector2(180f, 2f));
        }
    }
}
