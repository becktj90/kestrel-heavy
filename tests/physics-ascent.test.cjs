// M1 physics: headless ascent sim.
const assert = require('node:assert');
const path = require('node:path');
const test = require('node:test');
const phys = path.join(__dirname, '..', 'web', 'js', 'physics');

test('integrated Δv matches Tsiolkovsky within 1% (vacuum, no gravity losses counted)', async () => {
  const { simulate, stackDeltaV, MEDIUM_LIFTER } = await import(path.join(phys, 'headless.js'));
  const r = simulate(MEDIUM_LIFTER, { payload: 10000, vacuum: true, rotate: false, cutoff: false, tMax: 2000 });
  const ideal = stackDeltaV(MEDIUM_LIFTER, 10000).total;
  assert.ok(Math.abs(r.deltaVExpended - ideal) / ideal < 0.01, `${r.deltaVExpended} vs ${ideal}`);
  // single stage in pure vacuum, no gravity: integrated velocity change equals Tsiolkovsky
  const { createRK4 } = await import(path.join(phys, 'integrator.js'));
  const { deltaV, massFlow, G0 } = await import(path.join(phys, 'rocket-eq.js'));
  const F = 1e6, isp = 320, m0 = 100000, mf = 20000, md = massFlow(F, isp);
  const y = new Float64Array([0, m0]); const step = createRK4(2);
  const f = (t, s, o) => { o[0] = F / s[1]; o[1] = -md; };
  const T = (m0 - mf) / md, n = 2000;
  for (let i = 0; i < n; i++) step(f, 0, y, T / n);
  assert.ok(Math.abs(y[0] - deltaV(isp, m0, mf)) / deltaV(isp, m0, mf) < 0.01);
  void G0;
});

test('generic medium-lifter archetype reaches LEO with 10 t', async () => {
  const { simulate, MEDIUM_LIFTER } = await import(path.join(phys, 'headless.js'));
  const r = simulate(MEDIUM_LIFTER, { payload: 10000, targetAlt: 200e3 });
  assert.equal(r.outcome, 'orbit', JSON.stringify(r.orbit));
  assert.ok(r.orbit.periAlt > 150e3 && r.orbit.apoAlt < 400e3, `${r.orbit.periAlt} ${r.orbit.apoAlt}`);
  assert.ok(r.maxQ > 15e3 && r.maxQ < 60e3, `maxQ ${r.maxQ}`);
  assert.ok(r.finalMass > 10000, 'payload survives');
  assert.ok(r.events.some((e) => e.type === 'separation'));
});

test('an underpowered vehicle does not reach orbit', async () => {
  const { simulate, MEDIUM_LIFTER } = await import(path.join(phys, 'headless.js'));
  const r = simulate(MEDIUM_LIFTER, { payload: 60000 });
  assert.notEqual(r.outcome, 'orbit');
});
