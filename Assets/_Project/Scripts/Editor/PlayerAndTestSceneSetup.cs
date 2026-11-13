using System;
using BilgiAdasi.City;
using BilgiAdasi.Core;
using BilgiAdasi.Desk;
using BilgiAdasi.Order;
using BilgiAdasi.Player;
using BilgiAdasi.UI;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Faz 2 kurulumu: oyuncu ayarları + PlayerRig/PlayerSpawn/istasyon prefab'ları + Town test sahnesi.
    /// Var olan asset ve objelerin üzerine yazmaz; eksik olanı ekler.
    /// </summary>
    public static class PlayerAndTestSceneSetup
    {
        private const string PrefabsFolder = "Assets/_Project/Prefabs";
        private const string StationsFolder = PrefabsFolder + "/Stations";
        private const string SettingsPath = "Assets/_Project/Data/Player/PlayerMovementSettings.asset";
        private const string PlayerRigPath = PrefabsFolder + "/PlayerRig.prefab";
        private const string PlayerSpawnPath = PrefabsFolder + "/PlayerSpawn.prefab";
        private const string TownScenePath = "Assets/_Project/Scenes/Town.unity";
        private const string PlaceholderRootName = "_Placeholder";

        private const float EyeHeight = 1.56f;   // 10. sınıf
        private const float BodyHeight = 1.66f;

        [MenuItem("BilgiAdasi/Kurulum/3 - Oyuncu, istasyonlar ve test sahnesi", priority = 2)]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder("Assets/_Project/Data", "Player");
            EnsureFolder("Assets/_Project", "Prefabs");
            EnsureFolder(PrefabsFolder, "Stations");

            var settings = EnsureSettings();
            var rig = EnsurePlayerRig(settings);
            var spawn = EnsurePlayerSpawn(rig);
            var cauldron = EnsureStation<CauldronStation>("Station_Cauldron", "Kazana geç");
            var desk = EnsureStation<DeskStation>("Station_Desk", "Masaya otur");
            var door = EnsureStation<DoorStation>("Station_Door", "Kapıya bak");
            var board = EnsureStation<ChalkboardStation>("Station_Chalkboard", "Tahtaya bak");

            var scene = EditorSceneManager.OpenScene(TownScenePath);
            BuildTestScene(spawn, cauldron, desk, door, board);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[BilgiAdasi] Faz 2 kurulumu tamam: PlayerRig, PlayerSpawn, 4 istasyon prefab'ı ve Town test sahnesi.");
        }

        // ---------- Asset'ler ----------

        private static PlayerMovementSettings EnsureSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>(SettingsPath);
            if (existing != null) return existing;
            var settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            return settings;
        }

        private static GameObject EnsurePlayerRig(PlayerMovementSettings settings)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerRigPath);
            if (existing != null) return existing;

            var root = new GameObject("PlayerRig");

            var player = new GameObject("Player") { layer = ProjectSetup.PlayerLayer };
            player.transform.SetParent(root.transform, false);
            var cc = player.AddComponent<CharacterController>();
            cc.height = BodyHeight;
            cc.center = new Vector3(0f, BodyHeight * 0.5f, 0f);
            cc.radius = 0.3f;
            cc.slopeLimit = settings.SlopeLimit;
            cc.stepOffset = settings.StepOffset;

            var cameraRoot = new GameObject("CameraRoot");
            cameraRoot.transform.SetParent(player.transform, false);
            cameraRoot.transform.localPosition = new Vector3(0f, EyeHeight, 0f);

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(cameraRoot.transform, false);
            var playerCam = pivot.AddComponent<CinemachineCamera>();
            playerCam.Lens.FieldOfView = settings.FieldOfView;
            playerCam.Lens.NearClipPlane = 0.05f;

            var motor = player.AddComponent<PlayerMotor>();
            var look = player.AddComponent<PlayerLook>();
            var bob = player.AddComponent<HeadBob>();
            var interactor = player.AddComponent<PlayerInteractor>();

            Set(motor, "settings", settings);
            Set(look, "settings", settings);
            Set(look, "cameraPivot", pivot.transform);
            Set(look, "playerCamera", playerCam);
            Set(bob, "settings", settings);
            Set(bob, "motor", motor);
            Set(bob, "target", pivot.transform);
            Set(interactor, "rayOrigin", pivot.transform);
            Set(interactor, "motor", motor);
            Set(interactor, "look", look);
            Set(interactor, "headBob", bob);
            SetInt(interactor, "rayMask", ~(1 << ProjectSetup.PlayerLayer));

            UpgradePlayer(root);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(root.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            var brain = camGo.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.5f);

            return SavePrefab(root, PlayerRigPath);
        }

        /// <summary>
        /// Var olan PlayerRig prefab'ına sonradan eklenen bileşenleri ekler ve bağlar
        /// (adım sesleri, PlayerLook → motor, PlayerMotor → CameraRoot). Tekrar çalıştırmak güvenlidir.
        /// </summary>
        [MenuItem("BilgiAdasi/Kurulum/4 - Oyuncu prefab'ını güncelle", priority = 3)]
        public static void UpgradePlayerRigPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerRigPath) == null)
            {
                EditorUtility.DisplayDialog("PlayerRig", "Önce 'Kurulum → 3' çalıştırılmalı.", "Tamam");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(PlayerRigPath);
            try
            {
                UpgradePlayer(root);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerRigPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log("[BilgiAdasi] PlayerRig güncellendi: PlayerFootsteps + AudioSource, referanslar bağlandı.");
        }

        private static void UpgradePlayer(GameObject rigRoot)
        {
            var playerT = rigRoot.transform.Find("Player");
            if (playerT == null)
            {
                Debug.LogError("[BilgiAdasi] PlayerRig içinde 'Player' bulunamadı.");
                return;
            }
            var player = playerT.gameObject;
            var motor = player.GetComponent<PlayerMotor>();
            var look = player.GetComponent<PlayerLook>();

            if (motor != null) Set(motor, "cameraRoot", playerT.Find("CameraRoot"));
            if (look != null) Set(look, "motor", motor);

            if (!player.TryGetComponent<AudioSource>(out var source))
            {
                source = player.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f; // oyuncunun kendi sesi: 2D
            }
            if (!player.TryGetComponent<PlayerFootsteps>(out var footsteps))
                footsteps = player.AddComponent<PlayerFootsteps>();
            Set(footsteps, "motor", motor);
            Set(footsteps, "source", source);
        }

        private static GameObject EnsurePlayerSpawn(GameObject rig)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerSpawnPath);
            if (existing != null) return existing;

            var go = new GameObject("PlayerSpawn");
            var spawn = go.AddComponent<PlayerSpawn>();
            Set(spawn, "playerRigPrefab", rig);
            return SavePrefab(go, PlayerSpawnPath);
        }

        private static GameObject EnsureStation<T>(string name, string prompt) where T : MonoBehaviour
        {
            string path = $"{StationsFolder}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var root = new GameObject(name) { layer = ProjectSetup.InteractableLayer };
            var station = root.AddComponent<T>();
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.6f, 0f);
            box.size = new Vector3(1.2f, 1.2f, 1.2f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = PlaceholderRootName;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.transform.SetParent(visual.transform, false);
            cube.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            var camPoint = new GameObject("CameraPoint");
            camPoint.transform.SetParent(root.transform, false);
            camPoint.transform.localPosition = new Vector3(0f, 1.7f, -1.3f);
            camPoint.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
            var stationCam = camPoint.AddComponent<CinemachineCamera>();
            stationCam.Lens.FieldOfView = 55f;
            stationCam.Lens.NearClipPlane = 0.05f;
            stationCam.enabled = false;

            // Kazan ve kapı 13.11'den beri istasyon değil (kamera değişmez); yalnızca StationBase'ler kamera alır.
            if (new SerializedObject(station).FindProperty("promptText") != null) SetString(station, "promptText", prompt);
            if (station is StationBase) Set(station, "stationCamera", stationCam);

            return SavePrefab(root, path);
        }

        // ---------- Test sahnesi ----------

        private static void BuildTestScene(GameObject spawn, GameObject cauldron, GameObject desk, GameObject door, GameObject board)
        {
            var placeholder = GameObject.Find(PlaceholderRootName) ?? new GameObject(PlaceholderRootName);

            if (placeholder.transform.Find("Ground") == null)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.SetParent(placeholder.transform);
                ground.transform.localScale = new Vector3(4f, 1f, 4f);
            }

            AddBlock(placeholder, "Wall_Back", new Vector3(0f, 1.5f, 5f), new Vector3(12f, 3f, 0.3f), Quaternion.identity);
            AddBlock(placeholder, "Wall_Left", new Vector3(-6f, 1.5f, 0f), new Vector3(0.3f, 3f, 10f), Quaternion.identity);
            AddBlock(placeholder, "Step_0.25m", new Vector3(4f, 0.125f, -4f), new Vector3(2f, 0.25f, 2f), Quaternion.identity);
            AddBlock(placeholder, "Ramp_30deg", new Vector3(4f, 0.5f, -8f), new Vector3(2f, 0.2f, 4f), Quaternion.Euler(-30f, 0f, 0f));

            BuildPlaceholderHud(placeholder);
            EnsureEventSystem();

            PlacePrefab(spawn, "PlayerSpawn", new Vector3(0f, 0f, -5f), Quaternion.identity);
            PlacePrefab(cauldron, "Station_Cauldron", new Vector3(-3f, 0f, 1f), Quaternion.identity);
            PlacePrefab(desk, "Station_Desk", new Vector3(-1f, 0f, 1f), Quaternion.identity);
            PlacePrefab(door, "Station_Door", new Vector3(1f, 0f, 1f), Quaternion.identity);
            PlacePrefab(board, "Station_Chalkboard", new Vector3(3f, 0f, 1f), Quaternion.identity);

            BuildNeighborhood("Neighborhood_Denklestirme", "denklestirme", new Vector3(-10f, 0f, 10f));
            BuildNeighborhood("Neighborhood_TepkimeTurleri", "tepkime_turleri", new Vector3(10f, 0f, 10f));

            if (GameObject.Find("LockedZone_Example") == null)
            {
                var zone = new GameObject("LockedZone_Example");
                zone.transform.position = new Vector3(0f, 1.5f, 16f);
                zone.AddComponent<BoxCollider>().size = new Vector3(20f, 3f, 0.5f);
                zone.AddComponent<LockedZone>();
            }
        }

        private static void BuildPlaceholderHud(GameObject placeholder)
        {
            if (placeholder.transform.Find("HUD") != null) return;

            var hud = new GameObject("HUD", typeof(RectTransform));
            hud.transform.SetParent(placeholder.transform, false);
            var canvas = hud.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = hud.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            hud.AddComponent<GraphicRaycaster>();

            var crosshair = CreateLabel(hud.transform, "Crosshair", "•", 40f, Vector2.zero);
            var prompt = CreateLabel(hud.transform, "Prompt", "", 32f, new Vector2(0f, -90f));

            var view = hud.AddComponent<PlaceholderInteractPromptView>();
            Set(view, "promptLabel", prompt);
            Set(view, "crosshair", crosshair.gameObject);
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(900f, 60f);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static void BuildNeighborhood(string name, string id, Vector3 position)
        {
            if (GameObject.Find(name) != null) return;

            var root = new GameObject(name);
            root.transform.position = position;
            var view = root.AddComponent<NeighborhoodView>();
            SetString(view, "neighborhoodId", id);

            var before = new GameObject("Before");
            before.transform.SetParent(root.transform, false);
            AddBlock(before, "_Placeholder_Before", new Vector3(0f, 0.5f, 0f), Vector3.one, Quaternion.identity);

            var after = new GameObject("After");
            after.transform.SetParent(root.transform, false);
            AddBlock(after, "_Placeholder_After", new Vector3(0f, 1.5f, 0f), new Vector3(1f, 3f, 1f), Quaternion.identity);
            after.SetActive(false);

            var parts = new GameObject("Parts");
            parts.transform.SetParent(root.transform, false);
        }

        private static void AddBlock(GameObject parent, string name, Vector3 localPosition, Vector3 scale, Quaternion rotation)
        {
            if (parent.transform.Find(name) != null) return;
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent.transform, false);
            block.transform.localPosition = localPosition;
            block.transform.localRotation = rotation;
            block.transform.localScale = scale;
        }

        private static void PlacePrefab(GameObject prefab, string name, Vector3 position, Quaternion rotation)
        {
            if (GameObject.Find(name) != null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetPositionAndRotation(position, rotation);
        }

        // ---------- Yardımcılar ----------

        private static GameObject SavePrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void Set(Object target, string field, Object value) =>
            Modify(target, field, p => p.objectReferenceValue = value);

        private static void SetString(Object target, string field, string value) =>
            Modify(target, field, p => p.stringValue = value);

        private static void SetInt(Object target, string field, int value) =>
            Modify(target, field, p => p.intValue = value);

        private static void Modify(Object target, string field, Action<SerializedProperty> apply)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[BilgiAdasi] {target.GetType().Name}.{field} alanı bulunamadı.");
                return;
            }
            apply(prop);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}")) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
