// Tank sections: thin loader over data/tanks.json.
// diameter and length in m, masses in kg, cost in USD. dryMass is the M1 structure model at safety factor 1.4.
import RAW from './gen/tanks.js';

export const TANKS = Object.freeze(RAW.tanks);
export const TANK_BY_ID = Object.freeze(Object.fromEntries(TANKS.map((t) => [t.id, t])));
