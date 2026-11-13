using BilgiAdasi.Core;
using BilgiAdasi.Player;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Köy sakini: görevi verir (05 §2 replikleri TaskData'dan). Sıradaki görevin sakininde "!" işareti açılır.
    /// Kökte collider + layer Interactable olmalı (PlayerInteractor bunu arar).
    /// </summary>
    public sealed class ResidentNpc : MonoBehaviour, IInteractable
    {
        [SerializeField] private NeighborhoodRunner runner;
        [SerializeField] private TaskData task;
        [Tooltip("NPC modelindeki Animator (boşsa çocuklarda aranır).")]
        [SerializeField] private Animator animator;
        [SerializeField] private string idleClip = "";
        [SerializeField] private string talkClip = "";
        [Tooltip("Sıradaki görev bu sakindeyse açılan işaret (\"!\").")]
        [SerializeField] private GameObject questMarker;
        [SerializeField] private GameObject highlight;

        public TaskData Task => task;
        public bool CanInteract => isActiveAndEnabled && !Dialogue.IsOpen && runner != null && task != null;
        public string PromptText => runner != null && runner.Texts != null
            ? runner.Texts.TalkPrompt + ": " + task.ResidentName
            : task != null ? task.ResidentName : string.Empty;

        public void Configure(NeighborhoodRunner owner, TaskData ownTask, Animator ownAnimator, string idle, string talk, GameObject marker)
        {
            runner = owner;
            task = ownTask;
            animator = ownAnimator;
            idleClip = idle;
            talkClip = talk;
            questMarker = marker;
        }

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (highlight != null) highlight.SetActive(false);
        }

        private void OnEnable()
        {
            if (runner != null) runner.Changed += RefreshMarker;
            RefreshMarker();
        }

        private void OnDisable()
        {
            if (runner != null) runner.Changed -= RefreshMarker;
        }

        public void SetHighlighted(bool highlighted)
        {
            if (highlight != null) highlight.SetActive(highlighted);
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract) return;
            var texts = runner.Texts;
            string speaker = task.ResidentName;
            PlayClip(talkClip);

            if (runner.IsCompleted(task))
            {
                Talk(speaker, task.AfterLine, null);
            }
            else if (runner.NextTask != task)
            {
                Talk(speaker, texts != null ? texts.ResidentWaitTurn : string.Empty, null);
            }
            else if (runner.IsAccepted(task))
            {
                Talk(speaker, task.ReminderLine, null);
            }
            else
            {
                Talk(speaker, task.RequestLine, () =>
                {
                    runner.Accept(task);
                    if (texts != null) Dialogue.Toast(string.Format(texts.OrderAccepted, task.Title), 4f);
                });
            }
        }

        private void Talk(string speaker, string line, System.Action after)
        {
            Dialogue.Begin();
            Dialogue.Say(speaker, line, () =>
            {
                Dialogue.End();
                PlayClip(idleClip);
                after?.Invoke();
            });
        }

        private void RefreshMarker()
        {
            if (questMarker == null || runner == null) return;
            bool show = runner.NextTask == task && !runner.IsAccepted(task);
            questMarker.SetActive(show);
        }

        private void PlayClip(string clip)
        {
            if (animator == null || string.IsNullOrEmpty(clip)) return;
            animator.CrossFade(clip, 0.2f);
        }
    }
}
