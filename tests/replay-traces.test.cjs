// Fast sanity check of the D0 golden traces (the heavy replay is `npm run test:replay`).
const assert = require('node:assert/strict');
const { test } = require('node:test');
const fs = require('node:fs');
const path = require('node:path');

test('replay traces are well-formed and cover KH-1..5 × all difficulties', () => {
  const dir = path.join(__dirname, 'replay', 'traces');
  for (const m of ['KH-1', 'KH-2', 'KH-3', 'KH-4', 'KH-5']) {
    for (const d of ['KID', 'CADET', 'PAD_RAT']) {
      const t = JSON.parse(fs.readFileSync(path.join(dir, `${m}_${d}.json`), 'utf8'));
      assert.equal(t.mission, m); assert.equal(t.difficulty, d);
      assert.ok(Number.isInteger(t.seed));
      assert.ok(t.input.length > 0, 'has inputs');
      for (const [f, code, down] of t.input) { assert.ok(Number.isInteger(f) && f >= 0); assert.ok(['ArrowLeft', 'ArrowRight', 'Space'].includes(code)); assert.ok(down === 0 || down === 1); }
      assert.ok(t.expect.checkpoints.length > 10);
      assert.ok(t.expect.phases.some(([, p]) => p.startsWith('ASCENT')), 'reaches ascent');
      assert.ok(['salvage', 'rud', 'landed', 'splash', 'timeout'].includes(t.expect.result.outcome) || typeof t.expect.result.outcome === 'string');
    }
  }
});
