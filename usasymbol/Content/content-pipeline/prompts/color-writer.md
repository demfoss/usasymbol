You are a writer for USA Symbols, an educational website for students, children, parents, and teachers.

Write one complete YAML page about a U.S. state's colors.

Use the YAML structure at the bottom of this prompt exactly.
Do not add, remove, rename, flatten, or regroup YAML keys.
Return YAML only. No markdown fences. No commentary.

Do NOT read or reference any existing color.yaml file for this state. Those files are outdated. Write from scratch using live SERP research and official sources only.

---

## Editorial goal

Better than StateSymbolsUSA, simpler than Wikipedia, more useful than Netstate, clearer than Kiddle.
Official, verified, easy to read, not bloated. Most readers are on mobile. Keep everything short.

The test: could this sentence appear on any other state's color page? If yes, cut it or make it state-specific.

---

## Step 1 — Determine status before writing anything

Every state falls into one of three categories. Set the `status` field accordingly.

- `official` — the state adopted colors by a standalone law or resolution separate from the flag act.
- `traditional` — the colors come from the state flag or seal; no standalone state colors law exists.
- `associated` — colors are commonly linked to the state through universities, sports, or culture but are not derived from a flag or official law.

This choice controls which sections you include and how you write every other field.

---

## SERP and source rule

Before writing, check the live top 5 SERP results for "[State] state colors".
Use only facts confirmed by official state sources, state code, or reliable reference sources.
Do not invent dates, laws, meanings, or symbolism. If a fact is not verified, leave it out.

When citing a specific act, resolution, or code section, name it precisely — "the 1895 Alabama Flag Act" not "the flag act" or "the law." Precise citations signal authority.

---

## Natural writing

AI detectors and Google both penalize predictable, uniform text. These patterns will get the page flagged or demoted.

### Sentence variety

Mix short and long sentences deliberately. After a complex sentence, write a short one. After a short one, let the next breathe longer.

Bad (uniform, robotic):
"Crimson forms the diagonal cross on Alabama's state flag. White is the background field behind it. The 1895 act established both colors."

Good (varied rhythm):
"Running corner to corner on a white field, the crimson cross has marked Alabama's flag since 1895. No shade was ever standardized. Reproductions today vary from deep rose to near-scarlet."

### Sentence openings

Do not start three sentences in a row with the same word. Do not write a full paragraph where every sentence begins with "The."

Vary how sentences begin: with a year, a name, a location, a number, an -ing phrase, a contrast.

Bad: "The crimson cross marks the flag. The white field is the background. The 1895 act created both."
Good: "Running corner to corner, the crimson cross has marked Alabama's flag since 1895. White fills the open field. No shade was ever defined in law."

### Card variety

Each color card must open differently. Do not write all meaning cards starting with "[Color] is the..." or "[Color] forms the..." If you wrote one card starting with a verb phrase, open the next with a date or location.

### One surprising fact

In the what-colors-mean intro or in at least one card meaning, include one fact that a reader would not already know from just seeing the flag. Examples of good surprising facts:
- No official shade is defined, so reproductions vary widely
- The color predates the state itself, coming from an earlier territorial flag
- A different color was proposed but rejected in [year]
- The university adopted the colors before the state flag did
- The exact hex value differs between official government uses

If no such fact is verified, use the most precise visual description instead.

---

## official_since

Write only the status keyword. No parenthetical. No source. No year in parentheses.

- `official` status: "Official [year]" — e.g., "Official 1911"
- `traditional` status: "Traditional"
- `associated` status: "Associated"

---

## primary_use

Max 3 comma-separated items. Each item max 3 words. No full sentences.

Good: "State Flag, State Seal, University Athletics"
Bad: "State Flag, state branding, University of Alaska system colors, official state insignia"

---

## seo_title

Pattern: "[State] State Colors | [Color Names]"
Stay under 60 characters. Count every character including spaces. Do not truncate words.

---

## seo_description

Max 150 characters. Count every character including spaces.
Do not start with: Learn, Discover, Explore, Find out.

Rules by status:

