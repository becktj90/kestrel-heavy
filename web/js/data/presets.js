// Preset vehicle designs (DESIGN_VERSION 1): thin loader over data/presets.json.
// Each preset is a complete, plain design (same schema as saved designs). Generous margins;
// physics not faked.
// Starter templates (web/js/data/templates/*.json, see tools/templates/build.mjs) are referenced
// here, not duplicated; they replaced the old small-lift / medium-lift archetypes.
import RAW from './gen/presets.js';
import { TEMPLATES } from './templates/index.js';

export const PRESETS = Object.freeze([...RAW.presets, ...TEMPLATES]);
export const PRESET_BY_ID = Object.freeze(Object.fromEntries(PRESETS.map((p) => [p.id, p])));
