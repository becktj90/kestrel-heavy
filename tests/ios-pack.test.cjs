// The iOS bundle copy (ios/KestrelHeavy/Game) is generated from web/ by
// ios/scripts/pack_kestrelheavy_game.py. Game JS, audio and vendor files must match.
const assert = require('node:assert');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const root = path.join(__dirname, '..');
function walk(d, base = d) {
  return fs.readdirSync(d, { withFileTypes: true }).flatMap((e) =>
    e.isDirectory() ? walk(path.join(d, e.name), base) : [path.relative(base, path.join(d, e.name))]);
}
test('iOS game pack matches web/ (run npm run pack:ios)', () => {
  for (const sub of ['js', 'audio', 'vendor']) {
    const src = path.join(root, 'web', sub);
    for (const f of walk(src)) {
      if (f.endsWith('.md')) continue;
      const a = fs.readFileSync(path.join(src, f));
      const b = fs.readFileSync(path.join(root, 'ios/KestrelHeavy/Game', sub, f));
      assert.ok(a.equals(b), `${sub}/${f} differs`);
    }
  }
});
