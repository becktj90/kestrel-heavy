using NUnit.Framework;

namespace Kestrel.Kit.Tests
{
    public class ExplosionPlanTests
    {
        static readonly LaunchTimeOfDay[] Tods = { LaunchTimeOfDay.Day, LaunchTimeOfDay.Sunset, LaunchTimeOfDay.Night };

        static CinematicEvent Ev(CinematicKind k, float energy = 1f, bool grounded = false, bool over = false, bool reduce = false) =>
            new CinematicEvent { Kind = k, Energy01 = energy, Radius = 2f, EngineIndex = k == CinematicKind.EngineFailure ? 1 : -1, Grounded = grounded, Overpressure = over, Reduce = reduce };

        [Test]
        public void TierBudgetsAreTheDocumentedNumbers()
        {
            Assert.That(ExplosionBudgets.TotalParticles(0), Is.EqualTo(61));
            Assert.That(ExplosionBudgets.TotalParticles(1), Is.EqualTo(150));
            Assert.That(ExplosionBudgets.TotalParticles(2), Is.EqualTo(323));
            Assert.That(ExplosionBudgets.Lights(0), Is.EqualTo(0));
            Assert.That(ExplosionBudgets.Lights(1), Is.EqualTo(1));
            Assert.That(ExplosionBudgets.Lights(2), Is.EqualTo(1));
            for (int q = 0; q < 2; q++) Assert.That(ExplosionBudgets.TotalParticles(q), Is.LessThan(ExplosionBudgets.TotalParticles(q + 1)));
        }

        [Test]
        public void NoPlanEverExceedsTheTierCapForAnyLayer()
        {
            foreach (var t in Tods)
                for (int q = 0; q <= 2; q++)
                    foreach (CinematicKind k in new[] { CinematicKind.Crash, CinematicKind.Failure, CinematicKind.EngineFailure })
                        foreach (float en in new[] { 0f, 0.3f, 1f, 5f })
                            foreach (bool reduce in new[] { false, true })
                            {
                                var p = ExplosionPlan.Build(Ev(k, en, true, true, reduce), q, TimeOfDayPresets.For(t, 1), false, 1f);
                                for (int l = 0; l < ExplosionBudgets.LayerCount; l++)
                                    Assert.That(p.Count((ExplosionLayer)l), Is.InRange(0, ExplosionBudgets.Cap(q, (ExplosionLayer)l)), $"{t}/q{q}/{k}/{l}");
                                Assert.That(p.Total, Is.LessThanOrEqualTo(ExplosionBudgets.TotalParticles(q)));
                            }
        }

        [Test]
        public void LayersAllPresentForACrashAndScaleWithTier()
        {
            var day = TimeOfDayPresets.For(LaunchTimeOfDay.Day, 1);
            var hi = ExplosionPlan.Build(Ev(CinematicKind.Crash), 2, day, false, 1f);
            var lo = ExplosionPlan.Build(Ev(CinematicKind.Crash), 0, day, false, 1f);
            Assert.That(hi.Flash, Is.GreaterThan(0)); Assert.That(hi.Fire, Is.GreaterThan(0)); Assert.That(hi.Smoke, Is.GreaterThan(0));
            Assert.That(hi.Sparks, Is.GreaterThan(0)); Assert.That(hi.Embers, Is.GreaterThan(0));
            Assert.That(hi.Total, Is.GreaterThan(lo.Total));
        }

        [Test]
        public void EngineSputterIsSmallAndDoesNotFlash()
        {
            var day = TimeOfDayPresets.For(LaunchTimeOfDay.Day, 1);
            var sputter = ExplosionPlan.Build(Ev(CinematicKind.EngineFailure, 0.3f), 2, day, false, 1f);
            var crash = ExplosionPlan.Build(Ev(CinematicKind.Crash), 2, day, false, 1f);
            var over = ExplosionPlan.Build(Ev(CinematicKind.EngineFailure, 0.6f, false, true), 2, day, false, 1f);
            Assert.That(sputter.Flash, Is.EqualTo(0)); Assert.That(sputter.LightIntensity, Is.EqualTo(0f));
            Assert.That(sputter.Total, Is.LessThan(crash.Total));
            Assert.That(over.Flash, Is.GreaterThan(0)); Assert.That(over.Total, Is.LessThan(crash.Total));
        }

