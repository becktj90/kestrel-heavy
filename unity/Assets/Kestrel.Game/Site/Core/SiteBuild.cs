// Meshes for Pier 7. Near the pad: a regular grid (scrub, beach, apron). Everywhere else: strips that follow
// the shore out past the horizon, with Earth curvature on the vertices. Road, scrub cards and building
// silhouettes are merged into the land mesh so the whole site is two draw calls.
using System;
using System.Collections.Generic;

namespace Kestrel.Site
{
    public static class SiteBuild
    {
        static readonly float[] Inland = { 0f, 140f, 500f, 1600f, 5000f, 16000f, 50000f, 140000f, 400000f, 1000000f, 1700000f };
        static readonly float[] Seaward = { 0f, 45f, 140f, 400f, 1200f, 4000f, 14000f, 45000f, 120000f, 320000f, 750000f, 1700000f };

        public static SiteMesh Land(CoastalSite site, int lod)
        {
            var d = new Draft();
            int n = GridN(lod);
            float cell = 2f * Coastline.NearExtent / n;
            var land = new int[n + 1, n + 1];
            for (int iz = 0; iz <= n; iz++)
            for (int ix = 0; ix <= n; ix++)
            {
                float x = -Coastline.NearExtent + ix * cell, z = -Coastline.NearExtent + iz * cell;
                land[ix, iz] = Coastline.IsWater(x, z) ? -1 : d.Vert(site, x, z, 0.04f, false);
            }
            for (int iz = 0; iz < n; iz++)
            for (int ix = 0; ix < n; ix++)
            {
                int a = land[ix, iz], b = land[ix + 1, iz], c = land[ix + 1, iz + 1], e = land[ix, iz + 1];
                if (a < 0 || b < 0 || c < 0 || e < 0) { ClipCell(d, site, ix, iz, cell, false); continue; }
                d.TriUp(a, b, c); d.TriUp(a, c, e);
            }
            Strips(d, site, lod, true);
            Road(d, site);
            Buildings(d, site);
            Clumps(d, site, lod);
            return d.Finish();
        }

        public static SiteMesh Water(CoastalSite site, int lod)
        {
            var d = new Draft();
            int n = GridN(lod);
            float cell = 2f * Coastline.NearExtent / n;
            var wet = new int[n + 1, n + 1];
            for (int iz = 0; iz <= n; iz++)
            for (int ix = 0; ix <= n; ix++)
            {
                float x = -Coastline.NearExtent + ix * cell, z = -Coastline.NearExtent + iz * cell;
                wet[ix, iz] = Coastline.IsLand(x, z) ? -1 : d.VertWater(x, z, -0.05f);
            }
            for (int iz = 0; iz < n; iz++)
            for (int ix = 0; ix < n; ix++)
            {
                int a = wet[ix, iz], b = wet[ix + 1, iz], c = wet[ix + 1, iz + 1], e = wet[ix, iz + 1];
                if (a < 0 || b < 0 || c < 0 || e < 0) { ClipCell(d, site, ix, iz, cell, true); continue; }
                d.TriUp(a, b, c); d.TriUp(a, c, e);
            }
            Strips(d, site, lod, false);
            return d.Finish();
        }

        static int GridN(int lod) => lod <= 0 ? 96 : lod == 1 ? 64 : 56;

        static void ClipCell(Draft d, CoastalSite site, int ix, int iz, float cell, bool water)
        {
            float x0 = -Coastline.NearExtent + ix * cell, z0 = -Coastline.NearExtent + iz * cell;
            float[] xs = { x0, x0 + cell, x0 + cell, x0 };
            float[] zs = { z0, z0, z0 + cell, z0 + cell };
            int wet = 0;
            for (int i = 0; i < 4; i++) if (Coastline.IsWater(xs[i], zs[i])) wet++;
            if (water && wet == 0) return;
            if (!water && wet == 4) return;
            for (int i = 0; i < 4; i++)
            {
                bool w = Coastline.IsWater(xs[i], zs[i]);
                if (water && !w) xs[i] = Coastline.ShoreX(zs[i]);
                if (!water && w) xs[i] = Coastline.ShoreX(zs[i]);
            }
            int i0 = water ? d.VertWater(xs[0], zs[0], -0.05f) : d.Vert(site, xs[0], zs[0], 0.04f, false);
            int i1 = water ? d.VertWater(xs[1], zs[1], -0.05f) : d.Vert(site, xs[1], zs[1], 0.04f, false);
            int i2 = water ? d.VertWater(xs[2], zs[2], -0.05f) : d.Vert(site, xs[2], zs[2], 0.04f, false);
            int i3 = water ? d.VertWater(xs[3], zs[3], -0.05f) : d.Vert(site, xs[3], zs[3], 0.04f, false);
            d.TriUp(i0, i1, i2); d.TriUp(i0, i2, i3);
        }

