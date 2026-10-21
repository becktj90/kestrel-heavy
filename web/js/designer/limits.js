// Designer thresholds shared by validation and the params registry.
// Engines fit if Σ(nozzle d²) ≤ PACK·D² (loose circle packing incl. some overhang).
export const MIN_TWR = 1.0;
export const GOOD_TWR = 1.25;
export const MIN_MARGIN = 0.05;
export const PACK = 1.1;
// Most landing reserve a stage may hold, as a fraction of its propellant (data/designer-limits.json landingReserveFraction.max).
export const LANDING_RESERVE_MAX = 0.3;
