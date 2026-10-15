// Uniform grid broadphase. Debris and steering query a few cells instead of every shard.
// The cell lists are pooled so a flight does not allocate a new list per insert.
using System;
using System.Collections.Generic;

namespace Kestrel.Core.Feel
{
    public sealed class SpatialGrid
    {
        public readonly double Cell;
        readonly Dictionary<long, List<int>> cells = new Dictionary<long, List<int>>();
        readonly Stack<List<int>> pool = new Stack<List<int>>();

        public SpatialGrid(double cell) { Cell = cell > 1e-6 ? cell : 1; }

        public int Occupied => cells.Count;

        public void Clear()
        {
            foreach (var kv in cells)
            {
                kv.Value.Clear();
                pool.Push(kv.Value);
            }
            cells.Clear();
        }

        static long Key(int ix, int iy) => ((long)ix << 32) ^ (uint)iy;

        public void Insert(int id, double x, double y)
        {
            int ix = (int)Math.Floor(x / Cell);
            int iy = (int)Math.Floor(y / Cell);
            long key = Key(ix, iy);
            if (!cells.TryGetValue(key, out var list))
            {
                list = pool.Count > 0 ? pool.Pop() : new List<int>(4);
                cells[key] = list;
            }
            list.Add(id);
        }

        /// <summary>Ids whose cell overlaps the circle. The caller owns <paramref name="hits"/>; it is cleared first.</summary>
        public void Query(double x, double y, double radius, List<int> hits)
        {
            hits.Clear();
            if (radius < 0) radius = 0;
            int x0 = (int)Math.Floor((x - radius) / Cell);
            int x1 = (int)Math.Floor((x + radius) / Cell);
            int y0 = (int)Math.Floor((y - radius) / Cell);
            int y1 = (int)Math.Floor((y + radius) / Cell);
            for (int iy = y0; iy <= y1; iy++)
            {
                for (int ix = x0; ix <= x1; ix++)
                {
                    if (!cells.TryGetValue(Key(ix, iy), out var list)) continue;
                    for (int i = 0; i < list.Count; i++) hits.Add(list[i]);
                }
            }
        }
    }
}
