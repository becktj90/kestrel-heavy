// Rocket equation helpers. SI units; Isp in seconds.
import { G0, P0 } from './atmosphere.js';
export { G0 };

/** Tsiolkovsky Δv = Isp·g0·ln(m0/mf) (m/s). */
export function deltaV(isp, m0, mf) { return isp * G0 * Math.log(m0 / mf); }

/** Mass flow ṁ = F/(Isp·g0) (kg/s). */
export function massFlow(thrust, isp) { return thrust / (isp * G0); }

/**
 * Isp at ambient pressure p (Pa), linear between vacuum (p=0) and sea level (p=P0).
 * Linear-in-p is exact for a fixed nozzle at constant ṁ (F = F_vac − A_e·p).
 * Extrapolates below sea-level Isp for p>P0; clamped at 0.
 */
export function ispAt(ispSL, ispVac, p) { return Math.max(0, ispVac - (ispVac - ispSL) * p / P0); }

/** Thrust at ambient pressure given vacuum thrust and the Isp pair (constant ṁ). */
export function thrustAt(thrustVac, ispSL, ispVac, p) { return thrustVac * ispAt(ispSL, ispVac, p) / ispVac; }

/** Burn time (s) of propellant mass at vacuum thrust/Isp. */
export function burnTime(propMass, thrustVac, ispVac) { return propMass / massFlow(thrustVac, ispVac); }
