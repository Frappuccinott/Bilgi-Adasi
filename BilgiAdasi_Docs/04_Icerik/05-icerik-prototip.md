# 05 — Prototip İçeriği

Kimya şehri → Etkileşim ilçesi → iki oynanabilir köy. Bu dosyadaki her satır bir ScriptableObject'e girilir.

> Kimya içeriği ders kitabıyla kontrol edilecek (`06` → açık işler). Tür adları: **Sentez (oluşum)**, **Analiz (ayrışma)**, **Yanma**.
>
> Bu dosyadaki satırlar değişmez. Her göreve eklenen eğitsel katmanlar (Keşif Defteri, Hata Dedektifi, Ustanın Sorgusu, Atom Görüşü, tahmin adımı) ve Matematik/Biyoloji/Fizik saha notları seviye seviye `02_Mekanik/04-gorev-plani.md` §4'te.

## 1. Temel veriler

**Elementler:** H (beyaz), O (kırmızı), C (koyu gri), N (mavi), Ca (açık yeşil)

**Türler (SpeciesData):**

| Formül | Ad | Bileşim |
|---|---|---|
| H₂ | Hidrojen | H2 |
| O₂ | Oksijen | O2 |
| H₂O | Su | H2 O1 |
| CH₄ | Metan | C1 H4 |
| CO₂ | Karbondioksit | C1 O2 |
| C₃H₈ | Propan | C3 H8 |
| CaCO₃ | Kireç taşı | Ca1 C1 O3 |
| CaO | Sönmemiş kireç | Ca1 O1 |
| Ca(OH)₂ | Sönmüş kireç | Ca1 O2 H2 |
| N₂ | Azot | N2 |
| NH₃ | Amonyak | N1 H3 |
| CO | Karbon monoksit | C1 O1 |

**Tepkimeler (ReactionData):**

| id | Denklem | Tür |
|---|---|---|
| R_water | 2H₂ + O₂ → 2H₂O | Sentez |
| R_methane | CH₄ + 2O₂ → CO₂ + 2H₂O | Yanma |
| R_propane | C₃H₈ + 5O₂ → 3CO₂ + 4H₂O | Yanma |
| R_lime1 | CaCO₃ → CaO + CO₂ | Analiz |
| R_lime2 | CaO + H₂O → Ca(OH)₂ | Sentez |
| R_ammonia | N₂ + 3H₂ → 2NH₃ | Sentez |
| R_co | 2CO + O₂ → 2CO₂ | Yanma |
| R_waterSplit | 2H₂O → 2H₂ + O₂ | Analiz |

## 2. Köy: Denkleştirme (5 sipariş)

**Usta girişi:** "Bu köyde herkesin derdi bir 'kaç tane' sorusu. Kazana ne girerse o çıkar; ne bir eksik, ne bir fazla. Kavanozlar mühürlü, içlerini değiştiremezsin. Sen yalnızca kaç tane koyacağını seçersin."

| # | Sakin | Köy yeri | Tepkime | Mod | Raf | Hedef | Kart girenleri gösterir |
|---|---|---|---|---|---|---|---|
| 1 | Muhtar Yusuf | Meydan çeşmesi | R_water | Exact | H₂, O₂ | 2 H₂O | Evet |
| 2 | Fenerci Demir | Çarşı fenerleri | R_methane | Exact | CH₄, O₂ | 1 CO₂ + 2 H₂O | Evet |
| 3 | Fırıncı Zehra | Fırın | R_propane | Exact | C₃H₈, O₂ | 3 CO₂ + 4 H₂O | Hayır |
| 4 | Duvarcı Halil | Taş köprü | R_lime1 → R_lime2 | Exact (2 adım) | CaCO₃, H₂O | 1 Ca(OH)₂ | Hayır |
| 5 | Çiftçi Elif | Tarlalar | R_ammonia | Purchase | (pazardan; fazlası artan) | 4 NH₃ | Hayır |

