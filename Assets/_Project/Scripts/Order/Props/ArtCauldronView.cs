using System.Collections;
using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Atölyenin sanat kazanı (Kazan_Rig) ve ocağı (Ocak_Ates_Rig) için ICauldronView. CauldronVisuals'ın (yer tutucu
    /// sıvı/köpük/ateş) yerine geçer; aynı sahnede yalnızca biri açık olmalı. 08 §8 animasyon sözleşmesi: yalnızca
    /// Animator.CrossFade. Sıvı (Kazan_Iksir) ve kabarcıklar içeriğin ortalama rengine boyanır (_Renk), kazan boşken gizlenir.
    /// Kaynama döngüsü ocak yanarken sürer; döküm karıştırır, köpürme ve başarı tek seferlik oynar.
    /// </summary>
    public sealed class ArtCauldronView : RegisteredView<ICauldronView>, ICauldronView
    {
        [Header("Sanat")]
        [SerializeField] private Animator cauldron;
        [SerializeField] private Animator fire;
        [Tooltip("İçerik rengine boyanan ve kazan boşken gizlenen sıvı parçaları (Kazan_Iksir, kabarcıklar).")]
        [SerializeField] private Renderer[] liquidRenderers = new Renderer[0];

        [Header("Kazan klipleri")]
        [SerializeField] private string idleClip = "Kazan_Bosta";
        [SerializeField] private string stirClip = "Kazan_Karistir_Dongu";
        [SerializeField] private string boilClip = "Kazan_Kaynama_Dongu";
        [SerializeField] private string foamClip = "Kazan_Kopurme";
        [SerializeField] private string successClip = "Kazan_Basari";
        [SerializeField] private float stirSeconds = 1.2f;
        [SerializeField] private float foamClipSeconds = 2.17f;
        [SerializeField] private float successClipSeconds = 1.9f;

        [Header("Ocak klipleri")]
        [SerializeField] private string fireOffClip = "Ates_Bosta";
        [SerializeField] private string fireIgniteClip = "Ates_Yan";
        [SerializeField] private string fireLoopClip = "Ates_Titre_Dongu";
        [SerializeField] private string fireEndClip = "Ates_Son";
        [SerializeField] private float igniteSeconds = 0.63f;
        [SerializeField] private float fireEndSeconds = 0.5f;
        [SerializeField] private float crossFade = 0.12f;

        [Header("Buhar (kodla, karar 14.11)")]
        [Tooltip("Modeldeki buhar bulutu (kopyalanır; kendisi gizlenir). Boşsa kazan altında 'Kazan_Buhar_1' aranır.")]
        [SerializeField] private MeshRenderer steamTemplate;
        [SerializeField] private int steamPuffs = 14;
        [Tooltip("Tepkimede buharın çıktığı süre (sn); puflar bu sürede art arda ağızdan çıkar.")]
        [SerializeField] private float steamEmitSeconds = 1.6f;
        [SerializeField] private float steamLife = 2.2f;
        [SerializeField] private float steamRise = 1.1f;
        [SerializeField] private Vector2 steamScale = new Vector2(0.4f, 1.7f);
        [SerializeField, Range(0f, 1f)] private float steamOpacity = 0.85f;
        [Tooltip("Puf ışıması (köşe rengi × güç): krem duvarın önünde açık buhar olarak okunsun.")]
        [SerializeField] private float steamGlow = 0.6f;

        private static readonly int ColorId = Shader.PropertyToID("_Renk");
        private static readonly int OpacityId = Shader.PropertyToID("_Opaklik");
        private static readonly int GlowId = Shader.PropertyToID("_Isima");

        private MaterialPropertyBlock block;
        private bool stoveLit;
        private bool hasContents;
        private Coroutine cauldronRoutine;
        private Coroutine fireRoutine;

        private struct Puff
        {
            public Transform T;
            public MeshRenderer R;
            public float Age, Life, Spin;
            public Vector3 Start, Drift;
            public bool Alive;
        }

        private Puff[] puffs;
        private Transform steamOrigin;
        private Vector3 puffBaseScale;
        private float emitUntil, nextEmit;
        private MaterialPropertyBlock steamBlock;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            SetLiquidVisible(false);
            Play(cauldron, idleClip);
            Play(fire, fireOffClip);
            BuildSteam();
        }

        // ---------- Buhar ----------

        private void BuildSteam()
        {
            if (steamTemplate == null && cauldron != null)
                foreach (var r in cauldron.GetComponentsInChildren<MeshRenderer>(true))
                    if (r.name == "Kazan_Buhar_1") { steamTemplate = r; break; }
            if (steamTemplate == null || steamPuffs <= 0) return;
            var filter = steamTemplate.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return;

            // Modelin buhar kütleleri kapanır (animasyon ölçeklese de çizilmez); yerine yumuşak puflar.
            foreach (var r in cauldron.GetComponentsInChildren<MeshRenderer>(true))
                if (r.name.StartsWith("Kazan_Buhar_")) r.enabled = false;

            // Çıkış noktası: ortadaki buhar kemiği (kazan ağzı); dünya ölçeği şablonun dinlenme ölçeğinden.
            steamOrigin = steamTemplate.transform.parent != null ? steamTemplate.transform.parent : steamTemplate.transform;
            var middle = cauldron.transform.Find("Kok/Buhar_2");
            if (middle != null) steamOrigin = middle;
            // Buhar kemiği dinlenmede ölçek 0: şablonun dünya ölçeği kemiksiz hesaplanır (Kok × kendi yerel ölçeği).
            var kok = cauldron.transform.Find("Kok");
            puffBaseScale = Vector3.Scale(kok != null ? kok.lossyScale : cauldron.transform.lossyScale, steamTemplate.transform.localScale);
            if (puffBaseScale.sqrMagnitude < 1e-6f) puffBaseScale = Vector3.one;

            var root = new GameObject("Buhar_Puflar").transform;
            root.SetParent(transform, false);
            steamBlock = new MaterialPropertyBlock();
            puffs = new Puff[steamPuffs];
            for (int i = 0; i < steamPuffs; i++)
            {
                var go = new GameObject("Puf_" + i, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(root, false);
                go.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var r = go.GetComponent<MeshRenderer>();
                r.sharedMaterials = steamTemplate.sharedMaterials;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
                go.SetActive(false);
                puffs[i] = new Puff { T = go.transform, R = r };
            }
        }

        private void StartSteam(float seconds)
        {
            if (puffs == null) return;
            emitUntil = Time.time + seconds;
            nextEmit = Time.time;
        }

        private void Update()
        {
            if (puffs == null) return;
            float now = Time.time;
            while (now < emitUntil && now >= nextEmit)
            {
                EmitPuff();
                nextEmit += steamEmitSeconds / Mathf.Max(1, steamPuffs - 2);
            }

            float dt = Time.deltaTime;
            for (int i = 0; i < puffs.Length; i++)
            {
                if (!puffs[i].Alive) continue;
                ref var p = ref puffs[i];
                p.Age += dt;
                float k = p.Age / p.Life;
                if (k >= 1f)
                {
                    p.Alive = false;
                    p.T.gameObject.SetActive(false);
                    continue;
                }
                // Yükselirken yavaşlar (sıcak hava), hafifçe savrulur ve döner; büyür.
                float rise = 1f - (1f - k) * (1f - k);
                p.T.position = p.Start + Vector3.up * (steamRise * rise) + p.Drift * k;
                p.T.rotation = Quaternion.Euler(0f, p.Spin * p.Age, p.Spin * 0.35f * p.Age);
                p.T.localScale = puffBaseScale * Mathf.Lerp(steamScale.x, steamScale.y, rise);
                // Hızlı belirir, yükseldikçe söner: ağızda yoğun, tepede dağılmış.
                float alpha = Mathf.Clamp01(k / 0.1f) * Mathf.Pow(1f - k, 1.1f) * steamOpacity;
                p.R.GetPropertyBlock(steamBlock);
                steamBlock.SetFloat(OpacityId, alpha);
                steamBlock.SetFloat(GlowId, steamGlow);
                p.R.SetPropertyBlock(steamBlock);
            }
        }

        private void EmitPuff()
        {
            for (int i = 0; i < puffs.Length; i++)
            {
                if (puffs[i].Alive) continue;
                ref var p = ref puffs[i];
                var disc = Random.insideUnitCircle * 0.1f;
                p.Start = steamOrigin.position + new Vector3(disc.x, 0f, disc.y);
                var drift = Random.insideUnitCircle * 0.18f;
                p.Drift = new Vector3(drift.x, 0f, drift.y);
                p.Life = steamLife * Random.Range(0.8f, 1.15f);
                p.Spin = Random.Range(-40f, 40f);
                p.Age = 0f;
                p.Alive = true;
                p.T.position = p.Start;
                p.T.localScale = puffBaseScale * steamScale.x;
                p.T.gameObject.SetActive(true);
                return;
            }
        }

        public void ShowContents(IReadOnlyList<SpeciesAmount> contents)
        {
            int total = 0;
            Color mix = Color.clear;
            for (int i = 0; i < contents.Count; i++)
            {
                total += contents[i].Amount;
                mix += contents[i].Species.JarColor * contents[i].Amount;
            }
            hasContents = total > 0;
            SetLiquidVisible(hasContents);
            if (!hasContents) return;

            var color = mix / total;
            color.a = 1f;
            for (int i = 0; i < liquidRenderers.Length; i++)
            {
                var r = liquidRenderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(ColorId, color);
                r.SetPropertyBlock(block);
            }
        }

        public void PlayPour(SpeciesData species) => PlayOnce(stirClip, stirSeconds);

        public void PlayDrain() => PlayOnce(stirClip, stirSeconds * 0.6f);

        public void SetStoveLit(bool lit)
        {
            if (stoveLit == lit) return;
            stoveLit = lit;
            if (fireRoutine != null) StopCoroutine(fireRoutine);
            fireRoutine = fire != null ? StartCoroutine(lit ? FireOn() : FireOff()) : null;
            if (cauldronRoutine == null) Play(cauldron, RestingClip);
        }

        public void PlayFoam(float duration) => PlayOnce(foamClip, Mathf.Max(duration, foamClipSeconds));

        public void PlayReaction(float duration)
        {
            PlayOnce(successClip, Mathf.Max(duration, successClipSeconds));
            StartSteam(steamEmitSeconds);
        }

        /// <summary>Tek seferlik klipten sonra dönülen durum: ocak yanarken kaynama, değilse boşta.</summary>
        private string RestingClip => stoveLit && hasContents ? boilClip : idleClip;

        private void PlayOnce(string clip, float seconds)
        {
            if (cauldron == null) return;
            if (cauldronRoutine != null) StopCoroutine(cauldronRoutine);
            cauldronRoutine = StartCoroutine(Once(clip, seconds));
        }

        private IEnumerator Once(string clip, float seconds)
        {
            Play(cauldron, clip);
            yield return new WaitForSeconds(seconds);
            cauldronRoutine = null;
            Play(cauldron, RestingClip);
        }

        private IEnumerator FireOn()
        {
            Play(fire, fireIgniteClip);
            yield return new WaitForSeconds(igniteSeconds);
            Play(fire, fireLoopClip);
            fireRoutine = null;
        }

        private IEnumerator FireOff()
        {
            Play(fire, fireEndClip);
            yield return new WaitForSeconds(fireEndSeconds);
            Play(fire, fireOffClip);
            fireRoutine = null;
        }

        private void SetLiquidVisible(bool visible)
        {
            for (int i = 0; i < liquidRenderers.Length; i++)
                if (liquidRenderers[i] != null) liquidRenderers[i].enabled = visible;
        }

        private void Play(Animator animator, string clip)
        {
            if (animator == null || string.IsNullOrEmpty(clip) || !animator.isActiveAndEnabled) return;
            animator.CrossFadeInFixedTime(clip, crossFade);
        }
    }
}
