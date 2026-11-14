# 06 — Yapılacaklar

Öncelik sırası yukarıdan aşağı. Süre daralırsa **aşağıdan** kesilir. P0 olmadan oyun yok.

## Roller

| Rol | Sorumlu | Kapsam |
|---|---|---|
| Mekanik / kod | Mehmet | Bu listedeki `[KOD]` işleri, build |
| Harita / sahne + UI | Mustafa Akbaş | Şehir/köy sahnesi (greybox dahil), atölye yerleşimi; HUD, kâğıt, kart, sayaç, tahta, harita ekranı, menüler, fontlar |
| Modelleme | … | Kazan, teraziler, kavanoz, sakinler |
| İçerik / sunum | … | `05` metinlerinin kontrolü, ders kitabı doğrulaması, sunum, `05_Uretim/LICENSES.md` |

## P0 — Çekirdek (Cuma akşamına kadar)

- [x] `[KOD]` Proje iskeleti: klasörler, sahneler, `SceneFlow`, `GameEvents`, `SaveSystem`
- [x] `[KOD]` Kimya verisi: `ElementData`, `SpeciesData`, `ReactionData` + `AtomMath` + **birim testleri** (R_propane dengede mi, IsMinimal)
- [x] `[KOD]` FPS oyuncu (CharacterController, bakış, etkileşim) + istasyon modu (Cinemachine geçişi)
- [x] `[KOD]` Test sahnesi (`_Placeholder`) + sahne bağlantı noktaları (`08-mustafa-entegrasyon.md`)
- [x] `[KOD]` Sipariş: raf → kazan sürükleme, `−/+`, iki terazi + sayaçlar, ateşi yak → tepkime ya da köpürme
- [x] `[KOD]` Köpürme + yanıp sönen atomlar + `failCount`
- [x] `[KOD]` Mühür notu
- [x] `[KOD]` Tahta: denklem metni üretimi (alt indis, 1 katsayısı gizli)
- [x] `[KOD]` `NeighborhoodRunner`: 5 görevi sırayla çalıştır
- [x] `[İÇERİK]` Denkleştirme köyünün 5 siparişi SO olarak girildi
- [x] `[KOD]` Görev 4 zinciri + Görev 5 `Purchase` modu
- [x] **Gece yarısı ilk build**

## P1 — İkinci görev tipi ve çerçeve

