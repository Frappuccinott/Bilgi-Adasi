using System;
using System.Collections.Generic;
using BilgiAdasi.City;
using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Harita ekranı (IMapView) — marka dili: karartılmış oyunun üstünde ahşap cilt, içinde kâğıt. Solda adanın üstten
    /// görüntüsü (kâğıda basılmış gibi, mürekkep çerçeve) ve üstünde yerler: köyler kimlik renginde mühür, altında adı
    /// ve 5 görev noktası; oyuncu mum kırmızısı ok. Sağda şehir/ilçe başlığı, köylerin durumu (5 nokta + durum satırı)
    /// ve kilitli şehirler. Altta tuş ipuçları ve "Kapat". Kapatma isteği denetleyiciye gider (o mod değiştirir).
    /// </summary>
    public sealed class FigmaMapView : FigmaView<IMapView>, IMapView
    {
        [SerializeField] private Vector2 size = new Vector2(1640f, 900f);
        [SerializeField] private float mapSize = 812f;
        [SerializeField] private float pulseSpeed = 3f;

        private GameObject root;
        private RawImage mapImage;
        private RectTransform markers, legend, player, hintRow, keyMapRoot, keyEscRoot;
        private TextMeshProUGUI title, subtitle, closeLabel, keyMap, keyEsc;
        private Button closeButton;
        private Action closeRequested;
        private readonly List<GameObject> built = new List<GameObject>();
        private readonly List<RectTransform> pulses = new List<RectTransform>();

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake() => Build();

        protected override void OnEnable()
        {
            base.OnEnable();
            GameInput.SchemeChanged += OnScheme;
        }

        protected override void OnDisable()
        {
            GameInput.SchemeChanged -= OnScheme;
            base.OnDisable();
        }

        public void Open(MapScreenInfo info, Action onCloseRequested)
        {
            closeRequested = onCloseRequested;
            for (int i = 0; i < built.Count; i++) Destroy(built[i]);
            built.Clear();
            pulses.Clear();

            mapImage.texture = info.Image;
            title.text = info.Title;
            subtitle.text = info.Subtitle;
            closeLabel.text = info.CloseWord;

            for (int i = 0; i < info.Points.Count; i++) MakeMarker(info.Points[i]);
            player.SetAsLastSibling();
            BuildLegend(info);

            root.SetActive(true);
            OnScheme(GameInput.LastScheme);
            EventSystem.current?.SetSelectedGameObject(closeButton.gameObject);
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
            closeRequested = null;
        }

        public void SetPlayer(Vector2 uv, float yawDegrees)
        {
            if (!IsOpen) return;
            player.anchoredPosition = Clamp((uv - new Vector2(0.5f, 0.5f)) * mapSize, mapSize * 0.5f - 14f);
            player.localRotation = Quaternion.Euler(0f, 0f, -yawDegrees);
        }

        private static Vector2 Clamp(Vector2 p, float half) =>
            new Vector2(Mathf.Clamp(p.x, -half, half), Mathf.Clamp(p.y, -half, half));

        private void Update()
        {
            if (!IsOpen || pulses.Count == 0) return;
            float s = 1f + 0.18f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed));
            for (int i = 0; i < pulses.Count; i++) pulses[i].localScale = new Vector3(s, s, 1f);
        }

        private void OnScheme(ControlScheme _)
        {
            if (keyMap == null) return;
            UiKit.SetKeyCap(keyMap, keyMapRoot, ControlGlyphs.For(PromptAction.OpenMap), 40f);
            UiKit.SetKeyCap(keyEsc, keyEscRoot, ControlGlyphs.For(PromptAction.Cancel), 40f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(hintRow);
        }

        private void RequestClose() => closeRequested?.Invoke();

        // ---------- Harita üstündeki yerler ----------

        private void MakeMarker(MapPointInfo p)
        {
            var th = Theme;
            var rt = UiKit.Rect(markers, "Yer_" + p.Label);
            UiKit.Place(rt, new Vector2(0.5f, 0.5f), (p.Uv - new Vector2(0.5f, 0.5f)) * mapSize, Vector2.zero);
            built.Add(rt.gameObject);

            bool village = p.Kind == MapPointKind.Neighborhood;
            bool muted = p.State == MapPointState.Locked || p.State == MapPointState.Soon;
            float d = village ? 44f : p.Kind == MapPointKind.Market ? 24f : 34f;

            if (p.State == MapPointState.Current)
            {
                var pulse = UiKit.Img(rt, "Pulse", th.Gold, UiKit.Ring);
                UiKit.Place(pulse.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(d + 22f, d + 22f));
                pulses.Add(pulse.rectTransform);
            }
            Seal(rt, p, d, muted);

            // Ad levhası: kâğıt; köyde mührün altında (+5 nokta), diğer yerlerde mührün sağında (köyle çakışmasın).
            var plate = UiKit.Rect(rt, "Plate");
            plate.anchorMin = plate.anchorMax = new Vector2(0.5f, 0.5f);
            // Haritanın sağ yarısında etiket sola açılır (çerçeveden taşmasın).
            bool left = p.Uv.x > 0.5f;
            plate.pivot = village ? new Vector2(0.5f, 1f) : new Vector2(left ? 1f : 0f, 0.5f);
            plate.anchoredPosition = village ? new Vector2(0f, -d * 0.5f - 6f)
                : new Vector2((d * 0.5f + 6f) * (left ? -1f : 1f), 0f);
            var bg = plate.gameObject.AddComponent<Image>();
            bg.sprite = UiKit.Round8;
            bg.type = Image.Type.Sliced;
            bg.color = UiKit.WithAlpha(th.Paper, muted ? 0.78f : 0.94f);
            bg.raycastTarget = false;
            var v = plate.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(12, 12, 4, village ? 8 : 4);
            v.spacing = 2f;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = v.childForceExpandHeight = false;
            var fit = plate.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var label = UiKit.Text(plate, "Label", village ? th.HeadingFont : th.BodyFont, village ? 26f : 22f,
                muted ? UiKit.WithAlpha(th.Ink, 0.7f) : th.InkDeep, TextAlignmentOptions.Center, FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.text = p.Label;
            if (village && p.Total > 0) Dots(plate, p, 14f, 6f);
        }

        /// <summary>Mühür: koyu kenar + kimlik rengi; kilitliyse soluk ve kesik halka, bittiyse onay.</summary>
        private void Seal(RectTransform parent, MapPointInfo p, float d, bool muted)
        {
            var th = Theme;
            var edge = UiKit.Img(parent, "Edge", muted ? UiKit.WithAlpha(th.InkDeep, 0.55f) : th.InkDeep, UiKit.Disc);
            UiKit.Place(edge.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(d, d));
            var face = UiKit.Img(edge.transform, "Face", muted ? Color.Lerp(p.Color, th.PaperShade, 0.7f) : p.Color, UiKit.Disc);
            UiKit.Fill(face.rectTransform, 4, 4, 4, 4);
            if (muted)
            {
                var dash = UiKit.Img(edge.transform, "Dashed", th.Paper, UiKit.RingDashed);
                UiKit.Fill(dash.rectTransform, 7, 7, 7, 7);
            }
            if (p.State == MapPointState.Done)
            {
                var check = UiKit.Img(edge.transform, "Check", th.Paper, UiKit.Check);
                UiKit.Fill(check.rectTransform, 5, 5, 5, 5);
            }
        }

        /// <summary>5 görev noktası: biten = dolu mum, sıradaki = altın halka, kalan = boş halka.</summary>
        private HorizontalLayoutGroup Dots(Transform parent, MapPointInfo p, float dot, float gap)
        {
            var th = Theme;
            var row = UiKit.Rect(parent, "Dots");
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = gap;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            for (int i = 0; i < p.Total; i++)
            {
                bool done = i < p.Done;
                bool next = i == p.Done && (p.State == MapPointState.Open || p.State == MapPointState.Current);
                var img = UiKit.Img(row, "Dot_" + (i + 1), done ? th.WaxMid : next ? th.GoldDark : UiKit.WithAlpha(th.Ink, 0.55f),
                    done ? UiKit.Disc : UiKit.Ring);
                var le = img.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = le.preferredHeight = dot;
            }
            return h;
        }

        // ---------- Sağ sütun ----------

        private void BuildLegend(MapScreenInfo info)
        {
            var th = Theme;
            Heading(info.LegendTitle);
            for (int i = 0; i < info.Points.Count; i++)
            {
                var p = info.Points[i];
                if (p.Kind != MapPointKind.Neighborhood) continue;
                var row = Row(p, true);
                var col = UiKit.Rect(row, "Col");
                var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
                v.spacing = 4f;
                v.childControlWidth = v.childControlHeight = true;
                v.childForceExpandWidth = v.childForceExpandHeight = false;
                col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                var name = UiKit.Text(col, "Name", th.HeadingFont, 34f, th.InkDeep, TextAlignmentOptions.Left, FontStyles.Bold);
                name.text = p.State == MapPointState.Current ? p.Label + "  <size=24><color=#" +
                    ColorUtility.ToHtmlStringRGB(th.GoldDark) + ">• " + info.CurrentWord + "</color></size>" : p.Label;
                if (p.Total > 0) Dots(col, p, 22f, 10f).childAlignment = TextAnchor.MiddleLeft;
                var status = UiKit.Text(col, "Status", th.BodyFont, 28f,
                    p.State == MapPointState.Locked ? UiKit.WithAlpha(th.Ink, 0.7f) : th.Copper, TextAlignmentOptions.Left);
                status.text = p.Status;
            }

            Heading(info.CitiesTitle);
            for (int i = 0; i < info.Points.Count; i++)
            {
                var p = info.Points[i];
                if (p.Kind != MapPointKind.LockedCity) continue;
                var row = Row(p, false);
                var name = UiKit.Text(row, "Name", th.BodyFont, 30f, th.Ink, TextAlignmentOptions.Left, FontStyles.Bold);
                name.text = p.Label + "  <size=24><color=#" + ColorUtility.ToHtmlStringRGB(th.PaperShade) + ">" + p.Status + "</color></size>";
                name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            }

            // Buradasın
            var you = UiKit.Rect(legend, "YouAreHere");
            built.Add(you.gameObject);
            var yh = you.gameObject.AddComponent<HorizontalLayoutGroup>();
            yh.spacing = 16f;
            yh.padding = new RectOffset(4, 0, 18, 0);
            yh.childAlignment = TextAnchor.MiddleLeft;
            yh.childControlWidth = yh.childControlHeight = true;
            yh.childForceExpandWidth = yh.childForceExpandHeight = false;
            var arrow = UiKit.Img(you, "Arrow", th.WaxMid, UiKit.Arrow);
            var ale = arrow.gameObject.AddComponent<LayoutElement>();
            ale.preferredWidth = ale.preferredHeight = 30f;
            var yt = UiKit.Text(you, "Label", th.BodyFont, 28f, th.Ink, TextAlignmentOptions.Left);
            yt.text = info.YouAreHere;

            LayoutRebuilder.ForceRebuildLayoutImmediate(legend);
        }

        private void Heading(string text)
        {
            var th = Theme;
            var t = UiKit.Text(legend, "Heading", th.HeadingFont, 30f, th.Copper, TextAlignmentOptions.Left, FontStyles.Bold);
            t.text = text;
            t.margin = new Vector4(0f, 14f, 0f, 2f);
            built.Add(t.gameObject);
        }

        private RectTransform Row(MapPointInfo p, bool big)
        {
            var row = UiKit.Rect(legend, "Row_" + p.Label);
            built.Add(row.gameObject);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 18f;
            h.padding = new RectOffset(0, 0, 6, 6);
            h.childAlignment = TextAnchor.UpperLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            float d = big ? 40f : 30f;
            var holder = UiKit.Rect(row, "Seal");
            var le = holder.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = d;
            bool muted = p.State == MapPointState.Locked || p.State == MapPointState.Soon;
            Seal(holder, p, d, muted);
            return row;
        }

        // ---------- Kurulum ----------

        private void Build()
        {
            var th = Theme;
            var rootRt = UiKit.Fill(UiKit.Rect(transform, "Map"));
            root = rootRt.gameObject;
            var scrim = UiKit.Img(rootRt, "Scrim", UiKit.WithAlpha(th.InkDeep, 0.72f), null, true);
            UiKit.Fill(scrim.rectTransform);

            var book = UiKit.Rect(rootRt, "Book");
            UiKit.Place(book, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var shadow = UiKit.Img(book, "Shadow", UiKit.WithAlpha(th.InkDeep, 0.6f), UiKit.Round8);
            UiKit.Fill(shadow.rectTransform, 0, 12, 0, -12);
            var cover = UiKit.Img(book, "Cover", th.WoodDark, UiKit.Round8);
            UiKit.Fill(cover.rectTransform);
            var grain = UiKit.Img(cover.transform, "Grain", UiKit.WithAlpha(th.Wood, 0.45f), UiKit.WoodGrain);
            UiKit.Fill(grain.rectTransform);
            var page = UiKit.Img(cover.transform, "Page", th.Paper, UiKit.Round8);
            UiKit.Fill(page.rectTransform, 22, 22, 22, 22);

            // Sol: harita (mürekkep çerçeve, kâğıda basılmış)
            var frame = UiKit.Img(page.transform, "MapFrame", th.InkDark, UiKit.Round4);
            UiKit.Place(frame.rectTransform, new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(mapSize + 8f, mapSize + 8f));
            var mapRt = UiKit.Rect(frame.transform, "MapArea");
            UiKit.Fill(mapRt, 4, 4, 4, 4);
            mapRt.gameObject.AddComponent<RectMask2D>();
            mapImage = UiKit.Rect(mapRt, "Image").gameObject.AddComponent<RawImage>();
            UiKit.Fill(mapImage.rectTransform);
            mapImage.raycastTarget = false;
            var tint = UiKit.Img(mapRt, "Vignette", UiKit.WithAlpha(th.WoodDark, 0.55f), UiKit.Vignette);
            UiKit.Fill(tint.rectTransform);
            markers = UiKit.Rect(mapRt, "Markers");
            UiKit.Fill(markers);

            // Oyuncu: kâğıt alt gölge + mum kırmızısı ok
            player = UiKit.Rect(markers, "Player");
            UiKit.Place(player, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
            var under = UiKit.Img(player, "Under", th.Paper, UiKit.Arrow);
            UiKit.Fill(under.rectTransform, -6, -8, -6, -4);
            var arrow = UiKit.Img(player, "Arrow", th.WaxMid, UiKit.Arrow);
            UiKit.Fill(arrow.rectTransform);

            // Sağ: başlık + durum listesi
            float colX = 30f + mapSize + 8f + 44f;
            title = UiKit.Text(page.transform, "Title", th.HeadingFont, 56f, th.InkDark, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            UiKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(colX, -30f), new Vector2(640f, 66f));
            subtitle = UiKit.Text(page.transform, "Subtitle", th.BodyFont, 30f, th.Copper, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            UiKit.Place(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(colX, -96f), new Vector2(640f, 40f));
            var rule = UiKit.Img(page.transform, "Rule", UiKit.WithAlpha(th.Ink, 0.25f));
            UiKit.Place(rule.rectTransform, new Vector2(0f, 1f), new Vector2(colX, -148f), new Vector2(size.x - 44f - colX - 50f, 2f));

            legend = UiKit.Rect(page.transform, "Legend");
            legend.anchorMin = legend.anchorMax = legend.pivot = new Vector2(0f, 1f);
            legend.anchoredPosition = new Vector2(colX, -156f);
            legend.sizeDelta = new Vector2(size.x - 44f - colX - 50f, 0f);
            var lv = legend.gameObject.AddComponent<VerticalLayoutGroup>();
            lv.spacing = 8f;
            lv.childControlWidth = lv.childControlHeight = true;
            lv.childForceExpandWidth = true;
            lv.childForceExpandHeight = false;
            legend.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Alt: M / Esc ipucu + Kapat
            hintRow = UiKit.Rect(page.transform, "Hint");
            UiKit.Place(hintRow, new Vector2(0f, 0f), new Vector2(colX, 40f), new Vector2(260f, 40f));
            var hrow = hintRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hrow.spacing = 10f;
            hrow.childAlignment = TextAnchor.MiddleLeft;
            hrow.childControlWidth = hrow.childControlHeight = true;
            hrow.childForceExpandWidth = hrow.childForceExpandHeight = false;
            keyMap = UiKit.KeyCap(hintRow, th, 40f, out keyMapRoot);
            keyEsc = UiKit.KeyCap(hintRow, th, 40f, out keyEscRoot);

            var btnRt = UiKit.Rect(page.transform, "Close");
            UiKit.Place(btnRt, new Vector2(1f, 0f), new Vector2(-50f, 34f), new Vector2(200f, 58f));
            var outline = UiKit.Img(btnRt, "Outline", th.InkDark, UiKit.Round8, true);
            UiKit.Fill(outline.rectTransform);
            var inner = UiKit.Img(outline.transform, "Inner", th.Wood, UiKit.Round8);
            UiKit.Fill(inner.rectTransform, 3, 3, 3, 3);
            closeLabel = UiKit.Text(inner.transform, "Label", th.BodyFont, 28f, th.Paper, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Fill(closeLabel.rectTransform);
            closeButton = btnRt.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = outline;
            var colors = closeButton.colors;
            colors.selectedColor = colors.highlightedColor = th.Gold;
            closeButton.colors = colors;
            closeButton.onClick.AddListener(RequestClose);

            root.SetActive(false);
        }
    }
}
