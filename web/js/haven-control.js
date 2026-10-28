/**
 * Haven landing control shared by the Phaser scene.
 * A hold must reach the deck, and a soft on-paint arrival must be able to
 * stand up under the 14° tip line. The scripted strake glide is about -30°.
 */

export const HAVEN_TIP_DEG = 14;
export const HAVEN_LEVEL_ALT = 220;

export function havenSettleVy(landingVy, alt) {
  const lim = Math.max(0.1, Number(landingVy) || 6);
  return lim * (Number(alt) < 180 ? 0.45 : 0.75);
}

/**
 * Extra Matter force Y while HOLD TO BURN is down. Negative is up.
 * Lift only while the sink is faster than the settle rate, so a continuous
 * hold does not climb off the deck.
 */
export function havenBurnLift(vy, alt, landingVy) {
  if (Number(vy) > havenSettleVy(landingVy, alt)) return -0.032;
  return 0;
}

function clamp(v, lo, hi) {
  return Math.max(lo, Math.min(hi, v));
}

/**
 * Booster angle in degrees. Reentry and the open glide keep the nose down.
 * A lit burn, or KH-1 first-flight grace, stands the ship up inside the
 * last HAVEN_LEVEL_ALT px so the glide pose is not an automatic tip.
 * KID mode stands the ship up the same way even when the burn was never
 * lit: KID is the forgiving mode, and a soft, centered deck arrival should
 * not tip over solely because a kid never found the burn button.
 */
export function havenWantAngle({
  phase,
  side = -1,
  vx = 0,
  axis = 0,
  alt = 0,
  burnLit = false,
  firstFlight = false,
  kid = false,
}) {
  if (phase === 'reentry') return side * -46;
  const gliding = phase === 'strakes' || phase === 'glide';
  const closing = alt < HAVEN_LEVEL_ALT;
  const t = closing ? clamp((alt - 36) / (HAVEN_LEVEL_ALT - 36), 0, 1) : 1;
  const glide = clamp(side * -30 + vx * 1.4 + axis * 6, -40, 40);
  const deck = clamp(-axis * 8 + vx * 0.45, -(HAVEN_TIP_DEG - 2), HAVEN_TIP_DEG - 2);
  const level = Boolean(burnLit || firstFlight || kid);
  if (gliding && !(level && closing)) return glide;
  if (gliding && level && closing) return glide * t + deck * (1 - t);
  const burn = clamp(-axis * 18 + vx * 1.1, -24, 24);
  return closing ? burn * t + deck * (1 - t) : burn;
}