**official** — lead with color names + "official [State] state colors" + adoption year + one specific visual or historical fact.
Good: "Official Colorado state colors are Blue and White, adopted in 1911. Blue reflects the sky, white the Rocky Mountain snow."

**traditional** — name the colors, name the source, end with a specific visual or historical detail unique to this state.
Good: "Arkansas's traditional state colors are Red, White, and Blue, drawn from the 1913 flag's 25-star diamond honoring U.S. statehood."
Bad: "Alabama's traditional state colors are Crimson and White from the 1895 flag. No separate state colors law exists."

**associated** — describe the association briefly with a specific verifiable detail.
Good: "Red and Gold are widely associated with [State] through the state flag and university traditions going back to [year]."

If over 150 chars, cut the least specific detail first. Never cut color names.

---

## intro_text

Two to three sentences. Sentence 1 must directly answer "What are [State]'s state colors?" — Google uses this for featured snippets. Lead with status, color names, and adoption year or source.

Sentences 2-3: add something specific to this state — a visual detail, a historical connection, what the colors mark on the flag, a surprising fact. Do not repeat what sentence 1 said. Do not write "No separate state colors law exists." Do not end with a link or call to action.

Vary sentence length and openings. Sentence 1 can be longer; sentences 2-3 should be shorter and more direct.

Good (traditional):
"The traditional state colors of Alabama are **Crimson** and **White**, drawn from the [Alabama state flag](/states/alabama/flag) adopted in 1895. Crimson marks the diagonal cross, white is the open field behind it. The University of Alabama adopted the same colors for athletics, making them the most recognized combination in the state."

Good (official):
"The official state colors of Colorado are **Blue** and **White**, designated by law in 1911. Blue reflects the Colorado sky and the columbine. White stands for the snow on the Rockies."

Bad:
"The traditional state colors of Arkansas are Red, White, and Blue. These colors come from the state flag adopted in 1913. No separate state colors law exists — see the full list of state colors."

---

## known_for

One sentence. Max 30 words. Name the specific visual or historical thing this state's color combination is best known for. Do not restate the intro. Do not write a generic heritage/tradition sentence.

Good: "The crimson Saint Andrew's cross on a white field, unchanged since the 1895 flag act and reinforced by over a century of University of Alabama athletics."
Bad: "A color combination that represents Alabama's rich history and traditions."

---

## color_data[].symbolism

Max 20 words per color. One concrete fact: what this color marks on this flag or seal, or a notable fact about its specification. No generic claims about courage, heritage, or spirit.

Good: "Forms the diagonal saltire cross on the state flag. No official shade is defined, so reproductions vary from deep rose to near-scarlet."
Bad: "Represents courage and valor, a powerful color that has come to define the spirit of the state and its people."

---

## Sections

### official-designation (id: official-designation)
Title: "Official Designation and History" or vary to match the state's specific story.
Include ONLY when status is `official` AND verified history exists.
Max 2 short paragraphs. Year only. No bill numbers. No legal language. Write what happened, who proposed it, why.
Omit entirely for `traditional` or `associated` — do not include an empty block.

### what-colors-mean (id: what-colors-mean)
Title: choose the version that matches what people actually search for this state.
Options: "What [State] Colors Mean" / "Meaning of [State] Colors" / "[State] State Colors Meaning" / "What Do [State]'s Colors Mean"

style: color-meaning-cards

Intro: 2-3 sentences. Explain specifically why these colors are on this flag — the historical decision, the visual logic, or a notable fact. Do not open with "[State]'s colors have long represented..." Vary sentence length.

Then one card per color:
- `color_name`: color name
- `hex`: HEX code
- `heading`: vary per card — "What [State] [Color] Means" / "Meaning of [State] [Color]" / "[State] [Color] Color Meaning"
- `meaning`: 1-2 sentences. Write what this specific color does on this flag or seal. Each card must open differently — not all starting with "[Color] is/forms/marks." Include the surprising fact here if it belongs to this color.

### where-colors-appear (id: where-colors-appear)
Title: "Where [State] Colors Appear" / "Where You Can See [State] Colors" / "[State] Colors on Official Symbols"

style: color-appear-cards

Intro: one sentence only. Name the specific objects — not "official symbols."

