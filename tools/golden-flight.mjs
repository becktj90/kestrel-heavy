// Catalog flight cases for every engine and every preset.
// Outputs come only from the JS reference (simulate, dragForce, orbitalElements, the mass model).
// Engine propellant mass is a fixture input solved so the stack lifts; it is not an expected value.
// Regeneration: npm run gen:golden
import { atmosphere } from '../web/js/physics/atmosphere.js';
import { cdMach, dragForce, dynamicPressure, refArea, M_PEAK } from '../web/js/physics/aero.js';
import { orbitalElements } from '../web/js/physics/orbit.js';
import { burnTime, G0 } from '../web/js/physics/rocket-eq.js';
import { simulate } from '../web/js/physics/headless.js';
import { R_EARTH } from '../web/js/physics/gravity.js';
import { defaultDesign, defaultStage, validateDesign } from '../web/js/vehicle/design.js';
import { serialStages, requiredDeltaV, LAUNCH_LATITUDE } from '../web/js/designer/readout.js';
import { ENGINES } from '../web/js/data/engines.js';
import { PRESETS } from '../web/js/data/presets.js';

/** Same geometric altitudes as the atmosphere golden file. */
export const ALTITUDE_GRID_M = Object.freeze([-500, 0, 1000, 5000, 11000, 15000, 20000, 25000, 32000, 40000, 47000, 51000, 60000, 71000, 80000, 84852, 86000, 90000, 100000, 150000, 250000]);

/** Catalog-flight integration step. max-Q time tolerance in docs/PHYSICS.md is this step. */
export const CATALOG_FLIGHT_DT = 0.2;

const KINDS = ['ascent', 'staging', 'drag', 'orbit'];
/** Fixture inputs only (not expected outputs). See docs/PHYSICS.md. */
const FIXTURE_LIFTOFF_TWR = 1.5;
const FIXTURE_MIN_PROP_KG = 1;
const FIXTURE_PROP_CAP_KG = 2_000_000;
const FIXTURE_MIN_CLIMB_M = 10; // sim counts liftoff at 1 m; keep the ideal first burn well above that
const ENGINE_COUNT_CAP = 40; // validateDesign engineCount maximum

const finDeep = (v) => {
  if (typeof v === 'number') return Number.isFinite(v) ? v : String(v);
  if (Array.isArray(v)) return v.map(finDeep);
  if (v && typeof v === 'object') return Object.fromEntries(Object.entries(v).map(([k, x]) => [k, finDeep(x)]));
  return v;
};

function plainStages(stages) {
  return stages.map((s) => ({
    thrustSL: s.thrustSL, thrustVac: s.thrustVac, ispSL: s.ispSL, ispVac: s.ispVac,
    propMass: s.propMass, dryMass: s.dryMass, diameter: s.diameter, cd: s.cd,
  }));
}

function designFor(engine, engineCount, propMass) {
  const seed = defaultStage();
  const base = defaultDesign();
  const diameter = Math.min(15, Math.max(seed.diameter, engine.diameter));
  const stage = (name) => defaultStage({
    name, propellant: engine.propellant, engine: engine.id, engineCount, propMass, diameter,
  });
  // No satellite: a 1500 kg default payload (and a long fairing) dominates a small engine and
  // leaves a propellant load that burns out before the 1 m liftoff trigger. Fairing length
  // equals the stage diameter already chosen above. Avionics stay on a catalog tier.
  return defaultDesign({
    name: engine.id,
    stages: [stage('Stage 1'), stage('Stage 2')],
    fairing: { ...base.fairing, diameter, length: diameter },
    payload: { ...base.payload, mass: 0 },
    avionics: { tier: 'basic' },
  });
}

function prepared(engine, engineCount, propMass) {
  const design = designFor(engine, engineCount, propMass);
  const valid = validateDesign(design);
  if (!valid.ok) throw new Error(`${engine.id}: ${valid.errors.join('; ')}`);
  const serial = serialStages(design);
  const stages = plainStages(serial.stages);
  const vacuum = engine.thrustSL <= 0 || engine.ispSL <= 0;
  const thrust = vacuum ? stages[0].thrustVac : stages[0].thrustSL;
  const m0 = serial.payload + stages.reduce((a, s) => a + s.propMass + s.dryMass, 0);
  return { stages, payload: serial.payload, vacuum, twr: thrust / (m0 * G0), diameter: stages[0].diameter };
}

/** Ideal vertical distance during the first burn at constant liftoff TWR. Drag ignored on purpose. */
function idealClimb(prep) {
  const s = prep.stages[0];
  // simulate() depletes propellant at the vacuum mass flow, including at sea level.
  const t = burnTime(s.propMass, s.thrustVac, s.ispVac);
  const accel = (prep.twr - 1) * G0;
  return 0.5 * Math.max(0, accel) * t * t;
}

