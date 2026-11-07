"""Merge hand-written translations into data/tasks_{lang}.json.

Translations live in tools/translations/{lang}/**/*.json (one quest per file) so they survive
re-scraping. Each file:

    {"id": "tour", "name": "...", "objectives": [{"id": "o1", "text": "...", "details": ["..."]}],
     "guide": "...", "notes": "..."}

    python tools/scraper/build_translations.py tr
"""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def main():
    lang = sys.argv[1] if len(sys.argv) > 1 else "tr"
    src_dir = ROOT / "tools" / "translations" / lang
    out_path = ROOT / "data" / f"tasks_{lang}.json"
    en = {t["id"]: t for t in json.loads((ROOT / "data" / "tasks_en.json").read_text(encoding="utf-8"))}
    out = {t["id"]: t for t in json.loads(out_path.read_text(encoding="utf-8"))} if out_path.exists() else {}

    problems, count = [], 0
    for f in sorted(src_dir.rglob("*.json")):
        tr = json.loads(f.read_text(encoding="utf-8"))
        q = en.get(tr["id"])
        if q is None:
            problems.append(f"{f.name}: unknown quest id {tr['id']}")
            continue
        en_objs = {o["id"]: o for o in q["objectives"]}
        for o in tr.get("objectives", []):
            eo = en_objs.get(o["id"])
            if eo is None:
                problems.append(f"{f.name}: unknown objective {o['id']}")
            elif len(o.get("details", [])) != len(eo["details"]):
                problems.append(f"{f.name}: {o['id']} has {len(o.get('details', []))} details, English has {len(eo['details'])}")
        missing = [o for o in en_objs if o not in {x["id"] for x in tr.get("objectives", [])}]
        if missing:
            problems.append(f"{f.name}: untranslated objectives {missing}")
        entry = out.get(tr["id"], {"id": tr["id"], "sources": {}})
        for key in ("name", "objectives", "guide", "notes"):
            if tr.get(key):
                entry[key] = tr[key]
                entry.setdefault("sources", {})[key] = "manual"
        out[tr["id"]] = entry
        count += 1

    out_path.write_text(json.dumps(list(out.values()), ensure_ascii=False, indent=1), encoding="utf-8")
    print(f"{count} translated quests merged into {out_path.relative_to(ROOT)} ({len(out)} entries)")
    for p in problems:
        print("  !", p)
    sys.exit(1 if problems else 0)


if __name__ == "__main__":
    main()
