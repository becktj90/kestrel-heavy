// Fins and grid fins: thin loader over data/fins.json.
// span in m, mass and cost are the whole set.
import RAW from './gen/fins.js';

export const FINS = Object.freeze(RAW.fins);
export const FIN_BY_ID = Object.freeze(Object.fromEntries(FINS.map((f) => [f.id, f])));
