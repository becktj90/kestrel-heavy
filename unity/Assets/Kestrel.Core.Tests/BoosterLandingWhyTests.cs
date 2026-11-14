// BOOSTER LOST kid lines must not name a missing Recovery tab; Starter tips point at the Landing row.
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class BoosterLandingWhyTests
    {
        static RecoveryCraft Craft(double reserveKg, bool legs, bool fins = false) =>
            new RecoveryCraft { Mode = "expendable", ReserveKg = reserveKg, Legs = legs, GridFins = fins, DryKg = 25000 };

        [Test]
        public void Why_NoFuelNoLegs_IsFactual_AndDoesNotNameRecovery()
        {
            BoosterLanding.Why(Craft(0, false), false, out string kid, out _);
            StringAssert.Contains("no landing fuel", kid);
            StringAssert.Contains("no legs", kid);
            StringAssert.Contains("falls into the sea", kid);
            StringAssert.DoesNotContain("Recovery", kid);
            Assert.That(TrajectoryWatch.Plain(kid), Is.True, kid);
        }

        [Test]
        public void Why_FuelWithoutLegs_DoesNotNameRecovery()
        {
            BoosterLanding.Why(Craft(5000, false), false, out string kid, out _);
            StringAssert.Contains("no landing legs", kid);
            StringAssert.DoesNotContain("Recovery", kid);
        }

        [Test]
        public void Why_LegsWithoutFuel_DoesNotNameRecovery()
        {
            BoosterLanding.Why(Craft(0, true), false, out string kid, out _);
            StringAssert.Contains("no landing fuel", kid);
            StringAssert.DoesNotContain("Recovery", kid);
        }

        [Test]
        public void Why_FuelAndLegsButExpendable_DoesNotNameRecovery()
        {
            BoosterLanding.Why(Craft(5000, true, true), false, out string kid, out _);
            StringAssert.Contains("No landing was picked", kid);
            StringAssert.DoesNotContain("Recovery", kid);
        }
    }
}
