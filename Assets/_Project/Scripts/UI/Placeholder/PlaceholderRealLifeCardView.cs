using System;
using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>TEST YER TUTUCUSU — "Gerçek hayatta" kartı.</summary>
    public sealed class PlaceholderRealLifeCardView : RegisteredView<IRealLifeCardView>, IRealLifeCardView
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private Button closeButton;

        private Action closed;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (panel != null) panel.SetActive(false);
        }

        public void Show(string title, string text, Action onClosed)
        {
            closed = onClosed;
            panel.SetActive(true);
            titleLabel.text = title;
            bodyLabel.text = text;
            if (EventSystem.current != null && closeButton != null) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }

        private void Close()
        {
            panel.SetActive(false);
            var cb = closed;
            closed = null;
            cb?.Invoke();
        }
    }
}
