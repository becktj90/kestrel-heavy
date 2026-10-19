// Launch time of day: FNV-1a 64 (UTF-8 id bytes, then 8 LE seed bytes) and the pick through
// data/time-of-day.json. JS reference: web/js/flight/launch-time.js. C# replays it in
// LaunchTimeGoldenTests; tests/launch-time.test.cjs replays it in JS. Seeds and hashes are
// strings (decimal seed, 16-digit hex hash) because they do not fit a JSON double.
import { readFileSync } from 'node:fs';
import { fnv1a64, hash, hex, parseTable, pick } from '../../web/js/flight/launch-time.js';

const TEXTS = ['', 'a', 'foobar', 'é', 'KH-é', 'KH-🚀'];
const IDS = [...Array.from({ length: 20 }, (_, i) => `KH-${i + 1}`), 'KH-99', '', 'KH-é', 'KH-🚀'];
const SEEDS = [0n, 1n, 7n, 42n, 999983n, (1n << 53n) - 1n, 1n << 63n, (1n << 64n) - 1n];

export async function register({ put }) {
  const table = parseTable(JSON.parse(readFileSync(new URL('../../data/time-of-day.json', import.meta.url), 'utf8')));
  put('launch-time', { rel: 0, abs: 0, note: 'exact: hash hex and pick name; table is data/time-of-day.json' }, [
    ...TEXTS.map((text) => ({ fn: 'fnv1a64', in: { text }, out: { hash: hex(fnv1a64(text)) } })),
    ...IDS.flatMap((id) => SEEDS.map((seed) => ({
      fn: 'pick', in: { id, seed: seed.toString() }, out: { hash: hex(hash(id, seed)), pick: pick(table, id, seed) },
    }))),
  ]);
}
