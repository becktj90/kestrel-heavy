// Fixed pool of breakup shards. Positions are pad metres. A uniform grid finds overlaps;
// steering keeps the cloud from stacking on one point, then a positional solve separates
// anything that still intersects. The same seed always makes the same cloud.
using System;
using System.Collections.Generic;

namespace Kestrel.Core.Feel
{
    public struct Shard
    {
        public double X, Y, Vx, Vy, Spin, Angle, Radius, Life, Mass;
        public bool Alive;
    }

    public sealed class DebrisField
    {
        public readonly Shard[] Shards;
        public readonly SpatialGrid Grid;
        public int Live;
        readonly List<int> query = new List<int>(16);

        public DebrisField(int capacity, double cell)
        {
            if (capacity < 1) capacity = 1;
            Shards = new Shard[capacity];
            Grid = new SpatialGrid(cell);
        }

        public void Clear()
        {
            for (int i = 0; i < Shards.Length; i++) Shards[i].Alive = false;
            Live = 0;
            Grid.Clear();
        }

        /// <summary>Deterministic unit hash. The same n is the same value in the JS twin.</summary>
        public static double Hash01(int n)
        {
            unchecked
            {
                uint h = (uint)n + 0x9e3779b9u;
                h = (h ^ (h >> 16)) * 0x7feb352du;
                h = (h ^ (h >> 15)) * 0x846ca68bu;
                h ^= h >> 16;
                return h / 4294967296.0;
            }
        }

        /// <summary>
        /// Spawn up to <paramref name="count"/> shards around (x, y). Inherited velocity is scaled by
        /// <paramref name="inherit"/> so the cloud stays near the vehicle the camera is chasing.
        /// </summary>
        public int Burst(double x, double y, double vx, double vy, int count, double energy, double inherit, int seed)
        {
            if (count < 0) count = 0;
            if (inherit < 0) inherit = 0;
            if (double.IsNaN(energy) || energy < 0) energy = 0;
            int spawned = 0;
            for (int i = 0; i < Shards.Length && spawned < count; i++)
            {
                if (Shards[i].Alive) continue;
                double a = Hash01(seed + spawned * 17) * Math.PI * 2;
                double u = 0.35 + 0.65 * Hash01(seed + spawned * 31);
                double speed = Math.Sqrt(energy) * u;
                double radius = 0.35 + Hash01(seed + spawned * 7) * 1.1;
                double jitter = 0.25 + radius * 0.35;
                Shards[i] = new Shard
                {
                    X = x + Math.Cos(a) * jitter,
                    Y = y + Math.Sin(a) * jitter,
                    Vx = vx * inherit + Math.Cos(a) * speed,
                    Vy = vy * inherit + Math.Sin(a) * speed,
                    Spin = (Hash01(seed + spawned * 13) - 0.5) * 8,
                    Angle = Hash01(seed + spawned * 19) * Math.PI * 2,
                    Radius = radius,
                    Life = 4.5 + Hash01(seed + spawned * 3) * 2.5,
                    Mass = 0.4 + Hash01(seed + spawned * 11),
                    Alive = true,
                };
                spawned++;
            }
            Live += spawned;
            return spawned;
        }

        public void Step(double dt, double gravity, double drag)
        {
            if (double.IsNaN(dt) || dt < 0) dt = 0;
            if (dt > 0.05) dt = 0.05;
            if (double.IsNaN(gravity)) gravity = 0;
            if (double.IsNaN(drag) || drag < 0) drag = 0;
            Rebuild();
            for (int i = 0; i < Shards.Length; i++)
            {
                if (!Shards[i].Alive) continue;
                var s = Shards[i];
                Grid.Query(s.X, s.Y, s.Radius * 4, query);
                Steering.Separate(s.X, s.Y, s.Radius, Shards, query, i, out double sx, out double sy);
                double dragK = drag * s.Radius / Math.Max(0.2, s.Mass);
                s.Vx += (sx - s.Vx * dragK) * dt;
                s.Vy += (sy - gravity - s.Vy * dragK) * dt;
                s.X += s.Vx * dt;
                s.Y += s.Vy * dt;
                s.Angle += s.Spin * dt;
                s.Life -= dt;
                if (s.Life <= 0) s.Alive = false;
                Shards[i] = s;
            }
            Rebuild();
            Resolve();
            Live = 0;
            for (int i = 0; i < Shards.Length; i++) if (Shards[i].Alive) Live++;
        }

        void Rebuild()
        {
            Grid.Clear();
            for (int i = 0; i < Shards.Length; i++)
                if (Shards[i].Alive) Grid.Insert(i, Shards[i].X, Shards[i].Y);
        }

        void Resolve()
        {
            for (int i = 0; i < Shards.Length; i++)
            {
                if (!Shards[i].Alive) continue;
                Grid.Query(Shards[i].X, Shards[i].Y, Shards[i].Radius * 2.2, query);
                for (int k = 0; k < query.Count; k++)
                {
                    int j = query[k];
                    if (j <= i || !Shards[j].Alive) continue;
                    double dx = Shards[j].X - Shards[i].X;
                    double dy = Shards[j].Y - Shards[i].Y;
                    double min = Shards[i].Radius + Shards[j].Radius;
                    double d2 = dx * dx + dy * dy;
                    if (d2 >= min * min || d2 < 1e-8) continue;
                    double d = Math.Sqrt(d2);
                    double nx = dx / d, ny = dy / d;
                    double pen = min - d;
                    double im = 1 / Math.Max(0.05, Shards[i].Mass);
                    double jm = 1 / Math.Max(0.05, Shards[j].Mass);
                    double inv = im + jm;
                    var a = Shards[i];
                    var b = Shards[j];
                    a.X -= nx * pen * (im / inv);
                    a.Y -= ny * pen * (im / inv);
                    b.X += nx * pen * (jm / inv);
                    b.Y += ny * pen * (jm / inv);
                    double rel = (b.Vx - a.Vx) * nx + (b.Vy - a.Vy) * ny;
                    if (rel < 0)
                    {
                        double jimp = -1.15 * rel / inv;
                        a.Vx -= jimp * nx * im;
                        a.Vy -= jimp * ny * im;
                        b.Vx += jimp * nx * jm;
                        b.Vy += jimp * ny * jm;
                    }
                    Shards[i] = a;
                    Shards[j] = b;
                }
            }
        }
    }
}
