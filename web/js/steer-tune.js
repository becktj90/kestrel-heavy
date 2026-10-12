/**
 * Touch-steer tuning. Numbers and the curve description live in data/steer.json
 * so Unity can reuse them. This module is the Phaser copy; tests lock the two together.
 */
export const STEER = {
  note: 'Portrait touch steering for the Phaser web demo. Unity should apply the same curve. The Phaser iOS pack is frozen and is not updated from this file.',
  deadPx: 14,
  fullPx: 110,
  fullPxCompact: 72,
  compactMaxWidth: 390,
  expo: 1.6,
  formula: 'axis = sign(dx) * clamp(|dx| / (fullPx / sensitivity), 0, 1) ^ expo. fullPx drops to fullPxCompact when the viewport width is <= compactMaxWidth. Deadzone (deadPx) applies only until the drag arms.',
  sensitivity: {
    default: 1,
    min: 0.6,
    max: 1.6,
  },
};
