using System.Collections.Generic;
using BilgiAdasi.City;
using BilgiAdasi.Core;
using BilgiAdasi.Player;
using BilgiAdasi.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Kurulum 20: ana menü sahnesi (AnaMenu: kamera, EventSystem, HUD canvas'ında FigmaMainMenuView + kararma,
    /// MainMenuController), menü metinleri (Data/Resources/MenuTexts), Build Settings sırası [AnaMenu, Harita] ve
    /// Harita HUD'una "Otomatik kaydedildi" bildirimi. Tekrar çalıştırmak güvenli.
    /// Arka plan (Figma Ekran/Giriş "Oyun görüntüsü"): Harita'nın sanatı (Harita_Sanat + Atolye_Sanat) betiksiz/çarpışmasız
    /// kopyalanıp Prefabs/Menu/MenuBackdrop.prefab olur; ışık, sis, ortam ve post-process Harita'dan taşınır; kamera
    /// MenuFlyover ile haritanın üstünde 360° döner. Harita modeli değişince Kurulum 20'yi yeniden çalıştır.
    /// </summary>
    public static class MainMenuSetup
    {
        private const string TextsPath = "Assets/_Project/Data/Resources/MenuTexts.asset";
        private const string BackdropPath = "Assets/_Project/Prefabs/Menu/MenuBackdrop.prefab";
        private static readonly string[] BackdropRoots = { "Harita_Sanat", "Atolye_Sanat" };

        /// <summary>Harita sahnesinden menüye taşınan görünüm (ışık, ortam, sis, post-process) ve veri.</summary>
        private sealed class GameLook
        {
            public bool HasSun;
            public Vector3 SunEuler;
            public Color SunColor;
            public float SunIntensity;
            public LightShadows SunShadows;
            public float SunShadowStrength;
            public AmbientMode AmbientMode;
            public Color AmbientSky, AmbientEquator, AmbientGround, AmbientLight;
            public float AmbientIntensity;
            public Material Skybox;
            public bool Fog;
            public FogMode FogMode;
            public Color FogColor;
            public float FogDensity, FogStart, FogEnd;
            public VolumeProfile Profile;
            public float FarClip = 2000f;
            public Vector3 Pivot;
            public float Radius = 55f, Height = 64f, MapSize = 196f;
            public List<NeighborhoodData> Neighborhoods = new List<NeighborhoodData>();
            public PlayerMovementSettings Movement;
        }

        [MenuItem("BilgiAdasi/Kurulum/20 - Ana menü + otomatik kayıt bildirimi", priority = 21)]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Ana menü", "Önce Play modundan çık.", "Tamam");
                return;
            }
            // Harita açıksa kapatılmaz (kaydedilmemiş değişiklikleri silinmesin); menü sahnesi yanına eklenip kurulur.
            var game = SceneManager.GetSceneByPath(SceneFlow.GameScenePath);
            if (!game.isLoaded)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                game = EditorSceneManager.OpenScene(SceneFlow.GameScenePath);
            }

            EnsureTexts();
            FigmaUiSetup.AssignWeightFonts(AssetDatabase.LoadAssetAtPath<UiTheme>("Assets/_Project/Resources/UiTheme.asset"));
            var look = CaptureGameLook();
            AddAutoSaveNotice(game);
            BuildMenuScene(look, game);
            SetBuildScenes();
            Debug.Log("[Kurulum 20] Ana menü (AnaMenu) kuruldu: kuş bakışı harita turu (MenuBackdrop + MenuFlyover), Figma Ekran/Giriş. Build Settings: AnaMenu, Harita.");
        }

        private static void EnsureTexts()
        {
            if (AssetDatabase.LoadAssetAtPath<MenuTexts>(TextsPath) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<MenuTexts>(), TextsPath);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Harita'yı açar: sanatı betiksiz prefab'a çevirir, ışık/ortam ayarlarını ve köy sırasını okur.</summary>
        private static GameLook CaptureGameLook()
        {
            var look = new GameLook();

            var sun = RenderSettings.sun != null ? RenderSettings.sun : GameObject.Find("Gunes")?.GetComponent<Light>();
            if (sun != null)
            {
                look.HasSun = true;
                look.SunEuler = sun.transform.eulerAngles;
                look.SunColor = sun.color;
                look.SunIntensity = sun.intensity;
                look.SunShadows = sun.shadows;
                look.SunShadowStrength = sun.shadowStrength;
            }
            look.AmbientMode = RenderSettings.ambientMode;
            look.AmbientSky = RenderSettings.ambientSkyColor;
            look.AmbientEquator = RenderSettings.ambientEquatorColor;
            look.AmbientGround = RenderSettings.ambientGroundColor;
            look.AmbientLight = RenderSettings.ambientLight;
            look.AmbientIntensity = RenderSettings.ambientIntensity;
            look.Skybox = RenderSettings.skybox;
            look.Fog = RenderSettings.fog;
            look.FogMode = RenderSettings.fogMode;
            look.FogColor = RenderSettings.fogColor;
            look.FogDensity = RenderSettings.fogDensity;
            look.FogStart = RenderSettings.fogStartDistance;
            look.FogEnd = RenderSettings.fogEndDistance;

            foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (v.isGlobal && v.sharedProfile != null) { look.Profile = v.sharedProfile; break; }
            var cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null) look.FarClip = Mathf.Max(1500f, cam.farClipPlane);

            var runner = Object.FindAnyObjectByType<NeighborhoodRunner>(FindObjectsInactive.Include);
            if (runner != null)
                foreach (var n in runner.Neighborhoods)
                    if (n != null) look.Neighborhoods.Add(n);
            var guids = AssetDatabase.FindAssets("t:PlayerMovementSettings");
            if (guids.Length > 0)
                look.Movement = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));

            // Yörünge merkezi: harita verisinin merkezi (oynanan şehir), zemine oturtulmuş.
            var mapGuids = AssetDatabase.FindAssets("t:MapData");
            var map = mapGuids.Length > 0 ? AssetDatabase.LoadAssetAtPath<MapData>(AssetDatabase.GUIDToAssetPath(mapGuids[0])) : null;
            var center = map != null ? new Vector3(map.WorldCenter.x, 0f, map.WorldCenter.y) : Vector3.zero;
            if (Physics.Raycast(center + Vector3.up * 500f, Vector3.down, out var hit, 1000f, ~0, QueryTriggerInteraction.Ignore))
                center.y = hit.point.y;
            look.Pivot = center;
            float size = map != null ? map.WorldSize : 196f;
            // Yakın ve dik: gökyüzü ve haritanın dışı hiçbir açıda görünmesin (karar 14.11).
            look.Radius = size * 0.28f;
            look.Height = size * 0.33f;
            look.MapSize = size;

            BuildBackdropPrefab();
            return look;
        }

        /// <summary>Harita sanatının kopyası: yalnızca görünen şey kalır (betik, çarpışma, fizik, animatör, ses silinir).</summary>
        private static void BuildBackdropPrefab()
        {
            var holder = new GameObject("MenuBackdrop");
            foreach (var name in BackdropRoots)
            {
                var src = GameObject.Find(name);
                if (src == null)
                {
                    Debug.LogWarning($"[Kurulum 20] Harita'da '{name}' yok; menü arka planına eklenmedi.");
                    continue;
                }
                var copy = Object.Instantiate(src, holder.transform);
                copy.name = name;
                copy.transform.SetPositionAndRotation(src.transform.position, src.transform.rotation);
                copy.transform.localScale = src.transform.lossyScale;
                Strip(copy);
            }

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(BackdropPath));
            PrefabUtility.SaveAsPrefabAsset(holder, BackdropPath);
            Object.DestroyImmediate(holder);
        }

        private static void Strip(GameObject root)
        {
            // Önce betikler (RequireComponent bağımlılıkları çözülsün), sonra fizik/animasyon/ses. TMP tabelaları kalır.
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (mb != null && !(mb is TMP_Text)) Object.DestroyImmediate(mb);
            foreach (var c in root.GetComponentsInChildren<Joint>(true)) Object.DestroyImmediate(c);
            foreach (var c in root.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(c);
            foreach (var c in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var c in root.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(c);
            foreach (var c in root.GetComponentsInChildren<AudioSource>(true)) Object.DestroyImmediate(c);
            foreach (var c in root.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(c);
        }

        private static void BuildMenuScene(GameLook look, Scene game)
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneFlow.MenuScenePath) != null
                ? EditorSceneManager.OpenScene(SceneFlow.MenuScenePath, OpenSceneMode.Additive)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);   // yeni objeler ve RenderSettings menü sahnesine

            var theme = UiTheme.OrDefault(null);

            var cam = InScene<Camera>(scene);
            if (cam == null)
            {
                var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                go.tag = "MainCamera";
                cam = go.GetComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = theme.InkDeep;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = look.FarClip;
            cam.fieldOfView = 45f;
            var camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            var fly = cam.GetComponent<MenuFlyover>();
            if (fly == null) fly = cam.gameObject.AddComponent<MenuFlyover>();
            fly.Configure(look.Pivot, look.Radius, look.Height);

            ApplyLook(look, scene);

            if (InScene<EventSystem>(scene) == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            var ui = RootNamed(scene, "UI") ?? new GameObject("UI");
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

            var view = hud.GetComponentInChildren<FigmaMainMenuView>(true);
            var vso = new SerializedObject(view);
            vso.FindProperty("movementSettings").objectReferenceValue = look.Movement;
            vso.ApplyModifiedPropertiesWithoutUndo();

            var ctrl = InScene<MainMenuController>(scene);
            if (ctrl == null) ctrl = new GameObject("MainMenuController").AddComponent<MainMenuController>();
            var cso = new SerializedObject(ctrl);
            var list = cso.FindProperty("neighborhoods");
            list.arraySize = look.Neighborhoods.Count;
            for (int i = 0; i < look.Neighborhoods.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = look.Neighborhoods[i];
            cso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SceneFlow.MenuScenePath);
            SceneManager.SetActiveScene(game);
            EditorSceneManager.CloseScene(scene, true);
        }

        /// <summary>Menü sahnesine Harita'nın görünümü: arka plan prefab'ı, güneş, ortam/sis, global post-process.</summary>
        private static void ApplyLook(GameLook look, Scene scene)
        {
            var old = RootNamed(scene, "MenuBackdrop");
            if (old != null) Object.DestroyImmediate(old);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BackdropPath);
            if (prefab != null) PrefabUtility.InstantiatePrefab(prefab, scene);

            var sunGo = RootNamed(scene, "Gunes");
            if (sunGo == null) sunGo = new GameObject("Gunes", typeof(Light));
            var sun = sunGo.GetComponent<Light>();
            sun.type = LightType.Directional;
            if (look.HasSun)
            {
                sunGo.transform.eulerAngles = look.SunEuler;
                sun.color = look.SunColor;
                sun.intensity = look.SunIntensity;
                sun.shadows = look.SunShadows;
                sun.shadowStrength = look.SunShadowStrength;
            }
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = look.AmbientMode;
            RenderSettings.ambientSkyColor = look.AmbientSky;
            RenderSettings.ambientEquatorColor = look.AmbientEquator;
            RenderSettings.ambientGroundColor = look.AmbientGround;
            RenderSettings.ambientLight = look.AmbientLight;
            RenderSettings.ambientIntensity = look.AmbientIntensity;
            RenderSettings.skybox = look.Skybox;
            RenderSettings.fog = look.Fog;
            RenderSettings.fogMode = look.FogMode;
            RenderSettings.fogColor = look.FogColor;
            RenderSettings.fogDensity = look.FogDensity;
            // Kuş bakışında doğrusal sis haritayı yutmasın: başlangıç/bitiş yörünge boyuna göre uzatılır.
            RenderSettings.fogStartDistance = Mathf.Max(look.FogStart, look.MapSize * 0.75f);
            RenderSettings.fogEndDistance = Mathf.Max(look.FogEnd, look.MapSize * 3.7f);

            if (look.Profile != null)
            {
                var volGo = RootNamed(scene, "PostProcessVolume");
                if (volGo == null) volGo = new GameObject("PostProcessVolume", typeof(Volume));
                var vol = volGo.GetComponent<Volume>();
                vol.isGlobal = true;
                vol.sharedProfile = look.Profile;
            }
        }

        /// <summary>Harita HUD'una "Otomatik kaydedildi" bildirimi (yoksa). Sahne kaydedilmez, yalnızca kirli işaretlenir.</summary>
        private static void AddAutoSaveNotice(Scene game)
        {
            var ui = RootNamed(game, "UI");
            var hud = ui != null ? ui.transform.Find("HUD") : null;
            if (hud == null)
            {
                Debug.LogWarning("[Kurulum 20] Harita'da UI/HUD yok (önce Kurulum 14); bildirim eklenmedi.");
                return;
            }
            if (hud.GetComponentInChildren<FigmaAutoSaveNotice>(true) != null) return;
            var notice = Ensure<FigmaAutoSaveNotice>(hud, "AutoSaveNotice");
            // Esc menüsü ve defterin altında kalsın: başarım bildiriminin hemen arkasına.
            var toast = hud.GetComponentInChildren<FigmaAchievementToast>(true);
            if (toast != null) notice.transform.SetSiblingIndex(toast.transform.GetSiblingIndex() + 1);
            EditorSceneManager.MarkSceneDirty(game);
        }

        private static GameObject RootNamed(Scene scene, string name)
        {
            foreach (var r in scene.GetRootGameObjects())
                if (r.name == name) return r;
            return null;
        }

        private static T InScene<T>(Scene scene) where T : Component
        {
            foreach (var r in scene.GetRootGameObjects())
            {
                var c = r.GetComponentInChildren<T>(true);
                if (c != null) return c;
            }
            return null;
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
