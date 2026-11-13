using System;
using BilgiAdasi.Player;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Atölye kapısı (karar 13.11: istasyon kamerası yok). E → sakinle konuşma: sipariş alma ya da elle teslim.
    /// Akış NeighborhoodRunner'da (Used olayını dinler). Bu bileşen yalnızca bağlantı noktasıdır.
    /// </summary>
    public sealed class DoorStation : WorldInteractable
    {
        [Tooltip("Ekranda tuş simgesinin yanında görünen metin.")]
        [SerializeField] private string promptText = "Kapıya bak";

        /// <summary>Oyuncu kapıyla etkileşti. Runner konuşmayı başlatır.</summary>
        public event Action<PlayerInteractor> Used;

        public override string GetPrompt(PlayerInteractor interactor) => promptText;

        public override void Interact(PlayerInteractor interactor) => Used?.Invoke(interactor);
    }
}
