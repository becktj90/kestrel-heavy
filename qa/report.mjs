// Summarise results.json: pass rates by flight/skill/viewport and grouped findings.
import fs from 'node:fs';
const f = process.argv[2] || 'out/results.json'; const txt = fs.readFileSync(f, 'utf8');
const rs = (f.endsWith('.jsonl') ? txt.trim().split('\n').map((l) => JSON.parse(l)) : JSON.parse(txt)).filter(Boolean);
const pct = (a) => `${a.filter((r) => r.pass).length}/${a.length} (${Math.round(100 * a.filter((r) => r.pass).length / Math.max(1, a.length))}%)`;
const clr = (a) => `${a.filter((r) => r.cleared).length}/${a.length}`;
const grp = (k) => { const m = {}; for (const r of rs.filter((x) => !x.gc)) (m[r[k]] ||= []).push(r); return Object.entries(m).map(([g, a]) => `  ${g.padEnd(10)} pass ${pct(a)}  cleared ${clr(a)}`).join('\n'); };
const main = rs.filter((r) => !r.gc);
console.log(`OVERALL (bot matrix) pass ${pct(main)}  cleared ${clr(main)}`);
console.log('by flight\n' + grp('flight')); console.log('by skill\n' + grp('skill')); console.log('by viewport\n' + grp('vp'));
const k1 = main.filter((r) => r.flight === 'KH-1');
for (const sk of ['low', 'mid', 'high']) { const a = k1.filter((r) => r.skill === sk); const o = {}; a.forEach((r) => { o[r.outcome || (r.stuck ? 'STUCK' : 'ERR')] = (o[r.outcome || (r.stuck ? 'STUCK' : 'ERR')] || 0) + 1; }); console.log(`KH-1 ${sk}: ${JSON.stringify(o)}`); }
const out = {}; for (const r of main) { const k = `${r.flight}/${r.skill}`; (out[k] ||= {}); const o = r.outcome || (r.stuck ? 'STUCK' : 'ERR'); out[k][o] = (out[k][o] || 0) + 1; }
console.log('outcomes', JSON.stringify(out));
const gc = rs.filter((r) => r.gc); console.log(`GameCenter offline runs: ${gc.map((r) => `${r.tag}:${r.outcome || 'STUCK'} errs=${r.errors.length} cons=${r.console.length} gcCalls=${r.gcCalls}`).join('; ')}`);
const errs = {}; for (const r of rs) for (const e of [...r.errors, ...r.console]) (errs[e.split('\n')[0].slice(0, 160)] ||= []).push(r.tag);
console.log('ERRORS'); for (const [e, t] of Object.entries(errs)) console.log(`  ${t.length}x ${e}  e.g. ${t[0]}`);
const ov = {}; for (const r of rs) for (const [ph, list] of Object.entries(r.overlaps || {})) for (const i of list) { const k = `${ph} ${i.kind} ${i.a}${i.b ? ' vs ' + i.b : ''}`; (ov[k] ||= new Set()).add(r.vp); }
console.log('OVERLAPS'); for (const [k, v] of Object.entries(ov)) console.log(`  ${k}  [${[...v].join(',')}]`);
console.log('STUCK'); for (const r of rs.filter((x) => x.stuck)) console.log(`  ${r.tag} ${r.stuck.note} ${r.stuck.status}/${r.stuck.stage}/${r.stuck.jp} t=${r.stuck.t}`);
const net = new Set(rs.flatMap((r) => r.network || [])); console.log('NETWORK (off-box requests)', [...net]);
console.log('RUD reasons', JSON.stringify(main.filter((r) => r.outcome === 'rud').reduce((m, r) => (m[r.reason] = (m[r.reason] || 0) + 1, m), {})));
