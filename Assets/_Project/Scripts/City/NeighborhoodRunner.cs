using System;
using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Köyün görev sırası: sıradaki görev, alınmış (aktif) görev, tamamlama, köy parçaları.
    /// Sakinler (ResidentNpc) görevi verir; istasyonlar (kazan, masa) Active görevi çözer.
    /// </summary>
    [DefaultExecutionOrder(100)] // NeighborhoodView.Start'tan sonra çalışsın (parçaları kayda göre açar)
    public sealed class NeighborhoodRunner : MonoBehaviour
    {
        [SerializeField] private NeighborhoodData data;
        [SerializeField] private NeighborhoodView view;
        [SerializeField] private GameTexts texts;

        /// <summary>Görev alındı/bitti; sakin işaretleri ve HUD hedefi yenilenir.</summary>
        public event Action Changed;

        public NeighborhoodData Data => data;
        public GameTexts Texts => texts;

        private static SaveData Save => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

        private void Start()
        {
            RefreshView();
            Changed?.Invoke();
            PushObjective();
        }

        public bool IsCompleted(TaskData task) => task != null && Save != null && Save.IsTaskCompleted(task.TaskId);
        public bool IsAccepted(TaskData task) => task != null && Save != null && Save.IsTaskAccepted(task.TaskId);

        public int CompletedCount
        {
            get
            {
                int n = 0;
                if (data == null) return 0;
                for (int i = 0; i < data.Tasks.Count; i++) if (IsCompleted(data.Tasks[i])) n++;
                return n;
            }
        }

        /// <summary>Sırada bekleyen ilk bitmemiş görev (köy bittiyse null).</summary>
        public TaskData NextTask
        {
            get
            {
                if (data == null) return null;
                for (int i = 0; i < data.Tasks.Count; i++)
                    if (!IsCompleted(data.Tasks[i])) return data.Tasks[i];
                return null;
            }
        }

        /// <summary>Alınmış ama bitmemiş görev (yoksa null).</summary>
        public TaskData ActiveTask
        {
            get
            {
                var next = NextTask;
                return next != null && IsAccepted(next) ? next : null;
            }
        }

        public bool IsFirstTask(TaskData task) => data != null && data.IndexOf(task) == 0;

        public void Accept(TaskData task)
        {
            if (task == null || IsAccepted(task)) return;
            GameEvents.RaiseTaskAccepted(task.TaskId);
            Changed?.Invoke();
            PushObjective();
        }

        public void Complete(TaskData task)
        {
            if (task == null || IsCompleted(task)) return;
            GameEvents.RaiseTaskCompleted(task.TaskId);
            RefreshView();
            if (NextTask == null)
            {
                GameEvents.RaiseNeighborhoodCompleted(data.Id);
                if (texts != null) Dialogue.Toast(string.Format(texts.NeighborhoodDone, data.DisplayName), 4f);
            }
            Changed?.Invoke();
            PushObjective();
        }

        private void RefreshView()
        {
            if (view == null || data == null) return;
            bool done = Save != null && Save.IsNeighborhoodCompleted(data.Id);
            if (done) view.SetCompleted(true);
            else view.SetProgress(CompletedCount);
        }

        private void PushObjective()
        {
            var objective = ViewLocator.Get<IObjectiveView>();
            if (objective == null || texts == null) return;
            var active = ActiveTask;
            var next = NextTask;
            if (active != null) objective.SetObjective(string.Format(texts.ObjectiveActive, active.Title, active.ResidentName));
            else if (next != null) objective.SetObjective(string.Format(texts.ObjectiveNext, data.DisplayName, next.ResidentName));
            else objective.SetObjective(string.Empty);
        }
    }
}
