using BilgiAdasi.Chemistry;
using BilgiAdasi.City;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using BilgiAdasi.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Seviye 0 — Çıraklık "İlk Kural" (04-gorev-plani §4, 01 §1): veri (R_isitma 2H₂ → 2H₂, cirak_0, Çıraklık köyü,
    /// Ciraklik.asset metinleri, K-0) + sahne (ApprenticeshipDirector, runner listesinin başına köy, HUD'a hikâye kartı).
    /// Denkleştirme'nin önkoşulu Çıraklık olur. Tekrar çalıştırmak güvenlidir (GUID korunur). Metin düzeltmesi burada.
    /// </summary>
    public static class ApprenticeshipBuilder
    {
        private const string Data = "Assets/_Project/Data";
        private const string TaskFolder = Data + "/Tasks/Ciraklik";
        private const string TaskPath = TaskFolder + "/cirak_0.asset";
        private const string ReactionPath = Data + "/Reactions/R_isitma.asset";
        private const string NeighborhoodPath = Data + "/Neighborhoods/Neighborhood_ciraklik.asset";
        private const string DenklestirmePath = Data + "/Neighborhoods/Neighborhood_denklestirme.asset";
        private const string TextsPath = Data + "/Texts/Ciraklik.asset";

        // 01 §1 (açılış, uyanış) + 04 §4 Seviye 0 — içerik değişirse önce dokümanlar.
        private static readonly string[] ClassroomCards =
        {
            "Sonbahar sabahı. Kimya dersi.\nTahtada iki kelime: <b>Kimyasal Tepkimeler</b>.",
            "Sınıf ılık, öğretmenin sesi uzaklaşıyor…\nGöz kapakların ağırlaşıyor.",
            "…ve bir yerde, bir kazan fokurdamaya başlıyor."
        };

        // Göz açılınca ada: kare kare sinematik. Kamera konumları Harita sahnesine göre (dünya koordinatı).
        private static IntroShot[] IntroShots() => new[]
        {
            new IntroShot("Bilgi Adası",
                "Gözünü açtığında sınıf yok. Hiçbir haritada olmayan bir adadasın: burada her şehir bir ders.",
                new Vector3(-80f, 48f, -42f), new Vector3(-64f, 42f, -50f), new Vector3(-8f, 0f, 12f), new Vector3(-3f, 0f, 10f), 6f),
            new IntroShot("Sisin ardında",
                "Matematik, Fizik, Biyoloji… Öteki şehirlerin kapıları kapalı. Anahtarı, öğrendiklerin.",
                new Vector3(-21.5f, 9f, -40f), new Vector3(-21.5f, 8.2f, -46f), new Vector3(-21.5f, 7f, -72f), new Vector3(-21.5f, 7.5f, -74f)),
            new IntroShot("Denkleştirme Köyü",
                "Çeşme kurumuş, fenerler isleniyor, fırın soğumuş. Köylüler bir çare arıyor. Çare sende.",
                new Vector3(-37f, 2.8f, 18.2f), new Vector3(-39.5f, 2.5f, 18.6f), new Vector3(-46f, 1.3f, 19f), new Vector3(-48f, 1.3f, 19.4f)),
            new IntroShot("Câbir Usta'nın atölyesi",
                "Kapıya gelen her derdi bu atölyede çözeceksin. Usta yaşlandı; ona bir çırak lazım.",
                new Vector3(7.5f, 3.4f, 13.5f), new Vector3(6f, 2.8f, 11.5f), new Vector3(0f, 2f, 1f), new Vector3(0.3f, 2.2f, 1f)),
            new IntroShot("Kazan ve terazi",
                "Kazana ne girerse terazi onu tartar. Tek bir atom eksik kalsa kazan köpürür.",
                new Vector3(1.3f, 1.8f, -0.1f), new Vector3(0.5f, 1.6f, -0.6f), new Vector3(-1.2f, 0.75f, -2.6f), new Vector3(-1.3f, 0.7f, -2.7f)),
            new IntroShot("Atölyenin tezgâhı",
                "Köylüler tezgâhına gelecek. Malzemeyi çarşıdan al, kazanda pişir, siparişi kendi elinle teslim et.",
                new Vector3(-6.8f, 2.2f, 9.8f), new Vector3(-5.6f, 1.9f, 8.8f), new Vector3(-2f, 1.2f, 5.3f), new Vector3(-1.8f, 1.2f, 5f)),
            new IntroShot("Çırak",
                "Dün sıradan bir öğrenciydin. Bugün Kimya şehrinin yeni çırağısın. Ama önce… biri seni uyandırıyor.",
                new Vector3(2.6f, 0.45f, 7.4f), new Vector3(2.4f, 0.5f, 6.8f), new Vector3(1.8f, 2f, 3f), new Vector3(1.9f, 2.2f, 3f), 5.5f),
        };

        private static readonly string[] WakeLines =
        {
            "Nihayet uyandın. Kapımın önünde yatıyordun; çırak arıyordum zaten.",
            "Bu şehirde her köyün bir derdi var; dertler de ancak ölçüyle çözülür. Gel, sana ilk kuralı göstereyim.",
            "Al, bu defter senin. Gördüğünü yaz. Gördüğün şey kazanda kaybolmaz."
        };

        /// <summary>Yalnızca açılışı (sınıf kartları + ada kareleri) Ciraklik.asset'e yeniden yazar; sahneye dokunmaz.</summary>
        [MenuItem("BilgiAdasi/Kurulum/17b - Çıraklık: açılış sinematiği (metin + kareler)", priority = 18)]
        public static void RunIntro()
        {
            var texts = AssetDatabase.LoadAssetAtPath<ApprenticeshipData>(TextsPath);
            if (texts == null)
            {
                Debug.LogWarning("[Çıraklık] " + TextsPath + " yok; önce Kurulum 17.");
                return;
            }
            var so = new SerializedObject(texts);
            var cards = so.FindProperty("classroomCards");
            cards.arraySize = ClassroomCards.Length;
            for (int i = 0; i < ClassroomCards.Length; i++) cards.GetArrayElementAtIndex(i).stringValue = ClassroomCards[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            texts.SetIntro(IntroShots());
            EditorUtility.SetDirty(texts);
            AssetDatabase.SaveAssets();
            Debug.Log("[Çıraklık] Açılış: " + ClassroomCards.Length + " kart + " + texts.IntroShots.Length + " kare yazıldı.");
        }

        [MenuItem("BilgiAdasi/Kurulum/17 - Çıraklık (Seviye 0): veri + sahne", priority = 17)]
        public static void Run()
        {
            var h2 = AssetDatabase.LoadAssetAtPath<SpeciesData>(Data + "/Species/Species_H2.asset");
            var denklestirme = AssetDatabase.LoadAssetAtPath<NeighborhoodData>(DenklestirmePath);
            if (h2 == null || denklestirme == null)
            {
                EditorUtility.DisplayDialog("Çıraklık", "H₂ ya da Denkleştirme köyü bulunamadı (Kurulum 2 ve 6).", "Tamam");
                return;
            }
            if (!AssetDatabase.IsValidFolder(TaskFolder)) AssetDatabase.CreateFolder(Data + "/Tasks", "Ciraklik");

            var heat = AssetDatabase.LoadAssetAtPath<ReactionData>(ReactionPath);
            if (heat == null)
            {
                heat = ReactionData.Create("R_isitma", ReactionType.Other,
                    new[] { new SpeciesAmount(h2, 2) }, new[] { new SpeciesAmount(h2, 2) });
                AssetDatabase.CreateAsset(heat, ReactionPath);
            }

            var task = CreateOrUpdate(OrderTask.Create("cirak_0", "Câbir Usta",
                    new[] { R("İki kavanoz H₂ kazana, sonra ateş. Gözün terazide olsun.") },
                    new string[0],
                    R("İki kavanozun ikisini de dök. Terazide sol kefe kazan, sağ kefe beklenen; ikisi eşitse ateşi yak."),
                    "",
                    OrderMode.Exact,
                    new[] { new OrderStep(heat, new[] { new SpeciesAmount(h2, 2) }, true) },
                    new SpeciesData[0]),
                TaskPath);

            var taskSo = new SerializedObject(task);
            taskSo.FindProperty("reward").intValue = 0;   // çıraklıkta ödeme yok (kapıda teslim de yok)
            taskSo.ApplyModifiedPropertiesWithoutUndo();

            var ciraklik = CreateOrUpdate(NeighborhoodData.Create("ciraklik", "Çıraklık", new string[0], new TaskData[] { task }),
                NeighborhoodPath);

            denklestirme.SetRequires(ciraklik);
            EditorUtility.SetDirty(denklestirme);

            var texts = AssetDatabase.LoadAssetAtPath<ApprenticeshipData>(TextsPath);
            if (texts == null)
            {
                texts = ScriptableObject.CreateInstance<ApprenticeshipData>();
                AssetDatabase.CreateAsset(texts, TextsPath);
            }
            texts.Set(task, ClassroomCards, LearningDataBuilder.RichAll(WakeLines),
                R("Kazana iki kavanoz hidrojen koyup ısıtırsam, içindeki hidrojen azalır mı?"),
                new[] { "Hayır, aynı kalır.", "Azalır, buhar olup gider.", "Artar, ısı yeni hidrojen yapar." }, 0,
                new[]
                {
                    "Göreceğiz. Kazana dök, teraziye bak.",
                    "Öyle mi dersin? Dök de teraziyle birlikte bakalım.",
                    "Isı madde yapsaydı ocak zengin olurdu. Dök de görelim."
                },
                h2, 2);
            texts.SetIntro(IntroShots());
            EditorUtility.SetDirty(texts);

            LearningDataBuilder.Build();
            AssetDatabase.SaveAssets();
            SetUpScene(ciraklik, texts);
            Debug.Log("[Çıraklık] Seviye 0 kuruldu: cirak_0, Neighborhood_ciraklik (Denkleştirme'nin önkoşulu), K-0.");
        }

        private static void SetUpScene(NeighborhoodData ciraklik, ApprenticeshipData texts)
        {
            var runner = Object.FindAnyObjectByType<NeighborhoodRunner>(FindObjectsInactive.Include);
            if (runner == null)
            {
                Debug.LogWarning("[Çıraklık] Açık sahnede NeighborhoodRunner yok; sahne adımı atlandı (Harita'yı açıp tekrar çalıştır).");
                return;
            }

            // Runner listesinin başına Çıraklık (kapıda sıradaki siparişte önce gelir).
            var so = new SerializedObject(runner);
            var list = so.FindProperty("neighborhoods");
            for (int i = list.arraySize - 1; i >= 0; i--)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == ciraklik) list.DeleteArrayElementAtIndex(i);
            list.InsertArrayElementAtIndex(0);
            list.GetArrayElementAtIndex(0).objectReferenceValue = ciraklik;
            so.ApplyModifiedPropertiesWithoutUndo();

            var director = runner.GetComponent<ApprenticeshipDirector>() ?? runner.gameObject.AddComponent<ApprenticeshipDirector>();
            var dso = new SerializedObject(director);
            dso.FindProperty("data").objectReferenceValue = texts;
            dso.FindProperty("runner").objectReferenceValue = runner;
            dso.FindProperty("orderController").objectReferenceValue = Object.FindAnyObjectByType<OrderController>(FindObjectsInactive.Include);
            dso.FindProperty("dialogue").objectReferenceValue = runner.GetComponent<DialogueDirector>();
            var door = GameObject.Find("Atolye_Sanat/Kapi_Rig") ?? GameObject.Find("Station_Door");
            dso.FindProperty("wakeLookAt").objectReferenceValue = door != null ? door.transform : null;
            dso.ApplyModifiedPropertiesWithoutUndo();

            // Hikâye kartı: HUD'da kararmanın da üstünde (son kardeş).
            var hud = GameObject.Find("UI/HUD") ?? GameObject.Find("HUD");
            if (hud != null && hud.GetComponentInChildren<FigmaStoryCard>(true) == null)
            {
                var go = new GameObject("StoryCard", typeof(RectTransform));
                var rt = (RectTransform)go.transform;
                rt.SetParent(hud.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                go.AddComponent<FigmaStoryCard>();
            }
            if (hud != null) hud.GetComponentInChildren<FigmaStoryCard>(true).transform.SetAsLastSibling();

            EditorSceneManager.MarkSceneDirty(runner.gameObject.scene);
            EditorSceneManager.SaveScene(runner.gameObject.scene);
        }

        private static T CreateOrUpdate<T>(T fresh, string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(fresh, path);
                return fresh;
            }
            string keepName = existing.name;
            EditorUtility.CopySerialized(fresh, existing);
            existing.name = keepName;
            Object.DestroyImmediate(fresh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static string R(string text) => LearningDataBuilder.Rich(text);
    }
}
