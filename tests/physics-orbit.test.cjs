// M1 physics: vacuum two-body orbit propagation with RK4.
const assert = require('node:assert');
const path = require('node:path');
const test = require('node:test');
const phys = path.join(__dirname, '..', 'web', 'js', 'physics');

async function propagate(periods, ecc) {
  const { createRK4 } = await import(path.join(phys, 'integrator.js'));
  const { MU_EARTH, R_EARTH } = await import(path.join(phys, 'gravity.js'));
  const { orbit2D } = await import(path.join(phys, 'orbit.js'));
  const r0 = R_EARTH + 200e3, v0 = Math.sqrt(MU_EARTH * (1 + ecc) / r0);
  const y = new Float64Array([r0, 0, 0, v0]);
  const f = (t, s, o) => { const r2 = s[0] * s[0] + s[1] * s[1], k = -MU_EARTH / (r2 * Math.sqrt(r2)); o[0] = s[2]; o[1] = s[3]; o[2] = k * s[0]; o[3] = k * s[1]; };
  const step = createRK4(4);
  const o0 = orbit2D(y[0], y[1], y[2], y[3]);
  const dt = 1, n = Math.round(periods * o0.period / dt);
  let maxDE = 0;
  for (let i = 0; i < n; i++) {
    step(f, i * dt, y, dt);
    const E = (y[2] * y[2] + y[3] * y[3]) / 2 - MU_EARTH / Math.hypot(y[0], y[1]);
    maxDE = Math.max(maxDE, Math.abs((E - o0.energy) / o0.energy));
  }
  return { o0, o1: orbit2D(y[0], y[1], y[2], y[3]), maxDE };
}

test('energy is conserved in a vacuum orbit (RK4, dt=1 s)', async () => {
  const { maxDE } = await propagate(3, 0.1);
  assert.ok(maxDE < 1e-8, `relative energy drift ${maxDE}`);
});

test('circular orbit stays circular after 10 periods', async () => {
  const { o0, o1, maxDE } = await propagate(10, 0);
  assert.ok(o1.e < 1e-6, `e=${o1.e}`);
  assert.ok(Math.abs(o1.a - o0.a) < 1, `a drift ${o1.a - o0.a} m`);
  assert.ok(Math.abs(o1.periAlt - 200e3) < 10 && Math.abs(o1.apoAlt - 200e3) < 10);
  assert.ok(maxDE < 1e-8);
});
