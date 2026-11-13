using BilgiAdasi.Core;
using TMPro;
using UnityEngine;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Hata Dedektifi'nin karatahtadaki 3D yazısı (IErrorHuntView). Açıkken normal tahta yazısı (çözülen denklemler)
    /// gizlenir. Bekleyen iş varsa tahtanın köşesinde tebeşirle büyük bir "?" durur.
    /// </summary>
    public sealed class ChalkboardHuntView : RegisteredView<IErrorHuntView>, IErrorHuntView
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [SerializeField] private TMP_Text footer;
        [Tooltip("Dedektif açıkken gizlenen normal tahta yazısı.")]
        [SerializeField] private GameObject boardText;
        [Tooltip("Bekleyen iş işareti (\"?\").")]
        [SerializeField] private GameObject pendingMark;
        [SerializeField] private bool useThemeFont = true;

        private void Awake()
        {
            if (useThemeFont)
            {
                var font = UiTheme.OrDefault(null).BodyFont;
                if (font != null)
                {
                    if (title != null) title.font = font;
                    if (body != null) body.font = font;
                    if (footer != null) footer.font = font;
                }
            }
            SetShown(false);
        }

        public void ShowHunt(string titleText, string bodyText, string footerText)
        {
            SetShown(true);
            if (title != null) title.text = titleText;
            if (body != null) body.text = bodyText;
            if (footer != null) footer.text = footerText;
        }

        public void HideHunt() => SetShown(false);

        public void SetPending(bool pending)
        {
            if (pendingMark != null) pendingMark.SetActive(pending && (body == null || !body.gameObject.activeSelf));
        }

        private void SetShown(bool shown)
        {
            if (title != null) title.gameObject.SetActive(shown);
            if (body != null) body.gameObject.SetActive(shown);
            if (footer != null) footer.gameObject.SetActive(shown);
            if (boardText != null) boardText.SetActive(!shown);
            if (shown && pendingMark != null) pendingMark.SetActive(false);
        }
    }
}
