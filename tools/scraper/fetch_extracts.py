"""Extract (exfil) names per map -> data/extracts.json

    python tools/scraper/fetch_extracts.py

The app reads the exfil list the game shows on a double "O" press (top right of the screen) and
recognizes the map from it. Source: json.tarkov.dev static dumps (PvP + PvE, English + Turkish names,
the game can run in either). Map variants share their base map's name ("Night Factory" -> "Factory"),
the name the quests use.
"""
import json
import sys
import time
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

BASE = "https://json.tarkov.dev"
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "data" / "extracts.json"

MODES = ["regular", "pve"]
LANGS = ["en", "tr"]
# tarkov.dev map name -> the map name the quests use; None = not a raid map
VARIANTS = {
    "Night Factory": "Factory",
    "Infected Factory": "Factory",
    "Ground Zero 21+": "Ground Zero",
    "Ground Zero Tutorial": None,
    "The Lab (Dark)": "The Lab",
    "Infected Lab": "The Lab",
}


def get(path):
    req = urllib.request.Request(f"{BASE}/{path}", headers={"User-Agent": "TarkovQuestTracker/0.1"})
    for attempt in range(5):
        try:
            with urllib.request.urlopen(req, timeout=120) as r:
                return json.loads(r.read().decode("utf-8"))["data"]
        except Exception as e:  # noqa: BLE001 - network, retry with backoff
            wait = 3 * 2 ** attempt
            print(f"  {path}: {e} - retry in {wait}s", file=sys.stderr)
            time.sleep(wait)
    return None


def main():
    maps = {}  # our map name -> set of extract names (all languages)
    ok = 0
    for mode in MODES:
        dump = get(f"{mode}/maps")
        if dump is None:
            print(f"FAILED {mode} (json.tarkov.dev unavailable)")
            continue
        names = {lang: get(f"{mode}/maps_{lang}") or {} for lang in LANGS}
        en = names["en"]
        for m in dump["maps"].values():
            title = en.get(m["name"], m["name"])
            title = VARIANTS.get(title, title)
            if title is None:
                continue
            for x in m.get("extracts") or []:
                for lang in LANGS:
                    text = names[lang].get(x["name"]) or en.get(x["name"])
                    if text:
                        maps.setdefault(title, set()).add(text.strip())
        ok += 1
    if not ok:
        sys.exit(1)
    data = {
        "fetchedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "source": BASE,
        "maps": {k: sorted(v) for k, v in sorted(maps.items()) if v},
    }
    OUT.write_text(json.dumps(data, ensure_ascii=False, indent=1), encoding="utf-8")
    for k, v in data["maps"].items():
        print(f"{k}: {len(v)} extracts")
    sys.exit(0 if ok == len(MODES) else 1)


if __name__ == "__main__":
    main()
