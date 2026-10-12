// data/golden/README.md table must list every golden file once, in index.json order, with matching case counts.
const assert = require('node:assert/strict');
const { test } = require('node:test');
const fs = require('node:fs');
const path = require('node:path');

test('golden README table matches index.json (order, no duplicates, counts)', () => {
  const dir = path.join(__dirname, '..', 'data', 'golden');
  const idx = JSON.parse(fs.readFileSync(path.join(dir, 'index.json'), 'utf8')).files;
  const rows = fs.readFileSync(path.join(dir, 'README.md'), 'utf8').split('\n')
    .map((l) => l.match(/^\| (\S+)\.json \| (\d+) \|/)).filter(Boolean).map((m) => [m[1], Number(m[2])]);
  const names = rows.map((r) => r[0]);
  assert.equal(new Set(names).size, names.length, `duplicate rows: ${names}`);
  assert.deepEqual(names, Object.keys(idx), 'README rows must follow index.json order');
  for (const [n, c] of rows) assert.equal(c, idx[n], `${n} case count`);
});
