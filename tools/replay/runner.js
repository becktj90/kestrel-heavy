// In-page replay runner. Evaluated after boot; exposes window.__replayRun.
// mode 'record': a simple scripted pilot reads game state and presses keys;
//                every key change is written to the trace.
// mode 'replay': only the recorded trace drives input (no state reads).
const ANCHOR_MS = 600000; // 10 min virtual; boot is far shorter
const SETTLE_FRAMES = 120;
window.__replayRun = async function replayRun({ mission, difficulty, seed, mode, trace, maxFrames = 60 * 240, every = 30 }) {
  const R = window.__replay;
  const game = window.__ngGame;
  const scene = game.scene.scenes[0];
  const { flightBus } = await import('/js/narration/bus.js');
  const r = (v, d = 3) => (v == null || Number.isNaN(v) ? null : Number(Number(v).toFixed(d)));
  const fnv = (s) => { let h = 0x811c9dc5; for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 0x01000193); } return (h >>> 0).toString(16).padStart(8, '0'); };
  const key = (type, code) => window.dispatchEvent(new KeyboardEvent(type, { code, key: code, bubbles: true }));

  // Pin the virtual clock to the same absolute time on every run, then let
  // Phaser's / Matter's delta smoothing settle on identical 1/60 s frames.
  // Boot length varies with host speed; nothing after this point may depend on it.
  R.anchor(ANCHOR_MS);
  R.step(SETTLE_FRAMES);

  // Normalise menu state so every run starts identically.
  for (const c of ['ArrowLeft', 'ArrowRight', 'Space']) key('keyup', c);
  if (scene.status !== 'MENU') scene.returnToMenu();
  // The menu ships with only KH-1 unlocked. The gate is one trace per mission,
  // so open the manifest here. Deck-finish unlock in the game is unchanged.
  if (!Array.isArray(scene.settings.unlockedMissions)) scene.settings.unlockedMissions = [];
  for (const id of ['KH-1', 'KH-2', 'KH-3', 'KH-4', 'KH-5']) {
    if (!scene.settings.unlockedMissions.includes(id)) scene.settings.unlockedMissions.push(id);
  }
  scene.setDifficulty(difficulty);
  scene.setMission(mission);
  scene.nowSec = 0;
  R.virtualTimers(true);
  R.reseed(seed);
  if (window.Phaser?.Math?.RND) window.Phaser.Math.RND.sow([String(seed)]);

  const events = [];
  const off = flightBus.on('*', (ev) => { events.push([ev.type, r(ev.t, 4), ev.payload?.abortId || ev.payload?.reason || undefined].filter((x) => x !== undefined)); });
  const phases = [];
  const checkpoints = [];
  const out = mode === 'record' ? [] : null;
  const held = { ArrowLeft: false, ArrowRight: false, Space: false };
  const set = (code, down, f) => {
    if (held[code] === down) return;
    held[code] = down; key(down ? 'keydown' : 'keyup', code);
    out?.push([f, code, down ? 1 : 0]);
  };
  const tr = trace ? trace.slice() : [];
  let lastPhase = '';
  let endFrame = null;
  let f = 0;
  for (; f < maxFrames; f++) {
    if (mode === 'record') pilot(f);
    else while (tr.length && tr[0][0] === f) { const [, code, down] = tr.shift(); held[code] = !!down; key(down ? 'keydown' : 'keyup', code); }
    R.step(1);
    const s = scene.session || {};
    const ph = [scene.status, s.sepPhase || '', s.jacklynPhase || ''].join('/');
    if (ph !== lastPhase) { phases.push([f, ph]); lastPhase = ph; }
    if (f % every === 0) {
      const b = scene.rocket?.body;
      const snap = [scene.status, s.stage, r(scene.rocket?.x), r(scene.rocket?.y), r(scene.rocket?.angle), r(b?.velocity?.x), r(b?.velocity?.y),
        r(s.score, 2), r(s.health, 3), r(s.fuel, 4), r(s.tClock, 4), s.combo, r(s.altitudeKm, 3), r(scene.nowSec, 4),
        r(scene.cameras?.main?.zoom, 4), r(scene.cameras?.main?.scrollY, 2), (scene.hazards?.getLength?.() ?? scene.hazards?.length ?? null), JSON.stringify(scene.playfieldSize?.())];
      checkpoints.push([f, fnv(JSON.stringify(snap)), snap]);
    }
    if (scene.status === 'SUMMARY' && endFrame == null) endFrame = f;
    if (endFrame != null && f >= endFrame + 30) break;
  }
  off();
  for (const c of ['ArrowLeft', 'ArrowRight', 'Space']) if (held[c]) key('keyup', c);
  const s = scene.session || {};
  const end = events.find((e) => e[0] === 'flight.end');
  return {
    result: {
      outcome: end ? end[2] : 'timeout',
      endFrame, score: Math.max(0, Math.round(s.score || 0)), medal: s.medal || null,
      recovered: !!s.recovered, fired: Object.keys(s.fired || {}).filter((k) => s.fired[k]).sort(),
    },
    phases, events, checkpoints, trace: out, frames: f,
  };

  function pilot(fr) {
    const st = scene.status; const s = scene.session || {}; const rk = scene.rocket; const b = rk?.body;
    if (st === 'MENU') { set('Space', fr % 20 < 2, fr); return; }
    if (st === 'PRELAUNCH') { set('Space', fr % 40 >= 20 && fr % 40 < 23, fr); return; }
    if (!rk || !b) return;
    let target = 360; let vxGain = 18; let space = false;
    if (st === 'ASCENT') {
      target = scene.session.corridorX ?? (scene.scale?.gameSize?.width || 720) / 2;
    } else if (st === 'SEP') {
      target = rk.x; vxGain = 40;
      if (s.sepPhase === 'window' && (s.sepAlignHold || 0) > 0.35) space = fr % 12 < 2;
    } else if (st === 'HAVEN') {
      target = scene.deck?.x ?? rk.x; vxGain = 22;
      if (s.burnWindow && b.velocity.y > 1.2) space = true;
    }
    const err = (target - rk.x) / 120 - b.velocity.x / vxGain * 10;
    set('ArrowLeft', err < -0.25, fr);
    set('ArrowRight', err > 0.25, fr);
    set('Space', space, fr);
  }
};
