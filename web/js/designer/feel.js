/**
 * Designer feel. Haptic timings match data/designer-feel.json.
 * Stat deltas and the flight card are differences and pass-throughs of the
 * real readout (rocket equation + headless sim). Nothing here invents a number.
 */
export const DESIGNER_FEEL = {
  note: 'Designer feel for the Phaser web demo. Unity can reuse the haptic patterns. Live stats are not stored here — they are the rocket equation and the headless ascent sim.',
  haptics: {
    snap: [12],
    payoff: [18, 30, 18],
  },
};

export function hapticPattern(kind) {
  return DESIGNER_FEEL.haptics[kind] || DESIGNER_FEEL.haptics.snap;
}

/** Signed change between two quickReadout() results. */
export function previewDelta(prev, next) {
  if (!prev || !next) return null;
  return {
    deltaV: next.totalDeltaV - prev.totalDeltaV,
    twr: next.liftoffTWR - prev.liftoffTWR,
    mass: next.liftoffMass - prev.liftoffMass,
    margin: next.margin - prev.margin,
  };
}

/** Flight card. Every field is copied from simReadout / quickReadout. */
export function flightResult(sim, quick) {
  if (!sim) return null;
  return {
    outcome: sim.outcome,
    apogeeKm: sim.apogeeKm,
    perigeeKm: sim.perigeeKm,
    totalLosses: sim.totalLosses,
    maxQ: sim.maxQ,
    maxG: sim.maxG,
    deltaVExpended: sim.deltaVExpended,
    idealDeltaV: quick?.totalDeltaV ?? sim.idealDeltaV ?? null,
  };
}

export function pulseHaptic(pattern) {
  try {
    if (typeof navigator !== 'undefined' && typeof navigator.vibrate === 'function') {
      navigator.vibrate(pattern);
    }
  } catch { /* no vibrator */ }
}
