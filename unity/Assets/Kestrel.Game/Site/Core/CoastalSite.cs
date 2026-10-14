// Seeded Pier 7 plan: apron, access road, scrub clumps, distant silhouette buildings, and a Haven keep-out.
// The coast shape does not depend on the seed — every seed still puts the pad on the breakwater.
using System;
using System.Collections.Generic;

namespace Kestrel.Site
{
    public struct SeaReserve
    {
        public string Name;
        public float CenterX, CenterZ, HalfX, HalfZ;
        public bool Contains(float x, float z) =>
            Math.Abs(x - CenterX) <= HalfX && Math.Abs(z - CenterZ) <= HalfZ;
        public float Area => 4f * HalfX * HalfZ;
    }

    public struct SiteBox
    {
        public float X, Z, W, D, H;
        public float Distance => (float)Math.Sqrt((double)X * X + (double)Z * Z);
    }

    public struct ScrubClump
    {
        public float X, Z, Height, Radius;
    }

    /// <summary>A small prop merged into the coast mesh. Kind: 0 buoy, 1 boat, 2 gull, 3 rock, 4 foam, 5 apron seam.</summary>
    public struct SiteProp
    {
        public float X, Z, W, H, D;
        public byte Kind;
    }

    public sealed class CoastalSite
    {
        public const int Version = 1;
        public int Seed;
        public SeaReserve Haven;
        public float[] RoadX = Array.Empty<float>(), RoadZ = Array.Empty<float>();
        public readonly List<SiteBox> Buildings = new List<SiteBox>();
        public readonly List<ScrubClump> Clumps = new List<ScrubClump>();
        public readonly List<SiteProp> Props = new List<SiteProp>();

        SiteMesh[] _land = new SiteMesh[3], _water = new SiteMesh[3];

        public SiteMesh Land(int lod) => _land[lod] ??= SiteBuild.Land(this, lod);
        public SiteMesh Water(int lod) => _water[lod] ??= SiteBuild.Water(this, lod);

        public bool OnRoad(float x, float z)
        {
            float half = Coastline.RoadHalfWidth;
            for (int i = 0; i < RoadX.Length - 1; i++)
            {
                if (DistToSeg(x, z, RoadX[i], RoadZ[i], RoadX[i + 1], RoadZ[i + 1]) <= half) return true;
            }
            return false;
        }

        public static float DistToSeg(float px, float pz, float ax, float az, float bx, float bz)
        {
            float abx = bx - ax, abz = bz - az, apx = px - ax, apz = pz - az;
            float ab2 = abx * abx + abz * abz;
            float t = ab2 < 1e-6f ? 0f : Coastline.Saturate((apx * abx + apz * abz) / ab2);
            float dx = ax + abx * t - px, dz = az + abz * t - pz;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        public ulong Hash()
        {
            ulong h = 1469598103934665603UL;
            void F(float v) { h ^= (ulong)BitConverter.SingleToInt32Bits(v) & 0xffffffffUL; h *= 1099511628211UL; }
            F(Haven.CenterX); F(Haven.CenterZ); F(Haven.HalfX); F(Haven.HalfZ);
            F(Coastline.Relief(1400f, 600f, Seed));
            for (int i = 0; i < RoadX.Length; i += 3) { F(RoadX[i]); F(RoadZ[i]); }
            foreach (var b in Buildings) { F(b.X); F(b.Z); F(b.H); }
            var land = Land(1);
            for (int i = 0; i < land.VertexCount; i += 19) F(land.Positions[i * 3]);
            return h;
        }
    }

    public static class SiteGenerator
    {
        public const int Pier7Seed = 7;
        public const string PlaceName = "Pier 7";

        public static CoastalSite Generate(int seed)
        {
            var s = new CoastalSite { Seed = seed };
            BuildRoad(s);
            if (!TryPlaceHaven(out s.Haven)) throw new InvalidOperationException("Pier 7 has no clear water for Haven");
            PlaceBuildings(s, seed);
            PlaceClumps(s, seed);
            PlaceProps(s, seed);
            return s;
        }

