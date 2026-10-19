using Kestrel.Kit;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Environment.Tests
{
    public class StagingFlashFxTests
    {
        GameObject go; StagingFlashFx fx;

        [SetUp] public void SetUp()
        {
            ExplosionFx.QualityOverride = 2; FlashGovernor.Reset();
            go = new GameObject("sf-test"); fx = go.AddComponent<StagingFlashFx>(); fx.Subscribe();
        }

        [TearDown] public void TearDown()
        {
            ExplosionFx.QualityOverride = -1; fx.Unsubscribe(); Object.DestroyImmediate(go); TimeOfDayRig.ClearForce();
        }

        [Test] public void PoolIsBuiltAtTheTierBudget()
        {
            CinematicEvents.StagingFlash(StagingFlashKind.Separation, 0, 5, 0, 1f, false);
            Assert.That(fx.BuiltQuality, Is.EqualTo(2));
            Assert.That(fx.ParticleCap, Is.EqualTo(StagingFlashBudgets.Particles(2)));
        }

        [Test] public void LowTierNeverEnablesALight()
        {
            ExplosionFx.QualityOverride = 0;
            CinematicEvents.StagingFlash(StagingFlashKind.Separation, 0, 5, 0, 1f, false);
            Assert.That(fx.LightEnabled, Is.False);
        }

        [Test] public void SeparationLightsThenDecaysToOff()
        {
            CinematicEvents.StagingFlash(StagingFlashKind.Separation, 0, 5, 0, 1f, false);
            Assert.That(fx.LightEnabled, Is.True);
            for (int i = 0; i < 240; i++) fx.Tick(1f / 60f);
            Assert.That(fx.LightEnabled, Is.False);
        }

        [Test] public void ReduceMotionSeparationDrawsNothingButIgnitionGlowsGently()
        {
            CinematicEvents.StagingFlash(StagingFlashKind.Separation, 0, 5, 0, 1f, true);
            Assert.That(fx.LightEnabled, Is.False);
            CinematicEvents.StagingFlash(StagingFlashKind.Ignition, 0, 5, 0, 1f, true);
            Assert.That(fx.LightEnabled, Is.True);
            fx.Tick(1f / 60f);
            Assert.That(fx.LightLevel, Is.LessThan(0.2f));
        }

        [Test] public void SecondSeparationInsideTheWindowIsRefused()
        {
            CinematicEvents.StagingFlash(StagingFlashKind.Separation, 0, 5, 0, 1f, false);
            for (int i = 0; i < 240; i++) fx.Tick(1f / 60f);
            CinematicEvents.StagingFlash(StagingFlashKind.Separation, 0, 5, 0, 1f, false);
            Assert.That(fx.LightEnabled, Is.False);
        }
    }
}
