// Arms the flying design, then writes the post-flight card into the same career save the mission map reads.
// A captured failure with no finished flight still explains itself and does not touch the bank.
using System;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Save;
using Kestrel.Core.Vehicle;
using Kestrel.Game.Shell;
using UnityEngine;

namespace Kestrel.Slice
{
    public static class PostFlightSource
    {
        public static Action Relaunch { get; private set; }
        public static Action LeaveHome { get; private set; }
        /// <summary>NAV-04 / F4: the flight screen's own "reopen the designer" callback.</summary>
        public static Action OpenDesigner { get; private set; }

        static Catalog catalog;
        static Design design;
        static bool engineer;
        static bool starter;

        public static void Bind(Action relaunch, Action home, Action designer = null)
        {
            Relaunch = relaunch;
            LeaveHome = home;
            OpenDesigner = designer;
        }

        /// <param name="fromStarter">Launched from the Starter screen: the miss advice names the Starter's controls (Reset),
        /// not the pitch kick or one stage's fuel it does not have (MissInput.Starter).</param>
        public static void Arm(Catalog cat, Design flying, JsonObject doc, bool engineerDepth, bool fromStarter = false)
        {
            catalog = cat;
            design = flying;
            engineer = engineerDepth;
            starter = fromStarter;
            if (design == null && doc != null)
            {
                try { design = Design.FromJson(doc); }
                catch (Exception) { design = null; }
            }
        }

        public static FlightDebrief Publish(FlightDriver flight, FailureEvent failure)
        {
            var facts = Facts(flight, failure);
            var pool = LoadPool();
            long minor = MinorPerKc();
            var look = Look();
            bool live = flight != null && flight.Result != null;
            // DECISIONS #102: a test flight never opens the career save or the guide's memory. Compose writes nothing.
            bool test = GameSession.IsTestFlight;
            var card = !live || test
                ? FlightDebrief.Compose(null, GameSession.Current, facts, pool, minor, look)
                : FlightDebrief.Commit(TryStore(minor), GameSession.Current, facts, pool, minor, look);
            if (!test) GuideBinder.Debrief(flight, card);
            return card;
        }

        public static ReportActions Actions() => NextMission.Actions(Relaunch, LeaveHome, OpenDesigner);

