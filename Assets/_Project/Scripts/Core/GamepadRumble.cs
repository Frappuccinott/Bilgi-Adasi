using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Gamepad titreşimi. Yalnızca son kullanılan cihaz gamepad ise çalışır; duraklatınca, odak kaybında ve
    /// Play bitince motorlar durur. Oyun başında kendiliğinden kurulur (sahneye eklemek gerekmez) ve
    /// GameEvents.StoveFired'ı dinler: tepkime = güçlü vuruş + sönen uğultu, köpürme = iki kısa sarsıntı.
    /// </summary>
    public sealed class GamepadRumble : MonoBehaviour
    {
        // Tepkime: ateş 1 sn boyunca harlanır (titreşim %15'ten tepeye yükselir), tepede kısa durur, yavaşça söner (~1.9 sn).
        private const float ReactLow = 0.9f, ReactHigh = 0.5f, ReactRise = 1.0f, ReactStart = 0.15f, ReactHold = 0.2f, ReactFade = 0.7f;
        // Köpürme: kısa, sert, iki kez (hata hissi).
        private const float FoamLow = 0.35f, FoamHigh = 0.55f, FoamHold = 0.08f, FoamFade = 0.1f, FoamGap = 0.08f;

        private static GamepadRumble instance;
        private Gamepad pad;
        private Coroutine routine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (instance != null) return;
            var go = new GameObject("[GamepadRumble]") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GamepadRumble>();
        }

        /// <summary>count kez: hold sn tam güç, fade sn sönme; vuruşlar arası gap sn.</summary>
        public static void Pulse(float low, float high, float hold, float fade, int count = 1, float gap = 0f) =>
            Swell(low, high, 0f, 1f, hold, fade, count, gap);

        /// <summary>Yükselen titreşim: rise sn boyunca gücün startFraction katından tama çıkar, sonra hold + fade.</summary>
        public static void Swell(float low, float high, float rise, float startFraction, float hold, float fade, int count = 1, float gap = 0f)
        {
            if (instance == null || GameInput.LastDevice != InputDeviceKind.Gamepad) return;
            var target = Gamepad.current;
            if (target == null) return;
            instance.Play(target, low, high, rise, startFraction, hold, fade, count, gap);
        }

        public static void StopAll()
        {
            if (instance != null) instance.Stop();
        }

        private void OnEnable()
        {
            GameEvents.StoveFired += OnStoveFired;
            GameEvents.PauseChanged += OnPauseChanged;
        }

        private void OnDisable()
        {
            GameEvents.StoveFired -= OnStoveFired;
            GameEvents.PauseChanged -= OnPauseChanged;
            Stop();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) Stop();
        }

        private void OnStoveFired(bool reacting)
        {
            if (reacting) Swell(ReactLow, ReactHigh, ReactRise, ReactStart, ReactHold, ReactFade);
            else Pulse(FoamLow, FoamHigh, FoamHold, FoamFade, 2, FoamGap);
        }

        private void OnPauseChanged(bool paused)
        {
            if (paused) Stop();
        }

        private void Play(Gamepad target, float low, float high, float rise, float startFraction, float hold, float fade, int count, float gap)
        {
            Stop();
            pad = target;
            routine = StartCoroutine(Run(low, high, rise, startFraction, hold, fade, count, gap));
        }

        private IEnumerator Run(float low, float high, float rise, float startFraction, float hold, float fade, int count, float gap)
        {
            for (int c = 0; c < count; c++)
            {
                for (float t = 0f; t < rise; t += Time.unscaledDeltaTime)
                {
                    float p = t / rise;
                    float k = Mathf.Lerp(startFraction, 1f, p * p); // yavaş başlar, sona doğru belirginleşir
                    pad.SetMotorSpeeds(low * k, high * k);
                    yield return null;
                }
                pad.SetMotorSpeeds(low, high);
                for (float t = 0f; t < hold; t += Time.unscaledDeltaTime) yield return null;
                for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
                {
                    float k = 1f - t / fade;
                    pad.SetMotorSpeeds(low * k, high * k);
                    yield return null;
                }
                pad.SetMotorSpeeds(0f, 0f);
                if (c < count - 1)
                    for (float t = 0f; t < gap; t += Time.unscaledDeltaTime) yield return null;
            }
            routine = null;
            pad = null;
        }

        private void Stop()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            if (pad != null) pad.ResetHaptics();
            pad = null;
        }
    }
}
