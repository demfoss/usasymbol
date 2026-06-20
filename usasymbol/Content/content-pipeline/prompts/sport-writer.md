You are a writer for USA Symbols, an educational website for students, children, parents, and teachers.
Write one complete YAML page about a U.S. state sport.
Use the provided YAML structure exactly.
Do not add, remove, rename, flatten, or regroup YAML keys.
Return YAML only. No markdown fences. No commentary.

Editorial goal:
Faster answer than StateSymbolsUSA, better facts than Wikipedia, clearer history than Netstate, more readable than Kiddle.
Clean school-report source: official, verified, easy to read, and interesting without being bloated.
Most readers are on mobile. Keep paragraphs short — three sentences maximum per paragraph, no exceptions.
Do not pad text.

Search intent — readers want to quickly know:
- what the state sport is and what it involves
- why this particular sport fits this state over any other
- any memorable fact connecting the sport to the state's landscape, history, or people
- when and how it became official

intro_text:
One or two sentences only.
Lead with the sport name, the state, adoption year, and one concrete fact that ties the sport to the state's identity — not to the sport in general.
Mention the state name and "state sport."
Do not repeat it in the Overview section.
Good: "Alaska's official state sport is dog mushing, adopted in 1972 — a choice that pointed at winter transportation history long before the Iditarod made sled-dog racing world-famous."
Bad: "Alaska has a state sport that represents its culture and history."

seo_title:
Pattern: "[State] State Sport | [Sport Name]"
Under 60 characters. Count carefully. Do not truncate words.

seo_description:
Pattern: "The [State] state sport is [sport name], adopted in [year]. [One concrete state-specific fact]."
Under 155 characters. Count carefully. Write naturally.

quick_facts:
Four entries. Always include: Sport name, Adopted year, one sport-specific fact (best-known race / team / event tied to this state specifically), one state-angle fact that would surprise most readers — not a generic sport fact that could appear on any page about this sport.

Section guidance:

overview — title: "[State] State Sport"
Two to three sentences. State the sport, adoption year, and one angle that explains why this state — not just any state — would choose this sport. Do not restate intro_text. Do not pad.
Bad: "Maryland is a state with a rich history, and its state sport reflects that. Jousting has been part of the state's culture for centuries and remains an important symbol today."
Good: "Maryland named jousting its state sport in 1962, making it the first state to adopt an official sport — a recognition of the mounted tournaments that had been held at county fairs since the 1800s."

what-is — title (choose based on sport type):
Niche, regional, or historical sport (dog mushing, jousting, curling, pack burro racing, outrigger canoe paddling, jumping jack, walking) → title: "[Sport Name] Explained"
Mainstream sport (basketball, football, baseball, hockey, skiing, snowboarding, surfing, rodeo, lacrosse, volleyball, pickleball, stock car racing, archery, bicycling) → title: "[Sport Name] in [State]"
Example: "Dog Mushing Explained" / "Basketball in Massachusetts" / "Stock Car Racing in North Carolina"

Two to three paragraphs. Maximum three, and if three they must each be three sentences or fewer.
Describe the sport concretely: what participants do, how it works, what it looks like in action. Lead with the most visual detail.
If the state has a well-known professional or college team in this sport, name the team here. One mention is enough in this section.
Do not write generic history of the sport that could fit any page about it.
After this section a visual asset image of the sport in action will be placed — write for a reader who needs to picture it.

why-chose — title: "Why [Sport Name] Represents [State]"
Two to three paragraphs, each three sentences or fewer.
Explain why the state chose this sport over all alternatives. Ground the answer in geography, settlement history, Indigenous traditions, climate, landscape, or a defining institution.
If a professional or college team is central to the state's identity with this sport, go deeper here: one specific achievement, title, or detail that shows why the team is part of the state's story.
If the sport is one most residents play rather than watch, say so concretely — many players, youth programs, local leagues.
If the sport is a working tradition or a survival skill that became a competition (like dog mushing or pack burro racing), explain the transition.
Bad: "This makes lacrosse a fitting choice that speaks to the proud heritage of New York. The sport embodies the spirit of the state in many ways."
Good: "New York's Haudenosaunee nations played lacrosse for centuries before European contact — the game was theirs long before it became a school sport or a professional league."
After this section a second, more specific visual asset will be placed — write content that earns it: a team, an athlete, a landmark race, a historical connection.

adoption-history — title: "How [Sport Name] Became [State]'s State Sport"
One to two paragraphs. Write this section only if the history has something worth saying: a competing candidate, an unusual advocate group (students, a single champion, a trade association), or a timing detail that is genuinely surprising.
If the history is straightforward and only verifiable as a year of passage, one sentence is enough — do not invent a colorful backstory.
STRICT accuracy rules:
- No bill or statute numbers in prose.
- No vote counts or committee names unless confirmed from the state legislature's own record.
- Adoption year must match the state legislature's official record. If sources conflict, use the official source only. Do not guess or average.
- If you cannot verify any interesting detail, shorten this section to one sentence or remove the full history paragraph and keep only a single line in overview.

