// Pool caps for the pad ignition cloud and the ambient gulls. No extra light.
using Kestrel.Kit;
using NUnit.Framework;
using UnityEngine;

namespace Kestrel.Game.Environment.Tests
{
    public class PadIgnitionFxTests
    {
        GameObject anchor, fxGo, birdGo;
        PadIgnitionFx fx;
        SkyBirdView birds;

        [SetUp]
        public void SetUp()
        {
            ExplosionFx.QualityOverride = 2;
            PadVfxState.Current.ClearFlight();
            PadVfxState.Current.Deluge = true;
            PadVfxState.Current.Nozzles = 1;
            PadVfxState.Current.SetNozzle(0, 12f, 1.3f, 0f, -1f, 0f);
            PadVfxState.Current.BearingDeg = 0f;
            PadVfxState.Current.TowerHeight = 80f;
            PadVfxState.Current.ExitDiameter = 1.5f;
            anchor = new GameObject(PadVfxState.AnchorName);
            fxGo = new GameObject("pad-ignition-test");
            fx = fxGo.AddComponent<PadIgnitionFx>();
            birdGo = new GameObject("sky-birds-test");
            birds = birdGo.AddComponent<SkyBirdView>();
        }

        [TearDown]
        public void TearDown()
        {
            ExplosionFx.QualityOverride = -1;
            PadVfxState.Current.ClearFlight();
            PadVfxState.Current.Deluge = false;
            PadVfxState.Current.Nozzles = 0;
            if (fxGo != null) Object.DestroyImmediate(fxGo);
            if (birdGo != null) Object.DestroyImmediate(birdGo);
            if (anchor != null) Object.DestroyImmediate(anchor);
        }

        [Test]
        public void PoolsMatchTheTierBudgetAndAddNoLight()
        {
            PadVfxState.Current.Throttle = 1f;
            fx.Tick(1f / 60f);
            Assert.That(fx.BuiltQuality, Is.EqualTo(2));
            Assert.That(fx.SprayCap, Is.EqualTo(PadVfxBudgets.Spray(2)));
            Assert.That(fx.SteamCap, Is.EqualTo(PadVfxBudgets.Steam(2)));
            Assert.That(fx.BlastCap, Is.EqualTo(PadVfxBudgets.Blast(2)));
            Assert.That(fx.DiamondCap, Is.EqualTo(PadVfxBudgets.Diamonds(2)));
            Assert.That(fx.SprayPool, Is.EqualTo(PadVfxBudgets.Spray(2)));
            Assert.That(fx.SteamPool, Is.EqualTo(PadVfxBudgets.Steam(2)));
            Assert.That(fx.BlastPool, Is.EqualTo(PadVfxBudgets.Blast(2)));
            Assert.That(fx.DiamondPool, Is.EqualTo(PadVfxBudgets.Diamonds(2)));
            Assert.That(fx.LightCount, Is.EqualTo(0));
        }

        [Test]
        public void LowTierDrawsNoTrenchDiamonds()
        {
            ExplosionFx.QualityOverride = 0;
            PadVfxState.Current.Throttle = 1f;
            fx.Tick(1f / 60f);
            Assert.That(fx.DiamondCap, Is.EqualTo(0));
            Assert.That(fx.DiamondPool, Is.EqualTo(0));
            Assert.That(fx.SprayPool, Is.EqualTo(PadVfxBudgets.Spray(0)));
        }

        [Test]
        public void SteamHasNotStartedOnTheIgnitionFrame()
        {
            PadVfxState.Current.Throttle = 1f;
            fx.Tick(0f);
            Assert.That(PadVfxPlan.Steam(1f, 0f, true, false), Is.EqualTo(0f));
            Assert.That(fx.LightCount, Is.EqualTo(0));
        }

        [Test]
        public void SkyBirdsShowOnLowAndAddNoLight()
        {
            ExplosionFx.QualityOverride = 0;
            PadVfxState.Current.Height = 0f;
            birds.Tick(1f / 60f);
            Assert.That(birds.QuadCount, Is.EqualTo(SkyBirds.Count));
            Assert.That(birds.Shown, Is.EqualTo(PadVfxBudgets.Birds(0)));
            Assert.That(birds.LightCount, Is.EqualTo(0));
        }
    }
}
