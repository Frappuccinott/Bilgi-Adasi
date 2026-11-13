using System.Collections.Generic;
using System.Text;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Order;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Sipariş kartı — Figma: Marka Kimliği / Arayüz dili → UI/Sipariş kartı (kâğıt, 10 px ahşap çerçeve, sağ üstte mum
    /// mühür; sipariş veren Alegreya Black, "İstenen" altında büyük formül). Hazırsa altın "Teslim et" şeridi.
    /// HUD'da sol üstte durur. Yalnızca IOrderCardView; envanter/pazar hâlâ PlaceholderWorkshopHud'da.
    /// </summary>
    public sealed class FigmaOrderCard : FigmaView<IOrderCardView>, IOrderCardView
    {
        [Header("Etiketler")]
        [SerializeField] private string stepWord = "Sipariş";
        [SerializeField] private string wantedWord = "İstenen";
        [SerializeField] private string ingredientsWord = "Malzemeler";
        [SerializeField] private string rewardWord = "Ödeme";
        [SerializeField] private string currencyWord = "akçe";
        [SerializeField] private string readyWord = "Hazır, kapıya götür";

        [Header("Yerleşim")]
        [SerializeField] private float width = 360f;
        [SerializeField] private Vector2 margin = new Vector2(28f, 28f);

        private RectTransform card;
        private TextMeshProUGUI giver, sub, formula, ingredients, reward, ready;
        private GameObject readyStrip;
        private readonly StringBuilder sb = new StringBuilder(128);

        private void Awake() => Build();

        public void ShowOrder(OrderCardInfo info)
        {
            giver.text = info.GiverName;
            sb.Clear();
            if (info.StepCount > 1) sb.Append(stepWord).Append(' ').Append(info.StepIndex + 1).Append('/').Append(info.StepCount);
            sub.text = sb.ToString();
            sub.gameObject.SetActive(sb.Length > 0);

            sb.Clear();
            AppendAmounts(info.Products);
            formula.text = sb.ToString();

            if (info.Reactants != null && info.Reactants.Count > 0)
            {
                sb.Clear().Append(ingredientsWord).Append(": ");
                for (int i = 0; i < info.Reactants.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(info.Reactants[i].FormulaRich);
                }
                ingredients.text = sb.ToString();
                ingredients.gameObject.SetActive(true);
            }
            else ingredients.gameObject.SetActive(false);

            sb.Clear().Append(rewardWord).Append(": <b>").Append(info.Reward).Append("</b> ").Append(currencyWord);
            reward.text = sb.ToString();
            readyStrip.SetActive(info.ReadyToDeliver);
            card.gameObject.SetActive(true);
        }

        public void ClearOrder()
        {
            if (card != null) card.gameObject.SetActive(false);
        }

        private void AppendAmounts(IReadOnlyList<SpeciesAmount> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(" + ");
                if (list[i].Amount != 1) sb.Append(list[i].Amount).Append(' ');
                sb.Append(list[i].Species.FormulaRich);
            }
        }

        private void Build()
        {
            var th = Theme;
            card = UiKit.PaperCard(transform, "OrderCard", th, 6f, out var paper, 6f);
            UiKit.Place(card, new Vector2(0f, 1f), new Vector2(margin.x, -margin.y), new Vector2(width, 0f));

            var col = paper.gameObject.AddComponent<VerticalLayoutGroup>();
            col.padding = new RectOffset(22, 70, 16, 18);
            col.spacing = 2f;
            col.childControlWidth = col.childControlHeight = true;
            col.childForceExpandWidth = true;
            col.childForceExpandHeight = false;

            giver = UiKit.Text(paper, "Giver", th.HeadingFont, 30f, th.Ink, TextAlignmentOptions.Left, FontStyles.Bold);
            sub = UiKit.Text(paper, "Step", th.BodyFont, 22f, th.PaperShade);
            var rule = UiKit.Img(paper, "Rule", th.PaperShade);
            var ruleLe = rule.gameObject.AddComponent<LayoutElement>();
            ruleLe.preferredHeight = 2f;
            ruleLe.minHeight = 2f;
            var wanted = UiKit.Text(paper, "Wanted", th.BodyFont, 22f, th.PaperShade);
            wanted.text = wantedWord;
            formula = UiKit.Text(paper, "Formula", th.BodyFont, 40f, th.Ink, TextAlignmentOptions.Left, FontStyles.Bold);
            ingredients = UiKit.Text(paper, "Ingredients", th.BodyFont, 22f, th.Ink);
            reward = UiKit.Text(paper, "Reward", th.BodyFont, 22f, th.PaperShade);

            // Hazır şeridi: birincil altın düğme dili (Figma "Teslim et")
            var strip = UiKit.Img(paper, "Ready", th.Gold, UiKit.Round4);
            readyStrip = strip.gameObject;
            var stripLe = readyStrip.AddComponent<LayoutElement>();
            stripLe.preferredHeight = 38f;
            ready = UiKit.Text(strip.transform, "Label", th.BodyFont, 22f, th.InkDeep, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Fill(ready.rectTransform, 12, 0, 12, 0);
            ready.text = readyWord;

            // Mühür sağ üstte, kenardan taşar (Figma'daki gibi)
            var seal = UiKit.Img(card, "Seal", th.Seal != null ? Color.white : th.WaxMid, th.Seal != null ? th.Seal : UiKit.Disc);
            UiKit.Place(seal.rectTransform, new Vector2(1f, 1f), new Vector2(-8f, 14f), new Vector2(68f, 68f));
            seal.preserveAspect = true;

            card.gameObject.AddComponent<CardSizer>().Init(paper, 6f);
            card.gameObject.SetActive(false);
        }
    }
}
