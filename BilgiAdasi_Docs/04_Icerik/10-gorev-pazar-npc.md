# 10 — Görev Listesi, Malzemeler, Pazar, NPC'ler ve Yol Sohbetleri

> `05-icerik-prototip.md`'nin devamı. Oradaki 10 ana görev **değişmez**; bu dosya onların etrafını doldurur: hangi element/malzeme nerede kullanılır, pazarda ne alınır/satılır, yan müşteriler, sahne NPC'leri ve yürürken duyulan mini sohbetler.
> İsimler ve istekler **kurgu**dur. Kurallar: `01` §7 (şiddet/kumar/gerçek para yok, kadın-erkek dengeli, sakinler gündelik dille konuşur, formül yalnız etikette).

---

## 0. Öncelik ve 24 saat etkisi (teslim: 14.11 11.00, kod dondurma 08.00)

| Parça | Öncelik | Neden / maliyet |
|---|---|---|
| §1 Ana görev listesi (10 görev) | **P0** | Zaten tasarlı; burada yalnızca özet + ödül eklendi |
| §6 Yol sohbetleri (tetik bölgesi + altyazı) | **P1** | Ucuz: veri + 1 trigger script + 1 altyazı view. Şehri canlı gösterir, sunumda iyi durur |
| §5 Sahne NPC'leri (sabit, boşta animasyonlu) | **P1** | Model işi Mustafa'da; 3–4 gövde × renk/aksesuar varyasyonuyla çözülür |
| §3 Pazar — **tek tezgâh** (Elif'in sandığı yerine) | **P1** | `Purchase` modu zaten var; sandığı pazara taşımak = yeni yer, aynı kod |
| §3 Pazar — tüm tezgâhlar + satış | **P2** | Yeni dükkân UI'ı + cüzdan kaydı |
| §4 Yan müşteriler (8 yan sipariş) | **P2** | Kod yeni değil (`OrderTask` aynı), ama veri girişi + test süresi |

Kesme sırası: önce §4, sonra pazarın satış tarafı, sonra NPC sayısı (14 → 6).

---

## 1. Ana görev listesi (özet)

Akçe = oyun içi para (gerçek para karşılığı yok, rastgelelik yok, sabit fiyat).
Başlangıç kesesi: **10 akçe**.

### Denkleştirme köyü (sipariş, kazan)

| # | Görev adı | Veren | Tepkime | Kazana girenler (doğru cevap) | Malzeme nereden | Ödül |
|---|---|---|---|---|---|---|
| D-1 | Kuru Çeşme | Muhtar Yusuf | 2H₂ + O₂ → 2H₂O | 2 H₂ + 1 O₂ | Ustanın rafı | 4 akçe + H₂O kavanozu rafa |
| D-2 | İsli Fenerler | Fenerci Demir | CH₄ + 2O₂ → CO₂ + 2H₂O | 1 CH₄ + 2 O₂ | Ustanın rafı | 4 akçe |
| D-3 | Soğuk Fırın | Fırıncı Zehra | C₃H₈ + 5O₂ → 3CO₂ + 4H₂O | 1 C₃H₈ + 5 O₂ | Ustanın rafı | 5 akçe + "ilk somun" (süs) |
| D-4 | Yıkık Köprü | Duvarcı Halil | CaCO₃ → CaO + CO₂, sonra CaO + H₂O → Ca(OH)₂ | 1 CaCO₃ → 1 CaO + 1 H₂O | Raf + 1. adım ürünü | 6 akçe |
| D-5 | Gübre Ruhu | Çiftçi Elif | N₂ + 3H₂ → 2NH₃ (×2) | 2 N₂ + 6 H₂ | **Pazar** (Elif 20 akçe verir) | Artan akçe sende kalır |

D-5 hesabı: 2 N₂ × 3 + 6 H₂ × 2 = **18 akçe** ≤ 20. Fazla alınan kavanoz "artan madde" olur (§3.3 Takasçı yarı fiyata alır).

### Tepkime Türleri köyü (masa)

| # | Görev adı | Getiren | Soru tipi | Ödül |
|---|---|---|---|---|
| T-1 | Ustanın Defteri | Güvercin | FillBlank | 3 akçe |
| T-2 | Etiket Masası | Güvercin | Classify | 3 akçe |
| T-3 | Fırıncının Mektubu | Güvercin (Zehra) | MultipleChoice | 3 akçe |
| T-4 | Muhtarın Mektubu | Güvercin (Yusuf) | TrueFalse | 3 akçe |
| T-5 | Lonca Sınavı | Lonca Kâtibi Nevin (§5) | GuildExam (4 soru) | 6 akçe + Lonca mührü (defter kapağında) |

Köy bitişi: Denkleştirme → köprü onarılır, fenerler yanar, çeşme akar. Tepkime Türleri → güvercin kulesinde bayrak, lonca kapısı açık, pano dolu.

---

## 2. Elementler ve malzemeler

### 2.1 Oyunda hesaba giren elementler (terazide sayılır)

| Sembol | Ad | Renk | Mühür şekli | Nerede geçer |
|---|---|---|---|---|
| H | Hidrojen | Beyaz | Küre | D-1, D-2, D-3, D-4, D-5, yan siparişler |
| O | Oksijen | Kırmızı | Halka | Hemen her tepkime |
| C | Karbon | Koyu gri | Küp | D-2, D-3, D-4, yanma yan siparişleri |
| N | Azot | Mavi | Piramit | D-5, Bostancı yan siparişi |
| Ca | Kalsiyum | Açık yeşil | Altıgen | D-4, Badanacı ve Kireççi yan siparişleri |

### 2.2 Malzemeler (kavanozlar — `Malzeme_<Formül>`)

Sakinler ve satıcılar **gündelik adı** söyler; etikette formül yazar.

| Formül | Etiket adı | Gündelik ad (sohbette) | Kullanıldığı görev | Pazarda alış | Takasçı alır |
|---|---|---|---|---|---|
| H₂ | Hidrojen | "hafif hava" | D-1, D-5, Y-2, Y-7 | 2 akçe | 1 |
| O₂ | Oksijen | "can havası" | D-1, D-2, D-3, Y-1, Y-3, Y-8 | 1 akçe | — (ucuz) |
| N₂ | Azot | "durgun hava" | D-5, Y-7 | 3 akçe | 1 |
| H₂O | Su | "su" | D-4, Y-4, Y-5 | 1 akçe | — |
| CH₄ | Metan | "bataklık gazı" | D-2, Y-3 | 3 akçe | 1 |
| C₃H₈ | Propan | "fırın gazı" | D-3, Y-8 | 5 akçe | 2 |
| CaCO₃ | Kireç taşı | "kireç taşı" | D-4, Y-6 | 2 akçe | 1 |
| CaO | Sönmemiş kireç | "diri kireç" | D-4 (ara ürün), Y-4 | **satılmaz** (eldiven ister) | 2 |
| Ca(OH)₂ | Sönmüş kireç | "badana kireci" | D-4 ürünü | satılmaz | 2 |
| CO₂ | Karbondioksit | "ağır hava" | Ürün | satılmaz | 1 |
| CO | Karbon monoksit | "is havası" | Y-1 (müşteri getirir) | **satılmaz** | **almaz** |
| NH₃ | Amonyak | "gübre ruhu" | D-5 ürünü, Y-7 | satılmaz | 3 |

Kural: Ürünler (CO₂, Ca(OH)₂, NH₃) pazarda satılmaz, **sen üretirsin**. Böylece pazar kazanın yerini tutmaz.

### 2.3 Dönem maddeleri, külçeler, aletler (süs + bilgi; hesaba girmez)

Modelleri hazır (`08` §7). Pazarda görünür, bazıları alınabilir; terazide kullanılmaz. Üzerine bakınca tek satır bilgi çıkar.

| Eşya | Satan | Fiyat | Bakınca çıkan not |
|---|---|---|---|
| `Madde_Sirke` | Attar Saadet | 1 | "Ekşi. Ustalar bununla bakır kapları parlatır." |
| `Madde_Gul_Suyu` | Attar Saadet | 2 | "Damıtılmış gül. Damıtma bugün de kullanılır." |
| `Madde_Sap` | Attar Saadet | 2 | "Şap: suyu durultmak için eskiden kullanılırdı." |
| `Madde_Boraks` | Attar Saadet | 2 | "Boraks: lehimde, cam ve sırda işe yarar." |
| `Madde_Kukurt` | Attar Saadet | — (vitrin) | "Kükürt. Kokusu yüzünden tezgâhın en arkasında." |
| `Kulce_Bakir`, `_Kalay`, `_Demir` | Demirci Rıza | 3 / 3 / 2 | "Bakır + kalay = tunç. Havanın da tunçtan." |
| `Kulce_Altin`, `_Gumus` | Demirci Rıza | — (vitrin) | "Satılık değil evladım, göz zevki." |
| `Alet_Huni`, `_Elek`, `_Kepce` | Demirci Rıza | 1 / 1 / 1 | Süs (atölye rafına konabilir, P2) |
| `Madde_Civa`, `_Kezzap`, `_Kral_Suyu`, `_Zirnik` | — | **pazarda yok** | Yalnızca atölyede kilitli dolapta, "Usta izni olmadan dokunulmaz" (güvenlik mesajı) |

---

## 3. Pazar (Meydan, çınarın güney yarısı)

Meydanın (0, −8) güney kenarında yarım daire 4 tezgâh + 1 takas tezgâhı. Her tezgâh bir `Station` (kamera tezgâha yaklaşır, raf görünür, `−/+` ile adet, "Al" düğmesi). Fiyat sabit, pazarlık yok, şans yok.

### 3.1 Tezgâhlar

| Tezgâh | Satıcı | Satar | Görsel işaret (weenie) | Selamlama |
|---|---|---|---|---|
| **Hava Tezgâhı** | Körükçü Bahar | H₂, O₂, N₂ | Büyük deri körük, tavana asılı mühürlü cam kürecikler | "Hafif hava, can havası, durgun hava! Kavanozu mühürlü, içi tamam." |
| **Kireççi** | Kireççi Cemal | CaCO₃ | Beyaz tozlu çuval yığını, beyaz önlük | "Taş benden, ateş senden. Diri kireci bana sorma, eldivenin yoksa olmaz." |
| **Su ve Gaz** | Sakacı Nuri | H₂O, CH₄, C₃H₈ | Sırtta su tulumu, bataklık sazları dolu fıçı | "Su da var gaz da. Karıştırma ama, ayrı kavanozda dursun." |
| **Attar** | Attar Saadet | Dönem maddeleri (§2.3) | Renkli sırlı kavanoz duvarı, gül kokusu (partikül) | "Gül suyu, sirke, şap… Senin kazanına girmez ama evin kokusu değişir." |
| **Demirci tezgâhı** | Demirci Rıza | Külçe + alet (§2.3) | Örs, ışıldayan külçeler | "Tunç havan mı arıyorsun? Önce bakırla kalayı say." |
| **Takasçı** | Takasçı Gülsüm | **Alır:** artan kavanozlar (yarı fiyat), ürünler | Terazili masa, "ALIRIM" tahtası | "Artanı bana getir. Yarı fiyat ama israf olmaz." |

### 3.2 Alım akışı
1. Tezgâha Interact → istasyon kamerası.
2. Kavanoz satırı: ad + etiket + fiyat + `−/+` (fare tekeri / RB-LB).
3. "Al" → kese düşer, kavanoz envantere (6 göz). Göz doluysa: "Kesen var ama elin dolu. Önce atölyeye bırak."
4. Para yetmezse "Al" soluk; satıcı: "Bir akçe eksik. Bir siparişi bitir, gel."

### 3.3 Satış (Takasçı)
- Yalnızca **artan** kavanozlar ve ürünler. Fiyat §2.2 son sütun.
- Öğretici nokta: D-5'te fazla alınan kavanoz yarı fiyata döner → "artan madde" kavramı parayla hissedilir.
- Para kasma engeli: Takasçı bir kavanozu alış fiyatından **asla** pahalı almaz.

---

## 4. Yan müşteriler (yan siparişler, P2)

Pazar panosuna ya da atölye kapısına gelir. Ana görevleri **kesmez**, köy ilerlemesine sayılmaz, yalnızca akçe ve sohbet verir. Hepsi `05`'teki mevcut türler ve tepkimelerle; yeni `SpeciesData` yok.

| # | Müşteri | Nerede bekler | İstek (replik) | Tepkime / hedef | Mod | Ödül |
|---|---|---|---|---|---|---|
| Y-1 | Hamamcı Sevim | Pazarın doğusu | "Külhandan ağır bir duman geliyor, başım dönüyor. Bu iki kavanozu getirdim, bunu zararsız bir şeye çevirebilir misin?" (2 CO getirir) | 2CO + O₂ → 2CO₂ | Exact | 5 akçe |
| Y-2 | Camcı Leyla | Takasçının yanı | "Alevim yetmiyor, cam erimiyor. Bahar'ın can havası bugün tükenmiş. Sudan çıkar diyorlar, doğru mu?" | 2H₂O → 2H₂ + O₂ (hedef 2 H₂ + 1 O₂; O₂'yi Leyla alır) | Exact | 4 akçe + 2 H₂ rafa |
| Y-3 | Mumcu Kenan | Çarşı fenerleri | "Fenerci'nin işi bitti, şimdi benim iki fenerim var. Aynı tarif, iki kat." | CH₄ + 2O₂ → CO₂ + 2H₂O (×2) | Exact | 5 akçe |
| Y-4 | Badanacı Rukiye | Köprü başı | "Köprü bitti, şimdi duvarlar ağardı mı? İki kova badana kireci." | CaO + H₂O → Ca(OH)₂ (×2) | Exact | 5 akçe |
| Y-5 | Aşçı Melek | Fırın önü | "Kazanım boş, misafir çok. Dört kavanoz su, tastamam." | 2H₂ + O₂ → 2H₂O (×2) | Exact | 4 akçe |
| Y-6 | Kireç Ocakçısı Tamer | Tarla yolu | "Üç taş getirdim. Üçünden de diri kireç çıkar, ağır havası da çıksın gitsin." | CaCO₃ → CaO + CO₂ (×3) | Exact | 5 akçe |
| Y-7 | Bostancı Osman | Tarlalar | "Elif'in tarlası yeşerdi, benim bostan bakıyor. İki kavanoz gübre ruhu, param on akçe." | N₂ + 3H₂ → 2NH₃ | Purchase (bütçe 10) | Artan akçe |
| Y-8 | Simitçi Hatice | Meydan | "Zehra'nın fırınından simit alıyordum, şimdi kendi fırınımı yaktım. İki fırınlık gaz yak." | C₃H₈ + 5O₂ → 3CO₂ + 4H₂O (×2) | Exact | 6 akçe |

Teslim replikleri:
- Y-1 Sevim: "Duman gitti, hamam kokusu geldi. Gel bir gün, sana havlu benden."
- Y-2 Leyla: "Bak şu aleve! Mavi mavi. Sana bir boncuk yapayım."
- Y-3 Kenan: "İki fener, iki kat ışık, iki kat da teşekkür."
- Y-4 Rukiye: "Duvarlar bembeyaz. Sen olmasan kireç tozuna boğulacaktım."
- Y-5 Melek: "Tastamam dört. Saymayı bilen çırak altın değerinde."
- Y-6 Tamer: "Taş girdi, kireç çıktı, hava uçtu. Hiçbir şey kaybolmadı, değil mi?"
- Y-7 Osman: "On akçeyi tam yerinde harcadın. Artanı da al, cebine koy."
- Y-8 Hatice: "İlk simit senin! Susamı bol."

"Gerçek hayatta" kartları (tek cümle):
- Y-1: "Arabaların egzozundaki katalitik dönüştürücü, zehirli karbon monoksiti karbondioksite çevirir."
- Y-2: "Sudan elektrikle hidrojen ve oksijen ayrıştırılabilir; buna elektroliz denir."
- Y-4: "Sönmüş kireç bugün de badanada ve harçta kullanılır."
- Y-6: "Kireç fabrikalarında kireç taşı çok yüksek sıcaklıkta ısıtılır; bu sırada karbondioksit açığa çıkar."

---

## 5. Sahne NPC'leri (harita, sabit + boşta animasyon)

Konuşma tetiği yok (görev verenler hariç); yanından geçerken §6 sohbetleri ve tek satırlık laf atmaları (bark) çalışır. Kadın 7 / erkek 7. Model: 3–4 temel gövde × renk + **mesleği gösteren tek nesne** (`01` §2 kuralı). Palet `09`.

| # | NPC | Yer | Elindeki nesne / boşta hâli | Görev bağı | Önce → Sonra |
|---|---|---|---|---|---|
| 1 | Muhtar Yusuf | Meydan çeşmesi | Boş testi; çeşmeye bakıp başını sallar | D-1 | Kuru çeşme başında → testi doldurur |
| 2 | Fenerci Demir | Doğu çarşı | Uzun fener sırığı | D-2 | Fenerleri siler → fener yakar |
| 3 | Fırıncı Zehra | Fırın | Ekmek küreği | D-3, T-3 | Soğuk fırının önünde oturur → somun dizer |
| 4 | Duvarcı Halil | Taş köprü | Mala | D-4 | Yıkık köprüye bakar → köprüde harç çeker |
| 5 | Çiftçi Elif | Tarlalar | Çapa | D-5 | Sarı tarlada → yeşil tarlada |
| 6 | Lonca Kâtibi Nevin | Lonca kapısı (batı) | Mühür ve kalem kutusu | T-5 | Kapı kapalı, defter karıştırır → kapı açık, mühür basar |
| 7 | Güvercinci Selim | Güvercin kulesi | Kafes, yem kesesi | T-1…T-4 (güvercinleri o salar) | Güvercin besler (sürekli) |
| 8 | Körükçü Bahar | Pazar | Körük | Pazar | Körük basar |
| 9 | Kireççi Cemal | Pazar | Çuval | Pazar | Çuval taşır |
| 10 | Sakacı Nuri | Pazar ↔ çeşme arası yürür | Su tulumu | Pazar | Çeşme kuruyken "dereden" taşır → çeşmeden doldurur |
| 11 | Attar Saadet | Pazar | Havan | Pazar | Havanda döver |
| 12 | Takasçı Gülsüm | Pazar | El terazisi | Pazar | Tartar |
| 13 | Demirci Rıza | Pazar | Çekiç | Pazar | Örse vurur (ses çok kısık) |
| 14 | Çocuklar Ayşe & Mert | Çınar altı | Top / tahta at | Yok | Çınar altında oynar → çeşmede su sıçratır |

Opsiyonel (yan müşteriler P2'de açılırsa): Sevim, Leyla, Kenan, Rukiye, Melek, Tamer, Osman, Hatice — §4'teki yerlerde, görevleri gelene kadar gizli.

---

## 6. Yol sohbetleri (yürürken duyulan mini sohbetler)

**Nasıl çalışır:** Yol üzerinde görünmez tetik küreleri (Ø 6–8 m). Oyuncu girince iki NPC kendi arasında 2–4 satır konuşur; altyazı ekranın altında, konuşanın adıyla. Oyuncuya soru sorulmaz, durmak gerekmez. Her sohbet **bir kez** çalışır (koşul değişince yenisi açılır). Konuşanlar oyuncuya bakmaz, birbirine bakar.

Koşul kısaltmaları: `Her zaman` · `D<n` (Denkleştirme'de n görevden az) · `D≥n` · `T≥n` · `D=5` köy bitti.

### 6.1 Meydan (çınar + çeşme)

**S-01 · Kuru çeşme** — `D<1`
- **Sakacı Nuri:** Yine dereye mi ineceğiz muhtarım?
- **Muhtar Yusuf:** Dere de kurudu Nuri. Câbir Usta'nın yeni bir çırağı varmış, ona umut bağladım.
- **Sakacı Nuri:** Çırak dediğin kaç yaşında ki?
- **Muhtar Yusuf:** Boyuna bakma. Kafası işliyormuş.

**S-02 · Akan çeşme** — `D≥1`
- **Ayşe:** Mert! Su geldi, su!
- **Mert:** Hani hidro… hidrojen falan diyorlardı ya, o mu bu?
- **Ayşe:** Bilmem, ama ıslak.

**S-03 · Pazar dedikodusu** — `Her zaman`
- **Takasçı Gülsüm:** Bahar, dün biri sekiz kavanoz can havası almış, yarısını bana sattı.
- **Körükçü Bahar:** Saymadan almış. Kazan ne isterse o kadar alacaksın.
- **Takasçı Gülsüm:** Ben şikâyetçi değilim ama.

**S-04 · Boy uzaması** — `D=5` ya da `T=5` (ilk biten köy)
- **Attar Saadet:** Şu çırak geçen hafta bu kadar uzun muydu?
- **Demirci Rıza:** Öğrendikçe uzuyor derler. Bizim oğlan da kitap okusa…
- **Attar Saadet:** Rıza, oğlun senden uzun zaten.

### 6.2 Doğu yolu → köprü → Denkleştirme köyü

**S-05 · Geçici tahta köprü** — `D<4`
- **Duvarcı Halil:** Bu tahta köprü bir kışı daha çekmez.
- **Kireççi Cemal:** Taşı ben veririm, harcı kim karacak?
- **Duvarcı Halil:** Harç için sönmüş kireç lazım. Ustanın çırağına soracağım.

**S-06 · Onarılmış köprü** — `D≥4`
- **Kireççi Cemal:** Taş girdi, kireç çıktı. Bir tane bile eksik çuval yok, saydım.
- **Duvarcı Halil:** Sen çuvalı say, çırak atomu saydı.

**S-07 · Fenerler** — `D<2`
- **Fenerci Demir:** Camı silsem de bir saat sonra yine kapkara.
- **Mumcu Kenan:** Gaz çok, hava az, ondan. Annem de öyle derdi.
- **Fenerci Demir:** Annen haklıymış ama ne kadar hava, onu söylememiş.

**S-08 · Yanan fenerler** — `D≥2`
- **Mumcu Kenan:** Bak şu ışığa. İs yok, koku yok.
- **Fenerci Demir:** Bir bataklık gazına iki can havası. Çırak kartına yazdı, astım.

**S-09 · Fırın kokusu** — `D≥3`
- **Fırıncı Zehra:** Simitçi Hatice'ye de söyledim, gaz az olursa alev sarı yanar.
- **Aşçı Melek:** Benimki hep sarıydı, ben onu güzel sanıyordum!
- **Fırıncı Zehra:** Güzel ama isli. Mavisi doğrusu.

**S-10 · Tarla** — `D<5`
- **Çiftçi Elif:** Toprak yorgun Osman. Gübre ruhu olmadan bu yıl da sarı kalacak.
- **Bostancı Osman:** Pazardan alırsın.
- **Çiftçi Elif:** Pazarda satılmıyor ki! Hava var, kazan yok. Çırağa yaptıracağım.

**S-11 · Yeşeren tarla** — `D=5`
- **Bostancı Osman:** Elif, tarlan yeşil yeşil. Bana da yaptırsana.
- **Çiftçi Elif:** Parasını peşin ver, ama sayarak harcasın. Fazlası cebinden gider.

### 6.3 Batı yolu → Tepkime Türleri köyü

**S-12 · Güvercinler** — `T<1`
- **Güvercinci Selim:** Bu sabah üç mektup geldi, üçü de atölyeye.
- **Lonca Kâtibi Nevin:** Kasaba soru sormaya başladı demek. Güzel.
- **Güvercinci Selim:** Cevapları da çırak mı yazacak?
- **Lonca Kâtibi Nevin:** Yazmayacak, işaretleyecek. Mürekkep israf olmasın.

**S-13 · Etiketler** — `T≥2`
- **Attar Saadet:** Çırak benim karışık şişeleri de ayırsa ya. Birleşen bir yana, ayrışan bir yana…
- **Lonca Kâtibi Nevin:** Yananı da üçüncü yana. Saadet, sen de lonca sınavına gir bari.

**S-14 · Sınav heyecanı** — `T≥4`, `T<5`
- **Lonca Kâtibi Nevin:** Sınav kâğıtları mühürlü, kimse görmedi.
- **Güvercinci Selim:** Ezberleyen geçemez diyorlar, doğru mu?
- **Lonca Kâtibi Nevin:** Yanlış yapan başka soru alır. Ezber işe yaramaz, anlamak gerek.

**S-15 · Lonca açıldı** — `T=5`
- **Güvercinci Selim:** Kulenin bayrağı ilk kez dalgalanıyor.
- **Lonca Kâtibi Nevin:** Loncaya yeni bir mühür girdi. Kasabanın en genç simyacısı.

### 6.4 Kilitli bölge ağızları (pus, kapalı kapı)

**S-16 · Hesaplamalar köyü kapısı** — `Her zaman`
- **Bekçi Tuncay:** Bu kapı şimdilik kapalı. İçeride herkes tartı ve hesapla uğraşıyor.
- **Bekçi Tuncay:** Önce denkleştirmeyi öğren, sonra gel. Kaç tane olduğunu bilmeyen, ne kadar olduğunu da bilemez.

**S-17 · Güney pusu (Kimya Bilimi ilçesi)** — `Her zaman`, tek kişi (bark)
- **Yaşlı Hayriye** (pus kenarında oturur): Pusun ardında eski simyacıların köyü var derler. Ben gitmedim. Sen de daha gitme.

**S-18 · Uzak dağlar (diğer şehirler)** — `D=5` ve `T=5`
- **Güvercinci Selim:** Şu dağların ardında başka şehirler var. Biri sayılarla, biri ışıkla, biri canlılarla uğraşırmış.
- **Lonca Kâtibi Nevin:** Hepsi aynı adada. Bir gün yolu açılır.

### 6.5 Tek satırlık laf atmaları (bark, yanından geçerken; rastgele değil, sırayla döner)

| Kim | Satırlar (biri seçilir, sıradaki sefere diğeri) |
|---|---|
| Körükçü Bahar | "Mühür sağlam, hava tamam!" · "Kaç kavanoz? Saymadan alma." |
| Kireççi Cemal | "Kireç taşı, tozu bende, hesabı sende." · "Diri kirece eldivensiz dokunma!" |
| Sakacı Nuri | "Sııı-uuu!" · "Çeşme aktı, sırtım rahatladı." (`D≥1`) |
| Attar Saadet | "Gül suyu taze." · "Kükürdü arkada tutuyorum, burnum alışmadı." |
| Demirci Rıza | "Bakırla kalay, tunç olur." · "Altın satılık değil, sorma." |
| Takasçı Gülsüm | "Artan varsa alırım!" · "Yarı fiyat ama israf yok." |
| Ayşe / Mert | "Ebe sensin!" · "Çırak, oynasana!" |
| Muhtar Yusuf | `D<1`: "Su olmazsa kasaba olmaz." · `D≥1`: "Çeşmemiz şırıl şırıl, sayende." |
| Usta (atölye kapısında, yalnız ses) | "Acele etme, say." · "Kazana ne girerse o çıkar." |

Not: Oyuncunun adı ve cinsiyeti yok (`01` §2) → NPC'ler hep **"çırak"** der; "abla/abi/kızım/oğlum" kullanılmaz.

---

## 7. Kod ve sahne için notlar (karar Mehmet'in)

Öneri, kod değil:

| İhtiyaç | Önerilen veri / bileşen | Not |
|---|---|---|
| Yol sohbeti | `AmbientDialogueData` (SO): `id`, `lines[] {speakerName, text}`, `condition` (köy + min/max görev), `playOnce` | Metin SO'da (`CLAUDE.md` §8) |
| Tetik | `AmbientDialogueTrigger` (trigger collider + `AmbientDialogueData[]`, ilk uygun olanı çalar) | Player layer'ı ile |
| Altyazı | Yeni arayüz `ISubtitleView` (konuşan adı + satır, otomatik ilerleme ~2,5 sn/satır) | `IDialogueView`'dan ayrı: oyuncuyu durdurmaz |
| Bark | `BarkData` (SO) + yakınlık kontrolü, NPC başına 20 sn bekleme | |
| Pazar | `ShopData` (SO: satıcı, satılan türler + fiyat), `ShopStation : StationBase`, `IShopView` | D-5'in sandığı = Hava Tezgâhı |
| Cüzdan | `SaveData.coins` + `GameEvents.CoinsChanged`; HUD'da kese | |
| Yan sipariş | Mevcut `OrderTask`, `isSide = true` (köy ilerlemesine sayılmaz) | |
| NPC önce/sonra | Mevcut `NeighborhoodView` `Before/After/Parts` altına NPC'nin iki hâli konur | Yeni kod gerekmez |

Mustafa'nın sahne işi: tezgâhlar (5), NPC yerleşimi (14), tetik kürelerinin yeri (§6 başlıklarındaki konumlar: meydan, köprü, çarşı, fırın, tarla, batı yolu, lonca, kilitli kapılar).
