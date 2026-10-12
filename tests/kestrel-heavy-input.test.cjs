const assert = require('node:assert/strict');
const fs = require('node:fs');
const { test } = require('node:test');
const path = require('node:path');

const arcade = path.join(__dirname, '..', 'web/js');

test('touch steer dead zone, clamp, and release', async () => {
  const {
    createInput,
    setTouchSteer,
    peekTouchSteer,
    flightAxis,
    clearFlightHolds,
    TOUCH_STEER_DEAD_PX,
    TOUCH_STEER_FULL_PX,
  } = await import(path.join(arcade, 'input.js'));

  const calm = { sensitivity: 1, viewportWidth: 800 };
  assert.equal(peekTouchSteer(0, false, calm), 0);
  assert.equal(peekTouchSteer(TOUCH_STEER_DEAD_PX - 1, false, calm), 0);
  assert.ok(peekTouchSteer(TOUCH_STEER_DEAD_PX + 1, false, calm) > 0);
  assert.equal(peekTouchSteer(TOUCH_STEER_FULL_PX, true, calm), 1);
  assert.equal(peekTouchSteer(-TOUCH_STEER_FULL_PX - 40, true, calm), -1);
  const mid = TOUCH_STEER_FULL_PX / 2;
  const curved = peekTouchSteer(mid, true, calm);
  assert.ok(Math.abs(curved - (0.5 ** 1.6)) < 1e-9, 'expo curve is (dx/110)^1.6');
  assert.ok(curved < mid / TOUCH_STEER_FULL_PX, 'expo is gentler than linear near center');

  const input = createInput();
  assert.equal(setTouchSteer(input, 8), null);
  assert.equal(input.touchSteer, null);
  assert.equal(input.touchSteerArmed, false);

  const armed = setTouchSteer(input, 40);
  assert.ok(armed > 0 && armed < 1);
  assert.equal(input.touchSteerArmed, true);
  assert.equal(flightAxis(input, 640, 140), input.touchSteer);

  setTouchSteer(input, 8);
  assert.ok(input.touchSteer !== null, 'once armed, small drag still steers');

  setTouchSteer(input, null);
  assert.equal(input.touchSteer, null);
  assert.equal(input.touchSteerArmed, false);
  assert.equal(flightAxis(input, 640, 140), 0);

  input.left = true;
  assert.equal(flightAxis(input, 640, 140), -1);
  clearFlightHolds(input);
  assert.equal(input.thumbHeld, false);
  assert.equal(input.touchSteer, null);
  assert.equal(input.launchArmed, false);
});

test('Haven glide hold does not arm climb until the burn label', async () => {
  const { climbArmedFromLabel, isAscentCruise, paintThumbSteer } = await import(path.join(arcade, 'input.js'));
  assert.equal(climbArmedFromLabel('GLIDE'), false);
  assert.equal(climbArmedFromLabel('HOLD TO BURN'), true);
  assert.equal(climbArmedFromLabel('HOLD · DRAG'), false);
  assert.equal(climbArmedFromLabel('TAP TO LAUNCH'), false);
  assert.equal(climbArmedFromLabel('STEER'), false);
  assert.equal(climbArmedFromLabel('DRAG TO STEER'), false);
  assert.equal(isAscentCruise('ASCENT'), true);
  assert.equal(isAscentCruise('PRELAUNCH'), false);
  assert.equal(isAscentCruise('HAVEN'), false);

  const node = {
    dataset: {},
    style: { props: {}, setProperty(name, value) { this.props[name] = value; } },
    classList: { flags: {}, toggle(name, on) { this.flags[name] = Boolean(on); } },
  };
  paintThumbSteer(node, 0.8);
  assert.equal(node.dataset.steer, '0.80');
  assert.equal(node.style.props['--kh-steer'], '0.8');
  assert.equal(node.classList.flags['is-steer-right'], true);
  paintThumbSteer(node, 0);
  assert.equal(node.dataset.steer, '0.00');
  assert.equal(node.classList.flags['is-steer-right'], false);
});

test('steer expo, sensitivity, and compact full range match data/steer.json', async () => {
  const {
    applySteerCurve,
    peekTouchSteer,
    setSteerSensitivity,
    steerFullPx,
    TOUCH_STEER_EXPO,
    TOUCH_STEER_FULL_PX,
    TOUCH_STEER_FULL_PX_COMPACT,
  } = await import(path.join(arcade, 'input.js'));
  const { STEER } = await import(path.join(arcade, 'steer-tune.js'));
  const json = JSON.parse(fs.readFileSync(path.join(__dirname, '..', 'data', 'steer.json'), 'utf8'));
  assert.deepEqual(STEER, json);
  assert.equal(TOUCH_STEER_EXPO, 1.6);
  assert.equal(TOUCH_STEER_FULL_PX, 110);
  assert.equal(TOUCH_STEER_FULL_PX_COMPACT, json.fullPxCompact);
  assert.match(json.formula, /\^ expo/);

  setSteerSensitivity(1);
  assert.equal(steerFullPx(800), 110);
  assert.equal(steerFullPx(390), json.fullPxCompact);
  assert.equal(steerFullPx(375), json.fullPxCompact);
  assert.equal(applySteerCurve(json.fullPxCompact, { viewportWidth: 375, sensitivity: 1 }), 1);
  assert.ok(applySteerCurve(40, { viewportWidth: 375, sensitivity: 1 }) > applySteerCurve(40, { viewportWidth: 800, sensitivity: 1 }));

  const sens = json.sensitivity.max;
  const span = 110 / sens;
  assert.equal(peekTouchSteer(span, true, { sensitivity: sens, viewportWidth: 800 }), 1);
  assert.ok(peekTouchSteer(span * 0.5, true, { sensitivity: sens, viewportWidth: 800 }) < 1);
  assert.equal(setSteerSensitivity(9), json.sensitivity.max);
  assert.equal(setSteerSensitivity(0), json.sensitivity.min);
  setSteerSensitivity(1);
});
