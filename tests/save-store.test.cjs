// Save store + native bridge with mocked localStorage and kestrelSave handler.
const assert = require('node:assert');
const path = require('node:path');
const test = require('node:test');
const js = path.join(__dirname, '..', 'web', 'js');
const load = async () => ({
  ...(await import(path.join(js, 'save/store.js'))),
  ...(await import(path.join(js, 'save/bridge.js'))),
  ...(await import(path.join(js, 'vehicle/design.js'))),
});

function mockStorage({ quotaAfter = Infinity } = {}) {
  const m = new Map();
  return {
    m,
    getItem: (k) => (m.has(k) ? m.get(k) : null),
    setItem(k, v) {
      if (!m.has(k) && m.size >= quotaAfter) { const e = new Error('full'); e.name = 'QuotaExceededError'; e.code = 22; throw e; }
      m.set(k, String(v));
    },
    removeItem: (k) => { m.delete(k); },
    key: (i) => [...m.keys()][i] ?? null,
    get length() { return m.size; },
  };
}
function mockWin({ reply = true, fail = false } = {}) {
  const disk = new Map(); const calls = [];
  const postMessage = (msg) => {
    calls.push(msg);
    if (fail) throw new Error('native down');
    let v = null;
    if (msg.op === 'put') disk.set(msg.key, msg.value);
    if (msg.op === 'delete') disk.delete(msg.key);
    if (msg.op === 'get') v = disk.has(msg.key) ? disk.get(msg.key) : null;
    if (msg.op === 'list') v = [...disk.keys()].filter((k) => k.startsWith(msg.key));
    return reply ? Promise.resolve(v) : undefined;
  };
  return { disk, calls, win: { webkit: { messageHandlers: { kestrelSave: { postMessage } } } } };
}

test('CRUD, duplicate, rename with localStorage only', async () => {
  const { createStore, createBridge, defaultDesign } = await load();
  const s = createStore({ storage: mockStorage(), bridge: createBridge({ win: {} }) });
  const { id, local } = s.designs.put(defaultDesign(), { name: 'Alpha' });
  assert.ok(local);
  assert.strictEqual(s.designs.get(id).version, 1);
  const dup = s.designs.duplicate(id);
  s.designs.rename(dup.id, 'Beta');
  assert.deepStrictEqual(s.designs.list().map((d) => d.name).sort(), ['Alpha', 'Beta']);
  assert.ok(s.designs.delete(id));
  assert.strictEqual(s.designs.get(id), null);
  s.progress.put({ cash: 5 });
  assert.deepStrictEqual(s.progress.get(), { cash: 5 });
});

test('loads migrate pre-versioned designs', async () => {
  const { createStore, createBridge, DESIGN_PREFIX } = await load();
  const st = mockStorage();
  st.setItem(DESIGN_PREFIX + 'old', JSON.stringify({ schema: 1, kind: 'design', id: 'old', name: 'Old', updatedAt: 1, data: { name: 'Old' } }));
  const s = createStore({ storage: st, bridge: createBridge({ win: {} }) });
  assert.strictEqual(s.designs.get('old').version, 1);
});

test('atomic write leaves no temp key and recovers interrupted swap', async () => {
  const { createStore, createBridge, defaultDesign, DESIGN_PREFIX } = await load();
  const st = mockStorage();
  const s = createStore({ storage: st, bridge: createBridge({ win: {} }) });
  const { id } = s.designs.put(defaultDesign(), { name: 'A' });
  assert.ok(![...st.m.keys()].some((k) => k.endsWith('.tmp')));
  const json = st.getItem(DESIGN_PREFIX + id);
  st.removeItem(DESIGN_PREFIX + id); st.setItem(DESIGN_PREFIX + id + '.tmp', json);
  assert.ok(s.designs.get(id));
  assert.ok(st.getItem(DESIGN_PREFIX + id) && !st.getItem(DESIGN_PREFIX + id + '.tmp'));
});

test('bridge mirrors {op,key,value} and hydrate restores evicted saves', async () => {
  const { createStore, createBridge, defaultDesign } = await load();
  const n = mockWin();
  const s = createStore({ storage: mockStorage(), bridge: createBridge({ win: n.win }) });
  const { id } = s.designs.put(defaultDesign(), { name: 'Mirror' });
  await s.flush();
  assert.deepStrictEqual(Object.keys(n.calls[0]).sort(), ['key', 'op', 'value']);
  assert.strictEqual(n.calls[0].op, 'put');
  const fresh = createStore({ storage: mockStorage(), bridge: createBridge({ win: n.win }) });
  assert.strictEqual(fresh.designs.get(id), null);
  assert.deepStrictEqual(await fresh.hydrate(), { restored: 1 });
  assert.strictEqual(fresh.designs.getMeta(id).name, 'Mirror');
  s.designs.delete(id); await s.flush();
  assert.ok(!n.disk.has(id));
});

test('quota: tolerated with native mirror, SaveError without', async () => {
  const { createStore, createBridge, defaultDesign, SaveError } = await load();
  const n = mockWin();
  const s = createStore({ storage: mockStorage({ quotaAfter: 0 }), bridge: createBridge({ win: n.win }) });
  const r = s.designs.put(defaultDesign());
  assert.strictEqual(r.local, false); assert.strictEqual(r.error, 'quota');
  assert.strictEqual((await r.native).ok, true);
  const s2 = createStore({ storage: mockStorage({ quotaAfter: 0 }), bridge: createBridge({ win: {} }) });
  assert.throws(() => s2.designs.put(defaultDesign()), (e) => e instanceof SaveError && e.code === 'quota');
});

test('bridge failures fall back to localStorage and never throw', async () => {
  const { createStore, createBridge, defaultDesign } = await load();
  for (const opts of [{ fail: true }, { reply: false }]) {
    const n = mockWin(opts);
    const b = createBridge({ win: n.win });
    const s = createStore({ storage: mockStorage(), bridge: b });
    const r = s.designs.put(defaultDesign());
    assert.ok(r.local && s.designs.get(r.id));
    const nr = await r.native;
    assert.strictEqual(nr.ok, !opts.fail);
  }
  assert.deepStrictEqual(await createBridge({ win: {} }).put('k', 'v'), { ok: false, error: 'unavailable' });
});
