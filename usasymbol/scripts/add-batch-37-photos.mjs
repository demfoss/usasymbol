import fs from 'node:fs/promises';
import sharp from 'sharp';
import {parse} from 'yaml';
const output='artifacts/batch-37-photos';
const pages=JSON.parse(await fs.readFile(`${output}/audit.json`,'utf8'));
const governorSlug='age-requirements-for-governor-by-state';
const governor=parse(await fs.readFile(`Content/rankings/law/${governorSlug}.yml`,'utf8'));
pages.splice(23,0,{slug:governorSlug,category:'law',sections:governor.sections.filter(s=>s.paragraphs?.length&&!s.table&&!s.map).slice(0,2).map(s=>s.id)});
const pool={};
for(const config of ['healthcare-salary-photos','professional-salary-photos','jobs-and-pay-laws-photos','work-laws-trades-photos','chain-locations-photos']){
 for(const [,slug,photos] of JSON.parse(await fs.readFile(`scripts/${config}.json`,'utf8')))pool[slug]=photos;
}
const photo=(key,id,source,alt)=>pool[key]=[[id,source,alt]];
photo('wood','1756736668332-e921516c1305',null,'Woodworking at a workbench');
photo('mechanic','1767681092416-bccf9410bda4','VdXnpwrp1qc','Cars being repaired in an automotive workshop');
photo('lathe','1776090188130-26c7253ff423','_VBsh_IKsD8','Metal lathe in a machine workshop');
photo('construction','1752342625685-bc342beb248e','qKKbm3MjVA8','Excavator working at a construction site');
photo('earthwork','1647569699563-1b7a713a32ea','N8gLtdr4xLI','Excavator digging at a construction site');
photo('chef','1745236549199-542fe7a368f4','J8z7D4124WA','Chef preparing food in a professional kitchen');
photo('chefs','1721637713270-5470ea1d6389','80GJUuOhXgA','Chefs working together in a kitchen');
photo('salon','1695527081848-1e46c06e6458','PnDr2j28gXA','Hairdresser styling a client in a salon');
photo('salon2','1695527081874-b674c46f40fb','KMvUcGanzPc','Clients and stylists in a hair salon');
photo('massage','1745327883290-1e9c6447b938','oAvwAKFiU7Q','Massage room prepared for a treatment');
photo('bus','1664353656406-c4fcd6707cc1','_bL5NCgkaW8','Yellow school bus parked in a lot');
photo('bottles','1743342716826-1fb32cc467d9','ARCS27AGiSs','Plastic beverage bottles collected for recycling');
photo('vote','1722238451613-655732282ff2','TGxarF-ZBN8','Vote buttons featuring the American flag');
photo('cannabis','1745422315575-91a086c0f0eb','d56lrxRRLPA','Cannabis plants growing in a greenhouse');
photo('cigarette','1555441293-6c6fb1eb9773','iGcQPnpDQ-Q','Lit cigarette with smoke and ash');
photo('cigarette2','1566421415516-605eaf996e75','BPm5DMSop8A','Single cigarette with a glowing tip');
photo('beach','1654119955303-156bdc9dfb17','d1CZMGW3j7s','Person sunbathing on a towel at a beach');
const local=(key,src,alt)=>pool[key]=[{local:src,alt,source:`Repository asset: ${src}`}];
local('capitol','/images/rankings/government/governor-salary-by-state/hero.jpg','Indiana Statehouse and its grounds');
local('capitol2','/images/rankings/government/state-legislature-size-by-state/hero.jpg','State capitol building beneath a blue sky');
local('books','/images/rankings/government/state-constitution-length-by-state/hero.jpg','Bound books on library shelves');
local('books2','/images/rankings/government/state-constitution-length-by-state/section-1.jpg','Library books and a wooden ladder');
local('couple','/images/rankings/law/common-law-marriage-states/section-1.jpg','Couple holding hands while walking together');
local('couple2','/images/rankings/law/common-law-marriage-states/section-2.jpg','Couple holding hands on a beach');
local('beer','/images/rankings/law/open-container-laws-by-state/hero.jpg','Assortment of beer bottles');
local('beer2','/images/rankings/law/open-container-laws-by-state/section-1.jpg','Beer bottles on a wooden table');
local('school','/images/rankings/education/number-of-days-in-school-year-by-state/kansas-classroom.jpg','School classroom');
local('power','/images/rankings/infrastructure/power-outages-by-state/hero.jpg','Electrical infrastructure illustrating utility work');
local('guns','/images/rankings/law/ammunition-limits-by-state/section-1.jpg','Ammunition illustrating firearm regulations');
local('nature','/images/rankings/geography/record-high-temperature-by-state/hero.jpg','Natural landscape and sunlit desert ridges');
const mapping=[
 ['wood','construction'],['power'],['mechanic','truck-driver-salary-by-state'],['mechanic'],['lathe'],['construction','earthwork'],['earthwork','construction'],['construction','earthwork'],
 ['paid-family-leave-states'],['chef','chefs'],['salon','salon2'],['massage','occupational-therapist-salary-by-state'],['dentist-salary-by-state','dental-hygienist-salary-by-state'],['pharmacist-salary-by-state','medical-assistant-salary-by-state'],['medical-assistant-salary-by-state','licensed-practical-nurse-salary-by-state'],
 ['books','books2'],['school','books'],['bus'],['lawyer-salary-by-state','books','capitol'],['capitol','capitol2'],['guns','lawyer-salary-by-state'],['bottles','whole-foods-locations-by-state','beer'],['couple','couple2'],['capitol','capitol2'],
 ['beer','beer2','bottles'],['books','books2'],['vote','capitol','capitol2'],['beach','salon','massage'],['nature','books','capitol'],['whole-foods-locations-by-state','bottles','nature'],['cybersecurity-analyst-salary-by-state','mechanic','lathe'],['accountant-salary-by-state','social-worker-salary-by-state','financial-analyst-salary-by-state'],['paid-family-leave-states','books','couple'],['guns','lawyer-salary-by-state','capitol'],['cannabis','nature','books'],['cigarette','cigarette2','lawyer-salary-by-state'],['capitol','capitol2','books']
];
const cache=new Map();const manifest=[];const overview=[];
await fs.mkdir(output,{recursive:true});
async function bitmap(asset){
 if(asset.local)return sharp(`wwwroot${asset.local}`).resize(1600,900,{fit:'cover'}).jpeg({quality:85,mozjpeg:true}).toBuffer();
 const [id]=asset;if(cache.has(id))return cache.get(id);
 const url=`https://images.unsplash.com/photo-${id}?auto=format&fit=crop&w=1600&h=900&q=85`;
 const r=await fetch(url,{signal:AbortSignal.timeout(60000)});if(!r.ok)throw Error(`${id}: ${r.status}`);
 const data=await sharp(Buffer.from(await r.arrayBuffer())).resize(1600,900,{fit:'cover'}).jpeg({quality:85,mozjpeg:true}).toBuffer();cache.set(id,data);return data;
}
for(let p=0;p<pages.length;p++){
 const page=pages[p],file=`Content/rankings/${page.category}/${page.slug}.yml`;let text=await fs.readFile(file,'utf8');const original=parse(text);const nl=text.includes('\r\n')?'\r\n':'\n';
 const assets=mapping[p].flatMap(key=>pool[key]);if(!assets.length)throw Error(page.slug);
 const dir=`/images/rankings/${page.category}/${page.slug}`;await fs.mkdir(`wwwroot${dir}`,{recursive:true});
 const added=[];let offset=0;
 async function save(asset,name,section){
  const src=`${dir}/${name}.jpg`,data=await bitmap(asset);await fs.writeFile(`wwwroot${src}`,data);const meta=await sharp(data).metadata();if(meta.width!==1600||meta.height!==900)throw Error(src);
  const alt=asset.alt||asset[2],source=asset.source||(asset[1]?`https://unsplash.com/photos/${asset[1]}`:`https://images.unsplash.com/photo-${asset[0]}`);
  manifest.push({page:page.slug,src,alt,section,source,license:asset.local?'Existing repository asset':'https://unsplash.com/license',illustrative:true});return {src,alt};
 }
 if(!original.hero_image){const hero=await save(assets[0],'hero','hero');text=text.replace(/^author:/m,`hero_image: ${JSON.stringify(hero.src)}${nl}hero_image_alt: ${JSON.stringify(hero.alt)}${nl}${nl}author:`);offset=1;}
 for(let i=0;i<page.sections.length;i++){
  const section=page.sections[i];if(original.visual_assets?.some(a=>a.section===section))continue;
  const asset=assets[(i+offset)%assets.length];const saved=await save(asset,`section-${i+1}`,section);added.push({id:`section-${i+1}`,src:saved.src,alt:saved.alt,section,layout:'full-width-tall'});
 }
 if(added.length){const block=added.flatMap(a=>[`  - id: ${a.id}`,`    src: ${JSON.stringify(a.src)}`,`    alt: ${JSON.stringify(a.alt)}`,`    section: ${JSON.stringify(a.section)}`,'    layout: full-width-tall']).join(nl)+nl;
 if(original.visual_assets)text=text.replace(/^visual_assets:\s*\r?\n/m,`visual_assets:${nl}${block}`);else text=text.replace(/^faq:/m,`visual_assets:${nl}${block}${nl}faq:`);}
 const updated=parse(text);const restored=structuredClone(updated);for(const key of ['hero_image','hero_image_alt','visual_assets']){if(Object.hasOwn(original,key))restored[key]=original[key];else delete restored[key];}
 if(JSON.stringify(restored)!==JSON.stringify(original))throw Error(`Content changed: ${page.slug}`);
 for(const a of added)if(!updated.visual_assets?.some(v=>v.src===a.src&&v.section===a.section&&v.layout==='full-width-tall'))throw Error(page.slug);
 await fs.writeFile(file,text);overview.push({page:page.slug,hero:updated.hero_image,added:manifest.filter(m=>m.page===page.slug).length});console.log(`${p+1}/37 ${page.slug}: ${overview.at(-1).added} photos added`);
}
await fs.writeFile(`${output}/sources.json`,JSON.stringify(manifest,null,2)+'\n');await fs.writeFile(`${output}/results.json`,JSON.stringify(overview,null,2)+'\n');
const tiles=[];for(let i=0;i<overview.length;i++)tiles.push({input:await sharp(`wwwroot${overview[i].hero}`).resize(240,135).toBuffer(),left:(i%4)*240,top:Math.floor(i/4)*135});
await sharp({create:{width:960,height:Math.ceil(overview.length/4)*135,channels:3,background:'white'}}).composite(tiles).jpeg().toFile(`${output}/preview.jpg`);
console.log(`Validated ${pages.length} pages; added ${manifest.length} photos; original content and existing heroes preserved.`);
