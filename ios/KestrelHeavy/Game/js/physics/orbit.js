// State vector -> orbital elements. SI units.
import { MU_EARTH, R_EARTH } from './gravity.js';

/**
 * 3D state (r,v as [x,y,z]) -> { a, e, i (rad), apoapsis, periapsis (radii, m),
 * apoAlt, periAlt (m above R_EARTH), energy (J/kg), h (m²/s), period (s) }.
 * For hyperbolic/parabolic orbits a<=0 or Infinity and apoapsis = Infinity.
 */
export function orbitalElements(r, v, mu = MU_EARTH, out = {}) {
  const rx = r[0], ry = r[1], rz = r[2] || 0, vx = v[0], vy = v[1], vz = v[2] || 0;
  const rm = Math.sqrt(rx * rx + ry * ry + rz * rz);
  const v2 = vx * vx + vy * vy + vz * vz;
  const hx = ry * vz - rz * vy, hy = rz * vx - rx * vz, hz = rx * vy - ry * vx;
  const h = Math.sqrt(hx * hx + hy * hy + hz * hz);
  const energy = v2 / 2 - mu / rm;
  const e = Math.sqrt(Math.max(0, 1 + 2 * energy * h * h / (mu * mu)));
  const a = energy === 0 ? Infinity : -mu / (2 * energy);
  const peri = h * h / (mu * (1 + e));
  const apo = e < 1 ? a * (1 + e) : Infinity;
  out.a = a; out.e = e; out.i = h > 0 ? Math.acos(Math.max(-1, Math.min(1, hz / h))) : 0;
  out.energy = energy; out.h = h;
  out.periapsis = peri; out.apoapsis = apo;
  out.periAlt = peri - R_EARTH; out.apoAlt = apo - R_EARTH;
  out.period = e < 1 ? 2 * Math.PI * Math.sqrt(a * a * a / mu) : Infinity;
  return out;
}

const _r = [0, 0, 0], _v = [0, 0, 0];
/** Planar convenience: (x,y,vx,vy). Allocation-free when `out` is reused. */
export function orbit2D(x, y, vx, vy, mu = MU_EARTH, out = {}) {
  _r[0] = x; _r[1] = y; _v[0] = vx; _v[1] = vy;
  return orbitalElements(_r, _v, mu, out);
}

/** Circular orbital speed at altitude alt (m). */
export function circularVelocity(alt, mu = MU_EARTH, radius = R_EARTH) {
  return Math.sqrt(mu / (radius + alt));
}
