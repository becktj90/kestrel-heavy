// The liftoff haptic envelope (DECISIONS #100), frame-paced so the native side only ever plays one Taptic impact:
//   - ignition: a rumble whose rate and strength ramp with the spooling thrust;
//   - clamp release (the sim's liftoff): a hard kick, an echo and a short tail;
//   - an early tap the sequencer holds: one soft "clunk" (the stack hesitates; no rumble change, no fake damage);
//   - Max-Q: a buzz while live q is near the design's predicted peak, capped in length.
// Engine-free and allocation-free per frame (the caller owns the output list). The Haptics setting gates everything.
using System;
using System.Collections.Generic;
using Kestrel.Core.Flight;

namespace Kestrel.Core.Feel
{
    /// <summary>The UIKit impact styles the iOS bridge maps to (UIImpactFeedbackStyle order, plus nothing else).</summary>
    public enum HapticStyle { Light = 0, Medium = 1, Heavy = 2, Soft = 3, Rigid = 4 }

    public struct HapticPulse
    {
        public HapticStyle Style;
        /// <summary>0..1, passed to impactOccurredWithIntensity.</summary>
        public float Intensity;
    }

    /// <summary>What happened this frame. Plain values from the pad and the flight.</summary>
    public struct LiftoffHapticInput
    {
        /// <summary>The Haptics setting (Settings, and the Hangar switch: one pref, kh.hangar.haptics).</summary>
        public bool Enabled;
        /// <summary>Engines lit on the pad, clamps holding: the rumble follows <see cref="Thrust01"/>.</summary>
        public bool Spooling;
        public double Thrust01;
        /// <summary>An early tap was held by the sequencer this frame.</summary>
        public bool HesitatedNow;
        /// <summary>The sim started this frame (the clamps let go).</summary>
        public bool LiftoffNow;
        /// <summary>Live q over the design's predicted max-Q (0 when unknown or on the pad).</summary>
        public double QShare;
    }

    public sealed class LiftoffHaptics
    {
        /// <summary>The one haptics preference (the Hangar switch already wrote it; Settings now shows it too).</summary>
        public const string Pref = "kh.hangar.haptics";
        /// <summary>Taptic impacts are not queued faster than this, whatever the tuning says.</summary>
        public const double MaxHz = 30;

        readonly HapticTuning t;
        double rumblePhase, buzzPhase, buzzUsed;
        double kickAge = -1;
        bool echoSent, tailSent;

        public LiftoffHaptics(HapticTuning tuning) { t = tuning ?? new HapticTuning(); }

        /// <summary>Wall seconds of buzz spent so far this flight.</summary>
        public double BuzzSeconds => buzzUsed;

        public void Reset()
        {
            rumblePhase = buzzPhase = buzzUsed = 0;
            kickAge = -1;
            echoSent = tailSent = false;
        }

        public static bool On(Func<string, bool> has, Func<string, int> read)
        {
            if (has == null || read == null || !has(Pref)) return true;
            return read(Pref) != 0;
        }

        /// <summary>Rumble pulses per second at a thrust share (rises with the spool).</summary>
        public double RumbleHz(double thrust01) => Math.Min(MaxHz, Lerp(t.RumbleMinHz, t.RumbleMaxHz, Clamp01(thrust01)));

        /// <summary>Rumble strength: gentle at light-up, firm at full thrust (a 1.5 power so the last third swells).</summary>
        public double RumbleIntensity(double thrust01)
        {
            double f = Clamp01(thrust01);
            return Clamp01(Lerp(t.RumbleMinIntensity, t.RumbleMaxIntensity, Math.Pow(f, 1.5)));
        }

        /// <summary>Buzz strength for a q share; 0 under <see cref="HapticTuning.BuzzFromQ"/>.</summary>
        public double BuzzIntensity(double qShare)
        {
            if (double.IsNaN(qShare) || qShare < t.BuzzFromQ) return 0;
            double span = Math.Max(1e-6, 1 - t.BuzzFromQ);
            return Clamp01(Lerp(t.BuzzMinIntensity, t.BuzzMaxIntensity, Clamp01((qShare - t.BuzzFromQ) / span)));
        }

        /// <summary>One frame of <paramref name="dt"/> wall seconds. Appends this frame's pulses to <paramref name="pulses"/>; returns how many.</summary>
        public int Step(double dt, in LiftoffHapticInput input, List<HapticPulse> pulses)
        {
            if (pulses == null) return 0;
            if (double.IsNaN(dt) || dt < 0) dt = 0;
            if (dt > 0.1) dt = 0.1;
            int before = pulses.Count;
            if (!input.Enabled)
            {
                kickAge = -1;
                return 0;
            }
            if (input.LiftoffNow)
            {
                Add(pulses, HapticStyle.Heavy, t.KickIntensity);
                kickAge = 0;
                echoSent = tailSent = false;
                rumblePhase = 0;
            }
            else if (kickAge >= 0)
            {
                kickAge += dt;
                if (!echoSent && kickAge >= t.KickEchoS) { echoSent = true; Add(pulses, HapticStyle.Rigid, t.KickEchoIntensity); }
                if (!tailSent && kickAge >= t.KickTailS) { tailSent = true; Add(pulses, HapticStyle.Medium, t.KickTailIntensity); }
                if (echoSent && tailSent) kickAge = -1;
            }
            if (input.HesitatedNow) Add(pulses, HapticStyle.Soft, t.HesitateIntensity);
            if (input.Spooling && !input.LiftoffNow && input.Thrust01 > 0.02)
            {
                rumblePhase += dt * RumbleHz(input.Thrust01);
                if (rumblePhase >= 1)
                {
                    rumblePhase -= Math.Floor(rumblePhase);
                    double k = RumbleIntensity(input.Thrust01);
                    Add(pulses, k < 0.4 ? HapticStyle.Light : HapticStyle.Medium, k);
                }
            }
            else rumblePhase = 0;
            double buzz = BuzzIntensity(input.QShare);
            if (buzz > 0 && buzzUsed < t.BuzzMaxS)
            {
                buzzUsed += dt;
                buzzPhase += dt * Math.Min(MaxHz, t.BuzzHz);
                if (buzzPhase >= 1)
                {
                    buzzPhase -= Math.Floor(buzzPhase);
                    Add(pulses, HapticStyle.Light, buzz);
                }
            }
            else buzzPhase = 0;
            return pulses.Count - before;
        }

        static void Add(List<HapticPulse> list, HapticStyle style, double intensity)
        {
            float k = (float)Clamp01(intensity);
            if (k <= 0f) return;
            list.Add(new HapticPulse { Style = style, Intensity = k });
        }

        static double Lerp(double a, double b, double u) => a + (b - a) * u;

        static double Clamp01(double v) => double.IsNaN(v) || v < 0 ? 0 : v > 1 ? 1 : v;
    }
}
