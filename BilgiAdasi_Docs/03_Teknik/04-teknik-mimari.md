# 04 — Teknik Mimari

Hedef: 24 saatte bitecek kadar sade, ama yeni köy eklemenin **yalnızca veri girişi** olduğu bir yapı.

## 1. Sahneler

| Sahne | İçerik | Not |
|---|---|---|
| `Boot` | Yönetici nesneler (DontDestroyOnLoad), kayıt yükleme | Tek kare, hemen `Classroom`'a geçer |
| `Classroom` | Açılış/kapanış ara sahnesi (2D UI + basit animasyon) | `CutsceneMode.Intro / Outro` |
| `Town` | Atölye (kazan, teraziler, raf, tahta, kapı, çalışma masası) **ve** yürünen şehir | Tek oyun sahnesi; masa ve kazan birer istasyon kamerası. Harita = bu sahnede UI katmanı. Bağlantı noktaları: `08-mustafa-entegrasyon.md` |

Sahne geçişi: kısa kararma (0,3 sn). `SceneFlow.Load(string)`.

## 2. Klasör yapısı

```
Assets/_Project/
  Scripts/
    Core/        GameManager, SceneFlow, SaveSystem, GameEvents
    Data/        ScriptableObject tanımları
    Chemistry/   ElementData, SpeciesData, ReactionData, AtomMath
    Player/      PlayerMotor, PlayerLook, PlayerInteractor, PlayerGrowth
    Stations/    StationBase, CauldronStation, DeskStation, DoorStation
    City/        NeighborhoodRunner, NeighborhoodView, MapController
    Order/       OrderController, Cauldron, Scale, Shelf, JarView, Stove, SupplyChest
    Desk/        DeskController, PaperView, Question renderers
    UI/          DialogueBubble, HintBook, Chalkboard, RealLifeCard, Credits
    Cutscene/    ClassroomCutscene
  Data/
    Elements/ Species/ Reactions/ Questions/ Tasks/ Neighborhoods/ Themes/ Subjects/
  Prefabs/ Scenes/ Art/ Audio/ Fonts/
Docs/  (bu klasör: BilgiAdasi_Docs)
```

Namespace: `BilgiAdasi.Core`, `.Player`, `.Chemistry`, `.Order`, `.Desk`, `.City`, `.UI`.

Proje / ürün adı: **Bilgi Adası** (Player Settings → Product Name). Klasör: `Assets/_Project/` (adı nötr kalır).

## 3. Veri modeli (ScriptableObject)

```
SubjectData (Şehir)        id, title, isPlayable, List<ThemeData>
ThemeData (İlçe)           id, title, isUnlocked, List<NeighborhoodData>
NeighborhoodData (Köy) id, title, introLines[], List<TaskData> (5), lockedByDefault
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

        public static int Gcd(int a, int b) { while (b != 0) (a, b) = (b, a % b); return System.Math.Abs(a); }

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
(kapıda alınır) → Preparing ⇄ (Foam: malzeme gider) → Reacting → Board → ürün envantere → ReadyToDeliver → (kapıda elle) Deliver → Done
Preparing'de: Pour (eldeki maddeden 1, geri alınmaz) · Drain (kol, malzeme gider) · Fire (ocak)
                         └─ step tamam & sonraki step var → Preparing (yeni step)
```
- `Preparing` içinde her kazan değişiminde `RecalculateScales()`; `Reacting` yalnızca FireStove ile (otomatik değil).
- `Foam` 1,2 sn sürer, `Preparing`'e döner, `failCount++`.

**DeskController**
```
PigeonArrive → EnvelopeOpen → Answering ⇄ (Wrong) → Stamp → RealLife? → Done
```
- `Answering` her soruyu sırayla gösterir; `GuildExam` için soruları `randomCount` kadar rastgele çeker.

**NeighborhoodRunner**: köy görev listesini sırayla tipine göre `OrderController` ya da `DeskController`'a verir; görev bitince `GameEvents.TaskCompleted(taskId)`. Köyler birbirinden bağımsız ilerler (sıra serbest). Sipariş kapıda alınır (ilk görevde ustanın tanıtımı + sakinin derdi), kazanda çözülür, teslim repliği konuşma balonunda söylenir. İlerleme kayıttaki `completedTasks`'tan hesaplanır; `NeighborhoodProgress` olayıyla `NeighborhoodView`'a gider.

## 6. Olaylar

```csharp
public static class GameEvents
{
    public static event Action<string> TaskCompleted;          // taskId
    public static event Action<string> NeighborhoodCompleted;  // neighborhoodId
    public static event Action<string> HintOpened;             // taskId
    public static event Action<string> Failed;                 // taskId (köpürme / yanlış)
    public static event Action<string, int, int> NeighborhoodProgress; // neighborhoodId, biten, toplam
}
```
Köy canlanma görselleri, tahta, istatistik bu olayları dinler; denetleyiciler görselleri bilmez.

## 7. Kayıt

- `SaveData { int grade; List<string> completedNeighborhoods; HashSet<string> completedTasks; Dictionary<string,int> failCounts; HashSet<string> hintsOpened; List<string> boardEquations; }`
- JSON → `PlayerPrefs` (tek anahtar). Prototip için yeterli.
- **Geliştirici kısayolları** (`DevShortcuts`; yalnızca editör, `DEVELOPMENT_BUILD` ya da `BILGIADASI_DEMO` sunum bayrağı): `1–5` odaktaki köyün o görevine atla (gamepad: LT+RT+→ sıradaki görev), `Ctrl+R` kaydı sıfırla (gamepad: LT+RT+←; yanlışlıkla basılmasın diye tek tuş değil). "Haritaya dön" harita ekranı (Faz 6) gelince eklenir. Demo için hayati.

## 8. Kurallar

- `Update` içinde `Find`/`GetComponent` yok; referanslar Inspector'dan.
- Metinler SO'da; kodda sabit Türkçe metin yok (sonradan yerelleştirmeye hazır olur, ama **yarışma için hazır lokalizasyon paketi kullanılmaz** — "0'dan üret").
- TextMeshPro: formüller `<sub>` ile; fontta Türkçe karakterler (ğ, ş, ı, İ) ve `→` var mı kontrol et.
- Çözünürlük: 1920×1080 referans, Canvas Scaler "Scale With Screen Size".
- Build hedefi: Windows. Gece yarısı ve sabah 08.00'de build alınır.
