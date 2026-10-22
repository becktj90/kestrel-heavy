// Narrator states only true vehicle facts: manifest tag lint + coverage floor (PR A of the narrator-facts work).
// Schema: docs/narration-facts.md. Runtime picker (PR B) must use the same rules as evalRequires() below.
const assert = require('node:assert/strict');
const { test } = require('node:test');
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');

const root = path.join(__dirname, '..');
const A = path.join(root, 'assets/narration/manifest.json');
const U = path.join(root, 'unity/Assets/Kestrel.Game/Audio/Runtime/Resources/Narration/manifest.json');
const man = JSON.parse(fs.readFileSync(A, 'utf8'));
const clips = man.clips;

const PROPS = ['kerolox', 'hydrolox', 'methalox', 'solid', 'storable'];
const RECOVERY = ['none', 'barge', 'rtls'];
const KEY_RE = /^(stages|stage[1-9]\.engines|stage[1-9]\.propellant|strapons|strapons\.engines|strapons\.propellant|propellant\.all|recovery|legs|fairing|fairing\.halves|fins|finless|lit\.stage|lit\.engines|lit\.propellant|lit\.last)$/;
const NUMERIC = (k) => /^(stages|stage\d\.engines|strapons|strapons\.engines|fairing\.halves|lit\.stage|lit\.engines)$/.test(k);
const BOOL = (k) => /^(legs|fairing|fins|finless|lit\.last)$/.test(k);
const ENUM = (k) => (/propellant/.test(k) ? (k === 'propellant.all' ? [...PROPS, 'mixed'] : PROPS) : k === 'recovery' ? RECOVERY : null);
const CMP_RE = /^(>=|<=|!=|>|<)-?\d+$/;
const FACT_WORDS = /\b(one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|\d+|engines?|cores?|cluster|chambers?|methane|LOX|kerosene|hydrogen|methalox|kerolox|hydrolox|solids?|strap-?ons?|boosters?|upper stage|(first|second|third)[- ]stage|legs|fins?|finless|halves|fairing|Haven|barge|landing|pad landing)\b/i;
const ALLOW = /Pier 7|SES-1|Max-Q|T-minus \d+/gi;
const BANNED = /blue origin|new glenn|jacklyn|lc-36/i;

function valueOk(k, v) {
  if (Array.isArray(v)) return v.length > 0 && v.every((x) => valueOk(k, x) && !Array.isArray(x));
  if (NUMERIC(k)) return Number.isInteger(v) || (typeof v === 'string' && CMP_RE.test(v));
  if (BOOL(k)) return typeof v === 'boolean';
  const e = ENUM(k);
  return !!e && e.includes(v);
}

// Same semantics the picker must use: AND of keys; missing fact => false; unknown key => false.
function holds(have, want) {
  if (have === undefined || have === null) return false;
  if (Array.isArray(want)) return want.some((w) => holds(have, w));
  if (typeof want === 'string' && CMP_RE.test(want)) {
    const [, op, n] = want.match(/^(>=|<=|!=|>|<)(-?\d+)$/);
    const x = Number(n);
    if (typeof have !== 'number') return false;
    return op === '>=' ? have >= x : op === '<=' ? have <= x : op === '!=' ? have !== x : op === '>' ? have > x : have < x;
  }
  return have === want;
}
function eligible(c, facts) {
  if (c.neutral === true) return true;
  if (!c.requires) return false;
  return Object.entries(c.requires).every(([k, v]) => KEY_RE.test(k) && holds(facts[k], v));
}

test('both narration manifest copies are byte-equal', () => {
  assert.ok(fs.readFileSync(A).equals(fs.readFileSync(U)), 'assets/ and unity/ narration manifests differ');
});

