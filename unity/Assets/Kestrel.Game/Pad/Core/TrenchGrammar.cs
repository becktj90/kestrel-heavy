// Flame trench, deflector and deck-opening layout (docs/PAD_DETAIL_GRAMMAR.md §6).
// Ranges are seeded. Bearings are jittered off the service-tower axis. Nothing here is a real pad:
// no complex numbers, no agency marks, no copied trench azimuth.
using System;
using System.Collections.Generic;
using Kestrel.Kit.Contracts;

namespace Kestrel.Pad
{
    public enum DeflectorKind { Wedge, Ramp }

    /// <summary>One open end of the trench. XZ is pad space; Dir is the outward horizontal unit.</summary>
    public struct TrenchMouth
    {
        public float X, Y, Z;
        public float DirX, DirZ;
        public float Width, Height;
    }

    /// <summary>Seeded trench + the deck-detail parameters that share its stream. Engine-free.</summary>
    public sealed class TrenchSpec
    {
        public int Seed;
        public PadArchetype Archetype;
        public DeflectorKind Deflector;
        /// <summary>Trench axis bearing, degrees. 0 = +Z (toward the tower), 90 = +X.</summary>
        public float BearingDeg;
        public bool TwoSided;
        public float ClusterDiameter;
        /// <summary>Width / cluster after the strongback/tower clearance clamp. Grammar band is 2.5–4.</summary>
        public float WidthFactor;
        public float Width;
        public float Depth;
        public float FloorY;
        public float FaceAngleDeg;
        public float ApexY;
        public float WedgeRun;
        public float ExitLength;
        public float FlareDeg;
        public int PondCount;
        public float PondWidth, PondLength;
        public bool SpareDeflector;
        public float PlateSize;
        public float GratingWidth;
        public int DrainCount;
        public bool ClearanceClamped;
        /// <summary>Deck-slot half-width cap so the opening stays inboard of the strongback. The channel itself may be wider; the slab bridges the rest.</summary>
        public float SlotHalfCap;
        /// <summary>Matches Kestrel.Slice.PadGround.GroundY. The exit rises to this grade.</summary>
        public const float GradeY = -1f;

        public float HalfWidth(float u) =>
            Width * 0.5f + Math.Abs(u) * (float)Math.Tan(FlareDeg * (Math.PI / 180.0));

        /// <summary>Channel floor. Deep under the deck, rising to grade at the open end. Shallow rail trenches stay above grade.</summary>
        public float FloorAt(float u)
        {
            float deep = FloorY;
            float abs = Math.Abs(u);
            if (deep >= GradeY - 0.05f) return deep;
            float rampStart = Math.Max(WedgeRun + 1.5f, ExitLength * 0.62f);
            if (abs <= rampStart || ExitLength <= rampStart + 0.5f) return deep;
            float t = (abs - rampStart) / (ExitLength - rampStart);
            if (t < 0f) t = 0f; if (t > 1f) t = 1f;
            return deep + (GradeY - deep) * t;
        }

        /// <summary>Wall top. Flush with the deck inside the disc; a low parapet once the channel is out in the field.</summary>
        public float TopAt(float u, float deckRadius)
        {
            float floor = FloorAt(u);
            if (Math.Abs(u) <= Math.Max(2f, deckRadius) - 0.4f) return 0f;
            float parapet = floor + 1.35f;
            if (floor < GradeY) parapet = Math.Max(parapet, GradeY + 0.22f);
            return Math.Min(0f, parapet);
        }

        /// <summary>Deflector face height. Apex under the cluster, toe on the deep floor.</summary>
        public float SlopeY(float u)
        {
            float along = TwoSided ? Math.Abs(u) : Math.Max(0f, u);
            float t = WedgeRun <= 1e-3f ? 1f : along / WedgeRun;
            if (t < 0f) t = 0f; if (t > 1f) t = 1f;
            return ApexY + (FloorY - ApexY) * t;
        }

        public void ToWorld(float u, float v, out float x, out float z)
        {
            float b = BearingDeg * (float)(Math.PI / 180.0);
            float s = (float)Math.Sin(b), c = (float)Math.Cos(b);
            x = u * s + v * c;
            z = u * c - v * s;
        }

        public void ToFrame(float x, float z, out float u, out float v)
        {
            float b = BearingDeg * (float)(Math.PI / 180.0);
            float s = (float)Math.Sin(b), c = (float)Math.Cos(b);
            u = x * s + z * c;
            v = x * c - z * s;
        }
    }

