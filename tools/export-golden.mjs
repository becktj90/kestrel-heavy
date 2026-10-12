#!/usr/bin/env node
// Export golden vectors (inputs + outputs of the JS reference implementation) to data/golden/*.json.
// Any port must reproduce them. Closed form and long simulate() runs use the tolerance recorded
// in each file (see data/golden/README.md). catalog-flight.json is per quantity: absolute and
// relative bands in docs/PHYSICS.md, applied by tests/golden-harness.test.cjs.
// Usage: npm run gen:golden        (tests/catalogs-json.test.cjs fails if output drifts)
import { writeFileSync, mkdirSync, readdirSync } from 'node:fs';
import { atmosphere, geopotential } from '../web/js/physics/atmosphere.js';
import { cdMach, dragForce } from '../web/js/physics/aero.js';
import { deltaV, massFlow, ispAt, thrustAt, burnTime } from '../web/js/physics/rocket-eq.js';
import { orbitalElements, circularVelocity } from '../web/js/physics/orbit.js';
import { gravityAccel } from '../web/js/physics/gravity.js';
import { siteRotationSpeed } from '../web/js/physics/earth.js';
import { simulate, stackDeltaV, MEDIUM_LIFTER } from '../web/js/physics/headless.js';
import { stageMasses, strapOnMasses, vehicleMass, fairingMass } from '../web/js/vehicle/mass.js';
import { toSimStages, baseCd, referenceDiameter, idealDeltaV } from '../web/js/vehicle/stack.js';
import { sizeTank } from '../web/js/vehicle/design.js';
import { serialStages, requiredDeltaV } from '../web/js/designer/readout.js';
import { priceDesign, engineDevCost, engineBatchCost, learningFactor } from '../web/js/economy/costModel.js';
import { reliabilityOf, clusterReliability, engineOutTolerance } from '../web/js/economy/reliability.js';
import { PRESETS } from '../web/js/data/presets.js';
import { ENGINES } from '../web/js/data/engines.js';
import { ALTITUDE_GRID_M, catalogFlightCases, assertCatalogCoverage } from './golden-flight.mjs';

const GEN = 'tools/export-golden.mjs (JS reference: web/js)';
const files = {};
const put = (name, tolerance, cases) => { files[name] = { generatedBy: GEN, tolerance, count: cases.length, cases }; };
const REL = { rel: 1e-9, abs: 1e-9, note: 'closed form: |a-b| <= abs + rel*|b|' };

// ---- physics-core ----
const alts = ALTITUDE_GRID_M;
put('atmosphere', REL, alts.map((z) => ({ fn: 'atmosphere', in: { z }, out: { ...atmosphere(z), h: geopotential(z) } })));
const tsi = [[311, 550000, 150000], [348, 120000, 12000], [452, 30000, 8000], [245, 1000, 600], [282, 1e6, 999999]];
put('rocket-eq', REL, [
  ...tsi.map(([isp, m0, mf]) => ({ fn: 'deltaV', in: { isp, m0, mf }, out: deltaV(isp, m0, mf) })),
  ...[[934e3, 311], [981e3, 348], [2.5e6, 350], [4500, 315]].map(([F, isp]) => ({ fn: 'massFlow', in: { thrust: F, isp }, out: massFlow(F, isp) })),
  ...[[282, 311, 0], [282, 311, 101325], [282, 311, 50000], [0, 348, 30000], [327, 363, 120000]].map(([a, b, p]) => ({ fn: 'ispAt', in: { ispSL: a, ispVac: b, p }, out: ispAt(a, b, p) })),
  ...[[934e3, 282, 311, 40000]].map(([F, a, b, p]) => ({ fn: 'thrustAt', in: { thrustVac: F, ispSL: a, ispVac: b, p }, out: thrustAt(F, a, b, p) })),
  { fn: 'burnTime', in: { propMass: 100000, thrustVac: 981e3, ispVac: 348 }, out: burnTime(100000, 981e3, 348) },
]);
put('aero-gravity-earth', REL, [
  ...[0, 0.5, 0.8, 0.9, 1.0, 1.1, 1.5, 3, 8].map((M) => ({ fn: 'cdMach', in: { mach: M, cd0: 0.34 }, out: cdMach(M, 0.34) })),
  { fn: 'dragForce', in: { rho: 0.4, v: 400, a: 300, cd0: 0.3, area: 10.75 }, out: dragForce(0.4, 400, 300, 0.3, 10.75) },
  ...[6378137, 6578137, 42164000].map((r) => ({ fn: 'gravityAccel', in: { r }, out: gravityAccel(r) })),
  ...[0, 28.5, 45, 51.6].map((lat) => ({ fn: 'siteRotationSpeed', in: { lat }, out: siteRotationSpeed(lat) })),
]);
const fin = (o) => Object.fromEntries(Object.entries(o).map(([k, v]) => [k, Number.isFinite(v) ? v : String(v)]));
put('orbit', REL, [
  { r: [0, 6578137, 0], v: [-7784.34, 0, 0] },
  { r: [6778137, 0, 0], v: [0, 7000, 2000] },
  { r: [6578137, 0, 0], v: [0, 10100, 0] },
  { r: [-3000000, 6000000, 1000], v: [-6000, -2500, 500] },
  { r: [6578137, 0, 0], v: [0, 12000, 0] },
].map((c) => ({ fn: 'orbitalElements', in: c, out: fin(orbitalElements(c.r, c.v)) })).concat(
  [0, 200e3, 400e3, 35786e3].map((alt) => ({ fn: 'circularVelocity', in: { alt }, out: circularVelocity(alt) }))));

