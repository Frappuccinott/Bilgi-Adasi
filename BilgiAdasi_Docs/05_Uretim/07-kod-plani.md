# 07 — Kod Planı (Mehmet)

> Kaynak: `CLAUDE.md` + `01`–`06`. Çelişkide `CLAUDE.md` geçerlidir.
> Başlangıç: Cuma 13.11, öğlen · Kod dondurma: **Cmt 08.00** · Teslim: **Cmt 11.00**
> Projenin şu anki durumu: Boş URP şablonu. `Assets/_Project/` yok, Cinemachine ve ProBuilder kurulu değil, Input System (New) açık.

---

## 0. Başlamadan önce kararlar (Mehmet onaylar)

| # | Karar | Öneri |
|---|---|---|
| K1 | Proje git'te değil. 24 saatlik çalışmada geri dönüş noktası lazım. | `git init` + Unity `.gitignore`, her faz sonunda commit |
| K2 | **Cinemachine 3** kurulu değil (`CLAUDE.md` §2 bunu şart koşuyor). | `com.unity.cinemachine` 3.x kurulsun. Unity paketi, LICENSES'a satır eklenir |
| K3 | Greybox için ProBuilder kurulu değil. | Kurmadan **primitive** (Cube/Plane) ile greybox yapılsın; paket sayısı az kalır |
| K4 | Sahne yapısı: dokümandaki ayrı `Map` + `Workshop` sahneleri, yürünen şehir kararıyla (§6) uyuşmuyor. | `Boot` → `Classroom` → **`Town`** (atölye + şehir aynı sahnede). Harita = `Town` içinde UI katmanı |
| K5 | Örnek dosyalar: `SampleScene`, `TutorialInfo/`, `Readme.asset`, `InputSystem_Actions.inputactions` | Silinsin; yerine `BilgiAdasiControls.inputactions` gelsin |
| K6 | Product Name şu an `Bilgi-Adasi` | `Bilgi Adası` yapılsın (`04` §2) |

---

## 1. Fazlar ve zaman

Süre daralırsa **aşağıdan** kesilir. Her faz sonunda: Play mode testi + commit.

### Faz 0 — Kurulum · 12.00–12.45 · P0
- K1–K6 uygulanır.
- `Assets/_Project/` klasör ağacı (`04` §2 + `Player/`, `City/`, `Stations/`).
- `BilgiAdasiControls.inputactions`: `Player`, `Station`, `UI` map'leri, `CLAUDE.md` §3 tablosundaki **tüm** aksiyonlar, iki şema (KeyboardMouse + Gamepad), stick deadzone 0.15. *Generate C# Class* açık.
- Sahneler: `Boot`, `Classroom` (boş), `Town`. Build Settings sırası.

