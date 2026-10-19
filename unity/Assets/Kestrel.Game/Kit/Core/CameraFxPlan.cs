// Engine-free camera effect planner: trauma-based shake with decay, a small FOV punch, and brief slow-mo / hit-stop
// on key beats (ignition, liftoff, max-Q, staging, touchdown, crash).
// Hard caps: slow-mo never goes below 0.4x, lasts at most 0.35 s and starts at most once per 3 s; shake and FOV are
// bounded; reduce-motion plays nothing. The slow-mo value is a PRESENTATION time scale only (it is what gets fed to
// Core's PresentationTimeScale / FixedStepAccumulator); nothing here ever touches a physics dt.
using System;

namespace Kestrel.Kit
{
    public enum CameraBeat { Ignition = 0, Liftoff = 1, MaxQ = 2, Staging = 3, Touchdown = 4, Crash = 5 }

    public struct CameraBeatEvent
    {
        public CameraBeat Beat;
        public float Intensity01;   // 0..1 scales the beat
        public float ShakeMul;      // 0..1: lower when another system (FlightPresenter cues) already shakes on this beat
        public bool Reduce;
    }

    public static partial class CinematicEvents
    {
        public static event Action<CameraBeatEvent> CameraBeatRaised;

        public static void CameraBeat(CameraBeat beat, float intensity01, bool reduce, float shakeMul = 1f)
        {
            var h = CameraBeatRaised;
            if (h != null) h(new CameraBeatEvent { Beat = beat, Intensity01 = intensity01, ShakeMul = shakeMul, Reduce = reduce });
        }

        /// <summary>Presentation time scale the camera planner wants (1 = realtime). Feed it to PresentationTimeScale. Never a physics dt.</summary>
        public static double PresentationScale = 1.0;

        /// <summary>Metres of camera travel at full trauma = 0.012 x this. Set it to the vehicle height so shake scales with the view.</summary>
        public static float ShakeReferenceM = 60f;

        public static void ClearCameraSubscribers() { CameraBeatRaised = null; PresentationScale = 1.0; }
    }

    public struct CameraBeatSpec
    {
        public float Trauma, FovDeg, SlowScale, SlowSeconds;
        public bool SlowMo => SlowSeconds > 0f && SlowScale < 1f;
    }

    public static class CameraFxLimits
    {
        public const float MinTimeScale = 0.4f;
        public const float MaxSlowSeconds = 0.35f;
        public const float MinSlowGapSeconds = 3f;
        public const float MaxTrauma = 1f;
        public const float MaxFovPunchDeg = 2.5f;
        public const float MaxRollDeg = 1.5f;
        public const float MaxOffsetFraction = 0.012f;   // of ShakeReferenceM at trauma 1
        public const float TraumaDecayPerSec = 1.6f;
        public const float FovDecayPerSec = 7f;
        public const float EaseIn = 0.04f, EaseOut = 0.12f;   // slow-mo is eased, never a hard step

        public static CameraBeatSpec Spec(CameraBeat b)
        {
            switch (b)
            {
                case CameraBeat.Ignition: return new CameraBeatSpec { Trauma = 0.15f, FovDeg = 0.4f };
                case CameraBeat.Liftoff: return new CameraBeatSpec { Trauma = 0.30f, FovDeg = 0.8f, SlowScale = 0.6f, SlowSeconds = 0.25f };
                case CameraBeat.MaxQ: return new CameraBeatSpec { Trauma = 0.35f, FovDeg = 0.5f };
                case CameraBeat.Staging: return new CameraBeatSpec { Trauma = 0.30f, FovDeg = 0.7f, SlowScale = 0.7f, SlowSeconds = 0.20f };
                case CameraBeat.Touchdown: return new CameraBeatSpec { Trauma = 0.35f, FovDeg = 1.0f, SlowScale = 0.5f, SlowSeconds = 0.30f };
                default: return new CameraBeatSpec { Trauma = 0.80f, FovDeg = 2.0f, SlowScale = 0.4f, SlowSeconds = 0.35f };
            }
        }
    }

