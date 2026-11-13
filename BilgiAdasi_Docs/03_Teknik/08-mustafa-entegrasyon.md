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
| `Station_Cauldron` | 1 | Benim prefab'ım (`_Project/Prefabs/Stations/`) | Kazan + ocak + boşaltma kolu + 3D terazi (FPS, kamera değişmez) |
| `Shelf` | 1+ | `Prefabs/Workshop/Shelf.prefab` | Atölye rafı (8 yer). Birden fazla koyarsan her birinin **Shelf Id**'si farklı olsun |
| `MarketStall` | 1+ | `Prefabs/Workshop/MarketStall.prefab` | Şehirdeki pazar tezgâhı (meydanda). Ne sattığı **Catalog** alanındaki pazar kataloğundan gelir (`Data/Market/Pazar.asset`). İkinci pazar koyarsan ona yeni bir katalog ver (Create → Bilgi Adası → Pazar Kataloğu); aynı kataloğu paylaşan tezgâhlar stoğu da paylaşır |
| `Station_Desk` | 1 | Benim prefab'ım | Çalışma masası |
| `Station_Door` | 1 | Benim prefab'ım | Kapı (sakinler burada konuşur; FPS, kamera değişmez) |
| `Station_Chalkboard` | 1 | Benim prefab'ım | Tahta (okuma) |
| `Neighborhood_Denklestirme` | 1 | `NeighborhoodView` (`neighborhoodId = denklestirme`) | Köyün önce/sonra hali |
| `Neighborhood_TepkimeTurleri` | 1 | `NeighborhoodView` (`neighborhoodId = tepkime_turleri`) | 〃 |
| `NeighborhoodRunner` | 1 | `NeighborhoodRunner` + `DialogueDirector` + `DevShortcuts` (menü **Kurulum → 6** ekler) | Köy akışı: kapıda sipariş, teslim konuşması, köy ilerlemesi. Görünmez; yerinin önemi yok |
| Kilitli bölge bariyerleri | gerektiği kadar | `LockedZone` + collider (trigger **değil**) | Kilitli köy/ilçeye girişi keser |

### Atölye prefab'ları (karar 13.11: kazan ve kapı FPS, istasyon kamerası yok — menü **Kurulum → 8**)
```
Station_Cauldron            ← CauldronStation + OrderController + BoxCollider, layer = Interactable ("Kazana dök")
├── Visual                  ← kazan modelin (eski küpü sil)
├── Effects                 ← CauldronVisuals: Liquid (sıvı yüzeyi), Foam, Fire — yer tutucu; kendi objelerini alanlara sürükle
├── Stove                   ← CauldronStove + collider, layer Interactable ("Ateşi yak")
├── Drain                   ← CauldronDrain + collider, layer Interactable ("Kazanı boşalt"); Lever = dönen kol (pivot menteşede)
└── Scale                   ← ScaleModel (3D terazi): Beam (ortadan döner) / LeftHook/LeftPan, RightHook/RightPan, Status yazısı
Shelf                       ← Shelf (Shelf Id, ekonomi, varsayılan kavanoz)
└── Slot_1 … Slot_8         ← ShelfSlot + BoxCollider, layer Interactable; kavanoz bu noktaya (taban ortası) konur
    └── PlaceMarker         ← yeşil işaret (ShelfSlot.Place Marker): elde madde varken boş yerde yanar, bakılan yer açık yeşil. Kendi modelini koyarsan alanı ona ver (URP Lit, _BaseColor); collider olmasın
MarketStall                 ← MarketStall (Catalog, Economy, Texts) + BoxCollider, layer Interactable ("Pazar")
```
- Prefab'ları istediğin yere taşı/döndür; kök bileşenlerine ve bu adlara dokunma. Terazi ve raf **önü -Z**: oyuncu -Z tarafından bakar.
- Etkileşim collider'ları (kazan kökü, Stove, Drain, Slot_*, MarketStall) layer `Interactable` ve **trigger değil**; birbirinin önünü kesmesin (Drain kazan kutusunun dışında).
- **Kavanoz modeli:** kökünde `JarProp` olan bir prefab yap (etiket TMP + boyanacak renderer'lar), `Data/Species/` altındaki maddenin **Jar Prefab** alanına ver. Boş bırakılan maddeler `Prefabs/Jars/Jar_Placeholder` (renkli küp) kullanır. Pivot taban ortası; kavanozda collider olmasın (ya da trigger).
- **Atom küresi:** terazideki küre `Prefabs/Workshop/Atom_Placeholder` (kökünde Renderer); kendi modelin varsa ScaleModel'in Atom Prefab alanına ver.
- Masa ve tahta hâlâ istasyon (`StationBase` + `CameraPoint`); Faz 5'te kullanılır. Tahtanın önünde 3D yazı (`BoardText`, `ChalkboardText`).

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
| `Interactable` | Etkileşilen collider'lar: istasyon/kazan kökü, Stove, Drain, raf yerleri, pazar tezgâhı. Başka objeye verme. |
| `Player` | Yalnızca oyuncu prefab'ı. |

Layer'ları menü **BilgiAdasi → Kurulum** ekler; elle ekleme.

## 4. UI: arayüz (interface) kuralı

Kod hiçbir Canvas'a ya da objeye bağlı değildir. Her UI parçası için bir **arayüz** var. Senin prefab'ına, o arayüzü uygulayan küçük bir view script'i takılır (script'i ben yazarım ya da birlikte yazarız; sen yalnızca alanları sürükleyip bırakırsın).

