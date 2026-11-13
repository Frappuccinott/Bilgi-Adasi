# Referans — Öğrencinin FPS kolları

Kural: `09-tema-ve-model-kurallari.md` §6. Araştırma 13.11.2026. Görseller repoya konmaz.
Script: `ArtSource/Blender/Script/ogrenci_kol.py` · Kaynak dosya: `ArtSource/Blender/ogrenci.blend`
FBX: `Assets/_Project/Art/Animasyon/Ogrenci_Kol_Rig.fbx`

## 1. Hikâyedeki karşılığı

Oyuncu (Çırak) görünmez kalır (`01` §2): yüz, ad, cinsiyet yok. Kamerada yalnızca **ön kollar ve eller** görünür.
Öğrenci kimya dersinde uyuyakalıp adada uyandığı için hâlâ **kendi okul kıyafetini** giyiyor: modern bir
sweatshirt, kolları atölyede çalışırken dirseğe doğru sıvanmış. Ortaçağ dünyasındaki tek modern kumaş — hikâyeyi
tek bakışta anlatır (§5 madde 3: sebepli). Eller nötr: belirgin cinsiyet işareti, yüzük, oje, saat yok.

## 2. Kaynaklar ve alınanlar

| # | Kaynak | Ne alındı |
|---|---|---|
| 1 | [Unreal forum — Lowpoly Stylized First Person Arms (POLARRPANDAA)](https://forums.unrealengine.com/t/polarrpandaa-lowpoly-stylized-first-person-arms-fps-arms-ue5-ue4-compatible/2594583) | Stilize FPS kollarında ölçek: iki kol ~4,3 bin üçgen → bütçe 5.000. |
| 2 | [itch.io — First Person Arms Rig (keyschain)](https://keyschain.itch.io/first-person-arms-rig) | Kol başına ~900 üçgen yalın yaklaşım; parmakların ayrı modellenmesi bükülmeyi iyileştirir → parmaklar avuçtan ayrık uzatıldı, aralarında perde. |
| 3 | [Mocap Online — First-Person Animation Guide](https://mocaponline.com/blogs/mocap-news/first-person-animation-guide) | Kol iskeleti kameranın çocuğu; yürürken salınım üçüncü şahıstan daha kısıtlı. |
| 4 | [Animation Mentor — Animate first person POV](https://www.animationmentor.com/blog/tutorial-animate-first-person-pov-shots/) | Görünen ellerde ayna poz yok, küçük asimetri → sol/sağ bekleme pozu ve nefes fazı farklı. |
| 5 | [Steam forum — SCUM viewmodel FOV](https://steamcommunity.com/app/513710/discussions/0/3490879839973555273) · [CSDB — CS2 viewmodel](https://csdb.gg/guides/best-cs2-viewmodel/) | Kol modeli kamera FOV'u değişince kayar → pozlar FOV 70'e göre; FOV 60–90 aralığında el ekran içinde kalacak şekilde merkeze yakın. |
| 6 | [Bevy — first person view model örneği](https://bevy.org/examples-webgpu/camera/first-person-view-model/) | Kolların duvara girmemesi için ayrı katman/kamera ile çizim (Mehmet'e not, §4). |
| 7 | [Polycount — cartoon hand topology](https://polycount.com/discussion/166929/good-topology-technique-for-cartoon-hands) · [Creative Bloq — stylised character (C4D)](https://www.creativebloq.com/how-to/create-a-stylised-character-in-cinema4d) | Tombul el: kutudan çıkan dört parmak + yandan başparmak, düşük kafes + bir kademe yumuşatma. |
| 8 | [Renee Nejo — Stylized hand anatomy study (ArtStation)](https://reneenejo.artstation.com/projects/EzQ6d8) | Blok blok kütle: avuç kalın, parmaklar kısa ve kalın, boğumlar kütleyle. |
| 9 | [Amtgard wiki — Shirt](https://wiki.amtgard.com/Shirt) · [Rosalie Gilbert — Aprons](https://rosaliegilbert.com/aprons.html) | Çalışırken kol sıvamak dönem işi; kumaşın ön kolda **boğum boğum toplanması** → sıvanmış kolda 3 kat kıvrım. |
| 10 | [MEB kılık kıyafet yönetmeliği örneği (okul PDF)](https://osmanulubaskoskal.meb.k12.tr/meb_iys_dosyalar/38/14/766307/dosyalar/2024_12/06114815_kilikkyonetmeligiogrenci06aralik20241.pdf) | Okul kıyafeti okulca belirlenir, desen/işaret yok → düz, logosuz sweatshirt; ribanalı manşet. |
| 11 | [Thumbsticks — GDC 2015 The Art of Firewatch](https://www.thumbsticks.com/gdc-2015-the-art-of-firewatch) | Stilize birinci şahıs oyunda eller sade, düz renkli, dünyanın rengini taşır; detay değil siluet. |
| 12 | `BilgiAdasi_Docs/01_Tasarim/referans/tema-referans.webp` | Renk: sweatshirt soğuk taraf (uzak dağ), ten sıcak; gölge maviye/kızıla kayar, siyaha değil. |

## 3. Tasarım kararları

| Karar | Gerekçe |
|---|---|
| Ön kol + el yakın planda, üst kol kısa ve kaba | Kamerada görünmez; bütçe ele (#1, #2). |
| Sweatshirt `dag` paleti, kıvrımların içi gölge tonu | #10, #12. Lacivert paletin dışında; uzak dağ orta tonu okul kıyafeti gibi okunur. |
| Kol sıvanmış: 3 kıvrım + kalın manşet kenarı, sonra çıplak ön kol | #9; ten ile kumaş arasında net siluet çizgisi. |
| Ten: palete türetilmiş "Ten" satırı (09 §3) | Paletin dışına çıkılmaz. Üst yüz açık, avuç/alt orta, boğum ve parmak ucu hafif kızıl. |
| Tek mesh/kol, deri kaplı (deform) iskelet: Üst kol, Ön kol, El, 4×3 parmak, 3 başparmak | Avuç kapama ve tutma için gerekli (#2, #7). |
| `Tutma_R` kemiği (deform yok) | Eşya buraya bağlanır; konumu `el_testi.py` değerleriyle uyumlu (sağ +0,22 / aşağı 0,19 / ileri 0,42). |
| Klipler: `Kol_Bosta` (gizli), `Kol_Kaldir`, `Kol_Bekle_Dongu`, `Kol_Uzan`, `Kol_Tut_Dongu`, `Kol_Indir` | Sakin oyun: eller yalnızca etkileşimde ya da eşya tutarken öne gelir (#3, #11). |
