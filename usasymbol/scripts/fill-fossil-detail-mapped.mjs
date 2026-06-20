#!/usr/bin/env node

import { readFileSync, writeFileSync } from 'fs';
import { join } from 'path';
import { fileURLToPath } from 'url';
import { execFileSync } from 'child_process';
import sharp from 'sharp';

const __dirname = fileURLToPath(new URL('.', import.meta.url));
const ROOT = join(__dirname, '..');
const WEBROOT = join(ROOT, 'wwwroot');

const mappings = [
  { yaml: 'Content/states/colorado/fossil.yaml', page: 'Dinosaur Ridge', section: 'history', alt: 'Dinosaur Ridge in Colorado', caption: 'Dinosaur Ridge marks one of the best-known Colorado localities tied to the early history of Stegosaurus discovery.' },
  { yaml: 'Content/states/georgia/fossil.yaml', page: 'Altamaha River', section: 'history', alt: 'Altamaha River in Georgia', caption: 'The Altamaha River drains coastal plain sediments where fossil shark teeth are regularly found.' },
  { yaml: 'Content/states/illinois/fossil.yaml', page: 'Tullimonstrum gregarium', section: 'about', alt: 'Tully monster fossil specimen', caption: 'The Tully monster remains one of the most debated fossil animals in North America, despite decades of study.' },
  { yaml: 'Content/states/indiana/fossil.yaml', page: 'Mammut americanum', section: 'about', alt: 'American mastodon skeleton', caption: 'American mastodon fossils helped define Ice Age faunas across the Midwest and Great Lakes region.' },
  { yaml: 'Content/states/louisiana/fossil.yaml', page: 'Palmoxylon', section: 'about', alt: 'Petrified palmwood cross section', caption: 'Palmoxylon preserves the internal structure of ancient palm trunks in silicified stone.' },
  { yaml: 'Content/states/massachusetts/fossil.yaml', page: 'Eubrontes giganteus', section: 'about', alt: 'Large tridactyl dinosaur track slab', caption: 'Eubrontes tracks are among the most famous dinosaur footprints in the Connecticut River Valley.' },
  { yaml: 'Content/states/michigan/fossil.yaml', page: 'Mammut americanum', section: 'about', alt: 'American mastodon skeleton', caption: 'The American mastodon became one of the signature Ice Age mammals of the Great Lakes region.' },
  { yaml: 'Content/states/mississippi/fossil.yaml', page: 'Zygorhiza kochii', section: 'about', alt: 'Zygorhiza whale skeleton', caption: 'Zygorhiza was a smaller archaeocete whale closely related to the giant Basilosaurus of Gulf Coast seas.' },
  { yaml: 'Content/states/nebraska/fossil.yaml', page: 'Mammoth', section: 'about', alt: 'Mammoth skeleton mount', caption: 'Mammoth remains are among the most recognizable Ice Age fossils preserved in the Great Plains.' },
  { yaml: 'Content/states/nevada/fossil.yaml', page: 'Shonisaurus popularis', section: 'about', alt: 'Shonisaurus skeleton', caption: 'Shonisaurus was one of the largest marine reptiles of the Triassic seas, with specimens famous from Nevada.' },
  { yaml: 'Content/states/new-jersey/fossil.yaml', page: 'Hadrosaurus foulkii', section: 'about', alt: 'Hadrosaurus skeleton reconstruction', caption: 'Hadrosaurus became historically important as one of the first reasonably complete dinosaur skeletons described in North America.' },
  { yaml: 'Content/states/new-mexico/fossil.yaml', page: 'Coelophysis', section: 'about', alt: 'Coelophysis skeleton', caption: 'Coelophysis is one of the classic small theropods of the Late Triassic fossil record.' },
  { yaml: 'Content/states/new-york/fossil.yaml', page: 'Eurypterus remipes', section: 'about', alt: 'Sea scorpion fossil specimen', caption: 'Eurypterus is one of the best-known eurypterids, an extinct group often called sea scorpions.' },
  { yaml: 'Content/states/north-carolina/fossil.yaml', page: 'Aurora Fossil Museum', section: 'history', alt: 'Aurora Fossil Museum in North Carolina', caption: 'Aurora became nationally known for rich phosphate spoil piles loaded with shark teeth and marine fossils.' },
  { yaml: 'Content/states/oklahoma/fossil.yaml', page: 'Saurophaganax maximus', section: 'about', alt: 'Saurophaganax skeletal mount', caption: 'Saurophaganax is represented by large theropod material from Jurassic rocks in the American West.' },
  { yaml: 'Content/states/pennsylvania/fossil.yaml', page: 'Phacops rana', section: 'about', alt: 'Phacops trilobite fossil', caption: 'Phacops is especially known for large compound eyes and enrollable body armor.' },
  { yaml: 'Content/states/rhode-island/fossil.yaml', page: 'Trilobite', section: 'about', alt: 'Trilobite fossil with segmented exoskeleton', caption: 'Trilobites dominated Paleozoic marine ecosystems for hundreds of millions of years before disappearing at the end of the Permian.' },
  { yaml: 'Content/states/south-carolina/fossil.yaml', page: 'Edisto River', section: 'history', alt: 'Edisto River in South Carolina', caption: 'South Carolina riverbeds such as the Edisto are well known to fossil hunters searching for mammoth remains and shark teeth.' },
  { yaml: 'Content/states/tennessee/fossil.yaml', page: 'Pterotrigonia thoracica', section: 'about', alt: 'Trigonia clam fossil shell', caption: 'Trigoniid bivalves are notable for heavy ribbed shells and long persistence through Mesozoic seas.' },
  { yaml: 'Content/states/utah/fossil.yaml', page: 'Cleveland-Lloyd Dinosaur Quarry', section: 'history', alt: 'Cleveland-Lloyd Dinosaur Quarry in Utah', caption: 'Cleveland-Lloyd became famous for one of the densest Jurassic theropod bone accumulations ever discovered.' },
  { yaml: 'Content/states/virginia/fossil.yaml', page: 'Chesapecten jeffersonius', section: 'about', alt: 'Chesapecten scallop fossil shell', caption: 'Chesapecten is tied to Atlantic Coastal Plain marine deposits and is among the best-known fossil scallops in eastern North America.' },
  { yaml: 'Content/states/washington/fossil.yaml', page: 'Mammuthus columbi', section: 'about', alt: 'Columbian mammoth skeleton', caption: 'Columbian mammoths ranged far south of woolly mammoths and occupied many temperate North American landscapes.' },
  { yaml: 'Content/states/west-virginia/fossil.yaml', page: 'Megalonyx jeffersonii', section: 'about', alt: 'Jefferson ground sloth skeleton', caption: 'Megalonyx was the first fossil vertebrate from North America formally described in scientific literature.' }
];

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function curlBuffer(url) {
  return execFileSync('curl.exe', ['-L', '--silent', '--fail', url], { encoding: 'buffer', maxBuffer: 64 * 1024 * 1024 });
}

