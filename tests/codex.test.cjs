// Codex lessons: eight topics, kid length, sim numbers, and the real-history guards.
// Real-history rule (Trevor, 2026-11-11, DECISIONS #100/#101, follows PR #496): real names, history and
// dates are welcome, but only as sourced facts in an entry's `history` list. KH missions, the Kestrel
// rocket, our pads and Haven are game content and are never presented as real. Kestrel Heavy is not
// related to Blue Origin: its names may appear in relevant, sourced history, but nothing may tie them
// to our game (no-affiliation guard). Mirrored in unity/Assets/Kestrel.Core.Tests/CodexTests.cs.
const assert = require('node:assert');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

const root = path.join(__dirname, '..');
const book = JSON.parse(fs.readFileSync(path.join(root, 'data', 'codex.json'), 'utf8'));
const SITE_NAMES = JSON.parse(fs.readFileSync(path.join(root, 'data', 'sites.json'), 'utf8')).sites.map((s) => s.name);
const ids = ['delta-v', 'twr', 'isp', 'staging', 'max-q', 'cg-cp', 'orbits', 'booster-landing'];

// NA (no-affiliation): Blue Origin, its vehicles and sites. Spacing, dot, underscore and any hyphen or
// dash variant between the words count, and so does no separator at all. These names are allowed in a
// sourced history fact, but never in the same sentence as our game content, and never near a word that
// implies a tie (based on, licensed, official, partner, endorsed, ...). G3 already keeps game content
// out of history facts, and G2 keeps real names out of the game fields.
const SEP = '[\\s._\\-\\u2010-\\u2015]*';
const BO = new RegExp(['blue' + SEP + 'origin', 'new' + SEP + 'glenn', 'new' + SEP + 'shepard', 'jack' + SEP + 'lyn', 'lc' + SEP + '36(?!\\d)'].join('|'), 'i');
const AFFIL = /\b(based on|modell?ed (on|after)|inspired by|licen[cs]ed?|official(ly)?|partner(ed|s|ship)?|endorse(d|s|ment)?|sponsor(ed|s)?|affiliated?|approved by|in collaboration|version of|cop(y|ied) of|clone|replica|tribute)\b/i;
const NEAR = 80;
function affiliation(s) {
  const out = [];
  for (const sent of s.split(/(?<=[.!?])\s+|\n+/)) {
    const b = sent.match(BO);
    if (!b) continue;
    const f = sent.match(FICTION);
    if (f) out.push(`"${b[0]}" with "${f[0]}" in one sentence`);
  }
  const re = new RegExp(BO.source, 'gi');
  for (let m; (m = re.exec(s));) {
    const win = s.slice(Math.max(0, m.index - NEAR), m.index + m[0].length + NEAR).match(AFFIL);
    if (win) out.push(`"${m[0]}" near "${win[0]}"`);
  }
  return out;
}
// Still banned everywhere, unchanged from before: wording rules, not names.
const WORDING = ['suicide', 'RP-1', '\u2014', '\u2013'];
// G2: real-world history markers. These may appear only in a sourced `history` fact, never in the
// game fields (title, kid, engineer), which describe the sim.
const REAL_NAMES = /\b(Blue Origin|New Glenn|New Shepard|Jacklyn|LC-36|SpaceX|NASA|ESA|JAXA|ISRO|Roscosmos|Boeing|Lockheed|Northrop|Ariane(space)?|Soyuz|Vostok|Sputnik|Gagarin|Goddard|Korolev|von Braun|Armstrong|Aldrin|Glenn|Tereshkova|Leonov|Falcon|Starship|Saturn V|Saturn IB?|Apollo|Gemini|Mercury program|Space Shuttle|Shuttle|N1|Mariner|Voyager|Telstar|Hubble|Rocket Lab|ULA|Delta IV|Redstone)\b/;
const YEAR = /\b(1[89]\d\d|20\d\d)\b(?!\s*(m\b|km|kg|t\b|s\b|N\b|Pa|kPa|K\b|%|\u00b0|m\/s|x\b|\.\d|,\d))/;
// G3: fiction markers. A real-history fact that names one of these would present game content as real.
const FICTION = new RegExp(['\\bKH' + SEP + '\\d+', '\\bKestrel', '\\bHaven\\b', ...SITE_NAMES.map((n) => '\\b' + n.replace(/[.*+?^${}()|[\]\\]/g, '\\$&').replace(/\s+/g, SEP) + '\\b')].join('|'), 'i');

