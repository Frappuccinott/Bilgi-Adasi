using BilgiAdasi.City;
using BilgiAdasi.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Kurulum 27: prototip kapanışı. Harita sahnesinde: UI/HUD/Ending (FigmaEndingView; Figma Masa kâğıdı + Emeği
    /// Geçenler), EndingDirector (NeighborhoodRunner'ın yanında), Data/Texts/EndingTexts.asset ve kâğıt şeklinin
    /// (Art/UI/UI_MektupKagidi.png, Figma'dan) sprite ayarı. Yeniden çalıştırılabilir; sahne kaydedilmez (Ctrl+S).
    /// </summary>
    internal static class EndingSetup
    {
        private const string PaperPath = "Assets/_Project/Art/UI/UI_MektupKagidi.png";
        private const string TextsPath = "Assets/_Project/Data/Texts/EndingTexts.asset";

        [MenuItem("BilgiAdasi/Kurulum/27 - Kapanış (veda mektubu ve Emeği Geçenler)", priority = 44)]
        public static void Run()
        {
            var hud = GameObject.Find("UI/HUD");
            var runner = Object.FindAnyObjectByType<NeighborhoodRunner>(FindObjectsInactive.Include);
            if (hud == null || runner == null)
            {
                Debug.LogWarning("[Kapanış] Harita sahnesini açın (UI/HUD ve NeighborhoodRunner gerekli).");
                return;
            }

            var paper = EnsurePaperSprite();
            var texts = AssetDatabase.LoadAssetAtPath<EndingTexts>(TextsPath);
            if (texts == null)
            {
                texts = ScriptableObject.CreateInstance<EndingTexts>();
                AssetDatabase.CreateAsset(texts, TextsPath);
                AssetDatabase.SaveAssets();
            }

            // Görünüm kendi kök Canvas'ında (UI/Ending, sıra 100): HUD'un (envanter, para) üstünde çizilir.
            // HUD'un ölçekleme ayarları kopyalanır. Eski kurulumdan kalan UI/HUD/Ending silinir.
            var old = hud.transform.Find("Ending");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var uiRoot = hud.transform.parent;
            var viewTr = uiRoot.Find("Ending");
            if (viewTr == null)
            {
                var go = new GameObject("Ending", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler),
                    typeof(UnityEngine.UI.GraphicRaycaster));
                go.layer = hud.layer;
                viewTr = go.transform;
                viewTr.SetParent(uiRoot, false);
            }
            var canvas = viewTr.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var hudScaler = hud.GetComponent<UnityEngine.UI.CanvasScaler>();
            var scaler = viewTr.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (hudScaler != null) EditorUtility.CopySerialized(hudScaler, scaler);
            var view = viewTr.GetComponent<FigmaEndingView>();
            if (view == null) view = viewTr.gameObject.AddComponent<FigmaEndingView>();
            var vso = new SerializedObject(view);
            vso.FindProperty("letterPaper").objectReferenceValue = paper;
            vso.ApplyModifiedPropertiesWithoutUndo();

            // Denetleyici: NeighborhoodRunner objesinde.
            var director = runner.GetComponent<EndingDirector>();
            if (director == null) director = runner.gameObject.AddComponent<EndingDirector>();
            var dso = new SerializedObject(director);
            dso.FindProperty("runner").objectReferenceValue = runner;
            dso.FindProperty("texts").objectReferenceValue = texts;
            dso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(runner.gameObject.scene);
            Debug.Log("[Kapanış] Kuruldu: UI/HUD/Ending + EndingDirector (" + runner.name + "). Sahneyi kaydedin.");
        }

        private static Sprite EnsurePaperSprite()
        {
            var importer = AssetImporter.GetAtPath(PaperPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[Kapanış] " + PaperPath + " yok; mektup düz kâğıt kartla çizilir.");
                return null;
            }
            if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled || !importer.alphaIsTransparency)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(PaperPath);
        }
    }
}
