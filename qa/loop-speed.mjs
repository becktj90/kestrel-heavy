// Loop-speed probe: real taps (Playwright touch) against web/; measures wall time + tap counts for
// first open -> liftoff, failure -> liftoff, results -> liftoff. node qa/loop-speed.mjs [runs]
import { chromium } from 'playwright';
import http from 'node:http'; import fs from 'node:fs'; import path from 'node:path'; import { fileURLToPath } from 'node:url';
const web = path.join(path.dirname(fileURLToPath(import.meta.url)), '..', 'web');
const srv = http.createServer((q, s) => { let p = decodeURIComponent(new URL(q.url, 'http://x').pathname); if (p.endsWith('/')) p += 'index.html'; const f = path.join(web, p); if (!f.startsWith(web) || !fs.existsSync(f) || fs.statSync(f).isDirectory()) { s.writeHead(404); return s.end(); } s.writeHead(200, { 'content-type': p.endsWith('.js') ? 'text/javascript' : p.endsWith('.html') ? 'text/html' : p.endsWith('.css') ? 'text/css' : 'application/octet-stream' }); fs.createReadStream(f).pipe(s); });
await new Promise((r) => srv.listen(0, '127.0.0.1', r)); const BASE = `http://127.0.0.1:${srv.address().port}/`;
const RUNS = Number(process.argv[2] || 3); const VP = { width: 430, height: 932 };
const out = [];
const st = (pg) => pg.evaluate(() => window.__ngGame?.scene?.scenes?.[0]?.status);
const until = async (pg, want, ms = 20000) => { const t = Date.now(); while (Date.now() - t < ms) { if (want.includes(await st(pg))) return Date.now(); await pg.waitForTimeout(15); } throw new Error(`timeout waiting ${want}`); };
const tap = async (pg, sel, log) => { const el = await pg.waitForSelector(sel, { state: 'visible', timeout: 10000 }); const b = await el.boundingBox(); await pg.touchscreen.tap(b.x + b.width / 2, b.y + b.height / 2); log.push(sel); };
const tapCanvas = async (pg, log) => { await pg.touchscreen.tap(VP.width / 2, VP.height * 0.55); log.push('playfield'); };
const visibleButtons = (pg) => pg.evaluate(() => [...document.querySelectorAll('#ng-summary button')].filter((b) => !b.hidden && b.offsetParent).map((b) => `${b.id}:${b.textContent.trim()}`));
for (let i = 0; i < RUNS; i++) {
  const b = await chromium.launch({ args: ['--disable-webgl', '--disable-3d-apis'] });
  const ctx = await b.newContext({ viewport: VP, isMobile: true, hasTouch: true }); const pg = await ctx.newPage();
  await pg.route(/fonts\.(googleapis|gstatic)/, (r) => r.abort());
  const r = {};
  // A. first open -> first liftoff (fresh storage)
  const t0 = Date.now(); await pg.goto(BASE); const tMenu = await until(pg, ['MENU']);
  let log = []; await tap(pg, '#ng-play', log); await until(pg, ['PRELAUNCH']); await tapCanvas(pg, log);
  const tLift = await until(pg, ['ASCENT']);
  r.firstOpen = { toMenuMs: tMenu - t0, menuToLiftoffMs: tLift - tMenu, totalMs: tLift - t0, taps: log };
  // B. failure -> relaunch: forced RUD 3 s into ascent (stand-in for a bird strike)
  await pg.waitForTimeout(3000);
  const tFail = Date.now(); await pg.evaluate(() => window.__ngGame.scene.scenes[0].failFlight('bird'));
  await pg.waitForSelector('#ng-summary:not([hidden])', { timeout: 15000 }); const tSum = Date.now();
  const failButtons = await visibleButtons(pg);
  log = []; await tap(pg, '#ng-summary-continue', log); await until(pg, ['MENU']); await tap(pg, '#ng-play', log); await until(pg, ['PRELAUNCH']); await tapCanvas(pg, log);
  const tLift2 = await until(pg, ['ASCENT']);
  r.failure = { failToSummaryMs: tSum - tFail, summaryToLiftoffMs: tLift2 - tSum, failToLiftoffMs: tLift2 - tFail, taps: log, summaryButtons: failButtons };
  // C. results -> relaunch: forced salvage-on-deck end (Haven contact path, includes finishUpper delays)
  await pg.evaluate(() => { const sc = window.__ngGame.scene.scenes[0]; sc.retryLanding?.(); });
  await pg.evaluate(() => { const sc = window.__ngGame.scene.scenes[0]; sc.bootQaFlight(69.2, () => sc.enterJacklyn()); });
  await until(pg, ['HAVEN', 'JACKLYN']);
  const tLand = Date.now(); await pg.evaluate(() => { const sc = window.__ngGame.scene.scenes[0]; sc.session.salvage = true; sc.finishUpper(); });
  await pg.waitForSelector('#ng-summary:not([hidden])', { timeout: 15000 }); const tRes = Date.now();
  const resButtons = await visibleButtons(pg);
  log = []; const next = await pg.$('#ng-sum-next:not([hidden])');
  if (next) { await tap(pg, '#ng-sum-next', log); } else { await tap(pg, '#ng-summary-continue', log); await until(pg, ['MENU']); await tap(pg, '#ng-play', log); }
  await until(pg, ['PRELAUNCH']); await tapCanvas(pg, log); const tLift3 = await until(pg, ['ASCENT']);
  r.results = { finishUpperToSummaryMs: tRes - tLand, summaryToLiftoffMs: tLift3 - tRes, taps: log, summaryButtons: resButtons };
  out.push(r); console.log(JSON.stringify(r)); await b.close();
}
srv.close(); fs.writeFileSync(process.argv[3] || '/tmp/loop-speed.json', JSON.stringify(out, null, 1));
