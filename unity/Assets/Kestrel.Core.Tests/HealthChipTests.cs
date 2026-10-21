// The Eng / Hull / ... tabs show their real health; an open tab is a ring, not a colour (#332 follow-up).
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class HealthChipTests
    {
        static readonly VehicleHealthTone[] Tones = { VehicleHealthTone.Green, VehicleHealthTone.Amber, VehicleHealthTone.Red };

        [Test]
        public void OpenChangesOnlyTheRingNeverTheHealthColours()
        {
            foreach (var tone in Tones)
            {
                var shut = HealthChip.Look(tone, false);
                var open = HealthChip.Look(tone, true);
                Assert.That(open.Plate, Is.EqualTo(shut.Plate), tone + " plate");
                Assert.That(open.Text, Is.EqualTo(shut.Text), tone + " label");
                Assert.That(open.Edge, Is.EqualTo(shut.Edge), tone + " edge");
                Assert.That(open.RingPt, Is.EqualTo(shut.RingPt), tone + " ring width never moves the label");
                Assert.That(open.Ring, Is.EqualTo(HealthChip.OpenRing), tone + " open ring");
                Assert.That(shut.Ring, Is.EqualTo(shut.Plate), tone + " closed ring is the plate");
                Assert.That(HealthChip.Reddish(open.Ring), Is.False, "open is never shown in the alarm colour");
            }
        }

        [Test]
        public void TheChipColourIsTheRealHealth()
        {
            var green = HealthChip.Look(VehicleHealthTone.Green, true);
            var amber = HealthChip.Look(VehicleHealthTone.Amber, true);
            var red = HealthChip.Look(VehicleHealthTone.Red, false);
            // An open, healthy Eng or Hull: nothing about it is red (TestFlight: it looked red only because it was open).
            Assert.That(HealthChip.Reddish(green.Plate) || HealthChip.Reddish(green.Text) || HealthChip.Reddish(green.Edge), Is.False);
            Assert.That(green.Text.G, Is.GreaterThan(green.Text.R), "green reads green");
            Assert.That(HealthChip.Reddish(red.Text) && HealthChip.Reddish(red.Plate), Is.True, "red reads red, open or not");
            Assert.That(amber.Text.R > amber.Text.B && amber.Text.G > amber.Text.B, Is.True, "amber reads amber");
            Assert.That(green.Plate, Is.Not.EqualTo(red.Plate));
            Assert.That(amber.Plate, Is.Not.EqualTo(red.Plate));

            // The calm reading (what Eng and Hull show on a healthy ascent) is the green look.
            var calm = VehicleHealth.Read(new VehicleHealthInput { Fuel01 = 1, QLimitPa = 1, GLimit = 1, Thrust01 = 1 });
            Assert.That(calm[0].Mark, Is.EqualTo("Eng"));
            Assert.That(calm[2].Mark, Is.EqualTo("Hull"));
            Assert.That(calm[0].Tone, Is.EqualTo(VehicleHealthTone.Green));
            Assert.That(calm[2].Tone, Is.EqualTo(VehicleHealthTone.Green));
        }

        [Test]
        public void EveryLabelIsReadableOnEverySkyAndNothingBlinks()
        {
            foreach (var tone in Tones)
            foreach (bool open in new[] { false, true })
            {
                var look = HealthChip.Look(tone, open);
                Assert.That(CountdownStack.WorstContrast(look.Plate, look.Text), Is.GreaterThanOrEqualTo(4.5), tone + (open ? " open" : ""));
                // Static: the same state always draws the same colours (no pulse or blink, photosensitivity-safe).
                Assert.That(HealthChip.Look(tone, open), Is.EqualTo(look));
            }
            Assert.That(HealthChip.EdgePt, Is.GreaterThanOrEqualTo(1f));
            Assert.That(HealthChip.RingPt, Is.GreaterThanOrEqualTo(1f));
        }
    }
}
