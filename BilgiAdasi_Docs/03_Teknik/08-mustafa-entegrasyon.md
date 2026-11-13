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

### Bisiklet (menü **Kurulum → 9**)
```
Bicycle                     ← Bicycle + CharacterController, layer = Interactable ("Bisiklete bin") — DOKUNMA
├── InteractVolume          ← BoxCollider (bakma hacmi), layer Interactable; bisikletin boyutuna göre büyütülebilir
├── Visual                  ← BicycleRig; virajda yatan kök (pivot yerde)
│   └── _Placeholder        ← küp model: gerçek model gelince sil
└── SeatAnchor              ← sürücünün kök noktası (göz = bu + 1.56 m); gidon ekranın altında görünecek şekilde ayarla
```
- Model: **+Z ileri, +X sağ**, orijin iki teker arasında yerde. Dönen parçalar ayrı obje ve **pivotu dönme noktasında**: `FrontWheel` / `RearWheel` (göbek), `Steering` (furş borusu; çatal + gidon + ön teker bunun çocuğu), `Crank` (orta göbek; `Pedal_L` / `Pedal_R` bunun çocuğu).
- Modeli `Visual` altına koy, `BicycleRig` alanlarına bu parçaları sürükle. Model ekseni farklıysa `BicycleRig` → Eksenler'den düzelt (ters dönüyorsa eksiyi ver). `Wheel Radius` gerçek teker yarıçapı olmalı (dönüş hızı buradan).
- Modelde collider olmasın (çarpışma kapsülü ve InteractVolume yeter).
- **Eller gidonda (menü Kurulum → 14):** gidonun (`Steering`) çocuğu `Tutma_Sol` / `Tutma_Sag` boş objeleri `BicycleRig` → Sürücü elleri'ne bağlı. Konum = avuç ortası (tutamak ekseninde), dönüş = elin hedef dönüşü (yeşil Y parmaklar, mavi Z elin sırtı; obje seçiliyken gizmo). Menü bunları gidon modelinin tutamak bölgesinden ölçer; **Kurulum → 11** de (model güncellenince) yeniden ölçer. Elle düzeltmek istersen bu iki objeyi oynat.
- FPS kolları (`PlayerRig` → Main Camera → `Ogrenci_Kol_Rig`) üzerinde `RiderArmsIK`: binince eller tutma noktalarına uzanır, sürerken gidonla döner/yatar, inince bırakır. Kol iskeleti adları (`Kok`, `Kol_Ust_L/R`, `Kol_On_L/R`, `El_L/R`, `Tutma_L/R`, `Parmak_*_1..3_L/R`) değişirse Kurulum → 14'ü yeniden çalıştır.
- **Gövde ve bacaklar (menü Kurulum → 15):** `PlayerRig` → `Player/Govde` (`Ogrenci_Govde_Rig` + `PlayerBodyView`, controller `Kontrol/Ogrenci_Govde_Oyuncu`: `Yer` karışımı Hiz×Comel, `Havada`, `Bisiklet`). Bisiklette kalça `Visual/Sele_Oturma`'ya oturur (`BicycleRig` → Seat), ayaklar `Pedal` objelerine IK ile basar; `SeatAnchor` oturan sürücünün gözüne göre kurulur. Bisiklet modeli değişince (sele yeri) Kurulum → 11, sonra 15 (sele noktası `PlayerBodySetup.SeatInModel` sabitinde, `bisiklet.py` ile aynı tutulmalı).

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
| `IAchievementToastView` | **HUD:** sağ alttan kayan başarım bildirimi (simge, başlık "Öğrenildi!" / "Başarım kazanıldı!" / "Meydan okuma…", ad; tür rengi `info.Color`). Art arda gelirse sıraya alır; Esc menüsünde de akar (unscaled zaman) | 4 |
| `IPauseMenuView` | **Esc menüsü (UI modu):** ana sayfa (Devam / Başarımlar / Çık) + ders sekmeli (`AchievementMenuInfo.Tabs`: Kimya, Matematik, Fizik, Biyoloji; sekme rengi `tab.Color`) dallanan başarım ağacı (`AchievementNodeInfo`: sütun, satır, ebeveyn, durum açık/kilitli/gizli, renk, simge). `TryGoBack()` alt sayfadan döner. Düğmeler `IPauseMenuCommands.Resume / QuitGame` | 4 |

