#!/usr/bin/env python3
"""
Whole-state pass of the law-quote pipeline.

  python -I state_run.py <state-slug>

Reads  registry/<state>.json :
    {"code": "S.C. Code Ann.",                         # label used in "<code> § <cite>"
     "urls": ["https://official/page/with/statutes", ...]}
Walks  Content/states/<state>/**/*.yaml (skips hub/surnames and files that already have expert_quote),
fetches every registry URL ONCE, and for each symbol file looks for a VERBATIM designation sentence.
Writes reports/<state>.json and prints a table. Nothing in Content/ is modified.

Confidence:
  high    all words of the symbol name + a designation word + the symbol noun (bird, tree, ...) are in one
          sentence, the sentence is unique on the page, and a section heading with a number sits above it.
  medium  only the last word of the name matched (e.g. "mockingbird" for "Northern Mockingbird"), or the
          sentence has several designation words. A human must read it and set "approved": true.
  none    nothing found. Skip the file.
"""
import json
import os
import re
import sys

import yaml

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import extract as ex

ROOT = "d:/проекты/usasymbol/usasymbol/"
HERE = os.path.dirname(os.path.abspath(__file__)) + "/"
SKIP_FILES = {"hub.yaml", "surnames.yaml"}

NOUNS = {
    "bird": ["bird"], "flower": ["flower", "wildflower", "blossom"], "tree": ["tree"],
    "mammal": ["mammal", "animal"], "fish": ["fish"], "insect": ["insect", "butterfly", "bee", "moth"],
    "reptile": ["reptile"], "amphibian": ["amphibian", "frog", "salamander"], "fossil": ["fossil"],
    "dinosaur": ["dinosaur"], "gemstone": ["gem", "stone"], "mineral": ["mineral"], "rock": ["rock", "stone"],
    "soil": ["soil"], "song": ["song", "ballad", "anthem", "march"], "dance": ["dance"],
    "sport": ["sport"], "beverage": ["beverage", "drink"], "drink": ["beverage", "drink"],
    "fruit": ["fruit", "berry"], "vegetable": ["vegetable"], "dessert": ["dessert", "pie", "cookie", "cake"],
    "motto": ["motto"], "nickname": ["nickname", "known as"], "color": ["color", "colors"], "colors": ["color", "colors"],
    "firearm": ["firearm", "rifle", "gun"], "dog": ["dog"], "horse": ["horse"], "cat": ["cat"],
}
GENERIC = {"state", "common", "american", "northern", "eastern", "western", "southern", "great", "red", "white",
           "blue", "black", "green", "wild", "sweet", "official"}


def nouns_for(symbol_type, rel):
    words = re.findall(r"[a-z]+", (symbol_type or "").lower())
    words = [w for w in words if w != "state"]
    out = []
    for w in reversed(words):
        out += NOUNS.get(w, [w])
    if not out:
        base = os.path.splitext(rel.split("/")[0])[0].split("-")[0]
        out = NOUNS.get(base, [base])
    return list(dict.fromkeys(out))


def clean(quote):
    quote = re.sub(r"^(?:(?:Section|Sec\.|§)\s*[\d][\w.\-]*\.?\s+)", "", quote)
    quote = re.sub(r"^(?:\d[\d.]*(?:\(\w+\))*\s+)?(?:\(\w+\)\s+)+", "", quote)
    quote = re.sub(r"\s*[\(\[]\s*(?:Added|Amended|PL|P\.L\.|\d{4}|History)[^\)\]]*[\)\]]?\s*$", "", quote)
    return quote.strip()


def yaml_cite(leg):
    m = re.findall(r"\d+[A-Za-z]?(?:[\-.:]\d+[A-Za-z]?)+(?:\(\w+\))*", leg or "")
    return m


def load_symbol_files(state):
    base = ROOT + f"Content/states/{state}/"
    out = []
    for dp, _, files in os.walk(base):
        for f in files:
            if not f.endswith(".yaml") or f in SKIP_FILES:
                continue
            path = os.path.join(dp, f).replace("\\", "/")
            raw = open(path, encoding="utf-8-sig").read()
            if re.search(r"^expert_quote:", raw, re.M):
                continue
            try:
                d = yaml.safe_load(raw)
            except Exception:  # noqa: BLE001
                continue
            if not isinstance(d, dict) or not (d.get("title") or d.get("name")) or not d.get("sections"):
                continue
            rel = path.split(f"Content/states/{state}/")[1]
            out.append({"file": f"Content/states/{state}/{rel}", "title": str(d.get("name") or d.get("title")).strip(),
                        "symbol_type": d.get("symbol_type") or d.get("type") or d.get("designation_label") or "", "legislation": d.get("legislation", ""), "rel": rel})
    return out


