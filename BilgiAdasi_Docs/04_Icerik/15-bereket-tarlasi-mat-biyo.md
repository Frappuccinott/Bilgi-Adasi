# 15 — Bereket Tarlası: Matematik + Biyoloji köyü (5 görev)

> Talep (Mustafa, 14.11): Tarlaya 5 görev; **her biri hem Matematik hem Biyoloji**. Senaryo, ders notları (eğitici kısım) ve level planı birlikte.
> Durum: **plan + içerik taslağı**. Kod dondurma 08.00'e yetişmez → **Sürüm 2 (P2)**; sunumda "oyunun vaadi" (§9).
> Bu dosya `02_Mekanik/04-gorev-plani.md` §5'in Mat/Biyo görevlerinin (A4, A5, A7, A8, A9) yerini alır. Fizik/Kimya tarla görevleri (A1, A2, A3, A6, A10) traktörle birlikte sonraki sürümde kalır (§8).
> Ders notu biçimi `05_Uretim/13-ders-notu-plani.md` ve `Scripts/Editor/LessonNotesData.cs` ile aynıdır (sayfa = blok listesi), böylece içerik doğrudan veriye taşınır.

> **Uygulandı (14.11, 05.35) — karar: Tohum Nefes Alır (`bereket_2`) demo akışında Sv2.** Seviye sırası:
> Sv0 Çıraklık (`cirak_0`) → Sv1 denk_1 Kuru Çeşme → **Sv2 bereket_2 Tohum Nefes Alır** → Sv3–6 denk_2…denk_5 → Sv7–11 tur_1…tur_5.
> Görev Denkleştirme köyünün listesinde 2. sırada (köy 6 görev). Yürüyerek oynanır (traktör gerekmez):
> denk_1 teslim + sorgu bitince usta haberi verir → Elif'in derdi → ders notu (4 sayfa) → tarla sürülmüş görünür, 20 akçe tohum parası →
> pazarda "Tohum kesesi (10 tohum)" 2 akçe (yalnızca bu görevde) → tarla kapısındaki ekim sandığına dök (az gelirse Elif kaç filiz çıkacağını söyler)
> → 4 saha sorusu (Hesap Ustası 2, Hayat Ustası 2; Ustanın Sorgusu kartında) → **ekim animasyonu** (ekici sıra sıra, tohum yağar, toz kalkar,
> parseller ekili hâle büyür) → Elif → görev biter (otomatik kayıt) → Ustanın Sorgusu (beklenen sayı kesin mi?) + Keşif Defteri K-12.
> Kod: `Scripts/City/Field/` (FieldTask, FieldTaskDirector, SeedBox, FieldPlots), NeighborhoodRunner'da tarla görevi sunumu.
> Veri ve sahne: **Kurulum → 24**. Modeller: `tarla_ekim.py` (Ekim_Sandigi), `tarla_tohum_kesesi.py` (Esya_Tohum_Kesesi + simge).
> **Güncelleme 05.40 (traktörle ekim + mantık sırası):** sorular kendi anında sorulur — *Plan* (ders notundan hemen sonra, pazardan önce:
> kaç tohum, kaç kese) · *Ekim öncesi* (sandık dolunca: toprağı sürmeden ekseydik?) · *Ekim sonrası* (tohum enerjiyi nasıl alır).
> Sandık dolunca ekici (Alet_Ekici) traktörün pimine takılır; oyuncu traktörle tarla merkezinin çevresinde **2 tur** atar (tur bildirimi),
> ekicinin geçtiği parsel tohum yağarak ekilir; 2 tur bitince kalan parseller sıra sıra ekilir, ekici yerine döner.
> Tohum kesesi **Element pazarında** (`MarketStall_Elementler`, katalog `Pazar`) satılır.
> **Tamamlama 05.50:** ekim sandığı kapının dışına (−64.8, −2.0) alındı (içeride traktörün yolunu kesiyordu); tarla tabelası/kirişi
> 0,55 m yükseltildi (alt 3,41 m > tente 3,08); F6 geliştirici kısayolu (Denkleştirme 6 görev); H ipucu ve Esc "Ders Notu" tarla görevinde;
> başarım `ach_bereket_2` Matematik ve Biyoloji sekmelerinde kök (iki sekme artık "Yakında" değil); kartta "Görev x / 6", teslim adımı yok;
> Elif (köylü kadın modeli) ve Hesap Ustası (Hesap Hocası modeli) sandığın yanında. Harita sahnesi kaydedildi.
> Kalan: Hayat Ustası'nın modeli yok (konuşma portreyle); K-12 defterde Denkleştirme Köyü'nün 2. kaydı (seviye sırası gereği).
> (Eski not) Eksik: Mat/Biyo başarım ağaçlarına `ach_bereket_2_*` düğümleri (Kurulum 13b listesi) eklenmedi; Elif ve iki usta tarlada model olarak durmuyor
> (konuşmalar portreyle).

---

## 0. Kısa özet

- **Köy:** Bereket Tarlası (`bereket_tarlasi`), 5 görev `bereket_1 … bereket_5`, Seviye 11–15, defter K-11 … K-15.
- **Omurga:** Tarla modelindeki 6 durumun 5 geçişi = 5 görev. Her görev bitince 24 parselin hepsi bir sonraki duruma geçer: `Nadas → Surulmus → Ekili → Filiz → Olgun → Hasat`. Oyuncu bir ekin yılını baştan sona görür.
- **İki usta birlikte:** Hesap Ustası (Matematik, dağ mavisi, üçgen) ve Hayat Ustası (Biyoloji, yaprak yeşili, yaprak) tarladaki çardakta bekler. Her ders notunu birlikte anlatırlar: biri "ne oluyor" (Biyo), öteki "nasıl hesaplanır" (Mat).
- **Traktör gerekmez:** Bütün görevler yürüyerek, var olan sistemlerle (pazar, envanter, raf yuvası, 3D terazi, ders notu, tahmin kartı, ustanın sorgusu, defter) yapılır. Yeni tek parça: **saha sorusu kartı** (sayı girişi) ve **parsel durumu**.
- Her görev aynı döngüyü izler (13 §1): **Ders Notu → Tahmin → Sahada görev → Saha sorusu → Ustanın Sorgusu → Defter.**

