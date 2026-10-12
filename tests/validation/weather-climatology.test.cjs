// Validation (R5/X4 weather): climatology checks.
//
// Status on main (4685247): there is NO weather model to validate. web/js/flight/weather.js and
// Kestrel.Core/Flight/Weather.cs are a wind-hold *policy* ("Not a weather simulation"). A climatology
// check needs a seeded generator (R5: Weather.Generate(site, dateSeed, difficulty)). Until that exists, we
// would only be comparing NOAA normals with themselves. So this file:
//   1. pins the policy contract the generator must keep (DECISIONS #5: no holds in KID or Relaxed), and
//   2. lists the climatology checks as todo, with the planned public-domain reference and epsilon.
// No reference data ships in this PR, so there is no LICENSES.md row yet. The row lands with the data.
const assert = require('node:assert');
const path = require('node:path');
const test = require('node:test');
const wx = path.join(__dirname, '..', '..', 'web', 'js', 'flight', 'weather.js');

test('policy: KID and Relaxed never hold, whatever the wind', async () => {
  const { weatherHold } = await import(wx);
  for (const windMs of [0, 12, 50]) {
    assert.deepStrictEqual(weatherHold({ difficulty: 'KID', windMs }), { hold: false, allowed: false });
    assert.deepStrictEqual(weatherHold({ difficulty: 'STANDARD', relaxed: true, windMs }), { hold: false, allowed: false });
  }
});

test('policy: Standard holds strictly above the limit (default 12 m/s)', async () => {
  const { weatherHold, DEFAULT_WIND_LIMIT_MS } = await import(wx);
  assert.strictEqual(DEFAULT_WIND_LIMIT_MS, 12);
  assert.strictEqual(weatherHold({ difficulty: 'STANDARD', windMs: 12 }).hold, false);
  assert.strictEqual(weatherHold({ difficulty: 'STANDARD', windMs: 12.01 }).hold, true);
});

// Planned once R5 lands. Reference: NOAA NCEI U.S. Climate Normals 1991-2020 (public domain), monthly
// mean temperature and mean wind speed for a real coastal station standing in for the fictional site.
// Epsilons will be statistical, not fitted: the mean of N >= 1000 seeded days must fall within
// 3 sigma/sqrt(N) of the normal (sampling error), plus the normals' own interannual spread (stated per field).
test.todo('climatology: seeded monthly mean surface temperature within 3 sigma/sqrt(N) + interannual spread of NOAA 1991-2020 normals');
test.todo('climatology: seeded monthly mean surface wind within the same statistical bound; hold rate plausible vs normals');
test.todo('climatology: upper-level wind profile vs public radiosonde climatology (NOAA IGRA, PD) for shear rules');
