// Lit payload thumbnails, ray traced on the CPU from procedural parts (original art: no meshes, no bought assets).
// One scene per PayloadForms id: a detailed bus, gold MLI with a crinkle normal, solar wings with a blue cell grid,
// dishes, whips and booms. A warm key light, a cool rim light, a dusk sky reflection and a Dusk backdrop.
// Pure C# (no UnityEngine), so dotnet tests it and the WebGL player runs it without a camera, a shader or a GPU
// read-back. The game uploads the pixels once per (form, pixel size) and caches them (PayloadThumbnails).
using System;
using System.Collections.Generic;

namespace Kestrel.Core.Flight
{
    public enum PayloadSurface { Foil, SilverFoil, Cells, Bus, White, Steel, Orange, Dark, Lens, Dish }

    /// <summary>Dusk: an opaque night-to-deep-blue ground (the card tile). Clear: alpha 0 off the model (a map node).</summary>
    public enum PayloadBackdrop { Dusk, Clear }

    public static class PayloadRender
    {
        /// <summary>A sliced render of <paramref name="form"/> at <paramref name="px"/> square (see <see cref="Job"/>).</summary>
        public static Job Begin(string form, int px, PayloadBackdrop backdrop = PayloadBackdrop.Dusk) => new Job(form, px, backdrop);

        public const int MinPx = 16;
        public const int MaxPx = 1024;

        /// <summary>Every payload type the renderer draws: the same ids as <see cref="PayloadForms.Ids"/>.</summary>
        public static IReadOnlyList<string> Types => PayloadForms.Ids;

        /// <summary>RGBA32, <paramref name="px"/> x <paramref name="px"/>, rows from the top. Unknown forms draw "box".</summary>
        public static byte[] Rgba(string form, int px, PayloadBackdrop backdrop = PayloadBackdrop.Dusk)
        {
            var job = new Job(form, px, backdrop);
            while (!job.Step(int.MaxValue)) { }
            return job.Pixels;
        }

        /// <summary>
        /// A render that runs a few rows at a time, so the WebGL player can spread a thumbnail over frames and keep
        /// 60 fps. Two passes: one centre sample per pixel, then four rotated-grid samples where the hit part changes.
        /// </summary>
        public sealed class Job
        {
            readonly List<Prim> _scene;
            readonly Cam _cam;
            readonly PayloadBackdrop _backdrop;
            readonly int[] _ids;
            readonly C4[] _col;
            int _row;   // 0..px-1 first pass, px..2px-1 second pass

            public readonly int Px;
            public readonly string Form;
            public readonly byte[] Pixels;
            public bool Done => _row >= Px * 2;

            internal Job(string form, int px, PayloadBackdrop backdrop)
            {
                Px = Clamp(px, MinPx, MaxPx);
                Form = PayloadForms.KnownForm(form) ? form : "box";
                _backdrop = backdrop;
                _scene = Scene(Form);
                _cam = Frame(_scene);
                _ids = new int[Px * Px];
                _col = new C4[Px * Px];
                Pixels = new byte[Px * Px * 4];
            }

            /// <summary>Run up to <paramref name="rows"/> row passes. True when the image is finished.</summary>
            public bool Step(int rows)
            {
                int px = Px;
                for (int n = 0; n < rows && _row < px * 2; n++, _row++)
                {
                    if (_row < px)
                    {
                        int y = _row;
                        for (int x = 0; x < px; x++)
                        {
                            int i = y * px + x;
                            _col[i] = Sample(_scene, _cam, (x + 0.5f) / px, (y + 0.5f) / px, _backdrop, out _ids[i]);
                        }
                        continue;
                    }
                    int yy = _row - px;
                    for (int x = 0; x < px; x++)
                    {
                        int i = yy * px + x;
                        int id = _ids[i];
                        bool edge = (x > 0 && _ids[i - 1] != id) || (x < px - 1 && _ids[i + 1] != id)
                            || (yy > 0 && _ids[i - px] != id) || (yy < px - 1 && _ids[i + px] != id);
                        C4 c = _col[i];
                        if (edge)
                        {
                            c = default;
                            for (int s = 0; s < 4; s++)
                            {
                                var sc = Sample(_scene, _cam, (x + 0.5f + Rgss[s * 2]) / px, (yy + 0.5f + Rgss[s * 2 + 1]) / px, _backdrop, out _);
                                c.R += sc.R * 0.25f; c.G += sc.G * 0.25f; c.B += sc.B * 0.25f; c.A += sc.A * 0.25f;
                            }
                        }
                        int o = i * 4;
                        Pixels[o] = Byte(c.R);
                        Pixels[o + 1] = Byte(c.G);
                        Pixels[o + 2] = Byte(c.B);
                        Pixels[o + 3] = Byte(c.A);
                    }
                }
                return Done;
            }
        }

        public static byte[] Png(string form, int px, PayloadBackdrop backdrop = PayloadBackdrop.Dusk) =>
            PayloadPortrait.EncodePng(Clamp(px, MinPx, MaxPx), Clamp(px, MinPx, MaxPx), Rgba(form, px, backdrop));

        /// <summary>Variance of Rec.709 luma (0..1) over the opaque pixels. A flat or blank image is about 0.</summary>
        public static double LumaVariance(byte[] rgba)
        {
            if (rgba == null || rgba.Length < 4) return 0;
            double sum = 0, sq = 0;
            long n = 0;
            for (int i = 0; i + 3 < rgba.Length; i += 4)
            {
                if (rgba[i + 3] < 8) continue;
                double l = (0.2126 * rgba[i] + 0.7152 * rgba[i + 1] + 0.0722 * rgba[i + 2]) / 255.0;
                sum += l; sq += l * l; n++;
            }
            if (n < 2) return 0;
            double mean = sum / n;
            return Math.Max(0, sq / n - mean * mean);
        }

        /// <summary>Pixels the model covers (not the backdrop), as a fraction of the image.</summary>
        public static float Coverage(string form, int px)
        {
            var scene = Scene(form);
            var cam = Frame(scene);
            int hit = 0;
            for (int y = 0; y < px; y++)
                for (int x = 0; x < px; x++)
                {
                    Sample(scene, cam, (x + 0.5f) / px, (y + 0.5f) / px, PayloadBackdrop.Clear, out int id);
                    if (id >= 0) hit++;
                }
            return hit / (float)(px * px);
        }

        static readonly float[] Rgss = { -0.125f, -0.375f, 0.375f, -0.125f, 0.125f, 0.375f, -0.375f, 0.125f };

        // ---- Camera ---------------------------------------------------------------------------------

        struct Cam
        {
            public V3 Eye, F, R, U;
            public float X0, Y0, Span;
        }

        static readonly V3 ViewDir = V3.Norm(new V3(1.0f, 0.62f, 1.3f));
        static readonly V3 KeyDir = V3.Norm(new V3(-0.55f, 0.85f, 0.65f));
        static readonly V3 RimDir = V3.Norm(new V3(0.65f, 0.3f, -1.0f));
        static readonly V3 KeyCol = new V3(1.05f, 0.97f, 0.86f);
        static readonly V3 RimCol = new V3(0.42f, 0.62f, 1.0f);

