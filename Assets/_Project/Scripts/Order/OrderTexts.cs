using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>Sipariş istasyonunun görevden bağımsız metinleri (kodda sabit Türkçe metin olmaz).</summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Metin/Sipariş Metinleri", fileName = "OrderTexts")]
    public sealed class OrderTexts : ScriptableObject
    {
        [TextArea(2, 4)] [SerializeField] private string sealNote =
            "Kavanozun içindekini değiştiremezsin. Kaç kavanoz koyacağını değiştirirsin.";
        [TextArea(1, 3)] [SerializeField] private string notEnoughMoney = "Keseden yeterince akçe kalmadı.";
        [TextArea(1, 3)] [SerializeField] private string shortageForgiven = "Bir dahakine dikkat. Eksiği benden olsun.";
        [SerializeField] private string currencyName = "akçe";

        public string SealNote => sealNote;
        public string NotEnoughMoney => notEnoughMoney;
        public string ShortageForgiven => shortageForgiven;
        public string CurrencyName => currencyName;
    }
}