        static void BuildRoad(CoastalSite s)
        {
            float[] zs = { -4800f, -2600f, -1200f, -320f, 0f };
            var pts = new List<(float x, float z)>();
            for (int i = 0; i < zs.Length; i++)
            {
                float z = zs[i];
                float x = i == zs.Length - 1 ? -Coastline.ApronRadius : Coastline.ShoreX(z) - 1400f;
                pts.Add((x, z));
            }
            var xs = new List<float>(); var zz = new List<float>();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                float ax = pts[i].x, az = pts[i].z, bx = pts[i + 1].x, bz = pts[i + 1].z;
                float len = (float)Math.Sqrt((bx - ax) * (bx - ax) + (bz - az) * (bz - az));
                int n = Math.Max(1, (int)Math.Ceiling(len / 40f));
                for (int k = 0; k < n; k++)
                {
                    float t = k / (float)n;
                    float x = ax + (bx - ax) * t, z = az + (bz - az) * t;
                    PushInland(ref x, z, 36f);
                    xs.Add(x); zz.Add(z);
                }
            }
            xs.Add(-Coastline.ApronRadius); zz.Add(0f);
            s.RoadX = xs.ToArray(); s.RoadZ = zz.ToArray();
        }

        static void PushInland(ref float x, float z, float margin)
        {
            float shore = Coastline.ShoreX(z);
            if (x > shore - margin) x = shore - margin;
        }

        public static bool TryPlaceHaven(out SeaReserve best)
        {
            best = default; float score = float.MaxValue; bool found = false;
            for (float z = -5000f; z <= 7000f; z += 500f)
            for (float x = 2200f; x <= 12000f; x += 500f)
            {
                var r = new SeaReserve { Name = "Haven", CenterX = x, CenterZ = z, HalfX = 420f, HalfZ = 190f };
                if (!HavenClear(r)) continue;
                float dx = x - 4800f, dz = z - 400f, s2 = dx * dx + dz * dz;
                if (s2 < score) { score = s2; best = r; found = true; }
            }
            return found;
        }

        static bool HavenClear(SeaReserve r)
        {
            for (int iz = -2; iz <= 2; iz++)
            for (int ix = -2; ix <= 2; ix++)
            {
                float x = r.CenterX + ix * r.HalfX * 0.5f, z = r.CenterZ + iz * r.HalfZ * 0.5f;
                if (x < Coastline.ShoreX(z) + 700f) return false;
            }
            return true;
        }

        static void PlaceBuildings(CoastalSite s, int seed)
        {
            var rng = new SiteRng(seed * 17 + 3);
            float[] cx = { -1460f, -1880f };
            float[] cz = { 640f, -420f };
            for (int c = 0; c < cx.Length; c++)
            for (int i = 0; i < 5; i++)
            {
                float x = cx[c] + rng.Range(-70f, 70f), z = cz[c] + rng.Range(-55f, 55f);
                for (int n = 0; n < 14 && !BuildingOk(s, x, z); n++) x -= 90f;
                if (!BuildingOk(s, x, z)) continue;
                s.Buildings.Add(new SiteBox
                {
                    X = x, Z = z,
                    W = rng.Range(12f, 34f), D = rng.Range(10f, 26f), H = rng.Range(7f, 22f),
                });
            }
        }

        static bool BuildingOk(CoastalSite s, float x, float z)
        {
            if (!Coastline.IsLand(x, z) || Coastline.IsBeach(x, z) || Coastline.IsApron(x, z)) return false;
            if (s.OnRoad(x, z) || s.Haven.Contains(x, z)) return false;
            float d = (float)Math.Sqrt(x * x + z * z);
            return d > 1000f && d < 4200f;
        }

