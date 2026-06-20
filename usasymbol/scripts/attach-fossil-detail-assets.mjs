#!/usr/bin/env node

import { existsSync, readFileSync, writeFileSync } from 'fs';
import { join } from 'path';
import { fileURLToPath } from 'url';

const __dirname = fileURLToPath(new URL('.', import.meta.url));
const ROOT = join(__dirname, '..');
const WEBROOT = join(ROOT, 'wwwroot');

const mappings = [
  ['Content/states/colorado/fossil.yaml', 'colorado-fossil-detail.webp', 'colorado-dinosaur-ridge', 'history', 'Dinosaur Ridge in Colorado', 'Dinosaur Ridge marks one of the best-known Colorado localities tied to the early history of Stegosaurus discovery.'],
  ['Content/states/nevada/fossil.yaml', 'nevada-fossil-detail.webp', 'nevada-shonisaurus', 'about', 'Shonisaurus skull fossil', 'Shonisaurus was one of the largest marine reptiles of the Triassic seas, with specimens famous from Nevada.'],
  ['Content/states/new-jersey/fossil.yaml', 'new-jersey-fossil-detail.webp', 'new-jersey-hadrosaurus', 'about', 'Hadrosaurus reconstruction', 'Hadrosaurus became historically important as one of the first reasonably complete dinosaur skeletons described in North America.'],
  ['Content/states/new-mexico/fossil.yaml', 'new-mexico-fossil-detail.webp', 'new-mexico-coelophysis', 'about', 'Coelophysis skeleton mount', 'Coelophysis is one of the classic small theropods of the Late Triassic fossil record.'],
  ['Content/states/new-york/fossil.yaml', 'new-york-fossil-detail.webp', 'new-york-eurypterus', 'about', 'Sea scorpion fossil specimen', 'Eurypterus is one of the best-known eurypterids, an extinct group often called sea scorpions.'],
  ['Content/states/north-carolina/fossil.yaml', 'north-carolina-fossil-detail.webp', 'north-carolina-aurora-fossil-museum', 'history', 'Aurora Fossil Museum in North Carolina', 'Aurora became nationally known for rich phosphate spoil piles loaded with shark teeth and marine fossils.'],
  ['Content/states/oklahoma/fossil.yaml', 'oklahoma-fossil-detail.webp', 'oklahoma-saurophaganax', 'about', 'Saurophaganax fossil material', 'Saurophaganax is represented by large theropod material from Jurassic rocks in the American West.'],
  ['Content/states/pennsylvania/fossil.yaml', 'pennsylvania-fossil-detail.webp', 'pennsylvania-phacops', 'about', 'Phacops trilobite fossil', 'Phacops is especially known for large compound eyes and enrollable body armor.'],
  ['Content/states/rhode-island/fossil.yaml', 'rhode-island-fossil-detail.webp', 'rhode-island-trilobite', 'about', 'Trilobite illustration and fossil forms', 'Trilobites dominated Paleozoic marine ecosystems for hundreds of millions of years before disappearing at the end of the Permian.'],
  ['Content/states/south-carolina/fossil.yaml', 'south-carolina-fossil-detail.webp', 'south-carolina-edisto-river', 'history', 'Edisto River in South Carolina', 'South Carolina riverbeds such as the Edisto are well known to fossil hunters searching for mammoth remains and shark teeth.'],
  ['Content/states/tennessee/fossil.yaml', 'tennessee-fossil-detail.webp', 'tennessee-trigoniida', 'about', 'Trigoniid clam shell fossil', 'Trigoniid bivalves are notable for heavy ribbed shells and long persistence through Mesozoic seas.'],
  ['Content/states/utah/fossil.yaml', 'utah-fossil-detail.webp', 'utah-cleveland-lloyd', 'history', 'Cleveland-Lloyd Dinosaur Quarry in Utah', 'Cleveland-Lloyd became famous for one of the densest Jurassic theropod bone accumulations ever discovered.'],
  ['Content/states/virginia/fossil.yaml', 'virginia-fossil-detail.webp', 'virginia-chesapecten', 'about', 'Chesapecten fossil shell', 'Chesapecten is tied to Atlantic Coastal Plain marine deposits and is among the best-known fossil scallops in eastern North America.'],
  ['Content/states/west-virginia/fossil.yaml', 'west-virginia-fossil-detail.webp', 'west-virginia-megalonyx', 'about', 'Jefferson ground sloth skeleton', 'Megalonyx was the first fossil vertebrate from North America formally described in scientific literature.']
];

let attached = 0;
for (const [yamlRel, imageName, id, section, alt, caption] of mappings) {
  const yamlPath = join(ROOT, yamlRel);
  const imagePath = join(WEBROOT, 'images', 'fossils', imageName);
  if (!existsSync(imagePath)) continue;

  const text = readFileSync(yamlPath, 'utf8');
  if (/^visual_assets:/m.test(text)) continue;

  const block = [
    'visual_assets:',
    `  - id: ${id}`,
    `    src: /images/fossils/${imageName}`,
    `    alt: "${alt.replace(/"/g, '\\"')}"`,
    `    caption: "${caption.replace(/"/g, '\\"')}"`,
    `    section: ${section}`,
    '    layout: right',
    ''
  ].join('\n');

  writeFileSync(yamlPath, text.replace(/\nfaq:/, `\n${block}\nfaq:`));
  attached += 1;
}

console.log(`Attached fossil detail assets: ${attached}`);
