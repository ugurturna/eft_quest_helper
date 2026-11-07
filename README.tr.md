<p align="center">
  <img src="TarkovQuestTracker/Assets/app-icon.png" width="120" alt="EFT Quest Helper ikonu">
</p>

<h1 align="center">EFT Quest Helper</h1>

<p align="center">
  <b>Escape from Tarkov</b> için masaüstü görev takip ve planlama uygulaması — her zaman üstte duran bir overlay ile.<br>
  <a href="README.md">English</a> · <b>Türkçe</b> · <a href="README.ru.md">Русский</a>
</p>

<p align="center">
  <a href="https://github.com/ugurturna/eft_quest_helper/releases/latest">
    <img src="https://img.shields.io/github/v/release/ugurturna/eft_quest_helper?style=for-the-badge&label=%C4%B0ndir&color=2ea44f&logo=windows&logoColor=white" alt="Son sürümü indir">
  </a><br>
  <a href="https://github.com/ugurturna/eft_quest_helper/releases/latest">https://github.com/ugurturna/eft_quest_helper/releases/latest</a><br>
  🌐 <b>Web Sitesi:</b> <a href="https://ugurturna.github.io/eft_quest_helper/">https://ugurturna.github.io/eft_quest_helper/</a>
</p>

---

## Ban riski

> [!TIP]
> **Bilinen bir ban riski yok.** EFT Quest Helper normal, ayrı bir Windows uygulamasıdır. Oyunun belleğini **okumaz ve değiştirmez**, oyuna hiçbir şey **enjekte etmez**, oyunun dosyalarını veya loglarını **okumaz**, oyuna tuş/fare girdisi **göndermez** ve oyunun sunucularıyla **konuşmaz**. Overlay, ekranın üstünde tutulan sıradan bir penceredir (yapışkan not gibi); kısayol tuşları Windows'un standart `RegisterHotKey` API'sini kullanır.

> [!CAUTION]
> **Kullanım sorumluluğu sana aittir. Bu proje Battlestate Games ile bağlantılı değildir ve onlar tarafından onaylanmamıştır; BSG kurallarını her an değiştirebilir. BSG bir gün bu tür araçlara izin verilmediğini açıklarsa kullanmayı bırak.**

---

<p align="center">
  <img src="docs/screenshots/main-window.webp" width="780" alt="Ana pencere">
</p>

## Özellikler

- **Tüm görevler tek yerde** — ~870 görev: trader (yan) görevleri, hikaye bölümleri ve event görevleri; hedefler, gerekli itemler, ödüller, rehber ve notlarla birlikte.
- **İkonlu görev itemleri** — hangi trader'ın hangi görev için hangi itemlere ihtiyacı olduğu; item ikonları, adetler ve Found-in-Raid (FIR) işaretleriyle (bkz. [Görev itemleri](#görev-itemleri)).
- **Ön koşul zincirleri** — her görev neye ihtiyaç duyduğunu ve neyi açtığını bilir.
- **Hedef planlayıcı** — bir hedef seç (ör. *The Punisher – Part 3* ya da *Collector*), tüm zinciri sırayla, şu an yapabildiklerini ve kalanları gör.
- **Kappa / Collector yolu** — Kappa için gerçekten gereken görevler: *Collector* ve tüm ön koşul zinciri. İlerleme çubuğu, yapılabilir görevler **haritaya göre gruplu** (aynı raidde birkaçını birden yap) ve seviye bekleyen görevler.
- **Aktif görevler + overlay** — şu an yaptığın görevler ekranın sağ kenarındaki küçük takipçide görünür; hedef kutucukları, öldürme/item sayaçları ve item ikonlarıyla.
- **Bir görevi aktif yapmak zincirini tamamlar** — *Punisher Part 5*'i aktif yap, Part 1–4 otomatik tamamlandı sayılsın (tek tıkla geri alınabilir).
- **PvP ve PvE** — her mod için ayrı ilerleme; PvE'ye özel farklar (seviye, hedefler, itemler) gösterilir.
- **İngilizce ve Türkçe arama** — yazım hatalarına ve Türkçe karakterlere toleranslı (`punisher 3`, `punişır 3`, `cezalandırıcı`).
- **İki ayrı dil** — uygulama dili (menüler) ve görev dili (görev ve item adları) farklı olabilir; ör. uygulama Türkçe, görevler İngilizce. Tüm hikaye bölümleri Türkçeye çevrildi.
- **Profiller** — aynı bilgisayarda birden fazla oyuncu, herkesin kendi ilerlemesi ve ayarları.
- **Event görevleri** — aktif eventler listelenir; geçmiş eventler Ayarlar'dan gösterilebilir.
- **Hızlı** — listeler kaydırdıkça sayfa sayfa yüklenir.

