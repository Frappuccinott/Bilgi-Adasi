using System.Collections;
using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>Kazan_Rig + Ocak_Ates_Rig canlandırmaları (08 §8 animasyon sözleşmesi: yalnız Animator.Play).</summary>
    public sealed class WorkshopCauldronView : RegisteredView<ICauldronView>, ICauldronView
    {
        [SerializeField] private Animator cauldron;
        [SerializeField] private Animator fire;
        [SerializeField] private string idle = "Kazan_Bosta";
        [SerializeField] private string stir = "Kazan_Karistir_Dongu";
        [SerializeField] private string boil = "Kazan_Kaynama_Dongu";
        [SerializeField] private string foam = "Kazan_Kopurme";
        [SerializeField] private string success = "Kazan_Basari";
        [SerializeField] private float stirSeconds = 1.2f;
        [SerializeField] private float foamSeconds = 2.2f;
        [SerializeField] private float successSeconds = 1.9f;
        [SerializeField] private string fireOff = "Ates_Bosta";
        [SerializeField] private string fireIgnite = "Ates_Yan";
        [SerializeField] private string fireLoop = "Ates_Titre_Dongu";
        [SerializeField] private string fireEnd = "Ates_Son";
        [SerializeField] private float igniteSeconds = 0.63f;

        private bool boiling;
        private bool lit;
        private Coroutine cauldronRoutine;
        private Coroutine fireRoutine;

        public float SuccessDuration => successSeconds;

        public void PlayStir()
        {
            if (boiling) return;
            PlayTimed(stir, stirSeconds);
        }

        public void SetBoiling(bool value)
        {
            boiling = value;
            StopRoutine(ref cauldronRoutine);
            Play(cauldron, boiling ? boil : idle);
        }

        public void SetFire(bool value)
        {
            if (lit == value) return;
            lit = value;
            StopRoutine(ref fireRoutine);
            if (fire == null) return;
            fireRoutine = StartCoroutine(lit ? FireOn() : FireOff());
        }

        public void PlayFoam() => PlayTimed(foam, foamSeconds);

        public void PlaySuccess() => PlayTimed(success, successSeconds);

        private void PlayTimed(string clip, float seconds)
        {
            StopRoutine(ref cauldronRoutine);
            cauldronRoutine = StartCoroutine(Timed(clip, seconds));
        }

        private IEnumerator Timed(string clip, float seconds)
        {
            Play(cauldron, clip);
            yield return new WaitForSeconds(seconds);
            Play(cauldron, boiling ? boil : idle);
            cauldronRoutine = null;
        }

        private IEnumerator FireOn()
        {
            Play(fire, fireIgnite);
            yield return new WaitForSeconds(igniteSeconds);
            Play(fire, fireLoop);
            fireRoutine = null;
        }

        private IEnumerator FireOff()
        {
            Play(fire, fireEnd);
            yield return new WaitForSeconds(0.5f);
            Play(fire, fireOff);
            fireRoutine = null;
        }

        private void StopRoutine(ref Coroutine routine)
        {
            if (routine == null) return;
            StopCoroutine(routine);
            routine = null;
        }

        private static void Play(Animator animator, string clip)
        {
            if (animator != null && !string.IsNullOrEmpty(clip)) animator.CrossFade(clip, 0.1f);
        }
    }
}
