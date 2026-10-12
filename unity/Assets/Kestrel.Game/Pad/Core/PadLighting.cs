// D5 pad lighting plan (engine-free). Procedural and fictional: flood banks on the tower's vehicle face,
// light poles in apron rings (PAD_DETAIL_GRAMMAR §8: every 30–60 m), aviation obstruction lights on the
// tower cap and every third level. Each fixture is an emissive mesh (always, all tiers); only the
// brightest floods up to KitBudgets.PadRealtimeLights(q) also get a realtime light.
// Tower positions use the same frame as TowerAnchors (#135): level deck y = (l+1)·LevelHeight, s face z = NearFaceZ.
using System;
using System.Collections.Generic;
using Kestrel.Kit;
using Kestrel.Kit.Contracts;

namespace Kestrel.Pad
{
    public enum LightKind { Flood, Pole, Aviation, DeckStrip }

    public struct LightSpec
    {
        public string Id; public LightKind Kind;
        public Vec3 Position;          // pad space, fixture face
        public Vec3 Aim;               // unit direction the light points (Aviation/DeckStrip: up/omni)
        public Vec3 Color;             // linear RGB, 0..1
        public float Intensity;        // emissive/realtime multiplier at full night
        public float Range, SpotAngle; // realtime light + fake volumetric cone
        public float InnerSpotAngle;   // realtime spot falloff start (deg)
        public float Throw;            // m from fixture to what it lights; realtime intensity = Illum · Throw² (inverse-square)
        public float Illum;            // target illuminance at Throw (URP light units, sun ≈ 1–2)
        public float Halo;             // billboard flare diameter (m) at full night; 0 = none
        public float BlinkHz;          // 0 = steady
        public bool Realtime, Shadows;
    }

    public static class PadLighting
    {
        public static readonly Vec3 Sodium = new Vec3(1.00f, 0.78f, 0.52f), Led = new Vec3(0.92f, 0.95f, 1.00f), AvRed = new Vec3(1f, 0.08f, 0.04f);

