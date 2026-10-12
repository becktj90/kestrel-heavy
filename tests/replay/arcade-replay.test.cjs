// D0 recorded-replay regression gate (heavy; run with `npm run test:replay`).
// Replays the golden input traces for arcade KH-1..KH-5 × KID/CADET/PAD_RAT in
// headless Chrome under a seeded Math.random and a fixed 1/60 s clock, and
// asserts the phase sequence, bus events (beat times), outcome, score, medal and
// state checksums every 30 frames are unchanged. Re-record ONLY for intended
// behaviour changes: `npm run replay:record`.
const assert = require('node:assert/strict');
const { test, before, after } = require('node:test');
const fs = require('node:fs');
const path = require('node:path');

const DIR = path.join(__dirname, 'traces');
let H; let g;

before(async () => {
  H = await import(path.join(__dirname, '../../tools/replay/harness.mjs'));
  g = await H.openGame();
});
after(async () => { await g?.close(); });

function firstDiff(a, b) {
  const n = Math.max(a.length, b.length);
  for (let i = 0; i < n; i++) if (JSON.stringify(a[i]) !== JSON.stringify(b[i])) return { i, golden: a[i], now: b[i] };
  return null;
}

const files = fs.readdirSync(DIR).filter((f) => f.endsWith('.json')).sort();

test('golden traces cover every arcade mission × difficulty', () => {
  const want = [];
  for (const m of ['KH-1', 'KH-2', 'KH-3', 'KH-4', 'KH-5']) for (const d of ['KID', 'CADET', 'PAD_RAT']) want.push(`${m}_${d}.json`);
  assert.deepEqual(files, want.sort());
});

for (const f of files) {
  test(`replay ${f.replace('.json', '')}`, async () => {
    const gold = JSON.parse(fs.readFileSync(path.join(DIR, f), 'utf8'));
    const got = await g.run({ mission: gold.mission, difficulty: gold.difficulty, seed: gold.seed, mode: 'replay', trace: gold.input });
    const e = gold.expect;
    const cp = firstDiff(e.checkpoints, got.checkpoints);
    assert.equal(cp, null, `state diverged at checkpoint ${cp?.i}:\n golden ${JSON.stringify(cp?.golden)}\n now    ${JSON.stringify(cp?.now)}`);
    assert.deepEqual(got.phases, e.phases, 'phase sequence');
    assert.deepEqual(got.events, e.events, 'bus events / beat times');
    assert.deepEqual(got.result, e.result, 'outcome / score / medal');
    assert.equal(got.frames, e.frames);
    assert.deepEqual(g.errors, [], 'no page exceptions');
  });
}
