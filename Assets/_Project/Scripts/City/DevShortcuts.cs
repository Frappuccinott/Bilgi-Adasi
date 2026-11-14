using System.Collections;
using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.Desk;
using BilgiAdasi.Order;
using BilgiAdasi.Player;
using BilgiAdasi.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Geliştirici kısayolları (04 §7; demo ve test için). Yalnızca editörde, Development Build'de ya da
    /// BILGIADASI_DEMO tanımlıyken çalışır. "Dev" action map'i oyun modundan bağımsız hep açıktır.
    ///   DevJumpTask : klavye F1–F4, F6 → odaktaki köyün o görevine atla · gamepad LT+RT+→ → sıradaki görevi bitmiş say
    ///   DevCheatMenu: klavye F5 · gamepad LT+RT+R3 → hile menüsü (sunum): masa mektupları ya da kapanış mektubu
    ///   DevResetSave: klavye Ctrl+R · gamepad LT+RT+← → kaydı sil, sahneyi yeniden yükle
    /// </summary>
    [RequireComponent(typeof(NeighborhoodRunner))]
    public sealed class DevShortcuts : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || BILGIADASI_DEMO
        private const string MapName = "Dev";

        private NeighborhoodRunner runner;
        private InputActionMap map;
        private InputAction jumpTask;
        private InputAction resetSave;
        private InputAction cheatMenu;

        [Header("Hile menüsü (F5 · LT+RT+R3)")]
        [SerializeField] private string menuSpeaker = "Hile menüsü";
        [SerializeField] private string menuQuestion = "Hangi mektubu göstereyim?";
        [SerializeField] private string optionDesk = "Masa mektupları (Tepkime Türleri, 1. mektup)";
        [SerializeField] private string optionEnding = "Kapanış mektubu ve Emeği Geçenler";
        [SerializeField] private string optionCancel = "Vazgeç";

        private void Awake()
        {
            runner = GetComponent<NeighborhoodRunner>();
        }

        private void OnEnable()
        {
            map = GameInput.Controls.asset.FindActionMap(MapName);
            if (map == null)
            {
                // Input asset'i güncellendi ama C# sınıfı henüz yeniden üretilmediyse.
                Debug.LogWarning("[Dev] 'Dev' action map'i yok. BilgiAdasiControls.inputactions → Generate C# Class.", this);
                return;
            }
            jumpTask = map.FindAction("DevJumpTask", true);
            resetSave = map.FindAction("DevResetSave", true);
            cheatMenu = map.FindAction("DevCheatMenu", false);
            jumpTask.performed += OnJumpTask;
            resetSave.performed += OnResetSave;
            if (cheatMenu != null) cheatMenu.performed += OnCheatMenu;
            map.Enable();
        }

        private void OnDisable()
        {
            if (map == null) return;
            jumpTask.performed -= OnJumpTask;
            resetSave.performed -= OnResetSave;
            if (cheatMenu != null) cheatMenu.performed -= OnCheatMenu;
            if (GameInput.TryGetControls(out _)) map.Disable();
            map = null;
        }

        private void OnJumpTask(InputAction.CallbackContext ctx)
        {
            if (ctx.control is KeyControl key)
            {
                int index = key.keyCode - Key.F1; // F1 → 0 (1–6 envanter slotlarına ayrıldı)
                if (index >= 0 && index <= 5) runner.DevJumpTo(index); // F6: Denkleştirme 6 görev (Sv2 tarla, 14.11)
                return;
            }
            runner.DevSkipCurrent();
        }

        // ---------- Hile menüsü (sunumda mektupları göstermek için) ----------

        private bool menuOpen;
        private PlayerInteractor player;
        private readonly List<string> menuOptions = new List<string>(3);

        private void OnCheatMenu(InputAction.CallbackContext _)
        {
            if (menuOpen || GameInput.Mode != InputMode.Player || DialogueFocus.IsOpen || LearningDirector.IsBusy
                || PredictionDirector.IsBusy || StationBase.Active != null) return;
            var view = ViewLocator.Get<IReasoningView>();
            if (view == null) return;
            if (player == null) player = FindAnyObjectByType<PlayerInteractor>();
            menuOpen = true;
            if (player != null) player.SetControlEnabled(false);
            GameInput.SetCursorFree(true);
            menuOptions.Clear();
            menuOptions.Add(optionDesk);
            menuOptions.Add(optionEnding);
            menuOptions.Add(optionCancel);
            view.ShowQuestion(menuSpeaker, menuQuestion, menuOptions, OnCheatChosen, false);
        }

        private void OnCheatChosen(int choice)
        {
            ViewLocator.Get<IReasoningView>()?.Hide();
            menuOpen = false;
            if (player != null) player.SetControlEnabled(true);
            GameInput.SetCursorFree(GameInput.Mode != InputMode.Player);
            if (choice == 0) StartCoroutine(OpenDeskLetter());
            else if (choice == 1) FindAnyObjectByType<EndingDirector>()?.PlayTrial();
        }

        /// <summary>Tepkime Türleri köyünün ilk mektubuna atlar; güvercin gelince oyuncuyu masaya oturtur.</summary>
        private IEnumerator OpenDeskLetter()
        {
            if (runner.DevJumpToFirst<DeskTask>() == null) yield break;
            var desk = FindAnyObjectByType<DeskController>();
            if (desk == null) yield break;
            float waited = 0f;
            while (!desk.HasLetter && waited < 5f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            var station = desk.GetComponent<DeskStation>();
            if (station != null && player != null && station.CanInteract) station.Interact(player);
        }

        private static void OnResetSave(InputAction.CallbackContext _)
        {
            if (SaveSystem.Instance == null || SceneFlow.Instance == null || SceneFlow.Instance.IsLoading) return;
            SaveSystem.Instance.ResetSave();
            Debug.Log("[Dev] Kayıt silindi, sahne yeniden yükleniyor.");
            SceneFlow.Instance.Load(SceneManager.GetActiveScene().name);
        }
#else
        private void Awake() => enabled = false;
#endif
    }
}
