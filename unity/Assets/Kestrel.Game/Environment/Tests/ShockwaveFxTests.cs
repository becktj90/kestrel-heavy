using Kestrel.Kit;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Environment.Tests
{
    public class ShockwaveFxTests
    {
        GameObject go; ShockwaveFx fx;

        [SetUp]
        public void SetUp()
        {
            ExplosionFx.QualityOverride = 2;
            go = new GameObject("shock-test"); fx = go.AddComponent<ShockwaveFx>(); fx.Subscribe();
        }

        [TearDown]
        public void TearDown()
        {
            ExplosionFx.QualityOverride = -1;
            fx.Unsubscribe(); Object.DestroyImmediate(go); CinematicEvents.ClearSubscribers(); CinematicEvents.ClearShockwaveSubscribers();
        }

        [Test]
        public void PoolsMatchTheTierBudgets()
        {
            CinematicEvents.Shockwave(0f, 0f, 0f, 1f, true, false);
            Assert.That(fx.RingCap, Is.EqualTo(ShockwaveBudgets.Rings(2))); Assert.That(fx.HazeCap, Is.EqualTo(ShockwaveBudgets.HazeCards(2)));
            Assert.That(fx.LiveRings, Is.EqualTo(1));
        }

        [Test]
        public void LowTierAndReduceMotionDrawNothing()
        {
            ExplosionFx.QualityOverride = 0;
            CinematicEvents.Shockwave(0f, 0f, 0f, 1f, true, false); CinematicEvents.HeatHaze(0f, 0f, 0f, 1f, 3f, false);
            Assert.That(fx.LiveRings + fx.LiveHaze, Is.EqualTo(0));
            ExplosionFx.QualityOverride = 2;
            CinematicEvents.Shockwave(0f, 0f, 0f, 1f, true, true); CinematicEvents.HeatHaze(0f, 0f, 0f, 1f, 3f, true);
            Assert.That(fx.LiveRings + fx.LiveHaze, Is.EqualTo(0));
        }

        [Test]
        public void RingsRecycleTheOldestAndExpire()
        {
            for (int i = 0; i < 5; i++) CinematicEvents.Shockwave(i, 0f, 0f, 1f, true, false);
            Assert.That(fx.LiveRings, Is.EqualTo(ShockwaveBudgets.Rings(2)));
            for (int i = 0; i < 400; i++) fx.Tick(1f / 60f);
            Assert.That(fx.LiveRings, Is.EqualTo(0));
        }

        [Test]
        public void ACrashRaisesARingAndHazeThroughTheExistingExplosionCue()
        {
            CinematicEvents.Crash(0f, 1f, 0f, 1f, 2f, true, false);
            Assert.That(fx.LiveRings, Is.EqualTo(1)); Assert.That(fx.LiveHaze, Is.EqualTo(1));
            CinematicEvents.EngineFailure(0f, 5f, 0f, 0, false, false);   // a sputter makes no blast wave
            Assert.That(fx.LiveRings, Is.EqualTo(1));
        }
    }
}
