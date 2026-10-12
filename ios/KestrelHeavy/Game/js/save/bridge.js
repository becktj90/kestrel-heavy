// Native save bridge (JS side). Mirrors saves to the SwiftUI shell through
// window.webkit.messageHandlers.kestrelSave as {op, key, value}.
// Never throws: every call resolves to {ok, value?, error?}. The native side
// is optional; store.js always keeps localStorage as the fallback copy.

export const BRIDGE_NAME = 'kestrelSave';
export const BRIDGE_OPS = Object.freeze(['put', 'get', 'delete', 'list']);

export function createBridge({ win = (typeof window !== 'undefined' ? window : undefined), timeoutMs = 2000 } = {}) {
  const handler = () => {
    try { return win && win.webkit && win.webkit.messageHandlers && win.webkit.messageHandlers[BRIDGE_NAME]; }
    catch (_) { return undefined; }
  };
  const available = () => !!(handler() && typeof handler().postMessage === 'function');

  // WKScriptMessageHandlerWithReply makes postMessage return a Promise; the
  // legacy WKScriptMessageHandler returns undefined (fire-and-forget).
  function post(op, key, value) {
    if (!BRIDGE_OPS.includes(op)) return Promise.resolve({ ok: false, error: `bad op ${op}` });
    const h = handler();
    if (!h || typeof h.postMessage !== 'function') return Promise.resolve({ ok: false, error: 'unavailable' });
    let r;
    try { r = h.postMessage({ op, key: String(key), value: value === undefined ? null : value }); }
    catch (e) { return Promise.resolve({ ok: false, error: String((e && e.message) || e) }); }
    if (!r || typeof r.then !== 'function') return Promise.resolve({ ok: true, value: undefined, noReply: true });
    let timer;
    const to = new Promise((res) => { timer = setTimeout(() => res({ ok: false, error: 'timeout' }), timeoutMs); });
    return Promise.race([
      Promise.resolve(r).then((v) => ({ ok: true, value: v }), (e) => ({ ok: false, error: String((e && e.message) || e) })),
      to,
    ]).finally(() => clearTimeout(timer));
  }

  return {
    available,
    post,
    put: (key, value) => post('put', key, value),
    get: (key) => post('get', key, null),
    delete: (key) => post('delete', key, null),
    list: (prefix = '') => post('list', prefix, null),
  };
}
