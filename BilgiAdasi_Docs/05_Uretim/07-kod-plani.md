# 07 — Kod Planı (Mehmet)

> Kaynak: `CLAUDE.md` + `01`–`06`. Çelişkide `CLAUDE.md` geçerlidir.
> Plan **sıra ve önceliğe** göre yürür; saat yok. Sabit tarihler yalnızca: Build 1 (P0 bitince), **Cmt 08.00 kod dondurma**, **Cmt 11.00 teslim**.
> Haritalar ve UI görselleri Mustafa Akbaş'ta. Bu plan yalnızca mekanik/kod içindir; sahne tarafı `03_Teknik/08-mustafa-entegrasyon.md`.

---

## 0. Kararlar

| # | Karar | Durum |
|---|---|---|
| K1 | Git: `github.com/Frappuccinott/Bilgi-Adasi`, Unity `.gitignore`, her faz sonu commit | ✅ |
| K2 | Cinemachine 3 (`com.unity.cinemachine` 3.1.7) | ✅ |
| K3 | ProBuilder kurulmaz; test yer tutucuları primitive | ✅ |
| K4 | Tek oyun sahnesi `Town` (atölye + şehir); harita = UI katmanı | ✅ onaylandı |
| K5 | Şablon örnekleri silinir, yerine `BilgiAdasiControls.inputactions` | ✅ |
| K6 | Product Name `Bilgi Adası` | ✅ |

## 0.1 Mimari ilke: mantık ↔ görünüm

- Denetleyiciler (`OrderController`, `DeskController`, …) **hiçbir Canvas'ı, objeyi, prefab'ı tanımaz.** Veriyi arayüz ya da `GameEvents` ile yayınlar.
- Her görsel parça bir arayüzdür: `IScaleView`, `IOrderCardView`, `IPaperView`, `IInteractPromptView`, `IHintBookView`, `IRealLifeCardView`, `IChalkboardView`, `IDialogueView`, `IMapView`.
- Mustafa'nın prefab'ı bu arayüzü uygulayan küçük bir view script'iyle bağlanır. Ben her arayüz için **ham TMP + düğmeli** bir `Placeholder…View` yazarım; `_Placeholder` altında durur.
- Inspector'da arayüz referansı için `MonoBehaviour` alanı + `OnValidate` kontrolü kullanılır (Unity arayüzü doğrudan serileştirmez).

---

## 1. Fazlar (sıra = öncelik)

Her faz sonunda: Play mode testi + commit. Süre daralırsa §2'deki sırayla kesilir.

### Faz 0 — Kurulum · P0
- K1–K6.
- `Assets/_Project/` klasör ağacı (`04` §2) + `_Placeholder/`.
- `BilgiAdasiControls.inputactions`: `Player`, `Station`, `UI` map'leri, `CLAUDE.md` §3 tablosundaki **tüm** aksiyonlar, iki şema (KeyboardMouse + Gamepad), stick deadzone 0.15. C# sınıfı üretilir.
- Sahneler (`Boot`, `Classroom`, `Town`) ve Build Settings: editör menüsü **BilgiAdasi → Kurulum**.

### Faz 1 — Çekirdek + kimya · P0
| Script | Namespace | Not |
|---|---|---|
| `GameEvents` | Core | `TaskCompleted`, `NeighborhoodCompleted`, `HintOpened`, `Failed` |
| `GameManager` | Core | Singleton, DontDestroyOnLoad |
| `SceneFlow` | Core | Singleton, kararma (görünüm arayüzle: `IScreenFader`) |
| `SaveSystem` + `SaveData` | Core | JSON → PlayerPrefs. HashSet/Dictionary JsonUtility ile serileşmez → List |
| `ElementData`, `SpeciesData`, `ReactionData` | Chemistry | SO |
| `AtomMath` | Chemistry | **Saf C#** (`System.Math`), `Count`, `Equal`, `Gcd`, `IsMinimal`, `IsBalanced`, `RequiredForPurchase` |
| `FormulaFormatter` | Chemistry | Denklem metni: `<sub>`, 1 katsayısı gizli (tahta + kâğıt ortak) |
| `AtomMathTests` | Tests (EditMode) | 8 tepkime dengede, `IsMinimal(4,2,4) == false`, biçimleyici |

### Faz 2 — Oyuncu + istasyon modu + test sahnesi · P0
| Script | Not |
|---|---|
| `PlayerMovementSettings` (SO) | `CLAUDE.md` §4.2 değerleri |
| `PlayerMotor` | CharacterController, vektörel hızlanma/yavaşlama, %30 havada kontrol, −20 yerçekimi, `allowJump=false` |
| `PlayerLook` | Fare ham, gamepad eğri + ivme, ±80°, roll yok, FOV |
| `HeadBob` | ≤0.025 m, kapatılabilir |
| `PlayerInteractor` + `IInteractable` | 2.2 m raycast; ipucu `IInteractPromptView`'e gider |
| `InputDeviceTracker` | Son cihaz → ipucu ikonu (`E` ↔ `Ⓧ`) |
| `InputMapSwitcher` | Aynı anda tek map |
| `StationBase` | Interact → Station map, Cinemachine 0.5 sn blend, imleç; Exit → geri |

