// #338 (Kestrel Physics): a warning-only structural-margin note. FailureMonitor.Consider already folds the wind's
// q·alpha bending into the margin of safety (bend = q·A·CNα·α·|xcp − xcg|, turned into an equivalent axial load) for a
// stage with a set tank pressure; a breakup is ms < 0. The monitor now also records the first moment that bending uses
// 80% of what the walls can still take after thrust and drag (StructuralWarning). It never touches the throttle, the
// pitch or the outcome. Stages without a tank pressure have no bending allowance (fAllow 0) and break on q against q_lim
// only, so they never warn: every board starter is one of those.
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class BendWarningTests
    {
        Catalog catalog;
        HangarData hangar;
        Campaign campaign;

        [OneTimeSetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
        }

        /// <summary>The auto-built starter for a mission, every stage's tanks at <paramref name="bar"/>, at strength <paramref name="sf"/>.</summary>
        Design Pressurized(int index, double sf, double bar)
        {
            var d = Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, campaign.Mission(index)).Design);
            d.safetyFactor = sf;
            foreach (var s in d.stages)
            {
                if (s.tank == null) s.tank = new TankParams();
                s.tank.pressureBar = bar;
                s.tank.pressureSet = true;
            }
            return d;
        }

        [Test]
        public void UnpressurizedStarters_HaveNoBendingAllowance_SoNeverWarn()
        {
            for (int index = 3; index <= 20; index++)
            {
                var m = campaign.Mission(index);
                var d = Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, m).Design);
                var r = LivePadLoop.Fly(catalog, m, d, true);
                Assert.AreEqual(0.0, r.peakBendFraction, 0.0, m.Id + " (q against q_lim only)");
                Assert.IsNull(r.bendWarning, m.Id);
            }
        }

        [Test]
        public void Kh8PressurizedAtStrength1Point2_WarnsInTheSeedOneWind_AndStillReachesOrbit()
        {
            // KH-8's auto-built rocket with 0.5 bar tanks at strength 1.2. Seed-1 wind: the bending peaks at 89.6% of the
            // allowance, the warning is set, and the flight carries on to orbit (629.7 x 170.3 km) with no failure:
            // the note changes nothing. Calm air: 7.2%, no warning.
            var m = campaign.Mission(8);
            var d = Pressurized(8, 1.2, 0.5);
            var windy = LivePadLoop.Fly(catalog, m, d, true);
            Assert.IsNull(windy.failure, LivePadLoop.Text(windy));
            Assert.AreEqual("orbit", windy.outcome, LivePadLoop.Text(windy));
            Assert.AreEqual(629.7, windy.orbit.apoAlt / 1000, 0.5, LivePadLoop.Text(windy));
            Assert.AreEqual(170.3, windy.orbit.periAlt / 1000, 0.5, LivePadLoop.Text(windy));
            Assert.AreEqual(0.896, windy.peakBendFraction, 0.001);
            var w = windy.bendWarning;
            Assert.IsNotNull(w, "the wind passes 80% of the bending allowance");
            Assert.That(w.fraction, Is.InRange(FailureMonitor.BendWarnFraction, 1.0));
            Assert.That(w.alphaDeg, Is.GreaterThan(0));
            Assert.AreEqual(1, w.stage);
            StringAssert.StartsWith("Close call at T+", w.kid);
            StringAssert.Contains("of the bend your tanks can take. It held.", w.kid);
            StringAssert.StartsWith("q·alpha ", w.engineer);
            StringAssert.Contains("Warning at 80%, breakup at 100%.", w.engineer);

            var calm = LivePadLoop.Fly(catalog, m, d, false);
            Assert.AreEqual(0.072, calm.peakBendFraction, 0.001);
            Assert.IsNull(calm.bendWarning, "calm air stays under 80%");
        }

        [Test]
        public void Kh4PressurizedAtStrength1Point6_WarnsBeforeTheWindBreaksIt()
        {
            // KH-4's auto-built rocket with 0.5 bar tanks at strength 1.6 breaks up in the seed-1 wind (the margin of
            // safety goes below zero). The warning came first, below 100%.
            var m = campaign.Mission(4);
            var d = Pressurized(4, 1.6, 0.5);
            var windy = LivePadLoop.Fly(catalog, m, d, true);
            Assert.IsNotNull(windy.failure, LivePadLoop.Text(windy));
            Assert.AreEqual("structural-maxq", windy.failure.kind);
            var w = windy.bendWarning;
            Assert.IsNotNull(w);
            Assert.That(w.fraction, Is.InRange(FailureMonitor.BendWarnFraction, 1.0));
            Assert.That(w.t, Is.LessThan(windy.failure.t), "warned before the breakup");
        }

        [Test]
        public void TheWarningIsDeterministic()
        {
            // Same design, seed and wind: the same note at the same moment, and the same orbit.
            var m = campaign.Mission(8);
            var d = Pressurized(8, 1.2, 0.5);
            var a = LivePadLoop.Fly(catalog, m, d, true);
            var b = LivePadLoop.Fly(catalog, m, d, true);
            Assert.AreEqual(a.orbit.apoAlt, b.orbit.apoAlt);
            Assert.AreEqual(a.bendWarning.t, b.bendWarning.t);
            Assert.AreEqual(a.bendWarning.fraction, b.bendWarning.fraction);
            Assert.AreEqual(a.bendWarning.engineer, b.bendWarning.engineer);
        }
    }
}
