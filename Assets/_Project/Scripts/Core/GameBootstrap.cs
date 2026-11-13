using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// İlk sahne yüklenmeden önce kalıcı sistem objesini (_Systems) kurar.
    /// Hangi sahneden Play'e basılırsa basılsın GameManager, SaveSystem ve SceneFlow hazır olur.
    /// </summary>
    public static class GameBootstrap
    {
        public const string SystemsObjectName = "_Systems";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateSystems()
        {
            if (GameManager.Instance != null) return;

            var go = new GameObject(SystemsObjectName);
            Object.DontDestroyOnLoad(go);
            go.AddComponent<SaveSystem>();   // önce kayıt: GameManager olayları buna yazar
            go.AddComponent<SceneFlow>();
            go.AddComponent<GameManager>();
        }
    }
}
