import fs from 'node:fs/promises';
import path from 'node:path';
import sharp from 'sharp';
import { parse } from 'yaml';

const pages = [
  ['right-on-red-laws-by-state', [
    ['1556481323-2727d45e2921', '8BHgwMjxoNo', 'Close-up of an illuminated red traffic light'],
    ['1613549146788-7a448d14d1d0', 'klFzTXQERrY', 'Red traffic signal suspended above a city street'],
    ['1715645963336-675622d70bf2', 'wK0588AxHEY', 'Yellow traffic signal showing red beside city buildings'],
  ]],
  ['open-container-laws-by-state', [
    ['1562360469-ca4431597566', 'i-rDDt8E2iQ', 'Assortment of empty beer bottles'],
    ['1582981889862-604246f45c27', 'wmHn9NEzi9s', 'Three beer bottles on a wooden table'],
    ['1613166239017-4c86cadf604b', 'RvQCzbk_zeA', 'Brown glass beer bottles on a marble tabletop'],
  ]],
  ['lane-splitting-laws-by-state', [
    ['1597776090027-a84cbf9e95b2', 'ppIMEvKsgW0', 'Motorcyclist riding a winding road through red rock scenery'],
    ['1636616543445-2b1e1ec0c391', 'u8-9vmbu-xE', 'Helmeted motorcyclist on a two-lane forest road'],
    ['1694001953783-aae5271a4c3b', 'Mog_2FoBfxA', 'Motorcyclist navigating a curve on a forest road'],
  ]],
  ['pets-in-hot-cars-laws-by-state', [
    ['1620680779957-8901101c1162', '8QOFuMYXB1M', 'White dog looking out of a car window'],
    ['1719360567421-fda057165da5', 'zdXDZSuPiew', 'Dog sitting in the passenger seat of a car'],
    ['1590456150239-e6a671355c7a', 'KsdgjODuJQE', 'Brown long-haired dog looking through an open vehicle window'],
  ]],
  ['common-law-marriage-states', [
    ['1695918430470-44d6bc5b19f3', 'GeqSKKLaiEE', 'Couple walking hand in hand along a sunlit path'],
    ['1563899371025-f63c39efa10a', 'Dt5NZgX8R3I', 'Close-up of a couple holding hands while walking together'],
    ['1575722868199-4623812a9939', '7B6IhVlLXs0', 'Couple holding hands on a sandy beach'],
  ]],
];

const manifest = [];
for (const [slug, photos] of pages) {
  const yamlPath = `Content/rankings/law/${slug}.yml`;
  let content = await fs.readFile(yamlPath, 'utf8');
  const original = parse(content);
  if (original.hero_image || original.visual_assets) throw new Error(`Existing images: ${slug}`);
  const sections = original.sections.filter(s => s.paragraphs?.length).slice(0, 2);
  if (sections.length !== 2) throw new Error(`Missing text sections: ${slug}`);
  const publicDir = `/images/rankings/law/${slug}`;
  await fs.mkdir(`wwwroot${publicDir}`, { recursive: true });
  for (let i = 0; i < photos.length; i++) {
    const [photo, id, alt] = photos[i];
    const src = `${publicDir}/${i === 0 ? 'hero' : `section-${i}`}.jpg`;
    const imageUrl = `https://images.unsplash.com/photo-${photo}?auto=format&fit=crop&w=1600&h=900&q=85`;
    const response = await fetch(imageUrl, { signal: AbortSignal.timeout(60000) });
    if (!response.ok) throw new Error(`Download ${slug}: ${response.status}`);
    await sharp(Buffer.from(await response.arrayBuffer())).resize(1600, 900, { fit: 'cover' }).jpeg({ quality: 85, mozjpeg: true }).toFile(`wwwroot${src}`);
    manifest.push({ page: slug, src, alt, source: `https://unsplash.com/photos/${id}`, imageUrl, license: 'https://unsplash.com/license', illustrative: true });
    console.log(`${slug}: ${path.basename(src)}`);
  }
  const newline = content.includes('\r\n') ? '\r\n' : '\n';
  const hero = [`hero_image: "${publicDir}/hero.jpg"`, `hero_image_alt: ${JSON.stringify(photos[0][2])}`, ''].join(newline);
  content = content.replace(/^author:/m, `${hero}${newline}author:`);
  const assets = ['visual_assets:', ...sections.flatMap((s, i) => [
    `  - id: section-${i + 1}`,
    `    src: "${publicDir}/section-${i + 1}.jpg"`,
    `    alt: ${JSON.stringify(photos[i + 1][2])}`,
    `    section: ${JSON.stringify(s.id)}`,
    '    layout: full-width-tall',
  ]), '', ''].join(newline);
  content = content.replace(/^faq:/m, `${assets}faq:`);
  const updated = parse(content);
  if (updated.visual_assets.length !== 2 || updated.hero_image !== `${publicDir}/hero.jpg`) throw new Error(`Invalid images: ${slug}`);
  delete updated.visual_assets;
  delete updated.hero_image;
  delete updated.hero_image_alt;
  if (JSON.stringify(updated) !== JSON.stringify(original)) throw new Error(`Unintended content change: ${slug}`);
  await fs.writeFile(yamlPath, content);
}
await fs.mkdir('artifacts/driving-family-ranking-photos', { recursive: true });
await fs.writeFile('artifacts/driving-family-ranking-photos/sources.json', JSON.stringify(manifest, null, 2) + '\n');
console.log('Validated five pages and fifteen locally stored photos.');
