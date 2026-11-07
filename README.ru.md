<p align="center">
  <img src="TarkovQuestTracker/Assets/app-icon.png" width="120" alt="Иконка EFT Quest Helper">
</p>

<h1 align="center">EFT Quest Helper</h1>

<p align="center">
  Десктопный трекер и планировщик заданий для <b>Escape from Tarkov</b> — с оверлеем поверх всех окон.<br>
  <a href="README.md">English</a> · <a href="README.tr.md">Türkçe</a> · <b>Русский</b>
</p>

<p align="center">
  <a href="https://github.com/ugurturna/eft_quest_helper/releases/tag/v0.0.1">
    <img src="https://img.shields.io/badge/Download-v0.0.1-2ea44f?style=for-the-badge&logo=windows&logoColor=white" alt="Скачать v0.0.1">
  </a><br>
  <a href="https://github.com/ugurturna/eft_quest_helper/releases/tag/v0.0.1">https://github.com/ugurturna/eft_quest_helper/releases/tag/v0.0.1</a><br>
  🌐 <b>Веб-сайт:</b> <a href="https://ugurturna.github.io/eft_quest_helper/">https://ugurturna.github.io/eft_quest_helper/</a>
</p>

---

## Риск бана

> [!TIP]
> **Известного риска бана нет.** EFT Quest Helper — обычное отдельное приложение Windows. Оно **не читает и не изменяет** память игры, **ничего не внедряет** в игру, **не читает** файлы и логи игры, **не отправляет** в игру нажатия клавиш или мыши и **не обращается** к серверам игры. Оверлей — это обычное окно, которое держится поверх экрана (как стикер), а горячие клавиши используют стандартный API Windows `RegisterHotKey`.

> [!CAUTION]
> **Используйте на свой страх и риск. Проект не связан с Battlestate Games и не одобрен ими; BSG может изменить свои правила в любой момент. Если BSG объявит, что подобные инструменты запрещены, прекратите им пользоваться.**

---

<p align="center">
  <img src="docs/main-window.png" width="780" alt="Главное окно">
</p>

## Возможности

- **Все задания в одном месте** — ~870 заданий: задания торговцев, сюжетные главы и ивентовые задания, с целями, нужными предметами, наградами, гайдами и заметками.
- **Цепочки требований** — у каждого задания видно, что для него нужно и что оно открывает.
- **Планировщик целей** — выберите цель (например, *The Punisher – Part 3* или *Collector*) и увидите всю цепочку по порядку, что можно сделать прямо сейчас и что осталось.
- **Путь к Kappa / Collector** — шкала прогресса, доступные задания для Kappa **сгруппированы по картам** (можно сделать несколько за один рейд), а также задания, ждущие нужного уровня.
- **Активные задания + оверлей** — задания, которые вы выполняете сейчас, отображаются в небольшом трекере у правого края экрана, с галочками целей и счётчиками убийств/предметов.
- **Активация задания закрывает его цепочку** — сделайте *Punisher Part 5* активным, и части 1–4 автоматически отметятся выполненными (отмена в один клик).
- **PvP и PvE** — отдельный прогресс для каждого режима; показываются отличия PvE (уровни, цели).
- **Поиск на английском и турецком** — устойчив к опечаткам и турецким символам.
- **Два независимых языка** — язык приложения (меню) и язык заданий (текст заданий) можно выбирать отдельно. Сейчас доступны английский и турецкий.
- **Профили** — несколько игроков на одном ПК, у каждого свой прогресс и настройки.
- **Ивентовые задания** — текущие ивенты в списке; прошедшие можно включить в настройках.
- **Быстро** — списки подгружаются постранично при прокрутке.

### Оверлей

<img src="docs/overlay.png" width="320" align="right" alt="Оверлей">

- Прикреплён к правому краю, **двигается только вверх/вниз** (перетаскивайте заголовок).
- Настраиваемые **прозрачность**, **размер** (60–200 %, также <kbd>Ctrl</kbd> + колесо мыши) и **ширина**.
- Два вида: **только названия** или **название + детали**.
- <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>T</kbd> — заблокировать оверлей (клики проходят в игру) / разблокировать.
- <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>H</kbd> — показать / скрыть оверлей.
- Никогда не забирает фокус у игры.

