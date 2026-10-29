// Designer thresholds shared by validation and the params registry.
// Engines fit if Σ(nozzle d²) ≤ PACK·D² (loose circle packing incl. some overhang).
export const MIN_TWR = 1.0;
export const GOOD_TWR = 1.25;
export const MIN_MARGIN = 0.05;
export const PACK = 1.1;
// Most landing reserve a stage may hold, as a fraction of its propellant (data/designer-limits.json landingReserveFraction.max).
export const LANDING_RESERVE_MAX = 0.3;
// Fairing joint and flare (data/designer-limits.json fairingNarrowRatio and fairingFlare; DECISIONS #71). The fairing base is the top stage
// diameter at the joint, a conical shoulder of SHOULDER_ANGLE_DEG (at most SHOULDER_MAX_CYL_FRACTION of the cylinder) joins it to the barrel.
// The fairing-flare warning fires over FLARE_WARN_RATIO or a shoulder half angle over FLARE_WARN_ANGLE_DEG. Render and advice only.
export const FAIRING_NARROW_RATIO = 0.5;
export const FAIRING_FLARE_WARN_RATIO = 1.5;
export const FAIRING_FLARE_WARN_ANGLE_DEG = 30;
export const FAIRING_SHOULDER_ANGLE_DEG = 15;
export const FAIRING_SHOULDER_MAX_CYL_FRACTION = 0.5;
