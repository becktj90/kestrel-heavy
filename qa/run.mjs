// Kestrel Heavy QA bot harness: parallel Playwright runs over seeds x flights x skills x viewports.
// Usage: node qa/run.mjs [--seeds N] [--workers N] [--flights KH-1,..] [--skills low,mid,high] [--viewports phone,max,ipad] [--out dir]
import { chromium } from 'playwright';
import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const webRoot = path.join(here, '..', 'web');
const arg = (k, d) => { const i = process.argv.indexOf(`--${k}`); return i > 0 ? process.argv[i + 1] : d; };
const SEEDS = Number(arg('seeds', 3));
const WORKERS = Number(arg('workers', 6));
const FLIGHTS = arg('flights', 'KH-1,KH-2,KH-3,KH-4,KH-5').split(',');
const SKILLS = arg('skills', 'low,mid,high').split(',');
const VPS = { phone: { width: 390, height: 844 }, max: { width: 440, height: 956 }, ipad: { width: 1024, height: 1366 }, 'phone-land': { width: 844, height: 390 }, 'max-land': { width: 956, height: 440 }, 'ipad-land': { width: 1366, height: 1024 } };
const VIEWS = arg('viewports', 'phone,max,ipad').split(',');
const OUT = path.resolve(arg('out', path.join(here, 'out')));
const TIMEOUT_S = Number(arg('timeout', 200));
const STUCK_S = Number(arg('stuck', 20));
const GC_EXTRA = Number(arg('gc', 6)); // extra offline Game Center bridge runs
fs.mkdirSync(path.join(OUT, 'shots'), { recursive: true });
const botSrc = fs.readFileSync(path.join(here, 'bot.js'), 'utf8');

const MIME = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.png': 'image/png', '.mp3': 'audio/mpeg', '.ogg': 'audio/ogg', '.wav': 'audio/wav', '.json': 'application/json', '.svg': 'image/svg+xml', '.m4a': 'audio/mp4', '.webp': 'image/webp' };
const server = http.createServer((req, res) => {
  let p = decodeURIComponent(new URL(req.url, 'http://x').pathname); if (p.endsWith('/')) p += 'index.html';
  const f = path.join(webRoot, p);
  if (!f.startsWith(webRoot) || !fs.existsSync(f)) { res.writeHead(404); return res.end(); }
  res.writeHead(200, { 'content-type': MIME[path.extname(f)] || 'application/octet-stream' }); fs.createReadStream(f).pipe(res);
});
await new Promise((r) => server.listen(0, '127.0.0.1', r));
const BASE = `http://127.0.0.1:${server.address().port}/`;

const jobs = [];
for (const flight of FLIGHTS) for (const skill of SKILLS) for (const vp of VIEWS) for (let s = 1; s <= SEEDS; s++) jobs.push({ flight, skill, vp, seed: s * 1009 + FLIGHTS.indexOf(flight) * 17 });
for (let i = 0; i < GC_EXTRA; i++) jobs.push({ flight: 'KH-1', skill: 'high', vp: VIEWS[i % VIEWS.length], seed: 7000 + i, gc: i % 2 ? 'throw' : 'ok', offline: true });

const HUD_SEL = ['.mc-panel', '#ng-phase', '#t-clock', '#ng-health', '#ng-banner', '#t-obj', '#t-next', '#t-radio', '#t-payload',
  '#arcade-mute-btn', '#arcade-pause-btn', '#arcade-settings-btn', '#arcade-fullscreen-btn', '#ng-sep-btn', '#arcade-touch-btns button',
  '#ng-summary h2', '#ng-summary button', '#ng-sum-medal', '#ng-sum-why', '#ng-recover'];

