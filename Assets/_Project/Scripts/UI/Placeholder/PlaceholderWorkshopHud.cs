using System.Collections.Generic;
using System.Text;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// TEST YER TUTUCUSU — atölye HUD'u: alt ortada 6 slotluk envanter + kese, sol üstte sipariş kartı, alt ortada
    /// kısa notlar, defter ipucu, "Gerçek hayatta" kartı ve pazar ekranı. Asset kullanmaz, arayüzü kendisi kurar.
    /// Canvas altına konması yeter. Mustafa'nın view'ları gelince silinir (her arayüz ayrı view'a bölünebilir).
    /// </summary>
    public sealed class PlaceholderWorkshopHud : MonoBehaviour,
        IInventoryView, IOrderCardView, INoticeView, IHintBookView, IRealLifeCardView, IMarketView
    {
        [Header("Ham etiketler (yer tutucu)")]
        [SerializeField] private string currencyWord = "akçe";
        [SerializeField] private string hintWord = "Defter";
        [SerializeField] private string marketTitle = "Pazar";
        [SerializeField] private string closeWord = "Kapat";
        [SerializeField] private string readyWord = "Hazır — kapıya götür";
        [SerializeField] private string rewardWord = "Ödeme";
        [SerializeField] private float noticeSeconds = 4f;

        private static readonly Color Parchment = new Color(0.91f, 0.85f, 0.71f, 0.96f);
        private static readonly Color Ink = new Color(0.23f, 0.16f, 0.10f, 1f);
        private static readonly Color Wood = new Color(0.33f, 0.22f, 0.13f, 0.92f);
        private static readonly Color WoodLight = new Color(0.55f, 0.40f, 0.24f, 0.95f);
        private static readonly Color Gold = new Color(0.97f, 0.66f, 0.0f, 1f);

        private readonly Image[] slotFrames = new Image[PlayerInventory.SlotCount];
        private readonly Image[] slotJars = new Image[PlayerInventory.SlotCount];
        private readonly TMP_Text[] slotTexts = new TMP_Text[PlayerInventory.SlotCount];
        private TMP_Text moneyText, cardText, noticeText, hintText, realLifeText, marketMoney, hintKeyText;
        private GameObject card, notice, hintBox, realLife, market;
        private RectTransform marketList;
        private Image hintKey;
        private float noticeUntil;
        private readonly StringBuilder sb = new StringBuilder(256);

        public bool IsHintOpen { get; private set; }

        private void Awake() => Build();

        private void OnEnable()
        {
            ViewLocator.Register<IInventoryView>(this);
            ViewLocator.Register<IOrderCardView>(this);
            ViewLocator.Register<INoticeView>(this);
            ViewLocator.Register<IHintBookView>(this);
            ViewLocator.Register<IRealLifeCardView>(this);
            ViewLocator.Register<IMarketView>(this);
            GameInput.SchemeChanged += OnScheme;
            OnScheme(GameInput.LastScheme);
        }

        private void OnDisable()
        {
            ViewLocator.Unregister<IInventoryView>(this);
            ViewLocator.Unregister<IOrderCardView>(this);
            ViewLocator.Unregister<INoticeView>(this);
            ViewLocator.Unregister<IHintBookView>(this);
            ViewLocator.Unregister<IRealLifeCardView>(this);
            ViewLocator.Unregister<IMarketView>(this);
            GameInput.SchemeChanged -= OnScheme;
        }

        private void Update()
        {
            if (notice.activeSelf && Time.time > noticeUntil) notice.SetActive(false);
        }

        private void OnScheme(ControlScheme scheme)
        {
            if (hintKeyText != null) hintKeyText.text = hintWord + "  " + ControlGlyphs.For(PromptAction.WorkshopHint, scheme);
        }

        // ---------- IInventoryView ----------

        public void ShowInventory(IReadOnlyList<InventorySlot> slots, int selected, int money)
        {
            for (int i = 0; i < slotFrames.Length && i < slots.Count; i++)
            {
                var s = slots[i];
                slotFrames[i].color = i == selected ? Gold : Wood;
                slotJars[i].gameObject.SetActive(!s.IsEmpty);
                if (!s.IsEmpty) slotJars[i].color = s.Species.JarColor;
                slotTexts[i].text = s.IsEmpty ? $"<size=60%>{i + 1}</size>" : $"{s.Species.FormulaRich}\n<size=80%>×{s.Count}</size>";
            }
            moneyText.text = $"<b>{money}</b> {currencyWord}";
        }

        // ---------- IOrderCardView ----------

        public void ShowOrder(OrderCardInfo info)
        {
            sb.Clear();
            sb.Append("<size=32><b>").Append(info.GiverName).Append("</b></size>");
            if (info.StepCount > 1) sb.Append("   <size=22>adım ").Append(info.StepIndex + 1).Append('/').Append(info.StepCount).Append("</size>");
            sb.Append("\n<size=22>İstediği</size>\n<size=32>");
            AppendAmounts(info.Products);
            sb.Append("</size>");
            if (info.Reactants != null)
            {
                sb.Append("\n<size=22>Malzemeler: ");
                for (int i = 0; i < info.Reactants.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(info.Reactants[i].FormulaRich);
                }
                sb.Append("</size>");
            }
            sb.Append("\n<size=22>").Append(rewardWord).Append(": ").Append(info.Reward).Append(' ').Append(currencyWord).Append("</size>");
            if (info.ReadyToDeliver) sb.Append("\n<color=#3E7A2E><b>").Append(readyWord).Append("</b></color>");
            cardText.text = sb.ToString();
            card.SetActive(true);
        }

        public void ClearOrder() => card.SetActive(false);

        private void AppendAmounts(IReadOnlyList<SpeciesAmount> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(" + ");
                if (list[i].Amount != 1) sb.Append(list[i].Amount).Append(' ');
                sb.Append(list[i].Species.FormulaRich);
            }
        }

        // ---------- INoticeView ----------

        public void ShowNotice(string text)
        {
            noticeText.text = text;
            notice.SetActive(true);
            noticeUntil = Time.time + noticeSeconds;
        }

        // ---------- IHintBookView ----------

        public void SetGlowing(bool glowing)
        {
            hintKey.gameObject.SetActive(glowing || IsHintOpen);
            hintKey.color = glowing ? Gold : WoodLight;
        }

        public void ShowHint(string text)
        {
            IsHintOpen = true;
            hintText.text = "<b>Ustanın defteri</b>\n" + text;
            hintBox.SetActive(true);
        }

        public void HideHint()
        {
            IsHintOpen = false;
            if (hintBox != null) hintBox.SetActive(false);
        }

        // ---------- IRealLifeCardView ----------

        public void Show(string text)
        {
            realLifeText.text = "<b>Gerçek hayatta</b>\n" + text;
            realLife.SetActive(true);
        }

        public void Hide()
        {
            if (realLife != null) realLife.SetActive(false);
        }

        // ---------- IMarketView ----------

        private readonly List<GameObject> marketButtons = new List<GameObject>();

        public void Open(IReadOnlyList<MarketItem> items, int money)
        {
            // Liste yeniden kurulunca seçili düğme yok olur (gamepad odağı kaybolur): aynı sıradakini geri seç.
            int selectedIndex = -1;
            var es = EventSystem.current;
            if (market.activeSelf && es != null && es.currentSelectedGameObject != null
                && es.currentSelectedGameObject.transform.parent == marketList)
                selectedIndex = es.currentSelectedGameObject.transform.GetSiblingIndex();
            int oldCloseIndex = marketList.childCount - 1;

            for (int i = marketList.childCount - 1; i >= 0; i--)
            {
                var child = marketList.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            var buttons = marketButtons;
            buttons.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var s = item.Species;
                string label = $"{s.FormulaRich}  <size=70%>{s.DisplayName}</size>   <b>{item.Price} {currencyWord}</b>   <size=70%>elde {item.Owned}</size>";
                buttons.Add(MakeButton(marketList, label, () => ViewLocator.Get<IMarketCommands>()?.Buy(s), Parchment, 56).gameObject);
            }
            buttons.Add(MakeButton(marketList, closeWord + "  " + ControlGlyphs.For(PromptAction.Cancel),
                () => ViewLocator.Get<IMarketCommands>()?.CloseMarket(), WoodLight, 50).gameObject);

            marketMoney.text = $"Kese: <b>{money}</b> {currencyWord}";
            market.SetActive(true);

            // İlk açılışta ilk düğme. Yenilemede: "Kapat" seçiliyse yine Kapat; madde seçiliyse aynı sıradaki madde
            // (liste kısaldıysa sondaki madde).
            if (es == null) return;
            int closeIndex = buttons.Count - 1;
            int index = 0;
            if (selectedIndex >= 0)
                index = selectedIndex >= oldCloseIndex ? closeIndex : Mathf.Min(selectedIndex, Mathf.Max(0, closeIndex - 1));
            es.SetSelectedGameObject(buttons[index]);
        }

        public void Close() => market.SetActive(false);

        // ---------- Kurulum ----------

        private void Build()
        {
            var root = (RectTransform)transform;

            // Envanter: alt ortada 6 slot + kese
            var bar = Panel(root, "Inventory", new Color(0, 0, 0, 0), new Vector2(0.5f, 0f), new Vector2(0, 20), new Vector2(6 * 118 + 200, 120));
            for (int i = 0; i < PlayerInventory.SlotCount; i++)
            {
                var frame = Panel(bar, "Slot" + (i + 1), Wood, new Vector2(0f, 0.5f), new Vector2(i * 118, 0), new Vector2(110, 110));
                frame.pivot = new Vector2(0f, 0.5f);
                frame.anchoredPosition = new Vector2(i * 118, 0);
                slotFrames[i] = frame.GetComponent<Image>();
                var jar = Panel(frame, "Jar", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(40, 46));
                slotJars[i] = jar.GetComponent<Image>();
                slotTexts[i] = Text(frame, "", 24, Parchment, TextAlignmentOptions.Bottom);
                Fill(slotTexts[i].rectTransform, 4, 4, 4, 4);
            }
            var money = Panel(bar, "Money", Parchment, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(180, 64));
            money.pivot = new Vector2(1f, 0.5f);
            money.anchoredPosition = Vector2.zero;
            moneyText = Text(money, "", 28, Ink, TextAlignmentOptions.Center);
            Fill(moneyText.rectTransform, 0, 0, 0, 0);

            // Sipariş kartı: sol üst
            card = Panel(root, "OrderCard", Parchment, new Vector2(0f, 1f), new Vector2(24, -24), new Vector2(520, 240)).gameObject;
            cardText = Text((RectTransform)card.transform, "", 26, Ink, TextAlignmentOptions.TopLeft);
            Fill(cardText.rectTransform, 20, 16, 20, 16);
            card.SetActive(false);

            // Defter tuşu ipucu (2 hatadan sonra parlar): kartın altında
            hintKey = Panel(root, "HintKey", WoodLight, new Vector2(0f, 1f), new Vector2(24, -276), new Vector2(240, 52)).GetComponent<Image>();
            hintKeyText = Text((RectTransform)hintKey.transform, "", 26, Ink, TextAlignmentOptions.Center);
            Fill(hintKeyText.rectTransform, 0, 0, 0, 0);
            hintKey.gameObject.SetActive(false);

            hintBox = Panel(root, "Hint", Parchment, new Vector2(0f, 1f), new Vector2(24, -340), new Vector2(520, 150)).gameObject;
            hintText = Text((RectTransform)hintBox.transform, "", 26, Ink, TextAlignmentOptions.TopLeft);
            Fill(hintText.rectTransform, 18, 14, 18, 14);
            hintBox.SetActive(false);

            // Kısa not: envanterin üstünde
            notice = Panel(root, "Notice", Parchment, new Vector2(0.5f, 0f), new Vector2(0, 160), new Vector2(900, 90)).gameObject;
            noticeText = Text((RectTransform)notice.transform, "", 28, Ink, TextAlignmentOptions.Center);
            Fill(noticeText.rectTransform, 16, 8, 16, 8);
            notice.SetActive(false);

            // Gerçek hayatta: sağ üst
            realLife = Panel(root, "RealLife", Parchment, new Vector2(1f, 1f), new Vector2(-24, -24), new Vector2(560, 200)).gameObject;
            realLifeText = Text((RectTransform)realLife.transform, "", 26, Ink, TextAlignmentOptions.TopLeft);
            Fill(realLifeText.rectTransform, 18, 14, 18, 14);
            realLife.SetActive(false);

            // Pazar: ortada liste
            var m = Panel(root, "Market", Wood, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 720));
            market = m.gameObject;
            var title = Text(m, "<b>" + marketTitle + "</b>", 40, Parchment, TextAlignmentOptions.Top);
            Fill(title.rectTransform, 0, 16, 0, 0);
            marketMoney = Text(m, "", 28, Parchment, TextAlignmentOptions.Top);
            Fill(marketMoney.rectTransform, 0, 70, 0, 0);
            var listGo = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
            marketList = (RectTransform)listGo.transform;
            marketList.SetParent(m, false);
            Fill(marketList, 24, 120, 24, 24);
            var layout = listGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            market.SetActive(false);
        }

        private static RectTransform Panel(RectTransform parent, string name, Color color, Vector2 anchor, Vector2 position, Vector2 size)
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

        private static TMP_Text Text(RectTransform parent, string text, float size, Color color, TextAlignmentOptions align)
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

        private static Button MakeButton(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick, Color color, float height)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            go.GetComponent<LayoutElement>().preferredHeight = height;
            var b = go.GetComponent<Button>();
            b.onClick.AddListener(onClick);
            var colors = b.colors;
            colors.highlightedColor = new Color(1.1f, 1.05f, 0.85f, 1f);
            colors.selectedColor = new Color(1.15f, 1.0f, 0.6f, 1f);
            b.colors = colors;
            var t = Text((RectTransform)go.transform, label, 28, Ink, TextAlignmentOptions.Center);
            Fill(t.rectTransform, 12, 0, 12, 0);
            return b;
        }

        private static void Fill(RectTransform rt, float left, float top, float right, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
