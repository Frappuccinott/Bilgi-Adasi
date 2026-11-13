using System.Collections;
using BilgiAdasi.Core;
using BilgiAdasi.Player;
using BilgiAdasi.UI;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Atölyenin kilitli dolabı (10-gorev-pazar-npc: cıva, kezzap, kral suyu, zırnık pazarda yok, yalnızca burada).
    /// E / X: kapak kilide takılıp sarsılır, güvenlik notu çıkar. Açılmaz; metin WorkshopTexts'te.
    /// </summary>
    public sealed class LockedCabinet : WorldInteractable
    {
        [Tooltip("Sarsılan kapak(lar). Menteşe ekseninde Y etrafında birkaç derece oynar.")]
        [SerializeField] private Transform[] doors = new Transform[0];
        [Tooltip("Kilit (hafifçe sallanır). Boş bırakılabilir.")]
        [SerializeField] private Transform padlock;
        [SerializeField] private float rattleAngle = 2.5f;
        [SerializeField] private float rattleTime = 0.45f;
        [Tooltip("Sarsılınca çalan ses (isteğe bağlı).")]
        [SerializeField] private AudioSource rattleSound;

        private Quaternion[] doorRest;
        private Quaternion lockRest;
        private Coroutine routine;

        protected override void Awake()
        {
            base.Awake();
            doorRest = new Quaternion[doors.Length];
            for (int i = 0; i < doors.Length; i++) if (doors[i] != null) doorRest[i] = doors[i].localRotation;
            if (padlock != null) lockRest = padlock.localRotation;
        }

        public override string GetPrompt(PlayerInteractor interactor) => WorkshopTexts.Get().CabinetPrompt;

        public override void Interact(PlayerInteractor interactor)
        {
            ViewLocator.Get<INoticeView>()?.ShowNotice(WorkshopTexts.Get().CabinetNotice);
            if (rattleSound != null) rattleSound.Play();
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Rattle());
        }

        private IEnumerator Rattle()
        {
            float t = 0f;
            while (t < rattleTime)
            {
                t += Time.deltaTime;
                float k = 1f - t / rattleTime;
                float a = Mathf.Sin(t * 70f) * rattleAngle * k;
                for (int i = 0; i < doors.Length; i++)
                    if (doors[i] != null) doors[i].localRotation = doorRest[i] * Quaternion.Euler(0f, i % 2 == 0 ? a : -a, 0f);
                if (padlock != null) padlock.localRotation = lockRest * Quaternion.Euler(a * 4f, 0f, 0f);
                yield return null;
            }
            for (int i = 0; i < doors.Length; i++) if (doors[i] != null) doors[i].localRotation = doorRest[i];
            if (padlock != null) padlock.localRotation = lockRest;
            routine = null;
        }
    }
}
