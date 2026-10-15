// EditMode coverage for the guided stepper model. One gate per step, plus progress, navigation, and old saves.
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StepperModelTests
    {
        Catalog C => Golden.Catalog;

        [SetUp]
        public void FreshAscentCache() => ReferenceAscent.Reset();

        [Test]
        public void Catalog_HasSevenSteps_WithStructureInTanks_AndGuidanceInAero()
        {
            Assert.AreEqual(7, StepperCatalog.Count);
            Assert.AreEqual(7, StepperCatalog.All.Count);
            for (int n = 1; n <= 7; n++)
            {
                Assert.IsTrue(StepperCatalog.TryStep(n, out var step));
                var def = StepperCatalog.Get(step);
                Assert.AreEqual(n, def.Number);
                Assert.AreEqual(step, def.Step);
                Assert.LessOrEqual(def.SimpleControls.Length, 3);
                Assert.GreaterOrEqual(def.SimpleControls.Length, 1);
            }
            Assert.IsFalse(StepperCatalog.TryStep(0, out _));
            Assert.IsFalse(StepperCatalog.TryStep(8, out _));

            var tanks = StepperCatalog.Get(DesignerStep.Tanks);
            CollectionAssert.Contains(tanks.SimpleControls, "safetyFactor");
            CollectionAssert.Contains(tanks.EngineerControls, "material");
            CollectionAssert.Contains(tanks.EngineerControls, "avionics");
            CollectionAssert.AreEqual(
                new[] { "prop-depletion", "structural-maxq", "overg", "cryo-material", "separation-collision" },
                tanks.FailureKinds);

            var aero = StepperCatalog.Get(DesignerStep.Aero);
            CollectionAssert.Contains(aero.EngineerControls, "maxQThrottle");
            CollectionAssert.Contains(aero.EngineerControls, "gLimit");
            CollectionAssert.AreEqual(new[] { "tumble", "thermal", "structural-maxq" }, aero.FailureKinds);

            CollectionAssert.AreEqual(
                new[] { "engine-overpressure", "bad-mixture", "cryo-material", "engine-out" },
                StepperCatalog.Get(DesignerStep.Engines).FailureKinds);
            CollectionAssert.AreEqual(new[] { "leg-collapse", "thermal" }, StepperCatalog.Get(DesignerStep.Recovery).FailureKinds);
            Assert.AreEqual(0, StepperCatalog.Get(DesignerStep.Mission).FailureKinds.Length);
            Assert.AreEqual(0, StepperCatalog.Get(DesignerStep.Look).FailureKinds.Length);
            Assert.AreEqual(11, StepperCatalog.Get(DesignerStep.Review).FailureKinds.Length);

            foreach (var def in StepperCatalog.All)
            {
                Assert.AreNotEqual("Structure", def.Title);
                Assert.AreNotEqual("Guidance", def.Title);
            }
            Assert.AreEqual("STEP 1 OF 7", StepperCatalog.Header(DesignerStep.Mission));
            Assert.AreEqual("STEP 7 OF 7", StepperCatalog.Header(DesignerStep.Review));
        }

        [Test]
        public void Mission_PassesWhenPayloadAndOrbitAreChosen_IncludingFreeBuild()
        {
            var design = Parsed("workhorse");
            var pass = StepperChecks.Mission(design);
            Assert.AreEqual(StepGate.Pass, pass.Gate);
            Assert.IsNull(pass.Code);
            design.missionId = null;
            Assert.AreEqual(StepGate.Pass, StepperChecks.Mission(design).Gate, "free build counts as a mission");
            design.missionId = "free";
            Assert.AreEqual(StepGate.Pass, StepperChecks.Mission(design).Gate);

            design.payloadType = null;
            var missing = StepperChecks.Mission(design);
            Assert.AreEqual(StepGate.Red, missing.Gate);
            Assert.AreEqual("payload-missing", missing.Code);
            Assert.AreEqual("payload.type", missing.Field);

            design.payloadType = "leo-sat";
            design.payloadOrbit = "";
            var orbit = StepperChecks.Mission(design);
            Assert.AreEqual(StepGate.Red, orbit.Gate);
            Assert.AreEqual("orbit-missing", orbit.Code);
            Assert.AreEqual("payload.orbit", orbit.Field);

            var live = StepperModel.Evaluate(C, HangarFixtures.Template("workhorse"));
            Assert.AreEqual(StepGate.Pass, live[DesignerStep.Mission].Gate);
        }

        [Test]
        public void Engines_TwrGatesAt1_And1_3_MixtureAt6Percent()
        {
            var design = Parsed("workhorse");
            Assert.AreEqual(StepGate.Red, Engines(design, 0.99).Gate);
            Assert.AreEqual("twr-low", Engines(design, 0.99).Code);
            Assert.AreEqual("engineCount", Engines(design, 0.99).Field);

            var atBlock = Engines(design, 1.0);
            Assert.AreEqual(StepGate.Amber, atBlock.Gate, "TWR of 1.0 is under 1.3 and is not the red block");
            Assert.AreEqual("twr-marginal", atBlock.Code);

            Assert.AreEqual(StepGate.Amber, Engines(design, 1.29).Gate);
            var pass = Engines(design, 1.3);
            Assert.AreEqual(StepGate.Pass, pass.Gate);
            Assert.AreEqual(1.3, pass.Limit, 0);
            Assert.AreEqual(StepGate.Pass, Engines(design, 2.0).Gate);

            design.stages[0].mixtureOffsetSet = true;
            design.stages[0].mixtureOffsetPct = 6;
            Assert.AreEqual(StepGate.Pass, Engines(design, 1.5).Gate, "±6% is still inside the hazard line");
            design.stages[0].mixtureOffsetPct = -6;
            Assert.AreEqual(StepGate.Pass, Engines(design, 1.5).Gate);

            design.stages[0].mixtureOffsetPct = 6.01;
            var mix = Engines(design, 1.5);
            Assert.AreEqual(StepGate.Amber, mix.Gate);
            Assert.AreEqual("bad-mixture", mix.Code);
            Assert.AreEqual("mixtureOffsetPct", mix.Field);
            design.stages[0].mixtureOffsetPct = -7;
            Assert.AreEqual(StepGate.Amber, Engines(design, 1.5).Gate);

            design.stages[0].mixtureOffsetPct = 7;
            var both = Engines(design, 0.5);
            Assert.AreEqual(StepGate.Red, both.Gate);
            Assert.AreEqual("twr-low", both.Code, "a red liftoff block stays the primary fix");
            Assert.IsTrue(both.Findings.Exists(f => f.Code == "bad-mixture"));
        }

        [Test]
        public void Engines_OverpressureAndBlockAdvisories_RaiseTheGate()
        {
            var design = Parsed("workhorse");
            string id = design.stages[0].engine;
            double rated = EngineModel.EnvelopeFor(C.EngineById[id]).pcRated;
            Assert.Greater(rated, 0);
            design.stages[0].engineParams = new EngineParams { pcSet = true, pc = rated };
            Assert.AreEqual(StepGate.Pass, Engines(design, 1.5).Gate, "pc at rated has no overpressure hazard");
            design.stages[0].engineParams.pc = rated * 1.05;
            var hot = Engines(design, 1.5);
            Assert.AreEqual(StepGate.Amber, hot.Gate);
            Assert.AreEqual("engine-overpressure", hot.Code);
            Assert.AreEqual("engineParams.pc", hot.Field);

            var block = new Advisory
            {
                code = "zero-engines", severity = AdvisorySeverity.Block, tab = "engines",
                kid = "A stage has no engines.", engineer = "engineCount < 1", part = PartRef.Stage(0),
            };
            var blocked = StepperChecks.Engines(C, design, ReadoutOf(1.5), new[] { block });
            Assert.AreEqual(StepGate.Red, blocked.Gate);
            Assert.AreEqual("zero-engines", blocked.Code);
            Assert.AreEqual(0, blocked.Stage);
        }

        [Test]
        public void Engines_LiveDesign_RedWhenLiftoffCannotClimb()
        {
            var d = HangarFixtures.Template("workhorse");
            var stage = (JsonObject)d.Arr("stages")[0];
            JsonMut.Set(stage, "engineCount", 1d);
            JsonMut.Set(stage.Obj("cluster"), "pattern", "auto");
            var report = StepperModel.Evaluate(C, d);
            Assert.IsNotNull(report.Readout);
            Assert.Less(report.Readout.liftoffTWR, StepperChecks.BlockTwr);
            Assert.AreEqual(StepGate.Red, report[DesignerStep.Engines].Gate);
            Assert.AreEqual("twr-low", report[DesignerStep.Engines].Code);
            var forward = StepperModel.Forward(At(DesignerStep.Engines), report);
            Assert.IsFalse(forward.Allowed);
            Assert.IsTrue(forward.FixIt);
            Assert.AreEqual("Fix it", forward.Label);
            Assert.AreEqual("engineCount", forward.FocusField);
        }

        [Test]
        public void Tanks_MarginBelowZeroBlocks_MaxQAbove85PercentIsAmber()
        {
            var cool = ReadoutOf(1.5);
            cool.margin = 0;
            cool.required = 9000;
            cool.totalDeltaV = 9000;
            Assert.AreEqual(StepGate.Pass, StepperChecks.Tanks(cool, null).Gate);

            cool.margin = 0.04;
            Assert.AreEqual(StepGate.Pass, StepperChecks.Tanks(cool, null).Gate);

            var shortfall = ReadoutOf(1.5);
            shortfall.margin = -0.1;
            shortfall.required = 9000;
            var red = StepperChecks.Tanks(shortfall, null);
            Assert.AreEqual(StepGate.Red, red.Gate);
            Assert.AreEqual("dv-margin", red.Code);
            Assert.AreEqual("propMass", red.Field);

            var hot = ReadoutOf(1.5);
            hot.margin = 0.1;
            hot.maxQStale = false;
            hot.qLim_kPa = 100;
            hot.maxQ_kPa = 85;
            Assert.AreEqual(StepGate.Pass, StepperChecks.Tanks(hot, null).Gate, "exactly 85% of q_lim still passes");
            hot.maxQ_kPa = 85.1;
            var amber = StepperChecks.Tanks(hot, null);
            Assert.AreEqual(StepGate.Amber, amber.Gate);
            Assert.AreEqual("maxq-high", amber.Code);
            Assert.AreEqual("safetyFactor", amber.Field);

            hot.maxQStale = true;
            hot.maxQ_kPa = 200;
            var stale = StepperChecks.Tanks(hot, null);
            Assert.AreEqual(StepGate.Pass, stale.Gate, "a stale max-Q does not invent an amber");
            Assert.IsTrue(stale.Stale);

            hot.maxQStale = false;
            hot.qLim_kPa = null;
            Assert.AreEqual(StepGate.Pass, StepperChecks.Tanks(hot, null).Gate);

            shortfall.maxQStale = false;
            shortfall.maxQ_kPa = 200;
            shortfall.qLim_kPa = 40;
            Assert.AreEqual(StepGate.Red, StepperChecks.Tanks(shortfall, null).Gate, "negative margin stays red when max-Q is also hot");
        }

        [Test]
        public void Aero_FairingMissBlocks_AndTumbleBlocks_StableFitPasses()
        {
            var design = Parsed("workhorse");
            var ok = ReadoutOf(1.5);
            ok.fairingFits = true;
            ok.stabilityCal = 1.2;
            ok.minStability = 0.4;
            ok.minCar = 1.4;
            ok.maxQStale = false;
            Assert.AreEqual(StepGate.Pass, StepperChecks.Aero(C, design, ok, null).Gate);

            ok.fairingFits = false;
            var fit = StepperChecks.Aero(C, design, ok, null);
            Assert.AreEqual(StepGate.Red, fit.Gate);
            Assert.AreEqual("payload-doesnt-fit", fit.Code);
            Assert.AreEqual("fairing", fit.Field);

            ok.fairingFits = true;
            ok.minStability = -0.2;
            ok.minCar = 0.4;
            var tumble = StepperChecks.Aero(C, design, ok, null);
            Assert.AreEqual(StepGate.Red, tumble.Gate);
            Assert.AreEqual("tumble", tumble.Code);
            Assert.AreEqual("fins", tumble.Field);

            ok.minCar = 1;
            Assert.AreEqual(StepGate.Pass, StepperChecks.Aero(C, design, ok, null).Gate, "CAR of 1 is not a tumble");

            ok.minStability = null;
            ok.minCar = null;
            ok.stabilityCal = -0.4;
            Assert.IsTrue(DesignRules.PadSteered(C, design));
            Assert.AreEqual(StepGate.Pass, StepperChecks.Aero(C, design, ok, null).Gate, "a steered pad stage is not a tumble without CAR");

            design.stages[0].engine = "srm-small";
            Assert.IsFalse(DesignRules.PadSteered(C, design));
            var unguided = StepperChecks.Aero(C, design, ok, null);
            Assert.AreEqual(StepGate.Red, unguided.Gate);
            Assert.AreEqual("unstable", unguided.Code);
        }

        [Test]
        public void Recovery_SkipsWhenTheMissionDoesNotLand_AndGatesReserveWhenItDoes()
        {
            var raw = HangarFixtures.Template("workhorse");
            var design = Design.FromJson(raw);
            var readout = ReadoutOf(1.5);
            readout.landingReserve_mps = 0;
            readout.landingNeed_mps = 0;

            var skipped = StepperChecks.Recovery(design, raw, readout, false, null);
            Assert.AreEqual(StepGate.Skip, skipped.Gate);
            Assert.AreEqual("recovery-skip", skipped.Code);
            var noise = new Advisory
            {
                code = "landing-hardware", severity = AdvisorySeverity.Warn, tab = "recovery",
                kid = "Add legs.", engineer = "legs false",
            };
            var still = StepperChecks.Recovery(design, raw, readout, false, new[] { noise });
            Assert.AreEqual(StepGate.Skip, still.Gate, "a skipped recovery step does not start blocking");
            Assert.IsTrue(still.Findings.Exists(f => f.Code == "landing-hardware"));

            design.recoveryMode = "expendable";
            var needMode = StepperChecks.Recovery(design, raw, readout, true, null);
            Assert.AreEqual(StepGate.Red, needMode.Gate);
            Assert.AreEqual("landing-required", needMode.Code);
            Assert.AreEqual("recoveryMode", needMode.Field);

            design.recoveryMode = "rtls";
            design.legs = true;
            readout.landingNeed_mps = null;
            readout.maxQStale = true;
            var waiting = StepperChecks.Recovery(design, raw, readout, true, null);
            Assert.AreEqual(StepGate.Amber, waiting.Gate);
            Assert.AreEqual("landing-need-stale", waiting.Code);
            Assert.IsTrue(waiting.Stale);

            readout.landingNeed_mps = 800;
            readout.landingReserve_mps = 100;
            readout.maxQStale = false;
            var shortReserve = StepperChecks.Recovery(design, raw, readout, true, null);
            Assert.AreEqual(StepGate.Red, shortReserve.Gate);
            Assert.AreEqual("landing-reserve", shortReserve.Code);
            Assert.AreEqual(800, shortReserve.Limit, 0);

            readout.landingReserve_mps = 800;
            var covered = StepperChecks.Recovery(design, raw, readout, true, null);
            Assert.AreEqual(StepGate.Pass, covered.Gate);

            design.legs = false;
            var noLegs = StepperChecks.Recovery(design, raw, readout, true, null);
            Assert.AreEqual(StepGate.Amber, noLegs.Gate);
            Assert.AreEqual("landing-hardware", noLegs.Code);
            Assert.AreEqual("legs", noLegs.Field);

            design.recoveryMode = "barge";
            design.legs = true;
            readout.landingNeed_mps = 200;
            readout.landingReserve_mps = 200;
            Assert.AreEqual(StepGate.Pass, StepperChecks.Recovery(design, raw, readout, true, null).Gate);
        }

        [Test]
        public void Recovery_AutoSkipsFromCampaign_AndFromTheDesignFlag()
        {
            var campaign = Campaign.Load(Golden.Root);
            var free = HangarFixtures.Template("workhorse");
            var noMission = StepperModel.Evaluate(C, free, new StepperContext { Campaign = campaign });
            Assert.IsFalse(noMission.LandingRequired);
            Assert.AreEqual(StepGate.Skip, noMission[DesignerStep.Recovery].Gate);

            JsonMut.Set(free, "missionId", "m1");
            var early = StepperModel.Evaluate(C, free, new StepperContext { Campaign = campaign });
            Assert.IsFalse(early.LandingRequired);
            Assert.AreEqual(StepGate.Skip, early[DesignerStep.Recovery].Gate);

            JsonMut.Set(free, "missionId", "m6");
            var comeBack = StepperModel.Evaluate(C, free, new StepperContext { Campaign = campaign });
            Assert.IsTrue(comeBack.LandingRequired);
            Assert.AreEqual(StepGate.Red, comeBack[DesignerStep.Recovery].Gate);
            Assert.AreEqual("landing-required", comeBack[DesignerStep.Recovery].Code);

            var mission = new JsonObject();
            mission.Add("requiresLanding", false);
            JsonMut.Set(free, "mission", mission);
            var flaggedOff = StepperModel.Evaluate(C, free, new StepperContext { Campaign = campaign });
            Assert.IsFalse(flaggedOff.LandingRequired, "the design flag wins over the campaign id");
            Assert.AreEqual(StepGate.Skip, flaggedOff[DesignerStep.Recovery].Gate);

            Assert.AreEqual("rtls", StepperChecks.CanonicalMode("pad"));
            Assert.AreEqual("barge", StepperChecks.CanonicalMode("haven"));
            Assert.AreEqual("expendable", StepperChecks.CanonicalMode("expend"));
            var top = HangarFixtures.Template("workhorse");
            JsonMut.Set(top, "recoveryMode", "pad");
            Assert.AreEqual("rtls", StepperChecks.RecoveryMode(top, Design.FromJson(top)));
        }

        [Test]
        public void Look_AlwaysPasses()
        {
            var check = StepperChecks.Look();
            Assert.AreEqual(StepGate.Pass, check.Gate);
            Assert.AreEqual(DesignerStep.Look, check.Step);
            Assert.IsNull(check.Code);

            var d = HangarFixtures.Template("workhorse");
            JsonMut.Set(d.Obj("livery"), "name", "");
            var report = StepperModel.Evaluate(C, d);
            Assert.AreEqual(StepGate.Pass, report[DesignerStep.Look].Gate);
        }

        [Test]
        public void Review_UsesTheSameLaunchConfirmRulesAsDesignerV3Risk()
        {
            Assert.AreEqual("Launch", StepperLaunch.LaunchButtonLabel(false, false));
            Assert.AreEqual("Launch anyway", StepperLaunch.LaunchButtonLabel(true, false));
            Assert.AreEqual("Launch", StepperLaunch.LaunchButtonLabel(true, true));
            Assert.AreEqual("Launch", StepperLaunch.LaunchButtonLabel(false, true));
            Assert.IsTrue(StepperLaunch.NeedsConfirm(true, false));
            Assert.IsFalse(StepperLaunch.NeedsConfirm(true, true));
            Assert.IsFalse(StepperLaunch.CanLaunch(true, false));
            Assert.IsTrue(StepperLaunch.CanLaunch(true, true));
            Assert.IsTrue(StepperLaunch.CanLaunch(false, false));

            var stale = ReadoutOf(1.4);
            stale.risks = null;
            stale.maxQStale = true;
            var waiting = StepperChecks.Review(stale, false, out _, out bool hasRed);
            Assert.IsFalse(hasRed);
            Assert.AreEqual(StepGate.Pass, waiting.Gate);
            Assert.AreEqual("risks-stale", waiting.Code);
            Assert.IsTrue(waiting.Stale);

            var green = new FlightRisk { kind = "thermal", level = "green", tab = "aero", field = "nose" };
            var amber = new FlightRisk { kind = "overg", level = "amber", tab = "guidance", field = "gLimit" };
            stale.risks = new List<FlightRisk> { green, amber };
            var clear = StepperChecks.Review(stale, false, out var none, out bool stillClear);
            Assert.AreEqual(0, none.Count);
            Assert.IsFalse(stillClear);
            Assert.AreEqual(StepGate.Pass, clear.Gate);
            Assert.IsNull(clear.Code);

            var redRisk = new FlightRisk
            {
                kind = "structural-maxq", level = "red", tab = "structure", field = "safetyFactor",
                kid = "The tanks fold at max-Q.", engineer = "q > q_lim",
            };
            stale.risks = new List<FlightRisk> { green, redRisk };
            var blocked = StepperChecks.Review(stale, false, out var reds, out bool sawRed);
            Assert.IsTrue(sawRed);
            Assert.AreEqual(1, reds.Count);
            Assert.AreEqual(StepGate.Amber, blocked.Gate);
            Assert.AreEqual("structural-maxq", blocked.Code);
            Assert.AreEqual("safetyFactor", blocked.Field);
            Assert.AreEqual("The tanks fold at max-Q.", blocked.Kid);

            var armed = StepperChecks.Review(stale, true, out _, out bool stillRed);
            Assert.IsTrue(stillRed);
            Assert.AreEqual(StepGate.Pass, armed.Gate, "confirm armed leaves the launch button as Launch");

            var d = HangarFixtures.Template("workhorse");
            var live = StepperModel.Evaluate(C, d);
            Assert.AreEqual(StepperLaunch.LaunchLabel, live.LaunchLabel);
            Assert.IsFalse(live.HasRedRisk);
            Assert.IsTrue(live.CanLaunch);
            Assert.IsFalse(live.NeedsLaunchConfirm);

            d.Obj("payload"); // keep the tree
            var withRed = StepperModel.Evaluate(C, d, new StepperContext { LaunchConfirmArmed = false });
            withRed.Readout.risks = new List<FlightRisk> { redRisk };
            var reviewed = StepperChecks.Review(withRed.Readout, false, out _, out _);
            Assert.AreEqual(StepGate.Amber, reviewed.Gate);
            var progress = At(DesignerStep.Review);
            // Rebuild through Evaluate's launch fields by calling Review's predicates the way the model does.
            var confirmed = StepperModel.Evaluate(C, HangarFixtures.Template("workhorse"));
            confirmed.HasRedRisk = true;
            confirmed.NeedsLaunchConfirm = StepperLaunch.NeedsConfirm(true, false);
            confirmed.CanLaunch = StepperLaunch.CanLaunch(true, false);
            confirmed.LaunchLabel = StepperLaunch.LaunchButtonLabel(true, false);
            confirmed.Checks[(int)DesignerStep.Review] = reviewed;
            var action = StepperModel.Forward(progress, confirmed);
            Assert.AreEqual("Launch anyway", action.Label);
            Assert.IsTrue(action.NeedsConfirm);
            Assert.IsFalse(action.Launches);
            Assert.IsFalse(StepperModel.TryAdvance(progress, confirmed, true), "review never moves to another step");
        }

        [Test]
        public void Progress_OldSavesDefaultToStep1AllParts_AndRoundTrip()
        {
            var d = HangarFixtures.Template("workhorse");
            Assert.IsFalse(d.ContainsKey(StepperProgress.Field));
            var legacy = StepperProgress.Read(d);
            Assert.IsTrue(legacy.Legacy);
            Assert.AreEqual(DesignerStep.Mission, legacy.Current);
            Assert.AreEqual(StepperView.AllParts, legacy.View);
            Assert.AreEqual(0, legacy.Visited.Count);
            Assert.IsNull(legacy.AutoAssist);
            Assert.IsTrue(legacy.AutoAssistOn(DesignerDepth.Simple));
            Assert.IsFalse(legacy.AutoAssistOn(DesignerDepth.Engineer));
            Assert.IsFalse(d.ContainsKey(StepperProgress.Field), "reading an old save does not write the field");

            legacy.AutoAssist = false;
            Assert.IsFalse(legacy.AutoAssistOn(DesignerDepth.Simple), "a stored off wins in Simple");
            legacy.AutoAssist = true;
            Assert.IsTrue(legacy.AutoAssistOn(DesignerDepth.Engineer), "a stored on wins in Engineer");

            legacy.EnterGuided();
            Assert.IsFalse(legacy.Legacy);
            Assert.AreEqual(StepperView.Guided, legacy.View);
            Assert.IsTrue(legacy.Visited.Contains(DesignerStep.Mission));

            var fresh = StepperProgress.FreshGuided();
            fresh.Current = DesignerStep.Look;
            fresh.Visited.Add(DesignerStep.Engines);
            fresh.AutoAssist = false;
            fresh.ShowAllParts();
            string payload = d.Obj("payload").Str("type");
            double twr = Readout.Quick(C, d).liftoffTWR;
            fresh.Write(d);
            Assert.AreEqual(payload, d.Obj("payload").Str("type"));
            Assert.AreEqual(twr, Readout.Quick(C, d).liftoffTWR, 1e-9, "the progress field does not change the readout");

            var back = StepperProgress.Read(d);
            Assert.IsFalse(back.Legacy);
            Assert.AreEqual(DesignerStep.Look, back.Current);
            Assert.AreEqual(StepperView.AllParts, back.View);
            Assert.AreEqual(false, back.AutoAssist);
            CollectionAssert.AreEquivalent(
                new[] { DesignerStep.Mission, DesignerStep.Engines, DesignerStep.Look },
                back.Visited);

            string json = JsonWriter.Stringify(d);
            var reparsed = StepperProgress.Read(MiniJson.ParseObject(json));
            Assert.AreEqual(DesignerStep.Look, reparsed.Current);
            Assert.AreEqual(StepperView.AllParts, reparsed.View);
            Assert.AreEqual(false, reparsed.AutoAssist);

            var dropped = Design.FromJson(d).ToJson();
            Assert.IsNull(dropped.Obj(StepperProgress.Field), "Design.ToJson omits the field; the next load uses the old-save default");
            Assert.IsNotNull(d.Obj(StepperProgress.Field));
            var migrated = DesignMigration.Migrate(d, 3);
            Assert.AreEqual(3, migrated.Num("version"));
            Assert.IsNotNull(migrated.Obj(StepperProgress.Field));
            Assert.AreEqual(DesignerStep.Look, StepperProgress.Read(migrated).Current);
        }

        [Test]
        public void Progress_MalformedFieldDefaultsSafely()
        {
            var d = HangarFixtures.Template("workhorse");
            JsonMut.Set(d, StepperProgress.Field, "nope");
            Assert.IsTrue(StepperProgress.Read(d).Legacy);

            var bad = new JsonObject();
            bad.Add("current", 99d);
            bad.Add("visited", new List<object> { 0d, 3d, 8d, "nope", 4.5d });
            bad.Add("view", "sideways");
            JsonMut.Set(d, StepperProgress.Field, bad);
            var read = StepperProgress.Read(d);
            Assert.IsFalse(read.Legacy);
            Assert.AreEqual(DesignerStep.Mission, read.Current);
            Assert.AreEqual(StepperView.Guided, read.View);
            Assert.AreEqual(1, read.Visited.Count);
            Assert.IsTrue(read.Visited.Contains(DesignerStep.Tanks));
            Assert.IsNull(read.AutoAssist);

            var empty = new JsonObject();
            JsonMut.Set(d, StepperProgress.Field, empty);
            var blank = StepperProgress.Read(d);
            Assert.AreEqual(DesignerStep.Mission, blank.Current);
            Assert.AreEqual(0, blank.Visited.Count);
            Assert.IsFalse(blank.Legacy);

            Assert.DoesNotThrow(() => StepperModel.Evaluate(C, null));
            Assert.DoesNotThrow(() => StepperModel.Evaluate(C, new JsonObject()));
            var before = HangarFixtures.Template("workhorse");
            StepperModel.Evaluate(C, before);
            Assert.IsFalse(before.ContainsKey(StepperProgress.Field));
        }

        [Test]
        public void Navigation_RedBlocks_AmberConfirms_SkipJumpsRecovery_UnvisitedStayLocked()
        {
            var d = HangarFixtures.Template("workhorse");
            var report = StepperModel.Evaluate(C, d, new StepperContext { LandingRequired = false });
            Assert.AreEqual(StepGate.Skip, report[DesignerStep.Recovery].Gate);
            Assert.AreEqual(DesignerStep.Look, StepperModel.NextActive(DesignerStep.Aero, report));
            Assert.AreEqual(DesignerStep.Aero, StepperModel.PreviousActive(DesignerStep.Look, report));
            report.Checks[(int)DesignerStep.Aero] = StepperChecks.Aero(C, Parsed("workhorse"), ReadoutOf(1.5), null);
            Assert.AreEqual(StepGate.Pass, report[DesignerStep.Aero].Gate);

            var progress = StepperProgress.FreshGuided();
            progress.Current = DesignerStep.Aero;
            progress.Visited.Add(DesignerStep.Aero);
            Assert.IsTrue(StepperModel.TryAdvance(progress, report, false));
            Assert.AreEqual(DesignerStep.Look, progress.Current);
            Assert.IsFalse(progress.Visited.Contains(DesignerStep.Recovery));
            Assert.IsTrue(StepperModel.TryBack(progress, report));
            Assert.AreEqual(DesignerStep.Aero, progress.Current);

            progress.Current = DesignerStep.Recovery;
            Assert.IsTrue(StepperModel.Settle(progress, report));
            Assert.AreEqual(DesignerStep.Look, progress.Current);

            var guided = StepperProgress.FreshGuided();
            Assert.IsTrue(StepperModel.CanJump(guided, report, DesignerStep.Mission));
            Assert.IsFalse(StepperModel.CanJump(guided, report, DesignerStep.Review));
            Assert.IsTrue(StepperModel.CanJump(guided, report, DesignerStep.Recovery), "a skipped step can be opened");
            Assert.IsFalse(StepperModel.TryJump(guided, report, DesignerStep.Tanks));
            Assert.AreEqual(DesignerStep.Mission, guided.Current);
            var marks = StepperModel.Marks(guided, report);
            Assert.AreEqual(StepMark.Current, marks[0]);
            Assert.AreEqual(StepMark.Locked, marks[1]);
            Assert.AreEqual(StepMark.Skipped, marks[4]);

            guided.ShowAllParts();
            Assert.IsTrue(StepperModel.CanJump(guided, report, DesignerStep.Review));
            report.Checks[(int)DesignerStep.Engines] = Engines(Parsed("workhorse"), 1.5);
            Assert.AreEqual(StepMark.Open, StepperModel.Mark(guided, report, DesignerStep.Engines));

            report.Checks[(int)DesignerStep.Engines] = StepperChecks.Engines(C, Parsed("workhorse"), ReadoutOf(1.1), null);
            Assert.AreEqual(StepGate.Amber, report[DesignerStep.Engines].Gate);
            var atEngines = At(DesignerStep.Engines);
            var next = StepperModel.Forward(atEngines, report);
            Assert.IsTrue(next.NeedsConfirm);
            Assert.AreEqual("Next anyway", next.Label);
            Assert.IsFalse(StepperModel.TryAdvance(atEngines, report, false));
            Assert.AreEqual(DesignerStep.Engines, atEngines.Current);
            Assert.IsTrue(StepperModel.TryAdvance(atEngines, report, true));
            Assert.AreEqual(DesignerStep.Tanks, atEngines.Current);

            Assert.IsFalse(StepperModel.TryBack(At(DesignerStep.Mission), report));
        }

        [Test]
        public void Navigation_EarlierEditMarksLaterSteps_UntilTheyAreOpened()
        {
            var d = HangarFixtures.Template("workhorse");
            var before = StepperModel.Evaluate(C, d);
            foreach (JsonObject stage in d.Arr("stages")) JsonMut.Set(stage, "propMass", 1d);
            var after = StepperModel.Evaluate(C, d);
            Assert.AreEqual(StepGate.Red, after[DesignerStep.Tanks].Gate);

            var progress = StepperProgress.FreshGuided();
            progress.Visited.Add(DesignerStep.Tanks);
            progress.Visited.Add(DesignerStep.Aero);
            StepperModel.NoteEdit(progress, DesignerStep.Engines, before, after);
            Assert.IsFalse(progress.Affected.Contains(DesignerStep.Engines));
            Assert.IsTrue(progress.Affected.Contains(DesignerStep.Tanks));
            Assert.AreEqual(StepMark.Warn, StepperModel.Mark(progress, after, DesignerStep.Tanks));

            Assert.IsTrue(StepperModel.TryJump(progress, after, DesignerStep.Tanks));
            Assert.IsFalse(progress.Affected.Contains(DesignerStep.Tanks));
            Assert.AreEqual(StepMark.Current, StepperModel.Mark(progress, after, DesignerStep.Tanks));
        }

        static Design Parsed(string template) => Design.FromJson(HangarFixtures.Template(template));

        static QuickReadout ReadoutOf(double twr)
            => new QuickReadout
            {
                liftoffTWR = twr,
                margin = 0.1,
                required = 9000,
                totalDeltaV = 9900,
                fairingFits = true,
                stabilityCal = 1.2,
                maxQStale = true,
                landingReserve_mps = 0,
                landingNeed_mps = 0,
                qLim_kPa = 40,
                gLimit = 6,
            };

        static StepCheck Engines(Design design, double twr)
            => StepperChecks.Engines(Golden.Catalog, design, ReadoutOf(twr), null);

        static StepperProgress At(DesignerStep step)
        {
            var progress = StepperProgress.FreshGuided();
            progress.Current = step;
            progress.Visited.Add(step);
            return progress;
        }
    }
}
