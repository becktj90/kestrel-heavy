// Params registry, sensor noise, and weather-hold policy.
import { readFileSync } from 'node:fs';
import { PARAMS } from '../../web/js/params/registry.js';
import { fidelity, sense } from '../../web/js/flight/sensors.js';
import { weatherHold } from '../../web/js/flight/weather.js';

export async function register({ put, REL }) {
  const disk = JSON.parse(readFileSync(new URL('../../data/params.json', import.meta.url), 'utf8'));
  if (JSON.stringify(disk) !== JSON.stringify(PARAMS)) {
    throw new Error('data/params.json drifted from web/js/params/registry.js');
  }
  const truth = { altitude: 12000, speed: 320.5, angleDeg: 45, mass: 500000, propellant: 400000 };
  const fidelities = [];
  for (const difficulty of ['KID', 'CADET', 'PAD_RAT']) {
    for (const sandbox of [false, true]) {
      for (const layer of ['simple', 'standard', 'engineer']) {
        fidelities.push({ difficulty, sandbox, layer, mode: fidelity({ difficulty, sandbox, layer }) });
      }
    }
  }
  const senses = [1, 1000, 0, 0x6d2b79f5].map((seed) => ({
    seed, out: sense(truth, { difficulty: 'CADET', layer: 'standard', seed }),
  }));
  const exact = sense(truth, { difficulty: 'KID', layer: 'standard', seed: 1 });
  const weather = [
    { difficulty: 'KID', windMs: 40, out: weatherHold({ difficulty: 'KID', windMs: 40 }) },
    { difficulty: 'CADET', relaxed: true, windMs: 40, out: weatherHold({ difficulty: 'CADET', relaxed: true, windMs: 40 }) },
    { difficulty: 'CADET', windMs: 12, out: weatherHold({ difficulty: 'CADET', windMs: 12 }) },
    { difficulty: 'CADET', windMs: 12.1, out: weatherHold({ difficulty: 'CADET', windMs: 12.1 }) },
    { difficulty: 'PAD_RAT', windMs: 0, out: weatherHold({ difficulty: 'PAD_RAT', windMs: 0 }) },
  ];
  put('params', { ...REL, note: 'registry matches data/params.json; sensor draws are mulberry32; weather holds are the policy only' }, [
    { kind: 'registry', out: PARAMS },
    { kind: 'fidelity', cases: fidelities },
    { kind: 'sense', truth, exact, cases: senses },
    { kind: 'weather', cases: weather },
  ]);
}
