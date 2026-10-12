// Compare a reference rerun to committed golden vectors.
// Numeric bands are read from the golden-tolerances block in docs/PHYSICS.md.
// Rule: |actual - expected| <= abs + rel * |expected|.

const FENCE = '```golden-tolerances';

export function loadTolerances(markdown) {
  const at = markdown.indexOf(FENCE);
  if (at < 0) throw new Error('docs/PHYSICS.md is missing the golden-tolerances block');
  const rest = markdown.slice(at + FENCE.length);
  const end = rest.indexOf('```');
  if (end < 0) throw new Error('golden-tolerances block is not closed');
  const spec = JSON.parse(rest.slice(0, end));
  if (!spec.quantities || !Array.isArray(spec.exactKeys)) throw new Error('golden tolerances: bad shape');
  const byKey = new Map();
  for (const [name, q] of Object.entries(spec.quantities)) {
    if (!(q.abs >= 0) || !(q.rel >= 0) || !Array.isArray(q.keys)) throw new Error(`bad quantity ${name}`);
    for (const key of q.keys) {
      if (byKey.has(key)) throw new Error(`tolerance key ${key} mapped twice`);
      byKey.set(key, { name, abs: q.abs, rel: q.rel });
    }
  }
  for (const key of spec.exactKeys) if (byKey.has(key)) throw new Error(`exact key ${key} also has a band`);
  for (const [name, map] of Object.entries(spec.overlays || {})) {
    if (!map || typeof map !== 'object') throw new Error(`bad overlay ${name}`);
    for (const qty of Object.values(map)) if (!spec.quantities[qty]) throw new Error(`overlay ${name} → unknown ${qty}`);
  }
  return { spec, byKey, exact: new Set(spec.exactKeys) };
}

/** True when actual is inside the absolute+relative band of expected. */
export function within(actual, expected, tol) {
  if (typeof expected !== 'number' || typeof actual !== 'number') return Object.is(actual, expected);
  if (!Number.isFinite(actual) || !Number.isFinite(expected)) return Object.is(actual, expected);
  const limit = tol.abs + tol.rel * Math.abs(expected);
  return Math.abs(actual - expected) <= limit;
}

function leafKey(path) {
  const m = path.match(/\.([A-Za-z_][A-Za-z0-9_]*)$/);
  return m ? m[1] : '';
}

function bandFor(key, loaded, overlay) {
  if (loaded.exact.has(key)) return { exact: true };
  const over = overlay?.[key];
  if (over) {
    const q = loaded.spec.quantities[over];
    return { name: over, abs: q.abs, rel: q.rel };
  }
  return loaded.byKey.get(key) || null;
}

/**
 * Walk expected and compare actual. Numeric leaves use the quantity band for their key.
 * `overlay` maps a leaf key to a quantity name and wins over the default key map.
 * @returns {string[]} error messages, empty when the trees agree
 */
export function compareTree(actual, expected, loaded, path = '', overlay = null) {
  const errs = [];
  const walk = (a, e, p) => {
    if (Array.isArray(e)) {
      if (!Array.isArray(a) || a.length !== e.length) { errs.push(`${p}: length ${a?.length} vs ${e.length}`); return; }
      e.forEach((item, i) => walk(a[i], item, `${p}[${i}]`));
      return;
    }
    if (e && typeof e === 'object') {
      if (!a || typeof a !== 'object' || Array.isArray(a)) { errs.push(`${p}: not an object`); return; }
      const ak = Object.keys(a).sort();
      const ek = Object.keys(e).sort();
      if (ak.join('\0') !== ek.join('\0')) { errs.push(`${p}: keys ${ak.join(',')} vs ${ek.join(',')}`); return; }
      for (const k of ek) walk(a[k], e[k], `${p}.${k}`);
      return;
    }
    if (typeof e === 'number') {
      const key = leafKey(p);
      const tol = bandFor(key, loaded, overlay);
      if (!tol) { errs.push(`${p}: no tolerance for "${key}"`); return; }
      if (tol.exact) {
        if (!Object.is(a, e)) errs.push(`${p}: ${a} !== ${e} (exact)`);
        return;
      }
      if (!within(a, e, tol)) {
        const limit = tol.abs + tol.rel * Math.abs(e);
        errs.push(`${p}: ${a} vs ${e} outside ${tol.name} |d|<=${limit}`);
      }
      return;
    }
    if (!Object.is(a, e)) errs.push(`${p}: ${JSON.stringify(a)} !== ${JSON.stringify(e)}`);
  };
  walk(actual, expected, path || '$');
  return errs;
}
