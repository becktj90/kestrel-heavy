const assert = require('node:assert/strict');
const { test } = require('node:test');
const path = require('node:path');
const js = path.join(__dirname, '..', 'web/js');
const load = (f) => import(path.join(js, f));

async function mk(opts) {
  const { createQueue } = await load('narration/queue.js');
  const { PRIORITY } = await load('narration/manifest.js');
  return { q: createQueue({ readingSeconds: () => 2, ...opts }), P: PRIORITY };
}

test('higher priority first, then event order', async () => {
  const { q, P } = await mk();
  q.push({ id: 'a', channel: 'coach', priority: P.LOW, text: 'low', t: 2 });
  q.push({ id: 'b', channel: 'mc', priority: P.HIGH, text: 'high', t: 0 });
  q.push({ id: 'c', channel: 'mc', priority: P.HIGH, text: 'high2', t: 0 });
  assert.equal(q.tick(0).text, 'high');
  assert.equal(q.tick(2).text, 'high2');
  assert.equal(q.tick(4).text, 'low');
});

test('nothing overlaps: a non-critical line waits for the current one', async () => {
  const { q, P } = await mk();
  q.push({ id: 'a', channel: 'mc', priority: P.NORMAL, text: 'one', t: 0 });
  q.tick(0);
  q.push({ id: 'b', channel: 'mc', priority: P.HIGH, text: 'two', t: 0.5 });
  assert.equal(q.tick(1).text, 'one');
  assert.equal(q.tick(2).text, 'two');
});

test('critical preempts the line on air', async () => {
  const { q, P } = await mk();
  q.push({ id: 'host', channel: 'host', priority: P.LOW, text: 'chat', t: 0 });
  q.tick(0);
  q.push({ id: 'maxq', channel: 'mc', priority: P.CRITICAL, text: 'Max-Q.', t: 0.5 });
  assert.equal(q.tick(0.5).text, 'Max-Q.');
  assert.ok(q.log().some((l) => l.kind === 'preempt' && l.id === 'host'));
});

test('stale host lines are dropped, MC lines survive longer', async () => {
  const { q, P } = await mk();
  q.push({ id: 'mc', channel: 'mc', priority: P.HIGH, text: 'callout', t: 0 });
  q.push({ id: 'h', channel: 'host', priority: P.LOW, text: 'host says', t: 0 });
  q.push({ id: 'm2', channel: 'mc', priority: P.NORMAL, text: 'mc later', t: 0 });
  q.tick(0); // callout airs 0..2
  q.push({ id: 'block', channel: 'mc', priority: P.HIGH, text: 'block', t: 1 });
  assert.equal(q.tick(2).text, 'block'); // 2..4
  assert.equal(q.tick(4).text, 'mc later'); // mc still fresh at 4s
  assert.equal(q.tick(6), null); // host line (t=0) stale by 6s
  assert.ok(q.log().some((l) => l.kind === 'stale' && l.id === 'h'));
});

test('same line does not repeat immediately', async () => {
  const { q, P } = await mk();
  q.push({ id: 'align-green', channel: 'coach', priority: P.NORMAL, text: 'ALIGN green.', t: 0 });
  q.tick(0);
  q.push({ id: 'align-green', channel: 'coach', priority: P.NORMAL, text: 'ALIGN green.', t: 2 });
  assert.equal(q.tick(2), null);
  assert.ok(q.log().some((l) => l.kind === 'repeat'));
});

test('reading-speed timing scales with text and is clamped', async () => {
  const { readingSeconds } = await load('narration/queue.js');
  assert.equal(readingSeconds('Hi'), 1.4);
  assert.ok(readingSeconds('x'.repeat(60)) > readingSeconds('x'.repeat(20)));
  assert.equal(readingSeconds('x'.repeat(500)), 7);
});
