// Batch of headless ascents. The integrator allocates classes and lists, so it is not a Burst job.
// Altitude sampling (Atmosphere.SampleDensity) is the Burst-shaped path; this pool is the managed one.
// Campaign flights force gentle physics off and seed random failures from (design hash, mission seed).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    public sealed class CampaignFlight
    {
        public string missionId, designId, outcome, failure, landingGrade, boosterGrade, criterion;
        public ulong seed;
        public bool passed, gentle, fairingFits, payloadMatch, underBudget, redRisk;
        /// <summary>#313: the target acceptance (MissionScore.TargetMet) alone, before the mission gates.</summary>
        public bool reached;
        public int stars;
        public double score, margin, costKc, budgetKc, apoKm, periKm, incDeg, maxG, idealDv, requiredDv, c3Km2PerS2, reliability, boilOffKg, qPeakKPa, burnoutMps, burnoutAltKm;
        /// <summary>#313 Physics A: departure Δv left in the parking orbit, the burn it needs (with finite-burn loss), and the departing stage's T/W. NaN unless the mission departs from parking.</summary>
        public double departDv = double.NaN, departReqDv = double.NaN, departTW = double.NaN;
        /// <summary>#313: flight-path angle at a clean cutoff, degrees (NaN on burnout or failure).</summary>
        public double cutoffGammaDeg = double.NaN;
        public long costMinor;
        public readonly List<string> redKinds = new List<string>();
    }

    public static class FlightBatch
    {
        public const int ParallelThreshold = 8;

        public static SimResult[] Simulate(IList<SimStage[]> stages, IList<SimOptions> opts)
        {
            var r = new SimResult[stages.Count];
            #if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL has no worker threads: run sequentially.
            if (false)
#else
            if (stages.Count >= ParallelThreshold)
#endif
                Parallel.For(0, stages.Count, i => r[i] = Headless.Simulate(stages[i], opts[i]));
            else
                for (int i = 0; i < stages.Count; i++) r[i] = Headless.Simulate(stages[i], opts[i]);
            return r;
        }

        /// <summary>FNV-1a 64 of the fields that change the airframe. The failure draw is seeded with this and the mission seed.</summary>
        public static ulong DesignHash(Design design)
        {
            ulong h = 14695981039346656037UL;
            void Mix(string s)
            {
                if (s == null) s = "";
                for (int i = 0; i < s.Length; i++) { h ^= s[i]; h *= 1099511628211UL; }
            }
            if (design == null) return 1;
            Mix(design.id);
            Mix(design.payloadType);
            Mix(design.payloadMass.ToString("R", CultureInfo.InvariantCulture));
            Mix(design.avionicsTier);
            if (design.stages != null)
            {
                for (int i = 0; i < design.stages.Count; i++)
                {
                    var s = design.stages[i];
                    Mix(s.engine);
                    Mix(s.engineCount.ToString(CultureInfo.InvariantCulture));
                    Mix(s.propMass.ToString("R", CultureInfo.InvariantCulture));
                    Mix(s.diameter.ToString("R", CultureInfo.InvariantCulture));
                    Mix(s.landingPropMass.ToString("R", CultureInfo.InvariantCulture));
                    Mix(s.propellant);
                }
            }
            if (design.StrapOnCount > 0)
            {
                Mix("strap");
                Mix(design.strapOns.count.ToString(CultureInfo.InvariantCulture));
                Mix(design.strapOns.engine);
                Mix(design.strapOns.propMass.ToString("R", CultureInfo.InvariantCulture));
                Mix(design.strapOns.landingPropMass.ToString("R", CultureInfo.InvariantCulture));
            }
            return h == 0 ? 1 : h;
        }

        /// <summary>
        /// One ascent of a campaign design. Gentle physics is forced off on the monitor.
        /// A design whose tier is 1, 2, 3 or kid still has the private gentle latch, and this flight is then not a pass.
        /// </summary>
        public static CampaignFlight Fly(Catalog catalog, CampaignMission mission, Design design, ulong seed)
        {
            var flight = new CampaignFlight
            {
                missionId = mission != null ? mission.Id : null,
                designId = design != null ? design.id : null,
                seed = seed,
                budgetKc = mission != null ? mission.BudgetKc : 0,
            };
            if (catalog == null || mission == null || design == null || design.stages == null || design.stages.Count == 0)
            {
                flight.outcome = "error";
                flight.failure = "missing";
                return flight;
            }
            try
            {
                flight.gentle = FailureMonitor.GentleTier(design);
                flight.payloadMatch = design.payloadType == mission.PayloadId && design.payloadMass == mission.MassKg;
                var fit = PayloadFit.Check(design, PayloadFit.MissionSpec(mission));
                flight.fairingFits = fit.fits;
                var price = CostModel.PriceDesign(catalog, design);
                long perKc = CostModel.MinorPerKc(catalog);
                flight.costMinor = price.recurring;
                flight.costKc = perKc > 0 ? (double)price.recurring / perKc : 0;
                long budgetMinor = Career.KcToMinor(mission.BudgetKc, perKc);
                flight.underBudget = price.recurring <= budgetMinor;
                flight.reliability = Reliability.Of(catalog, design).reliability;
                flight.requiredDv = mission.RequiredDeltaV();

                // Same setup as the live pad flight (MissionFlightSetup): run length, aim and cutoff,
                // coast boil-off and strap-on landing fuel. The batch flies the reference pad in calm air.
                var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.BatchDt);
                var flown = setup.Flown;
                double target = setup.TargetAltM;
                flight.boilOffKg = setup.BoilOffKg;
                var monitor = FailureMonitor.Create(catalog, flown, target, DesignHash(design), seed);
                monitor.Gentle = false;
                setup.Options.monitor = monitor;
                // The live pad's director (#313): the batch flies the same max-Q call, throttle hold, pitch nudge and
                // staging the default player gets on the pad, not the bare sim.
                var director = MissionFlightSetup.Director(catalog, setup, design, DesignHash(design), seed);
                var sim = MissionFlightSetup.Fly(setup, director);

                flight.outcome = sim.outcome;
                flight.failure = sim.failure != null ? sim.failure.kind : null;
                flight.apoKm = MissionScore.FlownApoAltM(sim) / 1000.0;
                flight.periKm = sim.orbit.periAlt / 1000.0;
                flight.incDeg = sim.orbit.i * 180.0 / Math.PI;
                flight.maxG = sim.maxG;
                flight.idealDv = sim.idealDeltaV;
                if (mission.DepartsFromParking)
                {
                    flight.departDv = sim.departureDv;
                    flight.departTW = sim.departureTW;
                    flight.departReqDv = MissionScore.DepartureRequiredMps(mission.TargetKind, mission.ApoKm, mission.C3Km2PerS2, mission.GuidanceAltM, sim.departureTW);
                }
                flight.c3Km2PerS2 = MissionScore.C3(sim.orbit.energy) / 1e6;
                flight.cutoffGammaDeg = sim.cutoffGammaDeg;
                flight.qPeakKPa = sim.maxQ / 1000.0;
                flight.margin = flight.requiredDv > 0 ? (flight.idealDv - flight.requiredDv) / flight.requiredDv : 0;
                if (sim.risks != null)
                {
                    for (int i = 0; i < sim.risks.Count; i++)
                    {
                        if (sim.risks[i].level != "red") continue;
                        flight.redRisk = true;
                        flight.redKinds.Add(sim.risks[i].kind);
                    }
                }

                flight.burnoutMps = BurnoutSpeed(sim, CoreSerial(design));
                flight.burnoutAltKm = BurnoutAlt(sim, CoreSerial(design)) / 1000.0;
                flight.landingGrade = GradeLanding(catalog, design, sim, mission.LandingMode, false);
                if (mission.LandBoosters)
                    flight.boosterGrade = GradeLanding(catalog, design, sim, mission.LandingMode, true);

                bool intact = sim.failure == null && sim.outcome != "failure" && sim.outcome != "crash";
                flight.reached = TargetReached(mission, flight, out var criterion);
                flight.criterion = criterion;
                flight.passed = FlightDebrief.GoalMet(mission, intact, flight.apoKm * 1000.0, flight.periKm * 1000.0, flight.maxG, flight.landingGrade, FlightDebrief.FromFlight(flight));

                double gLimit = GLimit(catalog, design, mission);
                bool landingRequired = mission.LandingRequired;
                flight.stars = MissionScore.Stars(flight.passed, flight.margin, flight.costKc, flight.budgetKc, landingRequired, MissionScore.IsPerfect(flight.landingGrade), flight.maxG, gLimit);
                flight.score = MissionScore.Points(flight.passed, flight.margin, flight.costKc, flight.budgetKc, landingRequired ? flight.landingGrade : null);
            }
            catch (Exception ex)
            {
                flight.outcome = "error";
                flight.failure = ex.GetType().Name + ": " + ex.Message;
                flight.passed = false;
            }
            return flight;
        }

        /// <summary>True when every seed passes with gentle physics off. The seeds are the campaign's five.</summary>
        public static bool Viable(Catalog catalog, CampaignMission mission, Design design, IList<ulong> seeds, out CampaignFlight[] flights)
        {
            flights = new CampaignFlight[seeds != null ? seeds.Count : 0];
            if (flights.Length == 0) return false;
            bool ok = true;
            for (int i = 0; i < flights.Length; i++)
            {
                flights[i] = Fly(catalog, mission, design, seeds[i]);
                if (!flights[i].passed) ok = false;
            }
            return ok;
        }

        static bool TargetReached(CampaignMission mission, CampaignFlight flight, out string criterion)
        {
            // #313 Physics A: the honest departure check, the Δv actually left in the parking orbit against the burn.
            bool dv = !double.IsNaN(flight.departDv) && !double.IsNaN(flight.departReqDv) && flight.departDv + 1e-6 >= flight.departReqDv;
            return MissionScore.TargetMet(mission.TargetKind, mission.ApoKm, mission.PeriKm, mission.C3Km2PerS2,
                flight.apoKm * 1000, flight.periKm * 1000, flight.c3Km2PerS2, dv, out criterion);
        }

        static double GLimit(Catalog catalog, Design design, CampaignMission mission)
        {
            double g = 8;
            if (design.payloadType != null && catalog.PayloadById.TryGetValue(design.payloadType, out var pl) && pl.maxG > 0 && pl.maxG < g)
                g = pl.maxG;
            if (!double.IsNaN(mission.MaxG) && mission.MaxG > 0 && mission.MaxG < g) g = mission.MaxG;
            return g;
        }

        /// <summary>
        /// Predicted catch from the landing reserve versus boostback Δv at stage-1 burnout speed.
        /// The ascent integrator does not fly the return; RecoveryModel supplies the need.
        /// Margin uses the Haven perfect (0.45) and good (0.20) bands on 1 − need/available.
        /// </summary>
        /// <summary>Predicted catch. The ascent does not fly the return; the live card uses this when no recovery was flown.</summary>
        public static string PredictLandingGrade(Catalog catalog, Design design, SimResult sim, string mode, bool boosters)
            => GradeLanding(catalog, design, sim, mode, boosters);

        static string GradeLanding(Catalog catalog, Design design, SimResult sim, string mode, bool boosters)
        {
            if (mode != "rtls" && mode != "barge") return null;
            if (boosters)
            {
                if (design.StrapOnCount <= 0 || !(design.strapOns.landingPropMass > 0)) return "short";
            }
            else if (!design.legs || design.stages.Count == 0 || !(design.stages[0].landingPropMass > 0))
                return "short";

            double speed = BurnoutSpeed(sim, boosters ? 1 : CoreSerial(design));
            double need = RecoveryModel.BoostbackDeltaV(mode, speed);
            double avail = Reserve(catalog, design, boosters);
            if (!(avail > 0) || avail < need) return "short";
            double margin = 1 - need / avail;
            if (margin >= 0.45) return "perfect";
            if (margin >= 0.20) return "good";
            return "hard";
        }

        static int CoreSerial(Design design) => design.StrapOnCount > 0 ? 2 : 1;

        static double BurnoutSpeed(SimResult sim, int serialStage) => BurnoutEvent(sim, serialStage, true);

        static double BurnoutAlt(SimResult sim, int serialStage) => BurnoutEvent(sim, serialStage, false);

        static double BurnoutEvent(SimResult sim, int serialStage, bool speed)
        {
            if (sim?.events == null) return 0;
            for (int i = 0; i < sim.events.Count; i++)
            {
                var e = sim.events[i];
                if (e.type != "burnout" || e.stage != serialStage) continue;
                if (speed) return e.v.HasValue ? e.v.Value : 0;
                return e.alt.HasValue ? e.alt.Value : 0;
            }
            return 0;
        }

        static double Reserve(Catalog catalog, Design design, bool boosters)
        {
            var part = boosters ? design.strapOns : design.stages[0];
            if (part == null || !(part.landingPropMass > 0)) return 0;
            if (!catalog.EngineById.TryGetValue(part.engine ?? "", out var eng)) return 0;
            var point = EnginePoint.Of(catalog, part);
            double isp = point.known ? point.ispVac : eng.ispVac;
            var mass = Mass.Of(catalog, design);
            double dry;
            if (boosters) dry = mass.strapOns.eachDry;
            else dry = mass.stages != null && mass.stages.Count > 0 ? mass.stages[0].dry : 0;
            return RecoveryModel.ReserveDeltaV(isp, dry, part.landingPropMass);
        }
    }
}
