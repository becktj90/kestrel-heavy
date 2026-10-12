// Payload fairings: thin loader over data/fairings.json.
// diameter and length in m, mass in kg, cost in USD, cd is the subsonic nose coefficient.
import RAW from './gen/fairings.js';

export const FAIRINGS = Object.freeze(RAW.fairings);
export const FAIRING_BY_ID = Object.freeze(Object.fromEntries(FAIRINGS.map((f) => [f.id, f])));
