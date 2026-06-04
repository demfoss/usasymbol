import fs from "node:fs/promises";
import path from "node:path";

const root = process.cwd();
const pixabayKey = process.env.PIXABAY_KEY;

if (!pixabayKey) {
  console.error("Missing PIXABAY_KEY environment variable.");
  process.exit(1);
}

const extras = [
  {
    yaml: "Content/rankings/economy/states-by-median-income.yml",
    imageDir: "wwwroot/images/rankings/economy/states-by-median-income",
    file: "new-hampshire-income-placeholder.jpg",
    query: "suburban commuter town usa",
    id: "new-hampshire-income-placeholder",
    alt: "Quiet suburban neighborhood with tree-lined streets and detached homes",
    caption:
      "Commuter suburbs often benefit from nearby metro wages while keeping a distinct small-state housing pattern.",
    section: "new_hampshire_outlier",
    layout: "left",
  },
  {
    yaml: "Content/rankings/economy/states-by-cost-of-living.yml",
    imageDir: "wwwroot/images/rankings/economy/states-by-cost-of-living",
    file: "west-virginia-cost-of-living-placeholder.jpg",
    query: "small town houses appalachia",
    id: "west-virginia-cost-of-living-placeholder",
    alt: "Small-town homes and low-rise buildings along a quiet street",
    caption:
      "Lower housing costs shape the broader price structure in many of the country's least expensive states.",
    section: "why_west_virginia_cheapest",
    layout: "right",
  },
  {
    yaml: "Content/rankings/economy/states-by-home-value.yml",
    imageDir: "wwwroot/images/rankings/economy/states-by-home-value",
    file: "western-home-value-placeholder.jpg",
    query: "mountain suburb new homes",
    id: "western-home-value-placeholder",
    alt: "New homes spreading across a dry foothill landscape",
    caption:
      "Fast-growing Western housing markets often combine new construction with strong migration-driven demand.",
    section: "western_surge",
    layout: "left",
  },
  {
    yaml: "Content/rankings/economy/states-by-unemployment.yml",
    imageDir: "wwwroot/images/rankings/economy/states-by-unemployment",
    file: "california-unemployment-placeholder.jpg",
    query: "downtown skyline office towers california",
    id: "california-unemployment-placeholder",
    alt: "Dense downtown skyline filled with tall office buildings",
    caption:
      "Large, specialized urban labor markets can take longer to reabsorb displaced workers after major sector layoffs.",
    section: "why_california_high",
    layout: "right",
  },
  {
    yaml: "Content/rankings/economy/states-by-poverty-rate.yml",
    imageDir: "wwwroot/images/rankings/economy/states-by-poverty-rate",
    file: "new-hampshire-poverty-placeholder.jpg",
    query: "new england town main street",
    id: "new-hampshire-poverty-placeholder",
    alt: "Well-kept New England main street with brick buildings and local shops",
    caption:
      "Stable local business districts often reflect the stronger household earnings found in low-poverty states.",
    section: "why_new_hampshire_lowest",
    layout: "left",
  },
  {
    yaml: "Content/rankings/economy/states-by-gas-price.yml",
    imageDir: "wwwroot/images/rankings/economy/states-by-gas-price",
    file: "oklahoma-gas-price-placeholder.jpg",
    query: "oil refinery plains",
    id: "oklahoma-gas-price-placeholder",
    alt: "Refinery tanks and industrial towers on a flat plains landscape",
    caption:
      "States close to oil production and refining centers often see lower pump prices than coastal import-dependent markets.",
    section: "why_oklahoma_cheapest",
    layout: "right",
  },
  {
    yaml: "Content/rankings/taxes/states-by-income-tax.yml",
    imageDir: "wwwroot/images/rankings/taxes/states-by-income-tax",
    file: "no-income-tax-placeholder.jpg",
    query: "downtown skyline business district usa",
    id: "no-income-tax-placeholder",
    alt: "Modern business district with office towers and broad streets",
    caption:
      "States without wage income taxes still rely on other large tax bases tied to property, tourism, energy, or consumption.",
    section: "nine_states_no_income_tax",
    layout: "left",
  },
  {
    yaml: "Content/rankings/taxes/states-by-property-tax.yml",
    imageDir: "wwwroot/images/rankings/taxes/states-by-property-tax",
    file: "hawaii-property-tax-placeholder.jpg",
    query: "tropical homes neighborhood",
    id: "hawaii-property-tax-placeholder",
    alt: "Tropical residential neighborhood with palm trees and detached homes",
    caption:
      "Even low effective tax rates can produce meaningful bills when housing values remain exceptionally high.",
    section: "why_hawaii_lowest",
    layout: "right",
  },
  {
    yaml: "Content/rankings/health/states-by-life-expectancy.yml",
    imageDir: "wwwroot/images/rankings/health/states-by-life-expectancy",
    file: "utah-life-expectancy-placeholder.jpg",
    query: "family hiking desert trail",
    id: "utah-life-expectancy-placeholder",
    alt: "People hiking together on a wide trail in a dry mountain landscape",
    caption:
      "Active outdoor routines are one of the lifestyle patterns often discussed when comparing long-lived states.",
    section: "utah_outlier",
    layout: "left",
  },
  {
    yaml: "Content/rankings/health/states-by-crime-rate.yml",
    imageDir: "wwwroot/images/rankings/health/states-by-crime-rate",
    file: "new-england-crime-placeholder.jpg",
    query: "small town street new england",
    id: "new-england-crime-placeholder",
    alt: "Calm small-town street with storefronts, trees, and parked cars",
    caption:
      "Compact towns and smaller urban networks are part of the low-crime profile often associated with northern New England.",
    section: "why_new_england_safest",
    layout: "right",
  },
  {
    yaml: "Content/rankings/health/states-by-obesity-rate.yml",
    imageDir: "wwwroot/images/rankings/health/states-by-obesity-rate",
    file: "southern-obesity-placeholder.jpg",
    query: "rural southern town street",
    id: "southern-obesity-placeholder",
    alt: "Wide road running through a small Southern town with scattered businesses",
    caption:
      "Built environments with long driving distances and fewer recreation options can shape everyday activity patterns.",
    section: "southern_obesity_cluster",
    layout: "left",
  },
  {
    yaml: "Content/rankings/health/states-by-uninsured-rate.yml",
    imageDir: "wwwroot/images/rankings/health/states-by-uninsured-rate",
    file: "texas-uninsured-placeholder.jpg",
    query: "busy city hospital exterior texas",
    id: "texas-uninsured-placeholder",
    alt: "Large urban medical complex beside busy roads and parking areas",
    caption:
      "High-population states can have major hospital systems and still leave large numbers of residents outside formal coverage.",
    section: "why_texas_highest",
    layout: "right",
  },
  {
    yaml: "Content/rankings/education/states-by-k12-education.yml",
    imageDir: "wwwroot/images/rankings/education/states-by-k12-education",
    file: "northeast-k12-placeholder.jpg",
    query: "school bus campus brick school",
    id: "northeast-k12-placeholder",
    alt: "School bus near a brick academic building and campus lawn",
    caption:
      "The Northeast's strongest school systems are often tied to dense local district networks and long-established public campuses.",
    section: "northeast_dominance",
    layout: "left",
  },
];

