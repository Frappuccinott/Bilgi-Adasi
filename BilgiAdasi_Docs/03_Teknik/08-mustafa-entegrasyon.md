# 08 — Mustafa Entegrasyon Kuralları

> Kısa kural: **Sen görünümü yaparsın, kod bağlantı noktalarına ve arayüzlere bakar.** Bir objenin adı ya da yeri değişebilir; aşağıdaki **bileşen** üzerinde kalmalı.
> `_Placeholder` adlı her şey benim test yer tutucumdur. Kendi versiyonun hazır olunca sil.

## 1. Sahne

- Tek oyun sahnesi: `Assets/_Project/Scenes/Town.unity` (atölye + şehir). `Boot` ve `Classroom` sahnelerine dokunma.
- `Town` şu an test sahnesidir: `_Placeholder` kökü (zemin, duvar, basamak, rampa, ham HUD) senin düzenin gelince silinir. Prefab örnekleri (`PlayerSpawn`, `Station_*`, `Neighborhood_*`, `LockedZone_Example`) kalır, yalnızca yerlerini değiştirirsin.
- Kendi düzenini ayrı bir sahnede kurup sonra `Town`'a taşıyabilirsin; aynı sahnede aynı anda ikimiz çalışmayalım (merge çakışır). Sahneyi açmadan önce haber ver.
- Ölçek: 1 birim = 1 m. Göz yüksekliği 1.56 → 1.68 m (oyuncu büyür). Kapı ≥ 2.1 m, tezgâh ~0.9 m, raf üstü ≤ 1.8 m.
- Uçtan uca yürüyüş ≤ 40 sn (yürüme hızı 3.8 m/s → yol ~150 m'yi geçmesin).
- Yürünecek her yüzeyde **collider** olmalı. Kaldırım/eşik ≤ 0.3 m, rampa ≤ 45°.

## 2. Sahnede olması gerekenler

| Obje (önerilen ad) | Adet | Bileşen / ayar | Ne yapar |
|---|---|---|---|
| `PlayerSpawn` | 1 | `Prefabs/PlayerSpawn.prefab` | Oyuncu (ve Main Camera) Play'de burada doğar. Mavi ok = bakış yönü. Zeminin hemen üstüne koy. Sahneye ayrıca kamera koyma. |
| `EventSystem` | 1 | EventSystem + *Input System UI Input Module* | UI tıklamaları için |
| `Station_Cauldron` | 1 | Benim prefab'ım (`_Project/Prefabs/Stations/`) | Kazan istasyonu |
| `Station_Desk` | 1 | Benim prefab'ım | Çalışma masası |
| `Station_Door` | 1 | Benim prefab'ım | Kapı (sakinler burada konuşur) |
| `Station_Chalkboard` | 1 | Benim prefab'ım | Tahta (okuma) |
| `Neighborhood_Denklestirme` | 1 | `NeighborhoodView` (`neighborhoodId = denklestirme`) | Köyün önce/sonra hali |
| `Neighborhood_TepkimeTurleri` | 1 | `NeighborhoodView` (`neighborhoodId = tepkime_turleri`) | 〃 |
| Kilitli bölge bariyerleri | gerektiği kadar | `LockedZone` + collider (trigger **değil**) | Kilitli köy/ilçeye girişi keser |

### İstasyon prefab'ları
```
Station_Cauldron            ← kök: StationBase türevi + BoxCollider, layer = Interactable
├── Visual                  ← modelini buraya koy (eski küpü sil)
└── CameraPoint             ← istasyon kamerasının yeri ve bakışı; okunurluk için ayarla
```
- Prefab'ı istediğin yere taşı/döndür; kök bileşenine ve `CameraPoint` adına dokunma.
- BoxCollider'ı modele göre boyutlandır (oyuncu baktığında yakalanacak alan).
- Kamera `CameraPoint`'in **ileri (mavi) eksenine** bakar. Game view'da kontrol et: kazan + iki terazi tek kadrajda.

### Köy önce/sonra
```
Neighborhood_Denklestirme   ← NeighborhoodView
├── Before                  ← köy bitmeden görünen (kuru çeşme, sönük fener…)
├── After                   ← köy bitince açılan
└── Parts                   ← (isteğe bağlı) Part_1 … Part_5: her görev bitince biri açılır
```
Kod yalnızca bu objeleri aç/kapa yapar; içine istediğini koy.

## 3. Layer'lar

| Layer | Kim kullanır |
|---|---|
| `Interactable` | İstasyon kökleri. Başka objeye verme (raycast bunu arar). |
| `Player` | Yalnızca oyuncu prefab'ı. |

Layer'ları menü **BilgiAdasi → Kurulum** ekler; elle ekleme.

## 4. UI: arayüz (interface) kuralı

Kod hiçbir Canvas'a ya da objeye bağlı değildir. Her UI parçası için bir **arayüz** var. Senin prefab'ına, o arayüzü uygulayan küçük bir view script'i takılır (script'i ben yazarım ya da birlikte yazarız; sen yalnızca alanları sürükleyip bırakırsın).

