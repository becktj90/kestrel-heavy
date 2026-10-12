// E1 economy: cost model monotonicity, integer arithmetic, preset sanity, reliability with engine-out.
const assert = require('node:assert');
const path = require('node:path');
const test = require('node:test');
const js = path.join(__dirname, '..', 'web', 'js');
const load = async () => ({
  ...(await import(path.join(js, 'economy/costModel.js'))),
  ...(await import(path.join(js, 'economy/reliability.js'))),
  ...(await import(path.join(js, 'data/presets.js'))),
  ...(await import(path.join(js, 'data/engines.js'))),
  ...(await import(path.join(js, 'designer/readout.js'))),
});
const clone = (o) => structuredClone(o);

test('more thrust or chamber pressure never costs less (engine dev and unit)', async () => {
  const { ENGINES, engineDevCost, engineBatchCost } = await load();
  for (const e of ENGINES) {
    let pd = engineDevCost(e), pu = engineBatchCost(e, 1);
    for (const k of [1.05, 1.2, 1.5, 2, 4]) {
      const b = { ...e, thrustVac: e.thrustVac * k, thrustSL: e.thrustSL * k };
      assert.ok(engineDevCost(b) >= pd && engineBatchCost(b, 1) >= pu, `${e.id} thrust x${k}`);
      pd = engineDevCost(b); pu = engineBatchCost(b, 1);
    }
    const p = { ...e, pc: e.pc * 1.5 };
    assert.ok(engineDevCost(p) >= engineDevCost(e) && engineBatchCost(p, 1) >= engineBatchCost(e, 1), `${e.id} pc`);
  }
});

test('advanced cycles cost more than gas-generator at equal thrust/pc (fixes full-flow anomaly)', async () => {
  const { ENGINE_BY_ID, engineBatchCost } = await load();
  const g = ENGINE_BY_ID['gg-kerolox'];
  const as = (cycle) => engineBatchCost({ ...g, id: 'x', cycle }, 1);
  assert.ok(as('pressure-fed') < as('gas-generator') && as('gas-generator') < as('expander') && as('expander') < as('staged-combustion') && as('staged-combustion') < as('full-flow'));
});

test('per-design: more engines, bigger tanks, more strap-ons never cost less', async () => {
  const { PRESETS, priceDesign } = await load();
  for (const p of PRESETS) {
    const base = priceDesign(p).recurring;
    p.stages.forEach((_, i) => {
      const a = clone(p); a.stages[i].engineCount += 1;
      assert.ok(priceDesign(a).recurring >= base, `${p.id} +engine s${i}`);
      let prev = base;
      for (const k of [1.01, 1.1, 1.5, 2]) {
        const b = clone(p); b.stages[i].propMass = Math.round(p.stages[i].propMass * k);
        const c = priceDesign(b).recurring;
        assert.ok(c >= prev, `${p.id} tank x${k} s${i}`); prev = c;
      }
    });
    if (p.strapOns.count) { const s = clone(p); s.strapOns.count += 2; assert.ok(priceDesign(s).recurring >= base, `${p.id} strap-ons`); }
  }
});

test('learning curve 0.85: unit n costs n^log2(0.85) of unit 1; campaign units built lower the price', async () => {
  const { learningFactor, ENGINE_BY_ID, engineBatchCost } = await load();
  assert.ok(Math.abs(learningFactor(2) - 0.85) < 1e-12 && Math.abs(learningFactor(4) - 0.7225) < 1e-12);
  const e = ENGINE_BY_ID['gg-kerolox'];
  assert.ok(engineBatchCost(e, 9, 100) < engineBatchCost(e, 9, 0));
  assert.ok(engineBatchCost(e, 9) < 9 * engineBatchCost(e, 1));
});

test('all money is integer minor units and totals add exactly', async () => {
  const { PRESETS, priceDesign, marginalHints } = await load();
  for (const p of PRESETS) {
    const r = priceDesign(p);
    for (const l of [...r.lines, ...r.devLines]) assert.ok(Number.isSafeInteger(l.amount) && l.amount >= 0, `${p.id} ${l.label}`);
    assert.strictEqual(r.recurring, r.lines.reduce((a, l) => a + l.amount, 0));
    assert.strictEqual(r.dev, r.devLines.reduce((a, l) => a + l.amount, 0));
    assert.strictEqual(r.total, r.recurring + r.dev);
    assert.strictEqual(Object.values(r.byCategory).reduce((a, b) => a + b, 0), r.recurring);
    assert.ok(Number.isSafeInteger(r.perKg));
    for (const hnt of marginalHints(p)) assert.ok(Number.isSafeInteger(hnt.delta));
  }
});

test('developed engines are not charged dev again', async () => {
  const { PRESET_BY_ID, priceDesign } = await load();
  const p = PRESET_BY_ID['tpl-medium-kerolox'];
  const r = priceDesign(p, { devDone: ['gg-kerolox', 'gg-kerolox-vac', 'avionics:standard'] });
  assert.strictEqual(r.dev, 0);
  assert.strictEqual(r.recurring, priceDesign(p).recurring);
});

test('every preset gets a sane price and reliability', async () => {
  const { PRESETS, priceDesign, reliabilityOf, MINOR_PER_KC, quickReadout } = await load();
  for (const p of PRESETS) {
    const r = priceDesign(p), kc = r.recurring / MINOR_PER_KC, rel = reliabilityOf(p).reliability;
    assert.ok(kc > 1000 && kc < 1e6, `${p.id} price ${kc} Kc`);
    assert.ok(r.dev > 0, `${p.id} dev`);
    assert.ok(rel > 0.7 && rel < 0.9999 + 1e-12, `${p.id} reliability ${rel}`);
    const q = quickReadout(p);
    assert.strictEqual(q.cost, r.recurring); assert.strictEqual(q.reliability, rel);
  }
});

test('engine-out: super-heavy is well above the old 48% and clusters beat no-tolerance', async () => {
  const { PRESET_BY_ID, reliabilityOf, clusterReliability, engineOutTolerance, ENGINE_BY_ID } = await load();
  assert.ok(reliabilityOf(PRESET_BY_ID['super-heavy']).reliability > 0.7);
  const ff = ENGINE_BY_ID['ff-methalox'];
  assert.strictEqual(engineOutTolerance(33, ff), 3); assert.strictEqual(engineOutTolerance(9, ff), 1); assert.strictEqual(engineOutTolerance(3, ff), 0);
  assert.strictEqual(engineOutTolerance(9, ENGINE_BY_ID['srm-small']), 0);
  assert.ok(clusterReliability(33, 0.98, 3) > Math.pow(0.98, 33));
  assert.ok(Math.abs(clusterReliability(1, 0.99, 0) - 0.99) < 1e-12);
  // more tolerance never lowers reliability; better engines never lower it
  for (let k = 0; k < 5; k++) assert.ok(clusterReliability(9, 0.99, k + 1) >= clusterReliability(9, 0.99, k));
  assert.ok(clusterReliability(9, 0.995, 1) > clusterReliability(9, 0.99, 1));
});
