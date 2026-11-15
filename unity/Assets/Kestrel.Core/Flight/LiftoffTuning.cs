// Liftoff juice tunables (DECISIONS #100): the hold-down release gauge, the haptic envelope and, on the juice branch,
// the camera, sound and milestone timings. data/liftoff.json is the source; the compiled defaults below are the same
// numbers (LiftoffTuningTests pins the drift), so a player build without the synced file still plays the same moment.
// Presentation and timing only: nothing here reaches AscentSim, so the flown trajectory never changes.
using System.IO;
using Kestrel.Core.Json;

namespace Kestrel.Core.Flight
{
    public sealed class ClampWindow
    {
        /// <summary>Seconds of full thrust before nominal T-0 that a tap still counts as clean (green opens here).</summary>
        public double GreenBeforeS;
        /// <summary>Seconds after nominal T-0 the green stays open. The auto-release fires when it closes.</summary>
        public double GreenAfterS;
    }

    public sealed class ClampTuning
    {
        /// <summary>Count time (game seconds, negative before T-0) the engines light and the gauge appears.</summary>
        public double IgnitionS = -3.0;
        /// <summary>The player's own release window (Auto-assist off).</summary>
        public ClampWindow Standard = new ClampWindow { GreenBeforeS = 0.6, GreenAfterS = 0.9 };
        /// <summary>Kid / Auto-assist: a wider window before T-0 and the auto-release exactly at T-0 (today's flight).</summary>
        public ClampWindow Forgiving = new ClampWindow { GreenBeforeS = 1.2, GreenAfterS = 0.0 };
        /// <summary>Where the green band starts on the 0..1 gauge (the needle reaches it at full thrust).</summary>
        public double GaugeGreenFrom = 0.78;
        /// <summary>An early tap is held by the sequencer until full thrust, then this beat more (seconds).</summary>
        public double EarlyHesitateS = 0.4;
        /// <summary>How far the stack settles on its hold-down posts after an early release (presentation, metres).</summary>
        public double EarlySettleM = 0.08;
    }

    public sealed class HapticTuning
    {
        public double RumbleMinHz = 10, RumbleMaxHz = 26;
        public double RumbleMinIntensity = 0.15, RumbleMaxIntensity = 0.7;
        public double KickIntensity = 1.0, KickEchoIntensity = 0.6, KickEchoS = 0.07, KickTailIntensity = 0.35, KickTailS = 0.16;
        public double HesitateIntensity = 0.45;
        /// <summary>The Max-Q buzz runs while q is at least this share of the design's predicted max-Q.</summary>
        public double BuzzFromQ = 0.8;
        public double BuzzHz = 20, BuzzMinIntensity = 0.2, BuzzMaxIntensity = 0.6;
        /// <summary>Longest the buzz may run in one flight (wall seconds).</summary>
        public double BuzzMaxS = 8;
    }

    public sealed class LiftoffTuning
    {
        public const string FileName = "liftoff.json";

        public ClampTuning Clamps = new ClampTuning();
        public HapticTuning Haptics = new HapticTuning();

        static LiftoffTuning active;
        /// <summary>The tuning in play: the synced file when the app installed one, else the compiled defaults.</summary>
        public static LiftoffTuning Active { get => active ??= new LiftoffTuning(); set => active = value; }

        public static LiftoffTuning Load(string repoRoot)
            => FromJson(MiniJson.ParseObject(File.ReadAllText(Path.Combine(repoRoot, "data", FileName))));

        /// <summary>Missing keys keep their defaults; a malformed file throws (the caller keeps the defaults).</summary>
        public static LiftoffTuning FromJson(JsonObject o)
        {
            var t = new LiftoffTuning();
            if (o == null) return t;
            var c = o.Obj("clamps");
            if (c != null)
            {
                t.Clamps.IgnitionS = c.Num("ignitionS", t.Clamps.IgnitionS);
                t.Clamps.Standard = Window(c.Obj("standard"), t.Clamps.Standard);
                t.Clamps.Forgiving = Window(c.Obj("forgiving"), t.Clamps.Forgiving);
                t.Clamps.GaugeGreenFrom = c.Num("gaugeGreenFrom", t.Clamps.GaugeGreenFrom);
                t.Clamps.EarlyHesitateS = c.Num("earlyHesitateS", t.Clamps.EarlyHesitateS);
                t.Clamps.EarlySettleM = c.Num("earlySettleM", t.Clamps.EarlySettleM);
            }
            var h = o.Obj("haptics");
            if (h != null)
            {
                var d = t.Haptics;
                d.RumbleMinHz = h.Num("rumbleMinHz", d.RumbleMinHz);
                d.RumbleMaxHz = h.Num("rumbleMaxHz", d.RumbleMaxHz);
                d.RumbleMinIntensity = h.Num("rumbleMinIntensity", d.RumbleMinIntensity);
                d.RumbleMaxIntensity = h.Num("rumbleMaxIntensity", d.RumbleMaxIntensity);
                d.KickIntensity = h.Num("kickIntensity", d.KickIntensity);
                d.KickEchoIntensity = h.Num("kickEchoIntensity", d.KickEchoIntensity);
                d.KickEchoS = h.Num("kickEchoS", d.KickEchoS);
                d.KickTailIntensity = h.Num("kickTailIntensity", d.KickTailIntensity);
                d.KickTailS = h.Num("kickTailS", d.KickTailS);
                d.HesitateIntensity = h.Num("hesitateIntensity", d.HesitateIntensity);
                d.BuzzFromQ = h.Num("buzzFromQ", d.BuzzFromQ);
                d.BuzzHz = h.Num("buzzHz", d.BuzzHz);
                d.BuzzMinIntensity = h.Num("buzzMinIntensity", d.BuzzMinIntensity);
                d.BuzzMaxIntensity = h.Num("buzzMaxIntensity", d.BuzzMaxIntensity);
                d.BuzzMaxS = h.Num("buzzMaxS", d.BuzzMaxS);
            }
            return t;
        }

        static ClampWindow Window(JsonObject o, ClampWindow def)
        {
            if (o == null) return def;
            return new ClampWindow { GreenBeforeS = o.Num("greenBeforeS", def.GreenBeforeS), GreenAfterS = o.Num("greenAfterS", def.GreenAfterS) };
        }
    }
}
