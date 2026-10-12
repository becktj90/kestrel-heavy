// EditMode: the pad drives connectors from the contract timeline (retract before T-0, tail mast at liftoff).
using System.Collections.Generic;
using Kestrel.Kit.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Pad.Tests
{
    public class UmbilicalRuntimeTests
    {
        GameObject _root;
        [TearDown] public void TearDown() { if (_root != null) Object.DestroyImmediate(_root); }

        static AttachPoint Plate(int stage, UmbilicalType t, ReleaseStyle r, float y) => new AttachPoint
        {
            Id = UmbilicalIds.Make(stage, t, 0), Type = t, Stage = stage, Release = r, PlateRadius = 0.3f,
            Pose = new LocalPose(new Vec3(0, y, 2.5f), ContractMath.LookRotation(new Vec3(0, 0, 1), new Vec3(0, 1, 0))),
        };

        [Test]
        public void SequenceRetractsSwingArmsBeforeLiftoffAndDropsTheTailMast()
        {
            var pts = new List<AttachPoint>
            {
                Plate(1, UmbilicalType.Fuel, ReleaseStyle.SwingArmRetract, 36f),
                Plate(0, UmbilicalType.Oxidizer, ReleaseStyle.TailServiceMastDrop, 4f),
            };
            _root = new GameObject("pad");
            var r = PadBuilder.Build(_root.transform, 5, PadArchetype.Tower, new PadFit(70f, 2.5f), pts);
            Assert.That(r.Umbilicals.Count, Is.EqualTo(2));
            Assert.That(r.CrewYaw, Is.EqualTo(0f));

            var fuel = r.Arms.Find(a => a.AttachId == "s1.fuel.0");
            var tail = r.Umbilicals.Find(u => u.AttachId == "s0.lox.0");
            Assert.That(tail.IsConnector, Is.True);
            Assert.That(fuel.State, Is.EqualTo(UmbilicalState.Connected));

            r.AdvanceTo(-160);
            Assert.That(fuel.State, Is.EqualTo(UmbilicalState.Flowing));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(fuel.CurrentYaw, fuel.MatedYaw)), Is.LessThan(1f));
            Assert.That(tail.State, Is.Not.EqualTo(UmbilicalState.Retracted));

            r.AdvanceTo(-100);
            Assert.That(fuel.State, Is.EqualTo(UmbilicalState.Retracted));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(fuel.CurrentYaw, fuel.MatedYaw + fuel.Spec.RetractYawDeg)), Is.LessThan(1f));
            var fuelVis = r.Umbilicals.Find(u => u.AttachId == fuel.AttachId);
            Assert.That(fuelVis.Retract, Is.EqualTo(1f).Within(1e-3f));
            Assert.That(tail.State, Is.EqualTo(UmbilicalState.Flowing).Or.EqualTo(UmbilicalState.Connected));
            Assert.That(tail.Pitch, Is.EqualTo(0f).Within(1e-3f));

            r.AdvanceTo(0);
            Assert.That(tail.State, Is.EqualTo(UmbilicalState.Released));
            Assert.That(tail.Pitch, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(r.CrewYaw, Is.EqualTo(ServiceArmMotion.AccessRetractYaw).Within(0.05f));

            r.AdvanceTo(2);
            Assert.That(tail.State, Is.EqualTo(UmbilicalState.Retracted));
            Assert.That(tail.Pitch, Is.EqualTo(tail.Link.DropDeg).Within(0.05f));
        }
    }
}
