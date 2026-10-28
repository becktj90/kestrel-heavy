// Sim-flight helpers for the DESIGN screen's LAUNCH button: pure functions over a
// headless-sim result (physics/headless.js) and a quick readout (designer/readout.js).
// No DOM here, so these are unit-testable in node. The UI in ui.js only renders them.

/** LAUNCH gating: the sim flight runs exactly when engineering validation passes. */
export function canLaunch(validation) {
  return !!validation && validation.ok === true && (!validation.blocks || validation.blocks.length === 0);
}

const EVENT_TEXT = {
  ignition: (e) => `Stage ${e.stage} ignition`,
  liftoff: () => 'Liftoff — clear of the tower',
  burnout: (e) => `Stage ${e.stage} burnout`,
  separation: (e) => `Stage ${e.stage} separation`,
  'fairing-jettison': (e) => `Fairing jettison at ${(e.alt / 1000).toFixed(0)} km (${(e.mass / 1000).toFixed(1)} t dropped)`,
  cutoff: (e) => `Engine cutoff (stage ${e.stage})`,
  orbit: (e) => `Orbit reached — apogee ${(e.apoAlt / 1000).toFixed(0)} km, perigee ${(e.periAlt / 1000).toFixed(0)} km`,
  impact: () => 'Impact — vehicle lost',
};

/** Readable flight log from sim events, in time order. */
export function flightLog(sim) {
  if (!sim?.events) return [];
  return sim.events.map((e) => ({ t: e.t, type: e.type, text: (EVENT_TEXT[e.type] || ((x) => x.type))(e) }));
}

/**
 * Telemetry frame at flight time t: the latest sample at or before t (first sample if t
 * precedes it). Samples come from simulate() with sampleEvery > 0.
 */
export function frameAt(samples, t) {
  if (!samples?.length) return null;
  let frame = samples[0];
  for (const s of samples) {
    if (s.t <= t) frame = s;
    else break;
  }
  return frame;
}

/**
 * Flown-vs-readout comparison. idealDeltaV comes from the sim itself (same serial stages
 * as the readout), so a mismatch here means the readout and the sim disagree — the thing
 * the Design Lab promises never happens. Tolerance: 10% per the Oct 11 spec.
 */
export function flownVsReadout(quick, sim) {
  if (!quick || !sim) return null;
  const idealDiffPct = quick.totalDeltaV > 0 ? ((sim.idealDeltaV - quick.totalDeltaV) / quick.totalDeltaV) * 100 : 0;
  return {
    outcome: sim.outcome,
    apogeeKm: sim.apogeeKm,
    perigeeKm: sim.perigeeKm,
    timeS: sim.time,
    deltaVExpended: sim.deltaVExpended,
    idealDeltaV: sim.idealDeltaV,
    readoutDeltaV: quick.totalDeltaV,
    idealDiffPct,
    idealMatchesReadout: Math.abs(idealDiffPct) <= 10,
    totalLosses: sim.totalLosses,
    maxQ: sim.maxQ,
    maxG: sim.maxG,
    fairingJettisoned: (sim.events || []).some((e) => e.type === 'fairing-jettison'),
    log: flightLog(sim),
  };
}
