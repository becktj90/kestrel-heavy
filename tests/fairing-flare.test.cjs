// Fairing joint and flare, phase 1 (DECISIONS #71; data/designer-limits.json fairingFlare). Web twin of
// unity/Assets/Kestrel.Core.Tests/FairingFlareTests.cs; the texts match. Nothing here moves a mass, Cd, fit or flight number
// (tools/check-fairing-zero-movement.mjs proves that against the base commit).
const assert = require('node:assert');
const path = require('node:path');
const fs = require('node:fs');
const test = require('node:test');
const root = path.join(__dirname, '..');
const js = path.join(root, 'web', 'js');
const load = async () => ({
  ...(await import(path.join(js, 'designer/validate.js'))),
  ...(await import(path.join(js, 'designer/limits.js'))),
  ...(await import(path.join(js, 'data/presets.js'))),
  ...(await import(path.join(js, 'vehicle/stack.js'))),
});
const clone = (d) => JSON.parse(JSON.stringify(d));
const codes = (xs) => xs.map((x) => x.code);
const find = (xs, code) => xs.find((x) => x.code === code);
const top = (d) => d.stages[d.stages.length - 1].diameter;
const row = () => JSON.parse(fs.readFileSync(path.join(root, 'data', 'designer-limits.json'), 'utf8')).fairingFlare;
const FLARE = 'This fairing flares a lot above the stage. Try a wider stage or a smaller fairing.';

test('limits.js matches data/designer-limits.json', async () => {
  const L = await load();
  const r = row();
  assert.strictEqual(L.FAIRING_FLARE_WARN_RATIO, r.warnRatio);
  assert.strictEqual(L.FAIRING_FLARE_WARN_ANGLE_DEG, r.warnAngleDeg);
  assert.strictEqual(L.FAIRING_SHOULDER_ANGLE_DEG, r.shoulderAngleDeg);
  assert.strictEqual(L.FAIRING_SHOULDER_MAX_CYL_FRACTION, r.shoulderMaxCylFraction);
  assert.deepStrictEqual([r.warnRatio, r.warnAngleDeg, r.shoulderAngleDeg, r.shoulderMaxCylFraction], [1.5, 30, 15, 0.5]);
});

test('the shoulder is a 15 degree cone from the stage radius, limited to half the cylinder', async () => {
  const { fairingShoulderLength, fairingShoulderAngleDeg, fairingRadiusAt } = await load();
  const l = fairingShoulderLength(1.85, 2.15, 8);
  assert.ok(Math.abs(l - 0.3 / Math.tan(15 * Math.PI / 180)) < 1e-12);
  assert.ok(Math.abs(fairingShoulderAngleDeg(1.85, 2.15, l) - 15) < 1e-9);
  assert.strictEqual(fairingShoulderLength(1.85, 2.15, 1), 0.5, 'half of a 1 m cylinder');
  assert.ok(Math.abs(fairingShoulderAngleDeg(1.85, 2.15, 0.5) - Math.atan(0.6) * 180 / Math.PI) < 1e-9);
  assert.strictEqual(fairingShoulderLength(2, 2, 8), 0);
  assert.strictEqual(fairingShoulderLength(2, 2.5, 0), 0);
  const L = fairingShoulderLength(1.85, 2.6, 9);
  assert.strictEqual(fairingRadiusAt(1.85, 2.6, L, 0), 1.85, 'starts at the stage radius: no ledge');
  assert.ok(Math.abs(fairingRadiusAt(1.85, 2.6, L, L / 2) - 2.225) < 1e-12, 'linear');
  assert.strictEqual(fairingRadiusAt(1.85, 2.6, L, L), 2.6);
  assert.strictEqual(fairingRadiusAt(1.85, 2.6, L, L + 3), 2.6);
});

test('the flare predicate: ratio over 1.5 or shoulder angle over 30 degrees, flares only', async () => {
  const { fairingFlareExtreme } = await load();
  assert.strictEqual(fairingFlareExtreme(5.55, 3.7, 12, 'ogive'), false, 'exactly 1.5 does not warn');
  assert.strictEqual(fairingFlareExtreme(5.95, 3.7, 12, 'ogive'), true, 'r = 1.608');
  assert.strictEqual(fairingFlareExtreme(3.6, 4.1, 12, 'ogive'), false, 'narrower than the stage is not a flare');
  assert.strictEqual(fairingFlareExtreme(3.7, 3.7, 12, 'ogive'), false);
  assert.strictEqual(fairingFlareExtreme(5.4, 3.7, 4, 'ogive'), true, 'r = 1.46 but a short cylinder makes the shoulder ~37 degrees');
  assert.strictEqual(fairingFlareExtreme(5.4, 3.7, 12, 'ogive'), false, 'the same widths on a long fairing are a gentle 15 degrees');
});

