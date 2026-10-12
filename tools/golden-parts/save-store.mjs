// Save envelopes, design migration, base-36 ids, and the native key policy.
import { createStore } from '../../web/js/save/store.js';
import { migrateDesign, defaultDesign } from '../../web/js/vehicle/design.js';
import { checkKey, MAX_KEY_LENGTH, MAX_VALUE_BYTES } from '../../web/js/save/native-policy.js';

function memoryStorage() {
  const m = new Map();
  return {
    map: m,
    getItem: (k) => (m.has(k) ? m.get(k) : null),
    setItem: (k, v) => { m.set(k, String(v)); },
    removeItem: (k) => { m.delete(k); },
    key: (i) => [...m.keys()][i] ?? null,
    get length() { return m.size; },
  };
}

export async function register({ put, PRESETS }) {
  const byId = Object.fromEntries(PRESETS.map((p) => [p.id, p]));
  const mig = [];
  const add = (name, input) => {
    try { mig.push({ name, input, out: migrateDesign(input) }); }
    catch (e) { mig.push({ name, input, error: e.message }); }
  };
  add('empty', {});
  add('name-only', { name: 'Old' });
  add('version0', { version: 0, name: 'Z' });
  add('version1-extra', { version: 1, name: 'Stay', extra: true });
  add('version2', { version: 2 });
  add('version-half', { version: 1.5, name: 'Half' });
  add('negative', { version: -1 });
  add('preset-scout', byId.scout);
  add('default', defaultDesign());
  put('vehicle-migrate', { rel: 0, abs: 0, note: 'structural equality with migrateDesign(); v0 fills defaults, v1 is unchanged, newer throws' }, mig);

  const ids = [
    [1700000000000, 0.123456789],
    [1700000000000, 0.5],
    [1700000000000, 1 / 3],
    [0, 0],
    [35, 0.1],
    [36, 0.25],
    [1700000000001, 0.999999999],
  ].map(([now, random]) => ({ now, random, id: 'd' + now.toString(36) + random.toString(36).slice(2, 8) }));

  const keys = [
    'kh.save.v1.design.abc',
    'kh.save.v1.progress',
    'kh.save.',
    'kh.save.v1.design.bad key',
    'local.design',
    'kh.save.v1.design.' + 'a'.repeat(180),
    'kh.save.' + 'a'.repeat(MAX_KEY_LENGTH),
  ].map((key) => ({ key, ok: checkKey(key) }));

  let clock = 1700000000000;
  const randoms = [0.123456789, 0.5, 1 / 3];
  let ri = 0;
  const origNow = Date.now, origRandom = Math.random;
  Date.now = () => { const t = clock; clock += 1000; return t; };
  Math.random = () => { if (ri >= randoms.length) throw new Error('random exhausted'); return randoms[ri++]; };
  let snapshot, list, progress, got;
  try {
    const storage = memoryStorage();
    const s = createStore({ storage, bridge: { available: () => false } });
    const scout = JSON.parse(JSON.stringify(byId.scout));
    delete scout.id;
    const a = s.designs.put(scout, { name: 'Mine' });
    const b = s.designs.duplicate(a.id);
    s.designs.rename(a.id, 'Renamed');
    s.progress.put({ orbitsCompleted: 1, engineerEnabled: false });
    s.designs.delete(b.id);
    s.designs.put({ name: 'Old' }, { id: 'legacy' });
    snapshot = {};
    for (const [k, v] of storage.map) if (!k.endsWith('.tmp')) snapshot[k] = JSON.parse(v);
    list = s.designs.list();
    progress = s.progress.get();
    got = s.designs.get('legacy');
  } finally {
    Date.now = origNow;
    Math.random = origRandom;
  }

  put('save-store', { rel: 0, abs: 0, note: 'envelopes from createStore with a fixed clock; ids are JS toString(36)' }, [
    { kind: 'ids', cases: ids },
    { kind: 'keys', maxKeyLength: MAX_KEY_LENGTH, maxValueBytes: MAX_VALUE_BYTES, cases: keys },
    { kind: 'session', clockStart: 1700000000000, clockStep: 1000, randoms, snapshot, list, progress, legacy: got },
  ]);
}
