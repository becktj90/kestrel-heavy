const assert = require('node:assert/strict');
const fs = require('node:fs');
const { test } = require('node:test');
const path = require('node:path');

const arcade = path.join(__dirname, '..', 'web/js');

test('touchdown grades follow the margin formula in data/landing.json', async () => {
  const { LANDING, gradeTouchdown, landingMargins } = await import(path.join(arcade, 'landing-tune.js'));
  const { DIFFICULTY } = await import(path.join(arcade, 'config.js'));
  const json = JSON.parse(fs.readFileSync(path.join(__dirname, '..', 'data', 'landing.json'), 'utf8'));
  assert.deepEqual(LANDING, json);
  assert.equal(DIFFICULTY.PAD_RAT.landingVy, 4);
  assert.equal(json.padRatLandingVy, 4);

  const lim = { landingVy: 6, landingTol: 40 };
  const margins = landingMargins(3, 20, lim.landingVy, lim.landingTol);
  assert.ok(Math.abs(margins.vyMargin - 0.5) < 1e-9);
  assert.ok(Math.abs(margins.dxMargin - 0.5) < 1e-9);
  assert.equal(gradeTouchdown({ kind: 'water', vy: 1, dx: 0, ...lim, tipped: false }), null);
  assert.equal(gradeTouchdown({ kind: 'deck', vy: 3, dx: 20, ...lim, tipped: false }), 'Perfect');
  assert.equal(gradeTouchdown({ kind: 'deck', vy: 4.2, dx: 28, ...lim, tipped: false }), 'Good');
  assert.equal(gradeTouchdown({ kind: 'deck', vy: 5.7, dx: 38, ...lim, tipped: false }), 'Hard');
  assert.equal(gradeTouchdown({ kind: 'deck', vy: 6.5, dx: 4, ...lim, tipped: false }), 'Salvage');
  assert.equal(gradeTouchdown({ kind: 'deck', vy: 1, dx: 2, ...lim, tipped: true }), 'Salvage');
});

test('result copy names touchdown speed and offset, and the splash callout is not a no-op', () => {
  const mission = fs.readFileSync(path.join(arcade, 'mission.js'), 'utf8');
  assert.match(mission, /Touchdown  \$\{touchLabel\}  ·  speed \$\{td\.vy\.toFixed\(1\)\}  ·  offset \$\{Math\.round\(td\.dx\)\}/);
  assert.match(mission, /this\.callout\(splashId,/);
  assert.doesNotMatch(mission, /splashId === 'fuelHaven' \? 'splash' : 'splash'/);
});
