// KID rows: a pilot who holds the landing burn (so the gear comes down) must score at
// least as well as the pilot who does nothing, on every mission. Heavy (headless Chrome),
// so it lives with the D0 replay gate (`npm run test:replay`, and CI=true `npm test`).
// The burn pilot is the recorder's own pilot with `pilotKind: 'burn'` (holds Space while the
// burn window is open); its recorded input is replayed to prove it is deterministic.
const assert = require('node:assert/strict');
const { test, before, after } = require('node:test');
const fs = require('node:fs');
const path = require('node:path');

const DIR = path.join(__dirname, 'traces');
const OUTCOME = { recovered: 3, salvage: 2, tip: 1 };
const MEDAL = { gold: 3, silver: 2, bronze: 1 };
const rank = (r) => [OUTCOME[r.outcome] || 0, MEDAL[r.medal] || 0, r.score];
const MISSIONS = ['KH-1', 'KH-2', 'KH-3', 'KH-4', 'KH-5'];
let H; let g;

before(async () => {
  H = await import(path.join(__dirname, '../../tools/replay/harness.mjs'));
  g = await H.openGame();
});
after(async () => { await g?.close(); });

for (const m of MISSIONS) {
  test(`${m}_KID: burn + gear scores at least as well as doing nothing`, async () => {
    const gold = JSON.parse(fs.readFileSync(path.join(DIR, `${m}_KID.json`), 'utf8'));
    const idle = await g.run({ mission: gold.mission, difficulty: gold.difficulty, seed: gold.seed, mode: 'replay', trace: gold.input });
    const rec = await g.run({ mission: gold.mission, difficulty: gold.difficulty, seed: gold.seed, mode: 'record', pilotKind: 'burn' });
    const burn = await g.run({ mission: gold.mission, difficulty: gold.difficulty, seed: gold.seed, mode: 'replay', trace: rec.trace });
    assert.deepEqual(burn.result, rec.result, 'the burn pilot replays deterministically');
    assert.ok(rec.trace.some(([f, k, d]) => k === 'Space' && d === 1 && f > 3000), 'burn pilot holds Space in Haven');
    // The idle pilot never lit the burn, so no gear: not recovered, bronze at best.
    assert.notEqual(idle.result.outcome, 'recovered', 'gear never came down');
    assert.ok((MEDAL[idle.result.medal] || 0) <= MEDAL.bronze, 'no burn -> bronze at most');
    // The burn + gear lander is not penalised by the gate.
    assert.equal(burn.result.outcome, 'recovered');
    assert.ok(MEDAL[burn.result.medal] >= MEDAL.silver, `burn + gear medal ${burn.result.medal}`);
    const a = rank(burn.result); const b = rank(idle.result);
    for (let i = 0; i < 3; i++) {
      if (a[i] !== b[i]) { assert.ok(a[i] > b[i], `burn ${JSON.stringify(burn.result)} must not be worse than idle ${JSON.stringify(idle.result)}`); break; }
    }
    assert.deepEqual(g.errors, [], 'no page exceptions');
  });
}


