// G3-style headless fuzz of the M1 physics sim (web/js/physics/headless.js): seeded random
// payloads/targets/latitudes/dt; asserts no NaN, bounded time, known outcome. Fast, no browser.
import { simulate, MEDIUM_LIFTER } from '../web/js/physics/headless.js';
const N = Number(process.argv[2] || 300); let s = 12345;
const rnd = () => ((s = (s * 1103515245 + 12345) & 0x7fffffff) / 0x7fffffff);
const fails = []; const outcomes = {};
for (let i = 0; i < N; i++) {
  const o = { payload: Math.round(rnd() * 40000), targetAlt: 120000 + rnd() * 600000, latitude: -60 + rnd() * 120, dt: [0.02, 0.05, 0.1, 0.25][i % 4] };
  const t0 = Date.now(); let r;
  try { r = simulate(MEDIUM_LIFTER, o); } catch (e) { fails.push({ i, o, err: String(e) }); continue; }
  const ms = Date.now() - t0; outcomes[r.outcome] = (outcomes[r.outcome] || 0) + 1;
  const nums = [r.time, r.maxQ, r.maxG, r.orbit?.apoAlt, r.orbit?.periAlt].filter((x) => x !== undefined);
  if (nums.some((x) => !Number.isFinite(x)) && r.outcome === 'orbit') fails.push({ i, o, err: 'non-finite result', r: { time: r.time, orbit: r.orbit } });
  if (ms > 5000) fails.push({ i, o, err: `slow ${ms}ms` });
}
console.log(JSON.stringify({ runs: N, outcomes, fails: fails.slice(0, 20), failCount: fails.length }, null, 1));
