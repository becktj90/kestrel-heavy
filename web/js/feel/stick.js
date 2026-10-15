// Time response on top of the expo curve in input.js. A full shove arrives over a fraction
// of a second, and letting go is quicker than pressing in. Digital pads (the D0 replay
// keyboard) stay one-frame exact so recorded traces do not move.

export const ATTACK = 6.5;
export const RELEASE = 11;
export const MAX_RATE = 5.5;

function clamp11(v) {
  if (!Number.isFinite(v)) return 0;
  if (v > 1) return 1;
  if (v < -1) return -1;
  return v;
}

/** One step of the stick toward command, in [-1, 1]. */
export function stepStick(current, command, dt) {
  let cur = Number.isFinite(current) ? current : 0;
  const cmd = clamp11(command);
  let stepDt = Number.isFinite(dt) && dt > 0 ? dt : 0;
  if (stepDt > 0.1) stepDt = 0.1;
  if (stepDt === 0) return cur;
  const err = cmd - cur;
  const growing = Math.abs(cmd) + 1e-9 >= Math.abs(cur);
  const rate = growing ? ATTACK : RELEASE;
  let step = err * (1 - Math.exp(-rate * stepDt));
  const cap = MAX_RATE * stepDt;
  if (step > cap) step = cap;
  else if (step < -cap) step = -cap;
  return clamp11(cur + step);
}

/**
 * Analog touch and pointer get the stick filter, then a short coast back to centre.
 * Keyboard and pad buttons (`analog` false and not already coasting) pass through.
 */
export function shapeFlightAxis(raw, felt, dt, analog, coast) {
  const command = clamp11(raw);
  if (!analog && !coast) return { axis: command, felt: command, coast: false };
  const next = stepStick(felt, command, dt);
  if (!analog && Math.abs(next) < 0.02) return { axis: command, felt: command, coast: false };
  return { axis: next, felt: next, coast: true };
}
