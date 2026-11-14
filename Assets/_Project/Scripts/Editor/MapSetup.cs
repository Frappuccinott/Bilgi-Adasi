using System.Collections.Generic;
using System.IO;
using BilgiAdasi.City;
using BilgiAdasi.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Kurulum → 19: harita ekranı (M), sol alttaki mini harita ve yön tabelası yazıları.
    /// 1) Harita sahnesini üstten ortografik kamerayla 2048² pişirir (bulut, pus, oyuncu ve arayüz gizli; sis kapalı),
    ///    hafif kâğıt tonuyla UI/Harita/Harita_Ust.png'ye yazar.
    /// 2) Data/Map/MapData.asset: görüntü + dünya karesi; yer listesi boşsa sahnedeki çapalardan doldurulur
    ///    (sonradan Inspector'da yapılan düzenlemeler korunur).
    /// 3) Sahne: "Map" objesine MapController; HUD'a FigmaMapView ve FigmaMinimapView.
    /// 4) Yön tabelaları: her "Tabela_Yazi_n_*" çapasına kolun iki yüzünde hedefin adı (TMP, Alegreya Bold).
    /// Yeniden çalıştırılabilir; harita modeli değişince görüntüyü yenilemek için tekrar çalıştır.
    /// </summary>
    public static class MapSetup
    {
        private const string ImagePath = "Assets/_Project/UI/Harita/Harita_Ust.png";
        private const string DataPath = "Assets/_Project/Data/Map/MapData.asset";
        private const string ThemePath = "Assets/_Project/Resources/UiTheme.asset";
        private const string SignFontPath = "Assets/_Project/Fonts/Alegreya-Bold SDF.asset";
        private static readonly Vector2 Center = new Vector2(-5f, 12f);
        private const float Size = 196f;
        private const int Resolution = 2048;

        // Blender'daki tabela etiketi (harita_yon.py TABELALAR) → tabelada yazan ad.
        private static readonly Dictionary<string, string> SignNames = new Dictionary<string, string>
        {
            { "Atolye", "Atölye" },
            { "Denklestirme", "Denkleştirme" },
            { "Tepkime_Turleri", "Tepkime Türleri" },
            { "Matematik_Sehri", "Matematik Şehri" },
            { "Turkce_Sehri", "Türkçe Şehri" },
            { "Tarlalar", "Tarlalar" },
            { "Hesaplamalar", "Hesaplamalar" },
            { "Guvercin_Kulesi", "Güvercin Kulesi" },
            { "Tepkimeler", "Tepkimeler" },
        };

        [MenuItem("BilgiAdasi/Kurulum/19 - Harita ekranı, mini harita ve tabela yazıları", priority = 20)]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Harita", "Önce Play modundan çık.", "Tamam");
                return;
            }
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != Core.SceneFlow.GameScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(Core.SceneFlow.GameScenePath);
            }

            var tex = BakeImage();
            var data = SetupData(tex);
            SetupScene(data);
            int signs = SetupSigns();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MapSetup] Harita görüntüsü {ImagePath}, veri {DataPath}, tabela yazısı: {signs}.");
        }

        // ---------- 1) Üstten görüntü ----------

        private static Texture2D BakeImage()
        {
            var hidden = new List<GameObject>();
            foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            {
                string n = go.name;
                bool hide = n == "Bulutlar" || n.StartsWith("Pus_") || n == "UI" || n == "EventSystem"
                            || go.GetComponent<Player.PlayerMotor>() != null || go.GetComponent<Canvas>() != null;
                if (hide && go.gameObject.activeSelf)
                {
                    go.gameObject.SetActive(false);
                    hidden.Add(go.gameObject);
                }
            }
            bool fog = RenderSettings.fog;
            RenderSettings.fog = false;

            var camGo = new GameObject("_MapBake");
            var rt = new RenderTexture(Resolution, Resolution, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                antiAliasing = 4
            };
            Texture2D result;
            try
            {
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = Size * 0.5f;
                cam.aspect = 1f;
                cam.nearClipPlane = 1f;
                cam.farClipPlane = 500f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.886f, 0.859f, 0.694f);
                cam.cullingMask = ~(1 << LayerMask.NameToLayer("UI"));
                cam.transform.SetPositionAndRotation(new Vector3(Center.x, 250f, Center.y), Quaternion.Euler(90f, 0f, 0f));
                cam.targetTexture = rt;
                cam.Render();

                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                result = new Texture2D(Resolution, Resolution, TextureFormat.RGB24, false);
                result.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0);
                RenderTexture.active = prev;
                cam.targetTexture = null;
            }
            finally
            {
                Object.DestroyImmediate(camGo);
                rt.Release();
                Object.DestroyImmediate(rt);
                RenderSettings.fog = fog;
                foreach (var go in hidden) go.SetActive(true);
            }

            // Kâğıda basılmış ton: biraz soluk, biraz kâğıt sarısı (09 paleti: kâğıt #E2DBB1).
            var paper = new Color(0.886f, 0.859f, 0.694f);
            var px = result.GetPixels();
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                float g = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                c = Color.Lerp(c, new Color(g, g, g), 0.18f);
                px[i] = Color.Lerp(c, paper, 0.12f);
            }
            result.SetPixels(px);
            result.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(ImagePath));
            File.WriteAllBytes(ImagePath, result.EncodeToPNG());
            Object.DestroyImmediate(result);
            AssetDatabase.ImportAsset(ImagePath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(ImagePath) is TextureImporter ti)
            {
                ti.textureType = TextureImporterType.Default;
                ti.mipmapEnabled = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.maxTextureSize = Resolution;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ImagePath);
        }

        // ---------- 2) Veri ----------

        private static MapData SetupData(Texture2D tex)
        {
            var data = AssetDatabase.LoadAssetAtPath<MapData>(DataPath);
            if (data == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DataPath));
                data = ScriptableObject.CreateInstance<MapData>();
                AssetDatabase.CreateAsset(data, DataPath);
            }
            data.EditorSetImage(tex, Center, Size);
            if (data.Points.Count == 0) data.EditorSetPoints(DefaultPoints());
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            return data;
        }

        private static List<MapPoint> DefaultPoints()
        {
            var runner = Object.FindAnyObjectByType<NeighborhoodRunner>();
            NeighborhoodData Find(string id)
            {
                if (runner == null) return null;
                foreach (var n in runner.Neighborhoods) if (n != null && n.Id == id) return n;
                return null;
            }
            var denk = Find("denklestirme");
            var tepk = Find("tepkime_turleri");

            return new List<MapPoint>
            {
                P("Atölye", MapPointKind.Workshop, new Vector2(0.5f, -1f), 0x9F4E2B, null, true),
                P(denk != null ? denk.Title : "Denkleştirme", MapPointKind.Neighborhood, At("Neighborhood_Denklestirme", new Vector2(-52f, 18f)), 0xF7A800, denk, true),
                P(tepk != null ? tepk.Title : "Tepkime Türleri", MapPointKind.Neighborhood, At("Neighborhood_TepkimeTurleri", new Vector2(46f, 14f)), 0x2A9FD0, tepk, true),
                P("Pazar", MapPointKind.Market, At("MarketStall_Elementler", new Vector2(-46f, 22f)), 0xD77A45, null, true),
                P("Meydan", MapPointKind.Landmark, new Vector2(0f, 12f), 0xA3B1BE, null, false),
                P("Matematik Şehri", MapPointKind.LockedCity, At("LockedZone_Matematik", new Vector2(-21.5f, -73.8f)), 0x4F7F96, null, true),
                P("Türkçe Şehri", MapPointKind.LockedCity, At("LockedZone_Turkce", new Vector2(23.6f, 98f)), 0xE8602A, null, true),
                P("Hesaplamalar", MapPointKind.LockedCity, At("LockedZone_Hesaplamalar", new Vector2(-76.9f, 45.8f)), 0x6E8090, null, true),
                P("Tepkimeler", MapPointKind.LockedCity, At("LockedZone_Tepkimeler", new Vector2(66.8f, -13.8f)), 0x6E8090, null, true),
            };
        }

        private static Vector2 At(string objectName, Vector2 fallback)
        {
            var go = GameObject.Find(objectName);
            return go != null ? new Vector2(go.transform.position.x, go.transform.position.z) : fallback;
        }

        private static MapPoint P(string label, MapPointKind kind, Vector2 world, int hex, NeighborhoodData n, bool mini) =>
            new MapPoint
            {
                label = label,
                kind = kind,
                world = world,
                color = new Color(((hex >> 16) & 0xFF) / 255f, ((hex >> 8) & 0xFF) / 255f, (hex & 0xFF) / 255f),
                neighborhood = n,
                onMinimap = mini
            };

        // ---------- 3) Sahne ----------

        private static void SetupScene(MapData data)
        {
            var mapGo = GameObject.Find("Map") ?? new GameObject("Map");
            var controller = mapGo.GetComponent<MapController>();
            if (controller == null) controller = mapGo.AddComponent<MapController>();
            var so = new SerializedObject(controller);
            so.FindProperty("data").objectReferenceValue = data;
            so.FindProperty("runner").objectReferenceValue = Object.FindAnyObjectByType<NeighborhoodRunner>();
            so.ApplyModifiedPropertiesWithoutUndo();

            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
            var hud = GameObject.Find("UI")?.transform.Find("HUD");
            if (hud == null)
            {
                Debug.LogWarning("[MapSetup] UI/HUD yok: önce Kurulum → 14 (Figma arayüzü).");
                return;
            }
            // Mini harita HUD'un altında (diyalog ve defterler üstünde çizilsin); harita ekranı Esc menüsünün hemen altında.
            var mini = Ensure<FigmaMinimapView>(hud, "Minimap", theme);
            var prompt = hud.Find("InteractPrompt");
            mini.transform.SetSiblingIndex(prompt != null ? prompt.GetSiblingIndex() + 1 : 0);
            // Harita ekranı envanterin/kesenin üstünde; yalnızca kararma (ScreenFader) daha üstte.
            var map = Ensure<FigmaMapView>(hud, "MapScreen", theme);
            var fader = hud.Find("ScreenFader");
            map.transform.SetAsLastSibling();
            if (fader != null) fader.SetAsLastSibling();
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
            return view;
        }

        // ---------- 4) Yön tabelaları ----------

        private static int SetupSigns()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SignFontPath);
            var ink = new Color(0.839f, 0.8f, 0.698f); // kâğıt orta #D6CCB2: koyu ahşapta boya
            int count = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                // Yeniden çalıştırmada eski Yazi_ çocukları bu döngüde silinir; listede kalan ölü referansları atla.
                if (t == null) continue;
                if (!t.name.StartsWith("Tabela_Yazi_") || t.parent == null || !t.parent.name.StartsWith("SM_Tabela_")) continue;
                string key = KeyOf(t.name);
                if (key == null || !SignNames.TryGetValue(key, out var label)) continue;

                for (int i = t.childCount - 1; i >= 0; i--)
                    if (t.GetChild(i).name.StartsWith("Yazi_")) Object.DestroyImmediate(t.GetChild(i).gameObject);

                // Kol: direkten çapaya doğru; yazı kolun ortasında (ok ucu boyalı kısmın önünde), iki yüzde.
                var post = t.parent.position;
                var dir = t.position - post;
                dir.y = 0f;
                if (dir.sqrMagnitude < 1e-4f) continue;
                dir.Normalize();
                var normal = Vector3.Cross(Vector3.up, dir);
                var mid = new Vector3(post.x, t.position.y, post.z) + dir * 0.66f;
                for (int side = -1; side <= 1; side += 2)
                {
                    var n = normal * side;
                    var pos = mid + n * 0.046f;
                    var rot = Quaternion.LookRotation(-n, Vector3.up);
                    // Kollar Blender'da hafif rastgele açılı (yaw/eğim ±0.03 rad): yazıyı kolun gerçek yüzüne oturt.
                    if (Physics.Raycast(mid + n * 0.4f, -n, out var hit, 0.6f) && hit.collider.transform == t.parent)
                    {
                        pos = hit.point + hit.normal * 0.006f;
                        rot = Quaternion.LookRotation(-hit.normal, Vector3.up);
                    }
                    var go = new GameObject(side < 0 ? "Yazi_On" : "Yazi_Arka");
                    go.transform.SetParent(t, false);
                    go.transform.SetPositionAndRotation(pos, rot);
                    var tmp = go.AddComponent<TextMeshPro>();
                    if (font != null) tmp.font = font;
                    tmp.text = label;
                    tmp.color = ink;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.textWrappingMode = TextWrappingModes.NoWrap;
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = 0.6f;
                    tmp.fontSizeMax = 2.3f;
                    tmp.rectTransform.sizeDelta = new Vector2(1.0f, 0.24f);
                    count++;
                }
            }
            return count;
        }

        // "Tabela_Yazi_3_Tepkime_Turleri" → "Tepkime_Turleri"
        private static string KeyOf(string objectName)
        {
            var rest = objectName.Substring("Tabela_Yazi_".Length);
            int us = rest.IndexOf('_');
            if (us <= 0 || !int.TryParse(rest.Substring(0, us), out _)) return null;
            return rest.Substring(us + 1);
        }
    }
}
