using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>Dünya-uzayı etiketini kameraya çevirir (yalnız Y ekseni değil, tam billboard).</summary>
    public sealed class FaceCamera : MonoBehaviour
    {
        private static Camera cam;

        private void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            transform.rotation = cam.transform.rotation;
        }
    }
}
