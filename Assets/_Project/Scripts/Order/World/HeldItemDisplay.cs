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
    /// Kol rig'i varsa eşya avuçtadır: tut pozunun bileği içe çevrilir (şişe tutar gibi, avuç ekranın ortasına),
    /// eşya parmakların sardığı boşluğa oturur ve kavrama genişliğine göre küçültülür.
    /// Sıra 10010: Cinemachine kamerayı (ve ona bağlı kolları) taşıdıktan sonra çalışır, eşya elden kaymaz.
    /// </summary>
    [DefaultExecutionOrder(10010)]
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
        [Tooltip("Eldeyken bileğe eklenen dönüş, kamera uzayında (x geri/ileri bük, y içe çevir, z yuvarla). " +
                 "Kol_Tut_Dongu üstten kavrar; bu dönüş onu yandan kavramaya çevirir (eşya elin önünde değil içinde görünür).")]
        [SerializeField] private Vector3 wristEuler = new Vector3(-25f, -60f, -80f);
        [Tooltip("Bilek dönüşünün geçiş süresi (sn); kol animasyonunun geçişiyle aynı.")]
        [SerializeField] private float wristBlendTime = 0.2f;
        [Tooltip("Eşyanın avuçtaki genişliği bu boya ölçeklenir (m). Parmaklar gövdeyi sarar.")]
        [SerializeField] private float gripWidth = 0.085f;
        [Tooltip("Avuçtaki eşyanın en uzun kenarı en fazla (m).")]
        [SerializeField] private float handMaxSize = 0.17f;
        [Tooltip("Eşyanın tutma noktası yoksa avucun geleceği yükseklik (alttan, boyun oranı).")]
        [Range(0f, 1f)] [SerializeField] private float defaultGripHeight = 0.3f;
        [Tooltip("Yatık uzun eşya (kepçe, havaneli) en uzun kenarı boyunun bu katından büyükse dik tutulur.")]
        [SerializeField] private float uprightLongRatio = 1.6f;
        [Tooltip("Açık: eşya avuç noktasında durur ama yönü kameraya göre dik kalır (kemik dönüşü eşyayı yatırmasın).")]
        [SerializeField] private bool uprightInHand = true;
        [Tooltip("uprightInHand açıkken kameraya göre eğim (x öne, y içe dönüş, z yana).")]
        [SerializeField] private Vector3 handEuler = new Vector3(-6f, -22f, 6f);
        [Tooltip("Avuçtaki son ince ayar (tutucu uzayında, m). Play'de canlı ayarlanabilir.")]
        [SerializeField] private Vector3 gripOffset = Vector3.zero;
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
        private float wristBlend;
        private Transform handBone;
        private Transform thumbTip;
        private Transform[] fingerJoints;

        private void Awake()
        {
            if (cameraRoot == null) cameraRoot = transform.parent;
            if (gripPoint == null)
                foreach (var t in transform.root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Tutma_R") { gripPoint = t; break; }
            if (armsAnimator == null && gripPoint != null) armsAnimator = gripPoint.GetComponentInParent<Animator>();
            CacheHandBones();
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
            go.transform.localRotation = Quaternion.identity;
            var b = ItemModels.RendererBounds(go);
            if (gripPoint != null)
            {
                // Yatık uzun eşya (kepçe, havaneli, elek) avuçta dik durur: en uzun yatay kenar yukarı döner.
                if (Mathf.Max(b.size.x, b.size.z) > b.size.y * uprightLongRatio)
                {
                    go.transform.localRotation = b.size.x >= b.size.z
                        ? Quaternion.Euler(0f, 0f, 90f)
                        : Quaternion.Euler(90f, 0f, 0f);
                    b = ItemModels.RendererBounds(go);
                }
                // Avuç eşyanın kavrama yüksekliğinde gövdeyi sarar: sanat eşyasının tutma noktası
                // (esya_tasinir.py: <ad>_Tutma_Noktasi), yoksa alttan defaultGripHeight.
                var itemGrip = FindItemGrip(go.transform);
                float gripY = itemGrip != null
                    ? holder.InverseTransformPoint(itemGrip.position).y
                    : b.min.y + b.size.y * defaultGripHeight;
                float width = Mathf.Max(b.size.x, b.size.z);
                float longest = Mathf.Max(width, b.size.y);
                float k = Mathf.Min(width > 0.0001f ? gripWidth / width : 1f,
                                    longest > 0.0001f ? handMaxSize / longest : 1f);
                go.transform.localScale = Vector3.one * k;
                go.transform.localPosition = new Vector3(-b.center.x * k, -gripY * k, -b.center.z * k) + gripOffset;
            }
            else
            {
                float max = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                float k = max > 0.0001f ? holdSize / max : 1f;
                go.transform.localScale = Vector3.one * k;
                go.transform.localPosition = -b.center * k;
            }
            holder.gameObject.SetActive(wasActive);
            if (wasActive && cameraRoot != null) Snap();
            cache[s] = go;
            return go;
        }

        private Quaternion GripRotation() =>
            uprightInHand && cameraRoot != null
                ? cameraRoot.rotation * Quaternion.Euler(handEuler)
                : gripPoint.rotation * Quaternion.Euler(gripEuler);

        private static Transform FindItemGrip(Transform root)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name.EndsWith("_Tutma_Noktasi")) return all[i];
            return null;
        }

        /// <summary>El kemiği (Tutma_R'nin ebeveyni El_R) ve avuç boşluğunu çizen parmak eklemleri.</summary>
        private void CacheHandBones()
        {
            if (gripPoint == null) return;
            handBone = gripPoint.parent;
            if (handBone == null) return;
            var list = new List<Transform>();
            foreach (var t in handBone.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Parmak_")) continue;
                if (t.name.StartsWith("Parmak_Bas_")) { if (t.name.StartsWith("Parmak_Bas_3")) thumbTip = t; }
                else list.Add(t);
            }
            fingerJoints = list.ToArray();
        }

        /// <summary>Bakış çerçevesi: kolların bağlı olduğu kamera (aynı karede taşınır), yoksa kamera kökü.</summary>
        private Transform ViewFrame() => armsAnimator != null ? armsAnimator.transform : cameraRoot;

        /// <summary>Tut pozunun bileğini kamera uzayında çevirir (animasyon her kare yeniden yazar; üstüne eklenir).</summary>
        private void ApplyWrist()
        {
            if (handBone == null) return;
            wristBlend = Mathf.MoveTowards(wristBlend, 1f, Time.deltaTime / Mathf.Max(0.01f, wristBlendTime));
            var view = ViewFrame();
            var add = Quaternion.AngleAxis(wristEuler.x, view.right)
                    * Quaternion.AngleAxis(wristEuler.y, view.up)
                    * Quaternion.AngleAxis(wristEuler.z, view.forward);
            handBone.rotation = Quaternion.Slerp(Quaternion.identity, add, wristBlend) * handBone.rotation;
        }

        /// <summary>Parmakların sardığı boşluğun ortası: eklemlerin ortalaması, başparmak ucuna doğru çekilmiş.</summary>
        private Vector3 PalmCenter()
        {
            if (fingerJoints == null || fingerJoints.Length == 0 || thumbTip == null) return gripPoint.position;
            var sum = Vector3.zero;
            for (int i = 0; i < fingerJoints.Length; i++) sum += fingerJoints[i].position;
            return sum / fingerJoints.Length * 0.75f + thumbTip.position * 0.25f;
        }

        private void PlaceInHand()
        {
            ApplyWrist();
            holder.SetPositionAndRotation(PalmCenter(), GripRotation());
        }

        private void LateUpdate()
        {
            bool visible = shown != null && cameraRoot != null
                && (interactor == null || (interactor.ControlEnabled && !interactor.IsRiding));
            if (holder.gameObject.activeSelf != visible)
            {
                holder.gameObject.SetActive(visible);
                if (!visible) wristBlend = 0f;
                if (visible) Snap();
                if (armsAnimator != null && armsAnimator.isActiveAndEnabled)
                    armsAnimator.CrossFadeInFixedTime(visible ? holdState : idleState, 0.2f);
            }
            if (!visible) return;

            if (gripPoint != null)
            {
                // Eşya avuçta: kol animasyonuyla birlikte hareket eder.
                PlaceInHand();
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
                holder.SetPositionAndRotation(PalmCenter(), GripRotation());
                return;
            }
            holder.SetPositionAndRotation(cameraRoot.TransformPoint(localOffset), cameraRoot.rotation * Quaternion.Euler(localEuler));
        }
    }
}
