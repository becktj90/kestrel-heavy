// Launch time of day (web/js/flight/launch-time.js), the BigInt port of Kestrel.Core LaunchTime.
// Replays data/golden/launch-time.json, the same file Kestrel.Core.Tests/LaunchTimeGoldenTests runs.
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const root = path.join(__dirname, '..');
const load = () => import('../web/js/flight/launch-time.js');
const readJSON = (...p) => JSON.parse(fs.readFileSync(path.join(root, ...p), 'utf8'));

test('published FNV-1a 64 vectors and non-ASCII ids hash their UTF-8 bytes', async () => {
  const { fnv1a64, hash, hex } = await load();
  // Published FNV-1a 64 test vectors (Fowler/Noll/Vo).
  assert.equal(hex(fnv1a64('')), 'cbf29ce484222325');
  assert.equal(hex(fnv1a64('a')), 'af63dc4c8601ec8c');
  assert.equal(hex(fnv1a64('foobar')), '85944171f73967e8');
  assert.equal(hex(fnv1a64(null)), 'cbf29ce484222325');
  // Computed outside this file (Python, bytes of s.encode('utf-8')).
  assert.equal(hex(fnv1a64('é')), '0ac21707b7181e01');
  assert.equal(hex(hash('KH-é', 0)), '1be063d5f4d579ad');
  assert.equal(hex(hash('KH-🚀', 0)), '8b98646cb4673f6e');
  // A lone surrogate is U+FFFD (EF BF BD), like Encoding.UTF8 in C#.
  assert.equal(hex(fnv1a64('\uD800')), '6f6d661b9658624a');
  // Shipped literal from PR #272 (ASCII: UTF-8 bytes equal the old UTF-16 code units).
  assert.equal(hash('KH-1', 0), 0xC39D71C85DA55BA0n);
  assert.equal(hash('KH-2', 1), 0x5EE1AAD81086F3B8n);
  assert.equal(hash('', 7), 0x4BD7A317074C5B62n);
  assert.equal(hash('KH-1', 0n), hash('KH-1', 0));
  assert.throws(() => hash('KH-1', -1));
  assert.throws(() => hash('KH-1', 2 ** 53));
});

test('data/golden/launch-time.json replays exactly (shared with C#)', async () => {
  const { fnv1a64, hash, hex, parseTable, pick } = await load();
  const golden = readJSON('data', 'golden', 'launch-time.json');
  const table = parseTable(readJSON('data', 'time-of-day.json'));
  assert.equal(golden.count, golden.cases.length);
  let picks = 0;
  for (const c of golden.cases) {
    if (c.fn === 'fnv1a64') {
      assert.equal(hex(fnv1a64(c.in.text)), c.out.hash, JSON.stringify(c.in));
    } else {
      assert.equal(c.fn, 'pick');
      const seed = BigInt(c.in.seed);
      assert.equal(hex(hash(c.in.id, seed)), c.out.hash, JSON.stringify(c.in));
      assert.equal(pick(table, c.in.id, seed), c.out.pick, JSON.stringify(c.in));
      picks++;
    }
  }
  assert.ok(picks >= 150, 'pick cases');
  assert.ok(golden.cases.some((c) => c.fn === 'pick' && /[^\x00-\x7f]/.test(c.in.id)), 'a non-ASCII id');
});

test('JS picks follow the table: KH-1 pinned, unknown ids use the default, seeds vary', async () => {
  const { parseTable, pick } = await load();
  const table = parseTable(readJSON('data', 'time-of-day.json'));
  for (let s = 0; s < 64; s++) assert.equal(pick(table, 'KH-1', s), 'Day');
  assert.equal(pick(table, 'KH-7', 42), 'Day');
  assert.equal(pick(table, 'KH-7', 43), 'Sunset');
  assert.equal(pick(table, 'KH-20', 123456789), 'Night');
  assert.equal(pick(table, 'KH-99', 2), 'Sunset');
  assert.equal(pick(table, null, 7), pick(table, '', 7));
  const seen = new Set();
  for (let s = 0; s < 500; s++) seen.add(pick(table, 'KH-9', s));
  assert.equal(seen.size, 3);
  assert.throws(() => parseTable({ default: { day: 1 }, missions: { X: { fixed: 'Noon' } } }));
  assert.throws(() => parseTable({ default: { day: 0, sunset: 0, night: 0 } }));
});

test('per-mission distribution stays inside 3 sigma (JS, smaller N than C#)', async () => {
  const { parseTable, pick } = await load();
  const json = readJSON('data', 'time-of-day.json');
  const table = parseTable(json);
  const N = 4000;
  for (const [id, row] of Object.entries(json.missions)) {
    if (row.fixed) continue;
    const total = row.day + row.sunset + row.night;
    const n = { Day: 0, Sunset: 0, Night: 0 };
    for (let s = 0; s < N; s++) n[pick(table, id, s)]++;
    for (const [name, w] of [['Day', row.day], ['Sunset', row.sunset], ['Night', row.night]]) {
      const p = w / total;
      const band = 3 * Math.sqrt((p * (1 - p)) / N);
      assert.ok(Math.abs(n[name] / N - p) <= band, `${id} ${name}: ${n[name] / N} vs ${p} ± ${band}`);
    }
  }
});
