// §3 formulas: weakest joint, tumble rate, plume cut, leg fold, pooled particles, no shake under Reduce Motion.
using System;
using System.Collections.Generic;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using Kestrel.Game.Failure;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Tests
{
    public class FailureFxTests
    {
        [Test]
        public void RingDropsTheOldestSample()
        {
            var log = new TelemetryLog(3);
            log.Push(1, 0, 0, 0, 0, 0, 0, 0, 0);
            log.Push(2, 0, 0, 0, 0, 0, 0, 0, 0);
            log.Push(3, 0, 0, 0, 0, 0, 0, 0, 0);
            log.Push(4, 10, 20, 30, 2, 40, 1, 2, 3);
            Assert.That(log.Count, Is.EqualTo(3));
            Assert.That(log[0].T, Is.EqualTo(2));
            Assert.That(log[2].T, Is.EqualTo(4));
            Assert.That(log[2].Alt, Is.EqualTo(10));
            log.StampLatestLosses(9, 8, 7);
            Assert.That(log[2].LossGravity, Is.EqualTo(9));
            Assert.That(log[2].LossTotal, Is.EqualTo(24));
            int before = log.Count;
            log.Push(double.NaN, 0, 0, 0, 0, 0, 0, 0, 0);
            Assert.That(log.Count, Is.EqualTo(before));
        }

        [Test]
        public void LossesMatchTheAscentAccount()
        {
            double g = 0, d = 0, s = 0;
            LossAccount.Add(ref g, ref d, ref s, 9.8, 100, 200, 30, 0.9, 2, 0.5);
            var log = new TelemetryLog(4);
            log.Push(1, 10, 200, 1000, 2, 1000, g, d, s);
            Assert.That(log[0].LossGravity, Is.EqualTo(g).Within(1e-12));
            Assert.That(log[0].LossDrag, Is.EqualTo(d).Within(1e-12));
            Assert.That(log[0].LossSteering, Is.EqualTo(s).Within(1e-12));
            log.Mark(1, "failure", true);
            Assert.That(log.TryFailure(out var mark), Is.True);
            Assert.That(mark.Kind, Is.EqualTo("failure"));
        }

        [Test]
        public void MarginIsAllowableOverLoadMinusOne()
        {
            const float q = 40000f, r = 2f, allow = 400000f;
            double load = q * Math.PI * r * r;
            Assert.That(FailureFx.Margin(allow, q, r), Is.EqualTo(allow / load - 1.0).Within(1e-9));
            var joints = new[]
            {
                new FailureJoint { Y = 4f, Radius = 2f, Source = "interstage" },
                new FailureJoint { Y = 12f, Radius = 1f, Source = "interstage" },
            };
            Assert.That(FailureFx.Weakest(joints, q, allow), Is.EqualTo(0), "the wider joint is weaker");
            Assert.That(FailureFx.Margin(allow, q, 2f), Is.LessThan(0));
            Assert.That(FailureFx.Margin(allow, q, 1f), Is.GreaterThan(0));
        }

        [Test]
        public void JointsComeFromTheAirframeAndTheFairingBase()
        {
            var list = new List<FailureJoint>();
            FailureFx.AddInterstage(list, 10f, 14f, 1.8f, 1.6f);
            Assert.That(list.Count, Is.EqualTo(2), "two joint rings, stringers are not break points");
            Assert.That(list[0].Y, Is.LessThan(list[1].Y));
            Assert.That(list[0].Radius, Is.GreaterThan(list[1].Radius));
            Assert.That(list[0].Source, Is.EqualTo("interstage"));
            FailureFx.AddFairing(list, 20f, 1.5f, 8f, 0.3f, false);
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[2].Source, Is.EqualTo("fairing"));
        }

        [Test]
        public void BreakPicksTheJointTheCauseNames()
        {
            var joints = new[]
            {
                new FailureJoint { Y = 4f, Radius = 2f, Source = "interstage" },
                new FailureJoint { Y = 12f, Radius = 1.5f, Source = "interstage" },
                new FailureJoint { Y = 30f, Radius = 1.1f, Source = "fairing" },
            };
            var cue = new FailureCue { Joints = joints, QPa = 40000f, AllowableN = 400000f };
            var q = FailureFx.Plan("structural-maxq", "q", cue, 0.5f, false);
            var hot = FailureFx.Plan("structural-maxq", "hot-staging", cue, 0.5f, false);
            var bulk = FailureFx.Plan("structural-maxq", "common-bulkhead", cue, 0.5f, false);
            var over = FailureFx.Plan("overg", "", cue, 0.5f, false);
            Assert.That(q.JointIndex, Is.EqualTo(0));
            Assert.That(joints[hot.JointIndex].Source, Is.EqualTo("interstage"));
            Assert.That(hot.JointIndex, Is.EqualTo(1));
            Assert.That(bulk.JointIndex, Is.EqualTo(0));
            Assert.That(over.JointIndex, Is.EqualTo(2));
            Assert.That(over.Mode, Is.EqualTo(FailureFx.BreakMode));
            Assert.That(q.Gap, Is.GreaterThan(12f), "a structural break opens fast");
            Assert.That(q.ParticleSpeed, Is.GreaterThan(10f));
            Assert.That(q.AngleDeg, Is.GreaterThan(40f));
            var shard = FailureFx.Shard(0, 8, q.Gap, q.JointY, 0.5f);
            Assert.That(Math.Abs(shard.X) + Math.Abs(shard.Z), Is.GreaterThan(1f));
        }

        [Test]
        public void TumbleSpinsThenBreaksAndSnapsWhenMotionIsReduced()
        {
            double w = FailureFx.SpinRadPerSec(FailureModel.TumbleQPa, false);
            Assert.That(w, Is.EqualTo(Math.PI / FailureModel.TumbleHoldS).Within(1e-9));
            Assert.That(FailureFx.SpinRadPerSec(1, true), Is.EqualTo(0));
            var cue = new FailureCue { QPa = (float)FailureModel.TumbleQPa, Joints = new[] { new FailureJoint { Y = 8f, Radius = 1.5f, Source = "interstage" } } };
            var early = FailureFx.Plan("tumble", "alpha", cue, 0.4f, false);
            var late = FailureFx.Plan("tumble", "alpha", cue, 0.8f, false);
            var still = FailureFx.Plan("tumble", "stability", cue, 0.2f, true);
            Assert.That(early.Gap, Is.EqualTo(0f));
            Assert.That(early.AngleDeg, Is.GreaterThan(20f));
            Assert.That(late.Gap, Is.GreaterThan(0f));
            Assert.That(still.SpinRadPerSec, Is.EqualTo(0));
            Assert.That(still.AngleDeg, Is.EqualTo(FailureFx.SnapDeg));
            Assert.That(still.Shake, Is.EqualTo(0f));
            Assert.That(still.ParticleSpeed, Is.EqualTo(0f));
            Assert.That(FailureFx.ShakeMeters(0.12f, false), Is.Not.EqualTo(0f));
            Assert.That(FailureFx.ShakeMeters(0.12f, true), Is.EqualTo(0f));
        }

        [Test]
        public void PlumeCutFollowsTheBellIndex()
        {
            Assert.That(FailureFx.PlumeScale(FailureFx.BellMode, 9), Is.EqualTo(8f / 9f).Within(1e-6));
            Assert.That(FailureFx.PlumeScale(FailureFx.BellMode, 1), Is.EqualTo(0f));
            Assert.That(FailureFx.PlumeScale(FailureFx.SputterMode, 9), Is.EqualTo(0.45f));
            Assert.That(FailureFx.PlumeScale(FailureFx.FlameoutMode, 9), Is.EqualTo(0f));
            var cue = new FailureCue { EngineCount = 9, EngineIndex = 4 };
            var flash = FailureFx.Plan("engine-overpressure", "pc-cap", cue, 0.2f, false);
            var cut = FailureFx.Plan("engine-out", "engine-out", cue, 0.2f, false);
            var calm = FailureFx.Plan("engine-overpressure", "hazard", cue, 0.2f, true);
            var green = FailureFx.Plan("bad-mixture", "bad-mixture", cue, 0.2f, false);
            Assert.That(flash.EngineIndex, Is.EqualTo(4));
            Assert.That(flash.Flash, Is.True);
            Assert.That(flash.Mode, Is.EqualTo(FailureFx.BellMode));
            Assert.That(cut.EngineIndex, Is.EqualTo(4));
            Assert.That(cut.PlumeScale, Is.EqualTo(8f / 9f).Within(1e-6));
            Assert.That(calm.Flash, Is.False);
            Assert.That(calm.Tint, Is.EqualTo("dim"));
            Assert.That(green.Tint, Is.EqualTo("green"));
            Assert.That(green.Flash, Is.False);
            Assert.That(FailureFx.Plan("engine-out", "", new FailureCue { EngineCount = 3, EngineIndex = -1 }, 0f, false).EngineIndex, Is.EqualTo(0));
        }

        [Test]
        public void LegFoldUsesTheLandingStations()
        {
            const float radius = 2f;
            var planted = FailureFx.Plan("leg-collapse", "leg-collapse", new FailureCue { Radius = radius, AftY = 3f, Sink = 3f, VLeg = 6f }, 1f, false);
            Assert.That(planted.Fold01, Is.EqualTo(0f));
            Assert.That(planted.TipDeg, Is.EqualTo(0f));
            Assert.That(planted.FootRadius, Is.EqualTo(radius * 1.62f).Within(0.02f));
            var folded = FailureFx.Plan("leg-collapse", "leg-collapse", new FailureCue { Radius = radius, AftY = 3f, Sink = 12f, VLeg = 6f }, 1f, false);
            Assert.That(FailureFx.FoldAmount(12f, 6f), Is.EqualTo(1f));
            Assert.That(folded.Fold01, Is.EqualTo(1f));
            Assert.That(folded.TipDeg, Is.EqualTo(FailureFx.TipOverDeg));
            Assert.That(folded.FootRadius, Is.EqualTo(radius * 0.98f).Within(0.02f));
            Assert.That(folded.Mode, Is.EqualTo(FailureFx.FoldMode));
            var half = FailureFx.Plan("leg-collapse", "", new FailureCue { Radius = radius, AftY = 3f, Sink = 9f, VLeg = 6f }, 0f, false);
            Assert.That(half.Fold01, Is.EqualTo(0f), "the fold eases in unless Reduce Motion snaps it");
            var snap = FailureFx.Plan("leg-collapse", "", new FailureCue { Radius = radius, AftY = 3f, Sink = 9f, VLeg = 6f }, 0f, true);
            Assert.That(snap.Fold01, Is.EqualTo(0.5f).Within(1e-4));
        }

        [Test]
        public void PoolReusesTheOldestSlot()
        {
            var pool = new FailurePool();
            for (int i = 0; i < FailurePool.Capacity; i++)
                Assert.That(pool.Acquire(), Is.EqualTo(i));
            Assert.That(pool.Live, Is.EqualTo(FailurePool.Capacity));
            int again = pool.Acquire();
            Assert.That(again, Is.GreaterThanOrEqualTo(0).And.LessThan(FailurePool.Capacity));
            Assert.That(pool.Live, Is.EqualTo(FailurePool.Capacity));
            Assert.That(FailureFx.BurstCount(FailureFx.FlameoutMode, false), Is.EqualTo(0));
            Assert.That(FailureFx.BurstCount(FailureFx.BreakMode, true), Is.EqualTo(12));
            Assert.That(FailureFx.BurstCount(FailureFx.BreakMode, false), Is.EqualTo(24));
        }

        [Test]
        public void EveryKindHasAReadableMode()
        {
            var cue = new FailureCue { Joints = new[] { new FailureJoint { Y = 6f, Radius = 1.4f, Source = "interstage" } }, EngineCount = 7, EngineIndex = 2, Radius = 1.8f, AftY = 2f, Sink = 20f, VLeg = 8f };
            string[] kinds = { "structural-maxq", "overg", "tumble", "engine-overpressure", "bad-mixture", "thermal", "cryo-material", "prop-depletion", "separation-collision", "leg-collapse", "engine-out" };
            var modes = new HashSet<string>();
            foreach (var kind in kinds)
            {
                var plan = FailureFx.Plan(kind, kind == "thermal" ? "nose" : "", cue, 0.6f, false);
                Assert.That(plan.Mode, Is.Not.Empty);
                modes.Add(plan.Mode);
            }
            Assert.That(modes.Count, Is.GreaterThanOrEqualTo(6));
        }

        [Test]
        public void MeshSplitsOnTheJointPlane()
        {
            var mesh = new Mesh
            {
                vertices = new[] { new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0.5f, 2f, 0f) },
                triangles = new[] { 0, 1, 2 },
            };
            FailureCut.Halves(mesh, 1f, out var lower, out var upper);
            Assert.That(lower.vertexCount, Is.GreaterThan(0));
            Assert.That(upper.vertexCount, Is.GreaterThan(0));
            foreach (var v in lower.vertices) Assert.That(v.y, Is.LessThanOrEqualTo(1f + 1e-3f));
            foreach (var v in upper.vertices) Assert.That(v.y, Is.GreaterThanOrEqualTo(1f - 1e-3f));
        }
    }
}
