# 02 — Sipariş Mekaniği (Pazar + Raf + Kazan + 3D Terazi)

Uygulama gerektiren konuların görev tipi. Prototipte Denkleştirme köyünün 5 görevi.

> **Karar 13.11:** Kazan UI'dan çıktı; her şey birinci şahıstan, crosshair + E (gamepad X) ile. Malzeme siparişe göre gelmez: oyuncu pazardan alır, rafına koyar, gerekeni seçip kazana döker. Teslim elle, kapıda.

## 1. Temel fikir

Kavanozlar **mühürlüdür**: içindeki madde (formül) değiştirilemez. Oyuncu yalnızca **kaç kavanoz** koyacağını seçer. Böylece "denkleştirmek için alt indisi değiştirme" kavram yanılgısı mekanik olarak imkânsızdır. Jüriye gösterilecek ana nokta budur. Ek olarak oyuncu **doğru maddeleri** de kendisi seçer (rafta her şey durur).

## 2. Akış

**Kapıda sipariş al → pazardan al (envanter) → istersen rafa koy → raftan/envanterden seç → kazana dök → 3D terazi dengelenir → ateşi yak → ürün envantere → kapıda teslim → ödeme + kayıt**

| Öğe | Davranış |
|---|---|
| Pazar tezgâhı (şehirde) | E → pazar ekranı. Ne satılacağı tezgâhın kendi **pazar kataloğunda** (bkz. §2.1). Alınan kavanoz envantere girer, akçe düşer. Pazar açıkken sipariş alınır/biterse liste kendiliğinden yenilenir. |
| Kese | Başlangıç 30 akçe; her teslim sipariş ödemesi kadar artar. Para **o an satışta olan** en ucuz kavanoza yetmezse Usta keseye borç koyar (oyun kilitlenmez). |
| Envanter (HUD, 6 slot) | Slot = tek maddenin yığını (O₂ ×5). Seçili slot "eldeki" madde (1–6 / tekerlek, LB/RB). Yeni madde aynı yığına, yoksa soldan ilk boş slota. |
| Raf (atölye) | 8 yer. Dolu yere E: yığının tamamı envantere. Boş yere E: eldeki yığının tamamı rafa. Kayıtlı. |
| Kazan | Elde madde varken E: **1 kavanoz** dökülür, envanterden 1 düşer. **Dökülen geri alınmaz.** Sıvı rengi/yüksekliği içeriğe göre. |
| Boşaltma kolu | Kazanı boşaltır; malzeme gider, hata sayılmaz. |
| 3D terazi | Sol kefe: kazandaki atomlar; sağ kefe: siparişin ürünleri (sabit). Element başına bir sıra küre + `H 4` etiketi; eşitse yeşil, değilse kırmızı. Kol farka göre eğilir. |
| Ocak | Kendiliğinden yanmaz. E ile yakılır: dengedeyse tepkime, değilse köpürme. |
| Sipariş kartı (HUD) | Veren, istenen ürün ve miktar, ödeme. Görev 1–2'de girenler de yazar. Hazırsa "kapıya götür". |
| Ustanın defteri | 2 köpürmeden sonra parlar; H / Y ile ipucu. |
| Karatahta (3D) | Biten adımın denklemi ders kitabı biçiminde eklenir. |

### 2.1 Pazar kataloğu ve kuralları

Her tezgâh kendi kataloğunu taşır (`MarketCatalog`, prototipte `Data/Market/Pazar.asset`). İkinci bir pazar = yeni bir katalog asset'i + tezgâh; kod değişmez. Katalogda her satır (hepsi Inspector'dan):

| Alan | Anlamı |
|---|---|
| Madde | Satılan madde (`SpeciesData`). Sıra = ekrandaki sıra. |
| Fiyat | İşaretsizse maddenin kendi fiyatı; işaretliyse satıra yazılan fiyat. |
| Stok | Sınırsız ya da adet. Satılanlar kayda yazılır (tezgâh başına, katalog adıyla). |
| Şu görev bitince açılır | Boşsa baştan satılır. |
| Şu görev bitince kalkar | Boşsa hiç kalkmaz. |

