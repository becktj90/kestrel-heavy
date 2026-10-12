// M2 designer: validation (hard blocks + warnings), KID "Make it fly", save shim.
const assert = require('node:assert');
const path = require('node:path');
const test = require('node:test');
const js = path.join(__dirname, '..', 'web', 'js');
const load = async () => ({
  ...(await import(path.join(js, 'designer/validate.js'))),
  ...(await import(path.join(js, 'designer/store.js'))),
  ...(await import(path.join(js, 'data/presets.js'))),
  ...(await import(path.join(js, 'vehicle/design.js'))),
});
const clone = (d) => JSON.parse(JSON.stringify(d));
const codes = (xs) => xs.map((x) => x.code);

test('every preset has no hard blocks', async () => {
  const { PRESETS, validateEngineering } = await load();
  for (const p of PRESETS) assert.deepStrictEqual(validateEngineering(p).blocks, [], p.id);
});

test('hard blocks: TWR ≤ 1, zero engines, zero propellant, impossible geometry', async () => {
  const { PRESET_BY_ID, validateEngineering } = await load();
  let d = clone(PRESET_BY_ID.scout); d.stages[0].engineCount = 2;
  assert.ok(codes(validateEngineering(d).blocks).includes('twr-low'));
  d = clone(PRESET_BY_ID.scout); d.stages[1].engineCount = 0;
  assert.ok(codes(validateEngineering(d).blocks).includes('zero-engines'));
  d = clone(PRESET_BY_ID.scout); d.stages[0].propMass = 0;
  assert.ok(codes(validateEngineering(d).blocks).includes('zero-propellant'));
  d = clone(PRESET_BY_ID.scout); d.stages[0].engineCount = 30;
  assert.ok(codes(validateEngineering(d).blocks).includes('engines-dont-fit'));
  d = clone(PRESET_BY_ID.scout); d.stages[1].diameter = 6;
  assert.ok(codes(validateEngineering(d).blocks).includes('top-heavy'));
  d = clone(PRESET_BY_ID.kid); d.payload = { type: 'crew-capsule', mass: 12000, orbit: 'leo400' }; d.fairing.length = 1;
  assert.ok(codes(validateEngineering(d).blocks).includes('payload-doesnt-fit'));
});

test('warnings: marginal TWR, weak upper stage, short margin, landing without hardware, heavy fairing', async () => {
  const { PRESET_BY_ID, validateEngineering } = await load();
  let d = clone(PRESET_BY_ID.scout); d.stages[0].engineCount = 5;
  let v = validateEngineering(d); assert.ok(v.ok); assert.ok(codes(v.warnings).includes('twr-marginal'), JSON.stringify(v.warnings));
  d = clone(PRESET_BY_ID['tpl-medium-kerolox']); d.stages[1].engine = 'exp-hydrolox'; d.stages[1].propellant = 'hydrolox';
  assert.ok(codes(validateEngineering(d).warnings).includes('weak-upper'));
  d = clone(PRESET_BY_ID.scout); d.payload.mass = 3000;
  assert.ok(codes(validateEngineering(d).warnings).includes('dv-margin'));
  d = clone(PRESET_BY_ID.kid); d.stages[0].landingPropMass = 10000;
  assert.ok(codes(validateEngineering(d).warnings).includes('landing-hardware'));
  d = clone(PRESET_BY_ID.scout); d.fairing.material = 'steel'; d.fairing.length = 8;
  assert.ok(codes(validateEngineering(d).warnings).includes('heavy-fairing'));
});

test('messages suggest rather than scold', async () => {
  const { PRESET_BY_ID, validateEngineering } = await load();
  const d = clone(PRESET_BY_ID.scout); d.stages[0].engineCount = 2; d.payload.mass = 3000; d.fairing.material = 'steel'; d.fairing.length = 9;
  const v = validateEngineering(d);
  for (const m of [...v.blocks, ...v.warnings].map((x) => x.msg)) {
    assert.ok(!/\b(wrong|bad|stupid|fail(ed)?|error|invalid)\b/i.test(m), m);
    assert.ok(/try|aim|add|help|gives|saves|would|usually/i.test(m), `suggests a next step: ${m}`);
  }
});

test('Make it fly yields a valid design with margin for every preset and for broken variants', async () => {
  const { PRESETS, makeItFly, validateEngineering, validateDesign, MIN_MARGIN } = await load();
  const breakers = [
    (d) => d,
    (d) => { d.stages[0].engineCount = 1; },
    (d) => { d.stages[0].propMass = 0; },
    (d) => { d.stages.at(-1).engineCount = 0; },
    (d) => { d.payload.mass *= 3; },
    (d) => { d.stages[0].engineCount = 40; },
    (d) => { d.stages[0].engine = 'exp-hydrolox'; },
    (d) => { d.stages[0].landingPropMass = 5000; d.landing.legs = false; },
  ];
  for (const p of PRESETS) for (const [bi, br] of breakers.entries()) {
    const d = clone(p); br(d);
    const { design, changes } = makeItFly(d);
    const v = validateEngineering(design);
    assert.deepStrictEqual(v.blocks, [], `${p.id}/#${bi}`);
    assert.deepStrictEqual(validateDesign(design).errors, [], `${p.id}/#${bi} schema`);
    assert.ok(v.readout.margin >= MIN_MARGIN, `${p.id}/#${bi} margin ${v.readout.margin}`);
    assert.ok(v.readout.liftoffTWR > 1.0, `${p.id}/#${bi} twr`);
    assert.ok(Array.isArray(changes));
  }
  // never mutates input
  const before = JSON.stringify(PRESETS[0]); makeItFly(PRESETS[0]); assert.strictEqual(JSON.stringify(PRESETS[0]), before);
});

test('save shim: put, list, get, duplicate, rename, delete', async () => {
  const { createShimStore, PRESET_BY_ID } = await load();
  let t = 1;
  const s = createShimStore(null, () => t++);
  const a = s.designs.put(PRESET_BY_ID.scout, { name: 'Mine' });
  assert.ok(a.ok && a.id);
  assert.strictEqual(s.designs.get(a.id).name, 'Scout');
  const b = s.designs.duplicate(a.id);
  assert.notStrictEqual(b.id, a.id);
  s.designs.rename(a.id, 'Renamed');
  const list = s.designs.list();
  assert.strictEqual(list.length, 2);
  assert.ok(list.some((m) => m.name === 'Renamed') && list.some((m) => m.name === 'Mine copy'));
  assert.ok(s.designs.delete(b.id));
  assert.strictEqual(s.designs.list().length, 1);
});
