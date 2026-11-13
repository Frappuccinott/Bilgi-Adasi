using UnityEngine;
using UnityEngine.InputSystem;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Duraklatma (Esc) menüsü mantığı. Player/Pause (Esc · Start) ve Station/Pause (P · Start) ile açılır;
    /// UI/Cancel (Esc · B) alt sekmeden geri döner, ana sayfadaysa menüyü kapatır.
    /// Açıkken zaman durur (timeScale 0), ses duraklar, girdi UI moduna geçer; kapanınca önceki mod ve imleç
    /// durumu geri gelir (kapıdaki konuşmada imleç serbestti). Görünüm: IPauseMenuView.
    /// </summary>
    public sealed class PauseMenuController : MonoBehaviour, IPauseMenuCommands
    {
        [Tooltip("Başarım ağacını menüye verir. Boşsa sahnede aranır (Awake'te bir kez).")]
        [SerializeField] private AchievementController achievements;

        private InputAction playerPause, stationPause, cancel;
        private bool paused;
        private InputMode previousMode;
        private float previousTimeScale = 1f;
        private CursorLockMode previousLock;
        private bool previousCursorVisible;

        public bool IsPaused => paused;

        private void Awake()
        {
            if (achievements == null) achievements = FindAnyObjectByType<AchievementController>();
        }

        private void OnEnable()
        {
            playerPause = GameInput.FindAction("Player/Pause");
            stationPause = GameInput.FindAction("Station/Pause");
            cancel = GameInput.FindAction("UI/Cancel");
            playerPause.performed += OnPausePressed;
            stationPause.performed += OnPausePressed;
            cancel.performed += OnCancel;
        }

        private void OnDisable()
        {
            if (GameInput.TryGetControls(out _))
            {
                playerPause.performed -= OnPausePressed;
                stationPause.performed -= OnPausePressed;
                cancel.performed -= OnCancel;
            }
            if (paused) Resume();
        }

        private void OnPausePressed(InputAction.CallbackContext _)
        {
            if (!paused) Pause();
        }

        private void OnCancel(InputAction.CallbackContext _)
        {
            if (!paused) return; // pazar da UI/Cancel dinler; menü kapalıyken karışmaz
            var view = ViewLocator.Get<IPauseMenuView>();
            if (view != null && view.TryGoBack()) return;
            Resume();
        }

        public void Pause()
        {
            var view = ViewLocator.Get<IPauseMenuView>();
            if (paused || view == null) return;
            paused = true;

            previousMode = GameInput.Mode;
            previousTimeScale = Time.timeScale;
            previousLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;

            Time.timeScale = 0f;
            AudioListener.pause = true;
            GameInput.SetMode(InputMode.UI);
            ViewLocator.Register<IPauseMenuCommands>(this);

            view.Open(achievements != null ? achievements.BuildMenuInfo() : new AchievementMenuInfo());
            GameEvents.RaisePauseChanged(true);
        }

        public void Resume()
        {
            if (!paused) return;
            paused = false;

            ViewLocator.Get<IPauseMenuView>()?.Close();
            ViewLocator.Unregister<IPauseMenuCommands>(this);

            Time.timeScale = previousTimeScale;
            AudioListener.pause = false;
            GameInput.SetMode(previousMode == InputMode.None ? InputMode.Player : previousMode);
            Cursor.lockState = previousLock;
            Cursor.visible = previousCursorVisible;
            GameEvents.RaisePauseChanged(false);
        }

        public void QuitGame()
        {
            SaveSystem.Instance?.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnApplicationQuit()
        {
            // Editörde Play'den çıkarken timeScale 0 kalmasın.
            if (paused) Time.timeScale = previousTimeScale;
        }
    }
}