function lint(b) {
  const errs = [];
  const all = JSON.stringify(b);
  for (const w of WORDING) if (all.includes(w)) errs.push(`wording: ${JSON.stringify(w)}`);
  const strings = [];
  (function walk(v) {
    if (typeof v === 'string') strings.push(v);
    else if (Array.isArray(v)) v.forEach(walk);
    else if (v && typeof v === 'object') for (const [k, x] of Object.entries(v)) { strings.push(k); walk(x); }
  })(b);
  for (const t of strings) for (const a of affiliation(t)) errs.push(`NA no-affiliation: ${a}`);
  const refs = b.refs || {};
  const used = new Set();
  for (const e of b.entries) {
    // Game fields: sim facts with sim source ids; no real history here.
    if (!Array.isArray(e.sources) || e.sources.length === 0) errs.push(`G4 ${e.id}: sim sources empty`);
    for (const s of e.sources || []) if (!/^[A-Za-z0-9._/-]+$/.test(s)) errs.push(`G4 ${e.id}: bad sim source ${s}`);
    for (const f of ['title', 'kid', 'engineer']) {
      const t = e[f] || '';
      const n = t.match(REAL_NAMES), y = t.match(YEAR);
      if (n) errs.push(`G2 ${e.id}.${f}: real name "${n[0]}" belongs in a sourced history fact`);
      if (y) errs.push(`G2 ${e.id}.${f}: date "${y[0]}" belongs in a sourced history fact`);
    }
    // History: real-world facts only, each with at least one ref id that resolves.
    if (e.history !== undefined && !Array.isArray(e.history)) errs.push(`G4 ${e.id}: history is not a list`);
    (e.history || []).forEach((h, i) => {
      const at = `${e.id}.history[${i}]`;
      if (!h || typeof h.text !== 'string' || !h.text.trim()) { errs.push(`G4 ${at}: text missing`); return; }
      const fx = h.text.match(FICTION);
      if (fx) errs.push(`G3 ${at}: real-history fact names game content "${fx[0]}"`);
      if (!Array.isArray(h.refs) || h.refs.length === 0) errs.push(`G4 ${at}: no source ref`);
      for (const r of h.refs || []) {
        used.add(r);
        if (!Object.prototype.hasOwnProperty.call(refs, r)) errs.push(`G4 ${at}: unknown ref ${r}`);
      }
    });
  }
  for (const [id, r] of Object.entries(refs)) {
    if (!r || typeof r.title !== 'string' || !r.title.trim()) errs.push(`G4 ref ${id}: title missing`);
    if (!r || typeof r.url !== 'string' || !/^https:\/\/\S+$/.test(r.url)) errs.push(`G4 ref ${id}: url must be https`);
    const fx = `${r && r.title}`.match(FICTION);
    if (fx) errs.push(`G3 ref ${id}: source names game content "${fx[0]}"`);
    if (!used.has(id)) errs.push(`G4 ref ${id}: unused`);
  }
  return errs;
}

test('codex has the eight lessons and stays inside the kid limit', () => {
  assert.strictEqual(book.version, 1);
  assert.deepStrictEqual(book.entries.map((e) => e.id), ids);
  const seen = new Set();
  for (const e of book.entries) {
    assert.ok(!seen.has(e.id), e.id);
    seen.add(e.id);
    assert.ok(e.kid.length > 0 && e.kid.length <= 300, `${e.id} kid ${e.kid.length}`);
    assert.ok(e.engineer.length >= 80, e.id);
    assert.ok(e.sources.length > 0, e.id);
    const sentences = e.kid.split(/(?<=\.)\s+/).map((s) => s.trim()).filter(Boolean);
    for (const s of sentences) assert.ok(s.split(/\s+/).length <= 12, `${e.id}: ${s}`);
  }
  assert.match(book.entries[0].engineer, /8876/);
  assert.match(book.entries[0].engineer, /9\.80665/);
  assert.match(book.entries.find((e) => e.id === 'orbits').engineer, /11330/);
  assert.doesNotMatch(book.entries.find((e) => e.id === 'orbits').engineer, /9\.4/);
  assert.match(book.entries.find((e) => e.id === 'booster-landing').engineer, /landing burn/);
  assert.doesNotMatch(JSON.stringify(book), /suicide/);
});

test('codex passes the real-history guards (NA, G2-G4)', () => {
  assert.deepStrictEqual(lint(book), []);
});

// Negative cases: each guard must fail on a bad sample.
const good = () => ({
  version: 1,
  refs: { 'nasa-goddard-1926': { title: 'NASA: 95 years ago, the first liquid-fueled rocket', url: 'https://www.nasa.gov/history/95-years-ago-goddards-first-liquid-fueled-rocket/' } },
  entries: [{
    id: 'a', title: 'Staging', kid: 'Short.', engineer: 'y'.repeat(80), sources: ['RocketEq.DeltaV'],
    history: [{ text: 'In 1969, Apollo 11 rode a Saturn V to the Moon after a 1926 first liquid-fuel flight.', refs: ['nasa-goddard-1926'] }],
  }],
});
const has = (b, tag) => lint(b).some((x) => x.startsWith(tag));

