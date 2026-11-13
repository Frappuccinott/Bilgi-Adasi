using System;
using System.Collections;
using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using TMPro;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Malzeme rafı: siparişin kavanozlarını raf yuvalarına koyar (Malzeme_&lt;Formül&gt; prefabları),
    /// her kavanoza StationTarget + collider ve üstüne sayı etiketi ekler.
    /// </summary>
    public sealed class WorkshopShelfView : RegisteredView<IShelfView>, IShelfView
    {
        [Serializable]
        private struct JarPrefab
        {
            [SerializeField] private SpeciesData species;
            [SerializeField] private GameObject prefab;
            public SpeciesData Species => species;
            public GameObject Prefab => prefab;
        }

        private sealed class Jar
        {
            public GameObject Root;
            public Transform Visual;
            public TMP_Text Label;
            public Animator Animator;
            public Coroutine Motion;
        }

        [SerializeField] private Transform[] slots = Array.Empty<Transform>();
        [SerializeField] private List<JarPrefab> jarPrefabs = new List<JarPrefab>();
        [Tooltip("Etiket yazısı (dünya-uzayı TMP) için şablon; boşsa düz TMP oluşturulur.")]
        [SerializeField] private TMP_FontAsset labelFont;
        [SerializeField] private float labelHeight = 0.32f;
        [SerializeField] private string pourClip = "El_Dok";
        [SerializeField] private string idleClip = "El_Bosta";

        private readonly Dictionary<SpeciesData, Jar> jars = new Dictionary<SpeciesData, Jar>();
        private readonly List<SpeciesData> shown = new List<SpeciesData>();

        public void SetItems(IReadOnlyList<ShelfItem> items)
        {
            shown.Clear();
            for (int i = 0; i < items.Count && i < slots.Length; i++)
            {
                var item = items[i];
                var jar = GetJar(item.Species);
                if (jar == null) continue;
                shown.Add(item.Species);
                jar.Root.SetActive(true);
                jar.Root.transform.SetPositionAndRotation(slots[i].position, slots[i].rotation);
                string count = item.Count > 0 ? "<b>×" + item.Count + "</b>" : string.Empty;
                string top = item.Species.FormulaRich;
                jar.Label.text = string.IsNullOrEmpty(item.Label)
                    ? top + "\n" + count
                    : top + "  <size=70%>" + item.Label + "</size>\n" + count;
            }
            foreach (var pair in jars)
                if (!shown.Contains(pair.Key)) pair.Value.Root.SetActive(false);
        }

        public void Shake(SpeciesData species)
        {
            if (species == null || !jars.TryGetValue(species, out var jar) || !jar.Root.activeSelf) return;
            Restart(jar, ShakeRoutine(jar));
        }

        public void Pour(SpeciesData species)
        {
            if (species == null || !jars.TryGetValue(species, out var jar) || !jar.Root.activeSelf) return;
            Restart(jar, HopRoutine(jar));
        }

        private void Restart(Jar jar, IEnumerator routine)
        {
            if (jar.Motion != null) StopCoroutine(jar.Motion);
            jar.Visual.localRotation = Quaternion.identity;
            jar.Visual.localPosition = Vector3.zero;
            jar.Motion = StartCoroutine(routine);
        }

        private IEnumerator HopRoutine(Jar jar)
        {
            const float duration = 0.35f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                jar.Visual.localPosition = Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.06f;
                yield return null;
            }
            jar.Visual.localPosition = Vector3.zero;
            jar.Motion = null;
        }

        private IEnumerator ShakeRoutine(Jar jar)
        {
            const float duration = 0.4f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float angle = Mathf.Sin(t * 60f) * 8f * (1f - t / duration);
                jar.Visual.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
            jar.Visual.localRotation = Quaternion.identity;
            jar.Motion = null;
        }

        private Jar GetJar(SpeciesData species)
        {
            if (species == null) return null;
            if (jars.TryGetValue(species, out var existing)) return existing;

            GameObject prefab = null;
            for (int i = 0; i < jarPrefabs.Count; i++)
                if (jarPrefabs[i].Species == species) prefab = jarPrefabs[i].Prefab;

            var root = new GameObject("Kavanoz_" + species.Formula);
            root.transform.SetParent(transform, false);
            var visualRoot = new GameObject("Visual").transform;
            visualRoot.SetParent(root.transform, false);

            if (prefab != null)
            {
                var model = Instantiate(prefab, visualRoot);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
            }
            else
            {
                var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(cyl.GetComponent<Collider>());
                cyl.transform.SetParent(visualRoot, false);
                cyl.transform.localScale = new Vector3(0.14f, 0.11f, 0.14f);
                cyl.transform.localPosition = Vector3.up * 0.11f;
            }

            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.12f, 0f);
            box.size = new Vector3(0.2f, 0.26f, 0.2f);
            var target = root.AddComponent<StationTarget>();
            target.Setup(StationTargetKind.Jar, species, visualRoot);

            var labelGo = new GameObject("Etiket");
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = Vector3.up * labelHeight;
            var label = labelGo.AddComponent<TextMeshPro>();
            if (labelFont != null) label.font = labelFont;
            label.fontSize = 0.9f;
            label.alignment = TextAlignmentOptions.Bottom;
            label.rectTransform.sizeDelta = new Vector2(0.4f, 0.2f);
            label.color = new Color(0.98f, 0.94f, 0.85f);
            labelGo.AddComponent<FaceCamera>();

            var jar = new Jar
            {
                Root = root,
                Visual = visualRoot,
                Label = label,
                Animator = visualRoot.GetComponentInChildren<Animator>()
            };
            jars[species] = jar;
            return jar;
        }
    }
}
