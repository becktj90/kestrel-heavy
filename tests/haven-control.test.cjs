const assert = require('node:assert/strict');
const fs = require('node:fs');
const { test } = require('node:test');
const path = require('node:path');

const arcade = path.join(__dirname, '..', 'web/js');

test('Haven hold levels a soft catch and does not climb when sink is already in limits', async () => {
  const {
    HAVEN_TIP_DEG,
    havenBurnLift,
    havenSettleVy,
    havenWantAngle,
  } = await import(path.join(arcade, 'haven-control.js'));

  assert.ok(havenSettleVy(6.2, 400) > havenSettleVy(6.2, 100));
  assert.equal(havenBurnLift(1.2, 400, 6.2), 0, 'already-soft sink gets no upward force');
  assert.equal(havenBurnLift(5, 400, 6.2), -0.032);

  const noseDown = havenWantAngle({
    phase: 'glide', side: 1, vx: -1, axis: 0, alt: 40, burnLit: false, firstFlight: false,
  });
  assert.ok(Math.abs(noseDown) > HAVEN_TIP_DEG, 'unguided glide still kisses the deck at an angle');

  const first = havenWantAngle({
    phase: 'glide', side: 1, vx: -1, axis: 0, alt: 40, burnLit: false, firstFlight: true,
  });
  assert.ok(Math.abs(first) < HAVEN_TIP_DEG, 'KH-1 first flight stands up before contact');

  const held = havenWantAngle({
    phase: 'straighten', side: 1, vx: -1, axis: 0, alt: 40, burnLit: true, firstFlight: false,
  });
  assert.ok(Math.abs(held) < HAVEN_TIP_DEG, 'HOLD TO BURN stands the booster up');

  const kid = havenWantAngle({
    phase: 'glide', side: 1, vx: -1, axis: 0, alt: 40, burnLit: false, firstFlight: false, kid: true,
  });
  assert.ok(Math.abs(kid) < HAVEN_TIP_DEG, 'KID stands up before contact even without a lit burn');

  const mission = fs.readFileSync(path.join(arcade, 'mission.js'), 'utf8');
  assert.match(mission, /havenWantAngle\(/);
  assert.match(mission, /havenBurnLift\(/);
  assert.match(mission, /isFirstFlightGrace\(this\.currentFlight\(\)\.id/);
  assert.match(mission, /skipLandingEpilogue\(/);
  assert.match(mission, /reachedHaven = true/);
});
