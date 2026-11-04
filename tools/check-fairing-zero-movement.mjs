// Zero-movement gate for "Fairing joint and flare" phase 1: the flare advisory, the shoulder render and the
// fairing-narrow wording must not move a Cd, a mass, a payload fit, a readout or a sim stage for any shipped design.
// Usage: node tools/check-fairing-zero-movement.mjs <baseRepoRoot> [headRepoRoot=.]
// It imports web/js from both trees, snapshots every shipped design (board starters, campaign winners, designer
// templates, presets, golden campaign/contract designs), and diffs the snapshots. The only allowed difference is
// the new 'fairing-flare' warning code, which is filtered out (and reported if it ever appears on shipped data).
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

const baseRoot = path.resolve(process.argv[2] || '');
const headRoot = path.resolve(process.argv[3] || '.');
if (!process.argv[2]) { console.error('usage: check-fairing-zero-movement.mjs <baseRoot> [headRoot]'); process.exit(2); }

const FILES = ['board-starters', 'golden/campaign-viable', 'golden/contracts-viable', 'campaign-winners', 'designer-templates', 'presets'];

function designsOf(root) {
  const out = [];
  const walk = (o, label) => {
    if (Array.isArray(o)) return o.forEach((v, i) => walk(v, `${label}/${i}`));
    if (o && typeof o === 'object') {
      if (Array.isArray(o.stages) && o.stages[0] && typeof o.stages[0] === 'object' && 'engineCount' in o.stages[0]) out.push({ label: `${label}:${o.id ?? ''}`, d: o });
      for (const [k, v] of Object.entries(o)) walk(v, `${label}/${k}`);
    }
  };
  for (const f of FILES) {
    const p = path.join(root, 'data', f + '.json');
    if (fs.existsSync(p)) walk(JSON.parse(fs.readFileSync(p, 'utf8')), f);
  }
  return out;
}

async function snapshot(root) {
  const imp = (rel) => import(pathToFileURL(path.join(root, 'web', 'js', rel)).href);
  const [stack, mass, val, ro, presets] = await Promise.all([imp('vehicle/stack.js'), imp('vehicle/mass.js'), imp('designer/validate.js'), imp('designer/readout.js'), imp('data/presets.js')]);
  const snap = {};
  const all = designsOf(root).concat(presets.PRESETS.map((p) => ({ label: `web-preset:${p.id}`, d: p })));
  const flare = [];
  for (const { label, d } of all) {
    const row = {};
    const tryIt = (k, f) => { try { row[k] = f(); } catch (e) { row[k] = `ERR ${e.message}`; } };
    tryIt('baseCd', () => stack.baseCd(d));
    tryIt('referenceDiameter', () => stack.referenceDiameter(d));
    tryIt('simStages', () => stack.toSimStages(d));
    tryIt('fairingMass', () => mass.fairingMass(d.fairing));
    tryIt('stageMasses', () => mass.stageMasses(d));
    tryIt('vehicleMass', () => mass.vehicleMass(d));
    tryIt('payloadVolume', () => val.payloadVolume(d));
    tryIt('payloadFit', () => val.payloadFit(d));
    tryIt('quick', () => ro.quickReadout(d));
    tryIt('validate', () => {
      const r = val.validateEngineering(d);
      const w = r.warnings.filter((x) => x.code !== 'fairing-flare');
      if (r.warnings.some((x) => x.code === 'fairing-flare')) flare.push(label);
      return { ok: r.ok, blocks: r.blocks, warnings: w, readout: r.readout };
    });
    snap[label] = row;
  }
  return { snap, flare };
}

const norm = (v) => JSON.stringify(v, (k, x) => (typeof x === 'number' ? Number(x.toPrecision(15)) : x));
const A = await snapshot(baseRoot);
const B = await snapshot(headRoot);
let moved = 0, compared = 0;
const labels = new Set([...Object.keys(A.snap), ...Object.keys(B.snap)]);
for (const l of labels) {
  const a = A.snap[l], b = B.snap[l];
  if (!a || !b) { console.log(`ONLY ONE SIDE: ${l}`); moved++; continue; }
  for (const k of new Set([...Object.keys(a), ...Object.keys(b)])) {
    compared++;
    let x = a[k], y = b[k];
    // The one allowed text change: the fairing-narrow wording (a block on invalid designs only; shipped designs have none).
    if (k === 'validate' && x && y && typeof x === 'object') {
      const strip = (r) => ({ ...r, blocks: r.blocks.map((i) => (i.code === 'fairing-narrow' ? { ...i, msg: '<wording>' } : i)) });
      x = strip(x); y = strip(y);
    }
    if (norm(x) !== norm(y)) { console.log(`MOVED ${l} :: ${k}`); moved++; }
  }
}
console.log(`designs ${labels.size}, fields compared ${compared}, moved ${moved}`);
console.log(`new fairing-flare advisory on shipped designs: ${B.flare.length ? B.flare.join(', ') : 'none'}`);
process.exit(moved || B.flare.length ? 1 : 0);
