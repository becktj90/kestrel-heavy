// Design schema, mass model, stack and toSimStages.
const assert = require('node:assert');
const path = require('node:path');
const test = require('node:test');
const js = path.join(__dirname, '..', 'web', 'js');
const load = async () => ({
  ...(await import(path.join(js, 'vehicle/design.js'))),
  ...(await import(path.join(js, 'vehicle/mass.js'))),
  ...(await import(path.join(js, 'vehicle/stack.js'))),
  ...(await import(path.join(js, 'data/presets.js'))),
});
const G0 = 9.80665;

test('all presets validate and include required archetypes', async () => {
  const { PRESETS, validateDesign } = await load();
  const names = PRESETS.map((p) => p.name);
  for (const n of ['Stock Kestrel Heavy', 'Scout', 'Kid Cruiser']) assert.ok(names.includes(n), n);
  for (const t of ['small', 'medium', 'super-heavy']) assert.ok(PRESETS.some((p) => p.tier === t), t);
  for (const p of PRESETS) assert.deepStrictEqual(validateDesign(p).errors, [], p.id);
});

test('preset mass totals, liftoff TWR and ideal Δv', async () => {
  const { PRESETS, vehicleMass, toSimStages, idealDeltaV } = await load();
  // Expected liftoff masses (kg) for the M1 mass model, ±2%. Update deliberately if the model changes.
  const expected = { stock: 1041994, scout: 44330, kid: 164993, 'super-heavy': 5133142 };
  for (const p of PRESETS) {
    const m = vehicleMass(p);
    const sum = m.stages.reduce((a, s) => a + s.wet, 0) + m.strapOns.wet + m.payload + m.fairing;
    assert.ok(Math.abs(sum - m.liftoffMass) < 1e-6, `${p.id} totals add up`);
    const exp = expected[p.id] ?? p.template.stats.liftoffMassT * 1000; // templates: recorded stats
    assert.ok(Math.abs(m.liftoffMass / exp - 1) < 0.02, `${p.id} liftoff ${Math.round(m.liftoffMass)}`);
    const s = toSimStages(p);
    const sim = s.reduce((a, x) => a + x.propMass + x.dryMass + (x.reserveMass || 0), 0) + s.at(-1).payloadMass + s.at(-1).fairingMass;
    assert.ok(Math.abs(sim - m.liftoffMass) < 1e-6, `${p.id} sim stages conserve mass`);
    const thrust = s[0].thrustSL + (s[0].parallel ? s[1].thrustSL : 0);
    assert.ok(thrust / (m.liftoffMass * G0) > 1.2, `${p.id} liftoff TWR`);
    if (p.payload.orbit !== 'suborbital') assert.ok(idealDeltaV(s).total > 9400, `${p.id} has orbital-class ideal Δv`);
    m.ratios.forEach((r) => assert.ok(r.massRatio > 1, p.id));
  }
});

test('toSimStages shape', async () => {
  const { PRESET_BY_ID, toSimStages } = await load();
  const s = toSimStages(PRESET_BY_ID.stock);
  assert.strictEqual(s.length, 3);
  assert.strictEqual(s[0].role, 'strapons'); assert.strictEqual(s[0].parallel, true);
  for (const e of s) for (const k of ['thrustSL', 'thrustVac', 'ispSL', 'ispVac', 'propMass', 'dryMass', 'diameter', 'Cd']) assert.ok(Number.isFinite(e[k]) && e[k] >= 0, k);
  assert.ok(s.at(-1).payloadMass > 0 && s.at(-1).fairingMass > 0);
  assert.strictEqual(s[1].reserveMass, 25000);
});

test('validation catches bad shapes', async () => {
  const { defaultDesign, validateDesign } = await load();
  assert.ok(validateDesign(defaultDesign()).ok);
  const bad = defaultDesign({ stages: [] });
  assert.ok(!validateDesign(bad).ok);
  const six = defaultDesign(); six.stages = Array(6).fill(six.stages[0]);
  assert.ok(!validateDesign(six).ok);
  const mism = defaultDesign(); mism.stages[0].engine = 'sc-hydrolox';
  assert.match(validateDesign(mism).errors.join(), /burns hydrolox/);
  const so = defaultDesign(); so.strapOns = { ...so.strapOns, count: 3 };
  assert.ok(!validateDesign(so).ok);
  const solidX = defaultDesign(); solidX.strapOns = { ...solidX.strapOns, count: 2, propMass: 10000, crossfeed: true };
  assert.match(validateDesign(solidX).errors.join(), /crossfeed/);
});

test('migration from unversioned draft, refuses future versions', async () => {
  const { migrateDesign, validateDesign } = await load();
  const d = migrateDesign({ name: 'Old draft' });
  assert.strictEqual(d.version, 1); assert.strictEqual(d.name, 'Old draft');
  assert.ok(validateDesign(d).ok);
  assert.throws(() => migrateDesign({ version: 99 }));
});

test('tank sizing and staging sequence', async () => {
  const { sizeTank, stagingSequence, PRESET_BY_ID } = await load();
  const k = sizeTank('kerolox', 100000, 3.7), h = sizeTank('hydrolox', 100000, 3.7);
  assert.ok(h.length > 2.5 * k.length, 'LH2 needs much longer tanks');
  assert.ok(Math.abs(k.volume - (100000 / 1030) * 1.03) < 1e-9);
  const ev = stagingSequence(PRESET_BY_ID.stock).map((e) => e.type);
  assert.strictEqual(ev[0], 'ignition');
  assert.ok(ev.indexOf('strapon-sep') < ev.indexOf('stage-sep'));
  assert.ok(ev.includes('fairing-jettison')); assert.strictEqual(ev.at(-1), 'payload-sep');
});
