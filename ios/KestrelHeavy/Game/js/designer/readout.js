// Designer readout: pure math over a design (no DOM). Feeds the DESIGN screen and tests.
// quickReadout(): instant (rocket equation, TWR, cost, reliability, required Δv).
// simReadout():   runs the headless ascent sim (losses, apogee, outcome). Debounced by the UI.
import { ENGINE_BY_ID } from '../data/engines.js';
import { PROPELLANT_BY_ID } from '../data/propellants.js';
import { MATERIAL_BY_ID } from '../data/materials.js';
import { ORBITS } from '../data/payloads.js';
import { vehicleMass, structureMass } from '../vehicle/mass.js';
import { toSimStages } from '../vehicle/stack.js';
import { stackDeltaV, simulate } from '../physics/headless.js';
import { massFlow } from '../physics/rocket-eq.js';
import { MU_EARTH, R_EARTH } from '../physics/gravity.js';
import { siteRotationSpeed } from '../physics/earth.js';

export const G0 = 9.80665;
export const LAUNCH_LATITUDE = 28.5;
/** Budgeted ascent losses (gravity + drag + steering) for the required-Δv estimate, m/s. */
export const LOSS_BUDGET = 1500;
const AVIONICS_COST = { basic: 0.5e6, standard: 2e6, redundant: 5e6 };
const AVIONICS_REL = { basic: 0.985, standard: 0.995, redundant: 0.999 };

/**
 * Convert toSimStages() output into the plain serial list that physics/headless.js
 * (stackDeltaV/simulate) understands. Strap-ons burn in parallel with the core: they become
 * a combined phase (boosters + the core propellant burned meanwhile), then the core continues.
 * Landing reserve rides as dry mass. Payload for the sim = payload + fairing (carried to orbit;
 * jettison is an M3 sim feature, so this is conservative).
 */
export const TUNE_LIMIT = 15; // ± percent: "editable numbers" stay within build tolerance of the catalog engine

function applyTune(entry, tune) {
  if (!tune) return entry;
  const clampPct = (v) => Math.max(-TUNE_LIMIT, Math.min(TUNE_LIMIT, Number(v) || 0)) / 100;
  const kt = 1 + clampPct(tune.thrustPct), ki = 1 + clampPct(tune.ispPct);
  entry.thrustVac *= kt; entry.thrustSL *= kt; entry.ispVac *= ki; entry.ispSL *= ki;
  return entry;
}

export function serialStages(design) {
  const sim = toSimStages(design).map((s) => applyTune({ ...s }, s.role === 'strapons' ? design.strapOns?.tune : design.stages[s.index]?.tune));
  const last = sim[sim.length - 1];
  const payload = (last.payloadMass || 0) + (last.fairingMass || 0);
  const out = [];
  for (let k = 0; k < sim.length; k++) {
    const s = sim[k];
    const base = { diameter: s.diameter, cd: s.Cd ?? 0.3 };
    if (s.parallel && sim[k + 1]) {
      const c = sim[k + 1];
      const mdS = massFlow(s.thrustVac, s.ispVac), mdC = massFlow(c.thrustVac, c.ispVac);
      const tB = s.propMass / mdS;
      const coreBurn = Math.min(mdC * tB, c.propMass * 0.95);
      const thrustVac = s.thrustVac + c.thrustVac, thrustSL = s.thrustSL + c.thrustSL;
      // Effective Isp over the parallel phase: total impulse / total propellant.
      const ispVac = (s.thrustVac * tB + c.thrustVac * (coreBurn / mdC)) / ((s.propMass + coreBurn) * G0);
      const mdot = massFlow(thrustVac, ispVac);
      const ispSL = thrustSL / (mdot * G0);
      out.push({ ...base, thrustVac, thrustSL, ispVac, ispSL, propMass: s.propMass + coreBurn, dryMass: s.dryMass, role: 'strapons+core', label: `Core + ${s.count} strap-ons` });
      out.push({ ...base, diameter: c.diameter, thrustVac: c.thrustVac, thrustSL: c.thrustSL, ispVac: c.ispVac, ispSL: c.ispSL, propMass: c.propMass - coreBurn, dryMass: c.dryMass + (c.reserveMass || 0), role: 'core', index: c.index, label: design.stages[c.index].name || `Stage ${c.index + 1}` });
      k++;
      continue;
    }
    out.push({ ...base, thrustVac: s.thrustVac, thrustSL: s.thrustSL, ispVac: s.ispVac, ispSL: s.ispSL, propMass: s.propMass, dryMass: s.dryMass + (s.reserveMass || 0), role: 'core', index: s.index, label: design.stages[s.index].name || `Stage ${s.index + 1}` });
  }
  return { stages: out, payload };
}

/** Target orbit info: altitude to aim the sim at and required Δv (m/s). */
export function requiredDeltaV(orbitId, latitude = LAUNCH_LATITUDE) {
  const o = ORBITS[orbitId] || ORBITS.leo400;
  if (o.periKm < 0) { // suborbital hop: vertical-ish climb to apogee + losses
    const h = o.apoKm * 1000;
    return { orbit: o, targetAlt: h, required: Math.sqrt(2 * G0 * h) + 1000 };
  }
  const rp = R_EARTH + o.periKm * 1000, ra = R_EARTH + o.apoKm * 1000;
  const vCirc = Math.sqrt(MU_EARTH / rp);
  const vPeri = Math.sqrt(MU_EARTH * (2 / rp - 2 / (rp + ra)));
  const required = vPeri - siteRotationSpeed(latitude) + LOSS_BUDGET;
  return { orbit: o, targetAlt: Math.min(o.periKm, 600) * 1000, required, vCirc };
}

