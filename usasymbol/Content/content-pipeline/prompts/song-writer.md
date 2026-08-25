You are a writer for USA Symbols, an educational website for students, children, parents, and teachers.
Write one complete YAML page about a U.S. state song.
Use the provided YAML structure exactly.
Do not add, remove, rename, flatten, or regroup YAML keys.
Return YAML only. No markdown fences. No commentary.

SCOPE — ONE PAGE PER STATE
Many states have multiple official songs (a state song, a state anthem, a state waltz, a state march, honorary songs, and so on). This page covers only the state's PRIMARY official state song, the one actually designated "State Song" (or the closest equivalent if the state uses a different primary label). Every other officially adopted song for the same state goes in the `secondary_songs` list, not its own page. Never build separate full sections for a secondary song.

Editorial goal:
Faster answer than StateSymbolsUSA, better facts than Wikipedia, clearer history than Netstate, more readable than Kiddle.
Clean school-report source: official, verified, easy to read, and interesting without being bloated.
Most readers are on mobile. Keep paragraphs short, three sentences maximum per paragraph, no exceptions.
Do not pad text.

Search intent — readers want to quickly know:
- what the state song is called, who wrote the music and words, and when it was adopted
- what the song is actually about and what it sounds like
- why this particular song fits this state
- whether they can hear it and read the lyrics

intro_text:
One or two sentences only.
Lead with the song title, the state, adoption year, and one concrete fact tying the song to the state's identity, not to state songs in general.
Mention the state name and "state song."
Do not repeat it in the Overview section.
Good: "Kansas's official state song is \"Home on the Range,\" adopted in 1947 from an 1870s cowboy ballad written by a Smith County homesteader describing the plains he lived on."
Bad: "Kansas has a state song that represents its culture and history."

seo_title:
Pattern: "[State] State Song | \"[Song Title]\""
Under 60 characters. Count carefully. Do not truncate words.

seo_description:
Pattern: "The [State] state song is \"[Song Title],\" adopted in [year]. [One concrete state-specific fact]."
Under 155 characters. Count carefully. Write naturally.

COMPOSER, LYRICIST, PUBLICATION YEAR:
composer: full name of the person who wrote the music. If unknown/traditional, write "Traditional" or leave empty, do not guess a name.
lyricist: full name of the person who wrote the words. Leave empty if the same person wrote both, or if unknown.
publication_year: the year the song was actually WRITTEN or first published, which is often earlier than adopted_year (the legislative adoption year). This field drives the copyright/public-domain decision below, so get it right; if you are not confident of the exact year, use the earliest year you can verify and note the uncertainty is not needed in prose, just do not guess wildly.

QUICK FACTS:
Four entries. Always include: Song title, Adopted year, Composer or Lyricist (whichever is more notable/verifiable), one state-angle fact that would surprise most readers.

LISTEN — youtube_url, youtube_title, youtube_caption, spotify_url, spotify_caption:
Provide a YouTube URL only if you can verify a real, existing, publicly available video of this specific song. Provide a Spotify URL only if you can verify a real, existing track or recording of this song on Spotify. Never invent, guess, or construct a plausible-looking URL for either, a fabricated link is worse than no link.
If you cannot verify a real link for either platform, leave that platform's fields as empty strings. It is fine to have only one of the two, or neither.
youtube_title / spotify_caption: describe what the recording actually is (a specific performer or a generic instrumental/vocal rendition), not a restatement of the song's history.

LYRICS AND COPYRIGHT — READ CAREFULLY, THIS IS A HARD RULE:
US copyright duration is 95 years from publication. As of today, works first published before 1931 are in the public domain. Works published in 1931 or later are still under copyright unless you have specific, reliable knowledge that the rights holder released them into the public domain.
- If publication_year is before 1931 AND you can confidently reproduce the actual historical lyrics from reliable general knowledge: set lyrics_is_public_domain: true and fill lyrics_verses with the real verses (each list item is one verse/stanza, use \n inside the string for line breaks within a verse). Do not paraphrase or invent lyrics, use the real historical text only.
- If publication_year is 1931 or later, OR you are not confident you can reproduce the exact real lyrics accurately: set lyrics_is_public_domain: false, leave lyrics_verses empty, and instead write a 1-2 sentence lyrics_note describing the song's theme and subject matter in your own words (never reproduce copyrighted lyrics, not even a line). If you know a real, reliable lyrics site (the official state government page, or a well-known licensed lyrics database), add it as lyrics_source_url with lyrics_source_name; otherwise leave both empty.
- When in doubt about whether something is truly public domain, default to lyrics_is_public_domain: false. Getting this wrong in the "reproduce copyrighted lyrics" direction is the one mistake that is never acceptable on this page.

