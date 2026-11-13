# 02 — Sipariş Mekaniği (Kazan + Terazi)

Uygulama gerektiren konuların görev tipi. Prototipte Denkleştirme köyünün 5 görevi.

## 1. Temel fikir

Kavanozlar **mühürlüdür**: içindeki madde (formül) değiştirilemez. Oyuncu yalnızca **kaç kavanoz** koyacağını seçer. Böylece "denkleştirmek için alt indisi değiştirme" kavram yanılgısı mekanik olarak imkânsızdır. Jüriye gösterilecek ana nokta budur.

## 2. Sahne öğeleri

| Öğe | Davranış |
|---|---|
| Malzeme rafı | Görevin izin verdiği kavanozlar (`OrderTask.shelf`). Tamamlanan görevlerin ürünleri eklenir. |
| Kazan | Kavanoz buraya sürüklenir. Kazandaki her tür için `−` / `+` sayaç. Sayaç 0 olursa kavanoz rafa döner. |
| Giren terazisi (sol) | Kazandaki maddelerin atomlarını gösterir. |
| Çıkan terazisi (sağ) | Siparişin hedef ürünlerinin atomlarını gösterir (sabit). |
| Atom sayaçları | Her terazinin altında element başına: `H 4 / 4`. Eşitse yeşil, değilse kırmızı. |
| Ocak | Teraziler dengedeyse kendiliğinden yanar. |
| "Ateşi yak" düğmesi | Her zaman basılabilir. Dengesizken basılırsa → köpürme. |
| Sipariş kartı | İstenen ürün ve miktar. Görev 1–2'de girenler de yazar. |
| Ustanın defteri | 2 köpürmeden sonra parlar; ipucu metni. |
| Karatahta | Biten görevin denklemi ders kitabı biçiminde eklenir. |
| Tedarik sandığı + kese | Yalnızca `OrderTask.mode == Purchase` görevlerinde. |

## 3. Kurallar

### 3.1 Atom sayımı
```
sol[e]  = Σ (kazandaki_adet[tür] × tür.bileşim[e])
sağ[e]  = Σ (hedef_adet[ürün] × ürün.bileşim[e])
denge   = her e için sol[e] == sağ[e]
```
Kazan değiştikçe sol terazi ve sayaçlar **anında** güncellenir.

### 3.2 Modlar
| Mod | Başarı koşulu | Kullanıldığı yer |
|---|---|---|
| `Exact` | Tüm elementlerde sol == sağ | Görev 1–4 |
| `Purchase` | Her giren için `kazandaki ≥ gereken`; fazlası **artan madde** olarak terazide ayrı durur ve parası geri gelmez | Görev 5 |

`Purchase` modunda "gereken" = tepkimenin katsayıları × (hedef ürün / ürün katsayısı). Eksik varsa ocak yanmaz; oyuncu sandıktan eksiğini alabilir. Para biterse ve eksik varsa: sakin bir kez "Bir dahakine dikkat" der ve eksiği verir (oyun kilitlenmez).

### 3.3 Zincir görev
`OrderTask.steps` birden fazla adım içerebilir (Görev 4: CaCO₃ → CaO + CO₂, ardından CaO + H₂O → Ca(OH)₂). Her adım ayrı bir denge döngüsüdür. Adım ürünü rafa düşer ve sonraki adımda malzeme olur.

### 3.4 Mühür
Oyuncu kavanoz etiketine tıklayınca etiket düzenlenmez; mühür titrer ve not çıkar:
> "Kavanozun içindekini değiştiremezsin. Kaç kavanoz koyacağını değiştirirsin."

İlk denemede bir kez gösterilir, sonra yalnızca titreme.

### 3.5 Köpürme (zorlama)
Dengesizken "Ateşi yak":
1. Kazan köpürür (partikül + ses, ~1,2 sn).
2. Farkı oluşturan atomlar yanıp söner: fazlalar kırmızı, eksiklerin yeri kesikli boş daire.
3. Kazan boşalmaz; oyuncunun seçimi korunur (hızlı yeniden deneme için). *Senaryo taslağındaki "kazan boşalır" yerine bu tercih edildi: daha az sinir bozucu.*
4. `failCount++`. `failCount == 2` → defter parlar.

Hata anında ekrana açıklama metni çıkmaz; açıklama yalnızca defter açılırsa gelir.

### 3.6 Başarı
1. Ocak yanar, tepkime animasyonu (~1,5 sn).
2. Ürün kavanozu rafa eklenir.
3. Tahtaya denklem yazılır (katsayı 1 ise yazılmaz: `CH₄ + 2O₂ → CO₂ + 2H₂O`).
4. "Gerçek hayatta" kartı (tek cümle) açılır.
5. Sakin teşekkür eder → görev biter → köy parçası canlanır.

## 4. Arayüz kuralları

- Atomlar kefede **element başına sıra** halinde dizilir (C'ler bir sıra, H'ler bir sıra). 20+ atomda küçülerek sığar.
- Her atom küresinin üstünde harf (H, O, C, N, Ca). Renk tek başına bilgi taşımaz.
- Renkler: H beyaz, O kırmızı, C koyu gri, N mavi, Ca açık yeşil.
- Yazı en az 28 px (1080p). Projektörde test edilir.
- Sürükle-bırak + `−/+` düğmeleri birlikte; dokunmatik ve fare için.

## 5. Kabul kriterleri (bitti sayılması için)

- [ ] Kavanoz raftan kazana sürüklenir, `−/+` çalışır, 0'da rafa döner.
- [ ] Sol terazi ve sayaçlar her değişimde doğru güncellenir.
- [ ] Denge sağlanınca ocak kendiliğinden yanar; sağlanmazsa yanmaz.
- [ ] Dengesizken "Ateşi yak" → köpürme + doğru atomlar yanıp söner.
- [ ] 2 köpürmeden sonra defter parlar, açılınca görevin ipucu metni görünür.
- [ ] Etikete tıklayınca mühür notu çıkar; formül değişmez.
- [ ] Zincir görevde 1. adımın ürünü rafa düşer ve 2. adımda kullanılabilir.
- [ ] `Purchase` modunda para düşer, fazlalar "artan" olarak gösterilir, eksikte ocak yanmaz.
- [ ] Başarıda tahtaya denklem doğru biçimde (alt indis, 1 katsayısı gizli) yazılır.
- [ ] 5 görevin hepsi uçtan uca oynanır.
