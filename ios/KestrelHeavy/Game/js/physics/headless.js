// Headless 2-D (equatorial-plane, inertial, Earth-centred) ascent simulator.
// State y = [x, y, vx, vy, m] (m, m/s, kg). Fixed-step RK4. No per-step allocations.
import { createRK4 } from './integrator.js';
import { atmosphere, P0, G0 } from './atmosphere.js';
import { cdMach, refArea } from './aero.js';
import { MU_EARTH, R_EARTH } from './gravity.js';
import { siteRotationSpeed } from './earth.js';
import { orbit2D } from './orbit.js';
import { ispAt, massFlow, deltaV } from './rocket-eq.js';

/**
 * Normalise a plain stage list. Each stage:
 * { thrustSL, thrustVac (N), ispSL, ispVac (s), propMass, dryMass (kg), diameter (m), cd (subsonic Cd0) }
 * Stage 0 fires first. ṁ is derived from the vacuum pair; thrust at ambient pressure
 * is ṁ·g0·Isp(p). thrustSL is checked for consistency (warning) but not used directly.
 */
export function normalizeStages(stages) {
  return stages.map((s, i) => {
    const mdot = massFlow(s.thrustVac, s.ispVac);
    const impliedSL = mdot * G0 * s.ispSL;
    const warn = s.thrustSL > 0 && Math.abs(impliedSL - s.thrustSL) / s.thrustSL > 0.05
      ? `stage ${i + 1}: thrustSL ${s.thrustSL} N inconsistent with Isp ratio (implied ${impliedSL.toFixed(0)} N)` : null;
    return { ...s, cd: s.cd ?? 0.3, mdot, area: refArea(s.diameter), warn };
  });
}

/** Ideal (vacuum) Δv per stage and total for the stack plus payload. */
export function stackDeltaV(stages, payload = 0) {
  const st = normalizeStages(stages);
  const per = [];
  for (let i = 0; i < st.length; i++) {
    let above = payload;
    for (let j = i + 1; j < st.length; j++) above += st[j].propMass + st[j].dryMass;
    const m0 = above + st[i].propMass + st[i].dryMass;
    per.push(deltaV(st[i].ispVac, m0, m0 - st[i].propMass));
  }
  return { perStage: per, total: per.reduce((a, b) => a + b, 0) };
}

/**
 * Run an ascent. opts:
 *  payload (kg, 0), targetAlt (m, 200e3), latitude (deg, 28.5), dt (s, 0.05), tMax (s, 3000),
 *  kickSpeed (m/s, 60), kickAngle (deg, 4), vacuum (bool: no atmosphere/drag),
 *  rotate (bool, true: Earth rotation + co-rotating air), sampleEvery (s, 0 = none),
 *  cutoff (bool, true: stop upper-stage burn once periapsis is near target; false = burn to depletion).
 * Returns { outcome: 'orbit'|'suborbital'|'crash'|'timeout', time, events[], orbit, maxQ, maxQTime,
 *  maxG, losses:{gravity,drag,steering}, idealDeltaV, deltaVExpended, finalMass, samples[], warnings[] }.
 */
