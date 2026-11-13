using BilgiAdasi.Core;
using TMPro;
using UnityEngine;

namespace BilgiAdasi.UI
{
    /// <summary>TEST YER TUTUCUSU — kısa ekran notu (üst orta), süre dolunca kaybolur.</summary>
    public sealed class PlaceholderToastView : RegisteredView<IToastView>, IToastView
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text label;

        private float hideAt;

        protected override void OnEnable()
        {
            if (panel != null) panel.SetActive(false);
            base.OnEnable();
        }

        public void Show(string text, float seconds)
        {
            label.text = text;
            panel.SetActive(true);
            hideAt = Time.unscaledTime + seconds;
        }

        private void Update()
        {
            if (panel != null && panel.activeSelf && Time.unscaledTime >= hideAt) panel.SetActive(false);
        }
    }
}
