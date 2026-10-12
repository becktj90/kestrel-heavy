// Lofted flame trench, wedge/ramp deflector, and deck surface (plates, grating, drains, edge marks).
// CSG-free: stations along the trench axis, quads between them. One mesh per kit material
// (concrete, refractory, galvanized, warning stripes) so the pad spends four draw calls.
using System;
using System.Collections.Generic;
using Kestrel.Kit.Contracts;

namespace Kestrel.Pad
{
    public sealed class PadDetailBuild
    {
        public readonly PadMeshData Concrete = new PadMeshData();
        public readonly PadMeshData Refractory = new PadMeshData();
        public readonly PadMeshData Galvanized = new PadMeshData();
        public readonly PadMeshData Markings = new PadMeshData();

        public int DrawCalls
        {
            get
            {
                int n = 0;
                if (Concrete.TriangleCount > 0) n++;
                if (Refractory.TriangleCount > 0) n++;
                if (Galvanized.TriangleCount > 0) n++;
                if (Markings.TriangleCount > 0) n++;
                return n;
            }
        }

        public int Triangles => Concrete.TriangleCount + Refractory.TriangleCount + Galvanized.TriangleCount + Markings.TriangleCount;
    }

    public static class TrenchMeshBuilder
    {
        public const float StationSpacing = 8f;
        public const float WallThick = 0.42f;
        /// <summary>Plates, grating, drains and edge marks stay inside this radius. Lightning masts can
        /// push the concrete disc past 100 m; a 4 m plate grid out there exceeds the pad triangle budget.</summary>
        public const float DetailRadiusCap = 36f;

        public static PadDetailBuild Build(TrenchSpec spec, float surfaceRadius)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            var b = new PadDetailBuild();
            float deck = Math.Max(8f, surfaceRadius);
            float detail = Math.Min(deck, DetailRadiusCap);
            var us = Stations(spec, deck);
            Channel(b.Concrete, spec, deck, us);
            Deflector(b.Refractory, spec);
            if (!spec.TwoSided) BackWall(b.Concrete, spec, deck);
            Ponds(b.Concrete, spec);
            if (spec.SpareDeflector) Spare(b, spec);
            Slab(b.Concrete, spec, deck);
            Plinth(b.Concrete, spec, deck);
            Plates(b, spec, detail);
            Grating(b.Galvanized, spec, detail);
            Drains(b.Concrete, spec, detail);
            EdgeMarks(b.Markings, spec, detail);
            return b;
        }

        static List<float> Stations(TrenchSpec s, float deck)
        {
            float u0 = s.TwoSided ? -s.ExitLength : 0f;
            float u1 = s.ExitLength;
            int n = Math.Max(2, (int)Math.Ceiling((u1 - u0) / StationSpacing));
            var us = new List<float>(n + 6);
            for (int i = 0; i <= n; i++) us.Add(u0 + (u1 - u0) * i / n);
            us.Add(0f);
            us.Add(s.WedgeRun);
            if (s.TwoSided) us.Add(-s.WedgeRun);
            float cut = CutU(s, deck);
            us.Add(cut);
            if (s.TwoSided) us.Add(-cut);
            us.Sort();
            for (int i = us.Count - 1; i > 0; i--)
                if (Math.Abs(us[i] - us[i - 1]) < 0.05f) us.RemoveAt(i);
            return us;
        }

        static float CutU(TrenchSpec s, float deck)
        {
            float u = Math.Max(2f, deck);
            for (int i = 0; i < 10; i++)
            {
                float hw = s.HalfWidth(u);
                if ((float)Math.Sqrt(u * u + hw * hw) < deck - 0.55f && u > s.WedgeRun + 0.8f) break;
                u *= 0.9f;
            }
            return u;
        }

        struct St
        {
            public float U, BaseY, TopY, Hw;
            public Vec3 IPos, INeg, OPos, ONeg, IPosB, INegB, OPosB, ONegB;
        }

