// #341 (Kestrel Physics and Arcade rulings, DESIGN_341.md): the optional closed-loop guidance computer. It is opt-in
// (a design field, a feature flag off in the Starter designer, a later campaign unlock), costs mass and credits, and
// leaves every design without it byte-identical. With it fitted the upper stages steer to (aim altitude, vr = 0) at the
// energy cutoff, the live nudge is held on the upper stage (S1 only), and a dry top stage is explained by its cause.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Economy;
using Kestrel.Core.Features;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Save;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class GuidanceComputerTests
    {
        Catalog catalog;
        HangarData hangar;
        Campaign campaign;

        /// <summary>Physics ruling 2: |γ| at a clean cutoff with the part, every board starter, every seed and the live pad.</summary>
        const double GammaMaxDeg = 0.05;

        [OneTimeSetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
        }

        static Design Fitted(Design d) { var c = d.Clone(); GuidanceComputer.Fit(c, true); return c; }

        // ---- data and opt-in ----

        [Test]
        public void Data_PinsLabelMassPriceFlagAndUnlock()
        {
            var l = GuidanceComputer.Load(Golden.Root);
            Assert.AreEqual("Basic guidance: chases the right height, can't plan the engine cutoff", l.Basic);
            Assert.AreEqual("Upper-stage guidance computer", l.Part);
            Assert.AreEqual("Guidance computer is flying the upper stage", l.InControl);
            StringAssert.StartsWith(l.InControl, l.NudgeHeld);
            Assert.AreEqual(GuidanceComputer.PartId, l.Id);
            Assert.AreEqual(Mass.GUIDANCE_COMPUTER_MASS, l.MassKg);
            Assert.That(l.MassKg, Is.InRange(10, 99), "a realistic small mass: tens of kg");
            var credits = CreditsTable.Parse(File.ReadAllText(Path.Combine(Golden.Root, "data", "credits.json")));
            Assert.AreEqual(30, l.Credits);
            Assert.AreEqual(l.Credits, credits.GuidanceComputer);
            long redundant = (long)MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "credits.json"))).Obj("avionics").Num("redundant");
            Assert.That(credits.GuidanceComputer, Is.GreaterThan(credits.Legs).And.LessThan(redundant), "between legs and redundant avionics");
            Assert.AreEqual(FeatureKeys.DesignerGuidanceComputer, l.FeatureFlag);
            Assert.AreEqual(GuidanceComputer.UnlockAfterMission, l.UnlockAfterMission);
        }

        [Test]
        public void OptIn_FlagOffInStarter_LaterUnlock_NeverOnTheStarterScreen()
        {
            var flags = FeatureFlags.Load(Golden.Root);
            Assert.IsFalse(flags.IsOn(FeatureKeys.DesignerGuidanceComputer), "off in the Starter designer");
            Assert.IsFalse(StarterScreen.Shows(flags, FeatureKeys.DesignerGuidanceComputer));
            var career = Career.Start(campaign, CostModel.MinorPerKc(catalog));
            Assert.AreEqual(0, career.Cleared);
            Assert.IsFalse(GuidanceComputer.Available(flags, career), "new career: flag off and locked");
            career.Cleared = 20;
            Assert.IsFalse(GuidanceComputer.Available(flags, career), "the flag gates it even with the campaign done");
            var on = new FeatureFlags();
            Assert.IsTrue(on.IsOn(FeatureKeys.DesignerGuidanceComputer));
            career.Cleared = GuidanceComputer.UnlockAfterMission - 1;
            Assert.IsFalse(GuidanceComputer.Available(on, career), "locked before the unlock");
            career.Cleared = GuidanceComputer.UnlockAfterMission;
            Assert.IsTrue(GuidanceComputer.Available(on, career));
            Assert.That(GuidanceComputer.UnlockAfterMission, Is.GreaterThanOrEqualTo(8), "a later campaign unlock");
            Assert.IsFalse(new Design().guidanceComputer, "a new design has basic guidance");
        }

        [Test]
        public void Design_WithoutThePart_IsByteIdentical_AndThePartRoundTrips()
        {
            int seen = 0;
            foreach (var (mission, design, _) in BoardStarters())
            {
                string plain = JsonWriter.Stringify(design.ToJson());
                StringAssert.DoesNotContain("guidanceComputer", plain, mission.Id);
                ulong hash = FlightBatch.DesignHash(design);
                var fit = Fitted(design);
                string withPart = JsonWriter.Stringify(fit.ToJson());
                StringAssert.Contains("\"guidanceComputer\":true", withPart.Replace(" ", ""), mission.Id);
                Assert.IsTrue(Design.FromJson(fit.ToJson()).guidanceComputer, mission.Id + " parses back");
                GuidanceComputer.Fit(fit, false);
                Assert.AreEqual(plain, JsonWriter.Stringify(fit.ToJson()), mission.Id + " removing the part restores the bytes");
                Assert.AreEqual(hash, FlightBatch.DesignHash(design), mission.Id);
                seen++;
            }
            Assert.AreEqual(18, seen);
        }

        [Test]
        public void Part_Adds25KgToTheTopStage_AndItsPriceToTheQuote()
        {
            var credits = CreditsTable.Parse(File.ReadAllText(Path.Combine(Golden.Root, "data", "credits.json")));
            foreach (var (mission, design, _) in BoardStarters())
            {
                var plain = Mass.StageMasses(catalog, design);
                var fit = Mass.StageMasses(catalog, Fitted(design));
                int top = design.stages.Count - 1;
                for (int i = 0; i <= top; i++)
                {
                    double extra = i == top ? GuidanceComputer.MassKg * (i == 0 && design.legs ? 1.06 : 1) : 0;
                    Assert.AreEqual(plain[i].dry + extra, fit[i].dry, 1e-9, mission.Id + " stage " + i);
                }
                Assert.AreEqual(credits.Quote(design.ToJson()) + 30, credits.Quote(Fitted(design).ToJson()), mission.Id);
            }
        }

        [Test]
        public void SimOptions_CarryThePart_AndTheAim()
        {
            foreach (var (mission, design, _) in BoardStarters())
            {
                var basic = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.BatchDt).Options;
                Assert.IsFalse(basic.closedLoopInsertion, mission.Id);
                Assert.IsTrue(double.IsNaN(basic.closedLoopAimAlt), mission.Id);
                var fit = MissionFlightSetup.Build(catalog, mission, Fitted(design), MissionFlightSetup.BatchDt).Options;
                Assert.IsTrue(fit.closedLoopInsertion, mission.Id);
                Assert.AreEqual(mission.GuidanceAltM, fit.closedLoopAimAlt, 1e-6, mission.Id + " aims at the target (parking) radius");
            }
        }

        [Test]
        public void FreeBuildEllipse_AimsAtThePerigeeRadius()
        {
            var d = BoardStarters()[0].design.Clone();
            d.guidanceComputer = true;
            d.payloadOrbit = "gto";
            var setup = MissionFlightSetup.Build(catalog, null, d, MissionFlightSetup.BatchDt);
            Assert.IsTrue(setup.Options.energyCutoff && setup.Options.closedLoopInsertion);
            Assert.AreEqual(SimOptions.EnergyFor(35786e3, 200e3), setup.Options.cutoffEnergy, 1e-6);
            Assert.AreEqual(200e3, setup.Options.closedLoopAimAlt, 1e-6, "the GTO perigee radius");
            d.guidanceComputer = false;
            Assert.IsTrue(double.IsNaN(MissionFlightSetup.Build(catalog, null, d, MissionFlightSetup.BatchDt).Options.closedLoopAimAlt));
        }

        // ---- the flown law ----

        [Test]
        public void AllEighteenStarters_PassWithAndWithoutThePart_GammaUnder005WithIt()
        {
            var misses = new List<string>();
            double gammaMax = 0;
            foreach (var (mission, design, mode) in BoardStarters())
            {
                foreach (var (d, tag) in new[] { (design, "basic"), (Fitted(design), "part") })
                {
                    foreach (var seed in campaign.Seeds)
                    {
                        var f = FlightBatch.Fly(catalog, mission, d, seed);
                        if (!f.passed) misses.Add(mission.Id + " " + tag + " seed " + seed);
                        if (tag == "part")
                        {
                            Assert.IsTrue(f.guidanceComputer && f.guidanceInControl, mission.Id);
                            gammaMax = Math.Max(gammaMax, Math.Abs(f.cutoffGammaDeg));
                            if (!(Math.Abs(f.cutoffGammaDeg) < GammaMaxDeg)) misses.Add(mission.Id + " seed " + seed + " γ " + f.cutoffGammaDeg);
                        }
                        else Assert.IsFalse(f.guidanceComputer || f.guidanceInControl || f.upperOut, mission.Id);
                    }
                    foreach (bool wind in new[] { false, true })
                    {
                        var live = LivePadLoop.Fly(catalog, mission, d, wind);
                        if (!LivePadLoop.Passes(catalog, mission, d, live)) misses.Add(mission.Id + " " + tag + " live " + (wind ? "wind " : "calm ") + LivePadLoop.Text(live));
                        if (tag == "part")
                        {
                            gammaMax = Math.Max(gammaMax, Math.Abs(live.cutoffGammaDeg));
                            if (!(Math.Abs(live.cutoffGammaDeg) < GammaMaxDeg)) misses.Add(mission.Id + " live γ " + live.cutoffGammaDeg);
                            Assert.IsTrue(live.guidanceInControl, mission.Id);
                        }
                    }
                }
            }
            Assert.That(misses, Is.Empty, string.Join(" | ", misses));
            Console.WriteLine("#341 |γ| max with the part (18 starters, 5 seeds + live calm and wind): " + gammaMax.ToString("0.00000") + "°");
        }

        [Test]
        public void AllGoldens_PassWithThePart()
        {
            var misses = new List<string>();
            int seen = 0;
            void Check(CampaignMission m, Design d, string id, bool tracked)
            {
                int basic = 0, part = 0;
                var fit = Fitted(d);
                foreach (var seed in campaign.Seeds)
                {
                    if (FlightBatch.Fly(catalog, m, d, seed).passed) basic++;
                    if (FlightBatch.Fly(catalog, m, fit, seed).passed) part++;
                }
                if (part < basic || (!tracked && part < campaign.Seeds.Count)) misses.Add(id + " basic " + basic + " part " + part);
                seen++;
            }
            foreach (var gf in new[] { "campaign-winners", "campaign-viable" })
            {
                var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", gf + ".json")));
                foreach (JsonObject row in file.Arr("missions"))
                {
                    var m = campaign.ById(row.Str("id"));
                    if (m == null) continue;
                    int i = 0;
                    foreach (JsonObject dj in row.Arr("designs")) Check(m, Design.FromJson(dj), gf + " " + m.Id + " d" + i++, false);
                }
            }
            var pool = ContractGen.Load(Golden.Root);
            foreach (var cell in pool.Cells) { var m = ContractGen.ToMission(cell); foreach (var d in cell.Designs) Check(m, d, "contract " + cell.Id + " " + d.id, false); }
            foreach (var t in pool.Tracked) { var m = ContractGen.ToMission(t.Cell); foreach (var d in t.Cell.Designs) Check(m, d, "tracked " + t.Id + " " + d.id, true); }
            Assert.AreEqual(113, seen, "every golden design");
            Assert.That(misses, Is.Empty, string.Join(" | ", misses));
        }

        [Test]
        public void M7_SteeringLoss_WithinTenPercentOfThePitchLimitRelaxedReference()
        {
            var (mission, design, _) = BoardStarters().Find(x => x.mission.Id == "m7");
            double Loss(bool part, bool relax)
            {
                var d = part ? Fitted(design) : design;
                var setup = MissionFlightSetup.Build(catalog, mission, d, MissionFlightSetup.BatchDt);
                setup.Options.closedLoopRelaxPitch = relax;
                var sim = new AscentSim(setup.Stages, setup.Options);
                while (sim.Tick()) { }
                var r = sim.Finish();
                Assert.AreEqual("orbit", r.outcome);
                return r.lossSteering;
            }
            double basic = Loss(false, false), loop = Loss(true, false), reference = Loss(true, true);
            Console.WriteLine($"#341 m7 steering loss: basic {basic:0} m/s, guidance computer {loop:0}, pitch-limit-relaxed reference {reference:0} (limit costs {loop - reference:0})");
            Assert.That(loop, Is.LessThanOrEqualTo(1.10 * reference), "within 10% of the relaxed reference");
            Assert.That(loop, Is.LessThan(basic - 500), "the part removes the upper-stage steering loss");
        }

        [Test]
        public void LiveNudge_Stage1Only_HeldAndReportedOnTheUpperStage()
        {
            var (mission, design, _) = BoardStarters().Find(x => x.mission.Id == "m16");
            SimResult Run(Design d, Func<AscentSim, double> nudge, List<double> upperAlt)
            {
                var setup = MissionFlightSetup.Build(catalog, mission, d, MissionFlightSetup.BatchDt);
                var sim = new AscentSim(setup.Stages, setup.Options);
                while (!sim.Done)
                {
                    sim.LivePitchOffsetRad = nudge(sim);
                    if (sim.StageIdx > 0 && upperAlt != null) upperAlt.Add(sim.Y[0] * 1e-3 + sim.Y[1]);
                    Assert.AreEqual(sim.StageIdx > 0 && sim.Thrusting && d.guidanceComputer, sim.GuidanceInControl);
                    sim.Tick();
                }
                return sim.Finish();
            }
            double nudge = 3 * Math.PI / 180;
            var fit = Fitted(design);
            var clean = Run(fit, s => 0, null);
            var upperNudged = Run(fit, s => s.StageIdx > 0 ? nudge : 0, null);
            Assert.IsTrue(upperNudged.nudgeHeldByGuidance, "the upper-stage nudge is held and said so");
            Assert.IsFalse(clean.nudgeHeldByGuidance);
            Assert.AreEqual(clean.orbit.apoAlt, upperNudged.orbit.apoAlt, 0, "held: the upper stage flies exactly as without it");
            Assert.AreEqual(clean.orbit.periAlt, upperNudged.orbit.periAlt, 0);
            var s1Nudged = Run(fit, s => s.StageIdx == 0 ? nudge : 0, null);
            Assert.IsFalse(s1Nudged.nudgeHeldByGuidance);
            Assert.AreNotEqual(clean.lossSteering, s1Nudged.lossSteering, "stage 1 still takes the nudge");
            // Without the part the upper-stage nudge still steers (basic guidance unchanged).
            var basicClean = Run(design, s => 0, null);
            var basicNudged = Run(design, s => s.StageIdx > 0 ? nudge : 0, null);
            Assert.IsFalse(basicNudged.nudgeHeldByGuidance);
            Assert.AreNotEqual(basicClean.orbit.apoAlt, basicNudged.orbit.apoAlt);
            var labels = GuidanceComputer.Load(Golden.Root);
            Assert.AreEqual(labels.NudgeHeld, GuidanceComputer.NoteFor(labels, upperNudged));
            Assert.AreEqual(labels.InControl, GuidanceComputer.NoteFor(labels, clean));
            Assert.AreEqual(labels.Basic, GuidanceComputer.NoteFor(labels, basicClean));
        }

        [Test]
        public void EngineOutSeeds_SurviveWithThePart()
        {
            int eo = 0;
            foreach (var (mission, design, _) in BoardStarters())
            {
                var fit = Fitted(design);
                foreach (var seed in campaign.Seeds)
                {
                    var setup = MissionFlightSetup.Build(catalog, mission, fit, MissionFlightSetup.BatchDt);
                    var mon = FailureMonitor.Create(catalog, setup.Flown, setup.TargetAltM, FlightBatch.DesignHash(fit), seed);
                    bool anomaly = false;
                    for (int i = 0; i < setup.Stages.Count && i < EventModel.MaxStages; i++) anomaly |= mon.Events.EngineAnomaly(i);
                    if (!anomaly) continue;
                    eo++;
                    var f = FlightBatch.Fly(catalog, mission, fit, seed);
                    Assert.IsTrue(f.passed, mission.Id + " seed " + seed + " engine-out " + f.apoKm + " x " + f.periKm + " " + f.failure);
                    Assert.That(Math.Abs(f.cutoffGammaDeg), Is.LessThan(GammaMaxDeg), mission.Id + " seed " + seed);
                }
            }
            Assert.That(eo, Is.GreaterThanOrEqualTo(4), "KH-3, KH-7, KH-9 and KH-20 carry a seeded engine-out");
        }

        /// <summary>Physics ruling 5: windows gate on the engine-out-free seeds. KH-3 and KH-16 are the narrow ones.</summary>
        [TestCase("m3", 7.0, 11.5)]
        [TestCase("m16", 4.5, 9.5)]
        [TestCase("m12", 0.5, 10.5)]
        public void KickWindow_EngineOutFreeSeeds(string id, double lo, double hi)
        {
            var (mission, design, _) = BoardStarters().Find(x => x.mission.Id == id);
            for (double k = lo; k <= hi + 1e-9; k += 0.5)
            {
                var d = Fitted(design);
                d.guidance.pitchKickDeg = k; d.guidance.pitchKickDegSet = true;
                foreach (var seed in campaign.Seeds)
                {
                    var setup = MissionFlightSetup.Build(catalog, mission, d, MissionFlightSetup.BatchDt);
                    var mon = FailureMonitor.Create(catalog, setup.Flown, setup.TargetAltM, FlightBatch.DesignHash(d), seed);
                    bool anomaly = false;
                    for (int i = 0; i < setup.Stages.Count && i < EventModel.MaxStages; i++) anomaly |= mon.Events.EngineAnomaly(i);
                    if (anomaly) continue;
                    var f = FlightBatch.Fly(catalog, mission, d, seed);
                    Assert.IsTrue(f.passed, id + " kick " + k + " seed " + seed + ": " + f.apoKm.ToString("0.0") + " x " + f.periKm.ToString("0.0"));
                }
            }
        }

        // ---- MissReason ----

        [Test]
        public void Kh16At10_5_IsUpperOutFromAFlatHandoff_NotAddFuel()
        {
            var (mission, design, _) = BoardStarters().Find(x => x.mission.Id == "m16");
            foreach (double kick in new[] { 10.0, 10.5, 11.0 })
            {
                var d = Fitted(design);
                d.guidance.pitchKickDeg = kick; d.guidance.pitchKickDegSet = true;
                var f = FlightBatch.Fly(catalog, mission, d, campaign.Seeds[0]);
                Assert.IsFalse(f.passed, "kick " + kick);
                Assert.IsTrue(f.upperOut, "kick " + kick + " top stage ran dry");
                Assert.That(f.handoffLiftRatio, Is.LessThan(1), "kick " + kick);
                Assert.That(f.handoffGammaDeg, Is.LessThan(MissReason.HandoffSteepDeg), "kick " + kick);
                var x = MissReason.Explain(mission, MissReason.FromFlight(f, d.stages.Count));
                Assert.IsNotNull(x, "kick " + kick);
                Assert.AreEqual("upper-out-flat-handoff", x.Code, "kick " + kick);
                StringAssert.Contains("ran out of propellant", x.Line);
                StringAssert.Contains("smaller pitch kick", x.Line);
                StringAssert.Contains("is not the fix", x.Line);
                StringAssert.DoesNotContain("Add fuel", x.Line);
                if (kick == 10.5)
                {
                    Assert.AreEqual(71, f.handoffAltKm, 2, "stage 1 hands over at about 71 km");
                    Assert.AreEqual(5.9, f.handoffGammaDeg, 0.3);
                    Assert.AreEqual(0.73, f.handoffLiftRatio, 0.02);
                    Console.WriteLine("#341 KH-16 10.5°: " + x.Line);
                }
            }
            // Inside the window the hand-off is steep enough and the part lands it (KickWindow_EngineOutFreeSeeds).
            var ok = Fitted(design); ok.guidance.pitchKickDeg = 9.5; ok.guidance.pitchKickDegSet = true;
            var good = FlightBatch.Fly(catalog, mission, ok, campaign.Seeds[0]);
            Assert.IsTrue(good.passed);
            Assert.IsFalse(good.upperOut);
            Assert.That(good.handoffGammaDeg, Is.GreaterThanOrEqualTo(MissReason.HandoffSteepDeg));
        }

        [Test]
        public void KickAdviceRetiredOnCleanCutoffs_OnlyWithThePart()
        {
            var (mission, _, _) = BoardStarters().Find(x => x.mission.Id == "m12");
            MissInput Tilted(bool part) => new MissInput { ApoM = 753e3, PeriM = 447e3, MaxG = 3, Stages = 2, CutoffGammaDeg = 1.8, GuidanceComputer = part };
            Assert.AreEqual("fpa-steep", MissReason.Explain(mission, Tilted(false)).Code, "basic guidance keeps the kick advice");
            var withPart = MissReason.Explain(mission, Tilted(true));
            Assert.That(withPart.Code, Does.Not.StartWith("fpa-"));
            StringAssert.DoesNotContain("pitch kick", withPart.Line);
            // Fields only the part sets are ignored without it.
            var basic = new MissInput { ApoM = 300e3, PeriM = -900e3, MaxG = 3, Stages = 2, UpperOut = true, HandoffLiftRatio = 0.5, HandoffGammaDeg = 2 };
            Assert.That(MissReason.Explain(mission, basic).Code, Does.Not.StartWith("upper-out"));
        }

        [Test]
        public void UpperOut_WithAHealthyHandoff_IsAFuelMiss()
        {
            var (mission, _, _) = BoardStarters().Find(x => x.mission.Id == "m12");
            var f = new MissInput { ApoM = 400e3, PeriM = -1200e3, MaxG = 3, Stages = 2, GuidanceComputer = true, UpperOut = true, HandoffLiftRatio = 1.4, HandoffGammaDeg = 20, HandoffAltM = 150e3 };
            var x = MissReason.Explain(mission, f);
            Assert.AreEqual("upper-out", x.Code);
            StringAssert.Contains("Add fuel to stage 2", x.Line);
            f.FailureKind = "prop-depletion";
            Assert.AreEqual("upper-out", MissReason.Explain(mission, f).Code, "the monitor's prop-depletion is explained too, with the part");
            f.GuidanceComputer = false;
            Assert.IsNull(MissReason.Explain(mission, f), "without the part a failure explains itself, as before");
        }

        List<(CampaignMission mission, Design design, string mode)> BoardStarters()
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "board-starters.json")));
            var list = new List<(CampaignMission, Design, string)>();
            foreach (JsonObject row in file.Arr("missions"))
            {
                var mission = campaign.ById(row.Str("id"));
                Assert.IsNotNull(mission, row.Str("id"));
                var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
                Assert.IsNotNull(choice, mission.Id);
                var design = Design.FromJson(choice.Design);
                string mode = row.Str("mode");
                if (mode == "fix")
                {
                    var json = JsonMut.CloneObject(choice.Design);
                    Readout.Reference(catalog, json);
                    var report = StepperModel.Evaluate(catalog, json);
                    StepCheck check = null;
                    for (int i = 0; i < report.Checks.Length && check == null; i++)
                        if (report.Checks[i] != null && !string.IsNullOrEmpty(report.Checks[i].Code)) check = report.Checks[i];
                    StepperFix.Apply(catalog, json, check);
                    design = Design.FromJson(json);
                }
                else if (mode == "strength") design.safetyFactor = row.Num("restoreSafetyFactor");
                list.Add((mission, design, mode));
            }
            return list;
        }
    }
}