    public static class TrenchGrammar
    {
        public const float KeepOutDeg = 25f;
        public const float GradeY = TrenchSpec.GradeY;
        /// <summary>Same constants as Kestrel.Slice.PadGround so the slab matches the old disc.</summary>
        public const float MinSurfaceRadius = 15f, SurfaceMargin = 2f;

        /// <summary>Own RNG stream so tower/rail/strongback sampling in PadGenerator is unchanged.</summary>
        public static TrenchSpec Sample(int seed, PadArchetype type, PadFit fit, float deckRadius)
        {
            var rng = new PadRng(seed ^ unchecked((int)0x51F0E7A3));
            float h = Math.Max(8f, fit.VehicleHeight);
            float deck = Math.Max(8f, deckRadius);
            // One Range() per field for every archetype, so seeds stay comparable.
            float bearingT = rng.Range(0f, 1f);
            float clusterT = rng.Range(0.55f, 0.95f);
            float factorT = rng.Range(0f, 1f);
            float depthT = rng.Range(0f, 1f);
            float angleT = rng.Range(0f, 1f);
            float exitT = rng.Range(0f, 1f);
            float flareT = rng.Range(0f, 1f);
            float pondT = rng.Range(0f, 1f);
            float pondW = rng.Range(0.30f, 0.60f);
            float pondL = rng.Range(0.55f, 1.05f);
            float spareT = rng.Range(0f, 1f);
            float plate = rng.Range(3.2f, 4.8f);
            int drains = rng.Range(2, 5);
            float grating = rng.Range(1.2f, 2.0f);

            bool heavy = type == PadArchetype.TowerStrongback;
            bool rail = type == PadArchetype.Rail;
            // Tower/heavy exits run out the sides, clear of the +Z tower and the -Z strongback.
            float bearing = rail ? bearingT * 180f : 84f + bearingT * 12f;
            float minF = heavy ? 3.4f : 2.5f;
            float maxF = heavy ? 4f : rail ? 3.2f : 3.5f;
            float factor = minF + (maxF - minF) * factorT;
            float cluster = Math.Max(1.2f, fit.VehicleRadius * clusterT);
            // Both tower pads must clear the +Z tower face. The strongback is close on -Z; a wide heavy
            // channel runs under it and the deck slab bridges that footprint (see SlotHalfCap).
            float limitV = rail ? 1e6f : fit.VehicleRadius + Math.Max(1f, fit.Clearance) - 1.4f;
            float maxWidth = Math.Max(2.4f, 2f * limitV);
            bool clamped = false;
            if (factor * cluster > maxWidth)
            {
                clamped = true;
                if (maxWidth / factor >= 1.2f) cluster = maxWidth / factor;
                else { cluster = 1.2f; factor = Math.Max(2f, maxWidth / cluster); }
            }
            float width = factor * cluster;
            float depth = rail ? Math.Max(0.55f, h * (0.04f + 0.04f * depthT)) : h * (0.12f + 0.13f * depthT);
            float angle = 30f + 15f * angleT;
            float exit = h * (rail ? 1f + 0.6f * exitT : 1f + 1.5f * exitT);
            float flare = 10f + 10f * flareT;
            float floorY = -depth;
            float apexY = Math.Min(-0.18f, Math.Max(floorY + 0.35f, -0.42f));
            float rise = apexY - floorY;
            if (rise < 0.25f) { apexY = floorY + 0.25f; rise = 0.25f; }
            float run = rise / (float)Math.Tan(angle * Math.PI / 180.0);
            float maxRun = exit * 0.42f;
            if (run > maxRun && maxRun > 0.3f)
            {
                run = maxRun;
                angle = (float)(Math.Atan(rise / run) * 180.0 / Math.PI);
            }
            int ponds = rail || pondT < 0.5f ? 1 : 2;
            var spec = new TrenchSpec
            {
                Seed = seed, Archetype = type,
                Deflector = rail ? DeflectorKind.Ramp : DeflectorKind.Wedge,
                BearingDeg = bearing, TwoSided = !rail,
                ClusterDiameter = cluster, WidthFactor = width / cluster, Width = width,
                Depth = depth, FloorY = floorY, FaceAngleDeg = angle, ApexY = apexY, WedgeRun = run,
                ExitLength = exit, FlareDeg = flare,
                PondCount = ponds, PondWidth = h * pondW, PondLength = h * pondW * pondL,
                SpareDeflector = !rail && spareT < 0.3f,
                PlateSize = plate, GratingWidth = grating, DrainCount = drains,
                ClearanceClamped = clamped,
                SlotHalfCap = heavy ? fit.VehicleRadius + 0.7f : 1e6f,
            };
            FitClearance(spec, fit, type, deck);
            return spec;
        }