        /// <summary>Build the plan. Deterministic for (layout, quality).</summary>
        public static List<LightSpec> Plan(PadLayout L, int quality)
        {
            var o = new List<LightSpec>(); var rng = new PadRng(L.Seed * 31 + 7);
            var target = new Vec3(0, L.Fit.VehicleHeight * 0.45f, 0);
            float hv = Math.Max(10f, L.Fit.VehicleHeight);
            var warm = rng.Next() % 2 == 0;     // fictional pad picks one palette
            var floodCol = warm ? Sodium : Led;

            var tp = L.Tower;
            if (tp != null)
            {
                float hw = tp.Width * 0.5f;
                for (int l = 1; l < tp.Levels; l += 3)
                {
                    float y = (l + 1) * tp.LevelHeight;
                    foreach (var sx in new[] { -1f, 1f })
                    {
                        var p = new Vec3(sx * (hw - 0.6f), y + 0.4f, tp.NearFaceZ - 0.3f);
                        float d = Dist(p, target);
                        o.Add(new LightSpec { Id = $"flood.tower.l{l:00}.{(sx < 0 ? "w" : "e")}", Kind = LightKind.Flood, Position = p, Aim = Dir(p, target),
                            Color = floodCol, Intensity = 6f, Throw = d, Illum = 2.2f, Range = d * 1.8f, SpotAngle = 34f, InnerSpotAngle = 16f, Halo = 4.5f });
                    }
                }
                float top = tp.Height + tp.CapHeight;
                o.Add(new LightSpec { Id = "av.tower.cap", Kind = LightKind.Aviation, Position = new Vec3(0, top + 0.3f, tp.StandOff), Aim = new Vec3(0, 1, 0), Color = AvRed, Intensity = 8f, BlinkHz = 0.5f, Halo = 3.5f });
                for (int l = 2; l < tp.Levels; l += 3)
                    for (int c = 0; c < 4; c++)
                    {
                        float x = (c == 0 || c == 3) ? -hw : hw, z = (c < 2) ? tp.StandOff - hw : tp.StandOff + hw;
                        o.Add(new LightSpec { Id = $"av.tower.l{l:00}.{c}", Kind = LightKind.Aviation, Position = new Vec3(x, (l + 1) * tp.LevelHeight, z), Aim = new Vec3(0, 1, 0), Color = AvRed, Intensity = 3f, Halo = 1.6f });
                    }
                // deck strip: low walkway glow along each level's vehicle face (one fixture per level)
                for (int l = 0; l < tp.Levels; l++)
                    o.Add(new LightSpec { Id = $"strip.tower.l{l:00}", Kind = LightKind.DeckStrip, Position = new Vec3(0, (l + 1) * tp.LevelHeight + 0.15f, tp.NearFaceZ - 0.2f), Aim = new Vec3(0, 0, -1), Color = Led, Intensity = 1.2f, Halo = 0f });
            }

            // Pole rings: ring 2 at 0.5–1.5 H_v (apron), ring 3 sparse. Skip the plume keep-out (exit bearings ±90° from tower ±25°).
            for (int ring = 0; ring < 2; ring++)
            {
                float r = Math.Max(L.DeckRadius + 8f, hv * (ring == 0 ? rng.Range(0.6f, 0.9f) : rng.Range(1.2f, 1.5f)));
                float spacing = ring == 0 ? rng.Range(30f, 45f) : rng.Range(45f, 60f);
                int n = Math.Max(4, (int)Math.Round(2 * Math.PI * r / spacing)); float ph = rng.Range(0f, 360f);
                float poleH = ring == 0 ? 18f : 14f;
                for (int i = 0; i < n; i++)
                {
                    float b = ph + 360f * i / n; float bb = ((b % 360f) + 360f) % 360f;
                    if (Math.Abs(bb - 90f) < 25f || Math.Abs(bb - 270f) < 25f) continue;
                    double br = b * Math.PI / 180.0;
                    var p = new Vec3(r * (float)Math.Sin(br), poleH, r * (float)Math.Cos(br));
                    var foot = new Vec3(p.X * 0.3f, 0, p.Z * 0.3f); float d = Dist(p, foot);
                    o.Add(new LightSpec { Id = $"pole.r{ring + 2}.{i:00}", Kind = LightKind.Pole, Position = p, Aim = Dir(p, foot),
                        Color = floodCol, Intensity = ring == 0 ? 4f : 3f, Throw = d, Illum = 1.2f, Range = d * 1.6f, SpotAngle = 64f, InnerSpotAngle = 30f, Halo = ring == 0 ? 3.5f : 2.8f });
                }
            }

            // Realtime budget: brightest floods first (stable by id), then poles.
            int budget = KitBudgets.PadRealtimeLights(quality), shadows = KitBudgets.PadShadowedLights(quality);
            var order = new List<int>(); for (int i = 0; i < o.Count; i++) if (o[i].Kind == LightKind.Flood || o[i].Kind == LightKind.Pole) order.Add(i);
            order.Sort((a, b) => { int k = o[a].Kind.CompareTo(o[b].Kind); if (k != 0) return k; int c = o[b].Position.Y.CompareTo(o[a].Position.Y); return c != 0 ? c : string.CompareOrdinal(o[a].Id, o[b].Id); });
            // Balance: never put more realtime floods on one side of the vehicle than the other (+1); fill leftovers after.
            int left = 0, right = 0;
            for (int pass = 0; pass < 2; pass++)
                foreach (var i in order)
                {
                    if (budget <= 0) break; var s = o[i]; if (s.Realtime) continue;
                    bool isLeft = s.Position.X < 0;
                    if (pass == 0 && s.Kind == LightKind.Flood && (isLeft ? left > right : right > left)) continue;
                    if (s.Kind == LightKind.Flood) { if (isLeft) left++; else right++; }
                    s.Realtime = true; s.Shadows = shadows-- > 0; o[i] = s; budget--;
                }
            return o;
        }

        public static int CountRealtime(List<LightSpec> plan) { int n = 0; foreach (var s in plan) if (s.Realtime) n++; return n; }

        static float Dist(Vec3 a, Vec3 b) { float x = b.X - a.X, y = b.Y - a.Y, z = b.Z - a.Z; return (float)Math.Sqrt(x * x + y * y + z * z); }

        /// <summary>Realtime intensity giving Illum at Throw under inverse-square falloff, scaled by night.</summary>
        public static float RealtimeIntensity(LightSpec s, float night) => night * s.Illum * Math.Max(1f, s.Throw * s.Throw);

        /// <summary>Fake volumetric cones per tier: Low none, Medium floods, High floods + poles.</summary>
        public static bool HasCone(LightKind k, int quality) => quality >= 2 ? (k == LightKind.Flood || k == LightKind.Pole) : quality == 1 && k == LightKind.Flood;

        static Vec3 Dir(Vec3 a, Vec3 b)
        {
            float x = b.X - a.X, y = b.Y - a.Y, z = b.Z - a.Z, m = (float)Math.Sqrt(x * x + y * y + z * z);
            return m < 1e-5f ? new Vec3(0, -1, 0) : new Vec3(x / m, y / m, z / m);
        }

        /// <summary>0 at day, 1 at night. Sun elevation in degrees; ramps over civil twilight (+6° .. -6°).</summary>
        public static float NightFactor(float sunElevationDeg) => Math.Max(0f, Math.Min(1f, (6f - sunElevationDeg) / 12f));

        /// <summary>Square-wave blink for obstruction lights (on 40 % of the period), steady when hz = 0.</summary>
        public static float Blink(float hz, double t) => hz <= 0 ? 1f : ((t * hz) % 1.0) < 0.4 ? 1f : 0.05f;
    }
}
