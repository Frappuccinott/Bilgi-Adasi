using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Figma view'larının ortak kurucuları: kâğıt kart (ahşap çerçeve + sert gölge), tuş kapağı, halka/disk, yazı.
    /// Yuvarlak köşe, halka, disk ve onay işareti sprite'larını çalışma zamanında bir kez üretir (dış asset gerekmez).
    /// Tüm boyutlar 1920×1080 referans Canvas'a göredir (Figma kareleriyle birebir).
    /// </summary>
    public static class UiKit
    {
        private static Sprite round8, round4, pill, disc, ring, ringDashed, check, fadeUp, fadeLeft;

        public static Sprite Round8 => round8 ? round8 : round8 = MakeRound(8);
        public static Sprite Round4 => round4 ? round4 : round4 = MakeRound(4);
        /// <summary>Uçları tam yuvarlak hap (gamepad omuz/menü tuşu). Kenarlıklar yüksekliğe göre küçülür.</summary>
        public static Sprite Pill => pill ? pill : pill = MakeRound(32);
        public static Sprite Disc => disc ? disc : disc = MakeRing(64, 0f, false);
        public static Sprite Ring => ring ? ring : ring = MakeRing(64, 5f, false);
        public static Sprite RingDashed => ringDashed ? ringDashed : ringDashed = MakeRing(64, 4f, true);
        public static Sprite Check => check ? check : check = MakeCheck(64);
        /// <summary>Alttan yukarı saydamlaşan degrade (scrim alt).</summary>
        public static Sprite FadeUp => fadeUp ? fadeUp : fadeUp = MakeFade(true);
        /// <summary>Soldan sağa saydamlaşan degrade (scrim sol).</summary>
        public static Sprite FadeLeft => fadeLeft ? fadeLeft : fadeLeft = MakeFade(false);

        private static Sprite grain, vignette;
        /// <summary>
        /// Ahşap masa damarı (gri tonlu, 1024×576): beyaz = açık damar. Image rengiyle boyanır; tam ekran gerilir.
        /// Marka: "arayüz dünyanın parçası — ahşap çerçeve". Tohum sabit, her açılışta aynı desen.
        /// </summary>
        public static Sprite WoodGrain => grain ? grain : grain = MakeGrain(1024, 576);
        /// <summary>Kenarlara doğru koyulaşan vinyet (ortası saydam, kenarı opak). Image rengiyle boyanır.</summary>
        public static Sprite Vignette => vignette ? vignette : vignette = MakeVignette(256, 144);

        /// <summary>PlayStation yüz tuşu kapakları: yuvarlak kâğıt tuş, ortasında büyük çizgi sembol (□ ▲ ○ ×).</summary>
        public enum PadSymbol { Square, Triangle, Circle, Cross }

        private static readonly Sprite[] symbolSprites = new Sprite[4];

        /// <summary>Kâğıt yüz + mürekkep çerçeve + altta basılı tuş payı, renkler pişmiş (Image rengi beyaz kalır).</summary>
        public static Sprite Symbol(PadSymbol kind)
        {
            int i = (int)kind;
            if (symbolSprites[i] == null)
            {
                var th = UiTheme.OrDefault(null);
                symbolSprites[i] = MakeSymbol(kind, th.Paper, th.InkDeep);
            }
            return symbolSprites[i];
        }

        /// <summary>ControlGlyphs'in PS etiketi (× ○ □ ▲) → şekil.</summary>
        public static bool TryGetPadSymbol(string label, out PadSymbol kind)
        {
            switch (label)
            {
                case "□": kind = PadSymbol.Square; return true;
                case "▲":
                case "△": kind = PadSymbol.Triangle; return true;
                case "○": kind = PadSymbol.Circle; return true;
                case "×": kind = PadSymbol.Cross; return true;
                default: kind = PadSymbol.Square; return false;
            }
        }

        // ---------- Düzen ----------

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Figma koordinatı: anchor/pivot aynı köşe, pos o köşeden (y aşağı pozitif değil; Unity yönü).</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Fill(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Image Img(Transform parent, string name, Color color, Sprite sprite = null, bool raycast = false)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            }
            return img;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft, FontStyles style = FontStyles.Normal)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        /// <summary>
        /// Kâğıt kart: sert gölge (Figma: y 8–10, bulanıklık 0) + ahşap çerçeve + kâğıt iç. İçerik "Paper" altına kurulur.
        /// Döner: kartın kökü; içerik için <paramref name="inner"/>.
        /// </summary>
        public static RectTransform PaperCard(Transform parent, string name, UiTheme th, float border, out RectTransform inner,
            float shadowY = 8f)
        {
            var root = Rect(parent, name);
            var shadow = Img(root, "Shadow", WithAlpha(th.InkDeep, 0.45f), Round4);
            Fill(shadow.rectTransform, 0, shadowY, 0, -shadowY);
            var frame = Img(root, "Frame", th.Wood, Round4);
            Fill(frame.rectTransform);
            var paper = Img(frame.transform, "Paper", th.Paper, Round4);
            inner = Fill(paper.rectTransform, border, border, border, border);
            return root;
        }

        /// <summary>
        /// Tuş kapağı (Glyph/Klavye): kâğıt kare, koyu mürekkep çerçeve, ortada etiket. Gamepad glifi zaten renkli
        /// zengin metindir; çerçevesiz yazılır. Döner: etiket.
        /// </summary>
        public static TextMeshProUGUI KeyCap(Transform parent, UiTheme th, float height, out RectTransform root)
        {
            root = Rect(parent, "KeyCap");
            var frame = Img(root, "Frame", th.InkDeep, Round8);
            Fill(frame.rectTransform);
            var face = Img(frame.transform, "Face", th.Paper, Round8);
            Fill(face.rectTransform, 3, 3, 3, 5);
            var label = Text(face.transform, "Label", th.BodyFont, height * 0.5f, th.InkDeep, TextAlignmentOptions.Center, FontStyles.Bold);
            Fill(label.rectTransform, 6, 0, 6, 0);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            var fitter = root.gameObject.AddComponent<LayoutElement>();
            fitter.preferredHeight = height;
            fitter.minWidth = height;
            return label;
        }

        /// <summary>
        /// ControlGlyphs'in döndürdüğü simgeyi tuş kapağına çizer (son cihaza göre). Hepsi aynı marka dilinde:
        /// kâğıt yüz, koyu mürekkep çerçeve, mürekkep etiket ve altta basılı tuş payı (Glyph/Klavye ile aynı).
        /// Biçim tuşu anlatır: klavye → yuvarlak köşeli kare; yüz tuşu (A/B/X/Y, × ○ □ ▲) → daire;
        /// omuz/menü/çubuk/D-pad → uçları tam yuvarlak hap. Kapak genişliği etikete göre ayarlanır.
        /// </summary>
        public static void SetKeyCap(TextMeshProUGUI label, RectTransform root, string glyph, float height)
        {
            var th = UiTheme.OrDefault(null);
            ControlGlyphs.Describe(glyph, out var shape, out var text, out _);
            var frame = root.childCount > 0 ? root.GetChild(0).GetComponent<Image>() : null;
            var face = frame != null && frame.transform.childCount > 0 ? frame.transform.GetChild(0).GetComponent<Image>() : null;

            // PlayStation: yuvarlak tuş + vektör çizgi sembol (fonttaki □ ▲ ○ × çok küçük kalıyordu).
            if (shape == GlyphShape.Face && TryGetPadSymbol(text, out var symbol))
            {
                Style(frame, Color.white, Symbol(symbol));
                if (frame != null) frame.preserveAspect = true;
                if (face != null) face.enabled = false;
                label.text = string.Empty;
                root.sizeDelta = new Vector2(height, height);
                var sle = root.GetComponent<LayoutElement>();
                if (sle != null) sle.preferredWidth = height;
                return;
            }

            Sprite sprite = shape == GlyphShape.Face ? Disc : shape == GlyphShape.Pill ? Pill : Round8;
            Style(frame, th.InkDeep, sprite);
            Style(face, th.Paper, sprite);
            if (face != null) Fill(face.rectTransform, 3, 3, 3, 5);   // alt pay: basılı tuş derinliği

            label.text = text;
            label.color = th.InkDeep;
            label.fontSize = height * (shape == GlyphShape.Face ? 0.6f : shape == GlyphShape.Pill ? 0.42f : 0.5f);
            float w = shape == GlyphShape.Face
                ? height
                : Mathf.Max(height, label.GetPreferredValues(text, 1000f, height).x + (shape == GlyphShape.Pill ? 30f : 24f));
            root.sizeDelta = new Vector2(w, height);
            var le = root.GetComponent<LayoutElement>();
            if (le != null) le.preferredWidth = w;
        }

        private static void Style(Image img, Color color, Sprite sprite)
        {
            if (img == null) return;
            img.enabled = true;
            img.preserveAspect = false;
            img.sprite = sprite;
            img.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
        }

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        // ---------- Prosedürel sprite'lar ----------

        private static Sprite MakeRound(int radius)
        {
            int size = radius * 2 + 2;
            var tex = NewTex(size, size);
            var px = new Color32[size * size];
            float r = radius;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, r, size - r);
                float cy = Mathf.Clamp(y + 0.5f, r, size - r);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(r - d + 0.5f)));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        private static Sprite MakeRing(int size, float thickness, bool dashed)
        {
            var tex = NewTex(size, size);
            var px = new Color32[size * size];
            float c = size * 0.5f, outer = c - 1f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - c, dy = y + 0.5f - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(outer - d + 0.5f);
                if (thickness > 0f) a *= Mathf.Clamp01(d - (outer - thickness) + 0.5f);
                if (dashed)
                {
                    float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 180f; // 0–360
                    if (Mathf.Repeat(ang, 30f) > 17f) a = 0f; // 12 parça
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * a));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite MakeCheck(int size)
        {
            var tex = NewTex(size, size);
            var px = new Color32[size * size];
            // Figma'daki onay: kısa sol kol + uzun sağ kol, yuvarlak uçlu.
            Vector2 a = new Vector2(0.22f, 0.52f) * size, b = new Vector2(0.42f, 0.30f) * size, c = new Vector2(0.78f, 0.72f) * size;
            float half = size * 0.085f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d = Mathf.Min(SegDist(p, a, b), SegDist(p, b, c));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(half - d + 0.5f)));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite MakeFade(bool vertical)
        {
            const int n = 64;
            var tex = vertical ? NewTex(1, n) : NewTex(n, 1);
            var px = new Color32[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (n - 1f);
                // vertical: altta opak (i=0) → üstte saydam; horizontal: solda opak → sağda saydam
                float a = 1f - Mathf.SmoothStep(0f, 1f, t);
                px[i] = new Color32(255, 255, 255, (byte)(255 * a));
            }
            tex.SetPixels32(px);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite MakeGrain(int w, int h)
        {
            var tex = NewTex(w, h);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                // Tahta şeritleri: her ~96 px'de bir derz, şerit başına hafif ton farkı.
                int plank = y / 96;
                float plankTone = 0.86f + 0.14f * Mathf.PerlinNoise(plank * 3.1f, 7.7f);
                float seam = Mathf.Abs((y % 96) - 0.5f) < 1.5f ? 0.55f : 1f;
                for (int x = 0; x < w; x++)
                {
                    float wobble = Mathf.PerlinNoise(x * 0.004f, plank * 1.7f) * 22f;
                    float ring = Mathf.PerlinNoise(x * 0.0025f + plank * 5f, (y + wobble) * 0.09f);
                    float fine = Mathf.PerlinNoise(x * 0.02f, y * 0.6f) * 0.25f;
                    float v = Mathf.Clamp01((0.55f + ring * 0.35f + fine * 0.4f) * plankTone * seam);
                    byte b = (byte)(v * 255);
                    px[y * w + x] = new Color32(b, b, b, 255);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite MakeVignette(int w, int h)
        {
            var tex = NewTex(w, h);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f) / w * 2f - 1f, dy = (y + 0.5f) / h * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.1f);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1.35f, d));
                px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        private static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>
        /// PS kapağı: Glyph/Klavye diliyle yuvarlak tuş (kâğıt yüz, mürekkep çerçeve, altta basılı tuş payı) ve
        /// ortasında kapağın ~%40'ı boyunda kalın çizgili mürekkep sembol. İşaretli uzaklık alanıyla (SDF) çizilir;
        /// fonttaki küçük □ ▲ ○ × karakterlerinin yerine geçer.
        /// </summary>
        private static Sprite MakeSymbol(PadSymbol kind, Color paper, Color ink)
        {
            const int size = 128;
            const float radius = 55f, border = 7f, depth = 6f;
            var tex = NewTex(size, size);
            var px = new Color32[size * size];
            var center = new Vector2(size * 0.5f, size * 0.5f + depth * 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float d = p.magnitude - radius;
                float dDepth = (p + new Vector2(0f, depth)).magnitude - radius;
                float paperA = Mathf.Clamp01(0.5f - (d + border));
                float iconA = Mathf.Clamp01(0.5f - IconDistance(kind, p));
                var c = Color.Lerp(ink, paper, paperA);
                c = Color.Lerp(c, ink, iconA);
                c.a = Mathf.Max(Mathf.Clamp01(0.5f - d), Mathf.Clamp01(0.5f - dDepth));
                px[y * size + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Sembol çizgisinin işaretli uzaklığı (piksel, çizgi içinde negatif). Merkez (0,0), y yukarı.</summary>
        private static float IconDistance(PadSymbol kind, Vector2 p)
        {
            const float stroke = 4.75f; // yarı kalınlık: 128 px'te ~9.5 px → 44 px kapakta ~3.3 px
            switch (kind)
            {
                case PadSymbol.Circle:
                    return Mathf.Abs(p.magnitude - 21f) - stroke;
                case PadSymbol.Square:
                    return Mathf.Abs(Box(p, new Vector2(17f, 17f)) - 2.5f) - stroke;
                case PadSymbol.Triangle:
                {
                    // Eşkenar üçgen: şeklin kutusu ortalanır (ağırlık merkezi değil), optik olarak biraz yukarı.
                    var q = p + new Vector2(0f, 6f);
                    return Mathf.Abs(Triangle(q, 24f) - 1.5f) - stroke;
                }
                default: // Cross: 45° dönmüş iki kalın çizgi, uçlar yuvarlak
                {
                    const float c = 0.70710678f;
                    var q = new Vector2((p.x + p.y) * c, (p.y - p.x) * c);
                    return Mathf.Min(Box(q, new Vector2(24f, 0f)), Box(q, new Vector2(0f, 24f))) - stroke;
                }
            }
        }

        private static float Box(Vector2 p, Vector2 half)
        {
            float qx = Mathf.Abs(p.x) - half.x, qy = Mathf.Abs(p.y) - half.y;
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f);
        }

        /// <summary>Yukarı bakan eşkenar üçgen (yarı kenar r); Inigo Quilez SDF formülü (genel bilinen matematik).</summary>
        private static float Triangle(Vector2 p, float r)
        {
            const float k = 1.7320508f;
            p.x = Mathf.Abs(p.x) - r;
            p.y = p.y + r / k;
            if (p.x + k * p.y > 0f) p = new Vector2(p.x - k * p.y, -k * p.x - p.y) * 0.5f;
            p.x -= Mathf.Clamp(p.x, -2f * r, 0f);
            return -p.magnitude * Mathf.Sign(p.y);
        }

        private static Texture2D NewTex(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
        }
    }
}