/** Largest propellant load with liftoff TWR still at the target. Fixed-iteration binary search. */
function largestProp(engine, engineCount) {
  let lo = FIXTURE_MIN_PROP_KG;
  let hi = FIXTURE_MIN_PROP_KG;
  if (prepared(engine, engineCount, lo).twr < FIXTURE_LIFTOFF_TWR) return null;
  while (hi < FIXTURE_PROP_CAP_KG && prepared(engine, engineCount, hi).twr >= FIXTURE_LIFTOFF_TWR) {
    hi = Math.min(FIXTURE_PROP_CAP_KG, hi * 2);
  }
  for (let i = 0; i < 60; i++) {
    const mid = (lo + hi) / 2;
    if (prepared(engine, engineCount, mid).twr >= FIXTURE_LIFTOFF_TWR) lo = mid;
    else hi = mid;
  }
  return lo;
}

/** Solve fixture propellant with the real mass model. */
function engineFixture(engine) {
  const vacuum = engine.thrustSL <= 0 || engine.ispSL <= 0;
  let chosen = null;
  for (let n = 1; n <= ENGINE_COUNT_CAP; n++) {
    const propMass = largestProp(engine, n);
    if (propMass == null) continue;
    const solved = prepared(engine, n, propMass);
    if (idealClimb(solved) < FIXTURE_MIN_CLIMB_M) continue;
    chosen = { solved, propMass, engineCount: n };
    break;
  }
  if (!chosen) throw new Error(`${engine.id}: no cluster reaches TWR ${FIXTURE_LIFTOFF_TWR} and ${FIXTURE_MIN_CLIMB_M} m ideal climb`);
  const { solved, propMass, engineCount } = chosen;
  return {
    stages: solved.stages,
    payload: solved.payload,
    vacuum,
    fixture: {
      vacuum, engineCount, propMass, diameter: solved.diameter, nozzleDiameter: engine.diameter, liftoffTWR: solved.twr,
      twrTarget: FIXTURE_LIFTOFF_TWR, minClimbM: FIXTURE_MIN_CLIMB_M, idealClimbM: idealClimb(solved),
      note: 'Input fixture only. Smallest engine count whose largest propellant load at twrTarget still has an ideal first-burn climb of minClimbM. Payload mass is 0. Expected outputs are simulate / dragForce / orbitalElements.',
    },
  };
}

function flightOpts(payload, targetAlt, vacuum, stages) {
  const tBurn = stages.reduce((a, s) => a + burnTime(s.propMass, s.thrustVac, s.ispVac), 0);
  if (!Number.isFinite(tBurn) || tBurn <= 0) throw new Error(`non-positive burn time ${tBurn}`);
  return {
    payload, targetAlt, latitude: LAUNCH_LATITUDE, dt: CATALOG_FLIGHT_DT, sampleEvery: 1,
    tMax: Math.min(3600, Math.ceil(tBurn + 5)), vacuum, rotate: true, cutoff: true,
  };
}

function machPeak(cd0, area) {
  let best = null;
  for (const z of ALTITUDE_GRID_M) {
    if (!(z >= 0)) continue;
    const atm = atmosphere(z);
    const v = atm.a * M_PEAK;
    const q = dynamicPressure(atm.rho, v);
    if (!best || q > best.dynamicPressure) {
      best = {
        gridAltitudeM: z, mach: M_PEAK,
        dragForce: dragForce(atm.rho, v, atm.a, cd0, area),
        dynamicPressure: q,
        cd: cdMach(M_PEAK, cd0),
      };
    }
  }
  if (!best || !(best.dragForce > 0)) throw new Error('Mach-peak drag was not positive');
  return best;
}

function sampleDrag(samples, stages) {
  let peak = null;
  for (const s of samples) if (!peak || s.q > peak.q) peak = s;
  if (!peak) throw new Error('simulate returned no samples');
  const alt = Math.max(0, peak.alt);
  const atm = atmosphere(alt);
  const stage = stages[Math.min(Math.max(peak.stage, 1), stages.length) - 1];
  const area = refArea(stage.diameter);
  const vrel = peak.vrel;
  return {
    t: peak.t, alt: peak.alt, vrel, stage: peak.stage,
    dragForce: dragForce(atm.rho, vrel, atm.a, stage.cd, area),
    dynamicPressure: dynamicPressure(atm.rho, vrel),
    cd: cdMach(atm.a > 0 ? vrel / atm.a : 0, stage.cd),
  };
}

/**
 * Run one reference flight and split it into ascent, staging, drag/max-Q, and orbit determination.
 * @returns {{ascent: object, staging: object, drag: object, orbit: object, state: object}}
 */