        [Test]
        public void SmokeIsDarkerAtNightAndWarmerAtSunsetThanAtDay()
        {
            var d = ExplosionPlan.Build(Ev(CinematicKind.Crash), 2, TimeOfDayPresets.For(LaunchTimeOfDay.Day, 1), false, 1f);
            var s = ExplosionPlan.Build(Ev(CinematicKind.Crash), 2, TimeOfDayPresets.For(LaunchTimeOfDay.Sunset, 1), false, 1f);
            var n = ExplosionPlan.Build(Ev(CinematicKind.Crash), 2, TimeOfDayPresets.For(LaunchTimeOfDay.Night, 1), false, 1f);
            Assert.That(ExplosionPlan.Luminance(n.SmokeColor), Is.LessThan(ExplosionPlan.Luminance(d.SmokeColor)));
            Assert.That(s.SmokeColor.R / System.Math.Max(0.001f, s.SmokeColor.B), Is.GreaterThan(d.SmokeColor.R / System.Math.Max(0.001f, d.SmokeColor.B)));
            Assert.That(n.Night, Is.EqualTo(1f)); Assert.That(d.Night, Is.EqualTo(0f));
            Assert.That(n.FireAlpha, Is.LessThan(d.FireAlpha), "night exposure trims the additive fire");
        }

        [Test]
        public void FlashLightIsCappedAtEveryTimeOfDayAndTier()
        {
            foreach (var t in Tods)
                for (int q = 1; q <= 2; q++)
                {
                    var tod = TimeOfDayPresets.For(t, 3);
                    var p = ExplosionPlan.Build(Ev(CinematicKind.Crash, 1f), q, tod, false, 5f);
                    Assert.That(p.LightIntensity, Is.LessThanOrEqualTo(ExplosionBudgets.MaxLightIntensity(tod.NightFactor) + 1e-4f));
                    Assert.That(p.FlashAlpha, Is.LessThanOrEqualTo(ExplosionBudgets.FlashAmplitudeCap(tod.NightFactor) + 1e-4f));
                    Assert.That(p.LightIntensity, Is.GreaterThan(0f));
                    Assert.That(p.LightRise * 0.04f, Is.GreaterThanOrEqualTo(0.99f), "peak is reached over about 40 ms at the fastest");
                }
            Assert.That(ExplosionBudgets.MaxLightIntensity(1f), Is.LessThan(ExplosionBudgets.MaxLightIntensity(0f)));
        }

        [Test]
        public void LowTierHasNoRealtimeLight()
        {
            var p = ExplosionPlan.Build(Ev(CinematicKind.Crash), 0, TimeOfDayPresets.For(LaunchTimeOfDay.Night, 1), false, 1f);
            Assert.That(p.LightIntensity, Is.EqualTo(0f));
        }

        [Test]
        public void ReduceMotionPlanHasNoFlashNoSparksAndASteadyGlow()
        {
            foreach (var t in Tods)
            {
                var tod = TimeOfDayPresets.For(t, 1);
                var p = ExplosionPlan.Build(Ev(CinematicKind.Crash, 1f, false, false, true), 2, tod, false, 1f);
                Assert.That(p.Calm, Is.True);
                Assert.That(p.Flash, Is.EqualTo(0)); Assert.That(p.Sparks, Is.EqualTo(0)); Assert.That(p.FlashAlpha, Is.EqualTo(0f));
                Assert.That(p.LightIntensity, Is.LessThanOrEqualTo(1.2f + 1e-4f));
                Assert.That(p.LightRise, Is.LessThanOrEqualTo(2f), "no fast ramp");
                Assert.That(p.SparkSpeed, Is.EqualTo(0f));
                Assert.That(p.Fire, Is.GreaterThan(0)); Assert.That(p.Smoke, Is.GreaterThan(0));
            }
        }

