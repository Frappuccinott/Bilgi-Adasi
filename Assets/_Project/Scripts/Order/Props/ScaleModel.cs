using System.Collections.Generic;
using BilgiAdasi.Core;
using TMPro;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// 3D terazi (IScaleView): sol kefe kazandaki atomlar, sağ kefe siparişin ürünleri. Her element bir sıra küre
    /// (element rengi) + sıranın solunda "H 4" etiketi (renk tek başına bilgi taşımaz). Kol, iki taraftaki atom
    /// farkına göre eğilir; kefeler hep düz durur. Köpürmede eşit olmayan satırlar kırmızı yanar ve kol sallanır.
    /// Önü -Z yönüne bakar (oyuncu -Z tarafından okur). Modeller gelince beam/pan alanları yeniden bağlanır.
    /// </summary>
    public sealed class ScaleModel : RegisteredView<IScaleView>, IScaleView
    {
        [Header("Parçalar")]
        [Tooltip("Dönen kol (pivotu ortada, yerel Z ekseninde döner).")]
        [SerializeField] private Transform beam;
        [Tooltip("Sol kefe (giren / kazan). Kolun ucuna bağlı; dünyada düz tutulur.")]
        [SerializeField] private Transform leftPan;
        [Tooltip("Sağ kefe (çıkan / sipariş).")]
        [SerializeField] private Transform rightPan;
        [Tooltip("Atom küresi (kökünde Renderer). Element rengiyle boyanır.")]
        [SerializeField] private Renderer atomPrefab;
        [SerializeField] private TMP_Text statusLabel;

        [Header("Yerleşim")]
        [SerializeField] private float atomSize = 0.045f;
        [SerializeField] private int maxPerRow = 8;
        [SerializeField] private float labelFontSize = 0.32f;
        [SerializeField] private float tiltPerAtom = 1.5f;
        [SerializeField] private float maxTilt = 12f;

        [Header("Metin ve renk (yer tutucu)")]
        [SerializeField] private string balancedText = "Dengede — ateşi yakabilirsin";
        [SerializeField] private string unbalancedText = "Dengede değil";
        [SerializeField] private Color goodColor = new Color(0.25f, 0.55f, 0.2f);
        [SerializeField] private Color badColor = new Color(0.7f, 0.18f, 0.14f);
        [SerializeField] private Color neutralColor = new Color(0.23f, 0.16f, 0.10f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private sealed class Pan
        {
            public Transform Root;
            public readonly List<Renderer> Atoms = new List<Renderer>();
            public readonly List<TMP_Text> Labels = new List<TMP_Text>();
        }

        private readonly Pan left = new Pan();
        private readonly Pan right = new Pan();
        private readonly List<AtomRow> rows = new List<AtomRow>();
        private MaterialPropertyBlock block;
        private bool balanced, foam;
        private float targetTilt, shakeUntil;

        private void Awake()
        {
            left.Root = leftPan;
            right.Root = rightPan;
            if (statusLabel != null) statusLabel.text = string.Empty;
        }

        public void ShowAtoms(IReadOnlyList<AtomRow> newRows, bool isBalanced)
        {
            CopyRows(newRows);
            balanced = isBalanced;
            Layout();
        }

        public void ShowFoamDifference(IReadOnlyList<AtomRow> newRows)
        {
            CopyRows(newRows);
            foam = true;
            shakeUntil = Time.time + 1.2f;
            Layout();
        }

        public void ClearFoamDifference()
        {
            if (!foam) return;
            foam = false;
            Layout();
        }

        private void Update()
        {
            if (beam == null) return;
            bool shaking = Time.time < shakeUntil;
            float shake = shaking ? Mathf.Sin(Time.time * 40f) * 3f : 0f;
            float z = beam.localEulerAngles.z;
            if (z > 180f) z -= 360f;
            // Kol hedefte durduysa (sallanma yok, terazi kıpırdamıyorsa) transform'lar her karede yeniden yazılmaz.
            if (!shaking && Mathf.Abs(z - targetTilt) < 0.01f && transform.hasChanged == false) return;
            transform.hasChanged = false;
            float next = Mathf.Lerp(z, targetTilt + shake, 1f - Mathf.Exp(-6f * Time.deltaTime));
            beam.localRotation = Quaternion.Euler(0f, 0f, next);

            // Kefeler kol eğilse de düz durur.
            if (leftPan != null) leftPan.rotation = transform.rotation;
            if (rightPan != null) rightPan.rotation = transform.rotation;
        }

        private void CopyRows(IReadOnlyList<AtomRow> source)
        {
            rows.Clear();
            for (int i = 0; i < source.Count; i++) rows.Add(source[i]);
        }

        private void Layout()
        {
            int totalLeft = 0, totalRight = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                totalLeft += rows[i].Left;
                totalRight += rows[i].Right;
            }
            // Ağır taraf aşağı: sol ağırsa kol saat yönünün tersine (+Z) döner → sol uç aşağı.
            targetTilt = Mathf.Clamp((totalLeft - totalRight) * tiltPerAtom, -maxTilt, maxTilt);

            DrawPan(left, true);
            DrawPan(right, false);

            if (statusLabel != null)
            {
                bool any = rows.Count > 0 && totalLeft > 0;
                statusLabel.text = !any ? string.Empty : balanced ? balancedText : unbalancedText;
                statusLabel.color = balanced ? goodColor : badColor;
            }
        }

        private void DrawPan(Pan pan, bool isLeft)
        {
            if (pan.Root == null) return;
            int atomIndex = 0;
            float spacing = atomSize * 1.1f;

            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                int count = isLeft ? row.Left : row.Right;
                int drawn = Mathf.Min(count, maxPerRow);
                float y = atomSize * 0.5f + r * spacing;
                float startX = -(drawn - 1) * 0.5f * spacing;
                Color c = row.Element != null ? row.Element.Color : Color.white;

                for (int i = 0; i < drawn; i++)
                {
                    var atom = GetAtom(pan, atomIndex++);
                    atom.transform.localPosition = new Vector3(startX + i * spacing, y, 0f);
                    Tint(atom, c);
                }

                var label = GetLabel(pan, r);
                label.transform.localPosition = new Vector3(-(maxPerRow * 0.5f + 0.8f) * spacing, y, -atomSize * 0.6f);
                string symbol = row.Element != null ? row.Element.Symbol : "?";
                string extra = count > drawn ? "+" : "";
                string leftover = isLeft && row.Leftover > 0 ? $" <size=70%>(+{row.Leftover} artan)</size>" : "";
                label.text = $"<b>{symbol}</b> {count}{extra}{leftover}";
                bool bad = !row.IsEqual && (isLeft || foam);
                label.color = !isLeft ? neutralColor : row.IsEqual ? goodColor : badColor;
                label.fontStyle = foam && bad ? FontStyles.Bold | FontStyles.Underline : FontStyles.Normal;
            }

            for (int i = atomIndex; i < pan.Atoms.Count; i++) pan.Atoms[i].gameObject.SetActive(false);
            for (int i = rows.Count; i < pan.Labels.Count; i++) pan.Labels[i].gameObject.SetActive(false);
        }

        private Renderer GetAtom(Pan pan, int index)
        {
            while (pan.Atoms.Count <= index)
            {
                var atom = Instantiate(atomPrefab, pan.Root);
                atom.transform.localScale = Vector3.one * atomSize;
                pan.Atoms.Add(atom);
            }
            var a = pan.Atoms[index];
            a.gameObject.SetActive(true);
            return a;
        }

        private TMP_Text GetLabel(Pan pan, int index)
        {
            while (pan.Labels.Count <= index)
            {
                var go = new GameObject("RowLabel", typeof(RectTransform));
                go.transform.SetParent(pan.Root, false);
                ((RectTransform)go.transform).sizeDelta = new Vector2(0.3f, 0.06f);
                var t = go.AddComponent<TextMeshPro>();
                t.fontSize = labelFontSize;
                t.alignment = TextAlignmentOptions.Right;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                pan.Labels.Add(t);
            }
            var label = pan.Labels[index];
            label.gameObject.SetActive(true);
            return label;
        }

        private void Tint(Renderer r, Color c)
        {
            block ??= new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            block.SetColor(ColorId, c);
            r.SetPropertyBlock(block);
        }
    }
}
