// Designer audit: every template, every knob at its bounds, Fix it across warning combinations,
// undo/redo, save/load, and a few thousand random edits. No NaN or Infinity in the document.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class DesignerAuditTests
    {
        const int FuzzEdits = 2000;
        const int FuzzSeed = 20261018;

        Catalog C => Golden.Catalog;

        [SetUp]
        public void Fresh()
        {
            DesignerLimits.Load(Golden.Root);
            ReferenceAscent.Reset();
        }

        [Test]
        public void EveryTemplateAndPreset_IsFiniteAndLaunchable()
        {
            Assert.Greater(HangarFixtures.Data.templates.Count, 0);
            foreach (var starter in HangarFixtures.Data.templates)
            {
                var design = HangarFixtures.Template(starter.id);
                AssertFinite(design, starter.id);
                var shape = Validate.Shape(C, design);
                Assert.IsTrue(shape.ok, starter.id + ": " + string.Join("; ", shape.errors));
                var readout = Readout.Quick(C, design);
                AssertCoreReadout(readout, starter.id);
                var session = new HangarSession(C, HangarFixtures.Data, design);
                Assert.IsTrue(session.LaunchEnabled, starter.id);
                var report = StepperModel.Evaluate(C, session.Design);
                foreach (var def in StepperCatalog.All)
                {
                    Assert.IsNotNull(report[def.Step], starter.id + " " + def.Step);
                    Assert.IsNotNull(def.SimpleControls);
                    Assert.IsNotNull(def.EngineerControls);
                }
            }
            int presets = 0;
            foreach (var preset in HangarFixtures.Presets())
            {
                presets++;
                AssertFinite(preset, "preset");
                AssertCoreReadout(Readout.Quick(C, preset), "preset");
            }
            Assert.Greater(presets, 0);
        }

        [Test]
        public void EveryOption_AcceptsEachLegalChoice()
        {
            var design = HangarFixtures.Template("workhorse");
            foreach (var choice in HangarEditor.EngineChoices(C, design, PartRef.Stage(0)))
            {
                var edit = HangarEditor.SetEngine(C, design, PartRef.Stage(0), choice.id);
                Assert.IsTrue(edit.changed || Stage0(design).Str("engine") == choice.id, choice.id);
                AssertFinite(design, choice.id);
            }
            foreach (var pattern in DesignerLimits.Active.patterns)
            {
                HangarEditor.SetClusterPattern(design, PartRef.Stage(0), pattern.id);
                Assert.AreEqual(pattern.id, Stage0(design).Obj("cluster").Str("pattern"), pattern.id);
                if (pattern.fixedCount)
                    Assert.AreEqual(pattern.count, (int)Stage0(design).Num("engineCount"), pattern.id);
            }
            foreach (var id in new[] { "kerolox", "methalox", "hydrolox" })
            {
                var edit = HangarEditor.SetPropellant(C, design, PartRef.Stage(0), id);
                Assert.IsTrue(edit.changed || Stage0(design).Str("propellant") == id, id);
                Assert.AreEqual(id, Stage0(design).Str("engine") == null ? id : Stage0(design).Str("propellant"), id);
            }
            foreach (var nose in DesignerKnobs.Noses)
            {
                var edit = DesignerKnobs.SetNose(design, nose);
                Assert.IsTrue(edit.changed || design.Obj("aero").Str("nose") == nose, nose);
                var shape = Validate.Shape(C, design);
                Assert.IsFalse(Joined(shape.errors).Contains("aero.nose"), nose + " " + Joined(shape.errors));
            }
            foreach (var mode in new[] { "expendable", "rtls", "barge" })
            {
                JsonMut.Set(design, "recoveryMode", mode);
                var rec = design.Obj("recovery") ?? new JsonObject();
                JsonMut.Set(design, "recovery", rec);
                JsonMut.Set(rec, "mode", mode);
                Assert.AreEqual(mode, StepperChecks.RecoveryMode(design, null));
            }
            foreach (var tier in HangarEditor.AvionicsTiers)
                HangarEditor.SetAvionics(design, tier);
            foreach (var n in DesignerLimits.Active.strapOnCounts)
                HangarEditor.SetBoosterCount(C, design, n);
            AssertFinite(design, "options");
            AssertCoreReadout(Readout.Quick(C, design), "options");
        }

        [Test]
        public void PitchKickAlt_KeepsALegalCampaignAltitude()
        {
            Assert.AreEqual(50000, DesignerLimits.Active.pitchKickAltMax, 0);
            Assert.AreEqual(DesignerLimits.Active.pitchKickAltMax, DesignerKnobs.PitchAltMax, 0);
            var design = HangarFixtures.Template("workhorse");
            Assert.IsTrue(DesignerKnobs.SetGuidance(design, "pitchKickAlt", 12000).changed);
            Assert.AreEqual(12000, design.Obj("guidance").Num("pitchKickAlt"), 1e-6);
            Assert.IsTrue(DesignerKnobs.NudgeGuidance(design, "pitchKickAlt", 1).changed);
            Assert.AreEqual(12050, design.Obj("guidance").Num("pitchKickAlt"), 1e-6);
            DesignerKnobs.SetGuidance(design, "pitchKickAlt", 12000);
            Assert.IsTrue(DesignerKnobs.NudgeGuidance(design, "pitchKickAlt", -1).changed);
            Assert.AreEqual(11950, design.Obj("guidance").Num("pitchKickAlt"), 1e-6);
            DesignerKnobs.SetGuidance(design, "pitchKickAlt", 90000);
            Assert.AreEqual(DesignerKnobs.PitchAltMax, design.Obj("guidance").Num("pitchKickAlt"), 1e-6);
        }

        [Test]
        public void EngineChange_ReclampsOverrides_AndNudgeUsesTheResolvedPressure()
        {
            var design = HangarFixtures.Template("workhorse");
            var stage = PartRef.Stage(0);
            HangarEditor.SetEngine(C, design, stage, "gg-kerolox");
            double ggMax = DesignerKnobs.PcMax(C, design, 0);
            Assert.IsTrue(DesignerKnobs.SetEnginePc(C, design, 0, ggMax).changed);
            Assert.AreEqual(ggMax, Stage0(design).Obj("engineParams").Num("pc"), 1e-4);
            DesignerKnobs.SetExpansionRatio(C, design, 0, 40);
            Assert.AreEqual(40, Stage0(design).Obj("engineParams").Num("expansionRatio"), 1e-6);

            HangarEditor.SetEngine(C, design, stage, "pf-kerolox");
            double pfMax = DesignerKnobs.PcMax(C, design, 0);
            Assert.Less(pfMax, 20);
            Assert.AreEqual(pfMax, Stage0(design).Obj("engineParams").Num("pc"), 1e-4);
            Assert.AreEqual(40, Stage0(design).Obj("engineParams").Num("expansionRatio"), 1e-6, "an expansion that still fits stays");
            Assert.AreEqual(pfMax, DesignerKnobs.ShownPc(C, design, 0), 1e-4);
            Assert.IsFalse(DesignerKnobs.NudgePc(C, design, 0, 1).changed);
            Assert.AreEqual(pfMax, DesignerKnobs.ShownPc(C, design, 0), 1e-4);

            var stale = HangarFixtures.Template("workhorse");
            var body = Stage0(stale);
            JsonMut.Set(body, "engine", "pf-kerolox");
            JsonMut.Set(body, "propellant", "kerolox");
            var parms = new JsonObject();
            JsonMut.Set(parms, "pc", 112d);
            JsonMut.Set(body, "engineParams", parms);
            double resolved = DesignerKnobs.PcMax(C, stale, 0);
            Assert.AreEqual(resolved, DesignerKnobs.ShownPc(C, stale, 0), 1e-4);
            Assert.Less(DesignerKnobs.ShownPc(C, stale, 0), 20);
            Assert.IsFalse(DesignerKnobs.NudgePc(C, stale, 0, 1).changed);
            Assert.AreEqual(112, body.Obj("engineParams").Num("pc"), 1e-6);
            Assert.AreEqual(resolved, DesignerKnobs.ShownPc(C, stale, 0), 1e-4);
        }

        [Test]
        public void PropellantChoices_IncludeEveryFuelWithAnEngine()
        {
            var ids = HangarEditor.PropellantChoices(C, null);
            foreach (var id in new[] { "kerolox", "methalox", "hydrolox", "udmh-nto", "mmh-nto", "solid-apcp", "hybrid" })
                Assert.IsTrue(ids.Contains(id), id);
            var kept = HangarEditor.PropellantChoices(C, "stored-fuel");
            Assert.IsTrue(kept.Contains("stored-fuel"));
            Assert.IsTrue(kept.Contains("udmh-nto"));
            Assert.AreEqual(ids.Count + 1, kept.Count);
        }

        [Test]
        public void EveryStepper_StopsAtMinAndMax()
        {
            var lim = DesignerLimits.Active;
            var design = HangarFixtures.Template("workhorse");
            HangarEditor.SetFins(design, true);

            Pump(design, 1, () => DesignerKnobs.NudgeMixture(design, 0, 1), () => Stage0(design).Num("mixtureOffsetPct"), DesignerKnobs.MixtureMax);
            Pump(design, -1, () => DesignerKnobs.NudgeMixture(design, 0, -1), () => Stage0(design).Num("mixtureOffsetPct"), DesignerKnobs.MixtureMin);
            Assert.IsFalse(DesignerKnobs.SetMixtureOffset(design, 0, double.NaN).changed);
            Assert.IsFalse(DesignerKnobs.SetMixtureOffset(design, 0, double.PositiveInfinity).changed);

            Pump(design, 1, () => DesignerKnobs.NudgePc(C, design, 0, 1), () => DesignerKnobs.ShownPc(C, design, 0), DesignerKnobs.PcMax(C, design, 0));
            Pump(design, -1, () => DesignerKnobs.NudgePc(C, design, 0, -1), () => DesignerKnobs.ShownPc(C, design, 0), DesignerKnobs.PcMin(C, design, 0));
            var unclamped = HangarFixtures.Template("workhorse");
            Assert.IsTrue(DesignerKnobs.SetEnginePc(null, unclamped, 0, 280).changed);
            Assert.AreEqual(280, Stage0(unclamped).Obj("engineParams").Num("pc"), 1e-6);
            var clamped = HangarFixtures.Template("workhorse");
            DesignerKnobs.SetEnginePc(C, clamped, 0, 280);
            Assert.AreEqual(DesignerKnobs.PcMax(C, clamped, 0), DesignerKnobs.ShownPc(C, clamped, 0), 1e-6);
            Assert.Less(DesignerKnobs.ShownPc(C, clamped, 0), 280);

            Pump(design, 1, () => DesignerKnobs.NudgeExpansion(C, design, 0, 1), () => DesignerKnobs.ShownExpansion(C, design, 0), ExpansionMax(design));
            Pump(design, -1, () => DesignerKnobs.NudgeExpansion(C, design, 0, -1), () => DesignerKnobs.ShownExpansion(C, design, 0), ExpansionMin(design));
            Pump(design, 1, () => DesignerKnobs.NudgeGimbal(C, design, 0, 1), () => DesignerKnobs.ShownGimbal(C, design, 0), GimbalMax(design));
            Pump(design, -1, () => DesignerKnobs.NudgeGimbal(C, design, 0, -1), () => DesignerKnobs.ShownGimbal(C, design, 0), 0);
            Pump(design, 1, () => DesignerKnobs.NudgeThrottle(C, design, 0, 1), () => DesignerKnobs.ShownThrottle(C, design, 0), DesignerKnobs.ThrottleMax);
            Pump(design, -1, () => DesignerKnobs.NudgeThrottle(C, design, 0, -1), () => DesignerKnobs.ShownThrottle(C, design, 0), DesignerKnobs.ThrottleMin);

            Pump(design, 1, () => HangarEditor.SetEngineCount(design, PartRef.Stage(0), (int)Stage0(design).Num("engineCount") + 1),
                () => Stage0(design).Num("engineCount"), lim.engineCountMax);
            Pump(design, -1, () => HangarEditor.SetEngineCount(design, PartRef.Stage(0), (int)Stage0(design).Num("engineCount") - 1),
                () => Stage0(design).Num("engineCount"), lim.engineCountMin);

            Pump(design, 1, () => DesignerKnobs.NudgePressure(design, 0, 1), () => DesignerKnobs.ShownPressure(design, 0), DesignerKnobs.PressureMax);
            Pump(design, -1, () => DesignerKnobs.NudgePressure(design, 0, -1), () => DesignerKnobs.ShownPressure(design, 0), DesignerKnobs.PressureMin);
            HangarEditor.SetPropMass(design, PartRef.Stage(0), lim.propMassMax - 1000);
            Pump(design, 1, () => HangarEditor.SetPropMass(design, PartRef.Stage(0), Stage0(design).Num("propMass") + 1000),
                () => Stage0(design).Num("propMass"), lim.propMassMax);
            HangarEditor.SetPropMass(design, PartRef.Stage(0), lim.propMassMin + 1);
            Pump(design, -1, () => HangarEditor.SetPropMass(design, PartRef.Stage(0), Stage0(design).Num("propMass") * 0.5),
                () => Stage0(design).Num("propMass"), lim.propMassMin);
            Pump(design, 1, () => HangarEditor.SetSafetyFactor(design, design.Num("safetyFactor", 1.4) + lim.safetyStep),
                () => design.Num("safetyFactor"), lim.safetyMax);
            Pump(design, -1, () => HangarEditor.SetSafetyFactor(design, design.Num("safetyFactor", 1.4) - lim.safetyStep),
                () => design.Num("safetyFactor"), lim.safetyMin);

            Pump(design, 1, () => HangarEditor.AddStage(C, design, design.Arr("stages").Count),
                () => design.Arr("stages").Count, lim.stagesMax);
            Pump(design, -1, () => HangarEditor.RemoveStage(design, design.Arr("stages").Count - 1),
                () => design.Arr("stages").Count, lim.stagesMin);

            Pump(design, 1, () => HangarEditor.SetFairingLength(design, design.Obj("fairing").Num("length") + 0.5),
                () => design.Obj("fairing").Num("length"), lim.fairingLengthMax);
            Pump(design, -1, () => HangarEditor.SetFairingLength(design, design.Obj("fairing").Num("length") - 0.5),
                () => design.Obj("fairing").Num("length"), lim.fairingLengthMin);
            Pump(design, 1, () => HangarEditor.SetDiameter(design, PartRef.Fairing, design.Obj("fairing").Num("diameter") + 0.1),
                () => design.Obj("fairing").Num("diameter"), lim.diameterMax);
            Pump(design, -1, () => HangarEditor.SetDiameter(design, PartRef.Fairing, design.Obj("fairing").Num("diameter") - 0.1),
                () => design.Obj("fairing").Num("diameter"), lim.diameterMin);

            Pump(design, 1, () => HangarEditor.SetFinCount(design, (int)design.Obj("aero").Num("finCount", 4) + 1),
                () => design.Obj("aero").Num("finCount", 4), lim.finCountMax);
            Pump(design, -1, () => HangarEditor.SetFinCount(design, (int)design.Obj("aero").Num("finCount", 4) - 1),
                () => design.Obj("aero").Num("finCount", 4), lim.finCountMin);
            Pump(design, 1, () => HangarEditor.SetFinSpan(design, design.Obj("aero").Num("finSpanM", 1) + lim.finSpanStep),
                () => design.Obj("aero").Num("finSpanM", 1), lim.finSpanMax);
            Pump(design, -1, () => HangarEditor.SetFinSpan(design, design.Obj("aero").Num("finSpanM", 1) - lim.finSpanStep),
                () => design.Obj("aero").Num("finSpanM", 1), lim.finSpanMin);

            Pump(design, 1, () => MaxQThrottle.Nudge(design, 1), () => MaxQThrottle.Percent(design), 100);
            Pump(design, -1, () => MaxQThrottle.Nudge(design, -1), () => MaxQThrottle.Percent(design), MaxQThrottle.FloorPct);
            Assert.IsTrue(DesignerKnobs.SetGuidance(design, "maxQThrottle", 0.7).changed || Math.Abs(design.Obj("guidance").Num("maxQThrottle") - 0.7) < 1e-9);
            DesignerKnobs.SetGuidance(design, "maxQThrottle", 0.7);
            Assert.AreEqual(0.7, design.Obj("guidance").Num("maxQThrottle"), 1e-9);

            Pump(design, 1, () => DesignerKnobs.NudgeGuidance(design, "gLimit", 1), () => DesignerKnobs.ShownGuidance(design, "gLimit"), DesignerKnobs.GLimitMax);
            Pump(design, -1, () => DesignerKnobs.NudgeGuidance(design, "gLimit", -1), () => DesignerKnobs.ShownGuidance(design, "gLimit"), DesignerKnobs.GLimitMin);
            DesignerKnobs.SetGuidance(design, "pitchKickAlt", DesignerKnobs.PitchAltMax - DesignerKnobs.PitchAltStep * 2);
            Pump(design, 1, () => DesignerKnobs.NudgeGuidance(design, "pitchKickAlt", 1), () => DesignerKnobs.ShownGuidance(design, "pitchKickAlt"), DesignerKnobs.PitchAltMax);
            DesignerKnobs.SetGuidance(design, "pitchKickAlt", DesignerKnobs.PitchAltMin + DesignerKnobs.PitchAltStep * 2);
            Pump(design, -1, () => DesignerKnobs.NudgeGuidance(design, "pitchKickAlt", -1), () => DesignerKnobs.ShownGuidance(design, "pitchKickAlt"), DesignerKnobs.PitchAltMin);
            Pump(design, 1, () => DesignerKnobs.NudgeGuidance(design, "pitchKickDeg", 1), () => DesignerKnobs.ShownGuidance(design, "pitchKickDeg"), DesignerKnobs.PitchDegMax);
            Pump(design, -1, () => DesignerKnobs.NudgeGuidance(design, "pitchKickDeg", -1), () => DesignerKnobs.ShownGuidance(design, "pitchKickDeg"), DesignerKnobs.PitchDegMin);
            Pump(design, 1, () => DesignerKnobs.NudgeGuidance(design, "turnShape", 1), () => DesignerKnobs.ShownGuidance(design, "turnShape"), DesignerKnobs.TurnMax);
            Pump(design, -1, () => DesignerKnobs.NudgeGuidance(design, "turnShape", -1), () => DesignerKnobs.ShownGuidance(design, "turnShape"), DesignerKnobs.TurnMin);

            HangarEditor.SetPayloadMass(design, lim.payloadMassMax - 50);
            Pump(design, 1, () => HangarEditor.SetPayloadMass(design, design.Obj("payload").Num("mass") + 100),
                () => design.Obj("payload").Num("mass"), lim.payloadMassMax);
            HangarEditor.SetPayloadMass(design, lim.payloadMassMin + 50);
            Pump(design, -1, () => HangarEditor.SetPayloadMass(design, design.Obj("payload").Num("mass") - 100),
                () => design.Obj("payload").Num("mass"), lim.payloadMassMin);
            HangarEditor.SetPropMass(design, PartRef.Stage(0), 10000);
            Pump(design, 1, () => HangarEditor.SetLandingReserve(design, 0, Reserve(design) + 0.01),
                () => Reserve(design), lim.landingReserveMax, 0.02);
            Pump(design, -1, () => HangarEditor.SetLandingReserve(design, 0, Reserve(design) - 0.01),
                () => Reserve(design), lim.landingReserveMin, 0.02);

            AssertFinite(design, "bounds");
            AssertCoreReadout(Readout.Quick(C, design), "bounds");
        }

        [Test]
        public void FixIt_EachWarningCombination_IsOneNumberedUndo()
        {
            var flags = new[] { "twr-low", "bad-mixture", "payload-doesnt-fit", "landing-hardware" };
            var cases = new List<string[]>();
            for (int i = 0; i < flags.Length; i++)
            {
                cases.Add(new[] { flags[i] });
                for (int j = i + 1; j < flags.Length; j++)
                    cases.Add(new[] { flags[i], flags[j] });
            }
            foreach (var combo in cases)
            {
                string name = string.Join("+", combo);
                var design = HangarFixtures.Template("workhorse");
                foreach (var flag in combo) Provoke(design, flag);
                var beforeCodes = Codes(design);
                foreach (var flag in combo)
                    Assert.That(beforeCodes, Does.Contain(flag), name + " had " + string.Join(",", beforeCodes));
                int beforeScore = Score(design);
                var session = new HangarSession(C, HangarFixtures.Data, design);
                string snap = HangarSession.Snapshot(session.Design);
                StepperFixResult result = null;
                var edit = session.Apply(d =>
                {
                    result = StepperFix.Apply(C, d, new StepCheck
                    {
                        Step = DesignerStep.Look,
                        Code = combo[0],
                        Gate = StepGate.Amber,
                    });
                    return result.Edit;
                });
                Assert.IsNotNull(result, name);
                AssertFinite(session.Design, name);
                if (!edit.changed)
                {
                    Assert.That(result.Instruction, Does.Match(@"\d"), name + " " + result.Instruction);
                    Assert.AreEqual(0, session.history.UndoCount, name);
                    Assert.AreEqual(snap, HangarSession.Snapshot(session.Design), name);
                    continue;
                }
                Assert.AreEqual(1, session.history.UndoCount, name + " " + result.Instruction);
                var step = session.history.NextUndo;
                Assert.IsFalse(string.IsNullOrEmpty(step.from), name + " " + step.Label);
                Assert.IsFalse(string.IsNullOrEmpty(step.to), name + " " + step.Label);
                Assert.AreNotEqual(step.from, step.to, name);
                Assert.That(step.Label, Does.Contain("→"), step.Label);
                if (HasDigit(step.from) || HasDigit(step.to))
                    Assert.That(step.Label, Does.Match(@"\d"), step.Label);
                var afterCodes = Codes(session.Design);
                bool cleared = true;
                foreach (var flag in combo)
                    if (afterCodes.Contains(flag)) cleared = false;
                int afterScore = Score(session.Design);
                Assert.IsTrue(cleared || afterScore < beforeScore, name + " " + result.Instruction + " still " + string.Join(",", afterCodes));
                if (!cleared)
                    Assert.That(result.Instruction ?? "", Does.Match(@"\d"), result.Instruction);
                session.Undo();
                Assert.AreEqual(snap, HangarSession.Snapshot(session.Design), name);
                Assert.AreEqual(0, session.history.UndoCount, name);
                session.Redo();
                Assert.AreNotEqual(snap, HangarSession.Snapshot(session.Design), name);
                AssertFinite(session.Design, name + " redo");
            }
        }

        [Test]
        public void UndoRedo_RestoresTheSameDocument()
        {
            var session = new HangarSession(C, HangarFixtures.Data, HangarFixtures.Template("heavy"));
            string first = HangarSession.Snapshot(session.Design);
            session.Apply(d => DesignerKnobs.NudgeMixture(d, 0, 1));
            session.Apply(d => HangarEditor.SetEngineCount(d, PartRef.Stage(0), (int)Stage0(d).Num("engineCount") + 1));
            string edited = HangarSession.Snapshot(session.Design);
            Assert.AreEqual(2, session.history.UndoCount);
            session.Undo();
            session.Undo();
            Assert.AreEqual(first, HangarSession.Snapshot(session.Design));
            session.Redo();
            session.Redo();
            Assert.AreEqual(edited, HangarSession.Snapshot(session.Design));
            AssertFinite(session.Design, "undo");
        }

        [Test]
        public void SaveAndLoad_RoundTripsTheDesign()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kh-designer-audit-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                long clock = 1;
                var lib = new DesignLibrary(new NativeStore(dir, () => clock++, () => 0.42));
                var session = new HangarSession(C, HangarFixtures.Data, HangarFixtures.Template("lander"));
                session.Apply(d => DesignerKnobs.SetMixtureOffset(d, 0, 4));
                session.Apply(d => DesignerKnobs.SetNose(d, "biconic"));
                var saved = session.SaveTo(lib, "Audit lander");
                Assert.IsTrue(saved.ok, saved.error);
                double mix = Stage0(session.Design).Num("mixtureOffsetPct");
                string nose = session.Design.Obj("aero").Str("nose");
                session.Apply(d => DesignerKnobs.NudgeMixture(d, 0, 1));
                Assert.AreNotEqual(mix, Stage0(session.Design).Num("mixtureOffsetPct"));
                session.Load(lib.Load(saved.id), saved.id, saved.name);
                Assert.AreEqual(mix, Stage0(session.Design).Num("mixtureOffsetPct"), 1e-6);
                Assert.AreEqual(nose, session.Design.Obj("aero").Str("nose"));
                Assert.AreEqual(0, session.history.UndoCount);
                AssertFinite(session.Design, "load");
                var shape = Validate.Shape(C, session.Design);
                Assert.IsFalse(Joined(shape.errors).Contains("aero.nose"), Joined(shape.errors));
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch (Exception) { }
            }
        }

        [Test]
        public void Fuzz_RandomEditsStayFiniteAndUndo()
        {
            var rng = new Random(FuzzSeed);
            var session = new HangarSession(C, HangarFixtures.Data, HangarFixtures.Template("workhorse"), 80);
            var ops = Ops(rng);
            for (int i = 0; i < FuzzEdits; i++)
            {
                int pick = rng.Next(ops.Count);
                string name = ops[pick].name;
                try { session.Apply(ops[pick].op); }
                catch (Exception e) { Assert.Fail("edit " + i + " " + name + " " + e); }
                AssertFinite(session.Design, "fuzz " + i + " " + name);
                if (i % 20 == 0)
                {
                    QuickReadout readout = null;
                    try { readout = Readout.Quick(C, session.Design); }
                    catch (Exception e) { Assert.Fail("readout " + i + " " + name + " " + e); }
                    AssertCoreReadout(readout, "fuzz " + i + " " + name);
                }
                if (i % 60 == 59)
                    RoundTripHistory(session);
            }
            RoundTripHistory(session);
        }

        static void RoundTripHistory(HangarSession session)
        {
            var seen = new List<string>();
            while (session.history.CanUndo)
            {
                seen.Add(HangarSession.Snapshot(session.Design));
                session.Undo();
                AssertFinite(session.Design, "undo");
            }
            for (int k = seen.Count - 1; k >= 0; k--)
            {
                Assert.IsTrue(session.history.CanRedo);
                session.Redo();
                Assert.AreEqual(seen[k], HangarSession.Snapshot(session.Design));
            }
        }

        List<(string name, Func<JsonObject, Edit> op)> Ops(Random rng)
        {
            var lim = DesignerLimits.Active;
            var list = new List<(string, Func<JsonObject, Edit>)>
            {
                ("mix+", d => DesignerKnobs.NudgeMixture(d, 0, 1)),
                ("mix-", d => DesignerKnobs.NudgeMixture(d, 0, -1)),
                ("mix-nan", d => DesignerKnobs.SetMixtureOffset(d, 0, double.NaN)),
                ("pc+", d => DesignerKnobs.NudgePc(C, d, 0, 1)),
                ("pc-", d => DesignerKnobs.NudgePc(C, d, 0, -1)),
                ("eps+", d => DesignerKnobs.NudgeExpansion(C, d, 0, 1)),
                ("eps-", d => DesignerKnobs.NudgeExpansion(C, d, 0, -1)),
                ("gimbal+", d => DesignerKnobs.NudgeGimbal(C, d, 0, 1)),
                ("gimbal-", d => DesignerKnobs.NudgeGimbal(C, d, 0, -1)),
                ("thr+", d => DesignerKnobs.NudgeThrottle(C, d, 0, 1)),
                ("thr-", d => DesignerKnobs.NudgeThrottle(C, d, 0, -1)),
                ("p+", d => DesignerKnobs.NudgePressure(d, 0, 1)),
                ("p-", d => DesignerKnobs.NudgePressure(d, 0, -1)),
                ("q+", d => MaxQThrottle.Nudge(d, 1)),
                ("q-", d => MaxQThrottle.Nudge(d, -1)),
                ("g+", d => DesignerKnobs.NudgeGuidance(d, "gLimit", 1)),
                ("g-", d => DesignerKnobs.NudgeGuidance(d, "gLimit", -1)),
                ("alt+", d => DesignerKnobs.NudgeGuidance(d, "pitchKickAlt", 1)),
                ("alt-", d => DesignerKnobs.NudgeGuidance(d, "pitchKickAlt", -1)),
                ("kick+", d => DesignerKnobs.NudgeGuidance(d, "pitchKickDeg", 1)),
                ("kick-", d => DesignerKnobs.NudgeGuidance(d, "pitchKickDeg", -1)),
                ("turn+", d => DesignerKnobs.NudgeGuidance(d, "turnShape", 1)),
                ("turn-", d => DesignerKnobs.NudgeGuidance(d, "turnShape", -1)),
                ("eng+", d => HangarEditor.SetEngineCount(d, PartRef.Stage(0), (int)Stage0(d).Num("engineCount") + 1)),
                ("eng-", d => HangarEditor.SetEngineCount(d, PartRef.Stage(0), (int)Stage0(d).Num("engineCount") - 1)),
                ("fuel+", d => HangarEditor.SetPropMass(d, PartRef.Stage(0), Stage0(d).Num("propMass") * 1.25)),
                ("fuel-", d => HangarEditor.SetPropMass(d, PartRef.Stage(0), Stage0(d).Num("propMass") * 0.8)),
                ("sf+", d => HangarEditor.SetSafetyFactor(d, d.Num("safetyFactor", 1.4) + lim.safetyStep)),
                ("sf-", d => HangarEditor.SetSafetyFactor(d, d.Num("safetyFactor", 1.4) - lim.safetyStep)),
                ("fair+", d => HangarEditor.SetFairingLength(d, (d.Obj("fairing")?.Num("length") ?? 1) + 0.5)),
                ("fair-", d => HangarEditor.SetFairingLength(d, (d.Obj("fairing")?.Num("length") ?? 1) - 0.5)),
                ("dia+", d => HangarEditor.SetDiameter(d, PartRef.Fairing, (d.Obj("fairing")?.Num("diameter") ?? 1) + 0.1)),
                ("dia-", d => HangarEditor.SetDiameter(d, PartRef.Fairing, (d.Obj("fairing")?.Num("diameter") ?? 1) - 0.1)),
                ("fins", d => HangarEditor.SetFins(d, !(d.Obj("aero")?.Bool("fins") ?? false))),
                ("fin+", d => HangarEditor.SetFinCount(d, (int)(d.Obj("aero")?.Num("finCount", 4) ?? 4) + 1)),
                ("fin-", d => HangarEditor.SetFinCount(d, (int)(d.Obj("aero")?.Num("finCount", 4) ?? 4) - 1)),
                ("span+", d => HangarEditor.SetFinSpan(d, (d.Obj("aero")?.Num("finSpanM", 1) ?? 1) + lim.finSpanStep)),
                ("span-", d => HangarEditor.SetFinSpan(d, (d.Obj("aero")?.Num("finSpanM", 1) ?? 1) - lim.finSpanStep)),
                ("legs", d => HangarEditor.SetLegs(d, !(d.Obj("landing")?.Bool("legs") ?? false))),
                ("pay+", d => HangarEditor.SetPayloadMass(d, (d.Obj("payload")?.Num("mass") ?? 100) + 250)),
                ("pay-", d => HangarEditor.SetPayloadMass(d, (d.Obj("payload")?.Num("mass") ?? 100) - 250)),
                ("reserve+", d => HangarEditor.SetLandingReserve(d, 0, Reserve(d) + 0.02)),
                ("reserve-", d => HangarEditor.SetLandingReserve(d, 0, Reserve(d) - 0.02)),
                ("stage+", d => HangarEditor.AddStage(C, d, d.Arr("stages")?.Count ?? 1)),
                ("stage-", d => HangarEditor.RemoveStage(d, (d.Arr("stages")?.Count ?? 1) - 1)),
            };
            foreach (var id in new[] { "kerolox", "methalox", "hydrolox" })
            {
                string captured = id;
                list.Add(("prop-" + captured, d => HangarEditor.SetPropellant(C, d, PartRef.Stage(0), captured)));
            }
            foreach (var nose in DesignerKnobs.Noses)
            {
                string captured = nose;
                list.Add(("nose-" + captured, d => DesignerKnobs.SetNose(d, captured)));
            }
            foreach (var pattern in lim.patterns)
            {
                string captured = pattern.id;
                list.Add(("cluster-" + captured, d => HangarEditor.SetClusterPattern(d, PartRef.Stage(0), captured)));
            }
            foreach (var n in lim.strapOnCounts)
            {
                int captured = n;
                list.Add(("boost-" + captured, d => HangarEditor.SetBoosterCount(C, d, captured)));
            }
            Assert.Greater(list.Count, 40);
            Assert.Greater(rng.Next(3), -1);
            return list;
        }

        void Provoke(JsonObject design, string flag)
        {
            switch (flag)
            {
                case "twr-low":
                    HangarEditor.SetEngineCount(design, PartRef.Stage(0), 1);
                    break;
                case "bad-mixture":
                    DesignerKnobs.SetMixtureOffset(design, 0, 9);
                    break;
                case "payload-doesnt-fit":
                    HangarEditor.SetPayloadType(C, design, "gto-comsat");
                    HangarEditor.SetFairingLength(design, 1);
                    break;
                case "landing-hardware":
                    var stage = Stage0(design);
                    JsonMut.Set(stage, "landingPropMass", Math.Max(1, stage.Num("propMass") * 0.08));
                    HangarEditor.SetLegs(design, false);
                    var landing = design.Obj("landing");
                    if (landing != null) JsonMut.Set(landing, "gridFins", false);
                    JsonMut.Set(design, "recoveryMode", "rtls");
                    var rec = design.Obj("recovery");
                    if (rec == null) { rec = new JsonObject(); JsonMut.Set(design, "recovery", rec); }
                    JsonMut.Set(rec, "mode", "rtls");
                    break;
            }
        }

        static void Pump(JsonObject design, int dir, Func<Edit> nudge, Func<double> read, double bound, double tol = 1e-3)
        {
            string label = bound.ToString("R") + " dir " + dir;
            for (int i = 0; i < 400; i++)
            {
                string before = HangarSession.Snapshot(design);
                var edit = nudge();
                double value = read();
                Assert.IsTrue(DesignerKnobs.Finite(value), label + " " + value);
                if (!edit.changed || HangarSession.Snapshot(design) == before)
                {
                    Assert.AreEqual(bound, value, tol, label + " value " + value);
                    string stuck = HangarSession.Snapshot(design);
                    Assert.IsFalse(nudge().changed, label);
                    Assert.AreEqual(stuck, HangarSession.Snapshot(design), label);
                    return;
                }
            }
            Assert.Fail("did not reach " + label + " at " + read());
        }

        double ExpansionMax(JsonObject design)
        {
            C.EngineById.TryGetValue(Stage0(design).Str("engine") ?? "", out var engine);
            var env = Kestrel.Core.Vehicle.EngineModel.EnvelopeFor(engine);
            return env.epsMax > 0 ? env.epsMax : 120;
        }

        double ExpansionMin(JsonObject design)
        {
            C.EngineById.TryGetValue(Stage0(design).Str("engine") ?? "", out var engine);
            var env = Kestrel.Core.Vehicle.EngineModel.EnvelopeFor(engine);
            return env.epsMin > 0 ? env.epsMin : 8;
        }

        double GimbalMax(JsonObject design)
        {
            C.EngineById.TryGetValue(Stage0(design).Str("engine") ?? "", out var engine);
            return engine != null && engine.gimbal > 0 ? engine.gimbal : 0;
        }

        static double Reserve(JsonObject design)
        {
            var stage = Stage0(design);
            double prop = stage?.Num("propMass") ?? 0;
            if (!(prop > 0)) return 0;
            return stage.Num("landingPropMass") / prop;
        }

        int Score(JsonObject design)
        {
            var report = StepperModel.Evaluate(C, design);
            int score = 0;
            foreach (var def in StepperCatalog.All)
            {
                var check = report[def.Step];
                if (check == null) continue;
                if (check.Gate == StepGate.Red) score += 100;
                else if (check.Gate == StepGate.Amber) score += 10;
                for (int i = 0; i < check.Findings.Count; i++)
                {
                    var finding = check.Findings[i];
                    if (finding == null) continue;
                    if (finding.Level == "red") score += 5;
                    else if (finding.Level == "amber") score += 1;
                }
            }
            return score;
        }

        List<string> Codes(JsonObject design)
        {
            var list = new List<string>();
            var report = StepperModel.Evaluate(C, design);
            foreach (var def in StepperCatalog.All)
            {
                var check = report[def.Step];
                if (check == null) continue;
                if (!string.IsNullOrEmpty(check.Code) && check.Gate != StepGate.Pass && check.Gate != StepGate.Skip)
                    list.Add(check.Code);
                for (int i = 0; i < check.Findings.Count; i++)
                    if (check.Findings[i] != null && !string.IsNullOrEmpty(check.Findings[i].Code))
                        list.Add(check.Findings[i].Code);
            }
            if (report.Advisories != null)
                foreach (var advisory in report.Advisories)
                    if (advisory != null && advisory.severity != AdvisorySeverity.Note && !string.IsNullOrEmpty(advisory.code))
                        list.Add(advisory.code);
            return list;
        }

        static JsonObject Stage0(JsonObject design) => design.Arr("stages")[0] as JsonObject;

        static void AssertCoreReadout(QuickReadout readout, string what)
        {
            Assert.IsNotNull(readout, what);
            Assert.IsTrue(DesignerKnobs.Finite(readout.liftoffTWR), what + " twr");
            Assert.IsTrue(DesignerKnobs.Finite(readout.totalDeltaV), what + " dv");
            Assert.IsTrue(DesignerKnobs.Finite(readout.margin), what + " margin");
            Assert.IsTrue(DesignerKnobs.Finite(readout.required), what + " required");
            Assert.IsTrue(DesignerKnobs.Finite(readout.liftoffMass), what + " mass");
            Assert.IsTrue(DesignerKnobs.Finite(readout.reliability), what + " rel");
            Assert.IsTrue(DesignerKnobs.Finite(readout.stabilityCal), what + " stab");
            Assert.IsTrue(DesignerKnobs.Finite(readout.gLimit), what + " g");
        }

        static void AssertFinite(object node, string path)
        {
            if (node == null || node is string || node is bool) return;
            if (node is double number)
            {
                if (double.IsNaN(number) || double.IsInfinity(number))
                    Assert.Fail(path + " is " + number);
                return;
            }
            if (node is JsonObject obj)
            {
                foreach (var key in obj.Order)
                    AssertFinite(obj[key], path + "." + key);
                return;
            }
            if (node is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                    AssertFinite(list[i], path + "[" + i + "]");
            }
        }

        static bool HasDigit(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (int i = 0; i < text.Length; i++)
                if (text[i] >= '0' && text[i] <= '9') return true;
            return false;
        }

        static string Joined(List<string> errors) => errors == null ? "" : string.Join("; ", errors);
    }
}
