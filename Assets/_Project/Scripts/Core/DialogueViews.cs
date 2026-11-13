using System;
using System.Collections.Generic;

namespace BilgiAdasi.Core
{
    /// <summary>Konuşma balonu + (isteğe bağlı) seçenekler. options null/boşsa tek "Devam" düğmesi; seçim 0 döner.</summary>
    public interface IDialogueView
    {
        void Show(string speaker, string text, IReadOnlyList<string> options, Action<int> onChosen);
        void Hide();
    }

    /// <summary>"Gerçek hayatta" kartı (02 §3.6).</summary>
    public interface IRealLifeCardView
    {
        void Show(string title, string text, Action onClosed);
    }

    /// <summary>Ustanın defteri: iki ayraç, Ustanın Notları (ipucu) + Keşiflerim (04-gorev-plani §2.1).</summary>
    public interface IHintBookView
    {
        void SetGlowing(bool glowing);
        void Open(string hintTitle, string hintText, string discoveryTitle, IReadOnlyList<string> discoveries, Action onClosed);
        bool IsOpen { get; }
    }

    /// <summary>Kısa ekran notu (mühür notu, "Biraz uzadın gibi…", sipariş alındı).</summary>
    public interface IToastView
    {
        void Show(string text, float seconds);
    }

    /// <summary>HUD'daki tek satırlık görev hedefi ("Kazanda 2 H₂O yap").</summary>
    public interface IObjectiveView
    {
        void SetObjective(string text);
    }
}
