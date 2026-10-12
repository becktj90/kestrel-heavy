// Shared harness: serve web/, boot the real game in headless Chrome under the
// deterministic hooks, and run arcade cases. Used by record.mjs and the test.
import http from 'node:http';
import { readFileSync, existsSync, statSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { launch } from './cdp.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const WEB = path.resolve(HERE, '../../web');
export const MISSIONS = ['KH-1', 'KH-2', 'KH-3', 'KH-4', 'KH-5'];
export const DIFFICULTIES = ['KID', 'CADET', 'PAD_RAT'];
export const TRACE_DIR = path.resolve(HERE, '../../tests/replay/traces');
export const caseId = (m, d) => `${m}_${d}`;
export const seedFor = (m, d) => 1000 + MISSIONS.indexOf(m) * 10 + DIFFICULTIES.indexOf(d);

const TYPES = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.json': 'application/json', '.png': 'image/png', '.mp3': 'audio/mpeg', '.m4a': 'audio/mp4', '.ogg': 'audio/ogg', '.wav': 'audio/wav', '.svg': 'image/svg+xml' };

function serve() {
  const server = http.createServer((req, res) => {
    let p = decodeURIComponent(new URL(req.url, 'http://x').pathname);
    if (p === '/') p = '/index.html';
    const f = path.join(WEB, path.normalize(p));
    if (!f.startsWith(WEB) || !existsSync(f) || statSync(f).isDirectory()) { res.writeHead(404); res.end(); return; }
    res.writeHead(200, { 'content-type': TYPES[path.extname(f)] || 'application/octet-stream' });
    res.end(readFileSync(f));
  });
  return new Promise((r) => server.listen(0, '127.0.0.1', () => r(server)));
}

export async function openGame() {
  const server = await serve();
  const port = server.address().port;
  const b = await launch();
  await b.send('Page.enable');
  await b.send('Runtime.enable');
  await b.send('Network.enable');
  // Pin the viewport: camera zoom (and so hazard spawn rows) depends on the parent size.
  await b.send('Emulation.setDeviceMetricsOverride', { width: 430, height: 932, deviceScaleFactor: 1, mobile: false, screenWidth: 430, screenHeight: 932 });
  await b.send('Emulation.setTouchEmulationEnabled', { enabled: false });
  // Keep the page focused: a blur clears held keys (input.js) and resets Phaser's loop delta.
  await b.send('Emulation.setFocusEmulationEnabled', { enabled: true });
  await b.send('Network.setBlockedURLs', { urls: ['*fonts.googleapis.com*', '*fonts.gstatic.com*'] });
  await b.send('Page.addScriptToEvaluateOnNewDocument', { source: readFileSync(path.join(HERE, 'page-hooks.js'), 'utf8') });
  const errors = [];
  b.on((m) => { if (m.method === 'Runtime.exceptionThrown') errors.push(m.params.exceptionDetails?.exception?.description || m.params.exceptionDetails?.text); });
  const origin = `http://127.0.0.1:${port}`;
  async function boot() {
    await b.send('Storage.clearDataForOrigin', { origin, storageTypes: 'all' });
    await b.send('Page.navigate', { url: `${origin}/index.html` });
    const t0 = Date.now();
    for (;;) {
      const ok = await b.evaluate(`(() => { if (!window.__replay) return false; window.__replay.step(1); const s = window.__ngGame?.scene?.scenes?.[0]; return !!(s && s.sys?.settings?.status >= 5 && s.status === 'MENU' && s.rocket); })()`).catch(() => false);
      if (ok) break;
      if (Date.now() - t0 > 30000) throw new Error(`game did not boot: ${errors.join(' | ')}`);
      await new Promise((r) => setTimeout(r, 20));
    }
    // Rendering cannot affect game state; skip it for speed (logic still runs every frame).
    if (!process.env.REPLAY_RENDER) await b.evaluate(`(() => { const r = window.__ngGame.renderer; for (const k of ['preRender','render','postRender']) if (typeof r[k] === 'function') r[k] = () => {}; })()`);
    await b.evaluate(readFileSync(path.join(HERE, 'runner.js'), 'utf8'));
  }
  return {
    errors,
    // Every case gets a fresh page and empty storage, so runs are independent.
    async run(opts) { await boot(); return b.evaluate(`window.__replayRun(${JSON.stringify(opts)})`); },
    async close() { await b.close(); server.close(); },
  };
}
