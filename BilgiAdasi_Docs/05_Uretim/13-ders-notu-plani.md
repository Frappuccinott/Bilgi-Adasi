# 13 — Ders Notu (görev öncesi mini konu anlatımı) — Plan

> Talep (Mustafa, 14.11 gece): Her görevden önce o görevin konusu kısaca anlatılsın (ders notu), görevde ne yapılacağı söylensin; sonra görevde uygulamalı yapılsın.
> Durum: plan. Kod dondurma **08.00** — aşağıdaki kapsam buna göre kesildi.
>
> **Uygulandı (14.11 gece), denk_1–5, tam yol (hızlı yol kullanılmadı):**
> - Figma: "Keşif Defteri ve Başarılar" sayfası, `Ders Notu/1 Konu ve yöntem (denk_1)` ve `Ders Notu/2 Örnek ve görev (denk_1)`: açık defter, "Ustanın Notları" ayracı, sayım tablosu, numaralı adımlar, Dikkat/Unutma notları.
> - Veri: `Core/LessonNote.cs` (sayfa = blok listesi: Kicker, Title, Subhead, Body, Equation, Counts, Steps, Note, Warning, Rule, Footnote), `LearningEntry.lesson`, `LearningData.lessonTexts`. İçerik `Editor/LessonNotesData.cs` dosyasında, `LearningDataBuilder` menüsüyle asset'e yazılır.
> - Araştırma: `04_Icerik/12-ders-notu-arastirma.md` (TYMM KİM.10.1.6 / 10.1.7, "girenler (tepkenler)", yanılgı kaynakları).
> - Akış: kapı konuşması → ustanın satırı → Ders Notu → kapanınca sipariş başlar (`NeighborhoodRunner.OnDoorDialogueFinished`). Açıkken `LearningDirector.IsBusy`, girdi UI modunda. Tahmin kartı (PredictionDirector) nottan sonra gelir.
> - Görünüm: `UI/Figma/FigmaLessonNote.cs` (Kurulum 14 ekler). ← → sayfa, Enter/A ileri, Esc/B geri; fareyle sol sayfa geri, sağ sayfa ileri.
> - Yeniden okuma: H yerine Esc menüsünde "Ders Notu" düğmesi (yalnızca notu olan sipariş alınmışken). Ustanın ipucunu (H) ve `TaskWithoutHint`'i etkilemez.
> - Masa görevleri (tur_1–5) bu turda yok.

## 1. Öğrenme döngüsüne yeri

04-gorev-plani §1 iskeletinin başına bir adım eklenir:

```
0 DERS NOTU  Câbir Usta konuyu 3–4 kısa sayfada anlatır + "bu görevde ne yapacaksın"   (YENİ)
1 TAHMİN     ders notunun son sayfası: tek soru (varsa)                               (P2, aynı ekranda)
2 GÖREV      kazan + terazi / kâğıt (uygulama)                                         (mevcut)
4 SORGU      "Peki çırak, neden…?"                                                     (mevcut)
5 DEFTER     Keşif Defteri kaydı K-n                                                   (mevcut)
```

Böylece döngü: **anlat → uygula → açıkla → kaydet.** Ders notu ile Keşif Defteri birbirine bağlanır: not anlatır, kayıt öğrenileni özetler.

## 2. Oyuncu ne görür

**Sipariş görevi (denk_1–5):** Kapıda sakin derdini anlatır → konuşma biter → Usta: *"Dur, kazana koşmadan önce şunu bir oku."* → **Ders Notu** açılır → kapanınca sipariş kazana verilir (bugünkü `BeginPendingOrder`).

**Masa görevi (tur_1–5):** Güvercin mektubu getirir → oyuncu masaya oturur → kâğıttan önce **Ders Notu** → kâğıt açılır.

**Ders Notu sayfaları** (her görev 3–4 sayfa, her sayfa ≤ 3 kısa cümle; okuma ≤ 45 sn):

| Sayfa | İçerik | Örnek (D-1 Kuru Çeşme) |
|---|---|---|
| 1. Konu | Başlık + temel fikir | **Kütlenin korunumu.** Tepkimede atom yok olmaz, yoktan da var olmaz; yalnızca bağlar değişir. |
| 2. Nasıl yapılır | Yöntem, adım adım | Denklemi denkleştirmek: her elementi iki tarafta ayrı say. Eksik tarafa **katsayı** ekle; formülün içindeki küçük sayıya dokunma. |
| 3. Çözümlü örnek | Görevdekinden farklı ama benzer bir örnek | H₂ + Cl₂ → HCl: solda 2 H, sağda 1 H → **H₂ + Cl₂ → 2HCl** |
| 4. Bu görevde | Somut uygulama talimatı + dikkat | Muhtar su istiyor. Kazana H₂ ve O₂ dök; iki terazi eşitlenince ateşi yak. *Dikkat:* H₂O'yu H₂O₂ yapma — formül değişmez. |

