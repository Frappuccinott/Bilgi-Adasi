using System;
using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Atölye önü pazarına ürün almaya gelen müşterilerin istekleri (karar 14.11). Siparişten ayrıdır: köy ilerlemesine
    /// sayılmaz, yalnızca akçe verir. Müşteri ancak oyuncunun elinde (envanterinde) o ürün varsa gelir; böylece boşa
    /// yürüyüp gitmez. Fiyat sabit, pazarlık/şans yok (01 §7). Metinlerde {0} = ürünün etiket adı, {1} = fiyat.
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Pazar Müşteri İstekleri", fileName = "MusteriIstekleri")]
    public sealed class CustomerRequestSet : ScriptableObject
    {
        [Serializable]
        public sealed class Request
        {
            [Tooltip("Müşterinin adı (konuşma balonunda).")]
            [SerializeField] private string customerName = "Müşteri";
            [SerializeField] private Sprite portrait;
            [Tooltip("Kadın adı mı? Gelen gövde buna göre seçilir.")]
            [SerializeField] private bool female;
            [Tooltip("İstenen ürün (kazanda üretilen).")]
            [SerializeField] private SpeciesData product;
            [Tooltip("Bir kavanoz için ödenen akçe.")]
            [Min(1)] [SerializeField] private int price = 2;
            [Tooltip("Tezgâha gelince söylediği (gündelik dil; formül değil).")]
            [TextArea] [SerializeField] private string askLine = "";
            [Tooltip("Ürünü alınca.")]
            [TextArea] [SerializeField] private string thanksLine = "";

            public string CustomerName => customerName;
            public Sprite Portrait => portrait;
            public bool Female => female;
            public SpeciesData Product => product;
            public int Price => price;
            public string AskLine => askLine;
            public string ThanksLine => thanksLine;
        }

        [SerializeField] private List<Request> requests = new List<Request>();
        [Tooltip("Elde istenen ürün yokken. {0} = ürün adı.")]
        [TextArea] [SerializeField] private string wrongItemLine = "Bu değil evladım, ben {0} istemiştim.";
        [Tooltip("Satıştan sonra kese satırı. {0} = akçe.")]
        [SerializeField] private string paidLine = "Al bakalım, {0} akçe.";
        [Tooltip("Bir satıştan sonraki müşteri en erken kaç saniye sonra gelir.")]
        [Min(0f)] [SerializeField] private float cooldown = 25f;

        public IReadOnlyList<Request> Requests => requests;
        public string WrongItemLine => wrongItemLine;
        public string PaidLine => paidLine;
        public float Cooldown => cooldown;
    }
}
