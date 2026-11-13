using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>Boot sahnesinde durur; sistemler kurulduktan sonra ilk oyun sahnesine geçer.</summary>
    public sealed class BootLoader : MonoBehaviour
    {
        [Tooltip("Yüklenecek ilk oyun sahnesi.")]
        [SerializeField] private string firstScene = SceneFlow.GameScene;

        private void Start()
        {
            if (SceneFlow.Instance != null) SceneFlow.Instance.Load(firstScene);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(firstScene);
        }
    }
}
