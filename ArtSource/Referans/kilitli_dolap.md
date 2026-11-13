# Kilitli dolap — referans notu

Model: `ArtSource/Blender/Script/kilitli_dolap.py` → `Assets/_Project/Art/Esya/Kilitli_Dolap.fbx`
Hikâye: cıva, kezzap, kral suyu ve zırnık pazarda satılmaz; yalnızca atölyede, kilitli dolapta durur ("Usta izni olmadan dokunulmaz", `04_Icerik/10-gorev-pazar-npc.md`). Oyuncu dolaba E ile bakınca kapaklar kilide takılıp sarsılır, güvenlik notu çıkar (`LockedCabinet.cs`).

> Not (14.11, gece): Teslim saatine sıkıştığımız için referanslar bilinen örneklerden **tarif olarak** yazıldı; bağlantılar henüz eklenmedi. 09 §6'ya göre teslimden sonra her satırın bağlantısı eklenmeli.

| # | Kaynak (tür) | Ne alındı |
|---|---|---|
| 1 | Ortaçağ kilise "aumbry"si (duvar dolabı, demir kayışlı meşe kapak) | Kalın çerçeveli kapak, uçları sivri uzun menteşe kayışları |
| 2 | 17.–18. yy eczane zehir dolapları ("Gift" / poison cabinet, Alman eczane müzeleri) | Tehlikeli madde = ayrı, kilitli dolap fikri; iki kanat ortada asma kilitle buluşur |
| 3 | Ortaçağ asma kilidi (dövme demir, ağır gövde, kalın halka) | Kilit gövdesi iri ve sade; siluet uzaktan "kilit" okunur |
| 4 | Manastır kütüphanesi parmaklıklı kitap dolabı | Üst yarı parmaklık: içi görünür ama alınamaz (öğretici: "orada ama dokunma") |
| 5 | Stilize low-poly atölye/iç mekân dioramaları (çevre sanatçısı portfolyoları) | Taşan taç tahtası, kısa kalın ayak, hafif eğri yan pano; ahşapta alttan üste koyu → açık |
| 6 | Projenin kendi rafı (`atolye_esya.py` `shelf_bm`) | Aynı ahşap tonu, arka dikey tahta kaplama (şişeler koyu zeminde okunur) |
| 7 | Tema referansı (`01_Tasarim/referans/tema-referans.webp`) | Sıcak ahşap + soğuk demir karşıtlığı; vurgu (altın/bakır) yalnız kilitte |

Kontrol: tek renkte siluet okunuyor (taç + iki kanat + parmaklık + kilit). Üçgen 1.740 (büyük eşya bütçesi 1.500'ün biraz üstü; parmaklık çubukları ve şişeler okunurluk için tutuldu). Hareketli parçalar ayrı: `Dolap_Kapak_Sol/Sag` (pivot menteşe kenarı, taban), `Dolap_Kilit` (pivot asma noktası).
