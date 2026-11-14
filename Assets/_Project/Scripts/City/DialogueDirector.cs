using System;
using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.Player;
using BilgiAdasi.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Konuşma sırası: satırları IDialogueView'a tek tek verir. Kamera değişmez.
    /// Oyuncuyla yüz yüze konuşmada (Play'e interactor verilirse) hareket durur, imleç görünür; Interact / Confirm
    /// (E, Enter, Space / X, A) ya da fareyle tıklama (view → IDialogueCommands.Advance) ilerletir. Interactor verilmezse satırlar kendiliğinden akar.
    /// Görünümü yalnızca arayüzle tanır.
    /// </summary>
    public sealed class DialogueDirector : MonoBehaviour, IDialogueCommands
    {
        [Tooltip("Kendiliğinden akan konuşmada bir satırın ekranda kalma süresi (sn).")]
        [SerializeField] private float autoAdvanceSeconds = 4f;
        [Tooltip("Açan tuşun aynı basışla ilk satırı geçmesini önler (sn).")]
        [SerializeField] private float inputGrace = 0.2f;

        private readonly List<DialogueLine> lines = new List<DialogueLine>();
        private int index = -1;
        private Action onFinished;
        private float lineShownAt;
        private PlayerInteractor user;
        private InputAction interactAction, jumpAction;
        private IDialogueView view;

        public bool IsPlaying => index >= 0;

        private void OnEnable()
        {
            ViewLocator.Changed += OnViewsChanged;
            view = ViewLocator.Get<IDialogueView>();
        }

        private void OnDisable()
        {
            ViewLocator.Changed -= OnViewsChanged;
            ClearDeferred();
            Finish();
        }

        // ---------- Ertelenen konuşma ----------

        private readonly List<DialogueLine> deferredLines = new List<DialogueLine>();
        private Action deferredFinished;
        private PlayerInteractor deferredUser;

        private void Defer(IReadOnlyList<DialogueLine> source, Action finished, PlayerInteractor blockingUser)
        {
            ClearDeferred();
            for (int i = 0; i < source.Count; i++) deferredLines.Add(source[i]); // çağıran listesini yeniden kullanabilir
            deferredFinished = finished;
            deferredUser = blockingUser;
            GameInput.ModeChanged += OnModeChangedWhileDeferred;
        }

        private void ClearDeferred()
        {
            if (deferredUser == null) return;
            GameInput.ModeChanged -= OnModeChangedWhileDeferred;
            deferredLines.Clear();
            deferredFinished = null;
            deferredUser = null;
        }

        private void OnModeChangedWhileDeferred(InputMode mode)
        {
            if (mode != InputMode.Player || deferredUser == null) return;
            var copy = new List<DialogueLine>(deferredLines);
            var finished = deferredFinished;
            var user = deferredUser;
            ClearDeferred();
            Play(copy, finished, user);
        }

        /// <summary>Konuşmayı başlatır; sürmekte olan konuşma önce biter. blockingUser verilirse hareket durur.</summary>
        public void Play(IReadOnlyList<DialogueLine> source, Action finished, PlayerInteractor blockingUser = null)
        {
            // Pazar / Esc menüsü / harita gibi bir ekran açıkken birebir konuşma onun üstüne açılmasın: tam ekran
            // tıklama yakalayıcısı ekranın fare girdisini yutar, UI modunda E/Space de konuşmayı ilerletemez.
            // Ekran kapanıp oyuncu moduna dönülünce başlar.
            if (blockingUser != null && GameInput.Mode == InputMode.UI)
            {
                Defer(source, finished, blockingUser);
                return;
            }
            ClearDeferred();
            Finish();
            lines.Clear();
            for (int i = 0; i < source.Count; i++)
                if (!string.IsNullOrEmpty(source[i].Text)) lines.Add(source[i]);

            onFinished = finished;
            if (lines.Count == 0)
            {
                InvokeFinished();
                return;
            }

            ViewLocator.Register<IDialogueCommands>(this);
            if (blockingUser != null)
            {
                user = blockingUser;
                user.SetControlEnabled(false);
                GameInput.SetCursorFree(true); // fareyle balona tıklanabilsin
                interactAction = GameInput.Controls.Player.Interact;
                jumpAction = GameInput.FindAction("Player/Confirm"); // Enter, Space / A
                interactAction.performed += OnAdvanceInput;
                jumpAction.performed += OnAdvanceInput;
            }
            index = 0;
            ShowCurrent();
        }

        public void Advance()
        {
            if (!IsPlaying) return;
            GameEvents.RaiseSound(SoundId.DialogueAdvance);
            index++;
            if (index >= lines.Count)
            {
                Finish();
                return;
            }
            ShowCurrent();
        }

        /// <summary>Konuşmayı hemen bitirir.</summary>
        public void Finish()
        {
            if (!IsPlaying) return;
            index = -1;
            if (user != null)
            {
                if (GameInput.TryGetControls(out _))
                {
                    interactAction.performed -= OnAdvanceInput;
                    jumpAction.performed -= OnAdvanceInput;
                }
                user.SetControlEnabled(true);
                user = null;
                GameInput.SetCursorFree(GameInput.Mode != InputMode.Player);
            }
            ViewLocator.Unregister<IDialogueCommands>(this);
            view?.Hide();
            InvokeFinished();
        }

        private void Update()
        {
            if (!IsPlaying || user != null) return;
            if (Time.time - lineShownAt >= autoAdvanceSeconds) Advance();
        }

        private void ShowCurrent()
        {
            lineShownAt = Time.time;
            view?.ShowLine(lines[index], index + 1 < lines.Count);
        }

        private void InvokeFinished()
        {
            var callback = onFinished;
            onFinished = null;
            callback?.Invoke();
        }

        private void OnAdvanceInput(InputAction.CallbackContext _)
        {
            if (Time.time - lineShownAt < inputGrace) return;
            Advance();
        }

        private void OnViewsChanged(Type type)
        {
            if (type != typeof(IDialogueView)) return;
            view = ViewLocator.Get<IDialogueView>();
            if (IsPlaying) ShowCurrent();
        }
    }
}
