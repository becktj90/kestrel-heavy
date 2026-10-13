// Textbook nozzle contour: throat from the expansion ratio, a short parabola to the exit,
// a 30° cone into a chamber. Cycle changes the shape (not only the length) so two catalog
// engines with a different (ε, cycle) cannot share a silhouette. No real-engine contours.
using System;
using System.Collections.Generic;

namespace Kestrel.Vehicle
{
    public sealed class BellContour
    {
        public BellSpec Spec;
        public List<ProfilePoint> Wall = new List<ProfilePoint>();
        public float ExitRadius, ThroatRadius, ChamberRadius, Length, ThroatY;
        public int TubeCount;
        public float BandDepth;
        public string MeshKey;
    }

    public static class BellProfile
    {
        /// <summary>Fraction of a 15° cone used as the diverging length. Solids are shorter (spec §4).</summary>
        public static float LengthFraction(EngineCycleKind cycle)
        {
            switch (cycle)
            {
                case EngineCycleKind.GasGenerator: return 0.74f;
                case EngineCycleKind.StagedCombustion: return 0.86f;
                case EngineCycleKind.FullFlow: return 0.80f;
                case EngineCycleKind.Expander: return 0.93f;
                case EngineCycleKind.PressureFed: return 0.68f;
                case EngineCycleKind.Solid: return 0.70f;
                case EngineCycleKind.Hybrid: return 0.76f;
                default: return 0.80f;
            }
        }

        /// <summary>Chamber area / throat area. Pressure-fed is larger; solids barely have a chamber.</summary>
        public static float Contraction(EngineCycleKind cycle)
        {
            switch (cycle)
            {
                case EngineCycleKind.PressureFed: return 4.0f;
                case EngineCycleKind.FullFlow: return 3.5f;
                case EngineCycleKind.Expander: return 2.4f;
                case EngineCycleKind.StagedCombustion: return 2.7f;
                case EngineCycleKind.Solid: return 1.35f;
                case EngineCycleKind.Hybrid: return 1.8f;
                default: return 3.0f;   // gas-generator
            }
        }

