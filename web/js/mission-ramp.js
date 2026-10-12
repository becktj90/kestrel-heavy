/**
 * Mission ramp and star ratings. Numbers match data/mission-ramp.json.
 * Stars are a rating. They do not unlock the next flight.
 */
export const MISSION_RAMP = {
  note: 'Per-mission assists for the Phaser web demo. Unity should reuse these numbers. Wind is a multiplier on the difficulty wind force. landingFuel and swellAmp replace the Haven defaults. Physics integration is unchanged.',
  unlock: 'A deck finish (recovered or salvage, including a tip) unlocks the next mission. A splash does not. Stars never gate the next mission.',
  windFormula: 'windForce = difficulty.wind * mission.wind',
  stars: {
    threeMin: 0.45,
    twoMin: 0.2,
    formula: 'Splash and RUD score 0. A tipped deck or any negative margin scores 1. A recovered catch scores 3 when min(1-|vy|/landingVy, 1-|dx|/landingTol) >= threeMin, 2 when >= twoMin, otherwise 1.',
  },
  missions: {
    'KH-1': { wind: 0.55, landingFuel: 52, swellAmp: 6 },
    'KH-2': { wind: 0.75, landingFuel: 44, swellAmp: 10 },
    'KH-3': { wind: 1, landingFuel: 36, swellAmp: 16 },
    'KH-4': { wind: 1.2, landingFuel: 30, swellAmp: 22 },
    'KH-5': { wind: 1.45, landingFuel: 24, swellAmp: 28 },
  },
};

/** Deck finishes advance the manifest. Splash and RUD do not. Stars are ignored. */
export function unlocksNextMission(reason) {
  return reason === 'recovered' || reason === 'salvage';
}

export function starsForTouchdown(t, rules = MISSION_RAMP.stars) {
  if (!t || t.reason === 'splash' || t.reason === 'rud' || t.kind === 'water') return 0;
  if (t.kind !== 'deck') return 0;
  const limVy = Math.max(0.1, Number(t.landingVy) || 0.1);
  const limDx = Math.max(1, Number(t.landingTol) || 1);
  const margin = Math.min(
    1 - Math.abs(Number(t.vy) || 0) / limVy,
    1 - Math.abs(Number(t.dx) || 0) / limDx,
  );
  if (t.tipped || margin < 0 || !t.recovered) return 1;
  if (margin >= rules.threeMin) return 3;
  if (margin >= rules.twoMin) return 2;
  return 1;
}
