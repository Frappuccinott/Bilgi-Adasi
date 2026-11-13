namespace BilgiAdasi.Player
{
    /// <summary>
    /// Oyuncunun gövdesi (görünüm). Bicycle binince gövdeyi seleye oturtur, ayaklar pedallara IK ile basar;
    /// inince gövde yeniden yürüyen oyuncuya bağlanır. Mantık bilmez.
    /// </summary>
    public interface IRiderBodyView
    {
        /// <param name="instant">Geçiş olmadan doğrudan otur (kayıttan devam).</param>
        void BeginRide(BicycleRig rig, bool instant);

        void EndRide();
    }
}
