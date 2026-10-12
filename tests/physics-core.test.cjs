// M1 physics core: ISA, rocket equation, orbit conversion, integrator.
const assert = require('node:assert');
const path = require('node:path');
const test = require('node:test');
const phys = path.join(__dirname, '..', 'web', 'js', 'physics');
const near = (a, b, tol, msg) => assert.ok(Math.abs(a - b) <= tol, `${msg}: ${a} vs ${b} (±${tol})`);

test('ISA 1976 table values', async () => {
  const { atmosphere } = await import(path.join(phys, 'atmosphere.js'));
  // Layer-base values from the US Std Atm 1976 tables, keyed by geopotential height h (m).
  // [h, T K, p Pa, rho kg/m3]; converted to geometric z = R·h/(R−h) before lookup.
  const R = 6356766;
  const rows = [[0, 288.15, 101325, 1.2250], [11000, 216.65, 22632, 0.36392], [20000, 216.65, 5474.9, 0.088035],
    [32000, 228.65, 868.02, 0.013225], [47000, 270.65, 110.91, 0.0014275], [51000, 270.65, 66.939, 8.6160e-4],
    [71000, 214.65, 3.9564, 6.4211e-5], [84852, 186.87, 0.37338, 6.958e-6]].map(([h, ...r]) => [R * h / (R - h), ...r]);
  for (const [z, T, p, rho] of rows) {
    const a = atmosphere(z);
    near(a.T, T, 0.2, `T@${z}`); near(a.p / p, 1, 0.003, `p@${z}`); near(a.rho / rho, 1, 0.004, `rho@${z}`);
  }
  near(atmosphere(0).a, 340.29, 0.05, 'sound speed SL');
  const a100 = atmosphere(100000), a86 = atmosphere(86000);
  assert.ok(a100.p < a86.p && a100.p > 0, 'exponential tail decreasing');
});

test('Cd(Mach) has a transonic spike and is continuous', async () => {
  const { cdMach } = await import(path.join(phys, 'aero.js'));
  assert.equal(cdMach(0.5, 0.3), 0.3);
  assert.ok(cdMach(1.1, 0.3) > cdMach(0.8, 0.3) && cdMach(1.1, 0.3) > cdMach(3, 0.3));
  near(cdMach(1.1 - 1e-9, 0.3), cdMach(1.1 + 1e-9, 0.3), 1e-6, 'continuous at peak');
});

test('rocket equation helpers', async () => {
  const { deltaV, massFlow, ispAt, G0 } = await import(path.join(phys, 'rocket-eq.js'));
  near(deltaV(300, Math.E, 1), 300 * G0, 1e-9, 'dv');
  near(massFlow(1e6, 300), 1e6 / (300 * 9.80665), 1e-9, 'mdot');
  assert.equal(ispAt(280, 310, 101325), 280); assert.equal(ispAt(280, 310, 0), 310);
});

test('circular velocity at 200 km', async () => {
  const { circularVelocity, orbit2D } = await import(path.join(phys, 'orbit.js'));
  near(circularVelocity(200e3), 7784.3, 0.5, 'v_circ');
  const o = orbit2D(6578137, 0, 0, circularVelocity(200e3));
  near(o.e, 0, 1e-12, 'e'); near(o.periAlt, 200e3, 1e-3, 'pe'); near(o.apoAlt, 200e3, 1e-3, 'ap');
});

test('earth rotation site velocity', async () => {
  const { siteRotationSpeed } = await import(path.join(phys, 'earth.js'));
  near(siteRotationSpeed(0), 465.1, 0.1, 'equator'); near(siteRotationSpeed(28.5), 408.6, 0.3, 'KSC-latitude');
});

test('fixed stepper runs whole steps independent of frame rate', async () => {
  const { createFixedStepper } = await import(path.join(phys, 'integrator.js'));
  let n = 0; const s = createFixedStepper(() => n++, { dt: 0.01 });
  for (let i = 0; i < 100; i++) s.advance(1 / 60);
  assert.equal(n, Math.floor((100 / 60) / 0.01 + 1e-9));
  assert.ok(s.alpha >= 0 && s.alpha < 1);
});
