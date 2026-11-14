# 11 — UI / UX Planı

Kaynak: `01`, `02`, `03`, `04-gorev-plani`, `05`, `10`, `08` §4, `06-yapilacaklar` ve koddaki arayüzler (13.11). Görsel dil: `09` + Figma **"Bilgi Adası — Marka Kimliği"** (sayfalar: *Marka Kimliği*, *Ekranlar*). Bu dosya ekran listesini, akışı ve her ekranın kod tarafındaki karşılığını tek yerde tutar.

## 1. İlkeler

1. **Arayüz dünyanın parçası.** Kâğıt, ahşap tahta, mum mührü, ustanın defteri. Düz "uygulama kartı" ve cam efekti yok.
2. **Renk tek başına bilgi taşımaz.** Doğru = mühür (şekil), hata = köpük / mürekkep lekesi. Atomlarda harf + şekil kodu (H küre, O halka, C küp, N piramit, Ca altıgen).
3. **Vurgu tek yerde.** Altın (`#F7A800`) yalnızca odaktaki seçenek, dengedeki terazi, parlayan defter için.
4. **Projektör okunurluğu.** En küçük yazı 28 px (ipucu altyazısı 26 px'e kadar, yalnızca ikincil bilgi). 3 m testi.
5. **Her ekran iki cihazla çalışır.** Odak sırası (Navigate) her ekranda tanımlı; tuş ipuçları `GameInput.DeviceChanged` ile `E ↔ Ⓧ` değişir.
6. **Ceza yok, bağlam var.** Duraklatma ve haritada oyuncu "neredeyim, ne yapıyordum" sorusunun cevabını görür.
7. **Kimya terimi yalnızca etiket, terazi, tahta, kâğıt ve "Gerçek hayatta" kartında.** Sakinler gündelik konuşur.

## 2. Ekran akışı

```
AnaMenu ─ Devam Et ─→ Harita sahnesi (son görev anındaki otomatik kayıttan)
        ─ Yeni Oyun ─→ (kayıt varsa onay) → çıraklık açılış kartları (sınıf) → Harita sahnesi
        ─ Ayarlar ⇄
        ─ Emeği Geçenler ⇄
Harita (Player) ─ Esc/Start → Duraklatma ─ Devam / Başarımlar / Ders Notu / Ayarlar / ana menü
                ─ M/Tab/Select → Harita ekranı (bilgi + ilerleme)   [soru/konuşma açıkken açılmaz]
                ─ N/D-pad ↑ → Defter (Ustanın Notları | Keşiflerim | Ustalık)
                ─ E/Ⓧ → Kazan, raf, pazar tezgâhı, tarladaki kişiler (FPS) | Masa, Tahta (istasyon)
                ─ F/Ⓧ → Kapı, bisiklet, traktör
(14.11 güncel: Boot/Classroom/Town yok; kayıt yalnızca görev sonunda otomatik.)
İstasyon (Station) ─ P/Start → Duraklatma   ─ Esc/B → Player
Köy bitti → kısa kutlama + boy notu → (son köy) Kapanış → Son ekran (tahta, hata sayıları, Keşif Defteri) → Emeği Geçenler
```

## 3. Ekran listesi

Durum: **F** = Figma'da tasarlandı · **K** = kodda arayüz var · **—** = yok.

| # | Ekran | Öncelik | Arayüz (kod) | Durum | Not |
|---|---|---|---|---|---|
| 1 | Ana menü | P0* | `IMainMenuView` (öneri) | F | *Teslimde oyun bir ekranla açılmalı; yoksa Boot→Town. Ana menü sahnesi yok, `Boot` içinde UI katmanı önerilir. |
| 2 | Duraklatma (Ustanın defteri) | P0* | `IPauseView` (öneri) | F | Sağ sayfa: köy, n/5, aktif görev, hatırlatma. `GameInput.SetMode(UI)` + `Time.timeScale = 0`. |
| 3 | Ayarlar | P1 | `ISettingsView` (öneri) | F | Fare hassasiyeti, FOV 60–90, kafa sallanması, ses. `PlayerMovementSettings` + `PlayerLook.ApplyFieldOfView()` var. |
| 4 | Etkileşim ipucu + crosshair | P0 | `IInteractPromptView` | K, F | Crosshair ortada; ipucu kutusu (ad + tuş + eylem) **alt ortada**, alttan 150 px yukarıda. Placeholder bununla değiştirilir. |
| 5 | Kazan istasyonu (sipariş kartı, iki terazi, atom sayaçları, −/+, Ateşi yak, defter) | P0 | `IOrderCardView`, `IScaleView`, `IHintBookView` | F | Arayüz imzaları Mehmet'ten bekleniyor. |
| 6 | Konuşma balonu + portre (Usta, sakin) + 3 seçenek (Ustanın Sorgusu) | P0 / P1 | `IDialogueView` | F | |
| 7 | "Gerçek hayatta" kartı | P1 | `IRealLifeCardView` | F | Tek cümle, korkutmaz. |
| 8 | Masa kâğıdı (MultipleChoice, TrueFalse, Classify, FillBlank, BalanceEquation) | P1 | `IPaperView` | F (MC + DY) | Doğru = mühür, hata = leke. |
| 9 | Tahta (denklemler) | P0 | `IChalkboardView` | — | Dünya içi TMP; Figma'da yalnız yazı stili. |
| 10 | Harita (şehir → ilçe → köy, 5 nokta, sis) | P1 | `IMapView` | F | Hızlı seyahat P2. |
| 11 | Emeği Geçenler | P1 (zorunlu) | — | F | LICENSES.md tablosundan. |
| 12 | Boy notu "Biraz uzadın gibi…" / köy tamamlandı | P1 | `GameEvents.NeighborhoodCompleted` | F | 2,5 sn, oyunu durdurmaz. |
| 13 | Ekran kararması | P0 | `IScreenFader` | K | Kâğıt tonuna (`#3E281B`) kararır, saf siyah değil. |
| 14 | Altyazı (yol sohbeti) | P2 | `ISubtitleView` | — | |
| 15 | Pazar / kese (akçe) | P2 | `IShopView` | — | `coins` kayıtta yok. |
| 16 | Keşif Defteri (HUD sırtı, açılış animasyonu, Keşiflerim sayfası, yeni kayıt) | P1 | `IDiscoveryBookView` (öneri) | F | §5b. Figma prototipi: "Defter açılışı" akışı. |
| 17 | Başarı ağacı (ders sekmeleri; Kimya ağacı, Mat/Fizik/Bio madalyon + saha notları) | P1 | `IAchievementsView` (öneri) | F | §5b. Fizik: Makine Ustası, çark, 1/10 (D-2; D-4 sıradaki; Tepkime Türleri'nde fizik notu yok). Biyoloji: Hayat Ustası, yaprak, 3/10 (D-1…D-3; D-4, D-5, T-2…T-5). |
| 18 | Atom Görüşü, Hata Dedektifi, Saha sorusu | P2 / Sürüm 2 | `IAtomViewer`, `ISahaSoruView` | — | `04-gorev-plani` §10. |

## 4. Mehmet'ten istenenler (kod tarafı)

Arayüzler `Scripts/` altında olduğu için Mehmet yazar; view'ları Mustafa bağlar.

- `IMainMenuView`: `Show(bool hasSave, SaveSummary s)`, `event Action Continue, NewGame, Settings, Credits, Quit`.
- `IPauseView`: `Show(PauseContext c)`, `Hide()`, `event Action Resume, OpenMap, OpenSettings, SaveAndQuit`. `PauseContext`: köy adı, n/5, aktif görev başlığı + hedef formül, 1–2 hatırlatma (SO'dan), boy.
- `ISettingsView`: `Show(SettingsData)`, `event Action<SettingsData> Changed`. Ayar kaydı `PlayerPrefs` (SaveData'dan ayrı).
- `Pause` ve `OpenMap` aksiyonlarını dinleyen bir `UIFlow` (ya da `GameManager` içinde). Şu an hiçbir kod dinlemiyor.
- `IOrderCardView`, `IScaleView`, `IHintBookView`, `IDialogueView`, `IPaperView`, `IRealLifeCardView`, `IMapView` imzaları. Figma'daki ekranlar hangi verinin gösterildiğini belirler (§5).
- Oyuncuya görünen metinler (menü etiketleri, hatırlatmalar) ScriptableObject'te (CLAUDE.md §8).

## 5. Ekran kararları (Figma ile birlikte okunur)

> **v2 (13.11):** İlk tur (Figma sayfası "Ekranlar (v1 — reddedildi)") düz vektör arka plan, kalın kontur ve sert gölge yüzünden reddedildi. Geçerli tasarım **"Ekranlar (v2)"** sayfasıdır; kurallar ve referanslar `ArtSource/Referans/oyun_ui.md`. Özet: arka plan gerçek oyun render'ı (`ArtSource/Marka/oyun_goruntu/hd_*.png`), okunurluk kenar geçişiyle (scrim); menüler kutusuz metin (odak = altın + bakır baklava + fırça çizgisi); paneller ince çift çizgili parşömen ya da yarı saydam koyu plaka; yumuşak gölge; tuş simgeleri `Glyph/Klavye`, `Glyph/Gamepad` bileşenleri. Aşağıdaki maddeler v2 ile günceldir.

**Ana menü:** Gerçek oyun sahnesi (atölye sağda), soldan koyulaşan geçiş, solda logo ve metin menü. "Devam Et" yalnızca kayıt varsa görünür ve ilk odaktır; hemen altında tek satır kayıt özeti (köy, 5 nokta, n/5, boy). "Yeni Oyun" kayıt varsa onay ister. Altta güvenlik notu (`01` §7) ve cihaza göre tuş ipuçları.

**Duraklatma:** Oyun karartılır (`#1F140D` %72 + vinyet, bulanıklık yok). Solda konum başlığı ve metin menü; sağda çift çizgili, mühürlü görev parşömeni (sakin, istek, adımlar, köy ilerlemesi). Esc / Start ile kapanır.

**Kazan istasyonu:** Kamera kazana bakar; sahnedeki gerçek iki terazi kullanılır, UI onları yeniden çizmez. Sol üst: kompakt sipariş parşömeni. Her terazinin üstünde dünyaya bağlı koyu plaka (Girenler / Hedef, atom simgeleri, "+2 fazla"). Alt orta: element sayaçları (`O 4 / 2`); eşit = yeşil kontur + tik, eşit değil = kesikli turuncu kontur + `≠` (renk yanında şekil). Sağda kazandaki türlerin `−/+` satırları. Sağ altta "Ateşi yak" (F / Ⓨ), sol altta defter (H / Ⓧ; 2 hatadan sonra parlar). Hata metni çıkmaz.

**Masa kâğıdı:** Parşömen, başlık mektup/defter kılıfına göre. Şıklar mürekkep kutuları; seçili = altın kontur, doğru = mühür, yanlış = leke + sallanma.

**Harita:** Ada yukarıdan, ustanın masasında açık bir harita. Kimya şehri renkli, diğer 5 şehir açık mavi pusta. Etkileşim ilçesinde 4 köy düğümü, her birinin altında 5 nokta ve durum etiketi.

**Ayarlar:** Kâğıt üstünde sekmeler: Kontroller / Görüntü / Ses. Kaydırıcılar ahşap ray + altın topuz. Değişiklik anında uygulanır, "Varsayılana dön" var.

## 5b. Keşif Defteri ve Başarı ağacı (13.11)

Kaynak: `04-gorev-plani` §2.1, §2.5, §3, §8, §10. Figma sayfası: **"Keşif Defteri ve Başarılar"**.

### Keşif Defteri: oyuncunun hep yanında
- **Yeri:** Seviye 0'da usta verir; o andan sonra oyuncunun belinde. HUD'da sol altta defterin sırtı ve tuş ipucu durur (her ekranda değil, yalnızca yeni kayıt varken ya da 3 sn hareketsiz kalınca belirginleşir).
- **Açılış canlandırması (0,7 sn):** 0,00 tuş → 0,20 defter alttan, hafif eğik yükselir; oyun %55'e kadar kararır → 0,45 kapak açılır (sayfa çevirme) → 0,70 sayfalar oturur, yazı belirir. Kapanış tersi, 0,40 sn. Açıkken oyun durur (`InputMode.UI`, `Time.timeScale = 0`).
- **Yeni kayıt:** Görev bitince sağ altta kısa bildirim ("Deftere yeni sayfa: K-1"), defter sırtı bir kez parlar. Defter o sayfada açılır; kalem cümleyi ~1,5 sn'de yazar (yazı soldan sağa açılır, sonunda mürekkep kurur).
- **Düzen:** Üstte iki ayraç: *Ustanın Notları* (ipucu) ve *Keşiflerim*. Sayfa kenarında ders şeritleri (Kimya, Matematik, Fizik, Biyoloji, Türkçe, Sosyal; kilitli dersin şeridi soluk). Sol sayfa: seçili dersin konuları ve kayıt listesi (K-numarası, görev adı). Sağ sayfa: seçili kayıt; görev adı, oyuncunun yazdığı cümle (Alegreya Italic, el yazısı hissi), denklem, "nerede işe yaradı" (ipucu/kanıt bağlantıları) ve o görevin öteki derslerdeki saha notları.
- **Ders mantığı:** Her kayıt bir dersin bir konusuna bağlıdır (Kimya: Etkileşim > Denkleştirme). Saha notları (Mat, Bio, Fizik) kendi dersinin şeridinde de listelenir; aynı görev birden çok derste görünebilir.
- **Girdi önerisi (Mehmet'e; CLAUDE.md §3'e eklenmeden önce konuşulur):** Defteri aç/kapa — Player map: **N** / gamepad **D-pad ↑**. Station map'te mevcut **H / X** aynı defteri açar. Defter içinde: ayraç **Q / E** · LB / RB; ders şeridi **↑ ↓** · D-pad; kayıt seç **Enter** · A; kapat **N / Esc** · B.
- **Arayüz:** `IDiscoveryBookView` (öneri): `Open(BookTab tab, string subjectId, string entryId)`, `Close()`, `PlayWriteEntry(string entryId)`, `event Action<string> EntrySelected`. Veri `DiscoveryData` SO'su (04 §9) + `subject`, `topic` alanları.

### Başarı ağacı: ders ders görevler
- **Yeri:** Duraklatma menüsünde "Başarılar" ve defterde üçüncü ayraç *Ustalık*. Tam ekran; oyun karartılır.
- **Düzen:** Üstte ders sekmeleri. Kimya sekmesi ağaçtır: şehir → ilçe (ünite) → köy (konu) → 5 görev düğümü. Düğüm durumları: tamamlandı (mühürlü, dolu), sıradaki (altın halka), açık (boş halka), kilitli (kesikli, pus). Öteki dersler (Mat, Fizik, Bio): kapı madalyonu (10 çentik) + o derse saha notu veren görevlerin listesi; Türkçe ve Sosyal: "henüz saha notu yok", kilitli.
- **Odak düğümü:** Sağ panelde görev adı, sakin, öğrenilen (K kaydı), hata sayısı (puan değil, `GetFailCount`), "Defterde aç" bağlantısı.
- **Kural:** Puan, yıldız, sıralama yok (01 §7: rekabet/kumar dili yok). Başarı = mühür.

## 6. Yapılış sırası (14 Kasım teslimine göre)

1. P0: etkileşim ipucu view'ı, kazan istasyonu UI, diyalog balonu, fader. Bunlar oynanabilir demoyu taşır.
2. P0*: ana menü + duraklatma (Mehmet arayüzleri eklerse; yoksa Boot→Town kalır, Esc yalnız istasyondan çıkar).
3. P1: masa kâğıdı, harita, Gerçek hayatta kartı, Emeği Geçenler, boy notu, ayarlar.
4. P2: altyazı, pazar, Keşif Defteri.
