using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>TEST YER TUTUCUSU — ham UI kurucuları (başarım bildirimi ve Esc menüsü için). Mustafa'nınkiyle silinir.</summary>
    internal static class PlaceholderUi
    {
        public static readonly Color Parchment = new Color(0.91f, 0.85f, 0.71f, 0.97f);
        public static readonly Color Ink = new Color(0.23f, 0.16f, 0.10f, 1f);
        public static readonly Color Wood = new Color(0.33f, 0.22f, 0.13f, 0.95f);
        public static readonly Color WoodLight = new Color(0.55f, 0.40f, 0.24f, 0.95f);
        public static readonly Color Gold = new Color(0.97f, 0.66f, 0.0f, 1f);
        public static readonly Color Dim = new Color(0.18f, 0.13f, 0.09f, 0.95f);

        public static RectTransform Panel(RectTransform parent, string name, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }

        public static TMP_Text Text(RectTransform parent, string text, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            return t;
        }

        public static Button MakeButton(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick, Color color, float height)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            go.GetComponent<LayoutElement>().preferredHeight = height;
            var b = go.GetComponent<Button>();
            if (onClick != null) b.onClick.AddListener(onClick);
            SetHighlight(b);
            var t = Text((RectTransform)go.transform, label, 30, Ink, TextAlignmentOptions.Center);
            Fill(t.rectTransform, 12, 0, 12, 0);
            return b;
        }

        public static void SetHighlight(Selectable s)
        {
            var colors = s.colors;
            colors.highlightedColor = new Color(1.1f, 1.05f, 0.85f, 1f);
            colors.selectedColor = new Color(1.2f, 1.0f, 0.55f, 1f);
            s.colors = colors;
        }

        public static void Fill(RectTransform rt, float left, float top, float right, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
