# Referans — Köy yapıları (evler, fırın, fenerler, çeşme, köprü, tarla, güvercin kulesi, lonca, kilitli kapı)

Kural: `09-tema-ve-model-kurallari.md` §6. Araştırma 13.11.2026. Görseller repoya konmaz.
Script: `ArtSource/Blender/Script/harita_yapi.py` (önce `harita.py`). Yerleşim: `harita.md` §2.
Stil: atölye ile aynı dil (`atolye.md` §A — taş temel, sıva + kalın ahşap çatkı, iri sarkık turuncu çatı, sivri kemerli kapı).

## 1. Kaynaklar ve alınanlar

| # | Kaynak | Ne alındı |
|---|---|---|
| 1 | `atolye.md` §B (Tiny Glade, Townscaper, Sable, Ghibli köyü, 80.lv pub dioraması) | Gövdeye göre büyük çatı, kiremit sıralarının saçakta basamaklı siluet vermesi, sarkık mahya, kalın kiriş; detay kapı ve pencerede toplanır. |
| 2 | [Archnet — Isfahan pigeon towers](https://www.archnet.org/sites/2888) · [Amusing Planet — Pigeon towers of Iran](https://www.amusingplanet.com/2014/11/the-pigeon-towers-of-iran.html) · [AramcoWorld — Castles of the fields](https://archive.aramcoworld.com/issue/201102/castles.of.the.fields.htm) | Güvercin kulesi: silindir, içe eğik duvar, kerpiç + kireç sıva, tepede kubbe ve küçük taret; yüzlerce yuva deliği. → Kil/sıva kule, delikler köşe rengiyle bant, kubbe + taret. |
| 3 | [METU — Central Anatolian dovecotes](https://open.metu.edu.tr/bitstream/handle/11511/93178/79-90.pdf) · [ISMEO — Borj-e Kabotar](https://www.ismeo.eu/portfolio_page/borj-e-kabotar-architecture-and-anthropology-of-the-pigeon-towers-in-the-isfahan-province/) | Anadolu'da (Ürgüp, Göreme) köy kenarında güvercinlik; "mimarsız mimari". Kule köy kenarına, tepeye yakın konur. |
| 4 | [Belleten — Anatolian bridges](https://belleten.gov.tr/eng/full-text/2263/eng) · [Scotland's oldest bridges — glossary](https://scotlandsoldestbridges.co.uk/parts-of-a-bridge-and-glossary-of-old-stone-bridges.html) | Erken Anadolu köprüsü: **tek açıklık, iki uçtan rampa**, sivri kemer; kemer taşları (voussoir) halkası, korkuluk duvarı, dolgu. |
| 5 | [Wikipedia — Communal oven](https://en.wikipedia.org/wiki/Communal_oven) · [MOT — history of the bread oven](https://mot.be/en/opzoeken/bakovens/geschiedenis/geschiedenis-van-de-bakoven) · [MOT — what's what](https://www.mot.be/en/opzoeken/bakovens/wat-is-wat) | Köy fırını meydanda/yol üstünde; kubbeli fırın gövdesi + önünde yapı; **baca fırın ağzının üstünde**. Kubbe ısıyı tutar. |
| 6 | [Historic England — Tudor Merchants Hall](https://historicengland.org.uk/services-skills/education/educational-images/tudor-merchants-hall-southampton-6347) · [Thaxted Guildhall](https://prelive.historicengland.org.uk/education/schools-resources/educational-images/guildhall-town-street-thaxted-1940) · [Biberach Old Town Hall](https://www.oberschwaben-tourismus.de/en/attractions/old-and-new-city-hall-d3b407774c) · [Ledbury Market House](https://htt.herefordshire.gov.uk/herefordshires-past/the-post-medieval-period/architecture/buildings-1500-1750/public-buildings) | Lonca binası: **altı açık revaklı (taş kaide üstünde ahşap dikmeler), üstü ahşap çatkılı salon**. Pazar yerinde durur. |
| 7 | `harita.md` #4–#6 (Cumalıkızık, meydan çeşmeleri, Korgun) | Çeşme: taş cephe, sivri kemerli niş, lüle + asılı tas, önünde yalak. Meydanda yaşlı çınar. |
| 8 | `bisiklet.md` (fener/zil bakırı), tema referansı | Bakır ve ahşap vurgular; turuncu vurgu yalnız görev noktalarında. |

## 2. Görevlerle bağ (NeighborhoodView: parçalar yalnız **eklenir**, önceki hâli silmez)

| Köy | Parça | Önce (her zaman görünür ya da `Before`) | Görev bitince eklenen |
|---|---|---|---|
| Denkleştirme | Part_1 Muhtar | Meydan çeşmesi kuru, yalakta kuru yaprak | Lülede su, yalak dolu |
| | Part_2 Fenerci | Fener camları isli (koyu) | Camlar yanar (`M_Fener`) |
| | Part_3 Fırıncı | Fırın soğuk | Ağızda kor, bacada duman |
| | Part_4 Duvarcı | Yıkık taş köprü (`Before`) + yukarıda geçici tahta köprü | Onarılmış taş köprü |
| | Part_5 Çiftçi | Sürülmüş boş tarlalar | Ekin sıraları (buğday + sebze) |
| | After | — | Yol üstünde bayrak dizileri |
| Tepkime Türleri | Part_1…5 | Kule boş, lonca kepenkleri kapalı (`Before`), boş ilan panosu | 1 güvercinler · 2 lonca flamaları · 3 pencere saksıları · 4 pano dolu · 5 sancak direği |
| | After | — | Lonca kepenkleri açık, bayrak dizileri |

Not: Köprünün "önce" hâli `Before` grubunda olduğu için, onarılmış köprü (Part_4) yıkık köprüyle aynı yere gelir; yıkık parçalar onarılmışın içinde kalacak kadar küçüktür (görünmez).

## 3. Adlandırma (Unity aracı bu öneklere göre grup kurar)

`SM_<Koy>_*` sabit · `<Koy>_Once_*` → Before · `<Koy>_Sonra_*` → After · `<Koy>_P<n>_*` → Parts/Part_n. `<Koy>` = `Denk` / `Tepk` / `Meydan` (meydan çeşmesi Denk P1'e bağlı: `Denk_P1_Cesme_Su`).