test('every clip carries exactly one valid fact tag form', () => {
  const errs = [];
  for (const c of clips) {
    const isN = c.neutral === true;
    const isG = c.requires !== undefined || c.claims !== undefined;
    if (isN === isG) { errs.push(`${c.id}: needs exactly one of neutral or requires+claims`); continue; }
    if (isN) {
      if (typeof c.neutralReason !== 'string' || !c.neutralReason.trim()) errs.push(`${c.id}: neutral needs neutralReason`);
      continue;
    }
    if (c.neutralReason !== undefined) errs.push(`${c.id}: neutralReason on a gated clip`);
    const r = c.requires;
    if (!r || typeof r !== 'object' || Array.isArray(r) || !Object.keys(r).length) { errs.push(`${c.id}: requires must be a non-empty object`); continue; }
    for (const [k, v] of Object.entries(r)) {
      if (!KEY_RE.test(k)) errs.push(`${c.id}: unknown key ${k}`);
      else if (!valueOk(k, v)) errs.push(`${c.id}: bad value for ${k}: ${JSON.stringify(v)}`);
    }
    if (!Array.isArray(c.claims) || !c.claims.length) errs.push(`${c.id}: claims must be a non-empty array`);
    else {
      if (new Set(c.claims).size !== c.claims.length) errs.push(`${c.id}: duplicate claims`);
      for (const k of c.claims) if (!(k in r)) errs.push(`${c.id}: claim ${k} is not in requires`);
    }
  }
  assert.deepEqual(errs, []);
});

test('fact-word lines are gated or carry a neutral reason; no banned names', () => {
  for (const c of clips) {
    for (const t of [c.text, c.spokenText]) {
      assert.ok(!BANNED.test(t), `${c.id}: banned name`);
      if (FACT_WORDS.test(t.replace(ALLOW, ''))) {
        assert.ok((c.claims && c.claims.length) || (c.neutral && c.neutralReason), `${c.id}: fact words need claims or neutralReason`);
      }
    }
  }
});

test('every clip file exists in both trees with the recorded sha256, and loudness is in spec', () => {
  const ids = new Set();
  for (const c of clips) {
    assert.ok(!ids.has(c.id), `duplicate id ${c.id}`); ids.add(c.id);
    for (const base of ['assets/narration', 'unity/Assets/Kestrel.Game/Audio/Runtime/Resources/Narration']) {
      const f = path.join(root, base, c.file);
      assert.ok(fs.existsSync(f), `missing ${base}/${c.file}`);
      const h = crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
      assert.equal(h, c.sha256, `${base}/${c.file} sha256`);
    }
    assert.ok(fs.existsSync(path.join(root, 'unity/Assets/Kestrel.Game/Audio/Runtime/Resources/Narration', c.file + '.meta')), `${c.file}.meta`);
    assert.ok(c.lufs >= -17 && c.lufs <= -15, `${c.id}: ${c.lufs} LUFS`);
    assert.ok(c.truePeakDb <= -1.5, `${c.id}: ${c.truePeakDb} dBTP`);
    assert.ok(c.durationSec > 0 && c.durationSec <= 6, `${c.id}: ${c.durationSec}s`);
  }
  for (const base of ['assets/narration', 'unity/Assets/Kestrel.Game/Audio/Runtime/Resources/Narration']) {
    const files = ['mission-control', 'host', 'cohost'].flatMap((d) =>
      fs.readdirSync(path.join(root, base, d)).filter((f) => f.endsWith('.mp3')).map((f) => `${d}/${f}`));
    assert.deepEqual(files.sort(), clips.map((c) => c.file).sort(), `${base}: mp3 files must match the manifest`);
  }
});