- [x] `[KOD]` Masa: kamera geçişi, güvercin/zarf, `PaperView` — 13.11, Mustafa dalı (Kurulum 16)
- [x] `[KOD]` Soru render'ları: `MultipleChoice`, `TrueFalse`, `Classify`, `FillBlank` — 13.11, Mustafa dalı (Kurulum 16)
- [x] `[KOD]` `BalanceEquationQ` (ReactionData'dan otomatik) — 13.11, Mustafa dalı (Kurulum 16)
- [x] `[KOD]` Lonca Sınavı: rastgele çekme + yanlışta soru değişimi — 13.11, Mustafa dalı (Kurulum 16)
- [x] `[İÇERİK]` Tepkime Türleri köyünün 5 görevi SO olarak girildi — `DeskDataBuilder`
- [x] `[KOD]` Harita: şehir/ilçe/köy düğümleri, kilit durumları, 5 nokta göstergesi — 14.11, mini harita dahil (Kurulum 19)
- [x] `[KOD]` İpucu defteri (iki tip için ortak) — `FigmaHintBook`
- [x] `[KOD]` Boy uzama (`PlayerGrowth`, `CLAUDE.md` §7) — 14.11, Kurulum 25, `GrowthTests`
- [x] `[KOD]` Gamepad istasyon kontrolü (odak tabanlı, imleçsiz)
- [x] `[KOD]` "Gerçek hayatta" kartı — `IRealLifeCardView`; metin kontrolü: `04_Icerik/17-icerik-kontrol-listesi.md`
- [x] `[KOD]` Emeği Geçenler ekranı (`05_Uretim/LICENSES.md`'den elle aktarılır) — **lisans kuralı için zorunlu** — ana menüde (Kurulum 20); LICENSES.md 14.11'de düzeltildi, ekranla eşitliği kontrol et

## P2 — Cila

- [ ] `[KOD]` Sınıf açılış/kapanış ara sahnesi (yarım) — açılış: çıraklık açılış kartları (Kurulum 17); kapanış yok
- [ ] `[KOD]` Köy canlanma görselleri (aç/kapa parçalar)
- [x] `[KOD]` Geliştirici kısayolları (görev atla, kayıt sıfırla) — **demo için** — `DevShortcuts`: F1–F6 / Ctrl+R; gamepad LT+RT+D-pad
- [x] `[KOD]` Sakin figürleri / 2D kartlar kapıda — 14.11: müşteriler atölye önü pazarına gelir (`CustomerQueue`, Kurulum 22); konuşmalarda portre
- [x] Ses: köpürme, ocak, mühür, kâğıt (CC0) — `AudioDirector` + `SoundLibrary.asset`, adım sesleri `PlayerFootsteps`
- [ ] Müzik (CC0)

## 14.11 eklenenler (son görevler ve level)

- [x] Tek oyun sahnesi **`Harita`** (atölye + şehir), ilk açılan **`AnaMenu`** (menü arkasında kuş bakışı uçuş, `MenuFlyover`, Kurulum 20). Boot/Classroom/Town yok.
- [x] Çıraklık köyü `cirak_0` (İlk Kural): açılış kartları, uyanış, tahmin, ısıtma siparişi, K-0 (Kurulum 17).
- [x] Denkleştirme köyü **6 görev**: `denk_1` → `bereket_2` (Sv2 tarla) → `denk_2…5`.
- [x] Sv2 Tohum Nefes Alır (`bereket_2`, Mat + Biyo): ders notu + hesap soruları → tohum al → sandığa dök → traktörle 2 tur → Hayat Ustası'nın sorusu. HUD sipariş kartında 5 adımlı rehber; tarlada Elif, Hesap Ustası, Hayat Ustası'yla aşamaya göre konuşma (`FieldNpc`, ipucu verir, cevap vermez). Kurulum 23 (traktör) + 24 (tarla).
- [x] Masa görevleri `tur_1…5` için Ders Notu (mektup ilk açılınca, kâğıttan önce). Menü: BilgiAdasi → Seviye → Masa görevlerinin ders notlarını yaz.
- [x] Masada mektup beklerken zarf yeşil nabızla ışır (`ArtDeskView.letterHalo`, Kurulum 26).
- [x] Atölye önü pazarı: sipariş ve teslim tezgâhta, alıcılar envanterdeki ürünü almaya gelir (Kurulum 22).
- [x] Görev adım rehberi (HUD kartı), ortak diyalog çerçevesi, kazan buharı, atölye ışığı bake.
- [x] Harita ekranı + mini harita (Kurulum 19), defter (Ustanın Notları / Keşiflerim / Ustalık), Hata Dedektifi, Tahmin.
- [x] Boy uzama (Kurulum 25), görev sonu otomatik kayıt, başarım ağacı (ders sekmeleri; Mat/Biyo `bereket_2` ile açık).
- [x] Girdi: Move'a ok tuşları, yeni **Confirm** (Enter / Space · A) konuşmayı ilerletir ve şıkkı onaylar; soru/konuşma açıkken harita ve defter açılmaz (`DialogueFocus`); pazar/menü açıkken başlayan konuşma ekran kapanınca gelir.
- [ ] Hayat Ustası'nın kendi modeli yok (tarlada köylü erkek modeli ve portresi yer tutucu).
- [ ] `tur_1…5` ders notlarının kitapla kontrolü (`04_Icerik/17-icerik-kontrol-listesi.md`).

## P3 — Varsa

- [ ] Kapanışta sınıfta mini denkleştirme etkileşimi
- [ ] Kilitli şehirler için sisli silüetler

## Zaman planı

| Zaman | Hedef |
|---|---|
| Cuma 12.00–20.00 | P0 kod. Modelci atölye + kazan + terazi, UI sayaç/kart tasarımı paralel |
| Cuma 20.00–24.00 | P0 bitiş, veriler girildi, **build 1** |
| Cmt 00.00–05.00 | P1 |
| Cmt 05.00–08.00 | P2, hata düzeltme. **08.00 kod dondurma** |
| Cmt 08.00–10.00 | **Son build**, Emeği Geçenler, demo provası, yedek ekran kaydı |
| Cmt 10.00–11.00 | Tampon. Yeni özellik yok |

## "Bitti" tanımı

- Build Windows'ta açılır, iki köy uçtan uca hatasız oynanır.
- `02` ve `03`'teki kabul kriterleri işaretli.
- `05_Uretim/LICENSES.md` dolu; oyunda Emeği Geçenler var.
- Projektörde yazılar okunuyor.
- 4 dakikalık demo provası yapıldı; geliştirici kısayolları çalışıyor.

## Açık işler / sorulacaklar

- [ ] **Organizasyona sor:** "0'dan üret" eski kendi model/kodlarımızı kapsıyor mu? Cevaba göre `README` güncellenir.
- [ ] Tür adları (analiz/ayrışma), 10. sınıf Maarif ders kitabıyla kontrol. → `04_Icerik/17-icerik-kontrol-listesi.md`
- [ ] "Gerçek hayatta" cümleleri kitap/öğretmen kontrolü. → `04_Icerik/17-icerik-kontrol-listesi.md`
- [ ] Görev 5 fiyatları oynanarak ayarlanacak.