        [Test]
        public void RefusedFlashStillPlaysFireAndSmokeButNoFlashLight()
        {
            var p = ExplosionPlan.Build(Ev(CinematicKind.Crash), 2, TimeOfDayPresets.For(LaunchTimeOfDay.Day, 1), false, 0f);
            Assert.That(p.Flash, Is.EqualTo(0)); Assert.That(p.LightIntensity, Is.EqualTo(0f));
            Assert.That(p.Fire, Is.GreaterThan(0)); Assert.That(p.Smoke, Is.GreaterThan(0));
        }

        [Test]
        public void LimiterAllowsAtMostThreeFlashesPerSecondAndCapsAmplitude()
        {
            var lim = new FlashLimiter();
            Assert.That(lim.Grant(0f, 1f, 0.8f), Is.EqualTo(0.8f));
            Assert.That(lim.Grant(0.1f, 1f, 0.8f), Is.EqualTo(0f));
            Assert.That(lim.Grant(0.3f, 1f, 0.8f), Is.EqualTo(0f));
            Assert.That(lim.Grant(0.35f, 0.5f, 0.8f), Is.EqualTo(0.5f));
            int granted = 0; var l2 = new FlashLimiter();
            for (int i = 0; i < 600; i++) if (l2.Grant(i * (1f / 60f), 1f, 1f) > 0f) granted++;
            Assert.That(granted, Is.LessThanOrEqualTo(3 * 10 + 1), "3 per second over 10 s");
            Assert.That(lim.Grant(float.NaN, 1f, 1f), Is.EqualTo(0f));
        }

        [Test]
        public void EnvelopeRisesOverSeveralFramesPeaksOnceAndDecaysMonotonically()
        {
            var env = new FlashEnvelope();
            env.Trigger(1f, 0.06f);
            float prev = 0f; int risingFrames = 0; bool falling = false; int peaks = 0;
            for (int i = 0; i < 400; i++)
            {
                float v = env.Step(1f / 120f, 25f, 4.5f);
                Assert.That(v, Is.InRange(0f, 1f));
                if (v > prev + 1e-6f) { Assert.That(falling, Is.False, "no second rise"); risingFrames++; }
                if (v < prev - 1e-6f) { if (!falling) peaks++; falling = true; }
                Assert.That(v - prev, Is.LessThanOrEqualTo(25f / 120f + 1e-4f), "slew limited");
                prev = v;
            }
            Assert.That(risingFrames, Is.GreaterThanOrEqualTo(4));
            Assert.That(peaks, Is.EqualTo(1));
            Assert.That(prev, Is.EqualTo(0f));
        }

        [Test]
        public void CinematicEventsDeliverAStructToSubscribersAndCountRaises()
        {
            CinematicEvents.ClearSubscribers();
            CinematicEvent got = default; int calls = 0;
            System.Action<CinematicEvent> h = e => { got = e; calls++; };
            CinematicEvents.Raised += h;
            CinematicEvents.Crash(1f, 2f, 3f, 0.8f, 2.5f, true, false);
            CinematicEvents.EngineFailure(4f, 5f, 6f, 2, true, true);
            CinematicEvents.Raised -= h;
            CinematicEvents.Failure(0f, 0f, 0f, 1f, 1f, false);
            Assert.That(calls, Is.EqualTo(2));
            Assert.That(got.Kind, Is.EqualTo(CinematicKind.EngineFailure)); Assert.That(got.EngineIndex, Is.EqualTo(2));
            Assert.That(got.Overpressure && got.Reduce, Is.True); Assert.That(got.X, Is.EqualTo(4f));
            Assert.That(CinematicEvents.RaisedCount, Is.EqualTo(3));
            CinematicEvents.ClearSubscribers();
        }
    }
}