    /// <summary>Pure state machine. Trigger on beats, Step with real (unscaled) dt, read the outputs.</summary>
    public sealed class CameraFxState
    {
        public float Trauma, FovDeg, TimeScale = 1f;
        public float ShakeX, ShakeY, RollDeg;     // normalised offsets in [-1, 1] (x trauma^2) and roll in degrees
        float clock, slowStart = -1000f, slowAge = -1f, slowDur, slowMin = 1f;
        bool slowing;

        public bool SlowMoActive => slowing;

        public void Reset() { Trauma = FovDeg = 0f; TimeScale = 1f; ShakeX = ShakeY = RollDeg = 0f; slowing = false; slowStart = -1000f; clock = 0f; }

        public void Trigger(in CameraBeatEvent e)
        {
            if (e.Reduce) return;
            float i = float.IsNaN(e.Intensity01) ? 0f : Math.Max(0f, Math.Min(1f, e.Intensity01));
            if (i <= 0f) return;
            float mul = float.IsNaN(e.ShakeMul) ? 1f : Math.Max(0f, Math.Min(1f, e.ShakeMul));
            var s = CameraFxLimits.Spec(e.Beat);
            Trauma = Math.Min(CameraFxLimits.MaxTrauma, Trauma + s.Trauma * i * mul);
            FovDeg = Math.Min(CameraFxLimits.MaxFovPunchDeg, FovDeg + s.FovDeg * i);
            if (s.SlowMo && !slowing && clock - slowStart >= CameraFxLimits.MinSlowGapSeconds)
            {
                slowing = true; slowAge = 0f; slowStart = clock;
                slowDur = Math.Min(CameraFxLimits.MaxSlowSeconds, s.SlowSeconds);
                // Intensity deepens the slow-mo but never past the floor.
                slowMin = Math.Max(CameraFxLimits.MinTimeScale, 1f - (1f - s.SlowScale) * i);
            }
        }

        public void Step(float realDt)
        {
            if (float.IsNaN(realDt) || realDt <= 0f) return;
            if (realDt > 0.1f) realDt = 0.1f;
            clock += realDt;
            Trauma = Math.Max(0f, Trauma - CameraFxLimits.TraumaDecayPerSec * realDt);
            FovDeg *= (float)Math.Exp(-CameraFxLimits.FovDecayPerSec * realDt);
            if (FovDeg < 0.001f) FovDeg = 0f;
            float k = Trauma * Trauma;
            // ~12 Hz smooth noise, deterministic in the state clock.
            ShakeX = k * Noise(clock * 12f, 0.31f);
            ShakeY = k * Noise(clock * 12f, 7.17f);
            RollDeg = k * CameraFxLimits.MaxRollDeg * Noise(clock * 9f, 13.9f);
            if (slowing)
            {
                slowAge += realDt;
                float total = slowDur;
                if (slowAge >= total + CameraFxLimits.EaseOut) { slowing = false; TimeScale = 1f; }
                else
                {
                    float into = slowAge < CameraFxLimits.EaseIn ? slowAge / CameraFxLimits.EaseIn : 1f;
                    float outOf = slowAge > total ? 1f - (slowAge - total) / CameraFxLimits.EaseOut : 1f;
                    float depth = Math.Min(into, outOf); if (depth < 0f) depth = 0f;
                    TimeScale = 1f - (1f - slowMin) * depth;
                }
            }
            else TimeScale = 1f;
        }

        /// <summary>Camera-space shake in metres for a given reference size (vehicle height).</summary>
        public static float OffsetMetres(float normalised, float referenceM) => normalised * CameraFxLimits.MaxOffsetFraction * referenceM;

        static float Noise(float x, float seed)
        {
            // Value noise from a hash, smoothstep interpolated, range [-1, 1].
            int i = (int)Math.Floor(x); float f = x - i; f = f * f * (3f - 2f * f);
            float a = H(i, seed), b = H(i + 1, seed);
            return (a + (b - a) * f) * 2f - 1f;
        }

        static float H(int n, float seed)
        {
            uint h = (uint)(n * 374761393 + (int)(seed * 1000f) * 668265263); h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216f;
        }
    }
}
