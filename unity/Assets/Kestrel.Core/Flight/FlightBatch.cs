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
        /// <summary>The first stage's flown landing (BoosterLanding.FlyPlanned): its cause (null on a clean touchdown), miss
        /// from the deck centre, sink, landing fuel left, and the hand-over speed and height. NaN when nothing was flown.</summary>
        public string landingCause;
        public double landingMissM = double.NaN, landingSink = double.NaN, landingPropLeftKg = double.NaN, separationSpeedMps = double.NaN, separationAltKm = double.NaN;
        /// <summary>#341: the design carried the closed-loop guidance computer; it flew the upper stage (guidanceInControl);
        /// a live nudge was held; the top stage burned dry before the cutoff energy (upperOut); and the state the part was
        /// handed at its first upper-stage step (altitude, flight-path angle, thrust over net gravity). All false or NaN without the part.</summary>
        public bool guidanceComputer, guidanceInControl, nudgeHeldByGuidance, upperOut;
        public double handoffAltKm = double.NaN, handoffGammaDeg = double.NaN, handoffLiftRatio = double.NaN;
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
                // Step the flight so the batch sees the first stage's hand-over exactly where the live pad does
                // (BoosterLanding.Watch: the last state while the core is the active stage).
                var run = MissionFlightSetup.Start(setup, director);
                bool releases = BoosterLanding.Releases(catalog, design, out int coreSerial);
                double omegaAir = setup.Options.rotate ? Earth.SiteRotationSpeed(setup.Options.latitude) / Gravity.R_EARTH : 0;
                bool shot = false, separated = false;
                double shotDown = 0, shotAlt = 0, shotSpeed = 0, shotPitch = 0;
                while (true)
                {
                    bool more = run.Step();
                    if (releases && !separated)
                    {
                        if (run.Sim.StageIdx == coreSerial)
                        {
                            BoosterLanding.HandOver(run.Sim.Y, run.Sim.T, omegaAir, out shotDown, out shotAlt, out shotSpeed, out shotPitch);
                            shot = shotAlt > 20;
                        }
                        else if (shot && run.Sim.StageIdx > coreSerial) separated = true;
                    }
                    if (!more) break;
                }
                var sim = run.Finish();

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
                flight.guidanceComputer = design.guidanceComputer;
                flight.guidanceInControl = sim.guidanceInControl;
                flight.nudgeHeldByGuidance = sim.nudgeHeldByGuidance;
                flight.upperOut = sim.upperRanDry;
                flight.handoffAltKm = sim.guidanceHandoffAltM / 1000.0;
                flight.handoffGammaDeg = sim.guidanceHandoffGammaDeg;
                flight.handoffLiftRatio = sim.guidanceHandoffLiftRatio;
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
                // The landing the player sees: the booster flown from its hand-over on Auto-assist (one rule with the
                // live pad, BoosterLanding.FlyPlanned). A stage that never separated, or picked no landing, is short.
                if (mission.LandingMode == "rtls" || mission.LandingMode == "barge")
                {
                    var landed = separated ? BoosterLanding.FlyPlanned(catalog, design, shotDown, shotAlt, shotSpeed, shotPitch) : null;
                    // Legs and the mission's site are required before a flown grade counts (BoosterLanding.MissionGrade).
                    flight.landingGrade = landed != null ? BoosterLanding.MissionGrade(mission, design, FlownGrade(landed)) : "short";
                    flight.landingCause = landed != null ? landed.Cause : null;
                    flight.landingMissM = landed != null ? landed.MissM : double.NaN;
                    flight.landingSink = landed != null ? landed.Sink : double.NaN;
                    flight.landingPropLeftKg = landed != null ? landed.PropLeft : double.NaN;
                    flight.separationSpeedMps = separated ? shotSpeed : double.NaN;
                    flight.separationAltKm = separated ? shotAlt / 1000.0 : double.NaN;
                }
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

        /// <summary>The flown verdict's grade in the batch's words: perfect, good, hard, salvage or water.</summary>
        public static string FlownGrade(RecoveryVerdict v)
        {
            if (v == null || string.IsNullOrEmpty(v.GradeName)) return "short";
            return v.GradeName.ToLowerInvariant();
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
            else
            {
                if (!design.legs || design.stages.Count == 0 || !(design.stages[0].landingPropMass > 0)) return "short";
                // #369: the flown grade is "short" when the stage never separates (a one-stage hop) or lands
                // somewhere the mission does not grade (BoosterLanding.MissionGrade). The predictor must agree.
                if (design.stages.Count < 2) return "short";
                if (design.recoveryMode != mode) return "short";
            }

            double need = ReturnDeltaV(catalog, design, sim, mode, boosters);
            double avail = Reserve(catalog, design, boosters);
            if (!(avail > 0) || avail < need) return "short";
            double margin = 1 - need / avail;
            if (margin >= 0.45) return "perfect";
            if (margin >= 0.20) return "good";
            return "hard";
        }

        /// <summary>
        /// #369: the Δv the flown return actually needs, closed-form from the separation state of the predictor's
        /// flight: boostback + entry burn + landing burn. The RecoverySim/RecoveryModel/RecoveryGuidance constants
        /// are the single source of truth (no second copy of the numbers). Closed-form arithmetic only, so the
        /// Starter job stays inside its 80 ms ceiling: no simulated return.
        /// </summary>
        public static double ReturnDeltaV(Catalog catalog, Design design, SimResult sim, string mode, bool boosters)
            => ReturnDeltaVParts(catalog, design, sim, mode, boosters, out _, out _, out _);

        /// <summary>#369: <see cref="ReturnDeltaV"/> with the three shares (diagnostics, tuning).</summary>
        public static double ReturnDeltaVParts(Catalog catalog, Design design, SimResult sim, string mode, bool boosters,
            out double boostback, out double entry, out double land)
        {
            boostback = entry = land = 0;
            int serial = boosters ? 1 : CoreSerial(design);
            double alt = double.NaN, v = double.NaN, gammaDeg = double.NaN;
            if (sim?.events != null)
                foreach (var e in sim.events)
                {
                    if (e.type != "burnout" || e.stage != serial) continue;
                    if (e.alt.HasValue) alt = e.alt.Value;
                    if (e.v.HasValue) v = e.v.Value;
                    if (e.gamma.HasValue) gammaDeg = e.gamma.Value;
                    break;
                }
            if (!(v > 0) || double.IsNaN(alt) || alt < 0) return double.PositiveInfinity;

            // Ground frame, as RecoverySim flies it: the site rotation comes off the eastward speed.
            // The sim's default site (Headless.Options.latitude) is 28.5 deg.
            double gamma = double.IsNaN(gammaDeg) ? 0 : gammaDeg * Math.PI / 180.0;
            double vSite = Earth.SiteRotationSpeed(28.5);
            double vEast = v * Math.Cos(gamma) - vSite;
            double vUp = v * Math.Sin(gamma);

            // 1. Boostback: RecoveryModel.BoostbackDeltaV holds the RtlsFactor/BargeFactor.
            double bb = RecoveryModel.BoostbackDeltaV(mode, vEast);
            boostback = bb;

            // 2. Entry burn: retrograde from the entry-interface speed down to the guidance's floor target
            // (RecoveryGuidance.EntryTargetSpeed at EntryFloorM). The interface speed is the post-boostback
            // state flown down to the window top (EntryTopM) on vis-viva; below the window it is now.
            double vEastAfter = vEast - Math.Sign(vEast) * bb;
            double vInertEastAfter = vEastAfter + vSite;
            double v0sq = vInertEastAfter * vInertEastAfter + vUp * vUp;
            double vEntry;
            if (alt >= RecoveryGuidance.EntryTopM || vUp > 0)
            {
                double r0 = Gravity.R_EARTH + alt, r1 = Gravity.R_EARTH + RecoveryGuidance.EntryTopM;
                double v1sq = v0sq + 2 * Gravity.MU_EARTH * (1 / r1 - 1 / r0);
                vEntry = v1sq > 0 ? Math.Sqrt(v1sq) : Math.Sqrt(v0sq);
            }
            else vEntry = Math.Sqrt(v0sq);
            double entryNeed = Math.Max(0, vEntry - RecoveryGuidance.EntryTargetSpeed(RecoveryGuidance.EntryFloorM));
            entry = entryNeed;

            // 3. Landing burn: RecoverySim.LandingDeltaVNeed holds the vTerm/twr/tBurn.
            // No craft means no landing burn is flyable: the return cannot be bought.
            double landNeed;
            if (boosters)
            {
                if (!TryStrapOnCraft(catalog, design, mode, out var strapOn)) return double.PositiveInfinity;
                landNeed = RecoverySim.LandingDeltaVNeed(strapOn);
            }
            else
            {
                if (!RecoveryCrafts.TryFrom(catalog, design, out var craft)) return double.PositiveInfinity;
                landNeed = RecoverySim.LandingDeltaVNeed(craft);
            }
            land = landNeed;

            return bb + entryNeed + landNeed;
        }

        /// <summary>#369: a RecoveryCraft for the strap-on set (the booster grade's landing burn), from the
        /// strap-on part and its catalog engine. The core path uses RecoveryCrafts.TryFrom.</summary>
        static bool TryStrapOnCraft(Catalog catalog, Design design, string mode, out RecoveryCraft craft)
        {
            craft = default;
            var so = design?.strapOns;
            if (so == null || design.StrapOnCount <= 0) return false;
            if (!catalog.EngineById.TryGetValue(so.engine ?? "", out var eng)) return false;
            var point = EnginePoint.Of(catalog, so);
            double isp = point.known ? point.ispVac : eng.ispVac;
            if (!(isp > 1)) return false;
            var mass = Mass.Of(catalog, design);
            double dry = mass.strapOns.eachDry;
            if (!(dry > 1)) return false;
            int n = so.engineCount > 0 ? so.engineCount : 1;
            double thrustVac = (point.known ? point.thrustVac : eng.thrustVac) * n;
            double thrustSl = (point.known ? point.thrustSL : eng.thrustSL) * n;
            craft = new RecoveryCraft
            {
                DryKg = dry,
                ReserveKg = Math.Max(0, so.landingPropMass),
                ThrustVacN = thrustVac,
                ThrustSlN = thrustSl,
                IspSec = isp,
                DiameterM = so.diameter > 0.2 ? so.diameter : 1,
                Cd = 0.45,
                GridFins = design.gridFins,
                Legs = false,
                LegsKind = "light",
                Mode = mode,
            };
            return craft.ThrustVacN > 1 || craft.ThrustSlN > 1;
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

        /// <summary>#369: the landing reserve's Δv (diagnostic accessor).</summary>
        public static double LandingAvailDeltaV(Catalog catalog, Design design, bool boosters) => Reserve(catalog, design, boosters);
    }
}
