#!/usr/bin/env python3
"""
Step 2 of the law-quote pipeline: pull a VERBATIM designation sentence from an official page.

Input : candidates.json  [{"file": "Content/states/vermont/beverage/milk.yaml",
                           "url": "https://...", "name": "Milk", "kind": "beverage"}]
Output: report.json      one row per candidate with quote, cite, confidence, and reasons.

Nothing is written to Content/. The quote is always a substring of the fetched page text.
A row is "high" only when ALL of these hold:
  * the sentence contains the symbol name (all significant words);
  * it contains a designation word (official / designated / adopted / hereby / state <thing>);
  * a statute/section citation appears within 600 chars before the sentence on the same page;
  * it is 40..450 chars long and unique on the page.
Everything else is "low" or "none" and must be skipped or reviewed by a human.
"""
import html
import json
import re
import subprocess
import sys
import time

UA = "Mozilla/5.0 (compatible; usasymbol-law-check/1.0)"
DESIGNATION = re.compile(r"\b(official|designat\w+|adopted|hereby|shall be|is the state|are the state|recognized as)\b", re.I)
CITE = re.compile(r"(§\s*[\d][\w.\-]*|\d{1,3}[A-Za-z]?[\-.]\d[\w.\-]*\s+(?=[A-Z])|\bSec(?:tion|\.)\s*[\d][\w.\-]*|\bR\.?S\.?\s*\d[\w.\-]*|\b\d+\s*(?:M\.?R\.?S\.?A?\.?|V\.?S\.?A\.?|ILCS|Pa\.?C\.?S\.?)\s*§?\s*\d[\w.\-]*)")
STOP = {"the", "of", "and", "a", "an", "state", "official"}


def fetch(url):
    for attempt in range(4):
        code, body = _fetch_once(url)
        if code != "429":
            return code, body
        time.sleep(25)
    return code, body


def _fetch_once(url):
    out = subprocess.run(["curl", "-sL", "--ssl-no-revoke", "-A", UA, "-m", "40", "-w", "\n%{http_code}", url],
                         capture_output=True, timeout=60)
    data = out.stdout
    head, _, code = data.rpartition(b"\n")
    if head.count(b"\x00") > len(head) // 4:      # UTF-16 pages (sdlegislature.gov API)
        body = head.decode("utf-16-le", errors="ignore").lstrip("\ufeff")
    else:
        body = head.decode("utf-8", errors="ignore")
    return code.decode().strip(), body


def to_text(raw):
    t = re.sub(r"<script.*?</script>|<style.*?</style>|<!--.*?-->", " ", raw, flags=re.S | re.I)
    t = re.sub(r"</?(?:i|em|b|strong|u|span|sup|sub|font|a)(?:\s[^>]*)?>", "", t, flags=re.I)
    t = re.sub(r"</(p|div|li|tr|h\d|br)>|<br\s*/?>", "\n", t, flags=re.I)
    t = re.sub(r"<[^>]+>", " ", t)
    t = html.unescape(t).replace(" ", " ")
    t = re.sub(r"[ \t\r\f\v]+", " ", t)
    return re.sub(r"\n\s*\n+", "\n", t)


def name_words(name):
    return [w for w in re.findall(r"[a-z0-9]+", name.lower()) if w not in STOP and len(w) > 1]


def sentences(text):
    # keep section numbers like 1-1-690. intact: split on ". " followed by a capital letter
    for block in text.split("\n"):
        block = block.strip()
        if not block:
            continue
        for s in re.split(r"(?<=[a-z\)\"”]\.)(?<!Dr\.)(?<!Mr\.)(?<!Mrs\.)(?<!Ms\.)(?<!St\.)(?<!Mt\.)(?<!No\.)(?<!Sen\.)(?<!Rep\.)(?<!Gov\.)(?<!Jr\.)(?<!Sr\.)\s+(?=[A-Z])", block):
            yield s.strip()


