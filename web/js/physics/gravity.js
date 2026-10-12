// Point-mass Newtonian gravity, g = μ/r². SI units. (J2 not modeled in M1.)
export const MU_EARTH = 3.986004418e14; // m³/s² (WGS-84 / IERS)
export const R_EARTH = 6378137;         // m, equatorial (WGS-84)

/** Gravitational acceleration magnitude at radius r (m) -> m/s². */
export function gravityAccel(r, mu = MU_EARTH) { return mu / (r * r); }

/** Write the gravity acceleration vector for position (x,y[,z]) into out[0..2]. */
export function gravityVec(x, y, z, out, mu = MU_EARTH) {
  const r2 = x * x + y * y + z * z, r = Math.sqrt(r2), k = -mu / (r2 * r);
  out[0] = k * x; out[1] = k * y; out[2] = k * z;
  return out;
}
