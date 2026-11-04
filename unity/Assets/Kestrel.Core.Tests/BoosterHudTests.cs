// Booster follow HUD (Graphics Dev / Arcade, #381 landing capture): while the camera follows the booster the strip
// read S3's ascent numbers (T+491, 158.9 km, 4.53 km/s), a red Fair chip and BACK TO DESIGN with the booster 40 m
// over the Haven deck. BoosterHud turns the recovery sample BoosterLanding already steps into the booster's strip.
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class BoosterHudTests
    {
        static RecoveryCraft Craft(string mode, double reserveKg) => new RecoveryCraft { Mode = mode, ReserveKg = reserveKg, DryKg = 25000, Legs = true, LegsKind = "light" };

        [Test]
        public void FocusFollowsTheCameraTarget()
        {
            Assert.That(BoosterHud.Focus(true), Is.EqualTo(FlightHudFocus.Booster));
            Assert.That(BoosterHud.Focus(false), Is.EqualTo(FlightHudFocus.Ascent));
            Assert.That(BoosterHud.ShowsAscentChrome(FlightHudFocus.Ascent), Is.True, "health tabs, S3 chip and BACK TO DESIGN in ascent");
            Assert.That(BoosterHud.ShowsAscentChrome(FlightHudFocus.Booster), Is.False, "and hidden while the booster has the HUD");
        }

        [Test]
        public void LandingReadsTheBoosterNotTheUpperStage()
        {
            // The #381 moment: 40 m over the deck, coming down at 6 m/s, 15% of the landing reserve left, 12 m short.
            var s = new RecoverySample { Alt = 40, Radial = -6, Sink = 6, Speed = 6.5, Prop = 3000, MissM = -12, Phase = RecoveryPhase.Landing };
            var r = BoosterHud.Read(s, Craft("barge", 20000));
            Assert.That(r.Alt.Key, Is.EqualTo("ALT"));
            Assert.That(r.Alt.Value, Is.EqualTo("40 m"));
            Assert.That(r.Vertical.Key, Is.EqualTo("DOWN"));
            Assert.That(r.Vertical.Value, Is.EqualTo("6 m/s"));
            Assert.That(r.Fuel.Key, Is.EqualTo("FUEL"));
            Assert.That(r.Fuel.Value, Is.EqualTo("15%"));
            Assert.That(r.Fuel.Tone, Is.EqualTo(BoosterHudTone.Amber), "15% is low fuel");
            Assert.That(r.Target.Key, Is.EqualTo("TO DECK"));
            Assert.That(r.Target.Value, Is.EqualTo("12 m"), "distance, short or long");
            Assert.That(r.Alt.Spoken, Is.EqualTo("Booster height 40 metres."));
            Assert.That(r.Vertical.Spoken, Is.EqualTo("Booster coming down 6 metres a second."));
            Assert.That(r.Fuel.Spoken, Is.EqualTo("Landing fuel 15 percent left."));
            Assert.That(r.Target.Spoken, Is.EqualTo("Landing deck 12 metres away."));
            for (int i = 0; i < BoosterHudReadout.Count; i++)
            {
                Assert.That(r[i].Value, Does.Not.Contain("km/s").And.Not.Contain("kPa"), "no ascent units at the deck");
                Assert.That(r[i].Key.Length, Is.LessThanOrEqualTo(7), "short kid-readable keys fit the 4-cell strip");
            }
        }

        [Test]
        public void BoostbackRisesInKilometres()
        {
            var s = new RecoverySample { Alt = 62000, Radial = 300, Prop = 18000, MissM = 48000, Phase = RecoveryPhase.Boostback };
            var r = BoosterHud.Read(s, Craft("barge", 20000));
            Assert.That(r.Alt.Value, Is.EqualTo("62.0 km"));
            Assert.That(r.Vertical.Key, Is.EqualTo("UP"));
            Assert.That(r.Vertical.Value, Is.EqualTo("300 m/s"));
            Assert.That(r.Vertical.Spoken, Does.StartWith("Booster going up"));
            Assert.That(r.Fuel.Value, Is.EqualTo("90%"));
            Assert.That(r.Fuel.Tone, Is.EqualTo(BoosterHudTone.Green));
            Assert.That(r.Target.Value, Is.EqualTo("48.0 km"));
            Assert.That(r.Target.Spoken, Is.EqualTo("Landing deck 48.0 kilometres away."));
        }

        [Test]
        public void EntryFallsFastInKilometresASecond()
        {
            var s = new RecoverySample { Alt = 41000, Radial = -1250, Prop = 9000, MissM = 3000, Phase = RecoveryPhase.Entry };
            var r = BoosterHud.Read(s, Craft("barge", 20000));
            Assert.That(r.Vertical.Key, Is.EqualTo("DOWN"));
            Assert.That(r.Vertical.Value, Is.EqualTo("1.25 km/s"));
        }

        [Test]
        public void ReturnToLaunchSiteTargetsThePad()
        {
            var s = new RecoverySample { Alt = 900, Radial = -80, Prop = 4000, MissM = 250 };
            var r = BoosterHud.Read(s, Craft("rtls", 20000));
            Assert.That(r.Target.Key, Is.EqualTo("TO PAD"));
            Assert.That(r.Target.Value, Is.EqualTo("250 m"));
            Assert.That(r.Target.Spoken, Is.EqualTo("Landing pad 250 metres away."));
            Assert.That(r.Alt.Value, Is.EqualTo("900 m"));
        }

        [Test]
        public void FuelIsTheShareOfTheLandingReserve()
        {
            Assert.That(BoosterHud.Fuel01(5000, 20000), Is.EqualTo(0.25).Within(1e-9));
            Assert.That(BoosterHud.Fuel01(5000, 0), Is.EqualTo(0), "no reserve reads empty, never divides by zero");
            Assert.That(BoosterHud.Fuel01(-3, 20000), Is.EqualTo(0));
            Assert.That(BoosterHud.Fuel01(25000, 20000), Is.EqualTo(1));
            var dry = BoosterHud.Read(new RecoverySample { Alt = 10, Radial = -2, Prop = 0, FuelEmpty = true }, Craft("barge", 0));
            Assert.That(dry.Fuel.Value, Is.EqualTo("0%"));
            Assert.That(dry.Fuel.Tone, Is.EqualTo(BoosterHudTone.Amber));
        }

        [Test]
        public void BelowTheDeckNeverReadsNegative()
        {
            var r = BoosterHud.Read(new RecoverySample { Alt = -0.4, Radial = 0, Prop = 1000, MissM = 0, Phase = RecoveryPhase.Down }, Craft("barge", 20000));
            Assert.That(r.Alt.Value, Is.EqualTo("0 m"));
            Assert.That(r.Vertical.Key, Is.EqualTo("DOWN"), "standing still reads DOWN 0, not UP");
            Assert.That(r.Vertical.Value, Is.EqualTo("0 m/s"));
        }
    }
}
