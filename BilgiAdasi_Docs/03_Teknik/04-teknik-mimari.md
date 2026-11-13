# 04 — Teknik Mimari

Hedef: 24 saatte bitecek kadar sade, ama yeni mahalle eklemenin **yalnızca veri girişi** olduğu bir yapı.

## 1. Sahneler

| Sahne | İçerik | Not |
|---|---|---|
| `Boot` | Yönetici nesneler (DontDestroyOnLoad), kayıt yükleme | Tek kare, hemen `Classroom`'a geçer |
| `Classroom` | Açılış/kapanış ara sahnesi (2D UI + basit animasyon) | `CutsceneMode.Intro / Outro` |
| `Map` | Şehir → ilçe → mahalle seçimi | UI ağırlıklı; 3D diorama varsa arkada |
| `Workshop` | Atölye: kazan, teraziler, raf, tahta, kapı **ve** çalışma masası | Masa ayrı sahne değil; kamera noktası |

Sahne geçişi: kısa kararma (0,3 sn). `SceneFlow.Load(string)`.

## 2. Klasör yapısı

```
Assets/_Project/
  Scripts/
    Core/        GameManager, SceneFlow, SaveSystem, GameEvents
    Data/        ScriptableObject tanımları
    Chemistry/   ElementData, SpeciesData, ReactionData, AtomMath
    Map/         MapController, NeighborhoodNode
    Order/       OrderController, Cauldron, Scale, Shelf, JarView, Stove, SupplyChest
    Desk/        DeskController, PaperView, Question renderers
    UI/          DialogueBubble, HintBook, Chalkboard, RealLifeCard, Credits
    Cutscene/    ClassroomCutscene
  Data/
    Elements/ Species/ Reactions/ Questions/ Tasks/ Neighborhoods/ Themes/ Subjects/
  Prefabs/ Scenes/ Art/ Audio/ Fonts/
Docs/  (bu klasör: BilgiAdasi_Docs)
```

Namespace: `BilgiAdasi.Core`, `BilgiAdasi.Chemistry`, `BilgiAdasi.Order`, `BilgiAdasi.Desk`, `BilgiAdasi.Map`, `BilgiAdasi.UI`.

Proje / ürün adı: **Bilgi Adası** (Player Settings → Product Name). Klasör: `Assets/_Project/` (adı nötr kalır).

## 3. Veri modeli (ScriptableObject)

```
SubjectData (Şehir)        id, title, isPlayable, List<ThemeData>
ThemeData (İlçe)           id, title, isUnlocked, List<NeighborhoodData>
NeighborhoodData (Mahalle) id, title, introLines[], List<TaskData> (5), lockedByDefault
TaskData (abstract)        id, giverName, giverLines[], deliveryLines[], realLifeText, hintText
 ├─ OrderTask              mode (Exact|Purchase), List<OrderStep>, shelf[], budget, prices
 │    OrderStep            ReactionData reaction, targetProductAmounts[], cardShowsReactants
 └─ DeskTask               wrapper (Notebook|Labels|Letter|GuildExam), List<QuestionData>, randomCount
QuestionData (abstract)    id, prompt, hint
 ├─ MultipleChoiceQ        options[4], correctIndex
 ├─ TrueFalseQ             statements[] {text, isTrue}
 ├─ ClassifyQ              bins[], cards[] {text, binIndex}
 ├─ FillBlankQ             template ("... {0} ..."), answers[], distractors[]
 └─ BalanceEquationQ       ReactionData reaction (veya pool)
ElementData                symbol, displayName, color
SpeciesData                formulaRich ("H<sub>2</sub>O"), displayName, composition[] {ElementData, count}, jarColor
ReactionData               reactants[] {SpeciesData, coef}, products[] {SpeciesData, coef}, type (Synthesis|Analysis|Combustion|...)
```

> Polimorfik liste için `[SerializeReference]` ya da her alt tip için ayrı SO kullanın. 24 saat için **ayrı SO** daha az sürprizlidir: `List<QuestionData>` SO referansı tutar.

## 4. Çekirdek kimya kodu