export function simulate(stagesIn, opts = {}) {
  const st = normalizeStages(stagesIn);
  const payload = opts.payload ?? 0, targetAlt = opts.targetAlt ?? 200e3;
  const dtNom = opts.dt ?? 0.05, tMax = opts.tMax ?? 3000;
  const kickSpeed = opts.kickSpeed ?? 60, kickAngle = (opts.kickAngle ?? 4) * Math.PI / 180;
  const vacuum = !!opts.vacuum, rotate = opts.rotate !== false;
  const sampleEvery = opts.sampleEvery ?? 0;
  const lat = opts.latitude ?? 28.5;

  let stageIdx = 0;
  const upperMass = new Float64Array(st.length + 1); // mass above and including stage i (wet)
  upperMass[st.length] = payload;
  for (let i = st.length - 1; i >= 0; i--) upperMass[i] = upperMass[i + 1] + st[i].propMass + st[i].dryMass;

  // Planar model: site speed reduced by cos(lat); launch due east.
  const vSite = rotate ? siteRotationSpeed(lat) : 0;
  const y = new Float64Array(5);
  y[0] = 0; y[1] = R_EARTH; y[2] = -vSite; y[3] = 0; y[4] = upperMass[0];
  // Planar rotation: site moves along -x at (0,R), i.e. prograde direction t̂ = (-y, x)/r.
  // For the co-rotating air we use ω_eff = vSite / R so air speed at the pad is zero.
  const omegaAir = rotate ? vSite / R_EARTH : 0;

  const atm = { T: 0, p: 0, rho: 0, a: 0 };
  // Per-step guidance output (set before each step, read inside deriv).
  let mode = 0; // 0 vertical, 1 kick, 2 gravity turn, 3 closed-loop, 4 coast
  let pitchCmd = Math.PI / 2; // above local horizontal, used in modes 1 and 3
  let thrusting = true;

  const deriv = (t, s, out) => {
    const x = s[0], yy = s[1], vx = s[2], vy = s[3], m = s[4];
    const r2 = x * x + yy * yy, r = Math.sqrt(r2), gk = -MU_EARTH / (r2 * r);
    let ax = gk * x, ay = gk * yy, mdot = 0;
    const ux = x / r, uy = yy / r, tx = -uy, ty = ux;
    const alt = r - R_EARTH;
    let p = 0, rho = 0, a = 340;
    if (!vacuum && alt < 300e3) { atmosphere(alt, atm); p = atm.p; rho = atm.rho; a = atm.a; }
    const avx = vx + omegaAir * yy, avy = vy - omegaAir * x; // air-relative velocity
    const vrel = Math.sqrt(avx * avx + avy * avy);
    if (rho > 0 && vrel > 0) {
      const S = st[Math.min(stageIdx, st.length - 1)];
      const D = 0.5 * rho * vrel * vrel * cdMach(vrel / a, S.cd) * S.area;
      ax -= D * avx / (vrel * m); ay -= D * avy / (vrel * m);
    }
    if (thrusting && stageIdx < st.length) {
      const S = st[stageIdx];
      const F = S.mdot * G0 * ispAt(S.ispSL, S.ispVac, p);
      let dx, dy;
      if (mode === 2 && vrel > 1) { dx = avx / vrel; dy = avy / vrel; }
      else { const c = Math.cos(pitchCmd), sn = Math.sin(pitchCmd); dx = c * tx + sn * ux; dy = c * ty + sn * uy; }
      ax += F * dx / m; ay += F * dy / m; mdot = S.mdot;
    }
    out[0] = vx; out[1] = vy; out[2] = ax; out[3] = ay; out[4] = -mdot;
  };

  const step = createRK4(5);
  const events = [], samples = [], warnings = st.filter((s) => s.warn).map((s) => s.warn);
  const orb = {};
  let t = 0, maxQ = 0, maxQTime = 0, maxG = 0, lossG = 0, lossD = 0, lossS = 0, dvExp = 0;
  let nextSample = 0, outcome = 'timeout', kickStart = -1, liftedOff = false;
  const stageEndMass = () => upperMass[stageIdx] - st[stageIdx].propMass;
  events.push({ t: 0, type: 'ignition', stage: 1 });

  while (t < tMax) {
    const x = y[0], yy = y[1], vx = y[2], vy = y[3], m = y[4];
    const r = Math.hypot(x, yy), alt = r - R_EARTH;
    const ux = x / r, uy = yy / r, tx = -uy, ty = ux;
    const vr = vx * ux + vy * uy, vt = vx * tx + vy * ty;
    const avx = vx + omegaAir * yy, avy = vy - omegaAir * x;
    const vrel = Math.hypot(avx, avy);

    // ---- guidance (once per step) ----
    if (thrusting) {
      if (stageIdx === 0) {
        if (mode === 0 && vrel >= kickSpeed) { mode = 1; kickStart = t; pitchCmd = Math.PI / 2 - kickAngle; }
        else if (mode === 1) {
          const vgam = Math.atan2(avx * ux + avy * uy, avx * tx + avy * ty);
          if (t - kickStart > 3 && vgam <= pitchCmd + 0.002) mode = 2;
        }
      } else {
        mode = 3;
        const g = MU_EARTH / (r * r) - vt * vt / r; // net downward accel
        const vrCmd = Math.max(-150, Math.min(250, (targetAlt - alt) / 40));
        const S = st[stageIdx];
        atmosphere(Math.min(alt, 300e3), atm);
        const aT = S.mdot * G0 * ispAt(S.ispSL, S.ispVac, vacuum ? 0 : atm.p) / m;
        const sinP = Math.max(-0.6, Math.min(0.95, (g + 0.03 * (vrCmd - vr)) / aT));
        pitchCmd = Math.asin(sinP);
      }
    }

    // ---- step size: land exactly on burnout ----
    let dt = dtNom;
    if (thrusting && stageIdx < st.length) {
      const remain = m - stageEndMass();
      if (remain <= 1e-6) {
        // burnout -> staging
        events.push({ t, type: 'burnout', stage: stageIdx + 1, alt, v: Math.hypot(vx, vy) });
        stageIdx++;
        if (stageIdx >= st.length) { thrusting = false; mode = 4; continue; }
        y[4] = upperMass[stageIdx];
        events.push({ t, type: 'separation', stage: stageIdx, alt });
        events.push({ t, type: 'ignition', stage: stageIdx + 1, alt });
        continue;
      }
      dt = Math.min(dtNom, remain / st[stageIdx].mdot);
    }

    // ---- diagnostics (before step) ----
    let p = 0, rho = 0;
    if (!vacuum && alt < 300e3) { atmosphere(alt, atm); p = atm.p; rho = atm.rho; }
    const q = 0.5 * rho * vrel * vrel;
    if (q > maxQ) { maxQ = q; maxQTime = t; }
    const gl = MU_EARTH / (r * r);
    const v = Math.hypot(vx, vy);
    if (thrusting && stageIdx < st.length) {
      const S = st[stageIdx];
      const F = S.mdot * G0 * ispAt(S.ispSL, S.ispVac, p);
      const aT = F / m; dvExp += aT * dt;
      let dx, dy;
      if (mode === 2 && vrel > 1) { dx = avx / vrel; dy = avy / vrel; }
      else { const c = Math.cos(pitchCmd), sn = Math.sin(pitchCmd); dx = c * tx + sn * ux; dy = c * ty + sn * uy; }
      if (v > 1) lossS += aT * (1 - (dx * vx + dy * vy) / v) * dt;
      const Dm = rho > 0 ? 0.5 * rho * vrel * vrel * cdMach(vrel / (atm.a || 340), S.cd) * S.area / m : 0;
      lossD += Dm * dt;
      if (v > 1) lossG += gl * (vr / v) * dt;
      const gload = (aT - Dm) / G0; if (gload > maxG) maxG = gload;
    }
    if (sampleEvery > 0 && t >= nextSample) {
      samples.push({ t, alt, v, vrel, q, m, stage: stageIdx + 1, pitch: pitchCmd, mode });
      nextSample += sampleEvery;
    }

    step(deriv, t, y, dt);
    t += dt;

    const r1 = Math.hypot(y[0], y[1]), alt1 = r1 - R_EARTH;
    if (!liftedOff && alt1 > 1) { liftedOff = true; events.push({ t, type: 'liftoff' }); }
    if (alt1 < 0) {
      if (!liftedOff && thrusting) { y[0] = 0; y[1] = R_EARTH; y[2] = -vSite; y[3] = 0; continue; } // on pad, TWR<1
      outcome = 'crash'; events.push({ t, type: 'impact' }); break;
    }
    if (thrusting && mode === 3 && opts.cutoff !== false) {
      orbit2D(y[0], y[1], y[2], y[3], MU_EARTH, orb);
      if (orb.periAlt >= Math.min(targetAlt, alt1) - 10e3 && orb.e < 1) {
        thrusting = false; mode = 4;
        events.push({ t, type: 'cutoff', stage: stageIdx + 1, alt: alt1 });
      }
    }
    if (!thrusting) { break; }
  }

  orbit2D(y[0], y[1], y[2], y[3], MU_EARTH, orb);
  if (outcome === 'timeout' && !thrusting) outcome = orb.periAlt > 120e3 ? 'orbit' : 'suborbital';
  if (outcome === 'orbit') events.push({ t, type: 'orbit', apoAlt: orb.apoAlt, periAlt: orb.periAlt });
  const idealDeltaV = stackDeltaV(stagesIn, payload).total;
  return {
    outcome, time: t, events, orbit: { ...orb }, maxQ, maxQTime, maxG,
    losses: { gravity: lossG, drag: lossD, steering: lossS },
    idealDeltaV, deltaVExpended: dvExp, finalMass: y[4],
    state: { x: y[0], y: y[1], vx: y[2], vy: y[3], m: y[4] }, samples, warnings,
  };
}

/** Generic two-stage medium-lift archetype (kerosene/LOX, ~550 t GLOW). */
export const MEDIUM_LIFTER = Object.freeze([
  { thrustSL: 7.6e6, thrustVac: 8.23e6, ispSL: 282, ispVac: 311, propMass: 411000, dryMass: 25600, diameter: 3.7, cd: 0.3 },
  { thrustSL: 0, thrustVac: 981000, ispSL: 200, ispVac: 348, propMass: 107500, dryMass: 4500, diameter: 3.7, cd: 0.3 },
]);
