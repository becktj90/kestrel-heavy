// Landing gate: no lit burn -> medal never above bronze; gear not down -> not
// 'recovered' (Unity RecoveryScore: tipped = tilted || !legsDown). A lander that
// lit the burn and put the gear down is graded exactly as before.
const assert = require('node:assert/strict');
const test = require('node:test');
const fs = require('node:fs');
const path = require('node:path');

const web = path.join(__dirname, '..', 'web', 'js');
const lim = { landingVy: 7.1, landingTol: 78 };
const gold = { kind: 'deck', vy: 1.2, dx: 20, tilt: 2, recovered: true, ...lim };
const silver = { kind: 'deck', vy: 1.2, dx: 25, tilt: 4.3, recovered: true, ...lim };

test('medal gate: never lit burn caps the medal at bronze', async () => {
  const { gradeLanding } = await import(path.join(web, 'medals.js'));
  assert.equal(gradeLanding(gold), 'gold', 'flags not modelled -> unchanged');
  assert.equal(gradeLanding({ ...gold, burnLit: true, legsDown: true }), 'gold', 'burn + gear lander is unaffected');
  assert.equal(gradeLanding({ ...silver, burnLit: true, legsDown: true }), 'silver');
  assert.equal(gradeLanding({ ...gold, burnLit: false }), 'bronze');
  assert.equal(gradeLanding({ ...silver, burnLit: false }), 'bronze');
  assert.equal(gradeLanding({ ...gold, burnLit: false, legsDown: false }), 'bronze');
  assert.equal(gradeLanding({ ...gold, burnLit: true, legsDown: false }), 'bronze', 'gear not down is capped too');
});

test('touchdown gate: gear not down is Salvage like a tipped landing', async () => {
  const { gradeTouchdown, landingTipped } = await import(path.join(web, 'landing-tune.js'));
  const good = { kind: 'deck', vy: 1.2, dx: 20, ...lim, tipped: false };
  assert.equal(gradeTouchdown(good), 'Perfect', 'legsDown defaults to true');
  assert.equal(gradeTouchdown({ ...good, legsDown: true }), 'Perfect', 'burn + gear lander unaffected');
  assert.equal(gradeTouchdown({ ...good, legsDown: false }), 'Salvage');
  assert.equal(gradeTouchdown({ ...good, tipped: true, legsDown: true }), 'Salvage');
  assert.equal(gradeTouchdown({ kind: 'water', vy: 1, dx: 0, ...lim, legsDown: false }), null);
  assert.equal(landingTipped(false, true), false);
  assert.equal(landingTipped(true, true), true);
  assert.equal(landingTipped(false, false), true);
});

test('resolveLanding feeds the gear and burn flags to both graders and to the recovered result', () => {
  const m = fs.readFileSync(path.join(web, 'mission.js'), 'utf8');
  assert.match(m, /const legsDown = Boolean\(this\.session\.gearDeployed\)/);
  assert.match(m, /const burnLit = Boolean\(this\.session\.burnLit\)/);
  assert.match(m, /gradeTouchdown\(\{[^}]*legsDown/s);
  assert.match(m, /recovered: kind === 'deck' && onPaint && soft && !tipped && legsDown/);
  assert.match(m, /if \(kind === 'deck' && onPaint && soft && !tipped && legsDown\) \{\s*this\.session\.recovered = true/);
});
