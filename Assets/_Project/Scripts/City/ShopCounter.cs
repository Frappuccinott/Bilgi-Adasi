using System;
using BilgiAdasi.Core;
using BilgiAdasi.Player;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Atölye önü pazarının tezgâhı (karar 14.11): müşteri sokaktan gelip burada sipariş verir, hazır ürünü burada alır.
    /// Kapının (DoorStation) sipariş/teslim görevini devralır: NeighborhoodRunner'ın Counter alanı doluysa Used buradan gelir.
    /// E (gamepad X) ile konuşulur. Tezgâhta bekleyen müşteri yoksa ipucu soluk metin gösterir ama yine çalışır
    /// (Usta "kimse yok" der). Müşterinin gövdesine bakınca da çalışır (CustomerQueue proxy ekler).
    /// </summary>
    public sealed class ShopCounter : WorldInteractable
    {
        [Tooltip("Tezgâhta müşteri varken görünen ipucu.")]
        [SerializeField] private string customerPromptText = "Müşteriyle konuş";
        [Tooltip("Müşteri yokken görünen ipucu.")]
        [SerializeField] private string emptyPromptText = "Tezgâh";
        [Tooltip("Müşterileri yürüten sıra. Boşsa ipucu hep 'müşteri yok' olur.")]
        [SerializeField] private CustomerQueue queue;

        /// <summary>Oyuncu tezgâhta konuştu. NeighborhoodRunner siparişi/teslimi yürütür.</summary>
        public event Action<PlayerInteractor> Used;

        public override string GetPrompt(PlayerInteractor interactor) =>
            queue != null && queue.HasCustomerAtCounter ? customerPromptText : emptyPromptText;

        public override void Interact(PlayerInteractor interactor)
        {
            // Tezgâhta ürün almaya gelen alıcı varsa satışı sıra yürütür; yoksa sipariş/teslim (runner).
            if (queue != null && queue.TryHandle(interactor)) return;
            Used?.Invoke(interactor);
        }
    }
}