        static St At(TrenchSpec s, float deck, float u)
        {
            var st = new St { U = u, Hw = s.HalfWidth(u) };
            bool onWedge = s.TwoSided ? Math.Abs(u) <= s.WedgeRun + 0.02f : u >= -0.02f && u <= s.WedgeRun + 0.02f;
            st.BaseY = onWedge ? s.SlopeY(u) : s.FloorAt(u);
            st.TopY = Math.Max(s.TopAt(u, deck), st.BaseY + 0.12f);
            st.IPos = P(s, u, st.Hw, st.TopY); st.IPosB = P(s, u, st.Hw, st.BaseY);
            st.INeg = P(s, u, -st.Hw, st.TopY); st.INegB = P(s, u, -st.Hw, st.BaseY);
            st.OPos = P(s, u, st.Hw + WallThick, st.TopY); st.OPosB = P(s, u, st.Hw + WallThick, st.BaseY);
            st.ONeg = P(s, u, -(st.Hw + WallThick), st.TopY); st.ONegB = P(s, u, -(st.Hw + WallThick), st.BaseY);
            return st;
        }

        static Vec3 P(TrenchSpec s, float u, float v, float y)
        {
            s.ToWorld(u, v, out float x, out float z);
            return new Vec3(x, y, z);
        }

        static void Channel(PadMeshData m, TrenchSpec s, float deck, List<float> us)
        {
            s.ToWorld(0f, 1f, out float ax, out float az);
            var prev = At(s, deck, us[0]);
            for (int i = 1; i < us.Count; i++)
            {
                var cur = At(s, deck, us[i]);
                // Inner faces look into the channel. Outer faces look away. Cap only where the slab does not cover it.
                m.AddQuad(prev.IPos, prev.IPosB, cur.IPosB, cur.IPos, -ax, 0f, -az);
                m.AddQuad(prev.INegB, prev.INeg, cur.INeg, cur.INegB, ax, 0f, az);
                m.AddQuad(prev.OPosB, prev.OPos, cur.OPos, cur.OPosB, ax, 0f, az);
                m.AddQuad(prev.ONeg, prev.ONegB, cur.ONegB, cur.ONeg, -ax, 0f, -az);
                if (prev.TopY < -0.02f || cur.TopY < -0.02f)
                {
                    m.AddQuad(prev.IPos, cur.IPos, cur.OPos, prev.OPos, 0f, 1f, 0f);
                    m.AddQuad(prev.ONeg, cur.ONeg, cur.INeg, prev.INeg, 0f, 1f, 0f);
                }
                bool floorPrev = !OnWedge(s, prev.U);
                bool floorCur = !OnWedge(s, cur.U);
                if (floorPrev && floorCur)
                    m.AddQuad(prev.INegB, cur.INegB, cur.IPosB, prev.IPosB, 0f, 1f, 0f);
                prev = cur;
            }
        }

        static bool OnWedge(TrenchSpec s, float u) =>
            s.TwoSided ? Math.Abs(u) < s.WedgeRun - 0.03f : u > -0.03f && u < s.WedgeRun - 0.03f;

        static void Deflector(PadMeshData m, TrenchSpec s)
        {
            if (s.TwoSided)
            {
                Face(m, s, -s.WedgeRun, 0f);
                Face(m, s, 0f, s.WedgeRun);
            }
            else Face(m, s, 0f, s.WedgeRun);
        }

        static void Face(PadMeshData m, TrenchSpec s, float u0, float u1)
        {
            var a = P(s, u0, -s.HalfWidth(u0), s.SlopeY(u0));
            var b = P(s, u0, s.HalfWidth(u0), s.SlopeY(u0));
            var c = P(s, u1, s.HalfWidth(u1), s.SlopeY(u1));
            var d = P(s, u1, -s.HalfWidth(u1), s.SlopeY(u1));
            m.AddQuad(a, b, c, d, 0f, 1f, 0f);
        }

