using BilgiAdasi.Core;
using BilgiAdasi.UI;
using UnityEngine;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// Boy uzama (CLAUDE.md §7): öğrendikçe büyür. Her biten köyde +0.01 m, sınıf atlayınca belirgin.
    /// Boy kaydedilmez: yüklemede SaveData.Grade + CompletedNeighborhoods'tan hesaplanır ve geçişsiz uygulanır.
    /// Köy bitince hedef boya 2 sn'de yumuşakça çıkar. Tavan kontrolü PlayerMotor'da: başın üstü doluysa
    /// boy açık alana çıkınca uygulanır.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerGrowth : MonoBehaviour
    {
        [SerializeField] private GrowthSettings settings;

        private PlayerMotor motor;
        private float appliedHeight;
        private float targetHeight;
        private float speed;
        private int appliedGrade;
        private string pendingNotice;
        private SoundId pendingSound;
        private float noticeAt = -1f;

        /// <summary>Şu anki hedef ayakta boy (m).</summary>
        public float TargetHeight => targetHeight;

        private void Awake() => motor = GetComponent<PlayerMotor>();

        private void OnEnable() => GameEvents.NeighborhoodCompleted += HandleNeighborhoodCompleted;
        private void OnDisable() => GameEvents.NeighborhoodCompleted -= HandleNeighborhoodCompleted;

        private void Start()
        {
            if (settings == null)
            {
                Debug.LogWarning("[PlayerGrowth] GrowthSettings bağlı değil; boy değişmez.", this);
                enabled = false;
                return;
            }

            var data = Data;
            appliedGrade = data != null ? data.Grade : SaveData.DefaultGrade;
            appliedHeight = targetHeight = Compute(data, null);
            motor.SetStandingHeight(appliedHeight);
        }

        private void Update()
        {
            if (!Mathf.Approximately(appliedHeight, targetHeight))
            {
                appliedHeight = Mathf.MoveTowards(appliedHeight, targetHeight, speed * Time.deltaTime);
                motor.SetStandingHeight(appliedHeight);
            }

            if (noticeAt >= 0f && Time.unscaledTime >= noticeAt)
            {
                noticeAt = -1f;
                ViewLocator.Get<INoticeView>()?.ShowNotice(pendingNotice);
                GameEvents.RaiseSound(pendingSound);
            }
        }

        private void HandleNeighborhoodCompleted(string neighborhoodId)
        {
            if (settings == null || !settings.Counts(neighborhoodId)) return;

            var data = Data;
            int grade = data != null ? data.Grade : SaveData.DefaultGrade;
            float newTarget = Compute(data, neighborhoodId);
            bool gradeUp = grade > appliedGrade;
            appliedGrade = grade;
            if (newTarget <= targetHeight + 0.0001f && !gradeUp) return; // tavana (sonraki sınıf boyu) ulaşıldı

            targetHeight = newTarget;
            speed = Mathf.Abs(targetHeight - appliedHeight) / settings.TransitionSeconds;
            pendingNotice = gradeUp ? settings.GradeUpNotice : settings.GrewNotice;
            pendingSound = gradeUp ? SoundId.Achievement : SoundId.Info;
            noticeAt = Time.unscaledTime + settings.NoticeDelay;
        }

        /// <summary>Kayıttaki sayılan köyler (+ henüz kayda geçmemiş olabilecek justCompleted) → boy.</summary>
        private float Compute(SaveData data, string justCompleted)
        {
            int count = 0;
            bool justCounted = false;
            if (data != null)
            {
                var list = data.CompletedNeighborhoods;
                for (int i = 0; i < list.Count; i++)
                {
                    if (!settings.Counts(list[i])) continue;
                    count++;
                    if (list[i] == justCompleted) justCounted = true;
                }
            }
            if (justCompleted != null && !justCounted) count++;
            int grade = data != null ? data.Grade : SaveData.DefaultGrade;
            return settings.HeightFor(grade, count);
        }

        private static SaveData Data => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;
    }
}
