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
const SITES = JSON.parse(fs.readFileSync(path.join(root, 'data/sites.json'), 'utf8')).sites.map((x) => x.id);
const KEY_RE = /^(stages|stage[1-9]\.engines|stage[1-9]\.propellant|strapons|strapons\.engines|strapons\.propellant|propellant\.all|recovery|legs|fairing|fairing\.halves|fins|finless|site|lit\.stage|lit\.engines|lit\.propellant|lit\.last)$/;
const NUMERIC = (k) => /^(stages|stage\d\.engines|strapons|strapons\.engines|fairing\.halves|lit\.stage|lit\.engines)$/.test(k);
const BOOL = (k) => /^(legs|fairing|fins|finless|lit\.last)$/.test(k);
const ENUM = (k) => (/propellant/.test(k) ? (k === 'propellant.all' ? [...PROPS, 'mixed'] : PROPS) : k === 'recovery' ? RECOVERY : k === 'site' ? SITES : null);
const CMP_RE = /^(>=|<=|!=|>|<)-?\d+$/;
const FACT_WORDS = /\b(one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|\d+|engines?|cores?|cluster|chambers?|methane|LOX|kerosene|hydrogen|methalox|kerolox|hydrolox|solids?|strap-?ons?|boosters?|upper stage|(first|second|third)[- ]stage|legs|fins?|finless|halves|fairing|Haven|barge|landing|pad landing|Pier 7|Coastal Pad B|Salt Flat Pad|North Reach|strakes?)\b/i;
const ALLOW = /SES-1|Max-Q|T-minus \d+/gi;
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

// ---- designs: every shipped starter, template, winner, preset and golden-viable design ----
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
// The landing the design picked: design.recovery.mode (barge | rtls). No pick = expendable = 'none'.
// A booster that is not given a landing is never called "coming home" or "on Haven" (RecoverySim.Offered).
function recoveryOf(d) {
  const m = d.recovery && d.recovery.mode;
  return m === 'barge' || m === 'rtls' ? m : 'none';
}
function factsOf(d, site) {
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
  f.recovery = recoveryOf(d);
  f.fairing = !!(d.fairing && (d.fairing.jettisonAltitudeKm || 0) > 0);
  f.fins = !!(d.aero && d.aero.fins);
  f.finless = !f.fins;
  if (site) f.site = site;
  return f;
}
function contexts(f) {
  const lit = (k) => ({ ...f, 'lit.stage': k, 'lit.engines': f[`stage${k}.engines`], 'lit.propellant': f[`stage${k}.propellant`], 'lit.last': k === f.stages });
  const ev = [['pad', f], ['tankpress', lit(1)], ['ignition', lit(1)], ['liftoff', f], ['maxq', f], ['meco', lit(1)], ['fuel-low', lit(1)], ['abort-fuel', lit(1)], ['abort-corridor', f], ['deploy', f]];
  for (let k = 2; k <= f.stages; k++) ev.push(['ses1', lit(k)], ['fuel-low', lit(k)], ['abort-fuel', lit(k)], ['tankpress', lit(k)], ['seco', lit(k)]);
  if (f.stages >= 2) ev.push(['sep-clear', f]);
  if (f.fairing) ev.push(['fairing', f]);
  if (f.recovery !== 'none' && f.stages >= 2) {
    for (const e of ['entry', 'landing', 'touchdown', 'recovered', 'salvage', 'splash', 'abort-salvage', 'abort-splash', 'haven-maxq']) ev.push([e, f]);
  }
  return ev;
}
// mission-control floor per event: what a player must always hear (2 = rotation on retries).
const FLOOR = { pad: 2, ignition: 2, liftoff: 2, maxq: 2, meco: 2, ses1: 2, tankpress: 1, 'fuel-low': 1, 'abort-fuel': 2, deploy: 1, 'sep-clear': 1, seco: 1, entry: 1, landing: 1, touchdown: 2, recovered: 2, salvage: 2, splash: 2, 'abort-salvage': 1, 'abort-splash': 1, 'haven-maxq': 1, 'abort-corridor': 2 };
const eachCase = (fn) => {
  for (const { set, d } of designs()) for (const site of SITES) {
    const f = factsOf(d, site);
    for (const [ev, ctx] of contexts(f)) fn({ name: `${set}/${d.id || d.name}@${site}`, d, f, ev, ctx });
  }
};

