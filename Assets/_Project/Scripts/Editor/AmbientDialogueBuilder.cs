using System.Collections.Generic;
using BilgiAdasi.City;
using BilgiAdasi.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Yol sohbetleri (10-gorev-pazar-npc §6, S-01…S-18):
    /// 1) Metinleri Data/Texts/Ambient/S-nn.asset olarak üretir (metin düzeltmesi Inspector'dan ya da burada yapılıp yeniden çalıştırılır).
    /// 2) Açık sahnede "Yol_Sohbetleri" altına tetik noktalarını kurar. Var olan noktanın yeri korunur (elle taşınabilir),
    ///    yalnızca sohbet listesi güncellenir.
    /// 3) "HUD" Canvas'ına altyazı view'ını (FigmaSubtitleView) ekler.
    /// Koşullar görev kimlikleriyle: D≥n = denk_n bitti, D&lt;n = denk_n bitmedi; T aynı şekilde tur_n.
    /// </summary>
    public static class AmbientDialogueBuilder
    {
        private const string Folder = "Assets/_Project/Data/Texts/Ambient";
        private const string RootName = "Yol_Sohbetleri";

        private static readonly string[] None = new string[0];

        [MenuItem("BilgiAdasi/Kurulum/15 - Yol sohbetleri (veri + tetikler + altyazı)", priority = 15)]
        public static void Run()
        {
            var data = BuildData();
            PlaceTriggers(data);
            EnsureSubtitleView();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log($"[Yol sohbetleri] {data.Count} sohbet, {Spots().Length} tetik noktası hazır. Sahneyi kaydetmeyi unutma.");
        }

        // ---------- 1) Veri ----------

        private static Dictionary<string, AmbientDialogueData> BuildData()
        {
            EnsureFolder();
            var map = new Dictionary<string, AmbientDialogueData>();

            // 6.1 Meydan
            Make(map, "S-01", None, None, D(1),
                L("Sakacı Nuri", "Yine dereye mi ineceğiz muhtarım?"),
                L("Muhtar Yusuf", "Dere de kurudu Nuri. Câbir Usta'nın yeni bir çırağı varmış, ona umut bağladım."),
                L("Sakacı Nuri", "Çırak dediğin kaç yaşında ki?"),
                L("Muhtar Yusuf", "Boyuna bakma. Kafası işliyormuş."));
            Make(map, "S-02", D(1), None, None,
                L("Ayşe", "Mert! Su geldi, su!"),
                L("Mert", "Hani hidro… hidrojen falan diyorlardı ya, o mu bu?"),
                L("Ayşe", "Bilmem, ama ıslak."));
            Make(map, "S-03", None, None, None,
                L("Takasçı Gülsüm", "Bahar, dün biri sekiz kavanoz can havası almış, yarısını bana sattı."),
                L("Körükçü Bahar", "Saymadan almış. Kazan ne isterse o kadar alacaksın."),
                L("Takasçı Gülsüm", "Ben şikâyetçi değilim ama."));
            Make(map, "S-04", None, new[] { "denk_5", "tur_5" }, None,
                L("Attar Saadet", "Şu çırak geçen hafta bu kadar uzun muydu?"),
                L("Demirci Rıza", "Öğrendikçe uzuyor derler. Bizim oğlan da kitap okusa…"),
                L("Attar Saadet", "Rıza, oğlun senden uzun zaten."));

            // 6.2 Doğu yolu → köprü → Denkleştirme
            Make(map, "S-05", None, None, D(4),
                L("Duvarcı Halil", "Bu tahta köprü bir kışı daha çekmez."),
                L("Kireççi Cemal", "Taşı ben veririm, harcı kim karacak?"),
                L("Duvarcı Halil", "Harç için sönmüş kireç lazım. Ustanın çırağına soracağım."));
            Make(map, "S-06", D(4), None, None,
                L("Kireççi Cemal", "Taş girdi, kireç çıktı. Bir tane bile eksik çuval yok, saydım."),
                L("Duvarcı Halil", "Sen çuvalı say, çırak atomu saydı."));
            Make(map, "S-07", None, None, D(2),
                L("Fenerci Demir", "Camı silsem de bir saat sonra yine kapkara."),
                L("Mumcu Kenan", "Gaz çok, hava az, ondan. Annem de öyle derdi."),
                L("Fenerci Demir", "Annen haklıymış ama ne kadar hava, onu söylememiş."));
            Make(map, "S-08", D(2), None, None,
                L("Mumcu Kenan", "Bak şu ışığa. İs yok, koku yok."),
                L("Fenerci Demir", "Bir bataklık gazına iki can havası. Çırak kartına yazdı, astım."));
            Make(map, "S-09", D(3), None, None,
                L("Fırıncı Zehra", "Simitçi Hatice'ye de söyledim, gaz az olursa alev sarı yanar."),
                L("Aşçı Melek", "Benimki hep sarıydı, ben onu güzel sanıyordum!"),
                L("Fırıncı Zehra", "Güzel ama isli. Mavisi doğrusu."));
            Make(map, "S-10", None, None, D(5),
                L("Çiftçi Elif", "Toprak yorgun Osman. Gübre ruhu olmadan bu yıl da sarı kalacak."),
                L("Bostancı Osman", "Pazardan alırsın."),
                L("Çiftçi Elif", "Pazarda satılmıyor ki! Hava var, kazan yok. Çırağa yaptıracağım."));
            Make(map, "S-11", D(5), None, None,
                L("Bostancı Osman", "Elif, tarlan yeşil yeşil. Bana da yaptırsana."),
                L("Çiftçi Elif", "Parasını peşin ver, ama sayarak harcasın. Fazlası cebinden gider."));

            // 6.3 Tepkime Türleri yolu
            Make(map, "S-12", None, None, T(1),
                L("Güvercinci Selim", "Bu sabah üç mektup geldi, üçü de atölyeye."),
                L("Lonca Kâtibi Nevin", "Kasaba soru sormaya başladı demek. Güzel."),
                L("Güvercinci Selim", "Cevapları da çırak mı yazacak?"),
                L("Lonca Kâtibi Nevin", "Yazmayacak, işaretleyecek. Mürekkep israf olmasın."));
            Make(map, "S-13", T(2), None, None,
                L("Attar Saadet", "Çırak benim karışık şişeleri de ayırsa ya. Birleşen bir yana, ayrışan bir yana…"),
                L("Lonca Kâtibi Nevin", "Yananı da üçüncü yana. Saadet, sen de lonca sınavına gir bari."));
            Make(map, "S-14", T(4), None, T(5),
                L("Lonca Kâtibi Nevin", "Sınav kâğıtları mühürlü, kimse görmedi."),
                L("Güvercinci Selim", "Ezberleyen geçemez diyorlar, doğru mu?"),
                L("Lonca Kâtibi Nevin", "Yanlış yapan başka soru alır. Ezber işe yaramaz, anlamak gerek."));
            Make(map, "S-15", T(5), None, None,
                L("Güvercinci Selim", "Kulenin bayrağı ilk kez dalgalanıyor."),
                L("Lonca Kâtibi Nevin", "Loncaya yeni bir mühür girdi. Kasabanın en genç simyacısı."));

            // 6.4 Kilitli bölge ağızları
            Make(map, "S-16", None, None, None,
                L("Bekçi Tuncay", "Bu kapı şimdilik kapalı. İçeride herkes tartı ve hesapla uğraşıyor."),
                L("Bekçi Tuncay", "Önce denkleştirmeyi öğren, sonra gel. Kaç tane olduğunu bilmeyen, ne kadar olduğunu da bilemez."));
            Make(map, "S-17", None, None, None,
                L("Yaşlı Hayriye", "Pusun ardında eski simyacıların köyü var derler. Ben gitmedim. Sen de daha gitme."));
            Make(map, "S-18", new[] { "denk_5", "tur_5" }, None, None,
                L("Güvercinci Selim", "Şu dağların ardında başka şehirler var. Biri sayılarla, biri ışıkla, biri canlılarla uğraşırmış."),
                L("Lonca Kâtibi Nevin", "Hepsi aynı adada. Bir gün yolu açılır."));

            return map;
        }

        private static string[] D(int n) => new[] { "denk_" + n };
        private static string[] T(int n) => new[] { "tur_" + n };
        private static AmbientDialogueData.Line L(string speaker, string text) => new AmbientDialogueData.Line(speaker, text);

        private static void Make(Dictionary<string, AmbientDialogueData> map, string id, string[] all, string[] any, string[] blocked,
            params AmbientDialogueData.Line[] lines)
        {
            string path = Folder + "/" + id + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<AmbientDialogueData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<AmbientDialogueData>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.Initialize(id, lines, all, any, blocked);
            EditorUtility.SetDirty(asset);
            map[id] = asset;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Data/Texts")) AssetDatabase.CreateFolder("Assets/_Project/Data", "Texts");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project/Data/Texts", "Ambient");
        }

        // ---------- 2) Tetik noktaları ----------

        private readonly struct Spot
        {
            public readonly string Name;
            public readonly Vector3 Position;
            public readonly float Radius;
            public readonly string[] Ids;

            public Spot(string name, float x, float z, float radius, params string[] ids)
            {
                Name = name;
                Position = new Vector3(x, 0f, z);
                Radius = radius;
                Ids = ids;
            }
        }

        // Konumlar Deneme_Mustafa_Harita'daki yapılara göre (çeşme, köprü, fener direkleri, fırın, tarla, kule, lonca, kapılar).
        private static Spot[] Spots() => new[]
        {
            new Spot("Meydan_Cesme", -6.6f, 14.8f, 7f, "S-01", "S-02", "S-04"),
            new Spot("Pazar", 4.5f, 7.5f, 4.5f, "S-03"),
            new Spot("Denk_Kopru", -31f, 15f, 7f, "S-05", "S-06"),
            new Spot("Denk_Fenerler", -44f, 19f, 6f, "S-07", "S-08"),
            new Spot("Denk_Firin", -51f, 4f, 6f, "S-09"),
            new Spot("Denk_Tarla", -66f, 10f, 7f, "S-10", "S-11"),
            new Spot("Tepk_GuvercinKulesi", 55f, 4f, 6f, "S-12"),
            new Spot("Tepk_Lonca", 45f, 15f, 6f, "S-15", "S-14", "S-13"),
            new Spot("Kapi_Hesaplamalar", -74f, 40f, 7f, "S-16"),
            new Spot("Pus_Tepkimeler", 64f, -8f, 7f, "S-17"),
            new Spot("Kapi_Matematik", -21.5f, -66f, 8f, "S-18"),
        };

        private static void PlaceTriggers(Dictionary<string, AmbientDialogueData> data)
        {
            var root = GameObject.Find(RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(root, "Yol sohbetleri");
            }

            foreach (var spot in Spots())
            {
                var child = root.transform.Find(spot.Name);
                AmbientDialogueTrigger trigger;
                if (child == null)
                {
                    var go = new GameObject(spot.Name);
                    go.transform.SetParent(root.transform, false);
                    go.transform.position = spot.Position;
                    trigger = go.AddComponent<AmbientDialogueTrigger>();
                    Undo.RegisterCreatedObjectUndo(go, "Yol sohbeti tetiği");
                }
                else trigger = child.GetComponent<AmbientDialogueTrigger>() ?? child.gameObject.AddComponent<AmbientDialogueTrigger>();

                var so = new SerializedObject(trigger);
                var list = so.FindProperty("dialogues");
                list.arraySize = spot.Ids.Length;
                for (int i = 0; i < spot.Ids.Length; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = data[spot.Ids[i]];
                if (child == null) so.FindProperty("radius").floatValue = spot.Radius;
                so.ApplyModifiedProperties();
            }
        }

        // ---------- 3) Altyazı view'ı ----------

        private static void EnsureSubtitleView()
        {
            if (Object.FindAnyObjectByType<FigmaSubtitleView>(FindObjectsInactive.Include) != null) return;

            Canvas hud = null;
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
                if (c.isRootCanvas && c.name == "HUD") { hud = c; break; }
            if (hud == null)
            {
                Debug.LogWarning("[Yol sohbetleri] 'HUD' Canvas'ı yok: önce Kurulum → 14 (Figma arayüzü) çalıştır. Altyazı eklenmedi.");
                return;
            }

            var go = new GameObject("Subtitle", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(hud.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var view = go.AddComponent<FigmaSubtitleView>();
            Undo.RegisterCreatedObjectUndo(go, "Altyazı view'ı");

            // Temayı diyalog balonundan al; altyazı balonun altında, Esc menüsünün altında çizilsin.
            var dialogue = hud.GetComponentInChildren<FigmaDialogueView>(true);
            if (dialogue != null)
            {
                var theme = new SerializedObject(dialogue).FindProperty("theme").objectReferenceValue;
                var so = new SerializedObject(view);
                so.FindProperty("theme").objectReferenceValue = theme;
                so.ApplyModifiedPropertiesWithoutUndo();
                go.transform.SetSiblingIndex(dialogue.transform.GetSiblingIndex());
            }
        }
    }
}
