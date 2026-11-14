# 11 — Görev ve Başarım Listesi (ders bazlı)

> Kaynak: `02_Mekanik/04-gorev-plani.md` (Mustafa + Claude, 13.11) ve `05-icerik-prototip.md`. Bu dosya o planı **kodun kullandığı kimliklerle** tek listede toplar: her görevin `taskId`'si, dersi, köyün `neighborhoodId`'si ve bitince açılan başarımlar.
> Başarımlar **ders bazlı** ayrılır (karar 13.11): Esc → Başarımlar'da her ders bir sekme = bir `AchievementTree` asset'i (`Data/Achievements/AchievementTree_<Ders>.asset`). Bir görev, **defter kaydının (K-n) dersine** göre tek sekmeye girer; yeri (atölye, tarla, koru, çarşı) açıklamada yazar.
> İlk sürüm menü **Kurulum → 13b** ile bu listeden üretilir; sonra metin, renk, simge, konum Inspector'dan düzenlenir (kod değişmez). Yeni ders = yeni asset; Achievements objesine kendiliğinden bağlanır.
> **Güncelleme 14.11:** Bereket Tarlası'nın Mat/Biyo görevleri tek köyde toplandı: `bereket_1…5` (köy `bereket_tarlasi`, K-11…K-15), her görev hem Matematik hem Biyoloji sekmesinde. Ayrıntı ve ders notları: `04_Icerik/15-bereket-tarlasi-mat-biyo.md`. Traktörlü Kimya/Fizik tarla görevleri (`tarla_1, 2, 3, 6, 10`) sonraki sürümde, defterleri K-16'dan devam eder.
> **Güncelleme 14.11 (sabah):** oyunda oynanan sıra aşağıdaki "Oynanabilir durum" tablosunda. Denkleştirme köyü artık **6 görev**: Sv2 tarla görevi `bereket_2` (Tohum Nefes Alır) D-1'den sonra gelir (`Neighborhood_denklestirme.asset`; geliştirici kısayolu F1–F6). Matematik ve Biyoloji sekmeleri `ach_bereket_2_*` ile açıldı ("Yakında" değil).
> Kimlik kuralı: görev `denk_n · tur_n · bereket_n · tarla_n · orman_n · carsi_n`, başarım `ach_<görev>` / `ach_<görev>_<koşul>`. Görev verisi yazılırken (`TaskData.id`) **bu kimlikler kullanılmalı**, yoksa başarım açılmaz.

## Başarım türleri (sağ alttaki bildirim)

| Tür | Bildirim başlığı | Ne zaman |
|---|---|---|
| Öğrenildi (`Learned`) | **Öğrenildi!** | Görev bitti; açıklama = o görevin Keşif Defteri cümlesi (K-n) |
| Başarım (`Milestone`) | **Başarım kazanıldı!** | Çıraklık başladı, köy bitti, dersin bütün görevleri bitti (usta mührü) |
| Meydan okuma (`Challenge`) | **Meydan okuma tamamlandı!** | Hatasız (köpürmeden / yanlışsız) ya da defter açmadan; bazıları gizli (`???`) |

Koşullar: `GameStart` · `TaskCompleted` · `NeighborhoodCompleted` · `TaskFlawless` (görevde hiç hata yok) · `TaskWithoutHint` (defter açılmadı) · `AllOf` (listedeki başarımların hepsi açık).

## Sekmeler

| Sıra | Ders | Renk (09 paleti) | Durum | Usta |
|---|---|---|---|---|
| 0 | **Kimya** | bakır `#9F4E2B` | açık (prototip) | Câbir Usta |
| 1 | **Matematik** | dağ mavisi `#4F7F96` | açık (yalnız `bereket_2`), gerisi Sürüm 2 | Hesap Ustası |
| 2 | **Fizik** | kaya grisi `#6E8090` | Yakında (Sürüm 2) | Makine Ustası |
| 3 | **Biyoloji** | yaprak yeşili `#3FA83A` | açık (yalnız `bereket_2`), gerisi Sürüm 2 | Hayat Ustası |

Türkçe ve Sosyal'in henüz görevi yok; görevleri yazılınca asset eklenir (sekme kendiliğinden gelir).

## Oynanabilir durum (14.11, teslim build'i)

Görev verisi olan (`Data/Tasks/`) ve oyunda uçtan uca oynanan görevler. Gerisi bu listede plan olarak durur.

