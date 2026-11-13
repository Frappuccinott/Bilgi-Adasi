using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BilgiAdasi.Core
{
    public enum InputMode
    {
        None,
        Player,
        Station,
        UI
    }

    public enum InputDeviceKind
    {
        KeyboardMouse,
        Gamepad
    }

    /// <summary>Ekranda hangi tuş simgelerinin çizileceği. Tanınmayan gamepad'ler Xbox düzeninde gösterilir.</summary>
    public enum ControlScheme
    {
        KeyboardMouse,
        Xbox,
        PlayStation,
        Switch
    }

    /// <summary>
    /// Tek BilgiAdasiControls örneği. Aynı anda yalnızca bir action map açık tutar (CLAUDE.md §3),
    /// imleci moda göre ayarlar ve son kullanılan cihazı / tuş düzenini izler (ekrandaki [E] ↔ (X) ↔ □ ipucu için).
    /// </summary>
    public static class GameInput
    {
        private static BilgiAdasiControls controls;

        public static BilgiAdasiControls Controls
        {
            get
            {
                if (controls == null) Initialize();
                return controls;
            }
        }

        private static bool isQuitting;

        /// <summary>
        /// OnDisable/OnDestroy'da abonelik kaldırırken kullanın: kapanış sırasında yeni örnek yaratmaz.
        /// (Controls getter'ı çıkışta çağrılırsa dispose edilmemiş yeni bir örnek oluşurdu.)
        /// </summary>
        public static bool TryGetControls(out BilgiAdasiControls result)
        {
            result = controls;
            return result != null;
        }

        /// <summary>
        /// Aksiyonu adıyla bulur ("Player/OpenHint"). Yeni eklenen aksiyonlar için: üretilen C# sınıfı
        /// yeniden üretilmeden de derlenir. Bir kez çağırıp sonucu saklayın (her karede değil).
        /// </summary>
        public static InputAction FindAction(string mapAndAction) => Controls.asset.FindAction(mapAndAction, true);

        public static InputMode Mode { get; private set; } = InputMode.None;
        public static InputDeviceKind LastDevice { get; private set; } = InputDeviceKind.KeyboardMouse;
        /// <summary>Son kullanılan cihazın tuş düzeni (klavye / Xbox / PlayStation / Switch).</summary>
        public static ControlScheme LastScheme { get; private set; } = ControlScheme.KeyboardMouse;

        public static event Action<InputMode> ModeChanged;
        public static event Action<InputDeviceKind> DeviceChanged;
        /// <summary>Tuş düzeni değişti (ör. Xbox → PS). Tuş ipuçları bunu dinler.</summary>
        public static event Action<ControlScheme> SchemeChanged;

        private static int inventoryLocks;

        /// <summary>Envanterde gezinme (tekerlek, RB/LB, 1–6) kapalı mı? Ör. Ustanın Sorgusu açıkken.</summary>
        public static bool InventoryLocked => inventoryLocks > 0;

        /// <summary>Envanter gezinmesini kilitler/açar. Her kilit kendi açışıyla eşlenmeli (sayaç).</summary>
        public static void LockInventory(bool locked) =>
            inventoryLocks = Mathf.Max(0, inventoryLocks + (locked ? 1 : -1));

        public static void SetMode(InputMode mode)
        {
            if (isQuitting) return;
            var c = Controls;
            c.Player.Disable();
            c.Station.Disable();
            c.UI.Disable();

            switch (mode)
            {
                case InputMode.Player: c.Player.Enable(); break;
                case InputMode.Station: c.Station.Enable(); break;
                case InputMode.UI: c.UI.Enable(); break;
            }

            // Oyuncu modunda imleç kilitli ve gizli; istasyon/UI'da serbest.
            SetCursorFree(mode != InputMode.Player);

            // UI'ın kendi yön gezinmesi (çubukla düğmeden düğmeye atlama) yalnızca UI modunda açık.
            // İstasyonda gamepad seçimi denetleyicinin SelectPrevious/Next aksiyonlarıyla yapılır.
            var eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                eventSystem.sendNavigationEvents = mode == InputMode.UI;
                if (mode != InputMode.UI) eventSystem.SetSelectedGameObject(null);
            }

            if (Mode == mode) return;
            Mode = mode;
            ModeChanged?.Invoke(mode);
        }

        /// <summary>
        /// İmleci serbest bırakır (görünür) ya da kilitler. Mod değişmeden kısa süreli serbestlik için
        /// (ör. kapıdaki konuşma: hareket durur, fareyle tıklanır). Bitince SetCursorFree(Mode != Player).
        /// </summary>
        public static void SetCursorFree(bool free)
        {
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }

        private static void Initialize()
        {
            controls = new BilgiAdasiControls();
            InputSystem.onActionChange += OnActionChange;
            Application.quitting += OnQuitting;
        }

        private static void OnQuitting()
        {
            isQuitting = true;
            Shutdown();
        }

        private static void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed) return;
            if (obj is not InputAction action) return;

            var control = action.activeControl;
            var device = control?.device;
            if (device == null) return;

            // Gamepad kullanırken masadaki farenin titremesi ipuçlarını klavyeye çevirmesin.
            if (device is Mouse && control is UnityEngine.InputSystem.Controls.Vector2Control v2
                && v2.ReadValue().sqrMagnitude < MouseNoiseThresholdSqr) return;

            // Klavye/fareyle oynarken bağlı gamepad'in çubuk/tetik kayması ipuçlarını gamepad'e çevirmesin.
            // (Look/Move hem fareye hem çubuğa bağlı: fare durunca aksiyon kayan çubuğa geçip "performed" verir.)
            // Gamepad yalnızca bilinçli girdiyle seçilir: tuş basımı ya da yarıdan fazla itilmiş çubuk/tetik.
            if (device is Gamepad && !IsDeliberateGamepadInput(control)) return;

            var scheme = SchemeOf(device);
            if (scheme == LastScheme) return;

            var kind = scheme == ControlScheme.KeyboardMouse ? InputDeviceKind.KeyboardMouse : InputDeviceKind.Gamepad;
            LastScheme = scheme;
            SchemeChanged?.Invoke(scheme);
            if (kind != LastDevice)
            {
                LastDevice = kind;
                DeviceChanged?.Invoke(kind);
            }
        }

        private const float MouseNoiseThresholdSqr = 9f; // 3 piksel
        private const float GamepadAnalogThreshold = 0.5f;

        private static bool IsDeliberateGamepadInput(InputControl control)
        {
            switch (control)
            {
                case UnityEngine.InputSystem.Controls.ButtonControl button:
                    // Tetikler de ButtonControl'dür; analog değerleri yarıyı geçmeli.
                    return button.ReadValue() >= GamepadAnalogThreshold;
                case UnityEngine.InputSystem.Controls.Vector2Control stick:
                    return stick.ReadValue().sqrMagnitude >= GamepadAnalogThreshold * GamepadAnalogThreshold;
                case UnityEngine.InputSystem.Controls.AxisControl axis:
                    return Mathf.Abs(axis.ReadValue()) >= GamepadAnalogThreshold;
                default:
                    return true;
            }
        }

        private static ControlScheme SchemeOf(InputDevice device)
        {
            if (device is not Gamepad) return ControlScheme.KeyboardMouse;
            if (device is UnityEngine.InputSystem.DualShock.DualShockGamepad) return ControlScheme.PlayStation; // DualSense dahil
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX || UNITY_WSA
            if (device is UnityEngine.InputSystem.Switch.SwitchProControllerHID) return ControlScheme.Switch;
#endif
            return ControlScheme.Xbox; // XInput ve tanınmayan gamepad'ler
        }

        private static void Shutdown()
        {
            InputSystem.onActionChange -= OnActionChange;
            Application.quitting -= OnQuitting;
            controls?.Dispose();
            controls = null;
        }

        // Domain reload kapalıyken önceki Play oturumunun durumunu temizler.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Shutdown();
            isQuitting = false;
            Mode = InputMode.None;
            LastDevice = InputDeviceKind.KeyboardMouse;
            LastScheme = ControlScheme.KeyboardMouse;
            ModeChanged = null;
            DeviceChanged = null;
            SchemeChanged = null;
        }
    }
}
