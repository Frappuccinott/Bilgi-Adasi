# 14 — Defter (Figma'daki tam hâli) — Plan

> Talep (Mustafa, 14.11 gece): Figma'daki gibi defterin bütün kısımları; eksikler adım adım tamamlanacak.
> Kaynak: Figma "Keşif Defteri ve Başarılar" (Defter/1–5, Ders Notu/1–2), `11-ui-ux-plani` §5b, `13-ders-notu-plani`.

## 1. Figma ↔ oyun karşılaştırması

| Figma parçası | Oyunda (başlangıç) | Eksik |
|---|---|---|
| HUD: sol altta defter sırtı + `N Defter` + "Deftere yeni sayfa / K-n" | Yalnızca sağ altta kısa bildirim | Sırt, tuş ipucu, yeni kayıt notu, parlama |
| Açılış canlandırması: yükselir (0,20) → kapak açılır (0,45) → sayfalar oturur (0,70); kapanış 0,40 | Yok, defter anında açılır | Canlandırma |
| Ayraçlar: Ustanın Notları · Keşiflerim · Ustalık (Q / E) | Ders Notu'nda tek ayraç; Keşif Defteri ayrı, düz liste | Tek defter, üç ayraç, geçiş |
| Ders şeritleri (Kimya, Matematik, Fizik, Biyoloji, Türkçe, Sosyal) | Yok | Şeritler; kilitli ders soluk, saha notu olan ders listesi |
| Keşiflerim sol: "Kimya, n sayfa yazıldı", köy başlıkları + n/5, K-n satırları, seçili satır, yazılmamış soluk | Düz liste | İçindekiler |
| Keşiflerim sağ: K-n + görev adı, sakin/yer/köy, oyuncunun cümlesi (italik), denklem + ikon, "Öteki derslerde" saha notları, kullanım notu | Yok | Kayıt sayfası |
| Ustanın Notları (ders notu dışında): okunabilir notların listesi | Yalnızca görev öncesi | İçindekiler + okuma |
| Ustalık | Figma'da tasarım yok (11 §5b: başarı ağacının defterdeki yeri) | Tasarım + görünüm |
| Yeni kayıt yazılıyor (kalem, ~1,5 sn) | Yok | Yazı canlandırması |
| Alt satır: Q E Ayraç · ↑↓ Sayfa · N Kapat | Ders Notu'nda ←→ Sayfa · Enter | Girdi + ipuçları |

## 2. Adımlar (sırayla)

1. **Figma:** eksik iki ekranı çiz: *Ustanın Notları* (içindekiler + seçili not) ve *Ustalık* (köy mühürleri, sınıf, boy, başarımlar).
2. **Girdi:** `Player/OpenBook` (N / D-pad ↑), `UI/PrevTab` (Q / LB), `UI/NextTab` (E / RB). C# sınıfı yeniden üretilir, `ControlGlyphs` + CLAUDE.md §3 güncellenir. Kapatma `UI/Cancel` (Esc / B).
3. **Veri:** `LearningEntry`'ye görev adı, yer satırı, denklem, ikon formülü, saha notları, kullanım notu. `NotebookTexts` (ayraç adları, şeritler, köy başlıkları, boş sayfa metinleri). Saha notları 04-gorev-plani §4'ten.
4. **Model + denetleyici:** `NotebookDirector` (City): kayıttan ve köylerden defterin anlık görüntüsünü kurar (`NotebookModel`), N ile açar, Esc menüsündeki "Keşif Defteri"ni ve görev öncesi Ders Notu'nu aynı deftere yönlendirir.
5. **Görünüm:** `FigmaNotebook : INotebookView` — tek defter: ayraçlar, şeritler, üç sayfa türü, Ders Notu okuma kipi (bugünkü FigmaLessonNote içeriği bu defterin içine taşınır), alt satır ipuçları.
6. **HUD:** `FigmaNotebookSpine : INotebookHudView` — sol altta sırt, `N Defter`, yeni kayıtta "Deftere yeni sayfa / K-n" ve parlama.
7. **Canlandırma:** açılış 0,7 sn / kapanış 0,4 sn (unscaled), yeni kayıt yazısı ~1,5 sn (karakter karakter, TMP maxVisibleCharacters).
8. **Test:** N ile aç/kapat, Q/E, şeritler, ↑↓ kayıt, görev öncesi Ders Notu, Esc menüsünden açma, gamepad ipuçları.

## 2b. Durum (14.11, 01.30)

| Adım | Durum |
|---|---|
| 1 Figma | Yapıldı: `Defter/6 Ustanın Notları (içindekiler)`, `Defter/7 Ustalık`; Defter/4–5 alt satırında kapatma Esc. |
| 2 Girdi | Yapıldı: `Player/OpenBook` (N / D-pad ↑), `UI/PrevTab` (Q / LB), `UI/NextTab` (E / RB); C# sınıfı, `ControlGlyphs`, CLAUDE.md §3. |
| 3 Veri | Yapıldı: `LearningEntry` sayfa alanları + saha notları (`LessonNotesData.ApplyPage`), `NotebookTexts` (şeritler, köy başlıkları, Ustalık metinleri). |
| 4 Denetleyici | Yapıldı: `City/NotebookDirector.cs` (kendiliğinden kurulur; N, Esc menüsü "Keşif Defteri", yeni kayıtta HUD). |
| 5 Görünüm | Yapıldı: `UI/Figma/FigmaNotebook.cs` (INotebookView + ILessonView); eski `FigmaLessonNote` defterin içine taşındı ve silindi. |
| 6 HUD | Yapıldı: `UI/Figma/FigmaNotebookSpine.cs` (sırt, N Defter, "Deftere yeni sayfa"). |
| 7 Canlandırma | Açılış/kapanış yapıldı. **Kalem yazısı (~1,5 sn) yapılmadı** (P2). |
| 8 Test | Play'de: N ile açma, üç ayraç, Matematik şeridi, Ustalık, kapı → Ders Notu → ayraç değiştirip Esc ile nota dönme → Enter ile sipariş. Gerçek klavye/gamepad ile basılarak denenmedi (yansıma ile çağrıldı). |

## 3. Kararlar

- Ustanın Notları ayracında ustanın ipucu (H) gösterilmez: `TaskWithoutHint` başarımını bozmasın. Yalnızca ders notları (alınmış ya da bitmiş görevlerinki) okunur.
- Görev öncesi Ders Notu açıkken öteki ayraçlara geçilebilir, ama "Göreve başla" yalnızca Ders Notu'nun son açılışında.
- Defter açıkken oyun durur (`Time.timeScale = 0`, UI modu). Görev öncesi kipte de aynı.
- Puan, yıldız yok; Ustalık'ta başarı = mühür (11 §5b kuralı).
