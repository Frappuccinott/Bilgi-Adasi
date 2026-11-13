// Bilgi Adası — Blender'dan gelen kasaba haritasını açık sahneye tek seferde kurar.
// Menü: BilgiAdasi → Sanat → Haritayı açık sahneye yerleştir
// Kaynak FBX'ler (ArtSource/Blender/Script/harita*.py ile üretilir):
//   Art/Harita/Harita.fbx          arazi, yollar, meydan, su, köyler, yön, detay (Blender dünya konumları)
//   Art/Harita/Harita_Doga.fbx     doğa türleri (SM_<Tür>, bazıları _LOD0/_LOD1) — her biri orijinde
//   Art/Harita/Harita_Yerler.fbx   Yer_<Tür>_NNN boş objeleri (doğa kopyalarının konumu/dönüşü/ölçeği)
//   Art/Harita/Bereket_Tarlasi.fbx, Ormanci_Korusu.fbx (dünya konumları)
//   Art/Traktor/Traktor.fbx, Traktor_Aletler.fbx, Art/Bisiklet/Bisiklet.fbx (orijinde)
// Koordinat: Blender (x, y, z) → Unity (−x, z, −y). Haritada atölye Blender (0, 12)'dedir; Unity'de atölye
// (Atolye_Sanat) orijinde durduğu için harita kökü (0, 0, +12)'ye konur.
// Bu dosya sanat aracıdır (Mustafa); oyun kodu değildir. NeighborhoodView / LockedZone yalnızca sahneye eklenir.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using BilgiAdasi.City;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BilgiAdasi.SanatAraclari
{
    public static class HaritaYerlestir
    {
        const string Kok = "Assets/_Project/Art/";
        const string HaritaFbx = Kok + "Harita/Harita.fbx";
        const string DogaFbx = Kok + "Harita/Harita_Doga.fbx";
        const string YerlerFbx = Kok + "Harita/Harita_Yerler.fbx";
        const string TarlaFbx = Kok + "Harita/Bereket_Tarlasi.fbx";
        const string KoruFbx = Kok + "Harita/Ormanci_Korusu.fbx";
        const string TraktorFbx = Kok + "Traktor/Traktor.fbx";
        const string AletFbx = Kok + "Traktor/Traktor_Aletler.fbx";
        const string BisikletFbx = Kok + "Bisiklet/Bisiklet.fbx";
        const string PrefabKlasor = Kok + "Harita/Prefablar";
        public const string KokAdi = "Harita_Sanat";
        static readonly Vector3 HaritaOfset = new Vector3(0f, 0f, 12f);

        /// <summary>Blender XY (m) → Unity dünya XZ (harita kökü ofsetiyle).</summary>
        static Vector3 B2U(float bx, float by, float y = 0f) => new Vector3(-bx, y, -by) + HaritaOfset;

        // ------------------------------------------------------------------ menü
        [MenuItem("BilgiAdasi/Sanat/Haritayı açık sahneye yerleştir")]
        public static void Yerlestir()
        {
            var eski = GameObject.Find(KokAdi);
            if (eski != null) Undo.DestroyObjectImmediate(eski);
            var kok = new GameObject(KokAdi);
            Undo.RegisterCreatedObjectUndo(kok, "Haritayı yerleştir");

            var harita = Ornek(HaritaFbx, kok.transform, "Harita");
            if (harita == null) return;
            harita.transform.position = HaritaOfset;
            HaritayiHazirla(harita);
            KoyleriGrupla(harita);

            // Tek kök objeli FBX'lerde Unity kökün konumunu atar: kök, Blender'daki "Konum_" işaretine konur (birebir
            // Blender konumu; işaret yoksa eski yol: merkezdeki zemin).
            Physics.SyncTransforms();
            var tarla = Ornek(TarlaFbx, kok.transform, "Bereket_Tarlasi");
            if (tarla != null) { tarla.transform.position = KonumYa(harita, "Konum_Bereket_Tarlasi", () => ZemindeB(60f, 24f)); TarlayiHazirla(tarla); }
            var koru = Ornek(KoruFbx, kok.transform, "Ormanci_Korusu");
            if (koru != null) { koru.transform.position = KonumYa(harita, "Konum_Ormanci_Korusu", () => ZemindeB(40.5f, 24f)); KoruyuHazirla(koru); }

            Physics.SyncTransforms();
            int doga = DogayiDiz(kok.transform);
            Physics.SyncTransforms();
            AraclariKoy(kok.transform, harita);

            if (GameObject.Find("Atolye_Sanat") == null)
                EditorApplication.ExecuteMenuItem("BilgiAdasi/Sanat/Atölyeyi açık sahneye yerleştir");

            Selection.activeGameObject = kok;
            Debug.Log($"[Harita] Yerleştirildi: {KokAdi} — doğa kopyası {doga}, " +
                      $"renderer {kok.GetComponentsInChildren<Renderer>(true).Length}, collider {kok.GetComponentsInChildren<Collider>(true).Length}.");
        }

        /// <summary>Harita.fbx içindeki "Konum_*" işaretinin dünya konumu (Blender'daki yer, birebir); yoksa yedek.</summary>
        static Vector3 KonumYa(GameObject harita, string ad, System.Func<Vector3> yedek)
        {
            var t = harita.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == ad);
            return t != null ? t.position : yedek();
        }

        static Transform Konum(GameObject harita, string ad) =>
            harita.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == ad);

        static GameObject Ornek(string yol, Transform ebeveyn, string ad)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(yol);
            if (model == null) { Debug.LogError("[Harita] Bulunamadı: " + yol); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model, ebeveyn);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = ad;
            return go;
        }

        // ------------------------------------------------------------------ ana harita
        static bool Bas(string ad, params string[] onekler) => onekler.Any(ad.StartsWith);

        static void HaritayiHazirla(GameObject harita)
        {
            // Görünmez sınır (COL_ önekli; harita.py build_sinir): yalnız MeshCollider, renderer silinir. Oyuncu kaya
            // basamağına zıplayıp arazinin kenarından boşluğa düşüyordu (Play testi 13.11).
            foreach (var mr in harita.GetComponentsInChildren<MeshRenderer>(true).Where(m => m.name.StartsWith("COL_")).ToList())
            {
                var go = mr.gameObject;
                if (go.GetComponent<Collider>() == null)
                    go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(mr);
            }
            foreach (var mr in harita.GetComponentsInChildren<MeshRenderer>(true))
            {
                var go = mr.gameObject;
                var ad = go.name;
                bool uzak = Bas(ad, "SM_Uzak_", "Bulutlar", "Pus_");
                bool serit = Bas(ad, "SM_Yollar", "SM_Koy_Zeminleri", "Su_Dere", "SM_Meydan_Doseme");
                bool durum = Bas(ad, "Denk_", "Tepk_", "Degirmen_Kanat");          // görev parçaları / hareketli
                // Gölge bütçesi (URP 4 kademe × 50 m): arazi ve göz hizasının altındaki alçak eşyalar gölge düşürmez;
                // eğimler yine köşe normaliyle (N·L) gölgelenir, gölgeyi alırlar.
                bool alcak = Bas(ad, "SM_Arazi", "SM_Mil_Taslari", "SM_Tas_Duvarlar", "SM_Ev_Bahceleri", "SM_Odun_Yiginlari",
                                 "SM_Meydan_Banklar", "SM_Bisiklet_Park_Yerleri", "SM_Camasir_Ipleri", "SM_Denk_Tarlalar");
                if (uzak || serit || alcak || ad.Contains("Duman") || ad.Contains("Fener_Isik") || ad.Contains("Kor") || ad.Contains("Su"))
                {
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                    if (uzak) mr.receiveShadows = false;
                }
                // çarpışma: arazi + üstünde yürünen/çarpılan yapılar. Bahçe/tarla ekinleri, şeritler, uzak manzara hariç.
                // Mil taşları ve çamaşır direkleri de çarpar (Play'de içlerinden geçiliyordu, 13.11); köy bitince çıkan
                // sancak/bayrak direkleri de (görünür olduklarında içinden geçilmesin).
                bool collider = ad == "SM_Arazi" || ad == "SM_Meydan_Doseme" ||
                                (Bas(ad, "SM_") && !uzak && !serit && !Bas(ad, "SM_Ev_Bahceleri", "SM_Denk_Tarlalar", "SM_Denk_Fener_Direkleri")) ||
                                Bas(ad, "Denk_P4_Kopru_Tas", "Denk_Once_Kopru_Yikik", "Tepk_P5_Sancak", "Denk_Sonra_Bayraklar", "Tepk_Sonra_Bayraklar");
                if (ad == "SM_Meydan_Cinar")
                {
                    // yalnız gövde: tacın altından geçilir, taç yukarıdan bakan ışınları da kesmez
                    var cc = go.AddComponent<CapsuleCollider>();
                    cc.center = new Vector3(0f, 2.5f, 0f); cc.height = 5f; cc.radius = 0.9f;
                    collider = false;
                }
                if (collider && go.GetComponent<Collider>() == null)
                    go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
                if (!durum)
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic |
                                                               StaticEditorFlags.OccludeeStatic);
            }
            // fener direkleri ince: dikme başına kutu yerine tek mesh collider yeter (yolu kapatmaz)
            var fd = harita.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "SM_Denk_Fener_Direkleri");
            if (fd != null && fd.GetComponent<Collider>() == null) fd.gameObject.AddComponent<MeshCollider>();

            // kilitli bölgeler: boş objenin ölçeği bölge boyudur → BoxCollider + LockedZone (prototipte hep kilitli)
            foreach (var t in harita.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("LockedZone_")).ToList())
            {
                if (t.GetComponent<BoxCollider>() == null) t.gameObject.AddComponent<BoxCollider>();
                if (t.GetComponent<LockedZone>() == null) t.gameObject.AddComponent<LockedZone>();
            }
            // önerilen doğuş noktası bilgi içindir; PlayerSpawn sahnede ayrıca durur
            var oneri = harita.transform.Find("_PlayerSpawn_Oneri");
            if (oneri != null) oneri.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ köyler → NeighborhoodView (08 §2)
        static void KoyleriGrupla(GameObject harita)
        {
            Grupla(harita.transform.Find("Koy_Denk"), "Denk", "Neighborhood_Denklestirme", "denklestirme");
            Grupla(harita.transform.Find("Koy_Tepk"), "Tepk", "Neighborhood_TepkimeTurleri", "tepkime_turleri");
        }

        static void Grupla(Transform koy, string kisa, string yeniAd, string id)
        {
            if (koy == null) { Debug.LogWarning("[Harita] Köy kökü yok: " + kisa); return; }
            koy.name = yeniAd;
            var before = Alt(koy, "Before");
            var after = Alt(koy, "After");
            var parts = Alt(koy, "Parts");
            var part = new Transform[5];
            for (int i = 0; i < 5; i++) part[i] = Alt(parts, "Part_" + (i + 1));
            foreach (Transform c in koy.Cast<Transform>().ToList())
            {
                var ad = c.name;
                if (ad.StartsWith(kisa + "_Once_")) c.SetParent(before, true);
                else if (ad.StartsWith(kisa + "_Sonra_")) c.SetParent(after, true);
                else if (ad.StartsWith(kisa + "_P") && ad.Length > kisa.Length + 2 && char.IsDigit(ad[kisa.Length + 2]))
                {
                    int n = ad[kisa.Length + 2] - '0';
                    if (n >= 1 && n <= 5) c.SetParent(part[n - 1], true);
                }
            }
            if (!koy.TryGetComponent<NeighborhoodView>(out var view)) view = koy.gameObject.AddComponent<NeighborhoodView>();   // Unity "sahte null": ?? kullanılmaz
            var so = new SerializedObject(view);
            so.FindProperty("neighborhoodId").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Transform Alt(Transform ebeveyn, string ad)
        {
            var t = ebeveyn.Find(ad);
            if (t != null) return t;
            var go = new GameObject(ad);
            go.transform.SetParent(ebeveyn, false);
            return go.transform;
        }

        // ------------------------------------------------------------------ tarla + koru başlangıç durumları
        static void TarlayiHazirla(GameObject tarla)
        {
            foreach (var t in tarla.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Tarla_P"))
                {
                    t.gameObject.SetActive(t.name.EndsWith("_Nadas"));
                    var mr = t.GetComponent<MeshRenderer>();
                    if (mr) mr.shadowCastingMode = ShadowCastingMode.Off;
                }
                else if (t.name == "SM_Bereket_Cit" && t.GetComponent<Collider>() == null)
                {
                    t.gameObject.AddComponent<MeshCollider>();
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
                }
            }
        }

        static void KoruyuHazirla(GameObject koru)
        {
            string[] gizli = { "Koru_Agac_Kuru_Centikli", "Koru_Agac_Kuru_Devrik", "Koru_Kutuk_Halkali", "Koru_Odun_Destesi", "Koru_Fidanlar" };
            foreach (var t in koru.GetComponentsInChildren<Transform>(true))
            {
                if (gizli.Contains(t.name)) t.gameObject.SetActive(false);
                if (t.name.StartsWith("Koru_Agac_") && t.GetComponent<Collider>() == null)
                {
                    var cc = t.gameObject.AddComponent<CapsuleCollider>();
                    cc.center = new Vector3(0f, 1.5f, 0f); cc.height = 3f; cc.radius = t.name.Contains("Kuru") ? 0.5f : 0.4f;
                }
                if ((t.name == "SM_Koru_Baltalik" || t.name == "Koru_Kutuk_Halkali" || t.name == "Koru_Odun_Destesi") && t.GetComponent<Collider>() == null)
                    t.gameObject.AddComponent<MeshCollider>().convex = true;     // görünür olunca içinden geçilmesin
            }
        }

        // ------------------------------------------------------------------ doğa: prefab + LODGroup + yerleşim
        struct TurAyar { public float lod0, cull; public bool golge; public int carpisma; }   // 0 yok, 1 gövde kapsülü, 2 dışbükey mesh

        static TurAyar Ayar(string tur)
        {
            if (tur.StartsWith("Cimen") || tur.StartsWith("Cicek") || tur == "Cakil") return new TurAyar { cull = 0.012f, golge = false };
            if (tur == "Saz") return new TurAyar { cull = 0.01f, golge = false };
            if (tur.StartsWith("Cali")) return new TurAyar { lod0 = 0.08f, cull = 0.008f, golge = true };
            if (tur.StartsWith("Kaya_Kitle")) return new TurAyar { cull = 0.002f, golge = true, carpisma = 2 };
            if (tur.StartsWith("Kaya")) return new TurAyar { cull = 0.006f, golge = true, carpisma = 2 };
            return new TurAyar { lod0 = 0.15f, cull = 0.006f, golge = true, carpisma = 1 };                   // ağaç, kavak, çam, meyve
        }

        static GameObject TurPrefab(GameObject dogaModel, string tur, Dictionary<string, GameObject> onbellek)
        {
            if (onbellek.TryGetValue(tur, out var hazir)) return hazir;
            var kaynak = dogaModel.transform.Find("SM_" + tur);
            if (kaynak == null) { onbellek[tur] = null; Debug.LogWarning("[Harita] Doğa türü yok: SM_" + tur); return null; }
            if (!AssetDatabase.IsValidFolder(PrefabKlasor)) AssetDatabase.CreateFolder(Kok + "Harita", "Prefablar");
            var go = Object.Instantiate(kaynak.gameObject);
            go.name = "SM_" + tur;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var ayar = Ayar(tur);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ayar.golge ? ShadowCastingMode.On : ShadowCastingMode.Off;
                r.lightProbeUsage = LightProbeUsage.Off;
            }
            var lod0 = go.GetComponentsInChildren<Renderer>(true).Where(r => r.name.EndsWith("_LOD0")).ToArray();
            var lod1 = go.GetComponentsInChildren<Renderer>(true).Where(r => r.name.EndsWith("_LOD1")).ToArray();
            if (!go.TryGetComponent<LODGroup>(out var grup)) grup = go.AddComponent<LODGroup>();
            if (lod0.Length > 0 && lod1.Length > 0)
                grup.SetLODs(new[] { new LOD(ayar.lod0, lod0), new LOD(ayar.cull, lod1) });
            else
                grup.SetLODs(new[] { new LOD(ayar.cull, go.GetComponentsInChildren<Renderer>(true)) });
            grup.RecalculateBounds();
            if (ayar.carpisma == 1)
            {
                var cc = go.AddComponent<CapsuleCollider>();
                cc.center = new Vector3(0f, 1.4f, 0f); cc.height = 2.8f; cc.radius = tur == "Kavak" ? 0.3f : 0.38f;
            }
            else if (ayar.carpisma == 2)
            {
                var mf = go.GetComponentInChildren<MeshFilter>();
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh; mc.convex = true;
            }
            var yol = $"{PrefabKlasor}/SM_{tur}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, yol);
            Object.DestroyImmediate(go);
            onbellek[tur] = prefab;
            return prefab;
        }

        static int DogayiDiz(Transform kok)
        {
            var doga = AssetDatabase.LoadAssetAtPath<GameObject>(DogaFbx);
            var yerler = AssetDatabase.LoadAssetAtPath<GameObject>(YerlerFbx);
            if (doga == null || yerler == null) { Debug.LogError("[Harita] Doğa FBX'leri eksik."); return 0; }
            var dogaKok = new GameObject("Doga").transform;
            dogaKok.SetParent(kok, false);
            var gruplar = new Dictionary<string, Transform>();
            var onbellek = new Dictionary<string, GameObject>();
            int n = 0;
            foreach (Transform y in yerler.transform)
            {
                if (!y.name.StartsWith("Yer_")) continue;
                var govde = y.name.Substring(4);
                int son = govde.LastIndexOf('_');
                if (son <= 0) continue;
                var tur = govde.Substring(0, son);
                var prefab = TurPrefab(doga, tur, onbellek);
                if (prefab == null) continue;
                if (!gruplar.TryGetValue(tur, out var grup))
                {
                    grup = new GameObject(tur).transform;
                    grup.SetParent(dogaKok, false);
                    gruplar[tur] = grup;
                }
                var ins = (GameObject)PrefabUtility.InstantiatePrefab(prefab, grup);
                // Yükseklik Blender'dan AYNEN gelir (harita_doga.yer_z: arazi AĞININ kapladığı alandaki en alçak noktası,
                // türe göre hafif gömülü). Unity'de ayrıca yere oturtma yapılmaz: eski oturtma kayaları 0,5–1,6 m fazla
                // gömüyor, köprü altındaki sazı köprünün üstüne taşıyordu (Blender–Unity karşılaştırması 13.11).
                var p = y.localPosition + HaritaOfset;
                ins.transform.SetPositionAndRotation(p, y.localRotation);
                ins.transform.localScale = y.localScale;
                n++;
            }
            return n;
        }

        // ------------------------------------------------------------------ araçlar (traktör, aletler, bisiklet)
        static float Zemin(Vector3 p)
        {
            // yalnız yürünen yüzeyler (arazi, döşeme, köprü): ağaç tacı ya da çatı zemin sayılmaz
            float y = 0f; bool bulundu = false;
            foreach (var h in Physics.RaycastAll(p + Vector3.up * 60f, Vector3.down, 200f))
            {
                var ad = h.collider.name;
                if (ad == "SM_Arazi" || ad == "SM_Meydan_Doseme" || ad.Contains("Kopru"))
                    if (!bulundu || h.point.y > y) { y = h.point.y; bulundu = true; }
            }
            return y;
        }

        static Vector3 ZemindeB(float bx, float by) { var p = B2U(bx, by); p.y = Zemin(p); return p; }

        static void AraclariKoy(Transform kok, GameObject harita)
        {
            var araclar = new GameObject("Araclar").transform;
            araclar.SetParent(kok, false);
            // Traktör: Blender'daki "Konum_Traktor" işareti (44.6, 14.8, yön 90° doğu) → konum birebir; yön Unity Y −90°.
            var tr = Ornek(TraktorFbx, araclar, "Traktor");
            if (tr != null)
            {
                var k = Konum(harita, "Konum_Traktor");
                var p = k != null ? k.position : B2U(44.6f, 14.8f);
                if (k == null) p.y = Zemin(p);
                tr.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, -90f, 0f));
                KutuCollider(tr);
            }
            // Aletler: ahır önü sırası, Blender (40 + i·3.4, 10.5)
            var aletModel = AssetDatabase.LoadAssetAtPath<GameObject>(AletFbx);
            if (aletModel != null)
            {
                var aletler = Ornek(AletFbx, araclar, "Traktor_Aletler");
                int i = 0;
                foreach (var ad in new[] { "Alet_Pulluk", "Alet_Ekici", "Alet_Su_Tanki", "Alet_Serpici", "Alet_Bicer", "Alet_Kizak" })
                {
                    var a = aletler.transform.Find(ad);
                    if (a == null) { i++; continue; }
                    var k = Konum(harita, "Konum_" + ad);              // Blender'daki konum birebir
                    if (k != null) a.position = k.position;
                    else
                    {
                        var p = B2U(40f + i * 3.4f, 10.5f);
                        p.y = Zemin(p);
                        a.position = p;
                        var r = a.GetComponent<Renderer>();
                        if (r != null) a.position += Vector3.up * (p.y - r.bounds.min.y);
                    }
                    KutuCollider(a.gameObject);
                    i++;
                }
            }
            // Bisiklet: atölye kapısının doğusundaki park yerinin yanı, Blender (3.6, 5.6), doğuya bakar
            var bs = Ornek(BisikletFbx, araclar, "Bisiklet");
            if (bs != null)
            {
                var p = B2U(3.6f, 5.6f);
                p.y = Zemin(p);
                bs.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, -90f, 0f));
                KutuCollider(bs);
            }
        }

        static void KutuCollider(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0 || go.GetComponent<Collider>() != null) return;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            var bc = go.AddComponent<BoxCollider>();
            bc.center = go.transform.InverseTransformPoint(b.center);
            var s = go.transform.InverseTransformVector(b.size);
            bc.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        }
    }
}