        public static BellContour Build(BellSpec spec, int lod)
        {
            var problems = spec.Validate();
            if (problems.Count > 0) throw new ArgumentException(string.Join("; ", problems));

            float re = spec.ExitRadius;
            float eps = spec.ExpansionRatio;
            float rt = re / (float)Math.Sqrt(eps);
            float rc = rt * (float)Math.Sqrt(Contraction(spec.Cycle));
            float thetaN = WallAngle(eps, spec.Cycle, exit: false);
            float thetaE = WallAngle(eps, spec.Cycle, exit: true);
            float ln = LengthFraction(spec.Cycle) * (re - rt) / (float)Math.Tan(15.0 * Math.PI / 180.0);
            if (ln < rt) ln = rt;

            float rhoD = 0.38f * rt;
            float blendAx = rhoD * (float)Math.Sin(thetaN);
            if (blendAx > ln * 0.4f) { float s = (ln * 0.4f) / blendAx; rhoD *= s; blendAx = ln * 0.4f; }
            float bezierLen = ln - blendAx;
            float rBlend = rt + rhoD * (1f - (float)Math.Cos(thetaN));

            var raw = new List<(float r, float y)>(64);
            AddDiverging(raw, re, rBlend, bezierLen, thetaN, thetaE);
            float yThroat = ln;
            // Downstream blend: φ = θn at the parabola, φ = 0 at the throat. Y increases toward the chamber.
            const int blendSteps = 4;
            for (int i = blendSteps - 1; i >= 0; i--)
            {
                float phi = thetaN * i / blendSteps;
                float y = bezierLen + rhoD * (float)Math.Sin(phi);
                float r = rt + rhoD * (1f - (float)Math.Cos(phi));
                Push(raw, r, y);
            }
            Push(raw, rt, yThroat);

            float cone = spec.SolidLike ? Math.Min(thetaN, (float)(20.0 * Math.PI / 180.0)) : (float)(30.0 * Math.PI / 180.0);
            float rhoU = (spec.SolidLike ? 0.6f : 1.5f) * rt;
            int upSteps = 4;
            float yUp = yThroat;
            float rUp = rt;
            for (int i = 1; i <= upSteps; i++)
            {
                float phi = cone * i / upSteps;
                yUp = yThroat + rhoU * (float)Math.Sin(phi);
                rUp = rt + rhoU * (1f - (float)Math.Cos(phi));
                Push(raw, rUp, yUp);
            }
            if (rc > rUp + 1e-4f)
            {
                float coneAx = (rc - rUp) / (float)Math.Tan(Math.Max(0.15, cone));
                Push(raw, rc, yUp + coneAx);
                yUp += coneAx;
            }
            else Push(raw, rc, yUp);

            float lc = spec.Cycle == EngineCycleKind.Solid ? 0.55f * rt : 2.2f * rc;
            Push(raw, rc, yUp + lc);
            // Injector face closes the mesh. Same Y, radius falls to the axis.
            Push(raw, rc, yUp + lc);
            Push(raw, 0f, yUp + lc);

            int joint = eps >= 80f ? AddSkirtJoint(raw, re, yThroat) : -1;

            int throat = IndexOfMinRadius(raw);
            int axial = BellLod.Axial(lod);
            var kept = Resample(raw, axial, throat, joint);
            int keptThroat = IndexOfMinRadius(kept);

            var wall = new List<ProfilePoint>(kept.Count);
            float v = 0f;
            for (int i = 0; i < kept.Count; i++)
            {
                var p = kept[i];
                if (i > 0)
                {
                    float dr = p.r - kept[i - 1].r, dy = p.y - kept[i - 1].y;
                    v += (float)Math.Sqrt(dr * dr + dy * dy);
                }
                Tangent(kept, i, out float tr, out float ty);
                float nl = (float)Math.Sqrt(tr * tr + ty * ty);
                if (nl < 1e-8f) { tr = 1f; ty = 0f; nl = 1f; }
                wall.Add(new ProfilePoint(p.r, p.y, tr / nl, ty / nl, v));
            }

            int radial = BellLod.Radial(lod);
            BellVariation.Mesh(spec, radial, out int tubes, out float depth);
            return new BellContour
            {
                Spec = spec,
                Wall = wall,
                ExitRadius = re,
                ThroatRadius = rt,
                ChamberRadius = rc,
                Length = wall[wall.Count - 1].Y,
                ThroatY = kept[keptThroat].y,
                TubeCount = tubes,
                BandDepth = depth,
                MeshKey = MeshKey(spec, lod, tubes, depth)
            };
        }

        public static string MeshKey(BellSpec spec, int lod, int tubes, float depth) =>
            FormattableString.Invariant($"{spec.EngineId}|{spec.Cycle}|{spec.ExpansionRatio:R}|{spec.ExitDiameter:R}|t{tubes}|d{depth:R}|L{lod}");

        static void AddDiverging(List<(float r, float y)> raw, float re, float rBlend, float length, float thetaN, float thetaE)
        {
            // R(u) = Re + (rBlend-Re)*u + B*u*(1-u), u = Y/length. A quadratic, which is the Bézier
            // the spec asks for, with B chosen from the throat-end slope and clamped so R stays
            // between the throat blend and the exit (no fold, no lip past the exit radius).
            const int steps = 12;
            float dr = rBlend - re;
            float b = dr + length * (float)Math.Tan(thetaN);
            float bExit = -length * (float)Math.Tan(thetaE) - dr;
            b = 0.65f * b + 0.35f * bExit;
            for (int guard = 0; guard < 6; guard++)
            {
                bool ok = true;
                for (int i = 1; i < steps; i++)
                {
                    float u = i / (float)steps;
                    float r = re + dr * u + b * u * (1f - u);
                    if (r > re + 1e-4f || r < rBlend - 1e-3f) { ok = false; break; }
                }
                if (ok) break;
                b *= 0.5f;
            }
            for (int i = 0; i <= steps; i++)
            {
                float u = i / (float)steps;
                float r = i == 0 ? re : i == steps ? rBlend : re + dr * u + b * u * (1f - u);
                Push(raw, r, length * u);
            }
        }

