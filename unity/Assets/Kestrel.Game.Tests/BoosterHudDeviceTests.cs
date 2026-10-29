// Booster follow HUD on the real AscentHud (no scene, no flight): handing it the booster's readout swaps the
// telemetry strip to ALT / DOWN / FUEL / TO DECK with spoken tooltips and hides the upper stage's health tabs
// (Eng .. Fair) by visibility, so no row moves; handing it null brings the ascent strip and the tabs straight back.
using System.Reflection;
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class BoosterHudDeviceTests
    {
        static VisualElement Hud()
        {
            var hud = new VisualElement { name = UiIds.ScreenFlight };
            hud.style.width = 390;
            hud.style.height = 844;
            var band = new VisualElement { name = "hud-band" };
            band.Add(new Label { name = "h-t", text = "T+ 491.1" });
            hud.Add(band);
            var cast = new VisualElement { name = "webcast" };
            cast.Add(new Label { name = "h-verdict", text = "On track" });
            hud.Add(cast);
            return hud;
        }

        static BoosterHudReadout Landing() => BoosterHud.Read(
            new RecoverySample { Alt = 40, Radial = -6, Prop = 3000, MissM = -12, Phase = RecoveryPhase.Landing },
            new RecoveryCraft { Mode = "barge", ReserveKg = 20000 });

        static string Key(VisualElement hud, int i) => hud.Q<Label>("ascent-cell-" + i + "-k").text;
        static Label Val(VisualElement hud, int i) => hud.Q<Label>("ascent-cell-" + i + "-v");

        [Test]
        public void FollowingTheBoosterSwapsTheStripAndHidesTheUpperStageTabs()
        {
            var hud = Hud();
            var ascent = new AscentHud();
            AscentHud.ForceWide = false;
            try
            {
                ascent.Open(hud, 1f);
                ascent.Preview("158.9 km", "4.53 km/s", "On track", null);
                var health = hud.Q("ascent-health");
                Assert.That(health, Is.Not.Null);
                Assert.That(ascent.Focus, Is.EqualTo(FlightHudFocus.Ascent));
                Assert.That(Key(hud, 0), Is.EqualTo("ALT"));
                Assert.That(Key(hud, 1), Is.EqualTo("SPEED"));
                Assert.That(Val(hud, 0).text, Is.EqualTo("158.9 km"));
                Assert.That(health.style.visibility.keyword, Is.EqualTo(StyleKeyword.Null), "tabs show in ascent");

                var r = Landing();
                ascent.SetBooster(r);
                Assert.That(ascent.Focus, Is.EqualTo(FlightHudFocus.Booster));
                string[] keys = { "ALT", "DOWN", "FUEL", "TO DECK" };
                string[] vals = { "40 m", "6 m/s", "15%", "12 m" };
                for (int i = 0; i < keys.Length; i++)
                {
                    Assert.That(Key(hud, i), Is.EqualTo(keys[i]), "cell " + i);
                    Assert.That(Val(hud, i).text, Is.EqualTo(vals[i]), "cell " + i);
                    Assert.That(Val(hud, i).tooltip, Is.EqualTo(r[i].Spoken), "cell " + i + " is read out");
                }
                Assert.That(health.style.visibility.value, Is.EqualTo(Visibility.Hidden), "Eng .. Fair are S3's, hidden on the booster");
                Assert.That(health.style.display.keyword, Is.Not.EqualTo(StyleKeyword.None), "hidden by visibility: the strip does not jump");

                // A frame of upper-stage numbers while the camera is still on the booster does not take the strip back.
                ascent.Preview("159.0 km", "4.54 km/s", "On track", null);
                Assert.That(Key(hud, 1), Is.EqualTo("DOWN"));
                Assert.That(Val(hud, 0).text, Is.EqualTo("40 m"));

                ascent.SetBooster(null);
                Assert.That(ascent.Focus, Is.EqualTo(FlightHudFocus.Ascent));
                Assert.That(Key(hud, 0), Is.EqualTo("ALT"));
                Assert.That(Key(hud, 1), Is.EqualTo("SPEED"));
                Assert.That(Key(hud, 2), Is.EqualTo("Q"));
                Assert.That(Key(hud, 3), Is.EqualTo("G"));
                Assert.That(Val(hud, 0).text, Is.EqualTo("159.0 km"), "the latest ascent reading comes straight back");
                Assert.That(Val(hud, 0).tooltip, Is.Empty);
                Assert.That(health.style.visibility.keyword, Is.EqualTo(StyleKeyword.Null), "tabs come back");
            }
            finally
            {
                ascent.Close();
                AscentHud.ForceWide = null;
            }
        }

        [Test]
        public void AnOpenHealthNoteClosesWhenTheCameraGoesToTheBooster()
        {
            var hud = Hud();
            var ascent = new AscentHud();
            AscentHud.ForceWide = false;
            try
            {
                ascent.Open(hud, 1f);
                ascent.Preview("158.9 km", "4.53 km/s", "On track", null);
                var onHealth = typeof(AscentHud).GetMethod("OnHealth", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(onHealth, Is.Not.Null);
                onHealth.Invoke(ascent, new object[] { VehicleHealth.Count - 1 });
                var note = hud.Q(AscentHud.HealthDetailName);
                Assert.That(note, Is.Not.Null);
                Assert.That(note.style.display.value, Is.EqualTo(DisplayStyle.Flex), "Fair note open");
                ascent.SetBooster(Landing());
                Assert.That(note.style.display.value, Is.EqualTo(DisplayStyle.None), "the S3 note does not hang over the booster");
            }
            finally
            {
                ascent.Close();
                AscentHud.ForceWide = null;
            }
        }
    }
}
