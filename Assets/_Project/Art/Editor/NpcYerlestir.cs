// Bilgi Adası — NPC'leri (Art/Prefablar/NPC_*.prefab, npc.py ile üretilir) açık sahneye yerleştirir ve
// davranışlarını kurar: rota (yollar, pazar), duraklar (iş/bekleme), konuşma profili, çarpışma.
// Menü: BilgiAdasi → Sanat → NPC'leri açık sahneye yerleştir
// Önce harita ve atölye yerleştirilmiş olmalı (zemin ve engeller raycast ile okunur).
//
// "Takılmasınlar" güvencesi: rota çalışma anında fizikle değil, burada fırınlanan sık noktalı (0,5 m) yoldan
// birebir izlenir. Her nokta ve her ara parça kapsül taramasıyla (r 0,3 m, 0,4–1,6 m yükseklik) ve basamak/eğim
// kontrolüyle doğrulanır. Doğrulamadan geçmeyen rota KURULMAZ: NPC ilk durağında yerinde kalır ve hata yazılır.
//
// Koordinat: harita noktaları Blender (x, y) → Unity (−x, 0, −y) + (0, 0, 12) (HaritaYerlestir ile aynı);
// atölye içi noktalar doğrudan Unity koordinatıdır (Atolye_Sanat orijinde).
// Konuşma metinleri Art/NPC/Profil/<Ad>.asset (NpcProfile); yoksa ilk kurulumda oluşturulur, varsa dokunulmaz.
// Bu dosya sanat aracıdır (Mustafa). Çalışma anı davranışı: Art/NPC/Davranis/NpcAgent.cs.