test('coverage: every shipped design on every pad keeps true mission-control takes on every event it flies', () => {
  const gaps = new Set();
  eachCase(({ name, ev, ctx }) => {
    const mc = clips.filter((c) => c.event === ev && (c.channel === 'mc' || ev === 'fairing') && eligible(c, ctx));
    const need = FLOOR[ev] ?? 1;
    if (mc.length < need) gaps.add(`${name} ${ev}${ctx['lit.stage'] ? '@' + ctx['lit.stage'] : ''}: ${mc.length}/${need}`);
  });
  assert.deepEqual([...gaps], []);
});

// ---- independent claim-vs-design verifier ----
// Reads the spoken text of every take that is ELIGIBLE for a design and checks each fact it states against the raw
// design JSON (not against the tags). A line that states a fact must be gated by a tag that carries that fact.
const NUM = { one: 1, two: 2, three: 3, four: 4, five: 5, six: 6, seven: 7, eight: 8, nine: 9, ten: 10, eleven: 11, twelve: 12, fifteen: 15, sixteen: 16 };
const REC_EVENTS = new Set(['entry', 'landing', 'touchdown', 'recovered', 'salvage', 'tip', 'splash', 'abort-tip', 'abort-salvage', 'abort-splash', 'abort-fuelHaven', 'haven-maxq']);
// Facts a line states: [rule, key-family that must be claimed, check(design, ctx)].
function statedFacts(c) {
  const t = c.text; const out = [];
  const eng = (d, ctx) => d.stages[(ctx['lit.stage'] || 1) - 1].engineCount ?? 1;
  const totalEng = (d) => (d.stages[0].engineCount ?? 1) + ((d.strapOns && d.strapOns.count) || 0) * ((d.strapOns && d.strapOns.engineCount) || 1);
  const re = new RegExp(`\\b(${Object.keys(NUM).join('|')})\\b(?= core engines)`, 'i');
  const m1 = t.match(re);
  if (m1) out.push([`${m1[1]} core engines`, /engines$/, (d, ctx) => eng(d, ctx) === NUM[m1[1].toLowerCase()]]);
  const m2 = t.match(new RegExp(`\\ball (${Object.keys(NUM).join('|')})\\b`, 'i'));
  if (m2) out.push([`all ${m2[1]}`, /engines$|strapons$/, (d) => totalEng(d) === NUM[m2[1].toLowerCase()]]);
  if (/single engine|engine flamed out/i.test(t)) out.push(['one engine', /engines$/, (d, ctx) => eng(d, ctx) === 1]);
  if (/cluster|\bcores\b|chambers|\bengines\b/i.test(t) && !/core engines/i.test(t)) out.push(['several engines', /engines$/, (d, ctx) => eng(d, ctx) >= 2]);
  if (/strap-?on/i.test(t)) out.push(['strap-ons', /^strapons/, (d) => !!(d.strapOns && d.strapOns.count > 0)]);
  if (/\bsecond stage\b/i.test(t)) out.push(['second stage', /^(stages|lit\.stage)$/, (d, ctx) => d.stages.length >= 2 && (!ctx['lit.stage'] || ctx['lit.stage'] === 2)]);
  if (/\bthird stage\b/i.test(t)) out.push(['third stage', /^(stages|lit\.stage)$/, (d, ctx) => d.stages.length >= 3 && (!ctx['lit.stage'] || ctx['lit.stage'] === 3)]);
  if (/another stage|carries the payload|final push|insertion (is )?complete/i.test(t)) out.push(['stage position', /^(stages|lit\.last|lit\.stage)$/, (d, ctx) => /another stage/i.test(t) ? ctx['lit.stage'] < d.stages.length : ctx['lit.stage'] === d.stages.length]);
  if (/\bupper\b/i.test(t)) out.push(['upper stage', /^(stages|lit\.stage|lit\.last)$/, (d) => d.stages.length >= 2]);
  if (/\bbooster\b/i.test(t)) out.push(['booster', /^(stages|strapons|recovery|lit\.stage)/, (d) => d.stages.length >= 2 || !!(d.strapOns && d.strapOns.count > 0)]);
  if (/Haven|barge|\bdeck\b|sea state|in the drink/i.test(t)) out.push(['Haven', /^recovery$/, (d) => recoveryOf(d) === 'barge']);
  if (/hit the ground/i.test(t)) out.push(['ground', /^recovery$/, (d) => recoveryOf(d) === 'rtls']);
  if (REC_EVENTS.has(c.event) && /\bpad\b/i.test(t)) out.push(['landing on the pad', /^recovery$/, (d) => recoveryOf(d) === 'rtls']);
  if (/coming home|heading home|will fly again/i.test(t)) out.push(['booster comes home', /^recovery$/, (d) => recoveryOf(d) !== 'none']);
  for (const [name, id] of [['Pier 7', 'pier-7'], ['Coastal Pad B', 'coastal-pad-b'], ['Salt Flat Pad', 'salt-flat'], ['North Reach', 'north-reach']])
    if (t.includes(name)) out.push([name, /^site$/, (d, ctx) => ctx.site === id]);
  for (const [w, id] of [[/kerosene/i, 'kerolox'], [/methane/i, 'methalox'], [/hydrogen/i, 'hydrolox']])
    if (w.test(t)) out.push([`${id} propellant`, /propellant/, (d, ctx) => {
      const idx = ctx['lit.stage'] ? [ctx['lit.stage'] - 1] : d.stages.map((_, i) => i);
      return idx.every((i) => PROP_ID[d.stages[i].propellant] === id);
    }]);
  if (/\bhalves\b/i.test(t)) out.push(['fairing halves', /^fairing\.halves$/, () => false]);
  if (/\blegs\b/i.test(t)) out.push(['legs', /^legs$/, (d) => !!(d.landing && d.landing.legs)]);
  return out;
}

