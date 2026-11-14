using System;
using System.Collections;
using System.Collections.Generic;
using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Bereket Tarlası'nın 24 parseli (görünüm). Her parselin 6 durumu ayrı obje: Tarla_P&lt;satır&gt;&lt;sütun&gt;_&lt;Durum&gt;
    /// (Nadas, Surulmus, Ekili, Filiz, Olgun, Hasat). Aynı anda parsel başına yalnızca bir durum açık.
    /// AnimateTo: parseller sıra sıra aşağıdan büyür. SowTo (ekim animasyonu): ekici tarlada yılan gibi sıra sıra
    /// ilerler, her parsele tohum taneleri yağar, topraktan toz kalkar, parsel ekili hâle büyür.
    /// Mantık bilmez; durum görev ilerlemesinden hesaplanır (ayrıca kaydedilmez).
    /// </summary>
    public sealed class FieldPlots : MonoBehaviour
    {
        [Tooltip("Parsel objelerinin kökü (Bereket_Tarlasi). Boşsa bu obje.")]
        [SerializeField] private Transform root;
        [SerializeField] private float growDuration = 0.8f;
        [Tooltip("Parseller arasındaki gecikme (sn): ekim sıra sıra ilerler.")]
        [SerializeField] private float stagger = 0.06f;

        [Header("Ekim animasyonu")]
        [Tooltip("Ekicinin bir parselden ötekine geçme süresi (sn). 24 parsel ≈ 24 × bu.")]
        [SerializeField] private float sowStep = 0.36f;
        [Tooltip("Tohumun döküldüğü yükseklik (parselin üstünde, m).")]
        [SerializeField] private float sowHeight = 1.1f;
        [SerializeField] private float plotGrow = 0.55f;
        [Tooltip("Tohum taneleri (URP Particles/Unlit, opak). Boşsa efekt yok, yalnızca parseller büyür.")]
        [SerializeField] private Material seedMaterial;
        [Tooltip("Toprak tozu (URP Particles/Unlit, saydam).")]
        [SerializeField] private Material dustMaterial;
        [SerializeField] private Color seedColor = new Color(0.86f, 0.72f, 0.42f);
        [SerializeField] private Color dustColor = new Color(0.47f, 0.34f, 0.22f, 0.55f);

        private readonly Dictionary<string, List<Transform>> byState = new Dictionary<string, List<Transform>>();
        private readonly Dictionary<Transform, Vector3> restScale = new Dictionary<Transform, Vector3>();
        private readonly Dictionary<Transform, Vector3> centers = new Dictionary<Transform, Vector3>();
        private Coroutine routine;
        private bool indexed;
        private ParticleSystem seedFx;
        private ParticleSystem dustFx;

        public string Current { get; private set; } = "Nadas";

        private void Awake()
        {
            Index();
            BuildEffects();
        }

        private void Index()
        {
            if (indexed) return;
            indexed = true;
            if (root == null) root = transform;
            foreach (Transform child in root)
            {
                if (!child.name.StartsWith("Tarla_P")) continue;
                int us = child.name.LastIndexOf('_');
                if (us < 0) continue;
                string state = child.name.Substring(us + 1);
                if (!byState.TryGetValue(state, out var list)) byState[state] = list = new List<Transform>();
                list.Add(child);
                restScale[child] = child.localScale;
                var r = child.GetComponentInChildren<Renderer>(true);
                centers[child] = r != null ? r.bounds.center : child.position;
                if (child.gameObject.activeSelf) Current = state;
                if (r != null)
                {
                    var fb = FieldBounds;
                    if (fb.size == Vector3.zero) fb = r.bounds; else fb.Encapsulate(r.bounds);
                    FieldBounds = fb;
                }
            }
            foreach (var list in byState.Values) list.Sort(SerpentineOrder);
        }

        /// <summary>Ekim sırası: satır satır, her satırda yön değişir (sürücü/ekici yılan gibi gider).</summary>
        private static int SerpentineOrder(Transform a, Transform b)
        {
            Cell(a.name, out int ra, out int ca);
            Cell(b.name, out int rb, out int cb);
            if (ra != rb) return ra.CompareTo(rb);
            return ra % 2 == 0 ? cb.CompareTo(ca) : ca.CompareTo(cb);
        }

        /// <summary>"Tarla_P35_Filiz" → satır 3, sütun 5.</summary>
        private static void Cell(string n, out int row, out int col)
        {
            row = col = 0;
            int p = n.IndexOf("_P", StringComparison.Ordinal);
            if (p < 0 || p + 3 >= n.Length) return;
            row = n[p + 2] - '0';
            col = n[p + 3] - '0';
        }

        /// <summary>Tarlayı hemen o duruma getirir (yükleme).</summary>
        public void SetState(string state)
        {
            Index();
            if (!byState.ContainsKey(state)) return;
            if (routine != null) StopCoroutine(routine);
            routine = null;
            foreach (var pair in byState)
                foreach (var t in pair.Value)
                {
                    t.gameObject.SetActive(pair.Key == state);
                    t.localScale = restScale[t];
                }
            Current = state;
        }

        /// <summary>Parsel parsel yumuşak geçiş (sürme vb.).</summary>
        public void AnimateTo(string state)
        {
            Index();
            if (!byState.ContainsKey(state) || state == Current) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Grow(Current, state));
        }

        /// <summary>Ekim animasyonu: ekici sıra sıra ilerler, tohum yağar, parseller ekili olur. Bitince onDone.</summary>
        public void SowTo(string state, Action onDone)
        {
            Index();
            if (!byState.ContainsKey(state) || state == Current)
            {
                onDone?.Invoke();
                return;
            }
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Sow(Current, state, onDone));
        }

        private IEnumerator Grow(string from, string to)
        {
            byState.TryGetValue(from, out var oldList);
            var newList = byState[to];
            Current = to;
            for (int i = 0; i < newList.Count; i++)
            {
                var t = newList[i];
                t.localScale = Flat(t);
                t.gameObject.SetActive(true);
            }
            float total = growDuration + stagger * newList.Count;
            for (float time = 0f; time < total; time += Time.deltaTime)
            {
                for (int i = 0; i < newList.Count; i++)
                {
                    float k = Mathf.Clamp01((time - i * stagger) / growDuration);
                    newList[i].localScale = Lerp(newList[i], 1f - (1f - k) * (1f - k));
                    if (oldList != null && i < oldList.Count && k >= 1f && oldList[i].gameObject.activeSelf)
                        oldList[i].gameObject.SetActive(false);
                }
                yield return null;
            }
            routine = null;
            SetState(to);
        }

        private IEnumerator Sow(string from, string to, Action onDone, HashSet<int> skip = null)
        {
            byState.TryGetValue(from, out var oldList);
            var newList = byState[to];
            Current = to;
            var oldByCell = new Dictionary<int, Transform>();
            if (oldList != null)
                foreach (var t in oldList) { Cell(t.name, out int r, out int c); oldByCell[r * 10 + c] = t; }

            Transform first = null;
            foreach (var t in newList)
            {
                Cell(t.name, out int r0, out int c0);
                if (skip == null || !skip.Contains(r0 * 10 + c0)) { first = t; break; }
            }
            var seedEmission = seedFx != null ? seedFx.emission : default;
            if (seedFx != null && first != null)
            {
                seedFx.transform.position = centers[first] + Vector3.up * sowHeight;
                seedEmission.enabled = true;
                seedFx.Play();
            }
            if (first != null) GameEvents.RaiseSoundAt(SoundId.Pour, centers[first]);

            for (int i = 0; i < newList.Count; i++)
            {
                var plot = newList[i];
                Cell(plot.name, out int sr, out int sc);
                if (skip != null && skip.Contains(sr * 10 + sc)) continue; // traktör zaten ekti
                Vector3 target = centers[plot] + Vector3.up * sowHeight;
                // Ekici bir önceki parselden bu parsele kayar (tohum yağarak).
                if (seedFx != null)
                {
                    Vector3 start = seedFx.transform.position;
                    for (float t = 0f; t < sowStep; t += Time.deltaTime)
                    {
                        seedFx.transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / sowStep));
                        yield return null;
                    }
                    seedFx.transform.position = target;
                    seedFx.Emit(45); // parselin üstüne bir avuç tohum
                }
                else yield return new WaitForSeconds(sowStep);

                if (dustFx != null)
                {
                    dustFx.transform.position = centers[plot] + Vector3.up * 0.1f;
                    dustFx.Emit(10);
                }
                Cell(plot.name, out int pr, out int pc);
                oldByCell.TryGetValue(pr * 10 + pc, out var old);
                StartCoroutine(SwapPlot(old, plot));
            }

            if (seedFx != null) seedEmission.enabled = false;
            yield return new WaitForSeconds(plotGrow + 0.4f);
            routine = null; // SetState kendi koşan rutinimizi durdurmasın
            SetState(to);
            onDone?.Invoke();
        }

        // ---------- Traktörle ekim (ekici geçtikçe) ----------

        private bool driveSowing;
        private string driveFrom, driveTo;
        private readonly HashSet<int> sownCells = new HashSet<int>();
        private readonly Dictionary<int, Transform> driveOld = new Dictionary<int, Transform>();
        private readonly Dictionary<int, Transform> driveNew = new Dictionary<int, Transform>();
        private float trickle;

        /// <summary>Tarlanın (parsellerin) dünya sınırı ve merkezi; tur sayacı bunu kullanır.</summary>
        public Bounds FieldBounds { get; private set; }

        public int SownCount => sownCells.Count;
        public int CellCount => driveNew.Count;

        /// <summary>Ekici takılı traktörle ekim başlar: ekicinin geçtiği parsel ekili olur (SowAt).</summary>
        public void BeginDriveSow(string to)
        {
            Index();
            if (!byState.ContainsKey(to)) return;
            if (routine != null) { StopCoroutine(routine); routine = null; }
            driveFrom = Current;
            driveTo = to;
            driveOld.Clear();
            driveNew.Clear();
            sownCells.Clear();
            if (byState.TryGetValue(driveFrom, out var oldList))
                foreach (var t in oldList) { Cell(t.name, out int r, out int c); driveOld[r * 10 + c] = t; }
            foreach (var t in byState[to]) { Cell(t.name, out int r, out int c); driveNew[r * 10 + c] = t; }
            driveSowing = true;
        }

        /// <summary>Ekicinin o anki konumu (her kare, giderken): altındaki parsel ilk geçişte ekilir, tohum yağar.</summary>
        public void SowAt(Vector3 seederPosition, float dt)
        {
            if (!driveSowing) return;
            // geçerken ince tohum akışı
            trickle += dt;
            if (seedFx != null && trickle >= 0.08f)
            {
                trickle = 0f;
                seedFx.transform.position = seederPosition + Vector3.up * 0.45f;
                seedFx.Emit(4);
            }
            foreach (var pair in driveNew)
            {
                if (sownCells.Contains(pair.Key)) continue;
                driveOld.TryGetValue(pair.Key, out var old);
                var area = old != null ? AreaOf(old) : AreaOf(pair.Value);
                if (seederPosition.x < area.min.x || seederPosition.x > area.max.x
                    || seederPosition.z < area.min.z || seederPosition.z > area.max.z) continue;
                sownCells.Add(pair.Key);
                if (seedFx != null)
                {
                    seedFx.transform.position = seederPosition + Vector3.up * 0.45f;
                    seedFx.Emit(25);
                }
                if (dustFx != null)
                {
                    dustFx.transform.position = seederPosition;
                    dustFx.Emit(6);
                }
                StartCoroutine(SwapPlot(old, pair.Value));
                break;
            }
        }

        /// <summary>Turlar bitti: ekilmeyen parseller sıra sıra ekilir (ekici animasyonu), sonra onDone.</summary>
        public void FinishDriveSow(Action onDone)
        {
            if (!driveSowing) { onDone?.Invoke(); return; }
            driveSowing = false;
            var skip = new HashSet<int>(sownCells);
            Current = driveFrom;
            routine = StartCoroutine(Sow(driveFrom, driveTo, onDone, skip));
        }

        private readonly Dictionary<Transform, Bounds> areas = new Dictionary<Transform, Bounds>();

        private Bounds AreaOf(Transform t)
        {
            if (areas.TryGetValue(t, out var b)) return b;
            var r = t.GetComponentInChildren<Renderer>(true);
            b = r != null ? r.bounds : new Bounds(t.position, new Vector3(4f, 1f, 4f));
            areas[t] = b;
            return b;
        }

        /// <summary>Eski durum toprağa çöker, yenisi tohumlar düştükten hemen sonra hafif taşarak büyür.</summary>
        private IEnumerator SwapPlot(Transform old, Transform plot)
        {
            plot.localScale = Flat(plot);
            plot.gameObject.SetActive(true);
            for (float t = 0f; t < plotGrow; t += Time.deltaTime)
            {
                float k = t / plotGrow;
                float e = 1f + 0.12f * Mathf.Sin(k * Mathf.PI) * (1f - k); // küçük taşma
                plot.localScale = Lerp(plot, Mathf.Min(1.12f, (1f - (1f - k) * (1f - k)) * e));
                if (old != null) old.localScale = Lerp(old, 1f - k);
                yield return null;
            }
            plot.localScale = restScale[plot];
            if (old != null)
            {
                old.gameObject.SetActive(false);
                old.localScale = restScale[old];
            }
        }

        private Vector3 Flat(Transform t) => new Vector3(restScale[t].x, restScale[t].y * 0.02f, restScale[t].z);

        private Vector3 Lerp(Transform t, float k)
        {
            var rs = restScale[t];
            return new Vector3(rs.x, rs.y * Mathf.Lerp(0.02f, 1f, k), rs.z);
        }

        // ---------- Efektler (bir kez kurulur) ----------

        private void BuildEffects()
        {
            if (seedMaterial != null) seedFx = MakeSeeds();
            if (dustMaterial != null) dustFx = MakeDust();
        }

        private ParticleSystem MakeSeeds()
        {
            var go = new GameObject("Ekim_Tohum_Efekti");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.08f);
            main.startColor = new ParticleSystem.MinMaxGradient(seedColor, seedColor * 0.85f);
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 600;
            var emission = ps.emission;
            emission.rateOverTime = 60f;
            emission.enabled = false;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(3.2f, 0.1f, 3.2f);
            shape.rotation = new Vector3(90f, 0f, 0f); // yayılma aşağı doğru
            var col = ps.collision;
            col.enabled = true;
            col.type = ParticleSystemCollisionType.World;
            col.dampen = 0.8f;
            col.bounce = 0.1f;
            col.lifetimeLoss = 0.4f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = seedMaterial;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        private ParticleSystem MakeDust()
        {
            var go = new GameObject("Ekim_Toz_Efekti");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startColor = dustColor;
            main.gravityModifier = -0.02f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(3.4f, 0.1f, 3.4f);
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            fade.color = g;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = dustMaterial;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }
    }
}
