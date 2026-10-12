// Propellant catalog: thin loader over data/propellants.json (source of truth, see
// data/schemas/propellants.schema.json; regenerate mirrors with tools/gen-catalogs.mjs).
// SI units: density kg/m^3 (bulk, at mixture ratio), isp s (ispSL/ispVac: typical first-stage
// nozzle; ispVacMax: best large-expansion vacuum nozzle), costPerKg USD per kg loaded.
// storability: 'cryogenic' | 'deep-cryogenic' | 'storable' | 'solid'; boiloffPerHour: fraction/h.
import RAW from './gen/propellants.js';

export const PROPELLANTS = Object.freeze(RAW.propellants);
export const PROPELLANT_BY_ID = Object.freeze(Object.fromEntries(PROPELLANTS.map((p) => [p.id, p])));
export const densityImpulse = (p) => p.bulkDensity * p.ispVac;
/** Volume (m^3) needed for a propellant mass (kg). */
export const propVolume = (p, massKg) => massKg / p.bulkDensity;
