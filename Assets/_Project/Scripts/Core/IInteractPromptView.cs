namespace BilgiAdasi.Core
{
    /// <summary>
    /// Ekran ortasındaki nokta (crosshair) ve etkileşim ipucu: "[E] Kazana geç" / "Ⓐ Kazana geç".
    /// Hangi tuş simgesinin çizileceğine view karar verir (device parametresine göre).
    /// </summary>
    public interface IInteractPromptView
    {
        void ShowPrompt(string text, InputDeviceKind device);
        void HidePrompt();
        void SetCrosshairVisible(bool visible);
    }
}