_pages = {}


def page(url):
    if url not in _pages:
        code, raw = ex.fetch(url)
        _pages[url] = (code, ex.to_text(raw) if code == "200" else "")
    return _pages[url]


def heading_cite(text, quote):
    tpos = text.find(quote[:60])
    if tpos < 0:
        return None
    head_re = re.compile(r"^\s*(?:SECTION|Section|Sec\.|§|F\.S\.)\s*([0-9][\w.\-:]*?)\.?\s+(?:[0-9][\w.\-]*\s+)?(?!SECTION)[A-Z]", re.M)
    heads = list(head_re.finditer(text[:tpos + len(quote)]))
    if heads and (tpos - heads[-1].start()) < 1500:
        return heads[-1].group(1)
    return None


def find(sym, url, text):
    words = ex.name_words(sym["title"])
    nouns = nouns_for(sym["symbol_type"], sym["rel"])
    head = [w for w in words if w not in GENERIC][-1:] or words[-1:]
    best = None
    for s in ex.sentences(text):
        if not (25 <= len(s) <= 450):
            continue
        low = s.lower()
        if not ex.DESIGNATION.search(s):
            continue
        if not any(re.search(r"\b" + re.escape(n), low) for n in nouns):
            continue
        strict = all(re.search(r"\b" + re.escape(w), low) for w in words)
        loose = bool(head) and re.search(r"\b" + re.escape(head[0]), low) and len(head[0]) >= 4
        if not (strict or loose):
            continue
        q = clean(s)
        if not (q[:1].isupper() and q.endswith(".")) or q.count("  ") > 2:
            continue
        cand = (0 if strict else 1, len(q), q)
        if best is None or cand < best:
            best = cand
    if not best:
        return None
    strict_hit, _, quote = best
    flat = re.sub(r"\s+", " ", text)
    occ = flat.count(re.sub(r"\s+", " ", quote))
    cite = heading_cite(text, quote)
    desig = len(ex.DESIGNATION.findall(quote))
    reasons = []
    if strict_hit:
        reasons.append("strict name match")
    else:
        reasons.append("only last word of name matched")
    if occ != 1:
        reasons.append(f"quote occurs {occ}x on page")
    if not cite:
        reasons.append("no section heading above quote")
    if desig > 2:
        reasons.append("several designation words")
    conf = "high" if (strict_hit == 0 and occ == 1 and cite and desig <= 2) else ("medium" if occ >= 1 else "none")
    return {"quote": quote, "cite": cite, "url": url, "confidence": conf, "reasons": reasons}


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    state = sys.argv[1]
    reg = json.load(open(HERE + f"registry/{state}.json", encoding="utf-8"))
    syms = load_symbol_files(state)
    rank = {"high": 0, "medium": 1, "none": 2}
    rows = []
    for u in reg["urls"]:
        code, _ = page(u)
        print(f"fetch {code} {u}")
    for sym in syms:
        best = None
        for u in reg["urls"]:
            code, text = page(u)
            if not text:
                continue
            r = find(sym, u, text)
            if r and (best is None or rank[r["confidence"]] < rank[best["confidence"]]):
                best = r
        row = {"file": sym["file"], "name": sym["title"], "symbol_type": sym["symbol_type"], "code": reg["code"],
               "yaml_legislation": sym["legislation"]}
        if best:
            row.update(best)
            yc = yaml_cite(sym["legislation"])
            row["yaml_cite_match"] = bool(best["cite"] and any(best["cite"].replace(" ", "") in c or c in best["cite"] for c in yc)) if yc else None
        else:
            row.update({"confidence": "none", "reasons": ["no sentence found"]})
        rows.append(row)
    os.makedirs(HERE + "reports", exist_ok=True)
    json.dump(rows, open(HERE + f"reports/{state}.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    counts = {k: sum(1 for r in rows if r["confidence"] == k) for k in ("high", "medium", "none")}
    print(f"\n{state}: {len(rows)} files -> {counts}")
    for r in rows:
        if r["confidence"] != "none":
            print(f"{r['confidence']:6} {r['name'][:28]:28} § {r.get('cite')} | {r['quote'][:110]} | yaml-match={r.get('yaml_cite_match')} {'; '.join(r['reasons'][1:])}")


if __name__ == "__main__":
    main()
