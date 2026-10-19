// Maps ascent-sim events onto staging visuals. Burnout and cutoff, separation, the next
// ignition, and fairing jettison (a sim altitude condition after max-Q, or a "fairing" event)
// are the only triggers. Poses, plume scale and callout windows are functions of sim time.
// Nothing here reads a wall clock. Offer is the narrator hook; SilentFlightNarration does not speak,
// and flight Thrum is not called from this type.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    public enum StagingKind
    {
        Meco = 0,
        StageSep = 1,
        Ignition = 2,
        FairingSep = 3,
    }

    public struct StagingBody
    {
        public int Serial;
        public string Role;
        public int Index;
        public bool Releases;
    }

    public struct SepPose
    {
        public bool Released;
        public double Age, X, Y, Z, Pitch, Yaw, Roll;
    }

    public struct FairingPose
    {
        public bool Separated;
        /// <summary>The halves have tumbled out of the shot and should be despawned.</summary>
        public bool Gone;
        public double Age, YawDeg, OutM, DropM, SpinDeg;
    }

    public struct PlumeRead
    {
        public double Scale, Heat, Flash;
        public int Serial;
        public bool Lit;
    }

    public struct StagingCallout
    {
        public bool Visible;
        public StagingKind Kind;
        public string Title, Line, NarrationId;
        public int Stage;
        public double T, AltM, SpeedMps;
    }

    public struct StagingCue
    {
        public string NarrationId;
        public StagingKind Kind;
        public int Stage;
        public double T, AltM, SpeedMps;
    }

    /// <summary>Narrator / Thrum hook. Flight passes <see cref="SilentFlightNarration"/> and does not speak.</summary>
    public interface IFlightStagingNarration
    {
        void Offer(in StagingCue cue);
    }

    /// <summary>Records the offer and stays quiet. Spoke is always false.</summary>
    public sealed class SilentFlightNarration : IFlightStagingNarration
    {
        public readonly List<string> Offered = new List<string>();
        public bool Spoke => false;
        public string Spoken => "";

        public void Offer(in StagingCue cue)
        {
            if (!string.IsNullOrEmpty(cue.NarrationId)) Offered.Add(cue.NarrationId);
        }
    }

    public static class FlightStagingPlan
    {
        /// <summary>
        /// One visual body per sim serial stage. Strap-on serials release each side booster.
        /// Every later serial except the last releases that core stage. The last stage stays with the payload.
        /// </summary>
        public static List<StagingBody> Bodies(IList<SerialStage> serial, int boosterCount)
        {
            var list = new List<StagingBody>();
            if (serial == null) return list;
            if (boosterCount < 0) boosterCount = 0;
            for (int k = 0; k < serial.Count; k++)
            {
                var s = serial[k];
                string role = s != null ? s.role : null;
                bool strap = role != null && role.IndexOf("strap", StringComparison.Ordinal) >= 0;
                if (strap)
                {
                    for (int i = 0; i < boosterCount; i++)
                        list.Add(new StagingBody { Serial = k, Role = "booster", Index = i, Releases = true });
                    continue;
                }
                int index = s != null && s.index >= 0 ? s.index : k;
                bool last = k == serial.Count - 1;
                list.Add(new StagingBody { Serial = k, Role = "stage", Index = index, Releases = !last });
            }
            return list;
        }
    }

    /// <summary>Event → visual state. Feed the same sim event list and the same sim time and the result matches.</summary>
    public sealed class FlightStaging
    {
        public const double CollapseSec = 0.60;
        public const double AfterglowSec = 0.45;
        /// <summary>MECO to separation. A medium kerolox booster coasts about 3 s before the stages push apart.</summary>
        public const double SepDelaySec = 3.0;
        /// <summary>MECO to upper-stage ignition. The public anchor is about 11 s (MECO ~2:25, SES-1 ~2:36).</summary>
        public const double IgniteDelaySec = 11.0;
        public const double SpoolSec = 0.45;
        public const double CalloutSec = 1.20;
        /// <summary>How long a frozen sim still has curves to finish. The view may play this tail; events are not chosen by it.</summary>
        public const double TailSec = CollapseSec + AfterglowSec + CalloutSec;
        public const float CalloutPt = 22f;
        public const double SepPushMps = 7.0;
        public const double SepRadialMps = 4.5;
        public const double FallAccel = 4.5;
        public const double TumbleDeg = 28.0;
        public const double FairingSec = 2.2;
        public const double FairingPeelDeg = 68.0;
        public const double FairingOutM = 5.5;
        public const double FairingDropM = 3.2;
        /// <summary>After the peel, the halves tumble and fall, then despawn.</summary>
        public const double FairingTumbleDeg = 42.0;
        public const double FairingLife = 9.5;
        /// <summary>A recoverable booster pitches engines-down over this many seconds, then falls.</summary>
        public const double FlipSec = 4.0;
        public const double QArmPa = 8000;
        public const double QDrop = 0.98;

        struct Beat
        {
            public StagingKind Kind;
            public int Stage;
            public int Serial;
            public double EventT, Alt, Speed;
            public string NarrationId;
        }

        readonly List<Beat> beats = new List<Beat>();
        readonly List<StagingCue> cues = new List<StagingCue>();
        int seen, cueRead, boosterCount = 1;
        bool hasFairing, qPassed, reduced;
        /// <summary>Stage 1 has landing legs and a propellant reserve, so it flips and descends instead of tumbling away.</summary>
        public bool RecoverBooster;
        double fairingAltM = 110000, fairingT = -1, qPeak;

        public int CueCount => cues.Count;

        public void Reset(bool fairing, double jettisonAltM, int boosters, bool reduceMotion)
        {
            beats.Clear();
            cues.Clear();
            seen = 0;
            cueRead = 0;
            hasFairing = fairing;
            fairingAltM = jettisonAltM > 0 ? jettisonAltM : 110000;
            fairingT = -1;
            qPeak = 0;
            qPassed = false;
            reduced = reduceMotion;
            boosterCount = boosters > 0 ? boosters : 1;
        }

        /// <summary>Ingest events with t &lt;= <paramref name="t"/>. Fairing trips once, from a sim event or from altitude after max-Q.</summary>
        public void Advance(IList<SimEvent> events, double t, double alt, double speed, double q, double thrust01)
        {
            if (double.IsNaN(t) || t < 0) t = 0;
            if (double.IsNaN(alt) || alt < 0) alt = 0;
            if (double.IsNaN(speed) || speed < 0) speed = 0;
            if (double.IsNaN(q) || q < 0) q = 0;
            if (events != null)
            {
                while (seen < events.Count)
                {
                    var e = events[seen];
                    if (e == null || e.t > t + 1e-6) break;
                    seen++;
                    Ingest(e, alt, speed);
                }
            }
            NoteFairing(t, alt, speed, q);
        }

        public bool TakeCue(out StagingCue cue)
        {
            if (cueRead >= cues.Count)
            {
                cue = default;
                return false;
            }
            cue = cues[cueRead++];
            return true;
        }

        public PlumeRead Plume(double t, double thrust01)
        {
            double live = Clamp01(thrust01);
            int meco = Latest(StagingKind.Meco, t);
            if (meco < 0)
                return new PlumeRead { Scale = live, Heat = 0, Flash = 0, Serial = 0, Lit = live > 0.02 };
            var m = beats[meco];
            double age = t - m.EventT;
            int spent = m.Serial < 0 ? 0 : m.Serial;
            int ign = Next(meco, StagingKind.Ignition);
            if (age < CollapseSec)
            {
                double u = age / CollapseSec;
                double scale = 1 - u;
                return new PlumeRead { Scale = scale, Heat = u, Flash = 0, Serial = spent, Lit = scale > 0.02 };
            }
            if (ign < 0 || t < m.EventT + IgniteDelaySec)
            {
                double g = age - CollapseSec;
                double scale = g < AfterglowSec ? 0.18 * (1 - g / AfterglowSec) : 0;
                return new PlumeRead { Scale = scale, Heat = scale > 0 ? 1 : 0, Flash = 0, Serial = spent, Lit = scale > 0.02 };
            }
            double spoolT = m.EventT + IgniteDelaySec;
            double spool = (t - spoolT) / SpoolSec;
            if (spool < 0) spool = 0;
            if (spool > 1) spool = 1;
            int litSerial = beats[ign].Serial < 0 ? spent + 1 : beats[ign].Serial;
            if (spool < 1)
            {
                double flash = reduced ? 0 : 1 - spool;
                return new PlumeRead { Scale = spool, Heat = 0, Flash = flash, Serial = litSerial, Lit = spool > 0.02 };
            }
            return new PlumeRead { Scale = live, Heat = 0, Flash = 0, Serial = litSerial, Lit = live > 0.02 };
        }

        public bool Released(int serial, double t)
        {
            for (int i = 0; i < beats.Count; i++)
            {
                if (beats[i].Kind != StagingKind.StageSep || beats[i].Serial != serial) continue;
                return t >= beats[i].EventT + SepDelaySec;
            }
            return false;
        }

        public SepPose Pose(string role, int index, int serial, double t)
        {
            double eventT = -1;
            for (int i = 0; i < beats.Count; i++)
            {
                if (beats[i].Kind != StagingKind.StageSep || beats[i].Serial != serial) continue;
                eventT = beats[i].EventT;
                break;
            }
            if (eventT < 0 || t < eventT + SepDelaySec) return default;
            return Integrate(role, index, boosterCount, t - (eventT + SepDelaySec), reduced, RecoverBooster);
        }

        public FairingPose FairingAt(double t)
        {
            if (fairingT < 0 || t < fairingT) return default;
            double age = t - fairingT;
            double u = age / FairingSec;
            if (age >= FairingLife)
            {
                return new FairingPose { Separated = true, Gone = true, Age = age };
            }
            if (u < 0) u = 0;
            if (u > 1) u = 1;
            double s = u * u * (3 - 2 * u);
            double tumble = age > FairingSec ? age - FairingSec : 0;
            double spin = reduced ? 0 : FairingTumbleDeg * tumble;
            return new FairingPose
            {
                Separated = true,
                Gone = false,
                Age = age,
                YawDeg = (reduced ? 0 : FairingPeelDeg * s) + spin,
                OutM = FairingOutM * s + tumble * 1.6,
                DropM = FairingDropM * s + 0.5 * FallAccel * tumble * tumble,
                SpinDeg = spin,
            };
        }

        public bool TryCallout(double t, out StagingCallout call)
        {
            call = default;
            double prevEnd = -1e9;
            int hit = -1;
            for (int i = 0; i < beats.Count; i++)
            {
                Window(i, out double start, out double end);
                if (start < prevEnd)
                {
                    double dur = end - start;
                    if (dur < 0.05) dur = 0.05;
                    start = prevEnd;
                    end = start + dur;
                }
                if (t + 1e-9 >= start && t < end) hit = i;
                prevEnd = end;
            }
            if (hit < 0) return false;
            var b = beats[hit];
            call.Visible = true;
            call.Kind = b.Kind;
            call.Title = Title(b.Kind);
            call.Line = Format(call.Title, b.EventT, b.Alt, b.Speed);
            call.NarrationId = b.NarrationId;
            call.Stage = b.Stage;
            call.T = b.EventT;
            call.AltM = b.Alt;
            call.SpeedMps = b.Speed;
            return true;
        }

        /// <summary>Screen-y of the callout inside the padded HUD, just under the plot and stages cards.</summary>
        public static float CalloutTopInsideHud()
        {
            return FlightHudLayout.HealthPt + FlightHudLayout.GapPt
                + FlightHudLayout.TelemetryPt + FlightHudLayout.GapPt
                + FlightHudLayout.PlotPt + FlightHudLayout.GapPt;
        }

        public static string Title(StagingKind kind)
        {
            switch (kind)
            {
                case StagingKind.Meco: return "MECO";
                case StagingKind.StageSep: return "STAGE SEP";
                case StagingKind.Ignition: return "IGNITION";
                case StagingKind.FairingSep: return "FAIRING SEP";
                default: return "";
            }
        }

        public static string NarrationId(StagingKind kind)
        {
            switch (kind)
            {
                case StagingKind.Meco: return "meco";
                case StagingKind.StageSep: return "sep";
                case StagingKind.Ignition: return "ses1";
                case StagingKind.FairingSep: return "fairing";
                default: return "";
            }
        }

        public static string Format(string title, double t, double altM, double speed)
        {
            var inv = CultureInfo.InvariantCulture;
            string alt = (altM / 1000.0).ToString("0.0", inv) + "km";
            string spd = speed >= 1000.0
                ? (speed / 1000.0).ToString("0.00", inv) + "km/s"
                : speed.ToString("0", inv) + "m/s";
            return (title ?? "") + "  T+" + t.ToString("0.0", inv) + "  " + alt + "  " + spd;
        }

        static SepPose Integrate(string role, int index, int count, double age, bool reduced, bool recoverBooster)
        {
            if (age < 0) age = 0;
            if (count < 1) count = 1;
            bool booster = role == "booster";
            if (!booster && index == 0 && recoverBooster)
                return FlipAndFall(age, reduced);
            double ang = index * 2 * Math.PI / count;
            double radial = booster ? SepRadialMps : 1.6;
            double x = Math.Cos(ang) * radial * age;
            double z = Math.Sin(ang) * radial * age * (booster ? 1 : 0.25);
            if (!booster)
            {
                x = 1.6 * age;
                z = 0.45 * age * (index % 2 == 0 ? 1 : -1);
            }
            double y = -SepPushMps * age - 0.5 * FallAccel * age * age;
            var pose = new SepPose { Released = true, Age = age, X = x, Y = y, Z = z };
            if (!reduced)
            {
                double spin = TumbleDeg * (1 + 0.13 * (Math.Abs(index) % 3));
                pose.Pitch = spin * age;
                pose.Yaw = (index % 2 == 0 ? 9.0 : -9.0) * age;
                pose.Roll = (booster ? 22.0 : 14.0) * age;
            }
            return pose;
        }

        /// <summary>Engines pitch toward the ground, then the stage falls. No wild tumble.</summary>
        static SepPose FlipAndFall(double age, bool reduced)
        {
            if (age < 0) age = 0;
            double u = age / FlipSec;
            if (u < 0) u = 0;
            if (u > 1) u = 1;
            double s = u * u * (3 - 2 * u);
            return new SepPose
            {
                Released = true,
                Age = age,
                X = 1.2 * age,
                Y = -3.2 * age - 0.5 * 7.5 * age * age,
                Z = 0.15 * age,
                Pitch = reduced ? 0 : 180.0 * s,
                Yaw = reduced ? 0 : 6.0 * s,
                Roll = 0,
            };
        }

        void Ingest(SimEvent e, double alt, double speed)
        {
            if (e == null || string.IsNullOrEmpty(e.type)) return;
            if (e.type == "burnout" || e.type == "cutoff") Add(StagingKind.Meco, e, alt, speed);
            else if (e.type == "separation") Add(StagingKind.StageSep, e, alt, speed);
            else if (e.type == "ignition")
            {
                int stage = e.stage ?? 0;
                if (stage > 1) Add(StagingKind.Ignition, e, alt, speed);
            }
            else if (e.type == "fairing") AddFairing(e.t, e.alt ?? alt, e.v ?? speed, e.stage ?? 0);
        }

        void Add(StagingKind kind, SimEvent e, double liveAlt, double liveSpeed)
        {
            int stage = e.stage ?? 0;
            // burnout.stage and separation.stage are both the spent serial + 1 (separation stores the
            // post-increment StageIdx, which matches the 1-based burnout stage). Ignition.stage is the
            // lighting stage, so serial = stage - 1 is the stage that just lit.
            int serial = stage > 0 ? stage - 1 : -1;
            var beat = new Beat
            {
                Kind = kind,
                Stage = stage,
                Serial = serial,
                EventT = e.t,
                Alt = e.alt ?? liveAlt,
                Speed = e.v ?? liveSpeed,
                NarrationId = NarrationId(kind),
            };
            beats.Add(beat);
            cues.Add(ToCue(beat));
        }

        void AddFairing(double t, double alt, double speed, int stage)
        {
            if (fairingT >= 0) return;
            fairingT = t;
            var beat = new Beat
            {
                Kind = StagingKind.FairingSep,
                Stage = stage,
                Serial = -1,
                EventT = t,
                Alt = alt,
                Speed = speed,
                NarrationId = "fairing",
            };
            beats.Add(beat);
            cues.Add(ToCue(beat));
        }

        void NoteFairing(double t, double alt, double speed, double q)
        {
            if (!hasFairing || fairingT >= 0) return;
            if (q > qPeak) qPeak = q;
            if (!qPassed && qPeak >= QArmPa && q < qPeak * QDrop) qPassed = true;
            if (!qPassed || alt < fairingAltM) return;
            AddFairing(t, alt, speed, 0);
        }

        static StagingCue ToCue(Beat b) => new StagingCue
        {
            NarrationId = b.NarrationId,
            Kind = b.Kind,
            Stage = b.Stage,
            T = b.EventT,
            AltM = b.Alt,
            SpeedMps = b.Speed,
        };

        int Latest(StagingKind kind, double t)
        {
            int hit = -1;
            for (int i = 0; i < beats.Count; i++)
            {
                if (beats[i].Kind != kind || beats[i].EventT > t + 1e-9) continue;
                hit = i;
            }
            return hit;
        }

        int Next(int after, StagingKind kind)
        {
            double t = beats[after].EventT;
            for (int i = after + 1; i < beats.Count; i++)
            {
                if (beats[i].Kind != kind) continue;
                if (beats[i].EventT + 1e-3 < t) continue;
                return i;
            }
            return -1;
        }

        void Window(int i, out double start, out double end)
        {
            var b = beats[i];
            bool sep = Cluster(i, StagingKind.StageSep);
            bool ign = Cluster(i, StagingKind.Ignition);
            if (b.Kind == StagingKind.Meco)
            {
                start = b.EventT;
                end = b.EventT + (sep ? SepDelaySec : CalloutSec);
            }
            else if (b.Kind == StagingKind.StageSep)
            {
                start = b.EventT + SepDelaySec;
                end = ign ? b.EventT + IgniteDelaySec : start + CalloutSec;
            }
            else if (b.Kind == StagingKind.Ignition)
            {
                start = b.EventT + IgniteDelaySec;
                end = start + CalloutSec;
            }
            else
            {
                start = b.EventT;
                end = start + CalloutSec;
            }
        }

        bool Cluster(int i, StagingKind kind)
        {
            double t = beats[i].EventT;
            for (int j = 0; j < beats.Count; j++)
            {
                if (beats[j].Kind != kind) continue;
                if (Math.Abs(beats[j].EventT - t) <= 1e-3) return true;
            }
            return false;
        }

        static double Clamp01(double v)
        {
            if (double.IsNaN(v) || v < 0) return 0;
            if (v > 1) return 1;
            return v;
        }
    }
}