        /// <summary>Pull flare, then width, until the channel lip misses the +Z tower face.
        /// Flare is what walks the lip into the tower at the deck edge, so it goes first.</summary>
        static void FitClearance(TrenchSpec s, PadFit fit, PadArchetype type, float deck)
        {
            if (type == PadArchetype.Rail) return;
            for (int n = 0; n < 16 && !ClearsStructures(s, fit, type, deck) && s.FlareDeg > 10.05f; n++)
            {
                s.ClearanceClamped = true;
                s.FlareDeg = Math.Max(10f, s.FlareDeg - 1.25f);
            }
            for (int n = 0; n < 24 && !ClearsStructures(s, fit, type, deck); n++)
            {
                s.ClearanceClamped = true;
                s.Width = Math.Max(2.4f, s.Width * 0.9f);
                s.WidthFactor = s.Width / Math.Max(0.5f, s.ClusterDiameter);
            }
        }

        public static bool ClearsStructures(TrenchSpec s, PadFit fit, PadArchetype type, float deck)
        {
            if (s == null || type == PadArchetype.Rail) return true;
            float nearTower = fit.VehicleRadius + fit.Clearance - 0.45f;
            float uMax = Math.Max(2f, deck);
            for (int i = 0; i <= 14; i++)
            {
                float u = -uMax + 2f * uMax * i / 14f;
                if (!s.TwoSided && u < 0f) continue;
                float hw = s.HalfWidth(u);
                for (int k = -1; k <= 1; k += 2)
                {
                    s.ToWorld(u, k * hw, out float x, out float z);
                    if (z > nearTower && Math.Abs(x) < uMax) return false;
                }
            }
            return true;
        }

        /// <summary>±25° sector around each exit bearing, out to 3 × vehicle height, measured from the pad centre.</summary>
        public static bool InPlumeSector(TrenchSpec s, float x, float z, float vehicleHeight)
        {
            float dist = (float)Math.Sqrt(x * x + z * z);
            if (dist < 1f || dist > 3f * Math.Max(8f, vehicleHeight)) return false;
            float ang = (float)(Math.Atan2(x, z) * 180.0 / Math.PI);
            foreach (var bearing in ExitBearings(s))
                if (Math.Abs(Delta(ang, bearing)) <= KeepOutDeg) return true;
            return false;
        }

        public static IEnumerable<float> ExitBearings(TrenchSpec s)
        {
            yield return s.BearingDeg;
            if (s.TwoSided) yield return s.BearingDeg + 180f;
        }

        static float Delta(float a, float b)
        {
            float d = (a - b) % 360f;
            if (d > 180f) d -= 360f;
            if (d < -180f) d += 360f;
            return d;
        }

        public static float SurfaceRadius(PadLayout L)
        {
            float r = MinSurfaceRadius;
            if (L == null) return r;
            if (L.DeckRadius > r) r = L.DeckRadius;
            foreach (var m in L.Members)
            {
                if (Math.Min(m.A.Y, m.B.Y) > 0.5f) continue;
                float ra = (float)Math.Sqrt(m.A.X * m.A.X + m.A.Z * m.A.Z) + SurfaceMargin;
                float rb = (float)Math.Sqrt(m.B.X * m.B.X + m.B.Z * m.B.Z) + SurfaceMargin;
                if (ra > r) r = ra;
                if (rb > r) r = rb;
            }
            return r;
        }

        public static List<TrenchMouth> Mouths(TrenchSpec s, float deckRadius = 0f)
        {
            var list = new List<TrenchMouth>(2);
            AddMouth(list, s, s.ExitLength, deckRadius);
            if (s.TwoSided) AddMouth(list, s, -s.ExitLength, deckRadius);
            return list;
        }