        /// <summary>A raised joint where a long vacuum skirt meets the cooled bell. Local, so exit and throat stay exact.
        /// Returns the crest index so resampling cannot shave the ring off.</summary>
        static int AddSkirtJoint(List<(float r, float y)> raw, float re, float throatY)
        {
            float y0 = throatY * 0.42f, y1 = throatY * 0.50f;
            float bump = 0.08f * re;
            for (int i = 0; i < raw.Count - 1; i++)
            {
                if (raw[i].y < y0 && raw[i + 1].y >= y0)
                {
                    float nextY = raw[i + 1].y;
                    float crestY = Math.Min(y1, nextY);
                    float r = Math.Min(re, raw[i].r + bump);
                    raw.Insert(i + 1, (r, y0));
                    if (crestY > y0 + 1e-4f) raw.Insert(i + 2, (r, crestY));
                    return i + 1;
                }
            }
            return -1;
        }

        static void Push(List<(float r, float y)> raw, float r, float y)
        {
            if (raw.Count > 0)
            {
                var p = raw[raw.Count - 1];
                if (y < p.y) y = p.y;
                if (Math.Abs(y - p.y) < 1e-6f && Math.Abs(r - p.r) < 1e-6f) return;
            }
            raw.Add((r, y));
        }

        /// <summary>Throat: minimum radius on the wall. The injector pole (r = 0) is a cap, not the throat.</summary>
        static int IndexOfMinRadius(List<(float r, float y)> pts)
        {
            int k = -1;
            for (int i = 0; i < pts.Count; i++)
            {
                if (pts[i].r <= 1e-3f) continue;
                if (k < 0 || pts[i].r < pts[k].r - 1e-8f) k = i;
            }
            return k < 0 ? 0 : k;
        }

        /// <summary>Keep exit, throat and the injector, then split the widest arc-length gaps until `count`.</summary>
        static List<(float r, float y)> Resample(List<(float r, float y)> raw, int count, int throat, int joint)
        {
            if (raw.Count <= count) return raw;
            var cum = new float[raw.Count];
            for (int i = 1; i < raw.Count; i++)
            {
                float dr = raw[i].r - raw[i - 1].r, dy = raw[i].y - raw[i - 1].y;
                cum[i] = cum[i - 1] + (float)Math.Sqrt(dr * dr + dy * dy);
            }
            var keep = new SortedSet<int> { 0, throat, raw.Count - 1 };
            if (joint > 0 && joint < raw.Count - 1) keep.Add(joint);
            while (keep.Count < count)
            {
                int bestA = 0, bestB = raw.Count - 1, bestI = -1;
                float bestGap = -1f;
                int prev = -1;
                foreach (int idx in keep)
                {
                    if (prev >= 0 && cum[idx] - cum[prev] > bestGap)
                    {
                        bestGap = cum[idx] - cum[prev];
                        bestA = prev; bestB = idx;
                    }
                    prev = idx;
                }
                float mid = (cum[bestA] + cum[bestB]) * 0.5f;
                float nearest = float.MaxValue;
                for (int i = bestA + 1; i < bestB; i++)
                {
                    if (keep.Contains(i)) continue;
                    float d = Math.Abs(cum[i] - mid);
                    if (d < nearest) { nearest = d; bestI = i; }
                }
                if (bestI < 0) break;
                keep.Add(bestI);
            }
            var list = new List<(float r, float y)>(keep.Count);
            foreach (int i in keep) list.Add(raw[i]);
            return list;
        }

        /// <summary>Outward meridian normal from the neighbour chord: vertical wall → (1, 0).</summary>
        static void Tangent(List<(float r, float y)> pts, int i, out float nr, out float ny)
        {
            int a = Math.Max(0, i - 1), b = Math.Min(pts.Count - 1, i + 1);
            float dr = pts[b].r - pts[a].r, dy = pts[b].y - pts[a].y;
            // Perpendicular to (dr, dy). (dy, -dr) points outward on a vertical wall (dr=0, dy>0).
            nr = dy; ny = -dr;
        }

