// Preset vehicle designs (DESIGN_VERSION 1). Generous margins; physics not faked.
const base = (o) => ({
  version: 1,
  strapOns: { count: 0, kind: 'solid', propellant: 'solid-apcp', engine: 'srm-small', engineCount: 1, propMass: 0, diameter: 1.6, material: 'steel', crossfeed: false },
  aero: { nose: 'ogive', fins: false },
  landing: { legs: false, gridFins: false, rcs: false },
  avionics: { tier: 'standard' },
  safetyFactor: 1.4,
  livery: { primary: '#f2f2f2', accent: '#1a3c6e', name: '' },
  ...o,
});
const st = (o) => ({ interstageMass: 0, separationMass: 50, landingPropMass: 0, material: 'al-li', ...o });

export const PRESETS = Object.freeze([
  base({ id: 'stock', name: 'Stock Kestrel Heavy', tier: 'arcade',
    stages: [
      st({ name: 'Core', propellant: 'kerolox', engine: 'gg-kerolox', engineCount: 9, propMass: 300000, diameter: 3.7, interstageMass: 1500, landingPropMass: 25000 }),
      st({ name: 'Upper', propellant: 'kerolox', engine: 'gg-kerolox-vac', engineCount: 1, propMass: 70000, diameter: 3.7 }),
    ],
    strapOns: { count: 2, kind: 'liquid', propellant: 'kerolox', engine: 'gg-kerolox', engineCount: 9, propMass: 300000, diameter: 3.7, material: 'al-li', crossfeed: false },
    payload: { type: 'leo-sat', mass: 8000, orbit: 'leo400' },
    fairing: { diameter: 5.2, length: 13, material: 'composite', jettisonAltitudeKm: 110 },
    aero: { nose: 'ogive', fins: false },
    landing: { legs: true, gridFins: true, rcs: true },
    livery: { primary: '#f2f2f2', accent: '#c8102e', name: 'KESTREL HEAVY' } }),
  base({ id: 'scout', name: 'Scout', tier: 'small',
    stages: [
      st({ name: 'Stage 1', propellant: 'methalox', engine: 'gg-methalox-small', engineCount: 9, propMass: 35000, diameter: 1.8, material: 'composite', interstageMass: 200 }),
      st({ name: 'Stage 2', propellant: 'methalox', engine: 'gg-methalox-small', engineCount: 1, propMass: 6000, diameter: 1.8, material: 'composite' }),
    ],
    payload: { type: 'cubesat', mass: 150, orbit: 'leo400' },
    fairing: { diameter: 1.8, length: 2.5, material: 'composite', jettisonAltitudeKm: 100 },
    aero: { nose: 'ogive', fins: true },
    livery: { primary: '#222222', accent: '#ffb000', name: 'SCOUT' } }),
  base({ id: 'kid', name: 'Kid Cruiser', tier: 'kid',
    stages: [
      st({ name: 'Big Booster', propellant: 'kerolox', engine: 'gg-kerolox', engineCount: 5, propMass: 140000, diameter: 3.0, interstageMass: 800 }),
      st({ name: 'Space Stage', propellant: 'hydrolox', engine: 'exp-hydrolox', engineCount: 2, propMass: 12000, diameter: 3.0 }),
    ],
    payload: { type: 'kid-box', mass: 300, orbit: 'leo200' },
    fairing: { diameter: 3.0, length: 4, material: 'composite', jettisonAltitudeKm: 100 },
    aero: { nose: 'blunt', fins: true },
    safetyFactor: 1.6,
    livery: { primary: '#4fc3f7', accent: '#ff4081', name: 'KID CRUISER' } }),
  base({ id: 'small-lift', name: 'Small-lift (archetype)', tier: 'small',
    stages: [
      st({ name: 'Stage 1', propellant: 'kerolox', engine: 'gg-kerolox', engineCount: 1, propMass: 45000, diameter: 2.4, interstageMass: 300 }),
      st({ name: 'Stage 2', propellant: 'methalox', engine: 'gg-methalox-small', engineCount: 1, propMass: 7000, diameter: 2.4 }),
    ],
    payload: { type: 'leo-sat', mass: 400, orbit: 'sso' },
    fairing: { diameter: 2.4, length: 3.5, material: 'composite', jettisonAltitudeKm: 105 } }),
  base({ id: 'medium-lift', name: 'Medium-lift (archetype)', tier: 'medium',
    stages: [
      st({ name: 'Stage 1', propellant: 'kerolox', engine: 'gg-kerolox', engineCount: 9, propMass: 400000, diameter: 3.7, interstageMass: 1500 }),
      st({ name: 'Stage 2', propellant: 'kerolox', engine: 'gg-kerolox-vac', engineCount: 1, propMass: 100000, diameter: 3.7 }),
    ],
    payload: { type: 'gto-comsat', mass: 5500, orbit: 'gto' },
    fairing: { diameter: 5.2, length: 13, material: 'composite', jettisonAltitudeKm: 110 } }),
  base({ id: 'super-heavy', name: 'Super-heavy (archetype)', tier: 'super-heavy',
    stages: [
      st({ name: 'Booster', propellant: 'methalox', engine: 'ff-methalox', engineCount: 33, propMass: 3400000, diameter: 9, material: 'steel', interstageMass: 10000 }),
      st({ name: 'Upper', propellant: 'methalox', engine: 'ff-methalox-vac', engineCount: 3, propMass: 1200000, diameter: 9, material: 'steel' }),
    ],
    payload: { type: 'leo-sat', mass: 60000, orbit: 'leo400' },
    fairing: { diameter: 9, length: 18, material: 'steel', jettisonAltitudeKm: 120 } }),
]);
export const PRESET_BY_ID = Object.freeze(Object.fromEntries(PRESETS.map((p) => [p.id, p])));
