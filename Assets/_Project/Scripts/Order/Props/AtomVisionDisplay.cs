using System.Collections.Generic;
using System.Text;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using BilgiAdasi.UI;
using TMPro;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Atom Görüşü (IAtomVisionView, 04-gorev-plani §2.4): tepkime anında kazanın üstünde, oyuncuya dönük:
    /// 1) girenler molekül molekül belirir (solda), 2) bağlar kopar, her atom ürün molekülündeki yerine uçar (sağda),
    /// 3) altta sayaç "H 4 → 4 · O 2 → 2"; Purchase'ta eşi kalmayan moleküller solda gri kalır, üstünde "artan".
    /// Atom şekli/rengi terazidekiyle aynı (elementShapes). Animasyon verisi tepkimeden otomatik; elle animasyon yok.
    /// Kazan objesinin üstünde boş bir objede durur (kök = görüntünün merkezi).
    /// </summary>
    public sealed class AtomVisionDisplay : RegisteredView<IAtomVisionView>, IAtomVisionView
    {
        [System.Serializable]
        private struct ElementShape
        {
            [SerializeField] private ElementData element;
            [SerializeField] private Mesh mesh;
            public ElementData Element => element;
            public Mesh Mesh => mesh;
        }

        [Tooltip("Atom (kökünde Renderer). Element rengiyle boyanır; şekil elementShapes'ten.")]
        [SerializeField] private Renderer atomPrefab;
        [SerializeField] private List<ElementShape> elementShapes = new List<ElementShape>();
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text counterLabel;
        [Tooltip("Görüntü bu kameraya döner (boşsa Camera.main).")]
        [SerializeField] private Transform facing;
        [Tooltip("Görüntü kazandan oyuncuya doğru bu kadar öne çıkar (m): kazanın arkasındaki duvara/rafa girmesin.")]
        [SerializeField] private float towardViewer = 0.85f;

        [Header("Ölçü")]
        [SerializeField] private float atomSize = 0.085f;
        [Tooltip("Molekül içi atom aralığı.")]
        [SerializeField] private float bondLength = 0.09f;
        [Tooltip("Moleküller arası aralık.")]
        [SerializeField] private float moleculeGap = 0.26f;
        [Tooltip("Girenler ile ürünler arası yarı genişlik.")]
        [SerializeField] private float sideOffset = 0.5f;
        [SerializeField] private int moleculesPerRow = 4;

        [Header("Zaman (sn)")]
        [SerializeField] private float appearTime = 0.5f;
        [SerializeField] private float holdBefore = 0.8f;
        [SerializeField] private float moveTime = 1.1f;
        [SerializeField] private float holdAfter = 2.6f;
        [SerializeField] private float fadeTime = 0.4f;
        [SerializeField] private Color leftoverColor = new Color(0.55f, 0.52f, 0.48f);

        private sealed class AtomFx
        {
            public Renderer Renderer;
            public Vector3 From;
            public Vector3 To;
            public bool Moves;
            public float Delay;
        }

        private readonly List<AtomFx> atoms = new List<AtomFx>();
        private readonly List<Renderer> pool = new List<Renderer>();
        private readonly List<TMP_Text> leftoverLabels = new List<TMP_Text>();
        private readonly Dictionary<ElementData, int> leftCount = new Dictionary<ElementData, int>();
        private readonly Dictionary<ElementData, int> rightCount = new Dictionary<ElementData, int>();
        private readonly Dictionary<ElementData, Queue<Vector3>> slots = new Dictionary<ElementData, Queue<Vector3>>();
        private readonly List<ElementData> elementOrder = new List<ElementData>();
        private readonly StringBuilder sb = new StringBuilder(128);
        private MaterialPropertyBlock block;
        private Transform content;
        private float time = -1f;
        private float total;
        private Vector3 restLocal;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            restLocal = transform.localPosition;
            content = new GameObject("Icerik").transform;
            content.SetParent(transform, false);
            content.localPosition = new Vector3(0f, -0.12f, 0f);
            SetVisible(false);
            var font = UiTheme.OrDefault(null).BodyFont;
            if (font != null)
            {
                if (titleLabel != null) titleLabel.font = font;
                if (counterLabel != null) counterLabel.font = font;
            }
        }

        // ---------- IAtomVisionView ----------

        public void Play(IReadOnlyList<SpeciesAmount> reactants, IReadOnlyList<SpeciesAmount> leftovers, IReadOnlyList<SpeciesAmount> products)
        {
            if (atomPrefab == null) return;
            Clear();
            FaceViewer();

            // Ürün molekülleri sağda; her atomun hedef yeri element kuyruğuna girer.
            slots.Clear();
            int productMolecules = CountMolecules(products);
            int reactantMolecules = CountMolecules(reactants) + CountMolecules(leftovers);
            int index = 0;
            for (int i = 0; i < products.Count; i++)
                for (int m = 0; m < products[i].Amount; m++)
                    LayoutMolecule(products[i].Species, MoleculeCenter(index++, productMolecules, +1f), null, 0f, true);

            // Girenler solda; atomlar sırayla ürün yerlerine uçar.
            index = 0;
            float stagger = 0f;
            for (int i = 0; i < reactants.Count; i++)
                for (int m = 0; m < reactants[i].Amount; m++)
                {
                    LayoutMolecule(reactants[i].Species, MoleculeCenter(index++, reactantMolecules, -1f), slots, stagger, false);
                    stagger += 0.06f;
                }

            // Artanlar: solda kalır, gri, üstünde etiket.
            var texts = WorkshopTexts.Get();
            for (int i = 0; i < leftovers.Count; i++)
                for (int m = 0; m < leftovers[i].Amount; m++)
                {
                    var c = MoleculeCenter(index++, reactantMolecules, -1f);
                    LayoutMolecule(leftovers[i].Species, c, null, 0f, false, true);
                    AddLeftoverLabel(c + Vector3.up * (bondLength * 1.6f + atomSize), texts.LeftoverLabel);
                }

            BuildCounter(reactants, leftovers, products, texts);
            if (titleLabel != null) titleLabel.text = texts.AtomTitle;

            total = appearTime + holdBefore + moveTime + stagger + holdAfter + fadeTime;
            time = 0f;
            SetVisible(true);
        }

        // ---------- yerleşim ----------

        private int CountMolecules(IReadOnlyList<SpeciesAmount> list)
        {
            int n = 0;
            for (int i = 0; i < list.Count; i++) n += Mathf.Max(0, list[i].Amount);
            return n;
        }

        /// <summary>side −1: girenler (sol), +1: ürünler (sağ). Satırlar yukarıdan aşağı.</summary>
        private Vector3 MoleculeCenter(int index, int count, float side)
        {
            int perRow = Mathf.Max(1, moleculesPerRow);
            int rows = Mathf.Max(1, (count + perRow - 1) / perRow);
            int row = index / perRow;
            int col = index % perRow;
            int inRow = Mathf.Min(perRow, count - row * perRow);
            float x = side * sideOffset + (col - (inRow - 1) * 0.5f) * moleculeGap * 0.5f;
            float y = ((rows - 1) * 0.5f - row) * moleculeGap;
            return new Vector3(x, y, 0f);
        }

        /// <summary>
        /// Molekülün atomlarını merkez çevresine dizer. target != null: atomlar oradaki ilk boş ürün yerine uçar.
        /// isProduct: yer yalnızca kuyruğa yazılır (atom çizilmez; uçan atomlar gelir).
        /// </summary>
        private void LayoutMolecule(SpeciesData s, Vector3 center, Dictionary<ElementData, Queue<Vector3>> target,
            float delay, bool isProduct, bool leftover = false)
        {
            if (s == null) return;
            int n = 0;
            var comp = s.Composition;
            for (int i = 0; i < comp.Count; i++) n += comp[i].Count;
            int k = 0;
            for (int i = 0; i < comp.Count; i++)
            {
                var e = comp[i].Element;
                for (int c = 0; c < comp[i].Count; c++)
                {
                    var pos = center + AtomOffset(k++, n);
                    if (isProduct)
                    {
                        if (!slots.TryGetValue(e, out var q)) slots[e] = q = new Queue<Vector3>();
                        q.Enqueue(pos);
                        continue;
                    }
                    var fx = new AtomFx { Renderer = GetAtom(atoms.Count), From = pos, To = pos, Delay = delay };
                    if (target != null && e != null && target.TryGetValue(e, out var queue) && queue.Count > 0)
                    {
                        fx.To = queue.Dequeue();
                        fx.Moves = true;
                    }
                    Paint(fx.Renderer, e, leftover);
                    atoms.Add(fx);
                }
            }
        }

        /// <summary>Molekül içi düzen: 1 atom merkez, 2 yan yana, 3+ halka (ilk atom ortada değil, eşit aralıklı).</summary>
        private Vector3 AtomOffset(int i, int n)
        {
            if (n <= 1) return Vector3.zero;
            if (n == 2) return new Vector3((i - 0.5f) * bondLength, 0f, 0f);
            float r = bondLength * (n <= 4 ? 0.75f : 1.05f);
            float a = Mathf.PI * 0.5f + i * Mathf.PI * 2f / n;
            return new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
        }

        private Renderer GetAtom(int index)
        {
            while (pool.Count <= index)
            {
                var r = Instantiate(atomPrefab, content);
                var col = r.GetComponent<Collider>();
                if (col != null) Destroy(col);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                pool.Add(r);
            }
            var atom = pool[index];
            atom.gameObject.SetActive(true);
            return atom;
        }

        private void Paint(Renderer r, ElementData e, bool leftover)
        {
            Mesh mesh = null;
            for (int i = 0; i < elementShapes.Count; i++)
                if (elementShapes[i].Element == e) mesh = elementShapes[i].Mesh;
            var filter = r.GetComponent<MeshFilter>();
            if (filter != null && mesh != null) filter.sharedMesh = mesh;
            float scale = atomSize;
            if (filter != null && filter.sharedMesh != null)
            {
                var size = filter.sharedMesh.bounds.size;
                float max = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
                if (max > 0.0001f) scale = atomSize / max;
            }
            r.transform.localScale = Vector3.one * scale;
            r.transform.localRotation = Quaternion.identity;
            if (block == null) block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            Color c = leftover ? leftoverColor : (e != null ? e.Color : Color.white);
            block.SetColor(BaseColorId, c);
            block.SetColor(ColorId, c);
            r.SetPropertyBlock(block);
        }

        private void AddLeftoverLabel(Vector3 pos, string text)
        {
            if (counterLabel == null) return;
            TMP_Text label = null;
            for (int i = 0; i < leftoverLabels.Count; i++)
                if (!leftoverLabels[i].gameObject.activeSelf) { label = leftoverLabels[i]; break; }
            if (label == null)
            {
                label = Instantiate(counterLabel, content);
                label.fontSize = counterLabel.fontSize * 0.8f;
                leftoverLabels.Add(label);
            }
            label.gameObject.SetActive(true);
            label.transform.localPosition = pos;
            label.transform.localRotation = Quaternion.identity;
            label.text = text;
        }

        private void BuildCounter(IReadOnlyList<SpeciesAmount> reactants, IReadOnlyList<SpeciesAmount> leftovers,
            IReadOnlyList<SpeciesAmount> products, WorkshopTexts texts)
        {
            if (counterLabel == null) return;
            AtomMath.Count(reactants, leftCount);
            AtomMath.Count(products, rightCount);
            elementOrder.Clear();
            for (int i = 0; i < reactants.Count; i++) AddElements(reactants[i].Species);
            for (int i = 0; i < products.Count; i++) AddElements(products[i].Species);
            sb.Clear();
            for (int i = 0; i < elementOrder.Count; i++)
            {
                var e = elementOrder[i];
                leftCount.TryGetValue(e, out int l);
                rightCount.TryGetValue(e, out int r);
                if (i > 0) sb.Append("   ·   ");
                sb.Append("<b>").Append(e.Symbol).Append("</b> ").Append(l).Append(" → ").Append(r);
            }
            if (leftovers.Count > 0)
            {
                sb.Append("\n<size=80%>").Append(texts.LeftoverLabel).Append(':');
                for (int i = 0; i < leftovers.Count; i++)
                    sb.Append(' ').Append(leftovers[i].Species.FormulaRich).Append(" ×").Append(leftovers[i].Amount);
                sb.Append("</size>");
            }
            sb.Append("\n<size=70%><i>").Append(texts.AtomFooter).Append("</i></size>");
            counterLabel.text = sb.ToString();
        }

        private void AddElements(SpeciesData s)
        {
            if (s == null) return;
            var comp = s.Composition;
            for (int i = 0; i < comp.Count; i++)
                if (comp[i].Element != null && !elementOrder.Contains(comp[i].Element)) elementOrder.Add(comp[i].Element);
        }

        // ---------- animasyon ----------

        private void Update()
        {
            if (time < 0f) return;
            time += Time.deltaTime;
            float appear = Mathf.Clamp01(time / appearTime);
            float moveStart = appearTime + holdBefore;
            float fadeStart = total - fadeTime;
            float fade = time > fadeStart ? 1f - Mathf.Clamp01((time - fadeStart) / fadeTime) : 1f;
            float pop = Mathf.SmoothStep(0f, 1f, appear) * fade;

            for (int i = 0; i < atoms.Count; i++)
            {
                var a = atoms[i];
                float t = a.Moves ? Mathf.Clamp01((time - moveStart - a.Delay) / moveTime) : 0f;
                float s = Mathf.SmoothStep(0f, 1f, t);
                // Bağ kopunca atom biraz yukarı kavis çizer (yolda birbirine girmesin).
                var p = Vector3.Lerp(a.From, a.To, s) + Vector3.up * Mathf.Sin(s * Mathf.PI) * 0.12f
                        + Vector3.forward * Mathf.Sin(s * Mathf.PI) * (i % 2 == 0 ? 0.05f : -0.05f);
                a.Renderer.transform.localPosition = p + Vector3.down * (1f - pop) * 0.25f;
                a.Renderer.transform.localRotation = Quaternion.Euler(0f, time * 40f, 0f);
            }
            float labelAlpha = Mathf.Clamp01((time - moveStart - moveTime * 0.6f) / 0.4f) * fade;
            if (counterLabel != null) counterLabel.alpha = labelAlpha;
            if (titleLabel != null) titleLabel.alpha = pop;
            for (int i = 0; i < leftoverLabels.Count; i++) leftoverLabels[i].alpha = labelAlpha;
            content.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, pop);

            if (time >= total)
            {
                time = -1f;
                Clear();
                SetVisible(false);
            }
        }

        private void Clear()
        {
            atoms.Clear();
            for (int i = 0; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
            for (int i = 0; i < leftoverLabels.Count; i++) leftoverLabels[i].gameObject.SetActive(false);
        }

        private void SetVisible(bool v)
        {
            if (content != null) content.gameObject.SetActive(v);
            if (titleLabel != null) titleLabel.gameObject.SetActive(v);
            if (counterLabel != null) counterLabel.gameObject.SetActive(v);
        }

        private void FaceViewer()
        {
            var f = facing != null ? facing : (Camera.main != null ? Camera.main.transform : null);
            if (f == null) return;
            transform.localPosition = restLocal;
            var dir = transform.position - f.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            dir.Normalize();
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            transform.position -= dir * towardViewer;
        }
    }
}
