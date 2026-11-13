using System.IO;
using BilgiAdasi.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Kurulum 14 — Figma arayüzü ("Bilgi Adası — Marka Kimliği": Ekranlar (v2) + Keşif Defteri ve Başarılar).
    /// 1) Art/UI'daki mühür PNG'lerini sprite yapar. 2) Resources/UiTheme.asset'i kurar (palet, mühür, varsa Alegreya fontları).
    /// 3) Town sahnesinde "UI/HUD" Canvas'ı altına Figma view'larını ekler. 4) Aynı işi yapan yer tutucuları kapatır
    /// (envanter, pazar ve defter hâlâ PlaceholderWorkshopHud'da kalır). Tekrar çalıştırmak güvenlidir.
    /// </summary>
    public static class FigmaUiSetup
    {
        private const string TownScenePath = BilgiAdasi.Core.SceneFlow.GameScenePath;
        private const string ThemePath = "Assets/_Project/Resources/UiTheme.asset";
        private const string SealPath = "Assets/_Project/Art/UI/UI_Muhur.png";
        private const string WaxPath = "Assets/_Project/Art/UI/UI_MuhurMum.png";
        private const string FontDir = "Assets/_Project/Fonts";

        [MenuItem("BilgiAdasi/Kurulum/14 - Figma arayüzü (HUD, diyalog, Esc + başarılar, bildirim)", priority = 14)]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var theme = SetupTheme();
            SetupScene(theme);
            Debug.Log("[BilgiAdasi] Figma arayüzü kuruldu. Tema: " + ThemePath);
        }

        // ---------- Tema ----------

        private static UiTheme SetupTheme()
        {
            MakeSprite(SealPath);
            MakeSprite(WaxPath);

            Directory.CreateDirectory(Path.GetDirectoryName(ThemePath));
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UiTheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            var so = new SerializedObject(theme);
            so.FindProperty("seal").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SealPath);
            so.FindProperty("sealWax").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(WaxPath);
            // Başlık: Alegreya Bold; gövde: Alegreya Sans Medium. TTF yoksa alan boş kalır (TMP varsayılanı).
            var heading = FontAsset("Alegreya-Bold");
            var body = FontAsset("AlegreyaSans-Medium");
            if (heading != null) so.FindProperty("headingFont").objectReferenceValue = heading;
            if (body != null) so.FindProperty("bodyFont").objectReferenceValue = body;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            if (heading == null || body == null)
                Debug.LogWarning("[BilgiAdasi] Alegreya TTF'leri " + FontDir + " içinde yok; TMP varsayılan fontu kullanılacak. " +
                                 "Alegreya-Bold.ttf ve AlegreyaSans-Medium.ttf konup Kurulum 14 yeniden çalıştırılınca bağlanır.");
            return theme;
        }

        private static void MakeSprite(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) return;
            if (ti.textureType == TextureImporterType.Sprite && ti.spriteImportMode == SpriteImportMode.Single) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }

        /// <summary>Fonts/&lt;ad&gt;.ttf varsa yanına dinamik TMP font asset'i (&lt;ad&gt; SDF.asset) üretir ya da var olanı döner.</summary>
        private static TMP_FontAsset FontAsset(string fileName)
        {
            string assetPath = $"{FontDir}/{fileName} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;
            var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontDir}/{fileName}.ttf")
                       ?? AssetDatabase.LoadAssetAtPath<Font>($"{FontDir}/{fileName}.otf");
            if (font == null) return null;

            var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            fa.name = fileName + " SDF";
            AssetDatabase.CreateAsset(fa, assetPath);
            fa.material.name = fa.name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            foreach (var tex in fa.atlasTextures)
            {
                tex.name = fa.name + " Atlas";
                AssetDatabase.AddObjectToAsset(tex, fa);
            }
            // Gamepad glifleri (× ○ □ ▲ ↑↓) Alegreya'da olmayabilir: TMP varsayılanı yedek.
            var fallback = TMP_Settings.defaultFontAsset;
            if (fallback != null && fallback != fa) fa.fallbackFontAssetTable.Add(fallback);
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        // ---------- Sahne ----------

        private static void SetupScene(UiTheme theme)
        {
            var scene = EditorSceneManager.OpenScene(TownScenePath);

            var uiRoot = GameObject.Find("UI") ?? new GameObject("UI");
            var hudTf = uiRoot.transform.Find("HUD");
            GameObject hud;
            if (hudTf == null)
            {
                hud = new GameObject("HUD", typeof(RectTransform));
                hud.transform.SetParent(uiRoot.transform, false);
                var canvas = hud.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10; // yer tutucu HUD'un üstünde
                var scaler = hud.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                hud.AddComponent<GraphicRaycaster>();
            }
            else hud = hudTf.gameObject;

            // Sıra = çizim sırası: alttakiler önce, Esc menüsü en üstte.
            Ensure<FigmaInteractPrompt>(hud.transform, "InteractPrompt", theme);
            Ensure<FigmaOrderCard>(hud.transform, "OrderCard", theme);
            Ensure<FigmaNoticeView>(hud.transform, "Notice", theme);
            Ensure<FigmaRealLifeCard>(hud.transform, "RealLifeCard", theme);
            Ensure<FigmaDialogueView>(hud.transform, "Dialogue", theme);
            Ensure<FigmaAchievementToast>(hud.transform, "AchievementToast", theme);
            Ensure<FigmaHintBook>(hud.transform, "HintBook", theme);
            Ensure<FigmaReasoningView>(hud.transform, "Reasoning", theme);
            Ensure<FigmaPauseMenu>(hud.transform, "PauseMenu", theme).transform.SetAsLastSibling();
            // Esc menüsünün üstünde açılanlar ve kararma en sonda.
            Ensure<FigmaDiscoveryBook>(hud.transform, "DiscoveryBook", theme).transform.SetAsLastSibling();
            // Eski tek sayfalık Ders Notu (FigmaLessonNote) defterin içine taşındı: kalan objesi silinir.
            var oldLesson = hud.transform.Find("LessonNote");
            if (oldLesson != null) Object.DestroyImmediate(oldLesson.gameObject);
            Ensure<FigmaNotebookSpine>(hud.transform, "NotebookSpine", theme);
            Ensure<FigmaNotebook>(hud.transform, "Notebook", theme).transform.SetAsLastSibling();
            Ensure<FigmaScreenFader>(hud.transform, "ScreenFader", theme).transform.SetAsLastSibling();

            DisablePlaceholders();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static T Ensure<T>(Transform parent, string name, UiTheme theme) where T : MonoBehaviour
        {
            var view = parent.GetComponentInChildren<T>(true);
            if (view == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                var rt = (RectTransform)go.transform;
                rt.SetParent(parent, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                view = go.AddComponent<T>();
            }
            var so = new SerializedObject(view);
            var prop = so.FindProperty("theme");
            if (prop != null)
            {
                prop.objectReferenceValue = theme;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            view.transform.SetAsLastSibling();
            return view;
        }

        /// <summary>Figma view'ının yerini aldığı yer tutucuları kapatır (silmez; geri açmak için objeyi etkinleştir).</summary>
        private static void DisablePlaceholders()
        {
            foreach (var p in Object.FindObjectsByType<PlaceholderAchievementToast>(FindObjectsInactive.Include))
                p.gameObject.SetActive(false);
            foreach (var p in Object.FindObjectsByType<PlaceholderPauseMenu>(FindObjectsInactive.Include))
                p.gameObject.SetActive(false);
            foreach (var p in Object.FindObjectsByType<PlaceholderDialogueView>(FindObjectsInactive.Include))
                p.gameObject.SetActive(false);
            foreach (var p in Object.FindObjectsByType<PlaceholderInteractPromptView>(FindObjectsInactive.Include))
            {
                // Bileşen HUD kökünde durur (envanter de orada): yalnızca bileşeni ve ham etiketlerini kapat.
                var so = new SerializedObject(p);
                if (so.FindProperty("crosshair").objectReferenceValue is GameObject ch) ch.SetActive(false);
                if (so.FindProperty("promptLabel").objectReferenceValue is Component pl) pl.gameObject.SetActive(false);
                p.enabled = false;
            }
        }
    }
}