| Sıra | Köy (`neighborhoodId`) | Görev | Tip | Önünde | Durum |
|---|---|---|---|---|---|
| 0 | `ciraklik` | `cirak_0` İlk Kural | Sipariş (R_isitma) | açılış kartları + uyanış + tahmin | ✅ oynanıyor, K-0 |
| 1 | `denklestirme` | `denk_1` Kuru Çeşme | Sipariş | Ders Notu + Tahmin, Atom Görüşü | ✅ |
| 2 | `denklestirme` | `bereket_2` Tohum Nefes Alır (Sv2) | Tarla (Mat + Biyo) | Ders Notu + hesap soruları, traktörle 2 tur, Hayat Ustası sorusu | ✅ adım rehberi HUD kartında; tarlada Elif / Hesap Ustası / Hayat Ustası ile konuşulur (`FieldNpc`) |
| 3 | `denklestirme` | `denk_2` İsli Fenerler | Sipariş | Ders Notu + Tahmin | ✅ |
| 4 | `denklestirme` | `denk_3` Soğuk Fırın | Sipariş | Ders Notu + Hata Dedektifi (tahta) | ✅ |
| 5 | `denklestirme` | `denk_4` Yıkık Köprü | Sipariş (zincir) | Ders Notu + Tahmin (ara ürün sorusu) | ✅ |
| 6 | `denklestirme` | `denk_5` Gübre Ruhu | Sipariş (`Purchase`) | Ders Notu + Tahmin | ✅ |
| 7–11 | `tepkime_turleri` | `tur_1` … `tur_5` | Masa | **Ders Notu (yeni, 14.11):** mektup ilk açılınca, kâğıttan önce | ✅ tur_5 Lonca Sınavı rastgele çekme |

Masada mektup beklerken zarf yeşil nabızla ışır (Kurulum 26). Sipariş/teslim atölye önü pazarında (Kurulum 22). Her görev sonunda otomatik kayıt.

---

## Kimya

Ağaç: kök **İlk Kural** → üst dal Denkleştirme (D-1 → D-5 → köy), orta dal Tepkime Türleri (T-1 → T-5 → köy → iki köy), alt dal tarladaki Kimya görevleri. "Köpürmeden" meydan okumaları her denkleştirme görevinin altında.

| Sv | Görev | taskId | Yer · veren | Katman (04 planı) | Defter | Başarımlar |
|---|---|---|---|---|---|---|
| 0 | Çıraklık: İlk Kural | — (tanıtım; görev verisi yok) | Atölye · Câbir Usta | Atom Görüşü (tanıtım) | K-0 | **İlk Kural** (kök, Çıraklık görevi `cirak_0` bitince; karar 14.11) |
| 1 | D-1 Kuru Çeşme | `denk_1` | Denk · çeşme · Muhtar Yusuf | Atom Görüşü | K-1 | Kuru Çeşme · *Köpürmeden: Çeşme* |
| 2 | D-2 İsli Fenerler | `denk_2` | Denk · fener · Fenerci Demir | Tahmin: yanma için ne gerekir | K-2 | İsli Fenerler · *Köpürmeden: Fener* |
| 3 | D-3 Soğuk Fırın | `denk_3` | Denk · fırın · Fırıncı Zehra | Hata Dedektifi | K-3 | Soğuk Fırın · *Köpürmeden: Fırın* |
| 4 | D-4 Yıkık Köprü | `denk_4` | Denk · köprü · Duvarcı Halil | Ara ürün | K-4 | Yıkık Köprü · *Köpürmeden: Harç* |
| 5 | D-5 Gübre Ruhu | `denk_5` | Denk · tarla · Çiftçi Elif | En az akçe + artan madde | K-5 | Gübre Ruhu · *Köpürmeden: Gübre* · *Defter Kapalı* (gizli) |
| — | Köy bitti | köy `denklestirme` | | | | **Denkleştirme Köyü** |
| 6 | T-1 Ustanın Defteri | `tur_1` | Tepk · masa · güvercin | Gözlemden çıkarım | K-6 | Ustanın Defteri |
| 7 | T-2 Etiket Masası | `tur_2` | Tepk · masa · güvercin | Gözlem ↔ tür eşleme | K-7 | Etiket Masası |
| 8 | T-3 Fırıncının Mektubu | `tur_3` | Tepk · masa · güvercin (Zehra) | Kimyasal değişim kanıtı | K-8 | Fırıncının Mektubu |
| 9 | T-4 Muhtarın Mektubu | `tur_4` | Tepk · masa · güvercin (Yusuf) | Yanlış iddiayı düzelt | K-9 | Muhtarın Mektubu |
| 10 | T-5 Lonca Sınavı | `tur_5` | Tepk · lonca · Kâtip Nevin | Transfer | K-10 | Lonca Sınavı · *Tek Seferde: Lonca* (gizli) |
| — | Köy bitti | köy `tepkime_turleri` | | | | **Tepkime Türleri Köyü** |
| — | İki köy bitti (kapanış) | `AllOf` | | | | **Hiçbir Atom Eksik Değil** |
| 11 | Makineyi Uyandır (A1) | `tarla_1` | Bereket Tarlası (Sürüm 2) | Hata Dedektifi (yanma) | K-16 | Makineyi Uyandır |
| 13 | Toprağı Tanı (A3) | `tarla_3` | Bereket Tarlası (Sürüm 2) | Tahmin (asit–baz) | K-18 | Toprağı Tanı |