**Başarım tasarımını düzenlemek (kod gerekmez):**
- *Metin, simge, renk, sekme:* `Data/Achievements/AchievementTree_<Ders>.asset` → sekme adı/simgesi/rengi, "Yakında", tür görünümü (başlık, renk, varsayılan simge), başarım başına simge. Düğüm yerini beğenmezsen *Manual Position* aç, sütun/satır yaz. Inspector altta ağacı önizler ve hataları gösterir.
- *Bildirim ve menü kutuları:* ham view'larda (`_Placeholder/HUD/AchievementToast`, `PauseMenu`) renk, boyut, font, 9-slice sprite alanları var. Kendi prefab'ını yaparsan `IAchievementToastView` / `IPauseMenuView`'ı uygula ve ham olanı sil.
- Görev ↔ başarım listesi: `04_Icerik/11-gorev-ve-basarim-listesi.md`.
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

## 7. Taşınabilir eşya sözleşmesi (raf → 6 gözlü envanter → elde 3B → yere at → kazana koy)

> Model tarafı hazır (39 eşya, `ArtSource/Blender/Script/esya_tasinir.py`). Envanter/elde tutma/bırakma kodu Mehmet'in alanı; aşağıdakiler modelin kodla buluştuğu noktalardır. Araştırma ve gerekçeler: `ArtSource/Referans/esya.md`.

**Adlar:** `Malzeme_<Formül>` (oyun türleri: `Malzeme_H2`, `_O2`, `_N2`, `_CO2`, `_CO`, `_CH4`, `_C3H8`, `_H2O`, `_NH3`, `_CaCO3`, `_CaO`, `_CaOH2`), `Madde_<Ad>` (dönem maddeleri), `Kulce_<Metal>`, `Alet_<Ad>`. Ca(OH)₂ dosya adında `CaOH2`.

