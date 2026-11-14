using UnityEngine;
using UnityEngine.SceneManagement;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Ana menü (oyunun ilk sahnesi, SceneFlow.MenuScene). Oyna: yeni oyun (kayıt varsa önce onay) → Harita.
    /// Devam et: diskteki son otomatik kayıt (son görev anı) yüklenir → Harita. Görünümü yalnızca IMainMenuView ile tanır.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        private IMainMenuView view;
        private bool loading;

        private void Start()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            GameInput.SetMode(InputMode.UI);
            GameInput.SetCursorFree(true);

            view = ViewLocator.Get<IMainMenuView>();
            if (view == null)
            {
                Debug.LogError("[MainMenu] IMainMenuView yok (Kurulum 20).", this);
                return;
            }
            view.Show(MenuTexts.Load(), SaveSystem.HasSave, OnPlay, OnContinue, OnQuit);
        }

        private void OnPlay()
        {
            if (loading) return;
            if (SaveSystem.HasSave) view.AskNewGame(StartNewGame, null);
            else StartNewGame();
        }

        private void StartNewGame()
        {
            SaveSystem.Instance?.StartNewGame();
            LoadGame();
        }

        private void OnContinue()
        {
            if (loading || !SaveSystem.HasSave) return;
            SaveSystem.Instance?.Load();   // bellekteki değil, diske yazılmış son kayıt
            LoadGame();
        }

        private void LoadGame()
        {
            loading = true;
            view.SetBusy(true);
            if (SceneFlow.Instance != null) SceneFlow.Instance.Load(SceneFlow.GameScene);
            else SceneManager.LoadScene(SceneFlow.GameScene);
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
