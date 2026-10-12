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

test('materials and payloads', async () => {
  const { MATERIALS } = await import(path.join(js, 'data/materials.js'));
  const { PAYLOADS, ORBITS } = await import(path.join(js, 'data/payloads.js'));
  const by = Object.fromEntries(MATERIALS.map((m) => [m.id, m]));
  assert.ok(by.composite.structuralFraction < by['al-li'].structuralFraction);
  assert.ok(by['al-li'].structuralFraction < by.steel.structuralFraction);
  for (const p of PAYLOADS) { assert.ok(ORBITS[p.orbit], p.id); assert.ok(p.mass > 0 && p.maxG > 0, p.id); }
});
