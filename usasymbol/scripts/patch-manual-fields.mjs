#!/usr/bin/env node
/**
 * Fills in nearest_city, nearest_major_airport, reservation_status
 * for all national park YAML files. Never overwrites non-empty values.
 *
 * Usage:
 *   node scripts/patch-manual-fields.mjs [--dry-run]
 */

import { readFileSync, writeFileSync, readdirSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const PARKS_DIR = join(__dirname, '..', 'Content', 'parks', 'national');
const DRY_RUN   = process.argv.includes('--dry-run');

// nps_code (lowercase) → manual fields
const MANUAL = {
  acad: { city: 'Bar Harbor, Maine',            airport: 'Bangor International (BGR), ~48 miles',                    reservation: 'seasonal'  },
  arch: { city: 'Moab, Utah',                   airport: 'Canyonlands Regional (CNY), ~18 miles',                    reservation: 'required'  },
  badl: { city: 'Wall, South Dakota',           airport: 'Rapid City Regional (RAP), ~75 miles',                     reservation: 'none'      },
  bibe: { city: 'Alpine, Texas',                airport: 'Midland International Air & Space Port (MAF), ~165 miles', reservation: 'none'      },
  bisc: { city: 'Homestead, Florida',           airport: 'Miami International (MIA), ~25 miles',                     reservation: 'none'      },
  blca: { city: 'Montrose, Colorado',           airport: 'Montrose Regional (MTJ), ~15 miles',                       reservation: 'none'      },
  brca: { city: 'Bryce Canyon City, Utah',      airport: 'St. George Regional (SGU), ~80 miles',                     reservation: 'none'      },
  cany: { city: 'Moab, Utah',                   airport: 'Canyonlands Regional (CNY), ~30 miles',                    reservation: 'none'      },
  care: { city: 'Torrey, Utah',                 airport: 'Salt Lake City International (SLC), ~215 miles',           reservation: 'none'      },
  cave: { city: 'Carlsbad, New Mexico',         airport: 'El Paso International (ELP), ~150 miles',                  reservation: 'required'  },
  chis: { city: 'Ventura, California',          airport: 'Los Angeles International (LAX), ~65 miles',               reservation: 'none'      },
  cong: { city: 'Hopkins, South Carolina',      airport: 'Columbia Metropolitan (CAE), ~20 miles',                   reservation: 'none'      },
  crla: { city: 'Klamath Falls, Oregon',        airport: 'Rogue Valley International–Medford (MFR), ~75 miles',      reservation: 'none'      },
  cuva: { city: 'Akron, Ohio',                  airport: 'Cleveland Hopkins International (CLE), ~25 miles',         reservation: 'none'      },
  dena: { city: 'Healy, Alaska',                airport: 'Ted Stevens Anchorage International (ANC), ~240 miles',    reservation: 'seasonal'  },
  deva: { city: 'Pahrump, Nevada',              airport: 'Harry Reid International, Las Vegas (LAS), ~120 miles',    reservation: 'none'      },
  drto: { city: 'Key West, Florida',            airport: 'Key West International (EYW), ferry or seaplane only',     reservation: 'required'  },
  ever: { city: 'Homestead, Florida',           airport: 'Miami International (MIA), ~45 miles',                     reservation: 'none'      },
  gaar: { city: 'Fairbanks, Alaska',            airport: 'Fairbanks International (FAI), no road access to park',    reservation: 'none'      },
  glac: { city: 'Whitefish, Montana',           airport: 'Glacier Park International (FCA), ~30 miles',              reservation: 'seasonal'  },
  glba: { city: 'Gustavus, Alaska',             airport: 'Juneau International (JNU), ~45 miles by air',             reservation: 'none'      },
  grba: { city: 'Baker, Nevada',                airport: 'Salt Lake City International (SLC), ~285 miles',           reservation: 'none'      },
  grca: { city: 'Williams, Arizona',            airport: 'Flagstaff Pulliam (FLG), ~80 miles south',                 reservation: 'none'      },
  grsa: { city: 'Alamosa, Colorado',            airport: 'Alamosa San Luis Valley Regional (ALS), ~38 miles',        reservation: 'none'      },
  grsm: { city: 'Gatlinburg, Tennessee',        airport: 'McGhee Tyson Airport, Knoxville (TYS), ~45 miles',         reservation: 'none'      },
  grte: { city: 'Jackson, Wyoming',             airport: 'Jackson Hole Airport (JAC), ~12 miles',                    reservation: 'none'      },
  gumo: { city: 'Carlsbad, New Mexico',         airport: 'El Paso International (ELP), ~110 miles',                  reservation: 'none'      },
  hale: { city: 'Kula, Maui, Hawaii',           airport: 'Kahului Airport (OGG), ~35 miles',                         reservation: 'required'  },
  havo: { city: 'Hilo, Hawaii',                 airport: 'Hilo International (ITO), ~30 miles',                      reservation: 'none'      },
  hosp: { city: 'Hot Springs, Arkansas',        airport: 'Bill and Hillary Clinton National (LIT), ~55 miles',       reservation: 'none'      },
  indu: { city: 'Chesterton, Indiana',          airport: 'Chicago Midway (MDW), ~45 miles',                          reservation: 'none'      },
  isro: { city: 'Houghton, Michigan',           airport: 'Houghton County Memorial (CMX), ~5 miles',                 reservation: 'required'  },
  jeff: { city: 'St. Louis, Missouri',          airport: 'St. Louis Lambert International (STL), ~15 miles',         reservation: 'none'      },
  jotr: { city: 'Twentynine Palms, California', airport: 'Palm Springs International (PSP), ~40 miles',              reservation: 'seasonal'  },
  katm: { city: 'King Salmon, Alaska',          airport: 'King Salmon Airport (AKN), ~10 miles via Anchorage',       reservation: 'required'  },
  kefj: { city: 'Seward, Alaska',               airport: 'Ted Stevens Anchorage International (ANC), ~125 miles',    reservation: 'none'      },
  kova: { city: 'Kotzebue, Alaska',             airport: 'Ralph Wien Memorial (OTZ), ~75 miles',                     reservation: 'none'      },
  lacl: { city: 'Port Alsworth, Alaska',        airport: 'Ted Stevens Anchorage International (ANC), ~150 miles',    reservation: 'none'      },
  lavo: { city: 'Redding, California',          airport: 'Redding Municipal (RDD), ~50 miles',                       reservation: 'none'      },
  maca: { city: 'Cave City, Kentucky',          airport: 'Nashville International (BNA), ~90 miles',                 reservation: 'required'  },
  meve: { city: 'Cortez, Colorado',             airport: 'Durango–La Plata County (DRO), ~35 miles',                 reservation: 'required'  },
  mora: { city: 'Ashford, Washington',          airport: 'Seattle-Tacoma International (SEA), ~60 miles',            reservation: 'seasonal'  },
  neri: { city: 'Fayetteville, West Virginia',  airport: 'Yeager Airport, Charleston (CRW), ~65 miles',              reservation: 'none'      },
  noca: { city: 'Marblemount, Washington',      airport: 'Seattle-Tacoma International (SEA), ~105 miles',           reservation: 'none'      },
  npsa: { city: 'Pago Pago, American Samoa',    airport: 'Pago Pago International (PPG), ~10 miles',                 reservation: 'none'      },
  olym: { city: 'Port Angeles, Washington',     airport: 'Seattle-Tacoma International (SEA), ~90 miles',            reservation: 'none'      },
  pefo: { city: 'Holbrook, Arizona',            airport: 'Flagstaff Pulliam (FLG), ~90 miles',                       reservation: 'none'      },
  pinn: { city: 'Soledad, California',          airport: 'San Jose International (SJC), ~80 miles',                  reservation: 'seasonal'  },
  redw: { city: 'Crescent City, California',    airport: 'Arcata–Eureka Airport (ACV), ~75 miles',                   reservation: 'none'      },
  romo: { city: 'Estes Park, Colorado',         airport: 'Denver International (DEN), ~70 miles',                    reservation: 'seasonal'  },
  sagu: { city: 'Tucson, Arizona',              airport: 'Tucson International (TUS), ~10 miles',                    reservation: 'none'      },
  seki: { city: 'Visalia, California',          airport: 'Fresno Yosemite International (FAT), ~55 miles',           reservation: 'seasonal'  },
  shen: { city: 'Luray, Virginia',              airport: 'Dulles International (IAD), ~75 miles',                    reservation: 'none'      },
  thro: { city: 'Medora, North Dakota',         airport: 'Bismarck Municipal (BIS), ~135 miles',                     reservation: 'none'      },
  viis: { city: 'Cruz Bay, St. John, U.S. Virgin Islands', airport: 'Cyril E. King Airport, St. Thomas (STT), ferry only', reservation: 'none' },
  voya: { city: 'International Falls, Minnesota', airport: 'Duluth International (DLH), ~160 miles',                 reservation: 'none'      },
  whsa: { city: 'Alamogordo, New Mexico',       airport: 'El Paso International (ELP), ~85 miles',                   reservation: 'none'      },
  wica: { city: 'Hot Springs, South Dakota',    airport: 'Rapid City Regional (RAP), ~55 miles',                     reservation: 'required'  },
  wrst: { city: 'McCarthy, Alaska',             airport: 'Fairbanks International (FAI), ~300 miles',                reservation: 'none'      },
  yell: { city: 'West Yellowstone, Montana',    airport: 'Bozeman Yellowstone International (BZN), ~90 miles',       reservation: 'none'      },
  yose: { city: 'Mariposa, California',         airport: 'Fresno Yosemite International (FAT), ~65 miles south',     reservation: 'seasonal'  },
  zion: { city: 'Springdale, Utah',             airport: 'St. George Regional (SGU), ~45 miles',                     reservation: 'none'      },
};

function patchField(content, key, newValue) {
    // Match: key: "" (empty string only)
    const pattern = new RegExp(`(  ${key}:\\s*)""`, 'm');
    if (!pattern.test(content)) return content; // already filled or key not found
    const safe = newValue.replace(/\\/g, '\\\\').replace(/"/g, '\\"');
    return content.replace(pattern, `$1"${safe}"`);
}

const files = readdirSync(PARKS_DIR).filter(f => f.endsWith('.yml')).sort();
console.log(`Found ${files.length} park files\n`);

let updated = 0, skipped = 0, unknown = 0;

for (const file of files) {
    const filePath = join(PARKS_DIR, file);
    let content = readFileSync(filePath, 'utf8');
    const original = content;

    // Extract nps_code from file
    const m = content.match(/^nps_code:\s*"?([^"\n]+)"?/m);
    const npsCode = m ? m[1].trim().toLowerCase() : '';

    const data = MANUAL[npsCode];
    if (!data) {
        console.log(`  ??     ${file} (nps_code="${npsCode}" not in MANUAL dataset)`);
        unknown++;
        continue;
    }

    content = patchField(content, 'nearest_city',          data.city);
    content = patchField(content, 'nearest_major_airport', data.airport);
    content = patchField(content, 'reservation_status',    data.reservation);

    if (content !== original) {
        if (!DRY_RUN) writeFileSync(filePath, content, 'utf8');
        console.log(`  UPDATE ${file}`);
        updated++;
    } else {
        console.log(`  ok     ${file}`);
        skipped++;
    }
}

console.log('\n────────────────────────────────────────────────────────────');
console.log(`Updated: ${updated}  |  Already complete: ${skipped}  |  Unknown: ${unknown}`);
if (DRY_RUN) console.log('(dry-run — nothing written)');
