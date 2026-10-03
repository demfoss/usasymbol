#!/usr/bin/env python3
"""Generate state fish YAML pages with the Claude API.

Reads Content/content-pipeline/data/state-fish.json, writes one page per designation to
Content/states/{state}/{file}. Each request researches the designation with web search
before writing. Existing pages are skipped unless --force is given.

The API key comes from ANTHROPIC_API_KEY, or from the project's user-secrets
(AiPipeline:Claude:ApiKey) via `dotnet user-secrets list`.

Usage:
  python scripts/generate-state-fish.py --only alabama,alaska
  python scripts/generate-state-fish.py --limit 5
  python scripts/generate-state-fish.py            # everything not generated yet
"""

import argparse
import concurrent.futures as cf
import datetime as dt
import json
import os
import re
import subprocess
import sys
import threading
from pathlib import Path

import anthropic
import yaml

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / "Content/content-pipeline/data/state-fish.json"
PROMPT = ROOT / "Content/content-pipeline/prompts/fish-writer.md"
STYLE_EXAMPLE = ROOT / "Content/states/georgia/amphibian.yaml"
STATES = ROOT / "Content/states"
LOG_DIR = ROOT / "artifacts/fish-generation"

MODEL = "claude-opus-5-5"
INPUT_PER_MTOK, OUTPUT_PER_MTOK, CACHE_READ_PER_MTOK, CACHE_WRITE_PER_MTOK = 4.00, 20.00, 0.20, 5.00
SEARCH_PER_1K = 10.00
MAX_CONTINUATIONS = 5
LIST_LINK = " This profile appears in the [list of U.S. state fish](/symbols/fish)."
BANNED_WORDS = ["embodies", "tapestry", "testament", "vibrant", "delve", "boasts", "nestled",
                "Furthermore", "Moreover", "Additionally", "Notably"]

print_lock = threading.Lock()


def log(msg):
    with print_lock:
        print(msg, flush=True)


def api_key():
    key = os.environ.get("ANTHROPIC_API_KEY")
    if key:
        return key
    out = subprocess.run(["dotnet", "user-secrets", "list"], cwd=ROOT,
                         capture_output=True, text=True, encoding="utf-8").stdout
    for line in out.splitlines():
        name, _, value = line.partition(" = ")
        if name.strip() == "AiPipeline:Claude:ApiKey" and value.strip():
            return value.strip()
    sys.exit("No API key: set ANTHROPIC_API_KEY or AiPipeline:Claude:ApiKey in user-secrets.")


def file_stem(entry):
    return entry["file"].removesuffix(".yaml")


def hero_path(entry):
    s = entry["state_slug"]
    return f"/images/fish/{s}/{s}-{file_stem(entry)}-hero.webp"


def build_user_message(entry, today):
    others = [e for e in ALL if e["state_slug"] == entry["state_slug"] and e is not entry]
    other_text = "; ".join(f'{o["designation"]}: {o["name"]}' for o in others) or "none"
    record = {k: entry[k] for k in ("state", "state_fips", "designation", "name", "binomial_name", "adopted_year")}
    return (
        "Write the page for this state fish designation.\n\n"
        f"Starting record (verify it):\n{json.dumps(record, ensure_ascii=False, indent=1)}\n\n"
        f"Other fish designations of this state (do not write about them beyond one sentence): {other_text}\n\n"
        "Fixed values to copy exactly:\n"
        f'date_published: "{today}"\ndate_modified: "{today}"\n'
        f"hero_image: {hero_path(entry)}\n\n"
        "Style reference only (a finished page about a different symbol type; match its tone and density, "
        "not its keys):\n\n" + STYLE_EXAMPLE.read_text(encoding="utf-8")
    )


def extract_yaml(text):
    text = re.sub(r"^```(?:yaml)?\s*|\s*```\s*$", "", text.strip(), flags=re.M)
    start = text.find("# verification")
    if start < 0:
        start = text.find("type: State Fish")
    if start < 0:
        raise ValueError("no YAML found in response")
    return text[start:].strip() + "\n"


def validate(doc, raw):
    problems = []
    for key in ("type", "state", "name", "binomial_name", "seo_title", "seo_description",
                "intro_text", "sections", "faq", "sources"):
        if not doc.get(key):
            problems.append(f"missing {key}")
    if doc.get("type") != "State Fish":
        problems.append("type is not State Fish")
    if len(doc.get("seo_title", "")) > 60:
        problems.append(f"seo_title {len(doc['seo_title'])} chars")
    if len(doc.get("seo_description", "")) > 160:
        problems.append(f"seo_description {len(doc['seo_description'])} chars")
    body = raw.split("\nsources:")[0]
    if "—" in body or " -- " in body:
        problems.append("em dash in text")
    for word in BANNED_WORDS:
        if re.search(rf"\b{word}\b", body):
            problems.append(f"banned word {word}")
    ids = [s.get("id") for s in doc.get("sections") or []]
    for needed in ("overview", "about", "selection", "location", "facts"):
        if needed not in ids:
            problems.append(f"missing section {needed}")
    return problems