using System.Collections.Generic;
using System.Text;
using BilgiAdasi.City;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BilgiAdasi.SanatAraclari
{
    public static class NpcYerlestir
    {
        const string PrefabKlasor = "Assets/_Project/Art/Prefablar/";
        const string ProfilKlasor = "Assets/_Project/Art/NPC/Profil";
        public const string KokAdi = "NPC_Sanat";
        const int InteractableLayer = 6;                 // Scripts/Editor/ProjectSetup.InteractableLayer
        static readonly Vector3 HaritaOfset = new Vector3(0f, 0f, 12f);

        const float Aralik = 0.5f;        // fırınlanan nokta aralığı (m)
        const float Yaricap = 0.3f;       // gövde kapsülü
        const float AltPay = 0.4f;        // kapsülün alt ucu zeminin bu kadar üstünde (≤ 0,3 m eşikler geçilir)
        const float UstUc = 1.6f;
        const float MaxBasamak = 0.25f;   // iki nokta arası en büyük yükseklik farkı (0,5 m'de)
        const float MaxEgim = 30f;

        static Vector3 B2U(float bx, float by) => new Vector3(-bx, 0f, -by) + HaritaOfset;

        // ------------------------------------------------------------------ tanımlar
        struct Durak
        {
            public Vector3 P;
            public bool Bekle;
            public Vector2 Sure;
            public NpcActivity Is;
            public bool Bak;
            public Vector3 BakNokta;
        }

        static Durak Gec(Vector3 p) => new Durak { P = p };
        static Durak Calis(Vector3 p, float min, float max, Vector3 bak) =>
            new Durak { P = p, Bekle = true, Sure = new Vector2(min, max), Is = NpcActivity.Work, Bak = true, BakNokta = bak };
        static Durak Dur(Vector3 p, float min, float max, Vector3 bak) =>
            new Durak { P = p, Bekle = true, Sure = new Vector2(min, max), Is = NpcActivity.Idle, Bak = true, BakNokta = bak };

        class Kisi
        {
            public string Prefab, Onek, Ad, Not;
            public float Hiz;
            public bool IcMekan;          // atölye içi: zemin ışını 1,5 m'den başlar
            public NpcPathMode Mod;
            public Durak[] Duraklar;
            public NpcProfile.Conversation[] Sohbet;
            public string Gorunen;
        }

        static NpcProfile.Conversation S(params string[] satir) => new NpcProfile.Conversation(satir);

        // Konumlar: harita.py ROADS (Yol_Dogu, Yol_Bati, Yol_Atolye), MEYDAN (0,-8) R 11;
        // harita_detay.py TEZGAHLAR (46.4,-2.3) manav, (54.6,-2.2) ekmek, (46,-10.4) baharat, (-39.4,-6.6) kağıt;
        // harita_yapi.py CESME (6.6,-2.8), CINAR (-5.5,-4.2), PANO (-43.5,-5.6). Atölye içi: Karatahta (-3.9, -0.4),
        // tartım tezgâhı (-3.6, 1.6) (Unity). Rotalar yol ortasından ~1 m kenarda: oyuncunun yolunu kesmez.
        static Kisi[] Kisiler() => new[]
        {
            new Kisi
            {
                Prefab = "NPC_Cabir", Onek = "Cabir", Ad = "NPC_Cabir", Gorunen = "Câbir Usta",
                Not = "atölye: tahtaya yazar ↔ tartım tezgâhında çalışır", Hiz = 0.75f, IcMekan = true, Mod = NpcPathMode.PingPong,
                Duraklar = new[]
                {
                    // Tahtanın yan ucunda yazar: tahta istasyonunun kamerası (-2.25, 1.7, -0.35) ile tahta arasına girmez.
                    Calis(new Vector3(-3.3f, 0f, 1.05f), 9f, 14f, new Vector3(-3.95f, 0f, 0.5f)),
                    Gec(new Vector3(-3.05f, 0f, 1.2f)),
                    Calis(new Vector3(-2.8f, 0f, 1.3f), 6f, 10f, new Vector3(-3.6f, 0f, 1.3f)),   // asma kat direğinin önü
                },
                Sohbet = new[]
                {
                    S("Çırağım! Kazanın başında seni bekleyen işler var.",
                      "Unutma: tepkimede atomlar ne kaybolur ne de yoktan var olur."),
                    S("Tahtaya yazdıklarıma bir göz at istersen.", "Denkleştirmenin sırrı sabırda... bir de saymakta."),
                    S("Takıldığın yerde defterime bak. Ayıp değil; usta da bir zamanlar çıraktı."),
                },
            },
            new Kisi
            {
                Prefab = "NPC_Ulak", Onek = "Ulak", Ad = "NPC_Ulak", Gorunen = "Ulak Tayfun",
                Not = "atölye kapısının önü, çıkan oyuncuyu karşılar (yürümez)", Hiz = 1.2f, Mod = NpcPathMode.Static,
                Duraklar = new[] { Dur(B2U(0.6f, 3.6f), 9999f, 9999f, B2U(-1.4f, 6.4f)) },
                Sohbet = new[]
                {
                    S("Seni arıyordum! Köyde herkes ustanın yeni çırağını konuşuyor.",
                      "Bir işin olursa atölyenin kapısına bak; siparişler oraya gelir."),
                    S("Bu heybede neler var bir bilsen... Ama başkasının mektubunu okumak olmaz!"),
                },
            },
            new Kisi
            {
                Prefab = "NPC_HesapHocasi", Onek = "HesapHocasi", Ad = "NPC_HesapHocasi", Gorunen = "Hesap Hocası",
                Not = "meydanda yavaş tur: çınarı ölçer, tabelaya bakar", Hiz = 0.8f, Mod = NpcPathMode.Loop,
                Duraklar = new[]
                {
                    Calis(B2U(-2.6f, -4.4f), 5f, 8f, B2U(-5.5f, -4.2f)),
                    Gec(B2U(-6.5f, -9.0f)),
                    Dur(B2U(-3.5f, -13.5f), 3f, 5f, B2U(0f, -8f)),
                    Gec(B2U(2.0f, -14.0f)),
                    Calis(B2U(3.0f, -10.5f), 4f, 6f, B2U(0f, -8f)),   // buradan çınara düz döner (tabela direğinden uzak)
                },
                Sohbet = new[]
                {
                    S("Hmm... Bu çınarın gölgesi tam yedi adım. Dün altı buçuktu!",
                      "Matematik şehrinin kapısı hâlâ kapalı. Ama sayılar bekler, acele etmez."),
                    S("Bir tepkimenin iki yanındaki atomları say: eşit mi? İşte o kadar basit... ve o kadar zor."),
                    S("Usturlabımla yıldızları ölçerim, pergelimle dünyayı. Senin ölçün ne, genç?"),
                },
            },
            new Kisi
            {
                Prefab = "NPC_KopukEfendi", Onek = "KopukEfendi", Ad = "NPC_KopukEfendi", Gorunen = "Köpük Efendi",
                Not = "meydan: çeşmede suyu dener ↔ doğu yoluna bakar", Hiz = 0.85f, Mod = NpcPathMode.PingPong,
                Duraklar = new[]
                {
                    Calis(B2U(4.6f, -1.2f), 6f, 10f, B2U(6.6f, -2.8f)),
                    Gec(B2U(4.0f, -4.0f)),                         // çeşme havuzunun batısından dolaşır
                    Dur(B2U(7.5f, -6.5f), 2f, 4f, B2U(0f, -8f)),
                    Dur(B2U(8.0f, -10.0f), 3f, 5f, B2U(16f, -9.5f)),
                },
                Sohbet = new[]
                {
                    S("Dikkat! Az önce yine bir şey köpürdü. Kaşlarım hâlâ yerinde mi?",
                      "Altın kural: önce oranı bul, sonra kazana dök."),
                    S("Çeşme suyu mu, damıtılmış su mu? Sakın tadına bakarak anlamaya çalışma!"),
                    S("Câbir Usta'nın çırağı sen misin? Benden biraz uzak dur, bugün şanssız günümdeyim!"),
                },
            },
            new Kisi
            {
                Prefab = "NPC_KoyluErkek", Onek = "KoyluErkek", Ad = "NPC_KoyluErkek", Gorunen = "Bostancı Rıza",
                Not = "Denkleştirme pazarı: manav → fırın → baharatçı halkası", Hiz = 1.05f, Mod = NpcPathMode.Loop,
                Duraklar = new[]
                {
                    Dur(B2U(43.8f, -5.6f), 2f, 3f, B2U(31.6f, -8f)),
                    Calis(B2U(46.4f, -4.3f), 5f, 8f, B2U(46.4f, -2.3f)),
                    Gec(B2U(50.5f, -5.4f)),
                    Calis(B2U(54.6f, -4.2f), 4f, 7f, B2U(54.6f, -2.2f)),
                    Dur(B2U(57.6f, -5.2f), 2f, 3f, B2U(60f, -5f)),
                    Gec(B2U(55.0f, -7.6f)),
                    Calis(B2U(46.0f, -8.6f), 4f, 7f, B2U(46.0f, -10.4f)),
                    Gec(B2U(43.5f, -7.6f)),
                },
                Sohbet = new[]
                {
                    S("Pazarda bugün domatesler pek taze.", "Atölyedeki usta herkesin derdine derman olur derler."),
                    S("Köprü yıkılalı beri dereyi tahta köprüden geçiyoruz. Bir onarılsa..."),
                },
            },
            new Kisi
            {
                Prefab = "NPC_KoyluKadin", Onek = "KoyluKadin", Ad = "NPC_KoyluKadin", Gorunen = "Hatice Teyze",
                Not = "Tepkime Türleri: kâğıtçı → ilan panosu → köy ucu halkası", Hiz = 1.0f, Mod = NpcPathMode.Loop,
                Duraklar = new[]
                {
                    Gec(B2U(-37.6f, -4.4f)),
                    Calis(B2U(-39.4f, -4.8f), 5f, 8f, B2U(-39.4f, -6.6f)),
                    Dur(B2U(-43.5f, -4.0f), 4f, 6f, B2U(-43.5f, -5.6f)),
                    Gec(B2U(-48.5f, -2.4f)),
                    Dur(B2U(-52.5f, -0.6f), 2f, 3f, B2U(-46f, -2f)),
                    Gec(B2U(-47.5f, -0.4f)),
                    Gec(B2U(-41.0f, -1.6f)),
                },
                Sohbet = new[]
                {
                    S("İlan panosuna yeni duyurular asmışlar, okudun mu?"),
                    S("Kâğıtçının tezgâhında yeni defterler var.", "Sen de çırak mısın? Kolay gelsin evladım."),
                },
            },
        };

        // ------------------------------------------------------------------ menü
        [MenuItem("BilgiAdasi/Sanat/NPC'leri açık sahneye yerleştir")]
        public static void Yerlestir()
        {
            // eski yerleşimlerin hepsi silinir (aynı adlı birden çok kök kalmış olabilir; pasif olanlar dahil)
            foreach (var eski in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                if (eski.name == KokAdi) Undo.DestroyObjectImmediate(eski);
            Physics.SyncTransforms();

            // Rotalar NPC'ler sahnede yokken doğrulanır (kendi kapsülleri engel sayılmasın).
            var kisiler = Kisiler();
            var rotalar = new (Vector3[] yol, NpcStop[] duraklar, NpcPathMode mod, string hata)[kisiler.Length];
            for (int i = 0; i < kisiler.Length; i++) rotalar[i] = Firinla(kisiler[i]);

            var kok = new GameObject(KokAdi);
            Undo.RegisterCreatedObjectUndo(kok, "NPC'leri yerleştir");
            var yonetmen = Object.FindAnyObjectByType<DialogueDirector>();
            if (yonetmen == null) yonetmen = kok.AddComponent<DialogueDirector>();

            var rapor = new StringBuilder();
            int n = 0;
            for (int i = 0; i < kisiler.Length; i++)
            {
                var k = kisiler[i];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabKlasor + k.Prefab + ".prefab");
                if (prefab == null) { Debug.LogError("[NPC] Prefab yok: " + k.Prefab); continue; }
                var (yol, duraklar, mod, hata) = rotalar[i];

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, kok.transform);
                go.name = k.Ad;
                go.layer = InteractableLayer;
                var ilk = duraklar.Length > 0 && duraklar[0].FaceTarget ? duraklar[0].FacePoint
                        : (yol.Length > 1 ? yol[1] : yol[0] + Vector3.forward);
                var yon = ilk - yol[0]; yon.y = 0f;
                go.transform.SetPositionAndRotation(yol[0], yon.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(yon) : Quaternion.identity);

                var cc = go.AddComponent<CapsuleCollider>();
                cc.center = new Vector3(0f, 0.85f, 0f);
                cc.height = 1.7f;
                cc.radius = Yaricap;
                var rb = go.AddComponent<Rigidbody>();          // hareketli collider: kinematik gövde
                rb.isKinematic = true;
                rb.useGravity = false;

                var ajan = go.AddComponent<NpcAgent>();
                ajan.EditorSetup(Profil(k), yonetmen, go.GetComponentInChildren<Animator>(), k.Onek, k.Hiz, mod, yol, duraklar);

                rapor.AppendLine($"[NPC] {k.Ad}: {mod}, {yol.Length} nokta, {duraklar.Length} durak — {k.Not}" +
                                 (hata != null ? "  !! ROTA REDDEDİLDİ, yerinde kalır: " + hata : ""));
                if (hata != null) Debug.LogError($"[NPC] {k.Ad} rotası doğrulanmadı → yürümeyecek: {hata}");
                n++;
            }
            Debug.Log(rapor.ToString() + $"[NPC] Yerleştirildi: {KokAdi} ({n} karakter).");
            Selection.activeGameObject = kok;
            EditorSceneManager.MarkSceneDirty(kok.scene);
        }

        // ------------------------------------------------------------------ rota fırınlama + doğrulama
        static (Vector3[] yol, NpcStop[] duraklar, NpcPathMode mod, string hata) Firinla(Kisi k)
        {
            var d = k.Duraklar;
            var yol = new List<Vector3>();
            var anahtar = new int[d.Length];
            bool halka = k.Mod == NpcPathMode.Loop;
            int parca = k.Mod == NpcPathMode.Static ? 0 : (halka ? d.Length : d.Length - 1);

            for (int i = 0; i < d.Length; i++)
            {
                anahtar[i] = yol.Count;
                yol.Add(d[i].P);
                if (i >= parca) continue;
                var a = d[i].P;
                var b = d[(i + 1) % d.Length].P;
                float len = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                int adim = Mathf.Max(1, Mathf.CeilToInt(len / Aralik));
                for (int j = 1; j < adim; j++) yol.Add(Vector3.Lerp(a, b, j / (float)adim));
            }
            if (k.Mod == NpcPathMode.Static) { yol.Clear(); yol.Add(d[0].P); anahtar[0] = 0; }

            string hata = null;
            for (int i = 0; i < yol.Count; i++)
            {
                var p = yol[i];
                float z = Zemin(p, out bool zeminVar, k.IcMekan ? 1.5f : 60f);
                if (!zeminVar) { hata ??= $"zemin yok @ {p:F1}"; z = 0f; }
                yol[i] = new Vector3(p.x, z, p.z);
            }

            var duraklar = new List<NpcStop>();
            for (int i = 0; i < d.Length; i++)
            {
                if (!d[i].Bekle) continue;
                var bak = d[i].BakNokta;
                bak.y = yol[anahtar[i]].y;
                duraklar.Add(new NpcStop(anahtar[i], d[i].Sure, d[i].Is, d[i].Bak, bak));
            }

            // doğrulama: her nokta boş, her parça geçilebilir
            for (int i = 0; i < yol.Count && hata == null; i++)
            {
                if (Dolu(yol[i])) hata = $"nokta {i} engelin içinde @ {yol[i]:F1}";
            }
            int son = halka ? yol.Count : yol.Count - 1;
            for (int i = 0; i < son && hata == null && k.Mod != NpcPathMode.Static; i++)
            {
                var a = yol[i];
                var b = yol[(i + 1) % yol.Count];
                float dy = Mathf.Abs(b.y - a.y);
                float yatay = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                if (dy > MaxBasamak) hata = $"basamak {dy:F2} m @ {a:F1}";
                else if (yatay > 0.01f && Mathf.Atan2(dy, yatay) * Mathf.Rad2Deg > MaxEgim) hata = $"eğim @ {a:F1}";
                else if (Kesiyor(a, b)) hata = $"parça {i}→{(i + 1) % yol.Count} engele çarpıyor @ {a:F1}";
            }

            if (hata != null)
            {
                // güvenli geri dönüş: yürümez, ilk durağında (ya da ilk noktada) bekler
                var ilkDurak = duraklar.Count > 0 ? duraklar[0] : new NpcStop(0, new Vector2(9999f, 9999f), NpcActivity.Idle, false, Vector3.zero);
                var p0 = yol[ilkDurak.Node];
                return (new[] { p0 }, new[] { new NpcStop(0, ilkDurak.WaitSeconds, ilkDurak.Activity, ilkDurak.FaceTarget, ilkDurak.FacePoint) },
                        NpcPathMode.Static, hata);
            }
            return (yol.ToArray(), duraklar.ToArray(), k.Mod, null);
        }

        static bool Dolu(Vector3 taban) =>
            Physics.CheckCapsule(taban + Vector3.up * (AltPay + Yaricap), taban + Vector3.up * (UstUc - Yaricap), Yaricap,
                                 ~0, QueryTriggerInteraction.Ignore);

        static bool Kesiyor(Vector3 a, Vector3 b)
        {
            var v = b - a;
            float len = v.magnitude;
            if (len < 0.001f) return false;
            var p1 = a + Vector3.up * (AltPay + Yaricap);
            var p2 = a + Vector3.up * (UstUc - Yaricap);
            return Physics.CapsuleCast(p1, p2, Yaricap * 0.95f, v / len, len, ~0, QueryTriggerInteraction.Ignore);
        }

        /// <param name="basla">Işının başladığı yükseklik. İç mekânda 1,5 m: tavan, kiriş, asma kat, asılı otlar görülmez.</param>
        static float Zemin(Vector3 xz, out bool bulundu, float basla = 60f)
        {
            // mesh dikişinde ışın kaçabilir: merkezde vuruş yoksa ±5 cm komşulara bakılır
            float y = ZeminTek(xz, out bulundu, basla);
            for (int i = 0; i < 4 && !bulundu; i++)
            {
                var o = new Vector3(i == 0 ? 0.05f : i == 1 ? -0.05f : 0f, 0f, i == 2 ? 0.05f : i == 3 ? -0.05f : 0f);
                y = ZeminTek(xz + o, out bulundu, basla);
            }
            return y;
        }

        static float ZeminTek(Vector3 xz, out bool bulundu, float basla)
        {
            // tezgâh, çatı, saçak değil: yukarı bakan en alçak yüzey (arazi) ve onun ≤ 0,35 m üstündeki
            // en yüksek yüzey (meydan döşemesi, eşik, atölye tabanı — adım yüksekliği 0,3 m)
            // (normal eşiği 0,3: taban üçgen kenarında eğik vurulsa da sayılır; asılı otlar/tavan en alçak olamaz)
            var hits = Physics.RaycastAll(new Vector3(xz.x, basla, xz.z), Vector3.down, basla + 60f, ~0, QueryTriggerInteraction.Ignore);
            float alt = float.PositiveInfinity;
            foreach (var h in hits)
                if (h.normal.y > 0.3f && h.point.y < alt) alt = h.point.y;
            float best = alt;
            foreach (var h in hits)
                if (h.normal.y > 0.3f && h.point.y > best && h.point.y <= alt + 0.35f) best = h.point.y;
            bulundu = !float.IsPositiveInfinity(best);
            return bulundu ? best : 0f;
        }

        // ------------------------------------------------------------------ konuşma profili
        static NpcProfile Profil(Kisi k)
        {
            var yol = ProfilKlasor + "/" + k.Onek + ".asset";
            var p = AssetDatabase.LoadAssetAtPath<NpcProfile>(yol);
            if (p != null) return p;                         // var olan metinlere dokunulmaz
            if (!AssetDatabase.IsValidFolder(ProfilKlasor)) AssetDatabase.CreateFolder("Assets/_Project/Art/NPC", "Profil");
            p = ScriptableObject.CreateInstance<NpcProfile>();
            p.Initialize(k.Gorunen, "{0} ile konuş", k.Sohbet);
            AssetDatabase.CreateAsset(p, yol);
            AssetDatabase.SaveAssets();
            return p;
        }
    }
}
