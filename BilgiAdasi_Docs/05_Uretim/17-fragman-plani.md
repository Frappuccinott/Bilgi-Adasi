# 17 — Fragman Planı (After Effects)

Hedef: sunumda ve teslimde gösterilecek **60–75 sn** oynanış fragmanı. 1920×1080, 60 fps kayıt, 30 fps teslim (AE'de yavaşlatma payı için 60 çekilir).

Görsel kurallar `01_Tasarim/09-tema-ve-model-kurallari.md`'den gelir; bu dosya yalnızca fragmana özgü olanı ekler.

---

## 1. Referans araştırması — ne aldık

| Kaynak | Ne alıyoruz |
|---|---|
| Derek Lieu'nun trailer ilkeleri (aşağıdaki kaynaklar) | **Kanca → Tür → İçerik** yapısı. İlk 5 sn'de logo yok, hikâye yok, en çarpıcı oynanış anı. Enerji hep yüksek değil: iniş-çıkış. Müzik önce gelir, kurgu müziğe göre yapılır. HUD mümkünse gizlenir. |
| *Potion Craft* tanıtım fragmanları | Simya döngüsünü (malzeme → kazan → sonuç → müşteriye teslim) **tek bir sipariş üzerinden** anlatmak. Kâğıt/el yazması dilini UI'dan fragmana taşımak. |
| *Tiny Glade* fragmanları | Konuşma yok, metin çok az; **"tatmin eden an"** (oturan taş, açılan pencere) yakın planda ve yavaş. Küçük canlılık detayı (koyun) kanca olabilir. Gün ışığının kendisi bir oyuncu. |
| *Botany Manor* (Wholesome Direct) | Eğitsel/bulmaca türünü sakin tempoda ama net "ne yapıyorsun" cümleleriyle göstermek. Bilgi = oyunun kendisi. |
| *A Short Hike*, *Lil Gator Game* (bilinen fragmanlar) | Küçük ama dolu dünya hissi: tek bir kamera hareketinde bütün kasabanın görünmesi; hareket özgürlüğü (bizde bisiklet). |
| Kendi tema referansımız (`referans/tema-referans.webp`) | Renk ve ışık: öğle güneşi, sıcak çatılar ile soğuk gökyüzü karşıtlığı. Renk düzeltmesi buna göre. |

Kaynaklar:
- https://wnhub.io/news/analytics/item-237
- https://gameworldobserver.com/2022/09/22/common-game-trailer-mistkaes-indie-developers-steam
- https://www.pcgamer.com/uk/the-art-of-creating-the-perfect-first-game-trailer/
- https://presskit.gg/field-guides/game-trailers-screenshots-guide
- https://gematsu.com/2021/02/medieval-potion-shop-management-game-potion-craft-announced-for-pc
- https://www.techradar.com/gaming/pc-gaming/cozy-castle-builder-tiny-glade-just-got-a-new-ultra-satisfying-trailer-releasing-in-2024
- https://opencritic.com/game/16551/-/media

> Bu fragmanların görüntüsü, müziği ya da sesi **kullanılmaz**; yalnızca yapı ve tempo dersi alınır.

---

## 2. Akış (≈70 sn)

| # | Süre | Bölüm | Ne görünüyor | Ekran metni (Alegreya) |
|---|---|---|---|---|
| 1 | 0:00–0:04 | **Kanca** | Yakın plan: kazana son kavanoz dökülür → 3D terazi dengeye oturur → ocak yanar → ürün çıkar. Müzik ilk vuruşu burada. | — |
| 2 | 0:04–0:09 | Önerme | Kâğıt zemin, mürekkep yazı belirir. Arkada hafif kazan sesi. | *Kimya dersinde uyuyakaldın…* |
| 3 | 0:09–0:16 | Dünya | Kuş bakışı vadi uçuşu (MenuFlyover açısı) → aşağı inip atölyeye. | *…ve Bilgi Adası'nda uyandın.* |
| 4 | 0:16–0:22 | Tür | FPS: atölyeye giriş, Câbir Usta, raf ve kazan görünür. | *Câbir Usta'nın çırağı ol.* |
| 5 | 0:22–0:36 | **Çekirdek döngü (sipariş)** | Tezgâhta sakin sipariş verir → pazardan kavanoz → rafa/envantere → kazana döküş → terazi sallanır, dengesiz → köpürme (hata, komik an) → doğru oran → denge → ürün → tezgâhta teslim, akçe. Hızlı kesmeler. | *Tart. Denkleştir. Pişir.* |
| 6 | 0:36–0:44 | Masa görevi | Masa istasyonu: kâğıt üzerinde tepkime türü sorusu, doğru cevap, mühür basılır. | *Her köy bir konu.* |
| 7 | 0:44–0:52 | Keşif / nefes | Tempo düşer: bisikletle sokaktan iniş (ayakta pedal sallanması), yön tabelaları, pazar kalabalığı, uzakta sisli kilitli şehirler. | — |
| 8 | 0:52–0:58 | İlerleme | Harita ekranı (köy noktaları dolar), defter açılır, başarım bildirimi "Öğrenildi!", büyüme notu *"Biraz uzadın gibi…"* | — |
| 9 | 0:58–1:04 | Doruk | Müziğin en dolu yeri: 4–5 en güzel planın 0,5 sn'lik hızlı kesmesi, son kare kazanın parlaması. | — |
| 10 | 1:04–1:10 | Kapanış | Logo (Figma marka kimliğinden) kâğıt üzerine mühürle basılır. Altında tek satır. | *Kimya şehri açık. Diğerleri sisin ardında.* |

Tempo: 1 sakin → 5 hızlı → 7 yavaş → 9 en hızlı → 10 durur.

---

## 3. Çekim listesi

**Yol A = canlı oynanış** (OBS ya da Windows oyun çubuğu, Unity'de Game görünümü 1920×1080 ya da build).
**Yol B = sinematik plan** (Cinemachine kamera + kendi kare kare kayıt aracımız, HUD kapalı; bkz. §4).

| Plan | Sahne / konum | Yol | HUD | Not |
|---|---|---|---|---|
| P1 Kazan yakın plan | Harita → atölye, `CauldronStation` + `ScaleModel` | B | Kapalı | Kazan ve terazi aynı karede; 3 deneme: köpürme, denge, ürün. |
| P2 Vadi uçuşu | Harita, MenuFlyover açısı | B | Kapalı | 12 sn yavaş dönüş, iki ayrı yükseklik. |
| P3 Atölyeye iniş | Meydandan atölye kapısına | B | Kapalı | Dolly; kapı açılırken kesme. |
| P4 Usta | Atölye içi, Câbir Usta | A | Kapalı | Kısa diyalog balonu görünebilir. |
| P5 Sipariş | `ShopCounter` + `CustomerQueue` | A | Açık | Sakinin gelişi ve sipariş kartı. |
| P6 Pazar | `MarketStall` | A | Açık | Kavanozun envantere girişi, akçe düşüşü. |
| P7 Döküş + terazi | Kazan, FPS | A | Açık | Envanterden dökme; terazinin sallanması. |
| P8 Köpürme | Kazan, ocak | A | Kapalı | Bilerek yanlış oran. |
| P9 Teslim | Tezgâh | A | Açık | "Ben bunu istemedim" + doğru teslim, ikisi de çekilsin. |
| P10 Masa görevi | `DeskStation` | A | Açık | Tepkime Türleri, tur_1 ya da tur_2. |
| P11 Bisiklet | Sokak, `Bicycle` | A | Kapalı | Sprint ile ayakta pedal; kaldırım çıkışı. |
| P12 Kilitli şehirler | Harita kenarı, sis | B | Kapalı | Uzun odak, pus. |
| P13 Harita ekranı | M / Select | A | — | Köy noktaları. |
| P14 Defter + başarım | N, görev bitişi | A | Açık | "Öğrenildi!" bildirimi. |
| P15 Büyüme notu | Köy bitişi | A | Açık | DevJumpTask (F1–F5) ile köy bitirilip çekilir. |

Her plan için **en az 3 alım** ve baş/son 1'er sn boşluk. Dosya adı: `P07_dokus_alim2.mp4`.
Kayıt klasörü: `Kayitlar/` (repo dışında tutulur, `.gitignore`'a eklenir; büyük dosya).

Çekim öncesi:
- Kayıt temiz başlasın: DevResetSave (Ctrl+R), sonra gerekli göreve F1–F5.
- Kafa sallanması varsayılanda kalsın (FPS hissi), ama sinematik planlarda kapalı.
- Ses ayrı: oyun sesleri kayda girsin, müzik AE'de eklenir.

---

## 4. Unity'den kayıt — neyi kim yapabilir

| Yöntem | Ne için | Durum |
|---|---|---|
| Unity MCP `manage_camera screenshot` | Tek kare (kapak görseli, poster, sunum slaytı) | Claude yapabilir. **Video çekmez.** |
| Kendi kare kayıt aracımız (`Editor/TrailerCapture`, yazılacak) | Yol B planları: `Time.captureFramerate = 60` + her kare `ScreenCapture` ile PNG dizisi → AE PNG dizisini doğrudan içe alır | Claude yazar ve MCP ile Play'e girip çalıştırabilir. Paket yok, sıfırdan kod; yalnızca editörde, build'e girmez. |
| OBS (bilgisayarda kurulu) | Yol A canlı oynanış (kumandayla oynanırken) | İnsan oynar ve kaydeder; Claude oyunu sizin yerinize oynayamaz. |
| Unity Recorder paketi | Yol B'nin hazır hâli | Yeni paket (Unity Companion License) — CLAUDE.md gereği **sormadan eklenmez**; aracımız yetiyorsa gerek yok. |

Sinematik kameralar: Cinemachine 3 + Timeline (ikisi de projede) ile `_Trailer` adlı ayrı bir obje altında kurulur; oyun sahnesinin geri kalanına dokunulmaz, teslim build'inden önce kapatılır.

---

## 5. After Effects tasarım kuralları (marka kimliği)

**Fontlar** (projede var, SIL OFL): başlık **Alegreya Bold/Black**, alt metin **Alegreya Sans Medium**. Windows'a kurulmadan AE'de görünmez: `Assets/_Project/Fonts/*.ttf` → sağ tık → Yükle. AE'nin varsayılan fontu, Adobe Fonts'tan rastgele font kullanılmaz.

**Renkler** (yalnızca §3 paletinden):

| Kullanım | Renk |
|---|---|
| Metin kartı zemini | Kâğıt `#E2DBB1` / `#D6CCB2` |
| Metin | Mürekkep `#4A3427` |
| Logo mührü, vurgu | Mühür `#A2411E` |
| Tek vurgu parıltısı (yalnız doruk karesi) | Altın `#FFD23F` |
| Siyah yerine "boş" kare | Mürekkep gölge `#3E281B` |

**Geçişler ve efektler — izinli:** kâğıt kıvrılması/kayması, mürekkebin kâğıda yayılması (matte ile), mühür basılması (ölçek 110 → 100 + küçük sarsıntı), düz kesme, müzik vuruşunda kesme.

**Yasak (yapay zekâ/şablon görünümü):** mor-pembe ya da mavi-mor gradyan, neon/glow, lens flare, glitch/RGB kayma, cam efekti, parçacık patlaması, 3B dönen yazı, hazır AE/Envato şablonu, "yapay zekâ ile üretildi" hissi veren jenerik stok görüntü, yapay zekâyla üretilmiş müzik/ses/görsel/seslendirme. Saf siyah ve saf beyaz kullanılmaz.

**Metin:** cümle düzeninde (her şey büyük harf değil), Türkçe karakterler kontrol edilir (ğ ş ı İ), ekranda en fazla 6 kelime, her metin en az 2 sn kalır. Formül gösterilirse alt indisle (H₂O).

**Renk düzeltmesi:** Lumetri ile hafif sıcaklık; gölgeler maviye kayar (tema kuralı), doygunluk artırılmaz.

**Müzik:** insan yapımı, lisansı açık (CC0 ya da CC BY) ortaçağ/akustik bir parça; kaynağı ve lisansı `LICENSES.md`'ye yazılmadan kullanılmaz. Fragman kurgusu müziğe göre yapılır (önce müzik seçilir).

---

## 6. Sıra

1. Müzik seç + LICENSES.md satırı.
2. `TrailerCapture` aracı + `_Trailer` kameraları (P1, P2, P3, P12).
3. Canlı oynanış çekimi (P4–P15), kumanda ile — tuş ipuçları gamepad simgesiyle daha temiz görünür.
4. AE: önce metin kartları ve logo kapanışı (bağımsız), sonra müziğe göre kurgu.
5. Projektörde 3 m'den izle, metinler okunuyor mu.
