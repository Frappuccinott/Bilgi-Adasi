# Referans — Kasaba haritası (Town: atölye + meydan + iki köy)

Kural: `09-tema-ve-model-kurallari.md` §6. Araştırma 13.11.2026. Görseller repoya konmaz.
Script: `ArtSource/Blender/Script/harita.py` · Kaynak dosya: `ArtSource/Blender/harita.blend`
Bağlı belgeler: `CLAUDE.md` §6 (şehirde yürüme), `08-mustafa-entegrasyon.md` §1–2, `04_Icerik/05-icerik-prototip.md` (köy yerleri).

## 1. Kaynaklar ve alınanlar

| # | Kaynak | Ne alındı |
|---|---|---|
| 1 | [Herefordshire — planned villages](https://htt.herefordshire.gov.uk/herefordshires-past/the-medieval-period/villages/planned-villages) · [Encyclopedia.com — peasant communities](https://encyclopedia.com/history/news-wires-white-papers-and-books/farmers-and-peasants-building-peasant-communities) | Ortaçağ köyü: ortada meydan/yeşil alan, evler meydanın ya da ana yolun çevresinde, arkada bahçe şeridi; yollar tarlalara açılır. |
| 2 | [German History Docs — late medieval village](https://germanhistorydocs.org/en/from-the-reformations-to-the-thirty-years-war-1500-1648/functional-model-of-the-late-medieval-village) · [Woughton on the Green](https://theparkstrust.com/our-work/heritage/woughton-on-the-green-medieval-village) | İşlev halkaları: merkez (ev/işlik) → bahçe → tarla → mera/orman. Köy kenarı tarlayla biter. |
| 3 | [Herefordshire — why villages](https://htt.herefordshire.gov.uk/herefordshires-past/the-medieval-period/villages/why-did-some-people-live-in-villages/) | Su: köy dereye yakın kurulur → dere bir köyün yanından geçer, köprü gerekir (Duvarcı Halil'in "taş köprü" görevi). |
| 4 | [Bursa — Cumalıkızık](https://bursa.com.tr/en/mekan/cumalikizik-village-103) | Köy girişinde **ortasında yaşlı çınar olan meydan**; dar sokakların birleştiği küçük meydanlarda çeşme. → Meydan: çınar + çeşme. |
| 5 | [Turkish Culture — fountains](https://turkishculture.org/architecture/fountains-30.htm) · [Ekinci — Ottoman water culture](https://www.ekrembugraekinci.com/article/?ID=759) · [Kültür Envanteri — Ladik meydan çeşmesi](https://kulturenvanteri.com/en/yer/ladik-meydan-cesmesi-sarayonu/) | Meydan çeşmesi: taş cephe, musluk + asılı tas, önünde hayvanlar için yalak. Muhtar Yusuf'un çeşmesi böyle (önce kuru, sonra akan). |
| 6 | [Korgun çeşmeleri (ICONARP)](https://iconarp.ktun.edu.tr/index.php/iconarp/article/view/1312) | Köy çeşmeleri yerel taştan, süssüz, sivri/yuvarlak kemerli niş → atölye kemeriyle aynı dil. |
| 7 | [Unity — Level design basics](https://unity.com/blog/level-design-basics) · [80.lv — Blockmesh tips (David Shaver)](https://80.lv/articles/blockmesh-tips-from-david-shaver/) | Önce blokaj; **her alanın uzaktan görünen bir simgesi (weenie)**; yol çizgisi oyuncuyu taşır. |
| 8 | [Game Developer — leading lines (Ethan Carter)](https://gamedeveloper.com/design/guiding-the-player-leading-lines-in-quot-the-vanishing-of-ethan-carter-quot-) | Yol, çit, dere gibi doğal çizgiler yönlendirir; çizgi yalnız seçim noktasında işe yarar → meydan = tek seçim noktası (doğu/batı/atölye). |
| 9 | [PlayStation Blog — A Short Hike](https://blog.playstation.com/2021/08/05/crafting-a-tiny-open-world-a-look-behind-the-scenes-at-the-creation-of-a-short-hike/) · [Game Developer — Road to IGF: A Short Hike](https://www.gamedeveloper.com/disciplines/road-to-the-igf-adam-robinson-yu-s-i-a-short-hike-i-) | Küçük dünya büyük hissettirir: ilginç simgeler + tabelalar; alan kolay ezberlenir. |
| 10 | [ECF — longitudinal gradients](https://www.ecf.com/en/news/new-ecf-cycle-infrastructure-factsheet-longitudinal-gradients-/) · [TxDOT — shared use paths](https://www.txdot.gov/manuals/des/rdw/chapter-19-pedestrian-facilities/19-3-linear-pedestrian-facilities/19-3-3-shared-use-paths.html) | Bisiklet yolu eğimi ≤ %5 (kısa parçada %8); dikte yol genişler. → Yollar ≤ %5, köprü rampası ≤ %8, yol 3–4 m. |
| 11 | `ArtSource/Referans/atolye.md` §B (Tiny Glade, Townscaper, Sable, Ghibli köyü) | Çatı ritmi, içerik adaları, nefes alanı; aynı stil. |
| 12 | `BilgiAdasi_Docs/01_Tasarim/referans/tema-referans.webp` | Vadi: yeşil taban, iki yanda yükselen tepeler, uzakta mavi dağ ve pus. |

## 2. Yerleşim kararı

```
                 K (kuzey: tepeler, uzak dağ silueti = kilitli şehirler)
        [Güvercin kulesi]            Atölye (0, 12)
   Tepkime Türleri köyü  ── yol ──  MEYDAN (0, -8) ── yol ── köprü ~ dere ~ Denkleştirme köyü ── tarlalar
      (-46, -2)                    çınar + çeşme              (x 32)        (52, -6)            (66, -26)
                                         |
                          G (güney: pus = kilitli "Kimya Bilimi" ilçesi)
```

| Alan | Yer (m, X doğu / Y kuzey) | Simge (weenie) | Görev bağı |
|---|---|---|---|
| Atölye | (0, 12), kapı güneye | Baca dumanı, turuncu çatı | Bütün görevler burada çözülür |
| Meydan | (0, −8), Ø ~22 m taş döşeme | Yaşlı **çınar** | Muhtar Yusuf: meydan çeşmesi (D-1) |
| Denkleştirme köyü | doğu, (52, −6) | Fırın bacası, fener dizisi | Çarşı fenerleri (D-2), fırın (D-3), taş köprü (D-4), tarlalar (D-5) |
| Tepkime Türleri köyü | batı, (−46, −2) | **Güvercin kulesi** (ulak buradan uçar) | Lonca binası (sınav, T-5), defter/pano/mektuplar (T-1…4) |
| Kilitli: Hesaplamalar köyü | güneydoğu yol ağzı | Kapalı kapı + pus | "Devam edecek" |
| Kilitli: Tepkimeler köyü | kuzeybatı yol ağzı | Kapalı kapı + pus | — |
| Kilitli ilçe/şehirler | güney pusu, kuzeyde uzak dağ siluetleri | Açık mavi pus (09 §3) | — |

- Uçtan uca yol: batı köy kenarı (−58) → doğu tarlalar (+75) ≈ **133 m** (yürüyüş ~35 sn, bisiklet ~20 sn) — `08` §1 sınırı içinde.
- Dere kuzeyde vadi basamağından çağlayanla bir havuza düşer, vadiyi güneye geçer, güneydeki sazlık gölette biter (13.11 denetimi: önceden iki ucu tepeye tırmanıyordu). Doğu yolunu x ≈ 32'de keser → köprü tek geçiş. Köprü "önce" hâlinde yıkık taş köprünün yanında geçici tahta köprü (geçilebilir), "sonra" hâlinde onarılmış taş köprü.
- Sınırlar: kuzeyde ve iki yanda tepeler (> 45° yamaç, yürünmez), güneyde pus + çit. Görünmez duvar son çare.
- Zemin: oyun alanı neredeyse düz (yol eğimi ≤ %5), kenarlara doğru tepeler yükselir (vadi).

## 3. Teknik

- Arazi tek mesh (`SM_Arazi`), 2 m ızgara, köşe rengiyle çimen lekeleri, toprak yol, dere yatağı kaya. Unity'de MeshCollider.
- Su ayrı mesh, `M_Su`. Meydan döşemesi ve köprüler ayrı sabit mesh.
- Köyler `Neighborhood_*` kökü + `Before` / `After` / `Parts/Part_1…5` boş objeleriyle (08 §2) dışa aktarılır; içerik sonraki adımlarda.
- Atölye bu dosyada yalnız ayak izi işaretidir (`_Atolye_Isaret`, dışa aktarılmaz); asıl atölye Unity'de `Atolye_Sanat` ile (0, 12)'ye konur.

## 4. Durum (13.11, 1. geçiş)

Yapıldı (`harita.py`, `harita_doga.py`, `harita.blend`):
- Arazi 18,5 bin üçgen (2,5 m ızgara): düz vadi tabanı, kenarda yürünmez kaya basamağı + tombul tepeler, dere yatağı, atölye ve meydan düzlükleri.
- Yollar (7 şerit, 1,7 bin), meydan döşemesi (408), dere suyu (`M_Su`, 486).
- Doğa: 5 ağaç (3 yeşil, 2 altın vurgu), 3 çalı (LOD0/LOD1), 4 kaya. 386 yerleşim noktası (`Yer_*`).
- Köy kökleri `Koy_Denk` / `Koy_Tepk` (+ `_Once` / `_Sonra`), `_PlayerSpawn_Oneri` (atölye kapısı önü).
- FBX: `Assets/_Project/Art/Harita/Harita.fbx`, `Harita_Doga.fbx`, `Harita_Yerler.fbx`.

Sıradaki:
- Köprü (önce: yıkık taş + geçici tahta; sonra: onarılmış taş), meydan çınarı + çeşme (önce kuru / sonra akan).
- Köy yapıları: Denkleştirme (fırın, fener dizisi, evler, tarla), Tepkime Türleri (lonca binası, **güvercin kulesi**, evler).
- Kilitli yol ağızları: kapı + açık mavi pus; uzak dağ siluetleri (kilitli şehirler).
- Unity: `Yer_*` → prefab + LODGroup yerleştiren editör aracı (`Art/Editor/`), MeshCollider, ışık altında bakış.
- Bütçe: yakın ağaçlar LOD0'da ~640 üçgen; 358 ağacın hepsi LOD0'da 230 bin eder → LOD1 mesafesi kısa tutulmalı (ağaçların çoğu tepelerde, uzakta).

## 5. Durum (13.11, 2. geçiş: köy yapıları — ayrıntı `yapilar.md`)

- `harita_yapi.py`: 39 obje, 29,1 bin üçgen. Meydan: taş levha döşeme (şaşırtmalı), çeşme (Denk P1), çınar.
- Denkleştirme: 5 ev + fırın (kubbe, baca ağız üstünde), 5 fener direği, taş köprü (P4) + yıkık köprü (Before) + geçici tahta köprü ve patikaları, 3 tarla (P5 ekin), bayrak dizileri (After).
- Tepkime Türleri: 4 ev, lonca binası (revaklı alt kat + çan kulesi), güvercin kulesi, ilan panosu; P1 güvercin · P2 flama · P3 saksı · P4 ilan · P5 sancak; kepenk kapalı (Before) / açık + bayraklar (After).
- Kilitli: Hesaplamalar ve Tepkimeler yol ağızlarında kapı + örme çit + pus (`M_Bulut`), `LockedZone_*` boş objesi (ölçek = bölge boyu).
- Hepsi `Harita.fbx` içinde, `Koy_Denk` / `Koy_Tepk` altında (tek kademe). Önekler → Before/After/Part_n (`yapilar.md` §3).

Sıradaki: Unity yerleştirme aracı (Yer_* → prefab + LODGroup; önekleri Neighborhood Before/After/Parts altına dizme; MeshCollider), Unity'de ışık altında bakış.

## 6. Durum (13.11, 3. geçiş: yön bulma + kilitli şehir yolları — ayrıntı `yon_bulma.md`)

- `harita_yon.py`: 24 obje, 27,2 bin üçgen. Tabelalar (7), köy giriş kemerleri (2), mil taşları, köy zeminleri, yol ağaçları, meydanda harita kabartması.
- Yeni yollar: `Yol_Matematik` (kuzey geçidi), `Yol_Turkce` (güney geçidi); uçlarında kilitli şehir kapısı + pus + uzak siluet, `LockedZone_*`.
- Gezinti: yel değirmeni (+ `Degirmen_Kanat`), saman yığınları, kuyu, banklar, bisiklet park yerleri.
- Vadi tabanındaki rastgele ağaçlar kaldırıldı (orman yalnız vadi kenarında; taban açık ve düzenli).
- Toplam (yerleşim noktaları hariç): arazi+yol+meydan+su 23 bin, köy yapıları 29,1 bin, yön/şehir 27,2 bin üçgen.
- Atölye kapısı: Unity'de `Kapi_Rig` prefab kökü menteşede (SanatIceAktarma.cs), kanatta BoxCollider.

## 7. Durum (13.11, 4. geçiş: detay — ayrıntı `detay.md`)

- `harita_detay.py`: 4 pazar tezgahı (manav, ekmekçi, baharatçı, kâğıtçı), ev bahçeleri, taş duvarlar, araba, çamaşır ipleri, arı kovanları, odun yığınları, uzak dağlar + bulutlar (40 bin üçgen; bulut + dağ 6,9 bin).
- Yeni türler (`Harita_Doga.fbx`): çimen ×3, çiçek ×3, çakıl, saz, kavak, çam, meyve, kaya kütlesi ×2. Toplam 2.314 `Yer_*` noktası.
- Dışa aktarım artık `export_harita_tum()` (Harita.fbx'e detay koleksiyonu da girer).
- Önemli: `Yer_*` noktaları Unity'de prefab ile değiştirilmeden ağaç, çimen, çiçek görünmez → sıradaki iş Unity yerleştirme aracı.
