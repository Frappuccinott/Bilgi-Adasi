using System.Collections;
using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using BilgiAdasi.Player;
using BilgiAdasi.UI;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Seviye 0 — Çıraklık, "İlk Kural" (04-gorev-plani §4 Seviye 0, 01 §1). Yeni oyunda bir kez:
    /// sınıf kartları (kararmış ekran) → atölye kapısında uyanış, Câbir Usta konuşur → tahmin sorusu →
    /// usta iki kavanoz H₂ verir, sipariş kapıyı beklemeden kazana gelir (cirak_0) → oyuncu döker, ateşi yakar:
    /// tepkime yok, terazinin iki kefesi eşit, kavanozlar geri gelir → Sorgu + K-0 (LearningDirector) → görev biter
    /// (kapıda teslim yok). Sonra kapıya Muhtar Yusuf gelir (Denkleştirme, önkoşulu Çıraklık).
    /// Eski kayıtta (başka görev bitmişse) Seviye 0 atlanır ve bitmiş sayılır.
    /// NeighborhoodRunner objesinde durur; görünümleri arayüzle tanır (IStoryCardView, IReasoningView, INoticeView).
    /// </summary>
    public sealed class ApprenticeshipDirector : MonoBehaviour
    {
        [SerializeField] private ApprenticeshipData data;
        [SerializeField] private NeighborhoodRunner runner;
        [SerializeField] private OrderController orderController;
        [Tooltip("Boşsa aynı objedeki DialogueDirector.")]
        [SerializeField] private DialogueDirector dialogue;
        [Tooltip("Uyanınca oyuncu buraya döner (atölye kapısı). Boş bırakılabilir.")]
        [SerializeField] private Transform wakeLookAt;

        private PlayerInteractor user;
        private readonly List<DialogueLine> lines = new List<DialogueLine>();

        /// <summary>Açılış/uyanış/tahmin sürüyor (oyuncu kilitli).</summary>
        public static bool IsPlaying { get; private set; }

        private static SaveData Save => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

        private void Awake()
        {
            if (dialogue == null) dialogue = GetComponent<DialogueDirector>();
            if (runner == null) runner = GetComponent<NeighborhoodRunner>();
        }

        private void OnEnable()
        {
            if (orderController != null) orderController.ReadyToDeliver += OnReadyToDeliver;
        }

        private void OnDisable()
        {
            if (orderController != null) orderController.ReadyToDeliver -= OnReadyToDeliver;
            if (IsPlaying) Release();
        }

        private IEnumerator Start()
        {
            if (data == null || data.Task == null || runner == null) yield break;
            var task = data.Task;
            // Çıraklıkta kapı yok: kart rehberinin son adımı "Usta bakıyor".
            if (orderController != null) orderController.NoDoorTask = task;

            // Açılış oynanacaksa ilk kart ilk karede (solmadan) ekranı örter: sahne hiç görünmez.
            var card = ViewLocator.Get<IStoryCardView>();
            bool covered = false;
            if (card != null && data.ClassroomCards.Length > 0 && !runner.IsTaskDone(task) && !HasOtherProgress(task))
            {
                card.ShowImmediate(data.ClassroomCards[0]);
                covered = true;
            }

            // Oyuncu (PlayerSpawn) ve runner'ın kayıttan kurulumu bitsin.
            user = FindAnyObjectByType<PlayerInteractor>();
            for (int i = 0; i < 120 && user == null; i++)
            {
                yield return null;
                user = FindAnyObjectByType<PlayerInteractor>();
            }
            if (covered && user != null) user.SetControlEnabled(false);

            if (runner.IsTaskDone(task) || HasOtherProgress(task) ||
                (orderController != null && orderController.ActiveTask == task))
            {
                if (covered) card.Hide();
            }

            if (runner.IsTaskDone(task)) yield break;

            if (HasOtherProgress(task))
            {
                // Seviye 0'dan önce başlamış kayıt: çıraklık geçilmiş sayılır.
                Save?.MarkTaskCompleted(task.Id);
                SaveSystem.Instance?.Save();
                yield break;
            }

            if (orderController != null && orderController.ActiveTask == task)
            {
                if (orderController.IsReadyToDeliver) OnReadyToDeliver(task);
                yield break;
            }

            yield return Prologue(task);
        }

        private IEnumerator Prologue(OrderTask task)
        {
            IsPlaying = true;
            if (user != null) user.SetControlEnabled(false);
            // Uyanışta kapıya dönük: kartların arkasında döner, kart kalkınca oyuncu zaten kapıya bakar.
            FaceWakeTarget();

            // 1) Sınıf: kararmış ekranda kartlar (ilki Start'ta solmadan açıldı)
            var card = ViewLocator.Get<IStoryCardView>();
            if (card != null && data.ClassroomCards.Length > 0)
            {
                for (int i = 0; i < data.ClassroomCards.Length; i++)
                {
                    if (i > 0) card.Show(data.ClassroomCards[i]);
                    yield return WaitOrSkip(data.CardSeconds);
                }
                card.Hide();
                yield return new WaitForSecondsRealtime(0.7f);
            }

            // 2) Uyanış: atölye kapısında usta
            lines.Clear();
            for (int i = 0; i < data.WakeLines.Length; i++) lines.Add(new DialogueLine(data.MasterName, data.WakeLines[i], null));
            bool talked = false;
            if (dialogue != null && lines.Count > 0)
            {
                dialogue.Play(lines, () => talked = true, user);
                while (!talked) yield return null;
            }

            // 3) Tahmin: görevden önce tek soru
            var reasoning = ViewLocator.Get<IReasoningView>();
            if (reasoning != null && !string.IsNullOrWhiteSpace(data.PredictionQuestion))
            {
                if (user != null) user.SetControlEnabled(false);
                GameInput.SetCursorFree(true);
                bool answered = false;
                reasoning.ShowQuestion(data.MasterName, data.PredictionQuestion, data.PredictionOptions,
                    index => reasoning.ShowMessage(data.MasterName, data.ReplyFor(index), data.ContinueWord, () => answered = true));
                while (!answered) yield return null;
                reasoning.Hide();
            }

            // 4) Hediye: iki kavanoz H₂ (eksikse tamamlanır), sipariş kazana gelir
            var inventory = user != null ? user.Inventory : null;
            if (inventory != null && data.GiftSpecies != null)
            {
                int missing = data.GiftCount - inventory.CountOf(data.GiftSpecies);
                if (missing > 0) inventory.Add(data.GiftSpecies, missing);
            }
            runner.StartOrderNow(task);
            ViewLocator.Get<INoticeView>()?.ShowNotice(data.GiftNotice);
            Release();
        }

        private void FaceWakeTarget()
        {
            if (wakeLookAt == null || user == null) return;
            var look = user.GetComponentInParent<PlayerLook>() ?? FindAnyObjectByType<PlayerLook>();
            if (look == null) return;
            var dir = wakeLookAt.position - look.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) look.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        private void Release()
        {
            IsPlaying = false;
            if (user != null) user.SetControlEnabled(true);
            GameInput.SetCursorFree(GameInput.Mode != InputMode.Player);
        }

        /// <summary>Kartı süre dolunca ya da Interact/Jump/Confirm ile geçer.</summary>
        private static IEnumerator WaitOrSkip(float seconds)
        {
            // Açan basışın kartı hemen geçmemesi için kısa bekleme.
            yield return new WaitForSecondsRealtime(0.4f);
            var c = GameInput.Controls;
            for (float t = 0.4f; t < seconds; t += Time.unscaledDeltaTime)
            {
                if (c.Player.Interact.WasPressedThisFrame() || c.Player.Jump.WasPressedThisFrame() ||
                    c.UI.Submit.WasPressedThisFrame()) yield break;
                yield return null;
            }
        }

        // ---------- Görev sonu ----------

        private void OnReadyToDeliver(OrderTask task)
        {
            if (data == null || task != data.Task || orderController == null) return;
            // Çıraklıkta kapı yok: ürün (aynı kavanozlar) oyuncuda kalır, görev hemen biter. Sorgu + K-0 LearningDirector'da.
            orderController.Deliver();
            StartCoroutine(DoneNotice());
        }

        private IEnumerator DoneNotice()
        {
            yield return new WaitForSeconds(1f);
            while (LearningDirector.IsBusy) yield return null;
            ViewLocator.Get<INoticeView>()?.ShowNotice(data.DoneNotice);
        }

        private static bool HasOtherProgress(TaskData task)
        {
            var save = Save;
            if (save == null) return false;
            var done = save.CompletedTasks;
            for (int i = 0; i < done.Count; i++)
                if (done[i] != task.Id) return true;
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => IsPlaying = false;
    }
}