        static void AddMouth(List<TrenchMouth> list, TrenchSpec s, float u, float deckRadius)
        {
            float hw = s.HalfWidth(u);
            s.ToWorld(u, 0f, out float x, out float z);
            float sign = u >= 0f ? 1f : -1f;
            s.ToWorld(sign, 0f, out float dx, out float dz);
            float top = s.TopAt(u, deckRadius > 1f ? deckRadius : s.ExitLength);
            float floor = s.FloorAt(u);
            list.Add(new TrenchMouth
            {
                X = x, Y = (top + floor) * 0.5f, Z = z,
                DirX = dx, DirZ = dz,
                Width = hw * 2f, Height = Math.Max(0.6f, top - floor),
            });
        }

        /// <summary>Grade opening of the whole channel, expanded so the parapet is inside the ground cut.</summary>
        public static List<Vec2> GroundOpening(TrenchSpec s)
        {
            float u0 = s.TwoSided ? -s.ExitLength : 0f;
            // Uncapped channel. The deck slot is narrower on a heavy pad; the grade hole is the real trench.
            var raw = Outline(s, u0, s.ExitLength, false);
            return Expand(raw, 0.7f);
        }

        /// <summary>Slot in the deck slab. Strictly inside the disc so the lid triangulation stays simple.</summary>
        public static List<Vec2> DeckOpening(TrenchSpec s, float deckRadius)
        {
            float uMax = Math.Max(2f, deckRadius);
            for (int i = 0; i < 10; i++)
            {
                float hw = s.HalfWidth(uMax);
                float rad = (float)Math.Sqrt(uMax * uMax + hw * hw);
                if (rad < deckRadius - 0.55f && uMax > s.WedgeRun + 0.8f) break;
                uMax *= 0.9f;
            }
            float u0 = s.TwoSided ? -uMax : 0f;
            var pts = Outline(s, u0, uMax, true);
            EnsureInside(pts, deckRadius - 0.35f);
            return pts;
        }

        public static List<List<Vec2>> PondPolys(TrenchSpec s)
        {
            var list = new List<List<Vec2>>();
            var opening = Outline(s, s.TwoSided ? -s.ExitLength : 0f, s.ExitLength, false);
            for (int i = 0; i < s.PondCount; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                float u = s.ExitLength;
                if (s.TwoSided && i == 1) u = -s.ExitLength;
                float hw = s.HalfWidth(u);
                float v = side * (hw + s.PondWidth * 0.5f + 3f);
                var poly = Rect(s, u, v, s.PondLength * 0.5f, s.PondWidth * 0.5f);
                // Nudge outward until the basin sits beside the channel, not in it.
                for (int n = 0; n < 6 && Overlaps(opening, poly); n++)
                {
                    v += side * (s.PondWidth * 0.35f + 1f);
                    poly = Rect(s, u, v, s.PondLength * 0.5f, s.PondWidth * 0.5f);
                }
                list.Add(poly);
            }
            return list;
        }

        static List<Vec2> Rect(TrenchSpec s, float u, float v, float hu, float hv)
        {
            var p = new List<Vec2>(4);
            void A(float du, float dv) { s.ToWorld(u + du, v + dv, out float x, out float z); p.Add(new Vec2(x, z)); }
            A(-hu, -hv); A(hu, -hv); A(hu, hv); A(-hu, hv);
            return p;
        }

        static bool Overlaps(List<Vec2> a, List<Vec2> b)
        {
            foreach (var p in b) if (PolyCut.Contains(a, p.X, p.Y)) return true;
            foreach (var p in a) if (PolyCut.Contains(b, p.X, p.Y)) return true;
            return false;
        }

        /// <summary>Hexagon (or trapezoid) of the channel edges. includeWaist keeps the narrow centre, which makes the rail concave.</summary>
        public static List<Vec2> Outline(TrenchSpec s, float u0, float u1, bool includeWaist)
        {
            var pts = new List<Vec2>(6);
            void Add(float u, float v) { s.ToWorld(u, v, out float x, out float z); pts.Add(new Vec2(x, z)); }
            bool waist = includeWaist && u0 < -0.05f && u1 > 0.05f;
            float Cap(float u) => Math.Min(s.HalfWidth(u), s.SlotHalfCap > 0.5f ? s.SlotHalfCap : s.HalfWidth(u));
            // Deck slots pass includeWaist and take SlotHalfCap. Grade holes and pond tests pass false and keep the flared channel.
            float Hw(float u) => includeWaist ? Cap(u) : s.HalfWidth(u);
            Add(u1, Hw(u1));
            Add(u1, -Hw(u1));
            if (waist) Add(0f, -Hw(0f));
            Add(u0, -Hw(u0));
            Add(u0, Hw(u0));
            if (waist) Add(0f, Hw(0f));
            return pts;
        }

