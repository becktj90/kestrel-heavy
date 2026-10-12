// T1: forking a starter template goes through save/store.js duplicate and never overwrites it.
const assert = require('node:assert');
const path = require('node:path');
const test = require('node:test');
const js = path.join(__dirname, '..', 'web', 'js');
const load = async () => ({
  ...(await import(path.join(js, 'save/store.js'))),
  ...(await import(path.join(js, 'designer/templates-fork.js'))),
  ...(await import(path.join(js, 'data/templates/index.js'))),
  ...(await import(path.join(js, 'vehicle/design.js'))),
});
const mem = () => { const m = new Map(); return { getItem: (k) => (m.has(k) ? m.get(k) : null), setItem: (k, v) => m.set(k, String(v)), removeItem: (k) => m.delete(k), key: (i) => [...m.keys()][i] ?? null, get length() { return m.size; } }; };

test('fork = duplicate into a new id, tagged forkedFrom, no template record left behind', async () => {
  const { createStore, forkTemplate, TEMPLATE_BY_ID, validateDesign } = await load();
  const store = createStore({ storage: mem(), bridge: null });
  let dups = 0; const orig = store.designs.duplicate; store.designs.duplicate = (...a) => { dups++; return orig(...a); };
  const tpl = TEMPLATE_BY_ID['tpl-medium-kerolox'];
  const before = JSON.stringify(tpl);
  const a = forkTemplate(store, tpl), b = forkTemplate(store, tpl);
  assert.strictEqual(dups, 2);
  assert.notStrictEqual(a.id, b.id);
  assert.notStrictEqual(a.id, tpl.id);
  assert.strictEqual(JSON.stringify(tpl), before, 'template object untouched');
  assert.strictEqual(store.designs.list().length, 2, 'only the two forks are saved');
  const saved = store.designs.get(a.id);
  assert.deepStrictEqual(saved.meta.forkedFrom, { templateId: tpl.id, templateVersion: tpl.template.version });
  assert.strictEqual(saved.template, undefined, 'template block not copied into the player design');
  assert.deepStrictEqual(validateDesign(saved).errors, []);
  assert.deepStrictEqual(saved.stages, tpl.stages);
  saved.stages[0].engineCount = 7; store.designs.put(saved, { id: a.id });
  assert.strictEqual(store.designs.get(b.id).stages[0].engineCount, 9);
  assert.strictEqual(tpl.stages[0].engineCount, 9);
});

test('continue-last returns the most recent design; blank is a valid one-stage design', async () => {
  const { createStore, forkTemplate, lastDesign, blankDesign, TEMPLATES, validateDesign } = await load();
  let t = 1000; const store = createStore({ storage: mem(), bridge: null, now: () => (t += 10) });
  assert.strictEqual(lastDesign(store), null);
  forkTemplate(store, TEMPLATES[0]);
  const b = forkTemplate(store, TEMPLATES[1]);
  assert.strictEqual(lastDesign(store).id, b.id);
  assert.deepStrictEqual(validateDesign(blankDesign()).errors, []);
});
