using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Başarımlar ve Esc menüsü. Başarım verisi 04_Icerik/11-gorev-ve-basarim-listesi.md'den (Mustafa'nın görev planı):
    /// her ders bir sekme = bir AchievementTree asset'i (Kimya, Matematik, Fizik, Biyoloji); görev, defter kaydının dersine girer.
    /// 13: eksik sekmeleri oluşturur (var olana dokunmaz), Town'a AchievementController + PauseMenuController ve
    ///     _Placeholder/HUD altına ham bildirim ve ham Esc menüsü ekler. Tekrar çalıştırmak güvenlidir.
    /// 13b: sekmelerin başarım listesini görev listesinden yeniden yazar (tür görünümü ve simgeler korunur),
    ///      eski bölüm sekmelerini kaldırır ve açık sahnelerdeki AchievementController'ı eşitler.
    /// </summary>
    public static class AchievementSetup
    {
        private const string Folder = "Assets/_Project/Data/Achievements";
        private const string OldTreePath = Folder + "/AchievementTree.asset"; // ilk sürüm (tek ağaç)
        private const string TownScenePath = BilgiAdasi.Core.SceneFlow.GameScenePath;
        private const string ObjectName = "Achievements";

        private sealed class TabSeed
        {
            public string File, Title;
            public int Order;
            public Color Accent;
            public bool ComingSoon;
            public AchievementEntry[] Entries;
            public string Path => $"{Folder}/AchievementTree_{File}.asset";
        }

        private const AchievementKind L = AchievementKind.Learned, M = AchievementKind.Milestone, C = AchievementKind.Challenge;

        [MenuItem("BilgiAdasi/Kurulum/13 - Başarımlar ve Esc menüsü", priority = 12)]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var paths = EnsureTrees(overwrite: false);
            AssetDatabase.SaveAssets();
            SetUpTown(paths);
            Debug.Log("[BilgiAdasi] Başarımlar kuruldu: ders sekmeleri, Achievements objesi, ham bildirim ve Esc menüsü.");
        }

        [MenuItem("BilgiAdasi/Kurulum/13b - Başarımları görev listesinden yeniden yaz", priority = 13)]
        public static void Regenerate()
        {
            if (!EditorUtility.DisplayDialog("Başarımlar",
                "Sekmelerin başarım listesi 11-gorev-ve-basarim-listesi.md'ye göre yeniden yazılacak. " +
                "Inspector'da elle değiştirdiğin başarım metinleri/konumları gider; tür görünümü ve simgeler korunur. " +
                "Eski bölüm sekmeleri (Bereket Tarlası, Ormancı Korusu, Çarşı) silinir; görevleri ders sekmelerine geçti.",
                "Yeniden yaz", "Vazgeç")) return;
            EnsureTrees(overwrite: true);
            AssetDatabase.SaveAssets();
            int synced = SyncOpenControllers();
            Debug.Log($"[BilgiAdasi] Başarım sekmeleri görev listesinden yeniden yazıldı (ders bazlı). " +
                      $"Açık sahnede eşitlenen Achievements: {synced}. Sahneyi kaydet (Ctrl+S).");
        }

        /// <summary>Açık sahnelerdeki AchievementController'lar bütün ders sekmelerini bağlar.</summary>
        private static int SyncOpenControllers()
        {
            int count = 0;
            foreach (var c in Object.FindObjectsByType<AchievementController>(FindObjectsInactive.Include))
            {
                Undo.RecordObject(c, "Başarım sekmelerini eşitle");
                c.EditorSyncTrees();
                EditorUtility.SetDirty(c);
                EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
                count++;
            }
            return count;
        }

        private static List<string> EnsureTrees(bool overwrite)
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project/Data", "Achievements");
            var seeds = Seeds();

            // İlk sürümün tek ağacı Kimya sekmesine taşınır (GUID ve sahne referansı korunur), içeriği yeniden yazılır.
            bool migrated = false;
            if (AssetDatabase.LoadAssetAtPath<AchievementTree>(OldTreePath) != null
                && AssetDatabase.LoadAssetAtPath<AchievementTree>(seeds[0].Path) == null)
                migrated = string.IsNullOrEmpty(AssetDatabase.MoveAsset(OldTreePath, seeds[0].Path));

            // Bölüm sekmelerinden ders sekmelerine geçiş: eski sekmeler silinir, Kimya da (tarla dalı için) yeniden yazılır.
            for (int i = 0; i < ObsoleteFiles.Length; i++)
            {
                string old = $"{Folder}/AchievementTree_{ObsoleteFiles[i]}.asset";
                if (AssetDatabase.LoadAssetAtPath<AchievementTree>(old) != null && AssetDatabase.DeleteAsset(old)) migrated = true;
            }

            var paths = new List<string>();
            for (int i = 0; i < seeds.Length; i++)
            {
                var s = seeds[i];
                var tree = AssetDatabase.LoadAssetAtPath<AchievementTree>(s.Path);
                if (tree == null)
                {
                    tree = ScriptableObject.CreateInstance<AchievementTree>();
                    tree.Configure(s.Title, s.Order, s.Accent, s.ComingSoon, s.Entries);
                    AssetDatabase.CreateAsset(tree, s.Path);
                }
                else if (overwrite || (migrated && i == 0))
                {
                    Undo.RecordObject(tree, "Başarımları yeniden yaz");
                    tree.Configure(s.Title, s.Order, s.Accent, s.ComingSoon, s.Entries);
                    EditorUtility.SetDirty(tree);
                }
                paths.Add(s.Path);
            }
            return paths;
        }

        // ---------- İçerik (11-gorev-ve-basarim-listesi.md) ----------

        // Ders sekmeleri; renkler 09 paletinden: Kimya bakır, Matematik dağ mavisi, Fizik kaya grisi, Biyoloji yaprak yeşili.
        private static TabSeed[] Seeds() => new[]
        {
            new TabSeed { File = "Kimya", Order = 0, Title = "Kimya", Accent = Hex("#9F4E2B"), Entries = Kimya() },
            // Matematik ve Biyoloji: Sv2 Tohum Nefes Alır (bereket_2) oynanabilir → sekme açık (14.11).
            new TabSeed { File = "Matematik", Order = 1, Title = "Matematik", Accent = Hex("#4F7F96"), ComingSoon = false, Entries = Matematik() },
            new TabSeed { File = "Fizik", Order = 2, Title = "Fizik", Accent = Hex("#6E8090"), ComingSoon = true, Entries = Fizik() },
            new TabSeed { File = "Biyoloji", Order = 3, Title = "Biyoloji", Accent = Hex("#3FA83A"), ComingSoon = false, Entries = Biyoloji() },
        };

        // Önceki sürümün bölüm sekmeleri (görevleri artık ders sekmelerinde): yeniden yazarken kaldırılır.
        private static readonly string[] ObsoleteFiles = { "BereketTarlasi", "OrmanciKorusu", "Carsi" };

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;

        private static AchievementEntry Task(string task, string parent, string title, string defter) =>
            new AchievementEntry("ach_" + task, parent, L, AchievementTrigger.TaskCompleted, task, title, defter);

        private static AchievementEntry Flawless(string task, string title, string text, bool secret = false) =>
            new AchievementEntry($"ach_{task}_flawless", "ach_" + task, C, AchievementTrigger.TaskFlawless, task, title, text, secret);

        private static AchievementEntry Village(string id, string parent, string neighborhood, string title, string text) =>
            new AchievementEntry(id, parent, M, AchievementTrigger.NeighborhoodCompleted, neighborhood, title, text);

        // Sıra önemli: her düğümün ilk çocuğu ana dal (düz devam eder), sonrakiler alta iner.
        private static AchievementEntry[] Kimya() => new[]
        {
            // Sv 0 — kök: Çıraklık görevi (K-0) bitince; oyun başında verilmez, ders öğrenilmeden başarım olmaz.
            new AchievementEntry("ach_start", "", M, AchievementTrigger.TaskCompleted, "cirak_0",
                "İlk Kural", "K-0 · Kazana ne girerse o çıkar: tepkimede atomlar kaybolmaz, yoktan var olmaz."),

            // Denkleştirme köyü (Sv 1–5) — üst dal
            Task("denk_1", "ach_start", "Kuru Çeşme",
                "K-1 · Tepkimede atomların türü ve sayısı korunur; değişen bağlanışlarıdır.\n2H<sub>2</sub> + O<sub>2</sub> → 2H<sub>2</sub>O"),
            Task("denk_2", "ach_denk_1", "İsli Fenerler",
                "K-2 · Yakıt O<sub>2</sub> ile yanar; C ve H içeren yakıt CO<sub>2</sub> ve H<sub>2</sub>O verir. Hava yetmezse is çıkar."),
            Task("denk_3", "ach_denk_2", "Soğuk Fırın",
                "K-3 · Her elementi iki kefede ayrı say. Önce C ve H, oksijen en son."),
            Task("denk_4", "ach_denk_3", "Yıkık Köprü",
                "K-4 · Bir tepkimenin ürünü başka bir tepkimenin gireni olabilir.\nCaCO<sub>3</sub> → CaO → Ca(OH)<sub>2</sub>"),
            Task("denk_5", "ach_denk_4", "Gübre Ruhu",
                "K-5 · Girenler katsayıları oranında harcanır; biri biterse tepkime durur, fazlası artar."),
            Village("ach_denklestirme", "ach_denk_5", "denklestirme",
                "Denkleştirme Köyü", "Çeşme akıyor, fenerler yanıyor, köprü ayakta. Kütle korunur!"),
            new AchievementEntry("ach_denk_5_nohint", "ach_denklestirme", C, AchievementTrigger.TaskWithoutHint, "denk_5",
                "Defter Kapalı", "Gübre Ruhu'nu ustanın defterine bakmadan bitirdin.", secret: true),
            Flawless("denk_1", "Köpürmeden: Çeşme", "Kuru Çeşme'yi kazanı hiç köpürtmeden bitirdin."),
            Flawless("denk_2", "Köpürmeden: Fener", "İsli Fenerler'i kazanı hiç köpürtmeden bitirdin."),
            Flawless("denk_3", "Köpürmeden: Fırın", "Soğuk Fırın'ı kazanı hiç köpürtmeden bitirdin."),
            Flawless("denk_4", "Köpürmeden: Harç", "İki adımlı kireci kazanı hiç köpürtmeden çıkardın."),
            Flawless("denk_5", "Köpürmeden: Gübre", "Dört kavanozu kazanı hiç köpürtmeden tastamam çıkardın."),

            // Tepkime Türleri köyü (Sv 6–10) — alt dal
            Task("tur_1", "ach_start", "Ustanın Defteri",
                "K-6 · Sentez: birden çok madde → tek ürün. Analiz: tek madde → daha basit maddeler. Yanma: madde + O<sub>2</sub> → ısı ve ışık."),
            Task("tur_2", "ach_tur_1", "Etiket Masası",
                "K-7 · Tek giren varsa analiz, tek ürün varsa sentez; O<sub>2</sub> girip ısı-ışık çıkıyorsa yanma."),
            Task("tur_3", "ach_tur_2", "Fırıncının Mektubu",
                "K-8 · Kimyasal değişimin kanıtı: yeni madde, gaz, renk, ısı-ışık, koku. Yalnız şekil ya da hâl değişimi fizikseldir."),
            Task("tur_4", "ach_tur_3", "Muhtarın Mektubu",
                "K-9 · Bir iddiayı çürütmek için tek karşı örnek yeter; düzeltirken kanıt göster."),
            Task("tur_5", "ach_tur_4", "Lonca Sınavı",
                "K-10 · Aynı atomlar (C, H, O) doğada döngü hâlinde dolaşır: yakıtta, havada, bitkide, bizde."),
            Village("ach_tepkime_turleri", "ach_tur_5", "tepkime_turleri",
                "Tepkime Türleri Köyü", "Güvercinler uçuyor, lonca kapısı açık, pano dolu."),
            new AchievementEntry("ach_iki_koy", "ach_tepkime_turleri", M, AchievementTrigger.AllOf, "ach_denklestirme, ach_tepkime_turleri",
                "Hiçbir Atom Eksik Değil", "İki köyü de ayağa kaldırdın. Tahtanda hiçbir satırda bir atom eksik, bir atom fazla yok."),
            Flawless("tur_5", "Tek Seferde: Lonca", "Lonca Sınavı'nı hiç yanlış yapmadan geçtin.", secret: true),

            // Sürüm 2 — tarladaki Kimya görevleri (kökten üçüncü dal)
            Task("tarla_1", "ach_start", "Makineyi Uyandır",
                "K-11 · Bereket Tarlası. Makinede yakıtın kimyasal enerjisi ısıya, ısı da harekete dönüşür."),
            Task("tarla_3", "ach_tarla_1", "Toprağı Tanı",
                "K-13 · Bereket Tarlası. Ekşi (asidik) toprağı kireç gibi bazik maddeler düzeltir."),
        };

        // Sürüm 2 dersleri: her görev, defter kaydının dersine göre tek sekmede (11-gorev-ve-basarim-listesi.md).
        // Kök = dersin ilk görevi; ilk dal düz gider, diğer yerler (tarla, koru, çarşı) kökten ayrılan dallardır.

        private static AchievementEntry[] Matematik()
        {
            var list = Subject(
                new[]
                {
                    // Sv2: iki derse ait görev; aynı başarım iki sekmede (tek bildirim). tarla_4 Tohum Hesabı bunun içine girdi.
                    T("bereket_2", "Tohum Nefes Alır", "K-12 · Bereket Tarlası. Beklenen sayı = olasılık × deneme; tohum = filiz ÷ olasılık (120 ÷ 0,8 = 150)."),
                    T("tarla_5", "Ekim Düzeni", "K-15 · Bereket Tarlası. Bir işi adım adım yazınca (algoritma) kaç dönüş, kaç seçenek olduğunu sayabilirim."),
                    T("tarla_9", "Hasat Raporu", "K-19 · Bereket Tarlası. Ortalama tek sayıyla özetler; grafik farkları gösterir."),
                },
                new[] { T("orman_2", "Yön Çentiği", "K-22 · Ormancı Korusu. Üçgenin iç açıları toplamı 180°; yüzde = oran × 100.") },
                new[]
                {
                    T("carsi_1", "Teraziyle Satış", "K-27 · Çarşı. Doğru orantıda birim fiyat sabittir."),
                    T("carsi_2", "İndirim mi Kazık mı?", "K-28 · Çarşı. Yüzdeler toplanmaz, çarpılır."),
                    T("carsi_3", "Yol Hesabı", "K-29 · Çarşı. İki nokta arası uzaklık, dik üçgenin hipotenüsüdür."),
                });
            list.Add(Village("ach_carsi", "ach_carsi_3", "carsi", "Çarşının Hesabı", "Terazi, yüzde, yol: çarşıda kimse seni kandıramaz."));
            list.Add(Mastery("ach_matematik", "ach_tarla_9", list, "Hesap Ustası'nın Mührü",
                "Matematik görevlerinin hepsini bitirdin. Hesap Ustası kapının madalyonunu mühürledi."));
            return list.ToArray();
        }

        private static AchievementEntry[] Fizik()
        {
            var list = Subject(
                new[]
                {
                    T("tarla_2", "İlk Saban", "K-12 · Bereket Tarlası. Sabit hızda alınan yol = hız × zaman."),
                    T("tarla_6", "Kuyudan Tarlaya", "K-16 · Bereket Tarlası. İş = kuvvet × yol; güç = iş / zaman."),
                    T("tarla_10", "Değirmenden Fırına", "K-20 · Bereket Tarlası. Rüzgârın hareket enerjisi çarkla una, unun enerjisi ekmekle bize geçer."),
                },
                new[]
                {
                    T("orman_3", "Ağaç Düşüyor!", "K-23 · Ormancı Korusu. Yükseklikteki enerji düşerken hareket enerjisine dönüşür; toplam korunur."),
                    T("orman_4", "Kızakla Taşı", "K-24 · Ormancı Korusu. Kuvvet–yol grafiğinin altındaki alan yapılan işi verir."),
                    T("orman_6", "Odun Ateşe", "K-26 · Ormancı Korusu. Yenilenebilirlik, kaynağın yerine konup konmadığına bağlıdır."),
                });
            list.Add(Mastery("ach_fizik", "ach_tarla_10", list, "Makine Ustası'nın Mührü",
                "Fizik görevlerinin hepsini bitirdin. Makine Ustası çarkını sana emanet etti."));
            return list.ToArray();
        }

        private static AchievementEntry[] Biyoloji()
        {
            var list = Subject(
                new[]
                {
                    T("bereket_2", "Tohum Nefes Alır", "K-12 · Bereket Tarlası. Çimlenen tohum besinini oksijenle kullanır (oksijenli solunum); toprak bu yüzden sürülür."),
                    T("tarla_7", "Elif'in Gübresi", "K-17 · Bereket Tarlası. Azot döngüsü: havadaki N<sub>2</sub> bağlanmadan bitkiye geçmez."),
                    T("tarla_8", "Güneşten Besine", "K-18 · Bereket Tarlası. Fotosentez, solunum ve yanmanın tersi gibidir."),
                },
                new[]
                {
                    T("orman_1", "Hangi Ağaç?", "K-21 · Ormancı Korusu. Ağaç yaşı halkalardan okunur; yuvalı ağaç bir canlı topluluğunun evidir, korunur."),
                    T("orman_5", "Bir Kes, Üç Dik", "K-25 · Ormancı Korusu. Kestiğin yerine dikersen orman sürdürülebilir kalır."),
                });
            list.Add(Mastery("ach_biyoloji", "ach_tarla_8", list, "Hayat Ustası'nın Mührü",
                "Biyoloji görevlerinin hepsini bitirdin. Hayat Ustası defterine bir yaprak bastırdı."));
            return list.ToArray();
        }

        private static (string task, string title, string defter) T(string task, string title, string defter) => (task, title, defter);

        /// <summary>Kök = ilk dalın ilk görevi; ilk dal kökten düz devam eder, diğer dallar kökten ayrılır.</summary>
        private static List<AchievementEntry> Subject(params (string task, string title, string defter)[][] branches)
        {
            var list = new List<AchievementEntry>();
            string root = "ach_" + branches[0][0].task;
            for (int b = 0; b < branches.Length; b++)
            {
                string parent = b == 0 ? "" : root;
                foreach (var (task, title, defter) in branches[b])
                {
                    list.Add(Task(task, parent, title, defter));
                    parent = "ach_" + task;
                }
            }
            // Kökün çocukları: ilk dalın devamı listede diğer dallardan önce olmalı (ana dal düz gitsin) — öyle.
            return list;
        }

        /// <summary>Dersin bütün görev başarımları açılınca (AllOf).</summary>
        private static AchievementEntry Mastery(string id, string parent, List<AchievementEntry> subject, string title, string text)
        {
            var ids = new List<string>();
            foreach (var e in subject)
                if (e.Trigger == AchievementTrigger.TaskCompleted) ids.Add(e.Id);
            return new AchievementEntry(id, parent, M, AchievementTrigger.AllOf, string.Join(", ", ids), title, text);
        }


        // ---------- Sahne ----------

        private static void SetUpTown(List<string> treePaths)
        {
            var scene = EditorSceneManager.OpenScene(TownScenePath);

            var achievements = Object.FindAnyObjectByType<AchievementController>(FindObjectsInactive.Include);
            if (achievements == null) achievements = new GameObject(ObjectName).AddComponent<AchievementController>();
            var so = new SerializedObject(achievements);
            var list = so.FindProperty("trees");
            list.arraySize = treePaths.Count;
            for (int i = 0; i < treePaths.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AchievementTree>(treePaths[i]);
            so.ApplyModifiedPropertiesWithoutUndo();

            var pause = Object.FindAnyObjectByType<PauseMenuController>(FindObjectsInactive.Include);
            if (pause == null) pause = achievements.gameObject.AddComponent<PauseMenuController>();
            var pso = new SerializedObject(pause);
            pso.FindProperty("achievements").objectReferenceValue = achievements;
            pso.ApplyModifiedPropertiesWithoutUndo();

            var hud = GameObject.Find("_Placeholder/HUD");
            if (hud == null)
            {
                Debug.LogWarning("[BilgiAdasi] _Placeholder/HUD bulunamadı; ham bildirim ve Esc menüsü eklenmedi.");
            }
            else
            {
                if (hud.GetComponentInChildren<PlaceholderAchievementToast>(true) == null)
                    AddStretched(hud.transform, "AchievementToast").AddComponent<PlaceholderAchievementToast>();
                var menu = hud.GetComponentInChildren<PlaceholderPauseMenu>(true);
                if (menu == null) menu = AddStretched(hud.transform, "PauseMenu").AddComponent<PlaceholderPauseMenu>();
                menu.transform.SetAsLastSibling(); // her şeyin üstünde
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static GameObject AddStretched(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }
    }
}