### Görev itemleri

<p align="center">
  <img src="docs/screenshots/items.webp" width="780" alt="Eşyalar sekmesi: her trader'ın görev görev istediği itemler">
</p>

**Eşyalar** sekmesi "görevlerim için neleri saklamalıyım?" sorusunu cevaplar:

- Önce **trader'a**, sonra **göreve** göre gruplu; her item küçük bir kart: **ikonu**, kısa adı, **adedi** ve raid'de bulunması gerekiyorsa **FIR** işareti. Tam adı görmek için kartın üstüne gel.
- Birden fazla item kabul eden hedeflerde (ör. *herhangi 3 tıbbi item*) ilk item ve **+N** gösterilir; açılan kutuda alternatifler listelenir.
- Bir item **arat** (`Salewa`, `GPU`, `Tushonka`…), onu isteyen tüm görevleri gör.
- **Bitenleri gizle** (varsayılan açık) ve **Sadece FIR** filtreleri; üstte kaç görev ve kaç farklı item kaldığı yazar.
- Görev adına tıklayınca detay panelinde açılır.

Item ikonları **görevlerin içinde de** görünür: bulma / teslim / yerleştirme hedefleri; görev detayında, *Aktif Görevler* kartlarında ve overlay'de item'ın ikonunu gösterir. Detay paneli de görevin gerekli itemlerini kart olarak listeler. Itemler görev dilinde (İngilizce veya Türkçe) gösterilir.

### Overlay

<img src="docs/screenshots/overlay.webp" width="320" align="right" alt="Overlay">

- Sağ kenara yapışık, **sadece yukarı/aşağı hareket eder** (başlıktan sürükle).
- Ayarlanabilir **opaklık**, **boyut** (%60–200, ayrıca <kbd>Ctrl</kbd> + fare tekerleği) ve **genişlik**.
- İki görünüm: **sadece başlık** ya da **başlık + detay**.
- Item hedeflerinde **item ikonu** görünür; raid ortasında loot'u bir bakışta tanırsın.
- <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>T</kbd> — overlay'i kilitle (tıklamalar oyuna geçer) / aç.
- <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>H</kbd> — overlay'i göster / gizle.
- Oyundan odağı asla çalmaz.

> [!NOTE]
> Tarkov'u **Borderless (Çerçevesiz)** ya da **Windowed (Pencere)** modda çalıştır. Windows, tam ekran (exclusive fullscreen) bir oyunun üstüne başka pencere çizemez.

<br clear="right">

## İndir & çalıştır

