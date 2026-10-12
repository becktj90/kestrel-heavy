#!/usr/bin/env node
// Checks App Store Connect field limits for the ```asc:<field> blocks in docs/APPSTORE_CONNECT.md.
// Limits: docs/APPSTORE_REQUIREMENTS.md §4 (Apple ASC Help, accessed 2026-10-11). Exit 1 on any violation.
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const file = process.argv[2] ?? resolve(root, 'docs/APPSTORE_CONNECT.md');
const md = readFileSync(file, 'utf8');

const LIMITS = {
  name: { max: 30, min: 2, unit: 'chars' },
  subtitle: { max: 30, unit: 'chars' },
  promotional_text: { max: 170, unit: 'chars' },
  description: { max: 4000, unit: 'chars' },
  keywords: { max: 100, unit: 'bytes' },
  whats_new: { max: 4000, unit: 'chars' },
  review_notes: { max: 4000, unit: 'bytes' },
  testflight_whats_to_test: { max: 4000, unit: 'chars' },
  testflight_description: { max: 4000, unit: 'chars' },
};

const fields = {};
for (const m of md.matchAll(/```asc:([a-z_]+)\n([\s\S]*?)\n```/g)) fields[m[1]] = m[2];

const len = (s, unit) => (unit === 'bytes' ? Buffer.byteLength(s, 'utf8') : [...s].length);
let bad = 0;
const row = (ok, k, msg) => { console.log(`${ok ? 'ok  ' : 'FAIL'} ${k.padEnd(26)} ${msg}`); if (!ok) bad++; };

for (const k of ['name', 'subtitle', 'promotional_text', 'description', 'keywords']) if (!(k in fields)) row(false, k, 'missing');
for (const [k, v] of Object.entries(fields)) {
  const lim = LIMITS[k];
  if (!lim) { row(false, k, 'unknown field'); continue; }
  const n = len(v, lim.unit);
  row(n <= lim.max && n >= (lim.min ?? 1), k, `${n}/${lim.max} ${lim.unit}`);
  if (/<[a-z][^>]*>/i.test(v)) row(false, k, 'HTML not allowed');
}
if (fields.keywords) {
  const kws = fields.keywords.split(',').map((s) => s.trim());
  for (const w of kws) if (w.length <= 2) row(false, 'keywords', `"${w}" must be > 2 chars`);
  if (fields.keywords.includes(', ')) row(false, 'keywords', 'spaces after commas waste bytes');
  const dupes = kws.filter((w, i) => kws.indexOf(w) !== i);
  if (dupes.length) row(false, 'keywords', `duplicates: ${dupes.join(',')}`);
  const nameWords = (fields.name ?? '').toLowerCase().split(/\s+/);
  const rep = kws.filter((w) => nameWords.includes(w.toLowerCase()));
  if (rep.length) row(false, 'keywords', `repeats app name: ${rep.join(',')}`);
}
console.log(bad ? `\n${bad} problem(s)` : '\nall fields within limits');
process.exit(bad ? 1 : 0);