        /// <summary>Perspective from the front-right, above; the image window is fitted to the projected part bounds.</summary>
        static Cam Frame(List<Prim> scene)
        {
            V3 lo = new V3(1e9f, 1e9f, 1e9f), hi = new V3(-1e9f, -1e9f, -1e9f);
            foreach (var p in scene)
            {
                lo = V3.Min(lo, p.Center - new V3(p.Radius, p.Radius, p.Radius));
                hi = V3.Max(hi, p.Center + new V3(p.Radius, p.Radius, p.Radius));
            }
            var center = (lo + hi) * 0.5f;
            float extent = Math.Max(0.1f, (hi - lo).Length * 0.5f);
            var cam = new Cam();
            cam.Eye = center + ViewDir * (extent * 4.2f);
            cam.F = V3.Norm(center - cam.Eye);
            cam.R = V3.Norm(V3.Cross(cam.F, new V3(0, 1, 0)));
            cam.U = V3.Cross(cam.R, cam.F);
            float x0 = 1e9f, x1 = -1e9f, y0 = 1e9f, y1 = -1e9f;
            foreach (var p in scene)
                foreach (var c in p.Corners())
                {
                    var d = c - cam.Eye;
                    float z = V3.Dot(d, cam.F);
                    if (z < 1e-3f) continue;
                    float sx = V3.Dot(d, cam.R) / z, sy = V3.Dot(d, cam.U) / z;
                    x0 = Math.Min(x0, sx); x1 = Math.Max(x1, sx);
                    y0 = Math.Min(y0, sy); y1 = Math.Max(y1, sy);
                }
            float span = Math.Max(x1 - x0, y1 - y0) / 0.92f;
            cam.Span = span;
            cam.X0 = (x0 + x1) * 0.5f - span * 0.5f;
            cam.Y0 = (y0 + y1) * 0.5f + span * 0.5f;
            return cam;
        }

        static C4 Sample(List<Prim> scene, Cam cam, float u, float v, PayloadBackdrop backdrop, out int id)
        {
            float sx = cam.X0 + u * cam.Span;
            float sy = cam.Y0 - v * cam.Span;
            var dir = V3.Norm(cam.F + cam.R * sx + cam.U * sy);
            if (!Trace(scene, cam.Eye, dir, 1e9f, -1, out var hit))
            {
                id = -1;
                if (backdrop == PayloadBackdrop.Clear) return default;
                var b = Backdrop(u, v);
                return new C4 { R = b.X, G = b.Y, B = b.Z, A = 1f };
            }
            id = hit.Index;
            var c = Shade(scene, hit, dir);
            return new C4 { R = c.X, G = c.Y, B = c.Z, A = 1f };
        }

        /// <summary>Dusk palette ground: Night at the corners, Deep in the middle, a soft cool glow behind the model.</summary>
        static V3 Backdrop(float u, float v)
        {
            float dx = u - 0.5f, dy = v - 0.44f;
            float r = (float)Math.Sqrt(dx * dx + dy * dy) / 0.72f;
            if (r > 1f) r = 1f;
            var night = new V3(0x05 / 255f, 0x08 / 255f, 0x1A / 255f);
            var deep = new V3(0x0F / 255f, 0x17 / 255f, 0x40 / 255f);
            var glow = new V3(0x24 / 255f, 0x33 / 255f, 0x72 / 255f);
            V3 c = r < 0.45f ? V3.Lerp(glow, deep, r / 0.45f) : V3.Lerp(deep, night, (r - 0.45f) / 0.55f);
            // A warm horizon kiss at the bottom edge, like the board's sky.
            float warm = Smooth(0.78f, 1.0f, v) * 0.22f;
            c = V3.Lerp(c, new V3(0x3A / 255f, 0x2A / 255f, 0x5E / 255f), warm);
            return c;
        }

        // ---- Shading --------------------------------------------------------------------------------

        static V3 Shade(List<Prim> scene, Hit h, V3 dir)
        {
            var p = scene[h.Index];
            var n = h.N;
            var view = -dir;
            if (V3.Dot(n, view) < 0f) n = -n;   // two-sided (dish bowls, thin panels seen edge-on)
            var m = Surface(p, h, ref n);
            float ndv = Math.Max(0f, V3.Dot(n, view));

            float shadow = 1f;
            if (V3.Dot(n, KeyDir) > 0f && Trace(scene, h.P + n * 0.004f, KeyDir, 1e9f, -1, out _)) shadow = 0.18f;

            float key = Math.Max(0f, V3.Dot(n, KeyDir)) * shadow;
            float rim = Math.Max(0f, V3.Dot(n, RimDir));
            float sky = n.Y * 0.5f + 0.5f;
            var ambient = V3.Lerp(new V3(0.13f, 0.08f, 0.07f), new V3(0.12f, 0.15f, 0.28f), sky);
            var light = ambient + KeyCol * key + RimCol * (rim * 0.55f * (1f - 0.5f * m.Metal));
            var c = V3.Mul(m.Albedo, light) * m.Diffuse;

            var hk = V3.Norm(KeyDir + view);
            var hr = V3.Norm(RimDir + view);
            float sk = Pow(Math.Max(0f, V3.Dot(n, hk)), m.Power) * shadow;
            float sr = Pow(Math.Max(0f, V3.Dot(n, hr)), m.Power) * 0.6f;
            c += V3.Mul(m.Spec, KeyCol * sk + RimCol * sr) * m.SpecK;

            if (m.Env > 0f)
            {
                var refl = dir - n * (2f * V3.Dot(dir, n));
                float fres = 0.35f + 0.65f * Pow(1f - ndv, 4f);
                var env = Env(refl);
                // Metals mirror the sky's brightness in their own colour (gold stays gold under a blue sky).
                float lum = (env.X + env.Y + env.Z) * (1f / 3f);
                env = V3.Lerp(env, new V3(lum, lum, lum), m.Metal);
                c += V3.Mul(m.Spec, env) * (m.Env * fres);
            }
            // Soft rim light on the silhouette, cool like the board's night side.
            float edge = Pow(1f - ndv, 3f);
            c += RimCol * (edge * 0.28f * (0.4f + 0.6f * rim) * (1f - 0.6f * m.Metal));
            return Tone(c);
        }

        /// <summary>The dusk sky a shiny part reflects: amber horizon, deep blue above, dark below.</summary>
        static V3 Env(V3 r)
        {
            if (r.Y >= 0f)
            {
                float t = (float)Math.Sqrt(Math.Min(1f, r.Y));
                var horizon = new V3(0.95f, 0.58f, 0.32f);
                var high = new V3(0.16f, 0.22f, 0.52f);
                var c = V3.Lerp(horizon, high, t);
                // The key light's hot spot in the sky.
                float sun = Pow(Math.Max(0f, V3.Dot(r, KeyDir)), 24f);
                return c + new V3(1.4f, 1.25f, 1.0f) * sun;
            }
            float d = Math.Min(1f, -r.Y * 2f);
            return V3.Lerp(new V3(0.62f, 0.38f, 0.26f), new V3(0.16f, 0.12f, 0.16f), d);
        }

        static V3 Tone(V3 c) => new V3(Filmic(c.X), Filmic(c.Y), Filmic(c.Z));

        static float Filmic(float x)
        {
            if (x <= 0f) return 0f;
            float y = 1f - (float)Math.Exp(-x * 1.25f);
            return (float)Math.Pow(y, 1f / 1.12f);
        }

        struct Mat
        {
            public V3 Albedo, Spec;
            public float Diffuse, SpecK, Power, Env, Metal;
        }

        static Mat M(float r, float g, float b, float diffuse, float sr, float sg, float sb, float specK, float power, float env) =>
            new Mat { Albedo = new V3(r, g, b), Diffuse = diffuse, Spec = new V3(sr, sg, sb), SpecK = specK, Power = power, Env = env };

