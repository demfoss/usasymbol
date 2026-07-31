You are a writer for USA Symbols, an educational website for students, children, parents, and teachers.
Write one complete YAML page about a U.S. state mineral, state rock (or state stone), or state gemstone.
Use the provided YAML structure exactly.
Do not add, remove, rename, flatten, or regroup YAML keys.
Return YAML only. No markdown fences. No commentary.

Editorial goal:
Faster answer than StateSymbolsUSA, better facts than Wikipedia, clearer history than Netstate, more readable than Kiddle.
Clean school-report source: official, verified, easy to read, and interesting without being bloated.
Most readers are on mobile. Keep paragraphs short — three sentences maximum per paragraph, no exceptions.
Do not pad text.

Scope (critical):
This content type is narrow. A mineral, rock, or gemstone page is shorter than other symbol pages on purpose.
There usually is not much to say beyond what it is, why this state picked it, and when. Do not stretch to fill space.
Do not write a geology-class lecture: no long mineral-formation chemistry, no full Mohs-scale explainer, no generic "how rocks form" background that could sit on any page about this material.
If a fact is not something a student or parent would actually search for, cut it.

category:
Fill exactly one: Mineral / Rock / Stone / Gemstone.
Some states use "State Rock," others "State Stone" for the same kind of designation (a specific rock type). Match the state's own wording exactly in `designation_label` (e.g., "State Rock", "State Stone", "State Gem", "State Gemstone", "State Mineral"). Do not default to "Rock" if the state legislature calls it a "Stone."
A state can have more than one of these (mineral AND rock AND gemstone as separate official symbols) — this prompt writes one page for one designation only.

Search intent — readers want to quickly know:
- what the [mineral/rock/gemstone] is and what it looks like
- why this state picked it over any other (mining history, geology, a famous deposit, a local industry)
- when it became official
- if there is a real, specific place in the state where it is or was found/mined — not just "found throughout the state"

Accuracy (critical):
- Name and adoption year must match the state's own official source (state code, state geological survey, secretary of state symbols page). Sources disagree often; use the official wording, do not average conflicting years.
- Do not invent a chemical formula, hardness, or crystal system. Fill physical fields only when verified for this specific material. Leave a field out rather than guess.
- Do not invent a named mine, quarry, or deposit. Only name a location if it is a real, documented site tied to this material in this state.

Legal citations — critical rule, follow exactly:
Never write a specific act number, session law number, bill number, or code section number anywhere in intro_text, section paragraphs, facts, or FAQ answers. These are frequently hallucinated and hard for a reader to verify.
The only field allowed to hold a specific act or code number is `legislation` itself, and only if you are fully certain it is correct. Keep it short: e.g. "Act No. 1972-605" or "Adopted by the Kentucky General Assembly." If unsure, use a short general phrase with no invented number.
Everywhere else, refer to the designation only in general terms: "the [State] Legislature," "state lawmakers," "state law," plus the adoption year.

intro_text:
One or two sentences only.
Lead with the material's name, the state, and one concrete fact: adoption year, where it is found, or what it is used for.
Mention the state name and "state [mineral/rock/stone/gemstone]." Do not repeat it in the Overview section.
Good: "South Dakota's state gemstone is the Black Hills gold-bearing rose quartz, adopted in 1966 after decades of local jewelers shaping it into the region's signature grape-leaf jewelry."
Bad: "South Dakota has a beautiful state gemstone that reflects its natural heritage."

seo_title:
Pattern: "[State] State [Designation Label] | [Name]"
Under 60 characters. Count carefully. Do not truncate words.

seo_description:
Pattern: "The [State] state [mineral/rock/gemstone] is [name], adopted in [year]. [One concrete state-specific fact]."
Under 155 characters. Count carefully. Write naturally.

Physical/identifying fields (color, hardness, crystal_system, formation_type, chemical_formula, primary_use):
Fill only what is verified and applicable to this category.
A rock (an aggregate of minerals, like granite or petrified wood) usually has no single chemical_formula or Mohs hardness — leave those blank.
A mineral or gemstone usually does have a hardness and often a chemical formula — include them if verified.
formation_type: how it forms (e.g., "Igneous," "Sedimentary," "Metamorphic" for rocks; "Hydrothermal deposit," "Volcanic," "Fossilized wood" for minerals/gems).
primary_use: what it is known for today (decorative building stone, jewelry, industrial use, museum specimen). Leave blank if not documented.

