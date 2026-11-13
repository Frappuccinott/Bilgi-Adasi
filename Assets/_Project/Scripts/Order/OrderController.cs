using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using BilgiAdasi.Chemistry;
using BilgiAdasi.City;
using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Sipariş mantığı (02-siparis-mekanigi.md). Kazandaki kavanoz sayıları → atom sayımı → denge →
    /// başarı ya da köpürme. Görünüme yalnızca arayüzle (ViewLocator) konuşur; hiçbir objeyi tanımaz.
    /// </summary>
    public sealed class OrderController : MonoBehaviour
    {
        [SerializeField] private NeighborhoodRunner runner;
        [SerializeField] private GameTexts texts;
        [Tooltip("Denge sağlanınca tepkimenin kendiliğinden başlamasına kadar geçen süre (sn).")]
        [SerializeField] private float autoReactDelay = 1.2f;

        private const string SealFlag = "seal_note";
        private const string IntroFlagPrefix = "intro_";

        private readonly Dictionary<SpeciesData, int> inCauldron = new Dictionary<SpeciesData, int>();
        private readonly List<SpeciesData> shelf = new List<SpeciesData>();
        private readonly List<ShelfItem> shelfItems = new List<ShelfItem>();
        private readonly List<AtomCountRow> rows = new List<AtomCountRow>();
        private readonly List<ElementData> elementOrder = new List<ElementData>();
        private readonly List<SpeciesAmount> amounts = new List<SpeciesAmount>();
        private readonly Dictionary<ElementData, int> left = new Dictionary<ElementData, int>();
        private readonly Dictionary<ElementData, int> right = new Dictionary<ElementData, int>();
        private readonly Dictionary<ElementData, int> diff = new Dictionary<ElementData, int>();
        private readonly List<string> discoveryLines = new List<string>();
        private readonly StringBuilder sb = new StringBuilder(256);

        private OrderTask task;
        private int stepIndex;
        private int money;
        private bool giftUsed;
        private bool balanced;
        private bool busy;
        private bool sessionOpen;
        private Coroutine autoReact;

        /// <summary>Sipariş bitti (sakin teşekkür etti). İstasyon bunu dinleyip çıkar.</summary>
        public event Action OrderFinished;

        public bool Busy => busy;
        public bool HasOrder => task != null;
        public OrderTask Task => task;

        private OrderStep Step => task.Steps[stepIndex];
        private static SaveData Save => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

        private void OnEnable() => GameInput.DeviceChanged += OnDeviceChanged;
        private void Start() => PushChalkboard();
        private void OnDisable() => GameInput.DeviceChanged -= OnDeviceChanged;

        // ───────────────────────── oturum ─────────────────────────

        public void BeginSession()
        {
            sessionOpen = true;
            var active = runner != null ? runner.ActiveTask as OrderTask : null;
            if (active != task) LoadTask(active);

            PushChalkboard();
            if (task == null)
            {
                ViewLocator.Get<IOrderCardView>()?.Show(texts.MasterName, texts.NoOrder);
                ViewLocator.Get<IOrderCardView>()?.SetCounts(Array.Empty<AtomCountRow>(), false);
                ViewLocator.Get<IScalesView>()?.Clear();
                shelfItems.Clear();
                ViewLocator.Get<IShelfView>()?.SetItems(shelfItems);
                PushHelp();
                return;
            }

            PushAll();
            Evaluate(false);
            TryShowIntro();
        }

        public void EndSession()
        {
            sessionOpen = false;
            StopAutoReact();
            ViewLocator.Get<IOrderCardView>()?.Hide();
        }

        private void LoadTask(OrderTask newTask)
        {
            StopAutoReact();
            task = newTask;
            stepIndex = 0;
            inCauldron.Clear();
            shelf.Clear();
            giftUsed = false;
            balanced = false;
            if (task == null) return;

            money = task.Budget;
            if (task.Mode == OrderMode.Purchase)
                for (int i = 0; i < task.Prices.Count; i++) AddToShelf(task.Prices[i].Species);
            else
                for (int i = 0; i < task.Shelf.Count; i++) AddToShelf(task.Shelf[i]);

            bool glow = Save != null && Save.GetFailCount(task.TaskId) >= 2;
            ViewLocator.Get<IHintBookView>()?.SetGlowing(glow);
        }

        private void AddToShelf(SpeciesData species)
        {
            if (species != null && !shelf.Contains(species)) shelf.Add(species);
        }

        private void TryShowIntro()
        {
            if (runner == null || runner.Data == null || Save == null) return;
            if (!runner.IsFirstTask(task)) return;
            string flag = IntroFlagPrefix + runner.Data.Id;
            if (Save.HasFlag(flag) || string.IsNullOrWhiteSpace(runner.Data.MasterIntro)) return;
            Save.SetFlag(flag);
            Dialogue.Begin();
            Dialogue.Say(texts.MasterName, runner.Data.MasterIntro, Dialogue.End);
        }

        // ───────────────────────── oyuncu eylemleri ─────────────────────────

        public void AddJar(SpeciesData species)
        {
            if (busy || task == null || species == null || !shelf.Contains(species)) return;

            if (task.Mode == OrderMode.Purchase)
            {
                int price = task.PriceOf(species);
                if (money < price)
                {
                    TryGiftMissing();
                    return;
                }
                money -= price;
            }

            inCauldron.TryGetValue(species, out int count);
            inCauldron[species] = count + 1;

            ViewLocator.Get<IShelfView>()?.Pour(species);
            ViewLocator.Get<ICauldronView>()?.PlayStir();
            if (Save != null && !Save.HasFlag(SealFlag))
            {
                Save.SetFlag(SealFlag);
                Dialogue.Toast(texts.SealNote, 5f);
            }
            Evaluate(true);
        }

        public void RemoveJar(SpeciesData species)
        {
            if (busy || task == null || species == null) return;
            if (task.Mode == OrderMode.Purchase)
            {
                Dialogue.Toast(texts.NoRefund, 3f);
                return;
            }
            if (!inCauldron.TryGetValue(species, out int count) || count <= 0) return;
            if (count == 1) inCauldron.Remove(species);
            else inCauldron[species] = count - 1;
            Evaluate(true);
        }

        /// <summary>Mühürlü etikete tıklama (02 §3.4): etiket değişmez, kavanoz titrer.</summary>
        public void TouchSeal(SpeciesData species)
        {
            ViewLocator.Get<IShelfView>()?.Shake(species);
        }

        public void Fire()
        {
            if (busy || task == null) return;
            if (balanced) StartReaction();
            else Foam();
        }

        public void OpenHint()
        {
            var book = ViewLocator.Get<IHintBookView>();
            if (book == null || book.IsOpen) return;

            string hint = texts.NoHintYet;
            if (task != null && Save != null && (Save.GetFailCount(task.TaskId) >= 2 || Save.IsHintOpened(task.TaskId)))
            {
                hint = task.Hint;
                GameEvents.RaiseHintOpened(task.TaskId);
            }
            BuildDiscoveryLines();
            Dialogue.Begin();
            book.Open(texts.HintTitle, hint, texts.DiscoveryTitle, discoveryLines, Dialogue.End);
        }

        // ───────────────────────── hesap ─────────────────────────

        private void Evaluate(bool changedByPlayer)
        {
            if (task == null) return;
            Recount();
            PushAll();

            var cauldron = ViewLocator.Get<ICauldronView>();
            if (balanced)
            {
                cauldron?.SetFire(true);
                cauldron?.SetBoiling(true);
                if (changedByPlayer) Dialogue.Toast(texts.BalancedNote, 2f);
                StopAutoReact();
                if (sessionOpen) autoReact = StartCoroutine(AutoReact());
            }
            else
            {
                StopAutoReact();
                cauldron?.SetFire(false);
                cauldron?.SetBoiling(false);
            }
        }

        private void Recount()
        {
            amounts.Clear();
            foreach (var pair in inCauldron) amounts.Add(new SpeciesAmount(pair.Key, pair.Value));
            AtomMath.Count(amounts, left);

            right.Clear();
            AtomMath.AddTo(Step.Reaction.Products, Step.Multiplier, right);

            balanced = task.Mode == OrderMode.Purchase ? PurchaseSatisfied() : AtomMath.Equal(left, right);

            elementOrder.Clear();
            CollectElements(Step.Reaction.Products);
            CollectElements(Step.Reaction.Reactants);
            foreach (var pair in left) if (!elementOrder.Contains(pair.Key)) elementOrder.Add(pair.Key);

            rows.Clear();
            for (int i = 0; i < elementOrder.Count; i++)
            {
                var e = elementOrder[i];
                left.TryGetValue(e, out int l);
                right.TryGetValue(e, out int r);
                rows.Add(new AtomCountRow(e, l, r));
            }
        }

        private void CollectElements(IReadOnlyList<SpeciesAmount> terms)
        {
            for (int i = 0; i < terms.Count; i++)
            {
                var comp = terms[i].Species.Composition;
                for (int c = 0; c < comp.Count; c++)
                    if (!elementOrder.Contains(comp[c].Element)) elementOrder.Add(comp[c].Element);
            }
        }

        private bool PurchaseSatisfied()
        {
            var reactants = Step.Reaction.Reactants;
            for (int i = 0; i < reactants.Count; i++)
            {
                inCauldron.TryGetValue(reactants[i].Species, out int have);
                if (have < reactants[i].Amount * Step.Multiplier) return false;
            }
            return true;
        }

        /// <summary>Purchase: para bitti ve eksik var → sakin bir kez eksiği verir (oyun kilitlenmez).</summary>
        private void TryGiftMissing()
        {
            if (giftUsed)
            {
                Dialogue.Toast(texts.NotEnoughMoney, 2.5f);
                return;
            }
            giftUsed = true;
            var reactants = Step.Reaction.Reactants;
            for (int i = 0; i < reactants.Count; i++)
            {
                int need = reactants[i].Amount * Step.Multiplier;
                inCauldron.TryGetValue(reactants[i].Species, out int have);
                if (have < need) inCauldron[reactants[i].Species] = need;
            }
            Dialogue.Begin();
            Dialogue.Say(task.ResidentName, texts.OutOfMoney, () =>
            {
                Dialogue.End();
                Evaluate(true);
            });
        }

        // ───────────────────────── köpürme ─────────────────────────

        private void Foam()
        {
            GameEvents.RaiseFailed(task.TaskId);
            int fails = Save != null ? Save.GetFailCount(task.TaskId) : 0;

            ViewLocator.Get<ICauldronView>()?.PlayFoam();
            AtomMath.Difference(left, right, diff);
            ViewLocator.Get<IScalesView>()?.FlashDifference(diff);

            if (fails >= 2)
            {
                ViewLocator.Get<IHintBookView>()?.SetGlowing(true);
                Dialogue.Toast(texts.FoamNote + "\n" + texts.HintGlowNote, 4f);
            }
            else
            {
                Dialogue.Toast(texts.FoamNote, 3f);
            }
        }

        // ───────────────────────── başarı ─────────────────────────

        private IEnumerator AutoReact()
        {
            yield return new WaitForSeconds(autoReactDelay);
            autoReact = null;
            if (balanced && !busy && !Dialogue.IsOpen) StartReaction();
        }

        private void StopAutoReact()
        {
            if (autoReact == null) return;
            StopCoroutine(autoReact);
            autoReact = null;
        }

        private void StartReaction()
        {
            StopAutoReact();
            StartCoroutine(Reaction());
        }

        private IEnumerator Reaction()
        {
            busy = true;
            var cauldron = ViewLocator.Get<ICauldronView>();
            cauldron?.SetFire(true);
            cauldron?.PlaySuccess();
            yield return new WaitForSeconds(cauldron != null ? cauldron.SuccessDuration : 1.5f);

            Save?.AddBoardEquation(FormulaFormatter.Equation(Step.Reaction));
            PushChalkboard();
            string leftover = task.Mode == OrderMode.Purchase ? LeftoverText() : null;

            if (stepIndex < task.Steps.Count - 1)
            {
                var drops = Step.DropToShelf;
                sb.Clear();
                for (int i = 0; i < drops.Count; i++)
                {
                    AddToShelf(drops[i]);
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(drops[i].FormulaRich);
                }
                stepIndex++;
                inCauldron.Clear();
                cauldron?.SetBoiling(false);
                busy = false;
                Evaluate(false);
                if (sb.Length > 0) Dialogue.Toast(string.Format(texts.StepDone, sb.ToString()), 4f);
                yield break;
            }

            cauldron?.SetBoiling(false);
            cauldron?.SetFire(false);
            RunCompletion(leftover);
        }

        private string LeftoverText()
        {
            sb.Clear();
            var reactants = Step.Reaction.Reactants;
            for (int i = 0; i < reactants.Count; i++)
            {
                int need = reactants[i].Amount * Step.Multiplier;
                inCauldron.TryGetValue(reactants[i].Species, out int have);
                if (have <= need) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(have - need).Append(' ').Append(reactants[i].Species.FormulaRich);
            }
            return sb.Length > 0 ? string.Format(texts.Leftover, sb.ToString()) : null;
        }

        /// <summary>Artan → Sorgu → Keşif Defteri → Gerçek hayatta → sakinin teşekkürü → köy parçası.</summary>
        private void RunCompletion(string leftover)
        {
            var done = task;
            Dialogue.Begin();
            SayIf(texts.MasterName, leftover, () =>
            AskReasoning(done, () =>
            WriteDiscovery(done, () =>
            ShowRealLife(done, () =>
            SayIf(done.ResidentName, done.DeliverLine, () =>
            {
                Dialogue.End();
                runner?.Complete(done);
                LoadTask(null);
                ViewLocator.Get<IScalesView>()?.Clear();
                ViewLocator.Get<IHintBookView>()?.SetGlowing(false);
                busy = false;
                OrderFinished?.Invoke();
            })))));
        }

        private static void SayIf(string speaker, string text, Action next)
        {
            if (string.IsNullOrWhiteSpace(text)) next();
            else Dialogue.Say(speaker, text, next);
        }

        private void AskReasoning(TaskData done, Action next)
        {
            var r = done.Reasoning;
            if (r == null || r.IsEmpty)
            {
                next();
                return;
            }
            Dialogue.Ask(texts.MasterName, r.Question, r.Options, index => SayIf(texts.MasterName, r.ReplyFor(index), next));
        }

        private void WriteDiscovery(TaskData done, Action next)
        {
            if (string.IsNullOrWhiteSpace(done.DiscoveryText))
            {
                next();
                return;
            }
            GameEvents.RaiseDiscoveryAdded(done.TaskId);
            Dialogue.Say(texts.DiscoveryTitle, string.Format(texts.DiscoveryWritten, done.DiscoveryId, done.DiscoveryText), next);
        }

        private void ShowRealLife(TaskData done, Action next)
        {
            if (string.IsNullOrWhiteSpace(done.RealLife))
            {
                next();
                return;
            }
            var card = ViewLocator.Get<IRealLifeCardView>();
            if (card == null)
            {
                Dialogue.Say(texts.RealLifeTitle, done.RealLife, next);
                return;
            }
            ViewLocator.Get<IDialogueView>()?.Hide();
            card.Show(texts.RealLifeTitle, done.RealLife, next);
        }

        private void BuildDiscoveryLines()
        {
            discoveryLines.Clear();
            if (Save == null || runner == null || runner.Data == null) return;
            var tasks = runner.Data.Tasks;
            for (int i = 0; i < tasks.Count; i++)
            {
                var t = tasks[i];
                if (t == null || !Save.HasDiscovery(t.TaskId)) continue;
                discoveryLines.Add(t.DiscoveryId + "  " + t.DiscoveryText);
            }
        }

        // ───────────────────────── görünüm ─────────────────────────

        private void PushAll()
        {
            var card = ViewLocator.Get<IOrderCardView>();
            if (card != null)
            {
                card.Show(task.Title + " · " + task.ResidentName, BuildCardBody());
                card.SetCounts(rows, balanced);
            }
            PushHelp();

            var scales = ViewLocator.Get<IScalesView>();
            if (scales != null)
            {
                scales.SetTitles(texts.LeftScaleTitle, texts.RightScaleTitle);
                scales.ShowAtoms(rows, balanced);
            }

            shelfItems.Clear();
            for (int i = 0; i < shelf.Count; i++)
            {
                var s = shelf[i];
                inCauldron.TryGetValue(s, out int count);
                string label = task.Mode == OrderMode.Purchase ? string.Format(texts.PriceLabel, task.PriceOf(s)) : string.Empty;
                shelfItems.Add(new ShelfItem(s, count, label));
            }
            ViewLocator.Get<IShelfView>()?.SetItems(shelfItems);
        }

        private string BuildCardBody()
        {
            sb.Clear();
            sb.Append(texts.CardWanted).Append(": ");
            AppendTerms(Step.Reaction.Products, Step.Multiplier, true);
            if (task.ShowReactantsOnCard)
            {
                sb.Append('\n').Append(texts.CardReactants).Append(": ");
                AppendTerms(Step.Reaction.Reactants, 1, false);
            }
            if (task.Steps.Count > 1)
                sb.Append('\n').Append(string.Format(texts.CardStep, stepIndex + 1, task.Steps.Count));
            if (task.Mode == OrderMode.Purchase)
                sb.Append('\n').Append(string.Format(texts.CardPurse, money));
            return sb.ToString();
        }

        private void AppendTerms(IReadOnlyList<SpeciesAmount> terms, int multiplier, bool withAmounts)
        {
            for (int i = 0; i < terms.Count; i++)
            {
                if (i > 0) sb.Append(" + ");
                int n = terms[i].Amount * multiplier;
                if (withAmounts && n > 1) sb.Append(n).Append(' ');
                sb.Append(terms[i].Species.FormulaRich);
            }
        }

        private void PushHelp()
        {
            ViewLocator.Get<IOrderCardView>()?.SetHelp(
                GameInput.LastDevice == InputDeviceKind.Gamepad ? texts.ControlsGamepad : texts.ControlsKeyboard);
        }

        private void PushChalkboard()
        {
            if (Save != null) ViewLocator.Get<IChalkboardView>()?.SetEquations(Save.BoardEquations);
        }

        private void OnDeviceChanged(InputDeviceKind _)
        {
            if (sessionOpen) PushHelp();
        }
    }
}
