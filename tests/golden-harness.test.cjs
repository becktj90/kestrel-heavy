// Replay catalog-flight golden vectors through the reference physics and compare
// with the absolute/relative bands in docs/PHYSICS.md.
const assert = require('node:assert');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

const root = path.join(__dirname, '..');
const docPath = path.join(root, 'docs', 'PHYSICS.md');
const goldenPath = path.join(root, 'data', 'golden', 'catalog-flight.json');

const fin = (v) => {
  if (typeof v === 'number') return Number.isFinite(v) ? v : String(v);
  if (Array.isArray(v)) return v.map(fin);
  if (v && typeof v === 'object') return Object.fromEntries(Object.entries(v).map(([k, x]) => [k, fin(x)]));
  return v;
};

function proseTable(md) {
  const fence = md.indexOf('```golden-tolerances');
  assert.ok(fence > 0, 'golden-tolerances block');
  const rows = {};
  for (const line of md.slice(0, fence).split('\n')) {
    const m = line.match(/^\| ([A-Za-z][\w]*) \| ([^|]+) \| ([^|]+) \| (.+) \|$/);
    if (!m || m[1] === 'quantity') continue;
    rows[m[1]] = { abs: Number(m[2].trim()), rel: Number(m[3].trim()), reason: m[4].trim() };
  }
  return rows;
}

test('docs/PHYSICS.md bands match the table the harness reads', async () => {
  const { loadTolerances } = await import(path.join(root, 'tools', 'golden-compare.mjs'));
  const md = fs.readFileSync(docPath, 'utf8');
  const loaded = loadTolerances(md);
  const rows = proseTable(md);
  for (const [name, q] of Object.entries(loaded.spec.quantities)) {
    assert.ok(rows[name], `table row ${name}`);
    assert.strictEqual(rows[name].abs, q.abs, name);
    assert.strictEqual(rows[name].rel, q.rel, name);
    assert.ok(rows[name].reason.length > 40, `${name} reasoning`);
  }
  const prose = md.slice(0, md.indexOf('```golden-tolerances'));
  for (const key of loaded.spec.exactKeys) assert.ok(prose.includes(key), key);
  assert.ok(loaded.spec.overlays.orbitClosedForm);
  assert.ok(prose.includes('npm run gen:golden'));
});

test('comparator honours absolute and relative bands, and exact keys', async () => {
  const { loadTolerances, within, compareTree } = await import(path.join(root, 'tools', 'golden-compare.mjs'));
  const loaded = loadTolerances(fs.readFileSync(docPath, 'utf8'));
  const time = loaded.spec.quantities.time;
  const slack = time.abs + time.rel * 1000;
  assert.strictEqual(within(1000 + slack * 0.5, 1000, time), true);
  assert.strictEqual(within(1000 - slack * 0.5, 1000, time), true);
  assert.strictEqual(within(1000 + slack * 2, 1000, time), false);

  const peak = loaded.spec.quantities.maxQTime;
  assert.strictEqual(peak.rel, 0);
  assert.strictEqual(within(40 + peak.abs * 0.5, 40, peak), true);
  assert.strictEqual(within(40 + peak.abs * 2, 40, peak), false);
  assert.strictEqual(within(1000 + peak.abs * 2, 1000, peak), false);

  assert.ok(compareTree({ stage: 2 }, { stage: 1 }, loaded, '$.event').length > 0);
  assert.ok(compareTree({ surprise: 1 }, { surprise: 1 }, loaded, '$.x').some((e) => e.includes('no tolerance')));
});

test('catalog-flight covers every engine and preset within documented tolerances', async () => {
  const { loadTolerances, compareTree } = await import(path.join(root, 'tools', 'golden-compare.mjs'));
  const { evaluateFlight, assertCatalogCoverage, CATALOG_FLIGHT_DT, KINDS } = await import(path.join(root, 'tools', 'golden-flight.mjs'));
  const { orbitalElements } = await import(path.join(root, 'web', 'js', 'physics', 'orbit.js'));
  const { ENGINES } = await import(path.join(root, 'web', 'js', 'data', 'engines.js'));
  const { PRESETS } = await import(path.join(root, 'web', 'js', 'data', 'presets.js'));

  const loaded = loadTolerances(fs.readFileSync(docPath, 'utf8'));
  const golden = JSON.parse(fs.readFileSync(goldenPath, 'utf8'));
  assert.strictEqual(golden.tolerance.doc, 'docs/PHYSICS.md');
  assert.strictEqual(golden.count, golden.cases.length);
  assert.strictEqual(loaded.spec.quantities.maxQTime.abs, CATALOG_FLIGHT_DT);
  assertCatalogCoverage(golden.cases, ENGINES, PRESETS);

  const cache = new Map();
  const flightOf = (c) => {
    const key = JSON.stringify([c.in.stages, c.in.opts, c.in.nozzleDiameter ?? null]);
    if (!cache.has(key)) cache.set(key, evaluateFlight(c.in.stages, c.in.opts, c.in.nozzleDiameter ?? null));
    return cache.get(key);
  };
  const pick = (flight, kind) => (kind === 'ascent' ? flight.ascent : kind === 'staging' ? flight.staging : kind === 'drag' ? flight.drag : flight.orbit);

  const problems = [];
  for (const c of golden.cases) {
    assert.ok(KINDS.includes(c.kind), c.kind);
    assert.strictEqual(c.in.opts.dt, CATALOG_FLIGHT_DT);
    const label = `${c.subject.type}:${c.subject.id}:${c.kind}`;
    const flight = flightOf(c);
    problems.push(...compareTree(pick(flight, c.kind), c.out, loaded, label));
    if (c.kind === 'staging') {
      const types = c.out.events.map((e) => e.type);
      assert.ok(types.includes('liftoff'), label);
      assert.ok(types.some((t) => t === 'burnout' || t === 'separation' || t === 'cutoff'), label);
    }
    if (c.kind === 'drag' && c.fixture?.vacuum) {
      assert.strictEqual(c.out.maxQ, 0, label);
      assert.ok(c.out.nozzleMachPeak.dragForce > 0, label);
    }
    if (c.kind === 'orbit') {
      problems.push(...compareTree(
        { x: flight.state.x, y: flight.state.y, vx: flight.state.vx, vy: flight.state.vy },
        { x: c.in.r[0], y: c.in.r[1], vx: c.in.v[0], vy: c.in.v[1] },
        loaded,
        `${label}.state`,
      ));
      const fromState = fin(orbitalElements(c.in.r, c.in.v));
      problems.push(...compareTree(fromState, c.out, loaded, `${label}.closed`, loaded.spec.overlays.orbitClosedForm));
    }
  }
  assert.deepStrictEqual(problems, []);

  const sample = golden.cases.find((c) => c.kind === 'orbit' && typeof c.out.a === 'number');
  const bumped = { ...sample.out, a: sample.out.a + 0.1 };
  assert.ok(compareTree(bumped, sample.out, loaded, '$.closed', loaded.spec.overlays.orbitClosedForm).length > 0);
  assert.deepStrictEqual(compareTree(bumped, sample.out, loaded, '$.integrated'), []);
});
