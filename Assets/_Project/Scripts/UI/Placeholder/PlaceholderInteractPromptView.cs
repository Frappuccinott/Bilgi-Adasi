using BilgiAdasi.Core;
using TMPro;
using UnityEngine;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// TEST YER TUTUCUSU — ham TMP metniyle crosshair + etkileşim ipucu.
    /// Mustafa'nın HUD'u gelince sahnedeki _Placeholder objesiyle birlikte silinir.
    /// Gerçek view aynı arayüzü uygular: RegisteredView&lt;IInteractPromptView&gt;, IInteractPromptView.
    /// </summary>
    public sealed class PlaceholderInteractPromptView : RegisteredView<IInteractPromptView>, IInteractPromptView
    {
        [SerializeField] private TMP_Text promptLabel;
        [SerializeField] private GameObject crosshair;

        protected override void OnEnable()
        {
            HidePrompt();
            base.OnEnable();
        }

        public void ShowPrompt(string text, string glyph)
        {
            if (promptLabel == null) return;
            promptLabel.text = glyph + "  " + text;
            promptLabel.gameObject.SetActive(true);
        }

        public void HidePrompt()
        {
            if (promptLabel != null) promptLabel.gameObject.SetActive(false);
        }

        public void SetCrosshairVisible(bool visible)
        {
            if (crosshair != null) crosshair.SetActive(visible);
        }
    }
}
