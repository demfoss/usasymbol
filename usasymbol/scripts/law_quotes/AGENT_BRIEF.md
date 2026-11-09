# Law-quote pipeline: brief for agents

Goal: for every state-symbol YAML in `Content/states/<state>/**` add a **verbatim** quote of the law that designates
the symbol, with a link to the official source. The site renders it as a "From the source" block via the YAML keys
`expert_quote` (`text`, `source`) and `expert_quote_after_section`.

**A wrong quote is much worse than no quote. When unsure, skip.** The user explicitly allowed scripts for this
operation (see `AGENT_RULES.md` at repo root otherwise: no other bulk edits of Content/, no punctuation fixes, etc.).

## Rules
1. Only touch your assigned states. Write only: `registry/<state>.json`, `reports/<state>.json` (by the script),
   Content YAML through `insert.py`, and your log `logs/<your-name>.md`. Do NOT edit `extract.py`, `state_run.py`,
   `insert.py`, C# or views. If you find a tool bug, describe it in your log instead.
2. Never invent or "reconstruct" statute text. A quote must be a substring of a page the script fetched.
3. Do not commit, push, or run git write commands.
4. Sources: official legislature / revisor sites first. Reputable public mirrors (e.g. `*.public.law`) only if the
   official site cannot be fetched. `law.justia.com` returns 403 to scripts: do not use it. Do not use Wikipedia,
   statesymbolsusa, netstate, or usasymbol.com as quote sources.
5. Time-box: per state at most ~4 searches and ~6 URL attempts. If a state's code cannot be fetched as HTML, log it
   and move on. Many symbols were adopted by resolution/proclamation and are not in the code: no quote is correct.

## Per-state procedure (run from `scripts/law_quotes`, always `python -I`)
1. Find the page(s) of the state code that list the state symbols (usually one chapter, e.g. "State emblems").
   Use WebSearch/WebFetch to find URLs, then verify fetchability with curl
   (`curl -sL --ssl-no-revoke -A "Mozilla/5.0" -m 40 URL | head -c 2000`). JS-only pages return no text.
   Some states need one URL per section; list as many URLs as needed (they are fetched once each).
2. Create `registry/<state>.json` with the Write tool (not a shell heredoc):
   `{"code": "Fla. Stat.", "urls": ["https://...", "https://..."]}`. `code` is the citation prefix; the site shows
   `<code> § <section>`. Use the state's standard Bluebook-style short code name.
3. `python -I state_run.py <state>` prints a table and writes `reports/<state>.json`.
   Rows: `high` (all name words + designation word + symbol noun in one unique sentence, section heading found),
   `medium` (only partial name match or several designation words), `none`.
4. REVIEW every `high` and `medium` row by reading the quote:
   - the sentence must designate exactly this symbol (right species/item, right state, e.g. not "state bird" for a
     mammal file, not a different cultivar/breed, not a repealed or replaced designation);
   - the `cite` must be the section that contains the sentence;
   - `yaml_cite_match=False` means the YAML's own `legislation` cite differs from the statute heading: investigate and
     list it in your log as a possible YAML error (do not edit the YAML text).
   For correct `medium` rows set `"approved": true` in `reports/<state>.json` (edit the JSON with the Edit tool).
   For wrong `high` rows set `"confidence": "none"`.
5. Dry run: `python -I insert.py reports/<state>.json` then `python -I insert.py reports/<state>.json --apply`.
   `insert.py` inserts only high/approved rows, skips files that already have `expert_quote`, picks the section, and
   re-parses the YAML afterwards.
6. Log in `logs/<your-name>.md`: per state: URLs used, counts (high/medium/approved/inserted/none), states or symbol
   types that could not be done and why, and possible factual errors you noticed on our pages (file + what the law says).

## Final answer
A compact table: state, files, inserted, skipped, blocker. Plus the list of possible YAML errors (file, issue).