def finalize(raw, entry):
    """Force fixed fields and the list link without disturbing the model's formatting."""
    doc = yaml.safe_load(raw)
    doc["hero_image"] = hero_path(entry)
    doc["state_fips"] = entry["state_fips"]
    doc["author"] = "USA Symbol Team"
    intro = doc.get("intro_text", "").rstrip()
    if "/symbols/fish" not in intro:
        doc["intro_text"] = intro + LIST_LINK
    header = "\n".join(l for l in raw.splitlines() if l.startswith("# verification"))
    body = yaml.safe_dump(doc, sort_keys=False, allow_unicode=True, width=10_000)
    return (header + "\n" if header else "") + body, doc


def call_model(client, system, user_message):
    messages = [{"role": "user", "content": user_message}]
    usage = {"input": 0, "output": 0, "cache_read": 0, "cache_write": 0, "searches": 0}
    response = None
    for _ in range(MAX_CONTINUATIONS + 1):
        with client.beta.messages.stream(
            model=MODEL,
            max_tokens=32000,
            betas=["server-side-fallback-2026-07-01"],
            system=[{"type": "text", "text": system, "cache_control": {"type": "ephemeral"}}],
            tools=[{"type": "web_search_20260209", "name": "web_search", "max_uses": 8}],
            output_config={"effort": "medium"},
            messages=messages,
            extra_body={"fallbacks": "default"},
        ) as stream:
            response = stream.get_final_message()
        u = response.usage
        usage["input"] += u.input_tokens or 0
        usage["output"] += u.output_tokens or 0
        usage["cache_read"] += getattr(u, "cache_read_input_tokens", 0) or 0
        usage["cache_write"] += getattr(u, "cache_creation_input_tokens", 0) or 0
        stu = getattr(u, "server_tool_use", None)
        usage["searches"] += (getattr(stu, "web_search_requests", 0) or 0) if stu else 0
        if response.stop_reason == "pause_turn":
            messages = [{"role": "user", "content": user_message},
                        {"role": "assistant", "content": response.content}]
            continue
        break
    if response.stop_reason == "refusal":
        raise RuntimeError(f"refused: {response.stop_details}")
    if response.stop_reason == "max_tokens":
        raise RuntimeError("hit max_tokens")
    text = "".join(b.text for b in response.content if b.type == "text")
    return text, usage


def cost(u):
    return (u["input"] * INPUT_PER_MTOK + u["output"] * OUTPUT_PER_MTOK
            + u["cache_read"] * CACHE_READ_PER_MTOK + u["cache_write"] * CACHE_WRITE_PER_MTOK) / 1e6 \
        + u["searches"] * SEARCH_PER_1K / 1000


def generate(client, system, entry, today, force):
    out = STATES / entry["state_slug"] / entry["file"]
    label = f'{entry["state"]} / {entry["designation"]} / {entry["name"]}'
    if out.exists() and not force:
        return label, "skipped", 0.0
    user_message = build_user_message(entry, today)
    last_problem = None
    for attempt in (1, 2):
        try:
            text, usage = call_model(client, system, user_message)
        except anthropic.APIError as e:
            return label, f"API error: {e}", 0.0
        except RuntimeError as e:
            return label, str(e), 0.0
        LOG_DIR.mkdir(parents=True, exist_ok=True)
        (LOG_DIR / f'{entry["state_slug"]}-{file_stem(entry)}.raw.txt').write_text(text, encoding="utf-8")
        try:
            raw = extract_yaml(text)
            doc = yaml.safe_load(raw)
            problems = validate(doc, raw)
        except (ValueError, yaml.YAMLError) as e:
            problems = [f"unparseable: {e}"]
        if not problems:
            final, doc = finalize(raw, entry)
            out.parent.mkdir(parents=True, exist_ok=True)
            out.write_text(final, encoding="utf-8")
            verification = next((l for l in raw.splitlines() if l.startswith("# verification")), "")
            return label, f"ok ${cost(usage):.2f} {verification}", cost(usage)
        last_problem = "; ".join(problems)
        user_message += f"\n\nYour previous draft had these problems, fix them: {last_problem}"
        log(f"  retry {label}: {last_problem}")
    return label, f"FAILED validation: {last_problem}", 0.0


def main():
    global ALL
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", help="comma-separated state slugs")
    ap.add_argument("--limit", type=int)
    ap.add_argument("--force", action="store_true")
    ap.add_argument("--workers", type=int, default=4)
    args = ap.parse_args()

    ALL = json.loads(DATA.read_text(encoding="utf-8"))
    todo = ALL
    if args.only:
        wanted = set(args.only.split(","))
        todo = [e for e in todo if e["state_slug"] in wanted]
    if args.limit:
        todo = todo[: args.limit]

    client = anthropic.Anthropic(api_key=api_key(), max_retries=4, timeout=900)
    system = PROMPT.read_text(encoding="utf-8")
    today = dt.date.today().isoformat()
    total = 0.0
    with cf.ThreadPoolExecutor(args.workers) as ex:
        futures = [ex.submit(generate, client, system, e, today, args.force) for e in todo]
        for f in cf.as_completed(futures):
            label, status, c = f.result()
            total += c
            log(f"{label}: {status}")
    log(f"Done. {len(todo)} entries, total cost ${total:.2f}")


ALL = []

if __name__ == "__main__":
    main()