## Matematik (Sürüm 2)

Ağaç: kök Toprağın Canlıları (bereket_1) → tarla dalı (1 → 5 → köy); koru dalı ve çarşı dalı kökten ayrılır. Bütün Matematik görevleri → **Hesap Ustası'nın Mührü**.

| Sv | Görev | taskId | Yer | Konu | Defter | Başarım |
|---|---|---|---|---|---|---|
| 11 | Toprağın Canlıları | `bereket_1` | Bereket Tarlası | Örneklemden tahmin, ortalama | K-11 | `ach_bereket_1_mat` Örnekten Bütüne |
| 12 | Tohum Nefes Alır ✅ oynanıyor | `bereket_2` | Bereket Tarlası (Denkleştirme köyünün 2. görevi) | Olasılık | K-12 | `ach_bereket_2_mat` Beklenen Sayı · *Tek Kese Fazla Yok* (gizli) |
| 13 | Işık ve Boy | `bereket_3` | Bereket Tarlası | Eğim (MAT.10.6.2) | K-13 | `ach_bereket_3_mat` Tablodan Doğruya |
| 14 | Tırtıl, Kuş, Şahin | `bereket_4` | Bereket Tarlası | Yüzde, oran | K-14 | `ach_bereket_4_mat` Yüzde On |
| 15 | Hasat Raporu | `bereket_5` | Bereket Tarlası | İki kategorik değişken (MAT.10.2.1), ortalama | K-15 | `ach_bereket_5_mat` Hasat Raporu · *Defter Kapalı: Hasat* (gizli) |
| — | Köy bitti | köy `bereket_tarlasi` | | | | `ach_bereket_tarlasi_mat` **Bereket Tarlası** |
| ~~14~~ | ~~Tohum Hesabı (A4) `tarla_4`~~ → bereket_2 · ~~Ekim Düzeni (A5) `tarla_5`~~ çıkarıldı · ~~Hasat Raporu (A9) `tarla_9`~~ → bereket_5 | | | | | |
| 22 | Yön Çentiği (B2) | `orman_2` | Ormancı Korusu | Açı, yüzde | K-22 |
| 27 | Teraziyle Satış | `carsi_1` | Çarşı | Oran | K-27 |
| 28 | İndirim mi Kazık mı? | `carsi_2` | Çarşı | Yüzde | K-28 |
| 29 | Yol Hesabı | `carsi_3` | Çarşı | Analitik | K-29 |
| — | Çarşı bitti | köy `carsi` | | | **Çarşının Hesabı** |
| — | Dersin bütün görevleri | `AllOf` | | | **Hesap Ustası'nın Mührü** |

## Fizik (Sürüm 2)

Ağaç: kök İlk Saban → tarla dalı; koru dalı kökten ayrılır. → **Makine Ustası'nın Mührü**.

