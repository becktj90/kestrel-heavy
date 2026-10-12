// Versioned save store for designs and career progress.
// localStorage is the synchronous working copy; when the native kestrelSave
// bridge exists every write/delete is mirrored to disk on the iOS side, and
// hydrate() restores from native if WKWebView evicted localStorage.
// Writes are atomic: value -> `<key>.tmp`, verify, swap into `<key>`, drop tmp.
import { migrateDesign } from '../vehicle/design.js';
import { createBridge } from './bridge.js';

export const SAVE_SCHEMA = 1;
export const PREFIX = 'kh.save.v1.';
export const DESIGN_PREFIX = PREFIX + 'design.';
export const PROGRESS_KEY = PREFIX + 'progress';
const TMP = '.tmp';

export class SaveError extends Error {
  constructor(code, message) { super(message || code); this.name = 'SaveError'; this.code = code; }
}

export function isQuotaError(e) {
  if (!e) return false;
  return e.name === 'QuotaExceededError' || e.name === 'NS_ERROR_DOM_QUOTA_REACHED' || e.code === 22 || e.code === 1014;
}

function memoryStorage() {
  const m = new Map();
  return {
    getItem: (k) => (m.has(k) ? m.get(k) : null),
    setItem: (k, v) => { m.set(k, String(v)); },
    removeItem: (k) => { m.delete(k); },
    key: (i) => [...m.keys()][i] ?? null,
    get length() { return m.size; },
  };
}

function defaultStorage() {
  try {
    const ls = globalThis.localStorage;
    const t = '__kh_probe__'; ls.setItem(t, '1'); ls.removeItem(t);
    return ls;
  } catch (_) { return memoryStorage(); }
}

const newId = () => 'd' + Date.now().toString(36) + Math.random().toString(36).slice(2, 8);

export function createStore({ storage = defaultStorage(), bridge = createBridge(), now = () => Date.now() } = {}) {
  const pending = new Set();
  const track = (p) => { pending.add(p); p.finally(() => pending.delete(p)); return p; };
  const mirror = (op, key, value) => (bridge && bridge.available() ? track(bridge.post(op, key, value)) : null);

  function keys() {
    const out = [];
    for (let i = 0; i < storage.length; i++) { const k = storage.key(i); if (k) out.push(k); }
    return out;
  }
  function parse(raw) { try { return raw == null ? null : JSON.parse(raw); } catch (_) { return null; } }

  // Atomic local write. Throws SaveError('quota') / ('write').
  function writeLocal(key, json) {
    try {
      storage.setItem(key + TMP, json);
      if (storage.getItem(key + TMP) !== json) throw new SaveError('write', 'temp verify failed');
      storage.setItem(key, json);
      storage.removeItem(key + TMP);
    } catch (e) {
      try { storage.removeItem(key + TMP); } catch (_) { /* ignore */ }
      if (e instanceof SaveError) throw e;
      throw new SaveError(isQuotaError(e) ? 'quota' : 'write', String((e && e.message) || e));
    }
  }
  // Reads the record, recovering from an interrupted swap (tmp present, main bad).
  function readLocal(key) {
    const main = parse(storage.getItem(key));
    if (main) return main;
    const tmp = parse(storage.getItem(key + TMP));
    if (tmp) { try { storage.setItem(key, JSON.stringify(tmp)); storage.removeItem(key + TMP); } catch (_) { /* keep tmp */ } return tmp; }
    return null;
  }

  // Persist an envelope locally and to native. Local quota failure is tolerated
  // when the native mirror succeeds; the result reports both.
  function persist(key, env) {
    const json = JSON.stringify(env);
    let local = true, error = null;
    try { writeLocal(key, json); } catch (e) { local = false; error = e; }
    const native = mirror('put', key, json);
    if (!local && !native) throw error;
    return { ok: true, local, error: error && error.code, native: native || Promise.resolve({ ok: false, error: 'unavailable' }) };
  }
  function remove(key) {
    try { storage.removeItem(key); storage.removeItem(key + TMP); } catch (_) { /* ignore */ }
    mirror('delete', key, null);
  }

  const envelope = (kind, id, name, data, createdAt) =>
    ({ schema: SAVE_SCHEMA, kind, id, name, createdAt: createdAt ?? now(), updatedAt: now(), data });

  function loadDesignEnv(id) {
    const env = readLocal(DESIGN_PREFIX + id);
    if (!env || env.kind !== 'design') return null;
    try { return { ...env, data: migrateDesign(env.data) }; } catch (_) { return null; }
  }

  const designs = {
    list() {
      return keys()
        .filter((k) => k.startsWith(DESIGN_PREFIX) && !k.endsWith(TMP))
        .map((k) => readLocal(k))
        .filter((e) => e && e.kind === 'design')
        .map(({ id, name, createdAt, updatedAt }) => ({ id, name, createdAt, updatedAt }))
        .sort((a, b) => b.updatedAt - a.updatedAt);
    },
    get(id) { const e = loadDesignEnv(id); return e ? e.data : null; },
    getMeta(id) { const e = loadDesignEnv(id); return e ? { id: e.id, name: e.name, createdAt: e.createdAt, updatedAt: e.updatedAt } : null; },
    put(design, { id, name } = {}) {
      const data = migrateDesign(design);
      const sid = id || data.id || newId();
      const prev = readLocal(DESIGN_PREFIX + sid);
      const nm = name || (prev && prev.name) || data.name || 'Untitled';
      const r = persist(DESIGN_PREFIX + sid, envelope('design', sid, nm, data, prev && prev.createdAt));
      return { ...r, id: sid };
    },
    delete(id) { const had = !!readLocal(DESIGN_PREFIX + id); remove(DESIGN_PREFIX + id); return had; },
    duplicate(id, name) {
      const e = loadDesignEnv(id);
      if (!e) throw new SaveError('missing', `no design ${id}`);
      return designs.put(e.data, { id: newId(), name: name || `${e.name} copy` });
    },
    rename(id, name) {
      const e = readLocal(DESIGN_PREFIX + id);
      if (!e) throw new SaveError('missing', `no design ${id}`);
      return { ...persist(DESIGN_PREFIX + id, { ...e, name: String(name), updatedAt: now() }), id };
    },
  };

  const progress = {
    get() { const e = readLocal(PROGRESS_KEY); return e && e.kind === 'progress' ? e.data : null; },
    put(data) { const prev = readLocal(PROGRESS_KEY); return persist(PROGRESS_KEY, envelope('progress', 'progress', 'progress', data, prev && prev.createdAt)); },
    delete() { remove(PROGRESS_KEY); },
  };

  // Restore any native-only records into localStorage (e.g. after eviction).
  async function hydrate() {
    if (!bridge || !bridge.available()) return { restored: 0 };
    const r = await bridge.list(PREFIX);
    if (!r.ok || !Array.isArray(r.value)) return { restored: 0 };
    let restored = 0;
    for (const key of r.value) {
      if (typeof key !== 'string' || !key.startsWith(PREFIX) || storage.getItem(key) != null) continue;
      const g = await bridge.get(key);
      if (!g.ok || typeof g.value !== 'string' || !parse(g.value)) continue;
      try { writeLocal(key, g.value); restored++; } catch (_) { /* quota: native copy still exists */ }
    }
    return { restored };
  }

  return { designs, progress, hydrate, flush: () => Promise.all([...pending]), keys: () => keys().filter((k) => k.startsWith(PREFIX)) };
}
