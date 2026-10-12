// Payload archetypes + target orbits: thin loader over data/payloads.json.
// mass kg, volume m^3, maxG axial g-limit, maxHeatFlux W/m^2 at fairing jettison;
// orbits: altitudes km, inclination deg.
import RAW from './gen/payloads.js';

export const ORBITS = Object.freeze(RAW.orbits);
export const PAYLOADS = Object.freeze(RAW.payloads);
export const PAYLOAD_BY_ID = Object.freeze(Object.fromEntries(PAYLOADS.map((p) => [p.id, p])));