- Gezinme: **Devam / Geri / Kapat** (mevcut UI map: Submit, Cancel, Navigate). **Yeni girdi aksiyonu yok.**
- Tekrar okuma: görev sırasında **H (gamepad Y / istasyonda X)** → Ustanın Defteri'nde yeni ayraç **"Ders Notu"** (şu anki görevin notu). İpucu sekmesi ayrı kalır.
- Başarım: Ders Notu'nu tekrar açmak **"Defter Kapalı" / `TaskWithoutHint`'i bozmaz**; yalnızca ipucu bozar.
- Atlanabilir: ikinci kez oynayan (kayıtta görev bitmiş) ya da Dev kısayoluyla atlayan için not gösterilmez.

## 3. Veri (Mehmet)

Yeni asset açmak yerine var olan **`LearningData`** genişletilir (zaten `taskId` ile anahtarlı, Resources'tan kendiliğinden yükleniyor):

```csharp
[Serializable] public sealed class LessonPage { string heading; [TextArea] string body; [TextArea] string example; }
[Serializable] public sealed class LessonNote  { string title; string subject; List<LessonPage> pages; ReasoningPrompt prediction; }
// LearningEntry'ye: [SerializeField] private LessonNote lesson;
```

- Metinler SO'da (kod kuralı §8). Formüller TMP `<sub>`.
- `LearningDataBuilder` (Kurulum menüsü) 10 görevin notunu üretir; sonra Inspector'dan düzeltilir.
- `prediction` boşsa tahmin sayfası çıkmaz (P2 tahmin aynı yapıyla gelir; ayrı ekran gerekmez).

## 4. Kod (Mehmet)

| Parça | Ne | Süre |
|---|---|---|
| `LessonDirector` (ya da `LearningDirector`'a `ShowLesson(taskId, onDone)`) | Notu açar, oyuncu hareketini durdurur (`Block/Unblock` mevcut), kapanınca `onDone` | 45 dk |
| `NeighborhoodRunner.OnDoorDialogueFinished` | `BeginPendingOrder()`'dan önce `ShowLesson(order.Id, BeginPendingOrder)` | 15 dk |
| `DeskController` | Kâğıt açılmadan önce aynı çağrı | 20 dk |
| `OrderController` / `DeskController` ipucu (H) | Defterde "Ders Notu" ayracı | 30 dk |
| `ILessonView` (`UI/LearningViews.cs`) | `Open(title, pages, labels, onClosed)`; denetleyici Canvas bilmez | 10 dk |
| Test | denk_1 ve tur_1 baştan sona, gamepad ile | 20 dk |

**Toplam ≈ 2,5 sa.**

**Hızlı yol (zaman daralırsa):** `ILessonView` yazılana kadar **mevcut `IDiscoveryBookView`** (sayfalı defter görünümü) aynen kullanılır — sayfa = `<b>başlık</b>\n gövde \n<i>örnek</i>` metni. Kod 1 sa'e iner, UI hazır olur.

## 5. Görünüm (Mustafa)

- `LessonNoteView : RegisteredView<ILessonView>` — Figma dilindeki ipucu defterinin kardeşi: parşömen sayfa, sol üstte ders/köy etiketi (Kimya · Denkleştirme), başlık, gövde, altta **örnek kutusu** (tebeşir/mürekkep ayrımı), sayfa noktaları (●○○○), cihaz düzenine göre tuş ipucu (`ControlGlyphs`).
- Renkler ve font 09-tema kurallarından (bakır `#9F4E2B` başlık vurgusu).
- İsteğe bağlı (P2): sayfa 3'teki çözümlü örnek atölyedeki **karatahtaya da** yazılır (`IChalkboardView.ShowEquations`) — "usta tahtada anlattı" hissi, ek kod ~10 dk.
- Süre: 1–1,5 sa.

## 6. İçerik (10 görev) — konu ve "bu görevde"

| Görev | Konu (sayfa 1–2) | Çözümlü örnek (sayfa 3) | Bu görevde (sayfa 4) |
|---|---|---|---|
| denk_1 Kuru Çeşme | Kütlenin korunumu; katsayı ≠ alt indis | H₂ + Cl₂ → 2HCl | 2H₂ + O₂ → 2H₂O kur; teraziler eşitlenince ateş |
| denk_2 İsli Fenerler | Yanma: madde + O₂ → CO₂ + H₂O | 2CO + O₂ → 2CO₂ | CH₄'ü yak; önce C, sonra H, en son O'yu say |
| denk_3 Soğuk Fırın | Büyük moleküllerde sayma sırası (C → H → O) | C₂H₆ yanması (yalnız sayım) | C₃H₈ + 5O₂ → 3CO₂ + 4H₂O; O en sona |
| denk_4 Yıkık Köprü | Ayrışma ve birleşme; ara ürün | 2H₂O → 2H₂ + O₂ | CaCO₃ → CaO + CO₂, sonra CaO + H₂O → Ca(OH)₂ (iki adım) |
| denk_5 Gübre Ruhu | Oran ve sınırlayıcı madde; artan madde | 2H₂ + O₂: 3 H₂ ile 1 O₂ → 1 H₂ artar | N₂ + 3H₂ → 2NH₃; en az akçeyle al, artanı hesapla |
| tur_1 Ustanın Defteri | Gözlem → tepkime türü (gaz, renk, ısı, çökelti) | "Kireç suyu bulandı" → CO₂ var | Defterdeki boşlukları gözlemle doldur |
| tur_2 Etiket Masası | Tepkime türleri: yanma, sentez, analiz, nötralleşme, çökelme | Her türe bir örnek denklem | Şişeleri türlerine göre ayır |
| tur_3 Fırıncının Mektubu | Fiziksel ↔ kimyasal değişim kanıtı | Buzun erimesi vs. odunun yanması | Mektuptaki olayın kanıtını seç |
| tur_4 Muhtarın Mektubu | Yaygın yanılgılar (kütle kaybolur, katsayı formüle girer…) | "Yanan odun hafifler, kütle kayboldu" → gaz kaçtı | Doğru/yanlış: yanlışları düzelt |
| tur_5 Lonca Sınavı | Tekrar: ünitenin özeti (yeni bilgi yok) | — (yerine kısa özet listesi) | Karma sorular; defterini kullanabilirsin |

İçerik ilk taslağını Claude `LearningDataBuilder`'a yazar; **Mustafa/öğretmen gözüyle bir okuma** şart (MEB 10. sınıf "Kimyasal Tepkimeler" ünitesiyle terim uyumu: *sentez/analiz*, *yanma*, *nötralleşme*, *çözünme–çökelme*).

## 7. Öncelik ve 24 saat etkisi

| Sıra | Parça | Kim | Süre | Öncelik |
|---|---|---|---|---|
| 1 | Veri + builder içerik (denk_1–5) | Mehmet + Claude | 1 sa | **P1** |
| 2 | Ders Notu akışı (kapı + masa) — hızlı yol (`IDiscoveryBookView`) | Mehmet | 1 sa | **P1** |
| 3 | İçerik tur_1–5 | Claude + Mustafa okuma | 45 dk | P1 |
| 4 | `LessonNoteView` (Figma dili) | Mustafa | 1–1,5 sa | P1 |
| 5 | H ile tekrar okuma (Ders Notu ayracı) | Mehmet | 30 dk | P2 |
| 6 | Tahmin sayfası, tahtaya yazma | Mehmet + Mustafa | 30 dk | P2 |

Toplam P1 ≈ 4 sa (iki kişi paralel ≈ 2–2,5 sa). Vakit yetmezse: **yalnızca denk_1–5 + hızlı yol** yeterli; sunumda "her görevden önce usta konuyu anlatır" gösterilir.

**Risk:** Akışı uzatır. Not 45 sn'yi geçmesin, her sayfa tek fikir. Demo'da Dev kısayolu (F1–F5) notu atlar ki sunum hızlı kalsın.

## 8. Kabul ölçütü

- [ ] denk_1: kapı konuşması → ders notu (4 sayfa) → kazan; klavye ve gamepad ile gezilir.
- [ ] tur_1: masaya oturunca kâğıttan önce ders notu.
- [ ] Bitmiş göreve tekrar girince not çıkmaz; yeni oyunda çıkar.
- [ ] Notta oyuncuya görünen hiçbir metin kodda yok (hepsi `LearningData`).
- [ ] LICENSES.md beyan tablosuna "Ders Notu sistemi (Claude)" satırı.
