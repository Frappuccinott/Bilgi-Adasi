// Bilgi Adası — NPC prefablarını (Art/Prefablar/NPC_*.prefab, npc.py ile üretilir) açık sahneye yerleştirir.
// Menü: BilgiAdasi → Sanat → NPC'leri açık sahneye yerleştir
// Önce harita ve atölye yerleştirilmiş olmalı (zemin ve engeller raycast ile okunur).
// Koordinat: harita noktaları Blender (x, y) → Unity (−x, 0, −y) + (0, 0, 12) (HaritaYerlestir ile aynı);
// atölye içi noktalar doğrudan Unity koordinatıdır (Atolye_Sanat orijinde).
// Yalnızca görsel + çarpışma kapsülü eklenir; etkileşim/diyalog bileşenleri oyun kodudur (Mehmet), burada eklenmez.
// Bu dosya sanat aracıdır (Mustafa); oyun kodu değildir.

using UnityEditor;
using UnityEngine;

namespace BilgiAdasi.SanatAraclari
{
    public static class NpcYerlestir
    {
        const string PrefabKlasor = "Assets/_Project/Art/Prefablar/";
        public const string KokAdi = "NPC_Sanat";
        static readonly Vector3 HaritaOfset = new Vector3(0f, 0f, 12f);

        static Vector3 B2U(float bx, float by) => new Vector3(-bx, 0f, -by) + HaritaOfset;

        struct Yer
        {
            public string Prefab, Ad, Not;
            public Vector3 Konum, Bakis;   // Unity XZ (y yok sayılır)
        }

        // Konum ve bakış hedefi. Köy merkezleri harita.py: DENK (52,-6), TEPK (-46,-2), MEYDAN (0,-8);
        // harita_yapi.py: CESME (6.6,-2.8), CINAR (-5.5,-4.2), PANO (-43.5,-5.6).
        static Yer[] Yerler() => new[]
        {
            // Tahtanın önünde: kazan istasyonu kamerasında terazileri kapatmaz (13.11 kadraj testi).
            new Yer { Prefab = "NPC_Cabir", Ad = "NPC_Cabir", Not = "atölye içi, tahtanın önü, kapıya bakar",
                      Konum = new Vector3(-3.2f, 0f, 0.4f), Bakis = new Vector3(1.4f, 0f, 3.2f) },
            new Yer { Prefab = "NPC_Ulak", Ad = "NPC_Ulak", Not = "atölye kapısının önü, çıkan oyuncuyu karşılar",
                      Konum = B2U(0.6f, 3.6f), Bakis = B2U(-1.4f, 6.4f) },
            // meydandakiler atölyeden inen yola bakar: oyuncu meydana girerken yüzlerini görür
            new Yer { Prefab = "NPC_HesapHocasi", Ad = "NPC_HesapHocasi", Not = "meydan, çınarın yanı",
                      Konum = B2U(-3.6f, -3.2f), Bakis = B2U(-1.2f, 2.0f) },
            new Yer { Prefab = "NPC_KopukEfendi", Ad = "NPC_KopukEfendi", Not = "meydan, çeşmenin yanı",
                      Konum = B2U(4.6f, -1.2f), Bakis = B2U(-1.2f, 2.0f) },
            // köylüler yol kenarında (yolu kapatmaz), köprüden / meydandan gelen oyuncuya bakar
            new Yer { Prefab = "NPC_KoyluErkek", Ad = "NPC_KoyluErkek", Not = "Denkleştirme köyü girişi, pazar tezgâhı yanı",
                      Konum = B2U(47.5f, -3.3f), Bakis = B2U(31.6f, -8f) },
            new Yer { Prefab = "NPC_KoyluKadin", Ad = "NPC_KoyluKadin", Not = "Tepkime Türleri köyü, ilan panosu yanı",
                      Konum = B2U(-42.5f, -5.2f), Bakis = B2U(0f, -8f) },
        };

        [MenuItem("BilgiAdasi/Sanat/NPC'leri açık sahneye yerleştir")]
        public static void Yerlestir()
        {
            var eski = GameObject.Find(KokAdi);
            if (eski != null) Undo.DestroyObjectImmediate(eski);
            Physics.SyncTransforms();
            var kok = new GameObject(KokAdi);
            Undo.RegisterCreatedObjectUndo(kok, "NPC'leri yerleştir");
            int n = 0;
            foreach (var y in Yerler())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabKlasor + y.Prefab + ".prefab");
                if (prefab == null) { Debug.LogError("[NPC] Prefab yok: " + y.Prefab); continue; }
                var p = BosYer(y.Konum);
                var yon = y.Bakis - p; yon.y = 0f;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, kok.transform);
                go.name = y.Ad;
                go.transform.SetPositionAndRotation(p, yon.sqrMagnitude > 0.01f ? Quaternion.LookRotation(yon) : Quaternion.identity);
                // oyuncu içinden geçmesin: gövde kapsülü (şapka hariç ~1,7 m)
                var cc = go.AddComponent<CapsuleCollider>();
                cc.center = new Vector3(0f, 0.85f, 0f);
                cc.height = 1.7f;
                cc.radius = 0.3f;
                Debug.Log($"[NPC] {y.Ad} → {p:F2} ({y.Not})");
                n++;
            }
            Selection.activeGameObject = kok;
            Debug.Log($"[NPC] Yerleştirildi: {KokAdi} ({n} karakter).");
        }

        /// <summary>Zemine oturtur; nokta doluysa 2 m içinde en yakın boş noktayı arar (kapsül testi).</summary>
        static Vector3 BosYer(Vector3 xz)
        {
            for (int halka = 0; halka <= 8; halka++)
            {
                int adim = halka == 0 ? 1 : 8 * halka;
                for (int i = 0; i < adim; i++)
                {
                    float a = 2f * Mathf.PI * i / adim;
                    var q = xz + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (0.25f * halka);
                    var z = Zemin(q, out bool zeminVar);
                    if (!zeminVar) continue;
                    var taban = new Vector3(q.x, z, q.z);
                    if (!Physics.CheckCapsule(taban + Vector3.up * 0.45f, taban + Vector3.up * 1.6f, 0.35f,
                                              ~0, QueryTriggerInteraction.Ignore))
                        return taban;
                }
            }
            Debug.LogWarning("[NPC] Boş yer bulunamadı, istenen noktaya konuyor: " + xz);
            return new Vector3(xz.x, Zemin(xz, out _), xz.z);
        }

        static float Zemin(Vector3 xz, out bool bulundu)
        {
            // tezgâh, çatı, saçak değil: yukarı bakan en alçak yüzey (arazi) ve onun ≤ 0,35 m üstündeki
            // en yüksek yüzey (meydan döşemesi, eşik, atölye tabanı — adım yüksekliği 0,3 m)
            var hits = Physics.RaycastAll(new Vector3(xz.x, 60f, xz.z), Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore);
            float alt = float.PositiveInfinity;
            foreach (var h in hits)
                if (h.normal.y > 0.7f && h.point.y < alt) alt = h.point.y;
            float best = alt;
            foreach (var h in hits)
                if (h.normal.y > 0.7f && h.point.y > best && h.point.y <= alt + 0.35f) best = h.point.y;
            bulundu = !float.IsPositiveInfinity(best);
            return bulundu ? best : 0f;
        }
    }
}