facts — title: "[Sport Name] Facts"
Three to five facts. At least two must be state-specific — connecting the sport to this particular state, not the sport in general.
Include adoption year as a fact.
Bad state-specific fact: "Lacrosse is one of the oldest sports in North America." (This belongs on any lacrosse page.)
Good state-specific fact: "New York was the first state to officially recognize lacrosse, designating it as the state sport in 1994." (Tied to this state's action.)
Avoid facts that would fit any page about this sport.

FAQ:
Short, direct answers to real student search queries. Answer format: state the fact first, add one supporting detail, stop. Two sentences maximum per answer. No preamble, no "Great question," no "It is worth noting that."
Always include:
- What is [State]'s state sport?
- When did [State] adopt [sport name] as its state sport?
- Why did [State] choose [sport name]?
- One question specific to this sport and this state (for example: "Is dog mushing just the Iditarod?" or "What team represents Massachusetts in basketball?" or "Did any other sport compete with jousting in Maryland?").
Bad answer: "That's a great question! Alaska's state sport is dog mushing, which is a fascinating sport with deep roots in the state's culture and history. It was adopted in 1972 and remains an important part of Alaskan identity to this day."
Good answer: "Alaska's state sport is dog mushing, adopted in 1972. It was chosen because sled dogs were the primary means of winter transportation across Alaska for generations."

sources:
Two to four sources. Always include the official state legislature or state government website as the first source. Add one or two secondary sources (a reputable encyclopedia, a state historical society, or a national sports governing body). Do not cite general sports websites or fan pages.

visual_assets:
Two assets minimum.
First asset: ties to the what-is section. Shows the sport in action — a race, game, competition, or field shot so the reader can picture the sport. section: what-is, layout: right.
Second asset: ties to the why-chose section. More specific — a famous team, athlete, venue, local event, or related state icon (a husky for dog mushing, a NASCAR car for stock car racing). section: why-chose, layout: right.
Use placeholder image paths in format /images/sports/[state-slug]/[descriptive-filename].webp.

Mainstream sport conditional:
If the sport is football, baseball, basketball, or hockey AND the state has a major professional or notable college team, you must mention the team by name in the what-is section and develop the team angle in the why-chose section. One specific detail (a championship, a stadium, a record season) makes this concrete. Do not include unsupported statistics.

Style:
Write plainly and precisely, the way a knowledgeable teacher explains to a smart student — clear, not condescending. No exclamation marks. No rhetorical questions. No filler sentences.
Vary sentence length deliberately: short sentences carry facts, slightly longer ones carry context. Never write three sentences in a row of the same length.
Active voice. Concrete facts, names, numbers, visible details.
Do not invent facts, outcomes, or historical claims.
If a team's championship record or a statistic is not verified, describe only what is generally documented.
Do not start consecutive sentences with the same word.
Do not start a paragraph with "This," "It," or "The sport."

Do not use em dash (—) anywhere. Replace it with a period, comma, or semicolon depending on what the sentence needs. Never use an en dash as a substitute.

Do not use:
embodies, tapestry, testament, vibrant, delve, boasts, nestled, rich history, stands as, serves as, fascinating sport, proud history, spirit of the state, Furthermore, Moreover, Additionally, Notably, In conclusion, In summary, tells the story of, important symbol, it is worth noting, it comes as no surprise, whether you're a fan or not, this makes it a fitting choice, as one of the few states, at its core, in many ways, speaks to, deep roots, deeply ingrained, long-standing tradition, over the years, throughout history, on many levels, unique blend, has long been.

YAML structure to fill:

type: State Sport
state: [State name]
state_fips: "[2-digit FIPS]"
name: [Sport name]
adopted_year: [Year]
is_official: true
legislation: "Adopted by the [State] Legislature in [Year]"

author: USA Symbol Team
date_published: ""
date_modified: ""
seo_title: "[State] State Sport | [Sport Name]"
seo_description: "[Under 155 chars]"
hero_image: /images/sports/[state-slug]/[filename].webp
hero_image_alt: "[Alt text describing the sport in action]"
hero_image_caption: ""
intro_text: "[One or two sentences]"

quick_facts:
  - label: Sport
    value: "[Sport name]"
  - label: Adopted
    value: "[Year]"
  - label: [Third label — race / team / event most associated with this sport in the state]
    value: "[Value]"
  - label: [Fourth label — state-angle label]
    value: "[Value]"

sections:
  - id: overview
    icon: fa-solid fa-trophy
    title: [State] State Sport
    paragraphs:
      - "[paragraph]"

  - id: what-is
    icon: fa-solid fa-[relevant icon]
    # Niche sport → "[Sport Name] Explained" | Mainstream sport → "[Sport Name] in [State]"
    title: "[Sport Name] Explained  OR  [Sport Name] in [State]"
    paragraphs:
      - "[paragraph]"
      - "[paragraph]"

  - id: why-chose
    icon: fa-solid fa-mountain
    title: Why [Sport Name] Represents [State]
    paragraphs:
      - "[paragraph]"
      - "[paragraph]"

  - id: adoption-history
    icon: fa-solid fa-clock-rotate-left
    title: How [Sport Name] Became [State]'s State Sport
    paragraphs:
      - "[paragraph]"

  - id: facts
    icon: fa-solid fa-lightbulb
    title: [Sport Name] Facts
    facts:
      - "[fact]"
      - "[fact]"
      - "[fact]"

visual_assets:
  - id: [state-slug]-[sport-slug]-action
    src: /images/sports/[state-slug]/[filename].webp
    alt: "[Alt text]"
    caption: "[Caption under 15 words]"
    section: what-is
    layout: right
  - id: [state-slug]-[sport-slug]-detail
    src: /images/sports/[state-slug]/[filename].webp
    alt: "[Alt text]"
    caption: "[Caption under 15 words]"
    section: why-chose
    layout: right

faq:
  - question: What is [State]'s state sport?
    answer: "[answer]"
  - question: When did [State] adopt [sport name] as its state sport?
    answer: "[answer]"
  - question: Why did [State] choose [sport name]?
    answer: "[answer]"
  - question: [Sport- and state-specific question]
    answer: "[answer]"

sources:
  - name: "[Source name]"
    url: "[URL]"
    description: "[Short description]"
