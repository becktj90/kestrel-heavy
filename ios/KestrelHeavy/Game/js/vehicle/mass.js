// Mass model. All kg. Simple parametric estimates (flagged as M1-level, refined in M4).
import { PROPELLANT_BY_ID } from '../data/propellants.js';
import { ENGINE_BY_ID } from '../data/engines.js';
import { MATERIAL_BY_ID } from '../data/materials.js';

const REF_SAFETY = 1.4;            // catalog structural fractions assume SF 1.4
const SOLID_CASE_FRACTION = 0.09;  // solid motor case+nozzle per kg propellant (material-scaled)
const AVIONICS_MASS = { basic: 60, standard: 150, redundant: 300 };
const LEG_FRACTION = 0.06;         // of stage dry mass, first stage only
const GRIDFIN_MASS_PER_M2 = 120;   // per m^2 of stage cross-section (4 fins)
const RCS_MASS = 250;
const FAIRING_AREAL = { composite: 14, 'al-li': 20, steel: 45 }; // kg/m^2 of shell

/** Structural (tank/case) mass for a stage-like part. */
export function structureMass(part, safetyFactor = REF_SAFETY) {
  const prop = PROPELLANT_BY_ID[part.propellant];
  const mat = MATERIAL_BY_ID[part.material];
  if (!prop || !mat) return 0;
  const sfScale = safetyFactor / REF_SAFETY;
  if (prop.storability === 'solid') return part.propMass * SOLID_CASE_FRACTION * (mat.structuralFraction / 0.045) * sfScale;
  // Low-density propellants need more tank per kg: scale by volume relative to kerolox.
  const volScale = Math.sqrt(1030 / prop.bulkDensity);
  return part.propMass * mat.structuralFraction * volScale * sfScale;
}

export function enginesMass(part) {
  const e = ENGINE_BY_ID[part.engine];
  return e ? e.mass * part.engineCount : 0;
}

export function fairingMass(f) {
  const r = f.diameter / 2;
  const area = 2 * Math.PI * r * f.length * 0.85; // cylinder + ogive approximation
  return area * (FAIRING_AREAL[f.material] ?? 20);
}

/** Per-stage breakdown. stage index 0 is the first (bottom) stage. */
export function stageMasses(design) {
  const sf = design.safetyFactor;
  return design.stages.map((s, i) => {
    let dry = structureMass(s, sf) + enginesMass(s) + (s.interstageMass || 0) + (s.separationMass || 0);
    if (i === design.stages.length - 1) dry += AVIONICS_MASS[design.avionics?.tier] ?? 150;
    if (i === 0) {
      if (design.landing?.legs) dry *= 1 + LEG_FRACTION;
      if (design.landing?.gridFins) dry += GRIDFIN_MASS_PER_M2 * Math.PI * (s.diameter / 2) ** 2;
      if (design.landing?.rcs) dry += RCS_MASS;
    }
    const prop = s.propMass;
    const landingProp = Math.min(s.landingPropMass || 0, prop);
    return { name: s.name, dry, prop, wet: dry + prop, usableProp: prop - landingProp, landingProp };
  });
}

/** Strap-on set (all boosters combined). */
export function strapOnMasses(design) {
  const so = design.strapOns;
  if (!so || !so.count) return { count: 0, dry: 0, prop: 0, wet: 0, each: { dry: 0, prop: 0 } };
  const dryEach = structureMass(so, design.safetyFactor) + enginesMass(so) + 80; // 80 kg attach/sep hardware
  return { count: so.count, dry: dryEach * so.count, prop: so.propMass * so.count, wet: (dryEach + so.propMass) * so.count, each: { dry: dryEach, prop: so.propMass } };
}

/** Totals plus mass ratios (m0/mf) per stage, with everything above riding along. */
export function vehicleMass(design) {
  const stages = stageMasses(design);
  const straps = strapOnMasses(design);
  const payload = design.payload?.mass || 0;
  const fairing = fairingMass(design.fairing);
  let above = payload + fairing; // fairing treated as carried by the top-most stage for ratio purposes
  const ratios = new Array(stages.length);
  for (let i = stages.length - 1; i >= 0; i--) {
    const m0 = above + stages[i].wet;
    const mf = m0 - stages[i].usableProp;
    ratios[i] = { m0, mf, massRatio: m0 / mf, propFraction: stages[i].usableProp / m0 };
    above = m0;
  }
  const gliftoff = above + straps.wet;
  return { stages, strapOns: straps, payload, fairing, liftoffMass: gliftoff, dryMass: stages.reduce((a, s) => a + s.dry, 0) + straps.dry, ratios };
}
