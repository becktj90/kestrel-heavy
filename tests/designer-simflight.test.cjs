// Design Lab M3 slice: the headless sim flies the designed vehicle.
// - Fairing jettison drops exactly the fairing mass at the design altitude (vacuum check,
//   where the mass history is exact), and is a no-op when the options are absent.
// - simReadout wires the design's fairing through, so presets fly with the jettison event.
// - flown-vs-readout ideal Δv matches the readout within the spec's 10% for every preset.
// - LAUNCH gating follows engineering validation (hard blocks ground the vehicle).
const assert = require('node:assert');
const path = require('node:path');
const test = require('node:test');
const js = path.join(__dirname, '..', 'web', 'js');
const load = async () => ({
  ...(await import(path.join(js, 'physics/headless.js'))),
  ...(await import(path.join(js, 'designer/readout.js'))),
  ...(await import(path.join(js, 'designer/validate.js'))),
  ...(await import(path.join(js, 'designer/simflight.js'))),
  ...(await import(path.join(js, 'data/presets.js'))),
});

const STAGES = [
  { thrustSL: 7.6e6, thrustVac: 8.23e6, ispSL: 282, ispVac: 311, propMass: 411000, dryMass: 25600, diameter: 3.7, cd: 0.3 },
  { thrustSL: 0, thrustVac: 981000, ispSL: 200, ispVac: 348, propMass: 107500, dryMass: 4500, diameter: 3.7, cd: 0.3 },
];

test('fairing jettison drops exactly the fairing mass (vacuum, exact mass history)', async () => {
  const { simulate } = await load();
  const base = { stages: STAGES, payload: 1500 + 5000, targetAlt: 400e3, vacuum: true, dt: 0.05, sampleEvery: 1, tMax: 2500 };
  const noFair = simulate(STAGES, { ...base, payload: 1500 + 5000 });
  const withFair = simulate(STAGES, { ...base, fairingMass: 5000, fairingJettisonAltitudeM: 100 });
  const ev = withFair.events.find((e) => e.type === 'fairing-jettison');
  assert.ok(ev, 'fairing-jettison event recorded');
  assert.strictEqual(ev.mass, 5000);
  assert.ok(ev.alt >= 100, `jettison at ${ev.alt} m, not below 100 m`);
  assert.ok(!noFair.events.some((e) => e.type === 'fairing-jettison'), 'no event without the options');
  // After the event, both flights burn the same propellant on the same schedule, so the
  // mass histories differ by exactly the dropped fairing mass while thrusting.
  for (const s of withFair.samples) {
    if (s.t <= ev.t) continue;
    const ref = noFair.samples.find((x) => x.t === s.t);
    if (ref && ref.stage === s.stage) assert.ok(Math.abs((ref.m - s.m) - 5000) < 1, `t=${s.t}: mass gap ${ref.m - s.m}`);
  }
});

test('simReadout flies presets with the fairing event and unchanged outcomes', async () => {
  const { PRESETS, quickReadout, simReadout } = await load();
  const expected = { stock: 'suborbital', scout: 'orbit', kid: 'orbit' };
  for (const p of PRESETS.filter((x) => expected[x.id])) {
    const q = quickReadout(p);
    const s = simReadout(p, q);
    assert.ok(s.events.some((e) => e.type === 'fairing-jettison'), `${p.id}: fairing event`);
    assert.strictEqual(s.outcome, expected[p.id], `${p.id}: outcome unchanged by the jettison wiring`);
    assert.ok(s.apogeeKm > 0, `${p.id}: apogee`);
  }
});

test('flown ideal Δv matches the readout within 10% for every preset', async () => {
  const { PRESETS, quickReadout, simReadout, flownVsReadout } = await load();
  for (const p of PRESETS) {
    const cmp = flownVsReadout(quickReadout(p), simReadout(p));
    assert.ok(cmp.idealMatchesReadout, `${p.id}: ideal/readout diff ${cmp.idealDiffPct.toFixed(3)}%`);
  }
});

test('flight log names the fairing event; frameAt replays samples', async () => {
  const { PRESETS, quickReadout, simReadout, flightLog, frameAt } = await load();
  const stock = PRESETS.find((p) => p.id === 'stock');
  const s = simReadout(stock, quickReadout(stock));
  const log = flightLog(s);
  assert.ok(log.some((e) => e.type === 'fairing-jettison' && /Fairing jettison/.test(e.text)), 'log names fairing');
  assert.ok(log[0].t <= log.at(-1).t, 'log in time order');
  const f = frameAt(s.samples, s.samples[2].t);
  assert.strictEqual(f.t, s.samples[2].t);
  assert.strictEqual(frameAt(s.samples, -1).t, s.samples[0].t);
  assert.strictEqual(frameAt([], 5), null);
});

test('LAUNCH gating follows validation: blocked designs cannot launch', async () => {
  const { PRESETS, validateEngineering, canLaunch } = await load();
  const stock = PRESETS.find((p) => p.id === 'stock');
  assert.ok(canLaunch(validateEngineering(stock)), 'stock can launch');
  const broken = JSON.parse(JSON.stringify(stock));
  broken.stages[0].engineCount = 0;
  assert.ok(!canLaunch(validateEngineering(broken)), 'zero engines cannot launch');
  const low = JSON.parse(JSON.stringify(stock));
  low.stages[0].propMass = 3000000;
  const v = validateEngineering(low);
  if (!v.ok) assert.ok(!canLaunch(v), 'TWR<=1 cannot launch');
});
