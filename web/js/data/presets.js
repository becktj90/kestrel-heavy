// Preset vehicle designs (DESIGN_VERSION 1): thin loader over data/presets.json.
// Each preset is a complete, plain design (same schema as saved designs). Generous margins;
// physics not faked.
import RAW from './gen/presets.js';

export const PRESETS = Object.freeze(RAW.presets);
export const PRESET_BY_ID = Object.freeze(Object.fromEntries(PRESETS.map((p) => [p.id, p])));
