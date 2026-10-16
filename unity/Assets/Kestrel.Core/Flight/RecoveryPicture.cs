// What the flight picture draws, as a function of the sample. Plume scale, fin and leg
// deploy, the inset versus the follow camera, dust or splash, and which #247 failure
// picture a hard landing reuses. Callouts quote the sample, not a scripted clock.
using System;
using System.Globalization;

namespace Kestrel.Core.Flight
{
    public struct RecoveryPicture
    {
        public bool Show, Inset, Follow, Dust, Splash;
        public double Plume, Fins, Legs;
        public string BoomKind;
        public RecoveryCallout Call;
    }

    public struct RecoveryCallout
    {
        public bool Visible;
        public string Title, Line, NarrationId;
    }

    public static class RecoveryViews
    {
        public const double CalloutSec = 1.20;

        public static RecoveryPicture Of(in RecoverySample sample, RecoveryMark[] marks, string cause, double vLeg, bool down)
        {
            var pic = new RecoveryPicture
            {
                Show = sample.Phase != RecoveryPhase.Idle,
                Plume = sample.Throttle,
                Fins = sample.Fins,
                Legs = sample.Legs01,
                Inset = sample.Phase == RecoveryPhase.Boostback || sample.Phase == RecoveryPhase.Coast,
                Follow = sample.Phase == RecoveryPhase.Entry || sample.Phase == RecoveryPhase.Descent
                    || sample.Phase == RecoveryPhase.Landing || sample.Phase == RecoveryPhase.Down,
            };
            if (down)
            {
                bool deck = Math.Abs(sample.MissM) <= RecoverySim.DeckRadiusM && cause != RecoveryScore.Miss && cause != RecoveryScore.Fuel;
                pic.Dust = deck;
                pic.Splash = !deck;
                pic.BoomKind = Boom(cause, sample.Sink, vLeg);
                pic.Inset = false;
                pic.Follow = true;
            }
            pic.Call = Callout(sample, marks);
            return pic;
        }

        public static string Boom(string cause, double sink, double vLeg)
        {
            if (string.IsNullOrEmpty(cause)) return null;
            if (cause == RecoveryScore.Tilt) return "leg-collapse";
            if (cause == RecoveryScore.Fast && sink <= vLeg * 2.2 && sink <= 12) return "leg-collapse";
            if (cause == RecoveryScore.Fast || cause == RecoveryScore.Fuel || cause == RecoveryScore.Miss)
                return sink > vLeg ? "structural-maxq" : null;
            return null;
        }

        public static RecoveryCallout Callout(in RecoverySample sample, RecoveryMark[] marks)
        {
            if (marks == null || marks.Length == 0) return default;
            int hit = -1;
            for (int i = 0; i < marks.Length; i++)
            {
                if (marks[i].T > sample.T + 1e-9) break;
                double end = i + 1 < marks.Length ? marks[i + 1].T : marks[i].T + CalloutSec;
                if (sample.Phase == RecoveryPhase.Down && marks[i].Phase == RecoveryPhase.Down)
                    end = marks[i].T + CalloutSec;
                if (sample.T + 1e-9 >= marks[i].T && sample.T < end) hit = i;
            }
            if (hit < 0) return default;
            var m = marks[hit];
            string title = Title(m.Phase);
            if (title.Length == 0) return default;
            var call = new RecoveryCallout
            {
                Visible = true,
                Title = title,
                NarrationId = Narration(m.Phase),
                Line = m.Phase == RecoveryPhase.Down
                    ? Touchdown(m.Sink, m.TiltDeg, m.MissM)
                    : FlightStaging.Format(title, m.T, m.Alt, m.Speed),
            };
            return call;
        }

        public static string Title(RecoveryPhase phase)
        {
            switch (phase)
            {
                case RecoveryPhase.Boostback: return "BOOSTBACK";
                case RecoveryPhase.Entry: return "ENTRY BURN";
                case RecoveryPhase.Descent: return "GRID FINS";
                case RecoveryPhase.Landing: return "LANDING BURN";
                case RecoveryPhase.Down: return "TOUCHDOWN";
                default: return "";
            }
        }

        public static string Narration(RecoveryPhase phase)
        {
            switch (phase)
            {
                case RecoveryPhase.Boostback: return "haven-burn";
                case RecoveryPhase.Entry: return "entry";
                case RecoveryPhase.Descent: return "haven-strakes";
                case RecoveryPhase.Landing: return "landing";
                case RecoveryPhase.Down: return "touchdown";
                default: return "";
            }
        }

        public static string Touchdown(double sink, double tiltDeg, double missM)
        {
            var inv = CultureInfo.InvariantCulture;
            return "TOUCHDOWN  " + sink.ToString("0.0", inv) + " m/s  " + tiltDeg.ToString("0.0", inv) + "°  " + missM.ToString("0", inv) + " m";
        }
    }

    /// <summary>
    /// Last recovery the post-flight card should quote. Active while a recoverable
    /// booster is still in the air, so the card waits for the touchdown.
    /// </summary>
    public static class RecoveryLog
    {
        public static RecoveryVerdict Current { get; private set; }
        public static bool Active { get; private set; }

        public static void ArmFlight()
        {
            Current = null;
            Active = false;
        }

        public static void Engage() => Active = true;

        public static void Publish(RecoveryVerdict verdict)
        {
            Current = verdict;
            Active = false;
        }

        public static void Release() => Active = false;

        public static void Clear()
        {
            Current = null;
            Active = false;
        }
    }
}