        static void Strips(Draft d, CoastalSite site, int lod, bool land)
        {
            var zs = ZSamples(lod);
            float[] rings = land ? Inland : Seaward;
            int ringStep = lod <= 0 ? 1 : lod == 1 ? 1 : 2;
            var used = new List<int>();
            for (int r = 0; r < rings.Length; r++)
                if (r % ringStep == 0 || r == rings.Length - 1) used.Add(r);
            var idx = new int[zs.Count, used.Count];
            for (int s = 0; s < zs.Count; s++)
            for (int u = 0; u < used.Count; u++)
            {
                float x = Coastline.ShoreX(zs[s]) + (land ? -rings[used[u]] : rings[used[u]]);
                if (!land && x > SiteHorizon.WaterRadius) x = SiteHorizon.WaterRadius;
                if (land && x < -SiteHorizon.WaterRadius) x = -SiteHorizon.WaterRadius;
                idx[s, u] = land ? d.Vert(site, x, zs[s], 0f, false) : d.VertWater(x, zs[s], -0.12f);
            }
            for (int s = 0; s < zs.Count - 1; s++)
            for (int u = 0; u < used.Count - 1; u++)
            {
                int a = idx[s, u], b = idx[s + 1, u], c = idx[s + 1, u + 1], e = idx[s, u + 1];
                if (AllDeep(d, a, b, c, e)) continue;
                d.TriUp(a, b, c); d.TriUp(a, c, e);
            }
        }

        static bool AllDeep(Draft d, int a, int b, int c, int e) =>
            d.Deep(a) && d.Deep(b) && d.Deep(c) && d.Deep(e);

        static List<float> ZSamples(int lod)
        {
            var z = new List<float>();
            float limit = 1200000f, zc = -limit;
            z.Add(zc);
            while (zc < limit - 1f)
            {
                float az = Math.Abs(zc);
                float step = az < 600f ? 20f : az < 4000f ? 80f : az < 20000f ? 400f : az < 80000f ? 1600f : az < 250000f ? 5000f : 14000f;
                if (lod == 1) step *= 1.25f;
                if (lod >= 2) step *= 1.7f;
                if (az < 600f) step = Math.Min(step, 24f);
                float next = Math.Min(limit, zc + step);
                if (next <= zc) break;
                zc = next; z.Add(zc);
            }
            return z;
        }

        static void Road(Draft d, CoastalSite s)
        {
            int n = s.RoadX.Length; if (n < 2) return;
            var left = new int[n]; var right = new int[n];
            for (int i = 0; i < n; i++)
            {
                float x = s.RoadX[i], z = s.RoadZ[i];
                float dx, dz;
                if (i < n - 1) { dx = s.RoadX[i + 1] - x; dz = s.RoadZ[i + 1] - z; }
                else { dx = x - s.RoadX[i - 1]; dz = z - s.RoadZ[i - 1]; }
                float len = (float)Math.Sqrt(dx * dx + dz * dz); if (len < 1e-4f) len = 1f;
                float px = -dz / len * Coastline.RoadHalfWidth, pz = dx / len * Coastline.RoadHalfWidth;
                left[i] = d.Vert(s, x + px, z + pz, 0.16f, true);
                right[i] = d.Vert(s, x - px, z - pz, 0.16f, true);
            }
            for (int i = 0; i < n - 1; i++) { d.TriUp(left[i], right[i], right[i + 1]); d.TriUp(left[i], right[i + 1], left[i + 1]); }
        }