| Arayüz | Gösterdiği | Faz |
|---|---|---|
| `IInteractPromptView` | Crosshair + ipucu: `ShowPrompt(metin, simge)`. Simge hazır gelir (klavye `[E]`, Xbox `(X)`, PS `(□)`, Switch `(Y)`); view yalnızca çizer | 2 |
| `IScreenFader` | Sahne geçişi kararması | 1 |
| `IOrderStationView` | Kazan paneli açık/kapalı (`SetOpen`) | 3 |
| `IOrderCardView` | Sipariş kartı: veren, istenen ürünler, (Görev 1–2) girenlerin adı, kese; kısa mesaj | 3 |
| `IShelfView` | Raf: kavanozlar (kazanda kaç, elde kaç, fiyat); etiket tıklanınca mühür titremesi + ilk seferde not | 3 |
| `ICauldronView` | Kazandaki türler (adet), ocak yanık/sönük, köpürme ve tepkime animasyonu | 3 |
| `IScaleView` | **İki terazi birlikte**: element başına sol / sağ / artan; dengede mi; köpürmede fazla (kırmızı) / eksik (kesikli) | 3 |
| `IChalkboardView` | Tahtadaki denklemler (alt indisli TMP) | 3 |
| `IHintBookView` | Ustanın defteri: 2 hatadan sonra parlar, açılınca ipucu | 3 |
| `IRealLifeCardView` | "Gerçek hayatta" kartı (kapat düğmesi) | 3 |
| `IDialogueView` | Konuşma balonu + portre | 4 |
| `IPaperView` | Masa kâğıdı ve sorular | 5 |
| `IMapView` | Harita: köy durumu + 5 nokta | 6 |

**Tıklamalar → kod:** Düğmeler, sürükle-bırak ve etiket tıklaması `ViewLocator.Get<IOrderCommands>()` üzerinden çağrılır: `AddToCauldron`, `Increase`, `Decrease`, `Select`, `TouchLabel`, `FireStove`, `ToggleHint`, `CloseRealLifeCard`. Tam imzalar: `Scripts/Order/OrderViews.cs`, `Scripts/UI/SharedViews.cs`. Bir view birden fazla arayüzü uygulayabilir (örnek: `PlaceholderOrderView`).

View script'i `RegisteredView<IArayüz>`'den türer ve açılınca kendini kaydeder; bu yüzden **sahnede bir yere sürükleyip bağlaman gerekmez**, objenin açık olması yeterli. Örnek: `Scripts/UI/Placeholder/PlaceholderInteractPromptView.cs`.

Her arayüzün ham bir `Placeholder…View` versiyonu sahnede `_Placeholder` altında durur; seninki hazır olunca onu sil. Aynı arayüzü iki view birden uygulamasın (son açılan kazanır).

UI ayarları: Canvas Scaler → *Scale With Screen Size*, 1920×1080. Yazı en az 28 px. Formüller TMP'de `H<sub>2</sub>O`. Fontta ğ ş ı İ ve `→` olmalı.

## 5. Yapma

- Benim prefab'larımın kökündeki bileşenleri silme ya da adlarını değiştirme (`Visual` içi serbest).
- `_Project/Scripts/` ve `_Project/Data/` içinde değişiklik yapma; ihtiyacın olursa söyle.
- Hazır model/doku/font eklediysen `05_Uretim/LICENSES.md`'ye satır yaz (yoksa projeye girmez).

## 6. Repo ve haberleşme kuralları

> Simya reposundaki `md/02`'den bu projeyle çelişmeyen kurallar alındı. Görsel ve model kuralları: `01_Tasarim/09-tema-ve-model-kurallari.md`.

**Temel kural:** başkasının alanındaki dosyayı kendin düzeltmezsin. Bir şey gerekiyorsa sahibine yazarsın, o yapar. "Küçük bir şeydi" çakışmaların çoğunun sebebidir.

- **Klasör sahipliği:** Mustafa → `_Project/Art/`, `_Project/Level/`, `_Project/UI/`, `ArtSource/`. Mehmet → `_Project/Scripts/`, `_Project/Data/`, `_Project/Input/`, `_Project/Tests/`. Ortak alan → `ProjectSettings/`, `Packages/`, `Assets/Settings/`, `_Project/Prefabs/`: değiştirmeden önce diğerine haber verilir, aynı anda yalnızca bir kişi dokunur.
- **Sahne:** Bir sahneyi aynı anda tek kişi düzenler (§1). Başkasının sahnesini açıp bakabilirsin; **kaydetmezsin.** Kişisel deneme sahnesi `Deneme_<ad>.unity`; kimse başkasınınkine dokunmaz.
- **Model ↔ mekanik:** Model `Visual` altına ya da `_Project/Art/` altında script'siz prefab olarak teslim edilir. Mekanik script'leri bu prefaba değil, istasyon kökünde ya da prefab variant'ta durur. Böylece model güncellenince mekanik bozulmaz.
- **`.meta` dosyaları** her zaman dosyasıyla birlikte commit edilir. Dosya taşıma ve yeniden adlandırma Unity içinden yapılır, dosya gezgininden değil.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/` commit edilmez.
- **Commit mesajı** kısa ve Türkçe, alan önekiyle: `level: atölye zemini eklendi`, `ui: sipariş kartı`, `docs: tema kuralları`.
- Güne başlarken ve push'tan önce `main`'i çek.
- **Çakışma olursa:** durdur, zorla birleştirme (`--force`, "theirs/ours" ile ezme) yapma. Çakışan dosya kimin alanındaysa onun sürümü geçerlidir. Sahne/prefab çakışmasında dosya elle düzeltilmez; sahibi kendi sürümünü alır, diğeri değişikliğini Unity içinde yeniden uygular. Sebebi buraya kural olarak eklenir.
- **Mutlaka haber verilecekler:** `ProjectSettings/` ya da `Packages/` değişikliği, §2'deki bir adın değişmesi, klasör taşıma.

## 7. Tuş simgeleri

Kendi UI'ında tuş göstereceksen yazıyı elle yazma: `ControlGlyphs.For(PromptAction.FireStove)` son kullanılan cihaza göre doğru simgeyi verir (klavye / Xbox / PlayStation / Switch). Değişimi dinlemek için `GameInput.SchemeChanged`. İkon (sprite) kullanmak istersen aynı olaya bağlanıp kendi sprite'ını seçebilirsin; düzen `GameInput.LastScheme`'de.
