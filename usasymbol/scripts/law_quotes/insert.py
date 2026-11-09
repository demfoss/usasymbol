#!/usr/bin/env python3
"""
Step 3: insert verified quotes into YAML. DRY RUN unless --apply is passed.

  python -I insert.py report.json                 # shows what would change
  python -I insert.py report.json --apply         # writes files

Only rows with confidence == "high" AND a "code" field (e.g. "Fla. Stat.") are considered, and only
files that do not already have a top-level expert_quote. After writing, the YAML is re-parsed and the
quote must round-trip exactly, otherwise the file is restored.
"""
import json
import re
import sys

import yaml

ROOT = "d:/проекты/usasymbol/usasymbol/"
PREFER = re.compile(r"law|legal|statut|adopt|designat|official|histor", re.I)


def pick_section(text):
    try:
        secs = yaml.safe_load(text).get("sections") or []
    except Exception:  # noqa: BLE001
        return None
    ids = [str(x["id"]) for x in secs if isinstance(x, dict) and x.get("id")]
    if not ids:
        return None
    for i in ids:
        if PREFER.search(i):
            return i
    return ids[0]


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    rows = json.load(open(sys.argv[1], encoding="utf-8"))
    apply = "--apply" in sys.argv
    done = skipped = 0
    for r in rows:
        f = r["file"]
        if not (r.get("confidence") == "high" or r.get("approved") is True) or not r.get("code") or not r.get("cite"):
            print(f"SKIP  {f}: not high / no code or cite ({r.get('confidence')})")
            skipped += 1
            continue
        path = ROOT + f
        raw = open(path, encoding="utf-8", newline="").read()
        nl = "\r\n" if "\r\n" in raw else "\n"
        bom = raw.startswith("\ufeff")
        text = raw.lstrip("\ufeff")
        if re.search(r"^expert_quote:", text, re.M):
            print(f"SKIP  {f}: already has expert_quote")
            skipped += 1
            continue
        section = pick_section(text)
        if not section:
            print(f"SKIP  {f}: no sections")
            skipped += 1
            continue
        source = f"[{r['code']} § {r['cite']}]({r['url']})"
        block = ["expert_quote:",
                 f"  text: {json.dumps(r['quote'], ensure_ascii=False)}",
                 f"  source: {json.dumps(source, ensure_ascii=False)}"]
        out = text if text.endswith(("\n", "\r\n")) else text + nl
        if re.search(r"^expert_quote_after_section:", out, re.M):
            out = re.sub(r"^expert_quote_after_section:.*$", f"expert_quote_after_section: {section}", out, flags=re.M)
        else:
            block.insert(0, f"expert_quote_after_section: {section}")
        out += nl.join(block) + nl
        try:
            data = yaml.safe_load(out)
            assert data["expert_quote"]["text"] == r["quote"]
            assert data["expert_quote"]["source"] == source
        except Exception as ex:  # noqa: BLE001
            print(f"FAIL  {f}: yaml check {ex}")
            skipped += 1
            continue
        print(f"{'WRITE' if apply else 'would'} {f}  -> after section '{section}'  [{r['code']} § {r['cite']}]")
        if apply:
            open(path, "w", encoding="utf-8", newline="").write(("\ufeff" if bom else "") + out)
        done += 1
    print(f"\n{done} inserted, {skipped} skipped" + ("" if apply else " (dry run)"))


if __name__ == "__main__":
    main()
