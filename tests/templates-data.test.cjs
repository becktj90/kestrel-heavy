// T1 starter templates: schema, metadata, reference missions, stats, presets wiring.
const assert = require('node:assert');
const path = require('node:path');
const fs = require('node:fs');
const test = require('node:test');
const root = path.join(__dirname, '..');
const dir = path.join(root, 'web', 'js', 'data', 'templates');
const files = fs.readdirSync(dir).filter((f) => f.endsWith('.json'));
const load = async () => ({
  ...(await import(path.join(root, 'tools/templates/evaluate.mjs'))),
  ...(await import(path.join(dir, 'index.js'))),
  ...(await import(path.join(root, 'web/js/data/presets.js'))),
});
const read = (f) => JSON.parse(fs.readFileSync(path.join(dir, f), 'utf8'));

test('the first five templates (plus the reusable variant) exist', () => {
  const ids = files.map((f) => read(f).id).sort();
  for (const id of ['tpl-sounding-hybrid', 'tpl-small-kerolox', 'tpl-medium-kerolox', 'tpl-medium-kerolox-reusable', 'tpl-hydrogen-upper', 'tpl-methane-reusable']) assert.ok(ids.includes(id), id);
});

test('every template passes vehicle schema and template-meta validation, with no hard blocks', async () => {
  const { evaluateTemplate } = await load();
  for (const f of files) {
    const r = evaluateTemplate(read(f));
    assert.deepStrictEqual(r.schema, [], `${r.id} schema`);
    assert.deepStrictEqual(r.meta, [], `${r.id} meta`);
    assert.deepStrictEqual(r.blocks.map((b) => b.code), [], `${r.id} blocks`);
  }
});

test('every template reaches its reference objective in the headless sim', async () => {
  const { evaluateTemplate } = await load();
  for (const f of files) {
    const t = read(f); const r = evaluateTemplate(t);
    assert.ok(r.pass, `${t.id}: ${r.why}`);
    if (t.template.refMission.objective === 'apogee') assert.ok(r.sim.apogeeKm >= 100, `${t.id} clears 100 km`);
    else { assert.strictEqual(r.sim.outcome, 'orbit', t.id); assert.ok(r.sim.perigeeKm > 120, `${t.id} perigee`); }
  }
});

test('recorded stats match the catalogs and sim (no stale numbers)', async () => {
  const { evaluateTemplate } = await load();
  for (const f of files) { const t = read(f); assert.deepStrictEqual(t.template.stats, evaluateTemplate(t).stats, `${t.id}: run npm run templates:build`); }
});

test('generated index.js matches the JSON and presets reference it without duplicating', async () => {
  const { TEMPLATES, PRESETS } = await load();
  assert.strictEqual(TEMPLATES.length, files.length);
  for (const f of files) { const t = read(f); assert.deepStrictEqual(TEMPLATES.find((x) => x.id === t.id), t, `${t.id} index.js stale`); }
  for (const t of TEMPLATES) assert.strictEqual(PRESETS.find((p) => p.id === t.id), t, `${t.id} preset is the same object`);
  assert.ok(!PRESETS.some((p) => p.id === 'small-lift' || p.id === 'medium-lift'), 'old duplicates removed');
});

test('reuse costs payload: the reusable variant carries less than its expendable sibling', async () => {
  const { TEMPLATE_BY_ID: T } = await load();
  const r = T['tpl-medium-kerolox-reusable'], e = T['tpl-medium-kerolox'];
  assert.strictEqual(r.template.variantOf, e.id);
  assert.ok(r.payload.mass < e.payload.mass);
  assert.ok(r.stages[0].landingPropMass > 0 && r.landing.legs && r.landing.gridFins);
});

test('player-facing text names no real vehicle, engine or company; sources are cited', async () => {
  const { TEMPLATES } = await load();
  const banned = /falcon|merlin|electron|rutherford|atlas|centaur|rl10|spacex|rocket lab|\bula\b|starship|raptor|neutron|ariane|soyuz|saturn|nasa|skylark|petrel/i;
  for (const t of TEMPLATES) {
    const m = t.template;
    for (const s of [t.name, t.livery.name, m.tagline, m.brief, m.benchmark.class, m.benchmark.text, ...m.thingsToTry, ...m.tags]) assert.ok(!banned.test(s), `${t.id}: "${s}"`);
    assert.ok(m.benchmark.sources.length >= 1, t.id);
  }
});

// Pending until their modules land on main; flip to real checks then.
test('template pad compatibility at its tech tier', { todo: 'PENDING: needs G2 pad module (vehicle/compat.js)' }, () => {});
test('every template prices cleanly through the E1 cost model; reuse hardware shows up as recovery cost', async () => {
  const { TEMPLATES } = await load();
  const { priceDesign, MINOR_PER_KC } = await import(path.join(root, 'web/js/economy/costModel.js'));
  for (const t of TEMPLATES) {
    const r = priceDesign(t);
    assert.ok(Number.isSafeInteger(r.total) && Number.isSafeInteger(r.recurring) && r.recurring > 0, t.id);
    assert.strictEqual(Object.values(r.byCategory).reduce((a, b) => a + b, 0), r.recurring, `${t.id} breakdown sums`);
    assert.ok(r.recurring / MINOR_PER_KC > 100, `${t.id} not free`);
    if (t.landing.legs) assert.ok(r.byCategory.recovery > 0, `${t.id} recovery hardware priced`);
  }
});
test('template cost within its campaign tier budget', { todo: 'PENDING: tier budgets arrive with E3 contracts' }, () => {});
