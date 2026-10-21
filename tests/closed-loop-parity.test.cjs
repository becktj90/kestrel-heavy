// #341: the closed-loop guidance computer in the JS reference sim (web/js/physics/headless.js) and its data.
// The parity vectors live in tests/fixtures/closed-loop-parity.json (not data/golden); C# ClosedLoopParityTests flies
// the same inputs at the simulate() tolerance.
const assert = require('node:assert');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

const root = path.join(__dirname, '..');
const fixture = path.join(root, 'tests', 'fixtures', 'closed-loop-parity.json');
const readJSON = (...p) => JSON.parse(fs.readFileSync(path.join(root, ...p), 'utf8'));

test('closed-loop parity vectors are current (node tools/export-closed-loop-parity.mjs)', async () => {
  const { parityFile } = await import(path.join(root, 'tools', 'export-closed-loop-parity.mjs'));
  const now = JSON.parse(JSON.stringify(parityFile()));
  assert.deepStrictEqual(now, JSON.parse(fs.readFileSync(fixture, 'utf8')));
});

test('closed loop is opt-in: without closedLoop (or without the energy cutoff) the run is byte-identical', async () => {
  const { simulate, MEDIUM_LIFTER } = await import(path.join(root, 'web', 'js', 'physics', 'headless.js'));
  const base = { payload: 10000, targetAlt: 200e3, sampleEvery: 20 };
  const plain = simulate(MEDIUM_LIFTER, base);
  assert.deepStrictEqual(simulate(MEDIUM_LIFTER, { ...base, closedLoop: false }), plain);
  // The law needs the energy cutoff (its t_go and aim); asked for without it, nothing changes.
  assert.deepStrictEqual(simulate(MEDIUM_LIFTER, { ...base, closedLoop: true }), plain);
  // Every simulate golden still replays exactly (no new option set).
  for (const c of readJSON('data', 'golden', 'simulate.json').cases) {
    const r = simulate(c.in.stages, c.in.opts); delete r.warnings;
    assert.strictEqual(r.outcome, c.out.outcome, c.name);
    assert.strictEqual(r.time, c.out.time, c.name);
    assert.deepStrictEqual(r.events, c.out.events, c.name);
  }
});

test('closed loop zeroes the flight-path angle at cutoff and lands on the aim radius', () => {
  const f = JSON.parse(fs.readFileSync(fixture, 'utf8'));
  const by = Object.fromEntries(f.cases.map((c) => [c.name, c]));
  for (const name of ['medium-lifter-200-closed', 'medium-lifter-400-closed', 'medium-lifter-ellipse-200x900-closed', 'medium-lifter-200-closed-dt005']) {
    const o = by[name].out;
    assert.strictEqual(o.outcome, 'orbit', name);
    assert.ok(o.events.some((e) => e.type === 'cutoff'), name + ' clean cutoff');
    assert.ok(Math.abs(o.cutoffGammaDeg) < 0.05, `${name} |γ| ${o.cutoffGammaDeg}`);
    const aim = by[name].in.opts.closedLoopAimAlt ?? by[name].in.opts.targetAlt;
    assert.ok(Math.abs(o.orbit.periAlt - aim) < 2e3, `${name} perigee ${o.orbit.periAlt} vs aim ${aim}`);
  }
  // The basic law on the same energy cutoff leaves the tilt the guidance computer removes.
  assert.ok(Math.abs(by['medium-lifter-200-basic-energy'].out.cutoffGammaDeg) > 0.05);
});

test('guidance computer data: label, mass, price, flag and unlock', async () => {
  const g = readJSON('data', 'guidance.json');
  assert.strictEqual(g.basic.label, 'Pre-set pitch program, no correction');
  assert.strictEqual(g.closedLoop.inControlNote, 'Guidance in control');
  assert.ok(g.closedLoop.massKg >= 10 && g.closedLoop.massKg < 100, 'tens of kg');
  assert.strictEqual(readJSON('data', 'credits.json').parts.guidanceComputer, g.closedLoop.credits);
  assert.strictEqual(readJSON('data', 'features.json').flags[g.closedLoop.featureFlag], false, 'off in the Starter designer');
  assert.ok(g.closedLoop.unlockAfterMission >= 8, 'a later campaign unlock');
  const { GUIDANCE_COMPUTER_MASS, stageMasses } = await import(path.join(root, 'web', 'js', 'vehicle', 'mass.js'));
  assert.strictEqual(GUIDANCE_COMPUTER_MASS, g.closedLoop.massKg);
  const { PRESETS } = await import(path.join(root, 'web', 'js', 'data', 'presets.js'));
  for (const d of PRESETS) {
    const plain = stageMasses(d);
    const fitted = stageMasses({ ...d, avionics: { ...(d.avionics || {}), guidanceComputer: true } });
    const top = d.stages.length - 1;
    for (let i = 0; i < d.stages.length; i++) {
      const extra = i === top ? GUIDANCE_COMPUTER_MASS * (i === 0 && d.landing?.legs ? 1.06 : 1) : 0;
      assert.ok(Math.abs(fitted[i].dry - plain[i].dry - extra) < 1e-9, `${d.id} stage ${i}`);
    }
  }
});
