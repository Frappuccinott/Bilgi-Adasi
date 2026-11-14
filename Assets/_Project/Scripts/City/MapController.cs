using BilgiAdasi.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Harita ekranı ve mini haritanın mantığı. Player/OpenMap (M · Tab · Select) haritayı açar; açıkken aynı tuş ya da
    /// UI/Cancel (Esc · B) kapatır. Köy durumu NeighborhoodRunner'dan (biten görev, kilit, sıradaki köy) okunur.
    /// Mini harita yalnızca oyuncu modunda görünür; her kare oyuncunun yerini ve bakışını view'a verir.
    /// Görünüm: IMapView, IMinimapView (Canvas ya da obje referansı tutulmaz).
    /// </summary>
    public sealed class MapController : MonoBehaviour
    {
        [SerializeField] private MapData data;
        [Tooltip("Boşsa sahnede aranır (Awake'te bir kez).")]
        [SerializeField] private NeighborhoodRunner runner;
        [Tooltip("Oyuncunun yeri ve bakışı. Boşsa ana kamera (bisiklette de doğru).")]
        [SerializeField] private Transform tracked;

        private InputAction openMap, cancel;
        private InputMode previousMode = InputMode.Player;
        private IMinimapView minimap;
        private bool minimapReady;
        private readonly MapScreenInfo info = new MapScreenInfo();

        private bool IsOpen => ViewLocator.Get<IMapView>()?.IsOpen ?? false;

        private void Awake()
        {
            if (runner == null) runner = FindAnyObjectByType<NeighborhoodRunner>();
        }

        private void OnEnable()
        {
            openMap = GameInput.FindAction("Player/OpenMap");
            cancel = GameInput.FindAction("UI/Cancel");
            openMap.performed += OnOpenMap;
            cancel.performed += OnCancel;
            GameInput.ModeChanged += OnModeChanged;
            GameEvents.TaskCompleted += OnProgress;
            GameEvents.NeighborhoodCompleted += OnProgress;
        }

        private void OnDisable()
        {
            if (GameInput.TryGetControls(out _))
            {
                openMap.performed -= OnOpenMap;
                cancel.performed -= OnCancel;
            }
            GameInput.ModeChanged -= OnModeChanged;
            GameEvents.TaskCompleted -= OnProgress;
            GameEvents.NeighborhoodCompleted -= OnProgress;
        }

        private void Start() => SetupMinimap();

        private void OnProgress(string _) => minimapReady = false;

        private void OnModeChanged(InputMode mode) => minimap?.SetVisible(mode == InputMode.Player);

        private void OnOpenMap(InputAction.CallbackContext _)
        {
            if (IsOpen) CloseMap();
            // Açılış sekansında (kartlar, uyanış, ustanın sorusu) ve birebir konuşma / soru açıkken harita açılmaz
            // (gamepad'de D-pad ↓ soruda seçim taşır).
            else if (GameInput.Mode == InputMode.Player && !ApprenticeshipDirector.IsPlaying && !DialogueFocus.IsOpen) OpenMap();
        }

        private void OnCancel(InputAction.CallbackContext _)
        {
            if (IsOpen) CloseMap();
        }

        private void OpenMap()
        {
            var view = ViewLocator.Get<IMapView>();
            if (view == null || data == null) return;
            Rebuild();
            previousMode = GameInput.Mode;
            GameInput.SetMode(InputMode.UI);
            openMap.Enable(); // aynı tuş kapatsın (Player map'inin geri kalanı kapalı)
            view.Open(info, CloseMap);
            PushPlayer(view, null);
        }

        private void CloseMap()
        {
            var view = ViewLocator.Get<IMapView>();
            if (view == null || !view.IsOpen) return;
            view.Close();
            GameInput.SetMode(previousMode == InputMode.None ? InputMode.Player : previousMode);
        }

        private void SetupMinimap()
        {
            minimap = ViewLocator.Get<IMinimapView>();
            if (minimap == null || data == null) return;
            Rebuild();
            minimap.Setup(info);
            minimap.SetVisible(GameInput.Mode == InputMode.Player);
            minimapReady = true;
        }

        private void LateUpdate()
        {
            if (data == null) return;
            if (!minimapReady && ViewLocator.Get<IMinimapView>() != null) SetupMinimap();
            var view = IsOpen ? ViewLocator.Get<IMapView>() : null;
            if (minimap != null || view != null) PushPlayer(view, minimap);
        }

        private void PushPlayer(IMapView view, IMinimapView mini)
        {
            var t = tracked;
            if (t == null)
            {
                var cam = Camera.main;
                if (cam == null) return;
                t = cam.transform;
            }
            var fwd = t.forward;
            fwd.y = 0f;
            float yaw = fwd.sqrMagnitude > 1e-4f ? Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg : 0f;
            var uv = data.WorldToUv(t.position);
            view?.SetPlayer(uv, yaw);
            mini?.SetPlayer(uv, yaw);
        }

        // ---------- Veri → görünüm bilgisi ----------

        private void Rebuild()
        {
            info.Image = data.Image;
            info.Title = data.Title;
            info.Subtitle = data.Subtitle;
            info.LegendTitle = data.LegendTitle;
            info.CitiesTitle = data.CitiesTitle;
            info.CurrentWord = data.CurrentWord;
            info.YouAreHere = data.YouAreHere;
            info.CloseWord = data.CloseWord;
            info.MapWord = data.MapWord;
            info.NorthLetter = data.NorthLetter;
            info.MinimapUvSpan = data.MinimapSpan / Mathf.Max(1f, data.WorldSize);
            info.Points.Clear();

            var focused = runner != null ? runner.FocusedNeighborhood() : null;
            var points = data.Points;
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                var pi = new MapPointInfo
                {
                    Label = p.label,
                    Kind = p.kind,
                    Uv = data.WorldToUv(p.world),
                    Color = p.color,
                    OnMinimap = p.onMinimap,
                    State = MapPointState.Open
                };
                if (p.kind == MapPointKind.Neighborhood && p.neighborhood != null) FillNeighborhood(pi, p.neighborhood, focused);
                else if (p.kind == MapPointKind.LockedCity)
                {
                    pi.State = MapPointState.Soon;
                    pi.Status = data.StatusSoon;
                }
                info.Points.Add(pi);
            }
        }

        private void FillNeighborhood(MapPointInfo pi, NeighborhoodData n, NeighborhoodData focused)
        {
            pi.Total = n.Tasks.Count;
            pi.Done = runner != null ? runner.CompletedCount(n) : 0;
            if (pi.Total > 0 && pi.Done >= pi.Total)
            {
                pi.State = MapPointState.Done;
                pi.Status = data.StatusDone;
            }
            else if (n.LockedByDefault)
            {
                pi.State = MapPointState.Locked;
                pi.Status = data.StatusLocked;
            }
            else if (n.Requires != null && runner != null && runner.CompletedCount(n.Requires) < n.Requires.Tasks.Count)
            {
                pi.State = MapPointState.Locked;
                pi.Status = string.Format(data.StatusRequires, n.Requires.Title);
            }
            else
            {
                pi.State = n == focused ? MapPointState.Current : MapPointState.Open;
                pi.Status = string.Format(data.StatusOpen, pi.Done, pi.Total);
            }
        }
    }
}
