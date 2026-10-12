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
  window.__replay = {
    FRAME_MS,
    reseed(s) { seed = s >>> 0; },
    step(n = 1) {
      for (let i = 0; i < n; i++) {
        now += FRAME_MS;
        const run = queue; queue = [];
        for (const q of run) q.cb(now);
      }
    },
    now: () => now,
  };
})();
