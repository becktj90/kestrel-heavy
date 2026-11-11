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
const MISSIONS = Array.from({ length: 20 }, (_, i) => `m${i + 1}`);
const KEY_RE = /^(stages|stage[1-9]\.engines|stage[1-9]\.propellant|strapons|strapons\.engines|strapons\.propellant|propellant\.all|recovery|legs|fairing|fairing\.halves|fins|finless|site|throttle\.maxq|mission|lit\.stage|lit\.engines|lit\.propellant|lit\.last)$/;
const NUMERIC = (k) => /^(stages|stage\d\.engines|strapons|strapons\.engines|fairing\.halves|lit\.stage|lit\.engines)$/.test(k);
const BOOL = (k) => /^(legs|fairing|fins|finless|throttle\.maxq|lit\.last)$/.test(k);
const ENUM = (k) => (/propellant/.test(k) ? (k === 'propellant.all' ? [...PROPS, 'mixed'] : PROPS) : k === 'recovery' ? RECOVERY : k === 'site' ? SITES : k === 'mission' ? MISSIONS : null);
const CMP_RE = /^(>=|<=|!=|>|<)-?\d+$/;
const FACT_WORDS = /\b(one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|\d+|engines?|cores?|cluster|chambers?|methane|LOX|kerosene|hydrogen|methalox|kerolox|hydrolox|solids?|strap-?ons?|boosters?|upper stage|(first|second|third)[- ]stage|legs|fins?|finless|halves|fairing|Haven|barge|landing|pad landing|Pier 7|Coastal Pad B|Salt Flat Pad|North Reach|strakes?)\b/i;
const ALLOW = /SES-1|Max-Q|T-minus \d+/gi;
// DECISIONS #77, widened by #100: real names, history, dates and commonly known companies are allowed on
// every narration surface. A take is "historical" (mission-gated) when it is gated to one campaign mission
// AND carries sources. The banned-names rule (DECISIONS #100) applies to EVERY take, historical ones included.
const BANNED = /blue origin|new glenn|jacklyn|lc-36/i;
// Shipped takes that fail the banned-names rule and are waiting for a re-record. This list may only
// shrink: a test below fails if an id here no longer exists or no longer names a banned name.
// (Empty since host/color.pad.m7.1 and cohost/color.pad.m7.2 were re-recorded, 2026-11-11.)
const BANNED_PENDING_RERECORD = new Set([]);
const isHistorical = (c) => c.requires && typeof c.requires.mission === 'string'
  && Array.isArray(c.sources) && c.sources.length > 0;

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
      assert.ok(BANNED_PENDING_RERECORD.has(c.id) || !BANNED.test(t), `${c.id}: banned name (DECISIONS #100)`);
      if (FACT_WORDS.test(t.replace(ALLOW, ''))) {
        assert.ok((c.claims && c.claims.length) || (c.neutral && c.neutralReason), `${c.id}: fact words need claims or neutralReason`);
      }
    }
  }
});

test('historical takes (DECISIONS #77/#100): mission-gated and sourced; banned names fail on every take', () => {
  const errs = [];
  for (const c of clips) {
    if (BANNED.test(c.text) || BANNED.test(c.spokenText)) {
      if (!BANNED_PENDING_RERECORD.has(c.id)) errs.push(`${c.id}: banned name (DECISIONS #100 applies on every surface)`);
    }
    if (c.requires && 'mission' in c.requires) {
      if (!MISSIONS.includes(c.requires.mission)) errs.push(`${c.id}: bad mission ${c.requires.mission}`);
      if (!isHistorical(c)) errs.push(`${c.id}: mission-gated take needs sources`);
      if (!c.claims || !c.claims.includes('mission')) errs.push(`${c.id}: mission-gated take must claim mission`);
      if (!/^color\./.test(c.event)) errs.push(`${c.id}: mission-gated take must be a color take`);
    }
  }
  assert.deepEqual(errs, []);
});