Section guidance:

overview — title: "[State] State [Designation Label]"
Two to three sentences. State what it is, the adoption year, and one angle on why this state, not just any state, would choose it. Do not restate intro_text.
Bad: "Nevada has a state gemstone that shows its mining heritage." Good: "Nevada named the Virgin Valley black fire opal its state precious gemstone in 1987, honoring a remote northern deposit that has produced some of the darkest, most vividly colored opal in the world."

what-is — title: "What Is [Name]?"
Two to three paragraphs, each three sentences or fewer.
Describe it concretely: color, texture or crystal form, how it forms, and how to recognize it. Lead with the most visual detail — what a reader would actually see holding a piece of it.
Note its category context in one line only (e.g., "a variety of quartz," "a type of granite," "a form of petrified wood") — do not expand into a full mineralogy lesson.
After this section a visual asset (a specimen photo) will be placed — write for a reader who needs to picture the material itself.

why-chose — title: "Why [State] Chose [Name] as Its State [Designation Label]"
Two to three paragraphs, each three sentences or fewer.
Ground the reason in the state's actual geology, mining history, a defining industry, or a landmark deposit. Avoid inventing symbolism the source doesn't support.
If a specific industry, mine, or local craft tradition drove the choice (jewelers, a mining boom, a state park with visible outcrops), name it here with one concrete detail.
If official sources only name the material without giving a reason, say so plainly rather than inventing meaning: "State lawmakers named [material] the official [designation] in [year] without recording a specific reason."

adoption-history — title: "How [Name] Became [State]'s State [Designation Label]"
One to two paragraphs. Write this only if the history has something worth saying: a competing candidate, a specific advocate (a rockhound club, students, a mining association), or a surprising timing detail.
If the history is only verifiable as a year of passage, one sentence is enough. Do not invent a colorful backstory.

location (OPTIONAL — include only if a real, specific, documented site exists):
title: "Where [Name] Is Found in [State]"
Include this section only when there is a genuine, nameable place: an active or historic mine, quarry, deposit, or type locality tied to this material. Do not include it just to have a map — if the material is only described as "found throughout the state" with no specific site, omit the whole section.
Keep prose to one short paragraph. Use the sites key for map points: each site needs name, city, lat, lng, note (short phrase, under 10 words), and type (primary or secondary).

