using Kestrel.Kit;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Environment.Tests
{
    public class DebrisFxTests
    {
        GameObject go; DebrisFx fx;

        [SetUp]
        public void SetUp()
        {
            ExplosionFx.QualityOverride = 1;
            go = new GameObject("debris-test");
            fx = go.AddComponent<DebrisFx>();
            fx.Subscribe();
        }

        [TearDown]
        public void TearDown()
        {
            ExplosionFx.QualityOverride = -1;
            fx.Unsubscribe();
            Object.DestroyImmediate(go);
            CinematicEvents.ClearSubscribers();
        }

        [Test]
        public void StageSeparationCreatesARigidBodyAndPublishesItsHandle()
        {
            CinematicEvents.StageSeparation(0f, 20f, 0f, 0f, 0f, 0f, 1, 9000f, 30f, 1.8f, 22500f, 3.6f, 4f, false);
            Assert.That(CinematicEvents.LastDebrisHandle, Is.Not.EqualTo(0));
            Assert.That(fx.IsDriving(CinematicEvents.LastDebrisHandle), Is.True);
            var b = fx.World.Bodies[fx.World.SlotOf(CinematicEvents.LastDebrisHandle)];
            Assert.That(b.Kind, Is.EqualTo(DebrisKind.Stage));
            Assert.That(b.W.Z != 0f, Is.True, "spin derived from the impulse");
            Assert.That(Mathf.Abs(b.W.Z), Is.EqualTo(DebrisMath.SpinFromImpulse(22500f, 3.6f, DebrisMath.CylinderInertia(9000f, 1.8f, 30f))).Within(1e-5f));
        }

        [Test]
        public void FairingMakesTwoHalvesLeavingOppositeWays()
        {
            CinematicEvents.FairingSeparation(0f, 50f, 0f, 0f, 0f, 0f, 900f, 12f, 2.2f, 1800f, 0.5f, 4f, false);
            var a = fx.World.Bodies[fx.World.SlotOf(CinematicEvents.LastDebrisHandle)];
            var b = fx.World.Bodies[fx.World.SlotOf(CinematicEvents.LastDebrisHandleB)];
            Assert.That(a.V.X, Is.LessThan(0f)); Assert.That(b.V.X, Is.GreaterThan(0f));
            Assert.That(a.W.Y, Is.EqualTo(-b.W.Y).Within(1e-6f));
        }

        [Test]
        public void ClearEventKillsEverythingAndStalesHandles()
        {
            CinematicEvents.StageSeparation(0f, 20f, 0f, 0f, 0f, 0f, 1, 9000f, 30f, 1.8f, 22500f, 3.6f, 4f, false);
            int h = CinematicEvents.LastDebrisHandle;
            CinematicEvents.Crash(0f, 1f, 0f, 1f, 2f, true, false);
            Assert.That(fx.World.Live, Is.GreaterThan(1));
            CinematicEvents.DebrisClear();
            Assert.That(fx.World.Live, Is.EqualTo(0));
            Assert.That(fx.IsDriving(h), Is.False);
        }

        [Test]
        public void CrashAndFailureThrowDebrisWithinTheTierBudget()
        {
            CinematicEvents.Crash(0f, 1f, 0f, 1f, 2f, true, false);
            Assert.That(fx.World.LiveOf(DebrisKind.Fragment), Is.InRange(1, DebrisBudgets.Fragments(1)));
            Assert.That(fx.World.LiveOf(DebrisKind.Spark), Is.InRange(1, DebrisBudgets.Sparks(1)));
            fx.Tick(1f / 60f);
            CinematicEvents.EngineFailure(0f, 5f, 0f, 0, false, false);   // a sputter throws no chunks
            Assert.That(fx.World.Live, Is.LessThanOrEqualTo(DebrisBudgets.Capacity(1)));
        }

        [Test]
        public void ReduceMotionThrowsNoSparks()
        {
            CinematicEvents.Failure(0f, 30f, 0f, 1f, 2f, true);
            Assert.That(fx.World.LiveOf(DebrisKind.Spark), Is.EqualTo(0));
        }

        [Test]
        public void SeparationBodiesStayInTheirFrame_NoGroundCollision()
        {
            CinematicEvents.Surfaces = new DebrisSurfaces { GroundY = 0f };
            CinematicEvents.StageSeparation(0f, 5f, 0f, 0f, 0f, 0f, 1, 9000f, 30f, 1.8f, 22500f, 3.6f, 20f, false);
            for (int i = 0; i < 120; i++) fx.Tick(1f / 60f);
            var b = fx.World.Bodies[fx.World.SlotOf(CinematicEvents.LastDebrisHandle)];
            Assert.That(b.P.Y, Is.LessThan(0f), "falls below y=0 in the stack frame: nothing to hit");
        }
    }
}
