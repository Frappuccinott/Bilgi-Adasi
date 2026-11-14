using BilgiAdasi.Core;
using BilgiAdasi.UI;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Yol sohbeti tetiği (10-gorev-pazar-npc §6): oyuncu (kamera) bu noktanın çevresine girince listedeki
    /// ilk uygun, duyulmamış sohbeti altyazı olarak oynatır. Fizik tetiği değil yatay mesafe kullanır; böylece
    /// bisikletteyken de çalışır. Konuşma sürerken oyuncu kapıda/istasyonda ya da menüdeyse altyazı bekler.
    /// Aynı anda tek sohbet çalar; bir sohbet bitince oyuncu bölgeden çıkıp yeniden girmeden yenisi başlamaz.
    /// </summary>
    public sealed class AmbientDialogueTrigger : MonoBehaviour
    {
        [Tooltip("Sıra önemli: ilk uygun olan çalar.")]
        [SerializeField] private AmbientDialogueData[] dialogues = new AmbientDialogueData[0];
        [Tooltip("Yatay yarıçap (m). Belge: Ø 6–8 m.")]
        [SerializeField] private float radius = 7f;
        [Tooltip("Sohbet sürerken oyuncu yarıçapın bu kadar dışına çıkarsa sohbet kesilir (m).")]
        [SerializeField] private float leaveMargin = 12f;
        [Header("Altyazı süresi")]
        [SerializeField] private float minLineSeconds = 2.4f;
        [SerializeField] private float secondsPerCharacter = 0.055f;
        [SerializeField] private float gapSeconds = 0.35f;

        private const float PollInterval = 0.25f;

        private static AmbientDialogueTrigger playing;

        private Transform listener;
        private float pollTimer;
        private bool waitForExit;

        private AmbientDialogueData current;
        private int lineIndex = -1;
        private float lineTimer;
        private float lineDuration;
        private bool lineShown;
        private ISubtitleView view;

        private void Start() => listener = Camera.main != null ? Camera.main.transform : null;

        private void OnDisable()
        {
            if (playing == this) Stop();
        }

        private void Update()
        {
            if (current != null)
            {
                Tick(Time.deltaTime);
                return;
            }

            pollTimer -= Time.deltaTime;
            if (pollTimer > 0f) return;
            pollTimer = PollInterval;

            if (listener == null && Camera.main != null) listener = Camera.main.transform;
            if (listener == null) return;

            bool inside = IsInside(listener.position);
            if (!inside) { waitForExit = false; return; }
            if (waitForExit || playing != null || IsBlocked()) return;

            var save = SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;
            for (int i = 0; i < dialogues.Length; i++)
            {
                var d = dialogues[i];
                if (d == null || (d.PlayOnce && save != null && save.IsAmbientHeard(d.Id)) || !d.IsEligible(save)) continue;
                Begin(d, save);
                return;
            }
        }

        private bool IsInside(Vector3 p) => IsInside(p, radius);

        private bool IsInside(Vector3 p, float r)
        {
            var o = transform.position;
            float dx = p.x - o.x, dz = p.z - o.z;
            return dx * dx + dz * dz <= r * r;
        }

        /// <summary>Kapı/NPC konuşması, ustanın sorusu, istasyon, harita ya da menü açıkken altyazı beklesin.</summary>
        private static bool IsBlocked()
        {
            if (GameInput.Mode != InputMode.Player || Time.timeScale <= 0f) return true;
            if (DialogueFocus.IsOpen) return true;                // birebir konuşma / ustanın sorusu açık
            if (ApprenticeshipDirector.IsPlaying) return true;   // açılış sinematiği, uyanış, tahmin
            return ViewLocator.Get<IDialogueCommands>() is DialogueDirector director && director.IsPlaying;
        }

        private void Begin(AmbientDialogueData dialogue, SaveData save)
        {
            if (dialogue.PlayOnce && save != null) save.MarkAmbientHeard(dialogue.Id);
            playing = this;
            current = dialogue;
            lineIndex = 0;
            PrepareLine();
        }

        private void PrepareLine()
        {
            var line = current.Lines[lineIndex];
            int length = string.IsNullOrEmpty(line.Text) ? 0 : line.Text.Length;
            lineDuration = Mathf.Max(minLineSeconds, length * secondsPerCharacter);
            lineTimer = 0f;
            lineShown = false;
        }

        private void Tick(float dt)
        {
            // Oyuncu çok uzaklaştıysa sohbet orada kalır; başka tetikleri bekletmesin.
            if (listener != null && !IsInside(listener.position, radius + leaveMargin))
            {
                Stop();
                return;
            }

            if (IsBlocked())
            {
                if (lineShown) { view?.HideSubtitle(); lineShown = false; }
                return;
            }

            lineTimer += dt;
            if (lineTimer < 0f) return; // satır arası boşluk

            if (!lineShown)
            {
                view = ViewLocator.Get<ISubtitleView>();
                var line = current.Lines[lineIndex];
                view?.ShowSubtitle(line.Speaker, line.Text);
                lineShown = true;
            }

            if (lineTimer < lineDuration) return;

            view?.HideSubtitle();
            lineIndex++;
            if (lineIndex >= current.Lines.Count) { Stop(); return; }
            PrepareLine();
            lineTimer = -gapSeconds; // satırlar arasında kısa boşluk
        }

        private void Stop()
        {
            if (lineShown) view?.HideSubtitle();
            current = null;
            lineIndex = -1;
            lineShown = false;
            waitForExit = true;
            if (playing == this) playing = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => playing = null;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.62f, 0.31f, 0.17f, 0.8f);
            const int segments = 32;
            var o = transform.position;
            var prev = o + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                var next = o + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
