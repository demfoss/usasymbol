import fs from "node:fs/promises";
import path from "node:path";

const root = process.cwd();
const pixabayKey = process.env.PIXABAY_KEY;

if (!pixabayKey) {
  console.error("Missing PIXABAY_KEY environment variable.");
  process.exit(1);
}

const pages = [
  {
    yaml: "Content/rankings/economy/states-by-median-income.yml",
    imageDir: "wwwroot/images/rankings/economy/states-by-median-income",
    heroFile: "states-by-median-income-hero.jpg",
    heroQuery: "city skyline office district usa",
    heroAlt: "Downtown skyline with office towers and apartment buildings at sunset",
    sectionFile: "maryland-income-placeholder.jpg",
    sectionQuery: "government office campus building",
    sectionId: "why_maryland_leads",
    sectionAlt: "Modern office buildings and a landscaped campus",
    sectionCaption:
      "A dense office campus suggests the kind of high-salary professional work that lifts household incomes in government-centered regions.",
    layout: "right",
  },
  {
    yaml: "Content/rankings/economy/states-by-cost-of-living.yml",
    imageDir: "wwwroot/images/rankings/economy/states-by-cost-of-living",
    heroFile: "states-by-cost-of-living-hero.jpg",
    heroQuery: "honolulu aerial ocean city",
    heroAlt: "Coastal city skyline with mountains and ocean in the background",
    sectionFile: "hawaii-cost-of-living-placeholder.jpg",
    sectionQuery: "cargo ship containers port",
    sectionId: "why_hawaii_most_expensive",
    sectionAlt: "Shipping containers stacked at a busy port terminal",
    sectionCaption:
      "Stacks of imported cargo capture the shipping costs that ripple into food, fuel, and everyday prices in island markets.",
    layout: "left",
  },
  {
    yaml: "Content/rankings/economy/states-by-home-value.yml",
    imageDir: "wwwroot/images/rankings/economy/states-by-home-value",
    heroFile: "states-by-home-value-hero.jpg",
    heroQuery: "luxury homes aerial coast",
    heroAlt: "Large homes clustered along a sunny coastal hillside",
    sectionFile: "hawaii-home-value-placeholder.jpg",
    sectionQuery: "hillside homes ocean view",
    sectionId: "why_hawaii_leads",
    sectionAlt: "Rows of hillside homes overlooking water",
    sectionCaption:
      "Limited land and strong demand often push scenic housing markets far above national home-value averages.",
    layout: "right",
  },
  {
    yaml: "Content/rankings/economy/states-by-unemployment.yml",
    imageDir: "wwwroot/images/rankings/economy/states-by-unemployment",
    heroFile: "states-by-unemployment-hero.jpg",
    heroQuery: "industrial work site jobs",
    heroAlt: "Workers and equipment at a large industrial job site",
    sectionFile: "dakotas-unemployment-placeholder.jpg",
    sectionQuery: "oil field pump jack plains",
    sectionId: "why_dakotas_lead",
    sectionAlt: "Oil pump jack operating in an open plains landscape",
    sectionCaption:
      "Energy production and support work can keep small labor markets unusually tight when hiring stays steady.",
    layout: "left",
  },
  {
    yaml: "Content/rankings/economy/states-by-poverty-rate.yml",
    imageDir: "wwwroot/images/rankings/economy/states-by-poverty-rate",
    heroFile: "states-by-poverty-rate-hero.jpg",
    heroQuery: "small town neighborhood usa",
    heroAlt: "Modest residential neighborhood with closely spaced homes",
    sectionFile: "louisiana-poverty-placeholder.jpg",
    sectionQuery: "oil refinery industrial corridor",
    sectionId: "why_louisiana_highest",
    sectionAlt: "Industrial refinery complex with pipes, towers, and storage tanks",
    sectionCaption:
      "Heavy industry can dominate a state economy without spreading high wages evenly across nearby communities.",
    layout: "right",
  },
  {
    yaml: "Content/rankings/economy/states-by-gas-price.yml",
    imageDir: "wwwroot/images/rankings/economy/states-by-gas-price",
    heroFile: "states-by-gas-price-hero.jpg",
    heroQuery: "gas station road trip",
    heroAlt: "Gas station canopy beside a wide road under a bright sky",
    sectionFile: "california-gas-price-placeholder.jpg",
    sectionQuery: "gas station price sign",
    sectionId: "why_california_highest",
    sectionAlt: "Tall fuel price sign outside a gas station",
    sectionCaption:
      "A roadside fuel sign turns state-by-state price differences into something drivers recognize immediately.",
    layout: "left",
  },
  {
    yaml: "Content/rankings/taxes/states-by-income-tax.yml",
    imageDir: "wwwroot/images/rankings/taxes/states-by-income-tax",
    heroFile: "states-by-income-tax-hero.jpg",
    heroQuery: "state capitol building dome usa",
    heroAlt: "Capitol building with a large dome and formal grounds",
    sectionFile: "california-income-tax-placeholder.jpg",
    sectionQuery: "capitol dome government building",
    sectionId: "why_california_highest",
    sectionAlt: "Government building with a dome rising above trees",
    sectionCaption:
      "Income-tax policy is written in state capitols, where progressive brackets and rate changes are set by law.",
    layout: "right",
  },
  {
    yaml: "Content/rankings/taxes/states-by-property-tax.yml",
    imageDir: "wwwroot/images/rankings/taxes/states-by-property-tax",
    heroFile: "states-by-property-tax-hero.jpg",
    heroQuery: "suburban neighborhood aerial",
    heroAlt: "Aerial view of a dense suburban neighborhood with detached homes",
    sectionFile: "new-jersey-property-tax-placeholder.jpg",
    sectionQuery: "suburban homes street",
    sectionId: "why_new_jersey_highest",
    sectionAlt: "Detached houses lined along a residential suburban street",
    sectionCaption:
      "Property taxes are felt most directly in residential neighborhoods where school funding and local services meet the tax bill.",
    layout: "left",
  },
  {
    yaml: "Content/rankings/health/states-by-life-expectancy.yml",
    imageDir: "wwwroot/images/rankings/health/states-by-life-expectancy",
    heroFile: "states-by-life-expectancy-hero.jpg",
    heroQuery: "people walking beach park",
    heroAlt: "People walking along a sunny path near water and palm trees",
    sectionFile: "hawaii-life-expectancy-placeholder.jpg",
    sectionQuery: "walking trail healthy lifestyle",
    sectionId: "why_hawaii_leads",
    sectionAlt: "People walking on a paved trail through a green park",
    sectionCaption:
      "Daily walking in mild weather fits the kind of active routine often linked with longer life expectancy.",
    layout: "right",
  },
  {
    yaml: "Content/rankings/health/states-by-crime-rate.yml",
    imageDir: "wwwroot/images/rankings/health/states-by-crime-rate",
    heroFile: "states-by-crime-rate-hero.jpg",
    heroQuery: "police lights city night",
    heroAlt: "Blurred police lights on a dark city street at night",
    sectionFile: "alaska-crime-placeholder.jpg",
    sectionQuery: "remote highway patrol vehicle",
    sectionId: "why_alaska_highest",
    sectionAlt: "Patrol vehicle parked along a long road in a sparsely settled landscape",
    sectionCaption:
      "Distance, sparse settlement, and long travel corridors can complicate public safety in places far from dense urban service networks.",
    layout: "left",
  },
  {
    yaml: "Content/rankings/health/states-by-obesity-rate.yml",
    imageDir: "wwwroot/images/rankings/health/states-by-obesity-rate",
    heroFile: "states-by-obesity-rate-hero.jpg",
    heroQuery: "mountain hiking trail people",
    heroAlt: "Hikers moving along a mountain trail under a clear sky",
    sectionFile: "colorado-obesity-placeholder.jpg",
    sectionQuery: "hiking trail mountains",
    sectionId: "why_colorado_lowest",
    sectionAlt: "Hiking path winding through a mountain landscape",
    sectionCaption:
      "Easy access to outdoor recreation is one of the lifestyle factors often mentioned in low-obesity states.",
    layout: "right",
  },
  {
    yaml: "Content/rankings/health/states-by-uninsured-rate.yml",
    imageDir: "wwwroot/images/rankings/health/states-by-uninsured-rate",
    heroFile: "states-by-uninsured-rate-hero.jpg",
    heroQuery: "hospital building exterior",
    heroAlt: "Large hospital building with a main entrance and surrounding road",
    sectionFile: "massachusetts-uninsured-placeholder.jpg",
    sectionQuery: "hospital campus exterior",
    sectionId: "why_massachusetts_leads",
    sectionAlt: "Hospital campus with multiple buildings and a front entrance",
    sectionCaption:
      "Broad coverage systems depend on provider networks, enrollment infrastructure, and hospitals able to absorb large insured populations.",
    layout: "left",
  },
  {
    yaml: "Content/rankings/education/states-by-k12-education.yml",
    imageDir: "wwwroot/images/rankings/education/states-by-k12-education",
    heroFile: "states-by-k12-education-hero.jpg",
    heroQuery: "school campus building",
    heroAlt: "School building with brick walls, windows, and a front lawn",
    sectionFile: "massachusetts-k12-placeholder.jpg",
    sectionQuery: "school building classroom campus",
    sectionId: "why_massachusetts_ranks_first",
    sectionAlt: "Academic building on a school campus with paths and lawn",
    sectionCaption:
      "Strong K-12 systems are often associated with well-funded campuses, stable staffing, and long-standing academic expectations.",
    layout: "right",
  },
];

