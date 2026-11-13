using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Player;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Eldeki eşyanın (seçili slot) birinci şahıs görünümü: kameranın sağ altında, hafif gecikmeyle takip eden
    /// alet ya da kavanoz modeli. Slot değişince model değişir; istasyonda, konuşmada ve bisiklette gizlenir.
    /// Kamera kökünün (PlayerInteractor.rayOrigin) altında bir objede durur. Modeller türe göre önbellekte.
    /// </summary>
    public sealed class HeldItemDisplay : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerInteractor interactor;
        [Tooltip("Takip edilen kamera kökü (boşsa bu objenin ebeveyni).")]
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private JarProp defaultJarPrefab;
        [Tooltip("Kol rig'indeki sağ el tutma noktası (Tutma_R). Boşsa oyuncu rig'inde adıyla aranır; o da yoksa kamera ofseti kullanılır.")]
        [SerializeField] private Transform gripPoint;
        [Tooltip("Modelin tutma noktasına göre dönüşü. Varsayılan: Kol_Tut_Dongu pozunda model dik ve hafif içe dönük.")]
        [SerializeField] private Vector3 gripEuler = new Vector3(335.4f, 162.1f, 22f);
        [Tooltip("Eşyanın kenarının avuca ne kadar gireceği (m). Geniş eşya bu kadar içeriden tutulur.")]
        [SerializeField] private float gripInset = 0.035f;
        [Tooltip("Kol rig'inin Animator'ı: elde eşya varken tut pozu.")]
        [SerializeField] private Animator armsAnimator;
        [SerializeField] private string holdState = "Kol_Tut_Dongu";
        [SerializeField] private string idleState = "Kol_Bekle_Dongu";
        [Tooltip("Kameraya göre konum (sağ, aşağı, ileri).")]
        [SerializeField] private Vector3 localOffset = new Vector3(0.26f, -0.27f, 0.5f);
        [SerializeField] private Vector3 localEuler = new Vector3(4f, -18f, 0f);
        [Tooltip("Modelin en uzun kenarı bu boya ölçeklenir (m).")]
        [SerializeField] private float holdSize = 0.2f;
        [Tooltip("Dönüşte gecikme hissi (yüksek = sıkı takip).")]
        [SerializeField] private float followSharpness = 22f;
        [Tooltip("Slot değişince modelin aşağıdan gelme süresi (sn).")]
        [SerializeField] private float swapTime = 0.18f;

        private readonly Dictionary<SpeciesData, GameObject> cache = new Dictionary<SpeciesData, GameObject>();
        private Transform holder;
        private GameObject shown;
        private SpeciesData shownSpecies;
        private float swapT = 1f;

        private void Awake()
        {
            if (cameraRoot == null) cameraRoot = transform.parent;
            if (gripPoint == null)
                foreach (var t in transform.root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Tutma_R") { gripPoint = t; break; }
            if (armsAnimator == null && gripPoint != null) armsAnimator = gripPoint.GetComponentInParent<Animator>();
            // Tutucu sahne kökünde: kamerayı gecikmeyle izler (kameraya bağlı olsa sarsıntısız ama cansız durur).
            holder = new GameObject("ElTutucu").transform;
            holder.SetParent(null, false);
        }

        private void OnEnable()
        {
            if (inventory != null) inventory.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.Changed -= Refresh;
            if (holder != null) holder.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (holder != null) Destroy(holder.gameObject);
        }

        private void Refresh()
        {
            var s = inventory != null ? inventory.SelectedSpecies : null;
            if (s == shownSpecies) return;
            if (shown != null) shown.SetActive(false);
            shownSpecies = s;
            shown = s != null ? GetModel(s) : null;
            if (shown != null) shown.SetActive(true);
            swapT = 0f;
        }

        private GameObject GetModel(SpeciesData s)
        {
            if (cache.TryGetValue(s, out var go) && go != null) return go;
            go = ItemModels.Spawn(s, defaultJarPrefab, holder, out var jar);
            if (go == null) return null;
            var cols = go.GetComponentsInChildren<Collider>();
            for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;
            var rends = go.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < rends.Length; i++) rends[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (jar != null && jar.Label != null) jar.Label.gameObject.SetActive(false); // elde etiket kalabalık eder
            var anim = go.GetComponentInChildren<Animator>();
            if (anim != null) anim.enabled = false;

            // Boyu holdSize'a getir, pivotu sınırın ortasına al (kapalı renderer'ın sınırı sıfırdır: geçici aç).
            bool wasActive = holder.gameObject.activeSelf;
            holder.gameObject.SetActive(true);
            go.SetActive(true);
            holder.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.transform.localScale = Vector3.one;
            go.transform.localPosition = Vector3.zero;
            var b = ItemModels.RendererBounds(go);
            float max = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            float k = max > 0.0001f ? holdSize / max : 1f;
            go.transform.localScale = Vector3.one * k;
            go.transform.localPosition = -b.center * k;
            // Avuçta: geniş eşya (havan, elek) merkezinden değil kenarından tutulsun; el içine girmesin.
            if (gripPoint != null)
                go.transform.localPosition += Vector3.left * Mathf.Max(0f, b.extents.x * k - gripInset);
            holder.gameObject.SetActive(wasActive);
            if (wasActive && cameraRoot != null) Snap();
            cache[s] = go;
            return go;
        }

        private void LateUpdate()
        {
            bool visible = shown != null && cameraRoot != null
                && (interactor == null || (interactor.ControlEnabled && !interactor.IsRiding));
            if (holder.gameObject.activeSelf != visible)
            {
                holder.gameObject.SetActive(visible);
                if (visible) Snap();
                if (armsAnimator != null && armsAnimator.isActiveAndEnabled)
                    armsAnimator.CrossFadeInFixedTime(visible ? holdState : idleState, 0.2f);
            }
            if (!visible) return;

            if (gripPoint != null)
            {
                // Eşya avuçta: kol animasyonuyla birlikte hareket eder.
                holder.SetPositionAndRotation(gripPoint.position, gripPoint.rotation * Quaternion.Euler(gripEuler));
                return;
            }

            swapT = Mathf.Min(1f, swapT + Time.deltaTime / Mathf.Max(0.01f, swapTime));
            float drop = (1f - Mathf.SmoothStep(0f, 1f, swapT)) * 0.18f;
            var targetPos = cameraRoot.TransformPoint(localOffset + Vector3.down * drop);
            var targetRot = cameraRoot.rotation * Quaternion.Euler(localEuler);
            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            holder.position = targetPos; // konum sıkı (içine girmesin), dönüş gecikmeli
            holder.rotation = Quaternion.Slerp(holder.rotation, targetRot, t);
        }

        private void Snap()
        {
            if (gripPoint != null)
            {
                holder.SetPositionAndRotation(gripPoint.position, gripPoint.rotation * Quaternion.Euler(gripEuler));
                return;
            }
            holder.SetPositionAndRotation(cameraRoot.TransformPoint(localOffset), cameraRoot.rotation * Quaternion.Euler(localEuler));
        }
    }
}