async function overlapScan(page) {
  return page.evaluate((sels) => {
    const els = [];
    for (const sel of sels) for (const el of document.querySelectorAll(sel)) {
      const cs = getComputedStyle(el); if (el.hidden || cs.display === 'none' || cs.visibility === 'hidden' || Number(cs.opacity) < 0.05) continue;
      if (el.closest('[hidden]')) continue;
      const r = el.getBoundingClientRect(); if (r.width < 2 || r.height < 2) continue;
      if (!els.some((e) => e.el === el)) els.push({ el, sel, id: el.id || `${sel}:${(el.textContent || '').trim().slice(0, 16)}`, r });
    }
    const vw = innerWidth; const vh = innerHeight; const out = [];
    for (const a of els) if (a.r.left < -1 || a.r.top < -1 || a.r.right > vw + 1 || a.r.bottom > vh + 1) out.push({ kind: 'offscreen', a: a.id, rect: [a.r.left, a.r.top, a.r.right, a.r.bottom].map(Math.round) });
    for (let i = 0; i < els.length; i++) for (let j = i + 1; j < els.length; j++) {
      const a = els[i]; const b = els[j];
      if (a.el.contains(b.el) || b.el.contains(a.el)) continue;
      const w = Math.min(a.r.right, b.r.right) - Math.max(a.r.left, b.r.left);
      const h = Math.min(a.r.bottom, b.r.bottom) - Math.max(a.r.top, b.r.top);
      if (w > 2 && h > 2) out.push({ kind: 'overlap', a: a.id, b: b.id, area: Math.round(w * h), w: Math.round(w), h: Math.round(h) });
    }
    return { phase: document.body.dataset.phase, issues: out };
  }, HUD_SEL);
}