async function ensureDir(dirPath) {
  await fs.mkdir(dirPath, { recursive: true });
}

async function downloadPixabayImage(query, outPath) {
  const apiUrl = new URL("https://pixabay.com/api/");
  apiUrl.searchParams.set("key", pixabayKey);
  apiUrl.searchParams.set("q", query);
  apiUrl.searchParams.set("image_type", "photo");
  apiUrl.searchParams.set("orientation", "horizontal");
  apiUrl.searchParams.set("safesearch", "true");
  apiUrl.searchParams.set("per_page", "3");

  const response = await fetch(apiUrl, {
    headers: {
      "User-Agent": "CodexRankingAssets/1.0",
    },
  });

  if (!response.ok) {
    throw new Error(`Pixabay API failed for "${query}" with ${response.status}`);
  }

  const data = await response.json();
  const hit = data.hits?.[0];

  if (!hit?.largeImageURL && !hit?.webformatURL) {
    throw new Error(`No Pixabay results for "${query}"`);
  }

  const imageUrl = hit.largeImageURL || hit.webformatURL;
  const imageResponse = await fetch(imageUrl, {
    headers: {
      "User-Agent": "CodexRankingAssets/1.0",
    },
  });

  if (!imageResponse.ok) {
    throw new Error(`Image download failed for "${query}" with ${imageResponse.status}`);
  }

  const arrayBuffer = await imageResponse.arrayBuffer();
  await fs.writeFile(outPath, Buffer.from(arrayBuffer));
}

