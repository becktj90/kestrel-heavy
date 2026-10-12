// D0 contract: seeds, portrait viewport, input clock, FNV-1a, recorded expect, medal grade.
// Does not re-simulate Matter. Checkpoint body state stays in tests/replay/traces.
import { readFileSync, readdirSync } from 'node:fs';
import { gradeLanding } from '../../web/js/medals.js';

const MISSIONS = ['KH-1', 'KH-2', 'KH-3', 'KH-4', 'KH-5'];
const DIFFS = ['KID', 'CADET', 'PAD_RAT'];
const seedFor = (mission, difficulty) => 1000 + MISSIONS.indexOf(mission) * 10 + DIFFS.indexOf(difficulty);
const fnv = (s) => {
  let h = 0x811c9dc5;
  for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 0x01000193); }
  return (h >>> 0).toString(16).padStart(8, '0');
};
const PORTRAIT = '{"w":430,"h":932}';

function masksOf(input, frames) {
  const held = { ArrowLeft: 0, ArrowRight: 0, Space: 0 };
  let ei = 0;
  const masks = [];
  for (let f = 0; f < frames; f += 30) {
    while (ei < input.length && input[ei][0] <= f) {
      held[input[ei][1]] = input[ei][2] ? 1 : 0;
      ei++;
    }
    let m = 0;
    if (held.ArrowLeft) m |= 1;
    if (held.ArrowRight) m |= 2;
    if (held.Space) m |= 4;
    masks.push(m);
  }
  return masks;
}

export async function register({ put }) {
  const dir = new URL('../../tests/replay/traces/', import.meta.url);
  const files = readdirSync(dir).filter((f) => f.endsWith('.json')).sort();
  const traces = [];
  for (const f of files) {
    const trace = JSON.parse(readFileSync(new URL(f, dir), 'utf8'));
    const seed = seedFor(trace.mission, trace.difficulty);
    if (trace.seed !== seed) throw new Error(`${trace.id} seed ${trace.seed} != ${seed}`);
    const checkpoints = trace.expect.checkpoints;
    let portrait = true;
    for (const cp of checkpoints) {
      const snap = cp[2];
      if (snap[snap.length - 1] !== PORTRAIT) portrait = false;
      const json = JSON.stringify(snap);
      if (fnv(json) !== cp[1]) throw new Error(`${trace.id} checkpoint ${cp[0]} FNV mismatch`);
    }
    const picks = [0, Math.floor(checkpoints.length / 2), checkpoints.length - 1];
    traces.push({
      kind: 'trace', id: trace.id, mission: trace.mission, difficulty: trace.difficulty,
      seed: trace.seed, frameMs: trace.frameMs, frames: trace.expect.frames, portrait,
      result: trace.expect.result, phases: trace.expect.phases, events: trace.expect.events,
      checkpointCount: checkpoints.length,
      masks: masksOf(trace.input, trace.expect.frames),
      fnv: picks.map((i) => {
        const cp = checkpoints[i];
        return { frame: cp[0], hash: cp[1], json: JSON.stringify(cp[2]) };
      }),
    });
  }
  const landings = [
    { kind: 'deck', recovered: true, vy: 1, dx: 2, tilt: 1, landingVy: 6, landingTol: 36 },
    { kind: 'deck', recovered: true, vy: 4, dx: 20, tilt: 6, landingVy: 6, landingTol: 36 },
    { kind: 'deck', recovered: true, vy: 10, dx: 40, tilt: 20, landingVy: 6, landingTol: 36 },
    { kind: 'deck', recovered: false, vy: 0.1, dx: 0.1, tilt: 0, landingVy: 6, landingTol: 36 },
    { kind: 'water', recovered: true, vy: 0, dx: 0, tilt: 0, landingVy: 6, landingTol: 36 },
    { kind: 'deck', recovered: true, vy: 0, dx: 0, tilt: 0, landingVy: 0, landingTol: 0 },
    { kind: 'rud', recovered: true, vy: 0.2, dx: 1, tilt: 1, landingVy: 6, landingTol: 36 },
  ].map((t) => ({ kind: 'grade', touchdown: t, medal: gradeLanding(t) }));
  put('d0-replay', {
    rel: 0, abs: 0,
    note: 'Portrait 430x932, seedFor, input masks, and FNV-1a of JSON.stringify(snap). Matter body state is not re-simulated.',
  }, [...traces, ...landings]);
}
