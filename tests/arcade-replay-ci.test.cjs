// Runs the heavy D0 replay gate inside `npm test` ONLY on CI (GitHub sets CI=true),
// so local `npm test` stays fast. Locally: `npm run test:replay`.
const assert = require('node:assert/strict');
const { test } = require('node:test');
const { spawnSync } = require('node:child_process');
const path = require('node:path');

test('D0 arcade replay gate (CI only)', { skip: process.env.CI !== 'true' && 'set CI=true or run `npm run test:replay`', timeout: 8 * 60 * 1000 }, () => {
  const r = spawnSync(process.execPath, ['--experimental-websocket', '--test', '--test-concurrency=1', path.join(__dirname, 'replay')],
    { cwd: path.join(__dirname, '..'), encoding: 'utf8', env: { ...process.env, NODE_TEST_CONTEXT: undefined } });
  const tail = `${r.stdout}\n${r.stderr}`.split('\n').filter((l) => /^(not ok|ok|# (pass|fail))|diverged|golden|now /.test(l.trim())).join('\n');
  assert.equal(r.status, 0, `replay gate failed:\n${tail}`);
});
