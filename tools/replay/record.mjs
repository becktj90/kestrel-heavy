#!/usr/bin/env node
// Re-record golden traces: node --experimental-websocket tools/replay/record.mjs [KH-1_KID ...]
// Only re-record when a behaviour change is INTENDED; commit the diff with the change.
import { writeFileSync, mkdirSync } from 'node:fs';
import path from 'node:path';
import { openGame, MISSIONS, DIFFICULTIES, TRACE_DIR, caseId, seedFor } from './harness.mjs';

const only = process.argv.slice(2);
mkdirSync(TRACE_DIR, { recursive: true });
const g = await openGame();
try {
  for (const m of MISSIONS) for (const d of DIFFICULTIES) {
    const id = caseId(m, d);
    if (only.length && !only.includes(id)) continue;
    const seed = seedFor(m, d);
    const rec = await g.run({ mission: m, difficulty: d, seed, mode: 'record' });
    const rep = await g.run({ mission: m, difficulty: d, seed, mode: 'replay', trace: rec.trace });
    const same = JSON.stringify(rep.checkpoints) === JSON.stringify(rec.checkpoints);
    if (!same) throw new Error(`${id}: replay of fresh recording diverged — nondeterminism`);
    const golden = { id, mission: m, difficulty: d, seed, frameMs: 1000 / 60, input: rec.trace, expect: { result: rep.result, phases: rep.phases, events: rep.events, checkpoints: rep.checkpoints, frames: rep.frames } };
    writeFileSync(path.join(TRACE_DIR, `${id}.json`), `${JSON.stringify(golden)}\n`);
    console.log(id, rep.result.outcome, rep.result.score, rep.result.medal, `${rep.frames}f`);
  }
  if (g.errors.length) console.warn('page errors:', g.errors);
} finally { await g.close(); }
