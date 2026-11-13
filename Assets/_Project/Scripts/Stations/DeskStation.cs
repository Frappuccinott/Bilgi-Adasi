using BilgiAdasi.Player;
using UnityEngine;

namespace BilgiAdasi.Desk
{
    /// <summary>
    /// Çalışma masası istasyonu (sözel görevler). Yalnızca masada mektup varken oturulur;
    /// giriş/çıkış DeskController'a iletilir (kâğıt açılır/kapanır).
    /// </summary>
    public sealed class DeskStation : StationBase
    {
        private DeskController controller;

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponent<DeskController>();
        }

        // Ustanın sorgusu/defter notu açıkken masaya oturulmaz (önce o kapanır).
        public override bool CanInteract => base.CanInteract && !Order.LearningDirector.IsBusy &&
                                            (controller == null || controller.HasLetter);

        protected override void OnEnter()
        {
            if (controller != null) controller.OnStationEnter();
        }

        protected override void OnExit()
        {
            if (controller != null) controller.OnStationExit();
        }
    }
}
