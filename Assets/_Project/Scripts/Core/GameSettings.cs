using System;
using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Oyuncu ayarları (Ayarlar ekranı). PlayerPrefs'te kalıcıdır; ayarlanmamış değer null'dır ve oyun
    /// PlayerMovementSettings asset'indeki varsayılanı kullanır (asset'in kendisi değişmez).
    /// Değer değişince <see cref="Changed"/> tetiklenir; FOV ve hassasiyet her karede okunduğu için anında uygulanır.
    /// </summary>
    public static class GameSettings
    {
        private const string KeyMouse = "set.mouseSensitivity";
        private const string KeyPad = "set.gamepadLookSpeed";
        private const string KeyFov = "set.fieldOfView";
        private const string KeyBob = "set.headBob";
        private const string KeyInvert = "set.invertY";
        private const string KeyVolume = "set.masterVolume";

        public static float? MouseSensitivity { get; private set; }
        /// <summary>Gamepad yatay bakış hızı (°/sn). Dikey hız aynı oranla ölçeklenir.</summary>
        public static float? GamepadLookSpeed { get; private set; }
        public static float? FieldOfView { get; private set; }
        public static bool? HeadBob { get; private set; }
        public static bool InvertY { get; private set; }
        /// <summary>0–1.</summary>
        public static float MasterVolume { get; private set; } = 1f;

        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            Changed = null;
            MouseSensitivity = PlayerPrefs.HasKey(KeyMouse) ? PlayerPrefs.GetFloat(KeyMouse) : (float?)null;
            GamepadLookSpeed = PlayerPrefs.HasKey(KeyPad) ? PlayerPrefs.GetFloat(KeyPad) : (float?)null;
            FieldOfView = PlayerPrefs.HasKey(KeyFov) ? PlayerPrefs.GetFloat(KeyFov) : (float?)null;
            HeadBob = PlayerPrefs.HasKey(KeyBob) ? PlayerPrefs.GetInt(KeyBob) != 0 : (bool?)null;
            InvertY = PlayerPrefs.GetInt(KeyInvert, 0) != 0;
            MasterVolume = PlayerPrefs.GetFloat(KeyVolume, 1f);
            AudioListener.volume = MasterVolume;
        }

        public static void SetMouseSensitivity(float v) { MouseSensitivity = Mathf.Clamp(v, 0.01f, 0.5f); PlayerPrefs.SetFloat(KeyMouse, MouseSensitivity.Value); Save(); }
        public static void SetGamepadLookSpeed(float v) { GamepadLookSpeed = Mathf.Clamp(v, 30f, 400f); PlayerPrefs.SetFloat(KeyPad, GamepadLookSpeed.Value); Save(); }
        public static void SetFieldOfView(float v) { FieldOfView = Mathf.Clamp(v, 60f, 90f); PlayerPrefs.SetFloat(KeyFov, FieldOfView.Value); Save(); }
        public static void SetHeadBob(bool v) { HeadBob = v; PlayerPrefs.SetInt(KeyBob, v ? 1 : 0); Save(); }
        public static void SetInvertY(bool v) { InvertY = v; PlayerPrefs.SetInt(KeyInvert, v ? 1 : 0); Save(); }

        public static void SetMasterVolume(float v)
        {
            MasterVolume = Mathf.Clamp01(v);
            AudioListener.volume = MasterVolume;
            PlayerPrefs.SetFloat(KeyVolume, MasterVolume);
            Save();
        }

        /// <summary>Her şeyi asset varsayılanlarına döndürür.</summary>
        public static void ResetAll()
        {
            foreach (var k in new[] { KeyMouse, KeyPad, KeyFov, KeyBob, KeyInvert, KeyVolume }) PlayerPrefs.DeleteKey(k);
            MouseSensitivity = GamepadLookSpeed = FieldOfView = null;
            HeadBob = null;
            InvertY = false;
            MasterVolume = 1f;
            AudioListener.volume = 1f;
            Save();
        }

        private static void Save()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
