// Fix it either clears the named flag on a fresh evaluation, or leaves the design alone and
// says which control to move and by how much.
using System.Collections.Generic;
using Kestrel.Core;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StepperFixTests
    {
        Catalog C => Golden.Catalog;

        [SetUp]
        public void Fresh()
        {
            DesignerLimits.Load(Golden.Root);
            ReferenceAscent.Reset();
        }

        [Test]
        public void MaxQ_FixRaisesStrengthUntilTheAirLoadIsInsideTheLimit()
        {
            var design = HangarFixtures.Template("workhorse");
            double sf = design.Num("safetyFactor", 1.4);
            var beforeRead = StepperModel.Evaluate(C, design).Readout;
            Assert.IsNotNull(beforeRead);
            Assert.IsTrue(beforeRead.qLim_kPa.HasValue);
            double qLimPa = beforeRead.qLim_kPa.Value * 1000.0;
            double hotPa = qLimPa * 1.05;
            ReferenceAscent.Store(ReferenceAscent.CanonicalKey(C, design), hotPa, 11000, 3);
            ExpectClear(design, DesignerStep.Tanks, "maxq-high");
            Assert.Greater(design.Num("safetyFactor"), sf);
        }

        [Test]
        public void GLimit_FixLowersTheLimitOrNamesTheAmount()
        {
            var design = HangarFixtures.Template("workhorse");
            var guidance = design.Obj("guidance");
            if (guidance == null)
            {
                guidance = new JsonObject();
                JsonMut.Set(design, "guidance", guidance);
            }
            JsonMut.Set(guidance, "gLimit", 8d);
            ReferenceAscent.Store(ReferenceAscent.CanonicalKey(C, design), 20000, 12000, 30);
            var report = StepperModel.Evaluate(C, design);
            string code = report[DesignerStep.Aero].Code;
            if (code != "payload-g")
            {
                var sim = new SimResult
                {
                    maxQ = 10000,
                    maxQAlt = 11000,
                    maxG = 30,
                    risks = new List<FlightRisk>
                    {
                        new FlightRisk
                        {
                            kind = "overg", level = "red", tab = "guidance", field = "gLimit",
                            kid = "Too many g for this cargo.", engineer = "Peak 30 g is past the cargo rating.",
                        },
                    },
                };
                ReferenceAscent.Store(ReferenceAscent.CanonicalKey(C, design), sim);
                ExpectEither(design, DesignerStep.Review, "overg");
                return;
            }
            ExpectEither(design, DesignerStep.Aero, "payload-g");
        }

        [Test]
        public void Twr_FixAddsEnginesUntilLiftoffClears()
        {
            var design = HangarFixtures.Template("workhorse");
            HangarEditor.SetEngineCount(design, PartRef.Stage(0), 1);
            int before = (int)((JsonObject)design.Arr("stages")[0]).Num("engineCount");
            ExpectClear(design, DesignerStep.Engines, "twr-low");
            Assert.Greater((int)((JsonObject)design.Arr("stages")[0]).Num("engineCount"), before);
        }

        [Test]
        public void DeltaV_FixClosesTheShortfallOrNamesTheFuel()
        {
            var design = HangarFixtures.Template("workhorse");
            HangarEditor.SetPropMass(design, PartRef.Stage(0), 1000);
            var stages = design.Arr("stages");
            for (int i = 1; i < stages.Count; i++)
                HangarEditor.SetPropMass(design, PartRef.Stage(i), 1000);
            ExpectEither(design, DesignerStep.Tanks, "dv-margin");
        }

        [Test]
        public void Aero_FixGrowsTheFairingUntilTheCargoFits()
        {
            var design = HangarFixtures.Template("workhorse");
            HangarEditor.SetPayloadType(C, design, "gto-comsat");
            HangarEditor.SetFairingLength(design, 1);
            ExpectClear(design, DesignerStep.Aero, "payload-doesnt-fit");
            Assert.Greater(design.Obj("fairing").Num("length"), 1);
        }

        [Test]
        public void Launch_FixClearsTheRedRiskOrNamesTheControl()
        {
            var design = HangarFixtures.Template("workhorse");
            var readout = StepperModel.Evaluate(C, design).Readout;
            double qLimPa = (readout.qLim_kPa ?? 35) * 1000.0;
            var sim = new SimResult
            {
                maxQ = qLimPa * 1.2,
                maxQAlt = 11000,
                maxG = 3,
                risks = new List<FlightRisk>
                {
                    new FlightRisk
                    {
                        kind = "structural-maxq",
                        level = "red",
                        tab = "structure",
                        field = "safetyFactor",
                        kid = "The tanks fold when the air pushes hardest.",
                        engineer = "Air load is past the wall limit.",
                    },
                },
            };
            ReferenceAscent.Store(ReferenceAscent.CanonicalKey(C, design), sim);
            ExpectClear(design, DesignerStep.Review, "structural-maxq");
        }

        [Test]
        public void Twr_FixClearsLiftoffEvenWhenTheMixtureIsOff()
        {
            var design = HangarFixtures.Template("workhorse");
            HangarEditor.SetEngineCount(design, PartRef.Stage(0), 1);
            JsonMut.Set((JsonObject)design.Arr("stages")[0], "mixtureOffsetPct", 8d);
            var check = StepperModel.Evaluate(C, design)[DesignerStep.Engines];
            Assert.AreEqual("twr-low", check.Code);
            var result = StepperFix.Apply(C, design, check);
            Assert.IsTrue(result.Cleared, result.Instruction);
            Assert.AreNotEqual("twr-low", StepperModel.Evaluate(C, design)[DesignerStep.Engines].Code);
            Assert.Greater((int)((JsonObject)design.Arr("stages")[0]).Num("engineCount"), 1);
        }

        [Test]
        public void TwrFixIsOneUndoAndRestoresTheFlag()
        {
            var session = new HangarSession(C, HangarFixtures.Data, HangarFixtures.Template("workhorse"));
            session.Apply(d => HangarEditor.SetEngineCount(d, PartRef.Stage(0), 1));
            var check = StepperModel.Evaluate(C, session.Design)[DesignerStep.Engines];
            Assert.AreEqual("twr-low", check.Code);
            var result = new StepperFixResult();
            session.Apply(d =>
            {
                result = StepperFix.Apply(C, d, check);
                return result.Edit;
            });
            Assert.IsTrue(result.Cleared);
            Assert.IsTrue(session.history.CanUndo);
            Assert.AreNotEqual("twr-low", StepperModel.Evaluate(C, session.Design)[DesignerStep.Engines].Code);
            session.Undo();
            Assert.AreEqual("twr-low", StepperModel.Evaluate(C, session.Design)[DesignerStep.Engines].Code);
            Assert.AreEqual(1, (int)((JsonObject)session.Design.Arr("stages")[0]).Num("engineCount"));
        }

        [Test]
        public void MaxQWarningUsesPlainNumbers()
        {
            var design = HangarFixtures.Template("workhorse");
            var beforeRead = StepperModel.Evaluate(C, design).Readout;
            double qLimPa = beforeRead.qLim_kPa.Value * 1000.0;
            ReferenceAscent.Store(ReferenceAscent.CanonicalKey(C, design), qLimPa * 1.05, 11000, 3);
            var check = StepperModel.Evaluate(C, design)[DesignerStep.Tanks];
            Assert.AreEqual("maxq-high", check.Code);
            string kid = StepperPlain.Kid(check);
            string numbers = StepperPlain.Numbers(check);
            StringAssert.Contains("air", kid.ToLowerInvariant());
            StringAssert.Contains("kPa", numbers);
            StringAssert.DoesNotContain("q_lim", kid);
            StringAssert.DoesNotContain("q_lim", numbers);
            Assert.IsFalse(PlayerText.Leak(kid), kid);
            Assert.IsFalse(PlayerText.Leak(numbers), numbers);
        }

        [Test]
        public void AnEditKeepsTheStepIndexWhenTheDesignChanges()
        {
            var design = HangarFixtures.Template("workhorse");
            var progress = StepperProgress.FreshGuided();
            progress.Current = DesignerStep.Tanks;
            progress.Visited.Add(DesignerStep.Tanks);
            progress.Write(design);
            var before = StepperModel.Evaluate(C, design);
            double mass = ((JsonObject)design.Arr("stages")[0]).Num("propMass");
            HangarEditor.SetPropMass(design, PartRef.Stage(0), mass + 1000);
            var after = StepperModel.Evaluate(C, design);
            StepperModel.NoteEdit(progress, DesignerStep.Tanks, before, after);
            Assert.IsFalse(StepperModel.Settle(progress, after));
            Assert.AreEqual(DesignerStep.Tanks, progress.Current);
            Assert.AreEqual((int)DesignerStep.Tanks, (int)design.Obj(StepperProgress.Field).Num("current"));
        }

        static void ExpectClear(JsonObject design, DesignerStep step, string code)
        {
            var result = Run(design, step, code);
            Assert.IsTrue(result.Cleared, code + " " + result.Instruction);
            Assert.IsTrue(result.Edit.changed, code);
            Assert.AreNotEqual(code, StepperModel.Evaluate(Golden.Catalog, design)[step].Code, code);
        }

        static void ExpectEither(JsonObject design, DesignerStep step, string code)
        {
            string snap = HangarSession.Snapshot(design);
            var result = Run(design, step, code);
            if (!string.IsNullOrEmpty(result.Instruction))
                Assert.IsFalse(PlayerText.Leak(result.Instruction), result.Instruction);
            var after = StepperModel.Evaluate(Golden.Catalog, design)[step];
            if (result.Cleared || (result.Edit.changed && after.Code != code))
            {
                Assert.IsTrue(result.Edit.changed, code);
                Assert.AreNotEqual(code, after.Code, code);
                return;
            }
            Assert.IsFalse(result.Edit.changed, code + " edited without clearing");
            Assert.That(result.Instruction, Does.Not.Null.And.Not.Empty, code);
            Assert.IsFalse(string.IsNullOrEmpty(result.Field), code);
            Assert.That(result.Instruction, Does.Match(@"\d"), result.Instruction);
            Assert.IsFalse(PlayerText.Leak(result.Instruction), result.Instruction);
            Assert.AreEqual(snap, HangarSession.Snapshot(design), code);
        }

        static StepperFixResult Run(JsonObject design, DesignerStep step, string code)
        {
            var before = StepperModel.Evaluate(Golden.Catalog, design);
            Assert.AreEqual(code, before[step].Code, step + " setup");
            var result = StepperFix.Apply(Golden.Catalog, design, before[step]);
            if (!string.IsNullOrEmpty(result.Instruction))
                Assert.IsFalse(PlayerText.Leak(result.Instruction), result.Instruction);
            return result;
        }

    }
}
