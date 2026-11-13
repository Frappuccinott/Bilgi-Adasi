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
    /// <summary>TEST YER TUTUCUSU — Ustanın defteri: Ustanın Notları + Keşiflerim; parlama göstergesi.</summary>
    public sealed class PlaceholderHintBookView : RegisteredView<IHintBookView>, IHintBookView
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text hintTitleLabel;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private TMP_Text discoveryTitleLabel;
        [SerializeField] private TMP_Text discoveryLabel;
        [SerializeField] private Button closeButton;
        [Tooltip("HUD'daki defter simgesi; parlarken yanıp söner.")]
        [SerializeField] private TMP_Text glowIndicator;

        private readonly StringBuilder sb = new StringBuilder(512);
        private Action closed;
        private bool glowing;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (panel != null) panel.SetActive(false);
            SetGlowing(false);
        }

        private void Update()
        {
            if (!glowing || glowIndicator == null) return;
            glowIndicator.alpha = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
        }

        public void SetGlowing(bool value)
        {
            glowing = value;
            if (glowIndicator == null) return;
            glowIndicator.gameObject.SetActive(value);
            glowIndicator.alpha = 1f;
        }

        public void Open(string hintTitle, string hintText, string discoveryTitle, IReadOnlyList<string> discoveries, Action onClosed)
        {
            closed = onClosed;
            panel.SetActive(true);
            hintTitleLabel.text = hintTitle;
            hintLabel.text = hintText;
            discoveryTitleLabel.text = discoveryTitle;
            sb.Clear();
            for (int i = 0; i < discoveries.Count; i++) sb.Append(discoveries[i]).Append("\n\n");
            discoveryLabel.text = sb.Length > 0 ? sb.ToString() : "—";
            SetGlowing(false);
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
