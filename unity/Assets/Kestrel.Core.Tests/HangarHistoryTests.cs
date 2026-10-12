// Undo/redo: exact JSON restore, gesture coalescing, the 50-step cap, redo invalidation, and labels.
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class HangarHistoryTests
    {
        static HangarSession NewSession(string template = "workhorse")
            => new HangarSession(Golden.Catalog, HangarFixtures.Data, HangarFixtures.Template(template));

        [Test]
        public void Undo_RestoresTheExactJson_AndRedoReplays()
        {
            var s = NewSession();
            var snaps = new List<string> { HangarSession.Snapshot(s.Design) };
            s.Apply(d => HangarEditor.SetEngineCount(d, PartRef.Stage(0), 9)); snaps.Add(HangarSession.Snapshot(s.Design));
            s.Apply(d => HangarEditor.AddStage(Golden.Catalog, d, 2)); snaps.Add(HangarSession.Snapshot(s.Design));
            s.Apply(d => HangarEditor.MoveStage(d, 2, 0)); snaps.Add(HangarSession.Snapshot(s.Design));
            s.Apply(d => HangarEditor.SetPaint(d, "#123456", "#ABCDEF")); snaps.Add(HangarSession.Snapshot(s.Design));
            s.Apply(d => HangarEditor.RemoveStage(d, 1)); snaps.Add(HangarSession.Snapshot(s.Design));
            Assert.AreEqual(5, s.history.UndoCount);
            for (int i = snaps.Count - 2; i >= 0; i--)
            {
                Assert.IsNotNull(s.Undo());
                Assert.AreEqual(snaps[i], HangarSession.Snapshot(s.Design), $"undo to {i}");
            }
            Assert.IsNull(s.Undo(), "nothing left");
            for (int i = 1; i < snaps.Count; i++)
            {
                Assert.IsNotNull(s.Redo());
                Assert.AreEqual(snaps[i], HangarSession.Snapshot(s.Design), $"redo to {i}");
            }
            Assert.IsNull(s.Redo());
        }

        [Test]
        public void Labels_SayWhatChangedFromAndTo()
        {
            var s = NewSession();
            s.Apply(d => HangarEditor.SetEngineCount(d, PartRef.Stage(0), 8));
            Assert.AreEqual("Stage 1 engines 7 → 8", s.history.NextUndo.Label);
            var step = s.Undo();
            Assert.AreEqual("Stage 1 engines 7 to 8", step.Spoken);
            Assert.AreEqual("Stage 1 engines 7 → 8", s.history.NextRedo.Label);
            s.Apply(d => HangarEditor.AddStage(Golden.Catalog, d, 2));
            Assert.AreEqual("Added stage 3", s.history.NextUndo.Label);
        }

        [Test]
        public void Gesture_CoalescesIntoOneStep_KeepingTheFirstFromValue()
        {
            var s = NewSession();
            string start = HangarSession.Snapshot(s.Design);
            for (int n = 8; n <= 12; n++) s.Apply(d => HangarEditor.SetEngineCount(d, PartRef.Stage(0), n), "s0.engines");
            s.EndGesture();
            Assert.AreEqual(1, s.history.UndoCount);
            Assert.AreEqual("Stage 1 engines 7 → 12", s.history.NextUndo.Label);
            s.Apply(d => HangarEditor.SetEngineCount(d, PartRef.Stage(0), 13), "s0.engines");
            Assert.AreEqual(2, s.history.UndoCount, "a sealed gesture starts a new step");
            s.Undo(); s.Undo();
            Assert.AreEqual(start, HangarSession.Snapshot(s.Design));
        }

        [Test]
        public void Gesture_ThatReturnsToItsStart_LeavesNothingToUndo()
        {
            var s = NewSession();
            double kg = ((JsonObject)s.Design.Arr("stages")[0]).Num("propMass");
            s.Apply(d => HangarEditor.SetPropMass(d, PartRef.Stage(0), kg + 5000), "drag");
            s.Apply(d => HangarEditor.SetPropMass(d, PartRef.Stage(0), kg), "drag");
            s.EndGesture();
            Assert.AreEqual(0, s.history.UndoCount);
        }

        [Test]
        public void DifferentKeys_DoNotCoalesce()
        {
            var s = NewSession();
            s.Apply(d => HangarEditor.SetEngineCount(d, PartRef.Stage(0), 8), "a");
            s.Apply(d => HangarEditor.SetPropMass(d, PartRef.Stage(0), 250000), "b");
            Assert.AreEqual(2, s.history.UndoCount);
        }

        [Test]
        public void NewEdit_ClearsRedo()
        {
            var s = NewSession();
            s.Apply(d => HangarEditor.SetEngineCount(d, PartRef.Stage(0), 8));
            s.Undo();
            Assert.IsTrue(s.history.CanRedo);
            s.Apply(d => HangarEditor.SetLegs(d, true));
            Assert.IsFalse(s.history.CanRedo);
        }

        [Test]
        public void History_KeepsTheLatestFiftySteps()
        {
            var s = NewSession();
            for (int i = 0; i < 60; i++) s.Apply(d => HangarEditor.SetPropMass(d, PartRef.Stage(0), 100000 + i * 1000));
            Assert.AreEqual(EditHistory.DefaultCapacity, s.history.UndoCount);
            int undone = 0;
            while (s.Undo() != null) undone++;
            Assert.AreEqual(50, undone);
            Assert.AreEqual(109000, ((JsonObject)s.Design.Arr("stages")[0]).Num("propMass"), "the oldest 10 steps are gone; undo stops at step 10");
        }

        [Test]
        public void NoOpEdits_RecordNothing_AndDoNotNotify()
        {
            var s = NewSession();
            int changed = 0;
            s.Changed += () => changed++;
            s.Apply(d => HangarEditor.SetEngineCount(d, PartRef.Stage(0), 7));
            s.Apply(d => HangarEditor.SetNose(d, "ogive"));
            Assert.AreEqual(0, s.history.UndoCount);
            Assert.AreEqual(0, changed);
            s.Apply(d => HangarEditor.SetNose(d, "blunt"));
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void ThrowingOp_LeavesTheDesignUntouched()
        {
            var s = NewSession();
            string before = HangarSession.Snapshot(s.Design);
            Assert.Throws<System.InvalidOperationException>(() => s.Apply(d =>
            {
                JsonMut.Set(d, "name", "half-done");
                throw new System.InvalidOperationException("boom");
            }));
            Assert.AreEqual(before, HangarSession.Snapshot(s.Design));
            Assert.AreEqual(0, s.history.UndoCount);
        }

        [Test]
        public void Selection_FollowsRemovedStages()
        {
            var s = NewSession("moonshot");
            s.Select(PartRef.Stage(2));
            Assert.AreEqual(PartRef.Stage(2), s.Selected);
            s.Apply(d => HangarEditor.RemoveStage(d, 2));
            Assert.AreEqual(PartRef.Stage(1), s.Selected, "selection clamps to the new top stage");
            s.Select(PartRef.Boosters);
            Assert.AreEqual(PartRef.Stage(1), s.Selected, "no boosters to select");
            s.Undo();
            s.Select(PartRef.Interstage(2));
            Assert.AreEqual(PartRef.Interstage(2), s.Selected);
        }

        [Test]
        public void MakeItFly_IsOneUndoableStep()
        {
            var s = NewSession();
            s.Apply(d => HangarEditor.SetEngineCount(d, PartRef.Stage(0), 1));
            Assert.IsFalse(s.LaunchEnabled);
            string broken = HangarSession.Snapshot(s.Design);
            var changes = s.MakeItFly();
            Assert.IsNotEmpty(changes);
            Assert.IsTrue(s.LaunchEnabled, string.Join(", ", s.Report.blocks.ConvertAll(b => b.code)));
            Assert.AreEqual("Make it fly", s.history.NextUndo.Label);
            s.Undo();
            Assert.AreEqual(broken, HangarSession.Snapshot(s.Design));
        }
    }
}
