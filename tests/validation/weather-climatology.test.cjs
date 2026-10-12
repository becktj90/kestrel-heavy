// Validation (R5/X4 weather): climatology checks.
//
// Seeded generator lives in Kestrel.Core.Weather (P4). Statistical climatology vs NOAA
// Melbourne USW00012838 is executed by C# WeatherTests.Climatology_MonthlyMeans_VsNoaaMelbourne
// (bands = 3σ/√N + interannual spread). This file pins the same NOAA literals and the
// Flight.Weather.Hold policy contract (DECISIONS #5).
const assert = require('node:assert');
const path = require('node:path');
const test = require('node:test');
const wx = path.join(__dirname, '..', '..', 'web', 'js', 'flight', 'weather.js');

// NOAA NCEI U.S. Climate Normals 1991–2020 — Melbourne Intl AP, FL (USW00012838).
// Temp: MLY-TAVG-NORMAL °F→°C. Wind: monthly mean of HLY-WIND-AVGSPD mph→m/s.
// Interannual temp σ: MLY-TAVG-STDDEV °F→°C. Wind interannual: fictional 10% of mean.
// Thunder p: fictional Florida-coastal (~80 days/yr); not a NOAA station product.
const NOAA_STATION_ID = 'USW00012838';
const NOAA_STATION_NAME = 'MELBOURNE INTL AP, FL US';
const NOAA_MEAN_WIND_MS = [3.91, 4.05, 4.35, 4.46, 4.31, 3.60, 3.24, 3.30, 3.71, 4.17, 4.13, 3.75];
const NOAA_MEAN_TEMP_C = [16.50, 17.78, 19.67, 22.11, 24.89, 27.00, 27.61, 27.78, 27.00, 24.61, 20.56, 18.17];
const NOAA_INTERANNUAL_TEMP_SIGMA_C = [1.611, 1.889, 1.722, 1.278, 0.778, 0.611, 0.611, 0.722, 0.556, 1.056, 1.500, 2.167];
const THUNDER_DAY_P = [0.05, 0.06, 0.10, 0.15, 0.25, 0.40, 0.50, 0.48, 0.35, 0.15, 0.08, 0.05];
const TEMP_DRAW_SIGMA_C = 2.5;
const N_CLIM = 10000;

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

test('climatology: NOAA Melbourne USW00012838 monthly temp literals and 3σ/√N+interannual bands', () => {
  // Executable generator check: unity WeatherTests.Climatology_MonthlyMeans_VsNoaaMelbourne.
  assert.strictEqual(NOAA_STATION_ID, 'USW00012838');
  assert.ok(NOAA_STATION_NAME.includes('MELBOURNE'));
  assert.strictEqual(NOAA_MEAN_TEMP_C.length, 12);
  assert.strictEqual(NOAA_INTERANNUAL_TEMP_SIGMA_C.length, 12);
  for (let m = 0; m < 12; m++) {
    const band = (3 * TEMP_DRAW_SIGMA_C) / Math.sqrt(N_CLIM) + NOAA_INTERANNUAL_TEMP_SIGMA_C[m];
    assert.ok(band > 0.5, `temp band month ${m + 1}`);
    assert.ok(NOAA_MEAN_TEMP_C[m] > 10 && NOAA_MEAN_TEMP_C[m] < 35, `temp SI month ${m + 1}`);
  }
});

test('climatology: NOAA Melbourne monthly wind literals and Rayleigh 3σ/√N+interannual bands', () => {
  // Executable generator check: unity WeatherTests.Climatology_MonthlyMeans_VsNoaaMelbourne.
  // Hold-rate vs wind: unity WeatherTests.Ensemble_WindExceedance_MatchesRayleigh_LargeN.
  assert.strictEqual(NOAA_MEAN_WIND_MS.length, 12);
  for (let m = 0; m < 12; m++) {
    const mu = NOAA_MEAN_WIND_MS[m];
    const sigma = mu * Math.sqrt(4 / Math.PI - 1);
    const interannual = 0.10 * mu; // fictional 10% — same as SiteClimatology.InterannualWindSigmaMs
    const band = (3 * sigma) / Math.sqrt(N_CLIM) + interannual;
    assert.ok(band > 0.3 && band < 1.0, `wind band month ${m + 1}`);
    assert.ok(mu > 2 && mu < 6, `wind SI month ${m + 1}`);
  }
  const thBandJan = 3 * Math.sqrt(THUNDER_DAY_P[0] * (1 - THUNDER_DAY_P[0]) / N_CLIM) + 0.05;
  assert.ok(thBandJan > 0.05 && thBandJan < 0.1);
});

// Upper-level shear vs NOAA IGRA radiosonde climatology stays open: P4 shear is visual/LCC-only
// (AscentSim ignores load-relief/q·α, crosswind drift, steering loss). 6-DOF Engineer layer will
// add wind-relative α, qα load limits, bending/structural margin, and day-of-launch wind-biased
// trajectories — that is when an IGRA comparison becomes meaningful.
test.todo('climatology: upper-level wind profile vs NOAA IGRA (PD) — deferred until shear feeds 6-DOF AscentSim');
