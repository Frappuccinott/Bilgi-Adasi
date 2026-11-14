# 03 — Masa Mekaniği (Sözel Görevler)

Kavram, tanım ve sınıflandırma konularının görev tipi. Prototipte Tepkime Türleri köyünün 5 görevi.

## 1. Temel fikir

Sözel konu **kâğıt üzerinde** çözülür, ama her kâğıdın hikâyede bir sebebi vardır. Oyuncu "test çözmez"; ustanın defterini tamamlar, kasabalının mektubunu cevaplar, kavanozları etiketler, lonca sınavına girer. **Serbest metin yazdırılmaz** (yazım hatası kontrolü yok): her etkileşim seçme, sürükleme ya da işaretleme.

## 2. Sahne

Atölyenin köşesinde **çalışma masası**. Kamera masaya yaklaşır (aynı sahne, ayrı kamera noktası). Masada: kâğıt alanı, divit, mürekkep hokkası, ustanın defteri.

**Güncelleme 14.11:**
- Mektup masada beklerken zarf yeşil nabızla ışır (oyuncu masayı uzaktan fark eder; `ArtDeskView.letterHalo`, Kurulum 26).
- Mektup **ilk açılışta** kâğıttan önce o görevin **Ders Notu** gelir (`tur_1…5`; türler oyundaki üç kutuyla aynı: Sentez · Analiz · Yanma). Not kapanınca kâğıt açılır; yarım kâğıda dönüşte not gelmez. Plan: `05_Uretim/13-ders-notu-plani.md`.
- Soruda şık **Confirm** (Enter / Space · A) ile onaylanır; gamepad'de D-pad ↑↓ şık seçer.

Görev gelişi: Pencereye **ulak güvercin** konar → zarf masaya düşer → oyuncu tıklar → kâğıt açılır.

## 3. Hikâye sarmalayıcıları (wrapper)

Her masa görevi bir sarmalayıcı + bir ya da birkaç soru tipinden oluşur.

| Sarmalayıcı | Hikâye | Uygun soru tipleri |
|---|---|---|
| **Ustanın Defteri** | Defterin bazı kelimeleri silinmiş/ıslanmış; oyuncu tamamlar | Boşluk doldurma (kelime bankası) |
| **Etiket Masası** | Raftaki tepkime şişelerinin etiketleri karışmış; doğru kutuya koy | Sınıflandırma (sürükle-bırak) |
| **Mektup** | Bir kasabalı merak ettiğini yazmış; cevap kâğıdına işaretle | Çoktan seçmeli, doğru/yanlış |
| **Lonca Sınavı** | Köyün son görevi; simyacılar loncasının mühürlü sınav kâğıdı | Havuzdan karışık (rastgele 4 soru) |

## 4. Soru tipleri

| Tip | Etkileşim | Kontrol | Not |
|---|---|---|---|
| `MultipleChoice` | 4 şıktan birine tıkla | Tek doğru şık | Şıklar her açılışta karıştırılır |
| `TrueFalse` | Her ifade için D / Y mührü bas | Hepsi doğruysa geçer; yanlış satırlar lekelenir | Bir kâğıtta 3–5 ifade |
| `Classify` | Kartları kutulara sürükle | Her kart doğru kutuda | Kutular: tür adları |
| `FillBlank` | Kelime bankasından boşluğa sürükle | Her boşluk doğru | Bankada 1–2 çeldirici kelime |
| `BalanceEquation` | Denklemdeki katsayı kutularında `−/+` | Atom eşitliği + en küçük tam sayılar | `ReactionData`dan **otomatik üretilir**; soru yazmaya gerek yok |

`BalanceEquation`: atomlar eşit ama katsayılar sadeleşebiliyorsa (`4H₂ + 2O₂ → 4H₂O`) yanlış sayılmaz; kâğıtta not: *"Doğru, ama daha küçük sayılarla da olur."* ve tekrar denenir.

## 5. Kurallar

- **Hata:** Yanlış cevap mürekkep lekesine döner, kâğıt sallanır; tekrar denenir. Ceza yok.
- **İpucu:** Aynı görevde 2 hata → defter parlar → ilgili tanım cümlesi gösterilir (`QuestionData.hint`).
- **Geçme:** Görevdeki bütün sorular doğru → kâğıda ustanın mührü basılır → defterde bir sayfa dolar → köy parçası canlanır.
- **Lonca Sınavı:** Havuzdan rastgele 4 soru, aynı oturumda tekrar yok. Yanlış soru, havuzdan **başka bir soruyla** değiştirilir (ezberi engeller).
- Masa görevlerinde de "Gerçek hayatta" kartı olabilir (opsiyonel alan).

## 6. Sunum/kâğıt görünümü

- Kâğıt eski parşömen; yazı okunaklı modern serif (OFL lisanslı font).
- Formüller alt indisli.
- Doğru cevap: yeşil değil, **ustanın mührü** (tema içi geri bildirim). Renk körü için mühür şekli de farklı.

## 7. Kabul kriterleri

- [ ] Güvercin gelir, zarf tıklanınca kâğıt masada açılır, kamera masaya geçer.
- [ ] 5 soru tipinin her biri verilen veriden üretilir ve doğru kontrol edilir.
- [ ] `BalanceEquation` tepkime havuzundan otomatik soru üretir; en küçük tam sayı kontrolü çalışır.
- [ ] Yanlışta leke + tekrar; 2 yanlışta defter ipucu.
- [ ] Lonca Sınavı rastgele 4 soru çeker; yanlış soru başka soruyla değişir.
- [ ] Görev bitince mühür + defter sayfası + köy ilerlemesi.