Satıra ne olacağına kurallar karar verir (sırayla; her biri ayrı sınıf, saf C#, `Scripts/Order/Market/`):

1. **Görevle aç / kapat** (`TaskGateRule`): açılış görevi bitmediyse ya da kalkış görevi bittiyse satır gizlenir.
2. **Siparişin ürününü satma** (`HideOrderProductRule`, otomatik): alınmış siparişin bütün adımlarının ürünleri (ara ürünler dahil) gizlenir; oyuncu üretmek zorunda. Katalogda işaretlemeye gerek yok.
3. **Stok** (`StockRule`): kalan = stok − satılan; 0 olunca satır gizlenir ("tükendi" yazısı yok).

Yeni kural = `IMarketRule`'u uygulayan yeni bir sınıf, `MarketRuleSet`'e eklenir; ötekilere dokunulmaz.

## 3. Kurallar

### 3.1 Atom sayımı
```
sol[e]  = Σ (kazandaki_adet[tür] × tür.bileşim[e])
sağ[e]  = Σ (hedef_adet[ürün] × ürün.bileşim[e])
denge   = her e için sol[e] == sağ[e]  VE kazanda tepkimenin girenleri dışında madde yok
```
Kazan değiştikçe terazi **anında** güncellenir. Ürünü (ör. H₂O) dökerek atomları eşitlemek **denge sayılmaz** (yabancı madde).

### 3.2 Modlar
| Mod | Başarı koşulu | Kullanıldığı yer |
|---|---|---|
| `Exact` | Tüm elementlerde sol == sağ, yabancı madde yok | Görev 1–4 |
| `Purchase` | Her giren için `kazandaki ≥ gereken`, yabancı madde yok; fazlası **artan** (terazide "+n artan") ve boşa gider | Görev 5 |

"Gereken" = tepkimenin katsayıları × (hedef ürün / ürün katsayısı).

### 3.3 Zincir görev
`OrderTask.steps` birden fazla adım içerebilir (Görev 4: CaCO₃ → CaO + CO₂, ardından CaO + H₂O → Ca(OH)₂). Her adım ayrı bir denge döngüsüdür. Adım ürünleri **envantere** düşer; sonraki adımda oyuncu onları döker.

### 3.4 Mühür
Not ilk köpürmede kendiliğinden bir kez gösterilir:
> "Kavanozun içindekini değiştiremezsin. Kaç kavanoz koyacağını değiştirirsin."

### 3.5 Köpürme
Dengesizken (ya da yabancı madde varken) ateş yakılırsa:
1. Kazan köpürür (~1,2 sn), terazide eşit olmayan satırlar kırmızı yanar, kol sallanır.
2. **Kazandaki malzeme gider** (karar 13.11); oyuncu baştan döker (gerekirse pazardan yeniden alır).
3. `failCount++`. `failCount == 2` → defter parlar.

### 3.6 Başarı ve teslim
1. Ocak yanar, tepkime (~1,5 sn). Tahtaya denklem yazılır (katsayı 1 yazılmaz).
2. Ürün soldan ilk boş slota gelir (1, 2, 3 doluysa 4'e). Envanter doluysa kazanda bekler; yer açılınca kazana E ile alınır.
3. "Gerçek hayatta" kartı birkaç saniye görünür.
4. **Teslim elle:** oyuncu kapıya gider; **seçili slotta** istenen ürün olmalı ve envanterde istenen adet bulunmalı.
   - Elde bir şey yok → "Siparişim hazır mı?"; başka ürün → "Ben bunu istemedim."; eksik → "Eksik var gibi."
   - Doğruysa ürün envanterden çıkar, ödeme keseye girer, sakin teşekkür eder, görev biter, **oyun kaydedilir**, köy parçası canlanır. **Sıradaki sakin hemen gelmez:** oyuncu istediği kadar dolaşabilir; kapıyı yeniden kullanınca yeni sipariş gelir.

## 4. Arayüz kuralları

- Atomlar kefede **element başına sıra** halinde dizilir; sıranın solunda element harfi + sayı (renk tek başına bilgi taşımaz).
- Renkler: H beyaz, O kırmızı, C koyu gri, N mavi, Ca açık yeşil.
- HUD yazısı en az 28 px (1080p). Projektörde test edilir.

## 5. Kabul kriterleri

- [ ] Pazardan alınan kavanoz envantere girer, akçe düşer; para bitince Usta borç verir.
- [ ] Sipariş alınınca ürünü pazardan kalkar, teslimde geri gelir; görevle açılan/kalkan satırlar doğru zamanda değişir; stoklu satır bitince kalkar ve yeniden açılışta bitik kalır.
- [ ] Raf: al / koy çalışır, yeniden açılışta raf aynı.
- [ ] Kazana her E bir kavanoz döker, geri alınamaz; terazi her dökümde doğru güncellenir.
- [ ] Boşaltma kolu kazanı boşaltır.
- [ ] Dengedeyken ateş → tepkime, ürün envantere; dengesiz ya da yabancı madde → köpürme, malzeme gider.
- [ ] 2 köpürmeden sonra defter parlar, H/Y ile ipucu.
- [ ] Zincir görevde 1. adımın ürünü envantere düşer ve 2. adımda dökülebilir.
- [ ] `Purchase` modunda fazlalar "artan" görünür.
- [ ] Kapıda yanlış ürün → "Ben bunu istemedim"; doğru ürün → teslim + ödeme + kayıt.
- [ ] Başarıda tahtaya denklem doğru biçimde yazılır.
- [ ] 5 görevin hepsi uçtan uca oynanır.
