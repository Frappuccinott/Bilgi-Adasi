using BilgiAdasi.City;
using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Sol alttaki mini harita (IMinimapView) — ahşap çember, içinde adanın üstten görüntüsü. Oyuncu ortada, baktığı yön
    /// hep yukarıda (harita döner); çemberin üstünde kuzey harfi döner. Köyler kimlik renginde küçük mühür (kilitliyse
    /// soluk), sıradaki köy altın halkayla. Altında tuş ipucu "M Harita". Yalnızca oyuncu modunda görünür.
    /// Sol alt köşede (defter sırtı sağ altta, karar 14.11).
    /// </summary>
    public sealed class FigmaMinimapView : FigmaView<IMinimapView>, IMinimapView
    {
        [SerializeField] private Vector2 position = new Vector2(36f, 24f);
        [SerializeField] private float diameter = 236f;
        [SerializeField] private float ring = 12f;
        [SerializeField] private float fadeSeconds = 0.2f;

        private CanvasGroup group;
        private RectTransform rotator, content, markers, north, keyRoot, hint;
        private RawImage image;
        private TextMeshProUGUI northLabel, keyLabel, hintLabel;
        private float imageSize = 600f, target = 1f;
        private bool wanted = true;

        private float Inner => diameter - ring * 2f;

        private void Awake() => Build();

        protected override void OnEnable()
        {
            base.OnEnable();
            GameInput.SchemeChanged += OnScheme;
            // Konuşmada sol alt konuşanın portresine kalır: mini harita çekilir.
            FigmaDialogueView.VisibilityChanged += OnDialogue;
        }

        protected override void OnDisable()
        {
            GameInput.SchemeChanged -= OnScheme;
            FigmaDialogueView.VisibilityChanged -= OnDialogue;
            base.OnDisable();
        }

        private void OnDialogue(bool _) => Refresh();

        private void Refresh() => target = wanted && !FigmaDialogueView.IsVisible ? 1f : 0f;

        public void Setup(MapScreenInfo info)
        {
            var th = Theme;
            image.texture = info.Image;
            imageSize = Inner / Mathf.Max(0.05f, info.MinimapUvSpan);
            content.sizeDelta = new Vector2(imageSize, imageSize);
            northLabel.text = info.NorthLetter;
            hintLabel.text = info.MapWord;

            for (int i = markers.childCount - 1; i >= 0; i--) Destroy(markers.GetChild(i).gameObject);
            for (int i = 0; i < info.Points.Count; i++)
            {
                var p = info.Points[i];
                if (!p.OnMinimap) continue;
                bool muted = p.State == MapPointState.Locked || p.State == MapPointState.Soon;
                bool village = p.Kind == MapPointKind.Neighborhood;
                float d = village ? 22f : 16f;
                var at = (p.Uv - new Vector2(0.5f, 0.5f)) * imageSize;
                if (p.State == MapPointState.Current)
                {
                    var halo = UiKit.Img(markers, "Halo_" + p.Label, th.Gold, UiKit.Ring);
                    UiKit.Place(halo.rectTransform, new Vector2(0.5f, 0.5f), at, new Vector2(d + 14f, d + 14f));
                }
                var edge = UiKit.Img(markers, "Yer_" + p.Label, muted ? UiKit.WithAlpha(th.InkDeep, 0.5f) : th.InkDeep, UiKit.Disc);
                UiKit.Place(edge.rectTransform, new Vector2(0.5f, 0.5f), at, new Vector2(d, d));
                var face = UiKit.Img(edge.transform, "Face", muted ? Color.Lerp(p.Color, th.PaperShade, 0.7f) : p.Color, UiKit.Disc);
                UiKit.Fill(face.rectTransform, 3, 3, 3, 3);
                if (p.State == MapPointState.Done)
                {
                    var check = UiKit.Img(edge.transform, "Check", th.Paper, UiKit.Check);
                    UiKit.Fill(check.rectTransform, 3, 3, 3, 3);
                }
            }
            OnScheme(GameInput.LastScheme);
        }

        public void SetVisible(bool visible)
        {
            wanted = visible;
            Refresh();
        }

        public void SetPlayer(Vector2 uv, float yawDegrees)
        {
            // Harita, bakılan yön yukarıda kalacak şekilde döner; kuzey harfi çemberde aynı açıyla.
            rotator.localRotation = Quaternion.Euler(0f, 0f, yawDegrees);
            content.anchoredPosition = -(uv - new Vector2(0.5f, 0.5f)) * imageSize;
            float r = diameter * 0.5f - ring * 0.5f, a = yawDegrees * Mathf.Deg2Rad;
            north.anchoredPosition = new Vector2(-Mathf.Sin(a), Mathf.Cos(a)) * r;
        }

        private void Update()
        {
            if (Mathf.Approximately(group.alpha, target)) return;
            group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds));
        }

        private void OnScheme(ControlScheme _)
        {
            if (keyLabel == null) return;
            UiKit.SetKeyCap(keyLabel, keyRoot, ControlGlyphs.For(PromptAction.OpenMap), 36f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(hint);
        }

        private void Build()
        {
            var th = Theme;
            var rt = UiKit.Rect(transform, "Minimap");
            UiKit.Place(rt, Vector2.zero, position, new Vector2(diameter, diameter + 48f));
            group = rt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = group.interactable = false;

            var disc = UiKit.Rect(rt, "Disc");
            UiKit.Place(disc, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(diameter, diameter));

            // Çember: sert gölge + ahşap halka (damarlı)
            var shadow = UiKit.Img(disc, "Shadow", UiKit.WithAlpha(th.InkDeep, 0.5f), UiKit.Disc);
            UiKit.Fill(shadow.rectTransform, 0, 8, 0, -8);
            var wood = UiKit.Img(disc, "Ring", th.WoodDark, UiKit.Disc);
            UiKit.Fill(wood.rectTransform);
            var grain = UiKit.Img(disc, "RingGrain", UiKit.WithAlpha(th.Wood, 0.55f), UiKit.Ring);
            UiKit.Fill(grain.rectTransform, 3, 3, 3, 3);

            // Maske (daire) → döner kök → kayan görüntü
            var mask = UiKit.Img(disc, "Mask", th.Paper, UiKit.Disc);
            UiKit.Fill(mask.rectTransform, ring, ring, ring, ring);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            rotator = UiKit.Rect(mask.transform, "Rotator");
            UiKit.Place(rotator, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            content = UiKit.Rect(rotator, "Content");
            UiKit.Place(content, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(imageSize, imageSize));
            image = UiKit.Rect(content, "Image").gameObject.AddComponent<RawImage>();
            UiKit.Fill(image.rectTransform);
            image.raycastTarget = false;
            markers = UiKit.Rect(content, "Markers");
            UiKit.Fill(markers);
            var vignette = UiKit.Img(mask.transform, "Vignette", UiKit.WithAlpha(th.WoodDark, 0.5f), UiKit.Ring);
            UiKit.Fill(vignette.rectTransform, -2, -2, -2, -2);

            // Oyuncu: ortada, hep yukarı
            var player = UiKit.Rect(mask.transform, "Player");
            UiKit.Place(player, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
            var under = UiKit.Img(player, "Under", th.Paper, UiKit.Arrow);
            UiKit.Fill(under.rectTransform, -5, -6, -5, -3);
            var arrow = UiKit.Img(player, "Arrow", th.WaxMid, UiKit.Arrow);
            UiKit.Fill(arrow.rectTransform);

            // Kuzey: çemberin üstünde kâğıt rozet
            north = UiKit.Rect(disc, "North");
            UiKit.Place(north, new Vector2(0.5f, 0.5f), new Vector2(0f, diameter * 0.5f), new Vector2(30f, 30f));
            var nb = UiKit.Img(north, "Edge", th.InkDeep, UiKit.Disc);
            UiKit.Fill(nb.rectTransform);
            var nf = UiKit.Img(nb.transform, "Face", th.Paper, UiKit.Disc);
            UiKit.Fill(nf.rectTransform, 2, 2, 2, 2);
            northLabel = UiKit.Text(nf.transform, "Label", th.HeadingFont, 20f, th.WaxDark, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Fill(northLabel.rectTransform);

            // Altında: tuş ipucu
            hint = UiKit.Rect(rt, "Hint");
            UiKit.Place(hint, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(diameter, 36f));
            var h = hint.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            keyLabel = UiKit.KeyCap(hint, th, 36f, out keyRoot);
            hintLabel = UiKit.Text(hint, "Label", th.BodyFont, 28f, th.Paper, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            hintLabel.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }
}
