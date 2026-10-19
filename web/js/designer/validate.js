// Engineering validation (hard blocks + warnings) and the KID "Make it fly" auto-fix.
// Wording rule: suggest, don't scold. Every message says what to try next.
import { ENGINES, ENGINE_BY_ID } from '../data/engines.js';
import { PAYLOAD_BY_ID } from '../data/payloads.js';
import { validateDesign } from '../vehicle/design.js';
import { quickReadout } from './readout.js';
import { MIN_TWR, GOOD_TWR, MIN_MARGIN, PACK } from './limits.js';
export { MIN_TWR, GOOD_TWR, MIN_MARGIN, PACK } from './limits.js';

/** Payload volume scaled from its archetype by mass^(2/3) (a lighter build of the same kind is smaller). */
export function payloadVolume(d) {
  const pl = PAYLOAD_BY_ID[d.payload?.type];
  if (!pl) return 0;
  const m = d.payload.mass > 0 ? d.payload.mass : pl.mass;
  return pl.volume * Math.min(1.5, (m / pl.mass) ** (2 / 3));
}

// Fairing fit (§2.9), the same rule as Kestrel.Core.Vehicle.PayloadFit (B-04):
//   usable = π(0.45·D)²·0.8·L, and cargo diameter ≤ 0.9·D (the same 0.45·D usable radius).
// 0.9 is an approximate game number; real fairings offer about 0.85–0.885 of their outer diameter.
// 0.8 is optimistic: L is the whole fairing length, nose included, so it overstates the real volume
// for conic and ogive noses (geometric fractions are about 0.73 conic, 0.78 otherwise, 0.90 blunt). It
// stays 0.8 until the follow-up issue #302 "Payload volume packing retune: nose-inclusive L" (https://github.com/becktj90/kestrel-heavy/issues/302).
export const FIT_RADIUS = 0.45;
export const FIT_LENGTH = 0.8;
export const FIT_DIAMETER = 2 * FIT_RADIUS;
export function usableVolume(D, L) { const r = FIT_RADIUS * D; return Math.PI * r * r * FIT_LENGTH * L; }
export function requiredLength(volume, D) { const r = FIT_RADIUS * D; return volume / (Math.PI * r * r * FIT_LENGTH); }
export function requiredDiameter(cargoDiameter) { return cargoDiameter / FIT_DIAMETER; }
/** Fit of a design against its catalog payload; null without a known payload or fairing. */
export function payloadFit(d) {
  const pl = PAYLOAD_BY_ID[d.payload?.type];
  if (!pl || !d.fairing) return null;
  const volume = payloadVolume(d), diameter = pl.diameter || 0;
  const usable = usableVolume(d.fairing.diameter, d.fairing.length);
  const volumeMargin = usable - volume, diameterMargin = FIT_DIAMETER * d.fairing.diameter - diameter;
  return { fits: volumeMargin >= 0 && diameterMargin >= 0, usable, volume, diameter, volumeMargin, diameterMargin };
}

const fmt = (n, d = 2) => Number(n).toFixed(d);
const clone = (d) => JSON.parse(JSON.stringify(d));

function geometryIssues(d) {
  const out = [];
  const parts = d.stages.map((s, i) => ({ s, where: d.stages[i].name || `Stage ${i + 1}`, tab: 'stages', index: i }));
  if (d.strapOns?.count) parts.push({ s: d.strapOns, where: 'Strap-ons', tab: 'strapons' });
  for (const { s, where, tab, index } of parts) {
    const e = ENGINE_BY_ID[s.engine];
    if (e && s.engineCount * e.diameter ** 2 > PACK * s.diameter ** 2) {
      out.push({ code: 'engines-dont-fit', tab, index, msg: `${where}: ${s.engineCount} engines don't fit under a ${fmt(s.diameter, 1)} m body. Try fewer engines or a wider stage.` });
    }
  }
  for (let i = 1; i < d.stages.length; i++) {
    if (d.stages[i].diameter > d.stages[i - 1].diameter * 1.6) out.push({ code: 'top-heavy', tab: 'stages', index: i, msg: `${d.stages[i].name || `Stage ${i + 1}`} is much wider than the stage below it. Try a diameter under ${fmt(d.stages[i - 1].diameter * 1.6, 1)} m.` });
  }
  const top = d.stages[d.stages.length - 1];
  if (d.fairing && top && d.fairing.diameter < top.diameter * 0.5) out.push({ code: 'fairing-narrow', tab: 'payload', msg: 'The fairing is narrower than half the top stage. Try a fairing at least as wide as the stage.' });
  const pl = PAYLOAD_BY_ID[d.payload?.type];
  const fit = payloadFit(d);
  if (pl && fit) {
    if (fit.volumeMargin < 0) out.push({ code: 'payload-doesnt-fit', tab: 'payload', msg: `The ${pl.label} needs about ${fmt(fit.volume, 1)} m³; this fairing holds ${fmt(fit.usable, 1)} m³. Try a longer or wider fairing.` });
    else if (fit.diameterMargin < 0) out.push({ code: 'payload-doesnt-fit', tab: 'payload', msg: `The ${pl.label} is ${fmt(fit.diameter, 1)} m wide; it needs a fairing at least ${fmt(Math.ceil(requiredDiameter(fit.diameter) * 100) / 100, 2)} m wide. Try a wider fairing.` });
  }
  return out;
}

