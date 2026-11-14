# 04 — Görev Planı (seviye seviye): eğitsel katmanlar, Kimya köyleri, Tarla, Orman, Pazar

> Durum: **tasarım** (13.11.2026, Mustafa + Claude). Kod Mehmet'in alanıdır; bu belge her seviyede ne olacağını, ekrana gelen soruları, defter kayıtlarını, veriyi ve sahne bağlantı noktalarını tanımlar.
> Bağlı belgeler: `01-oyun-tasarimi.md` (hikâye) · `02-siparis-mekanigi.md` (kazan + terazi) · `03-masa-mekanigi.md` (kâğıt soruları) · `04_Icerik/05-icerik-prototip.md` (D-1…D-5, T-1…T-5 verisi; **bu belge oradaki satırları değiştirmez, üstlerine katman ekler**) · `ArtSource/Referans/yon_bulma.md` (şehir renkleri ve simgeleri).
> Kazanım adları: TYMM 10. sınıf resmî sayfaları (§12). Fizik ve Biyoloji'de kod biliniyorsa yazıldı; Kimya ve Matematik'te tema adı kullanıldı.

## 0. Kapsam, öncelik ve 24 saat etkisi

Teslim (14.11, 11.00; kod dondurma 08.00) **Kimya şehrinin iki köyüdür** (Bölüm 1, Seviye 0–10). Bölüm 2–4 (tarla, orman, pazar) sonraki sürümdür; yarışmada modeller ve bu planla "oyunun vaadi" olarak gösterilir.

| Parça | Öncelik | Kim | Tahmini süre | Neden bu öncelik |
|---|---|---|---|---|
| **Keşif Defteri** (görev sonunda kayıt + defterde "Keşiflerim" sekmesi) | **P1** | Mehmet (kayıt ekleme), Mustafa (sayfa görünümü) | 1,5 sa + 1 sa | Var olan ipucu defterinin ikinci bölümü; veri metin |
| **Ustanın Sorgusu** (başarıdan sonra 3 seçenekli "neden?") | **P1** | Mehmet | 1 sa | Masa görevlerinin `MultipleChoice` altyapısını kullanır |
| **Tahmin** adımı (görev öncesi tek soru) | P2 | Mehmet | 1 sa | Aynı `MultipleChoice` |
| **Hata Dedektifi** (tahtada hatalı denklem, dengesiz elementi işaretle) | P2 | Mehmet + Mustafa (tahta) | 2 sa | `IChalkboardView` genişler |
| **Atom Görüşü** (tepkimede atomların yeniden dizilişini gösteren kısa canlandırma) | P2 | Mehmet + Mustafa | 2–3 sa | Teraziler atomları zaten gösteriyor; bu, kazanın içini açar |
| **Saha notları** (Mat / Biyo / Fizik çapraz soruları) | Sürüm 2 | ikisi | — | Kilitli şehirlerin mühür sistemiyle birlikte gelir |
| Bölüm 2–4 (tarla, orman, pazar) | Sürüm 2 | ikisi | modeller hazır | `Art/Traktor/`, `Art/Harita/Bereket_Tarlasi.fbx`, `Ormanci_Korusu.fbx` |

Vakit kalmazsa sıra: Keşif Defteri → Ustanın Sorgusu → (gerisi sunumda anlatılır).

## 1. Öğrenme döngüsü (her seviyede aynı iskelet)

Her seviye, bilinen **Tahmin et → Gözle → Açıkla** yaklaşımına bir "kaydet" ve "bağla" adımı eklenmiş hâlidir. Yeni görev eklenmez; var olan her göreve **küçük bir düşünme aşaması** eklenir.

```
1 TAHMİN    görev başlamadan tek soru: "ne olacak / ne gerekir?"            (P2)
2 GÖREV     var olan mekanik: kazan + terazi ya da kâğıt                     (mevcut)
3 KATMAN    o görevin özel düşünme aşaması: Atom Görüşü / Hata Dedektifi /
            ara ürün / artan madde / kanıt / iddia düzeltme / transfer      (P2)
4 SORGU     Câbir Usta: "Peki çırak, neden…?" — 3 açıklama, 1 doğru          (P1)
5 DEFTER    Keşif Defteri'ne o görevin bilimsel çıkarımı yazılır              (P1)
6 BAĞLA     saha notu: aynı olayın Matematik / Biyoloji / Fizik yüzü          (Sürüm 2)
7 DÜNYA     köy parçası canlanır (NeighborhoodView Part_n — mevcut)
```

Her katman **tek tıklık / tek seçimlik** kalır; sınav hissi yok, akış kesilmez. Ustanın Sorgusu her görevde değil, aşağıdaki tabloda işaretli görevlerde gelir (yorgunluk olmasın: 10 görevde 6 sorgu).

## 2. Dört eğitsel katman

### 2.1 Keşif Defteri — kendi kimya kitabını yazmak
- **Ne:** Seviye 0'da Câbir Usta çırağa **boş bir defter** verir. Mevcut "Ustanın Defteri" (ipucu, `IHintBookView`) ile aynı kitaptır; iki ayraç: **Ustanın Notları** (ipucu, mevcut) · **Keşiflerim** (yeni).
- **Kayıt:** Her görev bitince bir cümle mürekkeple yazılır (canlandırma: kalem çizer, ~1,5 sn). Kayıt numaralı: `K-0`, `K-1`…
- **Kullanım (bilgiler sonra işe yarar):**
  1. **İpucu bağlantısı:** 2 hatadan sonra defter parlar; ustanın ipucunun altında ilgili eski keşif de gösterilir ("Sayfa K-2'ye bak").
  2. **Kanıt göster:** Bazı sorularda bir seçenek "Defterimden kanıt göster"dir; oyuncu doğru kaydı seçer (T-3, T-4).
  3. **Sorgu:** Ustanın Sorgusu'nun doğru seçeneği çoğunlukla bir önceki keşfe dayanır; yanlışlar tipik kavram yanılgılarıdır.
- **Öğrettiği:** Önceki bilgiyi kullanma, ilişkilendirme, kalıcı öğrenme. Prototip sonunda defter, oyuncunun **kendi yazdığı kimya kitabı** olarak son ekranda açılır (mevcut "oyuncunun tahtası" ekranının yanına).

### 2.2 Hata Dedektifi — yanlışın nedenini bulmak
- **Ne:** Câbir Usta tahtaya bilerek hatalı bir denklem bırakır. Oyuncu denklemi **düzeltmez**; önce hangi element(ler) açısından dengesiz olduğunu **işaretler** (element düğmeleri: H, O, C, N, Ca — açık/kapalı), sonra "Neden?" için bir cümle seçer.
- **Kontrol:** İşaretlenen küme = dengesiz elementler kümesi. Yanlışta tahtada o elementin iki taraftaki sayısı tebeşirle yazılır (gösterir, söylemez).
- **Öğrettiği:** Ezber işlem yerine hata analizi; "her elementi iki kefede ayrı say" alışkanlığı.

