// Fun review item 10, designer half: Kc in the strip, one row of tab chips, and the gear clear of the web FULL/EXIT.
using System.Linq;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class DesignerKcAndCornerTests
    {
        [Test]
        public void DefaultMinorPerKcMatchesTheShippedCatalog()
        {
            var catalog = Catalog.LoadFromRepo(Golden.Root);
            Assert.AreEqual(CostModel.MinorPerKc(catalog), CostModel.DefaultMinorPerKc);
        }

        [Test]
        public void FormatKcOverloadMatchesTheCatalogForm()
        {
            var catalog = Catalog.LoadFromRepo(Golden.Root);
            foreach (long minor in new long[] { 0, 85_000_000, 516_710_000, 1_660_781_357, 123_000_000_000 })
                Assert.AreEqual(CostModel.FormatKc(catalog, minor), CostModel.FormatKc(minor, CostModel.MinorPerKc(catalog)));
            Assert.AreEqual("5.2k Kc", CostModel.FormatKc(516_710_000, 100_000));
            Assert.AreEqual("850 Kc", CostModel.FormatKc(85_000_000, 0), "a bad rate falls back to the shipped one");
        }

        [Test]
        public void RailDropsStepsTheTabRowAlreadyShows()
        {
            string[] tabbed = { "Engines", "Tanks", "Stages", "Landing", "Look" };
            var rail = DesignerCoach.RailSteps(s => tabbed.Contains(s));
            CollectionAssert.AreEqual(new[] { "Mission", "Launch" }, rail);
            foreach (var t in tabbed) CollectionAssert.DoesNotContain(rail, t);
        }

        [Test]
        public void RailKeepsLaunchEvenWhenEveryStepHasATab()
        {
            CollectionAssert.AreEqual(new[] { "Launch" }, DesignerCoach.RailSteps(s => true));
            CollectionAssert.AreEqual(DesignerCoach.Flow, DesignerCoach.RailSteps(null));
        }

        [TestCase(390f, 844f)]
        [TestCase(335f, 700f)]
        [TestCase(440f, 956f)]
        [TestCase(1440f, 900f)]
        [TestCase(1280f, 800f)]
        public void GearStaysClearOfFullExitAtAnyPanelScale(float w, float h)
        {
            var frame = WebHomeLayout.Place(w, h, 0f, 0f, 0f, 0f);
            foreach (float u in new[] { 0.6f, 0.8f, 0.88f, 1f, 1.25f, 2f })
            {
                var g = WebHomeLayout.GearInPanel(frame.Gear, u, u);
                // Back to CSS px, where the page button lives.
                var css = new HudBox { X = g.X / u, Y = g.Y / u, W = g.W / u, H = g.H / u };
                Assert.That(css.Overlaps(frame.Exit), Is.False, w + "x" + h + " at " + u);
                Assert.That(css.Right, Is.LessThanOrEqualTo(w - HomeCornerLayout.Edge + 0.01f), "gear inside the screen edge");
                Assert.That(g.W, Is.GreaterThanOrEqualTo(HomeCornerLayout.Gear - 0.01f), "44 pt touch target");
            }
        }
    }
}