> [!NOTE]
> Запускайте Tarkov в режиме **Borderless (Без рамки)** или **Windowed (В окне)**. Windows не может рисовать другие окна поверх игры в эксклюзивном полноэкранном режиме.

<br clear="right">

## Скачать и запустить

**Скачать готовый релиз (рекомендуется):** [Релиз v0.0.1](https://github.com/ugurturna/eft_quest_helper/releases/tag/v0.0.1)

Требования: **Windows 10/11** и **[.NET 10 SDK](https://dotnet.microsoft.com/download)** (для сборки) или .NET 10 Desktop Runtime (для запуска готовой сборки).

```powershell
git clone git@github.com:ugurturna/eft_quest_helper.git
cd eft_quest_helper
dotnet run --project TarkovQuestTracker
```

Собрать автономную папку, которую можно скопировать куда угодно:

```powershell
dotnet publish TarkovQuestTracker -c Release -r win-x64 --self-contained true -o publish
# запустите publish\TarkovQuestTracker.exe
```

## Где хранится прогресс?

Всё хранится локально, ничего никуда не отправляется:

```
%APPDATA%\TarkovQuestTracker\
  app.json                          последний профиль
  profiles\<имя>\settings.json      оверлей, языки, горячие клавиши
  profiles\<имя>\progress_pvp.json
  profiles\<имя>\progress_pve.json
```

Прогресс сохраняется при каждом клике. Для резервной копии или переноса на другой ПК используйте **Настройки → Экспорт / Импорт**. Чтобы хранить данные в другом месте (портативный режим), задайте переменную окружения `TARKOV_TRACKER_HOME`.

Горячие клавиши меняются в `settings.json` (`clickThroughHotkey`, `toggleOverlayHotkey`, например `"Ctrl+Alt+F9"`).

## Данные заданий

Данные лежат в `data/` и собираются скриптами из `tools/scraper/` (Python 3.12, только стандартная библиотека):

| Команда | Что делает |
|---|---|
| `python tools/scraper/scrape_wiki.py` | Скачивает все задания с [EFT Fandom wiki](https://escapefromtarkov.fandom.com/) → `data/tasks_en.json` (`--refresh` игнорирует локальный кэш) |
| `python tools/scraper/fetch_tarkovdev.py` | Скачивает дополнительные данные с [tarkov.dev](https://tarkov.dev/) (флаги Kappa, уровни, недостающие требования, переводы названий) |
| `python tools/scraper/merge.py` | Объединяет данные tarkov.dev с данными вики и пишет `data/diff_report.md` с расхождениями между источниками |
| `python tools/scraper/build_translations.py tr` | Добавляет ручные переводы из `tools/translations/tr/` в `data/tasks_tr.json` |

Для каждого поля задания сохраняется источник (`sources`), так что данные вики можно отличить от данных tarkov.dev.

### Перевод

Переводы — это обычные JSON-файлы, по одному на задание, в `tools/translations/<язык>/`; они не теряются при повторной загрузке вики. Скопируйте существующий файл (например, `tools/translations/tr/story/tour.json`), переведите `name`, `objectives[].text`, `objectives[].details` и `guide`, затем запустите `build_translations.py`. Скрипт сообщит о пропущенных или несовпадающих целях. Русский перевод заданий и интерфейса приветствуется.

Тексты интерфейса находятся в `TarkovQuestTracker/Strings/Strings.<язык>.xaml`.

## Структура проекта

```
TarkovQuestTracker/          приложение WPF (.NET 10): окна, оверлей, view model, тексты интерфейса
TarkovQuestTracker.Core/     модели заданий, загрузка данных, поиск, граф требований, профили/прогресс
tests/                       тесты xUnit (планировщик, поиск, профили, проверки реальных данных)
tools/scraper/               скрипты данных (вики, tarkov.dev, объединение, переводы)
tools/translations/          ручные переводы
tools/make_icon.ps1          создаёт .ico из PNG
data/                        данные заданий, поставляемые с приложением
```

Тесты запускаются командой `dotnet test`.

## Источники и правовая информация

- Данные заданий: [Escape from Tarkov Wiki (Fandom)](https://escapefromtarkov.fandom.com/) — контент доступен по лицензии [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) — и [tarkov.dev](https://tarkov.dev/).
- *Escape from Tarkov*, имена и портреты торговцев являются товарными знаками/собственностью **Battlestate Games** и используются здесь только для обозначения игрового контента. Это некоммерческий фанатский проект.
