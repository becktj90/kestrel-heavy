// U.S. Standard Atmosphere 1976, 0–86 km geometric (layers by geopotential height),
// with an isothermal exponential tail above 86 km. SI units.
export const R_AIR = 287.053;      // J/(kg·K)
export const GAMMA_AIR = 1.4;
export const G0 = 9.80665;         // m/s²
export const R_EARTH_ISA = 6356766; // m, effective radius for geopotential conversion
export const P0 = 101325;          // Pa
export const T0 = 288.15;          // K
export const TAIL_SCALE_HEIGHT = 5800; // m (tail above 86 km; approximate)

// [base geopotential height m, base temperature K, lapse K/m, base pressure Pa]
const HB = [0, 11000, 20000, 32000, 47000, 51000, 71000, 84852];
const LB = [-0.0065, 0, 0.001, 0.0028, 0, -0.0028, -0.002, 0];
const TB = new Float64Array(8), PB = new Float64Array(8);
TB[0] = T0; PB[0] = P0;
for (let i = 1; i < 8; i++) {
  const dh = HB[i] - HB[i - 1], L = LB[i - 1];
  TB[i] = TB[i - 1] + L * dh;
  PB[i] = L === 0
    ? PB[i - 1] * Math.exp(-G0 * dh / (R_AIR * TB[i - 1]))
    : PB[i - 1] * Math.pow(TB[i] / TB[i - 1], -G0 / (R_AIR * L));
}
const Z86 = 86000;

/** Geometric altitude (m) -> geopotential altitude (m). */
export function geopotential(z) { return R_EARTH_ISA * z / (R_EARTH_ISA + z); }

/**
 * Fill `out` ({T,p,rho,a}) for geometric altitude z (m). Allocation-free if out given.
 * Below 0 m the troposphere formula is extrapolated.
 */
export function atmosphere(z, out = { T: 0, p: 0, rho: 0, a: 0 }) {
  let T, p;
  if (z > Z86) {
    T = TB[7];
    p = PB[7] * Math.exp(-(z - Z86) / TAIL_SCALE_HEIGHT);
  } else {
    const h = geopotential(z);
    let i = 0;
    while (i < 7 && h >= HB[i + 1]) i++;
    const L = LB[i], dh = h - HB[i];
    T = TB[i] + L * dh;
    p = L === 0 ? PB[i] * Math.exp(-G0 * dh / (R_AIR * TB[i])) : PB[i] * Math.pow(T / TB[i], -G0 / (R_AIR * L));
  }
  out.T = T; out.p = p; out.rho = p / (R_AIR * T); out.a = Math.sqrt(GAMMA_AIR * R_AIR * T);
  return out;
}

export function pressureAt(z) { return atmosphere(z, _scratch).p; }
export function densityAt(z) { return atmosphere(z, _scratch).rho; }
const _scratch = { T: 0, p: 0, rho: 0, a: 0 };
