# Atölye önü pazarı: referans notu

Modeller:
- `ArtSource/Blender/Script/atolye_pazar.py` → `Assets/_Project/Art/Esya/Atolye_Pazar.fbx` (tezgâh, tente, bank, çapalar)
- `ArtSource/Blender/Script/npc_musteri.py` → `Assets/_Project/Art/NPC/NPC_Musteri{Zehra,Demir,Yusuf}.fbx`

Hikâye (karar 14.11): sakinler siparişi artık atölye kapısına değil, atölyenin önündeki tezgâha gelip verir. Sipariş alınınca bankta beklerler, ürün hazır olunca tezgâha dönüp alırlar. Ayrıca sıradan alıcılar oyuncunun ürettiği ürünleri buradan satın alır (`CustomerQueue`, `ShopCounter`).

> Not (14.11, gece): Teslime az kaldığı için referanslar, `kilitli_dolap.md`'de olduğu gibi bilinen örneklerden **tarif olarak** yazıldı. 09 §6'ya göre teslimden sonra her satıra bağlantı eklenmeli.

## A. Tezgâh + tente

| # | Kaynak (tür) | Ne alındı |
|---|---|---|
| 1 | Ortaçağ pazar tezgâhı çizimleri (el yazması minyatürlerinde dört direkli, bez örtülü satış sergileri) | Dört direk, eğik bez çatı, önde satıcı ile alıcıyı ayıran kalın tabla |
| 2 | Ortaçağ dükkân önü kepenkleri ("shop board"; gündüz aşağı açılıp tezgâh olan kapak) | Tezgâhın atölyenin bir uzantısı olması, satıcının tabla arkasında durması |
| 3 | Osmanlı çarşısı / arasta dükkânları (önde sergi basamağı, malın alıcıya dönük durması) | İki kademeli sergi basamağı; ürün kavanozlarının sokağa dönük dizilmesi |
| 4 | Çizgili pazar tenteleri (Avrupa sokak pazarları, dilli tente kenarı) | Şeritli bez, ucunda yuvarlak kesimli sarkan diller: uzaktan "pazar" diye okunan silüet |
| 5 | Stilize low-poly pazar tezgâhı çalışmaları (çevre sanatçısı portfolyoları) | Kalın direk, taban taşı, köşe payandası; ahşapta alttan üste koyudan açığa geçiş |
| 6 | Projenin kendi tezgâh ve rafı (`simya_tezgahi.py`, `kilitli_dolap.py`) | Aynı ahşap tonu ve pah ölçüsü, pirinç vurgu yalnızca akçe kutusunda |
| 7 | Tema referansı (`01_Tasarim/referans/tema-referans.webp`) | Çatı turuncusu ile sıcak kâğıt tonunun yan yana kullanımı, mavi taban taşı gölgesi |

Kontrol: tek renkte silüet okunuyor (dört direk, eğik tente, dilli kenar, alın tabelası, tabla). Üçgen: tezgâh 1.232, tente+direkler 1.328, bank 396 (büyük eşya bütçesi parça başına 1.500). Yazı mesh değil: alın tahtasında kâğıt renkli levha var, yazı `Tabela_Yazi_Pazar` çapasına TMP ile konur.

## B. Müşteriler (sipariş veren sakinler)

| # | Kaynak (tür) | Ne alındı |
|---|---|---|
| 1 | Ortaçağ meslek tasvirleri (fırıncı, fenerci/kandilci, köy kâhyası) | Meslek işareti silüette: fırıncıda boydan önlük ve bez başlık, fenercide deri önlük ve yağdanlık, muhtarda uzun palto ve sakal |
| 2 | Projenin `npc.py` karakterleri | Aynı iskelet, oran, yüz yapısı ve animasyonlar; yalnızca kıyafet, renk ve aksesuar farklı |
| 3 | `04_Icerik/10-gorev-pazar-npc.md` §1 | Kimin hangi siparişi verdiği: Zehra D-3, Demir D-2, Yusuf D-1 |

Üçgen: 2.576 / 2.772 / 2.644 (insan NPC bütçesi 4.000). Renkler yalnızca paletten (kil, altın, dağ, gök, yaprak, ahşap, sıva).
