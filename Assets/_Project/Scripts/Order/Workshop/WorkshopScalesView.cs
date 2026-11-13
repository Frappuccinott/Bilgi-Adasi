using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using TMPro;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Atölyedeki iki terazi (Terazi_Giren, Terazi_Cikan). Atomlar kefelerde element başına sıra hâlinde dizilir
    /// (elementler iki kefeye sırayla dağıtılır). Dengesizken kollar hafifçe sallanır; dengede durur.
    /// </summary>
    public sealed class WorkshopScalesView : RegisteredView<IScalesView>, IScalesView
    {
        [Serializable]
        private struct AtomPrefab
        {
            [SerializeField] private ElementData element;
            [SerializeField] private GameObject prefab;
            public ElementData Element => element;
            public GameObject Prefab => prefab;
        }

        [Serializable]
        private sealed class Scale
        {
            [SerializeField] private Transform beam;
            [SerializeField] private Transform panA;
            [SerializeField] private Transform panB;
            [Tooltip("Kefelerin üst yüzü (atomların oturduğu yer). Kefenin çocuğu olmalı.")]
            [SerializeField] private Transform slotA;
            [SerializeField] private Transform slotB;
            [SerializeField] private TMP_Text label;

            [NonSerialized] public Quaternion BeamRest;
            [NonSerialized] public Vector3 PanARest, PanBRest;
            [NonSerialized] public readonly List<GameObject> Atoms = new List<GameObject>();
            [NonSerialized] public readonly Dictionary<ElementData, List<GameObject>> ByElement = new Dictionary<ElementData, List<GameObject>>();

            public Transform Beam => beam;
            public Transform PanA => panA;
            public Transform PanB => panB;
            public Transform SlotA => slotA;
            public Transform SlotB => slotB;
            public TMP_Text Label => label;
        }

        [SerializeField] private Scale left = new Scale();
        [SerializeField] private Scale right = new Scale();
        [SerializeField] private List<AtomPrefab> atomPrefabs = new List<AtomPrefab>();
        [Tooltip("Atom modeli yoksa kullanılan küre ve malzeme (renk elementten).")]
        [SerializeField] private Material fallbackMaterial;
        [SerializeField] private float atomSize = 0.06f;
        [SerializeField] private float spacing = 0.07f;
        [SerializeField] private int columns = 5;
        [Tooltip("Dengesizken kolun sallanma genliği (derece).")]
        [SerializeField] private float wobbleDegrees = 4f;
        [SerializeField] private float wobbleSpeed = 2.2f;
        [SerializeField] private Color okColor = new Color(0.35f, 0.75f, 0.35f);
        [SerializeField] private Color badColor = new Color(0.85f, 0.3f, 0.25f);

        private readonly StringBuilder sb = new StringBuilder(128);
        private readonly List<AtomCountRow> lastRows = new List<AtomCountRow>();
        private string leftTitle = "GİREN";
        private string rightTitle = "ÇIKAN";
        private bool unbalanced;
        private float wobbleWeight;
        private Coroutine flash;
        private MaterialPropertyBlock block;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            CacheRest(left);
            CacheRest(right);
        }

        private static void CacheRest(Scale s)
        {
            if (s.Beam != null) s.BeamRest = s.Beam.localRotation;
            if (s.PanA != null) s.PanARest = s.PanA.localPosition;
            if (s.PanB != null) s.PanBRest = s.PanB.localPosition;
        }

        private void Update()
        {
            float target = unbalanced ? 1f : 0f;
            wobbleWeight = Mathf.MoveTowards(wobbleWeight, target, Time.deltaTime * 2f);
            float angle = Mathf.Sin(Time.time * wobbleSpeed) * wobbleDegrees * wobbleWeight;
            ApplyTilt(left, angle);
            ApplyTilt(right, -angle);
        }

        /// <summary>Kol dünya Z ekseni etrafında döner; kefeler asılı kalır (yalnız dikey kayar).</summary>
        private static void ApplyTilt(Scale s, float angle)
        {
            if (s.Beam == null) return;
            var parent = s.Beam.parent;
            Vector3 axis = parent != null ? parent.InverseTransformDirection(Vector3.forward) : Vector3.forward;
            s.Beam.localRotation = Quaternion.AngleAxis(angle, axis) * s.BeamRest;
            if (s.PanA == null || s.PanB == null) return;
            float halfSpan = Mathf.Abs(s.PanARest.x - s.PanBRest.x) * 0.5f;
            float dy = Mathf.Sin(angle * Mathf.Deg2Rad) * halfSpan;
            float signA = s.PanARest.x > s.PanBRest.x ? 1f : -1f;
            s.PanA.localPosition = s.PanARest + Vector3.up * dy * signA;
            s.PanB.localPosition = s.PanBRest - Vector3.up * dy * signA;
        }

        public void SetTitles(string leftText, string rightText)
        {
            leftTitle = leftText;
            rightTitle = rightText;
        }

        public void ShowAtoms(IReadOnlyList<AtomCountRow> rows, bool balanced)
        {
            StopFlash();
            lastRows.Clear();
            for (int i = 0; i < rows.Count; i++) lastRows.Add(rows[i]);
            unbalanced = !balanced && HasAny(rows);

            Build(left, rows, true);
            Build(right, rows, false);
            WriteLabel(left, leftTitle, rows, true);
            WriteLabel(right, rightTitle, rows, false);
        }

        public void Clear()
        {
            StopFlash();
            lastRows.Clear();
            unbalanced = false;
            ClearAtoms(left);
            ClearAtoms(right);
            if (left.Label != null) left.Label.text = string.Empty;
            if (right.Label != null) right.Label.text = string.Empty;
        }

        public void FlashDifference(IReadOnlyDictionary<ElementData, int> difference)
        {
            StopFlash();
            flash = StartCoroutine(Flash(difference));
        }

        private static bool HasAny(IReadOnlyList<AtomCountRow> rows)
        {
            for (int i = 0; i < rows.Count; i++) if (rows[i].Left > 0) return true;
            return false;
        }

        private void Build(Scale s, IReadOnlyList<AtomCountRow> rows, bool isLeft)
        {
            ClearAtoms(s);
            int rowA = 0, rowB = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                int count = isLeft ? rows[i].Left : rows[i].Right;
                if (count <= 0) continue;
                bool onA = (i % 2 == 0) || s.SlotB == null;
                var slot = onA ? s.SlotA : s.SlotB;
                if (slot == null) continue;
                ref int row = ref onA ? ref rowA : ref rowB;
                var list = new List<GameObject>(count);
                for (int n = 0; n < count; n++)
                {
                    int col = n % columns;
                    int r = row + n / columns;
                    int inRow = Mathf.Min(columns, count - (n / columns) * columns);
                    float x = (col - (inRow - 1) * 0.5f) * spacing;
                    var atom = SpawnAtom(rows[i].Element, slot);
                    atom.transform.localPosition = new Vector3(x, atomSize * 0.5f + r * spacing, 0f);
                    list.Add(atom);
                    s.Atoms.Add(atom);
                }
                row += (count + columns - 1) / columns;
                s.ByElement[rows[i].Element] = list;
            }
        }

        private GameObject SpawnAtom(ElementData element, Transform parent)
        {
            GameObject prefab = null;
            for (int i = 0; i < atomPrefabs.Count; i++)
                if (atomPrefabs[i].Element == element) prefab = atomPrefabs[i].Prefab;

            GameObject atom;
            if (prefab != null)
            {
                atom = Instantiate(prefab, parent);
                atom.transform.localRotation = Quaternion.identity;
                atom.transform.localScale = Vector3.one;
            }
            else
            {
                atom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(atom.GetComponent<Collider>());
                atom.transform.SetParent(parent, false);
                atom.transform.localScale = Vector3.one * atomSize;
                var mr = atom.GetComponent<MeshRenderer>();
                if (fallbackMaterial != null) mr.sharedMaterial = fallbackMaterial;
                block.Clear();
                block.SetColor("_BaseColor", element != null ? element.Color : Color.white);
                mr.SetPropertyBlock(block);
            }
            atom.name = "Atom_" + (element != null ? element.Symbol : "?");
            return atom;
        }

        private static void ClearAtoms(Scale s)
        {
            for (int i = 0; i < s.Atoms.Count; i++) if (s.Atoms[i] != null) Destroy(s.Atoms[i]);
            s.Atoms.Clear();
            s.ByElement.Clear();
        }

        private void WriteLabel(Scale s, string title, IReadOnlyList<AtomCountRow> rows, bool isLeft)
        {
            if (s.Label == null) return;
            sb.Clear();
            sb.Append("<b>").Append(title).Append("</b>\n");
            for (int i = 0; i < rows.Count; i++)
            {
                var c = rows[i].Equal ? okColor : badColor;
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(c)).Append('>')
                  .Append(rows[i].Element.Symbol).Append(' ')
                  .Append(isLeft ? rows[i].Left : rows[i].Right)
                  .Append("</color>  ");
            }
            s.Label.text = sb.ToString();
        }

        private IEnumerator Flash(IReadOnlyDictionary<ElementData, int> difference)
        {
            // Fazla atomlar (sol, +) ve eksik kalan hedef atomları (sağ, −) 6 kez yanıp söner.
            for (int k = 0; k < 6; k++)
            {
                bool on = k % 2 == 0;
                foreach (var pair in difference)
                {
                    var side = pair.Value > 0 ? left : right;
                    if (!side.ByElement.TryGetValue(pair.Key, out var atoms)) continue;
                    int extra = Mathf.Abs(pair.Value);
                    for (int i = atoms.Count - 1; i >= 0 && extra > 0; i--, extra--)
                        if (atoms[i] != null) atoms[i].SetActive(on);
                }
                yield return new WaitForSeconds(0.2f);
            }
            foreach (var pair in difference)
            {
                var side = pair.Value > 0 ? left : right;
                if (!side.ByElement.TryGetValue(pair.Key, out var atoms)) continue;
                for (int i = 0; i < atoms.Count; i++) if (atoms[i] != null) atoms[i].SetActive(true);
            }
            flash = null;
        }

        private void StopFlash()
        {
            if (flash == null) return;
            StopCoroutine(flash);
            flash = null;
            SetAllActive(left);
            SetAllActive(right);
        }

        private static void SetAllActive(Scale s)
        {
            for (int i = 0; i < s.Atoms.Count; i++) if (s.Atoms[i] != null) s.Atoms[i].SetActive(true);
        }
    }
}
