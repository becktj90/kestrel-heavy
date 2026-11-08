// DECISIONS #83 (UX review #416 item 5): one verdict source. The designer and Starter badge, the count card's spare or
// short line, the in-flight speed verdict, the medal and the result screen all read whether the flight passes from here,
// so "Ready" never sits next to "89 m/s short", a doomed flight never shows a medal, and a pass never reads short.
// Before launch the source is the launch predictor's flight (ReferenceAscent, the live pad's director and checks) judged
// by the batch's own acceptance rule (LaunchPredictor.AcceptsMission); after the flight it is the flown result.
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    public enum VerdictState { Checking, Pass, Fail }

    public sealed class FlightVerdict
    {
        public VerdictState State;
        /// <summary>The predictor's cause for a fail (StarterVerdict cause ids), or null.</summary>
        public string Cause;

        public bool Known => State != VerdictState.Checking;
        /// <summary>"Ready" is shown only when this is true: the predictor's flight passes the mission.</summary>
        public bool Passes => State == VerdictState.Pass;

        public static readonly FlightVerdict Checking = new FlightVerdict { State = VerdictState.Checking };
        public static FlightVerdict Pass() => new FlightVerdict { State = VerdictState.Pass };
        public static FlightVerdict Fail(string cause) => new FlightVerdict { State = VerdictState.Fail, Cause = cause };

        // ---- the remembered predictor verdict, by ReferenceAscent.CanonicalKey (the Starter job and the predictor write it) ----

        static readonly Dictionary<string, FlightVerdict> Remembered = new Dictionary<string, FlightVerdict>();
        static readonly object Gate = new object();

        public static void Remember(string key, FlightVerdict v)
        {
            if (key == null || v == null) return;
            lock (Gate) Remembered[key] = v;
        }

        /// <summary>The remembered verdict for a design key, else <see cref="Checking"/>.</summary>
        public static FlightVerdict Recall(string key)
        {
            if (key == null) return Checking;
            lock (Gate) return Remembered.TryGetValue(key, out var v) ? v : Checking;
        }

        // ---- the pass rule: the result screen's own gates ----

        /// <summary>The result screen's gates (PostFlightSource → <see cref="FlightDebrief.GoalMet(CampaignMission, bool, double,
        /// double, double, string, GoalCheck)"/>) for <paramref name="design"/> flying <paramref name="result"/> on
        /// <paramref name="row"/>: the orbit or apogee band, g, landings, margin, reliability, red risks, coast boil-off and
        /// the airframe (gentle, payload, fairing, budget). "Ready" and the debrief both read this.
        /// <paramref name="landingJudged"/>: the caller has already judged the landing reserve (the Starter verdict's
        /// landing causes: "short" cannot land and fails; a tight "hard" reserve still lands, the documented KH-13 case),
        /// so the predicted grade is not a second gate. The flown landing grades the real result.</summary>
        public static bool ResultPasses(Catalog catalog, CampaignMission row, Design design, SimResult result, bool landingJudged = false)
        {
            if (row == null || design == null || result == null) return false;
            bool intact = result.failure == null && result.outcome != "failure" && result.outcome != "crash";
            if (!intact) return false;
            double required = row.RequiredDeltaV();
            var extra = new GoalCheck
            {
                IdealDv = result.idealDeltaV,
                DepartureDv = result.departureDv,
                DepartureTW = result.departureTW,
                C3Km2PerS2 = MissionScore.C3(result.orbit.energy) / 1e6,
                Margin = required > 1 ? (result.idealDeltaV - required) / required : 0,
            };
            if (result.risks != null)
            {
                extra.RedKnown = true;
                for (int i = 0; i < result.risks.Count; i++)
                    if (result.risks[i].level == "red") extra.RedRisk = true;
            }
            extra.Reliability = Economy.Reliability.Of(catalog, design).reliability;
            extra.Gentle = FailureMonitor.GentleTier(design);
            extra.PayloadMatch = design.payloadType == row.PayloadId && design.payloadMass == row.MassKg;
            extra.FairingFits = PayloadFit.Check(design, new PayloadSpec { id = row.PayloadId, volume = row.VolumeM3, diameter = row.DiameterM }).fits;
            var price = Economy.CostModel.PriceDesign(catalog, design);
            long per = Economy.CostModel.MinorPerKc(catalog);
            extra.UnderBudget = price.recurring <= Economy.Career.KcToMinor(row.BudgetKc, per > 0 ? per : 1);
            extra.AirframeKnown = true;
            if (row.CoastHours > 0 && design.stages != null && design.stages.Count > 0)
            {
                var upper = design.stages[design.stages.Count - 1];
                extra.BoilOffKg = upper.propMass - TankModel.RemainingPropKg(catalog, upper, row.CoastHours);
            }
            string grade = row.LandingRequired || row.LandBoosters
                ? (landingJudged ? "good" : FlightBatch.PredictLandingGrade(catalog, design, result, row.LandingMode, false)) : null;
            if (row.LandBoosters)
            {
                extra.BoosterKnown = true;
                extra.BoosterGrade = landingJudged ? "good" : FlightBatch.PredictLandingGrade(catalog, design, result, row.LandingMode, true);
            }
            return FlightDebrief.GoalMet(row, intact, MissionScore.FlownApoAltM(result), result.orbit.periAlt, result.maxG, grade, extra);
        }

        // ---- the consumers ----

        /// <summary>The medal a flight keeps: none on a failed flight.</summary>
        public static AscentMedal Medal(AscentMedal timing, bool passed) => passed ? timing : AscentMedal.None;

        /// <summary>In flight the medal is not known yet (the flight may still fail), so the line never names one.</summary>
        public const string InFlightMedalLine = "Flying the plan.";

        public const string PredictPass = "Test flight reaches {0}", PredictFail = "Test flight misses {0}";

        /// <summary>The count card's detail line. No sheet Δv number: the predictor's flight says pass or miss. Null when
        /// the verdict is not known (the caller keeps its own line).</summary>
        public static string CountLine(FlightVerdict v, string trip)
        {
            if (v == null || !v.Known) return null;
            return string.Format(v.Passes ? PredictPass : PredictFail, string.IsNullOrEmpty(trip) ? "this trip" : trip);
        }
    }
}
