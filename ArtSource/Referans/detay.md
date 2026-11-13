# Referans — Detay geçişi (zemin örtüsü, ağaç çeşitliliği, uzak manzara, köy hayatı, pazar tezgahları)

Kural: `09-tema-ve-model-kurallari.md` §6. Araştırma 13.11.2026. Script: `ArtSource/Blender/Script/harita_detay.py` (önce `harita_yon.py`).
İstek (Mustafa, 13.11): "Harita hâlâ istediğim kadar elit ve detaylı değil; özellikle pazar tezgahlarını detaylandır."

## 1. Kaynaklar ve alınanlar

| # | Kaynak | Ne alındı |
|---|---|---|
| 1 | [80.lv — Meadows: stylized nature in UE4](https://80.lv/articles/meadows-creating-stylized-nature-in-ue4/) · [80.lv — Provençal environment](https://80.lv/articles/designing-a-stylized-provencal-environment-in-zbrush-substance-ue4) · [80.lv — Howl's Moving Castle ortamı](https://80.lv/articles/creating-a-painterly-like-and-stylized-environment-inspired-by-howl-s-moving-castle) | Zemin örtüsü katmanlı: düz çimen / alçak çiçekli / yüksek çiçekli; birkaç çimen türü rastgele karışır; yoğunluk yol ve yapı çevresinde artar. → 3 çimen + 3 çiçek öbeği, yol kenarı yoğun, vadi tabanı seyrek. |
| 2 | [80.lv — Ghibli adası](https://80.lv/articles/creating-a-ghibli-inspired-island-in-blender-ue5-substance-3d) · [80.lv — Okino's Residence](https://80.lv/articles/okino-s-residence-creating-a-studio-ghibli-inspired-3d-house) | Ufukta katmanlı mavi dağlar + iri bulutlar sahneyi "bitmiş" gösterir. → Uzak dağ halkası + bulut kümeleri. |
| 3 | [Stirling — Turkish village (Kent ethnographics)](https://era.anthropology.ac.uk/Era_Resources/Era/Stirling/Pages/Page_20.html) · [Stirling C2](https://era.anthropology.ac.uk/Era_Resources/Era/Stirling/StirlingC2.html) | Orta Anadolu köyünde **dere boyunca söğüt ve dik kavaklar "yeşil vaha"** oluşturur; vadi tabanında sulanan tarlalar. → Dere kıyısında kavak dizisi + saz; yol boyunda kavak sırası. |
| 4 | [CGTrader — medieval market stall (açıklama)](https://www.cgtrader.com/3d-models/exterior/other/medieval-market-stall-game-ready-3d-asset) · [Project Kirtrite — wayside stall](https://projectkirtrite.com/shop/market-stall) · [Victrix — market stall](https://www.victrixlimited.com/products/market-stall-medieval-dark-ages) | Tezgah dili: ahşap çatkı, eğik tente, yükseltilmiş tezgah; meyve kasası, ekmek sepeti, asılı fener, fıçı/çuval/sepet. **Yalnız parça listesi; model kullanılmadı.** |
| 5 | `yapilar.md` (köy fırını, lonca), `yon_bulma.md` (kimlik rengi/simge) | Tezgah levhasında köy simgesi; Denkleştirme tezgahlarında terazi (köyün konusu: denge). |

## 2. Pazar tezgahları (4 esnaf, her biri ayrı silüet)

| Tezgah | Yer | Ne var |
|---|---|---|
| Manav (Denk) | yol kuzeyi, fırının batısı | 3 örgü sepet (domates/ayva/yeşillik), **terazi**, önde 3 çıtalı kasa (biri üst üste), kirişten sarkan sarımsak ve kuru biber dizileri, arka rafta küpler |
| Ekmekçi (Denk) | fırının önü | sepetlerde somunlar, rafta halka ekmekler, un çuvalları, asılı dizi |
| Baharatçı (Denk) | yol güneyi | 6 baharat kâsesi (renk renk tepecik), **terazi**, ağzı açık baharat çuvalları, asılı kuru ot demetleri, iki katlı rafta küp ve bakır kaplar |
| Kâğıtçı (Tepk) | lonca önü | mühürlü tomarlar, kitap yığını, mürekkep hokkası + kamış kalem, ipte asılı mühürlü mektuplar, kâğıt kasası (masa görevleri mektupla gelir) |

Ortak: oymalı dikme başları, kalın tezgah, önünde köy renginde dişli örtü, sarkık çizgili tente (Denk: turuncu-sıva, Tepk: mavi-sıva) + saçak dişleri, tepede köy simgeli levha, köşede fener.

## 3. Diğer eklenenler

- Zemin: 3 çimen, 3 çiçek (altın/beyaz · turuncu/altın · mavi/beyaz), çakıl, saz (normaller yukarı, 09 §3). ~2.000 yerleşim noktası.
- Ağaç: kavak (LOD), çam (tepelerde yüksekteki ağaçların yarısı), meyve bahçesi 5×4 (turuncu meyveli), iri kaya kütleleri.
- Köy hayatı: 5 ev arkası sebze bahçesi, kuru taş duvarlar (yol kenarı, tarla, bahçe), çuval yüklü araba, iki çamaşır ipi, saman arı kovanları, odun yığınları.
- Uzak manzara: dağ halkası (r 330–470 m, pusa kayan mavi), ızgara dışı zemin yakası, bulut kümeleri (`M_Bulut`). Unity'de bunlar gölge düşürmez.