**Son sürümü indir (önerilen):** [Sürümler](https://github.com/ugurturna/eft_quest_helper/releases/latest) → `TarkovQuestTracker-vX.Y.Z-win-x64.zip`

Zip'i istediğin yere çıkar ve `TarkovQuestTracker.exe`'yi çalıştır. Kurulum yok, .NET içinde geliyor. `data` klasörünü exe'nin yanında tut; görev verisi, item ikonları ve ilerlemen orada.

### Güncelleme

İlerlemen exe'nin yanındaki `data\profiles` klasöründe. Yeni sürümü ayrı bir klasöre çıkarıp `data\profiles`'ı oraya kopyala ya da eski sürümde **Ayarlar → Dışa aktar**, yenisinde **İçe aktar** kullan.

### Kaynaktan derleme

Gereksinimler: **Windows 10/11** ve **[.NET 10 SDK](https://dotnet.microsoft.com/download)**.

```powershell
git clone git@github.com:ugurturna/eft_quest_helper.git
cd eft_quest_helper
dotnet run --project TarkovQuestTracker
```

Her yere kopyalanabilen bağımsız bir klasör üretmek için:

```powershell
dotnet publish TarkovQuestTracker -c Release -r win-x64 --self-contained true -o publish
# publish\TarkovQuestTracker.exe dosyasını çalıştır
```

## İlerlemem nerede saklanıyor?

Her şey bilgisayarında saklanır, hiçbir yere yüklenmez. Exe'nin yanında `data` klasörü varsa (release zip'i, publish ya da derlenmiş uygulama) orası kullanılır:

```
data\
  app.json                      son kullanılan profil
  profiles\<ad>\settings.json   overlay, diller, kısayollar
  profiles\<ad>\progress_pvp.json
  profiles\<ad>\progress_pve.json
```

Exe'nin yanında `data` klasörü yoksa aynı dosyalar `%APPDATA%\TarkovQuestTracker\` altına yazılır. Başka bir yerde tutmak için `TARKOV_TRACKER_HOME` ortam değişkenini ayarla.

İlerleme her tıklamada kaydedilir. Yedeklemek ya da başka bilgisayara taşımak için **Ayarlar → Dışa / İçe aktar**'ı kullan.

Kısayollar `settings.json` içinden değiştirilebilir (`clickThroughHotkey`, `toggleOverlayHotkey`, ör. `"Ctrl+Alt+F9"`).

## Görev verisi

Görev verisi `data/` klasöründedir ve `tools/scraper/` içindeki scriptlerle üretilir (Python 3.12). Bu sırayla çalıştır:

| Komut | Ne yapar |
|---|---|
| `python tools/scraper/scrape_wiki.py` | Tüm görevleri [EFT Fandom wiki](https://escapefromtarkov.fandom.com/)'den indirir → `data/tasks_en.json` (`--refresh` yerel önbelleği yok sayar) |
| `python tools/scraper/fetch_tarkovdev.py` | [tarkov.dev](https://tarkov.dev/) verisini `json.tarkov.dev` üzerindeki hazır dump'lardan indirir (seviyeler, eksik ön koşullar, PvE farkları, Türkçe adlar) |
| `python tools/scraper/merge.py` | tarkov.dev verisini wiki verisine ekler, Kappa görevlerini işaretler (Collector ve ön koşul zinciri) ve kaynakların ayrıştığı yerleri `data/diff_report.md`'ye yazar |
| `python tools/scraper/build_translations.py tr` | `tools/translations/tr/` içindeki elle yazılmış çevirileri `data/tasks_tr.json`'a işler |
| `python tools/scraper/fetch_quest_items.py` | Her görevin istediği itemleri indirir → `data/quest_items.json`, ikonlarını → `data/icons/` ([Pillow](https://pypi.org/project/pillow/) gerekir: `pip install pillow`) |

`fetch_quest_items.py` dışındaki scriptler sadece standart kütüphaneyi kullanır. Her görev, her alanın hangi kaynaktan geldiğini (`sources`) saklar; wiki verisini tarkov.dev verisinden ayırt edebilirsin.

### Çeviri yapmak

Çeviriler `tools/translations/<dil>/` içinde, her görev için ayrı düz JSON dosyalarıdır ve wiki yeniden çekilse de kaybolmaz. Var olan bir dosyayı kopyala (ör. `tools/translations/tr/story/tour.json`), `name`, `objectives[].text`, `objectives[].details` ve `guide` alanlarını çevir, sonra `build_translations.py`'yi çalıştır. Script eksik ya da uyumsuz hedefleri bildirir.

Uygulama (menü) metinleri `TarkovQuestTracker/Strings/Strings.<dil>.xaml` dosyalarındadır.

## Sürüm çıkarma

Sürüm `TarkovQuestTracker/TarkovQuestTracker.csproj` içindeki `<Version>` ile belirlenir ve uygulamada başlığın yanında görünür. `v0.0.3` gibi bir tag push edilince [`.github/workflows/release.yml`](.github/workflows/release.yml) çalışır: testler, tek dosyalık bağımsız publish, zip ve zip'in eklendiği bir GitHub release. Web sitesi en yeni sürümü kendisi bulur.

```powershell
git tag v0.0.3
git push origin v0.0.3
```

## Proje yapısı

```
TarkovQuestTracker/          WPF uygulaması (.NET 10): pencereler, overlay, view model'ler, arayüz metinleri
TarkovQuestTracker.Core/     görev ve item modelleri, veri yükleme, arama, ön koşul grafiği, profiller/ilerleme
tests/                       xUnit testleri (planlayıcı, arama, profiller, gerçek veri kontrolleri)
tools/scraper/               veri scriptleri (wiki, tarkov.dev, birleştirme, çeviriler, görev itemleri)
tools/translations/          elle yazılmış çeviriler
tools/make_icon.ps1          PNG'den .ico üretir
data/                        uygulamayla gelen görev verisi, görev itemleri ve item ikonları
docs/                        web sitesi (GitHub Pages)
.github/workflows/           release workflow'u
```

Testleri `dotnet test` ile çalıştır.

## Kaynaklar & yasal

- Görev verisi: [Escape from Tarkov Wiki (Fandom)](https://escapefromtarkov.fandom.com/) — içerik [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) lisansıyla — ve [tarkov.dev](https://tarkov.dev/) (görev itemleri ve item ikonları).
- *Escape from Tarkov*, trader adları, trader portreleri ve item görselleri **Battlestate Games**'in markası/mülküdür. Burada sadece oyun içi içeriği tanımlamak için kullanılmıştır. Bu ticari olmayan bir hayran projesidir.
- Free code signing provided by [SignPath.io](https://signpath.io), certificate by [SignPath Foundation](https://signpath.org).
- [MIT Lisansı](LICENSE) altında lisanslanmıştır.
