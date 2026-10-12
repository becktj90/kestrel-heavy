// Exports web/js/tod.js TOD palettes to data/tod.json for non-JS engines (VISUAL_SPEC §4/§7:
// "engines other than Phaser need a generated export of tod.js"). tod.js stays the source.
// wash colours become "#rrggbb" strings. tests/visual-spec.test.cjs fails on drift.
// Usage: node tools/export-tod.mjs
import { writeFileSync } from 'node:fs';
import { TOD } from '../web/js/tod.js';

const hex = (n) => '#' + n.toString(16).padStart(6, '0');
export function render() {
  const out = { source: 'web/js/tod.js#TOD', palettes: {} };
  for (const [id, p] of Object.entries(TOD)) {
    out.palettes[id] = {
      label: p.label,
      sky: p.sky.map(([pos, c]) => ({ pos, color: c })),
      wash: Object.fromEntries(Object.entries(p.wash).map(([k, v]) => [k, hex(v)])),
      stars: p.stars,
    };
  }
  return JSON.stringify(out, null, 2) + '\n';
}
if (import.meta.url === `file://${process.argv[1]}`) {
  writeFileSync(new URL('../data/tod.json', import.meta.url), render());
  console.log('data/tod.json written');
}
