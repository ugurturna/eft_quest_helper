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
- **Kappa / Collector path** — the quests Kappa really needs: *Collector* and its whole prerequisite chain. Progress bar, available quests **grouped by map** so you can do several in one raid; each quest shows its required level.
- **Active quests + overlay** — all the quests you are doing are listed under *Active Quests*; only the ones you pin with **📌 Overlay** appear in the small tracker on the right edge of the screen, with objective checkboxes, kill/item counters and item icons. Activating a quest does not pin it; you decide what goes on the overlay.
- **Flea market prices in game** — <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>F</kbd> opens a price search in the middle of the screen; type 3 letters of an item and see its flea price for your mode (PvP / PvE). See [Flea market prices](#flea-market-prices).
- **Side menu** — pages (Quests, Goal Planner, Kappa, Items, Active Quests, End Game, Settings) are in a menu on the left.
- **What to bring into the raid** — keys, markers (with how many you need), items to plant, the weapon to use and the clothes to wear are shown at the top of each quest and on its *Active Quests* card.
- **Handing in accepts the next quests** — complete an active quest and the quests it unlocks become active (not pinned to the overlay: you still have to take them from the trader).
- **Activating a quest completes its chain** — set *Punisher Part 5* active and Parts 1–4 are marked done automatically (one-click undo).
- **PvP and PvE** — separate progress for each mode; PvE-specific differences (levels, objectives, items) are shown.
- **Search in English and Turkish** — tolerant of typos and Turkish characters (`punisher 3`, `punişır 3`, `cezalandırıcı`).
- **Two languages, independently** — app language (menus) and quest language (quest and item names) can differ, e.g. Turkish app with English quests. All story chapters are translated to Turkish.
- **Profiles** — several players on one PC, each with their own progress and settings.
- **Event quests** — active events are listed; past events that can't be done any more are left out.
- **End Game (The Ticket endings)** — which of the 4 endings you can still reach, the route to your target ending and where you are right now (see [End Game](#end-game-the-ticket-endings)).
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

### End Game (The Ticket endings)

<p align="center">
  <img src="docs/screenshots/endgame.jpg" width="780" alt="End Game tab: the 4 endings and the route to the target ending">
</p>

The story chapter **The Ticket** has 4 endings — **Survivor**, **Savior**, **Debtor** and **Fallen** — and 3 decisions you can't take back: what you did with the armored case, whether you work with Kerman, and how far you help him with the TerraGroup evidence. The **End Game** tab plans this for you:

1. **Open it** — the *End Game* tab, or make *The Ticket* a goal in the *Goal Planner*: the whole guide appears under its quest chain. *The Ticket*'s detail panel also has an **🎫 End Game guide** button.
2. **Mark your decisions** — click the choices you already made in game (click again to undo). Each option shows which endings it keeps open, e.g. *Don't work with Kerman → Survivor*.
3. **Pick a target ending** — press **★ Make target** on an ending card (each card shows the ending's achievement picture). The decisions ahead are marked **★ Pick this for your target**; if your choices already closed that ending you get a warning and the endings you can still reach.
4. **Follow the route** — every step in order with a checkbox: tasks, achievements on the way (*Easy Way*, *U Turn*, *Enough of Your Games!*, *Will It Blow*, optional *I Am Speed* and *Little Triumph* for all 36 minor evidence), warnings (BTR / Skier, the 72-hour timer, listen to every tape) and the escape rules (Shoreline 21:00–06:00, no weapon in hands, what to do if the escape fails). **◀ YOU ARE HERE** marks the next step.

- Steps that depend on earlier choices change with them: e.g. Prapor wants 300M roubles if you gave him the case and 500M if you kept it.
- **PvP and PvE** objectives differ (Co-Op extracts vs. 5 PMC kills without killing Scavs); the guide shows the ones for your mode.
- Choices, target and ticked steps are saved with your progress, per profile and mode. **Reset** clears them.
- The route data lives in `data/ticket_endings.json` (English and Turkish) and follows the community *Story Endings* flowchart v1.7.6 by Callsign_Smokey / Kay08x.

### Flea market prices

<p align="center">
  <img src="docs/screenshots/flea.webp" width="470" alt="Flea market price search, PvP">
  <img src="docs/screenshots/flea-pve.webp" width="470" alt="Flea market price search, PvE">
</p>

Press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>F</kbd> in game to check what an item sells for, without alt-tabbing to a browser:

- Opens **in the middle of the screen** and takes the keyboard; **<kbd>Esc</kbd> closes it and puts you back in the game**.
- Results appear as soon as you type **3 letters**, in English or Turkish (`gpu`, `ledx`, `salewa`, `ekran kartı`).
- Prices follow your **game mode**: PvP and PvE flea prices are different, the window shows the mode you selected.
- For each item: **flea price** (lowest offer, or the 24 h average when nobody is selling), **price per slot** for bigger items, **24 h average**, **48 h trend**, the **best trader** to sell to, and *Can't be sold on flea* for banned items.
- Prices come from [tarkov.dev](https://tarkov.dev) (`json.tarkov.dev/regular/items` and `/pve/items`). Both modes are downloaded in the background when the app starts and kept on disk with a timestamp; if they are older than **15 minutes** when you open the search, your mode is downloaded again while you search the saved copy. Searching never waits for the network.

### Overlay

<img src="docs/screenshots/overlay.webp" width="320" align="right" alt="Overlay">

- Starts on the right edge; **drag the header to move it anywhere** on the screen (or use the position sliders in Settings).
- Adjustable **opacity**, **size** (60–200 %, also <kbd>Ctrl</kbd> + mouse wheel) and **width**.
- Two views: **titles only** or **title + details**.
- Item objectives show the **item icon**, so you recognise the loot mid-raid.
- <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>T</kbd> — lock the overlay (clicks go through to the game) / unlock.
- <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>H</kbd> — show / hide the overlay.
- Shows only the quests pinned with **📌 Overlay** (even with 70 active quests). Pin / unpin them under *Active Quests*; the ✕ on the overlay only unpins, the quest stays active.
- It never takes focus away from the game.

> [!NOTE]
> Run Tarkov in **Borderless** or **Windowed** mode. Windows cannot draw other windows over a game in exclusive fullscreen.

<br clear="right">

## Shortcuts

| Shortcut | What it does |
|---|---|
| <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>T</kbd> | **Lock / unlock the overlay.** While locked it can't be clicked and every click goes through to the game, so leave it locked while you play. Unlock it to change settings or move it. |
| <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>H</kbd> | **Show / hide the overlay**, e.g. to clear the screen mid-raid. |
| <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>F</kbd> | **Flea market price search** in the middle of the screen, for your mode (PvP / PvE). <kbd>Esc</kbd> closes it and returns to the game. |
| <kbd>Ctrl</kbd> + mouse wheel | With the mouse over the (unlocked) overlay: **make it bigger / smaller** (60–200 %). |
| Drag the overlay header | **Move the overlay anywhere** on the screen (unlocked only). |

- The shortcuts work while the game is running and the app is in the background.
- They are always shown in the bottom bar of the main window, and explained at the top of **Settings**, because the overlay's own hint can be hard to read in game.
- To change them, edit `clickThroughHotkey` / `toggleOverlayHotkey` / `fleaSearchHotkey` in the profile's `settings.json` (e.g. `"Ctrl+Alt+F9"`). If a shortcut is already taken by another program, the app tells you at start.

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

**Automatic (from the version after v0.1.2):** when a new release is out, a bar at the top of the main window says so. Click **Update**: the app downloads the release zip from GitHub, checks it against the release's SHA-256, replaces its own files and restarts. Your `data\profiles` (progress, settings) and the flea price cache are never touched. You can turn the start-up check off, or check by hand, under **Settings → Updates**.

**By hand (or from v0.1.2 and older):** your progress is in `data\profiles` next to the exe. Extract the new version to a new folder and copy `data\profiles` over, or use **Settings → Export** in the old version and **Import** in the new one.

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

Hotkeys can be changed in `settings.json` (`clickThroughHotkey`, `toggleOverlayHotkey`, `fleaSearchHotkey`, e.g. `"Ctrl+Alt+F9"`).

## Credits & legal

- Quest data: [Escape from Tarkov Wiki (Fandom)](https://escapefromtarkov.fandom.com/), content available under [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/), and [tarkov.dev](https://tarkov.dev/) (quest items and item icons).
- *Escape from Tarkov*, trader names, trader portraits and item images are trademarks/property of **Battlestate Games**. They are used here only to identify in-game content. This is a non-commercial fan project.
- Free code signing provided by [SignPath.io](https://signpath.io), certificate by [SignPath Foundation](https://signpath.org).
- Licensed under the [MIT License](LICENSE).
