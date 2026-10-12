/**
 * Phaser Sound Manager mix for Kestrel Heavy.
 * Web Audio preferred. Unlock on first gesture. Mute button is authoritative —
 * prefers-reduced-motion never mutes on its own.
 *
 * NASA trims live in ./audio/ (see ATTRIBUTION.md). Procedural tones are fallback only.
 * S1: gains route through the audio core buses (./audio/core.js): master,
 * music, sfx, engine, voice -> limiter. One-shots use a capped voice pool.
 * Theme: Suno instrumental "Kestrel Heavy" by trevorjohnbeck — looping BGM, quieter than SFX.
 */
import { createAudioCore } from './audio/core.js';

export const core = createAudioCore();

const KEYS = {
  roar: { file: 'roar-loop', loop: true, vol: 0.36 },
  burn: { file: 'burn-loop', loop: true, vol: 0.32 },
  liftoff: { file: 'liftoff', vol: 0.72, lock: 3.2 },
  maxq: { file: 'maxq', vol: 0.58, lock: 1.6 },
  meco: { file: 'meco', vol: 0.52, lock: 2.1 },
  whoosh: { file: 'whoosh', vol: 0.5, lock: 1.4 },
  touchdown: { file: 'touchdown', vol: 0.58, lock: 1.8 },
  recovered: { file: 'recovered', vol: 0.64, lock: 2.2 },
  splash: { file: 'splash', vol: 0.56, lock: 1.6 },
  ui: { file: 'quindar', vol: 0.34, lock: 0.28 },
  countdown: { file: 'quindar', vol: 0.28, lock: 0.45 },
  pickup: { file: 'pickup', vol: 0.46, lock: 0.35 },
  fuel: { file: 'pickup', vol: 0.4, lock: 0.35 },
  shield: { file: 'pickup', vol: 0.4, lock: 0.35 },
  overdrive: { file: 'maxq', vol: 0.34, lock: 1.2 },
  hit: { file: 'hit', vol: 0.5, lock: 0.4 },
  rud: { file: 'rud', vol: 0.62, lock: 1.8 },
  success: { file: 'recovered', vol: 0.56, lock: 2.2 },
  ignite: { file: 'quindar', vol: 0.42, lock: 0.55 },
};

const BEDS = new Set(['roar', 'burn']);
const THEME = { file: 'theme', vol: 0.2 };
const CRITICAL_SFX = new Set([
  'liftoff', 'maxq', 'meco', 'whoosh', 'touchdown', 'recovered',
  'splash', 'rud', 'success',
]);

