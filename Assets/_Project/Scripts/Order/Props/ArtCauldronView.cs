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

        private static readonly int ColorId = Shader.PropertyToID("_Renk");

        private MaterialPropertyBlock block;
        private bool stoveLit;
        private bool hasContents;
        private Coroutine cauldronRoutine;
        private Coroutine fireRoutine;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            SetLiquidVisible(false);
            Play(cauldron, idleClip);
            Play(fire, fireOffClip);
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

        public void PlayReaction(float duration) => PlayOnce(successClip, Mathf.Max(duration, successClipSeconds));

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
