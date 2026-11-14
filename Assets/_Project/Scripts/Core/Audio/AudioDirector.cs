using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Oyunun tek ses noktası. GameEvents.SoundRequested isteklerini havuzdaki AudioSource'larla çalar,
    /// müzik ve ortam döngüsünü yönetir, girdi aksiyonlarına (menü gezinme, defter, harita) arayüz sesi verir.
    /// Sahnede bir kez bulunur (Harita sahnesi, "Ses" objesi). Denetleyiciler bu sınıfı bilmez.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class AudioDirector : MonoBehaviour
    {
        [SerializeField] private SoundLibrary library;
        [Tooltip("Aynı anda çalabilecek tek seferlik ses sayısı.")]
        [SerializeField] private int voices = 16;
        [Tooltip("Gamepad çubuğuyla menü gezinmesinde bu eşik aşılınca tık sesi çalar.")]
        [SerializeField] private float navigateThreshold = 0.5f;

        private readonly Dictionary<SoundId, SoundLibrary.Entry> map = new Dictionary<SoundId, SoundLibrary.Entry>();
        private readonly Dictionary<SoundId, float> lastPlayed = new Dictionary<SoundId, float>();
        private readonly Dictionary<SoundId, int> lastClip = new Dictionary<SoundId, int>();
        private readonly List<RaycastResult> raycastBuffer = new List<RaycastResult>();
        private AudioSource[] pool;
        private int next;
        private AudioSource musicSource;
        private AudioSource ambienceSource;
        private PointerEventData pointerData;
        private EventSystem pointerOwner;
        private bool navigateHeld;
        private float neighborhoodTime = -10f;

        private void Awake()
        {
            if (library != null)
                foreach (var e in library.Entries)
                    if (e != null) map[e.Id] = e;

            pool = new AudioSource[Mathf.Max(1, voices)];
            for (int i = 0; i < pool.Length; i++)
            {
                var go = new GameObject("Ses_" + i);
                go.transform.SetParent(transform, false);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.rolloffMode = AudioRolloffMode.Logarithmic;
                s.dopplerLevel = 0f;
                if (library != null)
                {
                    s.minDistance = library.SpatialMinDistance;
                    s.maxDistance = library.SpatialMaxDistance;
                }
                pool[i] = s;
            }

            musicSource = CreateLoop("Muzik", library != null ? library.Music : null, library != null ? library.MusicVolume : 0f);
            ambienceSource = CreateLoop("Ortam", library != null ? library.Ambience : null, library != null ? library.AmbienceVolume : 0f);
        }

        private AudioSource CreateLoop(string name, AudioClip clip, float volume)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = true;
            s.spatialBlend = 0f;
            s.clip = clip;
            s.volume = volume;
            s.priority = 0;
            if (clip != null) s.Play();
            return s;
        }

        private void OnEnable()
        {
            GameEvents.SoundRequested += OnSound;
            GameEvents.PauseChanged += OnPause;
            GameEvents.NeighborhoodCompleted += OnNeighborhoodCompleted;
            BindInput(true);
        }

        private void OnDisable()
        {
            GameEvents.SoundRequested -= OnSound;
            GameEvents.PauseChanged -= OnPause;
            GameEvents.NeighborhoodCompleted -= OnNeighborhoodCompleted;
            BindInput(false);
        }

        // ---------- Girdiye bağlı arayüz sesleri (UI kodu değişmeden) ----------

        private void BindInput(bool on)
        {
            if (!GameInput.TryGetControls(out var c) && !on) return;
            if (on) c = GameInput.Controls;

            Toggle(c.UI.Navigate, OnNavigate, on);
            Toggle(c.UI.Submit, OnSubmit, on);
            Toggle(c.UI.Cancel, OnCancel, on);
            Toggle(c.UI.PrevTab, OnTab, on);
            Toggle(c.UI.NextTab, OnTab, on);
            Toggle(c.Player.OpenMap, OnOpenMap, on);
            Toggle(c.Player.OpenBook, OnOpenBook, on);
            Toggle(c.Player.OpenHint, OnOpenBook, on);
            Toggle(c.Station.SelectPrevious, OnStationMove, on);
            Toggle(c.Station.SelectNext, OnStationMove, on);
            Toggle(c.Station.Increase, OnStationStep, on);
            Toggle(c.Station.Decrease, OnStationStep, on);
            Toggle(c.Station.OpenHint, OnOpenBook, on);
        }

        private static void Toggle(InputAction action, System.Action<InputAction.CallbackContext> handler, bool on)
        {
            if (on) action.performed += handler;
            else action.performed -= handler;
        }

        private void OnNavigate(InputAction.CallbackContext ctx)
        {
            bool held = ctx.ReadValue<Vector2>().sqrMagnitude >= navigateThreshold * navigateThreshold;
            if (held && !navigateHeld) Play(SoundId.UiNavigate, Vector3.zero, false);
            navigateHeld = held;
        }

        private void OnSubmit(InputAction.CallbackContext _) => Play(SoundId.UiConfirm, Vector3.zero, false);
        private void OnCancel(InputAction.CallbackContext _) => Play(SoundId.UiBack, Vector3.zero, false);
        private void OnTab(InputAction.CallbackContext _) => Play(SoundId.PageTurn, Vector3.zero, false);
        private void OnOpenMap(InputAction.CallbackContext _) => Play(SoundId.MapOpen, Vector3.zero, false);
        private void OnOpenBook(InputAction.CallbackContext _) => Play(SoundId.BookOpen, Vector3.zero, false);
        private void OnStationMove(InputAction.CallbackContext _) => Play(SoundId.UiNavigate, Vector3.zero, false);
        private void OnStationStep(InputAction.CallbackContext _) => Play(SoundId.UiSlot, Vector3.zero, false);

        /// <summary>Fareyle bir düğmeye (Selectable) tıklanınca onay sesi. Yalnızca imleç açıkken.</summary>
        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (Cursor.lockState == CursorLockMode.Locked) return;
            var es = EventSystem.current;
            if (es == null) return;

            if (pointerData == null || pointerOwner != es) { pointerData = new PointerEventData(es); pointerOwner = es; }
            pointerData.position = mouse.position.ReadValue();
            raycastBuffer.Clear();
            es.RaycastAll(pointerData, raycastBuffer);
            for (int i = 0; i < raycastBuffer.Count; i++)
            {
                var sel = raycastBuffer[i].gameObject.GetComponentInParent<Selectable>();
                if (sel == null) continue;
                if (sel.IsInteractable()) Play(SoundId.UiConfirm, Vector3.zero, false);
                return;
            }
        }

        // ---------- Olaylar ----------

        private void OnSound(SoundId id, Vector3 position, bool spatial)
        {
            // Köy müjdesi çaldıysa aynı anda gelen başarım müjdesi üst üste binmesin.
            if (id == SoundId.Achievement && Time.unscaledTime - neighborhoodTime < 1.5f) return;
            Play(id, position, spatial);
        }

        private void OnNeighborhoodCompleted(string _)
        {
            neighborhoodTime = Time.unscaledTime;
            Play(SoundId.NeighborhoodComplete, Vector3.zero, false);
        }

        private void OnPause(bool paused)
        {
            Play(SoundId.Pause, Vector3.zero, false);
            if (library == null) return;
            if (musicSource != null) musicSource.volume = library.MusicVolume * (paused ? library.PausedMusicMultiplier : 1f);
            if (ambienceSource != null) ambienceSource.volume = library.AmbienceVolume * (paused ? library.PausedMusicMultiplier : 1f);
        }

        // ---------- Çalma ----------

        private void Play(SoundId id, Vector3 position, bool spatial)
        {
            if (id == SoundId.None || !map.TryGetValue(id, out var e)) return;
            var clips = e.Clips;
            if (clips == null || clips.Count == 0) return;

            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(id, out float last) && now - last < e.MinInterval) return;
            lastPlayed[id] = now;

            int index = Random.Range(0, clips.Count);
            if (clips.Count > 1 && lastClip.TryGetValue(id, out int prev) && prev == index) index = (index + 1) % clips.Count;
            lastClip[id] = index;
            var clip = clips[index];
            if (clip == null) return;

            var s = NextVoice();
            s.transform.position = position;
            s.spatialBlend = spatial ? 1f : 0f;
            s.pitch = Random.Range(e.Pitch.x, e.Pitch.y);
            s.volume = e.Volume;
            s.clip = clip;
            s.Play();
        }

        /// <summary>Boş ses kaynağı; hepsi doluysa en eskisi kesilir.</summary>
        private AudioSource NextVoice()
        {
            for (int i = 0; i < pool.Length; i++)
            {
                int k = (next + i) % pool.Length;
                if (pool[k].isPlaying) continue;
                next = (k + 1) % pool.Length;
                return pool[k];
            }
            var s = pool[next];
            next = (next + 1) % pool.Length;
            return s;
        }
    }
}
