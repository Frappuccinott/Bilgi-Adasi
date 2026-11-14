using System.IO;
using BilgiAdasi.Desk;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Kurulum 26: masada mektup beklerken zarfın çevresinde nabız gibi atan yeşil hale (ArtDeskView.letterHalo).
    /// Doku kodla üretilir (yumuşak daire, hazır asset yok), malzeme URP Particles/Unlit toplamalı (additive).
    /// Yeniden çalıştırılabilir: var olan hale objesini ve asset'leri günceller.
    /// </summary>
    internal static class DeskGlowSetup
    {
        private const string Dir = "Assets/_Project/Art/VFX/Atolye";
        private const string TexPath = Dir + "/Mektup_Isik.png";
        private const string MatPath = Dir + "/Mektup_Isik.mat";
        private const string HaloName = "Mektup_Isik";
        private const string BeamName = "Mektup_Isik_Sutun";

        [MenuItem("BilgiAdasi/Kurulum/26 - Masada mektup ışığı (yeşil hale)", priority = 43)]
        public static void Run()
        {
            var view = Object.FindAnyObjectByType<ArtDeskView>(FindObjectsInactive.Include);
            if (view == null)
            {
                Debug.LogWarning("[Masa] Sahnede ArtDeskView yok; önce Kurulum 16 (masa).");
                return;
            }

            var mat = EnsureMaterial(EnsureTexture());
            if (mat == null) return;

            var so = new SerializedObject(view);
            var envelope = so.FindProperty("envelope").objectReferenceValue as GameObject;
            Vector3 center = envelope != null ? envelope.transform.position : view.transform.position;

            // Konum ve boy oyunda ArtDeskView'dan gelir (kameraya göre); burada yalnızca başlangıç değeri.
            var halo = EnsureQuad(view.transform, HaloName, mat, center + Vector3.up * 0.12f);
            var beam = EnsureQuad(view.transform, BeamName, mat, center + Vector3.up * 0.8f);
            beam.transform.localScale = new Vector3(0.45f, 1.8f, 1f);

            so.FindProperty("letterHalo").objectReferenceValue = halo;
            so.FindProperty("letterBeam").objectReferenceValue = beam;
            // İlk kurulumdan kalan eski değerler yeni varsayılanlara çekilir (açık yeşil, büyük hale).
            so.FindProperty("glowColor").colorValue = new Color(0.494f, 0.839f, 0.231f, 1f);
            so.FindProperty("haloSize").floatValue = 1.4f;
            so.FindProperty("beamSize").vector2Value = new Vector2(0.45f, 1.8f);
            so.FindProperty("beamAlphaMax").floatValue = 0.9f;
            so.FindProperty("haloAlphaMin").floatValue = 0.45f;
            so.FindProperty("haloAlphaMax").floatValue = 1f;
            so.ApplyModifiedProperties();

            // Sahne kaydedilmez (aynı editörde başka değişiklikler açık olabilir); Ctrl+S ile kaydedin.
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            Debug.Log("[Masa] Mektup ışığı kuruldu: " + AnimationUtility.CalculateTransformPath(halo.transform, null) + " + " + BeamName);
        }

        private static MeshRenderer EnsureQuad(Transform parent, string name, Material mat, Vector3 position)
        {
            Transform t = parent.Find(name);
            if (t == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = name;
                Object.DestroyImmediate(go.GetComponent<Collider>());
                Undo.RegisterCreatedObjectUndo(go, "Mektup ışığı");
                t = go.transform;
                t.SetParent(parent, false);
            }
            t.position = position;
            t.localScale = Vector3.one * 1.1f;
            var r = t.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            t.gameObject.SetActive(false);
            return r;
        }

        /// <summary>64×64 yumuşak daire: merkez dolu, kenara doğru üstel söner (alfa kanalında).</summary>
        private static Texture2D EnsureTexture()
        {
            EnsureFolder();
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half; // 0 merkez … 1 kenar
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);   // yumuşak kenar
                a *= Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(1f - d * 1.6f)); // merkez biraz daha parlak
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            File.WriteAllBytes(TexPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(TexPath, ImportAssetOptions.ForceUpdate);

            var imp = (TextureImporter)AssetImporter.GetAtPath(TexPath);
            imp.textureType = TextureImporterType.Default;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath);
        }

        private static Material EnsureMaterial(Texture2D tex)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                Debug.LogWarning("[Masa] URP Particles/Unlit bulunamadı; mektup ışığı kurulmadı.");
                return null;
            }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, MatPath);
            }
            mat.shader = shader;
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", new Color(0.494f, 0.839f, 0.231f, 0.8f));
            // Saydam + toplamalı: arkadaki duvarı karartmaz, üstüne yeşil ışık ekler.
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 2f);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/VFX")) AssetDatabase.CreateFolder("Assets/_Project/Art", "VFX");
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/_Project/Art/VFX", "Atolye");
        }
    }
}
