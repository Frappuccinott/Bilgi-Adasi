# 17 — İçerik kontrol listesi (teslim öncesi, 14.11)

Oyuncunun gördüğü kimya metinleri yapay zekâ yardımıyla yazıldı. `LICENSES.md` gereği **ekipçe ders kitabıyla** (10. sınıf Maarif / TYMM 2024 Kimya) kontrol edilir. Her satır kontrol edilince `[x]` işaretlenir ve kontrol edenin adı yazılır. Metin değişirse ilgili asset'te düzeltilir (kodda değil).

## 1. Tepkime türü adları

Oyunda kullanılan seçenekler: **Sentez · Analiz · Yanma** (`Data/Questions/TepkimeTurleri/tur_2_etiket`, `lonca_cl_mini` ve diğer `lonca_*`, `tur_*`; ders notu ve sorgu metinleri `Data/Resources/LearningData.asset`).

- [ ] Kitapta "sentez / analiz" mi, "birleşme / ayrışma" mı geçiyor? Gerekirse parantezle ikisi birden: "Sentez (birleşme)".
- [ ] "Yanma" tepkimesinin tanımı (oksijenle tepkime, ısı/ışık) kitaptaki tanımla aynı mı?
- [ ] Fotosentez ve solunum oyunda "Diğer" türünde (`R_photosynthesis`, `R_respiration` → `Other`). Kitap bunları bir türe koyuyorsa sorular buna uymalı.

Tepkimeler ve oyundaki türleri:

| Tepkime | Oyundaki tür | Kontrol |
|---|---|---|
| R_water (H₂ + O₂ → H₂O) | Sentez | [ ] |
| R_waterSplit (H₂O → H₂ + O₂) | Analiz | [ ] |
| R_methane (CH₄ yanması) | Yanma | [ ] |
| R_propane (C₃H₈ yanması) | Yanma | [ ] |
| R_co (eksik yanma, CO) | Yanma | [ ] |
| R_lime1 (CaCO₃ → CaO + CO₂) | Analiz | [ ] |
| R_lime2 (kireç söndürme) | Sentez | [ ] |
| R_ammonia (N₂ + H₂ → NH₃) | Sentez | [ ] |
| R_photosynthesis / R_respiration | Diğer | [ ] |

## 2. "Gerçek hayatta" kartları

Kaynak: her görev asset'inin `realLifeText` alanı (`Data/Tasks/...`).

| Görev | Metin (özet) | Kontrol |
|---|---|---|
| denk_1 | Hidrojen + oksijen hızlı, gürültülü tepkime; yalnızca laboratuvarda | [ ] |
| denk_2 | Yetersiz havada yanma → is ve karbon monoksit; havalandırma | [ ] |
| denk_3 | Tüp gazı tam yanınca mavi alev; sarı/isli alev = hava yetersiz | [ ] |
| denk_4 | Kireç söndürmede çok ısı; eldiven + gözlük | [ ] |
| denk_5 | Amonyak gübre hammaddesi; keskin kokulu, zararlı | [ ] |
| tur_5 | Sindirim, paslanma, ekmeğin kabarması tepkime türlerinden biri | [ ] Not: ekmeğin kabarması (mayalanma) ve sindirim sentez/analiz/yanma sınıflamasına tam oturmayabilir; cümle "kimyasal değişim" diye yumuşatılabilir. |
| bereket_2 | Çimlenme testi ile tohum oranı | [ ] |

**Boş kartlar:** `cirak_0` ve `tur_1`…`tur_4`'te `realLifeText` boş. Kart bu görevlerde görünmüyorsa sorun yok; görünüyorsa metin girilmeli.

## 3. Güvenlik dili (yarışma: gerçek felaket çağrışımı yok)

- [ ] denk_2'deki karbon monoksit cümlesi bilgilendirici kalıyor mu? Ölüm/kaza çağrışımı yok, yalnızca "havalandırılır".
- [ ] denk_1'deki "gürültülü tepkime" patlama sahnesine dönüşmüyor (oyunda köpürme var, patlama yok).

## 4. Ders notları

`BilgiAdasi_Docs/04_Icerik/12-ders-notu-arastirma.md` kazanımlara (KİM.10.1.6, 10.1.7) dayanıyor. Oyundaki notlar `LessonNotesData` → `LearningData.asset`.

- [ ] denk_1…5 ders notları kitaptaki terimlerle uyumlu (katsayı, alt indis, kütlenin korunumu).

Kontrol eden: ______  Tarih/saat: ______
