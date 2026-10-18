// Fuel family: flame colours from VISUAL_SPEC §3.4, and thrust/Isp only when the
// loaded propellant is not the one the engine was tabulated for.
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class PropellantFlightTests
    {
        static Propellant P(string id, double ispVac, double bulk)
        {
            return new Propellant { id = id, ispVac = ispVac, ispSL = ispVac * 0.9, bulkDensity = bulk, storability = id != null && id.IndexOf("solid", System.StringComparison.Ordinal) >= 0 ? "solid" : "cryogenic" };
        }

        [Test]
        public void KeroloxMatchesTheShippedPlume()
        {
            var f = PropellantFlight.Flame("kerolox");
            Assert.AreEqual(FuelFamily.Kerolox, f.Family);
            Assert.AreEqual(1, f.LengthScale, 1e-12);
            Assert.AreEqual(1, f.WidthScale, 1e-12);
            Assert.AreEqual(1, f.AlphaScale, 1e-12);
            Assert.AreEqual(0.35, f.Soot, 1e-12);
            AssertRgb(f.CoreR, f.CoreG, f.CoreB, 0xfff4d8);
            AssertRgb(f.MidR, f.MidG, f.MidB, 0xffb040);
            AssertRgb(f.TailR, f.TailG, f.TailB, 0xff6a1a);
            AssertRgb(f.HeadR, f.HeadG, f.HeadB, 0xffd27a);
        }

        [Test]
        public void FamiliesCarryTheirOwnFlame()
        {
            var meth = PropellantFlight.Flame("methalox");
            Assert.AreEqual(FuelFamily.Methalox, meth.Family);
            AssertRgb(meth.CoreR, meth.CoreG, meth.CoreB, 0xe8f0ff);
            Assert.That(meth.LengthScale, Is.GreaterThan(1));
            Assert.That(meth.WidthScale, Is.LessThan(1));

            var hydro = PropellantFlight.Flame("hydrolox");
            Assert.AreEqual(FuelFamily.Hydrolox, hydro.Family);
            Assert.AreEqual(0.40, hydro.AlphaScale, 1e-12);
            Assert.AreEqual(0, hydro.Soot, 1e-12);
            AssertRgb(hydro.TailR, hydro.TailG, hydro.TailB, 0x80a0ff);
            Assert.That(hydro.LengthScale, Is.GreaterThan(meth.LengthScale));

            var solid = PropellantFlight.Flame("solid-apcp");
            Assert.AreEqual(FuelFamily.Solid, solid.Family);
            Assert.AreEqual(0.60, solid.Soot, 1e-12);
            Assert.That(solid.WidthScale, Is.GreaterThan(1.2));
            Assert.That(solid.LengthScale, Is.LessThan(1));

            Assert.AreEqual(FuelFamily.Hypergol, PropellantFlight.Flame("mmh-nto").Family);
            Assert.AreEqual(FuelFamily.Hypergol, PropellantFlight.Flame("udmh-nto").Family);
            Assert.AreEqual(FuelFamily.Hybrid, PropellantFlight.Flame("hybrid").Family);
            Assert.AreEqual(FuelFamily.Kerolox, PropellantFlight.Flame(null).Family);
        }

        [Test]
        public void MatchedPropellantLeavesTheBellAlone()
        {
            var kero = P("kerolox", 311, 1030);
            var bell = PropellantFlight.Scale(kero, kero, 760, 845, 282, 311);
            Assert.IsFalse(bell.Scaled);
            Assert.AreEqual(845, bell.ThrustVac, 1e-12);
            Assert.AreEqual(311, bell.IspVac, 1e-12);
            Assert.AreEqual(760, bell.ThrustSL, 1e-12);
        }

        [Test]
        public void ADifferentLoadScalesThrustAndIspAtConstantMassFlow()
        {
            var loaded = P("hydrolox", 451, 360);
            var native = P("kerolox", 311, 1030);
            var bell = PropellantFlight.Scale(loaded, native, 760, 845, 282, 311);
            double k = 451.0 / 311.0;
            Assert.IsTrue(bell.Scaled);
            Assert.AreEqual(845 * k, bell.ThrustVac, 1e-9);
            Assert.AreEqual(760 * k, bell.ThrustSL, 1e-9);
            Assert.AreEqual(311 * k, bell.IspVac, 1e-9);
            Assert.AreEqual(282 * k, bell.IspSL, 1e-9);
            Assert.AreEqual(1, PropellantFlight.StructureScale(native), 1e-12);
            Assert.That(PropellantFlight.StructureScale(loaded), Is.EqualTo(System.Math.Sqrt(1030.0 / 360.0)).Within(1e-12));
            Assert.AreEqual(1, PropellantFlight.StructureScale(P("solid-apcp", 268, 1800)), 1e-12);
        }

        [Test]
        public void StackKeepsAMatchedBellAndScalesASwap()
        {
            var c = Golden.Catalog;
            Design seed = null;
            for (int i = 0; i < c.Presets.Count; i++)
            {
                var p = c.Presets[i];
                if (p != null && p.stages != null && p.stages.Count > 0 && !string.IsNullOrEmpty(p.stages[0].engine))
                {
                    seed = p;
                    break;
                }
            }
            Assert.IsNotNull(seed, "a preset with a first stage");
            var design = seed.Clone();
            var before = Stack.ToSimStages(c, design);
            var again = Stack.ToSimStages(c, design);
            var baseCore = Core0(before);
            var copyCore = Core0(again);
            Assert.AreEqual(baseCore.thrustVac, copyCore.thrustVac, 1e-9);
            Assert.AreEqual(baseCore.ispVac, copyCore.ispVac, 1e-9);

            string engineId = design.stages[0].engine;
            Assert.IsTrue(c.EngineById.TryGetValue(engineId, out var eng));
            Assert.IsTrue(c.PropellantById.TryGetValue(eng.propellant ?? "", out var native));
            string swap = native.id == "hydrolox" ? "methalox" : "hydrolox";
            Assert.IsTrue(c.PropellantById.TryGetValue(swap, out var loaded));
            design.stages[0].propellant = swap;
            var after = Stack.ToSimStages(c, design);
            var swapped = Core0(after);
            double k = loaded.ispVac / native.ispVac;
            Assert.AreEqual(baseCore.thrustVac * k, swapped.thrustVac, 1e-6);
            Assert.AreEqual(baseCore.thrustSL * k, swapped.thrustSL, 1e-6);
            Assert.AreEqual(baseCore.ispVac * k, swapped.ispVac, 1e-6);
            Assert.AreEqual(baseCore.ispSL * k, swapped.ispSL, 1e-6);
            var tank = design.stages[0].tank;
            bool customTank = tank != null && tank.Any;
            if (!customTank && loaded.storability != "solid" && native.storability != "solid" && loaded.bulkDensity < native.bulkDensity)
                Assert.That(swapped.dryMass, Is.GreaterThan(baseCore.dryMass), "a lighter propellant lengthens the tank");
        }

        static StackEntry Core0(System.Collections.Generic.List<StackEntry> list)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].role == "core" && list[i].index == 0) return list[i];
            Assert.Fail("missing core 0");
            return null;
        }

        static void AssertRgb(double r, double g, double b, int hex)
        {
            Assert.AreEqual(((hex >> 16) & 255) / 255.0, r, 1e-9);
            Assert.AreEqual(((hex >> 8) & 255) / 255.0, g, 1e-9);
            Assert.AreEqual((hex & 255) / 255.0, b, 1e-9);
        }
    }
}