        static void Buildings(Draft d, CoastalSite s)
        {
            foreach (var b in s.Buildings)
            {
                float y0 = Coastline.SurfaceY(b.X, b.Z, s.Seed, 0.06f);
                float hx = b.W * 0.5f, hz = b.D * 0.5f;
                Box(d, b.X - hx, y0, b.Z - hz, b.X + hx, y0 + b.H, b.Z + hz);
            }
        }

        static void Box(Draft d, float x0, float y0, float z0, float x1, float y1, float z1)
        {
            void Face(float ax, float ay, float az, float bx, float by, float bz, float cx, float cy, float cz, float dx, float dy, float dz, float nx, float ny, float nz, bool roof)
            {
                var col = roof ? Coastline.BuildingRoof : Coastline.Building;
                int i0 = d.VertRaw(ax, ay, az, nx, ny, nz, col);
                int i1 = d.VertRaw(bx, by, bz, nx, ny, nz, col);
                int i2 = d.VertRaw(cx, cy, cz, nx, ny, nz, col);
                int i3 = d.VertRaw(dx, dy, dz, nx, ny, nz, col);
                d.TriFacing(i0, i1, i2, nx, ny, nz);
                d.TriFacing(i0, i2, i3, nx, ny, nz);
            }
            Face(x0, y0, z0, x0, y1, z0, x1, y1, z0, x1, y0, z0, 0, 0, -1, false);
            Face(x1, y0, z1, x1, y1, z1, x0, y1, z1, x0, y0, z1, 0, 0, 1, false);
            Face(x0, y0, z1, x0, y1, z1, x0, y1, z0, x0, y0, z0, -1, 0, 0, false);
            Face(x1, y0, z0, x1, y1, z0, x1, y1, z1, x1, y0, z1, 1, 0, 0, false);
            Face(x0, y1, z0, x0, y1, z1, x1, y1, z1, x1, y1, z0, 0, 1, 0, true);
            Face(x0, y0, z1, x0, y0, z0, x1, y0, z0, x1, y0, z1, 0, -1, 0, false);
        }

        static void Clumps(Draft d, CoastalSite s, int lod)
        {
            int n = Math.Min(SiteGenerator.ClumpCount(lod), s.Clumps.Count);
            for (int i = 0; i < n; i++)
            {
                var c = s.Clumps[i];
                float y = Coastline.SurfaceY(c.X, c.Z, s.Seed, 0.05f);
                Card(d, c.X, y, c.Z, c.Radius, c.Height, 0.7f, 0.7f);
                Card(d, c.X, y, c.Z, c.Radius, c.Height, -0.7f, 0.7f);
            }
        }

        static void Card(Draft d, float x, float y, float z, float radius, float height, float dx, float dz)
        {
            float x0 = x - dx * radius, x1 = x + dx * radius, z0 = z - dz * radius, z1 = z + dz * radius;
            int a = d.VertRaw(x0, y, z0, dx, 0, dz, Coastline.Veg);
            int b = d.VertRaw(x0, y + height, z0, dx, 0, dz, Coastline.Veg);
            int c = d.VertRaw(x1, y + height, z1, dx, 0, dz, Coastline.Veg);
            int e = d.VertRaw(x1, y, z1, dx, 0, dz, Coastline.Veg);
            d.TriRaw(a, b, c); d.TriRaw(a, c, e);
            d.TriRaw(a, c, b); d.TriRaw(a, e, c);
        }

        sealed class Draft
        {
            readonly List<float> _p = new List<float>();
            readonly List<float> _n = new List<float>();
            readonly List<float> _c = new List<float>();
            readonly List<float> _acc = new List<float>();
            readonly List<int> _i = new List<int>();

            public int Vert(CoastalSite site, float x, float z, float bias, bool road)
            {
                float y = Coastline.SurfaceY(x, z, site.Seed, bias);
                if (road) y = Coastline.SurfaceY(x, z, site.Seed, 0f) + 0.18f;
                int o = _p.Count;
                _p.Add(x); _p.Add(y); _p.Add(z);
                _n.Add(0); _n.Add(1); _n.Add(0);
                _acc.Add(0); _acc.Add(0); _acc.Add(0);
                _c.Add(0); _c.Add(0); _c.Add(0); _c.Add(1);
                WriteLand(site, x, z, road, o / 3);
                return o / 3;
            }

