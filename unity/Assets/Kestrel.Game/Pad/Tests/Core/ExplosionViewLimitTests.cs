// Engine-free tests for the screen-area limit on the explosion flash and for sizing the blast from the real rocket.
using System;
using Kestrel.Kit;
using NUnit.Framework;

namespace Kestrel.Pad.Tests
{
    public class ExplosionViewLimitTests
    {
        static CinematicEvent Ev(float radius) => new CinematicEvent { Kind = CinematicKind.Failure, Energy01 = 1f, Radius = radius, EngineIndex = -1 };

        [TestCase(5f, 40f)] [TestCase(8f, 50f)] [TestCase(20f, 60f)] [TestCase(60f, 30f)] [TestCase(0f, 60f)] [TestCase(float.NaN, float.NaN)]
        public void FlashCardNeverSpansMoreThanItsShareOfTheScreen(float dist, float fov)
        {
            foreach (float radius in new[] { 0.4f, 1.5f, 6f, 500f })
            {
                var p = ExplosionPlan.Build(Ev(radius), 2, null, false, 0.85f).LimitFlashToView(dist, fov);
                float d = float.IsNaN(dist) || dist < 1f ? 1f : dist;
                float f = float.IsNaN(fov) ? 5f : Math.Max(5f, Math.Min(120f, fov));
                float viewH = 2f * d * (float)Math.Tan(f * 0.5f * Math.PI / 180.0);
                // The card grows by 1.3 (per-particle jitter) x 1.2 (size curve peak) after it is emitted.
                Assert.That(p.FlashSize * 1.3f * 1.2f, Is.LessThanOrEqualTo(viewH * ExplosionBudgets.MaxFlashScreenFraction + 1e-3f), $"radius {radius} dist {dist} fov {fov}");
                Assert.That(p.FlashSize, Is.GreaterThan(0f));
            }
        }

        [Test]
        public void ALimitNeverEnlargesTheFlashAndNeverTouchesFireOrSmoke()
        {
            var raw = ExplosionPlan.Build(Ev(0.4f), 2, null, false, 0.85f);
            var far = raw.LimitFlashToView(1000f, 60f);
            Assert.That(far.FlashSize, Is.EqualTo(raw.FlashSize), "a distant camera leaves a small flash alone");
            var near = ExplosionPlan.Build(Ev(6f), 2, null, false, 0.85f).LimitFlashToView(6f, 60f);
            Assert.That(near.FireSize, Is.EqualTo(ExplosionPlan.Build(Ev(6f), 2, null, false, 0.85f).FireSize));
            Assert.That(near.FlashSize, Is.LessThan(ExplosionPlan.Build(Ev(6f), 2, null, false, 0.85f).FlashSize));
        }

        [Test]
        public void TheBlastFollowsTheRocketNotAFixedNumber()
        {
            float skinny = FlightSinkMath.VehicleRadiusM(0.8), fat = FlightSinkMath.VehicleRadiusM(3.6);
            Assert.That(skinny, Is.EqualTo(0.4f).Within(1e-5f)); Assert.That(fat, Is.EqualTo(1.8f).Within(1e-5f));
            var a = ExplosionPlan.Build(Ev(skinny), 2, null, false, 0.85f); var b = ExplosionPlan.Build(Ev(fat), 2, null, false, 0.85f);
            Assert.That(a.Radius, Is.LessThan(b.Radius * 0.5f), "a 0.8 m rocket must not blow up like a 3.6 m one");
            var ringA = ShockwavePlan.Build(new ShockwaveEvent { Energy01 = 1f, Radius = skinny }, 2, null);
            var ringB = ShockwavePlan.Build(new ShockwaveEvent { Energy01 = 1f, Radius = fat }, 2, null);
            Assert.That(ringA.MaxRadius, Is.LessThan(ringB.MaxRadius));
            Assert.That(FlightSinkMath.VehicleRadiusM(0), Is.EqualTo(1.5f)); Assert.That(FlightSinkMath.VehicleRadiusM(double.NaN), Is.EqualTo(1.5f));
            Assert.That(FlightSinkMath.VehicleRadiusM(100), Is.EqualTo(6f)); Assert.That(FlightSinkMath.VehicleRadiusM(0.1), Is.EqualTo(0.3f));
        }
    }
}
