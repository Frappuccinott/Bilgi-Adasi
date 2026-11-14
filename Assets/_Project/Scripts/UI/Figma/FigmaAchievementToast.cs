using System.Collections.Generic;
using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Başarım bildirimi — Figma: "Keşif Defteri ve Başarılar" sayfası, UI/Başarım bildirimi (Tür=Görev/Köy/Meydan okuma)
    /// ve "Başarım bildirimi/HUD içinde". Kâğıt şerit + ahşap çerçeve, solda mum mühür (üstünde başarım simgesi),
    /// sağda başlık (tür rengi) / ad / açıklama, altta tür renginde boşalan süre çubuğu.
    /// Hareket: sağdan kayar (0,35 sn), mühür basılır (1,35× → 1×, −8° → 0°), 4,5 sn bekler, sağa kayarak çıkar.
    /// Görev sonu özeti (görev bildiriminde): ince çizginin altında öğrenilen konu ve "Hatasız · Defter açılmadan";
    /// özet varken kart biraz uzar (genişlik aynı) ve bildirim daha uzun kalır.
    /// Sıraya alır; zaman ölçeğinden bağımsızdır. HUD Canvas'ı altına tam ekran (stretch) konur.
    /// </summary>
    public sealed class FigmaAchievementToast : FigmaView<IAchievementToastView>, IAchievementToastView
    {
        [Header("Zamanlama")]
        [SerializeField] private float showSeconds = 4.5f;
        [SerializeField] private float slideSeconds = 0.35f;
        [SerializeField] private float stampDelay = 0.15f;
        [SerializeField] private float stampSeconds = 0.3f;

        [Header("Yerleşim (1920×1080 referans)")]
        [SerializeField] private Vector2 size = new Vector2(640, 170);
        [Tooltip("Sağ ve alt kenardan uzaklık. Alt: envanter şeridinin (alttan ~135) ve tuş ipuçlarının üstünde kalır.")]
        [SerializeField] private Vector2 margin = new Vector2(72, 210);
        [Tooltip("Görev sonu özeti varken kartın boyu (normal kart + konu ve hata/defter satırı).")]
        [SerializeField] private Vector2 summarySize = new Vector2(600, 196);
        [Tooltip("Özetli bildirim bu kadar daha uzun kalır (sn): okumaya zaman.")]
        [SerializeField] private float summaryExtraSeconds = 3f;

        [Header("Renk")]
        [Tooltip("Kapalı: renk türden gelir (Görev altın, Köy mühür, Meydan okuma su — marka paleti). Açık: sekme asset'indeki tür rengi.")]
        [SerializeField] private bool useAssetColor;

        [Tooltip("Bildirim, ekran bu kadar süre konuşmasız kalınca başlar (sn). Görev sonu usta sorusu ~0.6 sn sonra açılır.")]
        [SerializeField] private float startDelay = 1f;

        private readonly Queue<AchievementToastInfo> queue = new Queue<AchievementToastInfo>();
        private RectTransform box, sealRoot, barFill;
        private Image sealImage, iconImage, barImage;
        private TextMeshProUGUI header, title, description, topic, stats;
        private GameObject summaryRule;
        private float timer, quietTime, holdSeconds;
        private bool showing;
        private AchievementToastInfo current;
        private bool hasPaused;           // konuşma açılınca geri çekilen bildirim: önce o yeniden gösterilir
        private AchievementToastInfo paused;

        private void Awake() => Build();

        protected override void OnEnable()
        {
            base.OnEnable();
            FigmaDialogueView.VisibilityChanged += OnDialogue;
        }

        protected override void OnDisable()
        {
            FigmaDialogueView.VisibilityChanged -= OnDialogue;
            base.OnDisable();
        }

        public void ShowToast(AchievementToastInfo info)
        {
            queue.Enqueue(info);
            // Başlatma Update'te: ekran startDelay kadar konuşmasız kalınca.
        }

        // Konuşma (ya da ustanın sorusu) açılınca ekrandaki bildirim geri çekilir; konuşma bitince baştan gösterilir.
        private void OnDialogue(bool visible)
        {
            quietTime = 0f;
            if (!visible || !showing) return;
            paused = current;
            hasPaused = true;
            showing = false;
            box.gameObject.SetActive(false);
        }

        private Color AccentFor(in AchievementToastInfo info)
        {
            if (useAssetColor && info.Color.a > 0f) return info.Color;
            var th = Theme;
            switch (info.Kind)
            {
                case AchievementKind.Milestone: return th.WaxMid;
                case AchievementKind.Challenge: return th.WaterDark;
                default: return th.GoldDark;
            }
        }

        private void Next()
        {
            if ((queue.Count == 0 && !hasPaused) || FigmaDialogueView.IsVisible)
            {
                showing = false;
                box.gameObject.SetActive(false);
                return;
            }
            AchievementToastInfo info;
            if (hasPaused)
            {
                info = paused;
                hasPaused = false;
            }
            else info = queue.Dequeue();
            current = info;
            var accent = AccentFor(info);
            header.text = info.Header;
            header.color = accent;
            title.text = info.Title;
            description.text = info.Description;
            description.gameObject.SetActive(!string.IsNullOrEmpty(info.Description));
            barImage.color = accent;

            // Görev sonu özeti
            bool summary = info.HasSummary;
            box.sizeDelta = summary ? summarySize : size;
            holdSeconds = showSeconds + (summary ? summaryExtraSeconds : 0f);
            title.fontSize = summary ? 34f : 40f;
            description.fontSize = summary ? 24f : 28f;
            summaryRule.SetActive(summary);
            topic.text = info.Topic;
            topic.color = accent;
            topic.gameObject.SetActive(!string.IsNullOrEmpty(info.Topic));
            stats.text = info.Stats;
            stats.gameObject.SetActive(!string.IsNullOrEmpty(info.Stats));

            var th = Theme;
            bool hasIcon = info.Icon != null;
            // Simge varsa mum gövdesi + simge; yoksa Figma'daki tam mühür.
            sealImage.sprite = hasIcon ? (th.SealWax != null ? th.SealWax : UiKit.Disc) : (th.Seal != null ? th.Seal : UiKit.Disc);
            sealImage.color = sealImage.sprite == UiKit.Disc ? th.WaxMid : Color.white;
            iconImage.sprite = info.Icon;
            iconImage.gameObject.SetActive(hasIcon);

            timer = 0f;
            showing = true;
            box.gameObject.SetActive(true);
            Animate();
        }

        private void Update()
        {
            if (!showing)
            {
                if (queue.Count == 0 && !hasPaused) return;
                if (FigmaDialogueView.IsVisible) { quietTime = 0f; return; }
                quietTime += Time.unscaledDeltaTime;
                if (quietTime >= startDelay) Next();
                return;
            }
            timer += Time.unscaledDeltaTime;
            if (timer >= holdSeconds + 2f * slideSeconds)
            {
                // Sıradaki de aynı sessizlik beklemesiyle başlar (arka arkaya bildirimler arası kısa ara).
                showing = false;
                quietTime = startDelay * 0.5f;
                box.gameObject.SetActive(false);
                return;
            }
            Animate();
        }

        private void Animate()
        {
            float total = holdSeconds + 2f * slideSeconds;
            float slide = timer < slideSeconds ? timer / slideSeconds
                : timer > total - slideSeconds ? (total - timer) / slideSeconds
                : 1f;
            float eased = 1f - (1f - slide) * (1f - slide);
            box.anchoredPosition = new Vector2(Mathf.Lerp(box.sizeDelta.x + margin.x + 24f, -margin.x, eased), margin.y);

            float s = Mathf.Clamp01((timer - stampDelay) / stampSeconds);
            float stamp = 1f - (1f - s) * (1f - s) * (1f - s);
            sealRoot.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, stamp);
            sealRoot.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(8f, 0f, stamp));

            float remain = 1f - Mathf.Clamp01((timer - slideSeconds) / holdSeconds);
            barFill.anchorMax = new Vector2(remain, 1f);
        }

        private void Build()
        {
            var th = Theme;
            box = UiKit.PaperCard(transform, "AchievementToast", th, 6f, out var paper);
            UiKit.Place(box, new Vector2(1f, 0f), Vector2.zero, size);

            // Mühür (96 px), sol iç boşluk 22
            sealRoot = UiKit.Rect(paper, "Seal");
            UiKit.Place(sealRoot, new Vector2(0f, 0.5f), new Vector2(22f, 3f), new Vector2(96f, 96f));
            sealRoot.pivot = new Vector2(0.5f, 0.5f);
            sealRoot.anchoredPosition = new Vector2(22f + 48f, 3f);
            sealImage = UiKit.Img(sealRoot, "Wax", Color.white);
            UiKit.Fill(sealImage.rectTransform);
            sealImage.preserveAspect = true;
            iconImage = UiKit.Img(sealRoot, "Icon", Color.white);
            UiKit.Fill(iconImage.rectTransform, 24, 24, 24, 24);
            iconImage.preserveAspect = true;

            // Metin sütunu: başlık 28 / ad 40 / açıklama 28
            var col = UiKit.Rect(paper, "Text");
            UiKit.Fill(col, 22f + 96f + 22f, 14f, 32f, 18f);
            var layout = col.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 0f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            header = Line(col, "Header", th.BodyFont, 28f, th.GoldDark, FontStyles.Bold);
            title = Line(col, "Title", th.HeadingFont, 40f, th.InkDark, FontStyles.Bold);
            description = Line(col, "Description", th.BodyFont, 28f, th.PaperShade, FontStyles.Normal);

            // Görev sonu özeti: ince çizgi, konu (tür renginde), hata · defter
            var rule = UiKit.Img(col, "SummaryRule", th.PaperMid);
            var ruleLayout = rule.gameObject.AddComponent<LayoutElement>();
            ruleLayout.minHeight = ruleLayout.preferredHeight = 2f;
            summaryRule = rule.gameObject;
            topic = Line(col, "Topic", th.BodyFont, 24f, th.GoldDark, FontStyles.Bold);
            stats = Line(col, "Stats", th.BodyFont, 20f, th.PaperShade, FontStyles.Italic);
            summaryRule.SetActive(false);
            topic.gameObject.SetActive(false);
            stats.gameObject.SetActive(false);
            holdSeconds = showSeconds;

            // Süre çubuğu: kâğıdın alt kenarı, 6 px
            var track = UiKit.Img(paper, "Timer", th.PaperMid);
            var trt = track.rectTransform;
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 0f);
            trt.pivot = new Vector2(0.5f, 0f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = new Vector2(0f, 6f);
            barImage = UiKit.Img(trt, "Remaining", th.GoldDark);
            barFill = UiKit.Fill(barImage.rectTransform);

            box.gameObject.SetActive(false);
        }

        private static TextMeshProUGUI Line(RectTransform parent, string name, TMP_FontAsset font, float size, Color color, FontStyles style)
        {
            var t = UiKit.Text(parent, name, font, size, color, TextAlignmentOptions.Left, style);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.lineSpacing = -8f;
            // Uzun açıklama düzende başlık ve adı ezmesin (ezilen satır üç noktayla tamamen kaybolur).
            var le = t.gameObject.AddComponent<LayoutElement>();
            // Fontun gerçek satır yüksekliği (Alegreya Bold 40 ≈ 54 px): daha azı verilirse TMP satırı tümden siler.
            le.minHeight = le.preferredHeight = t.GetPreferredValues("ÇĞgy").y + 2f;
            return t;
        }
    }
}