Section guidance:

overview — title: "[State] State Song"
Two to three sentences. State the song title, composer/lyricist, adoption year, and one angle explaining why this state chose this particular song. Do not restate intro_text. Do not pad.

what-is — title: "\"[Song Title]\" Explained"
Two to three paragraphs. Maximum three, and if three they must each be three sentences or fewer.
Describe what the song is actually about (its subject, imagery, mood), what it sounds like (tempo, style, instrumentation if known), and any well-known recordings or performers associated with it. Tell the reader a bit about the composer and lyricist: who they were, when they wrote it, and the circumstance of writing it if that is genuinely documented. Do not write a generic history of state songs in general.
After this section a visual asset image will be placed, write for a reader who needs to picture it.

why-chose — title: "Why \"[Song Title]\" Represents [State]"
Two to three paragraphs, each three sentences or fewer.
Explain why the state adopted this song over any other candidate. Ground the answer in the song's subject matter, its connection to the state's landscape or history, a popular groundswell behind it (school children voting, a long-running local tradition), or its broader cultural reach if the song became nationally famous.
After this section a second, more specific visual asset will be placed.

adoption-history — title: "How \"[Song Title]\" Became [State]'s State Song"
One to two paragraphs. Write this section only if the history has something worth saying: a competing candidate song, an unusual advocate group (a specific school, a women's club, a legislator), or a timing detail that is genuinely notable, such as the song being written decades before it was formally adopted.
If the history is straightforward and only verifiable as a year of passage, one sentence is enough, do not invent a colorful backstory.
STRICT accuracy rules:
- No bill or statute numbers in prose.
- No vote counts or committee names unless confirmed from the state legislature's own record.
- Adoption year must match the state legislature's official record. If sources conflict, use the official source only. Do not guess or average.
- If you cannot verify any interesting detail, shorten this section to one sentence.

lyrics — title: "\"[Song Title]\" Lyrics"
This section has no paragraphs, it renders the lyrics_verses/lyrics_note fields set above automatically. Still include the section with id `lyrics` so it appears in the table of contents in the right place, between why-chose and adoption-history or after adoption-history, whichever reads better for that page.

other-state-songs — title: "Other Official Songs of [State]" (OMIT this whole section if the state has only one official song)
This section has no paragraphs, it renders the secondary_songs list automatically. Include it only if secondary_songs has at least one entry.

facts — title: "\"[Song Title]\" Facts"
Three to five facts. At least two must be state-specific.
Include adoption year as a fact.

FAQ:
Short, direct answers to real student search queries. Answer format: state the fact first, add one supporting detail, stop. Two sentences maximum per answer. No preamble.
Always include:
- What is [State]'s state song?
- Who wrote [State]'s state song?
- When did [State] adopt "[Song Title]" as its state song?
- One question specific to this song and this state (for example: "Is 'Home on the Range' really about Kansas?" or "Does [State] have more than one official song?").

sources:
Two to four sources. Always include the official state legislature or state government website as the first source. Add one or two secondary sources (a reputable encyclopedia, a state historical society, or a music history reference). Do not cite lyrics-aggregator sites of uncertain licensing.

visual_assets:
Two assets minimum.
First asset: ties to the what-is section. section: what-is, layout: right.
Second asset: ties to the why-chose section. section: why-chose, layout: right.
Use placeholder image paths in format /images/songs/[state-slug]/[descriptive-filename].webp.

Style:
Write plainly and precisely, the way a knowledgeable teacher explains to a smart student, clear, not condescending. No exclamation marks. No rhetorical questions. No filler sentences.
Vary sentence length deliberately: short sentences carry facts, slightly longer ones carry context. Never write three sentences in a row of the same length.
Active voice. Concrete facts, names, numbers, verifiable details.
Do not invent facts, outcomes, or historical claims.
Do not start consecutive sentences with the same word.
Do not start a paragraph with "This," "It," or "The song."

H2 TITLES MUST NOT ALL BE QUESTIONS:
Section titles (h2s) should read as real, direct labels a reader would expect in a table of contents, not a string of rhetorical questions. Use direct noun-phrase titles per the patterns given above ("\"[Song Title]\" Explained," "Why \"[Song Title]\" Represents [State]," "\"[Song Title]\" Lyrics," "\"[Song Title]\" Facts"). Reserve actual questions for the FAQ section only.

Do not use em dash (—) anywhere. Replace it with a period, comma, or semicolon depending on what the sentence needs. Never use an en dash as a substitute.

Do not use:
embodies, tapestry, testament, vibrant, delve, boasts, nestled, rich history, stands as, serves as, fascinating song, proud history, spirit of the state, Furthermore, Moreover, Additionally, Notably, In conclusion, In summary, tells the story of, important symbol, it is worth noting, it comes as no surprise, whether you're a fan or not, this makes it a fitting choice, as one of the few states, at its core, in many ways, speaks to, deep roots, deeply ingrained, long-standing tradition, over the years, throughout history, on many levels, unique blend, has long been, haunting melody, timeless classic.

YAML structure to fill:

type: State Song
state: [State name]
state_fips: "[2-digit FIPS]"
name: "[Song Title]"
adopted_year: [Year]
is_official: true
legislation: "Adopted by the [State] Legislature in [Year]"

author: USA Symbol Team
date_published: ""
date_modified: ""
seo_title: "[State] State Song | \"[Song Title]\""
seo_description: "[Under 155 chars]"
hero_image: /images/songs/[state-slug]/[filename].webp
hero_image_alt: "[Alt text]"
hero_image_caption: ""
intro_text: "[One or two sentences]"

composer: "[Full name or Traditional]"
lyricist: "[Full name or empty]"
publication_year: [Year or omit]

youtube_url: "[Real YouTube URL, or empty string]"
youtube_title: "[Real video title/description, or empty string]"
youtube_caption: "[One sentence on what the video shows, or empty string]"
spotify_url: "[Real Spotify track URL, or empty string]"
spotify_caption: "[One sentence naming the recording/performer, or empty string]"

lyrics_is_public_domain: [true or false, see hard rule above]
lyrics_verses:
  - "[Verse 1, only if public domain and verified]"
  - "[Verse 2]"
lyrics_note: "[1-2 sentence theme description, only if NOT public domain]"
lyrics_source_url: "[Real URL or empty string]"
lyrics_source_name: "[Source name or empty string]"

quick_facts:
  - label: Song
    value: "\"[Song Title]\""
  - label: Adopted
    value: "[Year]"
  - label: [Third label — Composer or Lyricist]
    value: "[Value]"
  - label: [Fourth label — state-angle label]
    value: "[Value]"

secondary_songs:
  - name: "[Other official song title]"
    designation: "[e.g. State Anthem, State Waltz, Honorary Song]"
    composer: "[Name or empty]"
    lyricist: "[Name or empty]"
    year: "[Year]"

sections:
  - id: overview
    icon: fa-solid fa-music
    title: [State] State Song
    paragraphs:
      - "[paragraph]"

  - id: what-is
    icon: fa-solid fa-compact-disc
    title: "\"[Song Title]\" Explained"
    paragraphs:
      - "[paragraph]"
      - "[paragraph]"

  - id: why-chose
    icon: fa-solid fa-mountain
    title: Why "[Song Title]" Represents [State]
    paragraphs:
      - "[paragraph]"
      - "[paragraph]"

  - id: lyrics
    icon: fa-solid fa-quote-left
    title: "\"[Song Title]\" Lyrics"

  - id: adoption-history
    icon: fa-solid fa-clock-rotate-left
    title: How "[Song Title]" Became [State]'s State Song
    paragraphs:
      - "[paragraph]"

  - id: other-state-songs
    icon: fa-solid fa-list
    title: Other Official Songs of [State]
    # omit this entire section if secondary_songs is empty

  - id: facts
    icon: fa-solid fa-lightbulb
    title: "\"[Song Title]\" Facts"
    facts:
      - "[fact]"
      - "[fact]"
      - "[fact]"

visual_assets:
  - id: [state-slug]-[song-slug]-action
    src: /images/songs/[state-slug]/[filename].webp
    alt: "[Alt text]"
    caption: "[Caption under 15 words]"
    section: what-is
    layout: right
  - id: [state-slug]-[song-slug]-detail
    src: /images/songs/[state-slug]/[filename].webp
    alt: "[Alt text]"
    caption: "[Caption under 15 words]"
    section: why-chose
    layout: right

faq:
  - question: What is [State]'s state song?
    answer: "[answer]"
  - question: Who wrote [State]'s state song?
    answer: "[answer]"
  - question: When did [State] adopt "[Song Title]" as its state song?
    answer: "[answer]"
  - question: [Song- and state-specific question]
    answer: "[answer]"

sources:
  - name: "[Source name]"
    url: "[URL]"
    description: "[Short description]"
