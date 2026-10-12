// Starter-template forking. Pure (no DOM) so it is unit-tested.
// A template is never written over: it is copied into a throwaway source record, forked with
// store.designs.duplicate(), the source record is removed, and the fork is tagged with
// meta.forkedFrom {templateId, templateVersion} (for the T4 diff/compare view).
import { defaultDesign, migrateDesign } from '../vehicle/design.js';

const SRC_PREFIX = 'tplsrc-';

/** Player-facing design data for a template: same schema, without the template block. */
export function templateDesign(tpl) {
  const { template, ...design } = JSON.parse(JSON.stringify(tpl));
  return { ...design, meta: { ...(design.meta || {}), forkedFrom: { templateId: tpl.id, templateVersion: template?.version ?? 1 } } };
}

/** Fork a template into a new saved design. Returns { id, design }. */
export function forkTemplate(store, tpl) {
  const src = `${SRC_PREFIX}${tpl.id}-${Date.now().toString(36)}${Math.random().toString(36).slice(2, 6)}`;
  const data = templateDesign(tpl);
  store.designs.put(data, { id: src, name: tpl.name });
  let r;
  try { r = store.designs.duplicate(src, tpl.name); } finally { store.designs.delete(src); }
  const design = migrateDesign({ ...data, id: r.id });
  store.designs.put(design, { id: r.id, name: tpl.name });
  return { id: r.id, design };
}

/** Most recently updated saved design, or null. Skips any leftover template source records. */
export function lastDesign(store) {
  const m = store.designs.list().find((x) => !String(x.id).startsWith(SRC_PREFIX));
  if (!m) return null;
  const d = store.designs.get(m.id);
  return d ? { id: m.id, name: m.name, design: { ...d, name: m.name } } : null;
}

export function blankDesign() { return defaultDesign({ name: 'Untitled' }); }
