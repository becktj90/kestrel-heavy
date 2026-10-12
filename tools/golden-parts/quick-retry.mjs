// One-tap retry clocks. data/retry.json must match PACE.ENTRY and PACE.MECO - sepLeadSec.
import { readFileSync } from 'node:fs';
import { PACE } from '../../web/js/config.js';

export async function register({ put }) {
  const disk = JSON.parse(readFileSync(new URL('../../data/retry.json', import.meta.url), 'utf8'));
  const landing = PACE.ENTRY;
  const sep = PACE.MECO - disk.sepLeadSec;
  if (disk.points.landing.tClock !== landing) throw new Error(`landing tClock ${disk.points.landing.tClock} != PACE.ENTRY ${landing}`);
  if (disk.points.sep.tClock !== sep) throw new Error(`sep tClock ${disk.points.sep.tClock} != PACE.MECO - lead ${sep}`);
  if (disk.points.sep.altitudeKm !== 54) throw new Error('sep resume altitude must stay 54 km');
  if (disk.taps !== 1 || disk.confirm !== false || disk.resultsToRelaunchSec !== 2) throw new Error('retry budget drifted');
  const kept = { taps: disk.taps, confirm: disk.confirm, resultsToRelaunchSec: disk.resultsToRelaunchSec, preserveUnlocks: true, preserveBests: true };
  const plan = (point) => {
    const p = disk.points[point];
    return { ...kept, point, phase: p.phase, tClock: p.tClock, altitudeKm: p.altitudeKm ?? null };
  };
  put('quick-retry', { rel: 0, abs: 0, note: 'exact clocks; a non-SUMMARY status or an unknown point is a no-op' }, [
    { fn: 'plan', point: 'landing', status: 'SUMMARY', out: plan('landing') },
    { fn: 'plan', point: 'sep', status: 'SUMMARY', out: plan('sep') },
    { fn: 'plan', point: 'landing', status: 'PLAY', out: null },
    { fn: 'plan', point: 'sep', status: 'MENU', out: null },
    { fn: 'plan', point: 'ascent', status: 'SUMMARY', out: null },
  ]);
}
