// Tank / structure materials. structuralFraction = tank+structure dry mass per kg of
// propellant loaded (before safety-factor scaling). density kg/m^3, costPerKg USD.
export const MATERIALS = Object.freeze([
  { id: 'al-li', label: 'Al-Li alloy', structuralFraction: 0.045, density: 2700, yieldMPa: 500, costPerKg: 60, maxTempK: 450, cryoOK: true },
  { id: 'steel', label: 'Stainless steel', structuralFraction: 0.075, density: 7900, yieldMPa: 1000, costPerKg: 8, maxTempK: 1100, cryoOK: true },
  { id: 'composite', label: 'Carbon composite', structuralFraction: 0.032, density: 1600, yieldMPa: 700, costPerKg: 300, maxTempK: 420, cryoOK: false, note: 'needs liner for deep-cryogenic LH2' },
]);
export const MATERIAL_BY_ID = Object.freeze(Object.fromEntries(MATERIALS.map((m) => [m.id, m])));
