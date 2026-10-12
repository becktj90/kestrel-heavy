// Phaser iOS will not ship. ios/KestrelHeavy/Game stays frozen.
// web/ is the demo and the design reference for Unity, so it may move ahead
// of that pack. Do not byte-compare them and do not rewrite the iOS tree.
const assert = require('node:assert');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const root = path.join(__dirname, '..');
test('Phaser iOS pack is frozen; web/ is the Unity design reference', () => {
  assert.equal(fs.existsSync(path.join(root, 'ios/scripts/pack_kestrelheavy_game.py')), true);
  assert.equal(fs.existsSync(path.join(root, 'ios/KestrelHeavy/Game/js/main.js')), true);
  assert.equal(fs.existsSync(path.join(root, 'web/js/main.js')), true);
});
