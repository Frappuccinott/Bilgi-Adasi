# 01 — Oyun Tasarımı

## 1. Çerçeve hikâye

**Açılış (Sınıf, 2D ara sahne, ~30 sn):** Sonbahar sabahı, 10. sınıfın ilk kimya dersi. Öğretmen tahtaya "Kimyasal Tepkimeler" yazar. Oyuncunun gözleri ağırlaşır, ekran kararır.

**Uyanış (Atölye):** Gözünü taş ve ahşap bir atölyede açar. Kazan fokurdar, raflarda mühürlü kavanozlar. Kapıda yaşlı bir usta: **Câbir Usta**.

> "Nihayet uyandın. Kapımın önünde yatıyordun, çırak arıyordum zaten. Bu şehirde her köyün bir derdi var; dertler de ancak ölçüyle çözülür. Gel, sana ilk kuralı göstereyim."

**Ada:** Atölye, **Bilgi Adası**'nın Kimya şehrindedir. Adanın her şehri bir derstir; ilk açık şehir Kimya'dır, diğerleri sisin ardında bekler.

**Gövde:** Oyuncu Kimya şehrinin köylerinde dolaşır, her köyde 5 görev çözer, köy canlanır.

**Kapanış (prototip sonu):** Etkileşim ilçesinin iki köyü bitince usta bir mektup bırakır, ekran kararır, oyuncu sınıfta uyanır. Öğretmen tahtadaki denklemi sorar: "Bunu kim denkleştirir?" Oyuncunun eli kalkar. Son ekran: oyuncunun tahtası (çözdüğü denklemler) + "Devam edecek: Hesaplamalar Köyü" + Emeği Geçenler.

**Neden ortaçağ/simya:** Simya kimyanın atasıdır; Câbir bin Hayyân tarihte deneysel yöntemin öncülerindendir. Sunumda tek cümleyle söylenir.

## 2. Karakterler

| Karakter | Rol | Görsel | Not |
|---|---|---|---|
| Oyuncu (Çırak) | 10. sınıf öğrencisi | Birinci şahıs; yalnız **kolları** görünür: okul sweatshirt'ü (kollar sıvalı) + nötr eller (`Ogrenci_Kol_Rig`) | Adı/cinsiyeti yok; düşünceleri kısa notlar |
| Câbir Usta | Öğretmen, ipucu veren | **Model yok**: konuşma balonu + portre (2D) | Kısa, sakin, ikinci tekil; çözüm söylemez, nereye bakılacağını söyler |
| Köy sakinleri | Sipariş verenler | Kapıda sabit low-poly figür ya da 2D kart | Ana renk + mesleğini gösteren tek nesne |
| Ulak (postacı güvercin) | Masa görevlerini getirir | Pencereye konan güvercin (basit model/sprite) | Konuşmaz; mektup bırakır |

## 3. Harita hiyerarşisi

```
Bilgi Adası (dünya)
└── Şehir = Ders              → Kimya (açık). Matematik, Fizik, Biyoloji, Türkçe, Sosyal (sisli, kilitli — yalnızca görsel)
    └── İlçe = Ünite (Tema)   → Etkileşim (açık). Kimya Bilimi, Çeşitlilik, Sürdürülebilirlik (kilitli)
        └── Köy = Konu        → Tepkimeler (kilitli), Tepkime Türleri (OYNANABİLİR), Denkleştirme (OYNANABİLİR), Hesaplamalar (kilitli, "devam edecek")
            └── 5 Görev       → Sipariş ya da Masa görevi
```

**Örnek (adlandırma kuralı):** Kimya şehri → *Kimya Bilimi* ilçesi (ünite) → *Simya* köyü (konu). Dersin ünite adı ilçenin, ünitenin konu başlığı köyün adı olur. Prototipte Kimya Bilimi ilçesi haritada kilitli görünür; oynanabilir köyler Etkileşim ilçesindedir.