        static float WallAngle(float eps, EngineCycleKind cycle, bool exit)
        {
            // Spec §2.1 table, degrees. Linear in ε between the knots.
            float e0 = 10f, e1 = 20f, e2 = 50f, e3 = 100f, e4 = 200f;
            float n0 = 21f, n1 = 24f, n2 = 28f, n3 = 32f, n4 = 35f;
            float x0 = 12f, x1 = 9f, x2 = 8f, x3 = 7f, x4 = 6f;
            float deg = exit
                ? LerpKnot(eps, e0, e1, e2, e3, e4, x0, x1, x2, x3, x4)
                : LerpKnot(eps, e0, e1, e2, e3, e4, n0, n1, n2, n3, n4);
            switch (cycle)
            {
                case EngineCycleKind.GasGenerator: deg += exit ? 1.5f : -3.5f; break;
                case EngineCycleKind.StagedCombustion: deg += exit ? -1.2f : 2.5f; break;
                case EngineCycleKind.FullFlow: deg += exit ? 0.4f : 4.0f; break;
                case EngineCycleKind.Expander: deg += exit ? -2.0f : -4.0f; break;
                case EngineCycleKind.PressureFed: deg += exit ? 2.0f : 1.0f; break;
                case EngineCycleKind.Hybrid: deg += exit ? 0.5f : -1.5f; break;
            }
            if (!exit && (cycle == EngineCycleKind.Solid || cycle == EngineCycleKind.Hybrid)) deg = Math.Min(deg, 20f);
            if (deg < 3f) deg = 3f;
            if (deg > 55f) deg = 55f;
            return deg * (float)(Math.PI / 180.0);
        }

        static float LerpKnot(float e, float e0, float e1, float e2, float e3, float e4, float v0, float v1, float v2, float v3, float v4)
        {
            if (e <= e0) return v0;
            if (e >= e4) return v4;
            if (e < e1) return Lerp(v0, v1, (e - e0) / (e1 - e0));
            if (e < e2) return Lerp(v1, v2, (e - e1) / (e2 - e1));
            if (e < e3) return Lerp(v2, v3, (e - e2) / (e3 - e2));
            return Lerp(v3, v4, (e - e3) / (e4 - e3));
        }

        static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }

    /// <summary>Seeded variation. Mesh fields are per (engine type, vehicle); roll and gimbal are per slot.</summary>
    public static class BellVariation
    {
        public static void Mesh(BellSpec spec, int radial, out int tubes, out float depth)
        {
            ulong h = Mix(spec.VehicleSeed ^ HashId(spec.EngineId) ^ (ulong)spec.Cycle);
            float u = (h & 0xFFFFFF) / (float)0x1000000;
            int basis = Math.Max(4, radial / 2);
            int want = (int)Math.Round(basis * (0.90 + 0.20 * u));
            tubes = SnapDivisor(radial, want);
            float u2 = ((h >> 24) & 0xFFFFFF) / (float)0x1000000;
            depth = spec.ExitRadius * (0.004f + 0.004f * u2);
        }

        public static BellInstance Place(BellSpec spec, int stage, int slot)
        {
            ulong h = Mix(spec.VehicleSeed ^ HashId(spec.EngineId) ^ (ulong)(stage * 131 + slot * 17 + 3));
            float roll = (h & 0xFFFF) / 65535f * 360f;
            float gx = (((h >> 16) & 0xFF) / 255f - 0.5f) * 0.8f;   // ±0.4°
            float gz = (((h >> 24) & 0xFF) / 255f - 0.5f) * 0.8f;
            return new BellInstance { Spec = spec, Stage = stage, Slot = slot, RollDeg = roll, GimbalXDeg = gx, GimbalZDeg = gz };
        }

        public static int SnapDivisor(int radial, int want)
        {
            int best = 1, bestDist = int.MaxValue;
            int n = Math.Max(3, radial);
            for (int d = 1; d <= n; d++)
            {
                if (n % d != 0) continue;
                int dist = Math.Abs(d - want);
                if (dist < bestDist) { bestDist = dist; best = d; }
            }
            return best;
        }

        public static ulong HashId(string id)
        {
            ulong h = 14695981039346656037UL;
            if (id == null) return h;
            for (int i = 0; i < id.Length; i++) h = (h ^ id[i]) * 1099511628211UL;
            return h;
        }

        public static ulong Mix(ulong z)
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
