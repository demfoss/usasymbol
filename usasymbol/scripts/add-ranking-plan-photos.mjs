import fs from 'node:fs/promises';
import sharp from 'sharp';
import { parse } from 'yaml';

const governmentBatch = process.argv.includes('--government');
const jobsBatch = process.argv.includes('--jobs');
const healthBatch = process.argv.includes('--health');
const workBatch = process.argv.includes('--work');
const batch = workBatch ? 'work-laws-trades-photos' : healthBatch ? 'healthcare-salary-photos' : jobsBatch ? 'jobs-and-pay-laws-photos' : governmentBatch ? 'governor-constitution-photos' : 'ranking-plan-14-32-48-63-82';
const pages = workBatch ? JSON.parse(await fs.readFile('scripts/work-laws-trades-photos.json', 'utf8')) : healthBatch ? JSON.parse(await fs.readFile('scripts/healthcare-salary-photos.json', 'utf8')) : jobsBatch ? JSON.parse(await fs.readFile('scripts/jobs-and-pay-laws-photos.json', 'utf8')) : governmentBatch ? [
  ['government', 'governor-salary-by-state', [
    ['1610055100752-a2f4ac29d58d', 'bJxCJw5TIp8', 'Indiana Statehouse in Indianapolis on a winter day'],
    ['1732723416426-8c3d5a826cb7', 'yMMAoNyt-T4', 'State capitol building with columns and an American flag'],
    ['1610055117772-a73ccefb8e8e', 'sOjH8_wDhpw', 'Front view of the Indiana Statehouse and its dome'],
  ]],
  ['government', 'state-constitution-length-by-state', [
    ['1755675673436-874fbed2dd32', 'gz_abaSsT7g', 'Rows of antique books on library shelves'],
    ['1554906493-4812e307243d', '1JBOZwuW7sI', 'Wooden ladder beside shelves of historic books'],
    ['1666993424332-9c24787b14cc', 'slfuNqxoBHk', 'Domed state capitol building and its grounds'],
  ]],
] : [
  ['law', 'bicycle-helmet-laws-by-state', [
    ['1611485100985-cb332cd79671', 'ANt3z4PYFNA', 'Red bicycle helmet resting on a silver bicycle'],
    ['1590093105704-fddd246ab64f', 'mZKF19ydEzk', 'White bicycle helmet with ventilation openings'],
    ['1562595790-27a12bf92219', 'VYHastjZd-s', 'White and blue mountain bike helmet'],
  ]],
  ['law', 'minimum-driving-age-by-state', [
    ['1581887605604-b1ec3bdac666', 'PH0e3VW_ORE', 'Driver holding a steering wheel during a daytime journey'],
    ['1581028735899-df24e30aab4e', '4wcj9LVnnLg', 'View from inside a car during a daytime drive'],
    ['1636012474705-b91610743811', 'z2Fa_e89XW8', 'Close-up of a driver holding the steering wheel'],
  ]],
  ['government', 'state-legislature-size-by-state', [
    ['1617804012886-1d9332082589', 'UbuKpqIb78U', 'Utah State Capitol beneath a blue sky'],
    ['1732723416426-8c3d5a826cb7', 'yMMAoNyt-T4', 'State capitol building with columns and an American flag'],
    ['1666993424332-9c24787b14cc', 'slfuNqxoBHk', 'Domed state capitol building and its grounds'],
  ]],
  ['economy', 'tipped-minimum-wage-by-state', [
    ['1542878232-0cb63077e944', 'AMMRy9-RzXE', 'Restaurant server assisting customers at a table'],
    ['1612434644663-3ec6eed0c0eb', 'R2MnhHhYyr0', 'Restaurant service with customers seated at tables'],
    ['1756158449200-678a8de156ac', 'jAn80ymalec', 'Cafe counter with pastries and coffee equipment'],
  ]],
  ['geography', 'record-high-temperature-by-state', [
    ['1724210578784-48bdd54ecd19', 'diguKnmUjTQ', 'Sunlit badlands at Zabriskie Point in Death Valley'],
    ['1495291718851-f86136152036', null, 'Barren ridges and desert landscape in Death Valley'],
    ['1729673694203-d82db55ecd2e', 'eEX_bFp9u8U', 'Dry cracked earth illustrating drought and extreme heat'],
  ]],
];
const outputDir = `artifacts/${batch}`;
await fs.mkdir(outputDir, { recursive: true });
const manifest = [];
const tiles = [];
for (const [category, slug, photos] of pages) {
  const file = `Content/rankings/${category}/${slug}.yml`;
  let text = await fs.readFile(file, 'utf8');
  const original = parse(text);
  if (original.hero_image || original.visual_assets) throw Error(`Existing assets: ${slug}`);
  const sections = original.sections.filter(s => s.paragraphs?.length && !s.table && !s.map && s.id !== 'table-intro').slice(0, 2);
  if (!jobsBatch && !healthBatch && !workBatch && sections.length !== 2) throw Error(`Missing prose sections: ${slug}`);
  const dir = `/images/rankings/${category}/${slug}`;
  await fs.mkdir(`wwwroot${dir}`, { recursive: true });
  for (let i = 0; i < Math.min(photos.length, sections.length + 1); i++) {
    const [photo, id, alt, sourceOverride] = photos[i];
    const src = `${dir}/${i ? `section-${i}` : 'hero'}.jpg`;
    const imageUrl = `https://images.unsplash.com/photo-${photo}?auto=format&fit=crop&w=1600&h=900&q=85`;
    const response = await fetch(imageUrl, { signal: AbortSignal.timeout(60000) });
    if (!response.ok) throw Error(`Download ${slug}: ${response.status}`);
    await sharp(Buffer.from(await response.arrayBuffer())).resize(1600, 900, { fit: 'cover' }).jpeg({ quality: 85, mozjpeg: true }).toFile(`wwwroot${src}`);
    const metadata = await sharp(`wwwroot${src}`).metadata();
    if (metadata.width !== 1600 || metadata.height !== 900) throw Error(src);
    const index = manifest.length;
    tiles.push({ input: await sharp(`wwwroot${src}`).resize(320, 180).toBuffer(), left: (index % 3) * 320, top: Math.floor(index / 3) * 180 });
    manifest.push({ page: slug, src, alt, section: i ? sections[i - 1].id : 'hero', source: sourceOverride || (id ? `https://unsplash.com/photos/${id}` : healthBatch ? 'https://unsplash.com/s/photos/activewear' : jobsBatch ? 'https://unsplash.com/s/photos/activewear' : 'https://unsplash.com/s/photos/death-valley'), imageUrl, license: 'https://unsplash.com/license', illustrative: true });
    console.log(`${slug}: ${i ? `section-${i}` : 'hero'}`);
  }
  const nl = text.includes('\r\n') ? '\r\n' : '\n';
  text = text.replace(/^author:/m, [`hero_image: "${dir}/hero.jpg"`, `hero_image_alt: ${JSON.stringify(photos[0][2])}`, '', 'author:'].join(nl));
  text = text.replace(/^faq:/m, ['visual_assets:', ...sections.flatMap((s, i) => [
    `  - id: section-${i + 1}`,
    `    src: "${dir}/section-${i + 1}.jpg"`,
    `    alt: ${JSON.stringify(photos[i + 1][2])}`,
    `    section: ${JSON.stringify(s.id)}`,
    '    layout: full-width-tall',
  ]), '', 'faq:'].join(nl));
  const updated = parse(text);
  if (sections.length === 0) text = text.replace(/^visual_assets:\r?\n\r?\n/m, '');
  const verified = parse(text);
  if ((verified.visual_assets?.length ?? 0) !== sections.length || verified.visual_assets?.some((a, i) => a.section !== sections[i].id || a.layout !== 'full-width-tall')) throw Error(slug);
  delete updated.hero_image;
  delete updated.hero_image_alt;
  delete updated.visual_assets;
  if (JSON.stringify(updated) !== JSON.stringify(original)) throw Error(`Unintended content changes: ${slug}`);
  await fs.writeFile(file, text);
}
await fs.writeFile(`${outputDir}/sources.json`, JSON.stringify(manifest, null, 2) + '\n');
await sharp({ create: { width: 960, height: Math.ceil(manifest.length / 3) * 180, channels: 3, background: 'white' } }).composite(tiles).jpeg().toFile(`${outputDir}/preview.jpg`);
console.log(`Validated ${manifest.length} photos, ${pages.length} YAML pages, prose section bindings and unchanged original content.`);