        /// <summary>Material at the hit: colour, gloss and (foil) the crinkled normal. Patterns use the part's own frame.</summary>
        static Mat Surface(Prim p, Hit h, ref V3 n)
        {
            var lp = h.Local;
            switch (p.Surface)
            {
                case PayloadSurface.Foil:
                case PayloadSurface.SilverFoil:
                {
                    // Multi-layer insulation: crinkled film (two octaves of value noise bend the normal) with quilt stitches.
                    var q = lp * 19f + new V3(p.Seed, p.Seed * 0.7f, p.Seed * 1.3f);
                    var bend = new V3(Noise(q) - 0.5f, Noise(q + new V3(17.1f, 3.3f, 9.2f)) - 0.5f, Noise(q + new V3(5.7f, 23.9f, 1.1f)) - 0.5f);
                    var q2 = lp * 46f;
                    bend += new V3(Noise(q2) - 0.5f, Noise(q2 + new V3(7.7f, 1.9f, 4.4f)) - 0.5f, Noise(q2 + new V3(2.2f, 8.8f, 6.6f)) - 0.5f) * 0.55f;
                    bool gold = p.Surface == PayloadSurface.Foil;
                    n = V3.Norm(n + (bend - n * V3.Dot(bend, n)) * (gold ? 0.85f : 0.45f));
                    float grain = 0.88f + 0.24f * Noise(lp * 61f);
                    float stitch = Stitch(lp, 0.21f, 0.012f);
                    var m = gold
                        ? M(0.98f * grain, 0.64f * grain, 0.14f * grain, 0.62f, 1.0f, 0.72f, 0.24f, 1.8f, 44f, 1.3f)
                        : M(0.84f * grain, 0.86f * grain, 0.90f * grain, 0.75f, 0.9f, 0.92f, 0.98f, 1.2f, 40f, 0.6f);
                    m.Metal = 0.85f;
                    if (stitch > 0f) { m.Albedo *= 1f - 0.35f * stitch; m.Env *= 1f - 0.45f * stitch; }
                    return m;
                }
                case PayloadSurface.Cells:
                    return Cells(p, h, lp);
                case PayloadSurface.Bus:
                {
                    // Honeycomb panels with seams, bolt heads and a radiator stripe.
                    FaceUv(p, h, lp, out float fu, out float fv, out float hu, out float hv);
                    float seam = Math.Max(Line(fu, 0.24f, 0.007f), Line(fv, 0.24f, 0.007f));
                    float boltU = Frac(fu / 0.12f), boltV = Frac(fv / 0.12f);
                    bool bolt = Math.Abs(boltU - 0.5f) < 0.09f && Math.Abs(boltV - 0.5f) < 0.09f && Frac(fu / 0.24f) < 0.5f && Frac(fv / 0.24f) < 0.5f;
                    bool radiator = Math.Abs(fv) < hv * 0.32f && Line(fu, 0.035f, 0.004f) > 0f;
                    var m = M(0.82f, 0.82f, 0.80f, 1f, 0.9f, 0.9f, 0.9f, 0.35f, 22f, 0.18f);
                    if (radiator) m.Albedo = new V3(0.92f, 0.93f, 0.94f) * 0.9f;
                    if (seam > 0f) m.Albedo *= 1f - 0.45f * seam;
                    if (bolt) m.Albedo = new V3(0.42f, 0.44f, 0.48f);
                    return m;
                }
                case PayloadSurface.White:
                {
                    float rib = Line(lp.Y, 0.11f, 0.004f);
                    var m = M(0.88f, 0.87f, 0.84f, 1f, 0.9f, 0.9f, 0.9f, 0.28f, 18f, 0.12f);
                    if (rib > 0f) m.Albedo *= 0.86f;
                    return m;
                }
                case PayloadSurface.Steel:
                {
                    var m = M(0.52f, 0.55f, 0.60f, 0.55f, 0.85f, 0.88f, 0.92f, 0.9f, 46f, 0.55f);
                    m.Metal = 0.7f;
                    return m;
                }
                case PayloadSurface.Orange:
                    return M(0.96f, 0.43f, 0.12f, 1f, 1f, 0.8f, 0.6f, 0.35f, 26f, 0.1f);
                case PayloadSurface.Dark:
                    return M(0.07f, 0.07f, 0.09f, 1f, 0.8f, 0.8f, 0.85f, 0.45f, 40f, 0.25f);
                case PayloadSurface.Lens:
                    return M(0.02f, 0.05f, 0.12f, 1f, 0.6f, 0.75f, 1f, 1.2f, 90f, 0.7f);
                default:
                {
                    // Dish: off-white composite with faint concentric ribs.
                    float rr = (float)Math.Sqrt(lp.X * lp.X + lp.Y * lp.Y);
                    var m = M(0.90f, 0.89f, 0.85f, 1f, 0.9f, 0.9f, 0.9f, 0.3f, 20f, 0.12f);
                    if (Line(rr, Math.Max(0.04f, p.A * 0.24f), 0.005f) > 0f) m.Albedo *= 0.84f;
                    return m;
                }
            }
        }

        /// <summary>Solar array: a grid of deep-blue cells with silver busbars, a frame round each face, glass gloss.</summary>
        static Mat Cells(Prim p, Hit h, V3 lp)
        {
            FaceUv(p, h, lp, out float fu, out float fv, out float hu, out float hv);
            const float frame = 0.022f;
            var m = M(0.04f, 0.10f, 0.32f, 1f, 0.62f, 0.72f, 1f, 0.95f, 70f, 0.42f);
            if (hu - Math.Abs(fu) < frame || hv - Math.Abs(fv) < frame)
                return M(0.62f, 0.64f, 0.68f, 0.7f, 0.9f, 0.9f, 0.9f, 0.6f, 36f, 0.35f);
            const float cell = 0.072f;
            float cu = Frac((fu + hu) / cell), cv = Frac((fv + hv) / cell);
            if (cu < 0.08f || cv < 0.08f)
                return M(0.72f, 0.74f, 0.78f, 0.9f, 0.9f, 0.9f, 0.95f, 0.5f, 40f, 0.3f);
            // Fingers across each cell and a per-cell tint, so the wing reads as real cells, not a flat blue.
            float tint = 0.85f + 0.3f * Hash((int)Math.Floor((fu + hu) / cell), (int)Math.Floor((fv + hv) / cell), p.Seed);
            m.Albedo *= tint;
            if (Frac(cu * 5f) < 0.07f) m.Albedo = V3.Lerp(m.Albedo, new V3(0.34f, 0.42f, 0.62f), 0.6f);
            return m;
        }

        /// <summary>The two in-face coordinates of a hit, centred, and the face half-sizes. Boxes use their hit face;
        /// round parts unroll (arc length round the axis, height along it).</summary>
        static void FaceUv(Prim p, Hit h, V3 lp, out float u, out float v, out float hu, out float hv)
        {
            if (p.Kind == Kind.Box)
            {
                var ln = h.LocalN;
                float ax = Math.Abs(ln.X), ay = Math.Abs(ln.Y), az = Math.Abs(ln.Z);
                if (ax >= ay && ax >= az) { u = lp.Z; v = lp.Y; hu = p.C; hv = p.B; }
                else if (ay >= az) { u = lp.X; v = lp.Z; hu = p.A; hv = p.C; }
                else { u = lp.X; v = lp.Y; hu = p.A; hv = p.B; }
                return;
            }
            float r = Math.Max(p.A, p.B);
            float ang = (float)Math.Atan2(lp.Y, lp.X);
            u = ang * r;
            v = lp.Z;
            hu = 1e3f;
            hv = p.C > 0f ? p.C : 1e3f;
        }

