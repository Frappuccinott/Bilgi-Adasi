using System.Collections.Generic;
using BilgiAdasi.City;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Ana menü (oyunun ilk sahnesi, SceneFlow.MenuScene). Yeni Oyun: yeni oyun (kayıt varsa önce onay) → Harita.
    /// Devam Et: diskteki son otomatik kayıt (son görev anı) yüklenir → Harita. Görünümü yalnızca IMainMenuView ile tanır.
    /// UI/Cancel (Esc · B) açık sayfayı (Ayarlar, Emeği Geçenler, onay) kapatır.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Tooltip("Köy sırası (Harita'daki NeighborhoodRunner ile aynı; Kurulum 20 doldurur). Kayıt özeti ilk bitmemiş köyü gösterir.")]
        [SerializeField] private List<NeighborhoodData> neighborhoods = new List<NeighborhoodData>();

        private IMainMenuView view;
        private InputAction cancel;
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
            bool hasSave = SaveSystem.HasSave;
            view.Show(MenuTexts.Load(), hasSave, hasSave ? BuildSummary() : null, OnNewGame, OnContinue, OnQuit);

            cancel = GameInput.FindAction("UI/Cancel");
            cancel.performed += OnCancel;
        }

        private void OnDestroy()
        {
            if (cancel != null) cancel.performed -= OnCancel;
        }

        private void OnCancel(InputAction.CallbackContext _)
        {
            if (!loading) view?.TryGoBack();
        }

        /// <summary>Kayıttaki ilk bitmemiş köy (hepsi bittiyse sonuncusu) ve sıradan biten görev sayısı.</summary>
        private MainMenuSaveSummary BuildSummary()
        {
            var data = SaveSystem.PeekSaved();
            if (data == null) return null;
            NeighborhoodData last = null;
            int lastDone = 0;
            for (int i = 0; i < neighborhoods.Count; i++)
            {
                var n = neighborhoods[i];
                if (n == null || n.Tasks.Count == 0) continue;
                int done = 0;
                while (done < n.Tasks.Count && n.Tasks[done] != null && data.IsTaskCompleted(n.Tasks[done].Id)) done++;
                last = n;
                lastDone = done;
                if (done < n.Tasks.Count) break;
            }
            return last == null ? null : new MainMenuSaveSummary { Place = last.Title, Done = lastDone, Total = last.Tasks.Count };
        }

        private void OnNewGame()
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
