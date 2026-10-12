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
    }
}