### 2.3 Ustanın Sorgusu — sonucu açıklamak
- **Ne:** Başarıdan hemen sonra Usta kısa bir soru sorar ("Peki çırak, neden iki su molekülü elde ettin?"). Üç açıklama; biri bilimsel olarak doğru, ikisi tipik yanılgı.
- **Sınav değil:** Yanlışta görev geri alınmaz; usta yanılgıyı tek cümleyle düzeltir ve doğru açıklama deftere eklenir. Doğruda "Aynen öyle." ve kayıt.
- **Öğrettiği:** Doğru işlemin ötesinde gerekçelendirme.

### 2.4 Atom Görüşü — kazanın içini görmek
- **Ne:** Tepkime anında (başarı canlandırması, ~3 sn) kazanın üstünde moleküller atom modeli olarak açılır; bağlar kopar, atomlar yeniden dizilir. Altta sayaç: `H 4 → 4 · O 2 → 2`. Sonradan istasyonda açılıp kapatılabilir.
- **Görsel dil:** Atomlar kavanoz mühürlerindeki kabartma koduyla aynı: **H küre/beyaz · O halka/kırmızı · C küp/koyu gri · N piramit/mavi · Ca altıgen/açık yeşil** (`ArtSource/Referans/esya.md`). Renk tek başına bilgi taşımaz.
- **Öğrettiği:** Kütlenin korunumu: atomlar kaybolmaz, yalnızca yeniden dizilir.

### 2.5 Saha notu ve mühür (çapraz ders — Sürüm 2)
Kilitli şehirlerin ustaları (Matematik: Hesap Ustası · Fizik: Makine Ustası · Biyoloji: Hayat Ustası; ayrıntı §6) aynı olayın kendi derslerindeki yüzünü sorar. Doğru saha notu, o şehrin kapı madalyonunda bir **mühür çentiği** doldurur; 10 çentik → şehir açılır. Kimya köylerinde saha notları "tadımlık"tır (her seviyede 1); tarla ve ormanda görevin kendisidir.

## 3. Seviye haritası (özet)

| Sv | Yer | Görev | Katman | Sorgu | Defter | Kimya | Matematik | Biyoloji | Fizik |
|---|---|---|---|---|---|---|---|---|---|
| 0 | Atölye | Çıraklık: İlk Kural | Atom Görüşü (tanıtım) | ✔ | K-0 | korunum | — | — | — |
| 1 | Denk · çeşme | D-1 Kuru Çeşme | **Atom Görüşü** | ✔ | K-1 | 2H₂+O₂→2H₂O | oran 2:1 | su döngüsü | — |
| 2 | Denk · fener | D-2 İsli Fenerler | **Tahmin: yanma için ne gerekir** | ✔ | K-2 | CH₄ yanması | sayma tablosu | hücresel solunum | kimyasal→ısı+ışık |
| 3 | Denk · fırın | D-3 Soğuk Fırın | **Hata Dedektifi** | — | K-3 | C₃H₈ yanması | orantı | fermantasyon | — |
| 4 | Denk · köprü | D-4 Yıkık Köprü | **Ara ürün: 1. adımın ürünü 2. adımda** | ✔ | K-4 | CaCO₃→CaO→Ca(OH)₂ | iki adımlı işlem | karbon döngüsü | ısı veren tepkime |
| 5 | Denk · tarla | D-5 Gübre Ruhu | **En az akçe + artan madde** | ✔ | K-5 | N₂+3H₂→2NH₃ | bütçe (doğrusal maliyet) | azot döngüsü | — |
| 6 | Tepk · masa | T-1 Ustanın Defteri | **Gözlemden çıkarım** | — | K-6 | tür tanımları | — | — | — |
| 7 | Tepk · masa | T-2 Etiket Masası | **Gözlem ↔ tür eşleme** | — | K-7 | sınıflandırma | sıklık tablosu + grafik | solunum ≈ yanma | — |
| 8 | Tepk · masa | T-3 Fırıncının Mektubu | **Kimyasal değişim kanıtı** | ✔ | K-8 | yanma ürünleri | — | maya fermantasyonu | — |
| 9 | Tepk · masa | T-4 Muhtarın Mektubu | **Yanlış iddiayı düzelt** | — | K-9 | korunum | karşı örnek | fotosentez yanılgısı | — |
| 10 | Tepk · lonca | T-5 Lonca Sınavı | **Transfer: yeni örnekler** | ✔ | K-10 | yeni denklemler | olasılık | fotosentez/solunum | — |
| 11–20 | Bereket Tarlası | A1–A10 | §5 | — | K-11…K-20 | yakıt, toprak, gübre | alan, olasılık, algoritma, istatistik, oran | azot, fotosentez, fermantasyon | hız, iş-güç, enerji kaynakları |
| 21–26 | Ormancı Korusu | B1–B6 | §6 | — | K-21…K-26 | yanma | açı, yüzde, olasılık | sürdürülebilirlik | mekanik enerjinin korunumu, iş-güç |
| 27–29 | Çarşı | C1–C3 | §7 | — | K-27…K-29 | — | oran, yüzde, analitik | — | — |

---

## 4. Bölüm 1 — Kimya şehri (prototip, Seviye 0–10)

