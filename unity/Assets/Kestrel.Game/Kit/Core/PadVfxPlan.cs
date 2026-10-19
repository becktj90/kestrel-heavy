// Engine-free pad ignition picture: deluge spray, steam where that water meets the jet,
// a ground-hugging blast along the trench, and shock diamonds in the deflected plume.
// Pools are per quality tier. Nothing here is a flash: no light, alpha stays low, steam
// takes more than a second to build. Reduce-motion keeps a dim steady share.
using System;

namespace Kestrel.Kit
{
    public static class PadVfxBudgets
    {
        public static int Spray(int q) => q <= 0 ? 12 : q == 1 ? 24 : 40;
        public static int Steam(int q) => q <= 0 ? 8 : q == 1 ? 16 : 32;
        public static int Blast(int q) => q <= 0 ? 6 : q == 1 ? 14 : 24;
        /// <summary>Shock diamonds in the trench jet. Low matches the basic plume: none.</summary>
        public static int Diamonds(int q) => q <= 0 ? 0 : q == 1 ? 4 : 8;
        public static int Birds(int q) => q <= 0 ? 6 : q == 1 ? 10 : 14;
        public static int PadBirds(int q) => q <= 0 ? 4 : q == 1 ? 6 : 8;
        public static int SkyBirds(int q) => q <= 0 ? 2 : q == 1 ? 4 : 6;
    }

    /// <summary>How hard each ignition layer draws, 0..1. Pure function of the flight state.</summary>
    public static class PadVfxPlan
    {
        public const float SteamRiseSeconds = 1.4f;
        public const float SteamAlpha = 0.42f;
        public const float SprayAlpha = 0.48f;
        public const float BlastAlpha = 0.38f;
        public const float DiamondAlpha = 0.5f;
        public const float ReduceShare = 0.35f;

        public static float Spray(float throttle, bool deluge, bool reduce)
        {
            if (!deluge || !(throttle > 0.02f)) return 0f;
            float t = throttle > 1f ? 1f : throttle;
            return reduce ? t * ReduceShare : t;
        }

        /// <summary>Steam builds after ignition. A step at t = 0 is not a flash.</summary>
        public static float Steam(float throttle, float secondsSinceIgnition, bool deluge, bool reduce)
        {
            if (!deluge || !(throttle > 0.02f)) return 0f;
            float s = secondsSinceIgnition < 0f ? 0f : secondsSinceIgnition;
            float rise = s / SteamRiseSeconds;
            if (rise > 1f) rise = 1f;
            float t = throttle > 1f ? 1f : throttle;
            float a = t * rise;
            return reduce ? a * ReduceShare : a;
        }

        /// <summary>Horizontal blast on the deck. Full on the pad, gone once the jet clears the tower.</summary>
        public static float Blast(float throttle, float height, float towerHeight, bool reduce)
        {
            if (!(throttle > 0.02f)) return 0f;
            float fade = towerHeight < 12f ? 80f : towerHeight;
            float u = (height < 0f ? 0f : height) / fade;
            if (u > 1f) u = 1f;
            float t = throttle > 1f ? 1f : throttle;
            float a = t * (1f - u * u);
            return reduce ? a * ReduceShare : a;
        }

        /// <summary>Sea-level over-expansion fades toward ideal as the stack climbs. Diamonds need x &gt; 1.15.</summary>
        public static float PressureRatio(float height)
        {
            float h = height < 0f ? 0f : height;
            float u = h / 8000f;
            if (u > 1f) u = 1f;
            return 2.4f - 1.4f * u;
        }

        /// <summary>Mach-diamond spacing along an over-expanded jet. About 0.9 D √(pe/pa).</summary>
        public static float DiamondSpacing(float exitDiameter, float pressureRatio)
        {
            float d = exitDiameter < 0.3f ? 0.3f : exitDiameter;
            float x = pressureRatio < 1f ? 1f : pressureRatio;
            if (x > 8f) x = 8f;
            return 0.9f * d * (float)Math.Sqrt(x);
        }

        public static int DiamondCount(int quality, float pressureRatio)
        {
            if (quality <= 0 || !(pressureRatio > 1.15f)) return 0;
            return PadVfxBudgets.Diamonds(quality);
        }
    }

    /// <summary>One pad's ignition inputs. The pad writes it; the particle view reads it. No per-frame allocation.</summary>
    public sealed class PadVfxState
    {
        public const int MaxNozzles = 16;
        /// <summary>Child under the pad. Particle and bird views parent to it so a floating-origin rebase carries them.</summary>
        public const string AnchorName = "KH_PadVfx";
        public static readonly PadVfxState Current = new PadVfxState();

