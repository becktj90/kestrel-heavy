// #344 (Kestrel Physics): unpressurized stages get a q·alpha bending check. Before this, a stage without a
// tank pressure had fAllow 0, so the margin check never ran and wind alpha could only break it through the
// tumble rule (alpha > 15 deg and q > 20 kPa); peakBendFraction was exactly 0 for every board starter. Now
// the allowable is min(yield, SP-8007 buckling) x 2*pi*r*t / SF, the axial load is the mass above the section
// (a strap-on carries only itself; the core is checked too during the strap-on phase), and a pure axial
// overload is its own failure kind instead of structural-maxq.
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class QAlphaUnpressurizedTests
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

        Design Starter(int index)
        {
            var m = campaign.Mission(index);
            var d = Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, m).Design);
            if (index == 8) d.safetyFactor = 1.8;
            return d;
        }

        [Test]
        public void UnpressurizedFAllow_IsBucklingGoverned_AndNonzero()
        {
            // The m10 core is titanium, unpressurized, 3.6 m dia, SF 1.8. Its thin wall buckles (214.8 MPa) well
            // before it yields (880 MPa), so the unpressurized allowable is the buckling value, not the yield one,
            // and it is nonzero (before #344 it was 0 and the bending check never ran).
            var d = Starter(10);
            var section = StructureModel.Section(catalog, d.stages[0], d.safetyFactor);
            double e = TankModel.ElasticGPa(d.stages[0].material ?? "");
            double fAllow = StructureModel.UnpressurizedFAllowN(section, e);
            double yieldOnly = section.yieldMPa * 1e6 * 2 * Math.PI * section.radiusM * section.wallM / d.safetyFactor;
            double buckling = StructureModel.BucklingStressPa(e, section.wallM, section.radiusM)
                * 2 * Math.PI * section.radiusM * section.wallM / d.safetyFactor;
            Assert.That(fAllow, Is.GreaterThan(0), "an unpressurized stage now has a bending allowable");
            Assert.AreEqual(buckling, fAllow, 1.0, "buckling governs (thin wall, SP-8007 knockdown 0.33)");
            Assert.That(fAllow, Is.LessThan(yieldOnly), "buckling is well below yield for this wall");
        }

        [Test]
        public void StrapOnMissions_DoNotBreakUpAtLiftoff_AndReachOrbit()
        {
            // m10 and m19 are the strap-on missions. Before #344 the failure monitor checked one strap-on section
            // against the whole-stack axial load; that is the load-path bug this PR fixes (a strap-on carries only
            // itself, and the core is checked against the mass above it). Both must fly to orbit in the seed-1
            // briefing wind with no liftoff breakup.
            foreach (var (index, apo, peri) in new[] { (10, 203.7, 196.3), (19, 205.1, 194.9) })
            {
                var m = campaign.Mission(index);
                var r = LivePadLoop.Fly(catalog, m, Starter(index), true);
                Assert.IsNull(r.failure, LivePadLoop.Text(r));
                Assert.AreEqual("orbit", r.outcome, LivePadLoop.Text(r));
                Assert.AreEqual(apo, r.orbit.apoAlt / 1000, 0.5, LivePadLoop.Text(r));
                Assert.AreEqual(peri, r.orbit.periAlt / 1000, 0.5, LivePadLoop.Text(r));
            }
        }

        [Test]
        public void UnpressurizedStarters_BendingCheckEngages_InSeedOneWind()
        {
            // The q·alpha bending share is no longer exactly 0 for unpressurized starters: the check runs, the
            // fraction is positive, and the flights still reach orbit (the warning threshold is 80%).
            foreach (var index in new[] { 3, 5, 8 })
            {
                var m = campaign.Mission(index);
                var r = LivePadLoop.Fly(catalog, m, Starter(index), true);
                Assert.IsNull(r.failure, LivePadLoop.Text(r));
                Assert.AreEqual("orbit", r.outcome, LivePadLoop.Text(r));
                Assert.That(r.peakBendFraction, Is.GreaterThan(0.1), m.Id + " bending now engages");
                Assert.That(r.peakBendFraction, Is.LessThan(FailureMonitor.BendWarnFraction), m.Id + " stays under the warning");
                Assert.IsNull(r.bendWarning, m.Id);
            }
        }

        [Test]
        public void AxialOverload_IsItsOwnFailureKind()
        {
            // A pure axial overload (axial load alone past the wall's allowable, no bending: q small, alpha 0) is
            // reported as axial-overload, not structural-maxq. Drive the m10 core's monitor directly with a
            // 500 t mass above it at 50 m/s^2: axial 25 MN against a 13.9 MN buckling allowable.
            var d = Starter(10);
            var monitor = FailureMonitor.Create(catalog, d, 400000);
            var step = new FailureMonitor.Step
            {
                t = 10, dt = 0.05, alt = 10000, qPa = 1000, accel = 50, alphaRad = 0,
                mass = 800000, area = 10.18, cd = 0.3, stageIndex = 1, propFraction = 1,
                thrusting = true, targetAlt = 400000, massAboveKg = 500000,
            };
            bool failed = monitor.Consider(step);
            Assert.IsTrue(failed, "the axial overload trips the monitor");
            Assert.IsNotNull(monitor.Event);
            Assert.AreEqual("axial-overload", monitor.Event.kind);
        }
    }
}