test('verifier: every eligible take states only facts that are true for the flown design (all starters, templates, winners, pads)', () => {
  const bad = new Set();
  eachCase(({ name, d, ev, ctx }) => {
    for (const c of clips.filter((x) => x.event === ev && eligible(x, ctx))) {
      for (const [what, , check] of statedFacts(c)) if (!check(d, ctx)) bad.add(`${name} ${c.id} says "${what}" (${c.text})`);
    }
  });
  assert.deepEqual([...bad], []);
});

// Event-implied facts: the event only fires when it is true, so a neutral take may state them.
const IMPLIED = { ses1: ['upper stage', 'second stage'], seco: ['upper stage'], 'sep-clear': ['booster', 'upper stage'], 'sep-wide': ['booster', 'upper stage'], 'sep-contact': ['booster', 'upper stage'], meco: ['booster'] };

test('verifier: a line that states a design fact carries a tag that claims it; neutral lines state none', () => {
  const errs = [];
  for (const c of clips) {
    for (const [what, claimRe] of statedFacts(c)) {
      if (c.neutral) {
        if (!(IMPLIED[c.event] || []).includes(what)) errs.push(`${c.id}: neutral but states "${what}"`);
      } else if (!(c.claims || []).some((k) => claimRe.test(k)) && !(IMPLIED[c.event] || []).includes(what)) errs.push(`${c.id}: states "${what}" but claims ${JSON.stringify(c.claims)}`);
    }
  }
  assert.deepEqual(errs, []);
});

test('verifier: no take is eligible without its tag (untagged, unknown key, or missing fact stays silent)', () => {
  const f = factsOf(designs()[0].d, 'pier-7');
  assert.equal(eligible({ id: 'x', text: 'x' }, f), false);
  assert.equal(eligible({ requires: { 'bogus.key': 1 } }, f), false);
  assert.equal(eligible({ requires: { 'fairing.halves': 2 } }, { ...f }), false);
  assert.equal(eligible({ requires: { site: 'pier-7' } }, {}), false);
  assert.equal(clips.filter((c) => c.requires && 'fairing.halves' in c.requires).every((c) => !designs().some(({ d }) => eligible(c, factsOf(d, 'pier-7')))), true);
});

test('regressions: seven, single-stage, RTLS-vs-Haven, pad and cluster', () => {
  eachCase(({ name, f, ev, ctx }) => {
    for (const c of clips.filter((x) => x.event === ev && eligible(x, ctx))) {
      if (f['stage1.engines'] !== 7) assert.ok(!/\bseven\b/i.test(c.text), `${name}: ${c.id} says seven`);
      if (f.stages === 1) assert.ok(!['mission-control/meco.1', 'mission-control/meco.2'].includes(c.id), `${name}: ${c.id}`);
      if (f.recovery !== 'barge') assert.ok(!/\bHaven\b|barge|sea state|drink/i.test(c.text), `${name}: ${c.id} Haven line without a barge landing`);
      if (f.recovery === 'none') assert.ok(!/heading home|coming home/i.test(c.text), `${name}: ${c.id} mentions recovery`);
      if (f.site !== 'pier-7') assert.ok(!/Pier 7/.test(c.text), `${name}: ${c.id} names Pier 7`);
    }
  });
  const hf = factsOf(designs().find((x) => x.set === 'hopper').d, 'pier-7');
  const ign = clips.filter((c) => c.event === 'ignition' && eligible(c, { ...hf, 'lit.stage': 1, 'lit.engines': 1, 'lit.last': true }));
  assert.ok(!ign.some((c) => /\bcluster|engines\b/i.test(c.text)), 'single-engine hopper hears a cluster line');
});

module.exports = { eligible, factsOf, designs, recoveryOf };