```csharp
namespace BilgiAdasi.Chemistry
{
    public static class AtomMath
    {
        // tür → adet listesinden element → atom sayısı
        public static Dictionary<ElementData, int> Count(IEnumerable<(SpeciesData sp, int n)> items)
        {
            var d = new Dictionary<ElementData, int>();
            foreach (var (sp, n) in items)
                foreach (var c in sp.composition)
                    d[c.element] = d.TryGetValue(c.element, out var v) ? v + c.count * n : c.count * n;
            return d;
        }

        public static bool Equal(Dictionary<ElementData, int> a, Dictionary<ElementData, int> b)
        {
            foreach (var k in a.Keys.Union(b.Keys))
                if (a.GetValueOrDefault(k) != b.GetValueOrDefault(k)) return false;
            return true;
        }

        public static int Gcd(int a, int b) { while (b != 0) (a, b) = (b, a % b); return Mathf.Abs(a); }

        // BalanceEquationQ için: katsayılar en küçük tam sayılar mı?
        public static bool IsMinimal(IEnumerable<int> coefs)
        {
            int g = 0; foreach (var c in coefs) g = Gcd(g, c);
            return g == 1;
        }
    }
}
```

`Purchase` modu için gereken miktar:
```
ölçek   = hedef_ürün_adedi / reaction.ürün_katsayısı   (prototipte tam sayı garanti)
gereken[giren] = giren.coef × ölçek
artan[giren]   = kazandaki[giren] − gereken[giren]   (≥ 0 ise)
```

## 5. Durum makineleri

**OrderController**
```
Arrive → Briefing → Preparing ⇄ (Foam) → Reacting → Board → RealLife → Deliver → Done
                         └─ step tamam & sonraki step var → Preparing (yeni step)
```
- `Preparing` içinde her kazan değişiminde `RecalculateScales()`; dengedeyse otomatik `Reacting`.
- `Foam` 1,2 sn sürer, `Preparing`'e döner, `failCount++`.

**DeskController**
```
PigeonArrive → EnvelopeOpen → Answering ⇄ (Wrong) → Stamp → RealLife? → Done
```
- `Answering` her soruyu sırayla gösterir; `GuildExam` için soruları `randomCount` kadar rastgele çeker.

**NeighborhoodRunner**: mahalle görev listesini sırayla tipine göre `OrderController` ya da `DeskController`'a verir; görev bitince `GameEvents.TaskCompleted(taskId)`.

## 6. Olaylar

```csharp
public static class GameEvents
{
    public static event Action<string> TaskCompleted;          // taskId
    public static event Action<string> NeighborhoodCompleted;  // neighborhoodId
    public static event Action<string> HintOpened;             // taskId
    public static event Action<string> Failed;                 // taskId (köpürme / yanlış)
}
```
Mahalle canlanma görselleri, tahta, istatistik bu olayları dinler; denetleyiciler görselleri bilmez.

## 7. Kayıt

- `SaveData { HashSet<string> completedTasks; Dictionary<string,int> failCounts; HashSet<string> hintsOpened; List<string> boardEquations; }`
- JSON → `PlayerPrefs` (tek anahtar). Prototip için yeterli.
- **Geliştirici kısayolları** (yalnızca `DEVELOPMENT_BUILD` ya da `#if UNITY_EDITOR` + sunum bayrağı): `1–5` görevi atla, `R` kaydı sıfırla, `M` haritaya dön. Demo için hayati.

## 8. Kurallar

- `Update` içinde `Find`/`GetComponent` yok; referanslar Inspector'dan.
- Metinler SO'da; kodda sabit Türkçe metin yok (sonradan yerelleştirmeye hazır olur, ama **yarışma için hazır lokalizasyon paketi kullanılmaz** — "0'dan üret").
- TextMeshPro: formüller `<sub>` ile; fontta Türkçe karakterler (ğ, ş, ı, İ) ve `→` var mı kontrol et.
- Çözünürlük: 1920×1080 referans, Canvas Scaler "Scale With Screen Size".
- Build hedefi: Windows. Gece yarısı ve sabah 08.00'de build alınır.