**Ekonomi (karar 13.11, taslak — oynanarak ayarlanacak):** başlangıç kesesi 30 akçe, Usta borcu 10 (`Data/Economy.asset`). Sipariş ödemeleri: 10, 12, 15, 15, 25 (görevin `reward` alanı). "Raf" sütunu artık kullanılmıyor; malzeme pazardan alınır.

**Pazar (`Data/Market/Pazar.asset`):** H₂ 2, O₂ 1, H₂O 1, CH₄ 3, C₃H₈ 5, CaCO₃ 4, N₂ 3 akçe (maddenin kendi fiyatı), hepsi stoğu sınırsız ve baştan açık. Alınmış siparişin ürünü pazarda otomatik görünmez (ör. Görev 1'de H₂O). Satıra stok, "şu görev bitince açılır / kalkar" ya da farklı fiyat vermek yalnızca veri işidir (02 §2.1).

**Replikler ve kartlar:**

1. **Muhtar Yusuf** — "Çeşme kurudu evladım! Çocuklar dereye iniyor, dere de kurudu. Sen de bilirsin değil mi?" · Teslim: "Aktı! Gerçekten aktı!" · İpucu: "Sudaki oksijenleri say. Bir su kaç oksijen ister?" · Gerçek hayatta: "Hidrojen ile oksijen gerçekte çok hızlı ve gürültülü tepkimeye girer; yalnızca laboratuvarda, uzman gözetiminde yapılır."
2. **Fenerci Demir** — "Fenerler is yapıyor. Akşam çarşıda kimse kimseyi görmüyor. Gaz bende, hava sende. Ne kadar lazımsa o kadar ver." · Teslim: "Hah. Şimdi oldu. Sağ ol çırak." · İpucu: "Sağ terazideki oksijenleri say: karbondioksitte kaç, suda kaç?" · Gerçek hayatta: "Yakıt yeterli havayla yanmazsa is ve zehirli karbon monoksit çıkar; soba ve şofben olan odalar bu yüzden havalandırılır."
3. **Fırıncı Zehra** — "Fenercinin fenerlerini yakmışsın, duydum. Benim fırın onun fenerinden büyük ama. Kasaba iki gündür peksimet yiyor." · Teslim: "İşte bu koku! Akşama ilk somun senin." · İpucu: "Oksijeni sona bırak. Önce karbonu, sonra hidrojeni eşitle." · Gerçek hayatta: "Mutfaktaki tüp gazı da tam yanınca mavi alev verir; sarı ve isli alev hava yetmediğini gösterir."
4. **Duvarcı Halil** — "Harç için sönmüş kireç lazım. Kireç taşı var ama. Ustan taştan kireç çıkarırdı, iki adımda. Acele etme." · Teslim: "Tam kıvamında. Çırağı ustasını aratmıyor." · İpucu: "Her denklem katsayı istemez. Saymadan kavanoz ekleme." · Gerçek hayatta: "Kireç söndürülürken çok ısı çıkar ve kireç cilde, göze zarar verir; ustalar eldiven ve gözlük takar."
5. **Çiftçi Elif** — "Dört kavanoz gübre ruhu istiyorum, ne eksik ne fazla. Parasını peşin veriyorum. Artanın hesabı senin cebinden." · Teslim: "Dört kavanoz, tastamam. Hesabını bilen simyacı!" · İpucu: "Önce bir kavanozluk denklemi kur, sonra ikiye katla." · Gerçek hayatta: "Amonyak en çok üretilen gübre hammaddelerindendir; keskin kokulu, zararlı bir gazdır ve kapalı fabrikalarda üretilir."

## 3. Köy: Tepkime Türleri (5 masa görevi)

**Usta girişi:** "Her tepkimenin bir huyu var. Kimi birleştirir, kimi ayırır, kimi yakar. Kasaba sana mektup yazacak; cevapları masada vereceksin."

### Görev 1 — Ustanın Defteri (FillBlank)
Kelime bankası: `sentez` · `analiz` · `yanma` · `oksijen` · *(çeldirici)* `buharlaşma` · `azot`
- "İki ya da daha fazla maddenin birleşip tek bir ürün oluşturduğu tepkimeye {0} denir." → sentez
- "Tek bir maddenin daha basit maddelere ayrıştığı tepkimeye {0} denir." → analiz
- "Bir maddenin {0} ile tepkimeye girip genellikle ısı ve ışık verdiği tepkimeye {1} denir." → oksijen, yanma

İpucu: "Kelimelere bak: birleşmek, ayrışmak, yanmak."

### Görev 2 — Etiket Masası (Classify)
Kutular: **Sentez** · **Analiz** · **Yanma**
Kartlar: R_water → Sentez · R_lime1 → Analiz · R_methane → Yanma · R_waterSplit → Analiz · R_ammonia → Sentez · R_propane → Yanma

İpucu: "Girenlere bak: tek madde mi var, oksijen var mı, ürün tek mi?"

### Görev 3 — Fırıncının Mektubu (MultipleChoice)
"Çırak, fırında gaz yanınca neler çıkıyor? Usta bir keresinde söylemişti, unuttum." — Zehra
- Propan tam yandığında ürünler hangisidir?
  - **Karbondioksit ve su** ✔
  - Hidrojen ve oksijen
  - Yalnızca su
  - Azot ve su

İpucu: "C₃H₈ + 5O₂ → ? Tahtana bak."

### Görev 4 — Muhtarın Mektubu (TrueFalse)
"Kasabada dedikodu çok. Hangisi doğru, hangisi yanlış, işaretleyip gönder." — Muhtar Yusuf
- Analiz tepkimesinde tek bir giren birden fazla ürüne ayrışır. → D
- Yanma tepkimelerinde oksijen girenler arasındadır. → D
- Sentez tepkimesinde ürünlerin sayısı girenlerden fazladır. → Y
- Bir tepkimede atomlar kaybolabilir. → Y

İpucu: "Ustanın tek kuralını hatırla: kazana ne girerse o çıkar."

### Görev 5 — Lonca Sınavı (GuildExam, randomCount = 4)
Havuz: Görev 1–4'teki bütün sorular + aşağıdakiler. Yanlış soru havuzdan başkasıyla değişir.
- MC: "CaCO₃ → CaO + CO₂ hangi tür tepkimedir?" → **Analiz** / Sentez / Yanma / Çözünme
- MC: "N₂ + 3H₂ → 2NH₃ hangi tür tepkimedir?" → **Sentez** / Analiz / Yanma / Nötrleşme
- MC: "Aşağıdakilerden hangisi bir yanma tepkimesinin girenidir?" → **O₂** / N₂ / H₂O / CO₂
- BalanceEquation: R_water, R_methane, R_co (otomatik)
- Classify (mini): 2CO + O₂ → 2CO₂ → Yanma; 2H₂O → 2H₂ + O₂ → Analiz

Gerçek hayatta (köy sonu): "Sindirim, paslanma, ekmeğin kabarması... Çevrendeki birçok değişim bu tepkime türlerinden biridir."

## 4. Kapanış

- Usta mektubu: "Çırağım, iki köyü ayağa kaldırdın. Tahtana baktım: hiçbir satırda bir atom eksik değil, bir atom fazla değil. Hesaplamalar Köyü seni bekliyor. Ama önce... uyanman gerek."
- Sınıf: Öğretmen tahtaya `CH₄ + _O₂ → CO₂ + _H₂O` yazar. "Bunu kim denkleştirir?" Oyuncu (opsiyonel mini etkileşim: katsayıları seçer) → öğretmen gülümser.
- Son ekran: oyuncunun tahtası + görev başına hata sayısı (öğretmen/jüri için, puan değil) + "Devam edecek" + Emeği Geçenler.
