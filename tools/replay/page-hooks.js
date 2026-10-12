// Injected before any page script (CDP Page.addScriptToEvaluateOnNewDocument).
// Makes the browser deterministic WITHOUT touching game code:
//  - Math.random → seeded mulberry32 (reseeded at mission start)
//  - performance.now / Date.now / requestAnimationFrame → a virtual clock
//    advanced only by __replay.step(), a fixed 1/60 s per frame.
(() => {
  const FRAME_MS = 1000 / 60;
  let seed = 1;
  function mulberry32() {
    seed |= 0; seed = (seed + 0x6D2B79F5) | 0;
    let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  }
  Math.random = mulberry32;
  let now = 1000;
  const epoch = Date.UTC(2026, 0, 1);
  performance.now = () => now;
  Date.now = () => epoch + now;
  let queue = [];
  let nextId = 1;
  window.requestAnimationFrame = (cb) => { const id = nextId++; queue.push({ id, cb }); return id; };
  window.cancelAnimationFrame = (id) => { queue = queue.filter((q) => q.id !== id); };
  // Page timers run on the virtual clock too (hud.js banner timers etc.), so
  // nothing depends on how fast the host machine steps frames.
  const realSetTimeout = window.setTimeout.bind(window);
  let timers = [];
  let timerSeq = 0;
  let virtualTimers = false;
  window.setTimeout = (fn, ms = 0, ...args) => {
    if (!virtualTimers) return realSetTimeout(fn, ms, ...args);
    const id = 1e6 + (++timerSeq);
    timers.push({ id, at: now + Math.max(0, Number(ms) || 0), seq: timerSeq, fn: typeof fn === 'function' ? () => fn(...args) : () => {} });
    return id;
  };
  const realClearTimeout = window.clearTimeout.bind(window);
  window.clearTimeout = (id) => { if (id > 1e6) timers = timers.filter((t) => t.id !== id); else realClearTimeout(id); };
  function runTimers() {
    for (;;) {
      timers.sort((a, b) => a.at - b.at || a.seq - b.seq);
      if (!timers.length || timers[0].at > now) return;
      const t = timers.shift();
      try { t.fn(); } catch (e) { console.error(e); }
    }
  }
  window.__replay = {
    virtualTimers(on) { virtualTimers = !!on; if (!on) timers = []; },
    FRAME_MS,
    reseed(s) { seed = s >>> 0; },
    step(n = 1) {
      for (let i = 0; i < n; i++) {
        now += FRAME_MS;
        const run = queue; queue = [];
        for (const q of run) q.cb(now);
        runTimers();
      }
    },
    now: () => now,
  };
})();
