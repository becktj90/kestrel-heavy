// Staging order, jettison events and the headless-sim stage list.
import { ENGINE_BY_ID } from '../data/engines.js';
import { NOSE_SHAPES } from './design.js';
import { stageMasses, strapOnMasses, fairingMass } from './mass.js';

/** Zero-lift subsonic Cd estimate at body reference area. Mach dependence lives in physics/aero.js (M1 physics). */
export function baseCd(design) {
  let cd = NOSE_SHAPES[design.aero?.nose] ?? 0.32;
  if (design.aero?.fins) cd += 0.04;
  if (design.landing?.gridFins) cd += 0.03;
  if (design.strapOns?.count) cd += 0.03 * design.strapOns.count;
  // Fairing wider than the stage below (hammerhead) adds drag.
  const top = design.stages[design.stages.length - 1];
  if (design.fairing && top && design.fairing.diameter > top.diameter) cd += 0.08 * (design.fairing.diameter / top.diameter - 1);
  return cd;
}

/** Reference diameter (m) giving the same frontal area as the whole stack cross-section. */
export function referenceDiameter(design) {
  const core = Math.max(...design.stages.map((s) => s.diameter), design.fairing?.diameter || 0);
  const so = design.strapOns;
  const area = Math.PI * (core / 2) ** 2 + (so?.count ? so.count * Math.PI * (so.diameter / 2) ** 2 : 0);
  return 2 * Math.sqrt(area / Math.PI);
}

/**
 * Ordered flight event list. Events refer to stage indices (0 = first stage).
 * Fairing jettison is altitude-triggered (player-adjustable in flight).
 */
export function stagingSequence(design) {
  const ev = [{ type: 'ignition', stage: 0, withStrapOns: design.strapOns?.count > 0 }];
  if (design.strapOns?.count) ev.push({ type: 'strapon-sep', count: design.strapOns.count, trigger: 'strapon-burnout', crossfeed: !!design.strapOns.crossfeed });
  design.stages.forEach((s, i) => {
    ev.push({ type: 'cutoff', stage: i, trigger: s.landingPropMass ? 'reserve-reached' : 'burnout' });
    if (i < design.stages.length - 1) {
      ev.push({ type: 'stage-sep', stage: i, jettisonMass: 0 /* stage dry mass, see toSimStages */ });
      ev.push({ type: 'ignition', stage: i + 1 });
    }
  });
  ev.push({ type: 'payload-sep' });
  // Fairing: inserted logically, the sim triggers it by altitude.
  ev.splice(1, 0, { type: 'fairing-jettison', trigger: 'altitude', altitudeKm: design.fairing?.jettisonAltitudeKm ?? 110, mass: fairingMass(design.fairing) });
  return ev;
}

function simEntry(part, mass, extra) {
  const e = ENGINE_BY_ID[part.engine];
  const n = part.engineCount;
  return { thrustSL: e.thrustSL * n, thrustVac: e.thrustVac * n, ispSL: e.ispSL, ispVac: e.ispVac, propMass: mass.prop, dryMass: mass.dry, ...extra };
}

/**
 * Headless-sim stage list: [{thrustSL, thrustVac, ispSL, ispVac, propMass, dryMass, diameter, Cd, ...}]
 * in burn order. Masses are per entry only (the sim stacks them).
 * - Strap-ons (if any) come first with `parallel: true`: they burn together with the next entry
 *   (the core first stage) and are dropped at their burnout. With `crossfeed: true` the sim should
 *   drain booster propellant first into the core engines.
 * - propMass is usable propellant; landing reserve stays in dryMass's companion `reserveMass`
 *   (carried, not burned on ascent).
 * - The last entry carries `payloadMass` and `fairingMass` (+ `fairingJettisonAltitudeM`).
 */
export function toSimStages(design) {
  const masses = stageMasses(design);
  const Cd = baseCd(design);
  const diameter = referenceDiameter(design);
  const out = [];
  const so = design.strapOns;
  if (so?.count) {
    const m = strapOnMasses(design);
    const e = ENGINE_BY_ID[so.engine];
    out.push({ thrustSL: e.thrustSL * so.engineCount * so.count, thrustVac: e.thrustVac * so.engineCount * so.count, ispSL: e.ispSL, ispVac: e.ispVac, propMass: m.prop, dryMass: m.dry, diameter, Cd, role: 'strapons', parallel: true, crossfeed: !!so.crossfeed, count: so.count });
  }
  design.stages.forEach((s, i) => {
    const m = masses[i];
    out.push(simEntry(s, { prop: m.usableProp, dry: m.dry }, {
      diameter: i === 0 ? diameter : Math.max(s.diameter, design.fairing.diameter), Cd, role: 'core', index: i, reserveMass: m.landingProp,
      throttle: ENGINE_BY_ID[s.engine].throttle.slice(), restart: ENGINE_BY_ID[s.engine].restart,
    }));
  });
  const last = out[out.length - 1];
  last.payloadMass = design.payload.mass;
  last.fairingMass = fairingMass(design.fairing);
  last.fairingJettisonAltitudeM = (design.fairing.jettisonAltitudeKm ?? 110) * 1000;
  return out;
}

const G0 = 9.80665;
/** Ideal (vacuum) Δv per sim entry, strap-ons counted as a parallel phase (approximation for readouts/tests). */
export function idealDeltaV(simStages) {
  const totalAbove = (k) => simStages.slice(k).reduce((a, s) => a + s.propMass + s.dryMass + (s.reserveMass || 0), 0) + (simStages.at(-1).payloadMass || 0) + (simStages.at(-1).fairingMass || 0);
  let dv = 0; const per = [];
  for (let k = 0; k < simStages.length; k++) {
    const s = simStages[k];
    const m0 = totalAbove(k);
    const mf = m0 - s.propMass;
    let isp = s.ispVac;
    if (s.parallel && simStages[k + 1]) {
      // Combined Isp of boosters + core while both burn (thrust-weighted); core propellant burned meanwhile is ignored (conservative).
      const c = simStages[k + 1];
      isp = (s.thrustVac + c.thrustVac) / (s.thrustVac / s.ispVac + c.thrustVac / c.ispVac);
    }
    const d = isp * G0 * Math.log(m0 / mf);
    per.push(d); dv += d;
  }
  return { total: dv, perStage: per };
}