| Sv | Görev | Geçiş | Biyoloji | Matematik | Sahadaki iş |
|---|---|---|---|---|---|
| 11 | **Toprağın Canlıları** `bereket_1` | Nadas → Sürülmüş | Ekosistem, ayrıştırıcılar, madde döngüsü | Örneklemden tahmin, ortalama, alan | 3 örnek karede solucan say |
| 12 | **Tohum Nefes Alır** `bereket_2` | Sürülmüş → Ekili | Oksijenli solunum (çimlenme) | Olasılık, beklenen değer | Pazardan doğru sayıda tohum kesesi al, ek |
| 13 | **Işık ve Boy** `bereket_3` | Ekili → Filiz | Fotosentez, ışık sınırlayıcı etken | Tablodan doğrusal ilişki, değişim oranı | Gölgelikli ve açık parselde filiz boyunu ölç |
| 14 | **Tırtıl, Kuş, Şahin** `bereket_4` | Filiz → Olgun | Besin zinciri, enerji akışı (%10), biyolojik mücadele | Yüzde, oran, bölme | Kuş yuvalarını direklere as |
| 15 | **Hasat Raporu** `bereket_5` | Olgun → Hasat | Deney ve kontrol grubu, gübrenin etkisi | Aritmetik ortalama, iki veri grubunu karşılaştırma, yüzde artış | Gübreli ve gübresiz parseli biç, terazide tart |

---

## 1. Senaryo

### 1.1 Tetik (D-5 Gübre Ruhu bitince)
Çiftçi Elif kapıya gelir (D-5 teslimi). Konuşmanın sonuna yeni satır:
- **Elif:** Gübre ruhu tamam. Ama tarlayı yalnız gübre büyütmez, çırak. Bu yıl ekini baştan sona benimle büyütür müsün?
- **Câbir Usta:** Tarla benim kazanıma sığmaz. Kilitli şehirlerden iki dostum gelecek: biri sayının, biri canlının ustası. Onlara çıraklık et.

Haritada Bereket Tarlası kilidi açılır (`LockedZone.unlockedBy = denklestirme`). Tarla girişinde çardak: iki usta oturur.

### 1.2 Ustalar
| Usta | Kim | Konuşma tonu | Görevdeki rolü |
|---|---|---|---|
| **Hesap Ustası** | el-Hârizmî'nin öğrencisi (kurgusal) | Kısa, sayıyla konuşur: "Önce say, sonra söyle." | Saha sorusunun sayı kısmını sorar; ders notunun "nasıl hesaplanır" sayfası |
| **Hayat Ustası** | İbnü'l-Baytâr'ın öğrencisi, bitki bilgini (kurgusal) | Sakin, gözlemci: "Toprağa bak, o anlatır." | Sorunun "neden" kısmı; ders notunun "ne oluyor" sayfası |

İkisi arada şakalaşır (ör. Hesap Ustası: *"Senin solucanların da bir sayısı var."* Hayat Ustası: *"Var, ama onlar sayıldıklarını bilmez."*). İki dersin birbirine ihtiyacı böylece hikâyede görünür.
Not: Gerçek kişilere tarihî iddia yüklenmez (12-ders-notu-arastirma §0-6 kuralı); ustalar kurgusaldır.

### 1.3 Görevler (konuşmalar)

**bereket_1 · Toprağın Canlıları (Nadas)**
- **Elif:** Toprak dinlendi, ama içi canlı mı, ölü mü bilmiyorum. Canlı toprak bereketli olur.
- **Hayat Ustası:** Toprağın canlılığını solucanı söyler. Ama bütün tarlayı kazamayız.
- **Hesap Ustası:** Kazmayız, tahmin ederiz. Üç küçük kare yeter.
- Bitiş: Elif öküzle sürer (kısa geçiş), parseller **Sürülmüş** olur. *"Dört binden fazla yardımcım varmış, haberim yokmuş!"*

**bereket_2 · Tohum Nefes Alır (Sürülmüş)**
- **Elif:** Bu tarlada 120 öbek filiz isterim. Tohum pahalı; fazla alırsam kesem boşalır, az alırsam tarla boş kalır.
- **Hesap Ustası:** Her tohum çimlenmez. Beklenen sayıyı bil, ona göre al.
- **Hayat Ustası:** Toprağı neden sürdüğümüzü de bil: tohum da nefes alır.
- Bitiş: Ekim yapılır, parseller **Ekili**. Fazla alınan kese rafta kalır (envanter kuralı), az alınırsa ekilmeyen parseller boş görünür (verim −1).

**bereket_3 · Işık ve Boy (Ekili)**
- **Elif:** Çardağın gölgesindeki iki parsel geri kaldı. Toprak aynı, su aynı. Neden?
- **Hayat Ustası:** Bitki besinini ışıkla yapar. Işık azsa besin az.
- **Hesap Ustası:** Ölç, tabloya yaz. Sayılar bir doğru çizer mi, bakalım.
- Bitiş: Gölgelik kaldırılır, parseller **Filiz**.

**bereket_4 · Tırtıl, Kuş, Şahin (Filiz)**
- **Elif:** Tırtıllar filizi yiyor! Komşu Osman zehir serpelim diyor.
- **Hayat Ustası:** Zehir yalnız tırtılı değil, onu yiyen kuşu da vurur. Kuşa yuva verirsen tırtılı o yer.
- **Hesap Ustası:** Kaç yuva? Kuşun iştahı belli, tırtılın sayısı belli.
- Bitiş: Kuşlar gelir, direkte bir şahin konar; parseller **Olgun**.
- İçerik kuralı: Zehir yalnızca söz olarak geçer, oyunda kullanılmaz; şiddet yok (kuş tırtılı "yer" — gösterilmez, tırtıl sayısı azalır).

**bereket_5 · Hasat Raporu (Olgun)**
- **Elif:** Bir sırayı gübresiz bırakmıştık, hatırladın mı? Şimdi gübre işe yaradı mı, görelim.
- **Hayat Ustası:** Gübresiz sıra bizim karşılaştırma ölçümüzdür.
- **Hesap Ustası:** Tartmadan "çok oldu" deme. Ortalamayı al, farkı söyle.
- Bitiş: parseller **Hasat**; köy biter. Elif: *"Bu yılın ekmeği senden."* Büyüme (CLAUDE.md §7): +0,01 m, "Biraz uzadın gibi…". Başarım: **Bereket Tarlası**.

---

## 2. Görev akışı ve mekanik (yürüyerek)

