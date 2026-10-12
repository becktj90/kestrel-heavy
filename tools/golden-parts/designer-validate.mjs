// Designer validation, Make it fly, and experience layers. JS is the reference.
import { validateEngineering, makeItFly } from '../../web/js/designer/validate.js';
import { present, resolveLayer, availableLayers } from '../../web/js/designer/experience.js';

const clone = (d) => JSON.parse(JSON.stringify(d));
const issue = (x) => {
  const o = { code: x.code, tab: x.tab, msg: x.msg };
  if (x.index !== undefined) o.index = x.index;
  return o;
};
const slim = (r) => (r ? {
  liftoffTWR: r.liftoffTWR, totalDeltaV: r.totalDeltaV, margin: r.margin, required: r.required,
  targetAlt: r.targetAlt, liftoffMass: r.liftoffMass, cost: r.cost, reliability: r.reliability,
  orbitLabel: r.orbit.label, fairingMass: r.mass.fairing, payloadMass: r.mass.payload,
  stages: r.stages.map((s) => ({ label: s.label, role: s.role, twr: s.twr, twrVac: s.twrVac, deltaV: s.deltaV })),
} : null);
const pack = (d) => {
  const v = validateEngineering(d);
  return { ok: v.ok, blocks: v.blocks.map(issue), warnings: v.warnings.map(issue), readout: slim(v.readout) };
};

export async function register({ put, PRESETS }) {
  const cases = [];
  const byId = Object.fromEntries(PRESETS.map((p) => [p.id, p]));
  for (const p of PRESETS) cases.push({ kind: 'validate', name: `preset:${p.id}`, design: p, out: pack(p) });

  const breakers = [
    ['as-is', (d) => d],
    ['one-engine', (d) => { d.stages[0].engineCount = 1; }],
    ['zero-prop', (d) => { d.stages[0].propMass = 0; }],
    ['zero-upper', (d) => { d.stages.at(-1).engineCount = 0; }],
    ['heavy-payload', (d) => { d.payload.mass *= 3; }],
    ['cluster-40', (d) => { d.stages[0].engineCount = 40; }],
    ['wrong-engine', (d) => { d.stages[0].engine = 'exp-hydrolox'; }],
    ['landing-fuel', (d) => { d.stages[0].landingPropMass = 5000; d.landing.legs = false; }],
  ];
  const scout = clone(byId.scout);
  const shown = [
    ['twr-low', () => { const d = clone(byId.scout); d.stages[0].engineCount = 2; return d; }],
    ['zero-engines', () => { const d = clone(byId.scout); d.stages[1].engineCount = 0; return d; }],
    ['zero-propellant', () => { const d = clone(byId.scout); d.stages[0].propMass = 0; return d; }],
    ['engines-dont-fit', () => { const d = clone(byId.scout); d.stages[0].engineCount = 30; return d; }],
    ['top-heavy', () => { const d = clone(byId.scout); d.stages[1].diameter = 6; return d; }],
    ['payload-doesnt-fit', () => { const d = clone(byId.kid); d.payload = { type: 'crew-capsule', mass: 12000, orbit: 'leo400' }; d.fairing.length = 1; return d; }],
    ['twr-marginal', () => { const d = clone(byId.scout); d.stages[0].engineCount = 5; return d; }],
    ['weak-upper', () => { const d = clone(byId['tpl-medium-kerolox']); d.stages[1].engine = 'exp-hydrolox'; d.stages[1].propellant = 'hydrolox'; return d; }],
    ['dv-margin', () => { const d = clone(byId.scout); d.payload.mass = 3000; return d; }],
    ['landing-hardware', () => { const d = clone(byId.kid); d.stages[0].landingPropMass = 10000; return d; }],
    ['heavy-fairing', () => { const d = clone(byId.scout); d.fairing.material = 'steel'; d.fairing.length = 8; return d; }],
    ['fractional-count', () => { const d = clone(byId.scout); d.stages[0].engineCount = 1.5; return d; }],
    ['schema-name', () => { const d = clone(byId.scout); delete d.name; return d; }],
  ];
  for (const [name, build] of shown) {
    const design = build();
    cases.push({ kind: 'validate', name, design, out: pack(design) });
  }

  for (const p of PRESETS) for (const [bi, [bname, br]] of breakers.entries()) {
    const d = clone(p); br(d);
    const before = JSON.stringify(d);
    const fly = makeItFly(d);
    if (JSON.stringify(d) !== before) throw new Error(`makeItFly mutated ${p.id}/#${bi}`);
    cases.push({ kind: 'makeItFly', name: `${p.id}#${bi}:${bname}`, design: d, out: { changes: fly.changes, design: fly.design } });
  }

  const schemaOnly = pack((() => { const d = clone(byId.scout); delete d.name; return d; })());
  const marginal = pack((() => { const d = clone(byId.scout); d.stages[0].engineCount = 5; return d; })());
  for (const layer of ['simple', 'standard', 'engineer']) {
    cases.push({ kind: 'present', name: `schema:${layer}`, layer, report: { ok: schemaOnly.ok, blocks: schemaOnly.blocks, warnings: schemaOnly.warnings }, out: present({ ok: schemaOnly.ok, blocks: schemaOnly.blocks, warnings: schemaOnly.warnings }, layer) });
    cases.push({ kind: 'present', name: `marginal:${layer}`, layer, report: { ok: marginal.ok, blocks: marginal.blocks, warnings: marginal.warnings }, out: present({ ok: marginal.ok, blocks: marginal.blocks, warnings: marginal.warnings }, layer) });
  }
  const ready = pack(scout);
  cases.push({ kind: 'present', name: 'ready:simple', layer: 'simple', report: { ok: ready.ok, blocks: ready.blocks, warnings: ready.warnings }, out: present({ ok: ready.ok, blocks: ready.blocks, warnings: ready.warnings }, 'simple') });

  const resolves = [
    [{}, 'default'],
    [{ layer: 'standard', orbitsCompleted: 0 }, 'standard-locked'],
    [{ layer: 'standard', orbitsCompleted: 1 }, 'standard-open'],
    [{ layer: 'engineer', engineerEnabled: false, orbitsCompleted: 0 }, 'engineer-before-orbit'],
    [{ layer: 'engineer', engineerEnabled: false, orbitsCompleted: 1 }, 'engineer-after-orbit'],
    [{ layer: 'engineer', engineerEnabled: true, orbitsCompleted: 0 }, 'engineer-toggle'],
  ];
  for (const [opts, name] of resolves) cases.push({ kind: 'resolve', name, opts, out: resolveLayer(opts), available: availableLayers(opts) });

  const nums = [0, 1, 1.005, 0.95, 1.15, 1.335, 2.675, -0.001, 1.1, 2.3, 9.99, 0.1 + 0.2];
  cases.push({ kind: 'toFixed', name: 'toFixed', out: nums.map((n) => ({ n, d0: n.toFixed(0), d1: n.toFixed(1), d2: n.toFixed(2) })) });

  put('designer-validate', {
    rel: 1e-9, abs: 1e-9, relReliability: 1e-12,
    note: 'messages and makeItFly changes exact; readout closed-form; cost exact integer minor units',
  }, cases);
}
