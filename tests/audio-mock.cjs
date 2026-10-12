// Mocked Web Audio + Phaser sound manager for audio-* tests.
class Param { constructor(v = 1) { this.value = v; this.calls = []; } setTargetAtTime(v) { this.value = v; this.calls.push(v); } setValueAtTime(v) { this.value = v; } exponentialRampToValueAtTime() {} }
class Node {
  constructor(kind) { this.kind = kind; this.out = []; this.gain = new Param(1); }
  connect(n) { this.out.push(n); return n; }
  disconnect() { this.out = []; }
}
class MockAudioContext {
  constructor() { this.state = 'running'; this.currentTime = 0; this.destination = new Node('dest'); this.listeners = {}; this.log = []; }
  createGain() { return new Node('gain'); }
  createDynamicsCompressor() { const n = new Node('comp'); ['threshold', 'knee', 'ratio', 'attack', 'release'].forEach((k) => { n[k] = new Param(0); }); return n; }
  createOscillator() { const n = new Node('osc'); n.frequency = new Param(440); n.start = () => {}; n.stop = () => {}; return n; }
  resume() { this.log.push('resume'); this.state = 'running'; return Promise.resolve(); }
  suspend() { this.log.push('suspend'); this.state = 'suspended'; return Promise.resolve(); }
  addEventListener(t, f) { (this.listeners[t] ||= []).push(f); }
  removeEventListener(t, f) { this.listeners[t] = (this.listeners[t] || []).filter((x) => x !== f); }
  fire(state) { this.state = state; (this.listeners.statechange || []).forEach((f) => f({ target: this })); }
}
class Target {
  constructor() { this.l = {}; }
  addEventListener(t, f) { (this.l[t] ||= []).push(f); }
  removeEventListener(t, f) { this.l[t] = (this.l[t] || []).filter((x) => x !== f); }
  dispatch(t) { (this.l[t] || []).slice().forEach((f) => f({ type: t })); }
}
function mockScene() {
  const ctx = new MockAudioContext();
  const master = new Node('phaser-master');
  master.connect(ctx.destination);
  const played = [];
  const sound = {
    context: ctx, masterVolumeNode: master, locked: false, mute: false, volume: 1,
    added: [],
    add(key, cfg) {
      const s = { key, cfg, isPlaying: false, isPaused: false, handlers: {}, destroyed: false,
        once(e, f) { this.handlers[e] = f; }, play() { this.isPlaying = true; played.push({ key, volume: cfg.volume }); },
        stop() { this.isPlaying = false; }, destroy() { this.destroyed = true; }, setVolume(v) { this.cfg.volume = v; },
        pause() { this.isPaused = true; }, resume() { this.isPaused = false; },
        finish() { this.isPlaying = false; this.handlers.complete?.(); } };
      this.added.push(s);
      return s;
    },
    get() { return null; }, unlock() { this.locked = false; },
  };
  let now = 0;
  const scene = { sound, played, settings: { volume: 0.72, sfxVolume: 1, musicVolume: 0.85 }, cache: { audio: { exists: () => true } },
    time: { get now() { return now; }, delayedCall: () => 1, removeEvent() {} }, advance(ms) { now += ms; } };
  return scene;
}
module.exports = { MockAudioContext, Target, Node, mockScene };
