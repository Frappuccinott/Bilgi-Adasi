namespace BilgiAdasi.Desk
{
    // Masa görünüm arayüzleri. Mustafa'nın view'ları bunları uygular (RegisteredView<IArayüz>);
    // DeskController yalnızca bu arayüzleri tanır.

    /// <summary>Kâğıdın başı: başlık, gönderen, mektup.</summary>
    public readonly struct PaperHeader
    {
        public readonly string Title;
        public readonly string FromLabel;
        public readonly string From;
        /// <summary>Kâğıdın başındaki mektup (giverLines). Köyün ilk görevinde ustanın tanıtımı başa eklenir.</summary>
        public readonly string Letter;
        public readonly string SubmitWord;
        public readonly bool Guild;

        public PaperHeader(string title, string fromLabel, string from, string letter, string submitWord, bool guild)
        {
            Title = title;
            FromLabel = fromLabel;
            From = from;
            Letter = letter;
            SubmitWord = submitWord;
            Guild = guild;
        }
    }

    public enum PaperNoteKind
    {
        Info,
        Wrong,
        Correct
    }

    /// <summary>
    /// Masadaki kâğıt (HUD). Odak: satır sırası; Rows.Count = "Gönder" düğmesi.
    /// Yanlışta lekeli satırlar PaperRow.Wrong'dan okunur.
    /// </summary>
    public interface IPaperView
    {
        void Open(PaperHeader header);
        void ShowQuestion(PaperQuestion question, string counter);
        /// <summary>Seçimler ya da odak değişti.</summary>
        void Refresh(PaperQuestion question, int focus);
        void ShowNote(string text, PaperNoteKind kind);
        void ShowHint(string title, string text);
        void HideHint();
        bool IsHintOpen { get; }
        /// <summary>Bütün sorular doğru: mühür basılır. Devam düğmesi IPaperCommands.Continue'yu çağırır.</summary>
        void ShowSeal(string text, string continueWord, bool guild);
        void Close();
    }

    /// <summary>View → kod (fare tıklamaları). Gamepad/klavye girdisini denetleyici kendisi dinler.</summary>
    public interface IPaperCommands
    {
        void Focus(int row);
        void SelectOption(int row, int option);
        void StepOption(int row, int delta);
        void Submit();
        void Continue();
    }

    /// <summary>Masa çevresindeki 3D: pencereye konan güvercin, masadaki zarf, açık kâğıt.</summary>
    public interface IDeskPropsView
    {
        /// <summary>Güvercin pencereye gelir; zarf masaya bırakılır.</summary>
        void PlayArrival(bool guild);
        /// <summary>Zarf masada görünür mü (kâğıt açıkken gizli).</summary>
        void SetEnvelope(bool visible, bool guild);
        /// <summary>Masada açık kâğıt (istasyondayken).</summary>
        void SetPaperOpen(bool open);
        /// <summary>Görev bitti: güvercin cevabı alıp uçar.</summary>
        void PlayDeparture();
    }
}
