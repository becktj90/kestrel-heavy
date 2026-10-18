using Kestrel.Kit;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Environment.Tests
{
    public class ExplosionFxTests
    {
        GameObject go; ExplosionFx fx;

        [SetUp]
        public void SetUp()
        {
            ExplosionFx.QualityOverride = 2;
            go = new GameObject("fx-test");
            fx = go.AddComponent<ExplosionFx>();
            fx.Subscribe();
        }

        [TearDown]
        public void TearDown()
        {
            ExplosionFx.QualityOverride = -1;
            fx.Unsubscribe();
            Object.DestroyImmediate(go);
            TimeOfDayRig.ClearForce();
        }

        [Test]
        public void PoolsAreBuiltOnFirstEventAtTheTierBudget()
        {
            CinematicEvents.Crash(0f, 10f, 0f, 1f, 2f, false, false);
            Assert.That(fx.BuiltQuality, Is.EqualTo(2));
            for (int l = 0; l < ExplosionBudgets.LayerCount; l++)
                Assert.That(fx.LayerCap((ExplosionLayer)l), Is.EqualTo(ExplosionBudgets.Cap(2, (ExplosionLayer)l)));
        }

        [Test]
        public void LowTierNeverEnablesALight()
        {
            ExplosionFx.QualityOverride = 0;
            CinematicEvents.Crash(0f, 10f, 0f, 1f, 2f, false, false);
            Assert.That(fx.LightEnabled, Is.False);
        }

        [Test]
        public void TwoBlastsInsideTheLimiterWindowOnlyFlashOnce()
        {
            TimeOfDayRig.Force(LaunchTimeOfDay.Night);
            CinematicEvents.Crash(0f, 10f, 0f, 1f, 2f, false, false);
            Assert.That(fx.LightEnabled, Is.True);
            Assert.That(fx.LightLevel, Is.EqualTo(0f), "light starts at zero and ramps");
            fx.Tick(1f / 60f);
            CinematicEvents.Failure(0f, 10f, 0f, 1f, 2f, false);
            Assert.That(fx.LightLevel, Is.LessThanOrEqualTo(ExplosionBudgets.MaxLightIntensity(1f) + 1e-4f));
        }

        [Test]
        public void DisabledComponentStopsListening()
        {
            CinematicEvents.Crash(0f, 10f, 0f, 1f, 2f, false, false);
            fx.Unsubscribe();
            int before = fx.BuiltQuality;
            ExplosionFx.QualityOverride = 1;
            CinematicEvents.Crash(0f, 10f, 0f, 1f, 2f, false, false);
            Assert.That(fx.BuiltQuality, Is.EqualTo(before), "no rebuild while disabled");
        }
    }
}