function buildVisualAssetBlock(page) {
  const publicDir = toPublicDir(page.imageDir);
  return [
    "visual_assets:",
    `  - id: ${path.parse(page.sectionFile).name}`,
    `    src: ${publicDir}/${page.sectionFile}`,
    `    alt: ${quote(page.sectionAlt)}`,
    `    caption: ${quote(page.sectionCaption)}`,
    `    section: ${page.sectionId}`,
    `    layout: ${page.layout}`,
    "",
  ].join("\n");
}

function quote(value) {
  return `"${String(value).replace(/"/g, '\\"')}"`;
}

function toPublicDir(imageDir) {
  return `/${imageDir.replaceAll("\\", "/").replace(/^wwwroot\//, "")}`;
}

async function updateYaml(page) {
  const yamlPath = path.join(root, page.yaml);
  let text = await fs.readFile(yamlPath, "utf8");
  const publicDir = toPublicDir(page.imageDir);

  text = text.replace(/^hero_image:\s*".*"$/m, `hero_image: "${publicDir}/${page.heroFile}"`);
  text = text.replace(/^hero_image_alt:\s*".*"$/m, `hero_image_alt: ${quote(page.heroAlt)}`);

  if (/^visual_assets:\s*$/m.test(text)) {
    text = text.replace(/^visual_assets:[\s\S]*?(?=^faq:|^related:|\Z)/m, buildVisualAssetBlock(page));
  } else if (/^faq:/m.test(text)) {
    text = text.replace(/^faq:/m, `${buildVisualAssetBlock(page)}faq:`);
  } else if (/^related:/m.test(text)) {
    text = text.replace(/^related:/m, `${buildVisualAssetBlock(page)}related:`);
  } else {
    text += `\n${buildVisualAssetBlock(page)}`;
  }

  await fs.writeFile(yamlPath, text);
}

async function main() {
  for (const page of pages) {
    const absDir = path.join(root, page.imageDir);
    await ensureDir(absDir);

    const heroPath = path.join(absDir, page.heroFile);
    const sectionPath = path.join(absDir, page.sectionFile);

    console.log(`Downloading assets for ${page.yaml}`);
    await downloadPixabayImage(page.heroQuery, heroPath);
    await downloadPixabayImage(page.sectionQuery, sectionPath);
    await updateYaml(page);
  }

  console.log("Ranking assets added successfully.");
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
