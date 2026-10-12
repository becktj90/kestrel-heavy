// Sensor fidelity. Exact in KID and Sandbox. Light noise only on the Standard layer.
// Mass and propellant are never noised. One mulberry32 sample per noisy channel, in [-1, 1).
export const LIGHT = Object.freeze({ altitude: 0.0015, speed: 0.002, angleDeg: 0.15 });

export function fidelity({ difficulty, sandbox = false, layer } = {}) {
  if (sandbox || difficulty === 'KID') return 'exact';
  if (layer === 'standard') return 'light-noise';
  return 'exact';
}

export function mulberry32(seed) {
  let a = seed >>> 0;
  return function next() {
    a = (a + 0x6D2B79F5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

export function sense(truth, opts = {}) {
  const mode = fidelity(opts);
  const out = {
    mode,
    altitude: truth.altitude,
    speed: truth.speed,
    angleDeg: truth.angleDeg,
  };
  if ('mass' in truth) out.mass = truth.mass;
  if ('propellant' in truth) out.propellant = truth.propellant;
  if (mode !== 'light-noise') return out;
  const rng = mulberry32(opts.seed >>> 0);
  const nAlt = rng() * 2 - 1, nSpd = rng() * 2 - 1, nAng = rng() * 2 - 1;
  out.altitude = truth.altitude * (1 + LIGHT.altitude * nAlt);
  out.speed = truth.speed * (1 + LIGHT.speed * nSpd);
  out.angleDeg = truth.angleDeg + LIGHT.angleDeg * nAng;
  return out;
}
