// Kestrel Heavy economy — launch reliability with engine-out tolerance (E1). Pure.
// Each engine fails with p = 1 − r (catalog per-flight reliability). A fraction of failures
// is catastrophic (uncontained) and loses the vehicle regardless; the rest are benign
// shutdowns the stage can absorb up to its engine-out tolerance k. See ECONOMY.md.
import { ECON } from '../economy-data/constants.js';
import { ENGINE_BY_ID } from '../data/engines.js';

function binom(n, k) { let c = 1; for (let i = 1; i <= k; i++) c = c * (n - i + 1) / i; return c; }

/** Benign engine failures a cluster of n liquid engines can survive. Solids/small clusters: 0. */
export function engineOutTolerance(n, engine) {
  const R = ECON.reliability;
  if (!engine || engine.cycle === 'solid' || n < R.engineOutMinCount) return 0;
  return Math.max(1, Math.floor(n / R.engineOutPerEngines));
}

/** P(cluster of n engines with per-engine reliability r completes its burn). */
export function clusterReliability(n, r, k) {
  if (n <= 0) return 1;
  const p = 1 - r, qb = p * (1 - ECON.reliability.catastrophicFraction);
  let s = 0;
  for (let j = 0; j <= Math.min(k, n); j++) s += binom(n, j) * Math.pow(qb, j) * Math.pow(r, n - j);
  return s;
}

export function reliabilityOf(design) {
  const R = ECON.reliability;
  const tier = design.avionics?.tier in ECON.avionics ? design.avionics.tier : 'standard';
  let rel = ECON.avionics[tier].reliability;
  const groups = design.stages.map((s) => ({ ...s, n: 1 }));
  if (design.strapOns?.count) groups.push({ ...design.strapOns, n: design.strapOns.count });
  const stages = [];
  for (const g of groups) {
    const e = ENGINE_BY_ID[g.engine];
    if (!e) continue;
    const perBooster = clusterReliability(g.engineCount, e.reliability, engineOutTolerance(g.engineCount, e));
    const r = Math.pow(perBooster, g.n);
    stages.push({ engine: e.id, count: g.engineCount * g.n, tolerance: engineOutTolerance(g.engineCount, e), reliability: r });
    rel *= r;
  }
  const seps = design.stages.length - 1 + (design.strapOns?.count ? 1 : 0);
  rel *= Math.pow(R.separationEvent, seps);
  const sf = design.safetyFactor ?? 1.4;
  if (sf < R.lowSafetyFactor) rel *= R.lowSafetyPenalty; else if (sf >= R.highSafetyFactor) rel *= R.highSafetyBonus;
  return { reliability: Math.min(R.cap, rel), stages };
}