        static void BackWall(PadMeshData m, TrenchSpec s, float deck)
        {
            float hw = s.HalfWidth(0f);
            float top = Math.Max(s.TopAt(0f, deck), s.ApexY);
            var a = P(s, 0f, -hw, s.FloorY);
            var b = P(s, 0f, hw, s.FloorY);
            var c = P(s, 0f, hw, top);
            var d = P(s, 0f, -hw, top);
            s.ToWorld(1f, 0f, out float dx, out float dz);
            m.AddQuad(a, b, c, d, dx, 0f, dz);
        }

        static void Ponds(PadMeshData m, TrenchSpec s)
        {
            foreach (var poly in TrenchGrammar.PondPolys(s))
            {
                if (poly.Count < 4) continue;
                float y0 = TrenchSpec.GradeY - Math.Min(1.4f, Math.Max(0.45f, s.PondWidth * 0.04f));
                float y1 = TrenchSpec.GradeY + 0.06f;
                // poly is (u-),(u+),(u+,v+),(u-,v+) in world, not necessarily that order. Use the four corners.
                var c0 = poly[0]; var c1 = poly[1]; var c2 = poly[2]; var c3 = poly[3];
                Vec3 W(Vec2 p, float y) => new Vec3(p.X, y, p.Y);
                m.AddQuad(W(c0, y0), W(c1, y0), W(c2, y0), W(c3, y0), 0f, 1f, 0f);
                Wall(m, W(c0, y0), W(c1, y0), W(c1, y1), W(c0, y1));
                Wall(m, W(c1, y0), W(c2, y0), W(c2, y1), W(c1, y1));
                Wall(m, W(c2, y0), W(c3, y0), W(c3, y1), W(c2, y1));
                Wall(m, W(c3, y0), W(c0, y0), W(c0, y1), W(c3, y1));
            }
        }

        static void Wall(PadMeshData m, Vec3 a, Vec3 b, Vec3 c, Vec3 d)
        {
            // CCW ring from above: outward is to the right of the bottom edge.
            float dx = b.X - a.X, dz = b.Z - a.Z;
            m.AddQuad(a, b, c, d, dz, 0f, -dx);
        }

        static void Spare(PadDetailBuild b, TrenchSpec s)
        {
            float u = s.ExitLength * 0.22f;
            float v = -(s.HalfWidth(u) + Math.Max(3.2f, s.GratingWidth + 1.4f));
            float run = 2.4f, hw = 0.85f, apex = TrenchSpec.GradeY + 1.15f, toe = TrenchSpec.GradeY + 0.16f;
            var a = P(s, u - run, v - hw, toe);
            var b0 = P(s, u - run, v + hw, toe);
            var c = P(s, u, v + hw, apex);
            var d = P(s, u, v - hw, apex);
            var e = P(s, u + run, v + hw, toe);
            var f = P(s, u + run, v - hw, toe);
            b.Refractory.AddQuad(a, b0, c, d, 0f, 1f, 0f);
            b.Refractory.AddQuad(d, c, e, f, 0f, 1f, 0f);
            for (int k = -1; k <= 1; k += 2)
                Beam(b.Galvanized, s, u - 3.6f, u + 3.6f, v + k * 0.55f, TrenchSpec.GradeY + 0.12f, 0.07f, 0.05f);
        }

        static void Beam(PadMeshData m, TrenchSpec s, float u0, float u1, float v, float y, float halfAcross, float halfUp)
        {
            var a = P(s, u0, v - halfAcross, y + halfUp);
            var b = P(s, u0, v + halfAcross, y + halfUp);
            var c = P(s, u1, v + halfAcross, y + halfUp);
            var d = P(s, u1, v - halfAcross, y + halfUp);
            m.AddQuad(a, b, c, d, 0f, 1f, 0f);
            var a0 = P(s, u0, v - halfAcross, y - halfUp);
            var b0 = P(s, u0, v + halfAcross, y - halfUp);
            var c0 = P(s, u1, v + halfAcross, y - halfUp);
            var d0 = P(s, u1, v - halfAcross, y - halfUp);
            s.ToWorld(0f, 1f, out float ax, out float az);
            m.AddQuad(a0, a, d, d0, -ax, 0f, -az);
            m.AddQuad(b, b0, c0, c, ax, 0f, az);
        }

