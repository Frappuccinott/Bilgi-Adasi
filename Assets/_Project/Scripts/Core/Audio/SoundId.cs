namespace BilgiAdasi.Core
{
    /// <summary>
    /// Oyundaki tek seferlik seslerin kimlikleri. Klipler <see cref="SoundLibrary"/>'de, çalan <see cref="AudioDirector"/>.
    /// Yeni ses: buraya ekle, kütüphanede klibini bağla, olayı <c>GameEvents.RaiseSound…</c> ile yayınla.
    /// </summary>
    public enum SoundId
    {
        None = 0,

        // Kazan ve ocak
        Pour = 10,
        Drain = 11,
        Ignite = 12,
        Boil = 13,
        ReactionSuccess = 14,
        Foam = 15,

        // Dünya
        Jar = 20,
        Coins = 21,
        CoinSingle = 22,
        DoorOpen = 23,
        DoorClose = 24,

        // Arayüz
        UiNavigate = 40,
        UiConfirm = 41,
        UiBack = 42,
        UiSlot = 43,
        PageTurn = 44,
        BookOpen = 45,
        BookClose = 46,
        MapOpen = 47,
        DialogueAdvance = 48,
        Pause = 49,

        // Geri bildirim
        Correct = 60,
        Wrong = 61,
        Info = 62,
        Achievement = 63,
        NeighborhoodComplete = 64,
    }
}
