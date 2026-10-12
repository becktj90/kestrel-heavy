const assert = require('node:assert/strict');
const fs = require('node:fs');
const { test } = require('node:test');
const path = require('node:path');

const arcade = path.join(__dirname, '..', 'web/js');

test('mission ramp and star rules match data/mission-ramp.json', async () => {
  const { MISSION_RAMP, starsForTouchdown, unlocksNextMission } = await import(path.join(arcade, 'mission-ramp.js'));
  const { MISSIONS, getMission } = await import(path.join(arcade, 'missions.js'));
  const { HAVEN } = await import(path.join(arcade, 'config.js'));
  const json = JSON.parse(fs.readFileSync(path.join(__dirname, '..', 'data', 'mission-ramp.json'), 'utf8'));
  assert.deepEqual(MISSION_RAMP, json);

  const winds = MISSIONS.map((m) => m.wind);
  const fuels = MISSIONS.map((m) => m.landingFuel);
  const swells = MISSIONS.map((m) => m.swellAmp);
  assert.deepEqual(winds, [0.55, 0.75, 1, 1.2, 1.45]);
  assert.deepEqual(fuels, [52, 44, 36, 30, 24]);
  assert.deepEqual(swells, [6, 10, 16, 22, 28]);
  assert.ok(winds.every((n, i) => i === 0 || n > winds[i - 1]));
  assert.ok(fuels.every((n, i) => i === 0 || n < fuels[i - 1]));
  assert.equal(getMission('KH-3').landingFuel, HAVEN.landingFuel);
  assert.equal(getMission('KH-3').swellAmp, HAVEN.swellAmp);

  assert.equal(unlocksNextMission('recovered'), true);
  assert.equal(unlocksNextMission('salvage'), true);
  assert.equal(unlocksNextMission('splash'), false);
  assert.equal(unlocksNextMission('rud'), false);

  const lim = { landingVy: 6, landingTol: 40, kind: 'deck', recovered: true, tipped: false };
  assert.equal(starsForTouchdown({ reason: 'splash', ...lim }), 0);
  assert.equal(starsForTouchdown({ reason: 'rud', kind: 'rud' }), 0);
  assert.equal(starsForTouchdown({ reason: 'recovered', vy: 2, dx: 8, ...lim }), 3);
  assert.equal(starsForTouchdown({ reason: 'recovered', vy: 4.2, dx: 28, ...lim }), 2);
  assert.equal(starsForTouchdown({ reason: 'recovered', vy: 5.7, dx: 38, ...lim }), 1);
  assert.equal(starsForTouchdown({ reason: 'salvage', vy: 9, dx: 80, ...lim, recovered: false }), 1);
  assert.equal(starsForTouchdown({ reason: 'recovered', vy: 1, dx: 1, ...lim, tipped: true }), 1);
});

test('splash does not unlock, stars do not gate, and the KH-2 tip spoiler is gone', () => {
  const mission = fs.readFileSync(path.join(arcade, 'mission.js'), 'utf8');
  const tips = fs.readFileSync(path.join(arcade, 'config.js'), 'utf8');
  assert.match(mission, /unlocksNextMission\(reason\)/);
  assert.doesNotMatch(mission, /reason === 'rud' \? null : nextMissionId/);
  assert.doesNotMatch(tips, /KH-2 landed the booster on Haven/);
});

test('hits scale with max HP and open a short invulnerability window', async () => {
  const { applyHit, hazardDamage, healthMaxFor, HIT_INVULN_SEC } = await import(path.join(arcade, 'health.js'));
  const { HEALTH } = await import(path.join(arcade, 'health-tune.js'));
  const json = JSON.parse(fs.readFileSync(path.join(__dirname, '..', 'data', 'health.json'), 'utf8'));
  assert.deepEqual(HEALTH, json);
  assert.equal(HIT_INVULN_SEC, 0.4);
  assert.equal(hazardDamage('bird', 'CADET'), 28);
  const scaled = hazardDamage('debris', 'PAD_RAT');
  const expected = Math.round(42 * (healthMaxFor('PAD_RAT') / 100) * 1.15);
  assert.equal(scaled, expected);
  assert.ok(scaled < Math.round(42 * 1.15), 'PAD RAT damage is a fraction of max HP, not a second penalty');

  const session = { health: 100, shield: 0, grace: 0 };
  const first = applyHit(session, 'bird', 'CADET');
  assert.equal(first.kind, 'graze');
  assert.equal(session.invuln, HIT_INVULN_SEC);
  session.health = first.health;
  const second = applyHit(session, 'debris', 'CADET');
  assert.equal(second.kind, 'invuln');
  assert.equal(second.damage, 0);
  assert.equal(second.health, first.health);
});
