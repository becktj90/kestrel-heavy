// The flight pad must follow Pad.For for the designed vehicle: first-stage diameter picks
// rail vs tower, and liquid + tower gets the strongback. Mesh half-width (strap-ons) must not
// promote a thin core onto a tower pad.
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using Kestrel.Pad;
using Kestrel.Slice;
using NUnit.Framework;
using TimelinePad = Kestrel.Core.Timeline.Pad;

namespace Kestrel.Game.Tests
{
    public class PadArchetypeForDesignTests
    {
        static string Repo => Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
        static Catalog Cat() => Catalog.LoadFromRepo(Repo);

        static Design Template(string file) =>
            Design.FromJson(MiniJson.ParseObject(File.ReadAllText(Path.Combine(Repo, "web", "js", "data", "templates", file))));

        [Test]
        public void ArchetypeForMatchesPadForOnTemplates()
        {
            var cat = Cat();
            foreach (var id in new[] { "stock", "super-heavy" })
            {
                var d = cat.Preset(id);
                // TimelinePad: bare Pad binds to namespace Kestrel.Pad when both usings are present.
                var pad = TimelinePad.For(cat, d);
                Assert.That(PadTower.ArchetypeFor(cat, d), Is.EqualTo(PadGenerator.ArchetypeFor(pad.id, pad.strongback)), id);
            }
            foreach (var file in new[] { "sounding-hybrid.json", "medium-kerolox-reusable.json", "hydrogen-upper.json" })
            {
                var d = Template(file);
                var pad = TimelinePad.For(cat, d);
                Assert.That(PadTower.ArchetypeFor(cat, d), Is.EqualTo(PadGenerator.ArchetypeFor(pad.id, pad.strongback)), file);
            }
        }

        [Test]
        public void ThinCoreWithWideStrapOnsStaysOnRail()
        {
            var cat = Cat();
            // First stage under 3 m ⇒ pad.rail even when strap-ons make the mesh wider than 3 m.
            var d = new Design
            {
                id = "thin-core-straps",
                stages = new List<StagePart>
                {
                    new StagePart { diameter = 2.2, propellant = "kerolox", propMass = 8000, engineCount = 1 },
                },
                strapOns = new StagePart { diameter = 1.6, propellant = "solid-apcp", propMass = 4000, engineCount = 1, count = 2 },
            };
            var pad = TimelinePad.For(cat, d);
            Assert.That(pad.id, Is.EqualTo("pad.rail"));
            Assert.That(pad.strongback, Is.False);
            Assert.That(PadTower.ArchetypeFor(cat, d), Is.EqualTo(PadArchetype.Rail));
            // The old mesh-radius rule would have picked Tower (half-width ≈ 2.2/2 + 1.6 > 1.5).
            float meshHalf = (float)(d.stages[0].diameter * 0.5 + d.strapOns.diameter);
            Assert.That(PadTower.DefaultArchetype(meshHalf), Is.EqualTo(PadArchetype.Tower), "sanity: mesh rule disagrees");
        }

        [Test]
        public void LiquidTowerGetsStrongbackArchetype()
        {
            var cat = Cat();
            var d = cat.Preset("super-heavy");
            Assert.That(TimelinePad.For(cat, d).strongback, Is.True);
            Assert.That(PadTower.ArchetypeFor(cat, d), Is.EqualTo(PadArchetype.TowerStrongback));
            var reusable = Template("medium-kerolox-reusable.json");
            Assert.That(reusable.stages[0].diameter, Is.GreaterThanOrEqualTo(3.0));
            Assert.That(PadTower.ArchetypeFor(cat, reusable), Is.EqualTo(PadArchetype.TowerStrongback));
        }

        [Test]
        public void SmallSolidStaysOnRail()
        {
            var cat = Cat();
            var d = Design.FromJson(MiniJson.ParseObject(File.ReadAllText(Path.Combine(Repo, "tests", "fixtures", "designs", "sounding-solid.json"))));
            Assert.That(TimelinePad.For(cat, d).id, Is.EqualTo("pad.rail"));
            Assert.That(PadTower.ArchetypeFor(cat, d), Is.EqualTo(PadArchetype.Rail));
        }
    }
}
