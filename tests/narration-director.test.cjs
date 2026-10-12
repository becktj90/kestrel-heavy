const assert = require('node:assert/strict');
const { test } = require('node:test');
const path = require('node:path');
const js = path.join(__dirname, '..', 'web/js');
const load = (f) => import(path.join(js, f));

async function setup(opts) {
  const { createBus } = await load('narration/bus.js');
  const { createDirector } = await load('narration/director.js');
  const bus = createBus({ strict: true });
  const d = createDirector(bus, { queueOpts: { readingSeconds: () => 1 }, ...opts });
  return { bus, d };
}

test('flight events produce captions in event order; MC text matches the HUD radio', async () => {
  const { bus, d } = await setup({ hosts: false });
  bus.emit('flight.begin', { missionId: 'KH-1', payload: 'Sat' }, 0);
  bus.emit('ignition', { text: 'Ignition radio' }, 0);
  bus.emit('liftoff', { text: 'Liftoff radio' }, 2);
  bus.emit('maxq', { text: 'MaxQ radio' }, 10);
  const aired = [];
  for (let t = 0; t <= 12; t += 0.25) { const c = d.tick(t); if (c && aired.at(-1) !== c.text) aired.push(c.text); }
  assert.deepEqual(aired, ['Ignition radio', 'Liftoff radio', 'MaxQ radio']);
});

test('abort flushes chatter and no nominal commentary follows it', async () => {
  const { bus, d } = await setup();
  bus.emit('flight.begin', { missionId: 'KH-1' }, 0);
  bus.emit('liftoff', { text: 'Liftoff.' }, 0);
  bus.emit('abort', { abortId: 'maxq', text: 'Vehicle lost at Max-Q.' }, 0.2);
  bus.emit('fairing', {}, 0.3);
  const aired = [];
  for (let t = 0; t <= 15; t += 0.1) { const c = d.tick(t); if (c && aired.at(-1)?.text !== c.text) aired.push(c); }
  const i = aired.findIndex((c) => c.text === 'Vehicle lost at Max-Q.');
  assert.ok(i >= 0);
  assert.ok(aired.slice(i + 1).every((c) => c.id.startsWith('abort-') || c.channel === 'mc'), JSON.stringify(aired));
  assert.ok(!aired.some((c) => c.id === 'fairing' && c.channel === 'host'));
});

test('host variants rotate across flights', async () => {
  const { bus, d } = await setup();
  const hostLines = [];
  for (let f = 0; f < 3; f += 1) {
    const t0 = f * 100;
    bus.emit('flight.begin', { missionId: 'KH-1', payload: 'Sat' }, t0);
    bus.emit('fairing', { text: 'Fairing.' }, t0);
    for (let t = t0; t < t0 + 10; t += 0.25) { const c = d.tick(t); if (c?.channel === 'host' && hostLines.at(-1) !== c.text) hostLines.push(c.text); }
  }
  assert.equal(new Set(hostLines).size, 3);
});
