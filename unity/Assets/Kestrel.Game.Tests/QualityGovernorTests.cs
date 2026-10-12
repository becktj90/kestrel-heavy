using Kestrel.Game.Quality;
using NUnit.Framework;

namespace Kestrel.Game.Tests
{
    public class QualityGovernorTests
    {
        static void Run(QualityGovernor g, double ms, double seconds)
        {
            for (double t = 0; t < seconds; t += ms / 1000.0) g.Tick(ms / 1000.0);
        }

        [Test]
        public void StepsDownAfterSustainedSlowFramesAndStopsAtFloor()
        {
            var g = new QualityGovernor(new GovernorConfig(), startLevel: 2);
            Run(g, 25, 2.0);              // window fill (3 s of slow frames not yet reached)
            Assert.That(g.Level, Is.EqualTo(2));
            Run(g, 25, 3.0 + 3.1);         // refill + 3 s hold
            Assert.That(g.Level, Is.EqualTo(1));
            Run(g, 25, 30);
            Assert.That(g.Level, Is.EqualTo(0), "floor");
        }

        [Test]
        public void ShortSpikeDoesNotStepDown()
        {
            var g = new QualityGovernor(new GovernorConfig(), 2);
            Run(g, 16.6, 5);
            Run(g, 40, 0.5);
            Run(g, 16.6, 5);
            Assert.That(g.Level, Is.EqualTo(2));
        }

        [Test]
        public void RecoversAfter30sStableButNotAboveCeiling()
        {
            var g = new QualityGovernor(new GovernorConfig(), 2);
            Run(g, 25, 10);
            Assert.That(g.Level, Is.LessThan(2));
            int dropped = g.Level;
            Run(g, 16.0, 20);
            Assert.That(g.Level, Is.EqualTo(dropped), "not before 30 s");
            Run(g, 16.0, 120);
            Assert.That(g.Level, Is.EqualTo(2), "back to ceiling, not beyond");
        }

        [Test]
        public void RenderScaleCapsBackbuffer()
        {
            Assert.That(QualityMath.RenderScale(2.0, 3), Is.EqualTo(2.0 / 3).Within(1e-12));
            Assert.That(QualityMath.RenderScale(2.5, 2), Is.EqualTo(1));
            Assert.That(QualityMath.RenderScale(1.5, 3), Is.EqualTo(0.5));
        }
    
        // #129-v7: liftoff tier hold and editor immunity (owned by graphics per the lead).
        [Test]
        public void NoStepDownDuringLiftoffHold()
        {
            var g = new QualityGovernor(new GovernorConfig(), 2);
            g.HoldFor(QualityGovernor.LiftoffHoldSeconds);
            Run(g, 40, 19.5);
            Assert.That(g.Level, Is.EqualTo(2), "held for 20 s after ignition");
        }

        [Test]
        public void NoStepDownWhenIgnoringFrameTimes()
        {
            var g = new QualityGovernor(new GovernorConfig(), 2) { IgnoreFrameTimes = true };
            Run(g, 40, 60);
            Assert.That(g.Level, Is.EqualTo(2), "editor frame times never step the tier");
        }

        [Test]
        public void StepDownStillWorksOnDeviceAfterHold()
        {
            var g = new QualityGovernor(new GovernorConfig(), 2);
            g.HoldFor(QualityGovernor.LiftoffHoldSeconds);
            Run(g, 40, 20.5);
            Assert.That(g.Level, Is.EqualTo(2));
            Run(g, 40, 2.0 + 3.0 + 3.5);   // window refill + 3 s over budget
            Assert.That(g.Level, Is.EqualTo(1), "steps down after the hold");
        }

        [Test]
        public void IgnitionHookRequestsTheHold()
        {
            double got = 0; System.Action<double> h = s => got = s;
            QualityHold.Requested += h;
            try { QualityHold.AtIgnition(); } finally { QualityHold.Requested -= h; }
            Assert.That(got, Is.EqualTo(QualityGovernor.LiftoffHoldSeconds));
        }
}
}
