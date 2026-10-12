// Kestrel Heavy economy — cost model (E1). Pure, DOM-free, deterministic.
// Every money value is an INTEGER in minor units (1 Kc = 100,000 minor; see economy-data).
// Each line item is rounded once (Math.round) and then only integers are summed, so totals
// are exact. Formulas are documented in ECONOMY.md and are monotone non-decreasing in
// thrust, chamber pressure, engine count, propellant mass and structure.
import { ECON } from '../economy-data/constants.js';
import { ENGINE_BY_ID } from '../data/engines.js';
import { PROPELLANT_BY_ID } from '../data/propellants.js';
import { structureMass, vehicleMass } from '../vehicle/mass.js';

export const MINOR_PER_KC = ECON.currency.minorPerCredit * ECON.currency.creditsPerKc;
const int = (x) => Math.max(0, Math.round(x));
export const CATEGORIES = Object.freeze(['engines', 'structure', 'propellant', 'avionics', 'recovery', 'ops']);

/** Display minor units as fictional kilocredits: "850 Kc", "34.1k Kc", "1.23M Kc". */
export function formatKc(minor) {
  const kc = minor / MINOR_PER_KC;
  const a = Math.abs(kc);
  const f = (v, d) => v.toLocaleString('en-US', { maximumFractionDigits: d, minimumFractionDigits: d });
  if (a >= 1e6) return `${f(kc / 1e6, 2)}M Kc`;
  if (a >= 1e3) return `${f(kc / 1e3, 1)}k Kc`;
  return `${f(kc, 0)} Kc`;
}

export function engineTrl(id) { return ECON.engine.trl[id] ?? ECON.engine.defaultTrl; }

/** Wright learning curve: cost multiplier of the n-th unit (1-based). */
export function learningFactor(n, rate = ECON.engine.learningRate) { return Math.pow(Math.max(1, n), Math.log2(rate)); }

/** One-time development cost of an engine type (minor units). */
export function engineDevCost(engine) {
  const E = ECON.engine;
  const F = Math.max(engine.thrustVac, engine.thrustSL) / E.refThrustN;
  const cyc = E.cycleMultiplier[engine.cycle] ?? 1;
  const trl = engineTrl(engine.id);
  return int(E.kDev * Math.pow(F, E.devThrustExp) * cyc * Math.pow(engine.pc / E.refPcBar, E.devPcExp) * (1 + E.trlPenaltyPerLevel * (9 - trl)));
}

/** First-unit (theoretical unit 1) recurring cost of an engine (minor units, unrounded). */
function engineT1(engine) {
  const E = ECON.engine;
  const F = Math.max(engine.thrustVac, engine.thrustSL) / E.refThrustN;
  return E.kUnit * Math.pow(F, E.unitThrustExp) * (E.cycleMultiplier[engine.cycle] ?? 1) * Math.pow(engine.pc / E.refPcBar, E.unitPcExp);
}

/** Recurring cost of `count` more units after `built` already built (learning curve applied). */
export function engineBatchCost(engine, count, built = 0) {
  const t1 = engineT1(engine);
  let sum = 0;
  for (let i = 1; i <= count; i++) sum += int(t1 * learningFactor(built + i));
  return sum;
}

/** Cylindrical tank surface (m²) for a propellant load at a diameter (domes as flat ends). */
export function tankArea(part) {
  const prop = PROPELLANT_BY_ID[part.propellant];
  if (!prop || !part.propMass || !part.diameter) return 0;
  const d = part.diameter, vol = part.propMass / prop.bulkDensity;
  const len = vol / (Math.PI * d * d / 4);
  return Math.PI * d * len + Math.PI * d * d / 2;
}

function structureCost(part, sf) {
  const S = ECON.structure, prop = PROPELLANT_BY_ID[part.propellant];
  const cryo = prop?.storability === 'deep-cryogenic' ? 1 + S.deepCryoFactor : 1;
  const sfk = Math.pow(Math.max(1, sf) / S.refSafetyFactor, S.safetyFactorExp);
  const m = structureMass(part, sf) + (part.interstageMass || 0) + (part.separationMass || 0);
  return int((m * (S.materialPerKg[part.material] ?? 0) + tankArea(part) * (S.materialPerM2[part.material] ?? 0)) * cryo * sfk);
}

function propellantCost(propId, massKg) {
  const P = ECON.propellant, prop = PROPELLANT_BY_ID[propId];
  if (!prop) return 0;
  return int(massKg * (P.perKg[propId] ?? 0) * (1 + (prop.boiloffPerHour || 0) * P.standbyHours));
}

/** Flattened hardware groups: each core stage once, strap-ons as one group × count. */
function groups(design) {
  const g = design.stages.map((s, i) => ({ ...s, key: `stage${i}`, label: s.name || `Stage ${i + 1}`, n: 1 }));
  if (design.strapOns?.count) g.push({ ...design.strapOns, key: 'strapons', label: `${design.strapOns.count} strap-ons`, n: design.strapOns.count });
  return g;
}

