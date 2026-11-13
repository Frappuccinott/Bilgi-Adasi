using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Bir görevin ortak verisi (sipariş ya da masa). Oyuncuya görünen bütün metinler buradadır.
    /// Alt tipler: OrderTask (BilgiAdasi.Order), DeskTask (BilgiAdasi.Desk).
    /// </summary>
    public abstract class TaskData : ScriptableObject
    {
        [Tooltip("Kayıtta ve olaylarda kullanılan benzersiz kimlik: denk_1, tur_3 …")]
        [SerializeField] private string id = "task_";
        [Tooltip("Görevi veren: Muhtar Yusuf, ulak …")]
        [SerializeField] private string giverName = "";
        [TextArea(2, 5)] [SerializeField] private string[] giverLines = new string[0];
        [TextArea(1, 3)] [SerializeField] private string[] deliveryLines = new string[0];
        [TextArea(2, 4)] [SerializeField] private string hintText = "";
        [TextArea(2, 4)] [SerializeField] private string realLifeText = "";

        public string Id => id;
        public string GiverName => giverName;
        public string[] GiverLines => giverLines;
        public string[] DeliveryLines => deliveryLines;
        public string HintText => hintText;
        public string RealLifeText => realLifeText;

        /// <summary>Editör veri üreticisi için.</summary>
        protected void SetCommon(string taskId, string giver, string[] lines, string[] delivery, string hint, string realLife)
        {
            id = taskId;
            giverName = giver;
            giverLines = lines;
            deliveryLines = delivery;
            hintText = hint;
            realLifeText = realLife;
        }
    }
}
