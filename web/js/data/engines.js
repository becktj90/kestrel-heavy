// Engine archetype catalog: thin loader over data/engines.json (generic, no real product names).
// SI units: thrust N, mass kg, pc bar, gimbal deg, cost USD per unit; throttle [min,max] fraction;
// reliability = per-flight success probability.
import RAW from './gen/engines.js';

export const ENGINES = Object.freeze(RAW.engines);
export const ENGINE_BY_ID = Object.freeze(Object.fromEntries(ENGINES.map((e) => [e.id, e])));
export const ENGINE_CYCLES = Object.freeze(RAW.cycles);
