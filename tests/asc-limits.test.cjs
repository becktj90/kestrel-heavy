const { test } = require('node:test');
const assert = require('node:assert');
const { spawnSync } = require('node:child_process');
const path = require('node:path');
test('App Store Connect fields are within Apple limits', () => {
  const r = spawnSync(process.execPath, [path.join(__dirname, '..', 'tools/asc/check_limits.mjs')], { encoding: 'utf8' });
  assert.strictEqual(r.status, 0, r.stdout + r.stderr);
});
