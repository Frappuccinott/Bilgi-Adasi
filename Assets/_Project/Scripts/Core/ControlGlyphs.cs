namespace BilgiAdasi.Core
{
    /// <summary>Ekranda ipucu gösterilen aksiyonlar (BilgiAdasiControls'taki aksiyonlarla eşleşir).</summary>
    public enum PromptAction
    {
        Interact,
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
        ExitStation
    }

    /// <summary>
    /// Aksiyon → tuş simgesi, son kullanılan cihazın düzenine göre (klavye / Xbox / PlayStation / Switch).
    /// Bunlar çevrilecek metin değil tuş simgeleridir; TMP zengin metni döner (renkli).
    /// PS simgeleri Liberation Sans'ta olan karakterlerle: × ○ □ ▲.
    /// Bağlamalar değişirse (input asset) bu tablo da güncellenir — CLAUDE.md §3 tablosu ile aynı.
    /// </summary>
    public static class ControlGlyphs
    {
        private enum Pad { South, East, West, North, LeftShoulder, RightShoulder, Start, Select, LeftStickPress, DpadVertical }

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
                default: return "?";
            }
        }

        private static Pad PadButton(PromptAction action)
        {
            switch (action)
            {
                case PromptAction.Interact: return Pad.West;
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
                default: return Pad.South;
            }
        }

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
                    }
                    break;

                case ControlScheme.Switch:
                    // Switch'te yüz tuşlarının harfleri Xbox'a göre yer değiştirir (güney = B).
                    switch (button)
                    {
                        case Pad.South: return Text("B");
                        case Pad.East: return Text("A");
                        case Pad.West: return Text("Y");
                        case Pad.North: return Text("X");
                        case Pad.LeftShoulder: return Text("L");
                        case Pad.RightShoulder: return Text("R");
                        case Pad.Start: return Text("+");
                        case Pad.Select: return Text("−");
                        case Pad.LeftStickPress: return Text("L-çubuk");
                        case Pad.DpadVertical: return Text("↑↓");
                    }
                    break;

                default: // Xbox ve tanınmayan gamepad'ler
                    switch (button)
                    {
                        case Pad.South: return Face("A", "#6CC24A");
                        case Pad.East: return Face("B", "#E8483A");
                        case Pad.West: return Face("X", "#3D8FE0");
                        case Pad.North: return Face("Y", "#F2C230");
                        case Pad.LeftShoulder: return Text("LB");
                        case Pad.RightShoulder: return Text("RB");
                        case Pad.Start: return Text("Menu");
                        case Pad.Select: return Text("View");
                        case Pad.LeftStickPress: return Text("LS");
                        case Pad.DpadVertical: return Text("↑↓");
                    }
                    break;
            }
            return Text("?");
        }
    }
}
