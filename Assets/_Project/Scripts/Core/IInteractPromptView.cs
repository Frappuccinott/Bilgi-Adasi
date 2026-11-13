namespace BilgiAdasi.Core
{
    /// <summary>
    /// Ekran ortasındaki nokta (crosshair) ve etkileşim ipucu: "[E] Kazana geç" / "(X) Kazana geç" / "(□) Kazana geç".
    /// glyph, son kullanılan cihaza göre ControlGlyphs'ten gelir (TMP zengin metin); view yalnızca çizer.
    /// </summary>
    public interface IInteractPromptView
    {
        void ShowPrompt(string text, string glyph);
        void HidePrompt();
        void SetCrosshairVisible(bool visible);
    }
}
