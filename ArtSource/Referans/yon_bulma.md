# Referans — Yön bulma, kilitli şehir yolları, gezinti noktaları

Kural: `09-tema-ve-model-kurallari.md` §6. Araştırma 13.11.2026. Script: `ArtSource/Blender/Script/harita_yon.py` (önce `harita_yapi.py`).
Sorun (Mustafa, 13.11): "İnsanlar ne nerede bilmiyor, dağınık." İstek: Matematik ve Türkçe tabelaları, oralara bisikletle gidilebilsin, açık haritada sıkılmasınlar.

## 1. Kaynaklar ve alınanlar

| # | Kaynak | Ne alındı |
|---|---|---|
| 1 | [Game Developer — Upton: landscaping game worlds (Lynch)](https://gamedeveloper.com/game-platforms/post-gdc-i-rainbow-six-i-s-upton-talks-landscaping-game-worlds) | Lynch'in 5 öğesi (yol, kenar, bölge, düğüm, simge) seviye dili; "hiçbir yere çıkmayan yol" = düğüm eksikliği. → Her yol bir düğümde (tabela) başlar, bir simgede (kapı/kemer) biter. |
| 2 | [Wellesley — The Image of the Open World Game](https://cs.wellesley.edu/~pmwh/mvmap/papers/open_world_elements/open_world_elements.html) | Açık dünyada oyuncu en çok **simge ve kenar** ile yön bulur; sokak adı yok → yol tek başına tarif olmaz. → Her köyün/şehrin uzaktan görünen simgesi + kimlik rengi. |
| 3 | [Game Developer — Realism and legibility in open world level design](https://gamedeveloper.com/design/realism-and-legibility-in-open-world-level-design) | Simge düğüme önem verir, düğüm simgeye anlam verir. → Meydan (düğüm) + çınar/harita masası; köy girişi (kenar) + kemer. |
| 4 | [Skeleton Code Machine — vocabulary for maps](https://www.skeletoncodemachine.com/p/maps-as-data) | Bölge sınırı belirsiz olabilir → köy zemini (sıkıştırılmış toprak lekesi) + giriş kemeri bölgeyi okutur. |
| 5 | [TCD — color & breadcrumbs dissertation](https://publications.scss.tcd.ie/theses/diss/2020/TCD-SCSS-DISSERTATION-2020-021.pdf) · [Wayfinding — Wikipedia](https://Www.wikipedia.org/wiki/Wayfinding_(urban_or_indoor)) | Renk kodu ve kırıntı izi; tabelalar küme halinde, hiyerarşili. → Kimlik rengi her yerde aynı (tabela ucu, kemer levhası, mil taşı tepesi, harita masası bayrağı). |
| 6 | [Game Developer — avoiding excessive yellow paint (Control)](https://www.gamedeveloper.com/design/control-resonant-director-lays-out-case-for-avoiding-excessive-yellow-paint-) | Yönlendirme dünyanın parçası olmalı, boya gibi yapıştırılmamalı. → Renk yalnız tabela, flama, mil taşı tepesi gibi dünyada anlamı olan yüzeylerde. |
| 7 | [Country Life — fingerposts](https://www.countrylife.co.uk/out-and-about/in-focus-fingerposts-and-what-they-mean-235335) · [Wikipedia — Fingerpost](https://wikipedia.com/wiki/Fingerpost) | Kavşak direği + ok uçlu kollar; Saksonya posta mil taşları. → Ok biçimli kollu tabela + yol boyu mil taşları. |
| 8 | `harita.md` (A Short Hike: simge + tabela; Unity level design: weenie) | Küçük dünya, kolay ezberlenen simgeler. |

## 2. Sistem

| Öğe (Lynch) | Ne | Nerede |
|---|---|---|
| **Bölge rengi + simge** | Denkleştirme altın + terazi · Tepkime Türleri gök mavisi + güvercin · Atölye bakır + şişe · Tarla yeşil + başak · **Matematik** dağ mavisi + üçgen/çember · **Türkçe** kiremit + açık kitap/kamış · Kilitli gri + kilit | Her tabela kolu, kemer, mil taşı, harita masası |
| **Düğüm** (yön tabelası) | 7 direk; meydanda 5 kollu (Atölye, Matematik, Denkleştirme, Tepkime Türleri, Türkçe) | Meydan, iki şehir yolu ayrımı, iki geçit ortası, iki köy içi kavşak |
| **Kenar** (giriş kemeri) | Köy renginde levha + simge, flamalar | Denkleştirme (köprüden sonra), Tepkime Türleri girişi |
| **Yol** (kırıntı izi) | Mil taşları: tepesi gidilen yerin renginde, simgeli yüz yola bakar; yol ağaçları | 4 ana yol boyunca, ~11 m arayla |
| **Bölge** | Köy zemini (toprak lekesi) | İki köy |
| **Simge** | Meydanda harita kabartması (kuzey = dünya kuzeyi, köy bayrakları, "buradasın" iğnesi), güvercin kulesi, yel değirmeni, şehir kapıları, uzak şehir siluetleri | — |

## 3. Kilitli şehirlere bisiklet yolu

- Matematik (kuzey) ve Türkçe (güney) şehirleri `CLAUDE.md` §1'e göre **kilitli**: yol tepeleri boğaz geçidiyle yarar (yanlar dik kaya, görünmez duvar yok), uçta kapalı büyük **şehir kapısı** (iki kule, madalyonda simge, kilit), arkasında pus ve uzakta şehir silueti (Matematik: silindir/küp/piramit/küre kuleler; Türkçe: kitap yığını kuleler, kalem uçlu minare, kubbeler).
- Yol uzunluğu meydandan kapıya ~95 m (bisiklette ~15 sn): kısa bir gezinti, sonunda "henüz kilitli" ödülü (manzara + kapı).
- `LockedZone_Matematik` / `LockedZone_Turkce` boş objeleri kapının önünde (Mehmet'in `LockedZone` bileşeni için yer).

## 4. Gezinti noktaları (sıkılmasın)

Yel değirmeni (tarla simgesi; kanatlar ayrı `Degirmen_Kanat`, pivot göbekte), saman yığınları, Tepkime köyünde kuyu, meydanda 3 bank, 3 bisiklet park yeri (atölye kapısı, iki köy girişi), meydanda harita masası.

## 5. Yazı

Yazı mesh değildir (09 §7). Her tabela kolunda, kemerde, şehir kapısında ve harita masasında `Tabela_Yazi_*` boş objesi var (ör. `Tabela_Yazi_1_Matematik_Sehri`). Font seçilince (09 §9) Unity'de TMP buraya bağlanır; o zamana kadar renk + simge okutur.
