using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.Desk;
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
    public sealed class NeighborhoodRunner : MonoBehaviour, ICurrentTaskSource
    {
        [Tooltip("Oynanabilir köyler. Kapıya birden fazla köyün siparişi hazırsa listedeki sıra önce gelir (demo: Denkleştirme).")]
        [SerializeField] private List<NeighborhoodData> neighborhoods = new List<NeighborhoodData>();
        [SerializeField] private NeighborhoodTexts texts;
        [Tooltip("Sahnedeki kazan istasyonunun OrderController'ı.")]
        [SerializeField] private OrderController orderController;
        [Tooltip("Sahnedeki atölye kapısı.")]
        [SerializeField] private DoorStation door;
        [Tooltip("Atölye önü pazarının tezgâhı (karar 14.11). Doluysa sipariş/teslim kapıda değil burada; kapı yalnızca açılır.")]
        [SerializeField] private ShopCounter counter;
        [Tooltip("Sahnedeki çalışma masası (masa görevleri güvercinle buraya gelir). Boşsa sahnede aranır.")]
        [SerializeField] private DeskController desk;
        [Tooltip("Tarla görevleri (Sv2 Tohum Nefes Alır). Boşsa sahnede aranır; yoksa tarla görevi gelmez.")]
        [SerializeField] private FieldTaskDirector fieldTasks;
        private NeighborhoodData activeFieldNeighborhood;

        private DialogueDirector dialogue;
        private readonly List<DialogueLine> lineBuffer = new List<DialogueLine>();
        // Ustanın tanıtımı köy başına bir kez (oturum içinde); kayıtta ilk görev bitmişse zaten söylenmez.
        private readonly HashSet<string> introShown = new HashSet<string>();
        // Bu oturumda kayıt yoksa (test) biten görevler burada tutulur.
        private readonly HashSet<string> localCompleted = new HashSet<string>();
        private readonly HashSet<string> localAdvancePaid = new HashSet<string>();

        private OrderTask activeOrder;
        private NeighborhoodData activeOrderNeighborhood;
        private OrderTask orderToBegin;
        private NeighborhoodData activeDeskNeighborhood;
        // Kapıdan her geçişte aynı hatırlatma (bekleniyor / kimse yok / ret) tekrar etmesin: durum değişene kadar bir kez.
        private string lastDoorReminder;

        public IReadOnlyList<NeighborhoodData> Neighborhoods => neighborhoods;

        /// <summary>Alınmış (kazanda/teslim bekleyen) sipariş; yoksa null. Pazar sırası (CustomerQueue) okur.</summary>
        public OrderTask ActiveOrder => activeOrder;

        /// <summary>Sipariş alınmamışsa sıradaki siparişi (kimin geleceğini) verir; yoksa null.</summary>
        public OrderTask PeekNextOrder() => activeOrder == null && TryGetNext<OrderTask>(out _, out var t) ? t : null;

        /// <summary>Kapı/tezgâh konuşması sürüyor mu?</summary>
        public bool IsTalking => dialogue != null && dialogue.IsPlaying;

        private static SaveData Data => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

        private void Awake()
        {
            dialogue = GetComponent<DialogueDirector>();
            if (desk == null) desk = FindAnyObjectByType<DeskController>();
            if (fieldTasks == null) fieldTasks = FindAnyObjectByType<FieldTaskDirector>();
        }

        private void OnEnable()
        {
            if (counter != null) counter.Used += OnDoorUsed;
            else if (door != null) door.Used += OnDoorUsed;
            GameEvents.HandItemChanged += OnHandItemChanged;
            if (orderController != null) orderController.TaskFinished += OnOrderFinished;
            if (desk != null) desk.TaskFinished += OnDeskFinished;
            if (fieldTasks != null) fieldTasks.TaskFinished += OnFieldFinished;
            ViewLocator.Register<ICurrentTaskSource>(this);
        }

        private void OnDisable()
        {
            if (counter != null) counter.Used -= OnDoorUsed;
            else if (door != null) door.Used -= OnDoorUsed;
            GameEvents.HandItemChanged -= OnHandItemChanged;
            if (orderController != null) orderController.TaskFinished -= OnOrderFinished;
            if (desk != null) desk.TaskFinished -= OnDeskFinished;
            if (fieldTasks != null) fieldTasks.TaskFinished -= OnFieldFinished;
            ViewLocator.Unregister<ICurrentTaskSource>(this);
        }

        private void Start()
        {
            for (int i = 0; i < neighborhoods.Count; i++)
                if (neighborhoods[i] != null) PublishProgress(neighborhoods[i]);
            RestoreReadyOrder();
            OfferDeskTask(true);
            OfferFieldTask(true);
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

        // ---------- Duraklatma: şu anki görev ----------

        /// <summary>
        /// Alınmış sipariş (kapıda konuşulmuş) için kart verisi; yoksa null. Adımlarda yalnızca girenler yazılır,
        /// ürün "?" kalır (katsayı ve cevap verilmez); biten adımda ürünler katsayısız açılır.
        /// </summary>
        public CurrentTaskInfo GetCurrentTask()
        {
            var order = activeOrder;
            if (order == null) return GetCurrentDeskTask() ?? GetCurrentFieldTask();
            var n = activeOrderNeighborhood != null ? activeOrderNeighborhood : FindOwner(order);
            var session = orderController != null ? orderController.Session : null;
            bool ready = orderController != null && orderController.IsReadyToDeliver;
            int stepIndex = ready ? order.Steps.Count : session != null && session.Task == order ? session.StepIndex : 0;

            var info = new CurrentTaskInfo
            {
                Title = order.GiverName,
                Quote = order.GiverLines != null && order.GiverLines.Length > 0 ? order.GiverLines[0] : null,
                WantedRich = Amounts(order.DeliveryProducts, true),
                ReadyToDeliver = ready,
                Place = n != null ? n.Title : null,
                Done = n != null ? CompletedCount(n) : 0,
                Total = n != null ? n.Tasks.Count : 0
            };
            for (int i = 0; i < order.Steps.Count; i++)
            {
                var step = order.Steps[i];
                bool done = i < stepIndex;
                string text = step.Reaction != null
                    ? Amounts(step.Reaction.Reactants, false) + " → " + (done ? Amounts(step.Reaction.Products, false) : "?")
                    : Amounts(step.TargetProducts, true);
                info.Steps.Add(new CurrentTaskInfo.Step(text, done));
            }
            return info;
        }

        /// <summary>Masadaki mektup (güvercin gelmişse) için kart verisi; yoksa null.</summary>
        private CurrentTaskInfo GetCurrentDeskTask()
        {
            if (desk == null || !desk.HasLetter || desk.ActiveTask == null) return null;
            var task = desk.ActiveTask;
            var n = activeDeskNeighborhood != null ? activeDeskNeighborhood : FindOwner(task);
            var info = new CurrentTaskInfo
            {
                Title = task.GiverName,
                Quote = task.GiverLines != null && task.GiverLines.Length > 0 ? task.GiverLines[0] : null,
                WantedRich = task.PaperTitle,
                Place = n != null ? n.Title : null,
                Done = n != null ? CompletedCount(n) : 0,
                Total = n != null ? n.Tasks.Count : 0
            };
            info.Steps.Add(new CurrentTaskInfo.Step(desk.Texts != null ? desk.Texts.CurrentTaskStep : task.PaperTitle, false));
            return info;
        }

        /// <summary>Tarlada süren görev (Tohum Nefes Alır) için kart verisi; yoksa null.</summary>
        private CurrentTaskInfo GetCurrentFieldTask()
        {
            var task = fieldTasks != null ? fieldTasks.ActiveTask : null;
            if (task == null) return null;
            var n = activeFieldNeighborhood != null ? activeFieldNeighborhood : FindOwner(task);
            return fieldTasks.GetCurrentInfo(n != null ? n.Title : null, n != null ? CompletedCount(n) : 0, n != null ? n.Tasks.Count : 0);
        }

        private static string Amounts(IReadOnlyList<Chemistry.SpeciesAmount> list, bool withCoefficients)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(" + ");
                if (withCoefficients && list[i].Amount != 1) sb.Append(list[i].Amount).Append(' ');
                sb.Append(list[i].Species.FormulaRich);
            }
            return sb.ToString();
        }

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
                if (n == null || n.LockedByDefault || !RequirementMet(n)) continue;
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

        /// <summary>Köyün önkoşulu (requires) bitti mi? Seviye sırası: Denkleştirme (1–5) → Tepkime Türleri (6–10).</summary>
        private bool RequirementMet(NeighborhoodData n)
        {
            var req = n.Requires;
            // Köyde zaten ilerleme varsa (eski kayıt, geliştirici atlaması) önkoşul aranmaz.
            return req == null || req == n || CompletedCount(req) >= req.Tasks.Count || CompletedCount(n) > 0;
        }

        private NeighborhoodData FindOwner(TaskData task)
        {
            for (int i = 0; i < neighborhoods.Count; i++)
                if (neighborhoods[i] != null && neighborhoods[i].IndexOf(task) >= 0) return neighborhoods[i];
            return null;
        }

        // ---------- Kapı ----------

        /// <summary>Eldeki madde değişti: ret repliği (yanlış ürün vb.) yeniden söylenebilir.</summary>
        private void OnHandItemChanged(bool _) => lastDoorReminder = null;

        /// <summary>Aynı hatırlatma az önce söylendiyse true (söylenmez); değilse not alır.</summary>
        private bool AlreadyReminded(OrderTask order, string text)
        {
            string key = (order != null ? order.Id : "-") + "|" + text;
            if (key == lastDoorReminder) return true;
            lastDoorReminder = key;
            return false;
        }

        private void OnDoorUsed(PlayerInteractor interactor)
        {
            if (dialogue.IsPlaying) return;
            lineBuffer.Clear();
            var inv = interactor != null ? interactor.Inventory : null;

            if (activeOrder != null && orderController != null && orderController.IsReadyToDeliver && inv != null)
            {
                var order = activeOrder;
                string refusal = CheckDelivery(order, inv);
                if (refusal != null)
                {
                    if (AlreadyReminded(order, refusal)) return;
                    AddLine(order.GiverName, refusal, order.GiverPortrait);
                }
                else
                {
                    lastDoorReminder = null;
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
                // Sipariş alındı ama hazır değil: görev sol üstteki kartta zaten görünür, kapıdan geçerken hatırlatılmaz.
                return;
            }
            else if (OfferNextOrder(inv))
            {
                lastDoorReminder = null;
            }
            else
            {
                string nobody = texts != null ? texts.NobodyAtDoor : null;
                if (AlreadyReminded(null, nobody)) return;
                AddMasterLine(nobody);
            }

            dialogue.Play(lineBuffer, OnDoorDialogueFinished, interactor);
        }

        /// <summary>
        /// Kapı konuşması bitti: alınan siparişin ders notu (varsa) açılır, kapanınca sipariş başlar (kart görünür).
        /// Kanat açık kalır; oyuncu F ile kapatır.
        /// </summary>
        private void OnDoorDialogueFinished()
        {
            var lessons = LearningDirector.Instance;
            if (orderToBegin != null && lessons != null && lessons.ShowLesson(orderToBegin.Id, BeginPendingOrder)) return;
            BeginPendingOrder();
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
        private bool OfferNextOrder(PlayerInventory inv)
        {
            if (!TryGetNext<OrderTask>(out var n, out var task)) return false;

            if (!introShown.Contains(n.Id) && CompletedCount(n) == 0)
                for (int i = 0; i < n.IntroLines.Length; i++) AddMasterLine(n.IntroLines[i]);
            introShown.Add(n.Id);

            for (int i = 0; i < task.GiverLines.Length; i++) AddLine(task.GiverName, task.GiverLines[i], task.GiverPortrait);
            PayAdvance(task, inv);
            // Ders notu varsa usta konuşmayı kapatır: "kazana koşmadan önce şu notu oku".
            var lessons = LearningDirector.Instance;
            if (lessons != null && lessons.HasLesson(task.Id) && !string.IsNullOrWhiteSpace(lessons.LessonIntroLine))
                AddMasterLine(lessons.LessonIntroLine);
            StartOrder(n, task);
            return true;
        }

        /// <summary>
        /// Peşin ödemeli sipariş (Purchase, ör. denk_5 "parasını peşin veriyorum"): budget keseye bir kez konur.
        /// Kayda işlenir; sipariş yarıda bırakılıp yeniden alınırsa tekrar verilmez.
        /// </summary>
        private void PayAdvance(OrderTask task, PlayerInventory inv)
        {
            if (task.Budget <= 0 || inv == null) return;
            var data = Data;
            if (data != null)
            {
                if (data.IsAdvancePaid(task.Id)) return;
                data.MarkAdvancePaid(task.Id);
            }
            else if (!localAdvancePaid.Add(task.Id)) return;
            inv.AddMoney(task.Budget);
            if (texts != null) AddLine(task.GiverName, string.Format(texts.AdvancePaid, task.Budget), task.GiverPortrait);
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
            if (CompletedCount(n) >= n.Tasks.Count)
            {
                var data = Data;
                if (data == null || !data.IsNeighborhoodCompleted(n.Id)) GameEvents.RaiseNeighborhoodCompleted(n.Id);
            }
            // Bir köyün bitmesi başka köyün önkoşulunu açabilir; sıradaki masa görevi güvercinle gelir.
            OfferDeskTask(false);
            OfferFieldTask(false);
        }

        // ---------- Tarla ----------

        /// <summary>Sıradaki görev tarla göreviyse (Sv2 Tohum Nefes Alır) haber önceki konuşma bitince gelir.</summary>
        private void OfferFieldTask(bool immediate)
        {
            if (fieldTasks == null || fieldTasks.ActiveTask != null) return;
            if (!TryGetNext<FieldTask>(out var n, out var task)) return;
            introShown.Add(n.Id);
            activeFieldNeighborhood = n;
            fieldTasks.Assign(task, immediate);
        }

        private void OnFieldFinished(FieldTask task)
        {
            var n = activeFieldNeighborhood != null && activeFieldNeighborhood.IndexOf(task) >= 0
                ? activeFieldNeighborhood
                : FindOwner(task);
            activeFieldNeighborhood = null;
            if (Data == null) localCompleted.Add(task.Id);
            if (n != null) AfterProgress(n);
        }

        // ---------- Kapısız görev (Seviye 0) ----------

        /// <summary>
        /// Siparişi kapıyı beklemeden kazana verir (Çıraklık: usta görevi atölyede verir). Görev bir köyün sıradaki
        /// görevi değilse ya da başka sipariş alınmışsa false.
        /// </summary>
        public bool StartOrderNow(OrderTask task)
        {
            if (task == null || IsTaskDone(task)) return false;
            if (activeOrder == task) return true;
            if (activeOrder != null) return false;
            var n = FindOwner(task);
            if (n == null || CompletedCount(n) >= n.Tasks.Count || n.Tasks[CompletedCount(n)] != task) return false;
            introShown.Add(n.Id);
            StartOrder(n, task, true);
            return true;
        }

        // ---------- Masa ----------

        /// <summary>
        /// Masa boşsa ve sıradaki görevi masa görevi olan bir köy varsa görevi masaya verir (güvercin gelir).
        /// immediate: güvercin beklenmez (açılış / geliştirici atlaması).
        /// </summary>
        private void OfferDeskTask(bool immediate)
        {
            if (desk == null || desk.Current != DeskController.State.Idle) return;
            if (!TryGetNext<DeskTask>(out var n, out var task)) return;
            bool first = CompletedCount(n) == 0 && !introShown.Contains(n.Id);
            introShown.Add(n.Id);
            activeDeskNeighborhood = n;
            desk.Assign(task, first ? n.IntroLines : null, immediate);
        }

        private void OnDeskFinished(DeskTask task)
        {
            var n = activeDeskNeighborhood != null && activeDeskNeighborhood.IndexOf(task) >= 0
                ? activeDeskNeighborhood
                : FindOwner(task);
            activeDeskNeighborhood = null;
            if (Data == null) localCompleted.Add(task.Id);
            if (n != null) AfterProgress(n);
        }

        // ---------- Geliştirici kısayolları ----------

        /// <summary>
        /// Odaktaki köy: alınmış siparişin köyü, yoksa bitmemiş ilk köy, yoksa ilk köy.
        /// </summary>
        public NeighborhoodData FocusedNeighborhood()
        {
            if (activeOrderNeighborhood != null) return activeOrderNeighborhood;
            if (activeDeskNeighborhood != null && desk != null && desk.HasLetter) return activeDeskNeighborhood;
            for (int i = 0; i < neighborhoods.Count; i++)
                if (neighborhoods[i] != null && CompletedCount(neighborhoods[i]) < neighborhoods[i].Tasks.Count) return neighborhoods[i];
            return neighborhoods.Count > 0 ? neighborhoods[0] : null;
        }

        /// <summary>
        /// Odaktaki köyde index'inci göreve atlar: öncekiler bitmiş, sonrakiler bitmemiş sayılır.
        /// index == görev sayısı → köy tamamlanır. Sipariş göreviyse kapıyı beklemeden kazana verilir.
        /// </summary>
        public void DevJumpTo(int index) => DevJumpTo(FocusedNeighborhood(), index);

        /// <summary>
        /// Hile menüsü: verilen türden ilk görevi olan köye atlar. Önceki köyler bitmiş sayılır (kayıtta), o köy
        /// o görevden başlar. Döner: atlanan görev (yoksa null).
        /// </summary>
        public T DevJumpToFirst<T>() where T : TaskData
        {
            var data = Data;
            for (int k = 0; k < neighborhoods.Count; k++)
            {
                var n = neighborhoods[k];
                if (n == null) continue;
                for (int t = 0; t < n.Tasks.Count; t++)
                {
                    if (n.Tasks[t] is not T found) continue;
                    // Önceki köyler bitmiş: odak bu köye geçsin.
                    for (int p = 0; p < k; p++)
                    {
                        var prev = neighborhoods[p];
                        if (prev == null) continue;
                        for (int i = 0; i < prev.Tasks.Count; i++)
                        {
                            if (data != null) data.MarkTaskCompleted(prev.Tasks[i].Id);
                            else localCompleted.Add(prev.Tasks[i].Id);
                        }
                        if (data != null) data.MarkNeighborhoodCompleted(prev.Id);
                        introShown.Add(prev.Id);
                        PublishProgress(prev);
                    }
                    DevJumpTo(n, t);
                    return found;
                }
            }
            return null;
        }

        private void DevJumpTo(NeighborhoodData n, int index)
        {
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
                    // Tahta yalnızca tepkimede yazılır; atlanan siparişlerin denklemleri de tahtada olsun (geri atlamada silinir).
                    if (n.Tasks[i] is OrderTask skipped)
                    {
                        for (int s = 0; s < skipped.Steps.Count; s++)
                        {
                            var reaction = skipped.Steps[s].Reaction;
                            if (reaction == null) continue;
                            string equation = Chemistry.FormulaFormatter.Equation(reaction);
                            if (done) data.AddBoardEquation(equation);
                            else data.RemoveBoardEquation(equation);
                        }
                    }
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
            if (desk != null) desk.ClearTask();
            activeDeskNeighborhood = null;
            if (fieldTasks != null) fieldTasks.ClearTask();
            activeFieldNeighborhood = null;

            if (index < n.Tasks.Count && n.Tasks[index] is OrderTask order) StartOrder(n, order, true);

            PublishProgress(n);
            if (index >= n.Tasks.Count && (data == null || !data.IsNeighborhoodCompleted(n.Id)))
                GameEvents.RaiseNeighborhoodCompleted(n.Id);
            OfferDeskTask(true);
            OfferFieldTask(true);
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
