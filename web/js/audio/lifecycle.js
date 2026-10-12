/**
 * JS-side interruption + visibility handling for an AudioContext (WKWebView).
 * - hidden/pagehide: onSuspend() then ctx.suspend()
 * - visible/pageshow: ctx.resume() then onResume()
 * - ctx 'interrupted' (iOS calls/Siri) or unexpected 'suspended' while
 *   visible: resume on the next user gesture (first-tap unlock rule).
 */
const GESTURES = ['pointerdown', 'touchend', 'keydown'];

export function attachLifecycle({ doc, win, getContexts, onSuspend, onResume } = {}) {
  const d = doc || (typeof document !== 'undefined' ? document : null);
  const w = win || (typeof window !== 'undefined' ? window : null);
  const ctxs = () => (getContexts ? getContexts() : []).filter(Boolean);
  let hidden = false;
  let pendingGesture = false;
  const state = { suspends: 0, resumes: 0, interruptions: 0 };

  const resumeAll = () => ctxs().forEach((c) => {
    if (c.state !== 'running' && c.state !== 'closed' && typeof c.resume === 'function') {
      try { const p = c.resume(); if (p?.catch) p.catch(() => {}); } catch { /* ignore */ }
    }
  });
  const onGesture = () => {
    if (!pendingGesture || hidden) return;
    pendingGesture = false;
    resumeAll();
  };
  const suspend = () => {
    if (hidden) return;
    hidden = true;
    state.suspends += 1;
    try { onSuspend?.(); } catch { /* ignore */ }
    ctxs().forEach((c) => { if (c.state === 'running' && c.suspend) { try { const p = c.suspend(); if (p?.catch) p.catch(() => {}); } catch { /* */ } } });
  };
  const resume = () => {
    if (!hidden) return;
    hidden = false;
    state.resumes += 1;
    resumeAll();
    pendingGesture = true; // WKWebView may refuse until a tap
    try { onResume?.(); } catch { /* ignore */ }
  };
  const onVis = () => (d?.visibilityState === 'hidden' ? suspend() : resume());
  const onCtxState = (e) => {
    const c = e?.target;
    if (!c || hidden) return;
    if (c.state === 'interrupted' || c.state === 'suspended') {
      state.interruptions += 1;
      pendingGesture = true;
      resumeAll();
    }
  };
  const watched = new Set();
  const watch = () => ctxs().forEach((c) => {
    if (watched.has(c) || typeof c.addEventListener !== 'function') return;
    watched.add(c);
    c.addEventListener('statechange', onCtxState);
  });

  d?.addEventListener?.('visibilitychange', onVis);
  w?.addEventListener?.('pagehide', suspend);
  w?.addEventListener?.('pageshow', resume);
  GESTURES.forEach((g) => w?.addEventListener?.(g, onGesture, true));
  watch();

  return {
    state,
    watch,
    get hidden() { return hidden; },
    get pendingGesture() { return pendingGesture; },
    detach() {
      d?.removeEventListener?.('visibilitychange', onVis);
      w?.removeEventListener?.('pagehide', suspend);
      w?.removeEventListener?.('pageshow', resume);
      GESTURES.forEach((g) => w?.removeEventListener?.(g, onGesture, true));
      watched.forEach((c) => c.removeEventListener?.('statechange', onCtxState));
      watched.clear();
    },
  };
}
