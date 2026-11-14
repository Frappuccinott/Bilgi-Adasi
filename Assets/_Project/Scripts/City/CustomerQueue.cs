using System;
using System.Collections.Generic;
using BilgiAdasi.Order;
using BilgiAdasi.Player;
using BilgiAdasi.UI;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Atölye önü pazarının müşteri sırası (karar 14.11). İki tür müşteri yürütür:
    ///  • Sipariş müşterisi: sıradaki görevi veren sakin (OrderTask.GiverName) sokaktan gelir, tezgâhta bekler.
    ///    Sipariş alınınca banka oturur; ürün hazır olunca tezgâha döner; teslimden sonra sokağa gidip kaybolur.
    ///  • Alıcı: tezgâh boşken ve oyuncunun elinde istenen bir ürün varsa (CustomerRequestSet) gelir, ürünü satın alır.
    ///    Siparişin teslim edilecek ürünü istenmez. Sipariş müşterisi tezgâha gelecekse alıcı yer açar.
    /// Fizikle yürümez: sokak → yaklaşma noktaları → tezgâh / bank düz çizgilerle (atölye önü düz ve boş).
    /// Konuşmayı NeighborhoodRunner (sipariş) ya da burası (alıcı) DialogueDirector ile oynatır.
    /// </summary>
    public sealed class CustomerQueue : MonoBehaviour
    {
        [Serializable]
        public sealed class Body
        {
            [Tooltip("Bu gövde kimin? OrderTask.GiverName ile aynı yazılır (ör. \"Fırıncı Zehra\"). Boşsa alıcı/yedek gövde.")]
            [SerializeField] private string giverName = "";
            [Tooltip("Sahnedeki müşteri modeli (kökünde Animator).")]
            [SerializeField] private Animator animator;
            [Tooltip("Klip öneki (ör. MusteriZehra). Boşsa obje adından NPC_ atılarak bulunur.")]
            [SerializeField] private string clipPrefix = "";
            [Tooltip("Alıcı isteğindeki kadın/erkek adıyla eşleşsin diye.")]
            [SerializeField] private bool female;

            public string GiverName => giverName;
            public bool Female => female;
            public Animator Animator => animator;
            public string ClipPrefix => clipPrefix;
        }

        private enum Spot { Street, Counter, Bench }

        private sealed class Walker
        {
            public Body Body;
            public Transform Tr;
            public readonly Vector3[] Path = new Vector3[16];
            public int Count, Index;
            public Spot At = Spot.Street, Target = Spot.Street;
            public bool Active;
            public OrderTask Task;
            public CustomerRequestSet.Request Request;
            public bool Asked;
            public float Patience;
            public int IdleHash, WalkHash, TalkHash, Playing;

            public bool Moving => Index < Count;
            public bool ArrivedAt(Spot s) => Active && !Moving && At == s;
        }

        [Header("Bağlantılar")]
        [SerializeField] private NeighborhoodRunner runner;
        [SerializeField] private OrderController orderController;
        [SerializeField] private ShopCounter counter;
        [Tooltip("Runner'daki DialogueDirector (alıcı konuşması). Boşsa runner'dan alınır.")]
        [SerializeField] private DialogueDirector dialogue;

        [Header("Noktalar (Atolye_Pazar çapaları)")]
        [Tooltip("Müşterinin göründüğü / kaybolduğu yer (sokakta, oyuncunun göremeyeceği kadar uzakta değil; köşe arkası).")]
        [SerializeField] private Transform street;
        [Tooltip("Sokaktan tezgâha giderken sırayla geçilen noktalar (tezgâh hariç). Boş olabilir.")]
        [SerializeField] private List<Transform> approach = new List<Transform>();
        [Tooltip("Musteri_Nokta_1: tezgâhın önü.")]
        [SerializeField] private Transform counterPoint;
        [Tooltip("Musteri_Nokta_2: bekleme bankı.")]
        [SerializeField] private Transform benchPoint;

        [Header("Müşteriler")]
        [SerializeField] private List<Body> bodies = new List<Body>();
        [Tooltip("Ürün almaya gelenlerin istekleri. Boşsa yalnızca sipariş müşterisi gelir.")]
        [SerializeField] private CustomerRequestSet requests;

        [Header("Hareket")]
        [SerializeField] private float walkSpeed = 1.3f;
        [SerializeField] private float turnSpeed = 300f;
        [Tooltip("Bir müşteri gittikten sonra sıradaki kaç saniye sonra yola çıkar.")]
        [SerializeField] private float nextDelay = 2.5f;
        [Tooltip("Alıcı, ürünü alamazsa en fazla kaç saniye bekler.")]
        [SerializeField] private float buyerPatience = 70f;

        private const float CrossFade = 0.2f;
        private const float ThinkInterval = 0.25f;

        private readonly Walker order = new Walker();
        private readonly Walker buyer = new Walker();
        private readonly List<DialogueLine> lines = new List<DialogueLine>();
        private PlayerInventory inventory;
        private float thinkTimer, orderDelay, buyerDelay;
        private int requestCursor;

        /// <summary>Tezgâhın önünde bekleyen (yürümeyen) bir müşteri var mı? Tezgâh ipucu bunu okur.</summary>
        public bool HasCustomerAtCounter => order.ArrivedAt(Spot.Counter) || buyer.ArrivedAt(Spot.Counter);

        private bool Talking => dialogue != null && dialogue.IsPlaying;

        private void Awake()
        {
            if (dialogue == null && runner != null) dialogue = runner.GetComponent<DialogueDirector>();
            int layer = LayerMask.NameToLayer("Interactable");
            for (int i = 0; i < bodies.Count; i++)
            {
                var a = bodies[i] != null ? bodies[i].Animator : null;
                if (a == null) continue;
                // Gövdeye bakınca da tezgâh çalışsın.
                var col = a.GetComponent<CapsuleCollider>();
                if (col == null)
                {
                    col = a.gameObject.AddComponent<CapsuleCollider>();
                    col.center = new Vector3(0f, 0.85f, 0f);
                    col.height = 1.7f;
                    col.radius = 0.28f;
                }
                if (layer >= 0) a.gameObject.layer = layer;
                if (counter != null)
                {
                    var proxy = a.GetComponent<InteractableProxy>();
                    if (proxy == null) proxy = a.gameObject.AddComponent<InteractableProxy>();
                    proxy.SetTarget(counter);
                }
                a.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            inventory = FindAnyObjectByType<PlayerInventory>();
            orderDelay = 1f;
            buyerDelay = requests != null ? Mathf.Min(requests.Cooldown, 8f) : 0f;
        }

        // ------------------------------------------------------------------ kare
        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;      // duraklatma menüsü

            thinkTimer -= dt;
            if (thinkTimer <= 0f)
            {
                thinkTimer = ThinkInterval;
                ThinkOrder(ThinkInterval);
                ThinkBuyer(ThinkInterval);
            }
            Step(order, dt);
            Step(buyer, dt);
        }

        // ------------------------------------------------------------------ sipariş müşterisi
        private void ThinkOrder(float dt)
        {
            if (runner == null) return;
            var active = runner.ActiveOrder;
            var want = active != null ? active : runner.PeekNextOrder();

            if (!order.Active)
            {
                if (want == null || Talking) return;
                orderDelay -= dt;
                if (orderDelay > 0f) return;
                var body = FindBody(want.GiverName, buyer.Body);
                if (body == null) return;
                Spawn(order, body);
                order.Task = want;
                return;
            }

            if (Talking) return;    // konuşma sürerken kimse yerinden kalkmaz
            if (order.Task != want)
            {
                // Teslim edildi (ya da görev değişti): sokağa döner.
                if (order.Target != Spot.Street) GoTo(order, Spot.Street);
                return;
            }

            Spot target = active == order.Task && !(orderController != null && orderController.IsReadyToDeliver)
                ? Spot.Bench
                : Spot.Counter;
            if (target == Spot.Counter && buyer.Active && buyer.Target == Spot.Counter)
            {
                // Tezgâhta alıcı var: o yer açana kadar bankta bekle; alıcı hemen ayrılır.
                SendBuyerAway();
                target = Spot.Bench;
            }
            if (order.Target != target) GoTo(order, target);
        }

        // ------------------------------------------------------------------ alıcı
        private void ThinkBuyer(float dt)
        {
            if (requests == null || inventory == null) return;

            if (buyer.Active)
            {
                if (buyer.ArrivedAt(Spot.Counter) && !Talking)
                {
                    buyer.Patience -= dt;
                    if (buyer.Patience <= 0f || inventory.CountOf(buyer.Request.Product) <= 0) SendBuyerAway();
                }
                return;
            }

            buyerDelay -= dt;
            if (buyerDelay > 0f || Talking) return;
            // Sipariş müşterisi tezgâha gelmek üzereyse alıcı çıkmaz.
            if (order.Active && order.Target == Spot.Counter) return;
            if (!order.Active && runner != null && runner.PeekNextOrder() != null) return;

            var req = PickRequest();
            if (req == null) return;
            var body = FindBody(null, order.Body, req.Female);
            if (body == null) return;
            Spawn(buyer, body);
            buyer.Request = req;
            buyer.Asked = false;
            buyer.Patience = buyerPatience;
            GoTo(buyer, Spot.Counter);
        }

        private CustomerRequestSet.Request PickRequest()
        {
            var list = requests.Requests;
            int n = list.Count;
            for (int k = 0; k < n; k++)
            {
                var r = list[(requestCursor + k) % n];
                if (r == null || r.Product == null || inventory.CountOf(r.Product) <= 0) continue;
                if (IsNeededForOrder(r.Product)) continue;
                requestCursor = (requestCursor + k + 1) % n;
                return r;
            }
            return null;
        }

        /// <summary>Teslim edilecek ürün satılmaz (görev kilitlenmesin).</summary>
        private bool IsNeededForOrder(Chemistry.SpeciesData s)
        {
            var task = runner != null ? runner.ActiveOrder : null;
            if (task == null) return false;
            var products = task.DeliveryProducts;
            for (int i = 0; i < products.Count; i++)
                if (products[i].Species == s) return inventory.CountOf(s) <= products[i].Amount;
            return false;
        }

        private void SendBuyerAway()
        {
            if (!buyer.Active || buyer.Target == Spot.Street) return;
            GoTo(buyer, Spot.Street);
            buyerDelay = requests != null ? requests.Cooldown : 20f;
        }

        /// <summary>
        /// Tezgâhta alıcı varsa konuşmayı burası yürütür (true). Sipariş müşterisi ya da kimse yoksa false → runner.
        /// </summary>
        public bool TryHandle(PlayerInteractor interactor)
        {
            if (!buyer.ArrivedAt(Spot.Counter) || order.ArrivedAt(Spot.Counter) || Talking || dialogue == null) return false;
            var req = buyer.Request;
            var inv = interactor != null ? interactor.Inventory : inventory;
            if (req == null || inv == null) return false;

            lines.Clear();
            string productName = req.Product.DisplayName;
            if (inv.SelectedSpecies == req.Product)
            {
                inv.Remove(req.Product, 1);
                inv.AddMoney(req.Price);
                if (!string.IsNullOrWhiteSpace(req.ThanksLine)) Add(req, req.ThanksLine);
                Add(req, string.Format(requests.PaidLine, req.Price));
                if (Core.SaveSystem.Instance != null) Core.SaveSystem.Instance.Checkpoint();
                dialogue.Play(lines, SendBuyerAway, interactor);
            }
            else
            {
                Add(req, !buyer.Asked ? string.Format(req.AskLine, productName, req.Price)
                                      : string.Format(requests.WrongItemLine, productName));
                buyer.Asked = true;
                dialogue.Play(lines, null, interactor);
            }
            return true;
        }

        private void Add(CustomerRequestSet.Request req, string text) =>
            lines.Add(new DialogueLine(req.CustomerName, text, req.Portrait));

        // ------------------------------------------------------------------ gövde ve yol
        private Body FindBody(string giverName, Body exclude, bool female = false)
        {
            if (string.IsNullOrEmpty(giverName))
                for (int i = 0; i < bodies.Count; i++)
                {
                    var b = bodies[i];
                    if (b != null && b.Animator != null && b != exclude && string.IsNullOrEmpty(b.GiverName) && b.Female == female)
                        return b;
                }
            // Sipariş müşterisi: yalnızca adı eşleşen gövde. Eşleşmeyen (ör. Câbir Usta'nın çıraklık görevi) pazara gelmez.
            // Alıcı: adı boş gövdeler.
            for (int i = 0; i < bodies.Count; i++)
            {
                var b = bodies[i];
                if (b == null || b.Animator == null || b == exclude) continue;
                if (string.IsNullOrEmpty(giverName) ? string.IsNullOrEmpty(b.GiverName) : b.GiverName == giverName) return b;
            }
            return null;
        }

        private void Spawn(Walker w, Body body)
        {
            w.Body = body;
            w.Tr = body.Animator.transform;
            string prefix = body.ClipPrefix;
            if (string.IsNullOrEmpty(prefix))
            {
                prefix = body.Animator.gameObject.name;
                if (prefix.StartsWith("NPC_", StringComparison.Ordinal)) prefix = prefix.Substring(4);
            }
            w.IdleHash = Animator.StringToHash(prefix + "_Bekle_Dongu");
            w.WalkHash = Animator.StringToHash(prefix + "_Yuru_Dongu");
            w.TalkHash = Animator.StringToHash(prefix + "_Konus_Dongu");
            w.Playing = 0;
            w.Active = true;
            w.At = Spot.Street;
            w.Target = Spot.Street;
            w.Count = w.Index = 0;
            if (street != null) w.Tr.SetPositionAndRotation(street.position, street.rotation);
            body.Animator.gameObject.SetActive(true);
        }

        private void GoTo(Walker w, Spot target)
        {
            w.Count = 0;
            w.Index = 0;
            w.Target = target;
            if (target == Spot.Street)
            {
                for (int i = approach.Count - 1; i >= 0; i--) Push(w, approach[i]);
                Push(w, street);
            }
            else if (w.At == Spot.Street)
            {
                for (int i = 0; i < approach.Count; i++) Push(w, approach[i]);
                Push(w, target == Spot.Bench ? benchPoint : counterPoint);
            }
            else Push(w, target == Spot.Bench ? benchPoint : counterPoint);
        }

        private static void Push(Walker w, Transform t)
        {
            if (t != null && w.Count < w.Path.Length) w.Path[w.Count++] = t.position;
        }

        private void Step(Walker w, float dt)
        {
            if (!w.Active || w.Tr == null) return;

            if (w.Moving)
            {
                Vector3 pos = w.Tr.position;
                Vector3 goal = w.Path[w.Index];
                Vector3 flat = goal - pos;
                flat.y = 0f;
                Face(w, flat, dt);
                w.Tr.position = Vector3.MoveTowards(pos, goal, walkSpeed * dt);
                if ((w.Tr.position - goal).sqrMagnitude < 0.0004f)
                {
                    w.Index++;
                    if (!w.Moving) Arrive(w);
                }
                Play(w, w.WalkHash);
                return;
            }

            // Durunca tezgâha döner; tezgâhta konuşma sürerken konuşma klibi.
            if (counter != null)
            {
                Vector3 to = counter.transform.position - w.Tr.position;
                to.y = 0f;
                Face(w, to, dt);
            }
            Play(w, w.At == Spot.Counter && Talking ? w.TalkHash : w.IdleHash);
        }

        private void Arrive(Walker w)
        {
            w.At = w.Target;
            if (w.At != Spot.Street) return;
            // Sokağa döndü: gizlenir, sıradaki biraz sonra gelir.
            w.Active = false;
            w.Task = null;
            w.Request = null;
            if (w.Body != null && w.Body.Animator != null) w.Body.Animator.gameObject.SetActive(false);
            w.Body = null;
            if (w == order) orderDelay = nextDelay;
        }

        private void Face(Walker w, Vector3 dir, float dt)
        {
            if (dir.sqrMagnitude < 1e-4f) return;
            var want = Quaternion.LookRotation(dir.normalized, Vector3.up);
            w.Tr.rotation = Quaternion.RotateTowards(w.Tr.rotation, want, turnSpeed * dt);
        }

        private void Play(Walker w, int hash)
        {
            if (w.Playing == hash) return;
            var a = w.Body != null ? w.Body.Animator : null;
            if (a == null || !a.HasState(0, hash)) return;
            a.CrossFadeInFixedTime(hash, CrossFade, 0);
            w.Playing = hash;
        }
    }
}
