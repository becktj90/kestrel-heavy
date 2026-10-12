// Haven touchdown grades. landing-tune.js is the reference.
import { readFileSync } from 'node:fs';
import { LANDING, gradeTouchdown, landingMargins } from '../../web/js/landing-tune.js';
import { DIFFICULTY } from '../../web/js/config.js';

export async function register({ put, REL }) {
  const disk = JSON.parse(readFileSync(new URL('../../data/landing.json', import.meta.url), 'utf8'));
  if (JSON.stringify(disk) !== JSON.stringify(LANDING)) throw new Error('data/landing.json drifted from landing-tune.js');
  const cases = [{
    fn: 'padRat',
    out: { padRatLandingVy: LANDING.padRatLandingVy, difficulty: DIFFICULTY.PAD_RAT.landingVy },
  }];
  const rows = [
    ['water', 'water', 1, 0, 6, 40, false],
    ['perfect', 'deck', 3, 20, 6, 40, false],
    ['good', 'deck', 4.2, 28, 6, 40, false],
    ['hard', 'deck', 5.7, 38, 6, 40, false],
    ['over-sink', 'deck', 6.5, 4, 6, 40, false],
    ['over-offset', 'deck', 1, 50, 6, 40, false],
    ['tipped', 'deck', 1, 2, 6, 40, true],
    ['zero-limits', 'deck', 0, 0, 0, 0, false],
    ['negative-vy', 'deck', -4.2, -28, 6, 40, false],
    ['edge-perfect', 'deck', 6 * (1 - LANDING.perfectMin), 0, 6, 40, false],
    ['edge-good', 'deck', 6 * (1 - LANDING.goodMin), 40 * (1 - LANDING.goodMin), 6, 40, false],
    ['rud', 'rud', 0.2, 1, 6, 36, false],
  ];
  for (const [name, kind, vy, dx, landingVy, landingTol, tipped] of rows) {
    const touch = { kind, vy, dx, landingVy, landingTol, tipped };
    const margins = landingMargins(vy, dx, landingVy, landingTol);
    cases.push({
      fn: 'grade', name, ...touch,
      out: { ...margins, grade: gradeTouchdown(touch) },
    });
  }
  put('landing-grade', { ...REL, note: 'margins closed form; grade string exact; null grade is ungraded' }, cases);
}
