using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>Boot sahnesinde durur; sistemler kurulduktan sonra ilk oyun sahnesine geçer.</summary>
    public sealed class BootLoader : MonoBehaviour
    {
        [Tooltip("Sınıf ara sahnesi hazır olana kadar doğrudan Town.")]
        [SerializeField] private string firstScene = SceneFlow.TownScene;

        private void Start()
        {
            if (SceneFlow.Instance != null) SceneFlow.Instance.Load(firstScene);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(firstScene);
        }
    }
}
