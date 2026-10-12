/** Caption preferences, persisted separately from game settings. Off by default. */
export const CAPTION_KEY = 'kestrelHeavy.captions.v1';
export const CAPTION_SIZES = ['s', 'm', 'l'];
export const DEFAULT_CAPTIONS = Object.freeze({
  enabled: false,
  size: 'm',
  bgOpacity: 0.6,
  highContrast: false,
  hosts: true,
});

export function normalizeCaptions(raw) {
  const c = { ...DEFAULT_CAPTIONS, ...(raw && typeof raw === 'object' ? raw : {}) };
  c.enabled = c.enabled === true;
  c.size = CAPTION_SIZES.includes(c.size) ? c.size : 'm';
  const o = Number(c.bgOpacity);
  c.bgOpacity = Number.isFinite(o) ? Math.min(1, Math.max(0, o)) : DEFAULT_CAPTIONS.bgOpacity;
  c.highContrast = c.highContrast === true;
  c.hosts = c.hosts !== false;
  return c;
}

export function loadCaptions(storage = globalThis.localStorage) {
  let c = DEFAULT_CAPTIONS;
  try { c = JSON.parse(storage?.getItem(CAPTION_KEY) || 'null') || c; } catch { /* default */ }
  c = normalizeCaptions(c);
  try {
    const q = new URLSearchParams(globalThis.location?.search || '');
    if (q.get('captions') === '1') c.enabled = true;
  } catch { /* no location */ }
  return c;
}

export function saveCaptions(c, storage = globalThis.localStorage) {
  const n = normalizeCaptions(c);
  try { storage?.setItem(CAPTION_KEY, JSON.stringify(n)); } catch { /* quota / private */ }
  return n;
}
