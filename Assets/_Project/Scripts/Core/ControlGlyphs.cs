using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>Tuş simgesinin çizim biçimi (UiKit.SetKeyCap): klavye kapağı, renkli yüz tuşu dairesi, hap (omuz/menü/D-pad).</summary>
    public enum GlyphShape
    {
        Key,
        Face,
        Pill
    }

    /// <summary>Ekranda ipucu gösterilen aksiyonlar (BilgiAdasiControls'taki aksiyonlarla eşleşir).</summary>
    public enum PromptAction
    {
        Interact,
        /// <summary>Kapı aç, bisiklete bin/in (Player map Use): F / X.</summary>
        Use,
        Jump,
        Crouch,
        Sprint,
        OpenMap,
        Pause,
        Confirm,
        Navigate,
        Increase,
        Decrease,
        FireStove,
        OpenHint,
        ExitStation,
        /// <summary>Atölyede (Player map) ustanın defteri: H / Y.</summary>
        WorkshopHint,
        /// <summary>Envanter slotu değiştir: tekerlek, 1–6 / LB-RB.</summary>
        CycleSlot,
        /// <summary>UI'dan çık (pazar): Esc / B.</summary>
        Cancel,
        /// <summary>UI menüsünde yukarı/aşağı gezinme (UI Navigate): ok tuşları / D-pad.</summary>
        UiVertical,
        /// <summary>UI menüsünde sola/sağa: sekme ya da ayar değeri (UI Navigate): ok tuşları / D-pad.</summary>
        UiHorizontal
    }

    /// <summary>
    /// Aksiyon → tuş simgesi, son kullanılan cihazın düzenine göre (klavye / Xbox / PlayStation / Switch).
    /// Bunlar çevrilecek metin değil tuş simgeleridir; TMP zengin metni döner (renkli).
    /// PS simgeleri Liberation Sans'ta olan karakterlerle: × ○ □ ▲.
    /// Bağlamalar değişirse (input asset) bu tablo da güncellenir — CLAUDE.md §3 tablosu ile aynı.
    /// </summary>
    public static class ControlGlyphs
    {
        private enum Pad { South, East, West, North, LeftShoulder, RightShoulder, Start, Select, LeftStickPress, DpadVertical, DpadHorizontal, Shoulders }

        public static string For(PromptAction action) => For(action, GameInput.LastScheme);

        public static string For(PromptAction action, ControlScheme scheme)
        {
            if (scheme == ControlScheme.KeyboardMouse) return Key(KeyboardLabel(action));
            return PadGlyph(PadButton(action), scheme);
        }

        private static string KeyboardLabel(PromptAction action)
        {
            switch (action)
            {
                case PromptAction.Interact: return "E";
                case PromptAction.Use: return "F";
                case PromptAction.Jump: return "Space";
                case PromptAction.Crouch: return "C";
                case PromptAction.Sprint: return "Shift";
                case PromptAction.OpenMap: return "M";
                case PromptAction.Pause: return "Esc";
                case PromptAction.Confirm: return "Enter";
                case PromptAction.Navigate: return "W/S";
                case PromptAction.Increase: return "+ / Tekerlek↑";
                case PromptAction.Decrease: return "− / Tekerlek↓";
                case PromptAction.FireStove: return "F";
                case PromptAction.OpenHint: return "H";
                case PromptAction.ExitStation: return "Esc";
                case PromptAction.WorkshopHint: return "H";
                case PromptAction.CycleSlot: return "1–6 / Tekerlek";
                case PromptAction.Cancel: return "Esc";
                case PromptAction.UiVertical: return "↑↓";
                case PromptAction.UiHorizontal: return "←→";
                default: return "?";
            }
        }

        private static Pad PadButton(PromptAction action)
        {
            switch (action)
            {
                case PromptAction.Interact: return Pad.West;
                case PromptAction.Use: return Pad.West;
                case PromptAction.Jump: return Pad.South;
                case PromptAction.Crouch: return Pad.East;
                case PromptAction.Sprint: return Pad.LeftStickPress;
                case PromptAction.OpenMap: return Pad.Select;
                case PromptAction.Pause: return Pad.Start;
                case PromptAction.Confirm: return Pad.South;
                case PromptAction.Navigate: return Pad.DpadVertical;
                case PromptAction.Increase: return Pad.RightShoulder;
                case PromptAction.Decrease: return Pad.LeftShoulder;
                case PromptAction.FireStove: return Pad.North;
                case PromptAction.OpenHint: return Pad.West;
                case PromptAction.ExitStation: return Pad.East;
                case PromptAction.WorkshopHint: return Pad.North;
                case PromptAction.CycleSlot: return Pad.Shoulders;
                case PromptAction.Cancel: return Pad.East;
                case PromptAction.UiVertical: return Pad.DpadVertical;
                case PromptAction.UiHorizontal: return Pad.DpadHorizontal;
                default: return Pad.South;
            }
        }

        /// <summary>
        /// For()'un döndürdüğü simgeyi çizim için çözer: biçim, etiket (zengin metinsiz) ve renk.
        /// Klavye "&lt;b&gt;[E]&lt;/b&gt;", yüz tuşu "&lt;b&gt;&lt;color=#..&gt;(A)&lt;/color&gt;&lt;/b&gt;", diğerleri "&lt;b&gt;(LB)&lt;/b&gt;".
        /// Tanınmayan metin olduğu gibi klavye kapağına yazılır.
        /// </summary>
        public static void Describe(string glyph, out GlyphShape shape, out string label, out Color color)
        {
            label = glyph ?? string.Empty;
            shape = GlyphShape.Key;
            color = Color.white;

            const string keyOpen = "<b>[", keyClose = "]</b>";
            const string faceOpen = "<b><color=", faceClose = ")</color></b>";
            const string pillOpen = "<b>(", pillClose = ")</b>";

            if (label.StartsWith(keyOpen) && label.EndsWith(keyClose))
            {
                label = label.Substring(keyOpen.Length, label.Length - keyOpen.Length - keyClose.Length);
            }
            else if (label.StartsWith(faceOpen) && label.EndsWith(faceClose))
            {
                int end = label.IndexOf(">(", faceOpen.Length, System.StringComparison.Ordinal);
                if (end < 0) return;
                ColorUtility.TryParseHtmlString(label.Substring(faceOpen.Length, end - faceOpen.Length), out color);
                int start = end + 2;
                label = label.Substring(start, label.Length - faceClose.Length - start);
                shape = GlyphShape.Face;
            }
            else if (label.StartsWith(pillOpen) && label.EndsWith(pillClose))
            {
                label = label.Substring(pillOpen.Length, label.Length - pillOpen.Length - pillClose.Length);
                shape = GlyphShape.Pill;
            }
        }

        // Xbox yüz tuşu renkleri: Figma "Glyph/Gamepad" bileşeni.
        private const string XboxA = "#6CBE3A", XboxB = "#E9622C", XboxX = "#4FC6E6", XboxY = "#F7CB45";
        // Switch yüz tuşları renksizdir: kâğıt tonunda halka + harf.
        private const string SwitchFace = "#E9E1D0";

        private static string Key(string label) => "<b>[" + label + "]</b>";

        private static string Face(string symbol, string color) => "<b><color=" + color + ">(" + symbol + ")</color></b>";

        private static string Text(string label) => "<b>(" + label + ")</b>";

        private static string PadGlyph(Pad button, ControlScheme scheme)
        {
            switch (scheme)
            {
                case ControlScheme.PlayStation:
                    switch (button)
                    {
                        case Pad.South: return Face("×", "#7CB2E8");
                        case Pad.East: return Face("○", "#FF6B6B");
                        case Pad.West: return Face("□", "#E59BD0");
                        case Pad.North: return Face("▲", "#40D9A0");
                        case Pad.LeftShoulder: return Text("L1");
                        case Pad.RightShoulder: return Text("R1");
                        case Pad.Start: return Text("Options");
                        case Pad.Select: return Text("Create");
                        case Pad.LeftStickPress: return Text("L3");
                        case Pad.DpadVertical: return Text("↑↓");
                        case Pad.DpadHorizontal: return Text("←→");
                        case Pad.Shoulders: return Text("L1/R1");
                    }
                    break;

                case ControlScheme.Switch:
                    // Switch'te yüz tuşlarının harfleri Xbox'a göre yer değiştirir (güney = B).
                    switch (button)
                    {
                        case Pad.South: return Face("B", SwitchFace);
                        case Pad.East: return Face("A", SwitchFace);
                        case Pad.West: return Face("Y", SwitchFace);
                        case Pad.North: return Face("X", SwitchFace);
                        case Pad.LeftShoulder: return Text("L");
                        case Pad.RightShoulder: return Text("R");
                        case Pad.Start: return Text("+");
                        case Pad.Select: return Text("−");
                        case Pad.LeftStickPress: return Text("L-çubuk");
                        case Pad.DpadVertical: return Text("↑↓");
                        case Pad.DpadHorizontal: return Text("←→");
                        case Pad.Shoulders: return Text("L/R");
                    }
                    break;

                default: // Xbox ve tanınmayan gamepad'ler
                    switch (button)
                    {
                        case Pad.South: return Face("A", XboxA);
                        case Pad.East: return Face("B", XboxB);
                        case Pad.West: return Face("X", XboxX);
                        case Pad.North: return Face("Y", XboxY);
                        case Pad.LeftShoulder: return Text("LB");
                        case Pad.RightShoulder: return Text("RB");
                        case Pad.Start: return Text("Menu");
                        case Pad.Select: return Text("View");
                        case Pad.LeftStickPress: return Text("LS");
                        case Pad.DpadVertical: return Text("↑↓");
                        case Pad.DpadHorizontal: return Text("←→");
                        case Pad.Shoulders: return Text("LB/RB");
                    }
                    break;
            }
            return Text("?");
        }
    }
}
