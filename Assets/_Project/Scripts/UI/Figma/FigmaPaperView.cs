using System;
using System.Collections;
using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.Desk;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Masadaki kâğıt (03 §6) — marka kimliği: ahşap çerçeveli parşömen, Alegreya başlık, mürekkep gövde.
    /// Doğru cevap yeşil değil ustanın mührüdür (balmumu, şekilli); yanlış satır mürekkep lekesi + kâğıt sallanır.
    /// Satır biçimleri: Chips (yan yana), Cycle (‹ değer ›), List (alt alta şıklar). Odaklı satır koyu kenarlı.
    /// Fare: seçeneğe tıkla / tekerlek. Gamepad ve klavye girdisini DeskController dinler; burası yalnızca çizer.
    /// </summary>
    public sealed class FigmaPaperView : FigmaView<IPaperView>, IPaperView
    {
        [SerializeField] private float width = 1240f;
        [Tooltip("Kâğıt bu yüksekliği aşarsa küçültülür (1080 referans).")]
        [SerializeField] private float maxHeight = 1000f;
        [Header("Alt satırdaki tuş ipuçları")]
        [SerializeField] private string navigateHint = "Satır";
        [SerializeField] private string changeHint = "Seç";
        [SerializeField] private string confirmHint = "Onay";
        [SerializeField] private string hintHint = "Ustanın notu";
        [SerializeField] private string exitHint = "Kalk";

        private RectTransform root;
        private RectTransform card;
        private RectTransform paper;
        private TextMeshProUGUI title, from, letter, counter, prompt, note;
        private RectTransform rowsRoot;
        private Image submitFrame;
        private TextMeshProUGUI submitLabel;
        private RectTransform hintCard;
        private TextMeshProUGUI hintTitle, hintBody;
        private RectTransform sealRoot;
        private TextMeshProUGUI sealText, sealContinue;
        private Image sealDisc;
        private readonly List<(PromptAction action, TextMeshProUGUI label, RectTransform rootRt)> keys =
            new List<(PromptAction, TextMeshProUGUI, RectTransform)>();

        private readonly List<RowWidget> rows = new List<RowWidget>();
        private PaperQuestion question;
        private int focus;
        private Coroutine shake, stamp;

        private IPaperCommands Commands => ViewLocator.Get<IPaperCommands>();

        public bool IsHintOpen => hintCard != null && hintCard.gameObject.activeSelf;

        private void Awake() => Build();

        protected override void OnEnable()
        {
            base.OnEnable();
            GameInput.SchemeChanged += OnScheme;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameInput.SchemeChanged -= OnScheme;
        }

        // ---------- IPaperView ----------

        public void Open(PaperHeader header)
        {
            title.text = header.Title;
            from.text = string.IsNullOrEmpty(header.From) ? string.Empty : header.FromLabel + ": <b>" + header.From + "</b>";
            letter.text = header.Letter;
            letter.gameObject.SetActive(!string.IsNullOrEmpty(header.Letter));
            submitLabel.text = header.SubmitWord;
            var th = Theme;
            title.color = header.Guild ? th.WaxDark : th.InkDeep;
            note.text = string.Empty;
            sealRoot.gameObject.SetActive(false);
            hintCard.gameObject.SetActive(false);
            OnScheme(GameInput.LastScheme);
            root.gameObject.SetActive(true);
        }

        public void ShowQuestion(PaperQuestion q, string counterText)
        {
            question = q;
            counter.text = counterText;
            note.text = string.Empty;
            sealRoot.gameObject.SetActive(false);
            for (int i = 0; i < rows.Count; i++) Destroy(rows[i].Root.gameObject);
            rows.Clear();
            for (int i = 0; i < q.Rows.Count; i++) rows.Add(BuildRow(i, q.Rows[i]));
        }

        public void Refresh(PaperQuestion q, int focusRow)
        {
            if (q != question) return;
            focus = focusRow;
            var th = Theme;
            prompt.text = q.PromptText;
            prompt.gameObject.SetActive(!string.IsNullOrEmpty(prompt.text));
            for (int i = 0; i < rows.Count; i++)
            {
                var w = rows[i];
                var data = q.Rows[i];
                bool visible = q.IsVisible(i);
                w.Root.gameObject.SetActive(visible);
                if (!visible) continue;
                bool focused = i == focusRow;
                w.Frame.color = focused ? th.InkDeep : UiKit.WithAlpha(th.PaperShade, 0.0f);
                w.Back.color = focused ? th.Paper : UiKit.WithAlpha(th.PaperMid, 0.55f);
                w.Label.text = q.RowLabel(i);
                w.Label.gameObject.SetActive(!string.IsNullOrEmpty(w.Label.text));
                w.Label.color = data.Wrong ? th.WaxDark : th.InkDark;
                w.Blot.gameObject.SetActive(data.Wrong);
                if (data.Style == RowStyle.Cycle)
                {
                    w.CycleValue.text = data.Selected >= 0 ? data.Options[data.Selected] : FillBlankPaper.Empty;
                    w.CycleValue.color = data.Wrong ? th.WaxDark : th.InkDeep;
                }
                else
                {
                    for (int k = 0; k < w.Options.Count; k++)
                    {
                        bool sel = k == data.Selected;
                        w.Options[k].Fill.color = sel ? th.InkDark : th.Paper;
                        w.Options[k].Text.color = sel ? th.Paper : th.InkDark;
                        if (w.Options[k].Bullet != null) w.Options[k].Bullet.sprite = sel ? UiKit.Check : UiKit.Ring;
                        if (w.Options[k].Bullet != null) w.Options[k].Bullet.color = sel ? th.Paper : th.Ink;
                    }
                }
            }
            bool submitFocused = focusRow >= q.Rows.Count;
            submitFrame.color = submitFocused ? th.GoldLight : th.WaxDark;
        }

        public void ShowNote(string text, PaperNoteKind kind)
        {
            var th = Theme;
            note.text = text;
            note.color = kind == PaperNoteKind.Wrong ? th.WaxDark : kind == PaperNoteKind.Correct ? th.WaterDark : th.Ink;
            if (kind == PaperNoteKind.Wrong)
            {
                if (shake != null) StopCoroutine(shake);
                shake = StartCoroutine(Shake());
            }
            if (question != null) Refresh(question, focus);
        }

        public void ShowHint(string titleText, string text)
        {
            hintTitle.text = titleText;
            hintBody.text = text;
            hintCard.gameObject.SetActive(true);
        }

        public void HideHint()
        {
            if (hintCard != null) hintCard.gameObject.SetActive(false);
        }

        public void ShowSeal(string text, string continueWord, bool guild)
        {
            var th = Theme;
            sealText.text = text;
            sealContinue.text = continueWord;
            sealDisc.color = guild ? th.GoldDark : th.Wax;
            sealRoot.gameObject.SetActive(true);
            HideHint();
            if (stamp != null) StopCoroutine(stamp);
            stamp = StartCoroutine(Stamp());
        }

        public void Close()
        {
            question = null;
            if (root != null) root.gameObject.SetActive(false);
        }

        // ---------- Yerleşim ----------

        private void LateUpdate()
        {
            if (card == null || !root.gameObject.activeSelf) return;
            // Uzun kâğıt (doğru-yanlış + düzeltmeler) ekrana sığsın: yükseklik sınırı aşılırsa küçült.
            float h = card.sizeDelta.y;
            float s = h > maxHeight ? maxHeight / h : 1f;
            if (Mathf.Abs(card.localScale.x - s) > 0.001f && shake == null) card.localScale = new Vector3(s, s, 1f);
        }

        private IEnumerator Shake()
        {
            var basePos = card.anchoredPosition;
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                float a = (1f - t / 0.35f) * 14f;
                card.anchoredPosition = basePos + new Vector2(Mathf.Sin(t * 70f) * a, 0f);
                yield return null;
            }
            card.anchoredPosition = basePos;
            shake = null;
        }

        private IEnumerator Stamp()
        {
            var t0 = sealDisc.rectTransform;
            float t = 0f;
            while (t < 0.28f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.28f);
                float s = Mathf.Lerp(1.9f, 1f, 1f - (1f - k) * (1f - k));
                t0.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            t0.localScale = Vector3.one;
            stamp = null;
        }

        private void OnScheme(ControlScheme scheme)
        {
            foreach (var k in keys) UiKit.SetKeyCap(k.label, k.rootRt, ControlGlyphs.For(k.action, scheme), 40f);
        }

        private void Build()
        {
            var th = Theme;
            root = UiKit.Rect(transform, "Paper");
            UiKit.Fill(root);
            var scrim = UiKit.Img(root, "Scrim", UiKit.WithAlpha(th.InkDeep, 0.35f), null, true);
            UiKit.Fill(scrim.rectTransform);

            card = UiKit.PaperCard(root, "Card", th, 5f, out paper, 10f);
            UiKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(-80f, 10f), new Vector2(width, 0f));
            var grain = UiKit.Img(paper, "Grain", UiKit.WithAlpha(th.PaperShade, 0.10f), UiKit.WoodGrain);
            UiKit.Fill(grain.rectTransform);
            grain.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var col = paper.gameObject.AddComponent<VerticalLayoutGroup>();
            col.padding = new RectOffset(56, 56, 40, 34);
            col.spacing = 12f;
            col.childControlWidth = col.childControlHeight = true;
            col.childForceExpandWidth = true;
            col.childForceExpandHeight = false;

            // başlık satırı: başlık solda, soru sayacı sağda
            var head = UiKit.Rect(paper, "Head");
            var headRow = head.gameObject.AddComponent<HorizontalLayoutGroup>();
            headRow.childControlWidth = headRow.childControlHeight = true;
            headRow.childForceExpandWidth = false;
            headRow.childAlignment = TextAnchor.LowerLeft;
            title = UiKit.Text(head, "Title", th.HeadingFont, 50f, th.InkDeep, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            counter = UiKit.Text(head, "Counter", th.BodyFont, 26f, th.Ink, TextAlignmentOptions.BottomRight, FontStyles.Italic);
            counter.textWrappingMode = TextWrappingModes.NoWrap;

            from = UiKit.Text(paper, "From", th.BodyFont, 25f, th.WaterDark, TextAlignmentOptions.Left);
            letter = UiKit.Text(paper, "Letter", th.BodyFont, 27f, th.Ink, TextAlignmentOptions.TopLeft, FontStyles.Italic);
            var rule = UiKit.Img(paper, "Rule", UiKit.WithAlpha(th.Ink, 0.45f));
            rule.gameObject.AddComponent<LayoutElement>().preferredHeight = 2f;

            prompt = UiKit.Text(paper, "Prompt", th.BodyFont, 31f, th.InkDeep, TextAlignmentOptions.TopLeft, FontStyles.Bold);

            rowsRoot = UiKit.Rect(paper, "Rows");
            var rowsCol = rowsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            rowsCol.spacing = 8f;
            rowsCol.childControlWidth = rowsCol.childControlHeight = true;
            rowsCol.childForceExpandWidth = true;
            rowsCol.childForceExpandHeight = false;

            note = UiKit.Text(paper, "Note", th.HeadingFont, 29f, th.Ink, TextAlignmentOptions.Left, FontStyles.Italic);
            note.gameObject.AddComponent<LayoutElement>().minHeight = 38f;

            // alt satır: tuş ipuçları solda, Gönder sağda
            var foot = UiKit.Rect(paper, "Foot");
            var footRow = foot.gameObject.AddComponent<HorizontalLayoutGroup>();
            footRow.spacing = 10f;
            footRow.childAlignment = TextAnchor.MiddleLeft;
            footRow.childControlWidth = footRow.childControlHeight = true;
            footRow.childForceExpandWidth = false;
            AddKey(foot, th, PromptAction.Navigate, navigateHint);
            AddKey(foot, th, PromptAction.Increase, changeHint);
            AddKey(foot, th, PromptAction.Confirm, confirmHint);
            AddKey(foot, th, PromptAction.OpenHint, hintHint);
            AddKey(foot, th, PromptAction.ExitStation, exitHint);
            var spacer = UiKit.Rect(foot, "Spacer");
            spacer.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            BuildSubmit(foot, th);

            var sizer = card.gameObject.AddComponent<CardSizer>();
            sizer.Init(paper, 5f);

            BuildHint(th);
            BuildSeal(th);
            root.gameObject.SetActive(false);
        }

        private void AddKey(RectTransform parent, UiTheme th, PromptAction action, string text)
        {
            var label = UiKit.KeyCap(parent, th, 40f, out var capRoot);
            keys.Add((action, label, capRoot));
            var t = UiKit.Text(parent, "KeyText", th.BodyFont, 22f, th.Ink, TextAlignmentOptions.Left);
            t.text = text;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.margin = new Vector4(0, 0, 10, 0);
        }

        private void BuildSubmit(RectTransform parent, UiTheme th)
        {
            submitFrame = UiKit.Img(parent, "Submit", th.WaxDark, UiKit.Round8, true);
            var le = submitFrame.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 230f;
            le.preferredHeight = 64f;
            var face = UiKit.Img(submitFrame.transform, "Face", th.Wax, UiKit.Round8);
            UiKit.Fill(face.rectTransform, 4, 4, 4, 7);
            submitLabel = UiKit.Text(face.transform, "Label", th.HeadingFont, 32f, th.Paper, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Fill(submitLabel.rectTransform);
            Hit.Add(submitFrame.gameObject, () => Commands?.Submit(), () =>
            {
                if (question != null) Commands?.Focus(question.Rows.Count);
            }, null);
        }

        private void BuildHint(UiTheme th)
        {
            hintCard = UiKit.PaperCard(root, "HintCard", th, 4f, out var inner);
            UiKit.Place(hintCard, new Vector2(1f, 0.5f), new Vector2(-48f, 40f), new Vector2(430f, 0f));
            var c = inner.gameObject.AddComponent<VerticalLayoutGroup>();
            c.padding = new RectOffset(26, 26, 20, 24);
            c.spacing = 8f;
            c.childControlWidth = c.childControlHeight = true;
            c.childForceExpandHeight = false;
            hintTitle = UiKit.Text(inner, "Title", th.HeadingFont, 30f, th.WaterDark, TextAlignmentOptions.Left, FontStyles.Bold);
            hintBody = UiKit.Text(inner, "Body", th.BodyFont, 27f, th.Ink, TextAlignmentOptions.TopLeft);
            hintCard.gameObject.AddComponent<CardSizer>().Init(inner, 4f);
            hintCard.gameObject.SetActive(false);
        }

        private void BuildSeal(UiTheme th)
        {
            sealRoot = UiKit.Rect(card, "Seal");
            UiKit.Fill(sealRoot);
            var veil = UiKit.Img(sealRoot, "Veil", UiKit.WithAlpha(th.Paper, 0.72f), UiKit.Round4, true);
            UiKit.Fill(veil.rectTransform, 5, 5, 5, 5);

            var disc = UiKit.Img(sealRoot, "Wax", th.Wax, th.SealWax != null ? th.SealWax : UiKit.Disc);
            sealDisc = disc;
            UiKit.Place(disc.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(210f, 210f));
            disc.rectTransform.localEulerAngles = new Vector3(0, 0, -12f);
            if (th.Seal != null)
            {
                var mark = UiKit.Img(disc.transform, "Mark", UiKit.WithAlpha(th.WaxDark, 0.9f), th.Seal);
                UiKit.Fill(mark.rectTransform, 40, 40, 40, 40);
            }
            else
            {
                var ring = UiKit.Img(disc.transform, "Ring", th.WaxDark, UiKit.RingDashed);
                UiKit.Fill(ring.rectTransform, 26, 26, 26, 26);
                var tick = UiKit.Img(disc.transform, "Check", th.WaxDark, UiKit.Check);
                UiKit.Fill(tick.rectTransform, 62, 62, 62, 62);
            }

            sealText = UiKit.Text(sealRoot, "Text", th.HeadingFont, 40f, th.InkDeep, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.Italic);
            UiKit.Place(sealText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -55f), new Vector2(width - 160f, 150f));
            sealText.alignment = TextAlignmentOptions.Top;

            var btn = UiKit.Img(sealRoot, "Continue", th.InkDeep, UiKit.Round8, true);
            UiKit.Place(btn.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -205f), new Vector2(260f, 64f));
            var face = UiKit.Img(btn.transform, "Face", th.Paper, UiKit.Round8);
            UiKit.Fill(face.rectTransform, 3, 3, 3, 6);
            sealContinue = UiKit.Text(face.transform, "Label", th.HeadingFont, 32f, th.InkDeep, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Fill(sealContinue.rectTransform);
            Hit.Add(btn.gameObject, () => Commands?.Continue(), null, null);
            sealRoot.gameObject.SetActive(false);
        }

        // ---------- Satırlar ----------

        private sealed class OptionWidget
        {
            public Image Fill;
            public Image Bullet;
            public TextMeshProUGUI Text;
        }

        private sealed class RowWidget
        {
            public RectTransform Root;
            public Image Frame, Back, Blot;
            public TextMeshProUGUI Label, CycleValue;
            public readonly List<OptionWidget> Options = new List<OptionWidget>();
        }

        private RowWidget BuildRow(int index, PaperRow data)
        {
            var th = Theme;
            var w = new RowWidget();
            var frame = UiKit.Img(rowsRoot, "Row_" + index, th.InkDeep, UiKit.Round8, true);
            w.Root = frame.rectTransform;
            w.Frame = frame;
            var back = UiKit.Img(frame.transform, "Back", th.Paper, UiKit.Round8);
            UiKit.Fill(back.rectTransform, 3, 3, 3, 3);
            w.Back = back;
            var col = frame.gameObject.AddComponent<VerticalLayoutGroup>();
            col.padding = new RectOffset(data.ParentRow >= 0 ? 60 : 20, 70, 10, 12);
            col.spacing = 6f;
            col.childControlWidth = col.childControlHeight = true;
            col.childForceExpandWidth = true;
            col.childForceExpandHeight = false;
            back.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            // Satır kendi tıklaması: odak; tekerlek seçeneği kaydırır.
            Hit.Add(frame.gameObject, null, () => Commands?.Focus(index), delta => Commands?.StepOption(index, delta));

            w.Label = UiKit.Text(frame.transform, "Label", th.BodyFont, 28f, th.InkDark, TextAlignmentOptions.TopLeft);
            ReserveFillBlankHeight(w.Label, index, data, col.padding.horizontal);

            if (data.Style == RowStyle.Cycle) BuildCycle(w, frame.transform, index, th);
            else BuildOptions(w, frame.transform, index, data, th);

            // mürekkep lekesi (yanlış): satırın sağında, düzene girmez
            var blot = UiKit.Img(frame.transform, "Blot", UiKit.WithAlpha(th.InkDeep, 0.82f), UiKit.Disc);
            blot.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiKit.Place(blot.rectTransform, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(58f, 50f));
            blot.rectTransform.localEulerAngles = new Vector3(0, 0, 18f);
            var drip = UiKit.Img(blot.transform, "Drip", UiKit.WithAlpha(th.InkDeep, 0.82f), UiKit.Disc);
            UiKit.Place(drip.rectTransform, new Vector2(0.15f, 0.1f), new Vector2(-8f, -10f), new Vector2(20f, 20f));
            var drip2 = UiKit.Img(blot.transform, "Drip2", UiKit.WithAlpha(th.InkDeep, 0.82f), UiKit.Disc);
            UiKit.Place(drip2.rectTransform, new Vector2(0.9f, 0.85f), new Vector2(8f, 6f), new Vector2(13f, 13f));
            w.Blot = blot;
            blot.gameObject.SetActive(false);
            return w;
        }

        /// <summary>
        /// Boşluk doldurmada cümle seçilen kelimeyle yeniden kırılır; uzun kelime ("buharlaşma") satırı iki satıra
        /// çıkarıp kâğıdı büyütüyor, kâğıt da ekrana sığmak için küçülüyordu (Play 14.11). Satır, bankadaki her kelimeyle
        /// alacağı en büyük yüksekliği baştan ayırır.
        /// </summary>
        private void ReserveFillBlankHeight(TextMeshProUGUI label, int index, PaperRow data, int rowPadding)
        {
            if (question is not FillBlankPaper fill || data.Style != RowStyle.Cycle) return;
            var paperPad = paper.GetComponent<VerticalLayoutGroup>().padding.horizontal;
            float textWidth = width - paperPad - rowPadding;
            float tallest = 0f;
            for (int k = 0; k < data.Options.Length; k++)
                tallest = Mathf.Max(tallest, label.GetPreferredValues(fill.RowLabelFilled(index, data.Options[k]), textWidth, 0f).y);
            tallest = Mathf.Max(tallest, label.GetPreferredValues(fill.RowLabelFilled(index, FillBlankPaper.Empty), textWidth, 0f).y);
            label.gameObject.AddComponent<LayoutElement>().minHeight = Mathf.Ceil(tallest);
        }

        private void BuildCycle(RowWidget w, Transform parent, int index, UiTheme th)
        {
            var line = UiKit.Rect(parent, "Cycle");
            var h = line.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            Arrow(line, th, "‹", () => Commands?.StepOption(index, -1));
            var valueBox = UiKit.Img(line, "Value", UiKit.WithAlpha(th.PaperMid, 0.9f), UiKit.Round4);
            var le = valueBox.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 260f;
            le.preferredHeight = 48f;
            w.CycleValue = UiKit.Text(valueBox.transform, "Text", th.BodyFont, 28f, th.InkDeep, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Fill(w.CycleValue.rectTransform, 8, 0, 8, 0);
            Arrow(line, th, "›", () => Commands?.StepOption(index, 1));
        }

        private void Arrow(RectTransform parent, UiTheme th, string glyph, Action click)
        {
            var b = UiKit.Img(parent, "Arrow", th.Ink, UiKit.Round8, true);
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 48f;
            le.preferredHeight = 48f;
            var t = UiKit.Text(b.transform, "G", th.HeadingFont, 34f, th.Paper, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Fill(t.rectTransform, 0, 0, 0, 4);
            t.text = glyph;
            Hit.Add(b.gameObject, click, null, null);
        }

        private void BuildOptions(RowWidget w, Transform parent, int index, PaperRow data, UiTheme th)
        {
            var box = UiKit.Rect(parent, "Options");
            HorizontalOrVerticalLayoutGroup layout = data.Style == RowStyle.List
                ? box.gameObject.AddComponent<VerticalLayoutGroup>()
                : box.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = data.Style == RowStyle.List ? 6f : 12f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = data.Style == RowStyle.List;
            layout.childForceExpandHeight = false;

            for (int k = 0; k < data.Options.Length; k++)
            {
                int option = k;
                var o = new OptionWidget();
                var edge = UiKit.Img(box, "Opt_" + k, th.Ink, UiKit.Round8, true);
                var fill = UiKit.Img(edge.transform, "Fill", th.Paper, UiKit.Round8);
                UiKit.Fill(fill.rectTransform, 2, 2, 2, 2);
                o.Fill = fill;
                var row = edge.gameObject.AddComponent<HorizontalLayoutGroup>();
                row.padding = new RectOffset(data.Style == RowStyle.List ? 14 : 22, 22, 8, 9);
                row.spacing = 12f;
                row.childAlignment = TextAnchor.MiddleLeft;
                row.childControlWidth = row.childControlHeight = true;
                row.childForceExpandWidth = false;
                fill.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                if (data.Style == RowStyle.List)
                {
                    o.Bullet = UiKit.Img(edge.transform, "Bullet", th.Ink, UiKit.Ring);
                    var ble = o.Bullet.gameObject.AddComponent<LayoutElement>();
                    ble.preferredWidth = ble.preferredHeight = 26f;
                }
                o.Text = UiKit.Text(edge.transform, "Text", th.BodyFont, data.Style == RowStyle.List ? 26f : 27f, th.InkDark,
                    TextAlignmentOptions.Left, data.Style == RowStyle.Chips ? FontStyles.Bold : FontStyles.Normal);
                o.Text.text = data.Options[k];
                if (data.Style == RowStyle.List) o.Text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                else o.Text.textWrappingMode = TextWrappingModes.NoWrap;
                Hit.Add(edge.gameObject, () => Commands?.SelectOption(index, option), () => Commands?.Focus(index),
                    delta => Commands?.StepOption(index, delta));
                w.Options.Add(o);
            }
        }

        /// <summary>Fare: tıklama ve üstüne gelme (odak). Tekerlek Station/Increase-Decrease ile odaklı satıra gider.</summary>
        private sealed class Hit : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
        {
            private Action click, enter;

            public static void Add(GameObject go, Action click, Action enter, Action<int> _)
            {
                var h = go.AddComponent<Hit>();
                h.click = click;
                h.enter = enter;
            }

            public void OnPointerClick(PointerEventData e)
            {
                if (e.button == PointerEventData.InputButton.Left) click?.Invoke();
            }

            public void OnPointerEnter(PointerEventData e) => enter?.Invoke();
        }
    }
}
