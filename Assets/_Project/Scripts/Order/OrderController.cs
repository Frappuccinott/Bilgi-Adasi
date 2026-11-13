using System;
using System.Collections;
using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using BilgiAdasi.Player;
using BilgiAdasi.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Sipariş akışı (04 §5): Preparing ⇄ Foam → Reacting → (sonraki adım | RealLife → Done).
    /// Kazan istasyonunun üzerinde durur. Görünümleri yalnızca arayüzlerle (ViewLocator) tanır;
    /// görünüm tıklamaları IOrderCommands ile gelir. Görev verisi BeginTask ile verilir (Faz 4: NeighborhoodRunner).
    /// </summary>
    [RequireComponent(typeof(CauldronStation))]
    public sealed class OrderController : MonoBehaviour, IOrderCommands
    {
        public enum State
        {
            Idle,
            Preparing,
            Foam,
            Reacting,
            RealLife,
            Done
        }

        private const int HintFailThreshold = 2;
        private const float RealLifeTimeoutWithoutView = 3f;

        [SerializeField] private OrderTexts texts;
        [Tooltip("Görev verilmeden istasyona girilirse bu görev başlar (test için). Faz 4'te NeighborhoodRunner verir.")]
        [SerializeField] private OrderTask testTask;
        [SerializeField] private float foamDuration = 1.2f;
        [SerializeField] private float reactionDuration = 1.5f;

        private CauldronStation station;
        private OrderSession session;
        private SpeciesData selected;
        private bool sealNoteShown;
        private bool realLifeClosed;
        private bool stationOpen;

        private readonly List<ShelfItem> shelfBuffer = new List<ShelfItem>();
        private readonly List<SpeciesData> reactantBuffer = new List<SpeciesData>();
        private readonly List<string> localBoard = new List<string>();

        private IOrderStationView stationView;
        private IOrderCardView cardView;
        private IShelfView shelfView;
        private ICauldronView cauldronView;
        private IScaleView scaleView;
        private IChalkboardView boardView;
        private IHintBookView hintView;
        private IRealLifeCardView realLifeView;

        public State Current { get; private set; } = State.Idle;
        public OrderSession Session => session;
        public bool IsBusy => Current == State.Foam || Current == State.Reacting || Current == State.RealLife;

        /// <summary>Görev bitti (teslim edildi). NeighborhoodRunner dinler.</summary>
        public event Action<OrderTask> TaskFinished;

        private void Awake()
        {
            station = GetComponent<CauldronStation>();
        }

        private void OnEnable()
        {
            StationBase.Entered += OnStationEntered;
            StationBase.Exited += OnStationExited;
            ViewLocator.Changed += OnViewsChanged;
            CacheViews();
        }

        private void OnDisable()
        {
            StationBase.Entered -= OnStationEntered;
            StationBase.Exited -= OnStationExited;
            ViewLocator.Changed -= OnViewsChanged;
            if (stationOpen) CloseStation();
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
            selected = null;
            Current = State.Preparing;
            hintView?.HideHint();
            hintView?.SetGlowing(false);
            realLifeView?.Hide();
            cauldronView?.SetStoveLit(false);
            RefreshAll();
        }

        // ---------- İstasyon ----------

        private void OnStationEntered(StationBase s)
        {
            if (s != station) return;
            if (session == null && testTask != null) BeginTask(testTask);

            stationOpen = true;
            ViewLocator.Register<IOrderCommands>(this);
            var a = GameInput.Controls.Station;
            a.Increase.performed += OnIncreaseAction;
            a.Decrease.performed += OnDecreaseAction;
            a.FireStove.performed += OnFireAction;
            a.OpenHint.performed += OnHintAction;

            stationView?.SetOpen(true);
            RefreshAll();
        }

        private void OnStationExited(StationBase s)
        {
            if (s == station && stationOpen) CloseStation();
        }

        private void CloseStation()
        {
            stationOpen = false;
            ViewLocator.Unregister<IOrderCommands>(this);
            var a = GameInput.Controls.Station;
            a.Increase.performed -= OnIncreaseAction;
            a.Decrease.performed -= OnDecreaseAction;
            a.FireStove.performed -= OnFireAction;
            a.OpenHint.performed -= OnHintAction;
            hintView?.HideHint();
            stationView?.SetOpen(false);
        }

        private void OnIncreaseAction(InputAction.CallbackContext _) => Increase(CurrentSelection());
        private void OnDecreaseAction(InputAction.CallbackContext _) => Decrease(CurrentSelection());
        private void OnFireAction(InputAction.CallbackContext _) => FireStove();
        private void OnHintAction(InputAction.CallbackContext _) => ToggleHint();

        private SpeciesData CurrentSelection()
        {
            if (selected != null) return selected;
            if (session == null) return null;
            if (session.Contents.Count > 0) return session.Contents[0].Species;
            return session.ShelfItems.Count > 0 ? session.ShelfItems[0] : null;
        }

        // ---------- IOrderCommands ----------

        public void AddToCauldron(SpeciesData species)
        {
            if (!CanEdit() || species == null) return;
            selected = species;
            var result = session.Add(species);
            if (result == AddResult.NotEnoughMoney && texts != null) cardView?.ShowMessage(texts.NotEnoughMoney);
            AfterCauldronChanged();
        }

        public void Increase(SpeciesData species) => AddToCauldron(species);

        public void Decrease(SpeciesData species)
        {
            if (!CanEdit() || species == null) return;
            selected = species;
            session.Remove(species);
            AfterCauldronChanged();
        }

        public void Select(SpeciesData species)
        {
            selected = species;
            RefreshCauldron();
        }

        public void TouchLabel(SpeciesData species)
        {
            if (species == null) return;
            string note = !sealNoteShown && texts != null ? texts.SealNote : null;
            sealNoteShown = true;
            shelfView?.PlaySealShake(species, note);
        }

        public void FireStove()
        {
            if (!CanEdit()) return;

            if (session.IsSatisfied)
            {
                StartCoroutine(ReactRoutine());
                return;
            }

            if (session.TryForgiveShortage())
            {
                if (texts != null) cardView?.ShowMessage(texts.ShortageForgiven);
                RefreshAll();
                StartCoroutine(ReactRoutine());
                return;
            }

            StartCoroutine(FoamRoutine());
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

        public void CloseRealLifeCard() => realLifeClosed = true;

        private bool CanEdit() => session != null && Current == State.Preparing;

        // ---------- Akış ----------

        private void AfterCauldronChanged()
        {
            RefreshCauldron();
            // Denge sağlanınca ocak kendiliğinden yanar (02 §2).
            if (session.IsSatisfied && Current == State.Preparing) StartCoroutine(ReactRoutine());
        }

        private IEnumerator FoamRoutine()
        {
            Current = State.Foam;
            int fails = session.RegisterFail();
            GameEvents.RaiseFailed(session.Task.Id);

            cauldronView?.PlayFoam(foamDuration);
            scaleView?.ShowFoamDifference(session.Rows);

            // Mühür kuralı (02 §3.4): öğrenci ilk dengesizlikte formülü değiştirmeyi düşünür.
            // Not ilk köpürmede kendiliğinden bir kez gösterilir; etikete tıklayan da aynı notu alır.
            if (!sealNoteShown && texts != null)
            {
                sealNoteShown = true;
                shelfView?.PlaySealShake(CurrentSelection(), texts.SealNote);
            }
            yield return new WaitForSeconds(foamDuration);
            scaleView?.ClearFoamDifference();

            // Kazan boşalmaz; oyuncunun seçimi korunur (02 §3.5).
            Current = State.Preparing;
            if (fails >= HintFailThreshold) hintView?.SetGlowing(true);
        }

        private IEnumerator ReactRoutine()
        {
            Current = State.Reacting;
            cauldronView?.SetStoveLit(true);
            cauldronView?.PlayReaction(reactionDuration);
            yield return new WaitForSeconds(reactionDuration);

            WriteToBoard(FormulaFormatter.Equation(session.Step.Reaction));
            cauldronView?.SetStoveLit(false);

            if (session.HasNextStep)
            {
                // Zincir: adım ürünü rafa düşer, sonraki adım başlar.
                session.AdvanceStep();
                selected = null;
                Current = State.Preparing;
                RefreshAll();
                yield break;
            }

            var task = session.Task;
            if (!string.IsNullOrEmpty(task.RealLifeText))
            {
                Current = State.RealLife;
                realLifeClosed = false;
                realLifeView?.Show(task.RealLifeText);
                float waitUntil = Time.time + RealLifeTimeoutWithoutView;
                while (!realLifeClosed && (realLifeView != null || Time.time < waitUntil)) yield return null;
                realLifeView?.Hide();
            }

            Current = State.Done;
            hintView?.SetGlowing(false);
            hintView?.HideHint();
            if (task.DeliveryLines.Length > 0) cardView?.ShowMessage(task.DeliveryLines[0]);

            GameEvents.RaiseTaskCompleted(task.Id);
            TaskFinished?.Invoke(task);
        }

        private void WriteToBoard(string richEquation)
        {
            var data = SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;
            if (data != null) data.AddBoardEquation(richEquation);
            else if (!localBoard.Contains(richEquation)) localBoard.Add(richEquation);
            RefreshBoard();
        }

        // ---------- Görünüm ----------

        private void OnViewsChanged(Type _)
        {
            CacheViews();
            RefreshAll();
        }

        private void CacheViews()
        {
            stationView = ViewLocator.Get<IOrderStationView>();
            cardView = ViewLocator.Get<IOrderCardView>();
            shelfView = ViewLocator.Get<IShelfView>();
            cauldronView = ViewLocator.Get<ICauldronView>();
            scaleView = ViewLocator.Get<IScaleView>();
            boardView = ViewLocator.Get<IChalkboardView>();
            hintView = ViewLocator.Get<IHintBookView>();
            realLifeView = ViewLocator.Get<IRealLifeCardView>();
            stationView?.SetOpen(stationOpen);
        }

        private void RefreshAll()
        {
            RefreshBoard();
            if (session == null) return;
            RefreshCard();
            RefreshCauldron();
            hintView?.SetGlowing(session.FailCount >= HintFailThreshold && Current != State.Done);
            cauldronView?.SetStoveLit(Current == State.Reacting);
        }

        private void RefreshCard()
        {
            if (cardView == null || session == null) return;
            var step = session.Step;
            reactantBuffer.Clear();
            if (step.CardShowsReactants)
                for (int i = 0; i < step.Reaction.Reactants.Count; i++) reactantBuffer.Add(step.Reaction.Reactants[i].Species);

            cardView.ShowOrder(new OrderCardInfo(
                session.Task.GiverName,
                step.TargetProducts,
                step.CardShowsReactants ? reactantBuffer : null,
                session.StepIndex,
                session.Task.Steps.Count,
                session.IsPurchase,
                session.Money,
                session.Task.Budget,
                texts != null ? texts.CurrencyName : ""));
        }

        private void RefreshCauldron()
        {
            if (session == null) return;

            if (shelfView != null)
            {
                shelfBuffer.Clear();
                var items = session.ShelfItems;
                for (int i = 0; i < items.Count; i++)
                {
                    var s = items[i];
                    shelfBuffer.Add(new ShelfItem(s, session.CountInCauldron(s),
                        session.IsPurchase ? session.OwnedCount(s) : -1, session.PriceOf(s)));
                }
                shelfView.ShowShelf(shelfBuffer, selected);
            }

            cauldronView?.ShowContents(session.Contents, selected);
            scaleView?.ShowAtoms(session.Rows, session.IsSatisfied);
            if (session.IsPurchase) RefreshCard(); // para değişti
        }

        private void RefreshBoard()
        {
            if (boardView == null) return;
            var data = SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;
            boardView.ShowEquations(data != null ? data.BoardEquations : localBoard);
        }
    }
}
