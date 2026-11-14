using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>Köy akışının görevden bağımsız metinleri (kodda sabit Türkçe metin olmaz).</summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Metin/Köy Metinleri", fileName = "NeighborhoodTexts")]
    public sealed class NeighborhoodTexts : ScriptableObject
    {
        [SerializeField] private string masterName = "Câbir Usta";
        [Tooltip("Ustanın portresi (2D). Boş bırakılabilir.")]
        [SerializeField] private Sprite masterPortrait;
        [TextArea(1, 3)] [SerializeField] private string nobodyAtDoor = "Tezgâhta bekleyen yok. Şimdilik herkesin derdine derman olmuşsun.";
        [TextArea(1, 3)] [SerializeField] private string orderWaiting = "Siparişim kazanın başında seni bekliyor.";

        [Header("Teslim (sakin söyler)")]
        [TextArea(1, 3)] [SerializeField] private string wrongItem = "Ben bunu istemedim.";
        [TextArea(1, 3)] [SerializeField] private string notEnough = "Hepsi bu mu? Eksik var gibi.";
        [TextArea(1, 3)] [SerializeField] private string handEmpty = "Siparişim hazır mı? Elinde göremiyorum.";
        [TextArea(1, 3)] [SerializeField] private string paid = "Al bakalım, {0} akçe. Hakkını helal et.";
        [Tooltip("Peşin ödemeli sipariş (OrderTask.budget) alınırken sakin söyler. {0} = akçe.")]
        [TextArea(1, 3)] [SerializeField] private string advancePaid = "Şu {0} akçe malzemenin parası, peşin. Gerisi teslimde.";

        public string WrongItem => wrongItem;
        public string NotEnough => notEnough;
        public string HandEmpty => handEmpty;
        public string Paid => paid;
        public string AdvancePaid => advancePaid;
        public string MasterName => masterName;
        public Sprite MasterPortrait => masterPortrait;
        public string NobodyAtDoor => nobodyAtDoor;
        public string OrderWaiting => orderWaiting;
    }
}