- Kilit kuralı (prototip): İki oynanabilir köy baştan açık, sıra serbest. Demo sırası: önce Denkleştirme.
- Köy durumu: `Kilitli` → `Açık` → `Devam ediyor (n/5)` → `Tamamlandı`.
- Oyuncu köyler arasında **yürür** (birinci şahıs). Harita (`OpenMap`) bilgi + ilerleme ekranıdır; hızlı seyahat yalnızca P2.
- Her köyün altında 5 nokta görünür (dolu = bitti).

## 4. İki görev tipi

| | Sipariş (uygulama) | Masa (sözel) |
|---|---|---|
| Ne zaman | Konu bir **işlem** gerektiriyorsa (denkleştirme, hesaplama) | Konu **kavram/sınıflandırma/tanım** ise (tepkime türleri, kanunlar) |
| Kim getirir | Kapıya gelen köy sakini | Pencereye konan ulak güvercin (mektup) |
| Nerede çözülür | Kazan + iki terazi | Çalışma masası (kâğıt, defter, etiketler) |
| Hikâye kılıfı | Sakinin gündelik derdi | Mektup, ustanın defteri, etiket masası, lonca sınavı (bkz. `03`) |
| Hata | Kazan köpürür, atom kırmızı yanar | Kâğıtta yanlış işaret mürekkep lekesine döner, tekrar denenir |
| Bitince | Tahtaya denklem yazılır, ürün rafa eklenir, köyde bir parça canlanır | Defterde sayfa dolar, köyde bir parça canlanır |

Bir köy tek tipten de olabilir, karışık da. Prototipte: Denkleştirme = 5 sipariş, Tepkime Türleri = 5 masa görevi.

## 5. Köy akışı

1. Köydeki sakin/ulak oyuncuyu **atölyeye** çağırır; oyuncu yürüyerek gelir.
2. **Atölyede** Usta köyün konusunu 2–3 cümleyle tanıtır (yalnızca ilk girişte).
3. **Görev 1–4**: sırayla gelir (kapı ya da güvercin).
4. **Görev 5**: köyün "sınavı" — sipariş köyünde en zor sipariş, masa köyünde **Lonca Sınavı** (havuzdan rastgele sorular).
5. **Köy canlanır** ("önce/sonra" değişimi köyde görülür), haritada tamamlandı işareti.

## 6. Ton ve ipucu

- Sıcak, hafif mizahlı. Kimse azarlanmaz.
- **İpucu kuralı (iki tip için ortak):** Aynı görevde 2 hata → ustanın defteri parlar. Açılırsa çözüm değil, bakılacak yer söylenir.
- Kimya terimleri yalnızca etiketlerde, terazilerde, tahtada, masadaki kâğıtlarda ve "Gerçek hayatta" kartlarında. Sakinler gündelik dille konuşur.
- Formüller her yerde alt indisle: H₂O (TMP: `H<sub>2</sub>O`).

## 7. İçerik kuralları (yarışma: uygun içerik)

- Şiddet, silah, patlama, yaralanma yok. Hata = köpük / mürekkep lekesi.
- Kimse yaşı, cinsiyeti, görünüşü, mesleği üzerinden alay konusu olmaz. Sakinlerde kadın-erkek dengeli.
- Dinî, siyasi, bölgesel ifade/sembol yok. Gerçek felaketleri çağrıştıran kurgu yok (maden kazası, yangın, deprem).
- Gerçek para, satın alma, reklam, şans/kumar mekaniği, kişisel veri toplama yok.
- Kimyasal güvenlik notları korkutmadan, tek cümle ("Gerçek hayatta" kartı). Açılışta tek satır: *"Bu atölyedeki tepkimeler bir oyundur. Gerçek kimyasallar yalnızca okul laboratuvarında, öğretmen gözetiminde denenir."*
- Uyuyakalma sahnesi esprili tutulur; öğretmen kızmaz, oyuncu uyandığında gülümser.
