#!/usr/bin/env node
// Headless ascent CLI.
//   node tools/sim.mjs                       # medium-lifter archetype, 10 t to 200 km
//   node tools/sim.mjs --payload 8000 --target 300000 --lat 28.5
//   node tools/sim.mjs --stages stages.json  # JSON array of stage objects (see PHYSICS.md)
//   add --json for machine-readable output, --samples 10 for a telemetry trace
import { readFileSync } from 'node:fs';
import { simulate, stackDeltaV, MEDIUM_LIFTER } from '../web/js/physics/headless.js';

const args = process.argv.slice(2);
const opt = (k, d) => { const i = args.indexOf(`--${k}`); return i >= 0 ? args[i + 1] : d; };
const stages = opt('stages') ? JSON.parse(readFileSync(opt('stages'), 'utf8')) : MEDIUM_LIFTER;
const o = { payload: +opt('payload', 10000), targetAlt: +opt('target', 200000), latitude: +opt('lat', 28.5),
  dt: +opt('dt', 0.05), sampleEvery: +opt('samples', 0) };
const r = simulate(stages, o);
if (args.includes('--json')) { console.log(JSON.stringify(r, null, 2)); process.exit(0); }
const km = (m) => (m / 1000).toFixed(1);
console.log(`Ideal Δv ${stackDeltaV(stages, o.payload).total.toFixed(0)} m/s  outcome: ${r.outcome}  t=${r.time.toFixed(1)} s`);
console.log(`Orbit: Ap ${km(r.orbit.apoAlt)} km  Pe ${km(r.orbit.periAlt)} km  e ${r.orbit.e.toFixed(4)}  a ${km(r.orbit.a)} km`);
console.log(`Max-Q ${(r.maxQ / 1000).toFixed(1)} kPa at T+${r.maxQTime.toFixed(1)} s  max accel ${r.maxG.toFixed(2)} g`);
console.log(`Losses: gravity ${r.losses.gravity.toFixed(0)}  drag ${r.losses.drag.toFixed(0)}  steering ${r.losses.steering.toFixed(0)} m/s`);
for (const e of r.events) console.log(`  T+${e.t.toFixed(1).padStart(6)}  ${e.type}${e.stage ? ' S' + e.stage : ''}${e.alt != null ? '  ' + km(e.alt) + ' km' : ''}`);
for (const s of r.samples) console.log(`  t ${s.t.toFixed(0).padStart(5)}  alt ${km(s.alt).padStart(7)} km  v ${s.v.toFixed(0).padStart(6)}  q ${s.q.toFixed(0).padStart(6)}  S${s.stage}`);
for (const w of r.warnings) console.warn('warning:', w);