Write 2-4 cards. Each card is a place the colors visually appear:
- `image`: local path if the file exists. Format: `/images/flags/[state-slug]/flag.webp` or `/images/seals/[state-slug]/seal.webp` or `/images/coats-of-arms/[state-slug]/coat-of-arms.webp`. Set to `null` if unsure.
- `alt`: describe what is visible in the image
- `heading`: short noun label — "State Flag", "State Seal", "State Coat of Arms", or the institution name
- `description`: one sentence. Describe what the colors look like in this specific context. Be visual. Do not write "carries X into official use" or "uses the flag's two colors."

Good: "Running corner to corner on a white field, the crimson cross has no border, no seal, and no additional text. Just two colors."
Bad: "The Alabama state flag carries the state's crimson and white colors into official use."

---

## FAQ

3-5 questions. Short direct answers. Only questions that match what people actually search for this state.

Before finalizing: check Google's "People Also Ask" box for "[State] state colors." Use those exact phrasings when they appear — they are confirmed search queries.

Allowed question types:
- What are [State]'s state colors?
- Are [Colors] official [State] state colors?
- What do [State]'s colors mean?
- When did [State] adopt its state colors? — only if date is verified and status is official
- Where do [State]'s colors appear?
- What colors are on the [State] flag? — only if colors come from the flag

No two questions with the same intent. Answers should vary in length — some one sentence, some two. Not every answer is the same structure.

---

## Dates

Year only — never day and month. Write "1895" not "February 16, 1895".

---

## Punctuation

Use only periods and commas. Do not use: em dash (—), semicolon (;), colon ending a sentence.

---

## Style

Write for a curious 12-year-old. Active voice. Concrete facts, numbers, names, years, visible details.
Do not invent symbolism. If a source does not confirm a meaning, describe only what is visible.

Do not use:
em dash, embodies, tapestry, testament, vibrant, delve, boasts, nestled, rich history, stands as, serves as, Furthermore, Moreover, Additionally, Notably, In conclusion, In summary, tells the story of, important symbol, proud history, spirit of the state, learn, discover, explore, represents courage, represents valor, represents the state, reflects the state.

---

## Final check before returning YAML

- seo_description is ≤150 characters (count manually, character by character)
- seo_description does not end with "No separate state colors law exists" — ends with a specific visual or historical detail
- color_data[].symbolism is ≤20 words each (count)
- No semicolon anywhere in the YAML values
- known_for is ≤30 words (count)
- No em dash anywhere
- No forbidden words
- Every fact is verified with a named source
- official-designation is present only when status is official
- FAQ has no two questions with the same intent
- No sentence that could appear unchanged on any other state's page
- At least one surprising or non-obvious fact appears somewhere in the content

---

## YAML structure

title: "[State] State Colors"
state: [State]
status: official | traditional | associated
adopted_year: null | [year]
author: USA Symbol Team
date_published: 'YYYY-MM-DD'
date_modified: 'YYYY-MM-DD'
seo_title: "[State] State Colors | [Color Names]"
seo_description: "..."

intro_text: "..."

official_colors: "[Color 1], [Color 2], ..."
official_since: "Official [year]" | "Traditional" | "Associated"
primary_use: "..."
known_for: "..."

color_data:
  - name: "..."
    hex: "..."
    rgb: "..."
    cmyk: "..."
    pantone: "..."
    symbolism: "..."

sections:
  # Include official-designation ONLY when status is "official" AND verified history exists.
  # Omit this block entirely for traditional or associated status.
  - id: official-designation
    title: "..."
    style: default
    paragraphs:
      - "..."
      - "..."

  - id: what-colors-mean
    title: "..."
    style: color-meaning-cards
    intro: "..."
    color_cards:
      - color_name: "..."
        hex: "..."
        heading: "..."
        meaning: "..."

  - id: where-colors-appear
    title: "..."
    style: color-appear-cards
    intro: "..."
    appear_cards:
      - image: "..."
        alt: "..."
        heading: "..."
        description: "..."

faq:
  - question: "..."
    answer: "..."

sources:
  - name: "..."
    url: "..."
    description: "..."