Köy sırası serbest (01 §3). Masa köyü (T) önce oynanırsa, Kimya köyüne dayanan gözlem kartları ustanın tahtasındaki hazır denklemlerden gelir (veri `ReactionData`'da zaten var).

### Seviye 0 — Çıraklık: "İlk Kural" (atölye, eğitim)
- **Hikâye:** Uyanış sahnesinin sonu (01 §1). Usta çırağa **boş defteri** verir: *"Gördüğünü yaz. Gördüğün şey kazanda kaybolmaz."*
- **Akış:**
  1. *Tahmin:* Kazana iki kavanoz H₂ konur. "Kazanı kapatıp ısıtırsam içindeki hidrojen azalır mı?" → **Hayır, aynı kalır** / Azalır, buhar olur / Artar.
  2. *Görev:* Oyuncu mühürlü kavanozu kazana sürükler, `−/+` sayacı, terazideki atom kürelerini görür (kontrol eğitimi). Mühür notu (02 §3.4) burada bir kez gösterilir.
  3. *Katman — Atom Görüşü (tanıtım):* Usta kazanı kısa ısıtır; atom görüşünde aynı 4 H'nin kazanda dolaştığı görülür, sayaç `H 4 → 4`.
  4. *Sorgu:* "Terazinin iki kefesi neden eşit kaldı?" → ✔ **Atomlar yok olmaz, yoktan da var olmaz.** · ✘ "Kazan kapalı olduğu için hava girmedi." (yalnız koşula bakma) · ✘ "Hidrojen hafif olduğu için tartılmaz." (kütle-ağırlık yanılgısı)
  5. *Defter K-0:* **"Kazana ne girerse o çıkar: tepkimede atomlar kaybolmaz, yoktan var olmaz."**
- **Dünya:** Tahtaya ilk satır: `Kural: ne girerse o çıkar.`

### Seviye 1 — D-1 Kuru Çeşme (Muhtar Yusuf · R_water · Exact)
- **Hikâye ve veri:** 05 §2 satır 1 (değişmez).
- **Akış:**
  1. *Tahmin (P2):* "Su yapmak için kazana ne koymalıyız?" → **H₂ ve O₂** / Yalnızca H₂ / H₂ ve CO₂.
  2. *Görev:* `2H₂ + O₂ → 2H₂O` (sipariş kartı girenleri gösterir).
  3. *Katman — **Atom Görüşü**:* Başarı canlandırmasında 2 H–H ve 1 O=O bağı kopar, 2 H–O–H dizilir. Sayaç: `H 4 → 4 · O 2 → 2`. Oyuncu bir kez "tekrar oynat"a basabilir.
  4. *Sorgu:* **"Peki çırak, neden iki su molekülü elde ettin?"**
     - ✔ "Bir O₂'de iki oksijen atomu var; her su bir oksijen alıyor, o yüzden iki su oluşuyor."
     - ✘ "Suyun formülü 2H₂O'dur." *(katsayıyı formülün parçası sanma)*
     - ✘ "İki hidrojen kavanozu koydum, oksijenin sayısı önemli değil." *(tek elemente bakma)*
  5. *Defter K-1:* **"Tepkimede atomların türü ve sayısı korunur; değişen, atomların birbirine bağlanışıdır. Su oluşurken her 2 H₂ için 1 O₂ harcanır."**
  6. *Saha notları (Sürüm 2):*
     - **Matematik · Sayılar (oran):** "Muhtar {6} su isteseydi kaç H₂, kaç O₂ gerekirdi?" → **6 H₂, 3 O₂** (2:1:2 oranı).
     - **Biyoloji · Ekoloji › Madde döngüleri (su):** "Dere neden kurudu? Su döngüsünde hangi adım eksik?" → **yağış** (buharlaşma–yoğuşma–yağış).
- **Dünya:** Part_1 — çeşme akar (`Denk_P1_Cesme_Su`).

### Seviye 2 — D-2 İsli Fenerler (Fenerci Demir · R_methane · Exact)
- **Akış:**
  1. *Tahmin — **yanmadan önce ne gerekir** (katman):* Fenerin kapağı açık; "Fener temiz yansın diye neler gerekir? (birden çok seç)" → ✔ **CH₄ (yakıt)** · ✔ **O₂ (hava)** · ✔ **kıvılcım/ısı** · ✘ H₂O · ✘ CO₂. Yanlış seçimde fener "puf" diye söner ve kurum çıkar (gösterir, söylemez).
  2. *Görev:* `CH₄ + 2O₂ → CO₂ + 2H₂O`.
  3. *Sorgu:* "Neden bir değil iki O₂ koydun?"
     - ✔ "CO₂'deki 2 ve iki suyun içindeki 2 oksijen eder 4; her O₂ iki oksijen getirir."
     - ✘ "Metan çok yanıcı, fazladan hava iyi gelir." *(nitel sezgi, sayım yok)*
     - ✘ "Karbon iki oksijen istiyor, bir O₂ yeter." *(suyu unutma)*
  4. *Defter K-2:* **"Yanma tepkimelerinde oksijenin rolü vardır: yakıt O₂ ile birleşir. Karbon ve hidrojen içeren yakıtlar CO₂ ve H₂O verir. Hava yetmezse is (karbon) ve CO çıkar."**
  5. *Saha notları:*
     - **Biyoloji · Enerji › Hücresel solunum:** "Hücrelerin besini 'yakması' fenere neden benzer?" → **ikisinde de O₂ kullanılır, CO₂ ve H₂O çıkar; hücrede yavaş ve kontrollü olur.**
     - **Fizik · Enerji › Enerji biçimleri (FİZ.10.2.3):** "Fenerde hangi enerji hangisine dönüşür?" → **kimyasal → ısı + ışık.**
     - **Matematik · Sayma:** Atom sayım tablosunu doldur (C, H, O × giren/ürün) — tablo okuma.
- **Dünya:** Part_2 — fenerler yanar.

### Seviye 3 — D-3 Soğuk Fırın (Fırıncı Zehra · R_propane · Exact)
- **Akış:**
  1. *Katman — **Hata Dedektifi** (iki tur):*
     - **Tur 1 (geçmişe bağ):** Usta dünkü fener denkleminin **hatalı kopyasını** tahtaya bırakır: `CH₄ + O₂ → CO₂ + H₂O`. "Hangi elementler dengesiz?" → ✔ **H** (4 / 2) ve ✔ **O** (2 / 3); C dengeli (1 / 1). Ardından "Neden?" → ✔ "Suyun ve oksijenin katsayısı unutulmuş." · ✘ "Metanın formülü yanlış yazılmış." *(alt indisle oynama yanılgısı)*
     - **Tur 2 (bugünün yakıtı):** `C₃H₈ + 5O₂ → CO₂ + 4H₂O` → ✔ **C** (3 / 1) ve ✔ **O** (10 / 6); H dengeli (8 / 8). Örüntü farklı; ezberle geçilemez.
  2. *Görev:* `C₃H₈ + 5O₂ → 3CO₂ + 4H₂O` (kart girenleri göstermez — 05).
  3. *Sorgu:* yok (Hata Dedektifi bu seviyenin düşünme aşaması).
  4. *Defter K-3:* **"Dengesizliği bulmak için her elementi iki kefede ayrı say. Denkleştirirken önce tek yerde geçen elementler (C, H), oksijen en son."**
  5. *Saha notları:*
     - **Matematik · Sayılar (orantı):** "1 propan 5 O₂ ister. Fırın bir günde {3} propan yakarsa kaç O₂ gerekir?" → **15**.
     - **Biyoloji · Enerji › Fermantasyon:** "Zehra'nın hamuru neden kabarır?" → **maya, etil alkol fermantasyonunda CO₂ üretir.**
- **Dünya:** Part_3 — fırında kor, bacada duman.

### Seviye 4 — D-4 Yıkık Köprü (Duvarcı Halil · R_lime1 → R_lime2 · Exact, 2 adım)
- **Akış:**
  1. *Görev adım 1:* `CaCO₃ → CaO + CO₂` (katsayı gerekmez — 05 ipucu: "Her denklem katsayı istemez").
  2. *Katman — **ara ürün**:* Adım 1 bitince rafa yeni bir kavanoz (CaO) düşer ve hafif parlar. Usta: *"Harç için su ile neyi birleştireceksin?"* Raf: CaCO₃, CaO, H₂O → ✔ **CaO** (az önce sen yaptın). Yanlışta (CaCO₃) terazi dengelenmez; tahtada 1. adım satırı parlar.
  3. *Görev adım 2:* `CaO + H₂O → Ca(OH)₂`.
  4. *Sorgu:* "Kireç taşının içindeki karbon nereye gitti?"
     - ✔ "CO₂ olarak havaya karıştı; atomları kaybolmadı, başka bir maddede." *(K-0'a bağ)*
     - ✘ "Isıyla yanıp yok oldu." *(korunum yanılgısı)*
     - ✘ "CaO'nun içinde saklı duruyor." *(ürünü yanlış okuma)*
  5. *Defter K-4:* **"Bir tepkimenin ürünü başka bir tepkimenin gireni olabilir (ara ürün). Kireç: CaCO₃ → CaO → Ca(OH)₂."**
  6. *Saha notları:*
     - **Biyoloji · Ekoloji › Madde döngüleri (karbon):** "Harç duvarda yıllarca havadaki CO₂'yi alıp yeniden taşa döner: `Ca(OH)₂ + CO₂ → CaCO₃ + H₂O`. Karbon nereden çıkıp nereye döndü?" → **taştan havaya, havadan yeniden taşa (döngü).**
     - **Fizik · Enerji › Enerji biçimleri (FİZ.10.2.3):** "Kireç söndürülürken kova neden ısınır?" → **tepkime kimyasal enerjiyi ısı olarak verir.** (05'teki güvenlik kartıyla birlikte.)
     - **Matematik · Nicelikler ve Değişimler:** iki adımlı işlem: "1. adımın çıktısı 2. adımın girdisi" — bileşke işlem fikri (ok diyagramı).
- **Dünya:** Part_4 — taş köprü onarılır.

### Seviye 5 — D-5 Gübre Ruhu (Çiftçi Elif · R_ammonia · Purchase)
- **Akış:**
  1. *Tahmin — **bütçe** (katman):* Sandıkta N₂ = 3 akçe, H₂ = 2 akçe, bütçe 20. "4 NH₃ için en az kaç akçe harcarsın?" → **18** (2 N₂ + 6 H₂ = 6 + 12). Seçenekler: **18** · 9 *(tek denklemin hesabı: 1 N₂ + 3 H₂ yalnız 2 NH₃ verir, ikiyle çarpılmamış)* · 20 *(bütçenin hepsini harcamak gerekir sanma)*.
  2. *Görev:* Purchase modu (02 §3.2): satın al, kazana koy. Fazla alınan **artan madde** olarak terazide ayrı durur (mevcut kural).
  3. *Katman — **artan madde**:* Oyuncu fazladan N₂ aldıysa (ya da ikinci denemede bilerek bir N₂ fazla koyarsa) Atom Görüşü'nde H₂ biter, bir N₂ eşsiz kalır. Etiket: *"artan."*
  4. *Sorgu:* "Fazladan aldığın N₂ neden kazanda kaldı?"
     - ✔ "H₂ bitti; N₂ eşleşecek hidrojen bulamadı, tepkime durdu."
     - ✘ "Kazan doldu, sığmadı." *(hacim yanılgısı)*
     - ✘ "N₂ ağır olduğu için dibe çöktü, karışmadı." *(fiziksel karışma ile tepkimeyi karıştırma)*
  5. *Defter K-5:* **"Tepkime girenleri katsayıları oranında harcar. Biri biterse tepkime durur; fazla olan artar."**
  6. *Saha notları:*
     - **Matematik · Nicelikler ve Değişimler (doğrusal maliyet):** "Maliyet = 3·(N₂ sayısı) + 2·(H₂ sayısı). {8} NH₃ için en az?" → **36**.
     - **Biyoloji · Ekoloji › Azot döngüsü:** "Bitki havadaki N₂'yi neden doğrudan kullanamaz, gübre ne işe yarar?" → **bitki azotu bağlanmış hâlde (amonyum/nitrat) alır; gübre bu azotu toprağa verir.**
- **Dünya:** Part_5 — tarlada ekin. Köy tamamlanır; Elif tarlayı gösterir (Bölüm 2'nin tetiği, Sürüm 2).

### Seviye 6 — T-1 Ustanın Defteri (FillBlank)
- **Akış:**
  1. *Katman — **gözlemden çıkarım**:* Kâğıt açılmadan önce masaya üç **gözlem kartı** gelir (oyuncunun kendi tahtasından; yoksa ustanın): *Çeşme:* "iki gaz birleşti, tek ürün oldu" · *Köprü 1. adım:* "tek madde ısıtıldı, iki maddeye ayrıldı" · *Fener:* "yakıt oksijenle birleşti, ısı ve ışık çıktı". Oyuncu her kartı bir boşluğun yanına sürükler (hangi gözlem hangi tanıma kanıt).
  2. *Görev:* 05'teki üç boşluk (sentez · analiz · oksijen/yanma), kelime bankası değişmez.
  3. *Defter K-6:* **"Sentez: birden çok madde → tek ürün. Analiz: tek madde → daha basit maddeler. Yanma: madde + O₂ → ısı ve ışık. (Kanıtlarım: çeşme, kireç taşı, fener.)"**
- **Dünya:** Tepkime köyü Part_1 (güvercinler).

### Seviye 7 — T-2 Etiket Masası (Classify)
- **Akış:**
  1. *Katman — **gözlem ↔ tür eşleme**:* 05'teki 6 denklem kartına ek olarak 3 **gözlem kartı** (formülsüz): "Kireç taşı ısıtılınca gaz çıktı, beyaz toz kaldı" → Analiz · "Fenerin alevi ısıttı, cam buğulandı" → Yanma · "İki gaz kazanda birleşti, damla damla su oldu" → Sentez.
  2. *Görev:* Kutular Sentez · Analiz · Yanma (mevcut).
  3. *Defter K-7:* **"Türü anlamak için: tek giren varsa analiz, tek ürün varsa sentez, girenlerde O₂ var ve ısı-ışık çıkıyorsa yanma."**
  4. *Saha notları:*
     - **Matematik · İstatistiksel Araştırma Süreci:** Kasabanın 8 tepkimesinin (`ReactionData`) türlere göre sıklık tablosu ve sütun grafiği → **Sentez 3 · Analiz 2 · Yanma 3**; "En az görülen tür?" → **analiz**.
     - **Biyoloji · Enerji › Hücresel solunum:** "Glikoz + O₂ → CO₂ + H₂O hangi türe benzer?" → **yanmaya; hücrede enzimlerle, adım adım ve yavaş gerçekleşir.**
- **Dünya:** Part_2 (lonca flamaları).

### Seviye 8 — T-3 Fırıncının Mektubu (MultipleChoice)
- **Akış:**
  1. *Görev:* 05'teki soru (propan → **karbondioksit ve su**). İpucu bağlantısı: K-2.
  2. *Katman — **kimyasal değişimin kanıtı**:* Zehra'nın ikinci satırı: *"Ekmek kabarıp kızarıyor; bu da mı kimyasal? Kanıt göster."* Oyuncu kanıt kartlarından seçer (birden çok): ✔ **gaz kabarcıkları (CO₂)** · ✔ **kabuğun renk değiştirmesi** · ✔ **koku (yeni madde)** · ✘ **hamurun yoğrulup şekil değiştirmesi** *(fiziksel)* · ✘ **suyun buharlaşması** *(hâl değişimi)*. Bir seçenek de "Defterimden kanıt göster" → K-2 ya da K-8 seçilebilir.
  3. *Sorgu:* "Kazandan çıkan su buharı neden kimyasal değişimin kanıtı değil?"
     - ✔ "Su hâlâ su; yeni madde oluşmadı, yalnızca hâli değişti."
     - ✘ "Çünkü buhar görünmez." *(görünürlüğü ölçüt sanma)*
     - ✘ "Çünkü ısı yok." *(buharlaşma ısı ister)*
  4. *Defter K-8:* **"Kimyasal değişimin kanıtları: yeni madde, gaz çıkışı, renk değişimi, ısı-ışık, koku. Yalnız şekil ya da hâl değişimi fizikseldir."**
  5. *Saha notu — Biyoloji · Enerji › Fermantasyon:* "Mayanın ürettiği gaz hangisi?" → **CO₂** (etil alkol fermantasyonu).
- **Dünya:** Part_3 (pencere saksıları).

### Seviye 9 — T-4 Muhtarın Mektubu (TrueFalse)
- **Akış:**
  1. *Görev:* 05'teki 4 ifade (D / Y mühürleri).
  2. *Katman — **yanlış iddiayı düzelt**:* Her "Y" mühründen sonra kâğıtta düzeltme satırı açılır; oyuncu doğru düzeltmeyi seçer:
     - "Sentezde ürün sayısı girenlerden fazladır." → ✔ **"Sentezde birden çok giren tek ürün oluşturur."**
     - "Bir tepkimede atomlar kaybolabilir." → ✔ **"Atomlar kaybolmaz; yalnızca yeniden dizilir."** + kanıt: Defter K-0 / K-1.
  3. *Defter K-9:* **"Bir iddiayı çürütmek için tek karşı örnek yeter; düzeltirken kanıt göster."**
  4. *Saha notları:*
     - **Matematik (akıl yürütme — karşı örnek):** "'Her yanma CO₂ verir' iddiasını çürüten tek örnek?" → **2H₂ + O₂ → 2H₂O** (hidrojenin yanması; karbon yok, CO₂ yok).
     - **Biyoloji · Enerji › Fotosentez:** Dedikodu: "Bitki besinini topraktan yer." → düzeltme: **"Bitki besinini fotosentezle kendisi üretir; topraktan su ve mineral alır."**
- **Dünya:** Part_4 (pano dolu).

### Seviye 10 — T-5 Lonca Sınavı (GuildExam)
- **Akış:**
  1. *Görev:* 05'teki havuz (rastgele 4, yanlış soru değişir).
  2. *Katman — **transfer** (havuza yeni örnekler, önceki görevlerde görülmemiş):*
     - BalanceEquation: **fotosentez** `6CO₂ + 6H₂O → C₆H₁₂O₆ + 6O₂` (yeni tür: C₆H₁₂O₆, glikoz).
     - BalanceEquation: **hücresel solunum** `C₆H₁₂O₆ + 6O₂ → 6CO₂ + 6H₂O`.
     - MC (üst düzey): "`Ca(OH)₂ + CO₂ → CaCO₃ + H₂O` (harcın sertleşmesi) üç kutudan hangisine girer?" → ✔ **"Hiçbirine tam uymaz; türler bütün tepkimeleri kapsamaz."** · Sentez · Analiz · Yanma.
  3. *Sorgu:* "Fotosentezle solunum arasında nasıl bir ilişki gördün?"
     - ✔ "Biri diğerinin tersi gibi: biri CO₂ ve suyu glikoza bağlar, öteki glikozu CO₂ ve suya geri çevirir."
     - ✘ "İkisi aynı tepkimedir." · ✘ "Fotosentez yalnız gece olur."
  4. *Defter K-10 (lonca mührüyle):* **"Tür, atomların nasıl yeniden dizildiğini anlatır. Aynı atomlar (C, H, O) doğada döngü hâlinde dolaşır: yakıtta, havada, bitkide, bizde."**
  5. *Saha notu — Matematik · Veriden Olasılığa:* "Havuzda {12} soru, {3}'ü denkleştirme. İlk sorunun denkleştirme gelme olasılığı?" → **3/12 = 1/4.**
- **Dünya:** Part_5 (sancak) + köy bitti (After). İki köy bitince prototip kapanışı (05 §4); son ekranda **Keşif Defteri** açılır: oyuncunun yazdığı 11 sayfa.

---

## 5. Bölüm 2 — Bereket Tarlası (Sürüm 2, Seviye 11–20)

> **Güncelleme 14.11:** Bu bölümün Matematik ve Biyoloji görevleri (A4, A5, A7, A8, A9) **Bereket Tarlası köyü** olarak yeniden yazıldı: 5 görev, her biri hem Mat hem Biyo, traktörsüz, ders notlarıyla → `04_Icerik/15-bereket-tarlasi-mat-biyo.md` (geçerli olan o). Aşağıdaki tablo traktörlü Kimya/Fizik görevleri (A1, A2, A3, A6, A10) için referans olarak kalır.

### 5.1 Kurgu
D-5'ten sonra Elif çırağı tarlasına çağırır: *"Gübre tamam, ama tarlayı kim sürecek? Ustan bir makine yapmış, ahırda tozlanıyor."* Ahırda **Simya Traktörü**: Câbir Usta'nın, el-Cezerî'nin makine kitabından esinlenip yaptığı buharlı çekiş arabası (büyük ahşap tekerlek, bakır kazan, baca, çark). Tarlanın işi tek derse sığmadığı için kilitli şehirlerin ustaları yardıma gelir (§8).

### 5.2 Mekanik
- **Traktör:** Bisikletle aynı binme bağlantısı (Interact → koltuk kamerası). Hız sınırı 4 m/s, sürmede 1,6 m/s. Aletler arkaya takılır: `Pulluk` (sür) · `Ekici` (ek) · `Su_Tanki` (sula) · `Serpici` (gübre) · `Bicer` (hasat) · `Kizak` (odun). Yakıt göstergesi bakır ibre; boşalınca kazanda yakıt hazırlanır.
- **Tarla:** 24 m × 16 m, 6 × 4 = 24 parsel. Durumlar: `Nadas → Surulmus → Ekili → Filiz → Olgun → Hasat`. Doğru aletle parselin üstünden geçince parsel sonraki duruma geçer.
- **Saha sorusu kartı:** Her aşamanın başında 1 zorunlu soru, aşama içinde 0–2 **olay sorusu**. Soru açıkken zaman 0,25× (akış kesilmez). Doğru → verim +1, mühür +1. Yanlış → usta yeri gösterir, **yeni sayılarla** ikinci deneme; yine yanlışsa eylem yapılır ama verim −1 (tıkanma yok). Sorular parametriktir; çeldiriciler tipik hatalardan; 3 zorluk kademesi.
- Her seviyede: **Tahmin** (bazılarında) · **Görev** · **Saha sorusu** · **Hata Dedektifi** (işaretli olanlarda) · **Defter kaydı**.

### 5.3 Seviyeler

| Sv | Görev | Akış (Tahmin → Görev → Katman) | Ekrandaki soru (örnek, `{}` parametre) | Ders · Tema (kazanım) | Cevap | Defter |
|---|---|---|---|---|---|---|
| 11 | **Makineyi Uyandır** (A1) | Hata Dedektifi: yakıt deposunda ustanın notu `CH₄ + 2O₂ → CO₂ + H₂O` → kazan siparişi | "Hangi element dengesiz?" | Kimya (yanma; K-2'ye bağ) | **H ve O** (H 4/2, O 4/3) | K-11: "Makinede yakıtın kimyasal enerjisi ısıya, ısı da harekete dönüşür." |
| 12 | **İlk Saban** (A2) | Pulluk takılır → sürme | "Tarla {24}×{16} m, iz {2} m: kaç şerit? {1,6} m/s sabit hızla dönüşsüz kaç s?" | Mat · Geometrik Şekiller; Fizik · Kuvvet ve Hareket (FİZ.10.1.1) | **8** şerit; 192 m → **120 s** | K-12: "Sabit hızda alınan yol = hız × zaman." |
| 13 | **Toprağı Tanı** (A3) | Tahmin: "Toprak ekşi kokuyor; ne eklenir?" | Raftan seç: CaCO₃ · Ca(OH)₂ · NH₃ · H₂O | Kimya (asit–baz tadımı; D-4 ürünü) | **Ca(OH)₂** | K-13: "Ekşi (asidik) toprağı kireç gibi bazik maddeler düzeltir. Köprünün kireci tarlada da işe yarar." |
| 14 | **Tohum Hesabı** (A4) | Ekici takılır | "Filizlenme olasılığı {0,8}; {120} filiz için en az kaç tohum?" | Mat · Veriden Olasılığa | **150** | K-14: "Beklenen sayı = olasılık × deneme sayısı." |
| 15 | **Ekim Düzeni** (A5) | Ekici tarlada | "8 şeridi yılan gibi sürersen kaç dönüş?" + "3 ürün 3 sıraya kaç türlü dizilir?" | Mat · Sayma, Algoritma ve Bilişim | **7**; **3! = 6** | K-15: "Bir işi adım adım yazınca (algoritma) kaç dönüş, kaç seçenek olduğunu sayabilirim." |
| 16 | **Kuyudan Tarlaya** (A6) | Su tankı kuyudan dolar | "{10} kg suyu {5} m derinden çıkarmak kaç J? {50} s'de güç?" | Fizik · Enerji (FİZ.10.2.2) | **500 J**; **10 W** | K-16: "İş = kuvvet × yol; güç = iş / zaman." |
| 17 | **Elif'in Gübresi** (A7) | Serpici; terazide `N₂ + H₂ → NH₃` | "Bitki azotu hangi biçimde alır?" + denkleştir | Biyo · Ekoloji (azot döngüsü); Kimya (K-5'e bağ) | **amonyum/nitrat**; **1, 3 → 2** | K-17: "Azot döngüsü: havadaki N₂ bağlanmadan bitkiye geçmez." |
| 18 | **Güneşten Besine** (A8) | Hata Dedektifi: `6CO₂ + H₂O → C₆H₁₂O₆ + 6O₂` → terazide fotosentez | "Hangi element dengesiz?" → denkleştir | Biyo · Enerji (BİY.10.1.2 fotosentez modeli); Kimya | **H ve O** (H 2/12, O 13/18); **6, 6 → 1, 6** | K-18: "Fotosentez, solunum ve yanmanın tersi gibidir (K-2, K-10)." |
| 19 | **Hasat Raporu** (A9) | Biçer; defterde kendi verim grafiğin | "Parseller {42, 38, 45, 35} kg: ortalama? Altında kalanlar?" | Mat · İstatistiksel Araştırma Süreci | **40**; 38 ve 35 | K-19: "Ortalama tek sayıyla özetler; grafik farkları gösterir." |
| 20 | **Değirmenden Fırına** (A10) | Başak değirmene, un fırına | "Büyük çark {48}, küçük {12} diş: küçük kaç tur?" + "Hamur neden kabarır?" + "Rüzgâr yenilenebilir mi?" | Mat · Sayılar (oran); Biyo · Enerji (fermantasyon); Fizik · Enerji (FİZ.10.2.5) | **4**; **CO₂** (K-8); **evet** | K-20: "Rüzgârın hareket enerjisi çarkla una, unun enerjisi ekmekle bize geçer." |

**Olay soruları (rastgele):** *Yağmur:* sabit hızla düşen damlanın konum–zaman grafiği → **doğrusal** (FİZ.10.1.3) · *Kuş sürüsü:* kuş–tohum–şahin zincirinde birincil tüketici → **kuş** (Biyo · Ekoloji) · *Yokuş:* {0}'dan {3} m/s'ye {6} s → ivme **0,5 m/s²** (FİZ.10.1.2).

## 6. Bölüm 3 — Ormancı Korusu (Sürüm 2, Seviye 21–26)

Tetik: Fırıncı Zehra'nın odunu biter (D-3'ten sonra): *"Fırın soğudu, odun lazım. Ama ormancı rastgele ağaç kestirmez."* Fizik bağı **Enerji** ünitesinden gelir (10. sınıf programında kaldıraç/tork yok; düşen ağaç = mekanik enerjinin korunumu, kızak = iş ve güç).

| Sv | Görev | Akış | Ekrandaki soru (örnek) | Ders · Tema (kazanım) | Cevap | Defter |
|---|---|---|---|---|---|---|
| 21 | **Hangi Ağaç?** (B1) | 3 işaretli ağacın kesitine bak | "Kesitte {37} halka: kaç yaşında?" + "Hangisi kesilir: genç sağlıklı · yaşlı kuru · yuvalı?" | Biyo · Ekoloji (ekolojik sürdürülebilirlik, biyoçeşitliliğin korunması) | ~**37**; **yaşlı kuru** | K-21: "Ağaç yaşı halkalardan okunur; yuvalı ağaç bir canlı topluluğunun evidir, korunur." |
| 22 | **Yön Çentiği** (B2) | Baltayla çentik | "Üst kesim {50}°, alt {20}°: içteki açı?" + "Menteşe çapın %{80}'i, çap {40} cm?" | Mat · Geometrik Şekiller; Sayılar | **110°**; **32 cm** | K-22: "Üçgenin iç açıları toplamı 180°; yüzde = oran × 100." |
| 23 | **Ağaç Düşüyor!** (B3) | Arka kesim → ağaç çentik yönüne düşer (ağır çekim) | Tahmin: "Ağaç nereye düşer?" → çentik yönü. "Tepe {12} m'den düşüyor; sürtünmesiz çarpma hızı?" + "Hangi yöne çekilirsin?" | Fizik · Enerji (mekanik enerjinin korunumu, FİZ.10.2.4) | √(2·10·12) ≈ **15,5 m/s**; düşüşün tersine, çapraz | K-23: "Yükseklikteki enerji (potansiyel) düşerken hareket enerjisine dönüşür; toplam korunur." |
| 24 | **Kızakla Taşı** (B4) | Kızak traktöre takılır | Kuvvet–yol grafiği: "{200} N ile {10} m: alan ne, kaç J? {40} s'de güç?" | Fizik · Enerji (FİZ.10.2.1, FİZ.10.2.2) | **2000 J**; **50 W** | K-24: "Kuvvet–yol grafiğinin altındaki alan yapılan işi verir." |
| 25 | **Bir Kes, Üç Dik** (B5) | 3 fidan dikilir | "Bir fidanın tutma olasılığı {0,6}; 3 fidandan beklenen?" + "Neden fidan dikeriz?" | Mat · Veriden Olasılığa; Biyo · Ekoloji (ekolojik ayak izi) | **1,8 ≈ 2**; karbon döngüsü ve yaşam alanı | K-25: "Kestiğin yerine dikersen orman sürdürülebilir kalır." |
| 26 | **Odun Ateşe** (B6) | Fırına odun | "Odun yanarken enerji dönüşümü? Odun yenilenebilir mi?" | Fizik · Enerji (FİZ.10.2.3, FİZ.10.2.5); Kimya (K-2) | **kimyasal → ısı + ışık**; dikilirse **yenilenebilir** | K-26: "Yenilenebilirlik, kaynağın yerine konup konmadığına bağlıdır." |

## 7. Bölüm 4 — Çarşı (Sürüm 2, Seviye 27–29, Hesap Ustası)

| Sv | Görev | Soru | Ders | Cevap | Defter |
|---|---|---|---|---|---|
| 27 | **Teraziyle Satış** (manav) | "{3} kg ayva {18} akçe; {5} kg?" | Mat · Sayılar (oran) | **30** | K-27: "Doğru orantıda birim fiyat sabittir." |
| 28 | **İndirim mi Kazık mı?** (baharatçı) | "%{20} zam, sonra %{20} indirim: fiyat aynı mı?" | Mat · Nicelikler ve Değişimler | **Hayır, %4 düşük** (1,2 × 0,8 = 0,96) | K-28: "Yüzdeler toplanmaz, çarpılır." |
| 29 | **Yol Hesabı** (harita masası) | "Meydan (0, −8), fırın (50, 2): kuş uçuşu?" | Mat · Analitik İnceleme | √(50² + 10²) ≈ **51 m** | K-29: "İki nokta arası uzaklık, dik üçgenin hipotenüsüdür." |

## 8. Misafir ustalar ve mühür (ilerleme kancası)

| Usta | Şehir (kilitli) | Renk / simge | Getirdiği |
|---|---|---|---|
| **Hesap Ustası** (el-Hârizmî'nin öğrencisi) | Matematik | dağ mavisi / üçgen | Alan, oran, olasılık, algoritma, veri |
| **Makine Ustası** (el-Cezerî'nin öğrencisi) | Fizik | kaya grisi / çark (yeni) | Hareket, iş-enerji-güç, enerji kaynakları |
| **Hayat Ustası** (İbnü'l-Baytâr'ın öğrencisi, bitki bilgini) | Biyoloji | yaprak yeşili / yaprak (yeni) | Fotosentez, fermantasyon, ekosistem, döngüler |
| Câbir Usta | Kimya (açık) | bakır / imbik | Kazan, terazi, defter |

Her doğru saha notu ilgili şehrin kapı madalyonuna bir **mühür çentiği** ekler (haritadaki Matematik ve Türkçe kapılarında madalyon var; Fizik ve Biyoloji kapıları sonraki sürümde). 10 çentik → kapı açılır → yeni şehir = yeni ders. Böylece Kimya'da ilerlerken öteki dersler "tadılır"; tarla ve orman, kilitli şehirlere giden yolun kendisidir.

## 9. Veri (ScriptableObject önerisi — Mehmet)

```
DiscoveryData (SO)            // Keşif Defteri kaydı
  id            : "K-2"
  text          : "Yanma tepkimelerinde oksijenin rolü vardır: ..."     (TR, oyuncuya görünen)
  unlockedBy    : taskId (D-2)
  linkedFrom    : [taskId...]   // ipucu/sorgu/kanıt olarak gösterildiği görevler

ReasoningPrompt (SO)          // Ustanın Sorgusu
  taskId, question, options[3], correctIndex, misconceptionNote[3], discoveryOnCorrect

ErrorHunt (SO)                // Hata Dedektifi
  wrongEquation (katsayılar + türler), unbalancedElements (otomatik hesaplanır: AtomMath),
  reasonOptions[], correctReason

PredictionPrompt (SO)         // Tahmin
  taskId, question, options[], correct[] (çoklu seçim olabilir), feedbackVisual (ör. "fener söner")

FieldQuestionData (SO)        // Saha notu / saha sorusu
  id, subject (Mat|Fizik|Biyo|Kimya), curriculumCode, type (MC|Number|DragMatch|GraphRead|Scale),
  template "{L} m × {W} m ...", params (aralıklar), answerExpr, distractorExprs[], tolerance,
  hint, realLife, difficulty 1..3, sealReward
```
- Oyuncuya görünen bütün metin SO'da (CLAUDE.md §8). `unbalancedElements` elle yazılmaz; `AtomMath` hesaplar (yanlış veri girişini önler). Parametrik ifade değerlendirici saf C# ve EditMode testli olur.
- Yeni tür: **C₆H₁₂O₆ (glikoz)** — Seviye 10 ve 18 için `SpeciesData`'ya eklenir (C, H, O; yeni element yok).

## 10. UI ve girdi

- **Keşif Defteri sayfası** (Mustafa): mevcut defter prefabına ikinci ayraç "Keşiflerim"; her kayıt numaralı, el yazısı hissi veren (ama 28 px üstü okunur) gövde fontu, formüller TMP alt indis. Yeni kayıtta kalem canlandırması.
- **Hata Dedektifi** (tahta): denklem tebeşirle; altında element düğmeleri (atom simgesi + harf). `IChalkboardView`'a `ShowErrorHunt(equation, elements)` ve `MarkCounts(element, left, right)` eklenir (Mehmet).
- **Atom Görüşü:** kazanın üstünde yarı saydam yuvarlak pencere; atom simgeleri mühür kodunda. Kod `IAtomViewer` ile tetikler.
- **Ustanın Sorgusu:** konuşma balonu + portre (`IDialogueView`) altında 3 seçenek kartı.
- **Saha sorusu kartı:** `ISahaSoruView` — kâğıt kart, köşede ders rozeti (renk + simge), ± düğmeleri, grafik tipi için çizilmiş eksenler. 1920×1080, en az 28 px.
- **Girdi önerisi** (`CLAUDE.md` §3'e eklenmeden önce Mehmet'le konuşulur; her aksiyon klavye/fare + gamepad):

| Aksiyon | Map | Klavye / Fare | Gamepad |
|---|---|---|---|
| Atom Görüşü aç/kapa | Station | V | R3 (sağ çubuk bas) |
| Element işaretle (Hata Dedektifi) | Station | Sol tık / 1–5 | A (sanal imleç) |
| Keşif Defteri aç | Station | H (mevcut OpenHint; ikinci ayraç) | X (West) |
| Traktöre bin / in | Player | E | X (West) |
| Alet indir / kaldır | Player (sürüşte) | Sol tık | RT |
| Alet değiştir (ahırda) | Player | Q | Y (North) |
| Sayı artır / azalt (soru kartı) | UI | ↑ / ↓, fare tekeri | D-pad ↑ / ↓ |
| Yanıtla | UI | Enter | A |

## 11. Sanat ve sahne bağlantı noktaları (Mustafa — Blender, hazır olanlar)

| Model | Durum | Dosya |
|---|---|---|
| Simya Traktörü | **yapıldı** (6,3 bin üçgen) | `Art/Traktor/Traktor.fbx` |
| Aletler ×6 (`Alet_Pulluk`, `_Ekici`, `_Su_Tanki`, `_Serpici`, `_Bicer`, `_Kizak`) | **yapıldı** | `Art/Traktor/Traktor_Aletler.fbx` |
| Bereket Tarlası (24 parsel × 6 durum, `Tarla_P<satır><sütun>_<Durum>`, başlangıçta `Nadas`) | **yapıldı** | `Art/Harita/Bereket_Tarlasi.fbx` |
| Ormancı Korusu (`Koru_*`: genç, kuru (ayakta/çentikli/devrik), halkalı kütük, yuvalı, odun destesi, fidanlar) | **yapıldı** | `Art/Harita/Ormanci_Korusu.fbx` |
| Keşif Defteri sayfası, Hata Dedektifi tahta düzeni, Atom Görüşü penceresi, saha sorusu kartı | sonra (UI) | `Assets/_Project/UI/` |
| Ahır + alet askısı, mühür madalyonu çentikleri, Fizik/Biyoloji kapıları | sonra | — |

- `Traktor` kökü; hareketli: `Traktor_Arka_Tekerler` (X), `Traktor_On_Aks` (dikey eksende direksiyon), `Traktor_On_Tekerler` (X; Unity'de `On_Aks` altına taşınır), `Traktor_Volan`, `Traktor_Direksiyon`. Boş objeler: `Traktor_Koltuk_Kamera`, `Traktor_Duman_Noktasi`, `Traktor_Baglanti` (alet pimi). Aletlerin pivotu pim; alet yerel +Y geriye uzanır.
- Bereket Tarlası merkezi (60, 24) [Blender X/Y], giriş güneyde; ahır önü (aletler) y ≈ 10,5; traktör (46, 14,8). Ormancı Korusu (40,5, 24).

## 12. Kaynaklar

- MEB TYMM 10. sınıf: [Fizik (Kuvvet ve Hareket, Enerji, Elektrik, Dalgalar)](https://tymm.meb.gov.tr/ogretim-programlari/fizik-dersi/12) · [Matematik (7 tema)](https://tymm.meb.gov.tr/ogretim-programlari/matematik-dersi/12) · [Biyoloji (Enerji, Ekoloji)](https://tymm.meb.gov.tr/ogretim-programlari/biyoloji-dersi/12) · [Kimya (Etkileşim, Çeşitlilik, Sürdürülebilirlik)](https://tymm.meb.gov.tr/ogretim-programlari/ders/kimya-dersi) · [Çerçeve planları](https://tymm.meb.gov.tr/taslak-cerceve-planlari)
- Ağaç kesimi: [OSHA — felling hinge](https://www.osha.gov/etools/logging/manual-operations/felling/cuts/hinge) · [Husqvarna — directional notch](https://www.husqvarna.com/us/discover/chainsaw-academy/basic-tree-felling/how-to-make-a-directional-notch/) · [Northern Woodlands — hinge hints](https://northernwoodlands.org/articles/article/hinge_hints)
- Buharlı çekiş makinesi: [MERL — John Fowler](https://merl.reading.ac.uk/explore/online-exhibitions/john-fowler/) · [The Engineer — Fowler's steam plough 1856](https://www.theengineer.co.uk/fowlers-steam-driven-plough-1856/) · [Grace's Guide — Fowler](https://gracesguide.co.uk/John_Fowler_and_Co:_Road_Locomotive)
- Katman fikirleri (Keşif Defteri, Hata Dedektifi, Ustanın Sorgusu, D/T tablosu): ekip notu, 13.11.2026.
