// Uniform grid. Query touches the cells under a circle instead of every body.

export function createGrid(cell) {
  const size = cell > 1e-6 ? cell : 1;
  const cells = new Map();
  const key = (ix, iy) => `${ix},${iy}`;
  return {
    cell: size,
    clear() { cells.clear(); },
    get occupied() { return cells.size; },
    insert(id, x, y) {
      const ix = Math.floor(x / size);
      const iy = Math.floor(y / size);
      const k = key(ix, iy);
      let list = cells.get(k);
      if (!list) { list = []; cells.set(k, list); }
      list.push(id);
    },
    query(x, y, radius, hits) {
      hits.length = 0;
      const r = radius > 0 ? radius : 0;
      const x0 = Math.floor((x - r) / size);
      const x1 = Math.floor((x + r) / size);
      const y0 = Math.floor((y - r) / size);
      const y1 = Math.floor((y + r) / size);
      for (let iy = y0; iy <= y1; iy++) {
        for (let ix = x0; ix <= x1; ix++) {
          const list = cells.get(key(ix, iy));
          if (!list) continue;
          for (let i = 0; i < list.length; i++) hits.push(list[i]);
        }
      }
      return hits;
    },
  };
}
