using System;
using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Eldekini (seçili slottaki yığın) bakılan düz yüzeye bırakır: yer, masa, tezgâh. Drop: G / D-pad ↓.
    /// Bırakılan yığın PlacedItem olur; E / X ile geri alınır. Konumlar kayıtta (SaveData.PlacedItems) ve
    /// sahne açılınca yeniden kurulur. Oyuncu objesinde durur (PlayerInteractor ve PlayerInventory ile).
    /// Geçerli yerde küçük yeşil bir halka görünür; başka bir hedefe bakılmıyorsa ipucu "[G] Bırak: …".
    /// </summary>
    public sealed class ItemPlacer : MonoBehaviour
    {
        private const string IdPrefix = "yer";

        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private PlayerInventory inventory;
        [Tooltip("SpeciesData.jarPrefab boşsa kullanılan kavanoz (rafınkiyle aynı).")]
        [SerializeField] private JarProp defaultJarPrefab;
        [Tooltip("Bırakılabilen yüzeyler (zemin, masa). Boşsa PlayerInteractor'ın ışın maskesi.")]
        [SerializeField] private LayerMask surfaceMask = 0;
        [Tooltip("Yüzey normalinin yukarıya en az bu kadar bakması gerekir (cos açı). 0.7 ≈ 45°.")]
        [SerializeField] private float minUpDot = 0.7f;
        [Tooltip("Bırakma halkası (düz silindir vb.). Boşsa markerMaterial ile çalışırken kurulur.")]
        [SerializeField] private Transform placeMarker;
        [Tooltip("Kurulan bırakma halkasının malzemesi (09 palet yeşil). Boşsa halka görünmez.")]
        [SerializeField] private Material markerMaterial;
        [Tooltip("Bırakılan eşyalar sahnede bu objenin altına girer (boşsa sahne kökü).")]
        [SerializeField] private Transform itemsRoot;

        private readonly List<PlacedItem> items = new List<PlacedItem>();
        private InputAction dropAction;
        private IInteractPromptView promptView;
        private bool valid;
        private Vector3 point;
        private bool showingPrompt;
        private string shownPrompt;
        private Chemistry.SpeciesData promptSpecies;
        private int promptCount = -1;

        private static SaveData Data => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

        private void Reset()
        {
            interactor = GetComponent<PlayerInteractor>();
            inventory = GetComponentInParent<PlayerInventory>();
        }

        private void OnEnable()
        {
            if (interactor == null) interactor = GetComponent<PlayerInteractor>();
            if (inventory == null) inventory = GetComponentInParent<PlayerInventory>();
            dropAction = GameInput.FindAction("Player/Drop");
            if (dropAction != null) dropAction.performed += OnDrop;
            ViewLocator.Changed += OnViewsChanged;
            promptView = ViewLocator.Get<IInteractPromptView>();
        }

        private void OnDisable()
        {
            if (dropAction != null && GameInput.TryGetControls(out _)) dropAction.performed -= OnDrop;
            ViewLocator.Changed -= OnViewsChanged;
            SetMarker(false);
            HidePrompt();
        }

        private void Start()
        {
            if (placeMarker == null && markerMaterial != null) placeMarker = CreateMarker();
            SetMarker(false);
            LoadSaved();
        }

        private Transform CreateMarker()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "_BirakmaHalkasi";
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = new Vector3(0.16f, 0.002f, 0.16f);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = markerMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        private void Update()
        {
            valid = false;
            bool holding = inventory != null && inventory.SelectedSpecies != null;
            if (holding && interactor != null && interactor.ControlEnabled && !interactor.IsRiding && interactor.RayOrigin != null)
            {
                var origin = interactor.RayOrigin;
                int mask = surfaceMask.value != 0 ? surfaceMask.value : interactor.RayMask.value;
                if (Physics.Raycast(origin.position, origin.forward, out var hit, interactor.Range, mask, QueryTriggerInteraction.Ignore)
                    && hit.normal.y >= minUpDot
                    && hit.collider.GetComponentInParent<WorldInteractable>() == null)
                {
                    valid = true;
                    point = hit.point;
                }
            }

            SetMarker(valid);
            if (valid && placeMarker != null) placeMarker.position = point + Vector3.up * 0.003f;
            UpdatePrompt();
        }

        // ---------- bırakma ----------

        private void OnDrop(InputAction.CallbackContext _)
        {
            if (!valid || inventory == null || inventory.SelectedSpecies == null) return;
            if (interactor == null || !interactor.ControlEnabled) return;
            var stack = inventory.TakeSelected();
            if (stack.IsEmpty) return;

            // Eşyanın önü oyuncuya dönük (etiket okunur).
            var toPlayer = interactor.RayOrigin.position - point;
            toPlayer.y = 0f;
            float yaw = toPlayer.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toPlayer).eulerAngles.y : 0f;
            var entry = new SaveData.PlacedEntry
            {
                id = IdPrefix + DateTime.Now.Ticks.ToString(),
                species = stack.Species.Formula,
                count = stack.Count,
                position = point,
                yaw = yaw
            };
            Spawn(entry, stack.Species);
            Data?.SetPlaced(entry);
            HidePrompt();
        }

        /// <summary>PlacedItem alınınca çağırır: kayıttan siler.</summary>
        public void Forget(PlacedItem item)
        {
            items.Remove(item);
            Data?.RemovePlaced(item.Id);
        }

        private void LoadSaved()
        {
            var data = Data;
            if (data == null || inventory == null || inventory.Economy == null) return;
            var list = data.PlacedItems;
            for (int i = 0; i < list.Count; i++)
            {
                var s = inventory.Economy.FindSpecies(list[i].species);
                if (s != null && list[i].count > 0) Spawn(list[i], s);
            }
        }

        private void Spawn(SaveData.PlacedEntry entry, Chemistry.SpeciesData species)
        {
            var go = new GameObject("Yerdeki_" + species.Formula);
            int layer = LayerMask.NameToLayer("Interactable");
            if (layer >= 0) go.layer = layer;
            if (itemsRoot != null) go.transform.SetParent(itemsRoot, false);
            go.transform.SetPositionAndRotation(entry.position, Quaternion.Euler(0f, entry.yaw, 0f));
            var item = go.AddComponent<PlacedItem>();
            item.Init(this, entry.id, species, entry.count, defaultJarPrefab);
            items.Add(item);
        }

        // ---------- görünüm ----------

        private void SetMarker(bool show)
        {
            if (placeMarker != null && placeMarker.gameObject.activeSelf != show) placeMarker.gameObject.SetActive(show);
        }

        /// <summary>İpucu yalnızca başka bir hedefe bakılmıyorken bizimdir (hedefin ipucu öncelikli).</summary>
        private void UpdatePrompt()
        {
            bool want = valid && interactor != null && !interactor.HasTarget;
            if (!want)
            {
                if (showingPrompt && (interactor == null || !interactor.HasTarget)) HidePrompt();
                showingPrompt = false;
                return;
            }
            var s = inventory.SelectedSpecies;
            int n = inventory.SelectedCount;
            if (shownPrompt == null || s != promptSpecies || n != promptCount)
            {
                promptSpecies = s;
                promptCount = n;
                shownPrompt = string.Format(WorkshopTexts.Get().DropPrompt, s.FormulaRich, n);
                showingPrompt = false;
            }
            if (showingPrompt) return;
            promptView?.ShowPrompt(shownPrompt, ControlGlyphs.For(PromptAction.Drop));
            showingPrompt = true;
        }

        private void HidePrompt()
        {
            if (showingPrompt) promptView?.HidePrompt();
            showingPrompt = false;
        }

        private void OnViewsChanged(Type type)
        {
            if (type != typeof(IInteractPromptView)) return;
            promptView = ViewLocator.Get<IInteractPromptView>();
            showingPrompt = false;
        }
    }
}