| Arayüz | Gösterdiği | Faz |
|---|---|---|
| `IInteractPromptView` | Crosshair + ipucu: `ShowPrompt(metin, simge)`. Simge hazır gelir (klavye `[E]`, Xbox `(X)`, PS `(□)`, Switch `(Y)`); view yalnızca çizer | 2 |
| `IScreenFader` | Sahne geçişi kararması | 1 |
| `IInventoryView` | **HUD:** ekranın altında 6 slot (madde + adet, seçili slot vurgulu) + kese: `ShowInventory(slotlar, seçili, para)` | 4 |
| `IOrderCardView` | **HUD:** aktif sipariş kartı: veren, istenen ürünler, (Görev 1–2) girenler, ödeme, "hazır" durumu; `ClearOrder()` | 4 |
| `INoticeView` | **HUD:** kısa not ("Envanterde yer yok", mühür notu, köpürme…) birkaç saniye | 4 |
| `IMarketView` | **HUD (UI modu):** pazar listesi (madde, fiyat, elde kaç) + kese; düğmeler `IMarketCommands.Buy / CloseMarket` | 4 |
| `IHintBookView` | **HUD:** ustanın defteri: 2 hatadan sonra parlar (H / Y), açılınca ipucu | 4 |
| `IRealLifeCardView` | **HUD:** "Gerçek hayatta" kartı; kod birkaç saniye sonra kapatır | 4 |
| `ICauldronView` | **3D:** sıvı, döküm, boşaltma, köpük, ateş (`CauldronVisuals`) | 4 |
| `IScaleView` | **3D terazi** (`ScaleModel`): element başına sol / sağ / artan; dengede mi; köpürmede fark | 4 |
| `IChalkboardView` | **3D:** tahtadaki denklemler (`ChalkboardText`, alt indisli TMP) | 4 |
| `IDialogueView` | Konuşma balonu + portre: `ShowLine(satır, devamıVar)`, `Hide()`. Satırda konuşan adı, metin, portre (sprite, boş olabilir). Kapıda hareket durur, E/Space (X/A) ilerletir; tıklayınca `IDialogueCommands.Advance()` | 4 |
| `IPaperView` | Masa kâğıdı ve sorular | 5 |
| `IMapView` | Harita: köy durumu + 5 nokta | 6 |

**Etkileşim → kod:** Atölyede (kazan, raf, ocak, kol, pazar, kapı) her şey crosshair + E/X ile; UI düğmesi yalnızca pazar ekranında (`ViewLocator.Get<IMarketCommands>()`). Tam imzalar: `Scripts/Order/OrderViews.cs`, `Scripts/UI/SharedViews.cs`. Bir view birden fazla arayüzü uygulayabilir (örnek: `PlaceholderWorkshopHud`).

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
