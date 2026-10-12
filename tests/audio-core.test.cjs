const assert = require('node:assert/strict');
const { test } = require('node:test');
const path = require('node:path');
const { MockAudioContext, Target, mockScene } = require('./audio-mock.cjs');
const dir = path.join(__dirname, '..', 'web', 'js', 'audio');
const imp = (f) => import(path.join(dir, f));

test('mixer: buses, defaults match pre-S1 settings math, limiter graph', async () => {
  const { createMixer, BUSES } = await imp('mixer.js');
  assert.deepEqual([...BUSES], ['master', 'music', 'sfx', 'engine', 'voice']);
  const m = createMixer();
  m.fromSettings({});
  assert.equal(m.effective('sfx'), 0.72);
  assert.ok(Math.abs(m.effective('music') - 0.72 * 0.85) < 1e-9);
  m.fromSettings({ volume: 2, sfxVolume: -1 });
  assert.equal(m.level.master, 1);
  assert.equal(m.level.sfx, 0);
  m.setMuted(true);
  assert.equal(m.effective('music'), 0);
  const ctx = new MockAudioContext();
  const g = m.connect(ctx);
  assert.equal(g.limiter.kind, 'comp');
  assert.equal(g.limiter.threshold.value, -1);
  assert.ok(g.nodes.music.out.includes(g.nodes.master));
  assert.ok(g.nodes.master.out.includes(g.limiter));
  assert.ok(g.limiter.out.includes(ctx.destination));
  assert.equal(g.nodes.master.gain.value, 0, 'muted master');
  assert.throws(() => m.setLevel('nope', 1));
});

test('voice pool: cap, priority stealing, pooling reuse', async () => {
  const { createVoicePool } = await imp('voices.js');
  const p = createVoicePool({ cap: 2 });
  let stopped = 0;
  const a = p.acquire('ui', 1); a.stop = () => { stopped += 1; };
  const b = p.acquire('hit', 1);
  const c = p.acquire('liftoff', 2);
  assert.ok(c);
  assert.equal(stopped, 1, 'oldest low-priority stolen');
  assert.equal(p.stats.stolen, 1);
  const d = p.acquire('ui', 0);
  assert.equal(d, null, 'lower priority cannot steal');
  b.release(); c.release();
  assert.equal(p.size, 0);
  const e = p.acquire('x', 0);
  assert.ok(e === b || e === c || e === a, 'slot objects reused');
});

test('ducking is driven by voice bus activity', async () => {
  const { createMixer } = await imp('mixer.js');
  const { createDucker, DUCK } = await imp('ducking.js');
  const m = createMixer();
  const timers = [];
  const d = createDucker(m, { setTimer: (f) => { timers.push(f); return timers.length; }, clearTimer: () => {} });
  d.voiceStart(); d.voiceStart();
  assert.equal(m.duck.music, DUCK.musicDuck);
  d.voiceEnd();
  assert.equal(m.duck.music, DUCK.musicDuck, 'still one voice');
  d.voiceEnd();
  timers.pop()();
  assert.equal(m.duck.music, 1);
  assert.equal(m.duck.sfx, 1);
});

test('lifecycle: visibility suspend/resume, interruption resumes on gesture', async () => {
  const { attachLifecycle } = await imp('lifecycle.js');
  const doc = new Target(); const win = new Target();
  const ctx = new MockAudioContext();
  let sus = 0;
  const lc = attachLifecycle({ doc, win, getContexts: () => [ctx], onSuspend: () => { sus += 1; } });
  doc.visibilityState = 'hidden'; doc.dispatch('visibilitychange');
  assert.equal(ctx.state, 'suspended'); assert.equal(sus, 1);
  doc.visibilityState = 'visible'; doc.dispatch('visibilitychange');
  assert.equal(ctx.state, 'running');
  ctx.fire('interrupted');
  ctx.state = 'interrupted';
  ctx.resume = function () { this.log.push('resume'); if (!this.gesture) return Promise.resolve(); this.state = 'running'; return Promise.resolve(); };
  assert.equal(lc.pendingGesture, true);
  ctx.gesture = true; win.dispatch('pointerdown');
  assert.equal(ctx.state, 'running');
  assert.equal(lc.pendingGesture, false);
  lc.detach();
});

test('bus adapter queues subscriptions until the shared bus connects', async () => {
  const { createAudioCore } = await imp('core.js');
  const core = createAudioCore();
  const handlers = {};
  const bus = { on: (t, f) => { (handlers[t] ||= []).push(f); }, off: (t, f) => { handlers[t] = handlers[t].filter((x) => x !== f); }, emit: (t, p) => (handlers[t] || []).forEach((f) => f({ type: t, payload: p })) };
  const seen = [];
  core.onEvent((e) => seen.push(e.type));
  assert.equal(core.connectBus(bus), true);
  bus.emit('liftoff'); bus.emit('maxq'); bus.emit('unknown');
  assert.deepEqual(seen, ['liftoff', 'maxq']);
  assert.equal(core.eventCounts.liftoff, 1);
  core.events.connect(null);
  assert.equal(handlers.liftoff.length, 0, 'unsubscribed on disconnect');
});

test('audio.js keeps behavior: same volumes, limiter inserted, pooled one-shots', async () => {
  global.window = new Target(); global.document = new Target();
  const { default: A } = await import(path.join(__dirname, '..', 'web', 'js', 'audio.js'));
  const scene = mockScene();
  A.attach(scene);
  const s = scene.sound;
  assert.equal(s.volume, 0.72, 'Phaser master = master*sfx as before');
  assert.equal(s.masterVolumeNode.out[0].kind, 'comp', 'limiter between Phaser master and destination');
  A.play('liftoff', scene.settings);
  assert.equal(scene.played.length, 1);
  assert.ok(Math.abs(scene.played[0].volume - 0.72 * 0.72) < 1e-9, 'spec.vol * sfxGain');
  A.play('liftoff', scene.settings);
  assert.equal(scene.played.length, 1, 'lock still applies');
  assert.equal(A.core.voices.size, 1);
  s.added[0].finish();
  assert.equal(A.core.voices.size, 0);
  assert.ok(s.added[0].destroyed);
  assert.ok(Math.abs(A.musicGain(scene.settings) - 0.72 * 0.85) < 1e-9);
  A.voiceStart();
  assert.ok(A.musicGain(scene.settings) < 0.72 * 0.85, 'voice ducks music');
  A.core.ducker.reset();
  A.play('hit', { ...scene.settings, muted: true });
  assert.equal(scene.played.length, 1, 'muted stays silent');
  A.core.lifecycle.detach();
  delete global.window; delete global.document;
});

test('audio core consumes the real shared flight bus (narration/bus.js)', async () => {
  const { flightBus } = await import(path.join(__dirname, '..', 'web', 'js', 'narration', 'bus.js'));
  const { default: A } = await import(path.join(__dirname, '..', 'web', 'js', 'audio.js'));
  A.attach(mockScene());
  const before = A.core.eventCounts.maxq || 0;
  flightBus.emit('maxq', {}, 60);
  assert.equal(A.core.eventCounts.maxq, before + 1);
});