test('banned-name re-record list only shrinks (DECISIONS #100)', () => {
  const byId = new Map(clips.map((c) => [c.id, c]));
  for (const id of BANNED_PENDING_RERECORD) {
    const c = byId.get(id);
    assert.ok(c, `${id}: re-recorded or removed; drop it from BANNED_PENDING_RERECORD`);
    assert.ok(BANNED.test(c.text) || BANNED.test(c.spokenText), `${id}: no longer names a banned name; drop it from BANNED_PENDING_RERECORD`);
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
    assert.ok(c.durationSec > 0 && c.durationSec <= (c.layer === 'color' ? 14 : 6), `${c.id}: ${c.durationSec}s`);
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
  // DECISIONS #80: true only for a design with its own max-Q throttle setting (NarrationFacts.From, #439).
  f['throttle.maxq'] = !!(d.guidance && typeof d.guidance.maxQThrottle === 'number');
  if (site) f.site = site;
  return f;
}
function contexts(f) {
  const lit = (k) => ({ ...f, 'lit.stage': k, 'lit.engines': f[`stage${k}.engines`], 'lit.propellant': f[`stage${k}.propellant`], 'lit.last': k === f.stages });
  const ev = [['pad', f], ['tankpress', lit(1)], ['ignition', lit(1)], ['liftoff', f], ['maxq', f], ['meco', lit(1)], ['fuel-low', lit(1)], ['abort-fuel', lit(1)], ['abort-corridor', f], ['deploy', f]];
  for (let k = 2; k <= f.stages; k++) ev.push(['ses1', lit(k)], ['fuel-low', lit(k)], ['abort-fuel', lit(k)], ['tankpress', lit(k)], ['seco', lit(k)]);
  if (f.stages >= 2) ev.push(['sep-clear', f]);
  // maxq-throttle joins this list (floor 1) when its recorded takes ship; today it has none and stays silent (HUD banner only).
  if (f['throttle.maxq']) ev.push(['maxq-through', f]);
  if (f.fairing) ev.push(['fairing', f]);
  if (f.recovery !== 'none' && f.stages >= 2) {
    for (const e of ['entry', 'landing', 'touchdown', 'recovered', 'salvage', 'splash', 'abort-salvage', 'abort-splash', 'haven-maxq']) ev.push([e, f]);
  }
  ev.push(['color.pad', f], ['color.ascent', f], ['color.karman', f], ['color.coast', f]);
  if (f.recovery !== 'none' && f.stages >= 2) ev.push(['color.recovery', f]);
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
    if (ev.startsWith('color.')) return; // color slots are covered by their own test below
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
const REC_EVENTS = new Set(['color.recovery', 'entry', 'landing', 'touchdown', 'recovered', 'salvage', 'tip', 'splash', 'abort-tip', 'abort-salvage', 'abort-splash', 'abort-fuelHaven', 'haven-maxq']);
// Facts a line states: [rule, key-family that must be claimed, check(design, ctx)].
function statedFacts(c) {
  // A propellant named only as a comparison ("warmer than hydrogen") is a general fact, not a claim about the flown stage.
  const t = c.text.replace(/\bthan (the )?(hydrogen|kerosene|methane|oxygen)\b/gi, 'than another fuel'); const out = [];
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
  // A historical take's "booster" is history (for example the first upright landing in 2015), not a claim
  // about the flown design, so like the oxygen rule below it does not apply to mission-gated historical takes.
  if (/\bbooster\b/i.test(t) && !isHistorical(c)) out.push(['booster', /^(stages|strapons|recovery|lit\.stage)/, (d) => d.stages.length >= 2 || !!(d.strapOns && d.strapOns.count > 0)]);
  if (/Haven|barge|\bdeck\b|sea state|in the drink/i.test(t)) out.push(['Haven', /^recovery$/, (d) => recoveryOf(d) === 'barge']);
  if (/hit the ground/i.test(t)) out.push(['ground', /^recovery$/, (d) => recoveryOf(d) === 'rtls']);
  if (REC_EVENTS.has(c.event) && /\bpad\b/i.test(t)) out.push(['landing on the pad', /^recovery$/, (d) => recoveryOf(d) === 'rtls']);
  if (/coming home|heading home|will fly again/i.test(t)) out.push(['booster comes home', /^recovery$/, (d) => recoveryOf(d) !== 'none']);
  for (const [name, id] of [['Pier 7', 'pier-7'], ['Coastal Pad B', 'coastal-pad-b'], ['Salt Flat Pad', 'salt-flat'], ['North Reach', 'north-reach']])
    if (t.includes(name)) out.push([name, /^site$/, (d, ctx) => ctx.site === id]);
  for (const [w, id] of [[/kerosene/i, 'kerolox'], [/methane/i, 'methalox'], [/hydrogen/i, 'hydrolox']])
    if (w.test(t)) out.push([`${id} propellant`, /propellant/, (d, ctx) => {
      const named = Object.keys(c.requires || {}).map((k) => k.match(/^stage(\d)\.propellant$/)).filter(Boolean).map((x) => Number(x[1]) - 1);
      const idx = named.length ? named : ctx['lit.stage'] ? [ctx['lit.stage'] - 1] : d.stages.map((_, i) => i);
      return idx.every((i) => PROP_ID[d.stages[i] && d.stages[i].propellant] === id);
    }]);
  // A historical take's "liquid oxygen" is history (Goddard's 1926 rocket), not a claim about the flown design,
  // so the oxygen rule does not apply to mission-gated historical takes (DECISIONS #77).
  if (/\boxygen\b/i.test(t) && !isHistorical(c)) out.push(['liquid oxygen', /propellant/, (d, ctx) => {
    const named = Object.keys(c.requires || {}).map((k) => k.match(/^stage(\d)\.propellant$/)).filter(Boolean).map((x) => Number(x[1]) - 1);
    const idx = named.length ? named : [0];
    return idx.every((i) => ['kerolox', 'hydrolox', 'methalox'].includes(PROP_ID[d.stages[i] && d.stages[i].propellant]));
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
      if (f['stage1.engines'] !== 7) assert.ok(!/\bseven\b(?! point eight)/i.test(c.text), `${name}: ${c.id} says seven`);
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

// ---- webcast color lines (docs/narration-webcast.md) ----
const COLOR = clips.filter((c) => c.layer === 'color' || c.event.startsWith('color.'));
const SRC = fs.readFileSync(path.join(root, 'docs/narration-sources.md'), 'utf8');

test('color: layer, schema, captions, word limits and sources', () => {
  const errs = [];
  for (const c of COLOR) {
    // DECISIONS #77 historical takes are single-voice, mission-gated and sourced; their shape is covered
    // by the historical test above, not by the two-voice webcast schema below.
    if (isHistorical(c)) continue;
    if (c.layer !== 'color' || !c.event.startsWith('color.') || c.interruptible !== true) errs.push(`${c.id}: layer/event/interruptible`);
    if (c.channel !== 'host') errs.push(`${c.id}: channel must stay host (subtitle code accepts mc/host/cohost)`);
    if (!Array.isArray(c.lines) || c.lines.length < 2) { errs.push(`${c.id}: lines`); continue; }
    if (c.lines.map((l) => l.text).join(' ') !== c.text) errs.push(`${c.id}: text != lines`);
    if (new Set(c.lines.map((l) => l.speaker)).size < 2) errs.push(`${c.id}: needs two speakers`);
    for (const l of c.lines) if (l.text.trim().split(/\s+/).length > 14) errs.push(`${c.id}: line over 14 words: ${l.text}`);
    if (!c.lines.every((l) => l.overlapSec >= 0 && l.overlapSec <= 0.3)) errs.push(`${c.id}: overlap out of range`);
    if (c.neutral && !(c.sources || []).length) errs.push(`${c.id}: history line has no sources`);
    for (const sid of c.sources || []) if (!new RegExp(`^\\| ${sid} \\|`, 'm').test(SRC)) errs.push(`${c.id}: source ${sid} missing from docs/narration-sources.md`);
  }
  assert.deepEqual(errs, []);
});

test('color: every design on every pad has an eligible neutral take for each color slot it can reach', () => {
  const gaps = new Set();
  eachCase(({ name, ev, ctx }) => {
    if (!ev.startsWith('color.') || ev === 'color.recovery') return;
    if (!COLOR.some((c) => c.event === ev && c.neutral === true)) gaps.add(`${name} ${ev}`);
  });
  assert.deepEqual([...gaps], []);
  for (const ev of ['color.pad', 'color.ascent', 'color.karman', 'color.coast', 'color.recovery']) assert.ok(COLOR.filter((c) => c.event === ev).length >= 1, ev);
});

test('color: a design with a landing never hears a color.recovery take that contradicts its recovery mode', () => {
  const rtls = factsOf(designs().find((x) => x.d.stages.length >= 2).d, 'pier-7');
  for (const mode of ['none', 'barge', 'rtls']) {
    const f = { ...rtls, recovery: mode, site: 'salt-flat' };
    const got = COLOR.filter((c) => c.event === 'color.recovery' && eligible(c, f)).map((c) => c.id);
    if (mode === 'none') assert.deepEqual(got, [], 'expendable hears nothing');
    if (mode === 'barge') assert.deepEqual(got, ['host/color.recovery.3'], 'no Haven on a pad without a barge');
    if (mode === 'rtls') assert.ok(!got.includes('host/color.recovery.2'));
  }
});

test('color: Codex fact-check corrections stay applied (no unsupported numbers or dates)', () => {
  const txt = (id) => clips.find((c) => c.id === id).text;
  // wording that was removed after review must not come back
  const STALE = [
    /three hundred thousand/i, /forty-one seconds/i, /disproportionately/i, /far more propellant/i,
    /thirteenth-century china/i, /worked out in 1925/i, /first human orbit/i, /thirty to forty percent/i,
    /orbital booster flew a second time/i, /first satellite weighed/i, /staying in orbit takes/i,
  ];
  for (const c of clips.filter((x) => x.layer === 'color')) {
    for (const r of STALE) assert.ok(!r.test(c.text), c.id + ' still says ' + r);
  }
  assert.match(txt('host/color.pad.6'), /hundreds of thousands of gallons in about forty seconds/);
  assert.match(txt('host/color.recovery.3'), /In late 2015 an orbital-class booster first landed upright after flight\. And in 2017 a landed booster flew a second time/);
  assert.match(txt('host/color.coast.5'), /first human spaceflight lasted one hundred eight minutes, launch to landing/);
  assert.match(txt('host/color.pad.9'), /1903 paper showed how the rocket equation applies to spaceflight/);
  assert.match(txt('host/color.pad.9'), /many tons of propellant/);
  assert.match(txt('host/color.pad.8'), /by the thirteenth century/);
  assert.doesNotMatch(txt('host/color.pad.8'), /1232/);
  assert.match(txt('host/color.coast.7'), /quarter to a third better/);
  assert.match(txt('host/color.coast.2'), /published in 1925/);
  assert.match(txt('host/color.ascent.4'), /low orbit/);
  assert.match(txt('host/color.coast.4'), /first artificial satellite/);
  // the doc records the disputed date and the corrected sources
  assert.match(SRC, /1232[^|]*DISPUTED/);
  assert.match(SRC, /nas\.nasa\.gov/);
  assert.match(SRC, /spacenews\.com/);
  assert.match(SRC, /launch to landing/i);
});