// ---- coverage: every common event keeps a true line on every shipped design ----
const PROP_ID = { kerolox: 'kerolox', hydrolox: 'hydrolox', methalox: 'methalox', 'solid-apcp': 'solid', 'mmh-nto': 'storable' };
function designs() {
  const out = [];
  const walk = (o, set) => {
    if (Array.isArray(o)) return o.forEach((v) => walk(v, set));
    if (o && typeof o === 'object') {
      if (Array.isArray(o.stages) && o.stages[0] && typeof o.stages[0] === 'object' && 'engineCount' in o.stages[0]) out.push({ set, d: o });
      Object.values(o).forEach((v) => walk(v, set));
    }
  };
  for (const f of ['board-starters', 'golden/campaign-viable', 'golden/contracts-viable', 'campaign-winners', 'designer-templates', 'presets'])
    walk(JSON.parse(fs.readFileSync(path.join(root, 'data', f + '.json'), 'utf8')), f);
  const hop = { id: 'KH-1/2 armed hopper', stages: [{ engineCount: 1, propellant: 'kerolox', landingPropMass: 0 }], strapOns: { count: 0 }, aero: { fins: true }, fairing: {} };
  out.push({ set: 'hopper', d: hop });
  return out;
}
function factsOf(d) {
  const f = {};
  const st = d.stages;
  f.stages = st.length;
  st.forEach((s, i) => {
    f[`stage${i + 1}.engines`] = s.engineCount ?? 1;
    const p = PROP_ID[s.propellant]; if (p) f[`stage${i + 1}.propellant`] = p;
  });
  const so = d.strapOns || {};
  f.strapons = so.count || 0;
  f['strapons.engines'] = f.strapons ? so.engineCount || 1 : 0;
  if (f.strapons && PROP_ID[so.propellant]) f['strapons.propellant'] = PROP_ID[so.propellant];
  const all = st.map((s) => PROP_ID[s.propellant]).concat(f.strapons ? [PROP_ID[so.propellant] || PROP_ID[st[0].propellant]] : []);
  if (all.every(Boolean)) f['propellant.all'] = all.every((p) => p === all[0]) ? all[0] : 'mixed';
  f.legs = !!(d.landing && d.landing.legs);
  f.recovery = f.legs && (st[0].landingPropMass || 0) > 0 ? 'barge' : 'none';
  f.fairing = !!(d.fairing && (d.fairing.jettisonAltitudeKm || 0) > 0);
  f.fins = !!(d.aero && d.aero.fins);
  f.finless = !f.fins;
  return f;
}
function contexts(f) {
  const lit = (k) => ({ ...f, 'lit.stage': k, 'lit.engines': f[`stage${k}.engines`], 'lit.propellant': f[`stage${k}.propellant`], 'lit.last': k === f.stages });
  const ev = [['tankpress', f], ['ignition', lit(1)], ['liftoff', f], ['maxq', f], ['meco', lit(1)], ['fuel-low', lit(1)], ['abort-fuel', lit(1)], ['deploy', f]];
  for (let k = 2; k <= f.stages; k++) ev.push(['ses1', lit(k)], ['fuel-low', lit(k)], ['abort-fuel', lit(k)]);
  if (f.stages >= 2) ev.push(['sep-clear', f], ['seco', lit(f.stages)]);
  if (f.fairing) ev.push(['fairing', f]);
  if (f.recovery !== 'none') for (const e of ['entry', 'landing', 'touchdown', 'recovered']) ev.push([e, f]);
  return ev;
}
// mission-control floor per event: what a player must always hear (2 = rotation on retries).
const FLOOR = { ignition: 2, liftoff: 2, maxq: 2, meco: 2, ses1: 2, tankpress: 1, 'fuel-low': 1, 'abort-fuel': 2, deploy: 1, 'sep-clear': 1, seco: 1, entry: 1, landing: 1, touchdown: 1, recovered: 1 };

test('coverage: every shipped design keeps true mission-control takes on every event it flies', () => {
  const gaps = [];
  for (const { set, d } of designs()) {
    const f = factsOf(d);
    for (const [ev, ctx] of contexts(f)) {
      const mc = clips.filter((c) => c.event === ev && (c.channel === 'mc' || ev === 'fairing') && eligible(c, ctx));
      const need = FLOOR[ev] ?? 1;
      if (mc.length < need) gaps.push(`${set}/${d.id || d.name} ${ev}${ctx['lit.stage'] ? '@' + ctx['lit.stage'] : ''}: ${mc.length}/${need}`);
    }
  }
  assert.deepEqual(gaps, []);
});

test('regressions: no false engine-count lines on the board starters and the hopper', () => {
  for (const { set, d } of designs().filter((x) => x.set === 'board-starters' || x.set === 'hopper')) {
    const f = factsOf(d);
    for (const [ev, ctx] of contexts(f)) {
      for (const c of clips.filter((c) => c.event === ev && eligible(c, ctx))) {
        if (f['stage1.engines'] !== 7) assert.ok(!/\bseven\b/i.test(c.text), `${d.id}: ${c.id} says seven`);
        if (f.stages === 1) assert.ok(!['mission-control/meco.1', 'mission-control/meco.2'].includes(c.id), `${d.id}: ${c.id}`);
        if (f.recovery === 'none') assert.ok(!/\bHaven\b|heading home|coming home/i.test(c.text), `${d.id}: ${c.id} mentions recovery`);
      }
    }
  }
  const hop = designs().find((x) => x.set === 'hopper').d;
  const hf = factsOf(hop);
  const ign = clips.filter((c) => c.event === 'ignition' && eligible(c, { ...hf, 'lit.stage': 1, 'lit.engines': 1, 'lit.last': true }));
  assert.ok(!ign.some((c) => /\bcluster|engines\b/i.test(c.text)), 'single-engine hopper hears a cluster line');
});

module.exports = { eligible, factsOf };