### Faz 1 — Çekirdek + kimya · 12.45–14.30 · P0
| Script | Namespace | Not |
|---|---|---|
| `GameEvents` | Core | `TaskCompleted`, `NeighborhoodCompleted`, `HintOpened`, `Failed` + `InputDeviceChanged` |
| `GameManager` | Core | Singleton, DontDestroyOnLoad |
| `SceneFlow` | Core | Singleton, 0.3 sn kararma |
| `SaveSystem` + `SaveData` | Core | JSON → PlayerPrefs. `grade`, `completedNeighborhoods`, `completedTasks`, `failCounts`, `hintsOpened`, `boardEquations` (HashSet/Dictionary JsonUtility ile serileşmez → List'e çevrilir) |
| `ElementData`, `SpeciesData`, `ReactionData` | Chemistry | SO |
| `AtomMath` | Chemistry | **Saf C#** (`Mathf` değil `System.Math`), `Count`, `Equal`, `Gcd`, `IsMinimal`, `RequiredForPurchase` |
| `AtomMathTests` | Tests (EditMode) | Tüm 8 tepkime dengede mi, `IsMinimal(4,2,4) == false` |
| Formül biçimleyici | Chemistry | Denklemi `<sub>` ile yazar, 1 katsayısını gizler (tahta + kâğıt ortak) |

### Faz 2 — Oyuncu + istasyon modu · 14.30–17.00 · P0
| Script | Not |
|---|---|
| `PlayerMovementSettings` (SO) | `CLAUDE.md` §4.2 değerleri |
| `PlayerMotor` | CharacterController, vektörel hızlanma/yavaşlama, %30 havada kontrol, −20 yerçekimi, `allowJump=false` |
| `PlayerLook` | Fare ham, gamepad eğri + ivme, ±80°, roll yok, FOV ayarı |
| `HeadBob` | ≤0.025 m, kapatılabilir |
| `PlayerInteractor` + `IInteractable` | Kamera merkezinden 2.2 m raycast, kontur, `[E]`/`Ⓐ` ipucu |
| `InputDeviceTracker` + `PromptIcon` | Son kullanılan cihaza göre ipucu ikonu |
| `InputMapSwitcher` | Aynı anda tek map açık |
| `StationBase` | Interact → Station map, Cinemachine 0.5 sn blend, imleç aç; Exit → geri dön |
| `Crosshair` | Ekran ortasında nokta |

Sahne: `Town` greybox: atölye odası (kazan, masa, kapı, tahta yerleri) + tek sokak + iki mahalle alanı + sis/bariyer. Uçtan uca yürüyüş ≤ 40 sn.

### Faz 3 — Sipariş mekaniği · 17.00–21.30 · P0 (en kritik)
| Script | Not |
|---|---|
| `TaskData`, `OrderTask`, `OrderStep` (SO) | `04` §3 |
| `CauldronStation : StationBase` | |
| `OrderController` | Durum makinesi `04` §5 |
| `Shelf`, `JarView` | Sürükle-bırak (fare + sanal imleç için ortak `Point`/`Click`), mühür notu + titreme |
| `Cauldron` | Tür başına `−/+`, 0'da rafa dönüş, `Increase/Decrease` aksiyonları |
| `Scale` + `AtomCounterView` | Element başına sıra, `H 4 / 4`, yeşil/kırmızı + harf |
| `Stove` | Dengede otomatik yanar; `FireStove` |
| Köpürme | Partikül + fark atomları yanıp söner, `failCount`, 2'de defter parlar |
| `Chalkboard` | Formül biçimleyiciyi kullanır |

### Faz 4 — Mahalle akışı + veri · 21.30–00.00 · P0
- `NeighborhoodData`, `NeighborhoodRunner` (5 görevi sırayla), `DoorStation` (sakin + diyalog balonu)
- Görev 4 zinciri (adım ürünü rafa düşer), Görev 5 `Purchase` + `SupplyChest` + bütçe
- Denkleştirme verisi: 5 element, 12 tür, 8 tepkime, 5 sipariş SO (`05` §1–2). İçerikçi girerse Mehmet yalnızca kontrol eder.
- **Geliştirici kısayolları** (P2'den buraya çekildi, demo ve test süresini kısaltır): görev atla, kayıt sıfırla.
- **🔨 Build 1 — gece yarısı.** Denkleştirme uçtan uca oynanıyor olmalı.

### Faz 5 — Masa mekaniği · 00.00–04.00 · P1
- `DeskStation`, `DeskController`, güvercin/zarf (basit), `PaperView`
- `QuestionData` alt tipleri (ayrı SO) + renderer'lar: `MultipleChoice`, `TrueFalse`, `Classify`, `FillBlank`, `BalanceEquation`
- Lonca Sınavı: rastgele 4, yanlış soru değişir
- Tepkime Türleri verisi (`05` §3)

### Faz 6 — Çerçeve · 04.00–06.30 · P1
- `HintBook` (iki tip ortak), `RealLifeCard`
- `PlayerGrowth` (§7): 1.66 → 1.68, tavan kontrolü, 2 sn geçiş. Ucuz ve sunumda vurgulanacak.
- Harita UI katmanı (`OpenMap`): mahalle durumu + 5 nokta
- **Gamepad sanal imleç** (`VirtualMouseInput`). Klavye/fare bağlamaları zaten var; bu yalnızca istasyonda gamepad ile tıklama.
- **Emeği Geçenler** (lisans kuralı için zorunlu)

### Faz 7 — Cila · 06.30–08.00 · P2
- Sınıf açılış/kapanış (2D, basit), mahalle canlanma parçaları, ses (CC0), hata düzeltme
- **08.00 kod dondurma.** Ardından son build, demo provası, yedek ekran kaydı.

---

## 2. Kesme sırası (geride kalırsak)

1. Sınıf ara sahnesi → tek metin ekranı
2. Mahalle canlanma → tek nesne aç/kapa
3. Güvercin animasyonu → zarf masada belirir
4. Lonca Sınavı rastgeleliği → sabit 4 soru
5. Sanal imleç → gamepad'de istasyon içinde D-pad ile odak (UI Navigation)
6. **Kesilmez:** sipariş mekaniği, mühür, tahta, iki mahalle uçtan uca, Emeği Geçenler

---

## 3. Dokümanlarda düzeltilecek çelişkiler

| Doküman | Şu an | `CLAUDE.md` kararı |
|---|---|---|
| `01` §3, §5 | Harita tıklamalı, oyuncu yürümez | Oyuncu yürür; harita bilgi ekranı |
| `04` §1 | `Map` + `Workshop` ayrı sahne | Öneri K4: tek `Town` sahnesi |
| `04` §2 | Namespace listesinde `Player`, `City` yok, `Map` var | `CLAUDE.md` §2 listesi |
| `04` §4 | `AtomMath` içinde `Mathf.Abs` | Saf C# kuralı → `Math.Abs` |
| `04` §7 | `SaveData` içinde `grade`, `completedNeighborhoods` yok | Büyüme için eklenir |
| `06` | FPS oyuncu, istasyon modu, büyüme yok | Faz 2 ve 6'daki gibi eklenir |
| `LICENSES.md` | Input System, URP, Cinemachine yok | Satırları eklenir |

---

## 4. Her faz sonu kontrolü

- [ ] Konsolda kırmızı hata yok
- [ ] Yeni aksiyonların klavye **ve** gamepad bağlaması var, `CLAUDE.md` §3 tablosu güncel
- [ ] `Update` içinde `Find`/`GetComponent`/LINQ/`new` yok
- [ ] Oyuncuya görünen metin SO'da
- [ ] Claude'un yazdığı önemli sistem LICENSES.md beyanına eklendi
- [ ] Commit atıldı