        static float Stitch(V3 lp, float pitch, float w)
        {
            float a = Line(lp.X + lp.Z * 0.5f, pitch, w);
            float b = Line(lp.Y + lp.Z * 0.3f, pitch * 1.3f, w);
            return Math.Max(a, b);
        }

        /// <summary>1 on a thin line every <paramref name="pitch"/>, 0 elsewhere.</summary>
        static float Line(float x, float pitch, float halfW)
        {
            float f = Frac(x / pitch + 0.5f) - 0.5f;
            return Math.Abs(f * pitch) < halfW ? 1f : 0f;
        }

        // ---- Noise ----------------------------------------------------------------------------------

        static float Hash(int x, int y, int z)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393) + (uint)(y * 668265263) + (uint)(z * 2147483647);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }

        static float Hash(int x, int y, float seed) => Hash(x, y, (int)(seed * 977f));

        static float Noise(V3 p)
        {
            int x0 = (int)Math.Floor(p.X), y0 = (int)Math.Floor(p.Y), z0 = (int)Math.Floor(p.Z);
            float fx = p.X - x0, fy = p.Y - y0, fz = p.Z - z0;
            float ux = fx * fx * (3f - 2f * fx), uy = fy * fy * (3f - 2f * fy), uz = fz * fz * (3f - 2f * fz);
            float c000 = Hash(x0, y0, z0), c100 = Hash(x0 + 1, y0, z0), c010 = Hash(x0, y0 + 1, z0), c110 = Hash(x0 + 1, y0 + 1, z0);
            float c001 = Hash(x0, y0, z0 + 1), c101 = Hash(x0 + 1, y0, z0 + 1), c011 = Hash(x0, y0 + 1, z0 + 1), c111 = Hash(x0 + 1, y0 + 1, z0 + 1);
            float x00 = c000 + (c100 - c000) * ux, x10 = c010 + (c110 - c010) * ux;
            float x01 = c001 + (c101 - c001) * ux, x11 = c011 + (c111 - c011) * ux;
            float y0v = x00 + (x10 - x00) * uy, y1v = x01 + (x11 - x01) * uy;
            return y0v + (y1v - y0v) * uz;
        }

        // ---- Ray casting ----------------------------------------------------------------------------

        enum Kind { Box, Cylinder, Cone, Sphere, Dish }

        struct Hit
        {
            public int Index;
            public float T;
            public V3 P, N, Local, LocalN;
        }

        static bool Trace(List<Prim> scene, V3 o, V3 d, float maxT, int skip, out Hit best)
        {
            best = default;
            best.T = maxT;
            best.Index = -1;
            for (int i = 0; i < scene.Count; i++)
            {
                if (i == skip) continue;
                var p = scene[i];
                // Bounding-sphere reject.
                var oc = o - p.Center;
                float b = V3.Dot(oc, d);
                float c = V3.Dot(oc, oc) - p.Radius * p.Radius;
                if (c > 0f && b > 0f) continue;
                if (b * b - c < 0f) continue;
                var lo = p.ToLocal(o - p.O);
                var ld = p.ToLocal(d);
                if (!p.Intersect(lo, ld, best.T, out float t, out V3 ln)) continue;
                best.T = t;
                best.Index = i;
                best.LocalN = ln;
                best.Local = lo + ld * t;
            }
            if (best.Index < 0) return false;
            var hp = scene[best.Index];
            best.P = o + d * best.T;
            best.N = V3.Norm(hp.U * best.LocalN.X + hp.V * best.LocalN.Y + hp.W * best.LocalN.Z);
            return true;
        }

        sealed class Prim
        {
            public Kind Kind;
            public V3 O, U, V, W;            // origin and orthonormal frame; W is the main axis
            public float A, B, C;            // box half sizes / radius, second radius, half length / dish sphere radius
            public float Z0, Z1;             // local extent along W
            public PayloadSurface Surface;
            public float Seed;
            public V3 Center;
            public float Radius;

            public V3 ToLocal(V3 v) => new V3(V3.Dot(v, U), V3.Dot(v, V), V3.Dot(v, W));

            public void Bake(int index)
            {
                Seed = index * 1.618f;
                float ex, ey;
                switch (Kind)
                {
                    case Kind.Box: ex = A; ey = B; Z0 = -C; Z1 = C; break;
                    case Kind.Cylinder: ex = ey = A; Z0 = -C; Z1 = C; break;
                    case Kind.Cone: ex = ey = Math.Max(A, B); Z0 = -C; Z1 = C; break;
                    case Kind.Sphere: ex = ey = A; Z0 = -A; Z1 = A; break;
                    default: ex = ey = A; Z0 = 0f; Z1 = DishDepth; break;
                }
                float ez = (Z1 - Z0) * 0.5f;
                Center = O + W * ((Z0 + Z1) * 0.5f);
                Radius = (float)Math.Sqrt(ex * ex + ey * ey + ez * ez) + 1e-3f;
            }

            public float DishDepth => C - (float)Math.Sqrt(Math.Max(0f, C * C - A * A));

            public IEnumerable<V3> Corners()
            {
                float ex = Kind == Kind.Box ? A : Kind == Kind.Cone ? Math.Max(A, B) : A;
                float ey = Kind == Kind.Box ? B : ex;
                for (int i = 0; i < 8; i++)
                {
                    float x = (i & 1) == 0 ? -ex : ex;
                    float y = (i & 2) == 0 ? -ey : ey;
                    float z = (i & 4) == 0 ? Z0 : Z1;
                    yield return O + U * x + V * y + W * z;
                }
            }

            public bool Intersect(V3 o, V3 d, float maxT, out float t, out V3 n)
            {
                switch (Kind)
                {
                    case Kind.Box: return HitBox(o, d, maxT, out t, out n);
                    case Kind.Cylinder: return HitCone(o, d, A, A, C, maxT, out t, out n);
                    case Kind.Cone: return HitCone(o, d, A, B, C, maxT, out t, out n);
                    case Kind.Sphere: return HitSphere(o, d, maxT, out t, out n);
                    default: return HitDish(o, d, maxT, out t, out n);
                }
            }

            bool HitBox(V3 o, V3 d, float maxT, out float t, out V3 n)
            {
                t = 0f; n = default;
                float tn = -1e9f, tf = 1e9f;
                int axis = 0; float sign = 1f;
                for (int a = 0; a < 3; a++)
                {
                    float oa = a == 0 ? o.X : a == 1 ? o.Y : o.Z;
                    float da = a == 0 ? d.X : a == 1 ? d.Y : d.Z;
                    float h = a == 0 ? A : a == 1 ? B : C;
                    if (Math.Abs(da) < 1e-8f)
                    {
                        if (oa < -h || oa > h) return false;
                        continue;
                    }
                    float t1 = (-h - oa) / da, t2 = (h - oa) / da;
                    float s = -1f;
                    if (t1 > t2) { float tmp = t1; t1 = t2; t2 = tmp; s = 1f; }
                    if (t1 > tn) { tn = t1; axis = a; sign = s; }
                    if (t2 < tf) tf = t2;
                    if (tn > tf) return false;
                }
                if (tn < 1e-4f || tn >= maxT) return false;
                t = tn;
                n = axis == 0 ? new V3(sign, 0, 0) : axis == 1 ? new V3(0, sign, 0) : new V3(0, 0, sign);
                return true;
            }

            static bool HitCone(V3 o, V3 d, float r0, float r1, float h, float maxT, out float t, out V3 n)
            {
                t = maxT; n = default;
                bool any = false;
                float k = (r1 - r0) / (2f * h);
                float mm = (r0 + r1) * 0.5f;
                float a = d.X * d.X + d.Y * d.Y - k * k * d.Z * d.Z;
                float rz = k * o.Z + mm;
                float b = 2f * (o.X * d.X + o.Y * d.Y - k * d.Z * rz);
                float c = o.X * o.X + o.Y * o.Y - rz * rz;
                if (Math.Abs(a) > 1e-9f)
                {
                    float disc = b * b - 4f * a * c;
                    if (disc >= 0f)
                    {
                        float sq = (float)Math.Sqrt(disc);
                        for (int s = 0; s < 2; s++)
                        {
                            float tt = (-b + (s == 0 ? -sq : sq)) / (2f * a);
                            if (tt < 1e-4f || tt >= t) continue;
                            float z = o.Z + d.Z * tt;
                            if (z < -h || z > h) continue;
                            float r = k * z + mm;
                            if (r < 0f) continue;
                            float x = o.X + d.X * tt, y = o.Y + d.Y * tt;
                            t = tt; n = V3.Norm(new V3(x, y, -k * r)); any = true;
                        }
                    }
                }
                // Caps.
                if (Math.Abs(d.Z) > 1e-9f)
                {
                    for (int s = 0; s < 2; s++)
                    {
                        float zc = s == 0 ? -h : h;
                        float rc = s == 0 ? r0 : r1;
                        float tt = (zc - o.Z) / d.Z;
                        if (tt < 1e-4f || tt >= t) continue;
                        float x = o.X + d.X * tt, y = o.Y + d.Y * tt;
                        if (x * x + y * y > rc * rc) continue;
                        t = tt; n = new V3(0, 0, s == 0 ? -1f : 1f); any = true;
                    }
                }
                return any;
            }

            bool HitSphere(V3 o, V3 d, float maxT, out float t, out V3 n)
            {
                t = 0f; n = default;
                float b = V3.Dot(o, d);
                float c = V3.Dot(o, o) - A * A;
                float disc = b * b - c;
                if (disc < 0f) return false;
                float sq = (float)Math.Sqrt(disc);
                float tt = -b - sq;
                if (tt < 1e-4f) tt = -b + sq;
                if (tt < 1e-4f || tt >= maxT) return false;
                t = tt;
                n = V3.Norm(o + d * tt);
                return true;
            }

            /// <summary>A spherical-cap bowl: sphere radius C centred at (0,0,C), the cap from the vertex up to the aperture A.</summary>
            bool HitDish(V3 o, V3 d, float maxT, out float t, out V3 n)
            {
                t = maxT; n = default;
                var co = o - new V3(0, 0, C);
                float b = V3.Dot(co, d);
                float c = V3.Dot(co, co) - C * C;
                float disc = b * b - c;
                if (disc < 0f) return false;
                float sq = (float)Math.Sqrt(disc);
                float depth = DishDepth;
                bool any = false;
                for (int s = 0; s < 2; s++)
                {
                    float tt = s == 0 ? -b - sq : -b + sq;
                    if (tt < 1e-4f || tt >= t) continue;
                    var p = o + d * tt;
                    if (p.Z > depth || p.Z < 0f) continue;
                    t = tt; n = V3.Norm(p - new V3(0, 0, C)); any = true;
                    break;
                }
                return any;
            }
        }

        // ---- Scene building ---------------------------------------------------------------------------

        sealed class Builder
        {
            public readonly List<Prim> Parts = new List<Prim>();

            void Add(Prim p) { p.Bake(Parts.Count); Parts.Add(p); }

            public void Box(float x, float y, float z, float w, float h, float d, PayloadSurface s, float yawDeg = 0f)
            {
                float a = yawDeg * (float)Math.PI / 180f;
                var u = new V3((float)Math.Cos(a), 0, -(float)Math.Sin(a));
                var w3 = new V3((float)Math.Sin(a), 0, (float)Math.Cos(a));
                Add(new Prim { Kind = Kind.Box, O = new V3(x, y, z), U = u, V = new V3(0, 1, 0), W = w3, A = w * 0.5f, B = h * 0.5f, C = d * 0.5f, Surface = s });
            }

            /// <summary>A box whose local Z runs along <paramref name="axis"/> (a tilted panel or a slat).</summary>
            public void Slab(V3 c, V3 axis, V3 upHint, float w, float h, float d, PayloadSurface s)
            {
                Basis(V3.Norm(axis), upHint, out var u, out var v, out var ww);
                Add(new Prim { Kind = Kind.Box, O = c, U = u, V = v, W = ww, A = w * 0.5f, B = h * 0.5f, C = d * 0.5f, Surface = s });
            }

            public void Rod(V3 a, V3 b, float r, PayloadSurface s) => Cone(a, r, b, r, s);

            public void Cone(V3 a, float ra, V3 b, float rb, PayloadSurface s)
            {
                var axis = b - a;
                float len = axis.Length;
                if (len < 1e-5f) return;
                Basis(axis * (1f / len), new V3(0, 1, 0), out var u, out var v, out var w);
                Add(new Prim { Kind = Math.Abs(ra - rb) < 1e-6f ? Kind.Cylinder : Kind.Cone, O = (a + b) * 0.5f, U = u, V = v, W = w, A = ra, B = rb, C = len * 0.5f, Surface = s });
            }

            public void Ball(V3 c, float r, PayloadSurface s) =>
                Add(new Prim { Kind = Kind.Sphere, O = c, U = new V3(1, 0, 0), V = new V3(0, 1, 0), W = new V3(0, 0, 1), A = r, Surface = s });

            /// <summary>A dish with its vertex at <paramref name="vertex"/>, opening toward <paramref name="facing"/>.</summary>
            public void Dish(V3 vertex, V3 facing, float aperture, float sphereR, PayloadSurface s = PayloadSurface.Dish)
            {
                Basis(V3.Norm(facing), new V3(0, 1, 0), out var u, out var v, out var w);
                Add(new Prim { Kind = Kind.Dish, O = vertex, U = u, V = v, W = w, A = aperture, C = Math.Max(sphereR, aperture * 1.05f), Surface = s });
            }

            /// <summary>A deployed wing of <paramref name="panels"/> cell panels along X from <paramref name="root"/>, with a yoke boom.</summary>
            public void Wing(V3 root, float dir, int panels, float pw, float ph, float gap, float tiltDeg)
            {
                float t = tiltDeg * (float)Math.PI / 180f;
                var normal = new V3(0, (float)Math.Sin(t), (float)Math.Cos(t));
                var up = new V3(0, (float)Math.Cos(t), -(float)Math.Sin(t));
                float boom = gap * 2.5f;
                Rod(root, root + new V3(dir * boom, 0, 0), 0.014f, PayloadSurface.Steel);
                for (int i = 0; i < panels; i++)
                {
                    float cx = root.X + dir * (boom + pw * 0.5f + i * (pw + gap));
                    Slab(new V3(cx, root.Y, root.Z), normal, up, pw, ph, 0.022f, PayloadSurface.Cells);
                    if (i > 0)
                    {
                        float hx = root.X + dir * (boom + i * (pw + gap) - gap * 0.5f);
                        Rod(new V3(hx, root.Y - ph * 0.3f, root.Z), new V3(hx, root.Y + ph * 0.3f, root.Z), 0.009f, PayloadSurface.Steel);
                    }
                }
            }

            static void Basis(V3 w, V3 upHint, out V3 u, out V3 v, out V3 ww)
            {
                ww = w;
                var hint = Math.Abs(V3.Dot(upHint, w)) > 0.95f ? new V3(1, 0, 0) : upHint;
                u = V3.Norm(V3.Cross(hint, w));
                v = V3.Cross(w, u);
            }
        }

        static V3 P(float x, float y, float z) => new V3(x, y, z);

        const PayloadSurface Foil = PayloadSurface.Foil, Silver = PayloadSurface.SilverFoil, Cells_ = PayloadSurface.Cells,
            Bus = PayloadSurface.Bus, White = PayloadSurface.White, Steel = PayloadSurface.Steel, Orange = PayloadSurface.Orange,
            Dark = PayloadSurface.Dark, Lens = PayloadSurface.Lens;

        static readonly Dictionary<string, List<Prim>> Scenes = new Dictionary<string, List<Prim>>();

        static List<Prim> Scene(string form)
        {
            string key = PayloadForms.KnownForm(form) ? form : "box";
            lock (Scenes)
            {
                if (Scenes.TryGetValue(key, out var cached)) return cached;
                var b = new Builder();
                switch (key)
                {
                    case "cubesat": Cubesat(b); break;
                    case "bus": BusSat(b); break;
                    case "weather": Weather(b); break;
                    case "comms": Comms(b); break;
                    case "telescope": Telescope(b); break;
                    case "probe": Probe(b); break;
                    case "cargo": Cargo(b); break;
                    case "crew": Crew(b); break;
                    case "dispenser": Dispenser(b); break;
                    case "relay": Relay(b); break;
                    case "lander": Lander(b); break;
                    default: MassBox(b); break;
                }
                Scenes[key] = b.Parts;
                return b.Parts;
            }
        }

        /// <summary>Part count for a form (tests: every scene is more than a box).</summary>
        public static int PartCount(string form) => Scene(form).Count;

        /// <summary>Surfaces a form's scene uses (tests: foil, cells and a bus where the payload has them).</summary>
        public static HashSet<PayloadSurface> Surfaces(string form)
        {
            var set = new HashSet<PayloadSurface>();
            foreach (var p in Scene(form)) set.Add(p.Surface);
            return set;
        }

        // Mass simulator: a bus box half wrapped in gold MLI, an orange band, a mast and a patch antenna.
        static void MassBox(Builder b)
        {
            b.Box(0, 0, 0, 0.9f, 0.62f, 0.7f, Bus);
            b.Box(0, -0.13f, 0, 0.93f, 0.38f, 0.73f, Foil);
            b.Box(0, 0.07f, 0, 0.94f, 0.05f, 0.74f, Orange);
            b.Box(0, 0.33f, 0, 0.5f, 0.04f, 0.4f, Steel);
            b.Rod(P(0.2f, 0.35f, 0.1f), P(0.2f, 0.9f, 0.1f), 0.014f, Steel);
            b.Ball(P(0.2f, 0.91f, 0.1f), 0.03f, Orange);
            b.Dish(P(-0.2f, 0.18f, 0.36f), P(0, 0.15f, 1), 0.11f, 0.2f);
            b.Rod(P(-0.2f, 0.18f, 0.36f), P(-0.2f, 0.18f, 0.44f), 0.01f, Steel);
            b.Box(0.46f, -0.2f, 0.2f, 0.04f, 0.1f, 0.1f, Dark);
        }

        // 3U-style smallsat: cells on every face, black rails, two deployed wings, whip and tape antennas.
        static void Cubesat(Builder b)
        {
            b.Box(0, 0, 0, 0.42f, 0.66f, 0.42f, Cells_);
            for (int i = 0; i < 4; i++)
            {
                float x = (i & 1) == 0 ? -0.215f : 0.215f, z = (i & 2) == 0 ? -0.215f : 0.215f;
                b.Box(x, 0, z, 0.045f, 0.72f, 0.045f, Steel);
            }
            b.Box(0, 0.345f, 0, 0.4f, 0.03f, 0.4f, Foil);
            b.Box(0, -0.345f, 0, 0.4f, 0.03f, 0.4f, Foil);
            b.Wing(P(-0.24f, 0.12f, 0), -1f, 2, 0.4f, 0.5f, 0.02f, 18f);
            b.Wing(P(0.24f, 0.12f, 0), 1f, 2, 0.4f, 0.5f, 0.02f, 18f);
            b.Rod(P(0.1f, 0.36f, 0.1f), P(0.16f, 0.95f, 0.14f), 0.011f, Steel);
            b.Ball(P(0.16f, 0.96f, 0.14f), 0.022f, Orange);
            b.Rod(P(-0.12f, 0.36f, -0.1f), P(-0.4f, 0.75f, -0.2f), 0.008f, Steel);
            b.Box(0, 0.05f, 0.225f, 0.12f, 0.12f, 0.02f, Lens);
        }

        // Small LEO bus: foil body, white deck, a front dish, two two-panel wings and a whip.
        static void BusSat(Builder b)
        {
            b.Box(0, 0, 0, 0.6f, 0.56f, 0.6f, Foil);
            b.Box(0, 0.3f, 0, 0.62f, 0.05f, 0.62f, Bus);
            b.Box(0, -0.3f, 0, 0.5f, 0.04f, 0.5f, Steel);
            b.Wing(P(-0.3f, 0.02f, 0), -1f, 2, 0.46f, 0.42f, 0.03f, 10f);
            b.Wing(P(0.3f, 0.02f, 0), 1f, 2, 0.46f, 0.42f, 0.03f, 10f);
            b.Rod(P(0.08f, 0.05f, 0.3f), P(0.12f, 0.1f, 0.42f), 0.018f, Steel);
            b.Dish(P(0.12f, 0.1f, 0.42f), P(0.25f, 0.2f, 1), 0.2f, 0.32f);
            b.Rod(P(0.12f, 0.1f, 0.42f), P(0.15f, 0.12f, 0.56f), 0.01f, Steel);
            b.Rod(P(-0.2f, 0.32f, -0.2f), P(-0.25f, 0.85f, -0.22f), 0.011f, Steel);
            b.Box(-0.15f, -0.1f, 0.31f, 0.14f, 0.14f, 0.03f, Lens);
        }

        // Weather satellite: white bus with an MLI skirt, an imager turret with a lens and sunshade, one long wing.
        static void Weather(Builder b)
        {
            b.Box(0, 0, 0, 0.56f, 0.6f, 0.56f, Bus);
            b.Box(0, -0.16f, 0, 0.58f, 0.3f, 0.58f, Foil);
            b.Rod(P(0.04f, 0.3f, 0.04f), P(0.04f, 0.62f, 0.04f), 0.17f, White);
            b.Rod(P(0.04f, 0.5f, 0.04f), P(0.04f, 0.5f, 0.3f), 0.09f, Steel);
            b.Rod(P(0.04f, 0.5f, 0.3f), P(0.04f, 0.5f, 0.32f), 0.075f, Lens);
            b.Cone(P(0.04f, 0.5f, 0.32f), 0.09f, P(0.04f, 0.5f, 0.42f), 0.13f, Foil);
            b.Wing(P(-0.28f, 0.0f, 0), -1f, 3, 0.4f, 0.48f, 0.03f, 25f);
            b.Dish(P(0.28f, 0.2f, 0.1f), P(1, 0.4f, 0.4f), 0.14f, 0.22f);
            b.Rod(P(0.2f, -0.31f, 0.2f), P(0.2f, -0.55f, 0.24f), 0.01f, Steel);
            b.Box(0.29f, -0.05f, -0.1f, 0.02f, 0.24f, 0.2f, Dark);
        }

        // GEO comsat: tall bus in gold MLI under a white top, two big reflectors, long three-panel wings.
        static void Comms(Builder b)
        {
            b.Box(0, 0, 0, 0.62f, 0.78f, 0.6f, Foil);
            b.Box(0, 0.42f, 0, 0.64f, 0.08f, 0.62f, Bus);
            b.Box(0, 0.25f, 0.305f, 0.5f, 0.2f, 0.02f, Bus);
            b.Wing(P(-0.31f, 0.05f, 0), -1f, 3, 0.42f, 0.56f, 0.03f, 8f);
            b.Wing(P(0.31f, 0.05f, 0), 1f, 3, 0.42f, 0.56f, 0.03f, 8f);
            b.Dish(P(-0.2f, -0.05f, 0.32f), P(-0.45f, 0.05f, 1), 0.3f, 0.48f);
            b.Dish(P(0.2f, 0.0f, 0.32f), P(0.45f, 0.2f, 1), 0.27f, 0.44f);
            b.Cone(P(-0.08f, 0.32f, 0.31f), 0.03f, P(-0.24f, 0.18f, 0.55f), 0.05f, Steel);
            b.Cone(P(0.08f, 0.33f, 0.31f), 0.03f, P(0.26f, 0.24f, 0.52f), 0.045f, Steel);
            b.Dish(P(0, 0.46f, 0.1f), P(0.1f, 1, 0.3f), 0.13f, 0.2f);
            b.Rod(P(0.2f, 0.46f, -0.2f), P(0.22f, 0.8f, -0.22f), 0.01f, Steel);
        }

        // Space telescope: silver-foil tube with an open aperture door, gold aft bus, two wings and high-gain antennas.
        static void Telescope(Builder b)
        {
            b.Rod(P(0, -0.35f, 0), P(0, 0.75f, 0), 0.3f, Silver);
            b.Rod(P(0, 0.745f, 0), P(0, 0.755f, 0), 0.27f, Dark);
            b.Rod(P(0, 0.25f, 0), P(0, 0.3f, 0), 0.305f, Steel);
            b.Rod(P(0, -0.72f, 0), P(0, -0.35f, 0), 0.33f, Foil);
            b.Rod(P(0, -0.76f, 0), P(0, -0.72f, 0), 0.25f, Steel);
            b.Slab(P(0, 0.95f, -0.2f), V3.Norm(P(0, 0.35f, 1)), P(0, 1, 0), 0.6f, 0.44f, 0.03f, White);
            b.Rod(P(0, 0.76f, -0.28f), P(0, 0.78f, -0.27f), 0.03f, Steel);
            b.Wing(P(-0.3f, -0.2f, 0), -1f, 2, 0.34f, 0.7f, 0.025f, 0f);
            b.Wing(P(0.3f, -0.2f, 0), 1f, 2, 0.34f, 0.7f, 0.025f, 0f);
            b.Rod(P(0.2f, -0.6f, 0.25f), P(0.35f, -0.7f, 0.5f), 0.012f, Steel);
            b.Dish(P(0.35f, -0.7f, 0.5f), P(0.3f, -0.2f, 1), 0.12f, 0.18f);
            b.Rod(P(-0.2f, -0.6f, 0.25f), P(-0.35f, -0.75f, 0.45f), 0.012f, Steel);
            b.Dish(P(-0.35f, -0.75f, 0.45f), P(-0.4f, -0.2f, 1), 0.11f, 0.17f);
        }

        // Deep-space probe: gold bus, big high-gain dish with a feed, an RTG on a boom and a long magnetometer whip.
        static void Probe(Builder b)
        {
            b.Rod(P(0, -0.22f, 0), P(0, 0.08f, 0), 0.3f, Foil);
            b.Rod(P(0, 0.08f, 0), P(0, 0.12f, 0), 0.24f, Steel);
            b.Dish(P(0, 0.12f, 0.02f), P(0, 1, 0.3f), 0.62f, 0.85f);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.0944f;
                b.Rod(P((float)Math.Cos(a) * 0.45f, 0.24f + (float)Math.Sin(a) * 0.12f, (float)Math.Sin(a) * 0.4f + 0.06f), P(0, 0.6f, 0.17f), 0.008f, Steel);
            }
            b.Cone(P(0, 0.55f, 0.15f), 0.04f, P(0, 0.66f, 0.19f), 0.07f, Orange);
            b.Rod(P(0.25f, -0.1f, 0.1f), P(0.95f, -0.32f, 0.3f), 0.016f, Steel);
            b.Rod(P(0.88f, -0.3f, 0.28f), P(1.12f, -0.38f, 0.35f), 0.07f, Dark);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 1.5708f + 0.4f;
                var c = P(1.0f, -0.34f, 0.315f);
                b.Slab(c + P(0, (float)Math.Cos(a) * 0.09f, (float)Math.Sin(a) * 0.09f), V3.Norm(P(0.95f, -0.3f, 0.28f)), P(0, (float)Math.Cos(a), (float)Math.Sin(a)), 0.012f, 0.08f, 0.22f, Dark);
            }
            b.Rod(P(-0.25f, -0.1f, -0.05f), P(-1.05f, -0.02f, -0.3f), 0.007f, Steel);
            b.Box(-0.18f, -0.28f, 0.28f, 0.16f, 0.12f, 0.12f, Bus);
            b.Rod(P(-0.18f, -0.28f, 0.34f), P(-0.18f, -0.28f, 0.38f), 0.035f, Lens);
        }

        // Cargo capsule: white pressurised hull, a nose cone and berthing ring, cell-wrapped trunk with fins.
        static void Cargo(Builder b)
        {
            b.Rod(P(0, -0.25f, 0), P(0, 0.35f, 0), 0.38f, White);
            b.Cone(P(0, 0.35f, 0), 0.38f, P(0, 0.62f, 0), 0.2f, White);
            b.Rod(P(0, 0.62f, 0), P(0, 0.7f, 0), 0.15f, Steel);
            b.Rod(P(0, 0.7f, 0), P(0, 0.72f, 0), 0.17f, Orange);
            b.Rod(P(0, -0.3f, 0), P(0, -0.25f, 0), 0.395f, Foil);
            b.Rod(P(0, -0.85f, 0), P(0, -0.3f, 0), 0.4f, Cells_);
            b.Slab(P(0.5f, -0.6f, 0), P(0, 0, 1), P(0, 1, 0), 0.22f, 0.4f, 0.02f, White);
            b.Slab(P(-0.5f, -0.6f, 0), P(0, 0, 1), P(0, 1, 0), 0.22f, 0.4f, 0.02f, White);
            b.Box(0.1f, 0.08f, 0.37f, 0.16f, 0.2f, 0.03f, Dark);
            b.Ball(P(-0.16f, 0.2f, 0.34f), 0.045f, Lens);
        }

        // Crew capsule: white cone with windows and a dark heat shield, a docking nose, a cell-wrapped trunk.
        static void Crew(Builder b)
        {
            b.Cone(P(0, -0.25f, 0), 0.48f, P(0, 0.35f, 0), 0.21f, White);
            b.Rod(P(0, -0.3f, 0), P(0, -0.25f, 0), 0.49f, Dark);
            b.Rod(P(0, 0.35f, 0), P(0, 0.45f, 0), 0.13f, Steel);
            b.Rod(P(0, 0.45f, 0), P(0, 0.47f, 0), 0.15f, Orange);
            b.Ball(P(0.13f, 0.12f, 0.29f), 0.05f, Lens);
            b.Ball(P(-0.13f, 0.12f, 0.29f), 0.05f, Lens);
            b.Ball(P(0.31f, 0.0f, 0.12f), 0.045f, Lens);
            b.Rod(P(0, -0.8f, 0), P(0, -0.3f, 0), 0.47f, Cells_);
            b.Slab(P(0.55f, -0.62f, 0.0f), P(0, 0, 1), P(0, 1, 0), 0.18f, 0.32f, 0.02f, White);
            b.Slab(P(-0.55f, -0.62f, 0.0f), P(0, 0, 1), P(0, 1, 0), 0.18f, 0.32f, 0.02f, White);
            b.Box(0.0f, -0.05f, 0.41f, 0.12f, 0.16f, 0.03f, Orange);
        }

        // Smallsat dispenser: gold platform on an adapter ring, four pods with orange doors, one door open.
        static void Dispenser(Builder b)
        {
            b.Rod(P(0, -0.62f, 0), P(0, -0.5f, 0), 0.42f, Steel);
            b.Box(0, -0.45f, 0, 0.92f, 0.08f, 0.92f, Foil);
            for (int i = 0; i < 4; i++)
            {
                float x = (i & 1) == 0 ? -0.22f : 0.22f, z = (i & 2) == 0 ? -0.22f : 0.22f;
                b.Box(x, -0.06f, z, 0.36f, 0.7f, 0.36f, Bus);
                if (z > 0f && x < 0f) b.Slab(P(x - 0.1f, -0.06f, z + 0.3f), V3.Norm(P(-1, 0, 1.2f)), P(0, 1, 0), 0.32f, 0.62f, 0.02f, Orange);
                else if (z > 0f) b.Box(x, -0.06f, z + 0.19f, 0.32f, 0.62f, 0.02f, Orange);
                b.Box(x, 0.3f, z, 0.37f, 0.03f, 0.37f, Steel);
            }
            b.Box(-0.22f, -0.04f, 0.4f, 0.2f, 0.2f, 0.2f, Cells_);
            b.Rod(P(0.44f, -0.4f, 0.44f), P(0.44f, 0.32f, 0.44f), 0.015f, Steel);
            b.Rod(P(-0.44f, -0.4f, -0.44f), P(-0.44f, 0.32f, -0.44f), 0.015f, Steel);
        }

        // Relay: gold bus, one big reflector on a mast, two-panel wings, three whips.
        static void Relay(Builder b)
        {
            b.Box(0, 0, 0, 0.5f, 0.5f, 0.5f, Foil);
            b.Box(0, 0.27f, 0, 0.52f, 0.04f, 0.52f, Bus);
            b.Rod(P(0, 0.29f, 0), P(0, 0.42f, 0.05f), 0.03f, Steel);
            b.Dish(P(0, 0.42f, 0.05f), P(0.15f, 0.8f, 0.6f), 0.55f, 0.75f);
            b.Rod(P(0.2f, 0.45f, 0.3f), P(0.08f, 0.82f, 0.38f), 0.008f, Steel);
            b.Rod(P(-0.2f, 0.45f, 0.3f), P(0.08f, 0.82f, 0.38f), 0.008f, Steel);
            b.Cone(P(0.08f, 0.8f, 0.37f), 0.03f, P(0.06f, 0.72f, 0.33f), 0.06f, Orange);
            b.Wing(P(-0.25f, -0.02f, 0), -1f, 2, 0.4f, 0.4f, 0.03f, 15f);
            b.Wing(P(0.25f, -0.02f, 0), 1f, 2, 0.4f, 0.4f, 0.03f, 15f);
            b.Rod(P(-0.2f, -0.25f, 0.2f), P(-0.3f, -0.7f, 0.3f), 0.009f, Steel);
            b.Rod(P(0.2f, -0.25f, 0.2f), P(0.32f, -0.7f, 0.28f), 0.009f, Steel);
            b.Rod(P(0.2f, -0.25f, -0.2f), P(0.3f, -0.68f, -0.3f), 0.009f, Steel);
        }

        // Lander: gold body, white deck with a cell panel and a small dish, tanks, an engine bell, four legs and pads.
        static void Lander(Builder b)
        {
            b.Rod(P(0, -0.15f, 0), P(0, 0.2f, 0), 0.4f, Foil);
            b.Rod(P(0, 0.2f, 0), P(0, 0.24f, 0), 0.42f, Bus);
            b.Slab(P(-0.1f, 0.27f, 0.02f), P(0, 1, 0), P(0, 0, -1), 0.4f, 0.3f, 0.02f, Cells_);
            b.Ball(P(0.24f, 0.32f, -0.12f), 0.11f, Steel);
            b.Rod(P(0.16f, 0.24f, 0.18f), P(0.16f, 0.38f, 0.2f), 0.012f, Steel);
            b.Dish(P(0.16f, 0.38f, 0.2f), P(0.3f, 1, 0.5f), 0.1f, 0.16f);
            b.Cone(P(0, -0.15f, 0), 0.08f, P(0, -0.38f, 0), 0.17f, Dark);
            for (int i = 0; i < 4; i++)
            {
                float a = 0.7854f + i * 1.5708f;
                float cx = (float)Math.Cos(a), cz = (float)Math.Sin(a);
                var hip = P(cx * 0.36f, -0.08f, cz * 0.36f);
                var foot = P(cx * 0.7f, -0.58f, cz * 0.7f);
                b.Rod(hip, foot, 0.022f, Steel);
                b.Rod(P(cx * 0.3f, 0.12f, cz * 0.3f), foot + P(0, 0.12f, 0) * 0.5f, 0.012f, Steel);
                b.Rod(foot + P(0, -0.02f, 0), foot + P(0, 0.02f, 0), 0.09f, Foil);
            }
        }

        // ---- Small math -------------------------------------------------------------------------------

        struct C4 { public float R, G, B, A; }

        static byte Byte(float v)
        {
            int i = (int)(v * 255f + 0.5f);
            return (byte)(i < 0 ? 0 : i > 255 ? 255 : i);
        }

        static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;
        static float Frac(float x) => x - (float)Math.Floor(x);
        static float Pow(float x, float p) => x <= 0f ? 0f : (float)Math.Pow(x, p);

        static float Smooth(float a, float b, float x)
        {
            float t = (x - a) / (b - a);
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            return t * t * (3f - 2f * t);
        }

        readonly struct V3
        {
            public readonly float X, Y, Z;
            public V3(float x, float y, float z) { X = x; Y = y; Z = z; }
            public float Length => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
            public static V3 operator +(V3 a, V3 b) => new V3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
            public static V3 operator -(V3 a, V3 b) => new V3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
            public static V3 operator -(V3 a) => new V3(-a.X, -a.Y, -a.Z);
            public static V3 operator *(V3 a, float s) => new V3(a.X * s, a.Y * s, a.Z * s);
            public static float Dot(V3 a, V3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
            public static V3 Cross(V3 a, V3 b) => new V3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
            public static V3 Mul(V3 a, V3 b) => new V3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
            public static V3 Min(V3 a, V3 b) => new V3(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
            public static V3 Max(V3 a, V3 b) => new V3(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));
            public static V3 Lerp(V3 a, V3 b, float t) => a + (b - a) * t;
            public static V3 Norm(V3 a)
            {
                float l = a.Length;
                return l > 1e-9f ? a * (1f / l) : new V3(0, 1, 0);
            }
        }
    }
}