async function getOgImage(page) {
  const html = curlBuffer(`https://en.wikipedia.org/wiki/${encodeURIComponent(page.replace(/\s+/g, '_'))}`).toString('utf8');
  const m = html.match(/property="og:image" content="([^"]+)"/);
  return m ? m[1] : null;
}

let done = 0;
for (const item of mappings) {
  const yamlPath = join(ROOT, item.yaml);
  let text = readFileSync(yamlPath, 'utf8');
  if (/^visual_assets:/m.test(text)) continue;

  const imageUrl = await getOgImage(item.page);
  if (!imageUrl) continue;
  const stateSlug = item.yaml.split('/states/')[1].split('/')[0];
  const fileBase = item.yaml.split('/').pop().replace(/\.yaml$/i, '');
  const webName = `${stateSlug}-${fileBase}-detail.webp`;
  const diskPath = join(WEBROOT, 'images', 'fossils', webName);

  let buf = null;
  for (let attempt = 0; attempt < 4; attempt += 1) {
    try {
      buf = curlBuffer(imageUrl);
      break;
    } catch {
      await sleep(1200 * (attempt + 1));
    }
  }
  if (!buf) continue;

  await sharp(buf, { failOn: 'none' })
    .rotate()
    .resize({ width: 1600, withoutEnlargement: true })
    .webp({ quality: 76, effort: 6 })
    .toFile(diskPath);

  const slug = `${stateSlug}-${item.page.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')}`;
  const block = [
    'visual_assets:',
    `  - id: ${slug}`,
    `    src: /images/fossils/${webName}`,
    `    alt: "${item.alt.replace(/"/g, '\\"')}"`,
    `    caption: "${item.caption.replace(/"/g, '\\"')}"`,
    `    section: ${item.section}`,
    '    layout: right',
    ''
  ].join('\n');

  text = text.replace(/\nfaq:/, `\n${block}\nfaq:`);
  writeFileSync(yamlPath, text);
  done += 1;
  await sleep(900);
}

console.log(`Filled mapped fossil detail assets: ${done}`);
