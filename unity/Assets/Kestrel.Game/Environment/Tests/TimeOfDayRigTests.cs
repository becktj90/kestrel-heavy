using Kestrel.Core.Sky;
using Kestrel.Core.Flight;
using Kestrel.Kit;
using NUnit.Framework;

namespace Kestrel.Game.Environment.Tests
{
    public class TimeOfDayRigTests
    {
        const double Lat = SkyClock.LatitudeDeg, Lon = SkyClock.LongitudeDeg;

        [TestCase(LaunchTimeOfDay.Day)] [TestCase(LaunchTimeOfDay.Sunset)] [TestCase(LaunchTimeOfDay.Night)]
        public void SolvedHourLandsNearThePresetElevation(LaunchTimeOfDay t)
        {
            var p = TimeOfDayPresets.For(t, 3);
            double h = TimeOfDayHours.Solve(p, Lat, Lon, SkyClock.Year, SkyClock.Month, SkyClock.Day);
            double e = TimeOfDayHours.ElevationDegAt(h, Lat, Lon, SkyClock.Year, SkyClock.Month, SkyClock.Day);
            if (t == LaunchTimeOfDay.Sunset) Assert.That(e, Is.EqualTo(p.SunElevationDeg).Within(1.0), $"h={h}");
            if (t == LaunchTimeOfDay.Day) Assert.That(e, Is.GreaterThan(10.0), $"h={h}");
            if (t == LaunchTimeOfDay.Night) Assert.That(p.NightFactor, Is.EqualTo(1f)); 
            if (t == LaunchTimeOfDay.Night) Assert.That(e, Is.LessThan(-8.0), $"h={h}");
            Assert.That(h, Is.InRange(0.0, 24.0));
        }

        [Test]
        public void ForcedWinsAndMissionSelectionIsDeterministic()
        {
            try
            {
                TimeOfDayRig.SetMission("KH-7", 42UL); var a = TimeOfDayRig.Current.Tod; var sa = TimeOfDayRig.Current.SunElevationDeg;
                TimeOfDayRig.SetMission("KH-7", 42UL); Assert.That(TimeOfDayRig.Current.Tod, Is.EqualTo(a)); Assert.That(TimeOfDayRig.Current.SunElevationDeg, Is.EqualTo(sa));
                TimeOfDayRig.Force(LaunchTimeOfDay.Night, 2); Assert.That(TimeOfDayRig.Current.Tod, Is.EqualTo(LaunchTimeOfDay.Night));
                TimeOfDayRig.ClearForce(); Assert.That(TimeOfDayRig.Current.Tod, Is.EqualTo(a));
            }
            finally { TimeOfDayRig.ClearForce(); }
        }
    }
}
