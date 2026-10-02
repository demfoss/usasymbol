import fs from 'node:fs/promises';
import sharp from 'sharp';
import { parse } from 'yaml';
const file = 'Content/collections/travel/largest-us-airlines.yml';
const dir = '/images/collections/travel/largest-us-airlines';
const output = 'artifacts/largest-us-airlines-photos';
const photos = [
  ['hero', '1717381991569-cad5bd2b9788', 'moXLWb4TZiU', 'American Airlines passenger jet flying beneath a blue sky'],
  ['section-1', '1703282311798-e527c069fd20', '-uev6tyhpK8', 'Delta Air Lines passenger jet in flight'],
];
await fs.mkdir(`wwwroot${dir}`, { recursive: true });
await fs.mkdir(output, { recursive: true });
const sources = [];
const tiles = [];
for (const [name, photo, id, alt] of photos) {
  const imageUrl = `https://images.unsplash.com/photo-${photo}?auto=format&fit=crop&w=1600&h=900&q=85`;
  const response = await fetch(imageUrl, { signal: AbortSignal.timeout(60000) });
  if (!response.ok) throw Error(`Download: ${response.status}`);
  const src = `${dir}/${name}.jpg`;
  await sharp(Buffer.from(await response.arrayBuffer())).resize(1600, 900).jpeg({ quality: 85, mozjpeg: true }).toFile(`wwwroot${src}`);
  const meta = await sharp(`wwwroot${src}`).metadata();
  if (meta.width !== 1600 || meta.height !== 900) throw Error(src);
  tiles.push({ input: await sharp(`wwwroot${src}`).resize(640, 360).toBuffer(), left: 0, top: sources.length * 360 });
  sources.push({ src, alt, source: `https://unsplash.com/photos/${id}`, imageUrl, license: 'https://unsplash.com/license' });
  console.log(src);
}
let text = await fs.readFile(file, 'utf8');
const original = parse(text);
if (original.visual_assets.some(a => a.id === 'airlines-overview-photo')) throw Error('Photo already present');
const nl = text.includes('\r\n') ? '\r\n' : '\n';
text = text.replace(/^hero_image:.*$/m, `hero_image: "${dir}/hero.jpg"`).replace(/^hero_image_alt:.*$/m, `hero_image_alt: ${JSON.stringify(photos[0][3])}`).replace(/^hero_image_caption:.*$/m, 'hero_image_caption: "American Airlines is headquartered in Fort Worth, Texas."');
text = text.replace(/^visual_assets:/m, ['visual_assets:', '  - id: airlines-overview-photo', `    src: "${dir}/section-1.jpg"`, `    alt: ${JSON.stringify(photos[1][3])}`, '    caption: "Delta Air Lines is headquartered in Atlanta, Georgia."', '    section: largest-us-airlines', '    layout: full-width-tall'].join(nl));
const updated = parse(text);
if (updated.visual_assets[0].section !== updated.sections[0].id) throw Error('Section mismatch');
updated.visual_assets.shift();
for (const key of ['hero_image', 'hero_image_alt', 'hero_image_caption']) updated[key] = original[key];
if (JSON.stringify(updated) !== JSON.stringify(original)) throw Error('Unintended content changes');
await fs.writeFile(file, text);
await fs.writeFile(`${output}/sources.json`, JSON.stringify(sources, null, 2) + '\n');
await sharp({ create: { width: 640, height: 720, channels: 3, background: 'white' } }).composite(tiles).jpeg().toFile(`${output}/preview.jpg`);
console.log('Validated two 1600x900 photos, section binding, and preservation of text, cards and flags.');