| Görev | Tahmin (kart) | Sahada görev (var olan sistemler) | Saha sorusu (Mat + Biyo) | Ustanın Sorgusu |
|---|---|---|---|---|
| **bereket_1** | "Bir avuç toprakta mı çok canlı vardır, bir avuç kumda mı?" → toprak | Tarlada 3 ahşap **örnek kare** (1 m × 1 m). Oyuncu her kareye bakıp Interact → kare yakınlaşır, solucanlar görünür; sayı kendisi sayar (10, 14, 12). Sayılar deftere düşer. | ① "Ortalama kaç solucan/m²?" → **12** ② "Tarla 24 m × 16 m. Tarlada yaklaşık kaç solucan?" → 384 × 12 = **4608** ③ (Biyo, seçmeli) "Ölü yaprağı toprağa mineral olarak geri verenler kimdir?" → **bakteri ve mantarlar (ayrıştırıcılar)** | "Neden tek kare yetmez?" → kareler farklı çıkıyor; ortalama daha güvenilir |
| **bereket_2** | "Toprağı sürmeden tohum ekseydik ne olurdu?" → az çimlenir (hava yok) | **Pazar tezgâhı** (var olan `MarketStall`): "Tohum kesesi (10 tohum), 2 akçe". Oyuncu kese sayısına kendisi karar verir, envantere alır; tarladaki **ekim sandığına** dökerek eker (kazan döküşü gibi; dökülen geri alınmaz). | ① "Çimlenme olasılığı 0,8. 120 filiz için kaç tohum?" → **150** → **15 kese** ② (Biyo) "Çimlenen tohum enerjiyi nasıl elde eder?" → **oksijenli solunum**: besin + O₂ → CO₂ + H₂O + enerji | "150 tohum ekersen kesin 120 filiz mi çıkar?" → hayır; beklenen değer ortalamadır, biraz az ya da çok çıkabilir |
| **bereket_3** | "Gölgedeki filiz mi uzun olur, güneşteki mi?" | İki deney parseli: biri **hasır gölgelik** altında. Oyuncu **ölçü çubuğuyla** (alet pazarından ya da çardaktan) dört işaretli filize bakar → boylar tabloya yazılır. Tablo: günlük ışık süresi 4, 6, 8, 10 saat → haftalık boy artışı 2, 3, 4, 5 cm. | ① "Günlük ışık 1 saat artınca boy artışı ne kadar değişir?" → **0,5 cm** ② "Kural: artış = ? × ışık saati" → **0,5** ③ (Biyo) "Gölgedeki filiz neden geride?" → **ışık az → fotosentez az → besin (glikoz) az** | "14 saat ışıkta 7 cm mi uzar?" → **kesin değil**: ışık bir yerden sonra sınırlayıcı olmaktan çıkar (su, CO₂, sıcaklık sınırlar); doğrusal kural yalnızca ölçülen aralıkta güvenilir |
| **bereket_4** | "Tırtılları zehirlersek kuşlara ne olur?" → zehirli tırtılı yiyen kuş da etkilenir | Tarlada tırtıl sayacı (Elif'in tahtası): **600 tırtıl**. Rafa benzer 6 **yuva direği** (var olan `ShelfSlot` mantığı): oyuncu çardaktan aldığı kuş yuvalarını direklere asar. Yeterli yuva → kuşlar gelir. | ① "Bir kuş günde 100 tırtıl yer. 600 tırtıl 2 günde bitsin: kaç kuş (yuva)?" → **3** ② "Ekin 10 000 kJ enerji tutuyor. Her basamağa yaklaşık %10 geçer: tırtıla, kuşa, şahine kaç kJ?" → **1000 / 100 / 10** ③ (Biyo) "Şahin neden az, tırtıl neden çok?" → **her basamakta enerjinin çoğu solunumla ısı olarak kaybolur** | "Kuşları kovsaydık?" → tırtıl çoğalır, ekin azalır; zincirin bir halkası koparsa ötekiler etkilenir |
| **bereket_5** | "Gübreli sıra mı çok ürün verir? Ne kadar çok?" (sayı tahmini, sonra gerçek değerle karşılaştırılır) | Oyuncu **orakla** (alet) 3 gübreli + 3 gübresiz parseli biçer (Interact). Çıkan çuvallar envantere girer; **3D terazide** (var olan `ScaleModel`) tek tek tartılır, kg deftere yazılır. Gübreli: 42, 46, 44 kg · Gübresiz: 30, 34, 32 kg. | ① "Gübreli ortalama?" → **44** ② "Gübresiz ortalama?" → **32** ③ "Gübre ürünü yüzde kaç artırdı?" → 12 / 32 = **%37,5** ④ (Biyo) "Gübresiz sırayı neden bıraktık?" → **kontrol grubu**: farkın gübreden geldiğini gösterir ⑤ (Mat, iki yönlü tablo) "40 kg ve üstü parsele *bereketli* diyelim. Gübreli 12 parselin 9'u, gübresiz 12 parselin 3'ü bereketli. Göreli sıklıklar? Gübre ile bereket ilişkili mi?" → **%75 ve %25 → ilişkili** | "Tek parseli tartsak yeter miydi?" → hayır; parseller arasında doğal fark var, ortalama gerekir |

**Saha sorusu kuralları** (04 §5.2'den, değişmedi): soru kartı açıkken zaman durmaz, 0,25×. Doğru → verim +1, iki usta mührüne birer çentik. Yanlış → usta ipucu verir, **yeni sayılarla** ikinci deneme (parametreler §2.1); yine yanlışsa cevap gösterilir, görev sürer, verim −1. Tıkanma yok.

### 2.1 Parametreler (ikinci deneme ve tekrar oynanış)
| Görev | Parametre | Aralık | Cevap ifadesi | Çeldiriciler (tipik hata) |
|---|---|---|---|---|
| bereket_1 | kare sayıları a, b, c · tarla L × W | a, b, c ∈ 8…16 (toplam 3'e bölünür) · L ∈ {20, 24}, W ∈ {12, 16} | ort = (a+b+c)/3 · ort × L × W | yalnızca a × L × W; L+W ile çarpma (çevre) |
| bereket_2 | p, hedef h, kese k | p ∈ {0,5; 0,6; 0,75; 0,8} · h ∈ {60, 90, 120} · k = 10 | h / p · ⌈h / p / k⌉ | h × p (96); h (120) |
| bereket_3 | eğim m, başlangıç | m ∈ {0,5; 1}, tablo 4 nokta | m | iki boyun farkı (gün farkına bölmeden); toplam boy |
| bereket_4 | tırtıl T, iştah r, gün g · enerji E | T ∈ {400, 600, 900} · r = 100 · g ∈ {2, 3} · E ∈ {1000, 10 000} | T / (r × g) · E × 0,1ⁿ | T / r (gün unutulur); %10 yerine her basamakta −10 |
| bereket_5 | iki grup × 3 değer | gübreli 38…48, gübresiz 26…36 | ortalamalar, (ō₁ − ō₂) / ō₂ × 100 | fark / ō₁ (yanlış taban); toplamları karşılaştırma |

Bütün sayılar tam ya da en fazla bir ondalık; 10. sınıf için hesap makinesi gerekmez.

---

## 3. Ders notları (eğitici kısım)

Biçim: her görev **4 sayfa = 2 açılış** (13 §2). Sayfa 1 Biyoloji konusu (Hayat Ustası), sayfa 2 Matematik yöntemi (Hesap Ustası), sayfa 3 çözümlü örnek (görevdekinden **farklı sayılarla**), sayfa 4 "Bu görevde" (cevabı vermez, ne yapılacağını söyler). Okuma ≤ 45 sn, her sayfa ≤ 3 kısa paragraf.
Blok kısaltmaları `LessonNotesData` ile aynı: **K** üst etiket · **T** başlık · **S** ara başlık · **B** gövde · **E** formül · **St** adımlar · **W** Dikkat (kavram yanılgısı) · **N** Unutma · **F** dipnot · **R** çizgi.
Kicker'da ders etiketi: `Ders notu · Biyoloji + Matematik`, ikinci satır `Bereket Tarlası, n. görev`.

### 3.1 bereket_1 · Toprağın Canlıları

**Sayfa 1 — Biyoloji: Toprak bir ekosistemdir** (Hayat Ustası)
- K: Ders notu · Biyoloji + Matematik — Bereket Tarlası, 1. görev
- T: Toprak canlıdır
- B: Bir ekosistemde canlı (biyotik) ve cansız (abiyotik) öğeler birlikte çalışır. Toprakta su, hava ve mineraller cansız öğelerdir; bitki kökleri, solucanlar, böcekler, mantarlar ve bakteriler canlı öğelerdir.
- S: Ölüler nereye gider?
- B: Ölü yaprak ve kökleri **ayrıştırıcılar** (bakteri ve mantarlar) parçalar; içlerindeki maddeler mineral olarak toprağa döner. Bitki bu mineralleri yeniden kullanır. Buna **madde döngüsü** denir.
- F: Solucan da ölü artıkları yer ve toprağı havalandırır; bu yüzden canlı, verimli toprağın işaretidir.

**Sayfa 2 — Matematik: Bütünü örnekten tahmin et** (Hesap Ustası)
- T: Sayamadığını tahmin et
- St:
  1. Bütünün küçük, eşit parçalarını seç (örnek kare).
  2. Her parçayı say.
  3. Sayıların ortalamasını al: toplam ÷ parça sayısı.
  4. Ortalamayı bütünün büyüklüğüyle çarp.
- R
- W: Dikkat — Tek kareye güvenme. Bir kare tesadüfen çok kalabalık ya da boş çıkabilir. Ne kadar çok kare sayarsan tahmin o kadar güvenilir olur.

**Sayfa 3 — Çözümlü örnek**
- K: Çözümlü örnek
- T: Koruda karınca yuvası
- B: Ormancı, korudaki 2 örnek alanda (her biri 100 m²) 3 ve 5 karınca yuvası saydı. Koru 2000 m².
- St:
  1. Ortalama: (3 + 5) ÷ 2 = 4 yuva / 100 m².
  2. Koru 2000 ÷ 100 = 20 örnek alan büyüklüğünde.
  3. Tahmin: 4 × 20 = **80 yuva**.
- N: Unutma — Bu bir tahmindir, kesin sayı değil. "Yaklaşık" deriz.

**Sayfa 4 — Bu görevde**
- K: Bu görevde — Çiftçi Elif, Bereket Tarlası
- T: Tarlada kaç yardımcı var?
- B: Tarlaya üç örnek kare konmuş. Her biri 1 m × 1 m.
- St:
  1. Her kareye bak, solucanları say. Sayılar defterine yazılır.
  2. Ortalamayı bul.
  3. Tarlanın alanını hesapla, tahminini Hesap Ustası'na söyle.
- R
- N: Unutma — Alan = en × boy. Çevreyle karıştırma.

### 3.2 bereket_2 · Tohum Nefes Alır

**Sayfa 1 — Biyoloji: Tohum da enerji harcar**
- K: Ders notu · Biyoloji + Matematik — Bereket Tarlası, 2. görev
- T: Tohum nefes alır
- B: Tohumun içinde depolanmış besin vardır. Tohum çimlenirken bu besini **oksijenli solunumla** kullanır ve büyümek için gereken enerjiyi elde eder.
- E: besin (glikoz) + O₂ → CO₂ + H₂O + enerji
- S: Toprağı neden süreriz?
- B: Sürülmüş toprak gevşektir; arasında hava (oksijen) ve su tutulur. Sıkışmış toprakta tohum yeterince oksijen bulamaz, çimlenme azalır.
- F: Bu denklemi tanıyorsun: yanmaya benzer (K-2). Ama hücrede yavaş ve adım adım olur; alev çıkmaz.

**Sayfa 2 — Matematik: Olasılık ve beklenen sayı**
- T: Kaç tanesi çimlenir?
- B: Çimlenme olasılığı 0,8 ise her 10 tohumdan yaklaşık 8'i çimlenir.
- St:
  1. **Beklenen sayı = olasılık × tohum sayısı.**
  2. Tersinden: istenen filiz sayısı biliniyorsa **tohum = filiz ÷ olasılık.**
  3. Tohum kesede satılıyorsa kese sayısını yukarı yuvarla.
- R
- W: Dikkat — 120 × 0,8 değil, 120 ÷ 0,8. Çarparsan daha az tohum bulursun ve tarla boş kalır. Sonucu kendine sor: "Bu kadar tohumun %80'i istediğim sayı mı?"

**Sayfa 3 — Çözümlü örnek**
- K: Çözümlü örnek
- T: Bostanda karpuz
- B: Bostancı Osman 45 karpuz fidesi istiyor. Karpuz tohumunun çimlenme olasılığı 0,9; tohum 10'luk paketlerde.
- St:
  1. Tohum = 45 ÷ 0,9 = 50.
  2. Kontrol: 50 × 0,9 = 45 ✓
  3. Paket: 50 ÷ 10 = **5 paket**.
- N: Unutma — Beklenen sayı ortalamadır. 50 tohumdan 44 de çıkabilir, 47 de.

**Sayfa 4 — Bu görevde**
- K: Bu görevde — Çiftçi Elif, Bereket Tarlası
- T: Tarla boş kalmasın
- B: Elif 120 öbek filiz istiyor. Pazarda tohum keselerini ve çimlenme olasılığını gör.
- St:
  1. Kaç tohum gerektiğini hesapla.
  2. Pazardan tam o kadar kese al.
  3. Keseleri ekim sandığına dök.
- R
- N: Unutma — Dökülen tohum geri alınmaz; fazla kese akçeni boşa harcar.

### 3.3 bereket_3 · Işık ve Boy

**Sayfa 1 — Biyoloji: Bitki besinini ışıkla yapar**
- K: Ders notu · Biyoloji + Matematik — Bereket Tarlası, 3. görev
- T: Işıktan besine
- B: Yeşil bitkiler **fotosentezle** kendi besinlerini üretir: ışık enerjisini kullanarak havadaki karbondioksit ve topraktaki sudan glikoz yapar, oksijen çıkarır.
- E: 6CO₂ + 6H₂O → C₆H₁₂O₆ + 6O₂ (ışık enerjisiyle)
- S: Ne yavaşlatır?
- B: Işık azsa fotosentez yavaşlar, bitki az besin yapar ve az büyür. Ama ışık bir yerden sonra artsa da hız artmaz; o zaman su, karbondioksit ya da sıcaklık sınırlayıcı olur.
- F: Denklemdeki 6'lar katsayıdır; terazide denkleştirdiğin gibi (K-1).

**Sayfa 2 — Matematik: Tablodan kural bul**
- T: Tablodan doğruya: eğim
- St:
  1. Ölçümleri tabloya yaz: ışık saati (yatay) ve boy artışı (dikey).
  2. Işık her arttığında boy artışı ne kadar değişiyor, bak.
  3. Artışlar hep aynıysa noktalar bir doğru üstündedir: **eğim = dikey değişim ÷ yatay değişim** (boy farkı ÷ ışık farkı).
  4. Kural: boy artışı = eğim × ışık saati.
- R
- W: Dikkat — Kural yalnızca ölçtüğün aralıkta güvenilir. Tablonun çok dışına çıkarsan (ör. 20 saat ışık) sonuç doğru olmayabilir.

**Sayfa 3 — Çözümlü örnek**
- K: Çözümlü örnek
- T: Fasulye ve su
- B: Bir fasulyeye günde 1, 2, 3 bardak su verildi; haftalık boy artışı 3, 6, 9 cm.
- St:
  1. Su 1 bardak artınca boy 3 cm artıyor.
  2. Oran: 3 ÷ 1 = 3 cm / bardak.
  3. Kural: artış = 3 × bardak.
- N: Unutma — 10 bardak su verirsen 30 cm uzamaz; kökü çürüyebilir. Canlıda doğrusallık sınırlıdır.

**Sayfa 4 — Bu görevde**
- K: Bu görevde — Çiftçi Elif, Bereket Tarlası
- T: Gölgedeki filizler
- B: İki parselde dört filiz işaretli; her biri farklı süre ışık aldı.
- St:
  1. Ölçü çubuğuyla her filize bak; boy artışı tabloya yazılır.
  2. Işık 1 saat artınca boy artışının nasıl değiştiğini bul.
  3. Hayat Ustası'na gölgedeki filizin neden geride kaldığını söyle.
- R
- N: Unutma — Önce farkları bul, sonra böl.

### 3.4 bereket_4 · Tırtıl, Kuş, Şahin

**Sayfa 1 — Biyoloji: Besin zinciri ve enerji akışı**
- K: Ders notu · Biyoloji + Matematik — Bereket Tarlası, 4. görev
- T: Kim kimi yer?
- B: Enerji ekosisteme güneşten girer. Üreticiler (bitkiler) onu besine çevirir; tüketiciler besini yiyerek alır. **Ekin → tırtıl → kuş → şahin** bir besin zinciridir.
- S: Enerji neden azalır?
- B: Her canlı aldığı enerjinin çoğunu yaşamak için harcar (solunum, hareket) ve ısı olarak kaybeder. Bir üst basamağa ortalama **yaklaşık %10'u** geçer. Bu yüzden zincirin tepesinde az canlı bulunur.
- F: Zararlıya karşı doğal düşmanını kullanmak **biyolojik mücadeledir**; zehir zincirin öteki halkalarına da zarar verebilir.

**Sayfa 2 — Matematik: Oran ve yüzde**
- T: Yüzde ile hesap
- St:
  1. %10 = 10 / 100 = 0,1.
  2. Bir basamaktan ötekine: enerji × 0,1.
  3. İki basamak yukarı: × 0,1 × 0,1 = × 0,01.
- R
- S: İş paylaştırma
- B: Toplam işi, birinin bir günde yaptığına ve gün sayısına böl: kişi = iş ÷ (günlük iş × gün).
- W: Dikkat — %10 "10 eksilt" demek değildir. 1000'in %10'u 100'dür, 990 değil.

**Sayfa 3 — Çözümlü örnek**
- K: Çözümlü örnek
- T: Göldeki zincir
- B: Su yosunu → küçük balık → büyük balık → balıkçıl. Yosunda 50 000 kJ enerji var.
- St:
  1. Küçük balık: 50 000 × 0,1 = 5000 kJ.
  2. Büyük balık: 5000 × 0,1 = 500 kJ.
  3. Balıkçıl: 500 × 0,1 = **50 kJ**.
- N: Unutma — %10 yaklaşık bir değerdir; gerçekte basamaktan basamağa değişir.

**Sayfa 4 — Bu görevde**
- K: Bu görevde — Çiftçi Elif, Bereket Tarlası
- T: Zehirsiz çare
- B: Elif'in tahtasında tırtıl sayısı yazıyor. Hayat Ustası bir kuşun günde kaç tırtıl yediğini biliyor.
- St:
  1. Tırtılların kaç günde bitmesini istediğine bak.
  2. Kaç kuş gerektiğini hesapla.
  3. Çardaktan o kadar yuva al, direklere as.
- R
- N: Unutma — Fazla yuva boş kalır, az yuva tırtılı yetiştiremez.

### 3.5 bereket_5 · Hasat Raporu

**Sayfa 1 — Biyoloji: Deney nasıl kurulur?**
- K: Ders notu · Biyoloji + Matematik — Bereket Tarlası, 5. görev
- T: Gübre işe yaradı mı?
- B: Bir şeyin etkisini anlamak için **deney grubu** ile **kontrol grubunu** karşılaştırırız. İki grupta her şey aynıdır (toprak, su, ışık, tohum); yalnızca incelediğimiz şey (gübre) farklıdır.
- S: Neden gübre?
- B: Bitki proteinlerini yapmak için azota ihtiyaç duyar ama havadaki azotu (N₂) doğrudan kullanamaz. Gübre, toprağa bitkinin alabileceği azotu verir (K-5).
- F: Değiştirdiğimiz şeye bağımsız değişken (gübre), ölçtüğümüz şeye bağımlı değişken (ürün kg) denir.

**Sayfa 2 — Matematik: Ortalama ve karşılaştırma**
- T: İki grubu karşılaştır
- St:
  1. Her grubun **aritmetik ortalamasını** bul: toplam ÷ değer sayısı.
  2. Ortalamaların farkını al.
  3. Yüzde artış = fark ÷ **kontrol grubunun ortalaması** × 100.
- R
- W: Dikkat — Yüzde artışta bölen, karşılaştırdığın başlangıçtır (gübresiz grup). Gübreli ortalamaya bölersen artışı küçük bulursun.
- S: İki yönlü tablo
- B: Parselleri iki soruya göre say: gübreli mi? bereketli mi? Her grubun içinde bereketlilerin oranını (göreli sıklık) bul. Oranlar çok farklıysa iki değişken **ilişkilidir**.

**Sayfa 3 — Çözümlü örnek**
- K: Çözümlü örnek
- T: Sulanan ve sulanmayan fide
- B: Sulanan 3 fide: 12, 15, 18 cm. Sulanmayan 3 fide: 8, 10, 12 cm.
- St:
  1. Ortalamalar: 45 ÷ 3 = 15 cm ve 30 ÷ 3 = 10 cm.
  2. Fark: 15 − 10 = 5 cm.
  3. Yüzde artış: 5 ÷ 10 × 100 = **%50**.
- N: Unutma — Tek fideye bakıp karar verme; fideler arasında doğal fark vardır.

**Sayfa 4 — Bu görevde**
- K: Bu görevde — Çiftçi Elif, Bereket Tarlası
- T: Hasat raporu
- B: Üç gübreli, üç gübresiz parsel işaretli.
- St:
  1. Orakla altı parseli biç.
  2. Her çuvalı terazide tart; kilolar defterine yazılır.
  3. Ortalamaları ve yüzde artışı bul, raporu Elif'e ver.
- R
- N: Unutma — Önce ortalama, sonra fark, en son yüzde.

---

## 4. Keşif Defteri kayıtları (K-11 … K-15)

Her kayıt iki dersi tek cümlede bağlar. Sayfa alanları `LearningEntry.WithPage` biçiminde; "Öteki derslerde" satırı yerine bu köyde **iki ana ders** yazılır.

| K | Görev | Yer satırı | Cümle (oyuncunun defterine) | Saha notu (Matematik) | Saha notu (Biyoloji) | Simge |
|---|---|---|---|---|---|---|
| K-11 | Toprağın Canlıları | Çiftçi Elif, Bereket Tarlası | Küçük karelerin ortalamasından bütün tarlayı tahmin edebilirim; toprağı canlı tutan, ölüleri mineral yapan ayrıştırıcılardır. | Örneklem: ortalama × alan = tahmin. | Ayrıştırıcılar maddeyi toprağa geri döndürür. | solucan |
| K-12 | Tohum Nefes Alır | Çiftçi Elif, Bereket Tarlası | Beklenen sayı = olasılık × deneme; çimlenen tohum besinini oksijenle yakar gibi kullanır, bu yüzden toprak havalı olmalı. | Tohum = filiz ÷ olasılık. | Oksijenli solunum: besin + O₂ → CO₂ + H₂O + enerji. | tohum |
| K-13 | Işık ve Boy | Çiftçi Elif, Bereket Tarlası | Işık arttıkça fotosentez ve büyüme artar, ama bir yere kadar; tablodaki doğrusal kural yalnızca ölçtüğüm aralıkta geçerli. | Eğim = dikey değişim ÷ yatay değişim. | Işık, fotosentez hızını etkileyen çevresel faktördür. | yaprak |
| K-14 | Tırtıl, Kuş, Şahin | Çiftçi Elif, Bereket Tarlası | Enerjinin yalnızca yaklaşık onda biri üst basamağa geçer; zinciri koparmadan zararlıyı doğal düşmanıyla durdurabilirim. | %10 = × 0,1. | Besin zinciri, biyolojik mücadele. | kuş |
| K-15 | Hasat Raporu | Çiftçi Elif, Bereket Tarlası | Bir şeyin etkisini kontrol grubuyla karşılaştırarak ve ortalama alarak anlarım: gübre ürünü yaklaşık üçte bir artırdı. | Yüzde artış = fark ÷ başlangıç × 100. | Deney ve kontrol grubu; bitki azotu gübreden alır. | başak |

Defter düzeni (`NotebookTexts`): yeni köy başlığı `bereket_tarlasi` → "Bereket Tarlası". Kayıtlar hem **Matematik** hem **Biyoloji** şeridinde listelenir (karar gerekli, §7).

---

## 5. Başarımlar

Sistem değişmeden: aynı göreve iki başarım, iki asset (koşul `TaskCompleted bereket_n`).

| Görev | Matematik sekmesi | Biyoloji sekmesi | Meydan okuma |
|---|---|---|---|
| bereket_1 | `ach_bereket_1_mat` Örnekten Bütüne | `ach_bereket_1_biyo` Toprağın Canlıları | — |
| bereket_2 | `ach_bereket_2_mat` Beklenen Sayı | `ach_bereket_2_biyo` Tohum Nefes Alır | *Tek Kese Fazla Yok* (gizli, `TaskFlawless`: tam 15 kese) |
| bereket_3 | `ach_bereket_3_mat` Tablodan Doğruya | `ach_bereket_3_biyo` Işıktan Besine | — |
| bereket_4 | `ach_bereket_4_mat` Yüzde On | `ach_bereket_4_biyo` Zehirsiz Çare | — |
| bereket_5 | `ach_bereket_5_mat` Hasat Raporu | `ach_bereket_5_biyo` Kontrol Grubu | *Defter Kapalı: Hasat* (gizli, `TaskWithoutHint`) |
| köy | `ach_bereket_tarlasi_mat` **Bereket Tarlası** | `ach_bereket_tarlasi_biyo` **Bereket Tarlası** | — |

Ağaç: Matematik ve Biyoloji sekmelerinde kök = bereket_1, tarla dalı 1 → 5 → köy. 11-gorev listesindeki eski `tarla_4/5/7/8/9` satırları bu dalla değiştirildi.

---

## 6. Level ve UI (Mustafa)

### 6.1 Sahne (Harita, Bereket Tarlası çevresi)
Konumlar 08-mustafa-entegrasyon ve 04 §11'den: tarla merkezi (60, 24) [Blender X/Y], giriş güney kapısı (60, 15,5), ahır önü y ≈ 10,5.

| Parça | Ne | Görev | Bağlantı noktası (boş obje / ad) |
|---|---|---|---|
| **Çardak** | Giriş kapısının içinde, sol tarafta; iki tabure, alçak masa, üstünde tartı ve defter. İki usta burada | hepsi | `Bereket_Cardak`, `Bereket_Usta_Hesap`, `Bereket_Usta_Hayat` (NPC duruş noktası) |
| **Örnek kare ×3** | 1 m × 1 m ahşap çerçeve, köşe kazıklı; içi kazılmış toprak, görünür solucanlar (10/14/12, kümeler halinde, sayılabilir) | 1 | `Bereket_OrnekKare_1…3` + her birinde `Bakis` (yakınlaşma kamerası) |
| **Ekim sandığı** | Tarla kenarında tahta sandık, kapaklı | 2 | `Bereket_EkimSandigi` |
| **Pazarda tohum kesesi** | Var olan tezgâhlardan birine "tohum" rafı; kese modeli (bez, ağzı bağlı) | 2 | Economy kataloğuna madde (Mehmet) |
| **Hasır gölgelik** | İki parselin üstünde dört direkli hasır; kaldırılabilir (açık/kapalı iki hâl) | 3 | `Bereket_Golgelik` (+ `_Kaldirilmis`) |
| **İşaretli filizler ×4** | Kazıkta renkli bez, yanında boy çizgisi | 3 | `Bereket_Filiz_1…4` |
| **Ölçü çubuğu** | Çentikli ahşap çubuk (alet; elde tutulur) | 3 | Alet pazarı ya da çardak |
| **Elif'in tahtası** | Tırtıl sayısının tebeşirle yazıldığı küçük kara tahta | 4 | `Bereket_Tahta` (TMP çapası `Tabela_Yazi_*` gibi) |
| **Yuva direkleri ×6** | Tarla kenarında direk, tepesinde askı; yuva (sepet/kutu) iki hâl: boş, asılı | 4 | `Bereket_YuvaDirek_1…6` (`ShelfSlot` mantığı) |
| **Kuşlar + şahin** | 3 kuş (basit uçuş döngüsü), direkte duran şahin; tırtıllar parsellerde küçük yeşil çizgiler | 4 | `Bereket_Kuslar`, `Bereket_Sahin` |
| **Gübresiz sıra kazıkları** | Bir sıranın başında "gübresiz" kazığı (simge yok, ad yazar — tabela kuralı 14.11) | 5 (kurulumu 2'de görünür) | `Bereket_Kazik_Gubresiz`, `_Gubreli` |
| **Orak** | Alet | 5 | Alet pazarı |
| **Çuval** | Biçilen parselden çıkan çuval (envanter maddesi) | 5 | — |
| **Parseller** | Var: 24 × 6 hâl (`Tarla_P<satır><sütun>_<Durum>`). Görev sonunda hepsi bir sonraki hâle; görev 5'te 3+3 işaretli parsel tek tek | hepsi | mevcut |

- Kurallar: 09-tema-ve-model-kuralları (palet renkleri, basite kaçmak yok, önce referans araştırması, hazır/üretilmiş model nihai olmaz). Her modelden önce referans: örnek kare (quadrat) arazi fotoğrafları, hasır gölgelik, ahşap kuş yuvası, orak.
- Okunurluk: örnek karedeki solucanlar ve tahtadaki sayılar 1,56 m göz yüksekliğinden, 1,5 m mesafeden okunmalı (CLAUDE.md §7).
- Kapsam: tarla zaten modelli. Yeni modeller küçük props; çardak en büyüğü.

### 6.2 UI (Figma → Unity view)
| Ekran | Ne | Interface (Mehmet) |
|---|---|---|
| **Saha sorusu kartı** | Kâğıt kart, köşede **iki rozet** (Mat dağ mavisi üçgen + Biyo yaprak yeşili yaprak); soru, sayı alanı ± (↑↓ / D-pad), seçmeli şıklar (Biyo kısmı), tablo/eksen alanı (görev 3, 5) | `IFieldQuestionView` |
| **Ders notu (iki usta)** | Var olan defter Ders Notu kipi; kicker'da "Biyoloji + Matematik", sayfa 1'de Hayat Ustası, sayfa 2'de Hesap Ustası küçük portresi | mevcut `ILessonView` (+ portre alanı) |
| **Defter tablosu** | Görev 1, 3, 5'te ölçülen sayılar defterde tablo olarak (el yazısı hissi) | `INotebookView` satır tipi "tablo" |
| **Ustalık sayfası** | Mat ve Biyo mühür çentikleri bu köyde dolmaya başlar | mevcut |
| **Usta portreleri** | Hesap Ustası, Hayat Ustası (konuşma balonu) | `IDialogueView` |

Figma'ya eklenecek kareler: `Saha Kartı/Mat+Biyo (sayı)`, `Saha Kartı/Mat+Biyo (tablo)`, `Ders Notu/bereket_1 (iki usta)`, `Defter/Keşiflerim — Bereket Tarlası`.

---

## 7. Kod ve veri (Mehmet) — öneri

| Parça | Ne | Süre |
|---|---|---|
| `LessonNotesData` | §3'teki 5 notu `For("bereket_n")` olarak ekle; §4 sayfaları `ApplyPage` | 45 dk (içerik hazır) |
| `FieldQuestionData` (SO) + saf C# ifade değerlendirici | 04 §9'daki yapı; §2.1 parametreleri; EditMode testi | 1,5 sa |
| `IFieldQuestionView` + denetleyici | Sayı/şık sorusu, ikinci deneme, verim ±1, iki mühre çentik | 1 sa |
| `FieldPlots` | 24 parselin hâlini değiştirir, kayda yazar (`SaveData`) | 1 sa |
| `FieldTaskRunner` | Görev akışı: ders notu → tahmin → sahada adımlar (sayaçlar: kare bakıldı, kese döküldü, yuva asıldı, çuval tartıldı) → soru → sorgu → defter | 2 sa |
| Var olanlara ekleme | Economy: tohum kesesi, yuva, çuval, orak, ölçü çubuğu; `ShelfSlot` yuva direği; `ScaleModel` çuval tartımı | 1 sa |
| Başarımlar | Kurulum → 13b listesine §5 | 20 dk |
| **Toplam** | | **≈ 7,5 sa** |

**Kararlar (Mehmet ile):**
1. Kimlikler `bereket_1…5` ve köy `bereket_tarlasi` — 11-gorev kimlik kuralına `bereket_n` eklenir.
2. Bir defter kaydı iki şeritte (Mat + Biyo) görünebilir mi? Olmazsa: Biyoloji şeridinde, Matematik şeridinde "saha notu" olarak.
3. Namespace: tarla için `BilgiAdasi.Field` (12-ilk-20 §"Karar gerekli" ile aynı soru).
4. Girdi: **yeni aksiyon yok.** Sayı alanı UI map'inde Navigate (↑↓ / D-pad), Submit (Enter / A), Cancel (Esc / B); sahadaki her şey Interact (E / X).

---

## 8. Eski tarla planıyla ilişki

| 04 §5 görevi | Bu planda |
|---|---|
| A4 Tohum Hesabı (Mat) | **bereket_2** içine girdi (+ Biyo solunum) |
| A5 Ekim Düzeni (Mat, sayma) | Çıkarıldı (Biyo bağı zayıf); istenirse traktör bölümüne |
| A7 Elif'in Gübresi (Biyo, azot) | **bereket_5** Sayfa 1'e ve K-5 bağına girdi |
| A8 Güneşten Besine (Biyo, fotosentez) | **bereket_3** (+ Mat değişim oranı); fotosentez denklemi ders notunda |
| A9 Hasat Raporu (Mat, istatistik) | **bereket_5** (+ Biyo kontrol grubu) |
| A1, A2, A3, A6, A10 (Kimya/Fizik, traktör) | Değişmedi; traktörle sonraki sürüm. Defter numaraları K-16'dan devam eder |

Olay soruları (04 §5.3 altı): "Kuş sürüsü" sorusu artık bereket_4'ün kendisi.

---

## 9. Öncelik ve 24 saat etkisi

- Bugün teslim (14.11, 11.00), kod dondurma 08.00. Toplam iş ≈ 7,5 sa kod + ≈ 6–8 sa model/UI → **teslime yetişmez. Öncelik P2 (Sürüm 2).**
- **Sunum için (P1, yalnızca görsel, kod yok):** çardak + örnek kareler sahnede dursun; Figma'da saha kartı ve bereket_1 ders notu ekranı slayta konsun. Anlatı: "Kimyadan sonra tarla, iki dersi birlikte öğretir."
- Teslimden sonra sıra: (1) ders notu verisi (içerik hazır) → (2) saha kartı + parsel → (3) bereket_1 dikey dilim → (4) kalan dört görev.

## 10. Müfredat dayanağı

Kodlar TYMM 10. sınıf Biyoloji ve Matematik programlarından doğrulanacak; doğrulanmayan kod veriye yazılmaz (12-ders-notu-arastirma kuralı). Tablo §10.1'de.

### 10.1 Görev ↔ öğrenme çıktısı
Kaynak: TYMM tema sayfaları, 14.11 okundu (Biyoloji: `tymm.meb.gov.tr/biyoloji-dersi/unite/8` Enerji, `/unite/10` Ekoloji · Matematik: `tymm.meb.gov.tr/matematik-dersi/unite/271, 274, 263, 250`). Sayfalar özetleyen bir araçla okundu; kod ve başlıklar görüldü **[doğrulandı]**, ayrıntı ifadeleri veriye yazılmadan önce sayfadan bir kez daha kontrol edilmeli.

| Görev | Biyoloji (10. sınıf) | Matematik (10. sınıf) | Not |
|---|---|---|---|
| bereket_1 | **BİY.10.2.1** ekosistemin cansız ve canlı bileşenleri · **BİY.10.2.3** enerji akışı (üretici/tüketici/ayrıştırıcı) · **BİY.10.2.4** madde döngüleri | **Tema 2 İstatistiksel Araştırma Süreci** (örneklemden evrene) — kısmi | Ortalama ve alan 10. sınıfta **ön bilgi**; görev bunları yeni bağlamda kullanır |
| bereket_2 | **BİY.10.1.7** hücresel solunum modeli · **BİY.10.1.8** oksijenli solunum | **Tema 7 Veriden Olasılığa** (deneysel olasılık) — kısmi | "Beklenen değer" 10. sınıf metninde yok **[doğrulanamadı]**; notta "beklenen sayı" sezgisel olarak, formül adı vermeden anlatılır |
| bereket_3 | **BİY.10.1.2** fotosentez modeli · **BİY.10.1.3** fotosentez deneyi (bağımlı/bağımsız değişken) · içerik çerçevesi "Fotosentez Hızına Etki Eden Çevresel Faktörler" | **MAT.10.6.2** doğrunun analitik incelemesi (eğim) · Tema 4 doğrusal fonksiyon | TYMM "değişim oranı" değil **eğim** diyor; ders notu buna göre yazıldı. "Sınırlayıcı etken" terimi metinde yok, "çevresel faktörler" var |
| bereket_4 | **BİY.10.2.3** enerji akışı, enerji piramidi · **BİY.10.2.2** komünite/popülasyon etkileşimleri | — (yüzde ve bölme **ön bilgi**) | "%10" ve "biyolojik mücadele" metinde yok **[doğrulanamadı]** → notta "yaklaşık" ve "genişletme" olarak; Matematik bağı en zayıf görev |
| bereket_5 | **BİY.10.1.3** deney tasarımı, bağımlı/bağımsız değişken · **BİY.10.2.4** azot döngüsü | **MAT.10.2.1** iki kategorik değişkenli veri, ilişkililiğe dayalı karar (iki yönlü tablo, göreli sıklık) | "Kontrol grubu" terimi metinde yok; değişken dili var. Ortalama ön bilgi; **⑤ iki yönlü tablo sorusu** doğrudan MAT.10.2.1 |

**Sonuç:** Biyoloji bağları beş görevde de doğrudan bir 10. sınıf çıktısına oturuyor. Matematikte doğrudan çıktı **bereket_3 (eğim)** ve **bereket_5 (iki kategorik değişken)**; bereket_1, 2, 4 ön bilgiyi (ortalama, alan, olasılık, yüzde) yeni bağlamda kullanır. Sunumda bu dürüstçe söylenir: "Matematik burada araç; Biyoloji konusu."
Bulunamayanlar (veriye kod yazılmaz): beklenen değer, %10 kuralı, biyolojik mücadele, kontrol grubu, sınırlayıcı etken.

## 11. Öğretmen gözüyle kontrol listesi

- [ ] "Ayrıştırıcı" yalnızca bakteri ve mantar için; solucan "ölü artıkla beslenen, toprağı havalandıran" olarak geçiyor (yaygın yanılgı).
- [ ] Fotosentez denklemi katsayılı ve denk: 6CO₂ + 6H₂O → C₆H₁₂O₆ + 6O₂.
- [ ] %10 kuralı "yaklaşık" diye veriliyor.
- [ ] Beklenen değerin kesin sonuç olmadığı söyleniyor (bereket_2 sorgusu).
- [ ] Doğrusal modelin sınırı söyleniyor (bereket_3 sorgusu + Dikkat).
- [ ] Yüzde artışta taban kontrol grubu (bereket_5 Dikkat).
- [ ] Hiçbir sayfada cevap verilmiyor; "Bu görevde" yalnızca ne yapılacağını söylüyor.
- [ ] Zehir oyunda kullanılmıyor, yalnızca söz olarak; şiddet yok.
- [ ] TYMM terimleri: "cansız ve canlı bileşenler" (biyotik/abiyotik parantez içinde), "eğim", "bağımlı/bağımsız değişken", "göreli sıklık".
- [ ] Veriye yalnızca §10.1'de [doğrulandı] olan kodlar yazılıyor.
