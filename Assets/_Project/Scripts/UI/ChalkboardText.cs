using System.Collections.Generic;
using System.Text;
using BilgiAdasi.Core;
using TMPro;
using UnityEngine;

namespace BilgiAdasi.UI
{
    /// <summary>Karatahtanın 3D yazısı (IChalkboardView): çözülen denklemler, dünya uzayı TMP. Tahta modelinin önüne konur.</summary>
    public sealed class ChalkboardText : RegisteredView<IChalkboardView>, IChalkboardView
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private string emptyText = "";

        [Tooltip("Açık: yazı tema fontuyla (Alegreya Sans) çizilir; kapalı: sahnedeki TMP fontu kalır.")]
        [SerializeField] private bool useThemeFont = true;

        private readonly StringBuilder sb = new StringBuilder(256);

        private void Awake()
        {
            if (!useThemeFont || text == null) return;
            var font = UiTheme.OrDefault(null).BodyFont;
            if (font != null) text.font = font;
        }

        public void ShowEquations(IReadOnlyList<string> richEquations)
        {
            if (text == null) return;
            sb.Clear();
            if (richEquations.Count == 0) sb.Append(emptyText);
            for (int i = 0; i < richEquations.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(richEquations[i]);
            }
            text.text = sb.ToString();
        }
    }
}
