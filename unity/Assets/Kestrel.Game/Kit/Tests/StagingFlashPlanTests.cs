using Kestrel.Kit;
using NUnit.Framework;

namespace Kestrel.Kit.Tests
{
    public class StagingFlashPlanTests
    {
        static TimeOfDayPreset Sun(float elev) => new TimeOfDayPreset { SunElevationDeg = elev };
        static StagingFlashEvent Sep(float i = 1f) => new StagingFlashEvent { Kind = StagingFlashKind.Separation, Intensity01 = i };
        static StagingFlashEvent Ign(float i = 1f) => new StagingFlashEvent { Kind = StagingFlashKind.Ignition, Intensity01 = i };

        [SetUp] public void SetUp() { FlashGovernor.Reset(); }

        [Test] public void SeparationNeedsAGrant()
        {
            Assert.That(StagingFlashPlan.Build(Sep(), 2, Sun(40), false, 0f).Draw, Is.False);
            Assert.That(StagingFlashPlan.Build(Sep(), 2, Sun(40), false, 0.8f).Draw, Is.True);
        }

        [Test] public void ReduceMotionHasNoSeparationFlashAndOnlyACalmIgnitionGlow()
        {
            Assert.That(StagingFlashPlan.Build(Sep(), 2, Sun(40), true, 1f).Draw, Is.False);
            var calm = StagingFlashPlan.Build(Ign(), 2, Sun(40), true, 0f);
            var norm = StagingFlashPlan.Build(Ign(), 2, Sun(40), false, 0f);
            Assert.That(calm.Draw && calm.Calm, Is.True);
            Assert.That(calm.LightIntensity, Is.LessThan(norm.LightIntensity));
            Assert.That(calm.LightRise, Is.LessThan(norm.LightRise));
        }

        [Test] public void AmplitudeAndLightAreCappedAndFallAtNight()
        {
            var day = StagingFlashPlan.Build(Sep(), 2, Sun(40), false, 5f);
            var night = StagingFlashPlan.Build(Sep(), 2, Sun(-30), false, 5f);
            Assert.That(day.ParticleAlpha, Is.LessThanOrEqualTo(ExplosionBudgets.FlashAmplitudeCap(0f)));
            Assert.That(night.ParticleAlpha, Is.LessThan(day.ParticleAlpha));
            Assert.That(night.LightIntensity, Is.LessThan(day.LightIntensity));
            Assert.That(day.LightIntensity, Is.LessThanOrEqualTo(ExplosionBudgets.MaxLightIntensity(0f) * StagingFlashBudgets.SeparationShare));
        }

        [Test] public void LowTierHasNoLightAndMediumHighKeepIt()
        {
            Assert.That(StagingFlashPlan.Build(Sep(), 0, Sun(40), false, 1f).LightIntensity, Is.EqualTo(0f));
            Assert.That(StagingFlashPlan.Build(Sep(), 1, Sun(40), false, 1f).LightIntensity, Is.GreaterThan(0f));
            Assert.That(StagingFlashPlan.Build(Ign(), 0, Sun(40), false, 0f).LightIntensity, Is.EqualTo(0f));
        }

        [Test] public void ParticleBudgetFollowsTheTier()
        {
            for (int q = 0; q < 2; q++)
                Assert.That(StagingFlashPlan.Build(Sep(), q, Sun(40), false, 1f).Particles, Is.LessThan(StagingFlashPlan.Build(Sep(), q + 1, Sun(40), false, 1f).Particles));
        }

        [Test] public void IgnitionGlowRisesSlowlyEnoughToNeverBeAFlash()
        {
            var p = StagingFlashPlan.Build(Ign(), 2, Sun(-30), false, 0f);
            Assert.That(1f / p.LightRise, Is.GreaterThanOrEqualTo(0.8f));
            Assert.That(p.LightIntensity, Is.LessThanOrEqualTo(ExplosionBudgets.MaxLightIntensity(1f) * StagingFlashBudgets.IgnitionShare));
        }

        [Test] public void SeparationEnvelopeIsASingleMonotonePeak()
        {
            var p = StagingFlashPlan.Build(Sep(), 2, Sun(40), false, 1f);
            var env = new FlashEnvelope(); env.Trigger(1f, p.LightHold);
            float prev = 0f; bool falling = false; int peaks = 0;
            for (int i = 0; i < 400; i++)
            {
                float l = env.Step(1f / 120f, p.LightRise, p.LightDecay);
                if (l < prev - 1e-6f) { if (!falling) peaks++; falling = true; }
                else if (l > prev + 1e-6f && falling) Assert.Fail("re-rose after falling");
                prev = l;
            }
            Assert.That(peaks, Is.EqualTo(1)); Assert.That(prev, Is.EqualTo(0f));
        }

        [Test] public void GovernorSharesTheThreeHertzBudget()
        {
            int granted = 0;
            for (int i = 0; i < 100; i++) if (FlashGovernor.Grant(i * 0.05f, 1f, 0.85f) > 0f) granted++;   // 5 s of requests every 50 ms
            Assert.That(granted, Is.LessThanOrEqualTo(15));
            Assert.That(granted, Is.GreaterThan(10));
        }

        [Test] public void NanAndZeroAreInert()
        {
            Assert.That(StagingFlashPlan.Build(Sep(float.NaN), 2, Sun(40), false, 1f).Draw, Is.False);
            Assert.That(StagingFlashPlan.Build(Ign(0f), 2, Sun(40), false, 0f).Draw, Is.False);
        }

        [Test] public void EventReachesSubscribers()
        {
            StagingFlashEvent got = default; int n = 0;
            System.Action<StagingFlashEvent> h = e => { got = e; n++; };
            CinematicEvents.StagingFlashRaised += h;
            try { CinematicEvents.StagingFlash(StagingFlashKind.Ignition, 1f, 2f, 3f, 0.5f, true); }
            finally { CinematicEvents.StagingFlashRaised -= h; }
            Assert.That(n, Is.EqualTo(1)); Assert.That(got.Z, Is.EqualTo(3f)); Assert.That(got.Reduce, Is.True);
        }
    }
}
