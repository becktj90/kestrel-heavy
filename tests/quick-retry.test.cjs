const assert = require('node:assert/strict');
const fs = require('node:fs');
const { test } = require('node:test');
const path = require('node:path');

test('result screen retries landing at PACE.ENTRY and sep in one tap', () => {
  const root = path.join(__dirname, '..');
  const html = fs.readFileSync(path.join(root, 'web/index.html'), 'utf8');
  const mission = fs.readFileSync(path.join(root, 'web/js/mission.js'), 'utf8');
  const hud = fs.readFileSync(path.join(root, 'web/js/hud.js'), 'utf8');
  assert.match(html, /id="ng-sum-retry-land"[^>]*>Retry landing</);
  assert.match(html, /id="ng-sum-retry-sep"[^>]*>Retry from sep</);
  assert.match(mission, /retryLanding\(\) \{[\s\S]*?bootQaFlight\(PACE\.ENTRY, \(\) => this\.enterJacklyn\(\)\)/);
  assert.match(mission, /retryFromSep\(\) \{[\s\S]*?bootQaFlight\(PACE\.MECO - 0\.05/);
  assert.match(hud, /\['ng-sum-retry-land', handlers\.retryLanding\]/);
  assert.match(hud, /\['ng-sum-retry-sep', handlers\.retryFromSep\]/);
  assert.doesNotMatch(mission, /retryLanding\(\) \{[\s\S]*?confirm\(/);
});
