// Design persistence for the DESIGN screen. Uses web/js/save/store.js when present on main;
// otherwise a thin localStorage shim with the same designs interface
// (list / get / put / duplicate / rename / delete) and the same key prefix, so saves carry over.
const PREFIX = 'kh.save.v1.design.';

export function createShimStore(storage = globalThis.localStorage, now = () => Date.now()) {
  const mem = new Map();
  const st = storage || { getItem: (k) => mem.get(k) ?? null, setItem: (k, v) => mem.set(k, String(v)), removeItem: (k) => mem.delete(k), key: (i) => [...mem.keys()][i], get length() { return mem.size; } };
  const newId = () => 'd' + now().toString(36) + Math.random().toString(36).slice(2, 8);
  const read = (id) => { try { return JSON.parse(st.getItem(PREFIX + id)); } catch (_) { return null; } };
  const write = (env) => { st.setItem(PREFIX + env.id, JSON.stringify(env)); return { ok: true, local: true, id: env.id }; };
  const designs = {
    list() {
      const out = [];
      for (let i = 0; i < st.length; i++) { const k = st.key(i); if (k && k.startsWith(PREFIX) && !k.endsWith('.tmp')) { const e = read(k.slice(PREFIX.length)); if (e && e.kind === 'design') out.push({ id: e.id, name: e.name, createdAt: e.createdAt, updatedAt: e.updatedAt }); } }
      return out.sort((a, b) => b.updatedAt - a.updatedAt);
    },
    get(id) { const e = read(id); return e ? e.data : null; },
    put(design, { id, name } = {}) {
      const sid = id || newId(); const prev = read(sid);
      return write({ schema: 1, kind: 'design', id: sid, name: name || prev?.name || design.name || 'Untitled', createdAt: prev?.createdAt ?? now(), updatedAt: now(), data: JSON.parse(JSON.stringify(design)) });
    },
    duplicate(id, name) { const e = read(id); if (!e) throw new Error(`no design ${id}`); return designs.put(e.data, { id: newId(), name: name || `${e.name} copy` }); },
    rename(id, name) { const e = read(id); if (!e) throw new Error(`no design ${id}`); return write({ ...e, name: String(name), updatedAt: now() }); },
    delete(id) { const had = !!read(id); st.removeItem(PREFIX + id); return had; },
  };
  return { designs, shim: true };
}

let cached = null;
export async function getDesignStore() {
  if (cached) return cached;
  try {
    const mod = await import('../save/store.js');
    cached = mod.createStore();
  } catch (_) {
    cached = createShimStore();
  }
  return cached;
}
