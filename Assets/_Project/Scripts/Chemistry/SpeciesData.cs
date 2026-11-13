using System;
using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.Chemistry
{
    /// <summary>Bir elementin bir türdeki atom sayısı (H₂O içinde H × 2).</summary>
    [Serializable]
    public struct ElementCount
    {
        [SerializeField] private ElementData element;
        [SerializeField] private int count;

        public ElementData Element => element;
        public int Count => count;

        public ElementCount(ElementData element, int count)
        {
            this.element = element;
            this.count = count;
        }
    }

    /// <summary>
    /// Bir madde (kavanozdaki tür). Formül düz yazılır ("Ca(OH)2"), alt indisli hali otomatik üretilir.
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Kimya/Tür (madde)", fileName = "Species_")]
    public sealed class SpeciesData : ScriptableObject
    {
        [Tooltip("Düz formül: H2O, Ca(OH)2, C3H8. Rakamlar otomatik alt indis olur.")]
        [SerializeField] private string formula = "H2O";
        [SerializeField] private string displayName = "Su";
        [SerializeField] private List<ElementCount> composition = new List<ElementCount>();
        [SerializeField] private Color jarColor = new Color(0.6f, 0.8f, 1f);
        [Tooltip("Raftaki 3D kavanoz modeli (kökünde JarProp olan prefab). Boşsa kazanın varsayılan kavanozu kullanılır, jarColor ile boyanır.")]
        [SerializeField] private GameObject jarPrefab;
        [Tooltip("Pazarda bir kavanozun akçe fiyatı.")]
        [Min(0)] [SerializeField] private int price = 1;

        [NonSerialized] private string formulaRichCache;

        public string Formula => formula;
        /// <summary>TextMeshPro için alt indisli formül: H&lt;sub&gt;2&lt;/sub&gt;O</summary>
        public string FormulaRich => formulaRichCache ??= FormulaFormatter.ToRich(formula);
        public string DisplayName => displayName;
        public IReadOnlyList<ElementCount> Composition => composition;
        public Color JarColor => jarColor;
        public GameObject JarPrefab => jarPrefab;
        public int Price => price;

        private void OnValidate() => formulaRichCache = null;

        public static SpeciesData Create(string formula, string displayName, params ElementCount[] composition)
        {
            var s = CreateInstance<SpeciesData>();
            s.formula = formula;
            s.displayName = displayName;
            s.composition = new List<ElementCount>(composition);
            s.name = "Species_" + formula;
            return s;
        }
    }
}
