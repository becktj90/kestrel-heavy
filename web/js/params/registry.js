// Product parameters. data/params.json must stay identical to PARAMS.
// Designer ranges, steps and thresholds live in data/designer-limits.json.
// Only the layer names stay here: Params/Registry reads designer.layers.
import { LAUNCH_LATITUDE, LOSS_BUDGET, G0 } from '../designer/readout.js';
import { DESIGN_VERSION } from '../vehicle/design.js';
import { LIGHT } from '../flight/sensors.js';
import { DEFAULT_WIND_LIMIT_MS } from '../flight/weather.js';

export const PARAMS = {
  version: 1,
  designVersion: DESIGN_VERSION,
  designer: {
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