        static FlightFacts Facts(FlightDriver flight, FailureEvent failure)
        {
            var result = flight != null ? flight.Result : null;
            var ascentFail = failure ?? (result != null ? result.failure : null);
            var rec = RecoveryLog.Current;
            // An expendable first stage's loss (BoosterLanding) is not this flight's failure or its landing grade.
            var ev = BoosterLanding.ReportFailure(ascentFail, rec);
            string landingGrade = BoosterLanding.ReportGrade(ascentFail, rec);
            var mission = GameSession.Current;
            var facts = new FlightFacts
            {
                Failure = ev,
                LandingGrade = landingGrade,
                // #79: the flown burn's player input reaches the score's landing-bonus multiplier.
                PlayerFlownBurn = rec != null && rec.PlayerFlown,
                Seed = mission != null ? mission.Seed : 0,
                DesignId = design != null ? design.name : null,
                RedundantAvionics = design != null && EventModel.Tier(design.avionicsTier) == "redundant",
                Finless = design != null && !design.fins,
                EngineCount = Engines(design),
                BudgetKc = mission != null ? mission.BudgetKc : 0,
                GoalKm = mission != null && mission.Target != null ? mission.Target.ApoKm : 0,
                // Same filter as BoosterLanding.ReportGrade: only a graded, non-expendable attempt counts.
                LandingOffsetM = rec != null && rec.Attempted && !rec.Expendable
                    ? Math.Abs(rec.MissM)
                    : double.NaN,
            };
            if (catalog != null && design != null)
            {
                try { facts.MassKg = Mass.Of(catalog, design).liftoffMass; }
                catch (Exception) { facts.MassKg = double.NaN; }
            }
            if (result != null)
            {
                facts.PeakG = result.maxG;
                double apo = MissionScore.FlownApoAltM(result);
                facts.ApoKm = apo / 1000.0;
                double peri = result.orbit.periAlt;
                CampaignMission row = Row(mission) ?? ApogeeFallback(mission);
                if (row != null)
                {
                    facts.GoalKm = row.ApoKm;
                    facts.LandingRequired = row.LandingRequired || row.LandBoosters;
                    facts.GLimit = double.IsNaN(row.MaxG) ? 0 : row.MaxG;
                    facts.RequiredMps = row.RequiredDeltaV();
                    facts.Margin = facts.RequiredMps > 1 ? (result.idealDeltaV - facts.RequiredMps) / facts.RequiredMps : 0;
                    bool intact = ascentFail == null && result.outcome != "failure" && result.outcome != "crash";
                    var extra = Check(row, result, facts);
                    facts.FillDeparture(row, apo, peri, extra);
                    // The flown grade counts only with legs and at the mission's site (the batch's rule).
                    facts.LandingGrade = BoosterLanding.MissionGrade(row, design, facts.LandingGrade);
                    if (string.IsNullOrEmpty(facts.LandingGrade) && (row.LandingRequired || row.LandBoosters))
                        facts.LandingGrade = FlightBatch.PredictLandingGrade(catalog, design, result, row.LandingMode, false);
                    if (row.LandBoosters)
                    {
                        extra.BoosterKnown = true;
                        extra.BoosterGrade = FlightBatch.PredictLandingGrade(catalog, design, result, row.LandingMode, true);
                        facts.BoostersHome = MissionScore.IsGood(extra.BoosterGrade);
                    }
                    facts.Passed = FlightDebrief.GoalMet(row, intact, apo, peri, result.maxG, facts.LandingGrade, extra);
                    // F4: an intact miss says why and what to change, from the same gates. The advice names the
                    // kick the design actually flew (the sim's default when the design never set one), so the
                    // flat/steep lever is never the old constant 6°.
                    facts.Miss = MissReason.Explain(row, new MissInput
                    {
                        Intact = intact, ApoM = apo, PeriM = peri, MaxG = result.maxG,
                        LandingGrade = facts.LandingGrade, Extra = extra,
                        Stages = design != null && design.stages != null ? design.stages.Count : 1,
                        CutoffGammaDeg = result.cutoffGammaDeg,
                        PitchKickDeg = design != null && design.guidance != null && design.guidance.pitchKickDegSet
                            ? design.guidance.pitchKickDeg : GuidanceProfile.DefaultKickDeg,
                        Starter = starter,
                    });
                }
            }
            if (flight != null && flight.Decisions != null)
            {
                var score = flight.Decisions.Score();
                if (score != null) facts.TimingMedal = FlightVerdict.Medal(score.Medal, facts.Passed); // DECISIONS #83
            }
            Quote(facts);
            if (GameSession.IsTestFlight)
            {
                // Test Flight's card reads the sim's own numbers for the rocket that flew (no mission gates).
                var top = flight != null ? flight.Top : null;
                facts.Test = result != null
                    ? Kestrel.Core.Flight.TestFlight.Facts(result, ascentFail, flight.Sim, Kestrel.Core.Flight.TestFlight.LiftoffTwr(catalog, design),
                        top != null ? top.AltM : double.NaN, top != null ? top.SpeedMps : double.NaN)
                    : new TestFlightFacts { Failure = ascentFail };
            }
            return facts;
        }

        static GoalCheck Check(CampaignMission row, SimResult result, FlightFacts facts)
        {
            var extra = new GoalCheck
            {
                IdealDv = result.idealDeltaV,
                DepartureDv = result.departureDv,
                DepartureTW = result.departureTW,
                C3Km2PerS2 = MissionScore.C3(result.orbit.energy) / 1e6,
                Margin = facts.Margin,
            };
            if (result.risks != null)
            {
                extra.RedKnown = true;
                for (int i = 0; i < result.risks.Count; i++)
                {
                    if (result.risks[i].level == "red") extra.RedRisk = true;
                }
            }
            if (catalog == null || design == null) return extra;
            try
            {
                extra.Reliability = Reliability.Of(catalog, design).reliability;
                extra.Gentle = FailureMonitor.GentleTier(design);
                extra.PayloadMatch = design.payloadType == row.PayloadId && design.payloadMass == row.MassKg;
                var fit = PayloadFit.Check(design, new PayloadSpec { id = row.PayloadId, volume = row.VolumeM3, diameter = row.DiameterM });
                extra.FairingFits = fit.fits;
                var price = CostModel.PriceDesign(catalog, design);
                long per = CostModel.MinorPerKc(catalog);
                long budget = Career.KcToMinor(row.BudgetKc, per > 0 ? per : 1);
                extra.UnderBudget = price.recurring <= budget;
                extra.AirframeKnown = true;
                if (row.CoastHours > 0 && design.stages != null && design.stages.Count > 0)
                {
                    var upper = design.stages[design.stages.Count - 1];
                    double before = upper.propMass;
                    extra.BoilOffKg = before - TankModel.RemainingPropKg(catalog, upper, row.CoastHours);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Kestrel] post-flight gates: " + e.Message);
            }
            return extra;
        }

