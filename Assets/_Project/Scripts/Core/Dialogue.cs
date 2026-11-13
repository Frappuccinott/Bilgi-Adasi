using System;
using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Konuşma akışı. Begin → (Say / Ask …) → End. Açıkken UI map'i açık, Player/Station map'i kapalıdır;
    /// End önceki moda döner. View yoksa satır konsola yazılır ve akış kilitlenmeden sürer.
    /// </summary>
    public static class Dialogue
    {
        private static int depth;
        private static InputMode previousMode = InputMode.None;
        private static readonly List<string> SingleOption = new List<string>(1);

        public static bool IsOpen => depth > 0;

        public static void Begin()
        {
            if (depth++ > 0) return;
            previousMode = GameInput.Mode;
            GameInput.SetMode(InputMode.UI);
        }

        public static void End()
        {
            if (depth == 0) return;
            if (--depth > 0) return;
            ViewLocator.Get<IDialogueView>()?.Hide();
            GameInput.SetMode(previousMode == InputMode.None ? InputMode.Player : previousMode);
        }

        /// <summary>Tek satır; oyuncu "Devam" deyince onDone.</summary>
        public static void Say(string speaker, string text, Action onDone)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                onDone?.Invoke();
                return;
            }
            var view = ViewLocator.Get<IDialogueView>();
            if (view == null)
            {
                Debug.Log($"[Dialogue] {speaker}: {text}");
                onDone?.Invoke();
                return;
            }
            SingleOption.Clear();
            view.Show(speaker, text, null, _ => onDone?.Invoke());
        }

        /// <summary>Seçenekli soru; onChosen seçilen indeksle çağrılır.</summary>
        public static void Ask(string speaker, string text, IReadOnlyList<string> options, Action<int> onChosen)
        {
            var view = ViewLocator.Get<IDialogueView>();
            if (view == null)
            {
                Debug.Log($"[Dialogue] {speaker}: {text} (view yok → 0 seçildi)");
                onChosen?.Invoke(0);
                return;
            }
            view.Show(speaker, text, options, onChosen);
        }

        /// <summary>Begin + satırlar sırayla + End + onDone. Boş satırlar atlanır.</summary>
        public static void Sequence(string speaker, IReadOnlyList<string> lines, Action onDone)
        {
            Begin();
            Next(speaker, lines, 0, () =>
            {
                End();
                onDone?.Invoke();
            });
        }

        private static void Next(string speaker, IReadOnlyList<string> lines, int index, Action onDone)
        {
            if (lines == null || index >= lines.Count)
            {
                onDone?.Invoke();
                return;
            }
            Say(speaker, lines[index], () => Next(speaker, lines, index + 1, onDone));
        }

        public static void Toast(string text, float seconds = 3f)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var view = ViewLocator.Get<IToastView>();
            if (view != null) view.Show(text, seconds);
            else Debug.Log("[Toast] " + text);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            depth = 0;
            previousMode = InputMode.None;
        }
    }
}