            void WriteLand(CoastalSite site, float x, float z, bool road, int v)
            {
                var tmp = new float[4];
                Coastline.WriteLandColor(x, z, site.Seed, road || site.OnRoad(x, z), tmp, 0);
                int o = v * 4;
                _c[o] = tmp[0]; _c[o + 1] = tmp[1]; _c[o + 2] = tmp[2]; _c[o + 3] = tmp[3];
            }

            public int VertWater(float x, float z, float bias)
            {
                float y = -0.08f - SiteHorizon.Drop(x, z) + bias;
                int v = _p.Count / 3;
                _p.Add(x); _p.Add(y); _p.Add(z);
                _n.Add(0); _n.Add(1); _n.Add(0);
                _acc.Add(0); _acc.Add(0); _acc.Add(0);
                _c.Add(0); _c.Add(0); _c.Add(0); _c.Add(0);
                var tmp = new float[4];
                Coastline.WriteWaterColor(x, z, tmp, 0);
                int o = v * 4; _c[o] = tmp[0]; _c[o + 1] = tmp[1]; _c[o + 2] = tmp[2]; _c[o + 3] = tmp[3];
                return v;
            }

            public int VertRaw(float x, float y, float z, float nx, float ny, float nz, float[] col)
            {
                int v = _p.Count / 3;
                _p.Add(x); _p.Add(y); _p.Add(z);
                _n.Add(nx); _n.Add(ny); _n.Add(nz);
                _acc.Add(nx); _acc.Add(ny); _acc.Add(nz);
                _c.Add(col[0]); _c.Add(col[1]); _c.Add(col[2]); _c.Add(col[3]);
                return v;
            }

            public bool Deep(int v) => Coastline.DeepInsideNear(_p[v * 3], _p[v * 3 + 2]);

            public void TriUp(int a, int b, int c)
            {
                if (!Facing(a, b, c, out float nx, out float ny, out float nz)) return;
                if (ny < 0f) { int t = b; b = c; c = t; nx = -nx; ny = -ny; nz = -nz; }
                Add(a, b, c, nx, ny, nz);
            }

            public void TriFacing(int a, int b, int c, float ex, float ey, float ez)
            {
                if (!Facing(a, b, c, out float nx, out float ny, out float nz)) return;
                if (nx * ex + ny * ey + nz * ez < 0f) { int t = b; b = c; c = t; nx = -nx; ny = -ny; nz = -nz; }
                Add(a, b, c, nx, ny, nz);
            }

            public void TriRaw(int a, int b, int c) { _i.Add(a); _i.Add(b); _i.Add(c); }

            bool Facing(int a, int b, int c, out float nx, out float ny, out float nz)
            {
                float ax = _p[a * 3], ay = _p[a * 3 + 1], az = _p[a * 3 + 2];
                float bx = _p[b * 3] - ax, by = _p[b * 3 + 1] - ay, bz = _p[b * 3 + 2] - az;
                float cx = _p[c * 3] - ax, cy = _p[c * 3 + 1] - ay, cz = _p[c * 3 + 2] - az;
                nx = by * cz - bz * cy; ny = bz * cx - bx * cz; nz = bx * cy - by * cx;
                return nx * nx + ny * ny + nz * nz > 1e-4f;
            }

            void Add(int a, int b, int c, float nx, float ny, float nz)
            {
                _i.Add(a); _i.Add(b); _i.Add(c);
                Acc(a, nx, ny, nz); Acc(b, nx, ny, nz); Acc(c, nx, ny, nz);
            }

            void Acc(int v, float nx, float ny, float nz)
            {
                _acc[v * 3] += nx; _acc[v * 3 + 1] += ny; _acc[v * 3 + 2] += nz;
            }

            public SiteMesh Finish()
            {
                for (int v = 0; v < _p.Count / 3; v++)
                {
                    float nx = _acc[v * 3], ny = _acc[v * 3 + 1], nz = _acc[v * 3 + 2];
                    float m = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
                    if (m < 1e-6f) continue;
                    _n[v * 3] = nx / m; _n[v * 3 + 1] = ny / m; _n[v * 3 + 2] = nz / m;
                }
                return new SiteMesh { Positions = _p.ToArray(), Normals = _n.ToArray(), Colors = _c.ToArray(), Indices = _i.ToArray() };
            }
        }
    }
}
