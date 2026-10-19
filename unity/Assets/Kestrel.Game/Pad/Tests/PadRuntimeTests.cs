using Kestrel.Kit;
using Kestrel.Kit.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Pad.Tests
{
    public class PadRuntimeTests
    {
        GameObject _root;
        [TearDown] public void TearDown() { if (_root != null) Object.DestroyImmediate(_root); MaterialLibrary.ClearCache(); }

        [Test]
        public void BuildInstancesEveryMemberInFewBatches()
        {
            _root = new GameObject("pad");
            var r = PadBuilder.Build(_root.transform, 11, PadArchetype.TowerStrongback, new PadFit(70f, 2.5f));
            int n = 0; foreach (var b in r.Batches.Values) n += b.Count;
            Assert.That(n, Is.EqualTo(r.Layout.Members.Count));
            Assert.That(r.Batches.Count, Is.LessThanOrEqualTo(9));   // D2: + crew arm, handrail, galvanized masts, insulator spikes
            Assert.That(_root.GetComponentsInChildren<MeshRenderer>(), Is.Empty, "no GameObject per member");
        }

        [Test]
        public void ArmsImplementContractAndFollowStateMachine()
        {
            _root = new GameObject("pad");
            var fit = new PadFit(70f, 2.5f);
            var r = PadBuilder.Build(_root.transform, 2, PadArchetype.Tower, fit);
            Assert.That(r.Arms.Count, Is.EqualTo(3));
            IUmbilicalConnector arm = r.Arms[0];
            Assert.That(arm.State, Is.EqualTo(UmbilicalState.Connected));
            Assert.That(r.Arms[0].CurrentYaw, Is.EqualTo(180f).Within(1f).Or.EqualTo(-180f).Within(1f));
            Assert.That(r.Arms[0].Length, Is.EqualTo(fit.Clearance).Within(0.5f));
            arm.OnUmbilicalEvent(new UmbilicalEvent(arm.AttachId, UmbilicalEventKind.FlowStart, -2000));
            Assert.That(arm.State, Is.EqualTo(UmbilicalState.Flowing));
            arm.OnUmbilicalEvent(new UmbilicalEvent(arm.AttachId, UmbilicalEventKind.Disconnect, -120));
            arm.OnUmbilicalEvent(new UmbilicalEvent(arm.AttachId, UmbilicalEventKind.ArmRetractStart, -119.8));
            r.Tick(-119.8 + 4); float mid = r.Arms[0].CurrentYaw;
            r.Tick(-119.8 + 20);
            arm.OnUmbilicalEvent(new UmbilicalEvent(arm.AttachId, UmbilicalEventKind.ArmRetractDone, -111.8));
            Assert.That(arm.State, Is.EqualTo(UmbilicalState.Retracted));
            float done = r.Arms[0].CurrentYaw, mated = r.Arms[0].MatedYaw;
            Assert.That(Mathf.Abs(done - mated), Is.EqualTo(Mathf.Abs(r.Arms[0].Spec.RetractYawDeg)).Within(0.01f));
            Assert.That(Mathf.Abs(mid - mated), Is.EqualTo(Mathf.Abs(r.Arms[0].Spec.RetractYawDeg) * 0.5f).Within(0.5f));
            // events for other ids are ignored
            arm.OnUmbilicalEvent(new UmbilicalEvent("nope", UmbilicalEventKind.Abort, 0));
            Assert.That(arm.State, Is.EqualTo(UmbilicalState.Retracted));
        }

        [Test]
        public void ConnectRejectsUnreachablePlate()
        {
            _root = new GameObject("pad");
            var r = PadBuilder.Build(_root.transform, 2, PadArchetype.Tower, new PadFit(70f, 2.5f));
            var arm = r.Arms[0];
            var far = new AttachPoint { Id = arm.AttachId, Type = arm.Type, Pose = new LocalPose(new Vec3(0, 0, -40), Quat.Identity) };
            Assert.That(arm.Connect(far, new LocalPose(new Vec3(0, arm.Spec.Pivot.Y, 0), Quat.Identity)), Is.False);
            Assert.That(arm.LastError, Does.Contain("reach"));
        }

        [Test]
        public void RebuildDoesNotLeakArms()
        {
            _root = new GameObject("pad");
            PadBuilder.Build(_root.transform, 1, PadArchetype.Tower, new PadFit(70f, 2.5f));
            var r = PadBuilder.Build(_root.transform, 1, PadArchetype.Rail, new PadFit(20f, 0.7f));
            Assert.That(r.Arms, Is.Empty);
            Assert.That(_root.GetComponentsInChildren<SwingArm>(), Is.Empty);
            // only D5 realtime lights and the ignition anchor may remain as children
            var lights = _root.GetComponent<PadLights>();
            int anchor = _root.transform.Find(PadVfxState.AnchorName) != null ? 1 : 0;
            Assert.That(_root.transform.childCount, Is.EqualTo(lights.Realtime.Count + anchor));
            Assert.That(lights.Realtime.Count, Is.LessThanOrEqualTo(KitBudgets.PadRealtimeLights(r.Quality)));
        }
    }
}