        static void Slab(PadMeshData m, TrenchSpec s, float deck)
        {
            const int N = 28;
            var outer = new List<Vec2>(N);
            for (int i = 0; i < N; i++)
            {
                double a = 2 * Math.PI * i / N;
                outer.Add(new Vec2(deck * (float)Math.Cos(a), deck * (float)Math.Sin(a)));
            }
            var hole = TrenchGrammar.DeckOpening(s, deck);
            var verts = new List<Vec2>(); var tris = new List<int>();
            PolyCut.Triangulate(outer, new List<IList<Vec2>> { hole }, verts, tris);
            for (int i = 0; i + 2 < tris.Count; i += 3)
            {
                var a = verts[tris[i]]; var b = verts[tris[i + 1]]; var c = verts[tris[i + 2]];
                float ny = (b.Y - a.Y) * (c.X - a.X) - (b.X - a.X) * (c.Y - a.Y);
                if (ny >= 0f) m.AddTri(a.X, 0f, a.Y, b.X, 0f, b.Y, c.X, 0f, c.Y);
                else m.AddTri(a.X, 0f, a.Y, c.X, 0f, c.Y, b.X, 0f, b.Y);
            }
        }

        static void Plinth(PadMeshData m, TrenchSpec s, float deck)
        {
            var hole = TrenchGrammar.GroundOpening(s);
            const int N = 28;
            float y0 = TrenchSpec.GradeY + 0.02f, y1 = 0f;
            for (int i = 0; i < N; i++)
            {
                double a0 = 2 * Math.PI * i / N, a1 = 2 * Math.PI * (i + 1) / N;
                float x0 = deck * (float)Math.Cos(a0), z0 = deck * (float)Math.Sin(a0);
                float x1 = deck * (float)Math.Cos(a1), z1 = deck * (float)Math.Sin(a1);
                float mx = (x0 + x1) * 0.5f, mz = (z0 + z1) * 0.5f;
                if (PolyCut.Contains(hole, mx, mz)) continue;
                // Also skip if the segment crosses the channel (one end inside).
                if (PolyCut.Contains(hole, x0, z0) || PolyCut.Contains(hole, x1, z1)) continue;
                var a = new Vec3(x0, y0, z0); var b = new Vec3(x1, y0, z1);
                var c = new Vec3(x1, y1, z1); var d = new Vec3(x0, y1, z0);
                m.AddQuad(a, b, c, d, mx, 0f, mz);
            }
        }

        static void Plates(PadDetailBuild b, TrenchSpec s, float deck)
        {
            var hole = TrenchGrammar.DeckOpening(s, deck);
            float plate = s.PlateSize;
            int n = Math.Max(2, (int)Math.Ceiling(2f * deck / plate));
            float origin = -deck;
            for (int ix = 0; ix < n; ix++)
                for (int iz = 0; iz < n; iz++)
                {
                    float x0 = origin + ix * plate, z0 = origin + iz * plate;
                    float cx = x0 + plate * 0.5f, cz = z0 + plate * 0.5f;
                    if (cx * cx + cz * cz > (deck - 0.35f) * (deck - 0.35f)) continue;
                    s.ToFrame(cx, cz, out float pu, out float pv);
                    float lip = Math.Min(s.HalfWidth(pu), s.SlotHalfCap > 0.5f ? s.SlotHalfCap : s.HalfWidth(pu));
                    if (Math.Abs(pv) < lip + s.GratingWidth + 0.4f && Math.Abs(pu) < deck) continue;
                    const float inset = 0.07f;
                    float x1 = x0 + inset, z1 = z0 + inset, x2 = x0 + plate - inset, z2 = z0 + plate - inset;
                    if (x2 <= x1 || z2 <= z1) continue;
                    if (PlateHitsSlot(hole, x1, z1, x2, z2)) continue;
                    var mesh = (ix + iz) % 5 == 0 ? b.Galvanized : b.Concrete;
                    var center = new Vec3((x1 + x2) * 0.5f, 0.055f, (z1 + z2) * 0.5f);
                    mesh.AddBox(center, new Vec3(x2 - x1, 0.07f, z2 - z1));
                }
        }

