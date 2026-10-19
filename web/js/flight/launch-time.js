/**
 * Launch time of day: Day, Sunset or Night for one flight.
 * BigInt port of Kestrel.Core.Flight.LaunchTime (unity/Assets/Kestrel.Core/Flight/LaunchTime.cs).
 * FNV-1a 64 over the UTF-8 bytes of the mission id, then the seed as eight little-endian bytes.
 * slot = hash % (day + sunset + night), walked Day, Sunset, Night. A `fixed` row ignores the seed.
 * data/golden/launch-time.json (tools/golden-parts/launch-time.mjs) is the shared golden both
 * suites replay: tests/launch-time.test.cjs here and LaunchTimeGoldenTests in Kestrel.Core.Tests.
 * Pure: the caller passes the parsed data/time-of-day.json, so nothing reads the file system.
 */
export const FNV_OFFSET = 14695981039346656037n;
export const FNV_PRIME = 1099511628211n;
const MASK = (1n << 64n) - 1n;
export const NAMES = ['Day', 'Sunset', 'Night'];

const enc = new TextEncoder();

/** FNV-1a 64 over bytes, continuing from h. */
function mixBytes(h, bytes) {
  for (const b of bytes) h = ((h ^ BigInt(b)) * FNV_PRIME) & MASK;
  return h;
}

/** Seeds are unsigned 64-bit. Numbers must be safe integers; BigInts wrap to 64 bits. */
export function toSeed(seed) {
  if (typeof seed === 'bigint') return BigInt.asUintN(64, seed);
  if (!Number.isSafeInteger(seed) || seed < 0) throw new RangeError('seed must be a non-negative safe integer or a BigInt');
  return BigInt(seed);
}

/** Plain FNV-1a 64 of the UTF-8 bytes of text (null is ""). Lone surrogates encode as U+FFFD. */
export function fnv1a64(text) {
  return mixBytes(FNV_OFFSET, enc.encode(text ?? ''));
}

/** Hash used for the pick: FNV-1a 64 of the id's UTF-8 bytes, then the seed's 8 LE bytes. */
export function hash(missionId, seed) {
  let h = fnv1a64(missionId);
  const s = toSeed(seed);
  for (let i = 0n; i < 8n; i++) h = ((h ^ ((s >> (8n * i)) & 0xffn)) * FNV_PRIME) & MASK;
  return h;
}

export const hex = (h) => h.toString(16).padStart(16, '0');

function weight(row, key, where) {
  const v = row[key];
  if (v === undefined || v === null) return 0;
  if (typeof v !== 'number' || !Number.isInteger(v) || v < 0 || v > 1000000) {
    throw new Error(`${where}.${key} must be a whole number from 0 to 1000000`);
  }
  return v;
}

function parseName(text) {
  const i = NAMES.findIndex((n) => n.toLowerCase() === String(text).toLowerCase());
  return i < 0 ? null : NAMES[i];
}

/** Validate the parsed data/time-of-day.json, same rules as LaunchTimeTable.Parse. */
export function parseTable(json) {
  if (json.version !== undefined && json.version !== 1) throw new Error('time-of-day version must be 1');
  if (!json.default) throw new Error('time-of-day default is missing');
  const w = (row, where) => ({ day: weight(row, 'day', where), sunset: weight(row, 'sunset', where), night: weight(row, 'night', where) });
  const fallback = w(json.default, 'default');
  if (fallback.day + fallback.sunset + fallback.night <= 0) throw new Error('time-of-day default weights must sum to more than 0');
  const fixed = new Map();
  const weights = new Map();
  for (const [id, row] of Object.entries(json.missions || {})) {
    if (!row || typeof row !== 'object') throw new Error(`time-of-day mission ${id} is not an object`);
    if ('fixed' in row) {
      const pin = parseName(row.fixed);
      if (!pin) throw new Error(`time-of-day ${id} fixed must be Day, Sunset, or Night`);
      fixed.set(id, pin);
    }
    const r = w(row, id);
    if (r.day + r.sunset + r.night > 0) weights.set(id, r);
  }
  return { fallback, fixed, weights };
}

/** 'Day' | 'Sunset' | 'Night' for this id and seed. */
export function pick(table, missionId, seed) {
  const key = missionId ?? '';
  const pin = table.fixed.get(key);
  if (pin) return pin;
  const w = table.weights.get(key) || table.fallback;
  const slot = hash(missionId, seed) % BigInt(w.day + w.sunset + w.night);
  if (slot < BigInt(w.day)) return 'Day';
  if (slot < BigInt(w.day + w.sunset)) return 'Sunset';
  return 'Night';
}
