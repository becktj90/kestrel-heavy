/**
 * S1 mix buses: master -> limiter -> destination, with music/sfx/engine/voice
 * buses feeding master. Works in two modes:
 *  - logical: pure gain math (used for Phaser sounds, which own their nodes)
 *  - graph: real GainNodes + DynamicsCompressor limiter on a given AudioContext
 * Gains are linear 0..1. effective(bus) = master * bus * duck(bus).
 */
export const BUSES = Object.freeze(['master', 'music', 'sfx', 'engine', 'voice']);
export const LIMITER = Object.freeze({ threshold: -1, knee: 0, ratio: 20, attack: 0.003, release: 0.12 });

const clamp01 = (v, d) => {
  const n = Number(v);
  return Number.isFinite(n) ? Math.max(0, Math.min(1, n)) : d;
};

export function createMixer() {
  const level = { master: 0.72, music: 0.85, sfx: 1, engine: 1, voice: 1 };
  const duck = { music: 1, sfx: 1, engine: 1 };
  let muted = false;
  let graph = null;

  const mixer = {
    level,
    duck,
    get muted() { return muted; },
    setMuted(on) { muted = Boolean(on); mixer.sync(); },
    setLevel(bus, value) {
      if (!BUSES.includes(bus)) throw new Error(`unknown bus ${bus}`);
      level[bus] = clamp01(value, level[bus]);
      mixer.sync();
    },
    /** Map the existing settings object onto bus levels (same defaults as before S1). */
    fromSettings(s) {
      level.master = clamp01(s?.volume, 0.72);
      level.sfx = clamp01(s?.sfxVolume, 1);
      level.music = clamp01(s?.musicVolume, 0.85);
      level.engine = level.sfx;
      muted = Boolean(s?.muted) || s?.sound === false;
      mixer.sync();
    },
    setDuck(bus, mul) {
      if (!(bus in duck)) return;
      duck[bus] = clamp01(mul, 1);
      mixer.sync();
    },
    effective(bus) {
      if (muted) return 0;
      if (bus === 'master') return level.master;
      return level.master * level[bus] * (duck[bus] ?? 1);
    },
    /** Build the node graph on ctx. Returns { input(bus), limiter, nodes }. */
    connect(ctx, destination = ctx.destination) {
      const nodes = {};
      BUSES.forEach((b) => { nodes[b] = ctx.createGain(); });
      const limiter = ctx.createDynamicsCompressor();
      Object.entries(LIMITER).forEach(([k, v]) => {
        if (limiter[k] && 'value' in limiter[k]) limiter[k].value = v;
      });
      BUSES.filter((b) => b !== 'master').forEach((b) => nodes[b].connect(nodes.master));
      nodes.master.connect(limiter);
      limiter.connect(destination);
      graph = { ctx, nodes, limiter };
      mixer.sync();
      return graph;
    },
    input(bus) { return graph?.nodes[bus] || null; },
    get graph() { return graph; },
    onChange: null,
    sync() {
      if (typeof mixer.onChange === 'function') { try { mixer.onChange(); } catch { /* ignore */ } }
      if (!graph) return;
      const t = graph.ctx.currentTime || 0;
      BUSES.forEach((b) => {
        const g = graph.nodes[b].gain;
        const v = b === 'master' ? (muted ? 0 : level.master) : level[b] * (duck[b] ?? 1);
        if (typeof g.setTargetAtTime === 'function') g.setTargetAtTime(v, t, 0.02);
        else g.value = v;
      });
    },
  };
  return mixer;
}
