// UX fun review 2026-11-03 #1 / QA #417: Fix it soft-lock. A host attached after the flight must not re-open the
// last card or replay the break-up; every route back lands in the designer within the grace period.
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class ReportGateTests
    {
        [Test]
        public void HostAttachedAfterTheFlightIgnoresTheResultStillOnTheDriver()
        {
            var last = new SimResult { outcome = "crash" };
            var closed = ReportGate.Stale(last, running: false);
            Assert.AreSame(last, closed);
            Assert.IsFalse(ReportGate.Opens(last, reported: false, closed), "Fix it rebuilt the rocket and re-reported the crash");
        }

        [Test]
        public void NextFlightOpensItsOwnCard()
        {
            var last = new SimResult { outcome = "crash" };
            var closed = ReportGate.Stale(last, running: false);
            var next = new SimResult { outcome = "orbit" };
            Assert.IsTrue(ReportGate.Opens(next, reported: false, closed));
            Assert.IsFalse(ReportGate.Opens(next, reported: true, closed), "one card per flight");
            Assert.IsFalse(ReportGate.Opens(null, reported: false, closed));
        }

        [Test]
        public void HostAttachedMidFlightIgnoresNothing()
        {
            Assert.IsNull(ReportGate.Stale(new SimResult(), running: true));
            Assert.IsNull(ReportGate.Stale(null, running: false));
            var first = new SimResult();
            Assert.IsTrue(ReportGate.Opens(first, reported: false, ReportGate.Stale(null, running: false)));
        }

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
