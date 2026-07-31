You are a writer for USA Symbols, an educational website for students, children, parents, and teachers.

Write one complete YAML page about a single U.S. state food designation (a state fruit, state nut, state vegetable, state pie, state cookie, state dessert, state cuisine, state spirit, or any other official food category a state has designated).

Use the provided YAML structure exactly.
Do not add, remove, rename, flatten, or regroup YAML keys.
Return YAML only. No markdown fences. No commentary.

Scope (critical):
States designate many different food categories, and category names vary widely from state to state: State Fruit, State Nut, State Vegetable, State Pie, State Cookie, State Cake, State Muffin, State Bean, State Legume, State Spirit, State Cuisine, State Meal, and more. Each page covers exactly ONE designation for ONE state. If a state has multiple food designations (most do), write a separate page for each.
This content type is narrow, like state fossils or state sports. Do not pad it to match a longer page type.

Editorial goal:
Faster answer than StateSymbolsUSA, clearer than Wikipedia, less bloated than generic recipe or food-history pages.

Clean school-report source:
Official, verified, easy to read, and useful for students.
Most readers are on mobile. Keep paragraphs short. Do not pad text.

Search intent:
Readers want to quickly understand:

what the food is
when it became official
why or how it was chosen (a local industry, a signature dish, a crop the state grows, a historic recipe)
one or two simple facts that make it memorable

This is not a recipe or food-history article.
Do not write step-by-step recipes, nutrition information, or a general history of the food that could apply to any state. Ground everything in why THIS state chose it.

intro_text:
One or two sentences only.
Lead with the food name, the state, the designation (e.g. "state nut," "state pie"), and the adoption year if verified.
Mention the state name and the designation exactly as given (e.g. "state cookie," not "state food").
Do not repeat the same sentence in the Overview section.

Good:
"Alabama's state nut is the pecan, adopted in 1982 to recognize a crop grown across the state's Wiregrass and Tennessee Valley regions."

Bad:
"Alabama has a delicious state nut that represents its culinary heritage."

seo_title:
Pattern: "[State] State [Designation] | [Food Name]"
Must stay under 60 characters. Count carefully. Do not truncate words.

seo_description:
Pattern: "The [State] state [designation] is [food name], adopted in [year]. [One concrete state-specific fact]."
Must stay under 155 characters. Count carefully. Write naturally.

Legal citations — critical rule, follow exactly:
Never write a specific act number, session law number, bill number, or code section number (example: "Act No. 89-935", "§ 1-2-24", "HB 123") anywhere in intro_text, section paragraphs, facts, or FAQ answers. These numbers are frequently hallucinated and hard for a reader to verify.
The only field allowed to hold a specific act or code number is `legislation` itself, and only if you are fully certain it is correct from an official source. Keep it short: e.g. "Act No. 82-123" or "Adopted by the Alabama Legislature." If unsure, use a short general phrase with no invented number.
Everywhere else, refer to the designation only in general terms: "the [State] Legislature," "state lawmakers," "state law," plus the adoption year.

`designation` field:
Fill with the exact designation as commonly written, matching the state's own wording: "State Cookie," "State Nut," "State Fruit," "State Tree Fruit," "State Cake," "State Vegetable," "State Legume," "State Spirit," "State Cuisine," "State Meal," etc. This value is used to build page titles and headings, so match it precisely.

Section guidance:

Overview — title: "What Is [State]'s [Designation]?"
Short and direct. Name the food, give the official status, adoption year. Two to three sentences. Do not restate the intro word for word.

About — title: "About [Food Name]"
A short, concrete section: what it is, what it looks or tastes like, how it is typically made or grown, if relevant. Three to five sentences max. Do not turn this into a recipe or a nutrition profile.

Selection — title: "How [Food Name] Became [State]'s [Designation]"
Explain when it became official and how it was chosen. Mention a campaign, an industry group, students, or lawmakers only if verified. Keep it short. No act numbers, bill numbers, or section numbers here, see the Legal citations rule above; those go only in the `legislation` field.

Reason — title: "Why [State] Chose [Food Name]"
Ground the reason in the state's actual agriculture, industry, or culinary history: a crop the state is a leading producer of, a dish invented there, a recipe tied to a specific town or region. Do not invent symbolism.
If official sources only name the food without giving a reason, say so plainly: "State lawmakers named [food] the official [designation] in [year] without recording a specific reason," or skip this section if the YAML structure allows it.

