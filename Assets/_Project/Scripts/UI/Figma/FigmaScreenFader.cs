using System.Collections;
using BilgiAdasi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Sahne geçişi kararması (IScreenFader) — marka dili: düz siyah değil en koyu mürekkep (InkDeep) + kenar
    /// gölgesi (vinyet). SceneFlow'a kendini kaydeder; zaman durmuşken de çalışır (unscaled).
    /// HUD Canvas'ının en üstünde durmalı (son kardeş).
    /// </summary>
    public sealed class FigmaScreenFader : MonoBehaviour, IScreenFader
    {
        [Tooltip("Boş = Resources/UiTheme ya da varsayılan palet.")]
        [SerializeField] private UiTheme theme;

        private CanvasGroup group;

        private void Awake()
        {
            var th = UiTheme.OrDefault(theme);
            var root = UiKit.Fill(UiKit.Rect(transform, "Fader"));
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            var fill = UiKit.Img(root, "Fill", th.InkDeep);
            UiKit.Fill(fill.rectTransform);
            var vig = UiKit.Img(root, "Vignette", Color.black, UiKit.Vignette);
            UiKit.Fill(vig.rectTransform);
        }

        private void OnEnable() => SceneFlow.RegisterFader(this);
        private void OnDisable() => SceneFlow.UnregisterFader(this);

        public IEnumerator FadeOut(float duration) => Fade(1f, duration);
        public IEnumerator FadeIn(float duration) => Fade(0f, duration);

        private IEnumerator Fade(float target, float duration)
        {
            float start = group.alpha;
            group.blocksRaycasts = true;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            group.alpha = target;
            group.blocksRaycasts = target > 0.5f;
        }
    }
}
