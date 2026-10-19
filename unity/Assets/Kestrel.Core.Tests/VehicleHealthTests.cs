// Vehicle health tones, the notes sheet above the dock, and 335 px portrait fit.
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class VehicleHealthTests
    {
        [Test]
        public void ACalmAscentIsGreen()
        {
            var parts = VehicleHealth.Read(Calm());
            Assert.AreEqual(VehicleHealth.Count, parts.Length);
            foreach (var p in parts)
                Assert.AreEqual(VehicleHealthTone.Green, p.Tone, p.Id + " " + p.Detail);
        }

        [Test]
        public void HeatFuelGAndEnginesPaintTheMatchingPart()
        {
            var hot = Calm();
            hot.QPa = 90000;
            hot.QLimitPa = 100000;
            var heating = VehicleHealth.Read(hot);
            Assert.AreEqual(VehicleHealthTone.Amber, Find(heating, "structure").Tone);
            Assert.AreEqual(VehicleHealthTone.Amber, Find(heating, "fairing").Tone);

            var broken = Calm();
            broken.QPa = 120000;
            broken.QLimitPa = 100000;
            Assert.AreEqual(VehicleHealthTone.Red, Find(VehicleHealth.Read(broken), "structure").Tone);

            var low = Calm();
            low.Fuel01 = 0.12;
            Assert.AreEqual(VehicleHealthTone.Amber, Find(VehicleHealth.Read(low), "tanks").Tone);
            low.Fuel01 = 0.02;
            Assert.AreEqual(VehicleHealthTone.Red, Find(VehicleHealth.Read(low), "tanks").Tone);

            var hard = Calm();
            hard.GLoad = 4.2;
            hard.GLimit = 5;
            Assert.AreEqual(VehicleHealthTone.Amber, Find(VehicleHealth.Read(hard), "guidance").Tone);
            hard.GLoad = 6;
            Assert.AreEqual(VehicleHealthTone.Red, Find(VehicleHealth.Read(hard), "guidance").Tone);

            var outage = Calm();
            outage.EngineOut = true;
            outage.ExpectThrust = true;
            Assert.AreEqual(VehicleHealthTone.Red, Find(VehicleHealth.Read(outage), "engines").Tone);

            var quiet = Calm();
            quiet.ExpectThrust = true;
            quiet.Thrust01 = 0.1;
            Assert.AreEqual(VehicleHealthTone.Amber, Find(VehicleHealth.Read(quiet), "engines").Tone);

            var cooked = Calm();
            cooked.PayloadHot = true;
            Assert.AreEqual(VehicleHealthTone.Red, Find(VehicleHealth.Read(cooked), "fairing").Tone);

            var dropped = Calm();
            dropped.FairingOff = true;
            Assert.AreEqual(VehicleHealthTone.Green, Find(VehicleHealth.Read(dropped), "fairing").Tone);
            Assert.AreEqual("Fairing is off.", Find(VehicleHealth.Read(dropped), "fairing").Detail);
        }

        [Test]
        public void LossKindForcesThePartThatFailed()
        {
            var s = Calm();
            s.Lost = true;
            s.LossKind = "structural-maxq";
            Assert.AreEqual(VehicleHealthTone.Red, Find(VehicleHealth.Read(s), "structure").Tone);
            s.LossKind = "prop-depletion";
            Assert.AreEqual(VehicleHealthTone.Red, Find(VehicleHealth.Read(s), "tanks").Tone);
            s.LossKind = "engine-out";
            Assert.AreEqual(VehicleHealthTone.Red, Find(VehicleHealth.Read(s), "engines").Tone);
            s.LossKind = "tumble";
            Assert.AreEqual(VehicleHealthTone.Red, Find(VehicleHealth.Read(s), "guidance").Tone);
            s.LossKind = "thermal";
            Assert.AreEqual(VehicleHealthTone.Red, Find(VehicleHealth.Read(s), "fairing").Tone);
        }

        [Test]
        public void NotesSitAboveTheDockAndMarksFitAt335()
        {
            const float width = 335f;
            const float height = 700f;
            var frame = FlightHudLayout.Place(width, height, 20f, 0f, 0f, 0f, FlightHudMode.Cruise);
            float view = FlightHudLayout.InfoViewPt(height, 0f, false);
            float sheetTop = frame.Dock.Y - view;
            Assert.That(sheetTop, Is.GreaterThanOrEqualTo(height * FlightHudLayout.StackBottom - 0.6f), "notes cover the rocket");
            Assert.That(frame.Dock.Y - sheetTop, Is.GreaterThanOrEqualTo(48f), "notes have no room to scroll");
            Assert.That(frame.Health.Bottom, Is.LessThanOrEqualTo(frame.Telemetry.Y + 0.6f));
            float cell = (frame.Health.W - 4f * FlightHudLayout.GapPt) / VehicleHealth.Count;
            foreach (var p in VehicleHealth.Read(Calm()))
            {
                Assert.That(FlightHudLayout.TextWidth(p.Mark, 11f), Is.LessThan(cell), p.Mark);
                Assert.That(FlightHudLayout.TextWidth(p.Detail, 12f) > width - 24f || p.Detail.Contains(" "), Is.True, p.Detail);
                Assert.That(LongestWord(p.Detail), Is.LessThan(width - 32f), p.Detail);
            }
            string callout = FlightStaging.Format(FlightStaging.Title(StagingKind.FairingSep), 999.9, 999900, 9990);
            Assert.That(LongestWord(callout), Is.LessThan(width - 32f), callout);
            float font = FlightHudLayout.ClockFontPt(frame.Clock.W, frame.Clock.H);
            Assert.That(FlightHudLayout.TextWidth("T-00:00.0", font), Is.LessThanOrEqualTo(frame.Clock.W - 1.5f));
            Assert.That(FlightHudLayout.TextWidth("T+ 00.0", font), Is.LessThanOrEqualTo(frame.Clock.W - 1.5f));
            Assert.That(FlightHudLayout.TrajectoryAlpha, Is.EqualTo(0f));
        }

        [Test]
        public void HealthStripClearsTheRailsOnAWideScreen()
        {
            var sizes = new[]
            {
                new[] { 1280f, 720f, 0f, 0f, 0f, 0f },
                new[] { 1920f, 1080f, 0f, 0f, 0f, 0f },
                new[] { 844f, 390f, 0f, 21f, 47f, 47f },
            };
            foreach (var d in sizes)
            {
                var f = FlightHudLayout.PlaceWide(d[0], d[1], d[2], d[3], d[4], d[5], FlightHudMode.Cruise);
                string where = d[0] + "x" + d[1];
                Assert.That(f.Health.Live, Is.True, where);
                Assert.That(f.Health.Overlaps(f.Plot), Is.False, where);
                Assert.That(f.Health.Overlaps(f.Telemetry), Is.False, where);
                Assert.That(f.Health.Overlaps(f.Dock), Is.False, where);
                Assert.That(f.Health.Overlaps(f.Follow), Is.False, where);
                Assert.That(f.Health.Bottom, Is.LessThanOrEqualTo(f.Plot.Y + 0.6f), where);
            }
        }

        static float LongestWord(string text)
        {
            float worst = 0f;
            if (string.IsNullOrEmpty(text)) return 0f;
            foreach (var word in text.Split(' '))
            {
                float w = FlightHudLayout.TextWidth(word, 13f);
                if (w > worst) worst = w;
            }
            return worst;
        }

        static VehicleHealthPart Find(VehicleHealthPart[] parts, string id)
        {
            for (int i = 0; i < parts.Length; i++)
                if (parts[i].Id == id) return parts[i];
            Assert.Fail(id);
            return null;
        }

        static VehicleHealthInput Calm() => new VehicleHealthInput
        {
            QPa = 20000,
            QLimitPa = 100000,
            Fuel01 = 0.8,
            GLoad = 1.6,
            GLimit = 5,
            Thrust01 = 0.9,
            ExpectThrust = true,
            HasFairing = true,
        };
    }
}