const GL = process.argv.includes('--webgl');
// Default: Canvas renderer (WebGL disabled) - swiftshader WebGL runs ~0.5x game speed with 6+ browsers; canvas holds 60fps.
const LAUNCH = { args: [...(GL ? ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] : ['--disable-webgl', '--disable-3d-apis']), '--autoplay-policy=no-user-gesture-required', '--disable-background-timer-throttling', '--disable-renderer-backgrounding', '--disable-backgrounding-occluded-windows'] };
// One browser per job: sharing a browser across pages starves rAF in non-foreground pages
// (Matter stops stepping while the game clock keeps going), which shows up as a false 'stuck on pad'.
async function runJob(_b, job, idx) {
  const browser = await chromium.launch(LAUNCH);
  const tag = `${job.flight}_${job.skill}_${job.vp}_s${job.seed}${job.gc ? `_gc-${job.gc}` : ''}`;
  const ctx = await browser.newContext({ viewport: VPS[job.vp], deviceScaleFactor: 1, isMobile: !job.vp.startsWith('ipad'), hasTouch: true });
  const page = await ctx.newPage();
  const r = { ...job, tag, errors: [], console: [], network: [], overlaps: {}, statuses: [], outcome: null, reason: null, score: null, stuck: null, shots: [] };
  page.on('pageerror', (e) => r.errors.push(String(e.stack || e).slice(0, 600)));
  page.on('console', (m) => { if (m.type() === 'error') r.console.push(m.text().slice(0, 300)); });
  page.on('request', (q) => { const u = q.url(); if (!u.startsWith(BASE) && !u.startsWith('data:') && !u.startsWith('blob:')) r.network.push(u.slice(0, 120)); }); // G8 privacy
  page.on('response', (q) => { if (q.status() >= 400) r.errors.push(`HTTP ${q.status()} ${q.url().replace(BASE, '/')}`); });
  page.on('crash', () => r.errors.push('PAGE CRASH'));
  await page.addInitScript(`window.__KH_QA_CFG=${JSON.stringify({ seed: job.seed, skill: job.skill, gcBridge: job.gc || null })};\n${botSrc}`);
  if (job.offline) await page.route(/^https?:\/\/(?!127\.0\.0\.1)/, (rt) => rt.abort('internetdisconnected'));
  else await page.route(/fonts\.(googleapis|gstatic)\.com/, (rt) => rt.abort()); // keep runs hermetic
  const shot = async (n) => { const p = path.join(OUT, 'shots', `${tag}_${n}.png`); await page.screenshot({ path: p }).catch(() => {}); r.shots.push(path.relative(OUT, p)); return p; };
  try {
    await page.goto(BASE, { waitUntil: 'load', timeout: 30000 });
    if (job.offline) await ctx.setOffline(true);
    await page.waitForFunction(() => window.__ngGame?.scene?.scenes?.[0]?.status === 'MENU', null, { timeout: 30000 });
    const menuScan = await overlapScan(page); if (menuScan.issues.length) r.overlaps.MENU = menuScan.issues;
    await page.evaluate((f) => {
      const sc = window.__ngGame.scene.scenes[0];
      if (f !== 'KH-1') { sc.settings.unlockedMissions = ['KH-1', 'KH-2', 'KH-3', 'KH-4', 'KH-5']; sc.settings.missionCount = 3; }
      sc.setMission(f); sc.startMission();
    }, job.flight);
    const t0 = Date.now(); let lastSig = ''; let lastChange = Date.now(); const seenPhases = new Set();
    while (true) {
      await page.waitForTimeout(1000);
      const st = await page.evaluate(() => { const sc = window.__ngGame.scene.scenes[0]; const s = sc.session || {};
        return { status: sc.status, stage: s.stage, x: Math.round(sc.rocket?.x), y: Math.round(sc.rocket?.y), t: +(s.tClock || 0).toFixed(1), alt: +(s.altitudeKm || 0).toFixed(1), health: s.health, score: Math.round(s.score || 0), jp: s.jacklynPhase, sp: s.sepPhase, recovered: s.recovered, failReason: s.failReason || null, paused: sc.paused, gc: window.__qaGcCalls || 0 }; });
      if (!seenPhases.has(st.status)) {
        seenPhases.add(st.status); r.statuses.push(st.status);
        await page.waitForTimeout(400);
        const sc = await overlapScan(page); if (sc.issues.length) { r.overlaps[st.status] = sc.issues; await shot(`overlap-${st.status}`); }
      }
      const sig = `${st.status}|${st.stage}|${st.x}|${st.y}|${st.t}|${st.jp}|${st.sp}`;
      if (sig !== lastSig) { lastSig = sig; lastChange = Date.now(); }
      if (st.status === 'SUMMARY') {
        const sum = await page.evaluate(() => ({ title: document.getElementById('ng-summary-title')?.textContent, body: document.getElementById('ng-summary-body')?.textContent, visible: !document.getElementById('ng-summary')?.hidden, bf: window.__ngGame.scene.scenes[0].settings.bestFlight }));
        r.outcome = sum.bf?.reason || 'unknown'; r.reason = st.failReason; r.score = sum.bf?.score; r.summary = sum; r.gcCalls = st.gc;
        if (!sum.visible) r.errors.push('SUMMARY status but summary overlay hidden');
        await shot('summary'); break;
      }
      if ((Date.now() - lastChange) / 1000 > STUCK_S) { r.stuck = { ...st, note: `no state change for ${STUCK_S}s` }; await shot('stuck'); break; }
      if ((Date.now() - t0) / 1000 > TIMEOUT_S) { r.stuck = { ...st, note: `timeout ${TIMEOUT_S}s` }; await shot('timeout'); break; }
    }
    // After summary: exercise return to menu + replay path for broken-state detection
    if (r.outcome) {
      await page.evaluate(() => window.__ngGame.scene.scenes[0].onPrimary());
      await page.waitForTimeout(800);
      const back = await page.evaluate(() => window.__ngGame.scene.scenes[0].status);
      if (back !== 'MENU') r.errors.push(`after SUMMARY->primary status is ${back}, expected MENU`);
    }
  } catch (e) { r.errors.push(`HARNESS: ${String(e.message || e).slice(0, 300)}`); await shot('harness-error'); }
  r.cleared = ['recovered', 'salvage', 'splash'].includes(r.outcome);
  // Known-benign in standalone web: iOS pack stubs local-store.js; reported separately, not a run failure.
  const KNOWN = [/^HTTP 404 \/toolbox\/js\/local-store\.js/];
  r.pass = r.cleared && !r.errors.filter((e) => !KNOWN.some((k) => k.test(e))).length && !r.stuck && !r.console.filter((c) => !/Failed to load resource/.test(c)).length && !Object.keys(r.overlaps).length;
  await ctx.close().catch(() => {}); await browser.close().catch(() => {});
  if (!job.retried && r.errors.some((e) => e.startsWith('HARNESS: ') && /closed/.test(e))) { job.retried = true; return runJob(_b, job, idx); } // infra kill, not a game bug
  process.stdout.write(`[${idx + 1}/${jobs.length}] ${tag} -> ${r.outcome || (r.stuck ? 'STUCK' : 'ERR')} ${r.reason || ''} errs=${r.errors.length} ovl=${Object.keys(r.overlaps).join('/')}\n`);
  return r;
}

const browser = null;
const results = []; let next = 0;
const jsonl = path.join(OUT, 'results.jsonl'); fs.writeFileSync(jsonl, '');
await Promise.all(Array.from({ length: WORKERS }, async () => { while (next < jobs.length) { const i = next++; results[i] = await runJob(browser, jobs[i], i); fs.appendFileSync(jsonl, JSON.stringify(results[i]) + '\n'); } }));
server.close();
fs.writeFileSync(path.join(OUT, 'results.json'), JSON.stringify(results, null, 1));
console.log(`done: ${results.filter((r) => r.pass).length}/${results.length} pass -> ${OUT}/results.json`);
