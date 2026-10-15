const assert = require('node:assert/strict');
const { test } = require('node:test');
const path = require('node:path');

const root = path.join(__dirname, '..', 'web/js/feel');

test('stick is rate limited, and a small release is quicker than the attack', async () => {
  const { stepStick, MAX_RATE, shapeFlightAxis } = await import(path.join(root, 'stick.js'));
  const dt = 1 / 60;
  assert.equal(stepStick(0, 1, dt), MAX_RATE * dt);
  const attack = stepStick(0, 0.1, dt);
  const release = Math.abs(stepStick(0.1, 0, dt));
  assert.ok(release > attack);
  const toward = stepStick(0.4, Number.NaN, dt);
  assert.ok(toward < 0.4 && toward > 0);
  assert.equal(stepStick(0.4, 1, 0), 0.4);
  let axis = 0;
  for (let i = 0; i < 180; i++) axis = stepStick(axis, 2, dt);
  assert.ok(Math.abs(axis - 1) < 1e-6);

  assert.deepEqual(shapeFlightAxis(-1, 0.4, dt, false, false), { axis: -1, felt: -1, coast: false });
  const analog = shapeFlightAxis(1, 0, dt, true, false);
  assert.equal(analog.axis, MAX_RATE * dt);
  assert.equal(analog.coast, true);
  let felt = 0.5;
  let coast = true;
  for (let i = 0; i < 90; i++) {
    const s = shapeFlightAxis(0, felt, dt, false, coast);
    felt = s.felt;
    coast = s.coast;
  }
  assert.equal(coast, false);
  assert.equal(felt, 0);
});

test('camera lead, fov and shake match the flight rig', async () => {
  const cam = await import(path.join(root, 'camera.js'));
  assert.deepEqual(cam.lead(0, 0, 70), { x: 0, y: 0 });
  const reach = 70 * cam.LEAD_FRACTION;
  const scale = (reach * (1 - Math.exp(-1))) / 180;
  const ahead = cam.lead(180, 0, 70);
  assert.ok(Math.abs(ahead.x - 180 * scale) < 1e-9);
  assert.equal(ahead.y, 0);
  assert.equal(cam.fovScale(0, 0, 0), 1);
  const wide = cam.fovScale(3, 1, 0);
  assert.ok(wide > 1 && wide < 1.12);
  assert.ok(cam.fovScale(3, 1, 1) < wide);
  const shake = cam.freshShake();
  cam.kick(shake, 16, 0.4);
  let peak = 0;
  for (let i = 0; i < 12; i++) {
    cam.stepShake(shake, 1 / 60);
    peak = Math.max(peak, Math.abs(shake.x) + Math.abs(shake.y));
  }
  assert.ok(peak > 0.05);
  for (let i = 0; i < 180; i++) cam.stepShake(shake, 1 / 60);
  assert.ok(Math.abs(shake.x) + Math.abs(shake.y) < 1e-3);
  let pitch = cam.stepPitch(90, 60, 4.2, 1 / 60);
  assert.ok(pitch < 90 && pitch > 60);
});

test('risk meter names Max-Q on the arcade sample and stays quiet early', async () => {
  const { computeTelemetry } = await import(path.join(__dirname, '..', 'web/js/telemetry.js'));
  const { evaluateRisk, DISPLAY_Q_LIM_PA, flightCue, airframeBand } = await import(path.join(root, 'feedback.js'));
  const sample = (tClock, altitudeKm, fuel) => computeTelemetry({
    status: 'ASCENT', tClock, altitudeKm, throttle: 0.9, fuel, angle: 2, velocity: 0, dt: 1 / 60,
  });
  const early = sample(5, 6, 90);
  const maxq = sample(21.6, 21.6 * 1.2, 60);
  const quiet = evaluateRisk({
    health01: 1, throttle01: 0.9, fuel01: 0.9, qPa: early.qKpa * 1000, qLimPa: DISPLAY_Q_LIM_PA, dvMargin: 0.1,
  });
  const hot = evaluateRisk({
    health01: 1, throttle01: 0.9, fuel01: 0.6, qPa: maxq.qKpa * 1000, qLimPa: DISPLAY_Q_LIM_PA, dvMargin: -0.2,
  });
  assert.equal(quiet.call, 'NOMINAL');
  assert.equal(hot.call, 'MAX-Q');
  assert.ok(hot.qRisk >= 0.85);
  const hurt = evaluateRisk({ health01: 0.2, throttle01: 0.9, fuel01: 0.6, qPa: 1000, qLimPa: DISPLAY_Q_LIM_PA, dvMargin: 0 });
  assert.equal(hurt.call, 'AIRFRAME');
  assert.equal(hurt.band, 'critical');
  assert.equal(airframeBand(0), 'lost');
  assert.equal(flightCue('breakup', true).shake, 0);
  assert.equal(flightCue('breakup', true).hapticMs, 0);
  assert.equal(flightCue('hit', false).hard, true);
});