function costOf(design, mass) {
  let cost = 0;
  const parts = design.stages.map((s) => ({ ...s, n: 1 }));
  if (design.strapOns?.count) parts.push({ ...design.strapOns, n: design.strapOns.count });
  for (const p of parts) {
    const e = ENGINE_BY_ID[p.engine], pr = PROPELLANT_BY_ID[p.propellant], mat = MATERIAL_BY_ID[p.material];
    if (e) cost += e.cost * p.engineCount * p.n;
    if (pr) cost += pr.costPerKg * p.propMass * p.n;
    if (mat) cost += mat.costPerKg * structureMass(p, design.safetyFactor) * p.n;
  }
  const fm = MATERIAL_BY_ID[design.fairing?.material];
  if (fm) cost += fm.costPerKg * mass.fairing;
  cost += AVIONICS_COST[design.avionics?.tier] ?? 2e6;
  return cost;
}

function reliabilityOf(design) {
  let r = AVIONICS_REL[design.avionics?.tier] ?? 0.995;
  const parts = design.stages.map((s) => ({ ...s, n: 1 }));
  if (design.strapOns?.count) parts.push({ ...design.strapOns, n: design.strapOns.count });
  for (const p of parts) { const e = ENGINE_BY_ID[p.engine]; if (e) r *= e.reliability ** (p.engineCount * p.n); }
  r *= 0.998 ** (design.stages.length - 1 + (design.strapOns?.count ? 1 : 0)); // each separation event
  if (design.safetyFactor < 1.25) r *= 0.97; else if (design.safetyFactor >= 1.5) r *= 1.002;
  return Math.min(0.9999, r);
}

/** Instant readout. Never throws on a shape-valid design; returns null fields if it can't compute. */
export function quickReadout(design) {
  const mass = vehicleMass(design);
  const { stages, payload } = serialStages(design);
  const dv = stackDeltaV(stages, payload);
  const per = stages.map((s, i) => {
    let above = payload;
    for (let j = i + 1; j < stages.length; j++) above += stages[j].propMass + stages[j].dryMass;
    const m0 = above + s.propMass + s.dryMass, mf = m0 - s.propMass;
    const mdot = massFlow(s.thrustVac, s.ispVac);
    const thrust = i === 0 ? s.thrustSL : s.thrustVac;
    return {
      label: s.label, role: s.role, m0, mf, massRatio: m0 / mf, mdot,
      burnTime: s.propMass / mdot, thrust, twr: thrust / (m0 * G0), twrVac: s.thrustVac / (m0 * G0),
      deltaV: dv.perStage[i], ispVac: s.ispVac, ispSL: s.ispSL,
    };
  });
  const req = requiredDeltaV(design.payload?.orbit);
  const margin = (dv.total - req.required) / req.required;
  return {
    mass, stages: per, serial: stages, payload, totalDeltaV: dv.total, liftoffMass: per[0]?.m0 ?? mass.liftoffMass,
    liftoffTWR: per[0]?.twr ?? 0, required: req.required, targetAlt: req.targetAlt, orbit: req.orbit, margin,
    cost: costOf(design, mass), reliability: reliabilityOf(design),
  };
}

/** Headless-sim readout (heavier: ~tens of ms). */
export function simReadout(design, quick = quickReadout(design), { dt = 0.2, sampleEvery = 2 } = {}) {
  const r = simulate(quick.serial, { payload: quick.payload, targetAlt: quick.targetAlt, latitude: LAUNCH_LATITUDE, dt, sampleEvery, tMax: 2500 });
  const losses = r.losses.gravity + r.losses.drag + r.losses.steering;
  return {
    outcome: r.outcome, time: r.time, losses: r.losses, totalLosses: losses,
    apogeeKm: (r.orbit.apoAlt ?? 0) / 1000, perigeeKm: (r.orbit.periAlt ?? 0) / 1000,
    maxQ: r.maxQ, maxG: r.maxG, deltaVExpended: r.deltaVExpended, idealDeltaV: r.idealDeltaV,
    samples: r.samples, events: r.events,
  };
}

/** Thrust-vs-time series for the mini plot from sim samples (N). */
export function thrustCurve(quick, sim, pressureAt) {
  if (!sim?.samples?.length) return [];
  return sim.samples.map((s) => {
    const st = quick.serial[s.stage - 1];
    if (!st) return { t: s.t, F: 0 };
    const p = pressureAt ? pressureAt(Math.max(0, s.alt)) : 0;
    const mdot = massFlow(st.thrustVac, st.ispVac);
    const isp = Math.max(0, st.ispVac - (st.ispVac - st.ispSL) * p / 101325);
    return { t: s.t, F: mdot * G0 * isp, stage: s.stage };
  });
}