test('guard: a well-formed real-history sample passes (Saturn and Apollo are allowed)', () => {
  assert.deepStrictEqual(lint(good()), []);
});

test('guard NA: Blue Origin names pass in relevant, sourced history', () => {
  for (const n of ['Blue Origin', 'New Glenn', 'Jacklyn', 'LC-36']) {
    const b = good(); b.entries[0].history[0].text = `In 2025, ${n} was in the news.`;
    assert.deepStrictEqual(lint(b), [], n);
  }
  const url = good(); url.refs['nasa-goddard-1926'].url = 'https://www.blueorigin.com/news';
  assert.deepStrictEqual(lint(url), []);
});

test('guard NA fails when Codex text ties our game to Blue Origin, its vehicles or sites', () => {
  const names = ['Blue Origin', 'blue-origin', 'BlueOrigin', 'NEW GLENN', 'New\u2011Glenn', 'newglenn', 'New Shepard', 'Jacklyn', 'jack-lyn', 'LC-36', 'lc 36', 'LC\u201336'];
  for (const n of names) {
    // A fictional item in the same sentence, in a game field, a history fact, or a ref title.
    const kid = good(); kid.entries[0].kid = `Haven is like ${n}.`;
    const hist = good(); hist.entries[0].history[0].text = `In 2025, Pier 7 hosted ${n}.`;
    const ref = good(); ref.refs['nasa-goddard-1926'].title = `Kestrel and ${n}`;
    // An affiliation word near the name, even with no fictional item named.
    const aff = good(); aff.entries[0].history[0].text = `In 2025, this game was officially licensed by ${n}.`;
    const based = good(); based.entries[0].history[0].text = `In 2025, the design was based on ${n}.`;
    for (const [w, b] of Object.entries({ kid, hist, ref, aff, based })) assert.ok(has(b, 'NA'), `${w}: ${n}`);
  }
  const later = good(); later.entries[0].history[0].text = 'In 2025, New Glenn flew. A partner station is planned.'; // within 80 chars
  assert.ok(has(later, 'NA'));
  const lc = good(); lc.entries[0].history[0].text = 'In 1969, LC-360 was a test stand name, officially.';
  assert.ok(!lint(lc).some((x) => x.startsWith('NA')), 'LC-360 is not LC-36');
});

test('guard G2 also keeps Blue Origin names out of the game fields', () => {
  const b = good(); b.entries[0].kid = 'New Glenn lands too.';
  assert.ok(has(b, 'G2'));
});

test('guard G2 fails on a real name or a date in the game fields', () => {
  for (const [f, t] of [['kid', 'Apollo used stages.'], ['title', 'Saturn V'], ['engineer', 'y'.repeat(80) + ' NASA said so.'], ['kid', 'It flew in 1969.']]) {
    const b = good(); b.entries[0][f] = t;
    assert.ok(has(b, 'G2'), `${f}: ${t}`);
  }
  const units = good(); units.entries[0].engineer = 'y'.repeat(80) + ' LOSS_BUDGET of 1500 m/s and 2000 kg.';
  assert.ok(!has(units, 'G2'), 'sim numbers with units are not dates');
});

test('guard G3 fails when a real-history fact names a KH mission, Kestrel, a pad or Haven', () => {
  for (const s of ['KH-3', 'kh 12', 'KH\u20137', 'Kestrel Heavy', 'the Kestrel', 'Pier 7', 'pier-7', 'Haven', 'Coastal Pad B', 'Salt Flat Pad', 'North Reach']) {
    const b = good(); b.entries[0].history[0].text = `In 1969, ${s} flew for real.`;
    assert.ok(has(b, 'G3'), s);
  }
  const r = good(); r.refs['nasa-goddard-1926'].title = 'Kestrel launch log';
  assert.ok(has(r, 'G3'), 'ref title');
});

test('guard G4 fails when a fact or lesson loses its source id', () => {
  const none = good(); none.entries[0].history[0].refs = [];
  const missing = good(); delete missing.entries[0].history[0].refs;
  const unknown = good(); unknown.entries[0].history[0].refs = ['made-up'];
  const http = good(); http.refs['nasa-goddard-1926'].url = 'http://www.nasa.gov/';
  const unused = good(); unused.refs.extra = { title: 'Extra', url: 'https://example.com/' };
  const noSim = good(); noSim.entries[0].sources = [];
  for (const [n, b] of Object.entries({ none, missing, unknown, http, unused, noSim })) assert.ok(has(b, 'G4'), n);
});

test('wording bans still hold everywhere, history included', () => {
  for (const w of ['suicide burn', 'RP-1', 'a\u2014b', 'a\u2013b']) {
    const b = good(); b.entries[0].history[0].text += ` ${w}`;
    assert.ok(lint(b).some((x) => x.startsWith('wording')), w);
  }
});

module.exports = { lint };
