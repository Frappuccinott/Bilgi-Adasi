using UnityEngine;

namespace BilgiAdasi.UI
{
    /// <summary>Konuşma balonunda gösterilecek tek satır.</summary>
    public readonly struct DialogueLine
    {
        public readonly string Speaker;
        public readonly string Text;
        /// <summary>2D portre; yoksa null.</summary>
        public readonly Sprite Portrait;

        public DialogueLine(string speaker, string text, Sprite portrait)
        {
            Speaker = speaker;
            Text = text;
            Portrait = portrait;
        }
    }

    /// <summary>
    /// Konuşma balonu + portre (Câbir Usta, sakinler). Satırları DialogueDirector verir.
    /// "Devam" tıklaması IDialogueCommands.Advance'ı çağırır; klavye/gamepad onayını director kendisi dinler.
    /// </summary>
    public interface IDialogueView
    {
        /// <param name="hasMore">Bu satırdan sonra başka satır var mı ("devam" / "kapat" ipucu için).</param>
        void ShowLine(DialogueLine line, bool hasMore);
        void Hide();
    }

    /// <summary>View → kod: konuşmayı bir satır ilerlet.</summary>
    public interface IDialogueCommands
    {
        void Advance();
    }
}
