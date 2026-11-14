// BOOSTER LOST kid lines: Starter never names Recovery (it has no such tab). Full designer keeps the
// Recovery tip — picking Haven there now arms legs and landing fuel (HangarEditor.SetRecoveryMode).
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class BoosterLandingWhyTests
    {
        static RecoveryCraft Craft(double reserveKg, bool legs, bool fins = false) =>
            new RecoveryCraft { Mode = "expendable", ReserveKg = reserveKg, Legs = legs, GridFins = fins, DryKg = 25000 };

        [Test]
        public void Why_Starter_NeverNamesRecovery_AndEndsWithStarterLandingFix()
        {
            BoosterLanding.Why(Craft(0, false), true, out string kid, out _);
            StringAssert.Contains("no landing fuel", kid);
            StringAssert.Contains("no legs", kid);
            StringAssert.Contains("falls into the sea", kid);
            StringAssert.DoesNotContain("Recovery", kid);
            StringAssert.EndsWith(BoosterLanding.StarterLandingFix, kid);
            Assert.That(TrajectoryWatch.Plain(kid), Is.True, kid);
        }

        [Test]
        public void Why_FullDesigner_NamesRecovery()
        {
            BoosterLanding.Why(Craft(0, false), false, out string kid, out _);
            StringAssert.Contains("in Recovery", kid);
            StringAssert.Contains("landing fuel and legs", kid);
        }

        [Test]
        public void Why_Starter_FuelWithoutLegs_DoesNotNameRecovery()
        {
            BoosterLanding.Why(Craft(5000, false), true, out string kid, out _);
            StringAssert.Contains("no legs", kid);
            StringAssert.DoesNotContain("Recovery", kid);
            StringAssert.EndsWith(BoosterLanding.StarterLandingFix, kid);
        }

        [Test]
        public void Why_Starter_LegsWithoutFuel_DoesNotNameRecovery()
        {
            BoosterLanding.Why(Craft(0, true), true, out string kid, out _);
            StringAssert.Contains("no landing fuel", kid);
            StringAssert.DoesNotContain("Recovery", kid);
            StringAssert.EndsWith(BoosterLanding.StarterLandingFix, kid);
        }
    }
}
