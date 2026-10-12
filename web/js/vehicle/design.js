// Vehicle design schema (versioned) + shape validation + migrations.
// A design is plain JSON (safe to persist). Units: SI (kg, m).
import { PROPELLANT_BY_ID } from '../data/propellants.js';
import { ENGINE_BY_ID } from '../data/engines.js';
import { MATERIAL_BY_ID } from '../data/materials.js';
import { PAYLOAD_BY_ID, ORBITS } from '../data/payloads.js';

export const DESIGN_VERSION = 1;
export const MAX_STAGES = 5;
export const STRAPON_COUNTS = Object.freeze([0, 2, 4]);
export const NOSE_SHAPES = Object.freeze({ ogive: 0.30, conic: 0.36, blunt: 0.50, 'von-karman': 0.28 });
// Tank dome allowance: ellipsoidal domes (sqrt2:1) add ~1/3 diameter of length per tank pair.
export const DOME_LENGTH_FACTOR = 0.33;
// Ullage volume fraction.
export const TANK_ULLAGE = 0.03;
export const ENGINE_COUNT_MIN = 1;
export const ENGINE_COUNT_MAX = 40;
export const DIAMETER_MIN = 0.3;
export const DIAMETER_MAX = 15;
export const SAFETY_MIN = 1;
export const SAFETY_MAX = 3;
export const AVIONICS_TIERS = Object.freeze(['basic', 'standard', 'redundant']);

/**
 * Tank sizing from propellant mass and diameter.
 * @returns {{volume:number,length:number,diameter:number}} m^3, m
 */
export function sizeTank(propellantId, propMass, diameter) {
  const p = PROPELLANT_BY_ID[propellantId];
  if (!p || !(diameter > 0)) return { volume: 0, length: 0, diameter: diameter || 0 };
  const volume = (propMass / p.bulkDensity) * (1 + TANK_ULLAGE);
  const area = Math.PI * (diameter / 2) ** 2;
  const length = volume / area + (p.storability === 'solid' ? 0 : DOME_LENGTH_FACTOR * diameter);
  return { volume, length, diameter };
}

export function defaultStage(over = {}) {
  return { name: 'Stage', propellant: 'kerolox', engine: 'gg-kerolox', engineCount: 1, propMass: 20000, diameter: 2.0, material: 'al-li', interstageMass: 0, separationMass: 50, landingPropMass: 0, ...over };
}

export function defaultDesign(over = {}) {
  return {
    version: DESIGN_VERSION,
    name: 'Untitled',
    stages: [defaultStage({ name: 'Stage 1' })],
    strapOns: { count: 0, kind: 'solid', propellant: 'solid-apcp', engine: 'srm-small', engineCount: 1, propMass: 0, diameter: 1.6, material: 'steel', crossfeed: false },
    payload: { type: 'leo-sat', mass: 1500, orbit: 'leo400' },
    fairing: { diameter: 2.0, length: 5, material: 'composite', jettisonAltitudeKm: 110 },
    aero: { nose: 'ogive', fins: false },
    landing: { legs: false, gridFins: false, rcs: false },
    avionics: { tier: 'standard' },
    safetyFactor: 1.4,
    livery: { primary: '#f2f2f2', accent: '#1a3c6e', name: '' },
    ...over,
  };
}

const isNum = (v) => typeof v === 'number' && Number.isFinite(v);

function checkStageLike(s, where, errs, { strap = false } = {}) {
  if (!s || typeof s !== 'object') { errs.push(`${where}: missing`); return; }
  if (!PROPELLANT_BY_ID[s.propellant]) errs.push(`${where}.propellant: unknown "${s.propellant}"`);
  const e = ENGINE_BY_ID[s.engine];
  if (!e) errs.push(`${where}.engine: unknown "${s.engine}"`);
  else if (s.propellant && e.propellant !== s.propellant) errs.push(`${where}.engine: ${e.id} burns ${e.propellant}, stage has ${s.propellant}`);
  if (!Number.isInteger(s.engineCount) || s.engineCount < ENGINE_COUNT_MIN || s.engineCount > ENGINE_COUNT_MAX) errs.push(`${where}.engineCount: integer 1–40`);
  if (!isNum(s.propMass) || s.propMass < (strap ? 0 : 1)) errs.push(`${where}.propMass: must be a positive number`);
  if (!isNum(s.diameter) || s.diameter < DIAMETER_MIN || s.diameter > DIAMETER_MAX) errs.push(`${where}.diameter: 0.3–15 m`);
  if (!MATERIAL_BY_ID[s.material]) errs.push(`${where}.material: unknown "${s.material}"`);
}