**Her eşyanın yapısı (tek kademe):**
```
Malzeme_H2                 ← boş üst obje; pivot = TABAN ORTASI (rafta/yerde dik durur)
├── Malzeme_H2_Model       ← tek mesh, M_Stilize (köşe rengi); dünya, el ve ikon için AYNI mesh
├── Malzeme_H2_Tutma_Noktasi   ← elde: bu nokta kameradaki tutma çapasına hizalanır
├── Malzeme_H2_Etiket_Yuzu     ← formül etiketi (TMP, dünya-uzayı); ok yönü = etiketin baktığı yön
└── Malzeme_H2_Agiz_Noktasi    ← kazana dökme/parçacık noktası (aletlerde olmayabilir)
```
- Etiket ve mühür kabın **ön yüzünde**: Blender'da −Y, **Unity'de eşyanın +Z yüzü** (13.11'de Unity'de ölçüldü). Elde tutulurken etiketin kameraya bakması için eşya kameraya göre **Y ekseninde 180°** döndürülür.
- Prefab yapısı: dönüşü sıfır sarmalayıcı kök (`<Ad>`) + altında model (Animator modelde). `Instantiate(prefab, konum, Quaternion.identity)` güvenlidir; eşya prefabları kendi tabanında (0,0,0) durur.
- Mühürdeki kabartmalar formülü gösterir: atom başına bir nokta, renk + şekil (H küre · O halka · C küp · N piramit · Ca altıgen).
- Bütçe: eşya başına ≤ 1.200 üçgen (O₂, CO₂, CaO, Ca(OH)₂, kükürt %10'a kadar aşıyor). LOD gerekmez.

**İkonlar:** `Assets/_Project/UI/Ikonlar/<Ad>.png` — 256×256, şeffaf, aynı kamera/ölçek (eşyalar birbirine göre doğru boyda). UI'da 64-96 px gösterilir. Yeniden üretim: `ikon_render.py`.

**Kod için öneriler (araştırma, karar Mehmet'in):**
- Elde: ayrı `ViewModel` katmanı + URP Render Objects (ayrı FOV 50-60°, duvara girmez) ya da Overlay kamera. Konum ~(+0,20; −0,15; +0,50) m, içe −15°. Gölge kapalı.
- Dünyada: Rigidbody + düz tabanlı Box/Convex collider (dik dursun), `centerOfMass` yükseklik × 0,25, Angular Damping 0,5-1,0, Continuous Speculative.
- Girdi (her aksiyon klavye + gamepad, `CLAUDE.md` §3): Al = Interact (E / X); **Bırak = G / D-pad aşağı** (B zaten CrouchToggle ve ExitStation); göz seçimi = 1-6 + fare tekeri / LB-RB (LB/RB istasyonda Increase/Decrease, Player map'te boş).
- **Doküman uyarısı:** `02-siparis-mekanigi.md` hâlâ "raftan kazana sürükleme" (istasyon modu) anlatıyor; envanter sistemine geçilecekse 02 ve `CLAUDE.md` güncellenmeli.

## 8. Animasyon sözleşmesi (bütün animasyonlar Blender'da; Unity'de yalnız `Animator.Play`)

**Unity'de elle iş yok:** `Assets/_Project/Art/Editor/SanatIceAktarma.cs` FBX içeri girince Generic rig kurar, `_Dongu` kliplerini döngüye alır, her FBX için `Kontrol/<Ad>.controller` (varsayılan durum `*_Bosta`) ve `Prefablar/<Ad>.prefab` üretir. Menü: **BilgiAdasi → Sanat → Atölyeyi açık sahneye yerleştir** (hareketsiz atölye + bütün düzenekler, dünya konumlarında, kök `Atolye_Sanat`). 13.11'de Unity 6000.4.6'da derlendi ve `Deneme_Mustafa.unity` sahnesinde denendi: shader hatasız, 47 kontrolcü boşta durumunda açılıyor, döngü klipleri döngüde.

Kod yalnızca: `animator.Play("Klip_Adi")` (ya da `CrossFade`). Her klip iskeletin **bütün** kemiklerine anahtar verir; bir klip oynarken diğer klibin efektleri görünmez (köpük, damla vb. gizli kalır). Aynı anda gereken hareketler ayrı iskelette (ör. ocak ateşi ≠ kazan; damlatma ≠ kâbile dolumu) → katman gerekmez. 30 kare/sn.

| Prefab (FBX) | Klipler | Ne zaman |
|---|---|---|
| `Kapi_Rig` | `Kapi_Bosta` (kapalı), `Kapi_Ac` (0,7 sn, 95° içeri), `Kapi_Kapat` (0,63 sn) | Kapı etkileşimi. Kanadın collider'ı `Kapi_Kanat` objesine (kemik çocuğu) konur. |
| `Kazan_Rig` | `Kazan_Bosta`, `Kazan_Karistir_Dongu` (2 sn), `Kazan_Kaynama_Dongu` (1,6 sn), `Kazan_Kopurme` (2,2 sn), `Kazan_Basari` (1,9 sn) | Kavanoz eklenince Karıştır; teraziler dengedeyken Kaynama; dengesiz ateşte Köpürme → Boşta; tepkimede Başarı → Boşta |
| `Ocak_Ates_Rig` | `Ates_Bosta` (sönük), `Ates_Yan` → `Ates_Titre_Dongu`, `Ates_Son` | Ocak dengeyle kendiliğinden yanar (02 §2) |
| `Damitma_Rig` (imbik) | `Damitma_Bosta`, `Damitma_Basla` (0,8 sn) → `Damitma_Dongu` (3 sn, 3 damla), `Damitma_Dur` | Damıtma görevi |
| `Kabile_Rig` | `Kabile_Bosta` (az), `Kabile_Dolum` (10 sn), `Kabile_Dolu` | Damıtma süresince Dolum; bitince Dolu |
| `SuBanyosu_Rig` | `SuBanyosu_Bosta`, `SuBanyosu_Basla` → `SuBanyosu_Dongu` (buhar), `SuBanyosu_Dur` | Yumuşak ısıtma görevi |
| `Havan_Rig` | `Havan_Bosta` (havaneli yaslı), `Havan_Dov_Dongu` (0,8 sn), `Havan_Ez_Dongu` (1,6 sn) | Ezme/toz etme görevi |
| `Koruk_Rig` | `Koruk_Bosta` (kapalı), `Koruk_Pompala_Dongu` (1 sn) | Ateşi harlatma |
| `Esya/<Ad>` (39 eşya) | `El_Bosta`, `El_Al` (0,33 sn), `El_Birak` (0,27 sn); oyun malzemelerinde (12) ayrıca `El_Dok` (2,2 sn) | Al/bırak/kazana dök (elde, viewmodel) |

| `Ogrenci_Kol_Rig` (FPS kolları) | `Kol_Bosta` (kadraj altında, gizli), `Kol_Kaldir` (0,37 sn), `Kol_Bekle_Dongu` (3 sn), `Kol_Uzan` (0,77 sn; 0,4 sn'de dokunma anı), `Kol_Tut_Dongu` (3 sn), `Kol_Indir` (0,3 sn) | Etkileşimde Uzan; elde eşya varken Tut; istasyona girince Indir → Bosta |

**FPS kolları (`Ogrenci_Kol_Rig`, 13.11):** öğrencinin okul sweatshirt'ü (kolları sıvalı) + eller; tek SkinnedMeshRenderer, 4.936 üçgen, 39 kemik, `BilgiAdasi/Stilize`. Gerekçe: `ArtSource/Referans/ogrenci_kol.md`. Yeniden üretim: `blender -b --factory-startup --python ArtSource/Blender/Script/ogrenci_kur.py -- disa`.
- Prefab `Assets/_Project/Art/Prefablar/Ogrenci_Kol_Rig.prefab` **`PlayerRig/Main Camera`'nın çocuğu olarak eklendi** (13.11; yerel 0, gölge düşürmez, Animator AlwaysAnimate). Pozlar FOV 70 (16:9) için ayarlı.
- Kontrolcü akışı (içe aktarma aracı kurar): açılışta `Kol_Kaldir` → `Kol_Bekle_Dongu`; `Kol_Uzan` → Bekle; `Kol_Indir` → `Kol_Bosta`. Kod yalnızca `Play` çağırır, dönüşler kendiliğinden.
- **Tutma noktası:** `Tutma_R` kemiği (deform yok, `El_R` çocuğu). `Kol_Tut_Dongu`'da kameraya göre tam (+0,22; −0,19; +0,42) — §7'deki eşya tutma çapasıyla aynı. Eşya `Tutma_R`'nin çocuğu yapılırsa elle birlikte nefes alır; kemiğin ekseni el yönündedir, eşyanın dönüşü yerelde düzeltilir.
- Kamera near = 0,05 (PlayerRig'de zaten). Kollar duvara girerse §7'deki ViewModel katmanı/Overlay kamera önerisi kollar için de geçerli; gölge düşürmesin.

**`El_Dok` zamanlaması** (kazana sayımı buna bağlayın): 0-0,4 sn kaldır, 0,7 sn'de tam eğim; **akış/efekt 0,73 sn'de başlar, 1,63 sn'de biter**; 2,2 sn'de el eski hâline döner. Gazlar 62° eğilir ve buhar bırakır; sıvılar (H₂O, NH₃) 115° akıtır; tozlar (CaO, Ca(OH)₂) 125°; kireç taşı 130° taş döker.

**Eşya iskeleti:** `<Ad>_Rig` → kemik `Kok` (tutma noktasında; model, `_Tutma_Noktasi`, `_Etiket_Yuzu`, `_Agiz_Noktasi` buna bağlı) + `Efekt` (ağız noktasında; dökme efekti). Elde: prefab tutma çapasına konur, `Kok` kemiği klipte tutma noktası etrafında döner.

**Görev önerisi (karar ekipte):** dönem aletleri ders kitabındaki yöntemlere karşılık gelir — **damıtma** (imbik, Câbir), **eritme/kireçleştirme** (kroze + mangal, Râzî), **ezme** (havan), **yumuşak ısıtma** (su banyosu). Örn. Görev 4'ün ilk adımı (CaCO₃ → CaO, "taştan kireç") kroze + mangal ile ısıtma olarak oynanabilir.

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

## 10. Tuş simgeleri

Kendi UI'ında tuş göstereceksen yazıyı elle yazma: `ControlGlyphs.For(PromptAction.FireStove)` son kullanılan cihaza göre doğru simgeyi verir (klavye / Xbox / PlayStation / Switch). Değişimi dinlemek için `GameInput.SchemeChanged`. İkon (sprite) kullanmak istersen aynı olaya bağlanıp kendi sprite'ını seçebilirsin; düzen `GameInput.LastScheme`'de.

## 9. Kasaba haritası (Unity'de birleştirme, 13.11)

**Araç:** `Assets/_Project/Art/Editor/HaritaYerlestir.cs` → menü **BilgiAdasi → Sanat → Haritayı açık sahneye yerleştir**. Kök `Harita_Sanat`; atölye yoksa onu da kurar (`Atolye_Sanat`). Tekrar çalıştırılabilir (eski kökü silip yeniden kurar).
- **Koordinat:** Blender (x, y, z) → Unity (−x, z, −y). Haritada atölye Blender (0, 12)'dedir; atölye Unity'de orijinde kaldığı için harita kökü (0, 0, +12)'ye konur. Meydan Unity (0, 0, 20) civarı; atölye kapısının önü (1,4; 0; 6).
- **Köyler:** `Neighborhood_Denklestirme` / `Neighborhood_TepkimeTurleri` kökleri `NeighborhoodView` taşır (id: `denklestirme`, `tepkime_turleri`); `Before`, `After`, `Parts/Part_1…5` adlarla kurulur (önekler: `_Once_` → Before, `_Sonra_` → After, `_P<n>_` → Part_n). Town sahnesinde Mehmet'in Neighborhood prefab'ı varsa bu çocuklar onun altına taşınır.
- **Kilitli bölgeler:** `LockedZone_Hesaplamalar`, `_Tepkimeler`, `_Matematik`, `_Turkce` → BoxCollider + `LockedZone` (unlockedBy boş = hep kilitli).
- **Çarpışma:** arazi, meydan döşemesi, bütün yapılar, köprüler, kapılar MeshCollider; ağaç/kavak/çam/meyve gövde kapsülü; kayalar dışbükey mesh; çimen, çiçek, saz, ekin, yol şeritleri çarpışmasız. Atölye duvar/zemin/mobilyası da artık MeshCollider alır (`SanatIceAktarma.cs`).
- **Doğa:** `Harita_Doga.fbx` türleri `Art/Harita/Prefablar/SM_<Tür>.prefab` olarak kaydedilir (LODGroup; ağaçta LOD0/LOD1, çimen ~35 m'de kesilir); `Harita_Yerler.fbx` noktalarına ~2.000 kopya, Blender'daki konum/yükseklik **aynen** (Unity ayrıca oturtmaz, §9.2).
- **Araçlar:** Traktör doğuya bakar (Blender 44,6, 14,8), 6 alet ahır önünde, Bereket Tarlası ve Ormancı Korusu kökleri — hepsi Harita.fbx'teki `Konum_*` işaretlerinden birebir yerleşir; bisiklet atölye kapısının doğusunda (park yerinin yanı). Araçlarda BoxCollider.
- **Test sahnesi:** `Assets/_Project/Scenes/Deneme_Mustafa_Harita.unity` (Mustafa'nın; Town'a dokunulmadı).

**Test sonucu (Play modu, gerçek PlayerRig + CharacterController):**
- Kapı → meydan → batı patikası → tahta köprü → doğu patikası → Denkleştirme sokağı → tarla kapısı: 87 m, takılma yok (en büyük basamak 0,23 m). Meydan → Tepkime Türleri → güvercin kulesi: 52 m. Meydan → Matematik kapısı: 100 m. Meydan → Türkçe kapısı: 92 m. Hepsi takılmasız.
- Kilitli kapılar (Tepkimeler, Hesaplamalar) ve onarılmamış taş köprü geçişi keser; `SetProgress(4)` sonrası taş köprüden geçilir.
- Başlangıçta köy parçaları kapalı, `Before` açık (NeighborhoodView doğru çalışıyor). Konsolda hata/uyarı yok.
- Doğuş noktasında ~250 bin üçgen (gölge geçişleri dahil), ~1.090 çizim çağrısı, 17 SetPass.

**Mehmet için (dokunmadım, öneri):**
1. `PlayerRig` kamerasında **Post Processing** kapalı, sahnede Volume yok → ton eşleme olmadığı için yeşiller fazla doygun. 09 §7 hedefi: Neutral tonlama + hafif bloom (`Assets/Settings/SampleSceneProfile.asset` hazır).
2. URP `PC_RPAsset`: 4 gölge kademesi × 50 m. Gölge düşüren üçgenler 4 kez çizilir; 2 kademe önerilir.
3. Atölye kapısı: `Kapi_Rig` kökü artık menteşede, kanatta BoxCollider var; açma için `animator.Play("Kapi_Ac")` (etkileşim kodu).
4. `Tabela_Yazi_*` boş objeleri TMP yazıları içindir (font seçilince).

**Mehmet'in yanıtı (13.11, uygulandı):**
1. Post processing: `PlayerRig` kamerasında açık; PlayerRig'in çocuğu `PostProcessVolume` (global, `SampleSceneProfile`: Neutral tonlama, bloom 0,25, vinyet 0,2). Oyuncuyla geldiği için her sahnede geçerli; sahneye ayrıca Volume koyma. Menü: **Kurulum/10**.
2. `PC_RPAsset` gölge kademesi 4 → 2 (ilk kademe %25 = 12,5 m).
3. Kapı: `DoorStation`'da `doorAnimator` alanı (Kapi_Rig kökündeki Animator). E'ye basınca `Kapi_Ac`, konuşma bitince `Kapi_Kapat`. Kanadın collider'ına bakınca da kapı çalışır (çalışırken `InteractableProxy` eklenir; prefab'a dokunulmaz). Town'a atölye sanatı girince menü **Kurulum/12** bağlar.
4. Bisiklet: `Bisiklet.fbx` artık `Bicycle.prefab` → `Visual/Model_Bisiklet` (küp yer tutucu silindi). Gidon/teker/pedal `BicycleRig`'e bağlı (pedal ön tekerde, ön 0,39 / arka 0,30 m). Menü: **Kurulum/11** (model güncellenince yeniden çalıştır). **Mustafa'dan istek:** `HaritaYerlestir.cs`'deki sabit `Bisiklet` (BoxCollider'lı) artık gerekmiyor; Town'da o noktaya (Blender 3,6; 5,6, doğuya bakar) `Bicycle.prefab` konacak. Haritadakini kaldırırsan tek bisiklet kalır.

### 9.1 Harita denetimi (13.11 akşam)

**Araç:** `ArtSource/Blender/Script/harita_denetim.py` (arka planda, canlı Blender'a dokunmaz). Her mesh'i ayrık parçalarına (ada) böler; araziye, meydana, yollara, dereye, bina gövdelerine göre ölçer. Yeniden kurulum: `harita_kur.py` (≈30 sn, bütün harita FBX'lerini dışa aktarır).
```
blender -b ArtSource/Blender/harita.blend --python ArtSource/Blender/Script/harita_kur.py
blender -b ArtSource/Blender/harita.blend --python ArtSource/Blender/Script/harita_denetim.py -- rapor.txt
```
İlk tarama 198 bulgu → düzeltmelerden sonra 24 (kalanlar bilerek: kemer flamaları ve kule tepelikleri "asılı", değirmen kanadının göbeği gövdede, köy zemininde ≤ 3 mm tek nokta).

**Düzeltilenler (kaynak scriptlerde):**
- Taş duvarlar ve mil taşları meydan döşemesinin içinden başlıyordu → R 11'in dışına alındı; duvar kapak taşları 5 cm havadaydı → oturtuldu.
- Dere iki uçta tepeye tırmanıyordu (su yokuş yukarı) → kuzeyde vadi basamağından **çağlayan + havuz**, güneyde **sazlık gölet**te biter (`GOLLER`, su seviyesi arazi mesh'inin kıyısından). Tepelerde kıyıdan 0,9 m havada kalan su şeridi gitti.
- `Yol_Tarla` Bereket Tarlası'nın çit köşesine çıkıyordu → güney giriş kapısına (60, 15,5) varır. Kapı levhası 1,45 m'deydi (oyuncu geçemiyordu) → 2,9 m.
- `Yol_Kule` Tepkimeler yoluna paralel ikinci yol gibiydi → kısa sapak.
- Denkleştirme 2. ev yolun ve tarlanın üstündeydi → fırının yanına (59,4, 2,9); 3. tarla Bereket kapısının önündeydi → Hesaplamalar yolunun doğusuna; araba ev içindeydi, odun yığınları iki evin duvarındaydı, ilk fener köprü patikasının ortasındaydı, 6 ve 2/4 numaralı tabelalar yolun üstündeydi → hepsi dışarı.
- Tepkime 3. ev loncaya yapışıktı (çatılar iç içe) → 1,6 m batıya.
- Ormancı Korusu'nun genç ağacı derenin içindeydi → kıyının dışına (kurdele adları aynı: `Koru_Kurdele_Koru_35/_46/_Kes`).
- Köy zeminleri seyrek halkalıydı, arazi içinden çıkıyordu (z-fighting) → sık halka; yol şeridinin (0,035) altında kalır.
- Kızaktaki üst kütük havadaydı; saman yığını tarla çitinin içindeydi; Türkçe kapısında çalı duvara giriyordu; arka planda bina konumları güncellenmeden okunduğu için 79 ot öbeği bina içindeydi → hepsi düzeldi.

**Unity'de doğrulama:** 401 ağaç/kaya/çalı kopyası zemine oturuyor, hiçbiri bina/çit collider'ıyla iç içe değil. 9 güzergâhta oyuncu kapsülü (1,66 m × r 0,3) yarım metrede bir: atölye→meydan, meydan→geçici köprü→Denkleştirme→tarla kapısı, Hesaplamalar, Tepkime→kule, Matematik, Türkçe, taş köprü → **engel yok**.

### 9.2 Play testi + Blender–Unity eşitleme (13.11 gece)

**Play'de bulunan ve düzeltilen hatalar:**
- **Animasyonlu düzenekler Play'de 2 kat kayıyordu** (kapı kasanın 2 m dışında, kazan arka duvarın dışında): FBX kökünün konum eğrileri Blender'daki dünya/katalog konumunu taşıyordu, prefab sarmalayıcısı da aynı konumdaydı. `SanatIceAktarma.OnPostprocessAnimation` kök konum eğrilerini ilk anahtara göre göreli yapar (471 eğri). **Mehmet:** taşınabilir eşyalar (Alet_/Kulce_/Madde_/Malzeme_, 38 FBX) da aynı hatayı taşıyordu — raftaki eşya Play'de 14–18 m öteye kayacaktı; artık kaymaz. NPC'ler ve FPS kolları etkilenmiyordu.
- **Oyuncu haritadan düşüyordu** (kaya basamağına zıplayıp tepeden arazinin kenarına; 72 yönün 72'si): `COL_Sinir_Duvari` (harita.py `build_sinir`) — oyun alanını çevreleyen içi dolu görünmez duvar + Matematik/Türkçe boğazlarında kapıya kadar yan duvarlar. Unity'de `COL_` önekli obje: yalnız MeshCollider, renderer silinir. Test: 72 yönden 71'i duruyor; kalan Türkçe boğazı, kapıda biter.
- **Çakışmalar (titreme, delinme):** katmanlar artık arazi AĞINA oturur (`arazi_z` / `zemin_ust`: SM_Arazi'nin 2,5 m ızgara üçgenlemesinin aynısı). Sıra: arazi < köy zemini (+3 cm) < patika/kule sapağı (+4,5) < yan yollar (+6) < ana yollar (+7,5) < meydan levhaları (+8…10,5). Atölye tabanının altında arazide delik (arazi döşemeyle aynı yükseklikte titriyordu). Meydan levhaları birbirine ve bordüre binmez. Çıkmaz yol uçları sivrilip toprağa iner (kare uç kalmaz). Su kenarı kıyının altına iner. Unity çakışma taraması (0,4 m ızgara, yukarı bakan yüzeyler arası < 6 mm): kalanlar yalnız yol/zemin eteklerinin araziyle kesiştiği çizgi ve atölye çatısındaki görünmez kiriş temasları.
- **Dere:** kuzey ucu basamaktaki çağlayan şeridi Play'de yamaçta düz beyaz levha gibi duruyordu → dere basamağın dibindeki pınar havuzundan doğar (üç kaya kütlesi arkasında).
- **Traktör ve aletler:** arka çamurluk tek yüzlü, bağlantısız levhaydı → kalın bant + kenet + iç yan levha + destek; serpici, su tankı, biçer ve pulluğun kopuk parçaları (tekerlek, aks, çark mili, ayaklar) bağlandı; traktör ekiciden ve çitten 0,9 m uzakta.
- **Taş duvarlar** "masa dizisi" gibi görünüyordu → üç sıra irili ufaklı taş + ince kapak taşı (kuru taş tarla duvarı).

**Blender = Unity (karşılaştırma):** iki taraftaki bütün objeler sayısal olarak karşılaştırıldı. Unity kendi yerleştirme kodunda doğayı yeniden oturtuyor ve türe göre gömüyordu (kayalar 0,5–1,6 m fazla gömülü, köprü altındaki saz/çimen köprünün üstüne çıkıyordu). Artık:
- Doğa yüksekliği tek kaynaktan: Blender `harita_doga.yer_z` (arazi ağının kapladığı alandaki en alçak noktası, türe göre hafif gömülü). Unity bu değeri aynen kullanır, ayrıca oturtmaz.
- Traktör, 6 alet, Bereket Tarlası ve Ormancı Korusu kökleri Blender'daki `Konum_*` işaretlerinden (Harita.fbx içinde) yerleşir.
- **Doğa modelleri kaymıştı (en büyük Unity-yalnız hata):** `Harita_Doga.fbx`'te ağaç/çalı/çam/kavak/meyve modellerinin LOD parçaları (`_LOD0/_LOD1`) Blender'daki model rafının konumunu (x 200–238) taşıyordu. Unity'de 354 kopya kökünden 190–290 m uzakta çiziliyordu: köylerin üstünde uçan ağaçlar, sırtlarda havada ağaçlar, boşlukta görünmez gövde kapsülleri. Blender önizlemesi meshi doğrudan kullandığı için orada her şey doğru görünüyordu. Düzeltme: `harita_doga.doga_disa_aktar` (LOD parçalarının yerel dönüşümü sıfırlanır, sahne güncellenip yazılır; harita_detay'daki eski kopya kaldırıldı). Doğrulama: kaymış kopya 0, vadide uçan ağaç 0.
- Sonuç: 252 meshin sınır kutusu Blender'la 1 cm içinde aynı; 2.020 doğa kopyasının yüksekliği en fazla 0,1 mm farklı ve her biri kendi kökünün üstünde çiziliyor.

**7 ajanlı görsel QA (240 göz hizası Play karesi + 12 üstten görünüm, 74 bulgu) sonrası düzeltilenler:** uçan ağaçlar (yukarıdaki LOD hatası); Hesaplamalar/Tepkimeler kapısındaki sis öbekleri kapının önüne taşıp çitle iç içe "mavi kaya" gibi duruyordu → tamamen kapı arkasında; kilitli kapıların 12 m çevresinde kaya kütlesi yok; Bereket Tarlası'nın GD köşesine binen sebze tarlası doğuya; Tepkime kuyusu loncanın saçağından uzağa; güvercin kulesine giden 2 m'lik kama gibi sapak kaldırıldı (kule ana yolun kenarında); Denkleştirme'de 1. ev fırından, 3. ev giriş kemerinden, baharat tezgâhı çatılardan uzaklaştırıldı; doğu yolu taş köprü ayağının tam ucunda biter (0,7 m çim boşluk vardı); dere yatağının gri bandı daraltıldı, kıyı yamaçları çimenlendi (kıyı "su levhası" gibi görünüyordu); nadas parselleri otlu toprak renginde; gölet halkası dere ağzında şerit altına iner, renk aynı (dikiş yok); arazi ağının dış kenarında 16 m'lik sırt (batı tepesi dikey kesik gibi bitiyordu).

**Çarpışma eklenenler:** mil taşları, çamaşır direkleri, tarla çitleri (`SM_Denk_Tarla_Citleri`) ve bahçe çitleri (`SM_Bahce_Citleri` — toprak/ekinden ayrı obje), köy bitince çıkan sancak/bayrak direkleri, korudaki kütük ve odun destesi. Kapsül süpürme testinde mil taşı 7/7, bahçe çiti 8/8, taş duvar 12/12 durdurur.

**Mehmet için öneri:** oyuncu yine de bir yerden düşerse, belli bir yüksekliğin altına inince (ör. y < −20) doğuş noktasına geri koyan küçük bir güvenlik kodu.

## 11. NPC'ler (13.11 gece)

Kasabada 6 NPC dolaşıyor ve çalışıyor (ayrıntı: `ArtSource/Referans/npc.md` §4). Kurulum menüsü: **BilgiAdasi → Sanat → NPC'leri açık sahneye yerleştir**. Harita ve atölye kurulduktan sonra çalıştırılır.

- Kod Mustafa'nın alanında: `Assets/_Project/Art/NPC/Davranis/NpcAgent.cs`, `NpcProfile.cs` (namespace `BilgiAdasi.City`, Assembly-CSharp). Mehmet'in dosyalarında değişiklik yok; yalnızca şunlar kullanılıyor: `WorldInteractable` (varsayılan `TriggerAction` = Interact), `DialogueDirector.Play(satırlar, bitince, interactor)`, `DialogueLine`, layer `Interactable` (6).
- Sahnede bir `DialogueDirector` varsa onu kullanır (Town'da `NeighborhoodRunner` üzerindeki). Yoksa araç `NPC_Sanat` köküne bir tane ekler. Konuşma balonu `IDialogueView` ile gösterilir.
- **Mehmet:** Görev akışı bir NPC'yi kullanacaksa (ör. ulağın "atölyeye gel" çağrısı), NPC'nin `NpcProfile` metinleri değiştirilmez; köy akışı kendi konuşmasını `DialogueDirector`'la oynatır. NPC o sırada konuşma durumunda değilse yürümeye devam eder. Gerekirse `NpcAgent`'a bir "dur/devam" çağrısı eklenir; Mustafa'ya yaz.
- `Town` sahnesine henüz konmadı (Town'da harita yok). Harita Town'a geçince aynı menü bir kez çalıştırılır; rota doğrulaması geçmezse NPC yürümez, yerinde durur ve konsola nedeni yazılır.
