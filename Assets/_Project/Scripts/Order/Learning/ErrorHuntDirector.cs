using System.Collections;
using System.Collections.Generic;
using System.Text;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using BilgiAdasi.Player;
using BilgiAdasi.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Hata Dedektifi akışı: görevin dedektif işi varsa (D-3) usta haber verir, tahtada "?" belirir. Oyuncu tahta
    /// istasyonuna girince: dengesiz elementleri işaretle → kontrol (yanlışta sayılar tebeşirle yazılır) →
    /// "Neden?" → ustanın cevabı → sonraki tur. Bitince bir daha sorulmaz (SaveData flag). Görev kilitlenmez.
    /// Girdi Station map'inden: SelectPrevious/Next (W S / ↑↓ / D-pad), Confirm (Enter / A), ExitStation çıkar.
    /// Sahneye elle konmaz (kazanı olan sahnede kurulur), veri Resources/ErrorHuntData.
    /// </summary>
    public sealed class ErrorHuntDirector : MonoBehaviour
    {
        private const string ObjectName = "_ErrorHuntDirector";
        private const string Cursor = "<color=#F2D27A>›</color> ";
        private const string NoCursor = "   ";
        private const string BadInk = "#F08A74";
        private const string GoodInk = "#A6E03C";

        private enum Phase { Marking, Why, Reply, Done }

        [SerializeField] private ErrorHuntData data;
        [SerializeField] private OrderController orderController;
        [SerializeField] private float noticeDelay = 2.5f;

        private readonly List<ElementData> order = new List<ElementData>();
        private readonly Dictionary<ElementData, int> left = new Dictionary<ElementData, int>();
        private readonly Dictionary<ElementData, int> right = new Dictionary<ElementData, int>();
        private readonly HashSet<ElementData> marked = new HashSet<ElementData>();
        private readonly StringBuilder sb = new StringBuilder(512);

        private ErrorHuntEntry entry;
        private ChalkboardStation station;
        private Phase phase;
        private int round;
        private int cursor;
        private bool revealed;
        private string feedback;
        private string reply;
        private string noticedTask;
        private bool listening;

        public static ErrorHuntDirector Instance { get; private set; }

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
            if (Instance != null) return;
            var controller = FindAnyObjectByType<OrderController>();
            if (controller == null) return;
            var asset = Resources.Load<ErrorHuntData>(ErrorHuntData.ResourcePath);
            if (asset == null) return;
            var go = new GameObject(ObjectName);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.SetActive(false);
            var d = go.AddComponent<ErrorHuntDirector>();
            d.data = asset;
            d.orderController = controller;
            go.SetActive(true);
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;
            StationBase.Entered += OnStationEntered;
            StationBase.Exited += OnStationExited;
            GameEvents.ActiveOrderChanged += OnActiveOrderChanged;
            ViewLocator.Changed += OnViewsChanged;
            RefreshPending(false);
        }

        private void OnDisable()
        {
            StationBase.Entered -= OnStationEntered;
            StationBase.Exited -= OnStationExited;
            GameEvents.ActiveOrderChanged -= OnActiveOrderChanged;
            ViewLocator.Changed -= OnViewsChanged;
            StopListening();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------- bekleyen iş ----------

        private ErrorHuntEntry PendingEntry()
        {
            var task = orderController != null ? orderController.ActiveTask : null;
            if (task == null || data == null) return null;
            var e = data.Find(task.Id);
            if (e == null || e.Rounds.Count == 0) return null;
            var save = Save;
            if (save != null && save.HasFlag(e.FlagId)) return null;
            return e;
        }

        private void OnActiveOrderChanged(TaskData _) => RefreshPending(true);

        private void OnViewsChanged(System.Type type)
        {
            if (type == typeof(IErrorHuntView)) RefreshPending(false);
        }

        private void RefreshPending(bool notify)
        {
            var e = PendingEntry();
            ViewLocator.Get<IErrorHuntView>()?.SetPending(e != null);
            if (e != null && notify && noticedTask != e.TaskId)
            {
                noticedTask = e.TaskId;
                StartCoroutine(NoticeLater());
            }
        }

        private IEnumerator NoticeLater()
        {
            yield return new WaitForSeconds(noticeDelay);
            if (PendingEntry() != null) ViewLocator.Get<INoticeView>()?.ShowNotice(WorkshopTexts.Get().HuntNotice);
        }

        // ---------- istasyon ----------

        private void OnStationEntered(StationBase s)
        {
            if (!(s is ChalkboardStation board)) return;
            var e = PendingEntry();
            if (e == null) return;
            entry = e;
            station = board;
            round = 0;
            StartRound();
            StartListening();
        }

        private void OnStationExited(StationBase s)
        {
            if (s != station) return;
            StopListening();
            ViewLocator.Get<IErrorHuntView>()?.HideHunt();
            station = null;
            entry = null;
            RefreshPending(false);
        }

        private void StartListening()
        {
            if (listening) return;
            listening = true;
            var st = GameInput.Controls.Station;
            st.SelectPrevious.performed += OnPrev;
            st.SelectNext.performed += OnNext;
            st.Confirm.performed += OnConfirm;
        }

        private void StopListening()
        {
            if (!listening) return;
            listening = false;
            if (!GameInput.TryGetControls(out var c)) return;
            c.Station.SelectPrevious.performed -= OnPrev;
            c.Station.SelectNext.performed -= OnNext;
            c.Station.Confirm.performed -= OnConfirm;
        }

        // ---------- akış ----------

        private ErrorHuntRound Round => entry.Rounds[round];

        private void StartRound()
        {
            var r = Round;
            ErrorHuntLogic.Count(r.Reactants, r.Products, order, left, right);
            marked.Clear();
            revealed = false;
            feedback = null;
            cursor = 0;
            phase = Phase.Marking;
            Render();
        }

        private int OptionCount()
        {
            switch (phase)
            {
                case Phase.Marking: return order.Count + 1; // elementler + "Kontrol et"
                case Phase.Why: return Round.Why.Options.Count;
                default: return 1;
            }
        }

        private void OnPrev(InputAction.CallbackContext _) => Move(-1);
        private void OnNext(InputAction.CallbackContext _) => Move(+1);

        private void Move(int d)
        {
            if (entry == null) return;
            int n = OptionCount();
            cursor = (cursor + d + n) % n;
            Render();
        }

        private void OnConfirm(InputAction.CallbackContext _)
        {
            if (entry == null) return;
            var texts = WorkshopTexts.Get();
            switch (phase)
            {
                case Phase.Marking:
                    if (cursor < order.Count)
                    {
                        var e = order[cursor];
                        if (!marked.Remove(e)) marked.Add(e);
                    }
                    else if (ErrorHuntLogic.IsCorrect(order, left, right, marked))
                    {
                        revealed = true;
                        feedback = "<color=" + GoodInk + ">" + texts.HuntCorrect + "</color>";
                        if (Round.Why != null && !Round.Why.IsEmpty)
                        {
                            phase = Phase.Why;
                            cursor = 0;
                        }
                        else NextRound();
                    }
                    else
                    {
                        revealed = true;
                        feedback = "<color=" + BadInk + ">" + texts.HuntWrong + "</color>";
                        GameEvents.RaiseFailed(entry.TaskId);
                    }
                    break;

                case Phase.Why:
                    reply = Round.Why.ReplyFor(cursor);
                    bool ok = cursor == Round.Why.CorrectIndex;
                    reply = "<color=" + (ok ? GoodInk : BadInk) + ">" + Round.Why.Options[cursor] + "</color>\n" + reply;
                    phase = Phase.Reply;
                    cursor = 0;
                    break;

                case Phase.Reply:
                    NextRound();
                    return;

                case Phase.Done:
                    station?.Exit();
                    return;
            }
            Render();
        }

        private void NextRound()
        {
            round++;
            if (round < entry.Rounds.Count)
            {
                StartRound();
                return;
            }
            Save?.SetFlag(entry.FlagId);
            phase = Phase.Done;
            cursor = 0;
            ViewLocator.Get<IErrorHuntView>()?.SetPending(false);
            Render();
        }

        // ---------- çizim ----------

        private void Render()
        {
            var view = ViewLocator.Get<IErrorHuntView>();
            if (view == null || entry == null) return;
            var texts = WorkshopTexts.Get();
            string title = texts.HuntTitle + "   <size=60%>" + string.Format(texts.RoundFormat, Mathf.Min(round + 1, entry.Rounds.Count), entry.Rounds.Count) + "</size>";
            sb.Clear();

            if (phase == Phase.Done)
            {
                sb.Append(texts.HuntDone).Append("\n\n").Append(Cursor).Append(texts.HuntContinue);
                view.ShowHunt(texts.HuntTitle, sb.ToString(), Footer(texts));
                return;
            }

            var r = Round;
            sb.Append("<size=120%>").Append(FormulaFormatter.Equation(r.Reactants, r.Products)).Append("</size>\n\n");

            if (phase == Phase.Marking)
            {
                sb.Append("<size=80%>").Append(texts.HuntQuestion).Append("</size>\n");
                for (int i = 0; i < order.Count; i++)
                {
                    var e = order[i];
                    sb.Append(i == cursor ? Cursor : NoCursor);
                    sb.Append(marked.Contains(e) ? "[x]  " : "[  ]  ").Append("<b>").Append(e.Symbol).Append("</b>");
                    if (revealed) AppendCount(e, texts);
                    sb.Append('\n');
                }
                sb.Append(cursor == order.Count ? Cursor : NoCursor).Append("<b>").Append(texts.HuntCheck).Append("</b>\n");
                if (feedback != null) sb.Append("\n<size=80%>").Append(feedback).Append("</size>");
            }
            else
            {
                // Sayılar artık tahtada; altında "Neden?" ya da ustanın cevabı.
                for (int i = 0; i < order.Count; i++)
                {
                    sb.Append(NoCursor).Append("<b>").Append(order[i].Symbol).Append("</b>");
                    AppendCount(order[i], texts);
                    sb.Append('\n');
                }
                sb.Append('\n');
                if (phase == Phase.Why)
                {
                    var why = r.Why;
                    sb.Append("<size=85%>").Append(data.MasterName).Append(": ").Append(why.Question).Append("</size>\n");
                    for (int i = 0; i < why.Options.Count; i++)
                        sb.Append(i == cursor ? Cursor : NoCursor).Append("<size=80%>").Append(why.Options[i]).Append("</size>\n");
                }
                else
                {
                    sb.Append("<size=80%>").Append(reply).Append("</size>\n\n").Append(Cursor).Append(texts.HuntContinue);
                }
            }
            view.ShowHunt(title, sb.ToString(), Footer(texts));
        }

        private void AppendCount(ElementData e, WorkshopTexts texts)
        {
            left.TryGetValue(e, out int l);
            right.TryGetValue(e, out int rr);
            sb.Append("    <color=").Append(l == rr ? GoodInk : BadInk).Append('>')
              .Append(string.Format(texts.HuntCount, e.Symbol, l, rr)).Append("</color>");
        }

        private static string Footer(WorkshopTexts texts) =>
            string.Format(texts.HuntControls, ControlGlyphs.For(PromptAction.Navigate), ControlGlyphs.For(PromptAction.Confirm),
                ControlGlyphs.For(PromptAction.ExitStation));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
