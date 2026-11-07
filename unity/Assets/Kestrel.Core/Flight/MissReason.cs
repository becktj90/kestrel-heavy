// F4: why an intact flight missed the mission, in one plain sentence plus what to change, with the flight's
// own numbers. Pure: the same gates as FlightDebrief.GoalMet, so the report never explains a miss the
// score did not count. One reason shows, picked in fix order (orbit, fuel, structure, reliability, setup),
// and "(+N more issues)" counts the other gates that missed. A failure event explains itself, and a pass
// has no reason.
// The circle advice comes from flying the board starters: a top stage that overshoots the high point
// with the low point on target rounds the orbit off with 2 to 10% more upper fuel (MissReasonTests). #313: one that
// cuts off cleanly but tilted (flight-path angle off zero, high point over and low point under) gets the steep or
// flat advice instead, each flown on the starters it names.
using System;
using System.Collections.Generic;
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
        /// <summary>#313: flight-path angle at a clean engine cutoff, degrees (+ climbing, − diving); NaN on a burnout or failure.</summary>
        public double CutoffGammaDeg = double.NaN;
        /// <summary>The pitch kick the flown design launched with, degrees; NaN when unknown. The flat/steep advice
        /// names it, so the lever is the design's own kick: the old constant text always named 6°, which on an 8.65°
        /// design told the player to set 6.5° — steeper, the opposite of the fix.</summary>
        public double PitchKickDeg = double.NaN;
        /// <summary>Flown from the Starter screen (fuel, stages, engine fuel and boosters, plus Reset): advice that needs a
        /// control the Starter does not have (pitch kick, one stage's fuel) names the Starter's own instead.</summary>
        public bool Starter;
        /// <summary>#341: the closed-loop guidance computer was fitted. Without it every field below is ignored and the
        /// explanation is exactly the basic one.</summary>
        public bool GuidanceComputer;
        /// <summary>#341: the top stage burned dry before the cutoff energy while the guidance computer flew it.</summary>
        public bool UpperOut;
        /// <summary>#341: the state stage 1 handed the guidance computer: altitude (m), flight-path angle (deg), and thrust
        /// acceleration over net gravity (g − vt²/r). NaN when unknown.</summary>
        public double HandoffAltM = double.NaN, HandoffGammaDeg = double.NaN, HandoffLiftRatio = double.NaN;
    }

    public sealed class MissExplanation
    {
        /// <summary>Which gate missed: upper-out, upper-out-flat-handoff (#341, guidance computer only), altitude-low, altitude-high, fpa-steep, fpa-steep-suborbital, fpa-flat, suborbital-short, suborbital-steep, apo-high, peri-low, orbit-low, speed, g, landing, boosters, margin, reliability, red-risk, boil-off, gentle, payload, fairing, budget.</summary>
        public string Code = "";
        public string Reason = "";
        public string Advice = "";
        /// <summary>How many other gates also missed. The reason ends with "(+N more issues)" when this is above 0.</summary>
        public int More;
        /// <summary>The other gates that missed, in fix order.</summary>
        public readonly List<string> Others = new List<string>();
        public string Line => string.IsNullOrEmpty(Advice) ? Reason : Reason + " " + Advice;
    }

    public static class MissReason
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        /// <summary>economy reliability.highSafetyFactor. MissReasonTests pins it to the catalog.</summary>
        public const double SafetyBonusAt = 1.5;

        /// <summary>#313: |γ| at a clean cutoff past this, with the high point over the band and the low point under it, is a flight-path-angle miss.</summary>
        public const double FlightPathAngleOffDeg = 0.5;
        /// <summary>#313 (Physics): the steep advice's lever is a smaller pitch kick. MissReasonTests flies it on KH-12's 6° build:
        /// 4.5° lands every seed and the live pad (calm and seed-1 wind); 4° and 5° miss. One more upper engine is not the lever:
        /// at 32,000 kg it tips KH-12 into a dive (753 x 447 km). The constant below is the unknown-kick fallback;
        /// <see cref="SteepAdviceFor"/> names the flown kick when it is known.</summary>
        public const string SteepAdvice = "Your rocket flew too steep. Use a smaller pitch kick at launch (for example 6° → 4.5° in Guidance).";
        /// <summary>#313 (Physics): the flat advice's lever is a larger pitch kick. MissReasonTests flies it on the KH-3 (8° → 8.5°)
        /// and KH-4 (6° → 6.5°) starters. The constant below is the unknown-kick fallback;
        /// <see cref="FlatAdviceFor"/> names the flown kick when it is known.</summary>
        public const string FlatAdvice = "Your rocket flew too flat. Use a larger pitch kick at launch (for example 6° → 6.5° in Guidance).";
        /// <summary>The Starter screen's lever for an orbit it flew off: its default rocket wins every mission (DECISIONS #75).</summary>
        public const string StarterReset = "On the Starter screen, tap Reset for the rocket that makes this orbit.";
        /// <summary>The flat advice's lever: the flown kick plus this, degrees. One designer step (DesignerKnobs.PitchDegStep).</summary>
        public const double FlatKickStepDeg = 0.5;
        /// <summary>The steep advice's lever: the flown kick minus this, degrees.</summary>
        public const double SteepKickStepDeg = 1.5;

        /// <summary>The flat advice for the kick the design actually flew, or <see cref="FlatAdvice"/> when it is unknown.</summary>
        public static string FlatAdviceFor(double kickDeg)
            => double.IsNaN(kickDeg) ? FlatAdvice
                : "Your rocket flew too flat. Use a larger pitch kick at launch (for example " + Kick(kickDeg) + "° → " + Kick(kickDeg + FlatKickStepDeg) + "° in Guidance).";
        /// <summary>The steep advice for the kick the design actually flew, or <see cref="SteepAdvice"/> when it is unknown.</summary>
        public static string SteepAdviceFor(double kickDeg)
            => double.IsNaN(kickDeg) ? SteepAdvice
                : "Your rocket flew too steep. Use a smaller pitch kick at launch (for example " + Kick(kickDeg) + "° → " + Kick(kickDeg - SteepKickStepDeg) + "° in Guidance).";
        /// <summary>One decimal when needed, so an integer kick reads as the old constant text did (6°, not 6.0°) and
        /// 8.65° reads as the designer shows it (8.7°).</summary>
        static string Kick(double deg) => Num(deg, "0.#");

        static bool FlightPathAngleOff(double gammaDeg) => !double.IsNaN(gammaDeg) && Math.Abs(gammaDeg) >= FlightPathAngleOffDeg;

        public static MissInput FromFlight(CampaignFlight flight, int stages, double pitchKickDeg = double.NaN)
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
                CutoffGammaDeg = flight.cutoffGammaDeg,
                PitchKickDeg = pitchKickDeg,
                GuidanceComputer = flight.guidanceComputer,
                UpperOut = flight.guidanceComputer && flight.upperOut,
                HandoffAltM = flight.handoffAltKm * 1000,
                HandoffGammaDeg = flight.handoffGammaDeg,
                HandoffLiftRatio = flight.handoffLiftRatio,
            };
        }

        /// <summary>Null when there is nothing to explain: no mission, a failure event, or the goal was met.</summary>
        public static MissExplanation Explain(CampaignMission row, MissInput f)
        {
            // #341: with the guidance computer fitted, a top stage that burned dry is explained by its cause, including the
            // prop-depletion failure the monitor files for it. Without the part this branch never runs.
            if (row != null && f != null && f.GuidanceComputer && f.UpperOut
                && (string.IsNullOrEmpty(f.FailureKind) || f.FailureKind == "prop-depletion"))
            {
                bool met = string.IsNullOrEmpty(f.FailureKind) && f.Intact
                    && FlightDebrief.GoalMet(row, true, f.ApoM, f.PeriM, f.MaxG, f.LandingGrade, f.Extra);
                if (!met) return UpperOutReason(f, "stage " + Math.Max(1, f.Stages).ToString(Inv));
            }
            if (row == null || f == null || !f.Intact || !string.IsNullOrEmpty(f.FailureKind)) return null;
            if (FlightDebrief.GoalMet(row, true, f.ApoM, f.PeriM, f.MaxG, f.LandingGrade, f.Extra)) return null;
            var extra = f.Extra;
            string top = "stage " + Math.Max(1, f.Stages).ToString(Inv);

            // Every gate that missed, in fix order: orbit, then fuel, then structure, then reliability, then the
            // flight's setup. The report shows the first one and counts the rest, so a fix never hides behind
            // a later gate (KH-8 says orbit first and reliability only once the fuel fix lands it).
            var misses = new List<MissExplanation>();
            void Add(MissExplanation x) { if (x != null) misses.Add(x); }

            // Orbit.
            Add(Target(row, f, top));
            // Fuel.
            if (extra != null && row.MarginNonNegative && !double.IsNaN(extra.Margin) && !(extra.Margin >= 0))
                Add(Miss("margin", "The speed budget was " + Pct(extra.Margin) + " short; this mission needs it covered.",
                    "Add fuel or a stage until the budget reads 0% or better."));
            if (extra != null && row.CoastHours > 0 && !double.IsNaN(extra.BoilOffKg) && !(extra.BoilOffKg >= 0))
                Add(Miss("boil-off", "The top stage could not hold its fuel through the " + Num(row.CoastHours, "0.#") + " h coast.",
                    "Pick a tank that keeps fuel cold longer (Tanks tab)."));
            if (row.LandingRequired && !MissionScore.IsGood(f.LandingGrade))
                Add(Miss("landing", "The landing graded " + Grade(f.LandingGrade) + "; this mission needs a good landing.",
                    "Keep more landing fuel in the booster (Landing tab)."));
            string boosters = extra != null && extra.BoosterKnown ? extra.BoosterGrade : f.LandingGrade;
            if (row.LandBoosters && !MissionScore.IsGood(boosters))
                Add(Miss("boosters", "The side boosters landed " + Grade(boosters) + "; this mission needs them back in good shape.",
                    "Keep more landing fuel in the boosters (Landing tab)."));
            // Structure.
            if (!double.IsNaN(row.MaxG) && f.MaxG > row.MaxG)
                Add(Miss("g", "Peak load was " + G(f.MaxG) + "; this cargo is rated for " + G(row.MaxG) + ".",
                    "Lower the g limit in Guidance, or throttle the top stage down."));
            if (extra != null && row.NoRedRisks && extra.RedKnown && extra.RedRisk)
                Add(Miss("red-risk", "A red risk was showing at launch; this mission only counts a flight with none.",
                    "Clear every red risk in the designer before you launch."));
            // Reliability.
            if (extra != null && !double.IsNaN(row.ReliabilityMin) && !double.IsNaN(extra.Reliability) && !(extra.Reliability >= row.ReliabilityMin))
                Add(Miss("reliability", "Reliability was " + Pct(extra.Reliability, row.ReliabilityMin) + "; this mission needs " + Pct(row.ReliabilityMin) + ".",
                    "Raise the safety factor to " + Num(SafetyBonusAt, "0.0#") + " or more for the reliability bonus, or pick redundant avionics."));
            // Setup.
            if (extra != null && extra.AirframeKnown)
            {
                if (extra.Gentle) Add(Miss("gentle", "Gentle physics was on, so this flight does not count for the mission.", "Turn gentle physics off and fly again."));
                if (!extra.PayloadMatch) Add(Miss("payload", "The rocket carried a different payload from the one this mission asks for.", "Load the mission's payload (Mission tab)."));
                if (!extra.FairingFits) Add(Miss("fairing", "The cargo did not fit inside the nose cone.", "Pick a wider or longer fairing (Mission tab)."));
                if (!extra.UnderBudget) Add(Miss("budget", "The rocket cost more than this mission's " + Num(row.BudgetKc, "#,0") + " Kc budget.", "Use cheaper parts or a smaller rocket."));
            }

            if (misses.Count == 0) return Miss("goal", "The flight did not meet this mission's goal.", "");
            var first = misses[0];
            first.More = misses.Count - 1;
            for (int i = 1; i < misses.Count; i++) first.Others.Add(misses[i].Code);
            if (first.More > 0) first.Reason += " " + MoreNote(first.More);
            return first;
        }

        /// <summary>"(+1 more issue)" or "(+N more issues)".</summary>
        public static string MoreNote(int more)
            => more <= 0 ? "" : "(+" + more.ToString(Inv) + (more == 1 ? " more issue)" : " more issues)");

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
                    // #341: the guidance computer zeroes γ at its cutoff, so a clean cutoff never gets the kick advice with it fitted.
                    if (!f.GuidanceComputer && f.ApoM > apoGoal && !apoOk && !periOk && f.PeriM < periGoal && FlightPathAngleOff(f.CutoffGammaDeg))
                    {
                        // #313: the engines cut off cleanly with the orbit's energy, but tilted, so the high point is
                        // far above the goal and the low point below it. More fuel does not round it off when climbing.
                        bool steep = f.CutoffGammaDeg > 0;
                        string tilt = steep ? "still climbing, " + Num(f.CutoffGammaDeg, "0.0") + "° up"
                            : "already diving, " + Num(-f.CutoffGammaDeg, "0.0") + "° down";
                        string reason = "Missed the " + Km(apoGoal) + " circle: the orbit flew " + orbit + ", and the high point needs " + Band(apoGoal)
                            + ". The low point, " + Km(f.PeriM) + ", needs " + Band(periGoal) + ". The engines cut off while it was " + tilt + ".";
                        // Fell back (low point under the ground): on the KH-6 fixtures neither lever above moves it; only a much
                        // longer upper burn does (2x to 3x the fuel, FixItRepairTests). Above the ground: the steep levers.
                        if (steep && f.PeriM < 0)
                            return Miss("fpa-steep-suborbital", reason, f.Starter
                                ? "Your rocket flew too steep and fell back down. " + StarterReset
                                : "Your rocket flew too steep and fell back down. Give " + top + " much more fuel (it can take double or more), so it burns long enough to level out.");
                        // The Starter has no pitch-kick control: its lever is Reset (every Starter default wins its mission).
                        if (f.Starter)
                            return Miss(steep ? "fpa-steep" : "fpa-flat", reason, (steep ? "Your rocket flew too steep. " : "Your rocket flew too flat. ") + StarterReset);
                        return steep
                            ? Miss("fpa-steep", reason, SteepAdviceFor(f.PitchKickDeg))
                            : Miss("fpa-flat", reason, FlatAdviceFor(f.PitchKickDeg));
                    }
                    // Trevor 11-07: the low point under the ground after climbing past the band is not an orbit that
                    // "overshot". It never went around: it fell back. Say so, and give the lever that helps.
                    if (f.PeriM < 0 && f.ApoM > -Gravity.R_EARTH && f.ApoM >= apoGoal * (1 - MissionScore.AltitudeFraction))
                        return Suborbital(f, apoGoal, periGoal, top);
                    if (f.ApoM > apoGoal && !apoOk)
                    {
                        // Speed at the low point above what an orbit with the goal's high point needs there.
                        double extra = PeriSpeed(f.PeriM, f.ApoM) - PeriSpeed(f.PeriM, Math.Max(f.PeriM, apoGoal));
                        string need = periOk || Band(periGoal) != Band(apoGoal)
                            ? ", and the high point needs " + Band(apoGoal) + "."
                            : ", and both points need " + Band(apoGoal) + ".";
                        string low = periOk || Band(periGoal) == Band(apoGoal) ? "" : " The low point, " + Km(f.PeriM) + ", needs " + Band(periGoal) + ".";
                        return Miss("apo-high", "Missed the " + Km(apoGoal) + " circle: the orbit flew " + orbit + need + " That is about " + Speed(extra) + " too fast at the low point." + low,
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

        /// <summary>#341: below this thrust over net gravity at the hand-off, together with a hand-off under
        /// <see cref="HandoffSteepDeg"/>, the upper stage was left too low and flat: holding the pitch limit to climb burns its fuel.</summary>
        public const double HandoffLiftWeak = 1.0;
        /// <summary>#341: KH-16 (220 kN on 52.6 t) lands every seed with a hand-off at 10° or more up and runs dry at 7.9° and below.</summary>
        public const double HandoffSteepDeg = 9.0;
        /// <summary>#341 (Physics ruling 6): "upper stage ran out of propellant", with its cause. Only for a flight with the guidance
        /// computer fitted. KH-16 at a 10.5° kick is the reference: stage 1 hands over at 71 km, 5.9° up, the 220 kN top stage
        /// has 0.73 of its net weight in thrust, and the computer spends the burn at its pitch limit climbing, so fuel is not the lever.</summary>
        public static MissExplanation UpperOutReason(MissInput f, string top)
        {
            string head = "The top stage ran out of propellant before it reached orbit speed.";
            bool flatHandoff = !double.IsNaN(f.HandoffLiftRatio) && f.HandoffLiftRatio < HandoffLiftWeak
                && !double.IsNaN(f.HandoffGammaDeg) && f.HandoffGammaDeg < HandoffSteepDeg;
            if (flatHandoff)
                return Miss("upper-out-flat-handoff",
                    head + " Stage 1 let go at " + Km(f.HandoffAltM) + ", only " + Num(f.HandoffGammaDeg, "0.0") + "° above flat. Up there "
                    + top + " pushes only " + Num(f.HandoffLiftRatio * 100, "0") + "% as hard as it takes to hold its height, so the guidance computer"
                    + " tipped it up to climb and spent the propellant on height instead of speed.",
                    "Let stage 1 climb higher and steeper before it lets go: use a smaller pitch kick at launch, or give " + top + " a stronger engine. More fuel in "
                    + top + " is not the fix.");
            return Miss("upper-out", head, "Add fuel to " + top + ", or add another stage.");
        }

        /// <summary>
        /// Trevor 11-07: a circular mission's flight that climbed past the band and fell back (low point under the ground).
        /// The reason says it never reached orbit and how much sideways speed it lacked at the top (circular speed at the
        /// flown peak minus the speed it had there). The lever comes from the orbit's energy: a mean altitude, (apo + peri) / 2,
        /// under the band ran out of push (more fuel or a stage, "suborbital-short"); one at or over it had the energy but
        /// climbed too steeply (a larger pitch kick turns it sideways sooner, "suborbital-steep"). KH-3's Starter at
        /// Kerosene and the lowest fuel (470 x -394 km in calm air) is the short case; at the next fuel dot (408 x -8 km)
        /// it is the steep one. A Starter flight has no kick or per-stage fuel control, so its advice is the Starter's own
        /// controls, ending with Reset (every Starter default wins its mission, StarterDefaultWindGateTests).
        /// </summary>
        static MissExplanation Suborbital(MissInput f, double apoGoal, double periGoal, string top)
        {
            double ra = Gravity.R_EARTH + f.ApoM;
            double shortBy = Math.Sqrt(Gravity.MU_EARTH / ra) - ApoSpeed(f.PeriM, f.ApoM);
            string band = Band(periGoal) == Band(apoGoal) ? Band(apoGoal) : Band(periGoal) + " at the low point";
            string reason = "Didn't reach orbit. It climbed to " + Km(f.ApoM) + " but wasn't going fast enough sideways, so it fell back down"
                + " (about " + Speed(shortBy) + " short at the top). The " + Km(apoGoal) + " circle needs " + band + " all the way round.";
            double mean = 0.5 * (f.ApoM + f.PeriM);
            bool ranOut = mean < 0.5 * (apoGoal + periGoal) * (1 - MissionScore.AltitudeFraction);
            if (ranOut)
                return Miss("suborbital-short", reason, f.Starter
                    ? "It ran out of push. Try more fuel or another stage. " + StarterReset
                    : "It ran out of push before it was going fast enough. Add fuel to " + top + ", or add a stage.");
            string steep = "It had the push but climbed too steeply.";
            return Miss("suborbital-steep", reason, f.Starter
                ? steep + " " + StarterReset
                : steep + " Use a larger pitch kick at launch so it turns sideways sooner"
                    + (double.IsNaN(f.PitchKickDeg) ? "." : " (for example " + Kick(f.PitchKickDeg) + "° → " + Kick(f.PitchKickDeg + FlatKickStepDeg) + "° in Guidance)."));
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
