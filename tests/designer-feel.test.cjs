const assert = require('node:assert/strict');
const fs = require('node:fs');
const { test } = require('node:test');
const path = require('node:path');

const js = path.join(__dirname, '..', 'web', 'js');

test('designer live stats are the readout delta, and the flight card is the sim', async () => {
  const { DESIGNER_FEEL, previewDelta, flightResult, hapticPattern } = await import(path.join(js, 'designer/feel.js'));
  const { quickReadout, simReadout } = await import(path.join(js, 'designer/readout.js'));
  const { PRESET_BY_ID } = await import(path.join(js, 'data/presets.js'));
  const json = JSON.parse(fs.readFileSync(path.join(__dirname, '..', 'data', 'designer-feel.json'), 'utf8'));
  assert.deepEqual(DESIGNER_FEEL, json);
  assert.deepEqual(hapticPattern('snap'), [12]);
  assert.deepEqual(hapticPattern('payoff'), [18, 30, 18]);

  const base = quickReadout(PRESET_BY_ID.scout);
  const heavier = structuredClone(PRESET_BY_ID.scout);
  heavier.stages[0].propMass += 5000;
  const next = quickReadout(heavier);
  const delta = previewDelta(base, next);
  assert.ok(Math.abs(delta.deltaV - (next.totalDeltaV - base.totalDeltaV)) < 1e-6);
  assert.ok(Math.abs(delta.twr - (next.liftoffTWR - base.liftoffTWR)) < 1e-9);
  assert.ok(Math.abs(delta.mass - (next.liftoffMass - base.liftoffMass)) < 1e-6);
  assert.ok(delta.mass > 0, 'more propellant shows up as mass');

  const sim = simReadout(PRESET_BY_ID.scout, base);
  const card = flightResult(sim, base);
  assert.equal(card.outcome, sim.outcome);
  assert.equal(card.apogeeKm, sim.apogeeKm);
  assert.equal(card.totalLosses, sim.totalLosses);
  assert.equal(card.deltaVExpended, sim.deltaVExpended);
  assert.equal(card.idealDeltaV, base.totalDeltaV);
  assert.equal(card.outcome, 'orbit');
});

test('the designer wires snap, the flight card, and a vibrate hook', () => {
  const ui = fs.readFileSync(path.join(js, 'designer/ui.js'), 'utf8');
  const css = fs.readFileSync(path.join(__dirname, '..', 'web/css/designer.css'), 'utf8');
  assert.match(ui, /previewDelta\(/);
  assert.match(ui, /flightResult\(/);
  assert.match(ui, /pulseHaptic\(hapticPattern\('snap'\)\)/);
  assert.match(ui, /bestFlight/);
  assert.match(ui, /FLIGHT RESULT/);
  assert.match(css, /kd-snap/);
  assert.match(css, /kd-flight/);
});
