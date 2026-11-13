using System;
using BilgiAdasi.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// İstasyon modu (CLAUDE.md §5): Interact → Player map kapanır, Station map açılır, Cinemachine
    /// istasyon kamerasına geçer (blend süresi CinemachineBrain'de, 0.5 sn), imleç açılır.
    /// ExitStation ile tersine döner; oyuncu yerinden kıpırdamadığı için aynı nokta ve bakışa döner.
    ///
    /// Prefab yapısı (08-mustafa-entegrasyon.md): kök (bu bileşen + collider, layer Interactable)
    /// ├─ Visual (model)  ├─ CameraPoint (CinemachineCamera, kapalı)  └─ Highlight (isteğe bağlı vurgu)
    /// </summary>
    public abstract class StationBase : MonoBehaviour, IInteractable
    {
        [Tooltip("Ekranda tuş simgesinin yanında görünen metin.")]
        [SerializeField] private string promptText = "Kullan";
        [Tooltip("CameraPoint üzerindeki CinemachineCamera. Başta kapalıdır; istasyona girince açılır.")]
        [SerializeField] private CinemachineCamera stationCamera;
        [Tooltip("Bakılınca açılan vurgu objesi (kontur vb.). Boş bırakılabilir.")]
        [SerializeField] private GameObject highlight;

        /// <summary>Şu an içinde olunan istasyon (yoksa null).</summary>
        public static StationBase Active { get; private set; }

        public static event Action<StationBase> Entered;
        public static event Action<StationBase> Exited;

        private PlayerInteractor user;

        public virtual bool CanInteract => Active == null && isActiveAndEnabled;
        public virtual PromptAction TriggerAction => PromptAction.Interact;
        public string PromptText => promptText;
        public string GetPrompt(PlayerInteractor interactor) => promptText;
        public bool IsActive => Active == this;
        protected PlayerInteractor User => user;

        protected virtual void Awake()
        {
            if (stationCamera != null) stationCamera.enabled = false;
            if (highlight != null) highlight.SetActive(false);
        }

        protected virtual void OnDisable()
        {
            if (Active == this) Exit();
        }

        public void SetHighlighted(bool highlighted)
        {
            if (highlight != null) highlight.SetActive(highlighted);
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract) return;
            Enter(interactor);
        }

        private void Enter(PlayerInteractor interactor)
        {
            Active = this;
            user = interactor;
            user.SetControlEnabled(false);
            if (stationCamera != null) stationCamera.enabled = true;

            GameInput.SetMode(InputMode.Station);
            GameInput.Controls.Station.ExitStation.performed += OnExitPerformed;

            OnEnter();
            Entered?.Invoke(this);
        }

        /// <summary>İstasyondan çık. Görev bitince denetleyici de çağırabilir.</summary>
        public void Exit()
        {
            if (Active != this) return;

            if (GameInput.TryGetControls(out var c)) c.Station.ExitStation.performed -= OnExitPerformed;
            OnExit();

            if (stationCamera != null) stationCamera.enabled = false;
            GameInput.SetMode(InputMode.Player);
            if (user != null) user.SetControlEnabled(true);

            user = null;
            Active = null;
            Exited?.Invoke(this);
        }

        private void OnExitPerformed(InputAction.CallbackContext ctx)
        {
            if (CanExit()) Exit();
        }

        /// <summary>Alt sınıf çıkışı engelleyebilir (ör. tepkime animasyonu sürerken).</summary>
        protected virtual bool CanExit() => true;

        protected virtual void OnEnter() { }
        protected virtual void OnExit() { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active = null;
            Entered = null;
            Exited = null;
        }
    }
}
