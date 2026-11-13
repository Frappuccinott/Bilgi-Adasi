using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using BilgiAdasi.Player;
using BilgiAdasi.UI;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Köy akışı (01 §5, 04 §5): her köyün görevleri sırayla açılır. Köyler birbirinden bağımsızdır (sıra serbest);
    /// sipariş görevleri kapıda alınır ve kazanda çözülür, masa görevleri (Faz 5) masada.
    /// Kapı: (ilk görevde ustanın tanıtımı) + sakinin derdi → görev kazana verilir → konuşma bitince kapıdan çıkılır.
    /// Teslim elle: ürün hazır olunca oyuncu kapıya döner; sakin teşekkür eder, görev biter. Sıradaki sakin
    /// ancak oyuncu kapıyı yeniden kullanınca gelir (teslimle aynı konuşmada değil).
    /// Köyün bütün görevleri bitince NeighborhoodCompleted.
    /// İlerleme kayıttaki completedTasks'tan hesaplanır (ayrıca saklanmaz).
    /// </summary>
    [RequireComponent(typeof(DialogueDirector))]
    public sealed class NeighborhoodRunner : MonoBehaviour
    {
        [Tooltip("Oynanabilir köyler. Kapıya birden fazla köyün siparişi hazırsa listedeki sıra önce gelir (demo: Denkleştirme).")]
        [SerializeField] private List<NeighborhoodData> neighborhoods = new List<NeighborhoodData>();
        [SerializeField] private NeighborhoodTexts texts;
        [Tooltip("Sahnedeki kazan istasyonunun OrderController'ı.")]
        [SerializeField] private OrderController orderController;
        [Tooltip("Sahnedeki atölye kapısı.")]
        [SerializeField] private DoorStation door;

        private DialogueDirector dialogue;
        private readonly List<DialogueLine> lineBuffer = new List<DialogueLine>();
        // Ustanın tanıtımı köy başına bir kez (oturum içinde); kayıtta ilk görev bitmişse zaten söylenmez.
        private readonly HashSet<string> introShown = new HashSet<string>();
        // Bu oturumda kayıt yoksa (test) biten görevler burada tutulur.
        private readonly HashSet<string> localCompleted = new HashSet<string>();

        private OrderTask activeOrder;
        private NeighborhoodData activeOrderNeighborhood;
        private OrderTask orderToBegin;

        public IReadOnlyList<NeighborhoodData> Neighborhoods => neighborhoods;

        private static SaveData Data => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

        private void Awake()
        {
            dialogue = GetComponent<DialogueDirector>();
        }

        private void OnEnable()
        {
            if (door != null) door.Used += OnDoorUsed;
            if (orderController != null) orderController.TaskFinished += OnOrderFinished;
        }

        private void OnDisable()
        {
            if (door != null) door.Used -= OnDoorUsed;
            if (orderController != null) orderController.TaskFinished -= OnOrderFinished;
        }

        private void Start()
        {
            for (int i = 0; i < neighborhoods.Count; i++)
                if (neighborhoods[i] != null) PublishProgress(neighborhoods[i]);
            RestoreReadyOrder();
        }

        /// <summary>Kayıtta teslim bekleyen sipariş varsa geri kurar (ürün envanterde duruyor).</summary>
        private void RestoreReadyOrder()
        {
            var data = Data;
            if (data == null || string.IsNullOrEmpty(data.ReadyTaskId) || orderController == null) return;
            for (int i = 0; i < neighborhoods.Count; i++)
            {
                var n = neighborhoods[i];
                if (n == null) continue;
                for (int t = 0; t < n.Tasks.Count; t++)
                {
                    if (n.Tasks[t] is not OrderTask order || order.Id != data.ReadyTaskId || IsTaskDone(order)) continue;
                    activeOrder = order;
                    activeOrderNeighborhood = n;
                    introShown.Add(n.Id);
                    orderController.RestoreReady(order);
                    return;
                }
            }
            data.ReadyTaskId = null;
        }

        // ---------- İlerleme ----------

        public bool IsTaskDone(TaskData task)
        {
            if (task == null) return false;
            var data = Data;
            return data != null ? data.IsTaskCompleted(task.Id) : localCompleted.Contains(task.Id);
        }

        /// <summary>Köyde sırayla bitmiş görev sayısı = sıradaki görevin indeksi.</summary>
        public int CompletedCount(NeighborhoodData n)
        {
            int i = 0;
            while (i < n.Tasks.Count && IsTaskDone(n.Tasks[i])) i++;
            return i;
        }

        private void PublishProgress(NeighborhoodData n)
        {
            GameEvents.RaiseNeighborhoodProgress(n.Id, CompletedCount(n), n.Tasks.Count);
        }

        /// <summary>Sıradaki görevi T tipinde olan ilk köy (kilitli köyler atlanır).</summary>
        private bool TryGetNext<T>(out NeighborhoodData neighborhood, out T task) where T : TaskData
        {
            for (int i = 0; i < neighborhoods.Count; i++)
            {
                var n = neighborhoods[i];
                if (n == null || n.LockedByDefault) continue;
                int next = CompletedCount(n);
                if (next < n.Tasks.Count && n.Tasks[next] is T typed)
                {
                    neighborhood = n;
                    task = typed;
                    return true;
                }
            }
            neighborhood = null;
            task = null;
            return false;
        }

        private NeighborhoodData FindOwner(TaskData task)
        {
            for (int i = 0; i < neighborhoods.Count; i++)
                if (neighborhoods[i] != null && neighborhoods[i].IndexOf(task) >= 0) return neighborhoods[i];
            return null;
        }

        // ---------- Kapı ----------

        private void OnDoorUsed(PlayerInteractor interactor)
        {
            lineBuffer.Clear();
            var inv = interactor != null ? interactor.Inventory : null;

            if (activeOrder != null && orderController != null && orderController.IsReadyToDeliver && inv != null)
            {
                var order = activeOrder;
                string refusal = CheckDelivery(order, inv);
                if (refusal != null)
                {
                    AddLine(order.GiverName, refusal, order.GiverPortrait);
                }
                else
                {
                    // Elle teslim: ürünler envanterden çıkar, kese dolar, sonra görev biter (TaskCompleted → kayıt).
                    var products = order.DeliveryProducts;
                    for (int i = 0; i < products.Count; i++) inv.Remove(products[i].Species, products[i].Amount);
                    inv.AddMoney(order.Reward);
                    for (int i = 0; i < order.DeliveryLines.Length; i++)
                        AddLine(order.GiverName, order.DeliveryLines[i], order.GiverPortrait);
                    if (texts != null && order.Reward > 0)
                        AddLine(order.GiverName, string.Format(texts.Paid, order.Reward), order.GiverPortrait);
                    // → OnOrderFinished: ilerleme + köy tamamlanması. Sıradaki sakin hemen gelmez (oyuncu dinlenebilir,
                    // pazara/rafa gidebilir); yeni sipariş, oyuncu kapıyı yeniden kullanınca alınır.
                    orderController.Deliver();
                }
            }
            else if (activeOrder != null)
            {
                // Sipariş alındı ama hazır değil.
                AddLine(activeOrder.GiverName, texts != null ? texts.OrderWaiting : null, activeOrder.GiverPortrait);
            }
            else if (!OfferNextOrder())
            {
                AddMasterLine(texts != null ? texts.NobodyAtDoor : null);
            }

            dialogue.Play(lineBuffer, BeginPendingOrder, interactor);
        }

        /// <summary>Teslim olur mu? Olmuyorsa sakinin repliği: elde yok / istemediği ürün / eksik.</summary>
        private string CheckDelivery(OrderTask order, PlayerInventory inv)
        {
            var held = inv.SelectedSpecies;
            var products = order.DeliveryProducts;
            if (held == null) return texts != null ? texts.HandEmpty : string.Empty;

            bool wanted = false;
            for (int i = 0; i < products.Count; i++)
                if (products[i].Species == held) wanted = true;
            if (!wanted) return texts != null ? texts.WrongItem : string.Empty;

            for (int i = 0; i < products.Count; i++)
                if (inv.CountOf(products[i].Species) < products[i].Amount) return texts != null ? texts.NotEnough : string.Empty;
            return null;
        }

        /// <summary>Sıradaki sipariş varsa (ilk görevde ustanın tanıtımıyla) satırlarını ekler ve görevi kazana verir.</summary>
        private bool OfferNextOrder()
        {
            if (!TryGetNext<OrderTask>(out var n, out var task)) return false;

            if (!introShown.Contains(n.Id) && CompletedCount(n) == 0)
                for (int i = 0; i < n.IntroLines.Length; i++) AddMasterLine(n.IntroLines[i]);
            introShown.Add(n.Id);

            for (int i = 0; i < task.GiverLines.Length; i++) AddLine(task.GiverName, task.GiverLines[i], task.GiverPortrait);
            StartOrder(n, task);
            return true;
        }

        /// <summary>
        /// Sipariş alınır. Kapıda: kart ve kazan konuşma bitince açılır (Usta/sakin konuşurken sol üstte kart çıkmasın);
        /// konuşma ancak bitince kapanabildiği için sipariş kaybolmaz. Geliştirici atlamasında hemen.
        /// </summary>
        private void StartOrder(NeighborhoodData n, OrderTask task, bool beginNow = false)
        {
            activeOrder = task;
            activeOrderNeighborhood = n;
            orderToBegin = beginNow ? null : task;
            if (beginNow && orderController != null) orderController.BeginTask(task);
        }

        /// <summary>Kapı konuşması bitti: bekleyen sipariş başlar (kart görünür).</summary>
        private void BeginPendingOrder()
        {
            var task = orderToBegin;
            orderToBegin = null;
            if (task != null && task == activeOrder && orderController != null) orderController.BeginTask(task);
        }

        // ---------- Teslim ----------

        private void OnOrderFinished(OrderTask task)
        {
            var n = activeOrderNeighborhood != null && activeOrderNeighborhood.IndexOf(task) >= 0
                ? activeOrderNeighborhood
                : FindOwner(task);
            activeOrder = null;
            activeOrderNeighborhood = null;

            // GameManager TaskCompleted'ı kayda yazdı; kayıt yoksa (test) burada tutulur.
            if (Data == null) localCompleted.Add(task.Id);
            if (n != null) AfterProgress(n);
        }

        private void AfterProgress(NeighborhoodData n)
        {
            PublishProgress(n);
            if (CompletedCount(n) < n.Tasks.Count) return;
            var data = Data;
            if (data != null && data.IsNeighborhoodCompleted(n.Id)) return;
            GameEvents.RaiseNeighborhoodCompleted(n.Id);
        }

        // ---------- Geliştirici kısayolları ----------

        /// <summary>
        /// Odaktaki köy: alınmış siparişin köyü, yoksa bitmemiş ilk köy, yoksa ilk köy.
        /// </summary>
        public NeighborhoodData FocusedNeighborhood()
        {
            if (activeOrderNeighborhood != null) return activeOrderNeighborhood;
            for (int i = 0; i < neighborhoods.Count; i++)
                if (neighborhoods[i] != null && CompletedCount(neighborhoods[i]) < neighborhoods[i].Tasks.Count) return neighborhoods[i];
            return neighborhoods.Count > 0 ? neighborhoods[0] : null;
        }

        /// <summary>
        /// Odaktaki köyde index'inci göreve atlar: öncekiler bitmiş, sonrakiler bitmemiş sayılır.
        /// index == görev sayısı → köy tamamlanır. Sipariş göreviyse kapıyı beklemeden kazana verilir.
        /// </summary>
        public void DevJumpTo(int index)
        {
            var n = FocusedNeighborhood();
            if (n == null || n.Tasks.Count == 0) return;
            index = Mathf.Clamp(index, 0, n.Tasks.Count);

            var data = Data;
            for (int i = 0; i < n.Tasks.Count; i++)
            {
                string id = n.Tasks[i].Id;
                bool done = i < index;
                if (data != null)
                {
                    if (done) data.MarkTaskCompleted(id);
                    else data.UnmarkTaskCompleted(id);
                }
                else if (done) localCompleted.Add(id);
                else localCompleted.Remove(id);
            }
            if (data != null && index < n.Tasks.Count) data.UnmarkNeighborhoodCompleted(n.Id);
            introShown.Add(n.Id);

            dialogue.Finish();
            activeOrder = null;
            activeOrderNeighborhood = null;
            if (orderController != null) orderController.ClearTask();

            if (index < n.Tasks.Count && n.Tasks[index] is OrderTask order) StartOrder(n, order, true);

            AfterProgress(n);
            SaveSystem.Instance?.Save();
            Debug.Log($"[Dev] {n.Id}: görev {index + 1}/{n.Tasks.Count}");
        }

        /// <summary>Odaktaki köyün sıradaki görevini bitmiş sayar (gamepad kısayolu).</summary>
        public void DevSkipCurrent()
        {
            var n = FocusedNeighborhood();
            if (n != null) DevJumpTo(CompletedCount(n) + 1);
        }

        // ---------- Yardımcılar ----------

        private void AddLine(string speaker, string text, Sprite portrait)
        {
            if (!string.IsNullOrEmpty(text)) lineBuffer.Add(new DialogueLine(speaker, text, portrait));
        }

        private void AddMasterLine(string text)
        {
            AddLine(texts != null ? texts.MasterName : string.Empty, text, texts != null ? texts.MasterPortrait : null);
        }
    }
}
