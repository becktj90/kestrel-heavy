#!/usr/bin/env node
// #341: parity vectors for the closed-loop guidance computer (JS web/js/physics/headless.js vs C# AscentSim). Not a
// golden: data/golden stays as it is. tests/closed-loop-parity.test.cjs regenerates these and fails on drift;
// ClosedLoopParityTests (C#) flies the same inputs and must match at the simulate() tolerance (SIMTOL).
// Usage: node tools/export-closed-loop-parity.mjs
import { writeFileSync } from 'node:fs';
import { simulate, MEDIUM_LIFTER } from '../web/js/physics/headless.js';
import { MU_EARTH, R_EARTH } from '../web/js/physics/gravity.js';
import { PRESETS } from '../web/js/data/presets.js';
import { serialStages, requiredDeltaV } from '../web/js/designer/readout.js';

export const SIMTOL = { rel: 1e-6, abs: 1e-6, note: 'same as data/golden/simulate.json: long integrations, libm may differ by 1 ulp between V8 and .NET' };
const energy = (apoAlt, periAlt) => -MU_EARTH / (2 * R_EARTH + apoAlt + periAlt);
const fin = (o) => Object.fromEntries(Object.entries(o).map(([k, v]) => [k, Number.isFinite(v) ? v : String(v)]));
const pick = (s) => s.map(({ thrustSL, thrustVac, ispSL, ispVac, propMass, dryMass, diameter, cd }) => ({ thrustSL, thrustVac, ispSL, ispVac, propMass, dryMass, diameter, cd }));

export function parityCases() {
  const cases = [];
  const add = (name, stages, opts) => {
    const r = simulate(stages, opts);
    delete r.warnings;
    const s = r.state, rr = Math.hypot(s.x, s.y);
    const gammaDeg = Math.atan2((s.x * s.vx + s.y * s.vy) / rr, (s.x * s.vy - s.y * s.vx) / rr) * 180 / Math.PI;
    cases.push({ name, in: { stages, opts }, out: { ...r, orbit: fin(r.orbit), cutoffGammaDeg: gammaDeg } });
  };
  const ml = { payload: 10000, dt: 0.2, sampleEvery: 20, tMax: 2500 };
  add('medium-lifter-200-basic-energy', MEDIUM_LIFTER, { ...ml, targetAlt: 200e3, energyCutoff: true, cutoffEnergy: energy(200e3, 200e3) });
  add('medium-lifter-200-closed', MEDIUM_LIFTER, { ...ml, targetAlt: 200e3, energyCutoff: true, cutoffEnergy: energy(200e3, 200e3), closedLoop: true });
  add('medium-lifter-400-closed', MEDIUM_LIFTER, { ...ml, payload: 6000, targetAlt: 400e3, energyCutoff: true, cutoffEnergy: energy(400e3, 400e3), closedLoop: true });
  add('medium-lifter-ellipse-200x900-closed', MEDIUM_LIFTER, { ...ml, payload: 4000, targetAlt: 200e3, energyCutoff: true, cutoffEnergy: energy(900e3, 200e3), closedLoop: true, closedLoopAimAlt: 200e3 });
  add('medium-lifter-200-closed-relaxed', MEDIUM_LIFTER, { ...ml, targetAlt: 200e3, energyCutoff: true, cutoffEnergy: energy(200e3, 200e3), closedLoop: true, closedLoopRelaxPitch: true });
  add('medium-lifter-200-closed-dt005', MEDIUM_LIFTER, { ...ml, dt: 0.05, targetAlt: 200e3, energyCutoff: true, cutoffEnergy: energy(200e3, 200e3), closedLoop: true });
  for (const d of PRESETS) {
    const s = serialStages(d), req = requiredDeltaV(d.payload.orbit);
    if (s.stages.length < 2) continue;
    add(`preset-${d.id}-closed`, pick(s.stages), { payload: s.payload, targetAlt: req.targetAlt, latitude: 28.5, dt: 0.2, sampleEvery: 10, tMax: 2500,
      energyCutoff: true, cutoffEnergy: energy(req.targetAlt, req.targetAlt), closedLoop: true });
  }
  return cases;
}

export function parityFile() {
  const cases = parityCases();
  return { generatedBy: 'tools/export-closed-loop-parity.mjs (JS reference: web/js/physics/headless.js)', tolerance: SIMTOL, count: cases.length, cases };
}

if (import.meta.url === `file://${process.argv[1]}`) {
  writeFileSync('tests/fixtures/closed-loop-parity.json', JSON.stringify(parityFile(), null, 1) + '\n');
  console.log('wrote tests/fixtures/closed-loop-parity.json');
}
