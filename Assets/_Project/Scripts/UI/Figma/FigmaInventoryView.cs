using System.Collections.Generic;
using System.Text;
using BilgiAdasi.Order;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Envanter (6 slot) + kese — marka dili: koyu deri tepsi içinde ahşap çerçeveli yuvalar, seçili yuva altın
    /// çerçeveyle yükselir ve üstünde maddenin adı yazar; ikonlar Mustafa'nın Malzeme_* çizimleri (yoksa kavanoz
    /// renginde disk). Kese sağ üstte: kâğıt plaka + gümüş akçe (Figma UI/Akçe); değişince sikke zıplar, "+10" süzülür.
    /// Konuşma açıkken (FigmaDialogueView.VisibilityChanged) slotlar yumuşakça gizlenir, bitince geri gelir.
    /// </summary>
    public sealed class FigmaInventoryView : FigmaView<IInventoryView>, IInventoryView
    {
        [SerializeField] private string currencyWord = "akçe";

        [Header("Yerleşim (1920×1080)")]
        [SerializeField] private float slotSize = 84f;
        [SerializeField] private float slotGap = 10f;
        [SerializeField] private float bottom = 24f;
        [SerializeField] private float raise = 8f;
        [SerializeField] private Vector2 purseMargin = new Vector2(28f, 28f);
        [SerializeField] private float fadeSeconds = 0.2f;

        private const int Slots = 6;
        private readonly RectTransform[] slotRoots = new RectTransform[Slots];
        private readonly Image[] frames = new Image[Slots];
        private readonly Image[] trays = new Image[Slots];
        private readonly Image[] icons = new Image[Slots];
        private readonly TextMeshProUGUI[] numbers = new TextMeshProUGUI[Slots];
        private readonly TextMeshProUGUI[] counts = new TextMeshProUGUI[Slots];
        private readonly float[] lift = new float[Slots];
        private CanvasGroup barGroup;
        private TextMeshProUGUI selectedLabel, amount, delta;
        private RectTransform coin, deltaRt;
        private int selected = -1, lastMoney = int.MinValue;
        private float pulse, deltaTimer = -1f, barTarget = 1f;
        private readonly StringBuilder sb = new StringBuilder(64);

        private void Awake() => Build();

        protected override void OnEnable()
        {
            base.OnEnable();
            FigmaDialogueView.VisibilityChanged += OnDialogue;
            OnDialogue(FigmaDialogueView.IsVisible);
            if (barGroup != null) barGroup.alpha = barTarget;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            FigmaDialogueView.VisibilityChanged -= OnDialogue;
        }

        private void OnDialogue(bool open) => barTarget = open ? 0f : 1f;

        public void ShowInventory(IReadOnlyList<InventorySlot> slots, int selectedIndex, int money)
        {
            var th = Theme;
            selected = selectedIndex;
            for (int i = 0; i < Slots; i++)
            {
                bool has = i < slots.Count && !slots[i].IsEmpty;
                bool sel = i == selectedIndex;
                frames[i].color = sel ? th.Gold : th.WoodDark;
                trays[i].color = sel ? UiKit.WithAlpha(th.WoodDark, 0.95f) : UiKit.WithAlpha(th.InkDeep, has ? 0.88f : 0.6f);
                numbers[i].color = UiKit.WithAlpha(sel ? th.GoldLight : th.Paper, sel ? 1f : 0.55f);
                icons[i].gameObject.SetActive(has);
                counts[i].gameObject.SetActive(has && slots[i].Count > 1);
                if (!has) continue;
                var species = slots[i].Species;
                var sprite = th.IconFor(species);
                icons[i].sprite = sprite != null ? sprite : UiKit.Disc;
                icons[i].color = sprite != null ? Color.white : species.JarColor;
                icons[i].rectTransform.sizeDelta = sprite != null ? new Vector2(slotSize - 22f, slotSize - 22f) : new Vector2(slotSize * 0.42f, slotSize * 0.42f);
                counts[i].text = "×" + slots[i].Count;
            }

            bool selHas = selectedIndex >= 0 && selectedIndex < slots.Count && !slots[selectedIndex].IsEmpty;
            if (selHas)
            {
                var s = slots[selectedIndex].Species;
                sb.Clear();
                if (!string.IsNullOrEmpty(s.DisplayName)) sb.Append(s.DisplayName).Append("  <color=#").Append(ColorUtility.ToHtmlStringRGB(th.GoldLight)).Append('>');
                sb.Append(s.FormulaRich);
                if (!string.IsNullOrEmpty(s.DisplayName)) sb.Append("</color>");
                selectedLabel.text = sb.ToString();
            }
            selectedLabel.gameObject.SetActive(selHas);

            if (money != lastMoney)
            {
                if (lastMoney != int.MinValue)
                {
                    int d = money - lastMoney;
                    delta.text = d > 0 ? "+" + d : "−" + (-d);
                    delta.color = d > 0 ? th.GoldDark : th.WaxMid;
                    deltaTimer = 0f;
                    pulse = 1f;
                }
                lastMoney = money;
                amount.text = money.ToString();
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            barGroup.alpha = Mathf.MoveTowards(barGroup.alpha, barTarget, dt / Mathf.Max(0.01f, fadeSeconds));
            barGroup.blocksRaycasts = barGroup.alpha > 0.5f;

            for (int i = 0; i < Slots; i++)
            {
                lift[i] = Mathf.MoveTowards(lift[i], i == selected ? raise : 0f, dt * 80f);
                slotRoots[i].anchoredPosition = new Vector2(slotRoots[i].anchoredPosition.x, lift[i]);
            }

            if (pulse > 0f)
            {
                pulse = Mathf.MoveTowards(pulse, 0f, dt / 0.35f);
                float s = 1f + 0.18f * Mathf.Sin(pulse * Mathf.PI);
                coin.localScale = new Vector3(s, s, 1f);
            }
            if (deltaTimer >= 0f)
            {
                deltaTimer += dt;
                float t = deltaTimer / 1.2f;
                if (t >= 1f)
                {
                    deltaTimer = -1f;
                    delta.gameObject.SetActive(false);
                }
                else
                {
                    delta.gameObject.SetActive(true);
                    deltaRt.anchoredPosition = new Vector2(-12f, -82f - 24f * t);
                    var c = delta.color;
                    c.a = 1f - t * t;
                    delta.color = c;
                }
            }
        }

        private void Build()
        {
            var th = Theme;

            // ---- Slot şeridi (alt orta) ----
            float barW = Slots * slotSize + (Slots - 1) * slotGap;
            var bar = UiKit.Rect(transform, "Inventory");
            UiKit.Place(bar, new Vector2(0.5f, 0f), new Vector2(0f, bottom), new Vector2(barW, slotSize + raise + 50f));
            barGroup = bar.gameObject.AddComponent<CanvasGroup>();

            // Deri tepsi: slotların arkasında, kenarları yumuşak
            var tray = UiKit.Img(bar, "Tray", UiKit.WithAlpha(th.InkDeep, 0.55f), UiKit.Round8);
            UiKit.Place(tray.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -10f), new Vector2(barW + 28f, slotSize + 20f));

            selectedLabel = UiKit.Text(bar, "Selected", th.HeadingFont, 28f, th.Paper, TextAlignmentOptions.Bottom, FontStyles.Bold);
            UiKit.Place(selectedLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, slotSize + raise + 6f), new Vector2(barW + 200f, 40f));
            selectedLabel.textWrappingMode = TextWrappingModes.NoWrap;
            var sh = selectedLabel.gameObject.AddComponent<Shadow>();
            sh.effectColor = UiKit.WithAlpha(th.InkDeep, 0.8f);
            sh.effectDistance = new Vector2(0f, -2f);

            for (int i = 0; i < Slots; i++)
            {
                var root = UiKit.Rect(bar, "Slot" + (i + 1));
                root.anchorMin = root.anchorMax = root.pivot = new Vector2(0f, 0f);
                root.sizeDelta = new Vector2(slotSize, slotSize);
                root.anchoredPosition = new Vector2(i * (slotSize + slotGap), 0f);
                slotRoots[i] = root;

                var shadow = UiKit.Img(root, "Shadow", UiKit.WithAlpha(th.InkDeep, 0.5f), UiKit.Round8);
                UiKit.Fill(shadow.rectTransform, 0, 5, 0, -5);
                frames[i] = UiKit.Img(root, "Frame", th.WoodDark, UiKit.Round8);
                UiKit.Fill(frames[i].rectTransform);
                trays[i] = UiKit.Img(frames[i].transform, "Tray", th.InkDeep, UiKit.Round8);
                UiKit.Fill(trays[i].rectTransform, 4, 4, 4, 4);

                icons[i] = UiKit.Img(trays[i].transform, "Icon", Color.white);
                UiKit.Place(icons[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(slotSize - 22f, slotSize - 22f));
                icons[i].preserveAspect = true;

                numbers[i] = UiKit.Text(trays[i].transform, "Number", th.BodyFont, 18f, th.Paper, TextAlignmentOptions.TopLeft, FontStyles.Bold);
                UiKit.Fill(numbers[i].rectTransform, 6, 2, 0, 0);
                numbers[i].text = (i + 1).ToString();

                counts[i] = UiKit.Text(trays[i].transform, "Count", th.BodyFont, 22f, th.Paper, TextAlignmentOptions.BottomRight, FontStyles.Bold);
                UiKit.Fill(counts[i].rectTransform, 0, 0, 6, 2);
                var cs = counts[i].gameObject.AddComponent<Shadow>();
                cs.effectColor = UiKit.WithAlpha(th.InkDeep, 0.9f);
                cs.effectDistance = new Vector2(1f, -1f);
            }

            // ---- Kese (sağ üst) ----
            var plate = UiKit.PaperCard(transform, "Purse", th, 4f, out var paper, 5f);
            UiKit.Place(plate, new Vector2(1f, 1f), new Vector2(-purseMargin.x, -purseMargin.y), new Vector2(190f, 60f));
            amount = UiKit.Text(paper, "Amount", th.HeadingFont, 34f, th.InkDark, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
            UiKit.Fill(amount.rectTransform, 56, 0, 70, 2);
            amount.textWrappingMode = TextWrappingModes.NoWrap;
            var word = UiKit.Text(paper, "Word", th.BodyFont, 22f, th.PaperShade, TextAlignmentOptions.MidlineLeft);
            UiKit.Fill(word.rectTransform, 124, 0, 6, 0);
            word.text = currencyWord;

            var coinImg = UiKit.Img(plate, "Coin", th.Coin != null ? Color.white : th.PaperShade, th.Coin != null ? th.Coin : UiKit.Disc);
            coin = coinImg.rectTransform;
            UiKit.Place(coin, new Vector2(0f, 0.5f), new Vector2(-26f, 0f), new Vector2(76f, 76f));
            coin.pivot = new Vector2(0.5f, 0.5f);
            coin.anchoredPosition = new Vector2(12f, 0f);
            coinImg.preserveAspect = true;

            delta = UiKit.Text(plate, "Delta", th.HeadingFont, 30f, th.GoldDark, TextAlignmentOptions.TopRight, FontStyles.Bold);
            deltaRt = delta.rectTransform;
            UiKit.Place(deltaRt, new Vector2(1f, 1f), new Vector2(-12f, -82f), new Vector2(160f, 40f));
            var dsh = delta.gameObject.AddComponent<Shadow>();
            dsh.effectColor = UiKit.WithAlpha(th.InkDeep, 0.8f);
            dsh.effectDistance = new Vector2(0f, -2f);
            delta.gameObject.SetActive(false);
            selectedLabel.gameObject.SetActive(false);
        }
    }
}
