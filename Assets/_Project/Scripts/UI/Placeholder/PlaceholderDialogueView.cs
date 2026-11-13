using System;
using System.Collections.Generic;
using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>TEST YER TUTUCUSU — konuşma balonu: konuşan, metin, en çok 3 seçenek ya da "Devam".</summary>
    public sealed class PlaceholderDialogueView : RegisteredView<IDialogueView>, IDialogueView
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text speakerLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private Button[] optionButtons = Array.Empty<Button>();
        [SerializeField] private Button continueButton;
        [SerializeField] private string continueText = "Devam";

        private static readonly string[] Letters = { "A) ", "B) ", "C) ", "D) " };
        private Action<int> callback;

        private void Awake()
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                optionButtons[i].onClick.AddListener(() => Choose(index));
            }
            if (continueButton != null) continueButton.onClick.AddListener(() => Choose(0));
            if (panel != null) panel.SetActive(false);
        }

        public void Show(string speaker, string text, IReadOnlyList<string> options, Action<int> onChosen)
        {
            callback = onChosen;
            panel.SetActive(true);
            speakerLabel.text = speaker;
            bodyLabel.text = text;

            bool hasOptions = options != null && options.Count > 0;
            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool on = hasOptions && i < options.Count && !string.IsNullOrEmpty(options[i]);
                optionButtons[i].gameObject.SetActive(on);
                if (on) optionButtons[i].GetComponentInChildren<TMP_Text>().text = Letters[i % Letters.Length] + options[i];
            }
            continueButton.gameObject.SetActive(!hasOptions);
            continueButton.GetComponentInChildren<TMP_Text>().text = continueText;

            var first = hasOptions ? optionButtons[0].gameObject : continueButton.gameObject;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(first);
            }
        }

        public void Hide()
        {
            callback = null;
            if (panel != null) panel.SetActive(false);
        }

        private void Choose(int index)
        {
            var cb = callback;
            callback = null;
            if (cb == null) return;
            cb(index);
            // Geri çağırma yeni bir satır açmadıysa ve konuşma bittiyse paneli kapat.
            if (callback == null && panel != null && !Dialogue.IsOpen) panel.SetActive(false);
        }
    }
}
