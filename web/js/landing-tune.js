/**
 * Touchdown grades. Thresholds match data/landing.json so Unity can reuse them.
 * Margin is how much of the catch limit is left. Physics limits stay landingVy / landingTol.
 */
export const LANDING = {
  note: 'Haven touchdown grades for the Phaser web demo. Unity should use the same margins. Sink rate and offset are the physics values already compared to landingVy and landingTol; this file does not change the integrator.',
  padRatLandingVy: 4,
  perfectMin: 0.45,
  goodMin: 0.2,
  formula: 'vyMargin = 1 - |vy| / landingVy; dxMargin = 1 - |dx| / landingTol; margin = min(vyMargin, dxMargin). Water is ungraded. Deck with tip, or either margin < 0, is Salvage. margin >= perfectMin is Perfect. margin >= goodMin is Good. Otherwise Hard.',
};

export function landingMargins(vy, dx, landingVy, landingTol) {
  const limVy = Math.max(0.1, Number(landingVy) || 0.1);
  const limDx = Math.max(1, Number(landingTol) || 1);
  const vyMargin = 1 - Math.abs(Number(vy) || 0) / limVy;
  const dxMargin = 1 - Math.abs(Number(dx) || 0) / limDx;
  return { vyMargin, dxMargin, margin: Math.min(vyMargin, dxMargin) };
}

/**
 * @returns {'Perfect'|'Good'|'Hard'|'Salvage'|null}
 */
export function gradeTouchdown({ kind, vy, dx, landingVy, landingTol, tipped }) {
  if (kind !== 'deck') return null;
  const { vyMargin, dxMargin, margin } = landingMargins(vy, dx, landingVy, landingTol);
  if (tipped || vyMargin < 0 || dxMargin < 0) return 'Salvage';
  if (margin >= LANDING.perfectMin) return 'Perfect';
  if (margin >= LANDING.goodMin) return 'Good';
  return 'Hard';
}
