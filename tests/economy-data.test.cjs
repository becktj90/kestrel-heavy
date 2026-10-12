// E1 economy-data: JSON is the source of truth; the generated JS mirror must match; schema-ish checks.
const assert = require('node:assert');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const dir = path.join(__dirname, '..', 'web', 'js', 'economy-data');

test('constants.js mirror equals constants.json (run node tools/gen-economy-data.mjs)', async () => {
  const json = JSON.parse(fs.readFileSync(path.join(dir, 'constants.json'), 'utf8'));
  delete json.$schema;
  const { ECON } = await import(path.join(dir, 'constants.js'));
  assert.deepStrictEqual(JSON.parse(JSON.stringify(ECON)), json);
});

test('constants satisfy the schema essentials and cover every catalog id', async () => {
  const schema = JSON.parse(fs.readFileSync(path.join(dir, 'schemas', 'constants.schema.json'), 'utf8'));
  const c = JSON.parse(fs.readFileSync(path.join(dir, 'constants.json'), 'utf8'));
  for (const k of schema.required) assert.ok(k in c, `missing ${k}`);
  assert.strictEqual(c.currency.code, 'Kc');
  const ints = (o, where) => { for (const [k, v] of Object.entries(o)) assert.ok(Number.isSafeInteger(v) && v >= 0, `${where}.${k}`); };
  ints(c.structure.materialPerKg, 'materialPerKg'); ints(c.structure.materialPerM2, 'materialPerM2'); ints(c.propellant.perKg, 'propellant.perKg'); ints(c.recovery, 'recovery');
  for (const k of ['kDev', 'kUnit']) assert.ok(Number.isSafeInteger(c.engine[k]));
  const { ENGINES, ENGINE_CYCLES } = await import(path.join(dir, '..', 'data', 'engines.js'));
  const { PROPELLANTS } = await import(path.join(dir, '..', 'data', 'propellants.js'));
  const { MATERIALS } = await import(path.join(dir, '..', 'data', 'materials.js'));
  for (const e of ENGINES) { assert.ok(c.engine.trl[e.id] >= 1 && c.engine.trl[e.id] <= 9, `trl ${e.id}`); }
  for (const cy of ENGINE_CYCLES) assert.ok(c.engine.cycleMultiplier[cy] > 0, cy);
  for (const p of PROPELLANTS) assert.ok(p.id in c.propellant.perKg, p.id);
  for (const m of MATERIALS) assert.ok(m.id in c.structure.materialPerKg && m.id in c.structure.materialPerM2, m.id);
  assert.strictEqual(c.failure.policy, 'moderate');
  assert.deepStrictEqual(c.failure.padDamageFraction, [0.1, 0.4]);
  assert.strictEqual(c.failure.lessonsLearnedRefund, 0.15);
  assert.strictEqual(c.difficulty.default, 'standard');
});

test('economy bus events are registered and documented', async () => {
  const { EVENTS } = await import(path.join(dir, '..', 'narration', 'bus.js'));
  const doc = fs.readFileSync(path.join(__dirname, '..', 'docs', 'FLIGHT_EVENTS.md'), 'utf8');
  for (const t of ['econ.quote', 'econ.purchase']) { assert.strictEqual(EVENTS[t].cat, 'economy'); assert.ok(doc.includes('`' + t + '`'), t); }
});
