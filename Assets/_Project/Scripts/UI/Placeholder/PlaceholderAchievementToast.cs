using System.Collections.Generic;
using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// TEST YER TUTUCUSU — sağ alttan kayan başarım bildirimi (simge + başlık + ad). Sıraya alır, birer birer gösterir.
    /// Zaman ölçeğinden bağımsızdır (Esc menüsü açıkken de akar). Başlık rengi ve simge sekme asset'indeki türden gelir;
    /// kutunun görünümü (renk, boyut, font, sprite, süre) Inspector'dan değişir. HUD Canvas'ı altına konması yeter.
    /// </summary>
    public sealed class PlaceholderAchievementToast : RegisteredView<IAchievementToastView>, IAchievementToastView
    {
        [Header("Zamanlama")]
        [SerializeField] private float showSeconds = 4.5f;
        [SerializeField] private float slideSeconds = 0.35f;

        [Header("Görünüm")]
        [SerializeField] private Vector2 size = new Vector2(460, 112);
        [SerializeField] private Vector2 margin = new Vector2(24, 24);
        [SerializeField] private Color frameColor = PlaceholderUi.Wood;
        [SerializeField] private Color backgroundColor = PlaceholderUi.Dim;
        [SerializeField] private Color titleColor = PlaceholderUi.Parchment;
        [SerializeField] private float headerFontSize = 24f;
        [SerializeField] private float titleFontSize = 28f;
        [Tooltip("Boş = varsayılan TMP fontu.")]
        [SerializeField] private TMP_FontAsset font;
        [Tooltip("Çerçeve ve arka plan (9-slice önerilir). Boş = düz renk.")]
        [SerializeField] private Sprite frameSprite;

        private readonly Queue<AchievementToastInfo> queue = new Queue<AchievementToastInfo>();
        private RectTransform box;
        private Image icon;
        private TMP_Text header, title;
        private float timer;
        private bool showing;

        private void Awake() => Build();

        public void ShowToast(AchievementToastInfo info)
        {
            queue.Enqueue(info);
            if (!showing) Next();
        }

        private void Next()
        {
            if (queue.Count == 0)
            {
                showing = false;
                box.gameObject.SetActive(false);
                return;
            }
            var info = queue.Dequeue();
            header.text = info.Header;
            header.color = info.Color;
            title.text = info.Title;
            icon.sprite = info.Icon;
            icon.color = info.Icon != null ? Color.white : info.Color;
            timer = 0f;
            showing = true;
            box.gameObject.SetActive(true);
            Place(0f);
        }

        private void Update()
        {
            if (!showing) return;
            timer += Time.unscaledDeltaTime;
            float total = showSeconds + 2f * slideSeconds;
            if (timer >= total)
            {
                Next();
                return;
            }
            float t = timer < slideSeconds ? timer / slideSeconds
                : timer > total - slideSeconds ? (total - timer) / slideSeconds
                : 1f;
            Place(t);
        }

        // t: 0 = ekran dışında (sağda), 1 = yerinde.
        private void Place(float t)
        {
            float eased = 1f - (1f - t) * (1f - t);
            float hiddenX = size.x + margin.x;
            box.anchoredPosition = new Vector2(Mathf.Lerp(hiddenX, -margin.x, eased), margin.y);
        }

        private void Build()
        {
            var root = (RectTransform)transform;
            box = PlaceholderUi.Panel(root, "AchievementToast", frameColor, new Vector2(1f, 0f), Vector2.zero, size);
            var inner = PlaceholderUi.Panel(box, "Inner", backgroundColor, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            PlaceholderUi.Fill(inner, 4, 4, 4, 4);
            if (frameSprite != null)
            {
                foreach (var img in new[] { box.GetComponent<Image>(), inner.GetComponent<Image>() })
                {
                    img.sprite = frameSprite;
                    img.type = frameSprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                }
            }

            float iconSize = size.y - 40f;
            icon = PlaceholderUi.Panel(inner, "Icon", Color.white, new Vector2(0f, 0.5f), new Vector2(14, 0), new Vector2(iconSize, iconSize)).GetComponent<Image>();
            icon.rectTransform.pivot = new Vector2(0f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(14, 0);
            icon.preserveAspect = true;

            float textLeft = 14 + iconSize + 18;
            header = PlaceholderUi.Text(inner, "", headerFontSize, PlaceholderUi.Gold, TextAlignmentOptions.TopLeft);
            PlaceholderUi.Fill(header.rectTransform, textLeft, 12, 12, 0);
            header.fontStyle = FontStyles.Bold;
            title = PlaceholderUi.Text(inner, "", titleFontSize, titleColor, TextAlignmentOptions.BottomLeft);
            PlaceholderUi.Fill(title.rectTransform, textLeft, 0, 12, 14);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            if (font != null)
            {
                header.font = font;
                title.font = font;
            }

            box.gameObject.SetActive(false);
        }
    }
}