test('spatial grid skips far bodies and debris from one seed stays apart', async () => {
  const { createGrid } = await import(path.join(root, 'spatial.js'));
  const { hash01, createDebris } = await import(path.join(root, 'debris.js'));
  const { seek, arrive, avoidWalls, blend } = await import(path.join(root, 'steering.js'));
  const grid = createGrid(10);
  grid.insert(1, 0, 0);
  grid.insert(2, 8, -6);
  grid.insert(3, -3, 4);
  grid.insert(9, 500, 500);
  const hits = [];
  grid.query(0, 0, 12, hits);
  assert.ok(hits.includes(1) && hits.includes(2) && hits.includes(3));
  assert.ok(!hits.includes(9));

  assert.ok(hash01(1) > 0 && hash01(1) < 1);
  assert.equal(hash01(1), hash01(1));
  assert.notEqual(hash01(1), hash01(2));
  const a = createDebris(40, 2.5);
  const b = createDebris(40, 2.5);
  assert.equal(a.burst(0, 100, 200, 40, 28, 180, 0.04, 0x5eed), 28);
  b.burst(0, 100, 200, 40, 28, 180, 0.04, 0x5eed);
  assert.equal(a.shards[0].vx, b.shards[0].vx);
  assert.equal(a.shards[3].y, b.shards[3].y);

  a.shards[0].x = 0; a.shards[0].y = 0; a.shards[0].vx = 0; a.shards[0].vy = 0;
  a.shards[0].radius = 1; a.shards[0].mass = 1; a.shards[0].alive = true; a.shards[0].life = 10;
  a.shards[1].x = 0.4; a.shards[1].y = 0; a.shards[1].vx = 0; a.shards[1].vy = 0;
  a.shards[1].radius = 1; a.shards[1].mass = 1; a.shards[1].alive = true; a.shards[1].life = 10;
  a.step(1 / 60, 0, 0);
  const dx = a.shards[1].x - a.shards[0].x;
  const dy = a.shards[1].y - a.shards[0].y;
  assert.ok(dx * dx + dy * dy > 0.16);

  const cloud = createDebris(40, 2.5);
  cloud.burst(0, 0, 0, 0, 20, 180, 0, 7);
  for (let i = 0; i < 90; i++) cloud.step(1 / 60, 9.81, 0.35);
  let span = 0;
  let overlaps = 0;
  for (let i = 0; i < cloud.shards.length; i++) {
    if (!cloud.shards[i].alive) continue;
    span = Math.max(span, Math.abs(cloud.shards[i].x) + Math.abs(cloud.shards[i].y));
    for (let j = i + 1; j < cloud.shards.length; j++) {
      if (!cloud.shards[j].alive) continue;
      const ex = cloud.shards[j].x - cloud.shards[i].x;
      const ey = cloud.shards[j].y - cloud.shards[i].y;
      const min = cloud.shards[i].radius + cloud.shards[j].radius;
      if (ex * ex + ey * ey < min * min * 0.98) overlaps += 1;
    }
  }
  assert.ok(span > 4);
  assert.equal(overlaps, 0);

  assert.equal(seek(0, 0, 0, 0, 10, 0, 5).ax, 5);
  assert.equal(arrive(0, 0, 0, 0, 2, 0, 8, 4).ax, 4);
  assert.ok(avoidWalls(-9, 10, 2) > 0);
  assert.ok(avoidWalls(9, 10, 2) < 0);
  assert.deepEqual(blend(4, 0, 0, 8, 1, 1), { ox: 2, oy: 4 });
});
