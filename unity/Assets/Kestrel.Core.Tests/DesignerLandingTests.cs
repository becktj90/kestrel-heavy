// UX fun review 2026-11-03 #1 / QA #417: Fix it soft-lock. Every route back lands in the designer within the grace
// period (the stale-result re-report itself is ReportLatch, #419, with its own tests).
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class DesignerLandingTests
    {
        [Test]
        public void LandingIsJudgedAtTheEndOfTheGrace()
        {
            var l = new DesignerLanding();
            Assert.IsFalse(l.Check(0f, false, false, false), "unarmed never forces");
            l.Arm(10f);
            Assert.IsTrue(l.Armed);
            Assert.IsFalse(l.Check(10.5f, designerShown: false, reportShown: true, flightShown: false), "still in grace");
            Assert.IsTrue(l.Check(10f + DesignerLanding.GraceSeconds, designerShown: true, reportShown: true, flightShown: false),
                "the card came back over the designer: force it");
            Assert.IsFalse(l.Armed, "judged once");
            Assert.IsFalse(l.Check(20f, false, false, false));
        }

        [Test]
        public void LandingAcceptsTheDesignerOrANewFlight()
        {
            var l = new DesignerLanding();
            l.Arm(0f);
            Assert.IsFalse(l.Check(2f, designerShown: true, reportShown: false, flightShown: false));
            Assert.IsFalse(l.Armed);
            l.Arm(0f);
            Assert.IsFalse(l.Check(0.2f, designerShown: false, reportShown: false, flightShown: true), "relaunched: disarm");
            Assert.IsFalse(l.Armed);
            l.Arm(0f);
            Assert.IsTrue(l.Check(1.5f, designerShown: false, reportShown: false, flightShown: false), "empty sky: force");
        }

        [Test]
        public void LandedTable()
        {
            Assert.IsTrue(DesignerLanding.Landed(true, false, false));
            Assert.IsFalse(DesignerLanding.Landed(true, true, false));
            Assert.IsFalse(DesignerLanding.Landed(false, false, false));
            Assert.IsTrue(DesignerLanding.Landed(false, true, true));
        }
    }
}
