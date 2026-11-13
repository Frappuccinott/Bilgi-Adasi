# Oyun UI / UX referansları (13.11)

İlk tur ekranlar (Figma "Ekranlar (v1)") **reddedildi**: düz vektör arka plan, her yerde kalın (8–10 px) kontur, sert ofset gölge, kutu içinde kutu, tahta düğme yığını. Sonuç "çıkartma / mobil oyun" gibi duruyordu. Bu dosya ikinci turun neye dayandığını yazar.

| # | Kaynak | Ne alındı |
|---|---|---|
| 1 | Potion Craft: Alchemist Simulator — UI incelemesi: https://medium.com/@saragiraltfdez/potion-craft-and-its-lovingly-well-crafted-ui-57670cec4e13 | Simya teması; arayüz el yazması/defter dili ama **ince** çizgi ve süsle; tıklanabilir olanın durumu her an belli. Hedefler bir kitapta tutulur → duraklatmada "Ustanın defteri". |
| 2 | Potion Craft önizlemesi (PC Gamer): https://pcgamer.com/grind-the-herbs-yourself-in-alchemy-simulator-potion-craft | Tezgâhın iki yanında menüler, ortası oyuna açık → kazan istasyonunda ortayı (kazanı) boş bırak, paneller kenarda. |
| 3 | Dorfromantik tasarım yazısı (80.lv): https://80.lv/articles/how-dorfromantik-expands-its-cozy-world-through-minimalist-design | Minimalizm + okunurluk + rahatlama. HUD neredeyse yok; bilgi gerektiğinde, küçük ve köşede. |
| 4 | Game UI Database: https://www.gameuidatabase.com | Ana menü / duraklatma kalıpları: sol hizalı metin menü + oyun sahnesi arkada + kenardan koyulaşan geçiş (scrim). Kutu düğme yerine metin + odak işareti. |
| 5 | Kingdom Come: Deliverance (ortaçağ el yazması UI) — genel gözlem | Parşömen sayfada ince çift çizgi çerçeve, köşe süsü, mühür; başlıklar harf aralıklı büyük harf. |
| 6 | Sea of Thieves — diegetik harita masası (genel gözlem) | Harita dünyada bir nesne: masanın üstünde, kâğıt kenarı yıpranmış; seçili yer mürekkep halkası. |
| 7 | Hades — tipografi ve odak durumu (genel gözlem) | Tek bir güçlü başlık yüzü, küçük harf aralıklı etiketler; odak = renk + ikon + hareket, kutu değil. |
| 8 | Bu oyunun kendi render'ları: `ArtSource/Marka/oyun_goruntu/hd_*.png` | Arka planlar artık gerçek oyun görüntüsü (Unity, 1920×1080). UI renkleri bu sahnenin ışığına göre seçilir. |

5–7 numaralı satırlar bilinen oyunlardan genel gözlemdir; bağlantı eklenmedi, görsel repoya konmadı.

## Çıkan kurallar (v2)

1. **Arka plan gerçek oyun.** UI, sahnenin üstünde yüzer; okunurluk için kenardan mürekkep tonunda geçiş (scrim), bulanıklık yok.
2. **İnce çizgi.** Kontur 1,5–3 px, bakır/altın; çift çizgi çerçeve ve köşe süsü. Kalın kontur yalnızca logoda.
3. **Yumuşak gölge.** Bulanık (24–40 px), mürekkep tonunda, düşük opaklık. Sert ofset gölge yok.
4. **Metin menü.** Menü öğesi kutu değil: pasif = kâğıt rengi %75, odak = altın + bakır baklava işareti + fırça alt çizgi.
5. **Hiyerarşi.** Ekran başına bir büyük başlık; etiketler küçük, harf aralıklı büyük harf; gövde 28–32 px.
6. **Orta boş.** İstasyonlarda oyuncunun baktığı nesne (kazan, kâğıt) ortada ve açık; paneller kenarda.
7. **Tuş simgeleri tek sistem.** Klavye tuşu = kâğıt kapak + alt pah; gamepad = yuvarlak yüz düğmesi.

## AI izi temizliği (v2.1, 13.11)

v2'de "yapay zekâ yaptı" diye okunan şeyler ayıklandı. Yeni ekranlarda bunlar **kullanılmaz**:

- Harf aralıklı BÜYÜK HARF mikro etiketler (her panelde bir tane). Etiketler normal yazımla, az sayıda. Not: Figma'da `toLocaleUpperCase('tr-TR')` çalışmıyor, büyük harf gerekiyorsa İ elle yazılır.
- `·` ve `—` ayraçları, noktalı virgüllü uzun cümleler, "Usta bekler; kazan soğumaz." gibi aforizmalar.
- Başlık altına italik gri "yardım cümlesi". Tasarım notu ekrana değil, çerçeve dışına yazılır.
- Hap çip + tik/≠ rozeti, altın gradyanlı CTA düğmesi, ince altın konturlu koyu kutu. Bilgi kutusuz metin + gölge ile, eylem tuş ipucu biçiminde.
- Baklava madde işareti, fırça alt çizgisi, simetrik köşe süsleri. Odak = altın metin + kısa çizgi.
- Vektör çizgi film portre, harfli düz atom daireleri, clip-art ada amblemi. Yerine oyunun kendi modelleri: `portre_cabir.png` (Unity render), `UI/Ikonlar/Malzeme_*.png` kavanoz ikonları. Ekranda logo yalnızca yazı markası (`Logo/Yazı (arayüz)`).
- Sahnede olmayan bilgi: HUD'da 15 m ötedeki karaktere "Konuş" ipucu verilmez (menzil 2,2 m).
