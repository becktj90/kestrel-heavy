// M2 designer: readout math agrees with physics/headless (stackDeltaV + simulate).
const assert = require('node:assert');
const path = require('node:path');
const test = require('node:test');
const js = path.join(__dirname, '..', 'web', 'js');
const load = async () => ({
  ...(await import(path.join(js, 'designer/readout.js'))),
  ...(await import(path.join(js, 'physics/headless.js'))),
  ...(await import(path.join(js, 'vehicle/stack.js'))),
  ...(await import(path.join(js, 'vehicle/mass.js'))),
  ...(await import(path.join(js, 'data/presets.js'))),
});
const G0 = 9.80665;

test('readout Δv equals stackDeltaV on the serial stage list, per stage and total', async () => {
  const { PRESETS, quickReadout, serialStages, stackDeltaV } = await load();
  for (const p of PRESETS) {
    const r = quickReadout(p);
    const { stages, payload } = serialStages(p);
    const ref = stackDeltaV(stages, payload);
    assert.ok(Math.abs(r.totalDeltaV - ref.total) < 1e-6, p.id);
    r.stages.forEach((s, i) => assert.ok(Math.abs(s.deltaV - ref.perStage[i]) < 1e-6, `${p.id} stage ${i}`));
  }
});

test('serial stages conserve mass and match toSimStages without strap-ons', async () => {
  const { PRESETS, serialStages, toSimStages, vehicleMass } = await load();
  for (const p of PRESETS) {
    const { stages, payload } = serialStages(p);
    const total = stages.reduce((a, s) => a + s.propMass + s.dryMass, 0) + payload;
    assert.ok(Math.abs(total / vehicleMass(p).liftoffMass - 1) < 1e-9, `${p.id} mass conserved`);
    if (!p.strapOns.count) {
      const sim = toSimStages(p);
      stages.forEach((s, i) => { assert.strictEqual(s.thrustVac, sim[i].thrustVac); assert.strictEqual(s.ispVac, sim[i].ispVac); });
    }
  }
});

test('TWR, burn time, mass flow and mass ratio are self-consistent', async () => {
  const { PRESETS, quickReadout } = await load();
  for (const p of PRESETS) {
    const r = quickReadout(p);
    assert.ok(Math.abs(r.liftoffTWR - r.serial[0].thrustSL / (r.liftoffMass * G0)) < 1e-9, p.id);
    r.stages.forEach((s, i) => {
      const st = r.serial[i];
      assert.ok(Math.abs(s.mdot - st.thrustVac / (st.ispVac * G0)) < 1e-9);
      assert.ok(Math.abs(s.burnTime * s.mdot - st.propMass) < 1e-6);
      assert.ok(Math.abs(s.deltaV - st.ispVac * G0 * Math.log(s.massRatio)) < 1e-6);
    });
    assert.ok(r.cost > 0 && r.reliability > 0.2 && r.reliability < 1, `${p.id} cost/reliability`);
  }
});

test('sim readout comes from simulate(): same outcome, losses and ideal Δv', async () => {
  const { PRESETS, quickReadout, simReadout, simulate } = await load();
  for (const p of PRESETS) {
    const q = quickReadout(p);
    const s = simReadout(p, q);
    const ref = simulate(q.serial, { payload: q.payload, targetAlt: q.targetAlt, latitude: 28.5, dt: 0.2, sampleEvery: 2, tMax: 2500 });
    assert.strictEqual(s.outcome, ref.outcome, p.id);
    assert.ok(Math.abs(s.totalLosses - (ref.losses.gravity + ref.losses.drag + ref.losses.steering)) < 1e-6);
    assert.ok(Math.abs(s.idealDeltaV - q.totalDeltaV) < 1e-6, `${p.id} sim ideal Δv == readout Δv`);
    assert.ok(s.totalLosses > 500 && s.totalLosses < 9000, `${p.id} losses ${s.totalLosses}`);
  }
});

test('archetype presets reach orbit in the headless sim; required Δv is in the real-world range', async () => {
  const { PRESET_BY_ID, quickReadout, simReadout, requiredDeltaV } = await load();
  for (const id of ['scout', 'kid', 'small-lift', 'medium-lift', 'super-heavy']) {
    assert.strictEqual(simReadout(PRESET_BY_ID[id]).outcome, 'orbit', id);
  }
  const leo = requiredDeltaV('leo200').required;
  assert.ok(leo > 8800 && leo < 9600, `LEO need ${leo}`);
  assert.ok(requiredDeltaV('gto').required > leo + 2000);
  void quickReadout;
});

test('engine tune scales thrust and Isp within ±15%', async () => {
  const { PRESET_BY_ID, quickReadout } = await load();
  const base = quickReadout(PRESET_BY_ID.scout);
  const d = JSON.parse(JSON.stringify(PRESET_BY_ID.scout));
  d.stages[0].tune = { thrustPct: 50, ispPct: 10 };
  const r = quickReadout(d);
  assert.ok(Math.abs(r.serial[0].thrustVac / base.serial[0].thrustVac - 1.15) < 1e-9, 'clamped to +15%');
  assert.ok(Math.abs(r.serial[0].ispVac / base.serial[0].ispVac - 1.10) < 1e-9);
});
