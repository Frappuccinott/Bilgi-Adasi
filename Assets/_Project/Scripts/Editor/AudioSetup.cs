using System.Collections.Generic;
using System.Linq;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using BilgiAdasi.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Kurulum → 21: ses klipleri (ArtSource/Audio/ses_sentez.py çıktısı) için içe aktarma ayarı,
    /// SoundLibrary asset'i ve açık sahneye AudioDirector + mevcut ses slotlarının (adım, dolap, bisiklet, ocak) bağlanması.
    /// Tekrar çalıştırılabilir; ses dosyaları yeniden üretilince bir daha çalıştırmak yeterli.
    /// </summary>
    public static class AudioSetup
    {
        private const string AudioRoot = "Assets/_Project/Audio";
        private const string LibraryPath = "Assets/_Project/Data/Audio/SoundLibrary.asset";

        [MenuItem("BilgiAdasi/Kurulum/21 - Sesler (kütüphane + sahne)", priority = 22)]
        public static void Run()
        {
            ConfigureImports();
            var lib = BuildLibrary();
            int bound = BindScene(lib);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Ses] Kütüphane hazır ({lib.Entries.Count} kayıt), sahnede {bound} bağlantı yapıldı. Sahneyi kaydedin.");
        }

        // ---------- İçe aktarma ----------

        private static void ConfigureImports()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is AudioImporter imp)) continue;
                bool longLoop = path.Contains("/Music/") || path.Contains("/Ambience/");
                var s = imp.defaultSampleSettings;
                s.loadType = longLoop ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = longLoop ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
                s.quality = 0.7f;
                s.preloadAudioData = !longLoop;
                imp.defaultSampleSettings = s;
                imp.forceToMono = !path.Contains("/Music/");
                imp.loadInBackground = longLoop;
                imp.SaveAndReimport();
            }
        }

        private static AudioClip Clip(string rel) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioRoot}/{rel}.wav");

        private static AudioClip[] Clips(params string[] rel) => rel.Select(Clip).Where(c => c != null).ToArray();

        private static AudioClip[] Series(string prefix, int count)
        {
            var list = new List<string>();
            for (int i = 1; i <= count; i++) list.Add($"{prefix}_{i}");
            return Clips(list.ToArray());
        }

        // ---------- Kütüphane ----------

        private static SoundLibrary BuildLibrary()
        {
            var lib = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
            if (lib == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(LibraryPath));
                lib = ScriptableObject.CreateInstance<SoundLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }

            var v = new Vector2(0.95f, 1.05f);
            var flat = new Vector2(1f, 1f);
            void Set(SoundId id, AudioClip[] clips, float volume, Vector2 pitch, float minInterval = 0.03f) =>
                lib.SetEntry(new SoundLibrary.Entry(id, clips, volume, pitch, minInterval));

            Set(SoundId.Pour, Series("SFX/Kazan_Dok", 3), 0.8f, v);
            Set(SoundId.Drain, Clips("SFX/Kazan_Bosalt"), 0.85f, v);
            Set(SoundId.Ignite, Clips("SFX/Ocak_Yak"), 0.8f, v);
            Set(SoundId.Boil, Clips("SFX/Kazan_Kaynama_Dongu"), 0.7f, flat);
            Set(SoundId.ReactionSuccess, Clips("SFX/Tepkime_Basarili"), 0.8f, flat);
            Set(SoundId.Foam, Clips("SFX/Kazan_Kopurme"), 0.85f, flat);
            Set(SoundId.Jar, Series("SFX/Kavanoz", 3), 0.6f, v);
            Set(SoundId.Coins, Clips("SFX/Akce"), 0.75f, v);
            Set(SoundId.CoinSingle, Clips("SFX/Akce_Tek"), 0.65f, v);
            Set(SoundId.DoorOpen, Clips("SFX/Kapi_Ac"), 0.7f, v);
            Set(SoundId.DoorClose, Clips("SFX/Kapi_Kapat"), 0.75f, v);

            Set(SoundId.UiNavigate, Clips("UI/Ui_Gezin"), 0.5f, v, 0.05f);
            Set(SoundId.UiConfirm, Clips("UI/Ui_Onay"), 0.55f, flat, 0.06f);
            Set(SoundId.UiBack, Clips("UI/Ui_Geri"), 0.55f, flat, 0.06f);
            Set(SoundId.UiSlot, Clips("UI/Ui_Slot"), 0.45f, v, 0.04f);
            Set(SoundId.PageTurn, Clips("UI/Sayfa_Cevir"), 0.6f, v, 0.08f);
            Set(SoundId.BookOpen, Clips("UI/Defter_Ac"), 0.65f, v, 0.15f);
            Set(SoundId.BookClose, Clips("UI/Defter_Kapat"), 0.6f, v, 0.15f);
            Set(SoundId.MapOpen, Clips("UI/Harita_Ac"), 0.6f, v, 0.15f);
            Set(SoundId.DialogueAdvance, Clips("UI/Diyalog_Ilerle"), 0.55f, v, 0.05f);
            Set(SoundId.Pause, Clips("UI/Duraklat"), 0.5f, flat, 0.1f);

            Set(SoundId.Correct, Clips("UI/Dogru"), 0.75f, flat);
            Set(SoundId.Wrong, Clips("UI/Yanlis"), 0.65f, flat, 0.2f);
            Set(SoundId.Info, Clips("UI/Bilgi"), 0.6f, flat, 0.2f);
            Set(SoundId.Achievement, Clips("UI/Basarim"), 0.75f, flat, 0.5f);
            Set(SoundId.NeighborhoodComplete, Clips("UI/Koy_Tamamlandi"), 0.85f, flat, 1f);

            lib.SetLoops(Clip("Music/Muzik_Ana_Dongu"), Clip("Ambience/Sehir_Ortam_Dongu"));
            EditorUtility.SetDirty(lib);
            return lib;
        }

        // ---------- Sahne ----------

        private static int BindScene(SoundLibrary lib)
        {
            int bound = 0;
            var scene = EditorSceneManager.GetActiveScene();

            // Yönetici
            var director = Object.FindFirstObjectByType<AudioDirector>(FindObjectsInactive.Include);
            if (director == null)
            {
                var go = new GameObject("Ses");
                Undo.RegisterCreatedObjectUndo(go, "Ses");
                director = go.AddComponent<AudioDirector>();
            }
            SetRef(director, "library", lib);
            bound++;

            // Adım / zıplama / iniş (oyuncu prefab'ı varsa prefab'a yazılır)
            foreach (var fs in Object.FindObjectsByType<PlayerFootsteps>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var target = PrefabUtility.GetCorrespondingObjectFromSource(fs) ?? fs;
                var so = new SerializedObject(target);
                SetArray(so.FindProperty("footstepClips"), Series("SFX/Adim", 6));
                SetArray(so.FindProperty("jumpClips"), Clips("SFX/Zipla"));
                SetArray(so.FindProperty("landClips"), Clips("SFX/Inis"));
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
                var src = target.GetComponent<AudioSource>();
                if (src != null)
                {
                    src.playOnAwake = false;
                    src.spatialBlend = 0f;
                    EditorUtility.SetDirty(src);
                }
                bound++;
            }

            // Oyuncu çalışma anında prefab'tan doğuyorsa: prefab asset'leri
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" }))
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                var fs = root != null ? root.GetComponentInChildren<PlayerFootsteps>(true) : null;
                if (fs == null) continue;
                var so = new SerializedObject(fs);
                SetArray(so.FindProperty("footstepClips"), Series("SFX/Adim", 6));
                SetArray(so.FindProperty("jumpClips"), Clips("SFX/Zipla"));
                SetArray(so.FindProperty("landClips"), Clips("SFX/Inis"));
                so.ApplyModifiedPropertiesWithoutUndo();
                var src = fs.GetComponent<AudioSource>();
                if (src != null)
                {
                    src.playOnAwake = false;
                    src.spatialBlend = 0f;
                }
                PrefabUtility.SavePrefabAsset(root);
                bound++;
            }

            // Kilitli dolap
            foreach (var cab in Object.FindObjectsByType<LockedCabinet>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var src = EnsureSource(cab.gameObject, Clip("SFX/Dolap_Kilitli"), false, 0.8f);
                SetRef(cab, "rattleSound", src);
                bound++;
            }

            // Bisiklet tekerlek döngüsü
            foreach (var bike in Object.FindObjectsByType<Bicycle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var src = EnsureSource(bike.gameObject, Clip("SFX/Bisiklet_Tekerlek_Dongu"), true, 0f);
                SetRef(bike, "rollingSource", src);
                bound++;
            }

            // Atölye ocağının çıtırtısı (3B döngü, hep açık)
            foreach (var stove in Object.FindObjectsByType<CauldronStove>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var child = stove.transform.Find("Ses_Ocak_Citirti");
                if (child == null)
                {
                    var go = new GameObject("Ses_Ocak_Citirti");
                    Undo.RegisterCreatedObjectUndo(go, "Ses_Ocak_Citirti");
                    go.transform.SetParent(stove.transform, false);
                    child = go.transform;
                }
                var src = EnsureSource(child.gameObject, Clip("Ambience/Ocak_Citirti_Dongu"), true, 0.55f);
                src.playOnAwake = true;
                src.minDistance = 1.5f;
                src.maxDistance = 12f;
                bound++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            return bound;
        }

        private static AudioSource EnsureSource(GameObject go, AudioClip clip, bool loop, float volume)
        {
            var src = go.GetComponent<AudioSource>();
            if (src == null) src = Undo.AddComponent<AudioSource>(go);
            src.clip = clip;
            src.loop = loop;
            src.playOnAwake = false;
            src.volume = volume;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 2f;
            src.maxDistance = 22f;
            src.dopplerLevel = 0f;
            EditorUtility.SetDirty(src);
            return src;
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogWarning($"[Ses] {target.GetType().Name}.{field} bulunamadı.");
                return;
            }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetArray(SerializedProperty p, AudioClip[] clips)
        {
            if (p == null) return;
            p.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
        }
    }
}