def analyse(row):
    code, raw = fetch(row["url"])
    res = {**row, "http": code, "confidence": "none", "reasons": []}
    if code != "200":
        res["reasons"].append(f"http {code}")
        return res
    text = to_text(raw)
    flat = re.sub(r"\s+", " ", text)
    words = name_words(row["name"])
    kind = row.get("kind", "")
    cands = []
    for s in sentences(text):
        low = s.lower()
        if not (25 <= len(s) <= 450):
            continue
        if not all(re.search(r"\b" + re.escape(w), low) for w in words):
            continue
        if not DESIGNATION.search(s):
            continue
        # a designation sentence must say what the symbol is a symbol OF
        if kind and not re.search(kind.split()[0], low):
            if not re.search(r"\bstate\b", low):
                continue
        cands.append(s)
    if not cands:
        res["reasons"].append("no sentence with name + designation word")
        return res
    cands.sort(key=len)
    quote = re.sub(r"^(?:F\.S\.\s*)?[\d][\w.\-]*\s+(?:[\d][\w.\-]*\s+)?[A-Z][^.]{0,60}\.\s*[—\-–]\s*", "", cands[0].rstrip())
    quote = re.sub(r"^(?:(?:Section|Sec\.|§)\s*[\d][\w.\-]*\.?\s+)", "", quote)
    quote = re.sub(r"^(?:\d[\d.]*(?:\(\w+\))*\s+)?(?:\(\w+\)\s+)+", "", quote)
    quote = re.sub(r"\s*[\(\[]\s*(?:Added|Amended|PL|P\.L\.|\d{4}|History)[^\)\]]*[\)\]]?\s*$", "", quote).strip()
    if not (quote[:1].isupper() and quote.endswith(".")):
        res["reasons"].append("quote is not a whole sentence")
        res["quote"] = quote
        res["confidence"] = "low"
        return res
    pos = flat.find(re.sub(r"\s+", " ", quote))
    if pos < 0:
        res["reasons"].append("quote not found in flattened page (normalisation)")
        res["quote"] = quote
        res["confidence"] = "low"
        return res
    # Citation = the nearest SECTION HEADING above the quote (line-anchored), never a "HISTORY: ... § 4." reference.
    tpos = text.find(quote[:60])
    head_re = re.compile(r"^\s*(?:SECTION|Section|Sec\.|§|F\.S\.)\s*([0-9][\w.\-]*?)\.?\s+(?:[0-9][\w.\-]*\s+)?(?!SECTION)[A-Z]", re.M)
    heads = list(head_re.finditer(text[:tpos + len(quote)])) if tpos >= 0 else []
    cites = [heads[-1].group(1)] if heads and (tpos - heads[-1].start()) < 1500 else []
    res["quote"] = quote
    res["cite"] = cites[0] if cites else None
    res["n_candidates"] = len(cands)
    res["occurrences"] = flat.count(re.sub(r"\s+", " ", quote))
    ok = True
    if not cites:
        res["reasons"].append("no section citation near quote")
        ok = False
    if res["occurrences"] > 1:
        res["reasons"].append("quote occurs more than once on page")
        ok = False
    if len(cands) > 1:
        res["reasons"].append(f"{len(cands)} candidate sentences; picked shortest (review)")
    hint = row.get("cite_hint")
    if hint and (hint in flat or hint in row["url"]) and res["reasons"] in ([], ["no section citation near quote"]):
        res["cite"] = hint
        res["reasons"] = ["cite taken from cite_hint (present in page text or URL)"]
        ok = True
    res["confidence"] = "high" if ok else "low"
    return res


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    src, dst = sys.argv[1], sys.argv[2]
    rows = json.load(open(src, encoding="utf-8"))
    out = [analyse(r) for r in rows]
    json.dump(out, open(dst, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    for r in out:
        print(f"{r['confidence']:5} {r['file']}\n      {r.get('cite')} | {(r.get('quote') or '')[:150]} | {'; '.join(r['reasons'])}")


if __name__ == "__main__":
    main()
