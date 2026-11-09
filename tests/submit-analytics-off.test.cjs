const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

test('submitAnalytics is 0 on the Unity project (privacy / G-4)', () => {
  const asset = fs.readFileSync(
    path.join(__dirname, '..', 'unity/ProjectSettings/ProjectSettings.asset'),
    'utf8',
  );
  const m = asset.match(/submitAnalytics:\s*(\d)/);
  assert.ok(m, 'submitAnalytics key present');
  assert.equal(m[1], '0', 'submitAnalytics must stay 0 for Data Not Collected');
});
