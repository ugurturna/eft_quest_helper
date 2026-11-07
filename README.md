<p align="center">
  <img src="TarkovQuestTracker/Assets/app-icon.png" width="120" alt="EFT Quest Helper icon">
</p>

<h1 align="center">EFT Quest Helper</h1>

<p align="center">
  A desktop quest tracker and planner for <b>Escape from Tarkov</b> — with an always-on-top overlay.<br>
  <b>English</b> · <a href="README.tr.md">Türkçe</a> · <a href="README.ru.md">Русский</a>
</p>

<p align="center">
  <a href="https://github.com/ugurturna/eft_quest_helper/releases/latest">
    <img src="https://img.shields.io/github/v/release/ugurturna/eft_quest_helper?style=for-the-badge&label=Download&color=2ea44f&logo=windows&logoColor=white" alt="Download the latest release">
  </a><br>
  <a href="https://github.com/ugurturna/eft_quest_helper/releases/latest">https://github.com/ugurturna/eft_quest_helper/releases/latest</a><br>
  🌐 <b>Website:</b> <a href="https://ugurturna.github.io/eft_quest_helper/">https://ugurturna.github.io/eft_quest_helper/</a>
</p>

---

## Ban risk

> [!TIP]
> **No known ban risk.** EFT Quest Helper is a normal, separate Windows application. It does **not** read or write game memory, does **not** inject anything into the game, does **not** read game files or logs, does **not** send input to the game and does **not** talk to the game's servers. The overlay is just an ordinary window kept on top of the screen (like a sticky note), and the hotkeys use the standard Windows `RegisterHotKey` API.

> [!CAUTION]
> **Use it at your own risk. This project is not affiliated with or endorsed by Battlestate Games, and BSG can change its rules at any time. If BSG ever states that tools like this are not allowed, stop using it.**

---

<p align="center">
  <img src="docs/screenshots/main-window.webp" width="780" alt="Main window">
</p>

## Features