**Test sahnesi + sahne bağlantı noktaları** (greybox şehir Mustafa'da):
- `Town` içinde `_Placeholder` kökü: düz zemin + birkaç küp (kazan, masa, kapı yerleri), ham TMP crosshair/ipucu.
- Bağlantı noktası bileşenleri: `PlayerSpawn`, `StationAnchor`, `NeighborhoodView` (önce/sonra kökleri), `LockedZone`. Kurallar `08-mustafa-entegrasyon.md`.

### Faz 3 — Sipariş mekaniği · P0 (en kritik)
| Script | Not |
|---|---|
| `TaskData`, `OrderTask`, `OrderStep` (SO) | `04` §3 |
| `CauldronStation : StationBase` | |
| `OrderController` | Durum makinesi `04` §5; görünüme yalnızca arayüzle konuşur |
| `Shelf`, `JarView` | Sürükle-bırak (fare + sanal imleç ortak `Point`/`Click`), mühür notu + titreme |
| `Cauldron` | Tür başına `−/+`, 0'da rafa dönüş |
| `IScaleView` | Element başına sayaç, `H 4 / 4`, denge durumu |
| `IOrderCardView` | Sipariş kartı |
| `Stove` | Dengede otomatik yanar; `FireStove` |
| Köpürme | Fark atomları + `failCount`, 2'de defter parlar |
| `IChalkboardView` | `FormulaFormatter` çıktısı |

### Faz 4 — Köy akışı + veri · P0
- `NeighborhoodData`, `NeighborhoodRunner`, `DoorStation` (`IDialogueView`)
- Görev 4 zinciri, Görev 5 `Purchase` + `SupplyChest` + bütçe
- Denkleştirme verisi (`05` §1–2)
- **Geliştirici kısayolları** (demo + test için öne çekildi)
- **🔨 Build 1** — Denkleştirme uçtan uca oynanıyor

### Faz 5 — Masa mekaniği · P1
- `DeskStation`, `DeskController`, zarf, `IPaperView`
- `QuestionData` alt tipleri (ayrı SO) + soru mantıkları: `MultipleChoice`, `TrueFalse`, `Classify`, `FillBlank`, `BalanceEquation`
- Lonca Sınavı: rastgele 4, yanlış soru değişir
- Tepkime Türleri verisi (`05` §3)

### Faz 6 — Çerçeve · P1
- İpucu defteri (`IHintBookView`), "Gerçek hayatta" kartı (`IRealLifeCardView`)
- `PlayerGrowth` (§7)
- Harita mantığı (`IMapView`): köy durumu + 5 nokta
- Gamepad sanal imleç (`VirtualMouseInput`)
- **Emeği Geçenler** (lisans kuralı için zorunlu)

### Faz 7 — Cila · P2
- Sınıf açılış/kapanış, köy canlanma, ses (CC0), hata düzeltme
- **Cmt 08.00 kod dondurma** → son build, demo provası, yedek ekran kaydı

---

## 2. Kesme sırası (geride kalırsak, önce bu gider)

1. Sınıf ara sahnesi → tek metin ekranı
2. Köy canlanma → tek nesne aç/kapa
3. Güvercin animasyonu → zarf masada belirir
4. Lonca Sınavı rastgeleliği → sabit 4 soru
5. Sanal imleç → gamepad'de istasyon içinde D-pad ile odak (UI Navigation)
6. **Kesilmez:** sipariş mekaniği, mühür, tahta, iki köy uçtan uca, Emeği Geçenler

---

## 3. Dokümanlarda düzeltilen çelişkiler

| Doküman | Önce | Şimdi | Durum |
|---|---|---|---|
| `01` §3, §5 | Harita tıklamalı, oyuncu yürümez | Oyuncu yürür; harita bilgi ekranı | ✅ |
| `04` §1 | `Map` + `Workshop` ayrı sahne | Tek `Town` sahnesi | ✅ |
| `04` §2 | Namespace'te `Player`, `City` yok | `CLAUDE.md` §2 listesi | ✅ |
| `04` §4 | `Mathf.Abs` | `System.Math.Abs` | ✅ |
| `04` §7 | `grade`, `completedNeighborhoods` yok | Eklendi | ✅ |
| `06` | FPS, istasyon, büyüme, roller yok | Eklendi | ✅ |
| `LICENSES.md` | URP, Input System, Cinemachine yok | Eklendi | ✅ |

---

## 4. Her faz sonu kontrolü

- [ ] Konsolda kırmızı hata yok
- [ ] Yeni aksiyonların klavye **ve** gamepad bağlaması var, `CLAUDE.md` §3 tablosu güncel
- [ ] `Update` içinde `Find`/`GetComponent`/LINQ/`new` yok
- [ ] Denetleyici hiçbir Canvas/objeye doğrudan bağlı değil
- [ ] Oyuncuya görünen metin SO'da
- [ ] Claude'un yazdığı önemli sistem LICENSES.md beyanına eklendi
- [ ] Commit atıldı
