using Kestrel.Kit;
using NUnit.Framework;

namespace Kestrel.Kit.Tests
{
    public class FlightSinkMathTests
    {
        [Test] public void EnergyIsClampedAndMonotone()
        {
            Assert.That(FlightSinkMath.Energy01(0), Is.EqualTo(0f));
            Assert.That(FlightSinkMath.Energy01(1e12), Is.EqualTo(1f));
            Assert.That(FlightSinkMath.Energy01(double.NaN), Is.EqualTo(0f));
            Assert.That(FlightSinkMath.Energy01(5e6), Is.LessThan(FlightSinkMath.Energy01(1e7)));
            Assert.That(FlightSinkMath.CrashEnergy01(0), Is.EqualTo(0.15f));
        }

        [Test] public void ImpactAndMaxQAndTouchdownScale()
        {
            Assert.That(FlightSinkMath.ImpactEnergy01(30), Is.LessThan(FlightSinkMath.ImpactEnergy01(100)));
            Assert.That(FlightSinkMath.ImpactEnergy01(500), Is.EqualTo(1f));
            Assert.That(FlightSinkMath.MaxQ01(10000), Is.LessThan(FlightSinkMath.MaxQ01(35000)));
            Assert.That(FlightSinkMath.Touchdown01(0.5, 0), Is.LessThan(FlightSinkMath.Touchdown01(5, 10)));
            Assert.That(FlightSinkMath.Touchdown01(-50, 90), Is.EqualTo(1f));
        }

        [Test] public void TouchdownRingNeedsSolidGroundAndAHardLanding()
        {
            Assert.That(FlightSinkMath.TouchdownShockwave01(1.0, true), Is.EqualTo(0f));
            Assert.That(FlightSinkMath.TouchdownShockwave01(6.0, false), Is.EqualTo(0f));
            Assert.That(FlightSinkMath.TouchdownShockwave01(6.0, true), Is.GreaterThan(0f));
        }

        [Test] public void SinkFlagIsRefCounted()
        {
            CinematicEvents.ResetSinkState();
            Assert.That(CinematicEvents.FlightSinkActive, Is.False);
            CinematicEvents.SinkAttached(); CinematicEvents.SinkAttached();
            CinematicEvents.SinkDetached();
            Assert.That(CinematicEvents.FlightSinkActive, Is.True);
            CinematicEvents.SinkDetached(); CinematicEvents.SinkDetached();
            Assert.That(CinematicEvents.FlightSinkActive, Is.False);
        }

        [Test] public void LaterStagesFlashSmaller()
        {
            Assert.That(FlightSinkMath.StageScale01(1), Is.GreaterThan(FlightSinkMath.StageScale01(2)));
            Assert.That(FlightSinkMath.StageScale01(2), Is.GreaterThan(FlightSinkMath.StageScale01(3)));
        }
    }
}
