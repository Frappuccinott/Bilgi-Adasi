using System.Collections;
using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Kazanın 3D görünümü (ICauldronView): sıvı yüzeyi (renk = içeriğin ortalaması, yükseklik = kavanoz sayısı),
    /// dökümde sıçrama, köpük, ateş. Objeler yer tutucudur; modeller gelince alanlar yeniden bağlanır.
    /// </summary>
    public sealed class CauldronVisuals : RegisteredView<ICauldronView>, ICauldronView
    {
        [SerializeField] private Renderer liquid;
        [SerializeField] private float liquidRisePerJar = 0.012f;
        [SerializeField] private float liquidMaxRise = 0.15f;
        [SerializeField] private GameObject foam;
        [SerializeField] private GameObject fire;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock block;
        private Vector3 liquidBasePosition, liquidBaseScale, foamBaseScale;
        private Coroutine foamRoutine, pulseRoutine;

        private void Awake()
        {
            if (liquid != null)
            {
                liquidBasePosition = liquid.transform.localPosition;
                liquidBaseScale = liquid.transform.localScale;
                liquid.gameObject.SetActive(false);
            }
            if (foam != null)
            {
                foamBaseScale = foam.transform.localScale;
                foam.SetActive(false);
            }
            if (fire != null) fire.SetActive(false);
        }

        public void ShowContents(IReadOnlyList<SpeciesAmount> contents)
        {
            if (liquid == null) return;
            int total = 0;
            Color mix = Color.clear;
            for (int i = 0; i < contents.Count; i++)
            {
                total += contents[i].Amount;
                mix += contents[i].Species.JarColor * contents[i].Amount;
            }
            liquid.gameObject.SetActive(total > 0);
            if (total == 0) return;

            liquid.transform.localPosition = liquidBasePosition + Vector3.up * Mathf.Min(total * liquidRisePerJar, liquidMaxRise);
            var color = mix / total;
            color.a = 1f;
            block ??= new MaterialPropertyBlock();
            liquid.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            liquid.SetPropertyBlock(block);
        }

        public void PlayPour(SpeciesData species) => Pulse(0.25f, 0.12f);

        public void PlayDrain() => Pulse(0.3f, 0.2f);

        public void SetStoveLit(bool lit)
        {
            if (fire != null) fire.SetActive(lit);
        }

        public void PlayFoam(float duration)
        {
            if (foam == null) return;
            if (foamRoutine != null) StopCoroutine(foamRoutine);
            foamRoutine = StartCoroutine(FoamRoutine(duration));
        }

        public void PlayReaction(float duration) => Pulse(duration, 0.06f);

        private void Pulse(float duration, float amount)
        {
            if (liquid == null || !liquid.gameObject.activeInHierarchy) return;
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseRoutine(duration, amount));
        }

        private IEnumerator PulseRoutine(float duration, float amount)
        {
            var t = liquid.transform;
            for (float e = 0f; e < duration; e += Time.deltaTime)
            {
                float k = 1f - e / duration;
                t.localScale = liquidBaseScale * (1f + Mathf.Sin(e * 30f) * amount * k);
                yield return null;
            }
            t.localScale = liquidBaseScale;
            pulseRoutine = null;
        }

        private IEnumerator FoamRoutine(float duration)
        {
            foam.SetActive(true);
            var t = foam.transform;
            for (float e = 0f; e < duration; e += Time.deltaTime)
            {
                float k = e / duration;
                float grow = Mathf.Sin(Mathf.Clamp01(k * 1.4f) * Mathf.PI * 0.5f);
                float wobble = 1f + Mathf.Sin(e * 25f) * 0.06f;
                t.localScale = foamBaseScale * (0.3f + 0.9f * grow) * wobble * (1f - Mathf.Clamp01((k - 0.8f) * 5f) * 0.7f);
                yield return null;
            }
            t.localScale = foamBaseScale;
            foam.SetActive(false);
            foamRoutine = null;
        }
    }
}
