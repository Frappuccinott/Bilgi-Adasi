# Lisanslar ve Yapay Zekâ Beyanı

Kayıtsız hiçbir hazır dosya projeye girmez. Bu tablo oyun içi **Emeği Geçenler** ekranına ve sunumdaki lisans slaytına aktarılır.

## Hazır varlıklar

| Varlık | Tür | Kaynak / yazar | Bağlantı | Lisans | Değişiklik | Ekleyen |
|---|---|---|---|---|---|---|
| Unity | Motor | Unity Technologies | — | Unity Personal | — | — |
| TextMeshPro | Paket | Unity | — | Unity paketi | — | — |
| Universal Render Pipeline | Paket | Unity | — | Unity paketi | — | — |
| Input System | Paket | Unity | — | Unity paketi | — | — |
| Cinemachine 3 | Paket | Unity | — | Unity paketi | — | Mehmet |
| Liberation Sans (TMP Essentials) | Font | Red Hat / Unity TMP | — | SIL OFL 1.1 | — | Mehmet |
| MCP for Unity v10.3.0 (`com.coplaydev.unity-mcp`) | Editör aracı (oyuna girmez) | CoplayDev / Aura | https://github.com/CoplayDev/unity-mcp | MIT | — | Mustafa |
| | | | | | | |

**Kurallar**
- Öncelik **CC0**. CC-BY ise yazar adı Emeği Geçenler'e yazılır.
- Freesound, Poly Pizza, Sketchfab gibi sitelerde lisans **dosya dosya** farklıdır; her dosyanın lisans sayfasının ekran görüntüsü `Docs/license-proof/` içine konur.
- "Kişisel kullanım" ya da lisansı yazılmayan şey kullanılmaz. Oyun/film/dizi/anime görüntü ve sesi kullanılmaz.
- Font: SIL Open Font License (ör. Google Fonts); Türkçe karakter desteği kontrol edilir.

## Yapay zekâ kullanım beyanı

| Araç | Ne için | Çıktı nerede | Kontrol eden |
|---|---|---|---|
| Claude | Senaryo güncellemesi, geliştirme dokümanları, kod yardımı | `Docs/`, ilgili scriptler | |
| Claude | Çekirdek sistemler (GameEvents, SaveSystem, SceneFlow, GameManager), kimya hesabı (AtomMath, FormulaFormatter) + birim testleri, girdi asset'i, editör kurulum aracı | `Assets/_Project/Scripts/Core`, `Chemistry`, `Editor`, `Tests` | Mehmet |
| Claude | Oyuncu (PlayerMotor, PlayerLook, HeadBob, PlayerInteractor, PlayerFootsteps), istasyon modu (StationBase), GameInput, ViewLocator, sahne bağlantı noktaları, Faz 2 editör kurulum aracı | `Assets/_Project/Scripts/Player`, `Stations`, `City`, `Core` | Mehmet |
| ChatGPT | Harita konsept görseli | Referans | |
| Claude | Simya tezgâhı (imbik, su banyosu, havan, körük), 8 düzenek + 39 eşya iskeleti ve 155 animasyon klibi (Blender scriptleri), Unity sanat içe aktarma aracı (`Art/Editor/SanatIceAktarma.cs`) | `ArtSource/Blender/Script/anim_*.py`, `simya_tezgahi.py`, `Assets/_Project/Art/` | Mustafa |
| Claude | Atölye kabuğu, iç düzeni, eşyalar ve 39 taşınabilir eşya için Blender scriptleri (model üretimi elle yazılmış Python ile; hazır mesh/üretici kullanılmadı), envanter ikonları, araştırma raporları | `ArtSource/Blender/Script/`, `ArtSource/Referans/`, `Assets/_Project/UI/Ikonlar/` | Mustafa |
| Claude | Kasaba haritası: arazi, yollar, dere, doğa (ağaç/çalı/kaya), köy yapıları (evler, fırın, köprü, güvercin kulesi, lonca, çeşme, kilitli kapılar), yön bulma (tabelalar, kemerler, mil taşları, harita masası), kilitli şehir kapıları ve siluetleri Blender scriptleri + araştırma | `ArtSource/Blender/Script/harita*.py`, `ArtSource/Referans/harita.md`, `yapilar.md`, `yon_bulma.md`, `Assets/_Project/Art/Harita/` | Mustafa |
| Claude | Simya traktörü + 6 alet, Bereket Tarlası (24 parsel × 6 durum), Ormancı Korusu modelleri (Blender scripti) ve seviye seviye görev planı (Keşif Defteri, Hata Dedektifi, Ustanın Sorgusu, Atom Görüşü katmanları; Mat/Biyo/Fizik saha soruları) | `ArtSource/Blender/Script/harita_tarla.py`, `BilgiAdasi_Docs/02_Mekanik/04-gorev-plani.md`, `Assets/_Project/Art/Traktor/` | Mustafa |
| Claude | Bisiklet (velosiped) modeli: referans araştırması + Blender scripti (elle yazılmış Python; hazır mesh/üretici kullanılmadı) | `ArtSource/Blender/Script/bisiklet.py`, `ArtSource/Referans/bisiklet.md`, `Assets/_Project/Art/Bisiklet/` | Mustafa |
| Claude | Tema ve model kuralları dokümanının uyarlanması (ekibin kendi Simya notlarından), repo kuralları | `BilgiAdasi_Docs/01_Tasarim/09-…`, `03_Teknik/08-…` §6 | Mustafa |
| | | | |

Kimya içeriği AI çıktısı olsa bile ders kitabıyla ekipçe kontrol edilir.
