using System;
using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using BilgiAdasi.Player;
using BilgiAdasi.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Defter (14-defter-plani): oyuncunun yanındaki tek defter. Üç ayraç: Ustanın Notları (ders notları), Keşiflerim
    /// (K-n kayıtları + öteki derslerin saha notları), Ustalık (köy mühürleri, sınıf, boy, başarım).
    /// N (gamepad D-pad ↑) ile açılır; açıkken oyun durur (UI modu, timeScale 0). Esc menüsündeki "Keşif Defteri" de bunu açar.
    /// Veri: Resources/LearningData + köyler (NeighborhoodRunner) + kayıt. Sahneye elle konmaz: köyü olan sahnede kendini kurar.
    /// Görünüme yalnızca arayüzle konuşur (INotebookView, INotebookHudView).
    /// </summary>
    public sealed class NotebookDirector : MonoBehaviour, INotebookSource, IDiscoveryBookCommands
    {
        private const string ObjectName = "_NotebookDirector";

        [SerializeField] private LearningData data;
        [SerializeField] private NeighborhoodRunner runner;

        private InputAction openAction;
        private PlayerInteractor blockedUser;
        private float timeScaleBefore = 1f;
        private InputMode modeBefore;
        private NotebookTab lastTab = NotebookTab.Discoveries;
        private string focusCode;

        /// <summary>Defter oyuncu tarafından (N) açıkken true.</summary>
        public static bool IsOpen { get; private set; }

        private static SaveData Save => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

        // ---------- kendiliğinden kurulum ----------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureFor(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode _) => EnsureFor(scene);

        private static void EnsureFor(Scene scene)
        {
            if (FindAnyObjectByType<NotebookDirector>() != null) return;
            var r = FindAnyObjectByType<NeighborhoodRunner>();
            if (r == null) return;
            var asset = Resources.Load<LearningData>(LearningData.ResourcePath);
            if (asset == null) return;
            var go = new GameObject(ObjectName);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.SetActive(false);
            var d = go.AddComponent<NotebookDirector>();
            d.data = asset;
            d.runner = r;
            go.SetActive(true);
        }

        private void OnEnable()
        {
            if (GameInput.TryGetControls(out _))
            {
                openAction = GameInput.FindAction("Player/OpenBook");
                openAction.performed += OnOpenPressed;
            }
            GameEvents.TaskCompleted += OnTaskCompleted;
            ViewLocator.Register<INotebookSource>(this);
            // LearningDirector'ın düz listesinin yerine Esc menüsündeki "Keşif Defteri" bu defteri açar.
            ViewLocator.Register<IDiscoveryBookCommands>(this);
        }

        private void Start() => ViewLocator.Register<IDiscoveryBookCommands>(this);

        private void OnDisable()
        {
            if (openAction != null && GameInput.TryGetControls(out _)) openAction.performed -= OnOpenPressed;
            GameEvents.TaskCompleted -= OnTaskCompleted;
            ViewLocator.Unregister<INotebookSource>(this);
            ViewLocator.Unregister<IDiscoveryBookCommands>(this);
            if (IsOpen) ViewLocator.Get<INotebookView>()?.Close();
        }

        // ---------- açma / kapama ----------

        private void OnOpenPressed(InputAction.CallbackContext _)
        {
            if (IsOpen || GameInput.Mode != InputMode.Player) return;
            if (LearningDirector.IsBusy || StationBase.Active != null || DialogueFocus.IsOpen) return;
            var dialogue = runner != null ? runner.GetComponent<DialogueDirector>() : null;
            if (dialogue != null && dialogue.IsPlaying) return;
            var view = ViewLocator.Get<INotebookView>();
            if (view == null || view.IsOpen) return;

            IsOpen = true;
            blockedUser = FindAnyObjectByType<PlayerInteractor>();
            if (blockedUser != null) blockedUser.SetControlEnabled(false);
            modeBefore = GameInput.Mode;
            GameInput.SetMode(InputMode.UI);
            timeScaleBefore = Time.timeScale;
            Time.timeScale = 0f;

            var model = BuildModel();
            view.OpenBook(model, focusCode != null ? NotebookTab.Discoveries : lastTab, OnClosedByPlayer);
            focusCode = null;
        }

        private void OnClosedByPlayer()
        {
            IsOpen = false;
            Time.timeScale = timeScaleBefore <= 0f ? 1f : timeScaleBefore;
            GameInput.SetMode(modeBefore == InputMode.None ? InputMode.Player : modeBefore);
            if (blockedUser != null && StationBase.Active == null) blockedUser.SetControlEnabled(true);
        }

        /// <summary>Esc menüsünden: menü UI modunda ve oyun durmuş; kapanınca menüye dönülür.</summary>
        public void OpenBook(Action onClosed)
        {
            var view = ViewLocator.Get<INotebookView>();
            if (view == null)
            {
                onClosed?.Invoke();
                return;
            }
            view.OpenBook(BuildModel(), NotebookTab.Discoveries, onClosed);
        }

        private void OnTaskCompleted(string taskId)
        {
            var entry = data != null ? data.Find(taskId) : null;
            if (entry == null || string.IsNullOrEmpty(entry.DiscoveryId) || string.IsNullOrWhiteSpace(entry.DiscoveryText)) return;
            // Bir sonraki açılışta defter yeni sayfada açılır; HUD sırtı parlar.
            focusCode = entry.DiscoveryId;
            ViewLocator.Get<INotebookHudView>()?.ShowNewPage(entry.DiscoveryId, entry.Title);
        }

        // ---------- anlık görüntü ----------

        public NotebookModel BuildModel()
        {
            var model = new NotebookModel
            {
                Texts = data.NotebookTexts,
                LessonTexts = data.LessonTexts,
                FocusCode = focusCode
            };
            var texts = data.NotebookTexts;
            var save = Save;
            var lessons = LearningDirector.Instance;
            var neighborhoods = runner != null ? runner.Neighborhoods : null;
            if (neighborhoods == null) return model;

            int sealedCount = 0, taskCount = 0;
            for (int ni = 0; ni < neighborhoods.Count; ni++)
            {
                var n = neighborhoods[ni];
                if (n == null) continue;
                string groupTitle = texts.GroupTitle(n.Id, n.Title);
                int done = runner.CompletedCount(n);
                string count = n.Tasks.Count > 1 ? done + "/" + n.Tasks.Count : null;

                var discoveries = new NotebookGroup { Title = groupTitle, Count = count };
                var notes = new NotebookGroup { Title = groupTitle, Count = count };
                var village = new MasteryVillage { Title = groupTitle, Done = done };
                bool anyLesson = false;

                for (int ti = 0; ti < n.Tasks.Count; ti++)
                {
                    var task = n.Tasks[ti];
                    if (task == null) continue;
                    var entry = data.Find(task.Id);
                    bool completed = runner.IsTaskDone(task);
                    bool written = completed || (save != null && save.ReadyTaskId == task.Id);
                    string title = entry != null && !string.IsNullOrEmpty(entry.Title) ? entry.Title : task.GiverName;
                    taskCount++;
                    if (completed) sealedCount++;

                    // Keşiflerim
                    if (entry != null && !string.IsNullOrWhiteSpace(entry.DiscoveryText))
                    {
                        var row = new NotebookRow
                        {
                            Code = entry.DiscoveryId,
                            Title = written ? title : string.Format(texts.NotWritten, title),
                            Enabled = written,
                            Kicker = entry.DiscoveryId,
                            Subtitle = n.Tasks.Count > 1 ? string.Format(texts.PlaceFormat, entry.PlaceLine, groupTitle, ti + 1) : entry.PlaceLine,
                            Body = written ? entry.DiscoveryText : texts.EmptyDiscovery,
                            Equation = written ? entry.Equation : string.Empty,
                            IconFormula = written ? entry.IconFormula : string.Empty,
                            Footer = written ? entry.Usage : string.Empty
                        };
                        if (written)
                        {
                            row.FieldNotes.AddRange(entry.FieldNotes);
                            AddFieldNotes(model, entry, groupTitle, title);
                        }
                        if (written) model.WrittenCount++;
                        discoveries.Rows.Add(row);
                    }

                    // Ustanın Notları
                    bool hasLesson = entry != null && entry.Lesson != null && entry.Lesson.HasPages;
                    anyLesson |= hasLesson;
                    bool opened = hasLesson && (completed || (lessons != null && lessons.IsCurrentTask(task.Id)));
                    if (opened) model.OpenedLessonCount++;
                    notes.Rows.Add(new NotebookRow
                    {
                        Code = (ti + 1).ToString(),
                        Title = !hasLesson ? texts.NoLesson : opened ? entry.Lesson.Title : texts.LessonLocked,
                        Enabled = opened,
                        Kicker = texts.LessonKicker,
                        Subtitle = string.Format(texts.LessonDetail, groupTitle, ti + 1),
                        Body = opened ? FirstBody(entry.Lesson) : string.Empty,
                        Lesson = opened ? entry.Lesson : null
                    });

                    // Ustalık
                    var state = completed ? MasteryState.Done : ti == done ? MasteryState.Next : MasteryState.Locked;
                    int fails = save != null ? save.GetFailCount(task.Id) : 0;
                    string detail = state == MasteryState.Done
                        ? (fails == 0 ? texts.NoFails : string.Format(texts.FailFormat, fails))
                        : state == MasteryState.Next ? texts.StateNext + "." : texts.StateLocked + ".";
                    village.Tasks.Add(new MasteryTask
                    {
                        Title = title,
                        Detail = string.IsNullOrEmpty(task.GiverName) ? detail : task.GiverName + ". " + detail,
                        State = state
                    });
                }

                if (discoveries.Rows.Count > 0) model.Discoveries.Add(discoveries);
                if (anyLesson) model.Lessons.Add(notes);
                else if (n.Tasks.Count > 1)
                {
                    // Bu köyün hiç ders notu yok: beş boş satır yerine tek satır.
                    notes.Rows.Clear();
                    notes.Rows.Add(new NotebookRow
                    {
                        Code = "1…" + n.Tasks.Count,
                        Title = texts.LessonsLater,
                        Enabled = false,
                        Kicker = texts.LessonKicker,
                        Subtitle = groupTitle
                    });
                    model.Lessons.Add(notes);
                }
                model.Villages.Add(village);
            }

            // Ustalık özeti
            if (save != null) model.MasteryStats.Add(new KeyValuePair<string, string>(texts.GradeLabel, save.Grade.ToString()));
            var motor = FindAnyObjectByType<PlayerMotor>();
            if (motor != null)
                model.MasteryStats.Add(new KeyValuePair<string, string>(texts.HeightLabel,
                    string.Format(texts.HeightFormat, motor.StandingHeight)));
            model.MasteryStats.Add(new KeyValuePair<string, string>(texts.SealedLabel, sealedCount + " / " + taskCount));
            var achievements = FindAnyObjectByType<AchievementController>();
            if (achievements != null)
            {
                var info = achievements.BuildMenuInfo();
                model.MasteryStats.Add(new KeyValuePair<string, string>(texts.AchievementsLabel,
                    info.UnlockedCount + " / " + info.TotalCount));
            }
            return model;
        }

        private static void AddFieldNotes(NotebookModel model, LearningEntry entry, string groupTitle, string title)
        {
            var notes = entry.FieldNotes;
            for (int i = 0; i < notes.Count; i++)
            {
                var note = notes[i];
                if (note == null || string.IsNullOrEmpty(note.Subject)) continue;
                if (!model.FieldNotesBySubject.TryGetValue(note.Subject, out var groups))
                    model.FieldNotesBySubject[note.Subject] = groups = new List<NotebookGroup>();
                var group = groups.Count > 0 && groups[groups.Count - 1].Title == groupTitle ? groups[groups.Count - 1] : null;
                if (group == null)
                {
                    group = new NotebookGroup { Title = groupTitle };
                    groups.Add(group);
                }
                var row = new NotebookRow
                {
                    Code = entry.DiscoveryId,
                    Title = title,
                    Enabled = true,
                    Kicker = note.Heading,
                    Body = note.Text,
                    FieldSubject = note.Subject
                };
                group.Rows.Add(row);
            }
        }

        private static string FirstBody(LessonNote lesson)
        {
            for (int p = 0; p < lesson.Pages.Count; p++)
            {
                var blocks = lesson.Pages[p].Blocks;
                for (int b = 0; b < blocks.Count; b++)
                    if (blocks[b] != null && blocks[b].Kind == LessonBlockKind.Body) return blocks[b].Text;
            }
            return string.Empty;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsOpen = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
