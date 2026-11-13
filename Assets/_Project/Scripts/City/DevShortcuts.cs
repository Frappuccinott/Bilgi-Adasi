using BilgiAdasi.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Geliştirici kısayolları (04 §7; demo ve test için). Yalnızca editörde, Development Build'de ya da
    /// BILGIADASI_DEMO tanımlıyken çalışır. "Dev" action map'i oyun modundan bağımsız hep açıktır.
    ///   DevJumpTask : klavye F1–F5 → odaktaki köyün o görevine atla · gamepad LT+RT+→ → sıradaki görevi bitmiş say
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
            jumpTask.performed += OnJumpTask;
            resetSave.performed += OnResetSave;
            map.Enable();
        }

        private void OnDisable()
        {
            if (map == null) return;
            jumpTask.performed -= OnJumpTask;
            resetSave.performed -= OnResetSave;
            if (GameInput.TryGetControls(out _)) map.Disable();
            map = null;
        }

        private void OnJumpTask(InputAction.CallbackContext ctx)
        {
            if (ctx.control is KeyControl key)
            {
                int index = key.keyCode - Key.F1; // F1 → 0 (1–6 envanter slotlarına ayrıldı)
                if (index >= 0 && index <= 4) runner.DevJumpTo(index);
                return;
            }
            runner.DevSkipCurrent();
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
