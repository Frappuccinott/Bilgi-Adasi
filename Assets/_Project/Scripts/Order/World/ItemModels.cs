using BilgiAdasi.Chemistry;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Bir maddenin / aletin 3D modelini kurar (raf, el, yere bırakılan eşya ortak kullanır):
    /// SpeciesData.jarPrefab (alet modeli ya da özel kavanoz), yoksa varsayılan kavanoz (jarColor ile boyanır).
    /// </summary>
    public static class ItemModels
    {
        /// <summary>
        /// Modeli kurar. jar: kökünde JarProp varsa (kavanoz; etiketli), yoksa null. Alet prefab'ları (Alet_Havan …)
        /// JarProp taşımaz: olduğu gibi kurulur.
        /// </summary>
        public static GameObject Spawn(SpeciesData species, JarProp defaultJar, Transform parent, out JarProp jar)
        {
            jar = null;
            if (species == null) return null;
            GameObject go;
            if (species.JarPrefab != null && species.JarPrefab.GetComponent<JarProp>() == null)
            {
                // Alet/malzeme modeli: varsayılan kavanozun kökü (etiket) + JarProp.AttachModel; kavanoz yoksa model tek başına.
                if (defaultJar != null)
                {
                    jar = Object.Instantiate(defaultJar, parent);
                    jar.Setup(species);
                    jar.AttachModel(species.JarPrefab);
                    go = jar.gameObject;
                }
                else go = Object.Instantiate(species.JarPrefab, parent);
            }
            else
            {
                var prefab = species.JarPrefab != null ? species.JarPrefab.GetComponent<JarProp>() : defaultJar;
                if (prefab == null) return null;
                jar = Object.Instantiate(prefab, parent);
                jar.Setup(species);
                go = jar.gameObject;
            }
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.name = "Item_" + species.Formula;
            return go;
        }

        /// <summary>Modelin renderer sınırları (dünya uzayı). Renderer yoksa boş kutu.</summary>
        public static Bounds RendererBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            bool any = false;
            var b = new Bounds(root.transform.position, Vector3.zero);
            for (int i = 0; i < renderers.Length; i++)
            {
                // Etiket metni sınırı şişirmesin.
                if (renderers[i] is MeshRenderer == false && renderers[i] is SkinnedMeshRenderer == false) continue;
                if (renderers[i].GetComponent<TMPro.TMP_Text>() != null) continue;
                if (!renderers[i].enabled) continue; // gizlenen yer tutucu gövde
                if (!any) { b = renderers[i].bounds; any = true; }
                else b.Encapsulate(renderers[i].bounds);
            }
            return b;
        }
    }
}
