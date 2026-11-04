// Engine-free bounds on the explosion plans (Trevor, TestFlight: a huge flat disc filled the screen when a skinny rocket blew up).
// These use only the plan API that existed before the fix, with literal limits, so they FAIL on the old code: a blast radius of
// 500 m used to turn into a 2 km fireball, a 1.6x flash card and a 7 km shockwave disc. Photosensitivity limits live here too.
using System;
using Kestrel.Kit;
using NUnit.Framework;

namespace Kestrel.Pad.Tests
{
    public class ExplosionBoundsTests
    {
        static CinematicEvent Ev(CinematicKind k, float radius, float energy = 1f, bool grounded = false, bool over = true)
            => new CinematicEvent { Kind = k, Energy01 = energy, Radius = radius, EngineIndex = k == CinematicKind.EngineFailure ? 0 : -1, Grounded = grounded, Overpressure = over };

        static readonly float[] Radii = { 0f, 0.3f, 1.5f, 5f, 40f, 500f, 1e6f, float.NaN, float.PositiveInfinity };

        [Test]
        public void BlastRadiusIsBoundedWhateverTheSourceRadius()
        {
            foreach (var k in new[] { CinematicKind.Crash, CinematicKind.Failure, CinematicKind.EngineFailure })
                foreach (float r in Radii)
                    foreach (float e in new[] { 0f, 0.5f, 1f })
                    {
                        var p = ExplosionPlan.Build(Ev(k, r, e), 2, null, false, 0.85f);
                        Assert.That(p.Radius, Is.LessThanOrEqualTo(16f).And.GreaterThanOrEqualTo(0.5f), $"{k} radius {r} energy {e}");
                        Assert.That(p.FlashSize, Is.LessThanOrEqualTo(16f * 1.6f + 1e-3f), $"{k} flash size for radius {r}");
                        Assert.That(p.FireSize, Is.LessThanOrEqualTo(16f), $"{k} fire size for radius {r}");
                        Assert.That(p.SmokeSize, Is.LessThanOrEqualTo(16f * 1.1f + 1e-3f), $"{k} smoke size for radius {r}");
                    }
        }

        [Test]
        public void FlashOpacityNeverExceedsSixtyPercent()
        {
            foreach (var night in new[] { 0f, 1f })
            {
                var tod = new TimeOfDayPreset { SunElevationDeg = night > 0.5f ? -10f : 40f };
                var p = ExplosionPlan.Build(Ev(CinematicKind.Crash, 2f), 2, tod, false, 1f);
                Assert.That(p.FlashAlpha, Is.LessThanOrEqualTo(0.6f + 1e-4f), "night " + night);
                Assert.That(p.FlashAlpha, Is.GreaterThan(0f), "the flash still happens");
            }
        }

        [Test]
        public void ShockwaveRingIsBoundedWhateverTheSourceRadius()
        {
            foreach (float r in Radii)
                foreach (bool grounded in new[] { true, false })
                {
                    var p = ShockwavePlan.Build(new ShockwaveEvent { Energy01 = 1f, Radius = r, Grounded = grounded }, 2, null);
                    Assert.That(p.Draw, Is.True);
                    Assert.That(p.MaxRadius, Is.LessThanOrEqualTo(30f), "ring for radius " + r);
                    Assert.That(p.PeakAlpha, Is.LessThanOrEqualTo(0.35f + 1e-4f));
                    for (float t = 0f; t <= p.Duration; t += 0.05f) Assert.That(p.RadiusAt(t), Is.LessThanOrEqualTo(30f));
                }
        }

        [Test]
        public void ARecoveryBlastAndAFlightBreakupBothStayInBounds()
        {
            // The two real raise sites: a full-vehicle breakup (energy 1) and a hard landing (energy by joules), as CinematicFlightSink raises them.
            var breakup = ExplosionPlan.Build(Ev(CinematicKind.Failure, 1.5f, 1f), 2, null, false, 0.85f);
            var landing = ExplosionPlan.Build(Ev(CinematicKind.Crash, 1.5f, 0.15f, true), 2, null, false, 0.85f);
            Assert.That(breakup.Radius, Is.LessThanOrEqualTo(16f)); Assert.That(landing.Radius, Is.LessThan(breakup.Radius));
        }

        [Test]
        public void FlashesNeverExceedThreePerSecond()
        {
            var lim = new FlashLimiter();
            int granted = 0;
            // 60 requests a second for ten seconds, far more than a real run raises.
            for (int i = 0; i < 600; i++) if (lim.Grant(i / 60f, 1f, 0.85f) > 0f) granted++;
            Assert.That(granted, Is.LessThanOrEqualTo(3 * 10), "more than 3 flashes in a second somewhere");
            // And no sliding one-second window holds more than 3.
            lim.Reset();
            var times = new System.Collections.Generic.List<float>();
            for (int i = 0; i < 600; i++) { float t = i / 60f; if (lim.Grant(t, 1f, 0.85f) > 0f) times.Add(t); }
            for (int i = 0; i + 3 < times.Count; i++) Assert.That(times[i + 3] - times[i], Is.GreaterThanOrEqualTo(1f - 1e-4f), "4 flashes inside one second at " + times[i]);
        }
    }
}
