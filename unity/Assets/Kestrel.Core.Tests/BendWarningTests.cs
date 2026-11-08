// #338 (Kestrel Physics): a warning-only structural-margin note. FailureMonitor.Consider already folds the wind's
// q·alpha bending into the margin of safety (bend = q·A·CNα·α·|xcp − xcg|, turned into an equivalent axial load) for a
// stage with a set tank pressure; a breakup is ms < 0. The monitor now also records the first moment that bending uses
// 80% of what the walls can still take after thrust and drag (StructuralWarning). It never touches the throttle, the
// pitch or the outcome. #344: stages without a tank pressure now have the same check (allowable = min(yield, SP-8007
// buckling)), so every board starter bends a little and can warn; none reaches 80% in the seed-1 wind.
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
        public void UnpressurizedStarters_NowHaveABendingAllowance_AndStayUnderTheWarning()
        {
            // #344: stages without a tank pressure now have a bending allowance (min(yield, SP-8007 buckling)),
            // so the q·alpha share is positive instead of exactly 0. None of the starters reaches the 80%
            // warning in the seed-1 wind, so no note fires and every flight still reaches orbit.
            for (int index = 3; index <= 20; index++)
            {
                var m = campaign.Mission(index);
                var d = Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, m).Design);
                var r = LivePadLoop.Fly(catalog, m, d, true);
                Assert.That(r.peakBendFraction, Is.GreaterThan(0), m.Id + " (bending now engages)");
                Assert.That(r.peakBendFraction, Is.LessThan(FailureMonitor.BendWarnFraction), m.Id);
                Assert.IsNull(r.bendWarning, m.Id);
            }
        }

        [Test]
        public void Kh8PressurizedAtStrength1Point2_StaysUnderTheWarning_AndStillReachesOrbit()
        {
            // KH-8's auto-built rocket with 0.5 bar tanks at strength 1.2. Seed-1 wind: the bending peaks at 25.8% of the
            // allowance (the #344 alpha cap at 15 deg keeps the linear CN-alpha model from overstating the moment at
            // large alpha; the old 89.6% pin was the uncapped extrapolation, and the warning it set was a false alarm),
            // no warning is set, and the flight carries on to orbit (632.9 x 167.1 km) with no failure.
            // Calm air: 2.3%, no warning. (#335: the fairing mass jettison moved the orbit from 629.7 x 170.3 km.)
            var m = campaign.Mission(8);
            var d = Pressurized(8, 1.2, 0.5);
            var windy = LivePadLoop.Fly(catalog, m, d, true);
            Assert.IsNull(windy.failure, LivePadLoop.Text(windy));
            Assert.AreEqual("orbit", windy.outcome, LivePadLoop.Text(windy));
            Assert.AreEqual(632.9, windy.orbit.apoAlt / 1000, 0.5, LivePadLoop.Text(windy));
            Assert.AreEqual(167.1, windy.orbit.periAlt / 1000, 0.5, LivePadLoop.Text(windy));
            Assert.AreEqual(0.258, windy.peakBendFraction, 0.001);
            Assert.IsNull(windy.bendWarning, "the wind stays under 80% of the bending allowance");

            var calm = LivePadLoop.Fly(catalog, m, d, false);
            Assert.AreEqual(0.023, calm.peakBendFraction, 0.001);
            Assert.IsNull(calm.bendWarning, "calm air stays under 80%");
        }

        [Test]
        public void Kh4PressurizedAtStrength1Point6_ReachesOrbit_WithoutWarning()
        {
            // KH-4's auto-built rocket with 0.5 bar tanks at strength 1.6 in the seed-1 wind. The old pin had it breaking
            // up on the bending margin with a warning first — but that breakup was the uncapped linear CN-alpha model
            // overstating the moment at large alpha. With the #344 15 deg cap the bending peaks at 51.2% of the
            // allowance, no warning is set, and the flight reaches orbit (713.9 x 286.1 km) with no failure.
            // (#335: the fairing mass jettison moved the orbit from 712.7 x 287.3 km.)
            var m = campaign.Mission(4);
            var d = Pressurized(4, 1.6, 0.5);
            var windy = LivePadLoop.Fly(catalog, m, d, true);
            Assert.IsNull(windy.failure, LivePadLoop.Text(windy));
            Assert.AreEqual("orbit", windy.outcome, LivePadLoop.Text(windy));
            Assert.AreEqual(713.9, windy.orbit.apoAlt / 1000, 0.5, LivePadLoop.Text(windy));
            Assert.AreEqual(286.1, windy.orbit.periAlt / 1000, 0.5, LivePadLoop.Text(windy));
            Assert.AreEqual(0.512, windy.peakBendFraction, 0.001);
            Assert.IsNull(windy.bendWarning, "the wind stays under 80% of the bending allowance");
        }

        [Test]
        public void TheFlightIsDeterministic()
        {
            // Same design, seed and wind: the same orbit and the same peak bending share. (The warning this used to
            // compare no longer fires for KH-8 under the #344 alpha cap, so determinism is pinned on the flight itself.)
            var m = campaign.Mission(8);
            var d = Pressurized(8, 1.2, 0.5);
            var a = LivePadLoop.Fly(catalog, m, d, true);
            var b = LivePadLoop.Fly(catalog, m, d, true);
            Assert.AreEqual(a.orbit.apoAlt, b.orbit.apoAlt);
            Assert.AreEqual(a.peakBendFraction, b.peakBendFraction);
            Assert.AreEqual(a.outcome, b.outcome);
        }
    }
}
