using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Order;
using BilgiAdasi.UI;
using UnityEditor;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Alet pazarı verisi (10-gorev-pazar-npc §2.3, Demirci tezgâhı): aletler SpeciesData olarak (IsTool) üretilir,
    /// Data/Market/Pazar_Aletler.asset kataloğu kurulur, Alet_* simgeleri sprite yapılıp UiTheme'e eklenir.
    /// Aletler envantere ve rafa girer, kazana dökülmez. Tekrar çalıştırılabilir.
    /// </summary>
    public static class ToolMarketBuilder
    {
        private const string ToolFolder = "Assets/_Project/Data/Tools";
        private const string CatalogPath = "Assets/_Project/Data/Market/Pazar_Aletler.asset";
        private const string IconFolder = "Assets/_Project/UI/Ikonlar";
        private const string ThemePath = "Assets/_Project/Resources/UiTheme.asset";
        private const string EconomyPath = "Assets/_Project/Data/Economy.asset";

        // formül (kısa ad, kartta büyük yazı) · görünen ad · model/simge adı · fiyat (akçe)
        private static readonly (string formula, string name, string asset, int price)[] Tools =
        {
            ("Havan", "Tunç havan", "Alet_Havan", 4),
            ("Havaneli", "Havan eli", "Alet_Havaneli", 1),
            ("Elek", "İnce elek", "Alet_Elek", 1),
            ("Huni", "Bakır huni", "Alet_Huni", 1),
            ("Kepçe", "Uzun kepçe", "Alet_Kepce", 1),
            ("El terazisi", "Tartı", "Alet_El_Terazisi", 3),
            ("Kroze", "Eritme potası", "Alet_Kroze", 3),
            ("Küpel", "Arıtma kabı", "Alet_Kupel", 2),
        };

        [MenuItem("BilgiAdasi/Seviye/Alet pazarı verisini üret")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(ToolFolder)) AssetDatabase.CreateFolder("Assets/_Project/Data", "Tools");
            var tools = new List<SpeciesData>();
            var sprites = new List<Sprite>();

            foreach (var t in Tools)
            {
                var sprite = MakeSprite($"{IconFolder}/{t.asset}.png");
                if (sprite != null) sprites.Add(sprite);

                string path = $"{ToolFolder}/Tool_{t.asset.Substring(5)}.asset";
                var s = AssetDatabase.LoadAssetAtPath<SpeciesData>(path);
                if (s == null)
                {
                    s = ScriptableObject.CreateInstance<SpeciesData>();
                    AssetDatabase.CreateAsset(s, path);
                }
                var so = new SerializedObject(s);
                so.FindProperty("formula").stringValue = t.formula;
                so.FindProperty("displayName").stringValue = t.name;
                so.FindProperty("composition").ClearArray();
                so.FindProperty("jarColor").colorValue = new Color(0.72f, 0.52f, 0.32f);
                so.FindProperty("jarPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Art/Prefablar/{t.asset}.prefab");
                so.FindProperty("price").intValue = t.price;
                so.FindProperty("isTool").boolValue = true;
                so.FindProperty("iconName").stringValue = t.asset;
                so.ApplyModifiedPropertiesWithoutUndo();
                tools.Add(s);
            }

            // Katalog
            var catalog = AssetDatabase.LoadAssetAtPath<MarketCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MarketCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            foreach (var s in tools) catalog.AddIfMissing(s);
            EditorUtility.SetDirty(catalog);

            // Ekonomi kataloğu: kayıttan geri yükleme formülle buradan bulur; aletler yoksa yüklemede silinir.
            var economy = AssetDatabase.LoadAssetAtPath<EconomyData>(EconomyPath);
            if (economy != null)
            {
                var eso = new SerializedObject(economy);
                var list = eso.FindProperty("catalog");
                foreach (var s in tools)
                {
                    bool has = false;
                    for (int i = 0; i < list.arraySize; i++)
                        if (list.GetArrayElementAtIndex(i).objectReferenceValue == s) has = true;
                    if (has) continue;
                    list.InsertArrayElementAtIndex(list.arraySize);
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = s;
                }
                eso.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("[Alet pazarı] Economy bulunamadı: " + EconomyPath);

            // Simgeler temaya
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
            if (theme != null)
            {
                var tso = new SerializedObject(theme);
                var icons = tso.FindProperty("speciesIcons");
                foreach (var sp in sprites)
                {
                    bool has = false;
                    for (int i = 0; i < icons.arraySize; i++)
                        if (icons.GetArrayElementAtIndex(i).objectReferenceValue == sp) has = true;
                    if (has) continue;
                    icons.InsertArrayElementAtIndex(icons.arraySize);
                    icons.GetArrayElementAtIndex(icons.arraySize - 1).objectReferenceValue = sp;
                }
                tso.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("[Alet pazarı] UiTheme bulunamadı: " + ThemePath);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Alet pazarı] {tools.Count} alet, katalog: {CatalogPath}, simge: {sprites.Count}");
        }

        private static Sprite MakeSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter ti &&
                (ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single))
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
