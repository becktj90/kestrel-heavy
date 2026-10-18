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
    public enum LightKind { Flood, Pole, Aviation, DeckStrip, Spot, Window }

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
        /// <summary>Target illuminance of tower floods / poles at their throw distance (URP units; daylight sun ≈ 1–2).</summary>
        public const float FloodIllum = 4.5f, PoleIllum = 1.6f, SpotIllum = 6f;
        /// <summary>Spotlight ring bearings (deg, 0 = tower): flank the tower, back side, away from plume exits and the design camera.</summary>
        public static readonly float[] SpotSlots = { 50f, 310f, 130f, 165f, 150f, 325f };
        /// <summary>Near-pole exclusion around the design camera bearing (deg each side).</summary>
        public const float CameraKeepOutDeg = 40f;
        public static readonly Vec3 Sodium = new Vec3(1.00f, 0.78f, 0.52f), Led = new Vec3(0.92f, 0.95f, 1.00f), AvRed = new Vec3(1f, 0.08f, 0.04f);

        /// <summary>Build the plan. Deterministic for (layout, quality).</summary>
        public static List<LightSpec> Plan(PadLayout L, int quality)
        {
            var o = new List<LightSpec>(); var rng = new PadRng(L.Seed * 31 + 7);
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
                        // aim each bank at the vehicle a little below its own height, so the banks together wash the
                        // whole stack (and the tower face they hang on) instead of all hitting one spot
                        var aim = new Vec3(0, Math.Max(hv * 0.08f, Math.Min(hv * 0.92f, y - tp.LevelHeight * 1.5f)), 0);
                        float d = Dist(p, aim);
                        o.Add(new LightSpec { Id = $"flood.tower.l{l:00}.{(sx < 0 ? "w" : "e")}", Kind = LightKind.Flood, Position = p, Aim = Dir(p, aim),
                            Color = floodCol, Intensity = 8f, Throw = d, Illum = FloodIllum, Range = d * 2.2f, SpotAngle = 44f, InnerSpotAngle = 22f, Halo = 5f });
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

            // Ground-mounted spotlights around the deck, aimed up at the vehicle (night: the beams light the rocket).
            // Bearings flank the tower and the back, clear of the plume exits (90/270 ±25°) and the design camera.
            {
                int n = L.Archetype == PadArchetype.TowerStrongback ? 6 : 4; float rs = L.DeckRadius + 4f;
                float aimY = hv * 0.55f;
                for (int i = 0; i < n; i++)
                {
                    float b = SpotSlots[i] + rng.Range(-5f, 5f); double br = b * Math.PI / 180.0;
                    var p = new Vec3(rs * (float)Math.Sin(br), 0.8f, rs * (float)Math.Cos(br));
                    var aimPt = new Vec3(0, aimY, 0); float d = Dist(p, aimPt);
                    o.Add(new LightSpec { Id = $"spot.{i:00}", Kind = LightKind.Spot, Position = p, Aim = Dir(p, aimPt), Color = Led, Intensity = 7f, Throw = d, Illum = SpotIllum,
                        Range = d * 1.6f, SpotAngle = 26f, InnerSpotAngle = 11f, Halo = 3.2f });
                }
            }
            // Emissive windows on the crew-access arm room (generic lit cabin glass).
            if (tp != null && tp.CrewLevel >= 0 && tp.CrewArmReach > 0.5f)
            {
                float wy = (tp.CrewLevel + 1) * tp.LevelHeight + 1.3f, wz = tp.NearFaceZ - tp.CrewArmReach + 0.2f;
                for (int w = -1; w <= 1; w++)
                    o.Add(new LightSpec { Id = $"win.crew.{w + 1}", Kind = LightKind.Window, Position = new Vec3(tp.CrewArmX + w * 0.5f, wy, wz), Aim = new Vec3(0, 0, -1), Color = new Vec3(1f, 0.82f, 0.55f), Intensity = 2.5f });
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
                    if (ring == 0 && AngleDiff(bb, PadGenerator.DefaultViewBearingDeg) < CameraKeepOutDeg) continue;   // don't stand in front of the design camera
                    double br = b * Math.PI / 180.0;
                    var p = new Vec3(r * (float)Math.Sin(br), poleH, r * (float)Math.Cos(br));
                    // near ring lights the tower + vehicle base, far ring the apron
                    var foot = ring == 0 && tp != null ? new Vec3(0, 2f, tp.StandOff * 0.5f) : new Vec3(p.X * 0.3f, 0, p.Z * 0.3f); float d = Dist(p, foot);
                    o.Add(new LightSpec { Id = $"pole.r{ring + 2}.{i:00}", Kind = LightKind.Pole, Position = p, Aim = Dir(p, foot),
                        Color = floodCol, Intensity = ring == 0 ? 4f : 3f, Throw = d, Illum = PoleIllum, Range = d * 1.6f, SpotAngle = 64f, InnerSpotAngle = 30f, Halo = ring == 0 ? 3.5f : 2.8f });
                }
            }

            // Realtime budget (fixtures only; the plume light is reserved separately): spots first (they light the rocket),
            // then tower floods, then poles. Each kind is balanced left/right of the vehicle.
            int budget = Math.Max(0, KitBudgets.PadRealtimeLights(quality) - KitBudgets.PadPlumeLights(quality)), shadows = KitBudgets.PadShadowedLights(quality);
            var kinds = new[] { LightKind.Spot, LightKind.Flood, LightKind.Pole };
            for (int ki = 0; ki < kinds.Length && budget > 0; ki++)
            {
                var kind = kinds[ki]; int quota = kind == LightKind.Spot ? (budget + 1) / 2 : budget;
                var order = new List<int>(); for (int i = 0; i < o.Count; i++) if (o[i].Kind == kind) order.Add(i);
                order.Sort((a, b) => { int c = o[b].Position.Y.CompareTo(o[a].Position.Y); return c != 0 ? c : string.CompareOrdinal(o[a].Id, o[b].Id); });
                int left = 0, right = 0;
                for (int pass = 0; pass < 2; pass++)
                    foreach (var i in order)
                    {
                        if (budget <= 0 || quota <= 0) break; var s = o[i]; if (s.Realtime) continue;
                        bool isLeft = s.Position.X < 0;
                        if (pass == 0 && kind != LightKind.Pole && (isLeft ? left > right : right > left)) continue;
                        if (isLeft) left++; else right++;
                        s.Realtime = true; s.Shadows = shadows-- > 0; o[i] = s; budget--; quota--;
                    }
            }
            return o;
        }

        public static int CountRealtime(List<LightSpec> plan) { int n = 0; foreach (var s in plan) if (s.Realtime) n++; return n; }

        public static float AngleDiff(float a, float b) { float d = Math.Abs(((a - b) % 360f + 360f) % 360f); return d > 180f ? 360f - d : d; }

        static float Dist(Vec3 a, Vec3 b) { float x = b.X - a.X, y = b.Y - a.Y, z = b.Z - a.Z; return (float)Math.Sqrt(x * x + y * y + z * z); }

        /// <summary>Realtime intensity giving Illum at Throw under inverse-square falloff, scaled by night.</summary>
        public static float RealtimeIntensity(LightSpec s, float night) => night * s.Illum * Math.Max(1f, s.Throw * s.Throw);

        /// <summary>Fake volumetric cones per tier: Low none, Medium floods, High floods + poles.</summary>
        public static bool HasCone(LightKind k, int quality) => quality >= 2 ? (k == LightKind.Flood || k == LightKind.Pole || k == LightKind.Spot) : quality == 1 && (k == LightKind.Flood || k == LightKind.Spot);

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
