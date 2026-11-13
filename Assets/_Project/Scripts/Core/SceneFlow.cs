using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BilgiAdasi.Core
{
    /// <summary>Sahne geçişi (kısa kararma ile). Singleton.</summary>
    public sealed class SceneFlow : MonoBehaviour
    {
        public const string BootScene = "Boot";
        public const string ClassroomScene = "Classroom";
        public const string TownScene = "Town";

        [SerializeField] private float fadeDuration = 0.3f;

        public static SceneFlow Instance { get; private set; }

        public bool IsLoading { get; private set; }

        private IScreenFader fader;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void RegisterFader(IScreenFader screenFader)
        {
            if (Instance != null) Instance.fader = screenFader;
        }

        public static void UnregisterFader(IScreenFader screenFader)
        {
            if (Instance != null && Instance.fader == screenFader) Instance.fader = null;
        }

        public void Load(string sceneName)
        {
            if (IsLoading) return;
            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            if (fader != null) yield return fader.FadeOut(fadeDuration);

            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone) yield return null;

            if (fader != null) yield return fader.FadeIn(fadeDuration);
            IsLoading = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;
    }
}
