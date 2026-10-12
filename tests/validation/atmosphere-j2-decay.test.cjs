// Validation (S2/S3): web/js/physics/atmosphere.js vs U.S. Standard Atmosphere 1976 tables,
// plus J2 precession and orbital decay status. Run: node --test tests/validation/
//
// Reference: U.S. Standard Atmosphere, 1976 (NOAA-S/T 76-1562, NOAA/NASA/USAF), Table I, geometric
// altitude, 5 significant figures. US Government work, public domain. Logged in LICENSES.md.
//
// Epsilons (relative, |model/table - 1|):
//   0-79 km  T, p, rho: 1e-4. The table is printed to 5 significant figures, so rounding alone is up to
//            5e-5 (e.g. 1.0524 Pa). The rest is headroom for R_AIR = 287.053 vs R*/M0 = 287.0531. Measured worst 6.7e-5.
//   80-86 km T: 5e-4. The table lists kinetic T. The code (like the USSA76 layer equations) uses
//            molecular-scale temperature T_M = T*M0/M; M/M0 = 0.99960 at 86 km gives a 4.0e-4 offset by design.
//            p, rho stay at 1e-4.
//   >86 km:  NOT validated. The code uses an isothermal exponential tail (H = 5.8 km). At 100 km it is
//            +11% in density vs USSA76. Above about 120 km it is wrong by orders of magnitude, because the
//            real thermosphere heats to 600-1000 K. That is the S2 gap. The 100 km case is a regression guard that
//            records the known error; it is not an accuracy claim.
const assert = require('node:assert');
const path = require('node:path');
const test = require('node:test');
const phys = path.join(__dirname, '..', '..', 'web', 'js', 'physics');

// [geometric z (m), T (K), p (Pa), rho (kg/m^3)], USSA76 Table I
const USSA76 = [
  [0, 288.150, 101325, 1.2250],
  [10e3, 223.252, 26500, 0.41351],
  [20e3, 216.650, 5529.3, 0.088910],
  [30e3, 226.509, 1197.0, 0.018410],
  [40e3, 250.350, 287.14, 0.0039957],
  [50e3, 270.650, 79.779, 0.0010269],
  [60e3, 247.021, 21.959, 3.0968e-4],
  [70e3, 219.585, 5.2209, 8.2829e-5],
  [80e3, 198.639, 1.0524, 1.8458e-5],
  [86e3, 186.870, 0.37338, 6.958e-6],
];
const rel = (a, b) => Math.abs(a / b - 1);

test('S2: atmosphere matches USSA76 Table I, 0-86 km', async () => {
  const { atmosphere } = await import(path.join(phys, 'atmosphere.js'));
  for (const [z, T, p, rho] of USSA76) {
    const a = atmosphere(z);
    const epsT = z >= 80e3 ? 5e-4 : 1e-4;
    assert.ok(rel(a.T, T) <= epsT, `T @ ${z} m: ${a.T} vs ${T}`);
    assert.ok(rel(a.p, p) <= 1e-4, `p @ ${z} m: ${a.p} vs ${p}`);
    assert.ok(rel(a.rho, rho) <= 1e-4, `rho @ ${z} m: ${a.rho} vs ${rho}`);
  }
});

test('S2 known gap: tail above 86 km is approximate (regression guard, not accuracy)', async () => {
  const { atmosphere } = await import(path.join(phys, 'atmosphere.js'));
  const a = atmosphere(100e3); // USSA76: T 195.08 K, p 0.032011 Pa, rho 5.604e-7
  const e = a.rho / 5.604e-7 - 1;
  // Measured +0.111. If this moves, the tail changed. Re-validate and update docs/ACCURACY_REVIEW.md.
  assert.ok(e > 0.10 && e < 0.12, `100 km density error ${e}`);
});

test.todo('S2: thermosphere density 100-1000 km vs USSA76 / NRLMSIS (needs Kestrel.Core.Atmo, not on main)');

// --- J2 ---------------------------------------------------------------------------------------------
// Earth constants: WGS-84 a = 6378137 m, GM = 3.986004418e14; J2 = 1.08262668e-3 (EGM96/WGS-84 C20 unnormalised).
// The sim is point-mass only today (gravity.js: "J2 not modeled in M1"), so these tests validate the
// *reference* first-order secular formula that S3 must reproduce:
//   dOmega/dt = -1.5 n J2 (R/p)^2 cos i.
// Checks against two published facts:
//   ISS (~420 km, i = 51.64 deg) node regression about -5.0 deg/day. eps 0.1 deg/day covers first-order J2
//     (J2^2 and J4 terms are about 1%) and the rounding of the published figure.
//   Sun-synchronous 800 km circular needs i of about 98.6 deg. eps 0.1 deg, same reasons.
const J2 = 1.08262668e-3, RE = 6378137, MU = 3.986004418e14, DAY = 86400;
const nodeRateDegPerDay = (a, e, iDeg) => {
  const n = Math.sqrt(MU / a ** 3), p = a * (1 - e * e);
  return -1.5 * n * J2 * (RE / p) ** 2 * Math.cos(iDeg * Math.PI / 180) * DAY * 180 / Math.PI;
};

test('J2 reference: ISS nodal regression about -5.0 deg/day', () => {
  const r = nodeRateDegPerDay(RE + 420e3, 0, 51.64);
  assert.ok(Math.abs(r - -5.0) <= 0.1, `ISS dOmega/dt ${r}`);
});

test('J2 reference: sun-synchronous inclination at 800 km about 98.6 deg', () => {
  const ssRate = 360 / 365.2422; // deg/day, mean Sun
  const a = RE + 800e3, n = Math.sqrt(MU / a ** 3);
  const cosI = -ssRate / (DAY * 180 / Math.PI) / (1.5 * n * J2 * (RE / a) ** 2);
  const iDeg = Math.acos(cosI) * 180 / Math.PI;
  assert.ok(Math.abs(iDeg - 98.6) <= 0.1, `sun-sync i ${iDeg}`);
});

test('J2 status: sim gravity is still point-mass (update this file when S3 lands)', async () => {
  const fs = require('node:fs');
  const src = fs.readFileSync(path.join(phys, 'gravity.js'), 'utf8');
  assert.match(src, /J2 not modeled/, 'gravity.js changed: replace this guard with a propagator-vs-reference J2 test');
});

test.todo('S3: propagator nodal regression matches nodeRateDegPerDay within 1% over 10 days (needs S3 propagator)');
test.todo('S2/S3: orbital decay lifetime vs reference (needs thermosphere density + decay model; DECISIONS on decay strength/F10.7 pending)');
