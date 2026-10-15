// Fixed-pool breakup. Same seed, same cloud as DebrisField in the Unity core.
import { createGrid } from './spatial.js';
import { separate } from './steering.js';

export function hash01(n) {
  let h = (n + 0x9e3779b9) >>> 0;
  h = Math.imul(h ^ (h >>> 16), 0x7feb352d) >>> 0;
  h = Math.imul(h ^ (h >>> 15), 0x846ca68b) >>> 0;
  h = (h ^ (h >>> 16)) >>> 0;
  return h / 4294967296;
}

function blank(count) {
  const shards = [];
  for (let i = 0; i < count; i++) {
    shards.push({ x: 0, y: 0, vx: 0, vy: 0, spin: 0, angle: 0, radius: 0.5, life: 0, mass: 1, alive: false });
  }
  return shards;
}

export function createDebris(capacity, cell) {
  const n = capacity > 0 ? capacity : 1;
  const shards = blank(n);
  const grid = createGrid(cell);
  const query = [];
  let live = 0;

  function rebuild() {
    grid.clear();
    for (let i = 0; i < shards.length; i++) {
      if (shards[i].alive) grid.insert(i, shards[i].x, shards[i].y);
    }
  }

  function resolve() {
    for (let i = 0; i < shards.length; i++) {
      if (!shards[i].alive) continue;
      grid.query(shards[i].x, shards[i].y, shards[i].radius * 2.2, query);
      for (let k = 0; k < query.length; k++) {
        const j = query[k];
        if (j <= i || !shards[j].alive) continue;
        const dx = shards[j].x - shards[i].x;
        const dy = shards[j].y - shards[i].y;
        const min = shards[i].radius + shards[j].radius;
        const d2 = dx * dx + dy * dy;
        if (d2 >= min * min || d2 < 1e-8) continue;
        const d = Math.sqrt(d2);
        const nx = dx / d;
        const ny = dy / d;
        const pen = min - d;
        const im = 1 / Math.max(0.05, shards[i].mass);
        const jm = 1 / Math.max(0.05, shards[j].mass);
        const inv = im + jm;
        shards[i].x -= nx * pen * (im / inv);
        shards[i].y -= ny * pen * (im / inv);
        shards[j].x += nx * pen * (jm / inv);
        shards[j].y += ny * pen * (jm / inv);
        const rel = (shards[j].vx - shards[i].vx) * nx + (shards[j].vy - shards[i].vy) * ny;
        if (rel < 0) {
          const jimp = -1.15 * rel / inv;
          shards[i].vx -= jimp * nx * im;
          shards[i].vy -= jimp * ny * im;
          shards[j].vx += jimp * nx * jm;
          shards[j].vy += jimp * ny * jm;
        }
      }
    }
  }

  return {
    shards,
    get live() { return live; },
    clear() {
      for (let i = 0; i < shards.length; i++) shards[i].alive = false;
      live = 0;
      grid.clear();
    },
    burst(x, y, vx, vy, count, energy, inherit, seed) {
      const want = count > 0 ? count : 0;
      const take = inherit > 0 ? inherit : 0;
      const e = Number.isFinite(energy) && energy > 0 ? energy : 0;
      let spawned = 0;
      for (let i = 0; i < shards.length && spawned < want; i++) {
        if (shards[i].alive) continue;
        const a = hash01(seed + spawned * 17) * Math.PI * 2;
        const u = 0.35 + 0.65 * hash01(seed + spawned * 31);
        const speed = Math.sqrt(e) * u;
        const radius = 0.35 + hash01(seed + spawned * 7) * 1.1;
        const jitter = 0.25 + radius * 0.35;
        const s = shards[i];
        s.x = x + Math.cos(a) * jitter;
        s.y = y + Math.sin(a) * jitter;
        s.vx = vx * take + Math.cos(a) * speed;
        s.vy = vy * take + Math.sin(a) * speed;
        s.spin = (hash01(seed + spawned * 13) - 0.5) * 8;
        s.angle = hash01(seed + spawned * 19) * Math.PI * 2;
        s.radius = radius;
        s.life = 4.5 + hash01(seed + spawned * 3) * 2.5;
        s.mass = 0.4 + hash01(seed + spawned * 11);
        s.alive = true;
        spawned += 1;
      }
      live += spawned;
      return spawned;
    },
    step(dt, gravity, drag) {
      let step = Number.isFinite(dt) && dt > 0 ? dt : 0;
      if (step > 0.05) step = 0.05;
      const g = Number.isFinite(gravity) ? gravity : 0;
      const dmp = Number.isFinite(drag) && drag > 0 ? drag : 0;
      rebuild();
      for (let i = 0; i < shards.length; i++) {
        const s = shards[i];
        if (!s.alive) continue;
        grid.query(s.x, s.y, s.radius * 4, query);
        const sep = separate(s.x, s.y, s.radius, shards, query, i);
        const dragK = dmp * s.radius / Math.max(0.2, s.mass);
        s.vx += (sep.ax - s.vx * dragK) * step;
        s.vy += (sep.ay - g - s.vy * dragK) * step;
        s.x += s.vx * step;
        s.y += s.vy * step;
        s.angle += s.spin * step;
        s.life -= step;
        if (s.life <= 0) s.alive = false;
      }
      rebuild();
      resolve();
      live = 0;
      for (let i = 0; i < shards.length; i++) if (shards[i].alive) live += 1;
    },
  };
}