        static void Grating(PadMeshData m, TrenchSpec s, float deck)
        {
            float uMax = CutU(s, deck);
            float u0 = s.TwoSided ? -uMax : 0f;
            int n = Math.Max(2, (int)Math.Ceiling((uMax - u0) / 0.55f));
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < n; i++)
                {
                    float ua = u0 + (uMax - u0) * i / n;
                    float ub = u0 + (uMax - u0) * (i + 1) / n;
                    float va0 = side * (Lip(s, ua) + 0.18f);
                    float vb0 = side * (Lip(s, ub) + 0.18f);
                    float va1 = va0 + side * s.GratingWidth;
                    float vb1 = vb0 + side * s.GratingWidth;
                    var a = P(s, ua, va0, 0.045f);
                    var b = P(s, ub, vb0, 0.045f);
                    var c = P(s, ub, vb1, 0.045f);
                    var d = P(s, ua, va1, 0.045f);
                    if (Outside(a, deck) || Outside(c, deck)) continue;
                    m.AddQuad(a, b, c, d, 0f, 1f, 0f);
                    // Rib across the walkway, proud of the plate.
                    var r0 = P(s, ua, va0, 0.07f);
                    var r1 = P(s, ua, va1, 0.07f);
                    var r2 = P(s, ua + Math.Min(0.08f, (ub - ua) * 0.45f), va1, 0.07f);
                    var r3 = P(s, ua + Math.Min(0.08f, (ub - ua) * 0.45f), va0, 0.07f);
                    m.AddQuad(r0, r3, r2, r1, 0f, 1f, 0f);
                }
            }
        }

        static bool Outside(Vec3 p, float deck) => p.X * p.X + p.Z * p.Z > (deck - 0.4f) * (deck - 0.4f);

        static float Lip(TrenchSpec s, float u)
        {
            float hw = s.HalfWidth(u);
            return s.SlotHalfCap > 0.5f ? Math.Min(hw, s.SlotHalfCap) : hw;
        }

        /// <summary>A plate whose centre is clear can still straddle the slot. Reject any overlap.</summary>
        static bool PlateHitsSlot(List<Vec2> hole, float x1, float z1, float x2, float z2)
        {
            if (PolyCut.Contains(hole, (x1 + x2) * 0.5f, (z1 + z2) * 0.5f)) return true;
            if (PolyCut.Contains(hole, x1, z1) || PolyCut.Contains(hole, x2, z1)) return true;
            if (PolyCut.Contains(hole, x1, z2) || PolyCut.Contains(hole, x2, z2)) return true;
            if (PolyCut.Contains(hole, (x1 + x2) * 0.5f, z1) || PolyCut.Contains(hole, (x1 + x2) * 0.5f, z2)) return true;
            if (PolyCut.Contains(hole, x1, (z1 + z2) * 0.5f) || PolyCut.Contains(hole, x2, (z1 + z2) * 0.5f)) return true;
            foreach (var p in hole)
                if (p.X >= x1 && p.X <= x2 && p.Y >= z1 && p.Y <= z2) return true;
            return false;
        }

        static Vec3 VAdd(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        static Vec3 VSub(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        static void Drains(PadMeshData m, TrenchSpec s, float deck)
        {
            var hole = TrenchGrammar.DeckOpening(s, deck);
            for (int i = 0; i < s.DrainCount; i++)
            {
                // Golden-angle walk so the drains are not a copied clock face.
                double ang = (i + 0.5) * 2.399963;
                float r0 = deck * 0.72f;
                float x = r0 * (float)Math.Sin(ang), z = r0 * (float)Math.Cos(ang);
                if (PolyCut.Contains(hole, x, z)) continue;
                float len = Math.Min(5.5f, r0 * 0.28f);
                float inv = 1f / Math.Max(0.5f, r0);
                float dx = -x * inv, dz = -z * inv;
                float px = -dz, pz = dx;
                float hw = 0.16f;
                var a = new Vec3(x + px * hw, -0.05f, z + pz * hw);
                var b = new Vec3(x - px * hw, -0.05f, z - pz * hw);
                var c = new Vec3(x + dx * len - px * hw, -0.05f, z + dz * len - pz * hw);
                var d = new Vec3(x + dx * len + px * hw, -0.05f, z + dz * len + pz * hw);
                float mx = x + dx * len * 0.5f, mz = z + dz * len * 0.5f;
                if (PolyCut.Contains(hole, mx, mz)) continue;
                m.AddQuad(a, b, c, d, 0f, 1f, 0f);
                // Shallow side walls so the groove reads.
                m.AddQuad(a, d, new Vec3(d.X, 0.02f, d.Z), new Vec3(a.X, 0.02f, a.Z), px, 0f, pz);
                m.AddQuad(b, new Vec3(b.X, 0.02f, b.Z), new Vec3(c.X, 0.02f, c.Z), c, -px, 0f, -pz);
            }
        }

        static void EdgeMarks(PadMeshData m, TrenchSpec s, float deck)
        {
            float rad = deck - 0.62f;
            float band = 0.30f;
            float dash = 1.7f, gap = 1.15f;
            float step = (dash + gap) / rad;
            float width = dash / rad;
            for (float a = 0.15f; a < (float)(Math.PI * 2); a += step)
            {
                float a1 = a + width;
                float x0 = rad * (float)Math.Cos(a), z0 = rad * (float)Math.Sin(a);
                float x1 = rad * (float)Math.Cos(a1), z1 = rad * (float)Math.Sin(a1);
                float x2 = (rad - band) * (float)Math.Cos(a1), z2 = (rad - band) * (float)Math.Sin(a1);
                float x3 = (rad - band) * (float)Math.Cos(a), z3 = (rad - band) * (float)Math.Sin(a);
                m.AddQuad(new Vec3(x0, 0.11f, z0), new Vec3(x1, 0.11f, z1), new Vec3(x2, 0.11f, z2), new Vec3(x3, 0.11f, z3), 0f, 1f, 0f);
            }
            var hole = TrenchGrammar.DeckOpening(s, deck);
            PolyCut.EnsureWinding(hole, true);
            float walked = 0f;
            for (int i = 0; i < hole.Count; i++)
            {
                var a = hole[i]; var b = hole[(i + 1) % hole.Count];
                float dx = b.X - a.X, dz = b.Y - a.Y;
                float len = (float)Math.Sqrt(dx * dx + dz * dz);
                if (len < 0.2f) continue;
                float ux = dx / len, uz = dz / len;
                // Outward from the slot: right of the CCW edge.
                float ox = uz, oz = -ux;
                for (float t = 0.6f; t < len - 0.3f; t += 3.4f)
                {
                    float px = a.X + ux * t, pz = a.Y + uz * t;
                    var p0 = new Vec3(px, 0.12f, pz);
                    var p1 = new Vec3(px + ox * 0.9f, 0.12f, pz + oz * 0.9f);
                    var side = new Vec3(-uz * 0.08f, 0f, ux * 0.08f);
                    m.AddQuad(VSub(p0, side), VAdd(p0, side), VAdd(p1, side), VSub(p1, side), 0f, 1f, 0f);
                    walked += 1f;
                }
            }
            if (walked < 1f)
            {
                // Still a visible lip mark if the slot edge was tiny.
                m.AddQuad(new Vec3(-0.4f, 0.12f, s.HalfWidth(0f) + 0.2f), new Vec3(0.4f, 0.12f, s.HalfWidth(0f) + 0.2f),
                    new Vec3(0.4f, 0.12f, s.HalfWidth(0f) + 0.45f), new Vec3(-0.4f, 0.12f, s.HalfWidth(0f) + 0.45f), 0f, 1f, 0f);
            }
        }
    }
}