function quote(value) {
  return `"${String(value).replace(/"/g, '\\"')}"`;
}

function toPublicDir(imageDir) {
  return `/${imageDir.replaceAll("\\", "/").replace(/^wwwroot\//, "")}`;
}

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
      "User-Agent": "CodexRankingAssetsExtra/1.0",
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
      "User-Agent": "CodexRankingAssetsExtra/1.0",
    },
  });

  if (!imageResponse.ok) {
    throw new Error(`Image download failed for "${query}" with ${imageResponse.status}`);
  }

  const arrayBuffer = await imageResponse.arrayBuffer();
  await fs.writeFile(outPath, Buffer.from(arrayBuffer));
}

function buildAssetBlock(item) {
  const publicDir = toPublicDir(item.imageDir);
  return [
    `  - id: ${item.id}`,
    `    src: ${publicDir}/${item.file}`,
    `    alt: ${quote(item.alt)}`,
    `    caption: ${quote(item.caption)}`,
    `    section: ${item.section}`,
    `    layout: ${item.layout}`,
  ].join("\n");
}

async function updateYaml(item) {
  const yamlPath = path.join(root, item.yaml);
  let text = await fs.readFile(yamlPath, "utf8");

  if (text.includes(`id: ${item.id}`)) {
    return;
  }

  const assetBlock = buildAssetBlock(item);
  const faqIndex = text.indexOf("\nfaq:");

  if (faqIndex === -1) {
    throw new Error(`Could not find faq block in ${item.yaml}`);
  }

  text = `${text.slice(0, faqIndex)}${assetBlock}\n${text.slice(faqIndex)}`;
  await fs.writeFile(yamlPath, text);
}

async function main() {
  for (const item of extras) {
    const absDir = path.join(root, item.imageDir);
    await ensureDir(absDir);
    const imagePath = path.join(absDir, item.file);

    console.log(`Adding extra asset for ${item.yaml}`);
    await downloadPixabayImage(item.query, imagePath);
    await updateYaml(item);
  }

  console.log("Extra ranking assets added successfully.");
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
