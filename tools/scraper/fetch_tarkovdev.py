"""tarkov.dev GraphQL -> data/sources/tarkovdev_{pvp,pve}_{en,tr}.json

    python tools/scraper/fetch_tarkovdev.py
"""
import json
import sys
import time
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

URL = "https://api.tarkov.dev/graphql"
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "data" / "sources"

QUERY = """
query($lang: LanguageCode, $mode: GameMode) {
  tasks(lang: $lang, gameMode: $mode) {
    id name normalizedName wikiLink experience minPlayerLevel
    kappaRequired lightkeeperRequired
    trader { name }
    map { name }
    taskRequirements { task { id name } status }
    traderRequirements { trader { name } value compareMethod }
    objectives { id type description optional maps { name } }
  }
}
"""
MODES = {"pvp": "regular", "pve": "pve"}
LANGS = ["en", "tr"]


def post(variables):
    body = json.dumps({"query": QUERY, "variables": variables}).encode()
    req = urllib.request.Request(URL, data=body, headers={
        "Content-Type": "application/json", "User-Agent": "TarkovQuestTracker/0.1"})
    for attempt in range(6):
        try:
            with urllib.request.urlopen(req, timeout=90) as r:
                data = json.loads(r.read().decode("utf-8"))
            if (data.get("data") or {}).get("tasks"):
                return data["data"]["tasks"]
            err = data.get("errors")
        except Exception as e:  # noqa: BLE001 - network, retry with backoff
            err = e
        wait = 5 * 2 ** attempt
        print(f"  {variables}: {err} - retry in {wait}s", file=sys.stderr)
        time.sleep(wait)
    return None


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    ok = 0
    for mode, gql_mode in MODES.items():
        for lang in LANGS:
            tasks = post({"lang": lang, "mode": gql_mode})
            if tasks is None:
                print(f"FAILED {mode}/{lang} (tarkov.dev unavailable)")
                continue
            (OUT / f"tarkovdev_{mode}_{lang}.json").write_text(
                json.dumps(tasks, ensure_ascii=False, indent=1), encoding="utf-8")
            print(f"{mode}/{lang}: {len(tasks)} tasks")
            ok += 1
    if ok:
        meta_path = ROOT / "data" / "meta.json"
        meta = json.loads(meta_path.read_text(encoding="utf-8")) if meta_path.exists() else {}
        meta["tarkovDev"] = {"fetchedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
                             "source": URL}
        meta_path.write_text(json.dumps(meta, indent=1), encoding="utf-8")
    sys.exit(0 if ok == len(MODES) * len(LANGS) else 1)


if __name__ == "__main__":
    main()
