// S2: Karman event. Density above 86 km is Atmosphere.At, not a second table.
using System.IO;
using Kestrel.Core.Astro;
using Kestrel.Core.Physics;
using Kestrel.Core.Timeline;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class UpperAtmosphereTests
    {
        static readonly double[] Grid = { 0, 86000, 100000, 120000, 150000, 200000, 300000, 1000000 };

        [Test]
        public void Above86KmIsAtmosphereAt()
        {
            foreach (double z in Grid)
            {
                var a = Atmosphere.At(z);
                var u = UpperAtmosphere.At(z);
                Assert.AreEqual(a.T, u.T, "T@" + z);
                Assert.AreEqual(a.p, u.p, "p@" + z);
                Assert.AreEqual(a.rho, u.rho, "rho@" + z);
                Assert.AreEqual(a.a, u.a, "a@" + z);
                double drag = Aero.DragForce(a.rho, UpperAtmosphere.DragSpeed, a.a, UpperAtmosphere.DragCd0, UpperAtmosphere.DragArea);
                Assert.AreEqual(drag, UpperAtmosphere.Drag(z), "drag@" + z);
            }
        }

        [Test]
        public void StarsComeFromTheShippedBsc5pCatalog()
        {
            var csv = File.ReadAllText(Path.Combine(Golden.Root, "data", "astro", "bsc5p-compact.csv"));
            var stars = StarCatalog.Parse(csv, maxCount: StarCatalog.BudgetForTier("high"));
            Assert.AreEqual(9000, stars.Count);
            Assert.LessOrEqual(stars[0].VMag, stars[stars.Count - 1].VMag);
        }

        [Test]
        public void KarmanEvent_IsTheFirstHundredKilometres()
        {
            var cat = Golden.Catalog;
            var heavy = cat.Preset("super-heavy");
            var tl = TimelineGenerator.Generate(cat, heavy, Pad.For(cat, heavy), new Mission { mode = "standard" }, 1);
            Assert.IsTrue(tl.checks.ok, string.Join(" | ", tl.checks.errors));
            MissionEvent karman = null;
            foreach (var e in tl.events) if (e.id == "asc.karman") karman = e;
            Assert.IsNotNull(karman, "orbital vehicle reaches 100 km");
            Assert.AreEqual("simCondition", karman.trigger.kind);
            Assert.AreEqual("alt>=100000", karman.trigger.expr);
            Assert.AreEqual("ascent", karman.phase);
            Assert.AreEqual("kid", karman.visibility);
            Assert.Greater(karman.nominal, 0);
            Assert.AreEqual("karman", karman.links.narration[0]);
            int seen = 0;
            foreach (var e in tl.events) if (e.id == "asc.karman") seen++;
            Assert.AreEqual(1, seen);

            var solid = Golden.Fixture(Golden.SolidSoundingFixture);
            var solidTl = TimelineGenerator.Generate(cat, solid, Pad.For(cat, solid), new Mission { mode = "standard" }, 1);
            Assert.IsTrue(solidTl.checks.ok, string.Join(" | ", solidTl.checks.errors));
            bool present = false;
            foreach (var e in solidTl.events) if (e.id == "asc.karman") present = true;
            if (!present)
            {
                bool explained = false;
                foreach (var a in solidTl.absent) if (a.id == "asc.karman" && !string.IsNullOrEmpty(a.explainWhy)) explained = true;
                Assert.IsTrue(explained, "a sounding rocket that stays below 100 km must explain the absence");
            }

            var stages = Headless.MediumLifter();
            var sim = Headless.Simulate(stages, new SimOptions { payload = 10000, targetAlt = 200e3, dt = 0.5, tMax = 2000 });
            Assert.IsTrue(sim.KarmanTime.HasValue, "medium lifter crosses 100 km");
            Assert.Greater(sim.KarmanTime.Value, 0);
            foreach (var ev in sim.events) Assert.AreNotEqual("karman", ev.type);
        }

        }
}