const AudioApi = {
  scene: null,
  beds: { roar: null, burn: null, theme: null },
  unlocked: false,
  ctx: null,
  master: null,
  unduckTimer: 0,
  lastPlay: Object.create(null),
  lastPlayAt: Object.create(null),

  preload(scene) {
    const files = [...new Set(Object.values(KEYS).map((spec) => spec.file))];
    files.forEach((file) => {
      scene.load.audio(`ng-${file}`, [`./audio/${file}.ogg`, `./audio/${file}.mp3`]);
    });
    scene.load.audio(`ng-${THEME.file}`, [`./audio/${THEME.file}.ogg`, `./audio/${THEME.file}.mp3`]);
  },

  core,
  settings: null,
  phaserLimited: null,

  attach(scene) {
    this.scene = scene;
    this.settings = scene.settings || this.settings;
    core.mixer.onChange = () => { if (this.settings) this.mixTheme(this.settings); };
    this.insertLimiter();
    if (typeof window !== 'undefined' && typeof document !== 'undefined') {
      core.attachLifecycle({ getContexts: () => [this.scene?.sound?.context, this.ctx] });
    }
    this.applyMix(scene.settings);
  },

  /** Put the master limiter between Phaser's master gain and the speakers. */
  insertLimiter() {
    const sound = this.scene?.sound;
    const ctx = sound?.context;
    const node = sound?.masterVolumeNode;
    if (!ctx || !node || this.phaserLimited === ctx || typeof ctx.createDynamicsCompressor !== 'function') return;
    try {
      const lim = core.mixer.connect(ctx, ctx.destination).limiter;
      node.disconnect();
      node.connect(lim);
      this.phaserLimited = ctx;
    } catch { /* keep Phaser's default routing */ }
  },

  /** Bus levels from settings (same defaults as before: 0.72 / 1 / 0.85). */
  levels(settings) {
    if (settings) {
      this.settings = settings;
      const cb = core.mixer.onChange;
      core.mixer.onChange = null;
      core.mixer.fromSettings(settings);
      core.mixer.onChange = cb;
    }
    return core.mixer.level;
  },

  masterVolume(settings) {
    return this.levels(settings).master;
  },

  sfxGain(settings, bus = 'sfx') {
    const l = this.levels(settings);
    return l.master * l[bus] * (core.mixer.duck[bus] ?? 1);
  },

  musicGain(settings) {
    const l = this.levels(settings);
    return l.master * l.music * core.mixer.duck.music;
  },

  /** Voice bus activity (narration, later N2). Ducks music/sfx/engine. */
  voiceStart() { core.ducker.voiceStart(); },
  voiceEnd() { core.ducker.voiceEnd(); },

  /** Subscribe the audio core to the shared flight event bus (narration/bus.js). */
  connectBus(bus) { return core.connectBus(bus); },

  silenced(settings) {
    return Boolean(settings?.muted) || settings?.sound === false;
  },

  bedsAllowed(settings) {
    return !this.silenced(settings) && settings?.music !== false;
  },

  unlock(settings) {
    const sound = this.scene?.sound;
    if (sound) {
      if (sound.locked) sound.unlock();
      this.unlocked = true;
      this.applyMix(settings);
      return true;
    }
    return this.ensureFallback();
  },

  applyMix(settings) {
    const sound = this.scene?.sound;
    if (!sound) return;
    const silent = this.silenced(settings);
    sound.mute = silent;
    if (typeof sound.setMute === 'function') sound.setMute(silent);
    const vol = this.sfxGain(settings);
    sound.volume = vol;
    if (typeof sound.setVolume === 'function') sound.setVolume(vol);
    if (!this.bedsAllowed(settings)) {
      this.stopBeds();
      this.stopTheme();
    } else {
      this.mixTheme(settings);
    }
  },

  setMute(settings) {
    this.applyMix(settings);
    if (this.master) {
      this.master.gain.setTargetAtTime(
        this.silenced(settings) ? 0.0001 : 0.16 * this.sfxGain(settings),
        this.now(),
        0.04,
      );
    }
  },

  setPaused(paused) {
    Object.values(this.beds).forEach((bed) => {
      if (!bed) return;
      if (paused) bed.pause();
      else if (bed.isPaused) bed.resume();
    });
  },

  play(name, settings) {
    if (!settings || this.silenced(settings)) return;
    this.unlock(settings);
    const spec = KEYS[name] || KEYS.ui;
    const now = this.scene?.time?.now ? this.scene.time.now / 1000 : Date.now() / 1000;
    const lock = spec.lock || 0.8;
    if (now - (this.lastPlayAt[name] || 0) < lock) return;
    this.lastPlayAt[name] = now;
    if (CRITICAL_SFX.has(name)) this.duckTheme(settings);
    const sound = this.scene?.sound;
    const key = spec ? `ng-${spec.file}` : null;
    if (spec && sound && this.scene.cache?.audio?.exists(key)) {
      try {
        const existing = typeof sound.get === 'function' ? sound.get(key) : null;
        if (existing?.isPlaying && lock >= 0.8) return;
        const slot = core.voices.acquire(name, CRITICAL_SFX.has(name) ? 2 : 1);
        if (!slot) return;
        const inst = sound.add(key, { volume: spec.vol * this.sfxGain(settings), loop: false });
        const done = () => {
          slot.release();
          try { inst.destroy(); } catch { /* ignore */ }
        };
        slot.stop = () => { try { inst.stop(); inst.destroy(); } catch { /* ignore */ } };
        inst.once('complete', done);
        inst.play();
        return;
      } catch {
        /* fallback */
      }
    }
    this.toneFor(name);
  },

  setBed(name, on, settings, volume) {
    if (!BEDS.has(name)) return;
    if (!on || !this.bedsAllowed(settings)) {
      this.stopBed(name);
      return;
    }
    this.unlock(settings);
    const spec = KEYS[name];
    const sound = this.scene?.sound;
    const key = `ng-${spec.file}`;
    if (!sound || !this.scene.cache?.audio?.exists(key)) {
      this.toneFor(name === 'roar' ? 'boost' : 'whoosh');
      return;
    }
    let bed = this.beds[name];
    const target = (volume == null ? spec.vol : volume) * this.musicGain(settings) * core.mixer.duck.engine;
    if (bed?.isPlaying || bed?.isPaused) {
      if (bed.isPaused) bed.resume();
      if (typeof bed.setVolume === 'function') bed.setVolume(target);
      return;
    }
    try {
      bed = sound.add(key, { loop: true, volume: target });
      this.beds[name] = bed;
      bed.play();
    } catch {
      this.toneFor('boost');
    }
  },

  stopBed(name) {
    const bed = this.beds[name];
    if (!bed) return;
    try { bed.stop(); } catch { /* ignore */ }
    try { bed.destroy(); } catch { /* ignore */ }
    this.beds[name] = null;
  },

  stopBeds() {
    this.stopBed('roar');
    this.stopBed('burn');
  },

  themeKey() {
    return `ng-${THEME.file}`;
  },

  themeVolume(settings, mul = 1) {
    return THEME.vol * mul * this.musicGain(settings);
  },

  mixTheme(settings) {
    const bed = this.beds.theme;
    if (!bed || !this.bedsAllowed(settings)) return;
    const target = this.themeVolume(settings);
    if (typeof bed.setVolume === 'function') bed.setVolume(target);
  },

  setTheme(on, settings) {
    if (!on || !this.bedsAllowed(settings)) {
      this.stopTheme();
      return;
    }
    this.unlock(settings);
    const sound = this.scene?.sound;
    const key = this.themeKey();
    if (!sound || !this.scene.cache?.audio?.exists(key)) return;
    let bed = this.beds.theme;
    const target = this.themeVolume(settings);
    if (bed?.isPlaying || bed?.isPaused) {
      if (bed.isPaused) bed.resume();
      if (typeof bed.setVolume === 'function') bed.setVolume(target);
      return;
    }
    try {
      bed = sound.add(key, { loop: true, volume: target });
      this.beds.theme = bed;
      bed.play();
    } catch {
      this.beds.theme = null;
    }
  },

  duckTheme(settings) {
    const bed = this.beds.theme;
    if (!bed || !this.bedsAllowed(settings)) return;
    const ducked = this.themeVolume(settings, 0.28);
    if (typeof bed.setVolume === 'function') bed.setVolume(ducked);
    if (this.unduckTimer && this.scene?.time) {
      this.scene.time.removeEvent(this.unduckTimer);
      this.unduckTimer = 0;
    }
    if (this.scene?.time) {
      this.unduckTimer = this.scene.time.delayedCall(780, () => {
        this.mixTheme(settings);
        this.unduckTimer = 0;
      });
    }
  },

  stopTheme() {
    if (this.unduckTimer && this.scene?.time) {
      this.scene.time.removeEvent(this.unduckTimer);
      this.unduckTimer = 0;
    }
    this.stopBed('theme');
  },

  rumble(level, settings) {
    if (!settings || this.silenced(settings)) return;
    this.setBed('roar', level > 0.02, settings, 0.14 + Math.min(0.32, level * 0.28));
  },

  now() {
    return this.ctx ? this.ctx.currentTime : 0;
  },

  ensureFallback() {
    if (this.ctx) return true;
    const AC = window.AudioContext || window.webkitAudioContext;
    if (!AC) return false;
    this.ctx = new AC();
    this.master = this.ctx.createGain();
    this.master.gain.value = 0.16;
    const graph = core.mixer.connect(this.ctx, this.ctx.destination);
    // Fallback tones: their own 0.16 trim -> limiter (bus gains already folded into setMute).
    this.master.connect(graph.limiter);
    core.lifecycle?.watch();
    if (this.ctx.state === 'suspended') this.ctx.resume();
    this.unlocked = true;
    return true;
  },

  tone(freq, dur, type, gain, slide) {
    if (!this.ensureFallback() || !this.master) return;
    const t = this.now();
    const osc = this.ctx.createOscillator();
    const g = this.ctx.createGain();
    osc.type = type || 'sine';
    osc.frequency.setValueAtTime(freq, t);
    if (slide) osc.frequency.exponentialRampToValueAtTime(Math.max(40, slide), t + dur);
    g.gain.setValueAtTime(gain || 0.04, t);
    g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    osc.connect(g);
    g.connect(this.master);
    osc.start(t);
    osc.stop(t + dur + 0.02);
  },

  toneFor(name) {
    switch (name) {
      case 'ui':
      case 'countdown':
        this.tone(2525, 0.08, 'sine', 0.03);
        break;
      case 'boost':
      case 'roar':
        this.tone(90, 0.16, 'sawtooth', 0.05, 60);
        break;
      case 'pickup':
      case 'fuel':
      case 'shield':
        this.tone(660, 0.1, 'triangle', 0.04, 880);
        break;
      case 'overdrive':
        this.tone(180, 0.2, 'sawtooth', 0.05, 420);
        break;
      case 'hit':
        this.tone(70, 0.18, 'square', 0.06, 40);
        break;
      case 'liftoff':
        this.tone(55, 0.4, 'sawtooth', 0.07, 140);
        break;
      case 'meco':
      case 'whoosh':
        this.tone(140, 0.22, 'triangle', 0.04, 90);
        break;
      case 'touchdown':
      case 'success':
      case 'recovered':
        this.tone(220, 0.28, 'sine', 0.05, 110);
        this.tone(330, 0.2, 'triangle', 0.03);
        break;
      case 'splash':
        this.tone(90, 0.3, 'sine', 0.05, 40);
        break;
      case 'rud':
        this.tone(48, 0.45, 'sawtooth', 0.07, 30);
        break;
      case 'maxq':
        this.tone(160, 0.18, 'sawtooth', 0.05, 220);
        break;
      default:
        this.tone(400, 0.06, 'sine', 0.025);
    }
  },
};

export default AudioApi;
