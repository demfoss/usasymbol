// usage: node check.js <candidates.txt>  (run from repo root)
// line format: category/slug|hub|angle|source
const fs = require('fs');
const ex = [];
for (const base of ['Content/rankings', 'Content/collections'])
  for (const c of fs.readdirSync(base))
    for (const f of fs.readdirSync(base + '/' + c)) ex.push(f.replace(/\.ya?ml$/, ''));
for (const f of ['PLAN.md', 'BACKLOG.md']) {
  if (!fs.existsSync(f)) continue;
  (fs.readFileSync(f, 'utf8').match(/\| [a-z]+\/[a-z0-9-]+ \|/g) || []).forEach(s => ex.push(s.slice(2, -2).split('/')[1]));
}
const stop = new Set('by state states laws law legal legality in the us of most salary cost per capita average number rate rates requirement requirements to a for with and production'.split(' '));
const tok = s => s.split('-').filter(t => !stop.has(t));
const L = fs.readFileSync(process.argv[2], 'utf8').trim().split(/\r?\n/);
const seen = new Set(); let ok = 0;
for (const l of L) {
  const slug = l.split('|')[0].split('/')[1];
  const t = tok(slug);
  if (seen.has(slug)) { console.log('DUP-IN-LIST', slug); continue; }
  seen.add(slug);
  const hit = ex.filter(e => { const et = new Set(e.split('-')); return t.length && t.every(x => et.has(x)); });
  if (hit.length) console.log('EXISTS?', slug, '=>', hit.slice(0, 3).join(', '));
  else ok++;
}
console.log('total', L.length, 'clean', ok);
