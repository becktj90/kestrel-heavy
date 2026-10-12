// data/visual-spec.json is the machine-readable copy of docs/VISUAL_SPEC.md §6 that the Unity
// shell (and any other engine) loads. This test fails if the two drift apart.
const test = require('node:test');
const assert = require('node:assert');
const fs = require('node:fs');
const path = require('node:path');

const root = path.join(__dirname, '..');
const doc = fs.readFileSync(path.join(root, 'docs', 'VISUAL_SPEC.md'), 'utf8');
const json = JSON.parse(fs.readFileSync(path.join(root, 'data', 'visual-spec.json'), 'utf8'));

test('data/visual-spec.json matches VISUAL_SPEC.md §6', () => {
  const m = doc.match(/## 6\. Machine-readable tables\s+```json\s+([\s\S]*?)```/);
  assert.ok(m, 'VISUAL_SPEC.md §6 json block not found');
  assert.deepStrictEqual(json, JSON.parse(m[1]));
});

test('portrait table is portrait-only, never letterboxed, and has the four device classes', () => {
  const p = json.portrait;
  assert.strictEqual(p.letterbox, false);
  assert.deepStrictEqual(p.devices.map(d => d.class), ['iphone', 'iphone-promax', 'ipad', 'ipad-large']);
  for (const d of p.devices) assert.ok(d.pt[1] > d.pt[0], `${d.class} must be portrait`);
});

test('envelop scale on portrait is height-dominated (VISUAL_SPEC §5 visible widths)', () => {
  const { W, H } = json.world;
  const want = { iphone: 332, 'iphone-promax': 331, ipad: 500, 'ipad-large': 540 };
  for (const d of json.portrait.devices) {
    const [vw, vh] = d.pt;
    const s = Math.max(vw / W, vh / H);
    assert.strictEqual(s, vh / H);
    assert.ok(Math.abs(vw / s - want[d.class]) < 1.5, `${d.class}: ${vw / s}`);
  }
});

test('data/tod.json is the current export of web/js/tod.js (run npm run gen:tod)', async () => {
  const { render } = await import(path.join(root, 'tools', 'export-tod.mjs'));
  const onDisk = fs.readFileSync(path.join(root, 'data', 'tod.json'), 'utf8');
  assert.strictEqual(onDisk, render());
});
