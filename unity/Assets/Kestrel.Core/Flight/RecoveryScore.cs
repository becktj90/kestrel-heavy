// Touchdown speed, tilt and distance from the Haven centre.
// Grades use the same margins as data/landing.json. v_leg is the leg limit from RecoveryModel.
// A miss, a fast hit, a tilt, or an empty tank is a named cause for the report and the medal.
using System;
using System.Globalization;
using Kestrel.Core.Json;

namespace Kestrel.Core.Flight
{
    public static class RecoveryScore
    {
        public const string Fuel = "recovery-fuel";
        public const string Fast = "recovery-fast";
        public const string Tilt = "recovery-tilt";
        public const string Miss = "recovery-miss";

        public static LandingTune HavenTune()
            => LandingTune.FromJson(MiniJson.ParseObject(
                "{\"padRatLandingVy\":4,\"perfectMin\":0.45,\"goodMin\":0.2}"));

        public static RecoveryVerdict Judge(in RecoverySample end, in RecoveryCraft craft, RecoveryMark[] marks)
        {
            double vLeg = craft.VLeg;
            if (!(vLeg > 0)) vLeg = RecoveryModel.LightVLeg;
            bool legsDown = !craft.Legs || end.Legs01 >= 0.92;
            bool onDeck = Math.Abs(end.MissM) <= RecoverySim.DeckRadiusM;
            bool tooFast = end.Sink > vLeg || !legsDown;
            bool tilted = end.TiltDeg > RecoverySim.TiltLimitDeg;
            // An empty tank that still hits hard or wide used up the reserve. A soft deck
            // landing may also finish dry; that one spent the fuel on purpose.
            bool dry = end.Prop <= 1e-3 && (tooFast || !onDeck);
            string cause = null;
            if (dry && (tooFast || !onDeck)) cause = Fuel;
            else if (!onDeck) cause = Miss;
            else if (tooFast) cause = Fast;
            else if (tilted) cause = Tilt;

            var tune = HavenTune();
            double landingVy = vLeg;
            bool tipped = tilted || !legsDown;
            var grade = tune.Grade(onDeck ? "deck" : "water", end.Sink, end.MissM, landingVy, RecoverySim.DeckRadiusM, tipped);
            var medal = MedalShelf.From(grade);
            var verdict = new RecoveryVerdict
            {
                Cause = cause,
                GradeName = grade.Name,
                Medal = medal.Medal,
                Sink = end.Sink,
                TiltDeg = end.TiltDeg,
                MissM = end.MissM,
                T = end.T,
                PropLeft = end.Prop,
                End = end,
                Marks = marks ?? Array.Empty<RecoveryMark>(),
            };
            if (cause != null) verdict.Failure = Event(cause, end, vLeg);
            return verdict;
        }

        public static FailureEvent Event(string cause, in RecoverySample end, double vLeg)
        {
            var ev = new FailureEvent { t = end.T, kind = cause, stage = 1 };
            var inv = CultureInfo.InvariantCulture;
            if (cause == Fuel)
            {
                ev.cause = "out-of-fuel";
                ev.value = end.Sink;
                ev.limit = vLeg;
                ev.kid = "The landing fuel ran out at " + end.Sink.ToString("0.0", inv) + " m/s, " + Math.Abs(end.MissM).ToString("0", inv) + " m from Haven.";
                ev.engineer = "Reserve empty at T+" + end.T.ToString("0.0", inv) + " s. Sink " + end.Sink.ToString("0.00", inv)
                    + " m/s, miss " + end.MissM.ToString("0.0", inv) + " m, v_leg " + vLeg.ToString("0.00", inv) + " m/s.";
                ev.fixHint = "Hold more landing fuel, or pick expendable.";
            }
            else if (cause == Fast)
            {
                ev.cause = "too-fast";
                ev.value = end.Sink;
                ev.limit = vLeg;
                ev.kid = "Touchdown was " + end.Sink.ToString("0.0", inv) + " m/s. The legs hold " + vLeg.ToString("0.0", inv) + " m/s.";
                ev.engineer = "Sink " + end.Sink.ToString("0.00", inv) + " m/s > v_leg " + vLeg.ToString("0.00", inv)
                    + " m/s. Legs " + (end.Legs01 * 100).ToString("0", inv) + "% out. Tilt " + end.TiltDeg.ToString("0.0", inv) + "°.";
                ev.fixHint = "Heavier legs, or more fuel for the landing burn.";
            }
            else if (cause == Tilt)
            {
                ev.cause = "tilted";
                ev.value = end.TiltDeg;
                ev.limit = RecoverySim.TiltLimitDeg;
                ev.kid = "It hit tilted " + end.TiltDeg.ToString("0.0", inv) + "°. Straight up is within " + RecoverySim.TiltLimitDeg.ToString("0", inv) + "°.";
                ev.engineer = "Tilt " + end.TiltDeg.ToString("0.00", inv) + "° > " + RecoverySim.TiltLimitDeg.ToString("0", inv)
                    + "°. Sink " + end.Sink.ToString("0.00", inv) + " m/s, miss " + end.MissM.ToString("0.0", inv) + " m.";
                ev.fixHint = "Start the landing burn earlier so the booster is upright.";
            }
            else
            {
                ev.cause = "missed-barge";
                ev.value = end.MissM;
                ev.limit = RecoverySim.DeckRadiusM;
                ev.kid = "It came down " + Math.Abs(end.MissM).ToString("0", inv) + " m from the Haven centre. The deck reaches " + RecoverySim.DeckRadiusM.ToString("0", inv) + " m.";
                ev.engineer = "Miss " + end.MissM.ToString("0.0", inv) + " m. Deck radius " + RecoverySim.DeckRadiusM.ToString("0", inv)
                    + " m. Sink " + end.Sink.ToString("0.00", inv) + " m/s at T+" + end.T.ToString("0.0", inv) + " s.";
                ev.fixHint = "More boostback fuel, or aim the entry at the barge.";
            }
            return ev;
        }
    }
}
