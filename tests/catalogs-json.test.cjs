// Step 0: data/*.json is the catalog source of truth; web/js/data/gen/*.js mirrors must match,
// every catalog must satisfy its JSON Schema, and data/golden must match a fresh export.
const assert = require('node:assert');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const test = require('node:test');
const { execFileSync } = require('node:child_process');
const root = path.join(__dirname, '..');
const NAMES = ['propellants', 'engines', 'materials', 'payloads', 'presets'];
const readJSON = (...p) => JSON.parse(fs.readFileSync(path.join(root, ...p), 'utf8'));

// Minimal JSON Schema (2020-12 subset) validator: type, const, enum, required, properties,
// additionalProperties, items, minItems, maxItems, minimum, maximum, exclusiveMinimum, pattern, $ref (#/$defs).
function validate(schema, v, at, root, errs) {
  if (schema.$ref) return validate(schema.$ref.split('/').slice(1).reduce((o, k) => o[k], root), v, at, root, errs);
  const type = (x) => (x === null ? 'null' : Array.isArray(x) ? 'array' : Number.isInteger(x) ? 'integer' : typeof x);
  if (schema.type) {
    const ts = [].concat(schema.type), t = type(v);
    if (!ts.includes(t) && !(t === 'integer' && ts.includes('number'))) { errs.push(`${at}: type ${t} not ${ts}`); return; }
  }
  if ('const' in schema && v !== schema.const) errs.push(`${at}: != const`);
  if (schema.enum && !schema.enum.includes(v)) errs.push(`${at}: not in enum`);
  if (typeof v === 'number') {
    if (schema.minimum != null && v < schema.minimum) errs.push(`${at}: < minimum`);
    if (schema.maximum != null && v > schema.maximum) errs.push(`${at}: > maximum`);
    if (schema.exclusiveMinimum != null && v <= schema.exclusiveMinimum) errs.push(`${at}: <= exclusiveMinimum`);
  }
  if (typeof v === 'string' && schema.pattern && !new RegExp(schema.pattern).test(v)) errs.push(`${at}: pattern`);
  if (Array.isArray(v)) {
    if (schema.minItems != null && v.length < schema.minItems) errs.push(`${at}: minItems`);
    if (schema.maxItems != null && v.length > schema.maxItems) errs.push(`${at}: maxItems`);
    if (schema.items) v.forEach((x, i) => validate(schema.items, x, `${at}[${i}]`, root, errs));
  } else if (v && typeof v === 'object') {
    for (const k of schema.required || []) if (!(k in v)) errs.push(`${at}: missing ${k}`);
    for (const [k, x] of Object.entries(v)) {
      if (schema.properties && k in schema.properties) validate(schema.properties[k], x, `${at}.${k}`, root, errs);
      else if (schema.additionalProperties === false) errs.push(`${at}: unexpected ${k}`);
      else if (schema.additionalProperties && typeof schema.additionalProperties === 'object') validate(schema.additionalProperties, x, `${at}.${k}`, root, errs);
    }
  }
}

for (const n of NAMES) {
  test(`data/${n}.json satisfies data/schemas/${n}.schema.json`, () => {
    const schema = readJSON('data', 'schemas', `${n}.schema.json`), errs = [];
    validate(schema, readJSON('data', `${n}.json`), n, schema, errs);
    assert.deepStrictEqual(errs, []);
  });
  test(`web/js/data/gen/${n}.js mirrors data/${n}.json (run node tools/gen-catalogs.mjs)`, async () => {
    const json = readJSON('data', `${n}.json`); delete json.$schema;
    const { default: gen } = await import(path.join(root, 'web', 'js', 'data', 'gen', `${n}.js`));
    assert.deepStrictEqual(JSON.parse(JSON.stringify(gen)), json);
  });
}

test('schema validator rejects a bad catalog entry', () => {
  const schema = readJSON('data', 'schemas', 'engines.schema.json'), data = readJSON('data', 'engines.json'), errs = [];
  data.engines[0] = { ...data.engines[0], cycle: 'warp', bogus: 1 };
  validate(schema, data, 'engines', schema, errs);
  assert.ok(errs.some((e) => e.includes('enum')) && errs.some((e) => e.includes('bogus')), errs.join('; '));
});

test('loaders expose the catalogs and lookups', async () => {
  const js = path.join(root, 'web', 'js', 'data');
  const { PRESETS, PRESET_BY_ID } = await import(path.join(js, 'presets.js'));
  const { ENGINES, ENGINE_BY_ID, ENGINE_CYCLES } = await import(path.join(js, 'engines.js'));
  const { TEMPLATES } = await import(path.join(js, 'templates', 'index.js')); // T1 starter templates ride along
  assert.strictEqual(PRESETS.length, readJSON('data', 'presets.json').presets.length + TEMPLATES.length);
  assert.strictEqual(PRESET_BY_ID.stock.stages[0].engineCount, 9);
  assert.ok(Object.isFrozen(ENGINES) && ENGINE_BY_ID['gg-kerolox'] && ENGINE_CYCLES.includes('full-flow'));
});

test('data/golden matches a fresh export (run node tools/export-golden.mjs)', () => {
  const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'golden-'));
  execFileSync(process.execPath, [path.join(root, 'tools', 'export-golden.mjs'), '--out', tmp], { stdio: 'pipe' });
  const names = fs.readdirSync(tmp).filter((f) => f.endsWith('.json')).sort();
  assert.deepStrictEqual(names, fs.readdirSync(path.join(root, 'data', 'golden')).filter((f) => f.endsWith('.json')).sort());
  for (const f of names) assert.deepStrictEqual(JSON.parse(fs.readFileSync(path.join(tmp, f), 'utf8')), readJSON('data', 'golden', f), f);
  assert.ok(readJSON('data', 'golden', 'index.json').total >= 100);
});
