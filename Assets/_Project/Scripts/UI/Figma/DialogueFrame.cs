using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>HUD yerleşim ölçüleri (1920×1080): diyalog, sol üstteki görev kartının sağ kenarından başlar.</summary>
    public static class HudLayout
    {
        /// <summary>Görev kartı: sol kenar boşluğu ve genişliği (FigmaOrderCard).</summary>
        public const float OrderCardLeft = 28f, OrderCardWidth = 400f;

        /// <summary>
        /// Diyalog (portre dahil) buradan başlar: kartın bittiği yer + boşluk. Kart görevle aşağı uzasa da
        /// diyalogla kesişmez (karar 14.11).
        /// </summary>
        public const float DialogueLeft = OrderCardLeft + OrderCardWidth + 36f;
    }

    /// <summary>
    /// Diyalog çerçevesi — Figma: Ekran/Diyalog. Normal konuşma (FigmaDialogueView) ve ustanın sorusu
    /// (FigmaReasoningView) aynı çerçeveyi kurar, birebir aynı görünür: alttan koyulaşan mürekkep geçişi; görev
    /// kartının sağından başlayan yuvarlak portre (kâğıt çerçeve); sağında altın konuşan adı, kâğıt renginde metin;
    /// metnin altında (sorularda) seçenekler; sağ altta tuş kapağı + kelime. Sütun alttan yukarı büyür, portre
    /// sütunun üstüne hizalı kalır.
    /// </summary>
    public sealed class DialogueFrame
    {
        public const float PortraitSize = 108f, Gap = 28f, Bottom = 96f, TextWidth = 1060f, FadeHeight = 560f;

        public RectTransform Root, Column, HintRoot, HintKeyRoot;
        public GameObject PortraitFrame;
        public Image Portrait;
        public TextMeshProUGUI Speaker, Body, HintKey, HintWord;
        public Button Catcher;

        public static DialogueFrame Build(Transform parent, string name, UiTheme th)
        {
            var f = new DialogueFrame();
            f.Root = UiKit.Fill(UiKit.Rect(parent, name));

            // Ekranın tamamı: arkaya tıklanmasın (konuşmada tıklama ilerletir).
            var catcher = UiKit.Img(f.Root, "ClickCatcher", new Color(0, 0, 0, 0), null, true);
            UiKit.Fill(catcher.rectTransform);
            f.Catcher = catcher.gameObject.AddComponent<Button>();
            f.Catcher.transition = Selectable.Transition.None;

            var fade = UiKit.Img(f.Root, "BottomFade", UiKit.WithAlpha(th.InkDeep, 0.88f), UiKit.FadeUp);
            var frt = fade.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(1f, 0f);
            frt.pivot = new Vector2(0.5f, 0f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = new Vector2(0f, FadeHeight);

            // Sütun: konuşan, metin, (seçenekler) — alttan yukarı büyür; portre sütunun solunda, üstüne hizalı.
            float columnX = HudLayout.DialogueLeft + PortraitSize + Gap;
            f.Column = UiKit.Rect(f.Root, "Column");
            f.Column.anchorMin = f.Column.anchorMax = Vector2.zero;
            f.Column.pivot = Vector2.zero;
            f.Column.anchoredPosition = new Vector2(columnX, Bottom);
            f.Column.sizeDelta = new Vector2(Mathf.Min(TextWidth, 1920f - columnX - 260f), 0f);
            var col = f.Column.gameObject.AddComponent<VerticalLayoutGroup>();
            col.spacing = 10f;
            col.childControlWidth = col.childControlHeight = true;
            col.childForceExpandWidth = true;
            col.childForceExpandHeight = false;
            f.Column.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            f.Speaker = UiKit.Text(f.Column, "Speaker", th.BodyFont, 24f, th.GoldLight, TextAlignmentOptions.Left, FontStyles.Bold);
            f.Body = UiKit.Text(f.Column, "Body", th.BodyFont, 31f, th.Paper);
            foreach (var t in new[] { f.Speaker, f.Body }) Shade(t, th);

            // Portre: 108 px daire, kâğıt çerçeve; sütunun sol üstüne bağlı (sütun uzayınca birlikte yükselir).
            var pf = UiKit.Img(f.Column, "PortraitFrame", th.Paper, UiKit.Disc);
            pf.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var prt = pf.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(0f, 1f);
            prt.pivot = new Vector2(1f, 1f);
            prt.sizeDelta = new Vector2(PortraitSize, PortraitSize);
            prt.anchoredPosition = new Vector2(-Gap, 6f);
            f.PortraitFrame = pf.gameObject;
            var ring = UiKit.Img(pf.transform, "Ring", th.GoldDark, UiKit.Ring);
            UiKit.Fill(ring.rectTransform, -3, -3, -3, -3);
            var mask = UiKit.Img(pf.transform, "Mask", Color.white, UiKit.Disc);
            UiKit.Fill(mask.rectTransform, 5, 5, 5, 5);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            f.Portrait = UiKit.Img(mask.transform, "Portrait", Color.white);
            UiKit.Fill(f.Portrait.rectTransform, -12, -6, -12, -18);
            f.Portrait.preserveAspect = true;

            // Sağ alt: tuş kapağı + kelime (Devam / Kapat / Seç)
            f.HintRoot = UiKit.Rect(f.Root, "Hint");
            UiKit.Place(f.HintRoot, new Vector2(1f, 0f), new Vector2(-72f, 56f), new Vector2(0f, 40f));
            var row = f.HintRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 10f;
            row.childAlignment = TextAnchor.MiddleRight;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            f.HintRoot.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            f.HintKey = UiKit.KeyCap(f.HintRoot, th, 40f, out f.HintKeyRoot);
            f.HintWord = UiKit.Text(f.HintRoot, "Word", th.BodyFont, 26f, th.Paper, TextAlignmentOptions.Left);
            f.HintWord.textWrappingMode = TextWrappingModes.NoWrap;
            return f;
        }

        /// <summary>Konuşanı ve portresini yazar; portre yoksa defterden (PortraitBook), o da yoksa çerçeve gizlenir.</summary>
        public void SetSpeaker(string name, Sprite portrait)
        {
            Speaker.text = name;
            var p = PortraitBook.Resolve(name, portrait);
            Portrait.sprite = p;
            PortraitFrame.SetActive(p != null);
        }

        public static void Shade(TextMeshProUGUI t, UiTheme th)
        {
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = UiKit.WithAlpha(th.InkDeep, 0.6f);
            sh.effectDistance = new Vector2(0f, -2f);
        }
    }
}
