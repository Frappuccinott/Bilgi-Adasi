using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Sahne bağlantı noktası: kilitli mahalle/ilçe bariyeri. Collider (trigger değil) oyuncuyu durdurur.
    /// unlockedBy doluysa o mahalle bitince bariyer kapanır; boşsa prototipte hep kilitli kalır.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class LockedZone : MonoBehaviour
    {
        [Tooltip("Bu mahalle bitince bariyer kalkar. Boş = hep kilitli (prototipteki kilitli ilçeler).")]
        [SerializeField] private string unlockedBy = "";

        private void OnEnable() => GameEvents.NeighborhoodCompleted += OnNeighborhoodCompleted;
        private void OnDisable() => GameEvents.NeighborhoodCompleted -= OnNeighborhoodCompleted;

        private void Start()
        {
            if (string.IsNullOrEmpty(unlockedBy)) return;
            var data = SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;
            if (data != null && data.IsNeighborhoodCompleted(unlockedBy)) gameObject.SetActive(false);
        }

        private void OnNeighborhoodCompleted(string id)
        {
            if (!string.IsNullOrEmpty(unlockedBy) && id == unlockedBy) gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!TryGetComponent<BoxCollider>(out var box)) return;
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.25f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
#endif
    }
}
