using System;
using System.Collections;
using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using BilgiAdasi.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Sipariş akışı (04 §5, karar 13.11): sipariş kapıda alınır → oyuncu malzemeyi pazardan/raftan envantere alır →
    /// kazana döker (geri alınamaz) → 3D terazi dengelenince ateşi yakar → ürün envantere gelir → kapıda teslim.
    /// Preparing ⇄ Foam (malzeme gider) → Reacting → (sonraki adım | ReadyToDeliver → [kapıda Deliver] → Done).
    /// Kazan objesinde durur; kazan/ocak/kol etkileşimleri buraya gelir. Görünümleri yalnızca arayüzle (ViewLocator) tanır.
    /// </summary>
    public sealed class OrderController : MonoBehaviour
    {
        public enum State
        {
            Idle,
            Preparing,
            Foam,
            Reacting,
            ReadyToDeliver,
            Done
        }

        private const int HintFailThreshold = 2;

        [SerializeField] private OrderTexts texts;
        [SerializeField] private float foamDuration = 1.2f;
        [SerializeField] private float reactionDuration = 1.5f;
        [Tooltip("\"Gerçek hayatta\" kartının ekranda kalma süresi (sn).")]
        [SerializeField] private float realLifeSeconds = 8f;

        private OrderSession session;
        private bool sealNoteShown;
        private PlayerInventory lastInventory;
        private InputAction hintAction;
        private Coroutine realLifeRoutine;

        private readonly List<SpeciesAmount> pending = new List<SpeciesAmount>();
        private readonly List<SpeciesData> reactantBuffer = new List<SpeciesData>();
        private readonly List<string> localBoard = new List<string>();

        private IOrderCardView cardView;
        private ICauldronView cauldronView;
        private IScaleView scaleView;
        private IChalkboardView boardView;
        private IHintBookView hintView;
        private IRealLifeCardView realLifeView;
        private INoticeView noticeView;

        public State Current { get; private set; } = State.Idle;
        public OrderSession Session => session;
        public OrderTexts Texts => texts;
        public OrderTask ActiveTask => session != null && Current != State.Done ? session.Task : null;
        public bool IsBusy => Current == State.Foam || Current == State.Reacting;
        public bool IsReadyToDeliver => Current == State.ReadyToDeliver;
        public bool CanPour => session != null && Current == State.Preparing;

        public bool HasPendingProduct => pending.Count > 0;

        /// <summary>Ürün hazır, kapıda teslim bekliyor.</summary>
        public event Action<OrderTask> ReadyToDeliver;
        /// <summary>Görev bitti (kapıda teslim edildi). NeighborhoodRunner dinler.</summary>
        public event Action<OrderTask> TaskFinished;
        /// <summary>Kazan içeriği ya da durum değişti (etkileşim ipuçları tazelenir).</summary>
        public event Action StateChanged;

        private static SaveData Data => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

        private void OnEnable()
        {
            ViewLocator.Changed += OnViewsChanged;
            hintAction = GameInput.FindAction("Player/OpenHint");
            hintAction.performed += OnHintAction;
            CacheViews();
            RefreshAll();
        }

        private void OnDisable()
        {
            ViewLocator.Changed -= OnViewsChanged;
            if (GameInput.TryGetControls(out _)) hintAction.performed -= OnHintAction;

            // Devre dışı kalınca coroutine'ler durur; köpürme/tepkime ortasında kalırsa görev kilitlenmesin.
            if (IsBusy)
            {
                StopAllCoroutines();
                session?.Clear();
                Current = State.Preparing;
            }
        }

        // ---------- Görev ----------

        public void BeginTask(OrderTask task)
        {
            if (task == null || task.Steps.Count == 0)
            {
                Debug.LogError("[OrderController] Görev boş ya da adımı yok.", this);
                return;
            }
            StopAllCoroutines();
            session = new OrderSession(task);
            pending.Clear();
            Current = State.Preparing;
            hintView?.HideHint();
            hintView?.SetGlowing(false);
            realLifeView?.Hide();
            cauldronView?.SetStoveLit(false);
            scaleView?.ClearFoamDifference();
            SetReadySave(null);
            RefreshAll();
            GameEvents.RaiseActiveOrderChanged(task);
        }

        /// <summary>Kayıttan: tepkimesi bitmiş, teslim bekleyen siparişi geri kurar (ürün envanterde).</summary>
        public void RestoreReady(OrderTask task)
        {
            if (task == null) return;
            BeginTask(task);
            session.Clear();
            Current = State.ReadyToDeliver;
            SetReadySave(task.Id);
            RefreshAll();
        }

        /// <summary>Görevi bırakır (geliştirici kısayolu: başka göreve atlama).</summary>
        public void ClearTask()
        {
            StopAllCoroutines();
            session = null;
            pending.Clear();
            Current = State.Idle;
            hintView?.HideHint();
            hintView?.SetGlowing(false);
            realLifeView?.Hide();
            cauldronView?.SetStoveLit(false);
            scaleView?.ClearFoamDifference();
            SetReadySave(null);
            RefreshAll();
            GameEvents.RaiseActiveOrderChanged(null);
        }

        /// <summary>Kapıda teslim: görev biter (TaskCompleted → kayıt). Ürünleri envanterden runner alır.</summary>
        public void Deliver()
        {
            if (session == null || Current != State.ReadyToDeliver) return;
            var task = session.Task;
            Current = State.Done;
            SetReadySave(null);
            RefreshAll();
            GameEvents.RaiseTaskCompleted(task.Id);
            // Görev kayda işlendikten sonra: pazar "bitince açılır/kalkar" satırlarını doğru görür.
            GameEvents.RaiseActiveOrderChanged(null);
            TaskFinished?.Invoke(task);
        }

        // ---------- Etkileşimler (kazan, ocak, kol) ----------

        /// <summary>Eldeki maddeden bir kavanoz kazana dökülür.</summary>
        public void Pour(PlayerInventory inventory)
        {
            if (inventory == null) return;
            if (session == null || Current == State.Done)
            {
                Notice(texts != null ? texts.NoOrder : null);
                return;
            }
            if (!CanPour) return;
            var s = inventory.SelectedSpecies;
            if (s == null || !inventory.RemoveSelected(1)) return;

            session.Pour(s);
            cauldronView?.PlayPour(s);
            RefreshCauldron();
        }

        /// <summary>Kazan boşalır; içindekiler gider (hata sayılmaz).</summary>
        public void Drain()
        {
            if (session == null || !CanPour || session.IsEmpty) return;
            session.Clear();
            cauldronView?.PlayDrain();
            Notice(texts != null ? texts.Drained : null);
            RefreshCauldron();
        }

        public void Fire(PlayerInventory inventory)
        {
            if (session == null || Current == State.Done)
            {
                Notice(texts != null ? texts.NoOrder : null);
                return;
            }
            if (!CanPour || session.IsEmpty) return;
            lastInventory = inventory;
            StartCoroutine(session.IsSatisfied ? ReactRoutine() : FoamRoutine());
        }

        /// <summary>Envanter doluyken kazanda bekleyen ürünü alır.</summary>
        public void TakePending(PlayerInventory inventory)
        {
            if (inventory == null || pending.Count == 0) return;
            for (int i = pending.Count - 1; i >= 0; i--)
                if (inventory.Add(pending[i].Species, pending[i].Amount)) pending.RemoveAt(i);
            if (pending.Count > 0) Notice(texts != null ? texts.InventoryFull : null);
            RefreshAll();
        }

        public void ToggleHint()
        {
            if (session == null || hintView == null) return;
            if (hintView.IsHintOpen)
            {
                hintView.HideHint();
                return;
            }
            if (session.FailCount < HintFailThreshold) return;
            hintView.ShowHint(session.Task.HintText);
            GameEvents.RaiseHintOpened(session.Task.Id);
        }

        private void OnHintAction(InputAction.CallbackContext _) => ToggleHint();

        // ---------- Akış ----------

        private IEnumerator FoamRoutine()
        {
            Current = State.Foam;
            StateChanged?.Invoke();
            int fails = session.RegisterFail();
            GameEvents.RaiseFailed(session.Task.Id);

            cauldronView?.SetStoveLit(true);
            cauldronView?.PlayFoam(foamDuration);
            scaleView?.ShowFoamDifference(session.Rows);
            yield return new WaitForSeconds(foamDuration);

            // Karar 13.11: köpüren kazanın malzemesi gider; sipariş için baştan dökülür.
            session.Clear();
            scaleView?.ClearFoamDifference();
            cauldronView?.SetStoveLit(false);
            Current = State.Preparing;

            // Mühür kuralı (02 §3.4): öğrenci ilk dengesizlikte formülü değiştirmeyi düşünür; not bir kez.
            string note = texts != null ? texts.FoamLost : null;
            if (!sealNoteShown && texts != null)
            {
                sealNoteShown = true;
                note += "\n" + texts.SealNote;
            }
            Notice(note);
            if (fails >= HintFailThreshold) hintView?.SetGlowing(true);
            RefreshAll();
        }

        private IEnumerator ReactRoutine()
        {
            Current = State.Reacting;
            StateChanged?.Invoke();
            cauldronView?.SetStoveLit(true);
            cauldronView?.PlayReaction(reactionDuration);
            yield return new WaitForSeconds(reactionDuration);

            var step = session.Step;
            WriteToBoard(FormulaFormatter.Equation(step.Reaction));
            cauldronView?.SetStoveLit(false);
            session.Clear();

            // Ürünler soldan ilk boş slota; yer yoksa kazanda bekler.
            var products = step.TargetProducts;
            for (int i = 0; i < products.Count; i++)
                if (lastInventory == null || !lastInventory.Add(products[i].Species, products[i].Amount))
                    pending.Add(products[i]);
            if (pending.Count > 0) Notice(texts != null ? texts.ProductWaiting : null);

            if (session.HasNextStep)
            {
                // Zincir: adım ürünü envantere düştü, sonraki adımda malzeme olur.
                session.AdvanceStep();
                Current = State.Preparing;
                if (pending.Count == 0) Notice(texts != null ? texts.NextStep : null);
                RefreshAll();
                yield break;
            }

            var task = session.Task;
            Current = State.ReadyToDeliver;
            hintView?.SetGlowing(false);
            hintView?.HideHint();
            SetReadySave(task.Id);
            if (pending.Count == 0) Notice(texts != null ? texts.ReadyToDeliver : null);
            if (!string.IsNullOrEmpty(task.RealLifeText) && realLifeView != null)
            {
                if (realLifeRoutine != null) StopCoroutine(realLifeRoutine);
                realLifeRoutine = StartCoroutine(RealLifeRoutine(task.RealLifeText));
            }
            RefreshAll();
            ReadyToDeliver?.Invoke(task);
        }

        private IEnumerator RealLifeRoutine(string text)
        {
            realLifeView?.Show(text);
            yield return new WaitForSeconds(realLifeSeconds);
            realLifeView?.Hide();
            realLifeRoutine = null;
        }

        private void WriteToBoard(string richEquation)
        {
            var data = Data;
            if (data != null) data.AddBoardEquation(richEquation);
            else if (!localBoard.Contains(richEquation)) localBoard.Add(richEquation);
            RefreshBoard();
        }

        private static void SetReadySave(string taskId)
        {
            var data = Data;
            if (data != null) data.ReadyTaskId = taskId;
        }

        private void Notice(string text)
        {
            if (!string.IsNullOrEmpty(text)) noticeView?.ShowNotice(text);
        }

        // ---------- Görünüm ----------

        private void OnViewsChanged(Type _)
        {
            CacheViews();
            RefreshAll();
        }

        private void CacheViews()
        {
            cardView = ViewLocator.Get<IOrderCardView>();
            cauldronView = ViewLocator.Get<ICauldronView>();
            scaleView = ViewLocator.Get<IScaleView>();
            boardView = ViewLocator.Get<IChalkboardView>();
            hintView = ViewLocator.Get<IHintBookView>();
            realLifeView = ViewLocator.Get<IRealLifeCardView>();
            noticeView = ViewLocator.Get<INoticeView>();
        }

        private void RefreshAll()
        {
            RefreshBoard();
            RefreshCard();
            RefreshCauldron();
            hintView?.SetGlowing(session != null && session.FailCount >= HintFailThreshold && Current == State.Preparing);
            cauldronView?.SetStoveLit(IsBusy);
        }

        private void RefreshCard()
        {
            if (cardView == null) return;
            if (session == null || Current == State.Done)
            {
                cardView.ClearOrder();
                return;
            }
            var step = session.Step;
            reactantBuffer.Clear();
            if (step.CardShowsReactants)
                for (int i = 0; i < step.Reaction.Reactants.Count; i++) reactantBuffer.Add(step.Reaction.Reactants[i].Species);

            cardView.ShowOrder(new OrderCardInfo(
                session.Task.GiverName,
                Current == State.ReadyToDeliver ? session.Task.DeliveryProducts : step.TargetProducts,
                step.CardShowsReactants ? reactantBuffer : null,
                session.StepIndex,
                session.Task.Steps.Count,
                session.Task.Reward,
                Current == State.ReadyToDeliver));
        }

        private void RefreshCauldron()
        {
            if (session != null)
            {
                cauldronView?.ShowContents(session.Contents);
                scaleView?.ShowAtoms(session.Rows, session.IsSatisfied);
            }
            else
            {
                cauldronView?.ShowContents(Array.Empty<SpeciesAmount>());
                scaleView?.ShowAtoms(Array.Empty<AtomRow>(), false);
            }
            StateChanged?.Invoke();
        }

        private void RefreshBoard()
        {
            if (boardView == null) return;
            var data = Data;
            boardView.ShowEquations(data != null ? data.BoardEquations : localBoard);
        }
    }
}