/**
 * @returns {{ok:boolean, blocks:Array<{code,msg,tab}>, warnings:Array<{code,msg,tab}>, readout:object|null}}
 * ok = no hard blocks (warnings allowed).
 */
export function validateEngineering(design) {
  const blocks = [], warnings = [];
  const shape = validateDesign(design);
  const zeroEng = [...design.stages, ...(design.strapOns?.count ? [design.strapOns] : [])].some((s) => !(s.engineCount >= 1));
  const zeroProp = design.stages.some((s) => !(s.propMass > 0)) || (design.strapOns?.count > 0 && !(design.strapOns.propMass > 0));
  if (zeroEng) blocks.push({ code: 'zero-engines', tab: 'engines', msg: 'Every stage needs at least one engine. Try adding one.' });
  if (zeroProp) blocks.push({ code: 'zero-propellant', tab: 'propellants', msg: 'A stage has no propellant. Try loading some fuel.' });
  if (!shape.ok && !zeroEng && !zeroProp) for (const e of shape.errors) blocks.push({ code: 'schema', tab: 'stages', msg: e });
  if (blocks.length) return { ok: false, blocks, warnings, readout: null };
  blocks.push(...geometryIssues(design));

  let r = null;
  try { r = quickReadout(design); } catch (e) { blocks.push({ code: 'math', tab: 'stages', msg: `Couldn't compute this design (${e.message}).` }); }
  if (r) {
    if (!(r.liftoffTWR > MIN_TWR)) blocks.push({ code: 'twr-low', tab: 'engines', msg: `Liftoff thrust-to-weight is ${fmt(r.liftoffTWR)}; it must be above 1 to leave the pad. Try more first-stage engines or less propellant.` });
    else if (r.liftoffTWR < GOOD_TWR) warnings.push({ code: 'twr-marginal', tab: 'engines', msg: `Liftoff TWR ${fmt(r.liftoffTWR)} is slow off the pad (lots of gravity loss). Aim for ${GOOD_TWR} or more.` });
    for (let i = 1; i < r.stages.length; i++) {
      const s = r.stages[i];
      if (s.twrVac < 0.5) warnings.push({ code: 'weak-upper', tab: 'engines', index: i, msg: `${s.label} pushes at only ${fmt(s.twrVac)} g. A stronger engine or lighter load would help it reach orbit.` });
    }
    if (r.stages.length > 1 && r.stages[r.stages.length - 1].deltaV < 0.2 * r.totalDeltaV) warnings.push({ code: 'weak-upper-dv', tab: 'propellants', msg: 'The top stage does little of the work. More upper-stage propellant usually pays off.' });
    if (r.margin < MIN_MARGIN) warnings.push({ code: 'dv-margin', tab: 'propellants', msg: r.margin < 0 ? `About ${fmt(-r.margin * r.required, 0)} m/s short of ${r.orbit.label}. Try more propellant, a lighter payload, or a higher-Isp upper stage.` : `Only ${fmt(r.margin * 100, 0)}% Δv margin for ${r.orbit.label}. A little more propellant gives breathing room.` });
    if (design.stages.some((s) => s.landingPropMass > 0) && !(design.landing?.legs && design.landing?.gridFins)) warnings.push({ code: 'landing-hardware', tab: 'recovery', msg: 'Landing fuel is reserved but legs or grid fins are missing. Add them, or set the landing reserve to 0.' });
    if (r.mass.fairing > Math.max(500, r.mass.payload)) warnings.push({ code: 'heavy-fairing', tab: 'payload', msg: `The fairing (${fmt(r.mass.fairing / 1000, 1)} t) outweighs the payload. A composite or shorter fairing saves mass.` });
  }
  return { ok: blocks.length === 0, blocks, warnings, readout: r };
}

function engineFor(propellant, upper) {
  const list = ENGINES.filter((e) => e.propellant === propellant);
  return (upper ? list.find((e) => e.thrustSL === 0) : list.find((e) => e.thrustSL > 0)) || list[0];
}

/**
 * KID "Make it fly": nudges a design until there are no hard blocks and (where reachable)
 * the Δv margin warning is gone. Returns { design, changes:[string] }. Never mutates input.
 */
