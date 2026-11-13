using System;
using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Ayarlar sayfası — Figma: Ekranlar (v2) / Ekran/Ayarlar. Sekmeler (Kontroller / Görüntü / Ses), satır listesi
    /// (çentikli kaydırıcı ya da ‹ Açık › seçici), sağda odaktaki ayarın açıklaması ve varsayılanı, "Varsayılana dön".
    /// Değerler GameSettings'e yazılır (PlayerPrefs) ve anında uygulanır; varsayılanlar PlayerMovementSettings asset'inden.
    /// Gezinme yeni aksiyon istemez: UI Navigate ↑↓ satır, ←→ değer / sekme; fare ile tıkla-sürükle.
    /// </summary>
    public sealed class FigmaSettingsPanel : MonoBehaviour
    {
        [Header("Etiketler")]
        [SerializeField] private string title = "Ayarlar";
        [SerializeField] private string controlsTab = "Kontroller";
        [SerializeField] private string displayTab = "Görüntü";
        [SerializeField] private string soundTab = "Ses";
        [SerializeField] private string onWord = "Açık";
        [SerializeField] private string offWord = "Kapalı";
        [SerializeField] private string defaultWord = "Varsayılan";
        [SerializeField] private string resetWord = "Varsayılana dön";
        [SerializeField] private string resetDesc = "Bu sayfadaki bütün ayarlar oyunun ilk değerlerine döner.";

        private sealed class Def
        {
            public string Label, Desc;
            public Func<float> Get, Default;
            public Action<float> Set;
            public float Min, Max, Step;
            public Func<float, string> Format;
            public bool Toggle;
        }

        private UiTheme th;
        private PlayerMovementSettings movement;
        private readonly List<List<Def>> tabs = new List<List<Def>>();
        private readonly List<TabLabel> tabItems = new List<TabLabel>();
        private readonly List<Row> rows = new List<Row>();
        private RectTransform list;
        private Selectable resetButton;
        private TextMeshProUGUI descTitle, descBody, descDefault;
        private int currentTab;

        public Selectable FirstSelectable => tabItems.Count > currentTab ? tabItems[currentTab].Selectable : null;

        public void Init(UiTheme theme, PlayerMovementSettings settings, Action<PromptAction, string> addHint)
        {
            th = theme;
            movement = settings;
            DefineSettings();
            Build(addHint);
        }

        public void Show()
        {
            ShowTab(currentTab);
            var es = EventSystem.current;
            if (es != null && rows.Count > 0) es.SetSelectedGameObject(rows[0].gameObject);
        }

        // ---------- Tanımlar ----------

        private void DefineSettings()
        {
            float mouseDef = movement != null ? movement.DefaultMouseSensitivity : 0.08f;
            float padDef = movement != null ? movement.DefaultGamepadLookSpeed : 180f;
            float fovDef = movement != null ? movement.DefaultFieldOfView : 70f;
            bool bobDef = movement == null || movement.DefaultHeadBob;

            tabs.Add(new List<Def>
            {
                new Def { Label = "Fare hassasiyeti", Desc = "Fareyi oynattığında kameranın ne kadar döneceğini belirler.",
                    Get = () => GameSettings.MouseSensitivity ?? mouseDef, Default = () => mouseDef,
                    Set = v => GameSettings.SetMouseSensitivity(v), Min = 0.02f, Max = 0.2f, Step = 0.01f, Format = v => v.ToString("0.00") },
                new Def { Label = "Gamepad bakış hızı", Desc = "Sağ çubuğu sonuna kadar ittiğinde kameranın saniyede kaç derece döneceği.",
                    Get = () => GameSettings.GamepadLookSpeed ?? padDef, Default = () => padDef,
                    Set = v => GameSettings.SetGamepadLookSpeed(v), Min = 90f, Max = 300f, Step = 10f, Format = v => v.ToString("0") + "°/sn" },
                new Def { Label = "Dikey bakışı ters çevir", Desc = "Açıkken fareyi ya da çubuğu yukarı itince kamera aşağı bakar.",
                    Get = () => GameSettings.InvertY ? 1f : 0f, Default = () => 0f,
                    Set = v => GameSettings.SetInvertY(v > 0.5f), Toggle = true },
            });
            tabs.Add(new List<Def>
            {
                new Def { Label = "Görüş alanı (FOV)", Desc = "Ekranda ne kadar geniş bir alan göreceğini belirler. Başın dönüyorsa biraz artır.",
                    Get = () => GameSettings.FieldOfView ?? fovDef, Default = () => fovDef,
                    Set = v => GameSettings.SetFieldOfView(v), Min = 60f, Max = 90f, Step = 1f, Format = v => v.ToString("0") + "°" },
                new Def { Label = "Kafa sallanması", Desc = "Yürürken kameranın adımlarla hafifçe inip kalkması. Rahatsız ederse kapat.",
                    Get = () => (GameSettings.HeadBob ?? bobDef) ? 1f : 0f, Default = () => bobDef ? 1f : 0f,
                    Set = v => GameSettings.SetHeadBob(v > 0.5f), Toggle = true },
            });
            tabs.Add(new List<Def>
            {
                new Def { Label = "Ana ses", Desc = "Oyundaki bütün seslerin (adım, kazan, ortam) yüksekliği.",
                    Get = () => GameSettings.MasterVolume * 100f, Default = () => 100f,
                    Set = v => GameSettings.SetMasterVolume(v / 100f), Min = 0f, Max = 100f, Step = 5f, Format = v => "%" + v.ToString("0") },
            });
        }

        // ---------- Sekme / satırlar ----------

        private void ShowTab(int index)
        {
            currentTab = index;
            for (int i = 0; i < tabItems.Count; i++) tabItems[i].SetActive(i == index);
            for (int i = list.childCount - 1; i >= 0; i--)
            {
                var c = list.GetChild(i).gameObject;
                if (c == resetButton.gameObject) continue;
                c.SetActive(false);
                Destroy(c);
            }
            rows.Clear();
            var defs = tabs[index];
            for (int i = 0; i < defs.Count; i++) rows.Add(MakeRow(defs[i]));
            resetButton.transform.SetAsLastSibling();

            // Gezinme: sekme ↓ ilk satır, satırlar ↑↓, son satır ↓ "Varsayılana dön"
            for (int i = 0; i < rows.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnUp = i > 0 ? rows[i - 1] : tabItems[index].Selectable;
                nav.selectOnDown = i < rows.Count - 1 ? rows[i + 1] : resetButton;
                rows[i].navigation = nav;
            }
            var rn = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = rows.Count > 0 ? rows[rows.Count - 1] : tabItems[index].Selectable };
            resetButton.navigation = rn;
            for (int i = 0; i < tabItems.Count; i++)
            {
                var tn = new Navigation { mode = Navigation.Mode.Explicit };
                if (i > 0) tn.selectOnLeft = tabItems[i - 1].Selectable;
                if (i < tabItems.Count - 1) tn.selectOnRight = tabItems[i + 1].Selectable;
                tn.selectOnDown = rows.Count > 0 ? rows[0] : resetButton;
                tabItems[i].Selectable.navigation = tn;
            }
            if (rows.Count > 0) Describe(defs[0]);
        }

        private void Describe(Def d)
        {
            if (d == null)
            {
                descTitle.text = resetWord;
                descBody.text = resetDesc;
                descDefault.text = string.Empty;
                return;
            }
            descTitle.text = d.Label;
            descBody.text = d.Desc;
            float def = d.Default();
            descDefault.text = defaultWord + ": " + (d.Toggle ? (def > 0.5f ? onWord : offWord) : d.Format(def));
        }

        private Row MakeRow(Def d)
        {
            var rt = UiKit.Rect(list, "Row_" + d.Label);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 77f;
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);

            var marker = UiKit.Img(rt, "Marker", th.GoldLight);
            UiKit.Place(marker.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(20f, 3f));
            var label = UiKit.Text(rt, "Label", th.BodyFont, 32f, th.Paper, TextAlignmentOptions.MidlineLeft);
            UiKit.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(68f, 0f), new Vector2(400f, 50f));
            label.text = d.Label;
            label.textWrappingMode = TextWrappingModes.NoWrap;

            var row = rt.gameObject.AddComponent<Row>();
            row.transition = Selectable.Transition.None;
            row.Init(this, d, th, marker, label);

            if (d.Toggle)
            {
                var box = UiKit.Rect(rt, "Toggle");
                UiKit.Place(box, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(214f, 46f));
                var left = UiKit.Text(box, "Prev", th.HeadingFont, 40f, th.Paper, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                UiKit.Fill(left.rectTransform);
                left.text = "‹";
                var right = UiKit.Text(box, "Next", th.HeadingFont, 40f, th.Paper, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
                UiKit.Fill(right.rectTransform);
                right.text = "›";
                var value = UiKit.Text(box, "Value", th.BodyFont, 32f, th.Paper, TextAlignmentOptions.Center, FontStyles.Bold);
                UiKit.Fill(value.rectTransform, 24, 0, 24, 0);
                row.SetToggle(value, onWord, offWord);
            }
            else
            {
                // Kaydırıcı: 360 px iz, 11 çentik, altın dolgu, kâğıt topuz; değer sağda
                var track = UiKit.Rect(rt, "Slider");
                UiKit.Place(track, new Vector2(1f, 0.5f), new Vector2(-24f - 134f, 0f), new Vector2(360f, 28f));
                var bg = UiKit.Img(track, "Track", UiKit.WithAlpha(th.Paper, 0.3f));
                UiKit.Place(bg.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(360f, 3f));
                for (int i = 0; i <= 10; i++)
                {
                    var tick = UiKit.Img(track, "Tick", UiKit.WithAlpha(th.Paper, 0.3f));
                    UiKit.Place(tick.rectTransform, new Vector2(0f, 0.5f), new Vector2(i * 36f, -10f), new Vector2(1.5f, 8f));
                }
                var fill = UiKit.Img(track, "Fill", th.Gold);
                UiKit.Place(fill.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(0f, 3f));
                var knob = UiKit.Img(track, "Knob", th.Paper, UiKit.Disc);
                UiKit.Place(knob.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(26f, 26f));
                knob.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var ring = UiKit.Img(knob.transform, "Ring", th.WoodDark, UiKit.Ring);
                UiKit.Fill(ring.rectTransform);
                var value = UiKit.Text(rt, "Value", th.BodyFont, 32f, th.Paper, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
                UiKit.Place(value.rectTransform, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(120f, 41f));
                value.textWrappingMode = TextWrappingModes.NoWrap;
                row.SetSlider(track, fill.rectTransform, knob.rectTransform, value);
            }
            row.Refresh();
            return row;
        }

        // ---------- Kurulum ----------

        private void Build(Action<PromptAction, string> addHint)
        {
            var page = (RectTransform)transform;

            var head = UiKit.Text(page, "Title", th.HeadingFont, 80f, th.Paper, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            UiKit.Place(head.rectTransform, new Vector2(0f, 1f), new Vector2(120f, -100f), new Vector2(800f, 110f));
            head.text = title;

            var tabBar = UiKit.Rect(page, "Tabs");
            UiKit.Place(tabBar, new Vector2(0f, 1f), new Vector2(120f, -280f), new Vector2(1000f, 60f));
            var hl = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 48f;
            hl.childControlWidth = hl.childControlHeight = true;
            hl.childForceExpandWidth = hl.childForceExpandHeight = false;
            var names = new[] { controlsTab, displayTab, soundTab };
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                var tr = UiKit.Rect(tabBar, "Tab_" + names[i]);
                var hit = tr.gameObject.AddComponent<Image>();
                hit.color = new Color(0, 0, 0, 0);
                var t = UiKit.Text(tr, "Label", th.HeadingFont, 40f, th.Paper, TextAlignmentOptions.TopLeft, FontStyles.Bold);
                UiKit.Fill(t.rectTransform, 0, 0, 0, 8);
                t.text = names[i];
                t.textWrappingMode = TextWrappingModes.NoWrap;
                var tle = tr.gameObject.AddComponent<LayoutElement>();
                tle.preferredWidth = t.GetPreferredValues(names[i]).x;
                tle.preferredHeight = 60f;
                var line = UiKit.Img(tr, "Underline", th.Gold);
                var lrt = line.rectTransform;
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = new Vector2(1f, 0f);
                lrt.pivot = new Vector2(0.5f, 0f);
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = new Vector2(0f, 3f);
                var tab = tr.gameObject.AddComponent<TabLabel>();
                tab.transition = Selectable.Transition.None;
                tab.Init(t, line, th, () => { if (currentTab != index) ShowTab(index); });
                tabItems.Add(tab);
            }
            var rule = UiKit.Img(page, "Rule", UiKit.WithAlpha(th.Paper, 0.25f));
            UiKit.Place(rule.rectTransform, new Vector2(0f, 1f), new Vector2(120f, -342f), new Vector2(1000f, 1f));

            list = UiKit.Rect(page, "List");
            UiKit.Place(list, new Vector2(0f, 1f), new Vector2(120f, -372f), new Vector2(1000f, 480f));
            var vl = list.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 4f;
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;

            // Varsayılana dön (ikincil kâğıt düğme dili: metin + altın çizgi odakta)
            var rr = UiKit.Rect(list, "Reset");
            rr.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;
            var rhit = rr.gameObject.AddComponent<Image>();
            rhit.color = new Color(0, 0, 0, 0);
            var rmark = UiKit.Img(rr, "Marker", th.GoldLight);
            UiKit.Place(rmark.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(20f, 3f));
            var rlabel = UiKit.Text(rr, "Label", th.BodyFont, 30f, UiKit.WithAlpha(th.Paper, 0.75f), TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            UiKit.Place(rlabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(68f, 0f), new Vector2(400f, 44f));
            rlabel.text = resetWord;
            var reset = rr.gameObject.AddComponent<ResetItem>();
            reset.transition = Selectable.Transition.None;
            reset.Init(rmark, rlabel, th, () =>
            {
                GameSettings.ResetAll();
                foreach (var r in rows) r.Refresh();
            }, () => Describe(null));
            resetButton = reset;

            // Açıklama (sağ)
            var desc = UiKit.Rect(page, "Description");
            UiKit.Place(desc, new Vector2(0f, 1f), new Vector2(1260f, -382f), new Vector2(540f, 260f));
            var bar = UiKit.Img(desc, "Bar", th.Gold);
            UiKit.Place(bar.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(3f, 40f));
            descTitle = UiKit.Text(desc, "Title", th.HeadingFont, 44f, th.Paper, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            UiKit.Place(descTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, -54f), new Vector2(540f, 56f));
            descBody = UiKit.Text(desc, "Body", th.BodyFont, 28f, UiKit.WithAlpha(th.Paper, 0.85f), TextAlignmentOptions.TopLeft);
            UiKit.Place(descBody.rectTransform, new Vector2(0f, 1f), new Vector2(0f, -117f), new Vector2(540f, 100f));
            descDefault = UiKit.Text(desc, "Default", th.BodyFont, 26f, UiKit.WithAlpha(th.Paper, 0.55f), TextAlignmentOptions.TopLeft);
            UiKit.Place(descDefault.rectTransform, new Vector2(0f, 1f), new Vector2(0f, -222f), new Vector2(540f, 34f));

            addHint(PromptAction.UiHorizontal, "Değiştir");
            addHint(PromptAction.UiVertical, "Gez");
            addHint(PromptAction.Cancel, "Geri");
        }

        // ---------- Satır ----------

        private sealed class Row : Selectable, IMoveHandler, IPointerDownHandler, IDragHandler, IPointerClickHandler
        {
            private FigmaSettingsPanel panel;
            private Def def;
            private UiTheme th;
            private Image marker;
            private TextMeshProUGUI label, value;
            private RectTransform track, fill, knob;
            private string on, off;
            private bool focused;

            public void Init(FigmaSettingsPanel p, Def d, UiTheme theme, Image m, TextMeshProUGUI l)
            {
                panel = p;
                def = d;
                th = theme;
                marker = m;
                label = l;
                SetFocus(false);
            }

            public void SetToggle(TextMeshProUGUI v, string onWord, string offWord)
            {
                value = v;
                on = onWord;
                off = offWord;
            }

            public void SetSlider(RectTransform t, RectTransform f, RectTransform k, TextMeshProUGUI v)
            {
                track = t;
                fill = f;
                knob = k;
                value = v;
            }

            public void Refresh()
            {
                float v = def.Get();
                if (def.Toggle)
                {
                    value.text = v > 0.5f ? on : off;
                    return;
                }
                float t = Mathf.InverseLerp(def.Min, def.Max, v);
                fill.sizeDelta = new Vector2(360f * t, 3f);
                knob.anchoredPosition = new Vector2(360f * t, 0f);
                value.text = def.Format(v);
            }

            private void Change(int dir)
            {
                if (def.Toggle) def.Set(def.Get() > 0.5f ? 0f : 1f);
                else def.Set(Mathf.Clamp(Snap(def.Get() + dir * def.Step), def.Min, def.Max));
                Refresh();
            }

            private float Snap(float v) => def.Step > 0f ? def.Min + Mathf.Round((v - def.Min) / def.Step) * def.Step : v;

            private void SetFocus(bool f)
            {
                focused = f;
                marker.color = f ? th.GoldLight : new Color(0, 0, 0, 0);
                label.color = f ? th.GoldLight : th.Paper;
            }

            public override void OnSelect(BaseEventData e)
            {
                base.OnSelect(e);
                SetFocus(true);
                panel.Describe(def);
            }

            public override void OnDeselect(BaseEventData e)
            {
                base.OnDeselect(e);
                SetFocus(false);
            }

            public override void OnPointerEnter(PointerEventData e)
            {
                base.OnPointerEnter(e);
                EventSystem.current?.SetSelectedGameObject(gameObject);
            }

            public override void OnMove(AxisEventData e)
            {
                if (e.moveDir == MoveDirection.Left) { Change(-1); e.Use(); return; }
                if (e.moveDir == MoveDirection.Right) { Change(1); e.Use(); return; }
                base.OnMove(e);
            }

            public override void OnPointerDown(PointerEventData e)
            {
                base.OnPointerDown(e);
                DragTo(e);
            }

            public void OnDrag(PointerEventData e) => DragTo(e);

            public void OnPointerClick(PointerEventData e)
            {
                if (def.Toggle) Change(1);
            }

            private void DragTo(PointerEventData e)
            {
                if (def.Toggle || track == null) return;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(track, e.position, e.pressEventCamera, out var local)) return;
                float x = local.x - track.rect.xMin;
                if (x < -40f || x > 400f) return; // izin dışına (etikete) tıklama değeri değiştirmez
                float t = Mathf.Clamp01(x / 360f);
                def.Set(Snap(Mathf.Lerp(def.Min, def.Max, t)));
                Refresh();
            }
        }

        private sealed class TabLabel : Selectable, ISubmitHandler, IPointerClickHandler
        {
            private TextMeshProUGUI label;
            private Image underline;
            private UiTheme th;
            private Action onFocus;
            public Selectable Selectable => this;

            public void Init(TextMeshProUGUI l, Image u, UiTheme theme, Action focus)
            {
                label = l;
                underline = u;
                th = theme;
                onFocus = focus;
            }

            public void SetActive(bool active)
            {
                label.color = UiKit.WithAlpha(th.Paper, active ? 1f : 0.55f);
                underline.enabled = active;
            }

            public override void OnSelect(BaseEventData e)
            {
                base.OnSelect(e);
                onFocus?.Invoke();
            }

            public void OnSubmit(BaseEventData _) => onFocus?.Invoke();
            public void OnPointerClick(PointerEventData _) => onFocus?.Invoke();
        }

        private sealed class ResetItem : Selectable, ISubmitHandler, IPointerClickHandler
        {
            private Image marker;
            private TextMeshProUGUI label;
            private UiTheme th;
            private Action onReset, onFocus;

            public void Init(Image m, TextMeshProUGUI l, UiTheme theme, Action reset, Action focus)
            {
                marker = m;
                label = l;
                th = theme;
                onReset = reset;
                onFocus = focus;
                marker.color = new Color(0, 0, 0, 0);
            }

            public override void OnSelect(BaseEventData e)
            {
                base.OnSelect(e);
                marker.color = th.GoldLight;
                label.color = th.GoldLight;
                onFocus?.Invoke();
            }

            public override void OnDeselect(BaseEventData e)
            {
                base.OnDeselect(e);
                marker.color = new Color(0, 0, 0, 0);
                label.color = UiKit.WithAlpha(th.Paper, 0.75f);
            }

            public override void OnPointerEnter(PointerEventData e)
            {
                base.OnPointerEnter(e);
                EventSystem.current?.SetSelectedGameObject(gameObject);
            }

            public void OnSubmit(BaseEventData _) => onReset?.Invoke();
            public void OnPointerClick(PointerEventData _) => onReset?.Invoke();
        }
    }
}
