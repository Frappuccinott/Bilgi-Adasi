using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Ekranda birebir konuşma kutusu (diyalog ya da ustanın sorusu) açık mı? Diyalog görünümü yazar,
    /// denetleyiciler okur (ör. yol sohbeti altyazısı bu açıkken gizlenir). Görünüme bağımlılık yaratmaz.
    /// </summary>
    public static class DialogueFocus
    {
        public static bool IsOpen { get; private set; }

        public static void Set(bool open) => IsOpen = open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => IsOpen = false;
    }
}
