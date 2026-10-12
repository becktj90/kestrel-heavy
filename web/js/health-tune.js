/**
 * Integrity hit tuning. Numbers match data/health.json so Unity can reuse them.
 */
export const HEALTH = {
  note: 'Hit assists for the Phaser web demo. Unity should reuse these numbers. Damage is a fraction of max HP so a smaller pool is not a second penalty. The invulnerability window stops stacked contacts from the same pass.',
  invulnSec: 0.4,
  damageRefHp: 100,
  formula: 'damage = round(base[kind] * (maxHp / damageRefHp) * difficultyMul[diff]). After a hit that is not grace, further hits deal 0 until invulnSec elapses.',
  difficultyMul: { KID: 0.55, CADET: 1, PAD_RAT: 1.15 },
  base: { bird: 28, balloon: 16, ice: 36, debris: 42, clutter: 24 },
};