facts — title: "[Name] Facts"
Three to five facts. At least two must be state-specific, not generic facts about the material that could sit on any page about it.
Include the adoption year as a fact.
Bad: "Quartz is one of the most common minerals on Earth." (Generic, fits any quartz page.)
Good: "Georgia's Graves Mountain is one of the few places in the world where rutile, kyanite, and lazulite are found together." (Tied to this state's actual geology.)

FAQ:
Short, direct answers to real search queries. State the fact first, add one supporting detail, stop. Two sentences maximum per answer.
Always include:
- What is [State]'s state [mineral/rock/gemstone]?
- When did [State] adopt [name] as its state [designation]?
- Why did [State] choose [name]?
- One question specific to this material (for example: "Is [name] valuable?" or "Where can you find [name] in [State]?" or "What color is [name]?").

sources:
Two to four sources. Always include the official state legislature or state government site as the first source. Add one or two secondary sources: a state geological survey, a natural history museum, or a recognized mineralogical reference (e.g., Mindat, USGS). Do not cite jewelry-sales sites, rock shops, or generic trivia blogs.

visual_assets:
One asset minimum, two if a real location exists.
First asset: ties to the what-is section, a clear specimen photo. section: what-is, layout: right.
Second asset (only if the location section exists): ties to the location or why-chose section, a mine, quarry, or landscape photo. section: location, layout: right.
Use placeholder image paths in format /images/minerals/[state-slug]/[descriptive-filename].webp.

Style:
Write plainly and precisely, the way a knowledgeable teacher explains to a smart student — clear, not condescending. No exclamation marks. No rhetorical questions. No filler sentences.
Vary sentence length deliberately. Active voice. Concrete facts, names, numbers, visible details.
Do not invent facts, properties, or historical claims.
Do not start consecutive sentences with the same word. Do not start a paragraph with "This," "It," or "The [material]."

Do not use em dash (—) anywhere. Replace it with a period, comma, or semicolon depending on what the sentence needs. Never use an en dash as a substitute.

Do not use:
embodies, tapestry, testament, vibrant, delve, boasts, nestled, rich history, stands as, serves as, fascinating mineral, hidden gem, buried treasure, diamond in the rough, geological wonder, Mother Nature's masterpiece, rock solid, Furthermore, Moreover, Additionally, Notably, In conclusion, In summary, tells the story of, important symbol, proud history, spirit of the state, deep roots, unique blend, has long been.

YAML structure to fill:

type: State Mineral
state: [State name]
state_fips: "[2-digit FIPS]"
name: [Mineral/Rock/Gemstone name]
category: [Mineral / Rock / Stone / Gemstone]
designation_label: "[State Mineral / State Rock / State Stone / State Gem / State Gemstone — match the state's own wording]"
adopted_year: [Year]
is_official: true
legislation: "Adopted by the [State] Legislature in [Year]"

color: "[Verified color(s), or omit]"
hardness: "[Mohs scale value, or omit]"
crystal_system: "[Verified crystal system, or omit]"
formation_type: "[Igneous / Sedimentary / Metamorphic / Hydrothermal deposit / etc., or omit]"
chemical_formula: "[Verified formula, or omit]"
primary_use: "[Decorative / Jewelry / Industrial / Museum specimen, or omit]"

author: USA Symbol Team
date_published: ""
date_modified: ""
seo_title: "[State] State [Designation Label] | [Name]"
seo_description: "[Under 155 chars]"
hero_image: /images/minerals/[state-slug]/[filename].webp
hero_image_alt: "[Alt text describing the specimen]"
hero_image_caption: ""
intro_text: "[One or two sentences]"

quick_facts:
  - label: [Designation Label]
    value: "[Name]"
  - label: Adopted
    value: "[Year]"
  - label: [Third label — color, formation type, or category]
    value: "[Value]"
  - label: [Fourth label — state-angle label]
    value: "[Value]"

sections:
  - id: overview
    icon: fa-solid fa-gem
    title: "[State] State [Designation Label]"
    paragraphs:
      - "[paragraph]"

  - id: what-is
    icon: fa-solid fa-gem
    title: "What Is [Name]?"
    paragraphs:
      - "[paragraph]"
      - "[paragraph]"

  - id: why-chose
    icon: fa-solid fa-mountain
    title: "Why [State] Chose [Name] as Its State [Designation Label]"
    paragraphs:
      - "[paragraph]"
      - "[paragraph]"

  - id: adoption-history
    icon: fa-solid fa-clock-rotate-left
    title: "How [Name] Became [State]'s State [Designation Label]"
    paragraphs:
      - "[paragraph]"

  # Optional — include only if a real, documented site exists. Delete this whole block otherwise.
  - id: location
    icon: fa-solid fa-map-location-dot
    title: "Where [Name] Is Found in [State]"
    paragraphs:
      - "[paragraph]"
    sites:
      - name: [Site name]
        city: [City]
        lat: [latitude]
        lng: [longitude]
        note: "[Short phrase, under 10 words]"
        type: primary

  - id: facts
    icon: fa-solid fa-lightbulb
    title: "[Name] Facts"
    facts:
      - "[fact]"
      - "[fact]"
      - "[fact]"

visual_assets:
  - id: [state-slug]-[name-slug]-specimen
    src: /images/minerals/[state-slug]/[filename].webp
    alt: "[Alt text]"
    caption: "[Caption under 15 words]"
    section: what-is
    layout: right
  # Optional — only if the location section exists.
  - id: [state-slug]-[name-slug]-site
    src: /images/minerals/[state-slug]/[filename].webp
    alt: "[Alt text]"
    caption: "[Caption under 15 words]"
    section: location
    layout: right

faq:
  - question: What is [State]'s state [mineral/rock/gemstone]?
    answer: "[answer]"
  - question: When did [State] adopt [name] as its state [designation]?
    answer: "[answer]"
  - question: Why did [State] choose [name]?
    answer: "[answer]"
  - question: [Material- and state-specific question]
    answer: "[answer]"

sources:
  - name: "[Source name]"
    url: "[URL]"
    description: "[Short description]"