export function evaluateFlight(stages, opts, nozzleDiameter = null) {
  const r = simulate(stages, opts);
  const finalAlt = Math.hypot(r.state.x, r.state.y) - R_EARTH;
  const finalV = Math.hypot(r.state.vx, r.state.vy);
  const elements = orbitalElements([r.state.x, r.state.y, 0], [r.state.vx, r.state.vy, 0]);
  const first = stages[0];
  return finDeep({
    ascent: {
      outcome: r.outcome, time: r.time, finalAlt, finalV,
      deltaVExpended: r.deltaVExpended, idealDeltaV: r.idealDeltaV, finalMass: r.finalMass, maxG: r.maxG,
      losses: { gravity: r.losses.gravity, drag: r.losses.drag, steering: r.losses.steering },
      state: { x: r.state.x, y: r.state.y, vx: r.state.vx, vy: r.state.vy, m: r.state.m },
    },
    staging: { events: r.events.map((e) => ({ ...e })) },
    drag: {
      maxQ: r.maxQ, maxQTime: r.maxQTime, dragLoss: r.losses.drag,
      sample: sampleDrag(r.samples, stages),
      machPeak: machPeak(first.cd, refArea(first.diameter)),
      // Nozzle exit from the catalog, so engines that share a stage diameter still differ.
      ...(nozzleDiameter > 0 ? { nozzleMachPeak: machPeak(first.cd, refArea(nozzleDiameter)) } : {}),
    },
    orbit: elements,
    state: { x: r.state.x, y: r.state.y, vx: r.state.vx, vy: r.state.vy },
  });
}

function assertFlew(id, vacuum, flight) {
  const types = new Set(flight.staging.events.map((e) => e.type));
  if (!types.has('liftoff')) throw new Error(`${id}: no liftoff (${flight.ascent.outcome})`);
  if (!types.has('burnout') && !types.has('separation') && !types.has('cutoff')) {
    throw new Error(`${id}: no staging event (${[...types].join(',')})`);
  }
  if (!vacuum && !(flight.drag.maxQ > 0)) throw new Error(`${id}: atmospheric flight produced no max-Q`);
}

function casesFor(subject, stages, opts, vacuum, fixture, nozzleDiameter = null) {
  const flight = evaluateFlight(stages, opts, nozzleDiameter);
  assertFlew(subject.id, vacuum, flight);
  const base = { subject, in: { stages, opts, ...(nozzleDiameter > 0 ? { nozzleDiameter } : {}) } };
  if (fixture) base.fixture = fixture;
  return [
    { ...base, kind: 'ascent', out: flight.ascent },
    { ...base, kind: 'staging', out: flight.staging },
    { ...base, kind: 'drag', out: flight.drag },
    {
      ...base, kind: 'orbit',
      in: { ...base.in, r: [flight.state.x, flight.state.y, 0], v: [flight.state.vx, flight.state.vy, 0] },
      out: flight.orbit,
    },
  ];
}

function presetTarget(design) {
  return requiredDeltaV(design.payload.orbit, LAUNCH_LATITUDE).targetAlt;
}

/** One ascent, staging, drag/max-Q, and orbit-determination case per engine and per preset. */
export function catalogFlightCases() {
  const cases = [];
  for (const engine of ENGINES) {
    const fx = engineFixture(engine);
    const targetAlt = requiredDeltaV(defaultDesign().payload.orbit, LAUNCH_LATITUDE).targetAlt;
    const opts = flightOpts(fx.payload, targetAlt, fx.vacuum, fx.stages);
    cases.push(...casesFor({ type: 'engine', id: engine.id }, fx.stages, opts, fx.vacuum, fx.fixture, engine.diameter));
  }
  for (const design of PRESETS) {
    const serial = serialStages(design);
    const stages = plainStages(serial.stages);
    const opts = flightOpts(serial.payload, presetTarget(design), false, stages);
    // Preset burns already use a 2500 s cap in simulate.json. Keep that cap when the
    // propellant-derived cap would be longer, and never cut a burn short of depletion.
    opts.tMax = Math.max(opts.tMax, 2500);
    cases.push(...casesFor({ type: 'preset', id: design.id }, stages, opts, false, null));
  }
  return cases;
}

export function assertCatalogCoverage(cases, engines = ENGINES, presets = PRESETS) {
  const missing = [];
  for (const engine of engines) {
    for (const kind of KINDS) if (!cases.some((c) => c.kind === kind && c.subject.type === 'engine' && c.subject.id === engine.id)) missing.push(`engine ${engine.id} ${kind}`);
  }
  for (const preset of presets) {
    for (const kind of KINDS) if (!cases.some((c) => c.kind === kind && c.subject.type === 'preset' && c.subject.id === preset.id)) missing.push(`preset ${preset.id} ${kind}`);
  }
  if (missing.length) throw new Error(`catalog flight coverage: ${missing.join('; ')}`);
}

export { KINDS };
