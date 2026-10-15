// Look-ahead, FOV breathe and a damped shake. Metres and degrees, no Phaser types.
// The chase camera in the Unity slice uses the same constants.

export const OMEGA = 28;
export const ZETA = 0.72;
export const LEAD_FRACTION = 0.42;
export const LEAD_VERTICAL = 0.35;
export const FOV_MACH = 0.04;
export const FOV_Q = 0.035;
export const PUNCH_FOV = 0.04;
export const SHAKE_SCALE = 0.12;

export function lead(vx, vy, height) {
  const h = height > 1 ? height : 1;
  const sp = Math.hypot(vx, vy);
  const reach = h * LEAD_FRACTION;
  if (sp < 1e-3) return { x: 0, y: 0 };
  const scale = (reach * (1 - Math.exp(-sp / 180))) / sp;
  return { x: vx * scale, y: vy * scale * LEAD_VERTICAL };
}

export function fovScale(mach, qNorm, punch) {
  let m = Number.isFinite(mach) && mach > 0 ? mach : 0;
  let q = Number.isFinite(qNorm) && qNorm > 0 ? qNorm : 0;
  if (q > 1.5) q = 1.5;
  let p = Number.isFinite(punch) && punch > 0 ? punch : 0;
  if (p > 1) p = 1;
  const widen = 1 + FOV_MACH * (1 - Math.exp(-m)) + FOV_Q * q;
  return widen * (1 - PUNCH_FOV * p);
}

export function decayPunch(punch, dt) {
  if (!Number.isFinite(punch) || punch < 0) return 0;
  const step = Number.isFinite(dt) && dt > 0 ? dt : 0;
  return punch * Math.exp(-3.2 * step);
}

export function kick(state, amp, angleRad) {
  const a = Number.isFinite(amp) && amp > 0 ? amp : 0;
  const ang = Number.isFinite(angleRad) ? angleRad : 0;
  state.vx += Math.cos(ang) * a;
  state.vy += Math.sin(ang) * a;
  return state;
}

function stepOnce(state, dt) {
  const w = OMEGA;
  const z = ZETA;
  const ax = -2 * z * w * state.vx - w * w * state.x;
  const ay = -2 * z * w * state.vy - w * w * state.y;
  state.vx += ax * dt;
  state.vy += ay * dt;
  state.x += state.vx * dt;
  state.y += state.vy * dt;
}

export function stepShake(state, dt) {
  if (!Number.isFinite(dt) || dt <= 0) return state;
  let step = dt > 0.1 ? 0.1 : dt;
  let n = 1;
  let h = step;
  if (h > 1 / 120) {
    n = Math.ceil(h * 120);
    if (n > 8) n = 8;
    h = step / n;
  }
  for (let i = 0; i < n; i++) stepOnce(state, h);
  if (state.x * state.x + state.y * state.y < 1e-10 && state.vx * state.vx + state.vy * state.vy < 1e-8) {
    state.x = state.y = state.vx = state.vy = 0;
  }
  return state;
}

export function freshShake() {
  return { x: 0, y: 0, vx: 0, vy: 0 };
}

export function stepPitch(currentDeg, targetDeg, rate, dt) {
  if (!Number.isFinite(currentDeg)) return targetDeg;
  if (!Number.isFinite(targetDeg)) return currentDeg;
  let step = Number.isFinite(dt) && dt > 0 ? dt : 0;
  if (step > 0.1) step = 0.1;
  const k = Number.isFinite(rate) && rate > 0 ? rate : 0;
  return currentDeg + (targetDeg - currentDeg) * (1 - Math.exp(-k * step));
}
