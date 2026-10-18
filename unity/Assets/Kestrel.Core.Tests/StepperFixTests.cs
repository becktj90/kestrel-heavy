// One Fix it press clears the flagged warning and the other current warnings, or explains
// the tradeoff. Undo restores the whole pass in one step.
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

        [Test]
        public void MaxQ_HighStrength_OnePressClearsTheAirLoad()
        {
            var raw = HangarFixtures.Template("workhorse");
            HangarEditor.SetSafetyFactor(raw, 2.25);
            var session = new HangarSession(C, HangarFixtures.Data, raw);
            ReferenceAscent.Store(ReferenceAscent.CanonicalKey(C, session.Design), 47400, 11000, 3);
            var before = HangarSession.Snapshot(session.Design);
            var check = StepperModel.Evaluate(C, session.Design)[DesignerStep.Tanks];
            Assert.AreEqual("maxq-high", check.Code);
            OnePress(session, before, check);
        }

        [Test]
        public void MaxQ_AlreadyAtTheStops_ExplainsTheTradeoff()
        {
            var raw = HangarFixtures.Template("workhorse");
            HangarEditor.SetSafetyFactor(raw, DesignerLimits.Active.safetyMax);
            HangarEditor.SetNose(raw, "von-karman");
            HangarEditor.SetFins(raw, true);
            HangarEditor.SetFinSpan(raw, 4);
            MaxQThrottle.SetPercent(raw, 40);
            var session = new HangarSession(C, HangarFixtures.Data, raw);
            ReferenceAscent.Store(ReferenceAscent.CanonicalKey(C, session.Design), 80000, 11000, 3);
            string before = HangarSession.Snapshot(session.Design);
            var check = StepperModel.Evaluate(C, session.Design)[DesignerStep.Tanks];
            Assert.AreEqual("maxq-high", check.Code);
            var result = StepperFix.Apply(C, session.Design, check);
            Assert.IsFalse(result.Cleared, result.Instruction);
            StringAssert.Contains("kPa", result.Instruction);
            StringAssert.DoesNotContain("Lower the Max-Q", result.Instruction);
            Assert.IsFalse(PlayerText.Leak(result.Instruction), result.Instruction);
            Assert.That(result.Instruction, Does.Match(@"\d"));
            if (!result.Edit.changed)
                Assert.AreEqual(before, HangarSession.Snapshot(session.Design));
        }

        [Test]
        public void LowTwr_OnePressClearsAndUndoRestores()
        {
            var raw = HangarFixtures.Template("workhorse");
            HangarEditor.SetEngineCount(raw, PartRef.Stage(0), 1);
            var session = new HangarSession(C, HangarFixtures.Data, raw);
            var before = HangarSession.Snapshot(session.Design);
            var check = StepperModel.Evaluate(C, session.Design)[DesignerStep.Engines];
            Assert.AreEqual("twr-low", check.Code);
            OnePress(session, before, check);
        }

        [Test]
        public void GLimit_OnePressClearsOrExplains()
        {
            var raw = HangarFixtures.Template("workhorse");
            var guidance = raw.Obj("guidance") ?? new JsonObject();
            JsonMut.Set(raw, "guidance", guidance);
            JsonMut.Set(guidance, "gLimit", 8d);
            var session = new HangarSession(C, HangarFixtures.Data, raw);
            ReferenceAscent.Store(ReferenceAscent.CanonicalKey(C, session.Design), 20000, 12000, 30);
            var before = HangarSession.Snapshot(session.Design);
            var report = StepperModel.Evaluate(C, session.Design);
            var check = report[DesignerStep.Aero];
            Assert.IsTrue(check.Gate == StepGate.Amber || check.Gate == StepGate.Red, check.Code);
            Press(session, before, check, allowExplain: true);
        }

        [Test]
        public void DeltaV_OnePressClearsOrExplains()
        {
            var raw = HangarFixtures.Template("workhorse");
            HangarEditor.SetPropMass(raw, PartRef.Stage(0), 1000);
            var stages = raw.Arr("stages");
            for (int i = 1; i < stages.Count; i++)
                HangarEditor.SetPropMass(raw, PartRef.Stage(i), 1000);
            var session = new HangarSession(C, HangarFixtures.Data, raw);
            var before = HangarSession.Snapshot(session.Design);
            var check = StepperModel.Evaluate(C, session.Design)[DesignerStep.Tanks];
            Assert.AreEqual("dv-margin", check.Code);
            Press(session, before, check, allowExplain: true);
        }

        [Test]
        public void Fairing_OnePressClearsAndUndoRestores()
        {
            var raw = HangarFixtures.Template("workhorse");
            HangarEditor.SetPayloadType(C, raw, "gto-comsat");
            HangarEditor.SetFairingLength(raw, 1);
            var session = new HangarSession(C, HangarFixtures.Data, raw);
            var before = HangarSession.Snapshot(session.Design);
            var check = StepperModel.Evaluate(C, session.Design)[DesignerStep.Aero];
            Assert.AreEqual("payload-doesnt-fit", check.Code);
            OnePress(session, before, check);
        }

        [Test]
        public void RefreshDerivedUpdatesTheChipWithoutAnUndo()
        {
            var session = new HangarSession(C, HangarFixtures.Data, HangarFixtures.Template("workhorse"));
            int derived = 0, changed = 0;
            session.DerivedChanged += () => derived++;
            session.Changed += () => changed++;
            session.RefreshDerived();
            Assert.AreEqual(1, derived);
            Assert.AreEqual(0, changed);
            Assert.IsFalse(session.history.CanUndo);
        }

        [Test]
        public void MaxQThrottle_LegacyFractionNudgesIntoAPercent()
        {
            var design = HangarFixtures.Template("workhorse");
            var guidance = new JsonObject();
            JsonMut.Set(guidance, "maxQThrottle", 0.7);
            JsonMut.Set(design, "guidance", guidance);
            Assert.AreEqual(70, MaxQThrottle.Percent(design), 1e-6);
            Assert.AreEqual("70%", MaxQThrottle.Label(design));
            var edit = MaxQThrottle.Nudge(design, -1);
            Assert.IsTrue(edit.changed);
            Assert.AreEqual(65, design.Obj("guidance").Num("maxQThrottle"), 1e-6);
            Assert.AreEqual("65%", MaxQThrottle.Label(design));
            Assert.IsFalse(MaxQThrottle.SetPercent(design, 65).changed);
        }

        static void OnePress(HangarSession session, string before, StepCheck check)
            => Press(session, before, check, allowExplain: false);

        static void Press(HangarSession session, string before, StepCheck check, bool allowExplain)
        {
            StepperFixResult result = null;
            session.Apply(d =>
            {
                result = StepperFix.Apply(Golden.Catalog, d, check);
                return result.Edit;
            });
            Assert.IsNotNull(result);
            Assert.IsFalse(PlayerText.Leak(result.Instruction ?? ""), result.Instruction);
            StringAssert.DoesNotContain("Lower the Max-Q", result.Instruction ?? "");
            int blocks = 0, warns = 0;
            foreach (var advisory in session.Advisories)
            {
                if (advisory.severity == AdvisorySeverity.Block) blocks++;
                else if (advisory.severity == AdvisorySeverity.Warn) warns++;
            }
            if (result.Cleared)
            {
                var left = new System.Text.StringBuilder(result.Instruction);
                foreach (var advisory in session.Advisories)
                    left.Append(" | ").Append(advisory.severity).Append(" ").Append(advisory.code);
                Assert.AreEqual(0, blocks, left.ToString());
                Assert.AreEqual(0, warns, left.ToString());
                Assert.That(result.Instruction, Does.Contain("→"));
            }
            else
            {
                Assert.IsTrue(allowExplain, result.Instruction);
                Assert.That(result.Instruction, Does.Not.Null.And.Not.Empty);
                Assert.That(result.Instruction, Does.Match(@"\d"), result.Instruction);
            }
            if (result.Edit.changed)
            {
                Assert.IsTrue(session.history.CanUndo);
                session.Undo();
                Assert.AreEqual(before, HangarSession.Snapshot(session.Design));
                Assert.IsFalse(session.history.CanUndo);
            }
            else
                Assert.AreEqual(before, HangarSession.Snapshot(session.Design));
        }

        static void ExpectClear(JsonObject design, DesignerStep step, string code)
        {
            var result = Run(design, step, code);
            var left = new System.Text.StringBuilder(result.Instruction);
            var after = StepperModel.Evaluate(Golden.Catalog, design);
            if (after.Advisories != null)
                foreach (var advisory in after.Advisories)
                    left.Append(" | ").Append(advisory.code);
            Assert.IsTrue(result.Cleared, code + " " + left);
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
            if (result.Edit.changed)
            {
                Assert.That(result.Instruction, Does.Not.Null.And.Not.Empty, code);
                Assert.That(result.Instruction, Does.Match(@"\d"), result.Instruction);
                Assert.IsFalse(PlayerText.Leak(result.Instruction), result.Instruction);
                StringAssert.DoesNotContain("Lower the Max-Q", result.Instruction);
                return;
            }
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
