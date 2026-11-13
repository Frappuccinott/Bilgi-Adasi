# Referans — Bisiklet (öğrencinin köyler arası aracı)

Kural: `09-tema-ve-model-kurallari.md` §6. Araştırma 13.11.2026. Görseller repoya konmaz.
Script: `ArtSource/Blender/Script/bisiklet.py` · Kaynak dosya: `ArtSource/Blender/bisiklet.blend`

## 1. Hikâyedeki karşılığı

Öğrenci ortaçağ havasındaki adada modern bir bisiklet bulamaz. Bisiklet **kasabanın arabacı ve demircisinin elinden çıkmış** gibi durmalı: ahşap tekerlek, demir lastik, bakır bağlantılar. Câbir Usta'nın çırağı köylere ayak işi yaptığı için önde **hasır sepet** var (malzeme taşıma; sebepsiz nesne değil — §5 madde 3).

Tip olarak 1860'ların **"kemik sarsan" velosipedi** (boneshaker) seçildi: pedallar doğrudan ön tekerlekte, zincir yok. Hem "eski zaman" okunur hem de bir arabacı atölyesinin yapabileceği kadar basittir.

## 2. Kaynaklar ve alınanlar

| # | Kaynak | Ne alındı |
|---|---|---|
| 1 | [Velocipede — Wikipedia](https://en.wikipedia.org/wiki/Velocipede) | Boneshaker yapısı: ahşap tekerlek + demir lastik, pedal ön göbekte, ~1 m tekerlek. |
| 2 | [Science Museum Group — English built boneshaker bicycle (co25717)](https://collection.sciencemuseumgroup.org.uk/objects/co25717/english-built-boneshaker-bicycle) | "Köy demircisi ve arabacısı yapmış" kaba işçilik → bizim kasaba hikâyesine uygun; oranlar kaba ve kalın. |
| 3 | [Silodrome — 1866 boneshaker](https://silodrome.com/for-sale-a-rare-boneshaker-bicycle-from-1866) | Araba yapımı yöntemiyle kurulmuş gövde; demir lastiklerin sıcak geçirilmesi (tekerlek çevresinde kalın bant). |
| 4 | [Motorious — 1866 boneshaker](https://www.motorious.com/articles/news/rare-1866-boneshaker-velocipede/) | Yan siluet: kavisli tek omurga, arka tekerlek küçük, sele yaprak yay üzerinde. |
| 5 | [ASME — Bicycle Bicentennial (Drais 1817)](https://asme.org/engineering-topics/articles/manufacturing-design/bicycle-bicentennial) | Draisine'in düz ahşap kirişi → bizde omurga ahşap, kalın ve hafif kavisli. |
| 6 | [Musée de Malartre — Draisienne à selle suspendue](https://www.musee-malartre.com/en/article/draisienne-selle-suspendue) | Asılı (yaylı) sele fikri; deri sele + demir yay. |
| 7 | [Amusing Planet — Drais ve Laufmaschine](https://www.amusingplanet.com/2024/03/karl-von-drais-and-laufmaschine.html) | Pirinç burçlu göbek → bakır paletinden göbek bileziği. Kiraz ağacı + yumuşak ağaç → iki ahşap tonu. |
| 8 | [road.cc — 10 stunning wooden bikes](https://road.cc/content/tech-news/183854-10-stunning-wooden-bikes) | Modern ahşap bisikletlerde kalın alt boru + ince üst boru: kütleyi tek kalın omurgada toplamak siluette okunur. |
| 9 | [makery — Top 10 wood/bamboo bicycles](https://www.makery.info/en/2018/03/06/le-top-10-des-velos-en-bois-et-bambou/) | Ahşap gövdede bağlantı yerlerinde bilezik/sargı → bakır bilezikler ve deri sargı. |
| 10 | [Unity Blog — How Frankenstein bikes inspired Wheel World](https://unity.com/en/blog/how-frankenstein-bikes-inspired-wheel-world) · [Creative Bloq — Wheel World](https://www.creativebloq.com/3d/video-game-design/how-wheel-worlds-massive-rolling-landscapes-were-made-possible-with-houdini) | Stilize oyunda bisiklet: abartılı kalın parçalar, az ama net parça; parçalar ayrı (tekerlek, gidon). |
| 11 | [CGTrader — stylized/toon bike pack örnekleri](https://www.cgtrader.com/3d-models/vehicle/bicycle/cartoon-bicycle) | Oyun hazır bisiklette parça ayrımı: teker, gidon, pedal ayrı; ~2–3 bin üçgen ölçeği. **Yalnızca ölçek/parça fikri; model kullanılmaz.** |
| 12 | [Bikepacking Simulator](https://nextquest.dev/games/bikepacking-simulator) | Birinci şahısta oyuncu gidonu, sepeti ve ön tekerleğin üstünü görür → bu üç parça en detaylı olanlar. |
| 13 | [Sinch bikes size guide](https://sinchbikes.co.nz/pages/size-guide) · [biketips sizing](https://biketips.com/how-to-choose-the-right-bike-size-a-complete-sizing-guide/) | 154–170 cm sürücü → küçük/orta gövde; sele ~0,78 m, gidon ~1,0 m. |
| 14 | `BilgiAdasi_Docs/01_Tasarim/referans/tema-referans.webp` | Renk ve ışık: sıcak ahşap, turuncu vurgu az, gölge maviye kayar. |

## 3. Tasarım kararları

| Karar | Gerekçe |
|---|---|
| **Ön tekerlek Ø0,78 m, arka Ø0,60 m**, aks arası ~1,05 m | Boneshaker oranı (#1, #4); 1,66 m öğrenci için ayak pedala yetişir (#13). |
| **Pedal ön göbekte (krank)**, zincir yok | Dönem mantığı; kodda ön tekerlek dönerken krank da döner — tek hareket. |
| **12 kalın ahşap jant teli**, ahşap jant (felloe) + demir lastik bandı | Araba tekerleği (#2, #3). Stilize: teller kalın, sayısı az (silueti bozmadan). |
| **Kavisli tek ahşap omurga** + bakır bilezikler | Draisine kirişi (#5) + ahşap bisiklet bağlantıları (#9). Siluetin ana çizgisi. |
| **Deri sele, demir yaprak yay üzerinde** | #4, #6. Ahşap gölge tonu (deri); yay demir. |
| **Ahşap gidon, deri sargılı tutamaklar, bakır zil** | FPS'te en çok görülen parça (#12): burada detay kütlede (pah, tombul tutamak). |
| **Hasır sepet (önde, çatala bağlı)** | Hikâye: ayak işi. Gidonla birlikte döner. |
| **Vurgu rengi (çatı turuncusu) yalnız jant kenar şeridinde ve sepet kurdelesinde** | 09 §3 oran kuralı: vurgu az ve oyuncunun gözünün olduğu yerde. |
| **Direksiyon ekseni 12° yatık**; `Bisiklet_Gidon` objesinin yerel Z'si = direksiyon ekseni | Hareketli parça pivotu ekseninde (09 §7). |

## 4. Ölçüler (1 birim = 1 m)

| Ölçü | Değer |
|---|---|
| Toplam uzunluk | ~1,55 m |
| Sele üstü | 0,80 m |
| Gidon tutamak yüksekliği | 1,00 m |
| Gidon genişliği | 0,58 m |
| Oturan göz yüksekliği (öneri) | ~1,45 m (sele + gövde; 1,56 m ayaktaki gözün ~%93'ü) |

## 5. Parça yapısı (hiyerarşi bir kademe)

| Obje | Pivot | Hareket |
|---|---|---|
| `Bisiklet` (boş üst obje) | arka-ön tekerlek ortası, zeminde | — |
| `SM_Bisiklet_Govde` | kök ile aynı | sabit (omurga, arka çatal, sele, yay) |
| `Bisiklet_Gidon` | direksiyon ekseni, ön aks hizası | yerel Z etrafında döner (sola/sağa) |
| `Bisiklet_On_Teker` | ön göbek | yerel X etrafında döner (+ krank) |
| `Bisiklet_Arka_Teker` | arka göbek | yerel X etrafında döner |
| `Bisiklet_Pedal_Sol` / `_Sag` | pedal mili | kod düz tutar (krankla döner, kendi ekseninde ters döner) |

Not: Ön teker ve pedaller gidonla birlikte dönmelidir; hiyerarşi bir kademe kuralı için Blender'da hepsi `Bisiklet`'in çocuğu. Unity'de prefab kurulurken `Bisiklet_On_Teker` ve pedaller `Bisiklet_Gidon`'un altına taşınır (FBX sorunu yalnız dışa aktarımda).

## 6. Sonuç (13.11, ilk sürüm)

- Toplam **4.704 üçgen** (gövde 884 · gidon+sepet 1.788 · ön teker+krank 1.064 · arka teker 816 · pedal 76×2). Bütçe: 09 §7 "Araç" satırı 4.800.
- FBX: `Assets/_Project/Art/Bisiklet/Bisiklet.fbx` (`export_bisiklet()`), tek malzeme `M_Stilize`.
- Kontrol edildi: yan, ¾, önden, üstten ve **oturan göz (1,45 m) kamerasından**; gidon, sepet, bohça okunuyor.
- Açık: LOD1 (uzaktan park hâli için) · Unity'de `BilgiAdasi/Stilize` ışığı altında bakış · prefab'da ön teker ve pedallerin `Bisiklet_Gidon` altına taşınması · binme/sürme kodu Mehmet'te.
