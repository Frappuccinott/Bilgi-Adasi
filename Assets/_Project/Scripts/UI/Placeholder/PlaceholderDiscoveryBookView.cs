using System;
using System.Collections.Generic;
using System.Text;
using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// TEST YER TUTUCUSU — Keşif Defteri sayfası (orta, parşömen): oyuncunun yazdığı kayıtlar K-1, K-2 …
    /// PlaceholderReasoningView ile birlikte kendiliğinden kurulur. Mustafa'nınkiyle silinir.
    /// </summary>
    public sealed class PlaceholderDiscoveryBookView : RegisteredView<IDiscoveryBookView>, IDiscoveryBookView
    {
        // Çalışma anında kurulur; Play'de yeniden derlemeden sağ çıksın diye serileştirilir.
        [SerializeField, HideInInspector] private GameObject panel;
        [SerializeField, HideInInspector] private TMP_Text titleLabel;
        [SerializeField, HideInInspector] private TMP_Text pagesLabel;
        [SerializeField, HideInInspector] private Button closeButton;
        [SerializeField, HideInInspector] private TMP_Text closeLabel;
        private readonly StringBuilder sb = new StringBuilder(1024);
        private Action closed;

        public bool IsOpen => panel != null && panel.activeSelf;

        internal void Build(RectTransform canvas)
        {
            var rt = (RectTransform)transform;
            rt.SetParent(canvas, false);
            PlaceholderUi.Fill(rt, 0, 0, 0, 0);

            var p = PlaceholderUi.Panel(rt, "Panel", PlaceholderUi.Parchment, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 820));
            p.GetComponent<Image>().raycastTarget = true; // arkadaki menüye tıklanmasın
            panel = p.gameObject;

            titleLabel = PlaceholderUi.Text(p, "", 44, PlaceholderUi.Wood, TextAlignmentOptions.Top);
            PlaceholderUi.Fill(titleLabel.rectTransform, 40, 28, 40, 720);
            titleLabel.fontStyle = FontStyles.Bold;

            pagesLabel = PlaceholderUi.Text(p, "", 30, PlaceholderUi.Ink, TextAlignmentOptions.TopLeft);
            PlaceholderUi.Fill(pagesLabel.rectTransform, 56, 110, 56, 120);
            pagesLabel.textWrappingMode = TextWrappingModes.Normal;
            pagesLabel.overflowMode = TextOverflowModes.Ellipsis;

            closeButton = PlaceholderUi.MakeButton(p, "", Close, PlaceholderUi.WoodLight, 64);
            var crt = (RectTransform)closeButton.transform;
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0f);
            crt.anchoredPosition = new Vector2(0, 36);
            crt.sizeDelta = new Vector2(260, 64);
            closeLabel = closeButton.GetComponentInChildren<TMP_Text>();

            panel.SetActive(false);
        }

        public void Open(string title, IReadOnlyList<string> pages, string emptyText, string closeWord, Action onClosed)
        {
            closed = onClosed;
            titleLabel.text = title;
            closeLabel.text = closeWord;
            sb.Clear();
            for (int i = 0; i < pages.Count; i++) sb.Append(pages[i]).Append("\n\n");
            pagesLabel.text = sb.Length > 0 ? sb.ToString() : emptyText;
            panel.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }

        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false);
            var cb = closed;
            closed = null;
            cb?.Invoke();
        }
    }
}
