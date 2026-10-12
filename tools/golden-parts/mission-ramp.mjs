// Mission ramp, stars, and Haven drift. mission-ramp.js is the reference.
import { readFileSync } from 'node:fs';
import { MISSION_RAMP, starsForTouchdown, unlocksNextMission } from '../../web/js/mission-ramp.js';
import { DIFFICULTY, FUEL_MAX, HAVEN } from '../../web/js/config.js';

const SWELL_W = 0.28;
const GUST_W = 0.62;
const GUST_FORCE = 0.0034;
const DRIFT_FLOOR = 0.28;

function windProduct(difficultyWind, mission) {
  const base = Number(difficultyWind) || 0;
  const scale = mission && Number.isFinite(mission.wind) ? mission.wind : 1;
  return base * scale;
}

function driftOf(swellT, difficultyWind, mission) {
  const product = windProduct(difficultyWind, mission);
  const wind = product || DRIFT_FLOOR;
  const t = Number(swellT) || 0;
  const amp = mission && Number.isFinite(mission.swellAmp) ? mission.swellAmp : HAVEN.swellAmp;
  const swell = Math.sin(t * SWELL_W) * amp;
  const gust = Math.sin(t * GUST_W) * wind;
  return { wind: product, driftWind: wind, swell, gust, forceX: gust * GUST_FORCE };
}

function havenFuel(sessionFuel, mission) {
  const floor = mission && Number.isFinite(mission.landingFuel) ? mission.landingFuel : HAVEN.landingFuel;
  return Math.min(FUEL_MAX, Math.max(sessionFuel, floor));
}

export async function register({ put, REL }) {
  const disk = JSON.parse(readFileSync(new URL('../../data/mission-ramp.json', import.meta.url), 'utf8'));
  if (JSON.stringify(disk) !== JSON.stringify(MISSION_RAMP)) throw new Error('data/mission-ramp.json drifted from mission-ramp.js');
  const scene = readFileSync(new URL('../../web/js/mission.js', import.meta.url), 'utf8');
  for (const snippet of [
    'const wind = this.missionWind(mode) || 0.28',
    'Math.sin((this.session.swellT || 0) * 0.28) * swellAmp',
    'Math.sin((this.session.swellT || 0) * 0.62) * wind',
    'gust * 0.0034',
    'Math.min(FUEL_MAX, Math.max(this.session.fuel, landingFuel))',
  ]) {
    if (!scene.includes(snippet)) throw new Error('mission.js drift: ' + snippet);
  }
  if (MISSION_RAMP.missions['KH-3'].landingFuel !== HAVEN.landingFuel) throw new Error('KH-3 landing fuel left the Haven default');
  if (MISSION_RAMP.missions['KH-3'].swellAmp !== HAVEN.swellAmp) throw new Error('KH-3 swell left the Haven default');

  const cases = [{
    fn: 'fallback',
    out: { landingFuel: HAVEN.landingFuel, swellAmp: HAVEN.swellAmp, driftWind: DRIFT_FLOOR, fuelMax: FUEL_MAX },
  }];
  for (const [id, m] of Object.entries(MISSION_RAMP.missions)) {
    cases.push({ fn: 'mission', mission: id, out: { wind: m.wind, landingFuel: m.landingFuel, swellAmp: m.swellAmp } });
  }
  for (const reason of ['recovered', 'salvage', 'splash', 'rud', 'abort']) {
    cases.push({ fn: 'unlock', reason, out: unlocksNextMission(reason) });
  }
  const lim = { landingVy: 6, landingTol: 40, kind: 'deck', recovered: true, tipped: false };
  const stars = [
    ['splash', { reason: 'splash', ...lim }],
    ['rud', { reason: 'rud', kind: 'rud' }],
    ['water', { reason: 'recovered', kind: 'water', vy: 0, dx: 0, landingVy: 6, landingTol: 40, recovered: true, tipped: false }],
    ['three', { reason: 'recovered', vy: 2, dx: 8, ...lim }],
    ['two', { reason: 'recovered', vy: 4.2, dx: 28, ...lim }],
    ['one', { reason: 'recovered', vy: 5.7, dx: 38, ...lim }],
    ['salvage', { reason: 'salvage', vy: 9, dx: 80, ...lim, recovered: false }],
    ['tipped', { reason: 'recovered', vy: 1, dx: 1, ...lim, tipped: true }],
  ];
  for (const [name, touch] of stars) {
    cases.push({ fn: 'stars', name, touch, out: starsForTouchdown(touch) });
  }
  for (const [difficulty, mode] of Object.entries(DIFFICULTY)) {
    for (const [id, mission] of Object.entries(MISSION_RAMP.missions)) {
      cases.push({
        fn: 'wind', difficulty, mission: id, difficultyWind: mode.wind,
        out: windProduct(mode.wind, mission),
      });
    }
  }
  for (const [mission, difficultyWind, swellT, sessionFuel] of [
    ['KH-1', DIFFICULTY.KID.wind, 0, 10],
    ['KH-1', DIFFICULTY.KID.wind, 1, 10],
    ['KH-3', 0, 4.2, 80],
    ['KH-5', DIFFICULTY.PAD_RAT.wind, 2.5, 120],
    ['KH-9', DIFFICULTY.CADET.wind, 1, 10],
  ]) {
    const missionRow = MISSION_RAMP.missions[mission];
    cases.push({
      fn: 'haven', mission, difficultyWind, swellT, sessionFuel,
      out: { ...driftOf(swellT, difficultyWind, missionRow), fuel: havenFuel(sessionFuel, missionRow) },
    });
  }
  put('mission-ramp', { ...REL, note: 'stars and unlock exact; Haven drift is sin, rel 1e-9. Stars never gate unlock.' }, cases);
}
