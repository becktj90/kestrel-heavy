// Point-mass Newtonian gravity plus zonal J2–J4 (Montenbruck & Gill 3.36–3.38). SI units.
// Constants match data/bodies.json / Geopotential.cs. Tesseral terms are out of scope.
export const MU_EARTH = 3.986004418e14; // m³/s² (WGS-84 / IERS)
export const R_EARTH = 6378137;         // m, equatorial (WGS-84)
export const J2 = 1.08262668e-3;
export const J3 = -2.53265648e-6;
export const J4 = -1.61962159e-6;

/** Gravitational acceleration magnitude at radius r (m) -> m/s². */
export function gravityAccel(r, mu = MU_EARTH) { return mu / (r * r); }

/** Write the point-mass gravity acceleration vector for position (x,y[,z]) into out[0..2]. */
export function gravityVec(x, y, z, out, mu = MU_EARTH) {
  const r2 = x * x + y * y + z * z, r = Math.sqrt(r2), k = -mu / (r2 * r);
  out[0] = k * x; out[1] = k * y; out[2] = k * z;
  return out;
}

/**
 * Point-mass plus zonal J2–J4. z is the Earth pole.
 * Same closed form as Kestrel.Core.Physics.Geopotential.Accel.
 */
export function gravityZonal(x, y, z, out, mu = MU_EARTH, re = R_EARTH, j2 = J2, j3 = J3, j4 = J4) {
  const r2 = x * x + y * y + z * z;
  const r = Math.sqrt(r2);
  const r3 = r2 * r, r5 = r2 * r3, r7 = r5 * r2;
  const zr2 = (z * z) / r2, zr4 = zr2 * zr2;
  const re2 = re * re, re3 = re2 * re, re4 = re2 * re2;
  const pm = -mu / r3;
  const k2 = -1.5 * mu * j2 * re2 / r5;
  const k3 = -2.5 * mu * j3 * re3 / r7;
  const k4 = 1.875 * mu * j4 * re4 / r7;
  out[0] = pm * x + k2 * x * (1 - 5 * zr2) + k3 * x * z * (3 - 7 * zr2) + k4 * x * (1 - 14 * zr2 + 21 * zr4);
  out[1] = pm * y + k2 * y * (1 - 5 * zr2) + k3 * y * z * (3 - 7 * zr2) + k4 * y * (1 - 14 * zr2 + 21 * zr4);
  out[2] = pm * z + k2 * z * (3 - 5 * zr2) + k3 * (6 * z * z - 7 * z * z * zr2 - 0.6 * r2)
    + k4 * z * (5 - (70 / 3) * zr2 + 21 * zr4);
  return out;
}

/** First-order J2 nodal rate, deg/day. */
export function nodeRateDegPerDay(a, e, iDeg, mu = MU_EARTH, re = R_EARTH, j2 = J2) {
  const n = Math.sqrt(mu / a ** 3), p = a * (1 - e * e);
  const factor = 1.5 * n * j2 * (re / p) ** 2;
  return -factor * Math.cos(iDeg * Math.PI / 180) * 86400 * 180 / Math.PI;
}
