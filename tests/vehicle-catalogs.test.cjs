// Catalog sanity for web/js/data (ESM loaded via dynamic import).
const assert = require('node:assert');
const path = require('node:path');
const test = require('node:test');
const js = path.join(__dirname, '..', 'web', 'js');

test('propellant catalog is physically sane', async () => {
  const { PROPELLANTS } = await import(path.join(js, 'data/propellants.js'));
  const ids = ['kerolox', 'hydrolox', 'methalox', 'udmh-nto', 'mmh-nto', 'solid-apcp', 'hybrid'];
  assert.deepStrictEqual(PROPELLANTS.map((p) => p.id).sort(), [...ids].sort());
  for (const p of PROPELLANTS) {
    assert.ok(p.bulkDensity > 50 && p.bulkDensity < 2500, p.id);
    assert.ok(p.ispVac > p.ispSL && p.ispSL > 150 && p.ispVac < 480, p.id);
    assert.ok(p.ispVacMax >= p.ispVac, p.id);
    assert.ok(p.costPerKg > 0 && typeof p.storability === 'string', p.id);
  }
  const by = Object.fromEntries(PROPELLANTS.map((p) => [p.id, p]));
  // Density vs specific impulse: hydrolox wins on Isp, loses badly on density impulse.
  assert.ok(by.hydrolox.ispVac > by.kerolox.ispVac);
  assert.ok(by.hydrolox.bulkDensity * by.hydrolox.ispVac < 0.6 * by.kerolox.bulkDensity * by.kerolox.ispVac);
});

test('engine catalog is generic and self-consistent', async () => {
  const { ENGINES, ENGINE_CYCLES } = await import(path.join(js, 'data/engines.js'));
  const { PROPELLANT_BY_ID } = await import(path.join(js, 'data/propellants.js'));
  const cycles = new Set(ENGINES.map((e) => e.cycle));
  for (const c of ['gas-generator', 'staged-combustion', 'full-flow', 'expander', 'pressure-fed', 'solid']) assert.ok(cycles.has(c), c);
  for (const e of ENGINES) {
    assert.ok(ENGINE_CYCLES.includes(e.cycle), e.id);
    assert.ok(PROPELLANT_BY_ID[e.propellant], e.id);
    assert.ok(e.thrustVac >= e.thrustSL && e.thrustVac > 0, e.id);
    assert.ok(e.ispVac > e.ispSL || e.ispSL === 0, e.id);
    assert.ok(e.ispVac <= PROPELLANT_BY_ID[e.propellant].ispVacMax, `${e.id} beats its propellant`);
    assert.ok(e.throttle[0] > 0 && e.throttle[0] <= e.throttle[1], e.id);
    assert.ok(e.reliability > 0.9 && e.reliability < 1, e.id);
    for (const k of ['mass', 'pc', 'gimbal', 'cost', 'expansionRatio']) assert.ok(typeof e[k] === 'number' && e[k] >= 0, `${e.id}.${k}`);
    assert.strictEqual(typeof e.restart, 'boolean');
    if (e.cycle === 'solid') assert.ok(!e.restart && e.throttle[0] === 1, e.id);
    // Thrust-to-weight of liquid engines within real-world envelope.
    if (e.mass > 0) { const twr = e.thrustVac / (e.mass * 9.80665); assert.ok(twr > 10 && twr < 220, `${e.id} T/W ${twr}`); }
  }
});

test('tank, fairing, interstage and fin catalogs reference real materials', async () => {
  const { MATERIAL_BY_ID } = await import(path.join(js, 'data/materials.js'));
  const { PROPELLANT_BY_ID } = await import(path.join(js, 'data/propellants.js'));
  const { TANKS } = await import(path.join(js, 'data/tanks.js'));
  const { FAIRINGS } = await import(path.join(js, 'data/fairings.js'));
  const { INTERSTAGES } = await import(path.join(js, 'data/interstages.js'));
  const { FINS } = await import(path.join(js, 'data/fins.js'));
  const ids = (rows) => {
    const seen = new Set();
    for (const row of rows) {
      assert.ok(!seen.has(row.id), `duplicate ${row.id}`);
      seen.add(row.id);
    }
  };
  ids(TANKS); ids(FAIRINGS); ids(INTERSTAGES); ids(FINS);
  for (const t of TANKS) {
    assert.ok(MATERIAL_BY_ID[t.material], t.id);
    if (t.propellant) assert.ok(PROPELLANT_BY_ID[t.propellant], t.id);
    assert.ok(t.dryMass > 0 && t.cost > 0 && t.diameter > 0 && t.length > 0, t.id);
  }
  for (const f of FAIRINGS) {
    assert.ok(MATERIAL_BY_ID[f.material], f.id);
    assert.ok(f.mass > 0 && f.cost > 0 && f.diameter > 0 && f.cd > 0 && f.cd < 1, f.id);
  }
  for (const s of INTERSTAGES) {
    assert.ok(MATERIAL_BY_ID[s.material], s.id);
    assert.ok(s.mass > 0 && s.cost > 0 && s.dragDeltaCd >= 0 && s.dragDeltaCd < 0.2, s.id);
  }
  for (const fin of FINS) {
    assert.ok(fin.mass > 0 && fin.cost > 0 && fin.span > 0, fin.id);
    assert.ok(fin.count === 3 || fin.count === 4, fin.id);
    if (fin.diameterMin && fin.diameterMax) assert.ok(fin.diameterMax > fin.diameterMin, fin.id);
  }
});

test('materials and payloads', async () => {
  const { MATERIALS } = await import(path.join(js, 'data/materials.js'));
  const { PAYLOADS, ORBITS } = await import(path.join(js, 'data/payloads.js'));
  const by = Object.fromEntries(MATERIALS.map((m) => [m.id, m]));
  assert.ok(by.composite.structuralFraction < by['al-li'].structuralFraction);
  assert.ok(by['al-li'].structuralFraction < by.steel.structuralFraction);
  for (const p of PAYLOADS) { assert.ok(ORBITS[p.orbit], p.id); assert.ok(p.mass > 0 && p.maxG > 0, p.id); }
});