| Sv | Görev | taskId | Yer | Konu | Defter |
|---|---|---|---|---|---|
| 12 | İlk Saban (A2) | `tarla_2` | Bereket Tarlası | Sabit hız (FİZ.10.1.1) | K-17 |
| 16 | Kuyudan Tarlaya (A6) | `tarla_6` | Bereket Tarlası | İş, güç (FİZ.10.2.2) | K-19 |
| 20 | Değirmenden Fırına (A10) | `tarla_10` | Bereket Tarlası | Enerji kaynakları (FİZ.10.2.5) | K-20 |
| 23 | Ağaç Düşüyor! (B3) | `orman_3` | Ormancı Korusu | Mekanik enerjinin korunumu (FİZ.10.2.4) | K-23 |
| 24 | Kızakla Taşı (B4) | `orman_4` | Ormancı Korusu | İş, güç (FİZ.10.2.1–2) | K-24 |
| 26 | Odun Ateşe (B6) | `orman_6` | Ormancı Korusu | Enerji dönüşümü (FİZ.10.2.3, 10.2.5) | K-26 |
| — | Dersin bütün görevleri | `AllOf` | | | **Makine Ustası'nın Mührü** |

## Biyoloji (Sürüm 2)

Ağaç: kök Toprağın Canlıları (bereket_1) → tarla dalı (1 → 5 → köy); koru dalı kökten ayrılır. → **Hayat Ustası'nın Mührü**.

| Sv | Görev | taskId | Yer | Konu | Defter | Başarım |
|---|---|---|---|---|---|---|
| 11 | Toprağın Canlıları | `bereket_1` | Bereket Tarlası | Ekosistem bileşenleri, ayrıştırıcılar (BİY.10.2.1, 10.2.3, 10.2.4) | K-11 | `ach_bereket_1_biyo` Toprağın Canlıları |
| 12 | Tohum Nefes Alır ✅ oynanıyor | `bereket_2` | Bereket Tarlası | Oksijenli solunum (BİY.10.1.7, 10.1.8) | K-12 | `ach_bereket_2_biyo` Tohum Nefes Alır |
| 13 | Işık ve Boy | `bereket_3` | Bereket Tarlası | Fotosentez, çevresel faktörler (BİY.10.1.2, 10.1.3) | K-13 | `ach_bereket_3_biyo` Işıktan Besine |
| 14 | Tırtıl, Kuş, Şahin | `bereket_4` | Bereket Tarlası | Enerji akışı (BİY.10.2.3, 10.2.2) | K-14 | `ach_bereket_4_biyo` Zehirsiz Çare |
| 15 | Hasat Raporu | `bereket_5` | Bereket Tarlası | Deney, değişkenler, azot (BİY.10.1.3, 10.2.4) | K-15 | `ach_bereket_5_biyo` Kontrol Grubu |
| — | Köy bitti | köy `bereket_tarlasi` | | | | `ach_bereket_tarlasi_biyo` **Bereket Tarlası** |
| ~~17~~ | ~~Elif'in Gübresi (A7) `tarla_7`~~ → bereket_5 · ~~Güneşten Besine (A8) `tarla_8`~~ → bereket_3 | | | | | |
| 21 | Hangi Ağaç? (B1) | `orman_1` | Ormancı Korusu | Sürdürülebilirlik | K-21 |
| 25 | Bir Kes, Üç Dik (B5) | `orman_5` | Ormancı Korusu | Ekolojik ayak izi | K-25 |
| — | Dersin bütün görevleri | `AllOf` | | | **Hayat Ustası'nın Mührü** |

## Ders ataması (birden çok derse dokunan görevler)

**İstisna — Bereket Tarlası (`bereket_1…5`):** iki dersli köy; her görevin iki başarımı var (`_mat`, `_biyo`), aynı koşul `TaskCompleted bereket_n`, iki ayrı asset'te. Kod değişikliği gerekmez.

Görev, **defter cümlesinin dersine** girer. Planda iki ders yazanlar: A2 İlk Saban (Mat · alan + Fizik · hız → K-17 hız: **Fizik**), A10 Değirmenden Fırına (Mat · oran + Biyo + Fizik → K-20 enerji: **Fizik**), A1 Makineyi Uyandır (Kimya · yanma → **Kimya**), B5 Bir Kes, Üç Dik (Mat · olasılık + Biyo → K-25: **Biyoloji**). Değiştirmek için: başarımı ilgili asset'e taşı (kimlik aynı kalır, kayıt bozulmaz).

Bölüm başarımları "Bereket Tarlası" ve "Ormancı Korusu" birden çok derse ait olduğu için kaldırıldı; köy olayları gelince istenirse Kimya sekmesine eklenebilir.

## Sonra eklenebilecek koşullar (kod gerekir, P2)
- Ustanın Sorgusu ilk seferde doğru → "Neden'ini biliyorum".
- Hata Dedektifi tek seferde → "Dedektif".
- Saha notu mührü (şehir başına 10 çentik) → dersin sekmesine.
