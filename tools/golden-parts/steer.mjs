// Expo thumb-steer. JS input.js is the reference; numbers live in data/steer.json.
import { readFileSync } from 'node:fs';
import { STEER } from '../../web/js/steer-tune.js';
import { applySteerCurve, clampSteerSensitivity, peekTouchSteer, steerFullPx } from '../../web/js/input.js';

export async function register({ put, REL }) {
  const disk = JSON.parse(readFileSync(new URL('../../data/steer.json', import.meta.url), 'utf8'));
  if (JSON.stringify(disk) !== JSON.stringify(STEER)) throw new Error('data/steer.json drifted from steer-tune.js');
  const cases = [];
  for (const viewportWidth of [0, 375, 390, 391, 800]) {
    cases.push({ fn: 'fullPx', viewportWidth, out: steerFullPx(viewportWidth) });
  }
  for (const sensitivity of [0, 0.6, 1, 1.6, 9]) {
    cases.push({ fn: 'clamp', sensitivity, out: clampSteerSensitivity(sensitivity) });
  }
  const samples = [-200, -110, -72, -55, -14, -13, 0, 13, 14, 40, 55, 72, 110, 150];
  for (const dx of samples) {
    for (const sensitivity of [0.6, 1, 1.6]) {
      for (const viewportWidth of [375, 800]) {
        const opts = { sensitivity, viewportWidth };
        cases.push({ fn: 'curve', dx, sensitivity, viewportWidth, out: applySteerCurve(dx, opts) });
        cases.push({ fn: 'peek', dx, sensitivity, viewportWidth, armed: false, out: peekTouchSteer(dx, false, opts) });
        cases.push({ fn: 'peek', dx, sensitivity, viewportWidth, armed: true, out: peekTouchSteer(dx, true, opts) });
      }
    }
  }
  put('steer', { ...REL, note: 'expo curve and sensitivity clamp; physics forces unchanged' }, cases);
}