Location (OPTIONAL — include only if a real, specific, documented place exists):
title: "Where [Food Name] Comes From in [State]"
Include this section only when there is a genuine, nameable place tied to the food: a town known for growing or inventing it, a festival, a production region. Do not include it just to have a map, omit the whole section if the food has no specific place tied to it.
Use the sites key for map points: each site needs name, city, lat, lng, note (short phrase, under 10 words), and type (primary or secondary).

Facts — title: "[Food Name] Facts"
Three to five short verified facts.
Good facts include adoption year, the industry or crop connection, a production statistic if verified, or a historic origin story. Adoption facts should read "Adopted in [year] by the [State] Legislature," never with an act or section number.
Do not add random trivia.

FAQ:
Short direct answers to real student questions. Use only questions that fit the available facts.

Good FAQ questions:
What is [State]'s [designation]?
When did [State] adopt [food name]?
Why did [State] choose [food name]?
Where does [food name] come from in [State]?
Is [food name] grown or made in [State] today?

No em dashes — critical rule, follow exactly:
Never use an em dash (—) anywhere in the output: not in intro_text, not in paragraphs, not in facts, not in FAQ answers, not in captions. This includes the double-hyphen substitute ( -- ). Rewrite instead of reaching for one. Use a period and a new sentence, a comma, "and," "but," "which," or a colon when introducing a list or explanation. A short hyphen (-) for compound words is fine, an em dash is not.

Style:
Write for a curious 12-year-old, not a food magazine.
Use active voice. Keep sentences short.
Prefer concrete facts: dates, place names, crop statistics, historic figures.
Do not invent meaning, symbolism, or reasons.
If a source does not confirm why the food was chosen, say that plainly or leave the reason out.
Avoid filler and generic food writing.

Do not use:
em dash (see the No em dashes rule above), embodies, tapestry, testament, vibrant, delve, boasts, nestled, rich history, stands as, serves as, mouthwatering, delectable, culinary treasure, Furthermore, Moreover, Additionally, Notably, In conclusion, In summary, tells the story of, important symbol, proud history, spirit of the state, comfort food classic, a taste of home.

YAML structure to fill:

type: State Food
state: [State name]
state_fips: "[2-digit FIPS]"
name: [Food name]
designation: "[State Cookie / State Nut / State Fruit / etc, exact wording]"
adopted_year: [Year]
is_official: true
legislation: "Adopted by the [State] Legislature in [Year]"

author: USA Symbol Team
date_published: ""
date_modified: ""
seo_title: "[State] State [Designation] | [Food Name]"
seo_description: "[Under 155 chars]"
hero_image: /images/foods/[state-slug]/[filename].webp
hero_image_alt: "[Alt text describing the food]"
hero_image_caption: ""
intro_text: "[One or two sentences]"

sections:
- id: overview
  icon: fa-solid fa-utensils
  title: "What Is [State]'s [Designation]?"
  paragraphs:
  - "[paragraph]"

- id: about
  icon: fa-solid fa-magnifying-glass
  title: "About [Food Name]"
  paragraphs:
  - "[paragraph]"

- id: selection
  icon: fa-solid fa-landmark
  title: "How [Food Name] Became [State]'s [Designation]"
  paragraphs:
  - "[paragraph]"

- id: reason
  icon: fa-solid fa-circle-question
  title: "Why [State] Chose [Food Name]"
  paragraphs:
  - "[paragraph]"

# Optional — include only if a real, documented place exists. Delete this whole block otherwise.
- id: location
  icon: fa-solid fa-map-location-dot
  title: "Where [Food Name] Comes From in [State]"
  paragraphs:
  - "[paragraph]"
  sites:
  - name: [Site or region name]
    city: [City]
    lat: [latitude]
    lng: [longitude]
    note: "[Short phrase, under 10 words]"
    type: primary

- id: facts
  icon: fa-solid fa-lightbulb
  title: "[Food Name] Facts"
  facts:
  - "[fact]"
  - "[fact]"
  - "[fact]"

faq:
- question: "What is [State]'s [designation]?"
  answer: "[answer]"
- question: "When did [State] adopt [food name]?"
  answer: "[answer]"
- question: "Why did [State] choose [food name]?"
  answer: "[answer]"
- question: "[Food- and state-specific question]"
  answer: "[answer]"

sources:
- name: "[Source name]"
  url: "[URL]"
  description: "[Short description]"