        static List<Vec2> Expand(List<Vec2> raw, float dist)
        {
            float cx = 0f, cy = 0f;
            foreach (var p in raw) { cx += p.X; cy += p.Y; }
            cx /= raw.Count; cy /= raw.Count;
            var o = new List<Vec2>(raw.Count);
            foreach (var p in raw)
            {
                float dx = p.X - cx, dy = p.Y - cy;
                float len = (float)Math.Sqrt(dx * dx + dy * dy);
                if (len < 1e-4f) o.Add(p);
                else o.Add(new Vec2(p.X + dx / len * dist, p.Y + dy / len * dist));
            }
            return o;
        }

        static void EnsureInside(List<Vec2> pts, float radius)
        {
            for (int i = 0; i < pts.Count; i++)
            {
                float r = (float)Math.Sqrt(pts[i].X * pts[i].X + pts[i].Y * pts[i].Y);
                if (r > radius && r > 1e-4f)
                {
                    float k = radius / r;
                    pts[i] = new Vec2(pts[i].X * k, pts[i].Y * k);
                }
            }
        }

        public static void Register(PadLayout L)
        {
            var s = L.Trench; if (s == null) return;
            var deck = L.Graph.Add("pad.deck", ElementCategory.Deck, TowerLayout.SiteId, "deck", "concrete");
            deck.DisplayName = "Pad deck";
            deck.Center = new Vec3(0, 0, 0);
            deck.Size = new Vec3(L.DeckRadius * 2f, 0.4f, L.DeckRadius * 2f);
            var plates = L.Graph.Add("pad.deck.plates", ElementCategory.Deck, "pad.deck", "plate", "concrete");
            plates.DisplayName = "Deck plates"; plates.Center = deck.Center; plates.Size = deck.Size;
            var grat = L.Graph.Add("pad.deck.grating", ElementCategory.Deck, "pad.deck", "grating", "galvanized");
            grat.DisplayName = "Grating walkway"; grat.Center = deck.Center; grat.Size = new Vec3(s.Width + 6f, 0.2f, L.DeckRadius * 2f);
            var drains = L.Graph.Add("pad.deck.drains", ElementCategory.Deck, "pad.deck", "drain");
            drains.DisplayName = "Deck drains"; drains.Center = deck.Center; drains.Size = deck.Size;
            var marks = L.Graph.Add("pad.deck.markings", ElementCategory.Deck, "pad.deck", "marking");
            marks.DisplayName = "Edge markings"; marks.Center = deck.Center; marks.Size = new Vec3(L.DeckRadius * 2f, 0.1f, L.DeckRadius * 2f);

            var trench = L.Graph.Add("pad.trench", ElementCategory.Structure, TowerLayout.SiteId, "trench", "concrete", "flame-trench");
            trench.DisplayName = "Flame trench";
            trench.Center = new Vec3(0, s.FloorY * 0.5f, 0);
            trench.Size = new Vec3(s.ExitLength * 2f + 4f, Math.Abs(s.FloorY) + 1.5f, s.ExitLength * 2f + 4f);
            var def = L.Graph.Add("pad.trench.deflector", ElementCategory.Structure, "pad.trench", "deflector", "refractory");
            def.DisplayName = "Flame deflector";
            def.Center = new Vec3(0, (s.ApexY + s.FloorY) * 0.5f, 0);
            def.Size = new Vec3(s.WedgeRun * 2f + 1f, Math.Max(0.4f, s.ApexY - s.FloorY), s.Width + 1f);
            for (int i = 0; i < s.PondCount; i++)
            {
                var pond = L.Graph.Add($"pad.trench.pond.{i}", ElementCategory.Structure, "pad.trench", "pond", "concrete");
                pond.DisplayName = "Catch basin";
                pond.Center = new Vec3(0, GradeY, 0);
                pond.Size = new Vec3(s.PondLength, 1.5f, s.PondWidth);
            }
            if (s.SpareDeflector)
            {
                var spare = L.Graph.Add("pad.trench.spare", ElementCategory.Structure, "pad.trench", "spare", "deflector");
                spare.DisplayName = "Spare deflector";
                spare.Center = new Vec3(0, GradeY + 0.6f, 0);
                spare.Size = new Vec3(6f, 1.5f, 3f);
            }
        }
    }
}
