using BilgiAdasi.Player;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>Tarlada bekleyen kişi (FieldTask konuşma tablosundaki sırası).</summary>
    public enum FieldNpcRole
    {
        Giver = 0,   // Çiftçi Elif
        Math = 1,    // Hesap Ustası
        Biology = 2  // Hayat Ustası
    }

    /// <summary>
    /// Tarladaki kişi (Elif, Hesap Ustası, Hayat Ustası): bakıp Interact (E / X) → görevin o anki aşamasına göre bir ipucu söyler
    /// (cevabı vermez). Konuşma ve metinler FieldTaskDirector / FieldTask'ta; bileşen yalnızca haber verir ve konuşurken oyuncuya döner.
    /// </summary>
    public sealed class FieldNpc : WorldInteractable
    {
        [SerializeField] private FieldTaskDirector director;
        [SerializeField] private FieldNpcRole role;
        [Tooltip("Konuşurken oyuncuya dönme hızı (°/sn).")]
        [SerializeField] private float turnSpeed = 240f;

        private Transform lookTarget;
        private float lookUntil;

        public FieldNpcRole Role => role;

        public override bool CanInteract => base.CanInteract && director != null && director.CanTalk;

        public override string GetPrompt(PlayerInteractor interactor) => director != null ? director.TalkPrompt(role) : string.Empty;

        public override void Interact(PlayerInteractor interactor)
        {
            if (director == null) return;
            director.Talk(role, interactor);
            lookTarget = interactor != null ? interactor.transform : null;
            lookUntil = Time.time + 6f;
        }

        private void Update()
        {
            if (lookTarget == null || Time.time > lookUntil) return;
            Vector3 dir = lookTarget.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            var want = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
        }
    }
}