        static void PlaceClumps(CoastalSite s, int seed)
        {
            for (float z = -1500f; z <= 1500f && s.Clumps.Count < 36; z += 120f)
            for (float x = -1800f; x <= 200f && s.Clumps.Count < 36; x += 120f)
            {
                if (Coastline.Hash01((int)x, (int)z, seed) < 0.62f) continue;
                float jx = x + (Coastline.Hash01((int)x + 5, (int)z, seed) - 0.5f) * 36f;
                float jz = z + (Coastline.Hash01((int)x, (int)z + 9, seed) - 0.5f) * 36f;
                if (!ClumpOk(s, jx, jz)) continue;
                float h = Coastline.Hash01((int)jx, (int)jz + 2, seed + 1);
                s.Clumps.Add(new ScrubClump
                {
                    X = jx, Z = jz,
                    Height = 0.45f + h * 0.7f,
                    Radius = 0.8f + h * 0.9f,
                });
            }
            s.Clumps.Sort((a, b) => (a.X * a.X + a.Z * a.Z).CompareTo(b.X * b.X + b.Z * b.Z));
        }

        static void PlaceProps(CoastalSite s, int seed)
        {
            // Buoys and one work boat just off the beach, clear of Haven and of the shoreline samples the tests poke.
            for (int i = 0; i < 8; i++)
            {
                float z = 180f + i * 95f;
                float x = Coastline.ShoreX(z) + 55f + (i % 3) * 28f + (seed & 3) * 0.25f;
                if (s.Haven.Contains(x, z) || !Coastline.IsWater(x, z)) continue;
                s.Props.Add(new SiteProp { X = x, Z = z, W = 1.6f, H = 2.4f, D = 1.6f, Kind = 0 });
            }
            float bz = 640f;
            float bx = Coastline.ShoreX(bz) + 90f;
            if (Coastline.IsWater(bx, bz) && !s.Haven.Contains(bx, bz))
                s.Props.Add(new SiteProp { X = bx, Z = bz, W = 16f, H = 3.2f, D = 5f, Kind = 1 });
            for (int i = 0; i < 10; i++)
            {
                float z = -900f + i * 160f;
                float x = Coastline.ShoreX(z) - 40f - (i % 4) * 12f;
                if (!Coastline.IsBeach(x, z)) continue;
                s.Props.Add(new SiteProp { X = x, Z = z, W = 1.4f + (i % 3) * 0.6f, H = 0.7f, D = 1.1f, Kind = 3 });
                s.Props.Add(new SiteProp { X = x - 8f, Z = z + 6f, W = 2.2f, H = 0.35f, D = 0.15f, Kind = 2 });
            }
            for (int i = 0; i < 12; i++)
            {
                float z = -700f + i * 110f;
                float x = Coastline.ShoreX(z) + 8f;
                if (!Coastline.IsWater(x, z) || s.Haven.Contains(x, z)) continue;
                s.Props.Add(new SiteProp { X = x, Z = z, W = 6f, H = 0.12f, D = 1.4f, Kind = 4 });
            }
            // Apron plate seams, off the origin so the pad centre sample stays the ground.
            for (int i = 0; i < 6; i++)
            {
                float a = i * 1.05f;
                float r = 28f + (i % 2) * 18f;
                s.Props.Add(new SiteProp
                {
                    X = (float)Math.Cos(a) * r, Z = (float)Math.Sin(a) * r,
                    W = 7f, H = 0.04f, D = 0.35f, Kind = 5,
                });
            }
        }

        static bool ClumpOk(CoastalSite s, float x, float z)
        {
            if (!Coastline.IsLand(x, z) || Coastline.IsBeach(x, z) || Coastline.IsApron(x, z)) return false;
            if (s.OnRoad(x, z) || s.Haven.Contains(x, z)) return false;
            float d = (float)Math.Sqrt(x * x + z * z);
            return d > 100f && d < 1700f;
        }

        public static int ClumpCount(int lod) => lod <= 0 ? 36 : lod == 1 ? 18 : 8;
    }

    public struct SiteRng
    {
        uint _s;
        public SiteRng(int seed) { _s = (uint)seed * 2654435761u ^ 0x9E3779B9u; if (_s == 0) _s = 1; Next(); }
        public uint Next() { _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5; return _s; }
        public float Range(float a, float b) => a + (b - a) * (Next() / 4294967296f);
    }
}
