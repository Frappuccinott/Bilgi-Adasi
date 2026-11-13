using System.Collections.Generic;
using System.Text;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using TMPro;
using UnityEngine;

namespace BilgiAdasi.UI
{
    /// <summary>TEST YER TUTUCUSU — sipariş kartı (sol üst), sayaç tablosu ve kontrol yardımı (alt).</summary>
    public sealed class PlaceholderOrderCardView : RegisteredView<IOrderCardView>, IOrderCardView
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private TMP_Text countsLabel;
        [SerializeField] private TMP_Text helpLabel;
        [SerializeField] private Color okColor = new Color(0.45f, 0.85f, 0.45f);
        [SerializeField] private Color badColor = new Color(0.95f, 0.45f, 0.4f);

        private readonly StringBuilder sb = new StringBuilder(128);

        protected override void OnEnable()
        {
            Hide();
            base.OnEnable();
        }

        public void Show(string title, string body)
        {
            panel.SetActive(true);
            helpLabel.gameObject.SetActive(true);
            titleLabel.text = title;
            bodyLabel.text = body;
        }

        public void SetCounts(IReadOnlyList<AtomCountRow> rows, bool balanced)
        {
            sb.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                var c = rows[i].Equal ? okColor : badColor;
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(c)).Append('>')
                  .Append(rows[i].Element.Symbol).Append("   ").Append(rows[i].Left).Append(" / ").Append(rows[i].Right)
                  .Append(rows[i].Equal ? "   =" : "   !").Append("</color>\n");
            }
            countsLabel.text = sb.ToString();
        }

        public void SetHelp(string text) => helpLabel.text = text;

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            if (helpLabel != null) helpLabel.gameObject.SetActive(false);
        }
    }
}
