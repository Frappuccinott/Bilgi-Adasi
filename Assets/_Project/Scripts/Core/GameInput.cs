using System;
using UnityEngine;
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

    /// <summary>
    /// Tek BilgiAdasiControls örneği. Aynı anda yalnızca bir action map açık tutar (CLAUDE.md §3),
    /// imleci moda göre ayarlar ve son kullanılan cihazı izler (ekrandaki E ↔ Ⓧ ipucu için).
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

        public static InputMode Mode { get; private set; } = InputMode.None;
        public static InputDeviceKind LastDevice { get; private set; } = InputDeviceKind.KeyboardMouse;

        public static event Action<InputMode> ModeChanged;
        public static event Action<InputDeviceKind> DeviceChanged;

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
            bool locked = mode == InputMode.Player;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;

            if (Mode == mode) return;
            Mode = mode;
            ModeChanged?.Invoke(mode);
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

            var device = action.activeControl?.device;
            if (device == null) return;

            var kind = device is Gamepad ? InputDeviceKind.Gamepad : InputDeviceKind.KeyboardMouse;
            if (kind == LastDevice) return;
            LastDevice = kind;
            DeviceChanged?.Invoke(kind);
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
            ModeChanged = null;
            DeviceChanged = null;
        }
    }
}
