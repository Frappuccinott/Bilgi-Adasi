// Bilgi Adası — Blender'dan gelen animasyonlu FBX'leri Unity'de elle ayar gerektirmeden kurar.
// Kapsam: Assets/_Project/Art/Animasyon/*.fbx ve Assets/_Project/Art/Esya/*.fbx
//   1) Rig = Generic, kamera/ışık içe aktarılmaz.
//   2) Klip adları "Rig|Klip" önekinden temizlenir; adı "_Dongu" ile biten klipler döngüye alınır (Loop Time).
//   3) Her FBX için Animator Controller kurulur (Kontrol/<Ad>.controller); "_Bosta" klibi varsayılan durumdur.
//   4) Kontrolcüsü bağlı prefab üretilir (Prefablar/<Ad>.prefab). Oyun kodu yalnızca Animator.Play("Klip_Adi") çağırır.
// Blender tarafı: ArtSource/Blender/Script/anim_ortak.py, anim_atolye.py. Kurallar: 09 §7, 08 §7-8.
// Bu dosya sanat içe aktarma aracıdır (Mustafa); oyun kodu değildir.

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace BilgiAdasi.SanatAraclari
{
    public sealed class SanatIceAktarma : AssetPostprocessor
    {
        const string Kok = "Assets/_Project/Art/";
        const string ShaderYolu = Kok + "Shaders/Stilize.shader";
        static readonly string[] Klasorler = { Kok + "Animasyon/", Kok + "Esya/" };

        static bool Fbx(string yol) => yol.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);
        static bool SanatFbx(string yol) => Fbx(yol) && yol.StartsWith(Kok);
        static bool Kapsamda(string yol) => Fbx(yol) && Klasorler.Any(k => yol.StartsWith(k));

        void OnPreprocessModel()
        {
            if (!SanatFbx(assetPath)) return;
            var mi = (ModelImporter)assetImporter;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importNormals = ModelImporterNormals.Import;     // Blender'daki yumuşak/keskin normaller korunur (09 §7)
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            context.DependsOnSourceAsset(ShaderYolu);          // shader değişince modeller yeniden içe aktarılır
            if (!Kapsamda(assetPath))
            {
                mi.animationType = ModelImporterAnimationType.None;   // hareketsiz atölye
                return;
            }
            mi.animationType = ModelImporterAnimationType.Generic;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.importAnimation = true;
        }

        /// <summary>FBX'teki malzemeyi adına göre BilgiAdasi/Stilize'ye bağlar (köşe rengi; beyaz görünmez).</summary>
        void OnPreprocessMaterialDescription(MaterialDescription aciklama, Material malzeme, AnimationClip[] animasyonlar)
        {
            if (!SanatFbx(assetPath)) return;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderYolu) ?? Shader.Find("BilgiAdasi/Stilize");
            if (shader == null)
            {
                Debug.LogWarning("BilgiAdasi/Stilize bulunamadı; menüden 'Animasyonlu FBX'leri yeniden kur' çalıştırın: " + assetPath);
                return;
            }
            malzeme.shader = shader;
            var ad = aciklama.materialName ?? malzeme.name;
            var i = ad.IndexOf('.');                            // Blender kopya eki (M_Su.001)
            if (i > 0) ad = ad.Substring(0, i);
            malzeme.SetColor("_Renk", Color.white);
            malzeme.SetFloat("_Cull", 0f);
            switch (ad)
            {
                case "M_Fener":   Ayarla(malzeme, puruz: 0.3f, isima: 3.0f, golge: false); break;
                case "M_Kopuk":   Ayarla(malzeme, puruz: 0.45f, isima: 0.35f, golge: false); break;
                case "M_Su":      Ayarla(malzeme, puruz: 0.75f, isima: 0.05f, golge: false); break;
                case "M_Duman":   Ayarla(malzeme, puruz: 0.0f, isima: 0.15f, golge: false, opaklik: 0.55f); break;
                case "M_Cam":     Ayarla(malzeme, puruz: 0.85f, isima: 0.0f, golge: false, opaklik: 0.35f); break;
                default:          Ayarla(malzeme, puruz: 0.12f, isima: 0.0f, golge: true); break;   // M_Stilize
            }
        }

        static void Ayarla(Material m, float puruz, float isima, bool golge, float opaklik = 1f)
        {
            m.SetFloat("_Puruzsuzluk", puruz);
            m.SetFloat("_Isima", isima);
            m.SetFloat("_Opaklik", opaklik);
            var saydam = opaklik < 1f;
            m.SetFloat("_Saydam", saydam ? 1f : 0f);
            if (saydam)
            {
                m.EnableKeyword("_SAYDAM");
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                m.SetShaderPassEnabled("DepthOnly", false);
                m.SetShaderPassEnabled("DepthNormals", false);
            }
            else
            {
                m.DisableKeyword("_SAYDAM");
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                m.SetFloat("_ZWrite", 1f);
                m.SetOverrideTag("RenderType", "Opaque");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            }
            m.SetShaderPassEnabled("ShadowCaster", golge && !saydam);
            m.enableInstancing = true;
        }

        void OnPreprocessAnimation()
        {
            if (!Kapsamda(assetPath)) return;
            var mi = (ModelImporter)assetImporter;
            var klipler = mi.defaultClipAnimations;
            foreach (var k in klipler)
            {
                var ad = k.name;
                var i = ad.LastIndexOf('|');
                if (i >= 0) ad = ad.Substring(i + 1);
                k.name = ad;
                k.loopTime = ad.EndsWith("_Dongu");
                k.loopPose = false;
            }
            mi.clipAnimations = klipler;
        }

        static void OnPostprocessAllAssets(string[] eklenen, string[] silinen, string[] tasinan, string[] eskiYollar)
        {
            var hedefler = eklenen.Where(Kapsamda).ToList();
            if (hedefler.Count == 0) return;
            EditorApplication.delayCall += () =>
            {
                foreach (var yol in hedefler) Kur(yol);
                AssetDatabase.SaveAssets();
            };
        }

        static void KlasorHazirla(string klasor)
        {
            if (AssetDatabase.IsValidFolder(klasor)) return;
            var ust = Path.GetDirectoryName(klasor).Replace('\\', '/');
            KlasorHazirla(ust);
            AssetDatabase.CreateFolder(ust, Path.GetFileName(klasor));
        }

        static void Kur(string fbxYolu)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxYolu);
            if (model == null) return;
            var klipler = AssetDatabase.LoadAllAssetsAtPath(fbxYolu).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToList();
            var ad = Path.GetFileNameWithoutExtension(fbxYolu);
            var klasor = Path.GetDirectoryName(fbxYolu).Replace('\\', '/');

            // Animator Controller: her klip bir durum; varsayılan "_Bosta"
            KlasorHazirla(klasor + "/Kontrol");
            var kontrolYolu = klasor + "/Kontrol/" + ad + ".controller";
            var kontrol = AssetDatabase.LoadAssetAtPath<AnimatorController>(kontrolYolu)
                          ?? AnimatorController.CreateAnimatorControllerAtPath(kontrolYolu);
            var sm = kontrol.layers[0].stateMachine;
            foreach (var cs in sm.states.ToArray()) sm.RemoveState(cs.state);
            AnimatorState varsayilan = null;
            foreach (var klip in klipler)
            {
                var durum = kontrol.AddMotion(klip);
                durum.name = klip.name;
                durum.writeDefaultValues = true;
                if (klip.name.EndsWith("_Bosta")) varsayilan = durum;
            }
            if (varsayilan == null)
            {
                // Boşta klibi yoksa hareketsiz durum: Write Defaults ile varsayılan poz (ör. kapı kapalı) korunur
                varsayilan = sm.AddState("Bosta");
                varsayilan.writeDefaultValues = true;
            }
            sm.defaultState = varsayilan;
            EditorUtility.SetDirty(kontrol);

            // Prefab: dönüşü sıfır bir sarmalayıcı kök + altında model (Animator modelde).
            // Blender FBX iskelet kökü 270° X dönük gelir; sarmalayıcı sayesinde kod prefabı
            // Quaternion.identity ile oluşturabilir. Klipler Animator'ın objesine göre yollandığı için değişmez.
            KlasorHazirla(Kok + "Prefablar");
            var sarmal = new GameObject(ad);
            var ornek = (GameObject)PrefabUtility.InstantiatePrefab(model, sarmal.transform);
            if (fbxYolu.StartsWith(Kok + "Esya/"))
                ornek.transform.localPosition = Vector3.zero;    // katalog konumu atılır: eşya kendi tabanında durur
            else
            {
                // Düzenekler dünya konumunda gelir. Sarmalayıcı kökü iskeletin dönme noktasına (kapıda menteşe,
                // kazanda taban) taşınır; model ters kaydırılır → dünya konumu değişmez, prefabın pivotu doğru yerde olur.
                // FBX kökü iskeletin kendisidir ve dönme noktasında durur (Blender'da armature konumu).
                var p = ornek.transform.position;
                sarmal.transform.position = p;
                ornek.transform.position = p;
            }
            if (ad == "Kapi_Rig")
            {
                // Kanat kapalıyken geçişi keser, animasyonla birlikte döner (collider kemiğin çocuğunda).
                var kanat = ornek.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m => m.name == "Kapi_Kanat");
                if (kanat != null && kanat.GetComponent<BoxCollider>() == null)
                {
                    var bc = kanat.gameObject.AddComponent<BoxCollider>();
                    bc.center = kanat.sharedMesh.bounds.center;
                    bc.size = kanat.sharedMesh.bounds.size;
                }
            }
            var animator = ornek.GetComponent<Animator>();
            if (animator == null) animator = ornek.AddComponent<Animator>();   // Unity "sahte null": ?? kullanılmaz
            animator.runtimeAnimatorController = kontrol;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            PrefabUtility.SaveAsPrefabAsset(sarmal, Kok + "Prefablar/" + ad + ".prefab");
            Object.DestroyImmediate(sarmal);
        }

        /// <summary>Açık sahneye atölyeyi kurar: hareketsiz atölye + animasyonlu düzenek prefabları, hepsi dünya
        /// konumlarında (Blender'daki konumlar korunur; kök (0,0,0)). Eski "Atolye_Sanat" kökü varsa önce silinir.
        /// Taşınabilir eşyalar (Esya) yerleştirilmez: onları oyun kodu raflara/yuva noktalarına koyar.</summary>
        [MenuItem("BilgiAdasi/Sanat/Atölyeyi açık sahneye yerleştir")]
        static void AtolyeyiYerlestir()
        {
            var eski = GameObject.Find("Atolye_Sanat");
            if (eski != null) Undo.DestroyObjectImmediate(eski);
            var kok = new GameObject("Atolye_Sanat");
            Undo.RegisterCreatedObjectUndo(kok, "Atölyeyi yerleştir");
            var statik = AssetDatabase.LoadAssetAtPath<GameObject>(Kok + "Atolye/Atolye_Statik.fbx");
            if (statik != null)
            {
                var s = (GameObject)PrefabUtility.InstantiatePrefab(statik, kok.transform);
                // Not: ContributeGI verilmez — Stilize shader ışık haritası okumaz; ortam ışığı SH/ışık probundan gelir.
                GameObjectUtility.SetStaticEditorFlags(s, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic |
                                                          StaticEditorFlags.OccludeeStatic);
                foreach (Transform t in s.GetComponentsInChildren<Transform>(true))
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, GameObjectUtility.GetStaticEditorFlags(s));
                // Duvar, zemin, eşik ve mobilya çarpışması (oyuncu içeride yürür, duvardan geçmez).
                // Işık/alev gibi kendi ışığı olan ince parçalar hariç.
                foreach (var mf in s.GetComponentsInChildren<MeshFilter>(true))
                {
                    var r = mf.GetComponent<Renderer>();
                    if (r == null || mf.GetComponent<Collider>() != null) continue;
                    var m = r.sharedMaterial;
                    if (m != null && (m.name.StartsWith("M_Fener") || m.name.StartsWith("M_Duman") || m.name.StartsWith("M_Kopuk"))) continue;
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                }
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Kok + "Prefablar" }))
            {
                var yol = AssetDatabase.GUIDToAssetPath(guid);
                var adi = Path.GetFileNameWithoutExtension(yol);
                if (!adi.EndsWith("_Rig") || adi.StartsWith("Malzeme_") || adi.StartsWith("Madde_") ||
                    adi.StartsWith("Kulce_") || adi.StartsWith("Alet_")) continue;
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(yol), kok.transform);
            }
            Selection.activeGameObject = kok;
            Debug.Log("Atölye yerleştirildi: Atolye_Sanat (" + kok.transform.childCount + " parça).");
        }

        [MenuItem("BilgiAdasi/Sanat/Animasyonlu FBX'leri yeniden kur")]
        static void HepsiniYenidenKur()
        {
            foreach (var k in Klasorler.Append(Kok + "Atolye/"))
            {
                if (!AssetDatabase.IsValidFolder(k.TrimEnd('/'))) continue;
                foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { k.TrimEnd('/') }))
                {
                    var yol = AssetDatabase.GUIDToAssetPath(guid);
                    AssetDatabase.ImportAsset(yol, ImportAssetOptions.ForceUpdate);
                }
            }
        }
    }
}