        public float Throttle, Height, TowerHeight = 80f, ExitDiameter = 1.5f, BearingDeg;
        public bool Deluge;
        public int Nozzles;
        public readonly float[] X = new float[MaxNozzles];
        public readonly float[] Y = new float[MaxNozzles];
        public readonly float[] Z = new float[MaxNozzles];
        public readonly float[] DirX = new float[MaxNozzles];
        public readonly float[] DirZ = new float[MaxNozzles];

        public void SetNozzle(int i, float x, float y, float z, float dx, float dz)
        {
            if (i < 0 || i >= MaxNozzles) return;
            X[i] = x; Y[i] = y; Z[i] = z; DirX[i] = dx; DirZ[i] = dz;
        }

        public void ClearFlight() { Throttle = 0f; Height = 0f; }
    }

    /// <summary>
    /// Ambient gulls. The web game drew a dark M wing inside a thin amber ring; these are that
    /// mark, flown around the pad and a little higher in the sky. A 1.05 m span keeps the rocket huge.
    /// Ignition flushes the pad birds. The sky birds stay, then leave before they become specks.
    /// </summary>
    public struct SkyBirdPose
    {
        public float X, Y, Z, Yaw, Span, Presence;
    }

    public static class SkyBirds
    {
        public const int PadCount = 8;
        public const int SkyCount = 6;
        public const int Count = PadCount + SkyCount;
        public const float WingSpan = 1.05f;
        public const float FlushAlt = 1.2f;
        public const float PadGoneAlt = 140f;
        public const float SkyHoldAlt = 400f;
        public const float SkyGoneAlt = 2500f;

        public static bool IsSky(int i) => i >= PadCount;

        public static void Evaluate(float time, float altitude, float flapScale, int quality, SkyBirdPose[] dst)
        {
            if (dst == null || dst.Length < Count) throw new ArgumentException("pose buffer");
            int q = quality < 0 ? 0 : quality > 2 ? 2 : quality;
            int padN = PadVfxBudgets.PadBirds(q), skyN = PadVfxBudgets.SkyBirds(q);
            float flap = flapScale < 0f ? 0f : flapScale > 1f ? 1f : flapScale;
            for (int i = 0; i < Count; i++)
            {
                bool sky = IsSky(i);
                int slot = sky ? i - PadCount : i;
                bool drawn = sky ? slot < skyN : slot < padN;
                Pose(i, slot, sky, time, altitude, flap, drawn, out dst[i]);
            }
        }

        static void Pose(int i, int slot, bool sky, float time, float altitude, float flap, bool drawn, out SkyBirdPose p)
        {
            p = new SkyBirdPose();
            if (!drawn) return;
            float phase = i * 0.73f;
            float ang = time * (sky ? 0.18f : 0.31f) + phase * 2.1f;
            float radius = sky ? 90f + slot * 22f + 8f * (float)Math.Sin(time * 0.2f + phase) : 48f + slot * 9f;
            float y = sky ? 55f + slot * 28f + 4f * (float)Math.Sin(time * 0.4f + phase) : 8f + (slot % 3) * 5f + 1.5f * (float)Math.Sin(time * 0.8f + phase);
            float presence = sky ? SkyPresence(altitude) : PadPresence(altitude);
            if (presence <= 0f) return;
            if (!sky && altitude > FlushAlt)
            {
                float u = (altitude - FlushAlt) / 40f;
                if (u > 1f) u = 1f;
                radius += 30f * u;
                y += 18f * u;
            }
            // Floor stays above 0.74 so the smallest bird (0.9 of span) never drops under 0.7 m.
            float beat = flap <= 0f ? 1f : 0.88f + 0.12f * (float)Math.Sin(time * 6.5f + phase * 4f);
            p.X = radius * (float)Math.Cos(ang);
            p.Z = radius * (float)Math.Sin(ang);
            p.Y = y;
            p.Yaw = ang + 1.5708f;
            p.Span = WingSpan * beat * (0.9f + (i % 5) * 0.03f);
            p.Presence = presence;
        }

        public static float PadPresence(float altitude)
        {
            if (altitude <= 40f) return 1f;
            if (altitude >= PadGoneAlt) return 0f;
            float u = (altitude - 40f) / (PadGoneAlt - 40f);
            return 1f - u * u * (3f - 2f * u);
        }

        public static float SkyPresence(float altitude)
        {
            if (altitude <= SkyHoldAlt) return 1f;
            if (altitude >= SkyGoneAlt) return 0f;
            float u = (altitude - SkyHoldAlt) / (SkyGoneAlt - SkyHoldAlt);
            return 1f - u * u * (3f - 2f * u);
        }
    }
}