        static void Quote(FlightFacts facts)
        {
            if (catalog == null || design == null) return;
            try
            {
                var price = CostModel.PriceDesign(catalog, design);
                long per = CostModel.MinorPerKc(catalog);
                if (per < 1) per = 100000;
                facts.CostMinor = price.recurring;
                facts.CostKc = (double)price.recurring / per;
                bool home = MissionScore.IsGood(facts.LandingGrade);
                // DECISIONS #87: landed boosters come home too, so reuse reads cheaper on the card.
                facts.RecoveredHardwareMinor = home ? CostModel.RecoveredHardwareMinor(price, true, facts.BoostersHome) : 0;
                facts.EconomyKnown = true;
                if (string.IsNullOrEmpty(facts.ArticleKey)) facts.ArticleKey = "core";
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Kestrel] build cost unavailable: " + e.Message);
            }
        }

        static int Engines(Design d)
        {
            if (d == null || d.stages == null) return 0;
            int n = 0;
            for (int i = 0; i < d.stages.Count; i++)
            {
                var stage = d.stages[i];
                if (stage == null) continue;
                int copies = stage.count > 1 ? stage.count : 1;
                n += (stage.engineCount > 0 ? stage.engineCount : 1) * copies;
            }
            return n;
        }

        static CampaignMission Row(MissionContext mission)
        {
            if (mission == null || mission.IsFree || string.IsNullOrEmpty(mission.MissionId)) return null;
            var campaign = ScreenStub.LoadCampaign();
            return campaign != null ? campaign.ById(mission.MissionId) : null;
        }

        /// <summary>
        /// Campaign file missing: still grade an apogee hop from the session target.
        /// Max-g stays off (NaN) because KH-1's 6 g line is a bonus, not a gate.
        /// </summary>
        static CampaignMission ApogeeFallback(MissionContext mission)
        {
            if (mission == null || mission.IsFree || mission.Target == null) return null;
            if (mission.Target.Kind != "apogee" || mission.Target.ApoKm <= 0) return null;
            return new CampaignMission
            {
                Id = mission.MissionId,
                TargetKind = "apogee",
                ApoKm = mission.Target.ApoKm,
            };
        }

        static long MinorPerKc()
        {
            if (catalog != null)
            {
                try
                {
                    long per = CostModel.MinorPerKc(catalog);
                    if (per >= 1) return per;
                }
                catch (Exception) { }
            }
            return 100000;
        }

        static NarrationPool LoadPool()
        {
            try
            {
                string root = ScreenStub.RepoRoot();
                if (!string.IsNullOrEmpty(root) && File.Exists(Path.Combine(root, "data", "narration.json")))
                    return NarrationPool.Load(root);
            }
            catch (Exception) { }
            var asset = Resources.Load<TextAsset>("KestrelData/narration");
            if (asset != null && !string.IsNullOrWhiteSpace(asset.text))
            {
                try { return NarrationPool.Parse(asset.text); }
                catch (Exception) { }
            }
            return NarrationPool.Empty;
        }

        static HangarData Look()
        {
            try
            {
                string root = ScreenStub.RepoRoot();
                if (!string.IsNullOrEmpty(root)) return HangarData.LoadFromRepo(root);
            }
            catch (Exception) { }
            return null;
        }

        static CareerStore TryStore(long minor)
        {
            try
            {
                var campaign = ScreenStub.LoadCampaign();
                if (campaign == null) return null;
                string dir = Path.Combine(Application.persistentDataPath, "saves");
                Directory.CreateDirectory(dir);
                var native = new NativeStore(dir, () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), () => 0.5);
                return CareerStore.Load(native, campaign, minor);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Kestrel] career save unavailable: " + e.Message);
                return null;
            }
        }
    }
}
