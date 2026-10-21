// Nav-harness ids for screen-flight and screen-pause: kebab-case, unique, prefixed by their screen.
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class UiIdsFlightTests
    {
        static readonly Regex Kebab = new Regex("^[a-z0-9]+(-[a-z0-9]+)*$");

        [Test]
        public void FlightAndPauseIdsFollowTheHarnessConvention()
        {
            Assert.That(UiIds.ScreenFlight, Is.EqualTo("screen-flight"));
            Assert.That(UiIds.ScreenPause, Is.EqualTo("screen-pause"));
            var seen = new HashSet<string>();
            foreach (var id in UiIds.FlightControls)
            {
                Assert.That(Kebab.IsMatch(id), Is.True, id + " is kebab-case");
                Assert.That(id.StartsWith("flight-") || id.StartsWith("pause-"), Is.True, id + " is <screen>-<control>");
                Assert.That(seen.Add(id), Is.True, id + " is unique");
            }
            foreach (var m in FlightView.Picker)
            {
                string chip = UiIds.FlightCameraChip(m);
                Assert.That(chip, Is.EqualTo(UiIds.FlightCamera + "-" + FlightView.Name(m).ToLowerInvariant()));
                Assert.That(seen.Contains(chip), Is.True, chip);
            }
            Assert.That(UiIds.FlightHealth(2), Is.EqualTo("flight-health-2"));
        }
    }
}