- **All quests in one place** — ~870 quests: trader (side) quests, story chapters and event quests, with objectives, required items, rewards, guides and notes.
- **Quest items with icons** — which trader needs which items for which quest, with item icons, counts and Found-in-Raid flags (see [Quest items](#quest-items)).
- **Prerequisite chains** — every quest knows what it needs and what it unlocks.
- **Goal planner** — pick a goal (e.g. *The Punisher – Part 3* or *Collector*) and see the whole chain in order, what you can do right now and what is left.
- **Kappa / Collector path** — the quests Kappa really needs: *Collector* and its whole prerequisite chain. Progress bar, available quests **grouped by map** so you can do several in one raid, and quests waiting for your level.
- **Active quests + overlay** — the quests you are doing right now appear in a small tracker on the right edge of the screen, with objective checkboxes, kill/item counters and item icons.
- **Activating a quest completes its chain** — set *Punisher Part 5* active and Parts 1–4 are marked done automatically (one-click undo).
- **PvP and PvE** — separate progress for each mode; PvE-specific differences (levels, objectives, items) are shown.
- **Search in English and Turkish** — tolerant of typos and Turkish characters (`punisher 3`, `punişır 3`, `cezalandırıcı`).
- **Two languages, independently** — app language (menus) and quest language (quest and item names) can differ, e.g. Turkish app with English quests. All story chapters are translated to Turkish.
- **Profiles** — several players on one PC, each with their own progress and settings.
- **Event quests** — active events are listed; past events can be shown in Settings.
- **Fast** — lists load page by page while you scroll.

### Quest items

<p align="center">
  <img src="docs/screenshots/items.webp" width="780" alt="Items tab: items each trader needs, quest by quest">
</p>

The **Items** tab answers "what do I need to keep for my quests?":

- Grouped **by trader**, then **by quest**; every item is a small card with its **icon**, short name, **count** and a **FIR** mark when it has to be found in raid. Hover a card for the full name.
- Objectives that accept several items (e.g. *any 3 medical items*) show the first one with **+N**; the tooltip lists the alternatives.
- **Search** an item (`Salewa`, `GPU`, `Tushonka`…) to see every quest that wants it.
- **Hide completed** (on by default) and **FIR only** filters; the header shows how many quests and distinct items are left.
- Click a quest name to open it in the detail panel.

Item icons also appear **inside the quests**: find / hand-over / plant objectives show the item's icon in the quest details, on the *Active Quests* cards and in the overlay, and the detail panel lists the quest's required items as cards. Items are shown in the quest language (English or Turkish).

### Overlay

<img src="docs/screenshots/overlay.webp" width="320" align="right" alt="Overlay">

- Docked to the right edge, **moves only up/down** (drag the header).
- Adjustable **opacity**, **size** (60–200 %, also <kbd>Ctrl</kbd> + mouse wheel) and **width**.
- Two views: **titles only** or **title + details**.
- Item objectives show the **item icon**, so you recognise the loot mid-raid.
- <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>T</kbd> — lock the overlay (clicks go through to the game) / unlock.
- <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>H</kbd> — show / hide the overlay.
- It never takes focus away from the game.

> [!NOTE]
> Run Tarkov in **Borderless** or **Windowed** mode. Windows cannot draw other windows over a game in exclusive fullscreen.

<br clear="right">

## Download & run

**Download the latest release (recommended):** [Releases](https://github.com/ugurturna/eft_quest_helper/releases/latest) → `TarkovQuestTracker-vX.Y.Z-win-x64.zip`

Extract the zip anywhere and run `TarkovQuestTracker.exe`. Nothing to install: .NET is included. Keep the `data` folder next to the exe; it holds the quest data, the item icons and your progress.

### "Windows protected your PC" warning

The exe is not code-signed yet, so Microsoft Defender SmartScreen may show **"Windows protected your PC — Unknown publisher"** the first time you run it. This is expected for new, unsigned open-source apps and goes away as more people use it.

> [!IMPORTANT]
> **Every release is submitted to Microsoft for malware analysis.** Microsoft's review takes **3–5 business days**, so a brand-new release can show this warning until the review is done.

To run it anyway: click **More info** → **Run anyway**.

If you'd rather check it first, the full source is in this repo and every release is built by [GitHub Actions](.github/workflows/release.yml) from a tagged commit. You can also scan the zip on [VirusTotal](https://www.virustotal.com/) before running it.

### Updating

Your progress is in `data\profiles` next to the exe. Extract the new version to a new folder and copy `data\profiles` over, or use **Settings → Export** in the old version and **Import** in the new one.

## Where is my progress stored?

Everything is stored locally, nothing is uploaded. When there is a `data` folder next to the exe (release zip, published or built app), it is used:

```
data\
  app.json                      last used profile
  profiles\<name>\settings.json overlay, languages, hotkeys
  profiles\<name>\progress_pvp.json
  profiles\<name>\progress_pve.json
```

Without a `data` folder next to the exe, the same files go to `%APPDATA%\TarkovQuestTracker\`. Set the `TARKOV_TRACKER_HOME` environment variable to keep them anywhere else.

Progress is saved on every click. Use **Settings → Export / Import** to back it up or move it to another PC.

Hotkeys can be changed in `settings.json` (`clickThroughHotkey`, `toggleOverlayHotkey`, e.g. `"Ctrl+Alt+F9"`).

## Credits & legal

- Quest data: [Escape from Tarkov Wiki (Fandom)](https://escapefromtarkov.fandom.com/), content available under [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/), and [tarkov.dev](https://tarkov.dev/) (quest items and item icons).
- *Escape from Tarkov*, trader names, trader portraits and item images are trademarks/property of **Battlestate Games**. They are used here only to identify in-game content. This is a non-commercial fan project.
- Free code signing provided by [SignPath.io](https://signpath.io), certificate by [SignPath Foundation](https://signpath.org).
- Licensed under the [MIT License](LICENSE).