test('r = 1.6 raises fairing-flare as a warning, never a block', async () => {
  const { PRESET_BY_ID, validateEngineering } = await load();
  const d = clone(PRESET_BY_ID['tpl-medium-kerolox']);
  d.fairing.diameter = Math.round(top(d) * 1.6 * 100) / 100;
  const r = validateEngineering(d);
  const w = find(r.warnings, 'fairing-flare');
  assert.ok(w, JSON.stringify(r.warnings));
  assert.strictEqual(w.tab, 'payload');
  assert.strictEqual(w.msg, FLARE);
  assert.ok(!codes(r.blocks).includes('fairing-flare'));
  assert.strictEqual(r.ok, true, JSON.stringify(r.blocks));
});

test('a shoulder over 30 degrees raises fairing-flare with r under 1.5, as a warning only', async () => {
  const { PRESET_BY_ID, validateEngineering } = await load();
  const d = clone(PRESET_BY_ID['tpl-medium-kerolox']); // top 3.7 m
  d.fairing.diameter = 5.4; d.fairing.length = 4;
  const r = validateEngineering(d);
  assert.ok(codes(r.warnings).includes('fairing-flare'));
  assert.ok(!codes(r.blocks).includes('fairing-flare'));
});

test('the advisory changes no block and no readout', async () => {
  const { PRESET_BY_ID, validateEngineering } = await load();
  const a = clone(PRESET_BY_ID['tpl-medium-kerolox']);
  const b = clone(a); b.fairing.diameter = top(b) * 1.6;
  assert.deepStrictEqual(codes(validateEngineering(a).blocks), codes(validateEngineering(b).blocks));
});

test('fairing-narrow still blocks, and its text names the fit rule', async () => {
  const { PRESET_BY_ID, validateEngineering, FIT_DIAMETER } = await load();
  const d = clone(PRESET_BY_ID['tpl-medium-kerolox']);
  d.fairing.diameter = top(d) * 0.4;
  const r = validateEngineering(d);
  const b = find(r.blocks, 'fairing-narrow');
  assert.ok(b, JSON.stringify(r.blocks));
  assert.strictEqual(r.ok, false);
  assert.match(b.msg, /less than half as wide as the top stage/);
  assert.match(b.msg, /0\.9/);
  assert.strictEqual(FIT_DIAMETER, 0.9, 'the rule the text quotes');
  d.fairing.diameter = top(d) * 0.5;
  assert.ok(!codes(validateEngineering(d).blocks).includes('fairing-narrow'), 'exactly half is allowed');
});

test('nothing that ships raises the flare advisory', async () => {
  const { PRESETS, validateEngineering, fairingFlareExtreme } = await load();
  for (const p of PRESETS) assert.ok(!codes(validateEngineering(p).warnings).includes('fairing-flare'), p.id);
  let n = 0;
  const flagged = [];
  const walk = (o, set) => {
    if (Array.isArray(o)) return o.forEach((v) => walk(v, set));
    if (o && typeof o === 'object') {
      if (Array.isArray(o.stages) && o.stages[0] && typeof o.stages[0] === 'object' && o.fairing && o.fairing.diameter) {
        n++;
        if (fairingFlareExtreme(o.fairing.diameter, top(o), o.fairing.length, o.aero && o.aero.nose)) flagged.push(`${set}:${o.id}`);
      }
      Object.values(o).forEach((v) => walk(v, set));
    }
  };
  for (const f of ['board-starters', 'golden/campaign-viable', 'golden/contracts-viable', 'campaign-winners', 'designer-templates', 'presets'])
    walk(JSON.parse(fs.readFileSync(path.join(root, 'data', f + '.json'), 'utf8')), f);
  assert.ok(n > 30, `walked ${n} designs`);
  // The one exception is a legacy flight-loop golden fixture, not a player design: a 1.2 m fairing on a 0.75 m stage (r = 1.6).
  // m1p-360 is a v1 flight fixture that is never designer-reachable (it fails the designer's schema, so the advisory is never shown for it).
  // The pin stays so a new shipped design over the threshold cannot slip in unnoticed.
  assert.deepStrictEqual(flagged, ['golden/campaign-viable:m1p-360']);
});
