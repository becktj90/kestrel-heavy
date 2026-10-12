// Product and designer parameters. data/params.json must stay identical to PARAMS.
import { MIN_TWR, GOOD_TWR, MIN_MARGIN, PACK } from '../designer/limits.js';
import { TUNE_LIMIT, LAUNCH_LATITUDE, LOSS_BUDGET, G0 } from '../designer/readout.js';
import {
  ENGINE_COUNT_MIN, ENGINE_COUNT_MAX, DIAMETER_MIN, DIAMETER_MAX, SAFETY_MIN, SAFETY_MAX,
  AVIONICS_TIERS, MAX_STAGES, STRAPON_COUNTS, DESIGN_VERSION,
} from '../vehicle/design.js';
import { LIGHT } from '../flight/sensors.js';
import { DEFAULT_WIND_LIMIT_MS } from '../flight/weather.js';

export const PARAMS = {
  version: 1,
  designVersion: DESIGN_VERSION,
  designer: {
    minTwr: MIN_TWR,
    goodTwr: GOOD_TWR,
    minMargin: MIN_MARGIN,
    pack: PACK,
    tuneLimitPct: TUNE_LIMIT,
    engineCountMin: ENGINE_COUNT_MIN,
    engineCountMax: ENGINE_COUNT_MAX,
    diameterMinM: DIAMETER_MIN,
    diameterMaxM: DIAMETER_MAX,
    safetyMin: SAFETY_MIN,
    safetyMax: SAFETY_MAX,
    maxStages: MAX_STAGES,
    strapOnCounts: [...STRAPON_COUNTS],
    avionicsTiers: [...AVIONICS_TIERS],
    layers: { default: 'simple', standardUnlock: 'first-orbit', engineer: 'settings-toggle' },
  },
  sensors: {
    kid: 'exact',
    sandbox: 'exact',
    standard: 'light-noise',
    altitudeRel: LIGHT.altitude,
    speedRel: LIGHT.speed,
    angleDeg: LIGHT.angleDeg,
  },
  weather: { kidHold: false, relaxedHold: false, windLimitMs: DEFAULT_WIND_LIMIT_MS },
  display: { orientation: 'portrait', bigScreen: 'themed-fill', refreshHz: 120, quality: 'Auto', batterySaver: false },
  comfort: { safeFlash: true, reducedMotion: 'follow-os' },
  career: { failurePenalty: 'moderate' },
  flight: { launchLatitude: LAUNCH_LATITUDE, lossBudget: LOSS_BUDGET, g0: G0 },
};
