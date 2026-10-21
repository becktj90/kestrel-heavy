// F4: why an intact flight missed the mission, in one plain sentence plus what to change, with the flight's
// own numbers. Pure: the same gates as FlightDebrief.GoalMet, in the same order, so the report never
// explains a miss the score did not count. A failure event explains itself, and a pass has no reason.
// The circle advice comes from flying the board starters: a top stage that overshoots the high point
// with the low point on target rounds the orbit off with 2 to 10% more upper fuel (MissReasonTests).
using System;
using System.Globalization;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Flight
{
    /// <summary>What the flight did, in the shape <see cref="FlightDebrief.GoalMet"/> reads.</summary>
    public sealed class MissInput
    {
        public bool Intact = true;
        public string FailureKind;
        public double ApoM = double.NaN, PeriM = double.NaN, MaxG = double.NaN;
        public string LandingGrade;
        public GoalCheck Extra;
        /// <summary>Core stages on the design (1 for a single-stage hop). The advice names the top one.</summary>
        public int Stages = 1;
    }

    public sealed class MissExplanation
    {
        /// <summary>Which gate missed: altitude-low, altitude-high, apo-high, peri-low, orbit-low, speed, g, landing, boosters, margin, reliability, red-risk, boil-off, gentle, payload, fairing, budget.</summary>
        public string Code = "";
        public string Reason = "";
        public string Advice = "";
        public string Line => string.IsNullOrEmpty(Advice) ? Reason : Reason + " " + Advice;
    }

    public static class MissReason
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        /// <summary>economy reliability.highSafetyFactor. MissReasonTests pins it to the catalog.</summary>
        public const double SafetyBonusAt = 1.5;

        public static MissInput FromFlight(CampaignFlight flight, int stages)
        {
            if (flight == null) return null;
            return new MissInput
            {
                Intact = flight.failure == null && flight.outcome != "failure" && flight.outcome != "crash" && flight.outcome != "error",
                FailureKind = flight.failure,
                ApoM = flight.apoKm * 1000,
                PeriM = flight.periKm * 1000,
                MaxG = flight.maxG,
                LandingGrade = flight.landingGrade,
                Extra = FlightDebrief.FromFlight(flight),
                Stages = Math.Max(1, stages),
            };
        }

        /// <summary>Null when there is nothing to explain: no mission, a failure event, or the goal was met.</summary>
        public static MissExplanation Explain(CampaignMission row, MissInput f)
        {
            if (row == null || f == null || !f.Intact || !string.IsNullOrEmpty(f.FailureKind)) return null;
            if (FlightDebrief.GoalMet(row, true, f.ApoM, f.PeriM, f.MaxG, f.LandingGrade, f.Extra)) return null;
            var extra = f.Extra;
            string top = "stage " + Math.Max(1, f.Stages).ToString(Inv);

            var target = Target(row, f, top);
            if (target != null) return target;

            if (!double.IsNaN(row.MaxG) && f.MaxG > row.MaxG)
                return Miss("g", "Peak load was " + G(f.MaxG) + "; this cargo is rated for " + G(row.MaxG) + ".",
                    "Lower the g limit in Guidance, or throttle the top stage down.");
            if (row.LandingRequired && !MissionScore.IsGood(f.LandingGrade))
                return Miss("landing", "The landing graded " + Grade(f.LandingGrade) + "; this mission needs a good landing.",
                    "Keep more landing fuel in the booster (Landing tab).");
            string boosters = extra != null && extra.BoosterKnown ? extra.BoosterGrade : f.LandingGrade;
            if (row.LandBoosters && !MissionScore.IsGood(boosters))
                return Miss("boosters", "The side boosters landed " + Grade(boosters) + "; this mission needs them back in good shape.",
                    "Keep more landing fuel in the boosters (Landing tab).");
            if (extra == null) return Miss("goal", "The flight did not meet this mission's goal.", "");
            if (row.MarginNonNegative && !double.IsNaN(extra.Margin) && !(extra.Margin >= 0))
                return Miss("margin", "The speed budget was " + Pct(extra.Margin) + " short; this mission needs it covered.",
                    "Add fuel or a stage until the budget reads 0% or better.");
            if (!double.IsNaN(row.ReliabilityMin) && !double.IsNaN(extra.Reliability) && !(extra.Reliability >= row.ReliabilityMin))
                return Miss("reliability", "Reliability was " + Pct(extra.Reliability, row.ReliabilityMin) + "; this mission needs " + Pct(row.ReliabilityMin) + ".",
                    "Raise the safety factor to " + Num(SafetyBonusAt, "0.0#") + " or more for the reliability bonus, or pick redundant avionics.");
            if (row.NoRedRisks && extra.RedKnown && extra.RedRisk)
                return Miss("red-risk", "A red risk was showing at launch; this mission only counts a flight with none.",
                    "Clear every red risk in the designer before you launch.");
            if (row.CoastHours > 0 && !double.IsNaN(extra.BoilOffKg) && !(extra.BoilOffKg >= 0))
                return Miss("boil-off", "The top stage could not hold its fuel through the " + Num(row.CoastHours, "0.#") + " h coast.",
                    "Pick a tank that keeps fuel cold longer (Tanks tab).");
            if (extra.AirframeKnown)
            {
                if (extra.Gentle) return Miss("gentle", "Gentle physics was on, so this flight does not count for the mission.", "Turn gentle physics off and fly again.");
                if (!extra.PayloadMatch) return Miss("payload", "The rocket carried a different payload from the one this mission asks for.", "Load the mission's payload (Mission tab).");
                if (!extra.FairingFits) return Miss("fairing", "The cargo did not fit inside the nose cone.", "Pick a wider or longer fairing (Mission tab).");
                if (!extra.UnderBudget) return Miss("budget", "The rocket cost more than this mission's " + Num(row.BudgetKc, "#,0") + " Kc budget.", "Use cheaper parts or a smaller rocket.");
            }
            return Miss("goal", "The flight did not meet this mission's goal.", "");
        }

        static MissExplanation Target(CampaignMission row, MissInput f, string top)
        {
            double apoGoal = row.ApoKm * 1000, periGoal = row.PeriKm * 1000;
            bool apoOk = MissionScore.AltitudeWithin(f.ApoM, apoGoal);
            bool periOk = MissionScore.AltitudeWithin(f.PeriM, periGoal);
            switch (row.TargetKind)
            {
                case "apogee":
                    if (apoOk) return null;
                    if (f.ApoM < apoGoal)
                        return Miss("altitude-low", "Topped out at " + Km(f.ApoM) + " of " + Km(apoGoal) + " (it needs " + Band(apoGoal) + ").",
                            "Add fuel to " + top + ", or a bigger engine.");
                    return Miss("altitude-high", "Climbed to " + Km(f.ApoM) + ", past the " + Km(apoGoal) + " goal (it needs " + Band(apoGoal) + ").",
                        "Carry less fuel in " + top + ".");
                case "circular":
                    if (apoOk && periOk) return null;
                    string orbit = Km(f.PeriM) + " × " + Km(f.ApoM);
                    if (f.ApoM > apoGoal && !apoOk)
                    {
                        // Speed at the low point above what an orbit with the goal's high point needs there.
                        double extra = PeriSpeed(f.PeriM, f.ApoM) - PeriSpeed(f.PeriM, Math.Max(f.PeriM, apoGoal));
                        string low = periOk ? "" : " The low point, " + Km(f.PeriM) + ", needs " + Band(periGoal) + ".";
                        return Miss("apo-high", "Missed the " + Km(apoGoal) + " circle: the orbit flew " + orbit + ", and the high point needs " + Band(apoGoal) + ". That is about " + Speed(extra) + " too fast at the low point." + low,
                            "Add fuel to " + top + ", about 2% at a time (up to 10%), until the high point comes down into the band.");
                    }
                    if (apoOk)
                    {
                        // Speed to raise the low point to the goal, added at the high point.
                        double shortBy = ApoSpeed(Math.Max(f.PeriM, periGoal), f.ApoM) - ApoSpeed(f.PeriM, f.ApoM);
                        return Miss("peri-low", "Missed the " + Km(periGoal) + " circle: the orbit flew " + orbit + ", and the low point needs " + Band(periGoal) + ". That is about " + Speed(shortBy) + " short at the high point.",
                            "Add a little fuel to " + top + ".");
                    }
                    {
                        double need = Math.Sqrt(Gravity.MU_EARTH / (Gravity.R_EARTH + apoGoal));
                        double have = f.ApoM > -Gravity.R_EARTH && f.PeriM > -Gravity.R_EARTH ? PeriSpeed(f.PeriM, f.ApoM) : 0;
                        return Miss("orbit-low", "Missed the " + Km(apoGoal) + " circle by about " + Speed(need - have) + ": " + (f.PeriM > 0 ? "the orbit flew " + orbit : "it topped out at " + Km(f.ApoM) + " of " + Km(apoGoal)) + ".",
                            "Add fuel to " + top + ", or another stage.");
                    }
                case "ellipse":
                case "c3":
                {
                    var extra = f.Extra;
                    bool dv = extra != null && !double.IsNaN(extra.IdealDv) && extra.IdealDv + 1e-6 >= row.RequiredDeltaV();
                    bool met = row.TargetKind == "ellipse" ? (apoOk && periOk) || (periOk && dv)
                        : (extra != null && !double.IsNaN(extra.C3Km2PerS2) && MissionScore.WithinFraction(extra.C3Km2PerS2, row.C3Km2PerS2, MissionScore.AltitudeFraction)) || (periOk && dv);
                    if (met) return null;
                    if (!periOk && f.PeriM < periGoal)
                        return Miss("peri-low", "The parking orbit's low point was " + Km(f.PeriM) + "; it needs " + Band(periGoal) + ".",
                            "Add fuel to " + top + ".");
                    if (!dv && extra != null && !double.IsNaN(extra.IdealDv))
                        return Miss("speed", "Short of the transfer speed by " + Speed(row.RequiredDeltaV() - extra.IdealDv) + ".",
                            "Add fuel to " + top + ", or another stage.");
                    return Miss("speed", "The orbit (" + Km(f.PeriM) + " × " + Km(f.ApoM) + ") is not the transfer this mission asks for.", "Add fuel to " + top + ".");
                }
            }
            return null;
        }

        static double PeriSpeed(double periM, double apoM)
        {
            double rp = Gravity.R_EARTH + periM, ra = Gravity.R_EARTH + apoM;
            return Math.Sqrt(Gravity.MU_EARTH * (2 / rp - 2 / (rp + ra)));
        }

        static double ApoSpeed(double periM, double apoM)
        {
            double rp = Gravity.R_EARTH + periM, ra = Gravity.R_EARTH + apoM;
            return Math.Sqrt(Gravity.MU_EARTH * (2 / ra - 2 / (rp + ra)));
        }

        static MissExplanation Miss(string code, string reason, string advice) => new MissExplanation { Code = code, Reason = reason, Advice = advice };

        static string Num(double v, string fmt) => v.ToString(fmt, Inv);
        public static string Km(double m) => Num(m / 1000.0, Math.Abs(m) < 100000 ? "#,0.#" : "#,0") + " km";
        static string Band(double goalM) => Km(goalM * (1 - MissionScore.AltitudeFraction)) + " to " + Km(goalM * (1 + MissionScore.AltitudeFraction));
        public static string Speed(double mps)
        {
            mps = Math.Abs(mps);
            return mps >= 1000 ? Num(mps / 1000.0, "0.0") + " km/s" : Num(Math.Max(1, Math.Round(mps)), "0") + " m/s";
        }
        static string G(double g) => Num(g, "0.0") + " g";
        static string Pct(double f) => Num(f * 100, "0.#") + "%";
        /// <summary>One decimal, or two when one would round the flown value onto the goal.</summary>
        static string Pct(double f, double goal) => Num(f * 100, Num(f * 100, "0.#") == Num(goal * 100, "0.#") ? "0.00" : "0.#") + "%";
        static string Grade(string g) => string.IsNullOrEmpty(g) ? "no landing" : g;
    }
}
