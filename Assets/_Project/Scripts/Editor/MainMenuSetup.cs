using BilgiAdasi.Core;
using BilgiAdasi.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Kurulum 20: ana menü sahnesi (AnaMenu: kamera, EventSystem, HUD canvas'ında FigmaMainMenuView + kararma,
    /// MainMenuController), menü metinleri (Data/Resources/MenuTexts), Build Settings sırası [AnaMenu, Harita] ve
    /// Harita HUD'una "Otomatik kaydedildi" bildirimi. Tekrar çalıştırmak güvenli.
    /// </summary>
    public static class MainMenuSetup
    {
        private const string TextsPath = "Assets/_Project/Data/Resources/MenuTexts.asset";

        [MenuItem("BilgiAdasi/Kurulum/20 - Ana menü + otomatik kayıt bildirimi", priority = 21)]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Ana menü", "Önce Play modundan çık.", "Tamam");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureTexts();
            BuildMenuScene();
            AddAutoSaveNotice();
            SetBuildScenes();
            EditorSceneManager.OpenScene(SceneFlow.GameScenePath);
            Debug.Log("[Kurulum 20] Ana menü (AnaMenu) kuruldu; Build Settings: AnaMenu, Harita. Harita HUD'una otomatik kayıt bildirimi eklendi.");
        }

        private static void EnsureTexts()
        {
            if (AssetDatabase.LoadAssetAtPath<MenuTexts>(TextsPath) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<MenuTexts>(), TextsPath);
            AssetDatabase.SaveAssets();
        }

        private static void BuildMenuScene()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneFlow.MenuScenePath) != null
                ? EditorSceneManager.OpenScene(SceneFlow.MenuScenePath)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var theme = UiTheme.OrDefault(null);

            var cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null)
            {
                var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                go.tag = "MainCamera";
                cam = go.GetComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = theme.InkDeep;

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            var ui = GameObject.Find("UI") ?? new GameObject("UI");
            var hudTf = ui.transform.Find("HUD");
            GameObject hud;
            if (hudTf == null)
            {
                hud = new GameObject("HUD", typeof(RectTransform));
                hud.transform.SetParent(ui.transform, false);
                var canvas = hud.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10;
                var scaler = hud.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                hud.AddComponent<GraphicRaycaster>();
            }
            else hud = hudTf.gameObject;

            Ensure<FigmaMainMenuView>(hud.transform, "MainMenu");
            Ensure<FigmaScreenFader>(hud.transform, "ScreenFader").transform.SetAsLastSibling();

            var ctrl = Object.FindAnyObjectByType<MainMenuController>();
            if (ctrl == null) new GameObject("MainMenuController").AddComponent<MainMenuController>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SceneFlow.MenuScenePath);
        }

        private static void AddAutoSaveNotice()
        {
            var scene = EditorSceneManager.OpenScene(SceneFlow.GameScenePath);
            var hud = GameObject.Find("UI/HUD");
            if (hud == null)
            {
                Debug.LogWarning("[Kurulum 20] Harita'da UI/HUD yok (önce Kurulum 14); bildirim eklenmedi.");
                return;
            }
            var notice = Ensure<FigmaAutoSaveNotice>(hud.transform, "AutoSaveNotice");
            // Esc menüsü ve defterin altında kalsın: başarım bildiriminin hemen arkasına.
            var toast = hud.GetComponentInChildren<FigmaAchievementToast>(true);
            if (toast != null) notice.transform.SetSiblingIndex(toast.transform.GetSiblingIndex() + 1);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SceneFlow.MenuScenePath, true),
                new EditorBuildSettingsScene(SceneFlow.GameScenePath, true),
            };
        }

        private static T Ensure<T>(Transform parent, string name) where T : MonoBehaviour
        {
            var view = parent.GetComponentInChildren<T>(true);
            if (view != null) return view;
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go.AddComponent<T>();
        }
    }
}
