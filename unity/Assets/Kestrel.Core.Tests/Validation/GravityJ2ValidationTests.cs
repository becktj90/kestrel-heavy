// PHYSICS_CORE_AUDIT.md §P8c category 3 — Gravity/J2.
// Live checks live in GeopotentialTests (zonal accel, secular Ω̇, SSO, critical i, mean/osc, 10-day numeric).
using Kestrel.Core.Physics;
using NUnit.Framework;

namespace Kestrel.Core.Tests.Validation
{
    public class GravityJ2ValidationTests
    {
        [Test]
        public void ZonalConstantsMatchBodiesAndAuditPrint()
        {
            Assert.AreEqual(1.08262668e-3, Geopotential.J2);
            Assert.AreEqual(-2.53265648e-6, Geopotential.J3);
            Assert.AreEqual(-1.61962159e-6, Geopotential.J4);
        }
    }
}
