// In-page autopilot for Kestrel Heavy. Injected via addInitScript.
// Seeds Math.random (mulberry32) and drives scene.inputState like a player would.
(() => {
  const cfg = window.__KH_QA_CFG || {};
  let s = (cfg.seed >>> 0) || 1;
  const rnd = () => { s |= 0; s = (s + 0x6D2B79F5) | 0; let t = Math.imul(s ^ (s >>> 15), 1 | s); t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t; return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
  Math.random = rnd;
  let b = 0x9E3779B9 ^ ((cfg.seed || 1) * 7919); // separate bot RNG so bot noise doesn't perturb spawns
  const brnd = () => { b ^= b << 13; b ^= b >>> 17; b ^= b << 5; return ((b >>> 0) % 100000) / 100000; };
  if (cfg.gcBridge) {
    window.webkit = { messageHandlers: { kestrelGameCenter: { postMessage(m) {
      window.__qaGcCalls = (window.__qaGcCalls || 0) + 1;
      if (cfg.gcBridge === 'throw') throw new Error('GKErrorDomain: The Internet connection appears to be offline.');
    } } } };
  }
  const SK = {
    high: { tick: 60, look: 280, noise: 0, miss: 0.0, burnAlt: 1.0, sepDelay: 0.2 },
    mid:  { tick: 160, look: 190, noise: 22, miss: 0.25, burnAlt: 0.85, sepDelay: 0.6 },
    low:  { tick: 320, look: 120, noise: 48, miss: 0.5, burnAlt: 0.6, sepDelay: 1.4 },
  }[cfg.skill || 'mid'];
  const log = window.__qaLog = { statuses: [], events: [] };
  let corridor = null;
  let lastStatus = null; let sepWindowAt = null; let target = null;
  function scene() { return window.__ngGame?.scene?.scenes?.[0]; }
  async function loop() {
    const sc = scene();
    if (sc && sc.session && sc.rocket && sc.inputState) {
      if (!corridor) { try { corridor = await import('/js/corridor.js'); } catch (e) { corridor = {}; } }
      step(sc);
    }
    setTimeout(loop, SK.tick);
  }
  function steerTo(sc, x, dead = 10) {
    const inp = sc.inputState;
    const err = x - sc.rocket.x;
    inp.left = err < -dead; inp.right = err > dead;
  }
  function step(sc) {
    const st = sc.status; const ss = sc.session; const inp = sc.inputState;
    if (st !== lastStatus) { log.statuses.push({ st, t: performance.now() }); lastStatus = st; }
    if (window.__qaHold) return;
    if (st === 'PRELAUNCH') { if (!ss.launchArmed) sc.requestLaunch(); return; }
    if (st === 'ASCENT') {
      let cx = sc.rocket.x;
      if (corridor.corridorBounds) { const bd = corridor.corridorBounds(sc.rocket.y); cx = (bd.left + bd.right) / 2; }
      if (target == null || brnd() < 0.3) target = cx + (brnd() - 0.5) * 2 * SK.noise;
      let goal = target;
      // dodge nearest hazard ahead (above = smaller y)
      let threat = null;
      for (const h of sc.hazards || []) {
        if (!h.active) continue;
        const dy = sc.rocket.y - h.y; const dx = h.x - sc.rocket.x;
        if (dy > -40 && dy < SK.look && Math.abs(dx) < 90 && (!threat || dy < threat.dy)) threat = { dy, dx, h };
      }
      if (threat && !(threat.h.__qaMiss ??= brnd() < SK.miss)) goal = sc.rocket.x + (threat.dx >= 0 ? -120 : 120);
      // pickups: drift toward if nothing threatening
      if (!threat) for (const p of sc.pickups || []) { if (p.active && sc.rocket.y - p.y > 0 && sc.rocket.y - p.y < SK.look) { goal = p.x; break; } }
      if (corridor.corridorBounds) { const bd = corridor.corridorBounds(sc.rocket.y); goal = Math.max(bd.left + 40, Math.min(bd.right - 40, goal)); }
      steerTo(sc, goal, 12);
      return;
    }
    if (st === 'SEP') {
      const vx = sc.rocket.body?.velocity?.x || 0; const ang = sc.rocket.angle || 0;
      const corr = vx * 30 + ang * 3;
      inp.left = corr > 4; inp.right = corr < -4;
      if (ss.sepPhase === 'window') {
        sepWindowAt ??= performance.now();
        if ((performance.now() - sepWindowAt) / 1000 > SK.sepDelay && (ss.sepAlignHold > 0.5 || (performance.now() - sepWindowAt) > 4000)) {
          const btn = document.getElementById('ng-sep-btn');
          if (btn && !btn.hidden && btn.offsetParent) btn.click(); else sc.requestSep();
        }
      }
      return;
    }
    sepWindowAt = null;
    if (st === 'HAVEN' || st === 'JACKLYN') {
      const vx = sc.rocket.body?.velocity?.x || 0; const vy = sc.rocket.body?.velocity?.y || 0;
      const deckX = (sc.deck?.x ?? sc.jacklyn.x);
      const alt = sc.jacklyn.y - sc.rocket.y;
      const tGo = vy > 0.3 ? Math.min(alt / vy, 120) : 60;
      const pred = sc.rocket.x + vx * Math.min(tGo, 40) * 0.5;
      steerTo(sc, deckX + (pred - sc.rocket.x) * -1 + (brnd() - 0.5) * SK.noise, 8);
      const now = sc.nowSec;
      if (ss.burnWindow) {
        // The prompt is HOLD TO BURN through the window. Glide sink is already
        // softer than the old vy>3 threshold, which never lit the engines and
        // tipped every catch at the -30° strake attitude.
        const altPx = (sc.jacklyn?.y || 0) - sc.rocket.y;
        const late = SK.burnAlt < 1 && brnd() > SK.burnAlt;
        const want = !late && altPx > 70;
        inp.boostHeld = want; if (want) inp.boostUntil = now + 0.1;
      } else { inp.boostHeld = false; }
      return;
    }
  }
  loop();
})();