// ---- vehicle ----
put('vehicle', REL, PRESETS.map((d) => ({
  preset: d.id, design: d,
  out: {
    stageMasses: stageMasses(d), strapOnMasses: strapOnMasses(d), vehicleMass: vehicleMass(d), fairingMass: fairingMass(d.fairing),
    baseCd: baseCd(d), referenceDiameter: referenceDiameter(d), toSimStages: toSimStages(d), idealDeltaV: idealDeltaV(toSimStages(d)),
    tanks: d.stages.map((s) => sizeTank(s.propellant, s.propMass, s.diameter)),
    serialStages: serialStages(d), requiredDeltaV: requiredDeltaV(d.payload.orbit), stackDeltaV: (() => { const s = serialStages(d); return stackDeltaV(s.stages, s.payload); })(),
  },
})));

// ---- simulate ----
const SIMTOL = { rel: 1e-6, abs: 1e-6, note: 'long integrations: libm (exp/pow/log/sin) may differ by 1 ulp between V8 and .NET; events must match exactly in type/stage/order, times within abs 1e-6 s' };
const simCase = (name, stages, opts) => {
  const r = simulate(stages, opts);
  delete r.warnings;
  return { name, in: { stages, opts }, out: { ...r, orbit: fin(r.orbit) } };
};
const sims = [
  simCase('medium-lifter-vacuum', MEDIUM_LIFTER, { payload: 10000, vacuum: true, rotate: false, cutoff: false, tMax: 2000, sampleEvery: 50 }),
  simCase('medium-lifter-200km', MEDIUM_LIFTER, { payload: 10000, targetAlt: 200e3, sampleEvery: 20 }),
  simCase('medium-lifter-overloaded', MEDIUM_LIFTER, { payload: 60000, sampleEvery: 20 }),
];
for (const d of PRESETS) {
  const s = serialStages(d), req = requiredDeltaV(d.payload.orbit);
  sims.push(simCase(`preset-${d.id}`, s.stages.map(({ thrustSL, thrustVac, ispSL, ispVac, propMass, dryMass, diameter, cd }) => ({ thrustSL, thrustVac, ispSL, ispVac, propMass, dryMass, diameter, cd })),
    { payload: s.payload, targetAlt: req.targetAlt, latitude: 28.5, dt: 0.2, sampleEvery: 10, tMax: 2500 }));
}
put('simulate', SIMTOL, sims);

// ---- economy ----
put('economy', { rel: 0, abs: 0, note: 'integer minor units: exact; reliability doubles: rel 1e-12', relReliability: 1e-12 }, [
  ...PRESETS.map((d) => ({ fn: 'priceDesign', preset: d.id, design: d, out: priceDesign(d) })),
  ...PRESETS.map((d) => ({ fn: 'priceDesign', preset: d.id, design: d, companyState: { unitsBuilt: { 'gg-kerolox': 40 }, devDone: ['gg-kerolox', 'avionics:standard'] }, out: priceDesign(d, { unitsBuilt: { 'gg-kerolox': 40 }, devDone: ['gg-kerolox', 'avionics:standard'] }) })),
  ...PRESETS.map((d) => ({ fn: 'reliabilityOf', preset: d.id, design: d, out: reliabilityOf(d) })),
  ...ENGINES.map((e) => ({ fn: 'engineDevCost', engine: e.id, out: engineDevCost(e) })),
  ...ENGINES.map((e) => ({ fn: 'engineBatchCost', engine: e.id, count: 9, built: 3, out: engineBatchCost(e, 9, 3) })),
  ...[1, 2, 10, 100].map((n) => ({ fn: 'learningFactor', n, out: learningFactor(n) })),
  ...[[9, 0.995], [33, 0.98], [1, 0.99]].map(([n, r]) => { const e = { cycle: 'gas-generator' }; const k = engineOutTolerance(n, e); return { fn: 'clusterReliability', n, r, k, out: clusterReliability(n, r, k) }; }),
]);

const flightCases = catalogFlightCases();
assertCatalogCoverage(flightCases);
put('catalog-flight', {
  doc: 'docs/PHYSICS.md',
  block: 'golden-tolerances',
  note: 'Per-quantity absolute and relative bands live in docs/PHYSICS.md. The golden harness reads that block. Regenerate with npm run gen:golden.',
}, flightCases);

const partsDir = new URL('./golden-parts/', import.meta.url);
let partFiles = [];
try { partFiles = readdirSync(partsDir).filter((f) => f.endsWith('.mjs')).sort(); } catch { partFiles = []; }
for (const f of partFiles) {
  const mod = await import(new URL(f, partsDir).href);
  if (typeof mod.register === 'function') await mod.register({ put, REL, PRESETS, ENGINES });
}

const oi = process.argv.indexOf('--out');
const dir = oi > 0 ? new URL(`file://${process.argv[oi + 1].replace(/\/?$/, '/')}`) : new URL('../data/golden/', import.meta.url);
mkdirSync(dir, { recursive: true });
let total = 0;
for (const [name, body] of Object.entries(files)) { writeFileSync(new URL(`${name}.json`, dir), JSON.stringify(body, null, 1) + '\n'); total += body.count; }
writeFileSync(new URL('index.json', dir), JSON.stringify({ generatedBy: GEN, files: Object.fromEntries(Object.entries(files).map(([k, v]) => [k, v.count])), total }, null, 1) + '\n');
console.log(`data/golden: ${Object.keys(files).length} files, ${total} vectors`);
