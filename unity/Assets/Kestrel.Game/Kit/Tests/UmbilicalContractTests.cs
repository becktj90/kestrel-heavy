using System.Collections.Generic;
using Kestrel.Kit.Contracts;
using NUnit.Framework;

namespace Kestrel.Kit.Tests
{
    public class UmbilicalContractTests
    {
        static List<AttachPoint> TwoStage() => new List<AttachPoint>
        {
            P(0, UmbilicalType.Fuel, ReleaseStyle.TailServiceMastDrop), P(0, UmbilicalType.Oxidizer, ReleaseStyle.TailServiceMastDrop),
            P(1, UmbilicalType.Fuel, ReleaseStyle.SwingArmRetract), P(1, UmbilicalType.Oxidizer, ReleaseStyle.SwingArmRetract),
            P(1, UmbilicalType.PowerData, ReleaseStyle.LanyardPull), P(1, UmbilicalType.Purge, ReleaseStyle.Hinged),
        };
        static AttachPoint P(int s, UmbilicalType t, ReleaseStyle r) => new AttachPoint
        { Id = UmbilicalIds.Make(s, t, 0), Type = t, Stage = s, Release = r, PlateRadius = 0.4f, Pose = new LocalPose(new Vec3(0, 10 + 30 * s, 1.8f), Quat.Identity) };

        [Test] public void IdsFollowScheme() => Assert.That(UmbilicalIds.Make(1, UmbilicalType.Oxidizer, 2), Is.EqualTo("s1.lox.2"));

        [Test]
        public void DefaultTimelineIsValidAndEverythingReleasesByLiftoff()
        {
            var pts = TwoStage(); var ev = UmbilicalTimeline.Build(pts);
            Assert.That(UmbilicalTimeline.Validate(pts, ev), Is.Empty);
            foreach (var e in ev) if (e.Kind == UmbilicalEventKind.Disconnect) Assert.That(e.T, Is.LessThanOrEqualTo(0.1));
            for (int i = 1; i < ev.Count; i++) Assert.That(ev[i].T, Is.GreaterThanOrEqualTo(ev[i - 1].T));
        }

        [Test]
        public void SwingArmsReleaseBeforeTailMast()
        {
            Assert.That(UmbilicalTimeline.DefaultDisconnectT(UmbilicalType.Oxidizer, ReleaseStyle.SwingArmRetract),
                Is.LessThan(UmbilicalTimeline.DefaultDisconnectT(UmbilicalType.Oxidizer, ReleaseStyle.TailServiceMastDrop)));
        }

        [Test]
        public void ValidateCatchesDuplicatesLateReleaseAndMissingDisconnect()
        {
            var pts = TwoStage(); pts.Add(P(0, UmbilicalType.Fuel, ReleaseStyle.Hinged));
            Assert.That(UmbilicalTimeline.Validate(pts, UmbilicalTimeline.Build(pts)), Has.Some.Contains("duplicate"));

            pts = TwoStage(); var ev = UmbilicalTimeline.Build(pts);
            ev.RemoveAll(e => e.AttachId == "s1.pwr.0" && e.Kind == UmbilicalEventKind.Disconnect);
            Assert.That(UmbilicalTimeline.Validate(pts, ev), Has.Some.Contains("never disconnects"));

            ev = new List<UmbilicalEvent> { new UmbilicalEvent("s0.fuel.0", UmbilicalEventKind.Connect, -10), new UmbilicalEvent("s0.fuel.0", UmbilicalEventKind.Disconnect, 2) };
            Assert.That(UmbilicalTimeline.Validate(new[] { pts[0] }, ev), Has.Some.Contains("after liftoff"));
        }

        [Test]
        public void StateMachineHandlesHoldAndRecycle()
        {
            var s = UmbilicalState.Detached;
            foreach (var k in new[] { UmbilicalEventKind.Connect, UmbilicalEventKind.FlowStart, UmbilicalEventKind.Disconnect, UmbilicalEventKind.Abort })
                s = UmbilicalStates.Apply(s, k);
            Assert.That(s, Is.EqualTo(UmbilicalState.Connected), "abort before retract re-mates");
            s = UmbilicalStates.Apply(UmbilicalStates.Apply(UmbilicalStates.Apply(s, UmbilicalEventKind.Disconnect), UmbilicalEventKind.ArmRetractDone), UmbilicalEventKind.Abort);
            Assert.That(s, Is.EqualTo(UmbilicalState.Retracted), "retracted arms stay retracted");
            Assert.That(UmbilicalStates.Apply(UmbilicalState.Detached, UmbilicalEventKind.FlowStart), Is.EqualTo(UmbilicalState.Detached));
        }
    }
}