export function makeItFly(input) {
  const d = clone(input);
  const changes = [];
  const say = (m) => { if (!changes.includes(m)) changes.push(m); };
  if (!Array.isArray(d.stages) || !d.stages.length) { d.stages = [{ name: 'Stage 1', propellant: 'kerolox', engine: 'gg-kerolox', engineCount: 5, propMass: 100000, diameter: 3, material: 'al-li', interstageMass: 0, separationMass: 50, landingPropMass: 0 }]; say('Added a first stage'); }
  const fixPart = (s, i, upper) => {
    const e = ENGINE_BY_ID[s.engine];
    if (!e || e.propellant !== s.propellant) { const ne = engineFor(s.propellant, upper) || ENGINE_BY_ID['gg-kerolox']; s.engine = ne.id; s.propellant = ne.propellant; say(`Matched ${s.name || 'stage'} engine to its propellant`); }
    if (!(s.engineCount >= 1)) { s.engineCount = 1; say(`Gave ${s.name || 'stage'} an engine`); }
    s.engineCount = Math.min(40, Math.round(s.engineCount));
    if (!(s.propMass > 0)) { s.propMass = upper ? 10000 : 50000; say(`Loaded propellant into ${s.name || 'stage'}`); }
    const en = ENGINE_BY_ID[s.engine];
    const need = Math.sqrt(s.engineCount * en.diameter ** 2 / PACK) + 0.05;
    if (s.diameter < need) { s.diameter = Math.min(15, Math.ceil(need * 10) / 10); say(`Widened ${s.name || 'stage'} so the engines fit`); }
  };
  d.stages.forEach((s, i) => fixPart(s, i, i > 0));
  if (d.strapOns?.count) {
    if (d.strapOns.kind === 'solid') { d.strapOns.propellant = 'solid-apcp'; d.strapOns.crossfeed = false; }
    fixPart(d.strapOns, -1, false);
  }
  for (let i = 1; i < d.stages.length; i++) if (d.stages[i].diameter > d.stages[i - 1].diameter * 1.6) { d.stages[i].diameter = d.stages[i - 1].diameter; say('Matched upper stage width to the stage below'); }
  const top = d.stages[d.stages.length - 1];
  if (d.fairing.diameter < top.diameter) { d.fairing.diameter = top.diameter; say('Widened the fairing to match the top stage'); }
  const pl = PAYLOAD_BY_ID[d.payload?.type];
  if (pl) {
    const needD = requiredDiameter(pl.diameter || 0);
    if (d.fairing.diameter < needD) { d.fairing.diameter = Math.ceil(needD * 10) / 10; say('Widened the fairing so the payload fits'); }
    if (!payloadFit(d).fits) { d.fairing.length = Math.ceil(requiredLength(payloadVolume(d), d.fairing.diameter) * 10) / 10 + 0.5; say('Stretched the fairing so the payload fits'); }
  }
  if (d.stages.some((s) => s.landingPropMass > 0) && !(d.landing.legs && d.landing.gridFins)) { d.landing.legs = true; d.landing.gridFins = true; say('Added landing legs and grid fins'); }

  for (let iter = 0; iter < 80; iter++) {
    const v = validateEngineering(d);
    const r = v.readout;
    if (!r) break;
    const s0 = d.stages[0];
    if (r.liftoffTWR < 1.3) {
      const e = ENGINE_BY_ID[s0.engine];
      const maxFit = Math.floor(PACK * s0.diameter ** 2 / e.diameter ** 2);
      if (s0.engineCount < Math.min(40, maxFit)) { s0.engineCount++; say('Added first-stage engines for a stronger liftoff'); }
      else if (s0.diameter < 15 && s0.engineCount < 40) { s0.diameter = Math.min(15, +(s0.diameter + 0.3).toFixed(1)); say('Widened the first stage to fit more engines'); }
      else { s0.propMass *= 0.9; say('Trimmed first-stage propellant to lighten liftoff'); }
      continue;
    }
    const weak = r.stages.findIndex((s, i) => i > 0 && s.twrVac < 0.5);
    if (weak > 0) {
      const si = r.serial[weak].index; const s = d.stages[si];
      if (s && s.engineCount < 40) { s.engineCount++; say(`Added an engine to ${s.name}`); continue; }
    }
    if (r.margin < MIN_MARGIN) {
      // Kid-friendly order: grow upper stage, then booster (if TWR allows), then lighten payload.
      const up = d.stages[d.stages.length - 1];
      if (d.stages.length > 1 && r.stages[r.stages.length - 1].twrVac > 0.7 && iter % 2 === 0) { up.propMass *= 1.15; say('Added upper-stage propellant'); continue; }
      if (r.liftoffTWR > 1.45) { s0.propMass *= 1.1; say('Added first-stage propellant'); continue; }
      if (d.payload.mass > 50) { d.payload.mass = Math.max(50, Math.round(d.payload.mass * 0.85)); say('Lightened the payload'); continue; }
    }
    if (v.ok && r.margin >= MIN_MARGIN) break;
    if (v.ok && iter > 60) break;
  }
  return { design: d, changes };
}