/**
 * Price a design. companyState.unitsBuilt = { [engineId]: n } shifts the learning curve
 * (campaign), companyState.devDone = Set/array of engine ids already developed.
 * Returns { dev, recurring, total, perKg, lines[], byCategory{}, devLines[] } (all minor units).
 */
export function priceDesign(design, companyState = {}) {
  const built = { ...(companyState.unitsBuilt || {}) };
  const devDone = new Set(companyState.devDone || []);
  const sf = design.safetyFactor ?? 1.4;
  const lines = [];
  const devLines = [];
  const add = (cat, label, amount, group) => { if (amount > 0) lines.push({ cat, label, amount, group }); };
  const gs = groups(design);
  const nStages = design.stages.length;

  for (const p of gs) {
    const e = ENGINE_BY_ID[p.engine];
    if (e) {
      const count = (p.engineCount || 0) * p.n;
      add('engines', `${p.label}: ${count}× ${e.label}`, engineBatchCost(e, count, built[e.id] || 0), p.key);
      built[e.id] = (built[e.id] || 0) + count;
      if (!devDone.has(e.id)) { devDone.add(e.id); devLines.push({ cat: 'engines', label: `Develop ${e.label}`, amount: engineDevCost(e) }); }
    }
    add('structure', `${p.label}: tanks & structure`, structureCost(p, sf) * p.n, p.key);
    add('propellant', `${p.label}: ${PROPELLANT_BY_ID[p.propellant]?.label || p.propellant}`, propellantCost(p.propellant, (p.propMass + (p.landingPropMass || 0))) * p.n, p.key);
  }
  // Fairing
  const mass = vehicleMass(design);
  if (design.fairing && mass.fairing) {
    const S = ECON.structure, f = design.fairing;
    const area = Math.PI * f.diameter * f.length * 0.75;
    add('structure', 'Fairing', int(mass.fairing * (S.materialPerKg[f.material] ?? 0) + area * (S.materialPerM2[f.material] ?? 0)), 'fairing');
  }
  const seps = nStages - 1 + (design.strapOns?.count ? 1 : 0);
  add('structure', `Separation systems (${seps})`, seps * ECON.structure.separationEvent, 'sep');
  // Avionics (dev + unit, redundancy scales with stage count)
  const tier = design.avionics?.tier in ECON.avionics ? design.avionics.tier : 'standard';
  const av = ECON.avionics[tier];
  add('avionics', `Avionics (${tier})`, int(av.unit * (1 + ECON.avionics.perExtraStage * (nStages - 1))), 'avionics');
  if (av.dev && !devDone.has(`avionics:${tier}`)) devLines.push({ cat: 'avionics', label: `Qualify ${tier} avionics`, amount: av.dev });
  // Recovery hardware (on the first stage)
  const L = design.landing || {}, R = ECON.recovery;
  add('recovery', 'Recovery hardware', (L.legs ? R.legs : 0) + (L.gridFins ? R.gridFins : 0) + (L.rcs ? R.rcs : 0), 'recovery');
  // Operations
  const O = ECON.ops;
  const maxD = Math.max(...gs.map((p) => p.diameter || 0), design.fairing?.diameter || 0);
  add('ops', 'Range fee', O.rangeFee, 'ops');
  add('ops', 'Crew & standby', O.crewDay * (O.baseDays + O.daysPerStage * nStages), 'ops');
  add('ops', 'Integration', O.integrationPerStage * (nStages + (design.strapOns?.count || 0)), 'ops');
  add('ops', maxD > O.oversizeDiameterM ? 'Transport (oversize)' : 'Transport', int(O.transport * (maxD > O.oversizeDiameterM ? O.oversizeFactor : 1)), 'ops');

  const byCategory = Object.fromEntries(CATEGORIES.map((c) => [c, 0]));
  for (const l of lines) byCategory[l.cat] += l.amount;
  const recurring = lines.reduce((a, l) => a + l.amount, 0);
  const dev = devLines.reduce((a, l) => a + l.amount, 0);
  const payloadKg = design.payload?.mass || 0;
  return { recurring, dev, total: recurring + dev, perKg: payloadKg > 0 ? Math.round(recurring / payloadKg) : null, lines, devLines, byCategory };
}

/**
 * Marginal-cost hints: what common edits would change in per-launch cost (minor units).
 * Cheap: re-prices a handful of variants.
 */
export function marginalHints(design, companyState) {
  const base = priceDesign(design, companyState).recurring;
  const out = [];
  const tryEdit = (label, mut) => {
    const d = structuredClone(design); mut(d);
    out.push({ label, delta: priceDesign(d, companyState).recurring - base });
  };
  design.stages.forEach((s, i) => {
    const name = s.name || `Stage ${i + 1}`;
    tryEdit(`+1 engine on ${name}`, (d) => { d.stages[i].engineCount += 1; });
    tryEdit(`+10% propellant on ${name}`, (d) => { d.stages[i].propMass = Math.round(d.stages[i].propMass * 1.1); });
  });
  if (design.strapOns?.count) tryEdit('−1 strap-on pair', (d) => { d.strapOns.count = Math.max(0, d.strapOns.count - 2); });
  return out.sort((a, b) => Math.abs(b.delta) - Math.abs(a.delta));
}