/**
 * Shape validation (not engineering validation — that lives in designer/validate.js, M2).
 * @returns {{ok:boolean, errors:string[]}}
 */
export function validateDesign(d) {
  const errs = [];
  if (!d || typeof d !== 'object') return { ok: false, errors: ['design: not an object'] };
  if (d.version !== DESIGN_VERSION) errs.push(`version: expected ${DESIGN_VERSION}, got ${d.version} (run migrateDesign)`);
  if (typeof d.name !== 'string') errs.push('name: string required');
  if (!Array.isArray(d.stages) || d.stages.length < 1 || d.stages.length > MAX_STAGES) errs.push(`stages: 1–${MAX_STAGES} required`);
  else d.stages.forEach((s, i) => {
    checkStageLike(s, `stages[${i}]`, errs);
    for (const k of ['interstageMass', 'separationMass', 'landingPropMass']) if (s[k] != null && (!isNum(s[k]) || s[k] < 0)) errs.push(`stages[${i}].${k}: non-negative number`);
  });
  const so = d.strapOns;
  if (!so || !STRAPON_COUNTS.includes(so.count)) errs.push('strapOns.count: 0, 2 or 4');
  else if (so.count > 0) {
    if (!['solid', 'liquid'].includes(so.kind)) errs.push('strapOns.kind: solid or liquid');
    checkStageLike(so, 'strapOns', errs, { strap: true });
    if (so.kind === 'solid' && so.propellant !== 'solid-apcp') errs.push('strapOns: solid kind needs solid propellant');
    if (so.kind === 'solid' && so.crossfeed) errs.push('strapOns.crossfeed: not possible with solids');
    if (typeof so.crossfeed !== 'boolean') errs.push('strapOns.crossfeed: boolean');
  }
  const pl = d.payload;
  if (!pl || !isNum(pl.mass) || pl.mass < 0) errs.push('payload.mass: non-negative number');
  else {
    if (pl.type && !PAYLOAD_BY_ID[pl.type]) errs.push(`payload.type: unknown "${pl.type}"`);
    if (!ORBITS[pl.orbit]) errs.push(`payload.orbit: unknown "${pl.orbit}"`);
  }
  const f = d.fairing;
  if (!f || !isNum(f.diameter) || f.diameter <= 0 || !isNum(f.length) || f.length <= 0) errs.push('fairing: diameter and length > 0');
  else if (!MATERIAL_BY_ID[f.material]) errs.push(`fairing.material: unknown "${f.material}"`);
  if (!d.aero || !(d.aero.nose in NOSE_SHAPES)) errs.push(`aero.nose: one of ${Object.keys(NOSE_SHAPES).join(', ')}`);
  if (!d.landing || ['legs', 'gridFins', 'rcs'].some((k) => typeof d.landing[k] !== 'boolean')) errs.push('landing: legs/gridFins/rcs booleans');
  if (!d.avionics || !AVIONICS_TIERS.includes(d.avionics.tier)) errs.push(`avionics.tier: one of ${AVIONICS_TIERS.join(', ')}`);
  if (!isNum(d.safetyFactor) || d.safetyFactor < SAFETY_MIN || d.safetyFactor > SAFETY_MAX) errs.push('safetyFactor: 1.0–3.0');
  if (!d.livery || typeof d.livery.primary !== 'string') errs.push('livery.primary: string');
  return { ok: errs.length === 0, errors: errs };
}

// Migration hooks: MIGRATIONS[n] upgrades a design from version n to n+1.
// Register new entries when DESIGN_VERSION is bumped; never edit old ones.
export const MIGRATIONS = {
  0: (d) => ({ ...defaultDesign(), ...d, version: 1 }), // pre-versioned drafts: fill defaults
};
export function migrateDesign(input) {
  let d = JSON.parse(JSON.stringify(input || {}));
  let v = Number.isInteger(d.version) ? d.version : 0;
  if (v > DESIGN_VERSION) throw new Error(`design version ${v} is newer than supported ${DESIGN_VERSION}`);
  while (v < DESIGN_VERSION) {
    const step = MIGRATIONS[v];
    if (!step) throw new Error(`no migration from design version ${v}`);
    d = step(d); v = d.version;
  }
  return d;
}
