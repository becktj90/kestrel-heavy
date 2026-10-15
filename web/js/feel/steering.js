// Separation, seek and wall avoidance for the debris cloud and any later guide.

export function seek(x, y, vx, vy, tx, ty, maxSpeed) {
  const dx = tx - x;
  const dy = ty - y;
  const d = Math.hypot(dx, dy);
  if (d < 1e-6) return { ax: -vx, ay: -vy };
  const sp = maxSpeed > 0 ? maxSpeed : 0;
  return { ax: (dx / d) * sp - vx, ay: (dy / d) * sp - vy };
}

export function arrive(x, y, vx, vy, tx, ty, maxSpeed, slowRadius) {
  const dx = tx - x;
  const dy = ty - y;
  const d = Math.hypot(dx, dy);
  if (d < 1e-6) return { ax: -vx, ay: -vy };
  let sp = maxSpeed > 0 ? maxSpeed : 0;
  if (slowRadius > 1e-6 && d < slowRadius) sp *= d / slowRadius;
  return { ax: (dx / d) * sp - vx, ay: (dy / d) * sp - vy };
}

export function separate(x, y, radius, shards, neighbors, self) {
  let ax = 0;
  let ay = 0;
  let n = 0;
  const reach = Math.max(0.1, radius) * 4;
  for (let k = 0; k < neighbors.length; k++) {
    const j = neighbors[k];
    if (j === self || j < 0 || j >= shards.length || !shards[j].alive) continue;
    const dx = x - shards[j].x;
    const dy = y - shards[j].y;
    const d2 = dx * dx + dy * dy;
    const lim = reach + shards[j].radius;
    if (d2 > lim * lim || d2 < 1e-8) continue;
    const d = Math.sqrt(d2);
    ax += dx / d / d;
    ay += dy / d / d;
    n += 1;
  }
  if (n > 0) { ax /= n; ay /= n; }
  const mag = Math.hypot(ax, ay);
  if (mag > 40) { ax *= 40 / mag; ay *= 40 / mag; }
  return { ax, ay };
}

export function avoidWalls(x, halfWidth, margin) {
  if (!(margin > 0) || !(halfWidth > 0)) return 0;
  let ax = 0;
  const left = x + halfWidth;
  const right = halfWidth - x;
  if (left < margin) ax += (margin - left) / margin;
  if (right < margin) ax -= (margin - right) / margin;
  return ax;
}

export function blend(ax, ay, bx, by, wa, wb) {
  const w = wa + wb;
  if (!(w > 1e-8)) return { ox: 0, oy: 0 };
  return { ox: (ax * wa + bx * wb) / w, oy: (ay * wa + by * wb) / w };
}
