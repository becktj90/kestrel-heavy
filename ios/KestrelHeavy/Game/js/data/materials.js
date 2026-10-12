// Tank / structure materials: thin loader over data/materials.json.
// structuralFraction = tank+structure dry mass per kg of propellant (before safety-factor
// scaling). density kg/m^3, costPerKg USD.
import RAW from './gen/materials.js';

export const MATERIALS = Object.freeze(RAW.materials);
export const MATERIAL_BY_ID = Object.freeze(Object.fromEntries(MATERIALS.map((m) => [m.id, m])));
