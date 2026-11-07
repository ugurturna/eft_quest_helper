"""tarkov.dev -> data/sources/tarkovdev_{pvp,pve}_{en,tr}.json

    python tools/scraper/fetch_tarkovdev.py

Reads the static dumps on json.tarkov.dev (the GraphQL API is often down) and writes
the same shape the GraphQL "tasks" query returns, so merge.py doesn't care which was used.
"""
import json
import sys
import time
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

BASE = "https://json.tarkov.dev"
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "data" / "sources"

MODES = {"pvp": "regular", "pve": "pve"}
LANGS = ["en", "tr"]


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


def convert(tasks, tr):
    """Dump records (ids + translation keys) -> GraphQL-shaped records with plain text."""
    def name(task_id):
        return tr["tasks"].get(f"{task_id} name", "")

    def trader(trader_id):
        return {"name": tr["traders"].get(f"{trader_id} Nickname", trader_id)}

    def map_(map_id):
        return {"name": tr["maps"].get(f"{map_id} Name", map_id)}

    out = []
    for t in tasks.values():
        out.append({
            "id": t["id"], "name": name(t["id"]), "normalizedName": t.get("normalizedName"),
            "wikiLink": t.get("wikiLink"), "experience": t.get("experience"),
            "minPlayerLevel": t.get("minPlayerLevel"),
            "kappaRequired": bool(t.get("kappaRequired")),
            "lightkeeperRequired": bool(t.get("lightkeeperRequired")),
            "trader": trader(t["trader"]),
            "map": map_(t["map"]) if t.get("map") else None,
            "taskRequirements": [{"task": {"id": r["task"], "name": name(r["task"])}, "status": r.get("status") or []}
                                 for r in t.get("taskRequirements") or []],
            "traderRequirements": [{"trader": trader(r["trader"]), "value": r.get("value"),
                                    "compareMethod": r.get("compareMethod")}
                                   for r in t.get("traderRequirements") or []],
            "objectives": [{"id": o["id"], "type": o["type"],
                            "description": tr["tasks"].get(o["description"], o["description"]),
                            "optional": bool(o.get("optional")),
                            "maps": [map_(m) for m in o.get("maps") or []]}
                           for o in t.get("objectives") or []],
        })
    return out


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    ok = 0
    for mode, path in MODES.items():
        dump = get(f"{path}/tasks")
        if dump is None:
            print(f"FAILED {mode} (json.tarkov.dev unavailable)")
            continue
        for lang in LANGS:
            tr = {kind: get(f"{path}/{kind}_{lang}") or {} for kind in ("tasks", "traders", "maps")}
            tasks = convert(dump["tasks"], tr)
            (OUT / f"tarkovdev_{mode}_{lang}.json").write_text(
                json.dumps(tasks, ensure_ascii=False, indent=1), encoding="utf-8")
            print(f"{mode}/{lang}: {len(tasks)} tasks")
            ok += 1
    if ok:
        meta_path = ROOT / "data" / "meta.json"
        meta = json.loads(meta_path.read_text(encoding="utf-8")) if meta_path.exists() else {}
        meta["tarkovDev"] = {"fetchedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
                             "source": BASE}
        meta_path.write_text(json.dumps(meta, indent=1), encoding="utf-8")
    sys.exit(0 if ok == len(MODES) * len(LANGS) else 1)


if __name__ == "__main__":
    main()
