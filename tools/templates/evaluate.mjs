// Template evaluation: schema + engineering validation, quick readout and the headless
// reference mission (same simReadout the designer uses). Pure; used by build.mjs and tests.
import { validateDesign, migrateDesign } from '../../web/js/vehicle/design.js';
import { validateEngineering } from '../../web/js/designer/validate.js';
import { simReadout } from '../../web/js/designer/readout.js';
import { ORBITS } from '../../web/js/data/payloads.js';

export const SIM_DT = 0.2; // same step the designer's live readout uses
export const REQUIRED_META = ['version', 'order', 'difficulty', 'tags', 'tagline', 'brief', 'benchmark', 'thingsToTry', 'refMission', 'pending'];
export const DIFFICULTIES = ['beginner', 'easy', 'medium', 'hard', 'expert'];

/** Template-block shape errors (the vehicle part is checked by validateDesign). */
export function validateTemplateMeta(t) {
  const e = [], m = t.template;
  if (!/^tpl-[a-z0-9-]+$/.test(t.id || '')) e.push('id: tpl-<kebab>');
  if (!m || typeof m !== 'object') return [...e, 'template: block required'];
  for (const k of REQUIRED_META) if (m[k] == null) e.push(`template.${k}: required`);
  if (!DIFFICULTIES.includes(m.difficulty)) e.push(`template.difficulty: one of ${DIFFICULTIES.join(', ')}`);
  if (!Array.isArray(m.tags) || !m.tags.length) e.push('template.tags: non-empty array');
  if (!Array.isArray(m.thingsToTry) || m.thingsToTry.length < 2) e.push('template.thingsToTry: at least 2');
  const b = m.benchmark || {};
  if (!b.class || !b.text) e.push('template.benchmark: class and text required');
  if (!Array.isArray(b.sources) || !b.sources.length || b.sources.some((s) => !/^https:\/\//.test(s.url || '') || !s.title || !s.claim)) e.push('template.benchmark.sources: [{title, url (https), claim}]');
  const r = m.refMission || {};
  if (!['orbit', 'apogee'].includes(r.objective)) e.push('template.refMission.objective: orbit | apogee');
  if (r.objective === 'apogee' && !(r.minApogeeKm > 0)) e.push('template.refMission.minApogeeKm required');
  if (r.objective === 'orbit' && ORBITS[t.payload?.orbit]?.periKm < 0) e.push('orbit objective needs an orbital target');
  return e;
}

const round = (v, d = 0) => Math.round(v * 10 ** d) / 10 ** d;

/** Full evaluation. stats are what the gallery shows; pass = reaches its reference objective. */
export function evaluateTemplate(t) {
  const design = migrateDesign(t);
  const schema = validateDesign(design).errors;
  const meta = validateTemplateMeta(t);
  const eng = validateEngineering(design);
  const q = eng.readout;
  const sim = q && eng.ok ? simReadout(design, q, { dt: SIM_DT, sampleEvery: 0 }) : null;
  const ref = t.template?.refMission || {};
  let pass = false, why = '';
  if (sim) {
    if (ref.objective === 'apogee') { pass = sim.apogeeKm >= ref.minApogeeKm; why = `apogee ${round(sim.apogeeKm)} km vs ${ref.minApogeeKm} km`; }
    else { pass = sim.outcome === 'orbit' && q.margin >= 0; why = `sim ${sim.outcome} (Pe ${round(sim.perigeeKm)} km), Δv margin ${round(q.margin * 100, 1)}%`; }
  } else why = `not flyable: ${eng.blocks.map((b) => b.code).join(', ')}`;
  const stats = q ? {
    stages: design.stages.length,
    liftoffMassT: round(q.liftoffMass / 1000, 1),
    liftoffTWR: round(q.liftoffTWR, 2),
    deltaV: round(q.totalDeltaV),
    requiredDeltaV: round(q.required),
    marginPct: round(q.margin * 100, 1),
    payloadKg: design.payload.mass,
    target: ORBITS[design.payload.orbit].label,
    simOutcome: sim?.outcome ?? null,
    simApogeeKm: sim ? round(sim.apogeeKm) : null,
    simPerigeeKm: sim && sim.outcome === 'orbit' ? round(sim.perigeeKm) : null,
    maxG: sim ? round(sim.maxG, 1) : null,
  } : null;
  return { id: t.id, schema, meta, blocks: eng.blocks, warnings: eng.warnings.map((w) => w.code), sim, stats, pass, why };
}
