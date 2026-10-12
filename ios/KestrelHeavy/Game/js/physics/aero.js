// Drag coefficient vs Mach for a slender launch vehicle, and drag force. SI units.
// Shape: flat subsonic Cd0, smooth rise from M_CRIT to a transonic peak at M_PEAK
// (peak = Cd0 * PEAK_FACTOR), then supersonic decay Cd0*(1 + (PEAK_FACTOR-1)*(M_PEAK/M)^2).
// This is an engineering fit, not CFD; see PHYSICS.md.
export const M_CRIT = 0.8, M_PEAK = 1.1, PEAK_FACTOR = 2.0;

/** Cd as a function of Mach number, scaled from subsonic Cd0. */
export function cdMach(mach, cd0 = 0.3) {
  const M = Math.abs(mach);
  if (M <= M_CRIT) return cd0;
  if (M < M_PEAK) {
    const s = (M - M_CRIT) / (M_PEAK - M_CRIT);
    return cd0 * (1 + (PEAK_FACTOR - 1) * s * s * (3 - 2 * s));
  }
  const r = M_PEAK / M;
  return cd0 * (1 + (PEAK_FACTOR - 1) * r * r);
}

/** Reference area (m²) of a circular cross-section of diameter d (m). */
export function refArea(d) { return Math.PI * d * d / 4; }

/** Dynamic pressure q = ½ρv² (Pa). */
export function dynamicPressure(rho, v) { return 0.5 * rho * v * v; }

/** Drag magnitude D = q·Cd(M)·A (N). */
export function dragForce(rho, v, soundSpeed, cd0, area) {
  return 0.5 * rho * v * v * cdMach(v / soundSpeed, cd0) * area;
}
