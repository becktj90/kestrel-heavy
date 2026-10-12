// Codex lessons: eight topics, kid length, sim numbers, no real company names.
const assert = require('node:assert');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

const book = JSON.parse(fs.readFileSync(path.join(__dirname, '..', 'data', 'codex.json'), 'utf8'));
const ids = ['delta-v', 'twr', 'isp', 'staging', 'max-q', 'cg-cp', 'orbits', 'booster-landing'];
const banned = ['Blue Origin', 'New Glenn', 'Jacklyn', 'LC-36', 'SpaceX', 'NASA', 'Boeing', 'Lockheed', 'Ariane', 'Soyuz', 'Falcon', 'Starship', 'Saturn', 'Apollo', 'suicide', 'RP-1', '\u2014', '\u2013'];

test('codex has the eight lessons and stays inside the kid limit', () => {
  assert.strictEqual(book.version, 1);
  assert.deepStrictEqual(book.entries.map((e) => e.id), ids);
  const seen = new Set();
  for (const e of book.entries) {
    assert.ok(!seen.has(e.id), e.id);
    seen.add(e.id);
    assert.ok(e.kid.length > 0 && e.kid.length <= 300, `${e.id} kid ${e.kid.length}`);
    assert.ok(e.engineer.length >= 80, e.id);
    assert.ok(e.sources.length > 0, e.id);
    const sentences = e.kid.split(/(?<=\.)\s+/).map((s) => s.trim()).filter(Boolean);
    for (const s of sentences) assert.ok(s.split(/\s+/).length <= 12, `${e.id}: ${s}`);
    const text = `${e.title}\n${e.kid}\n${e.engineer}`;
    for (const ban of banned) assert.ok(!text.includes(ban), `${e.id} contains ${ban}`);
  }
  assert.match(book.entries[0].engineer, /8876/);
  assert.match(book.entries[0].engineer, /9\.80665/);
  assert.match(book.entries.find((e) => e.id === 'orbits').engineer, /11330/);
  assert.doesNotMatch(book.entries.find((e) => e.id === 'orbits').engineer, /9\.4/);
  assert.match(book.entries.find((e) => e.id === 'booster-landing').engineer, /landing burn/);
  assert.doesNotMatch(JSON.stringify(book), /suicide/);
});
