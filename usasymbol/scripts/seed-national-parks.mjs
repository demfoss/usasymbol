#!/usr/bin/env node
/**
 * NPS API → YAML seeder for national parks.
 *
 * Usage:
 *   NPS_API_KEY=your_key node scripts/seed-national-parks.mjs
 *   node scripts/seed-national-parks.mjs YOUR_API_KEY [--force]
 *
 * Get a free key: https://www.nps.gov/subjects/developer/get-started.htm
 *
 * --force   Overwrite existing YAML files (default: skip)
 * --dry-run Print what would be created without writing files
 *
 * What this script fills in:
 *   - slug, name, nps_code, location (lat/lng/state/region)
 *   - activities (mapped to site slugs), entrance fee, one NPS image
 *   - Google Maps URLs, NPS source link
 *
 * What you fill in manually:
 *   - stats.area_acres, stats.visitation_rank
 *   - quick_facts (established date, area, annual visitors)
 *   - filters.landscapes, filters.seasons, filters.reservation_status
 *   - All sections (overview, hiking, history, etc.)
 *   - FAQs, nearest_city, nearest_major_airport
 */

import { writeFileSync, existsSync, mkdirSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import https from 'https';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = join(__dirname, '..');
const OUTPUT_DIR = join(ROOT, 'Content', 'parks', 'national');

const API_KEY = process.env.NPS_API_KEY || process.argv[2];
const FORCE = process.argv.includes('--force');
const DRY_RUN = process.argv.includes('--dry-run');

if (!API_KEY || API_KEY.startsWith('--')) {
    console.error('Error: NPS API key required.');
    console.error('  NPS_API_KEY=your_key node scripts/seed-national-parks.mjs');
    console.error('  Get a free key: https://www.nps.gov/subjects/developer/get-started.htm');
    process.exit(1);
}

// ── Activity name → site slug ──────────────────────────────────────────
const ACTIVITY_MAP = {
    'Hiking': 'hiking',
    'Backpacking': 'backpacking',
    'Camping': 'camping',
    'Rock Climbing': 'rock_climbing',
    'Climbing': 'rock_climbing',
    'Scenic Driving': 'scenic_driving',
    'Wildlife Watching': 'wildlife_watching',
    'Astronomy': 'stargazing',
    'Stargazing': 'stargazing',
    'Photography': 'photography',
    'Swimming': 'swimming',
    'Kayaking': 'kayaking',
    'Canoeing': 'canoeing',
    'Snorkeling': 'snorkeling',
    'Scuba Diving': 'diving',
    'Biking': 'biking',
    'Cycling': 'biking',
    'Fishing': 'fishing',
    'Cross-Country Skiing': 'skiing',
    'Skiing': 'skiing',
    'Snowshoeing': 'snowshoeing',
    'Horseback Riding': 'horseback_riding',
    'Surfing': 'surfing',
    'Whitewater Rafting': 'rafting',
    'Stand Up Paddleboarding': 'paddleboarding',
    'Boating': 'boating',
    'Sailing': 'sailing',
};

// ── State code → full name ─────────────────────────────────────────────
const STATE_NAMES = {
    AL: 'Alabama', AK: 'Alaska', AZ: 'Arizona', AR: 'Arkansas', CA: 'California',
    CO: 'Colorado', CT: 'Connecticut', DE: 'Delaware', FL: 'Florida', GA: 'Georgia',
    HI: 'Hawaii', ID: 'Idaho', IL: 'Illinois', IN: 'Indiana', IA: 'Iowa',
    KS: 'Kansas', KY: 'Kentucky', LA: 'Louisiana', ME: 'Maine', MD: 'Maryland',
    MA: 'Massachusetts', MI: 'Michigan', MN: 'Minnesota', MS: 'Mississippi', MO: 'Missouri',
    MT: 'Montana', NE: 'Nebraska', NV: 'Nevada', NH: 'New Hampshire', NJ: 'New Jersey',
    NM: 'New Mexico', NY: 'New York', NC: 'North Carolina', ND: 'North Dakota', OH: 'Ohio',
    OK: 'Oklahoma', OR: 'Oregon', PA: 'Pennsylvania', RI: 'Rhode Island', SC: 'South Carolina',
    SD: 'South Dakota', TN: 'Tennessee', TX: 'Texas', UT: 'Utah', VT: 'Vermont',
    VA: 'Virginia', WA: 'Washington', WV: 'West Virginia', WI: 'Wisconsin', WY: 'Wyoming',
    DC: 'Washington D.C.', VI: 'U.S. Virgin Islands', AS: 'American Samoa', GU: 'Guam',
};

// ── State code → region ────────────────────────────────────────────────
const STATE_REGION = {
    ME: 'Northeast', VT: 'Northeast', NH: 'Northeast', MA: 'Northeast', RI: 'Northeast',
    CT: 'Northeast', NY: 'Northeast', NJ: 'Northeast', PA: 'Northeast', MD: 'Northeast',
    DE: 'Northeast', DC: 'Northeast',
    VA: 'Southeast', WV: 'Southeast', NC: 'Southeast', SC: 'Southeast', GA: 'Southeast',
    FL: 'Southeast', AL: 'Southeast', MS: 'Southeast', TN: 'Southeast', KY: 'Southeast',
    AR: 'Southeast', LA: 'Southeast',
    OH: 'Midwest', IN: 'Midwest', IL: 'Midwest', MI: 'Midwest', WI: 'Midwest',
    MN: 'Midwest', IA: 'Midwest', MO: 'Midwest', ND: 'Midwest', SD: 'Midwest',
    NE: 'Midwest', KS: 'Midwest',
    TX: 'Southwest', OK: 'Southwest', NM: 'Southwest', AZ: 'Southwest', NV: 'Southwest',
    CO: 'Rockies', UT: 'Rockies', WY: 'Rockies', MT: 'Rockies', ID: 'Rockies',
    CA: 'West', OR: 'West', WA: 'West',
    AK: 'Alaska',
    HI: 'Pacific', VI: 'Pacific', AS: 'Pacific', GU: 'Pacific',
};

// ─────────────────────────────────────────────────────────────────────
function toSlug(name) {
    return name.toLowerCase()
        .replace(/[''`]/g, '')
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-|-$/g, '');
}

function fetchJson(url) {
    return new Promise((resolve, reject) => {
        https.get(url, { headers: { 'User-Agent': 'USASymbol-seed/1.0' } }, (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                try { resolve(JSON.parse(data)); }
                catch (e) { reject(new Error(`JSON parse error for ${url}: ${e.message}`)); }
            });
        }).on('error', reject);
    });
}

function yamlStr(str) {
    if (str === null || str === undefined) return '""';
    str = String(str).trim();
    if (!str) return '""';
    // Use double-quoted string to be safe
    return '"' + str.replace(/\\/g, '\\\\').replace(/"/g, '\\"').replace(/\r?\n/g, ' ').replace(/\t/g, ' ') + '"';
}

function buildYaml(park) {
    const lat = parseFloat(park.latitude) || 0;
    const lng = parseFloat(park.longitude) || 0;
    const stateCodes = park.states ? park.states.split(',').map(s => s.trim()).filter(Boolean) : [];
    const primaryCode = stateCodes[0] || '';
    const primaryState = STATE_NAMES[primaryCode] || primaryCode;
    const region = STATE_REGION[primaryCode] || 'West';
    const slug = toSlug(park.fullName);

    // Multi-state parks: list all state names for display
    const allStateNames = stateCodes.map(c => STATE_NAMES[c] || c);

    // Activities
    const activities = [...new Set(
        (park.activities || [])
            .map(a => ACTIVITY_MAP[a.name])
            .filter(Boolean)
    )];
    if (activities.length === 0) activities.push('hiking');

    // Images: hero (first) + up to 4 highlights from NPS CDN
    const allImages = park.images || [];
    const heroImg = allImages[0];
    const imgUrl = heroImg ? heroImg.url : '';
    const imgAlt = heroImg ? (heroImg.altText || heroImg.title || `${park.fullName} landscape`) : '';
    const imgCredit = heroImg ? (heroImg.credit || 'NPS') : 'NPS';
    const highlightImgs = allImages.slice(1, 5);

    // Entrance fee
    let feeDisplay = 'Free';
    let hasFee = false;
    if (park.entranceFees && park.entranceFees.length > 0) {
        const vehicleFee = park.entranceFees.find(f =>
            (f.title || '').toLowerCase().includes('vehicle') ||
            (f.description || '').toLowerCase().includes('vehicle')
        );
        const fee = vehicleFee || park.entranceFees[0];
        const cost = parseFloat(fee.cost || '0');
        if (cost > 0) {
            hasFee = true;
            feeDisplay = vehicleFee
                ? `$${Math.round(cost)}/vehicle`
                : `$${Math.round(cost)}`;
        }
    }

    const googleSearch = (lat && lng)
        ? `https://www.google.com/maps/search/?api=1&query=${lat},${lng}`
        : '';
    const googleDir = (lat && lng)
        ? `https://www.google.com/maps/dir/?api=1&destination=${lat},${lng}`
        : '';

    const today = new Date().toISOString().split('T')[0];

    const intro = park.description
        ? park.description.replace(/\r?\n/g, ' ').substring(0, 220).trim() +
          (park.description.length > 220 ? '...' : '')
        : '';

    const activitiesYaml = activities.map(a => `    - ${a}`).join('\n');

    return `id: ${slug}
slug: ${slug}
designation: national_park
name: ${yamlStr(park.fullName)}
nps_code: ${yamlStr(park.parkCode)}

seo_title: ${yamlStr(`${park.fullName}: Map, Things to Do, Best Time to Visit & Tips`)}
seo_description: ${yamlStr(`Plan your trip to ${park.fullName}: where it is, top sights, best time to visit, entrance fees, and hiking.`)}
intro_text: ${yamlStr(intro)}

author: "USASymbol Editorial Team"
date_published: "${today}"
date_modified: "${today}"

location:
  state: ${yamlStr(primaryState)}
  state_code: "${primaryCode}"
  region: "${region}"
  latitude: ${lat}
  longitude: ${lng}
  nearest_city: ""
  nearest_major_airport: ""

map:
  zoom: 10
  google_search_url: ${yamlStr(googleSearch)}
  google_directions_url: ${yamlStr(googleDir)}

# Fill in area_acres (Wikipedia/NPS reports) and visitation_rank (1 = most visited among 63 national parks)
stats:
  area_acres: 0
  visitation_rank: 0
  entrance_fee_display: ${yamlStr(feeDisplay)}

quick_facts:
  - label: "Established"
    value: ""
  - label: "Area"
    value: ""
  - label: "Annual visitors"
    value: ""
  - label: "Entrance fee"
    value: ${yamlStr(hasFee ? feeDisplay : 'Free')}
  - label: "NPS code"
    value: "${(park.parkCode || '').toUpperCase()}"
  - label: "State"
    value: ${yamlStr(allStateNames.join(', '))}

filters:
  landscapes: []
  activities:
${activitiesYaml}
  has_entrance_fee: ${hasFee}
  reservation_status: ""
  seasons:
    - spring
    - summer
    - fall

media:
  hero_image: ${imgUrl ? yamlStr(imgUrl) : '""'}
  hero_alt: ${yamlStr(imgAlt)}
  hero_credit: ${yamlStr(imgCredit)}
  highlights: ${highlightImgs.length === 0 ? '[]' : '\n' + highlightImgs.map(i =>
    `    - image: ${yamlStr(i.url)}\n      alt: ${yamlStr(i.altText || i.title || '')}\n      credit: ${yamlStr(i.credit || 'NPS')}`
  ).join('\n')}

sections:
  overview: ""
  known_for: ""
  best_things_to_see: ""
  best_time_to_visit: ""
  hiking: ""
  camping: ""
  fees_reservations: ""
  getting_there: ""
  geology: ""
  wildlife: ""
  history: ""

faq: []

sources:
  - name: ${yamlStr(`National Park Service — ${park.fullName}`)}
    url: ${yamlStr(`https://www.nps.gov/${park.parkCode}/`)}
    description: "Official NPS page with current fees, alerts, and visitor information."
`;
}

async function main() {
    const apiUrl = `https://developer.nps.gov/api/v1/parks?limit=500&api_key=${API_KEY}&fields=images,entranceFees,activities,topics`;

    console.log('Fetching parks from NPS API…');

    let data;
    try {
        data = await fetchJson(apiUrl);
    } catch (e) {
        console.error('Failed to fetch parks:', e.message);
        if (e.message.includes('401') || e.message.includes('403')) {
            console.error('Check your API key at https://www.nps.gov/subjects/developer/get-started.htm');
        }
        process.exit(1);
    }

    const NP_DESIGNATIONS = new Set([
        'National Park',
        'National Park & Preserve',
        'National Park and Preserve',
        'National Parks',
        'National and State Parks',
    ]);
    // American Samoa has empty designation in the API but is an official national park
    const NP_PARK_CODES = new Set(['npsa']);
    const nationalParks = (data.data || []).filter(p =>
        NP_DESIGNATIONS.has(p.designation) || NP_PARK_CODES.has(p.parkCode)
    );
    console.log(`Found ${nationalParks.length} parks with designation "National Park"`);

    if (nationalParks.length === 0) {
        console.error('No parks found. Check your API key and try again.');
        process.exit(1);
    }

    if (!DRY_RUN) {
        mkdirSync(OUTPUT_DIR, { recursive: true });
    }

    let created = 0, skipped = 0;

    for (const park of nationalParks.sort((a, b) => a.fullName.localeCompare(b.fullName))) {
        const slug = toSlug(park.fullName);
        const outPath = join(OUTPUT_DIR, `${slug}.yml`);
        const exists = existsSync(outPath);

        if (exists && !FORCE) {
            console.log(`  SKIP  ${slug}`);
            skipped++;
            continue;
        }

        const yaml = buildYaml(park);

        if (DRY_RUN) {
            console.log(`  DRY   ${slug}`);
            created++;
            continue;
        }

        writeFileSync(outPath, yaml, 'utf8');
        console.log(`  ${exists ? 'OVERWR' : 'CREATE'} ${slug}`);
        created++;
    }

    console.log('\n─────────────────────────────────────────');
    console.log(`${created} ${DRY_RUN ? 'would be' : ''} created${FORCE ? '/overwritten' : ''}, ${skipped} skipped`);

    if (!DRY_RUN && created > 0) {
        console.log('\nNext steps:');
        console.log('  1. Fill in stats.area_acres and stats.visitation_rank for each park');
        console.log('     (source: Wikipedia or NPS annual visitation reports)');
        console.log('  2. Fill in nearest_city, established date, area in quick_facts');
        console.log('  3. Write sections: overview, hiking, history, etc.');
        console.log('  4. Set filters.landscapes and filters.seasons');
        console.log('  5. Download hero images locally to /images/parks/{slug}/hero.jpg');
    }

    if (skipped > 0 && !FORCE) {
        console.log(`\n  Use --force to overwrite the ${skipped} existing file(s).`);
    }
}

main().catch(e => { console.error(e); process.exit(1); });
