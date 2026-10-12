const assert = require('node:assert/strict');
const { test } = require('node:test');
const path = require('node:path');
const js = path.join(__dirname, '..', 'web/js');
const load = (f) => import(path.join(js, f));

test('bus delivers events in emit order with seq, rejects unknown types', async () => {
  const { createBus, EVENT_TYPES } = await load('narration/bus.js');
  const bus = createBus({ strict: true });
  const seen = [];
  bus.on('*', (e) => seen.push(`${e.type}@${e.seq}`));
  bus.emit('liftoff', {}, 1); bus.emit('maxq', {}, 1); bus.emit('meco', {}, 2);
  assert.deepEqual(seen, ['liftoff@1', 'maxq@2', 'meco@3']);
  assert.throws(() => bus.emit('nope'));
  assert.throws(() => bus.on('nope', () => {}));
  assert.ok(EVENT_TYPES.includes('abort') && EVENT_TYPES.includes('flight.begin'));
  const loose = createBus();
  assert.equal(loose.emit('nope'), null);
  assert.equal(loose.stats().dropped, 1);
});

test('a throwing subscriber does not break other subscribers', async () => {
  const { createBus } = await load('narration/bus.js');
  const bus = createBus();
  let got = 0;
  bus.on('liftoff', () => { throw new Error('x'); });
  bus.on('liftoff', () => { got += 1; });
  const warn = console.warn; console.warn = () => {};
  bus.emit('liftoff');
  console.warn = warn;
  assert.equal(got, 1);
});

test('every voice.js key and spine beat is a bus event and has a manifest entry', async () => {
  const { LINES, ABORTS } = await load('voice.js');
  const { isEventType } = await load('narration/bus.js');
  const { MANIFEST, CHANNEL } = await load('narration/manifest.js');
  for (const k of Object.keys(LINES)) {
    assert.ok(isEventType(k), `bus missing ${k}`);
    assert.ok(MANIFEST[k], `manifest missing ${k}`);
  }
  for (const a of Object.keys(ABORTS)) assert.ok(MANIFEST[`abort-${a}`]);
  const channels = new Set(Object.values(MANIFEST).flatMap((e) => e.lines.map((l) => l.channel)));
  for (const c of Object.values(CHANNEL)) assert.ok(channels.has(c), `no ${c} lines`);
  for (const e of Object.values(MANIFEST)) for (const l of e.lines) {
    for (const v of l.variants) {
      assert.ok(!/\b(SpaceX|NASA|Blue Origin|ULA|Rocket Lab)\b/i.test(v), v);
      if (l.channel === 'host' || l.channel === 'cohost') assert.ok(!/\d/.test(v), `host line has a number: ${v}`);
    }
  }
});
