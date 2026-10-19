// Device stand-in: KESTREL_REPO_ROOT is an empty directory, and the flight fonts
// come from Resources. Launch must hide the designer sheet and the web readout
// column, and the notes sheet must scroll above the dock at 335 px.
using System;
using System.IO;
using System.Reflection;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class FlightHudPolishDeviceTests
    {
        string prevRoot;
        string away;

        [SetUp]
        public void EmptyRoot()
        {
            prevRoot = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            away = Path.Combine(Path.GetTempPath(), "kh-empty-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(away);
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", away);
        }

        [TearDown]
        public void RestoreRoot()
        {
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", prevRoot);
            if (Directory.Exists(away)) Directory.Delete(away, true);
        }

        [Test]
        public void LaunchHidesTheDesignerSheetAndTheWebHost()
        {
            Assert.That(Catalog.RepoRootOrNull(), Is.Null);
            var title = Resources.Load<Font>("KHFonts/Fraunces-Bold");
            var body = Resources.Load<Font>("KHFonts/SpaceGrotesk-Medium");
            var heavy = Resources.Load<Font>("KHFonts/SpaceGrotesk-Bold");
            Assert.That(title, Is.Not.Null);
            Assert.That(body, Is.Not.Null);
            Assert.That(heavy, Is.Not.Null);
            Assert.That(FlightDusk.TitleFace(), Is.SameAs(title));
            Assert.That(FlightDusk.BodyFace(), Is.SameAs(body));

            var ui = new VisualElement { name = "ui" };
            var design = new VisualElement { name = "design" };
            design.style.display = DisplayStyle.Flex;
            var sheet = new VisualElement { name = "designer-v3" };
            sheet.style.display = DisplayStyle.Flex;
            design.Add(sheet);
            var host = new VisualElement { name = FlightEntry.WebHost };
            host.style.display = DisplayStyle.Flex;
            host.Add(new Button { name = "launch", text = "Launch" });
            var home = new VisualElement { name = "home" };
            home.style.display = DisplayStyle.Flex;
            var chip = new Label { name = "issue-chip", text = "2 checks" };
            chip.AddToClassList("hidden");
            var hud = new VisualElement { name = "hud" };
            hud.AddToClassList("hidden");
            hud.style.width = 335;
            hud.style.height = 700;
            var band = new VisualElement { name = "hud-band" };
            band.Add(new Label { name = "h-t", text = "T-00:10.0" });
            var cast = new VisualElement { name = "webcast" };
            cast.style.backgroundColor = new Color(0.04f, 0.07f, 0.12f, 0.32f);
            cast.Add(new Label { name = "h-verdict", text = "Speed is there" });
            cast.Add(new Label { name = "h-pulse", text = "0.10 of 7.60 km/s" });
            cast.Add(new Label { name = "h-callout", text = "Tap an event." });
            hud.Add(band);
            hud.Add(cast);
            ui.Add(design);
            ui.Add(host);
            ui.Add(home);
            ui.Add(chip);
            ui.Add(hud);

            FlightEntry.Show(ui);
            Assert.That(design.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(sheet.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(host.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(home.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(hud.ClassListContains("hidden"), Is.False);
            Assert.That(hud.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(chip.ClassListContains("hidden"), Is.True);

            var ascent = new AscentHud();
            ascent.Open(hud, 1f);
            var help = hud.Q<ScrollView>("ascent-help");
            Assert.That(help, Is.Not.Null, "flight notes are a scroll view");
            Assert.That(help.verticalScrollerVisibility, Is.EqualTo(ScrollerVisibility.Auto));
            Assert.That(hud.Q("ascent-health"), Is.Not.Null);
            for (int i = 0; i < VehicleHealth.Count; i++)
                Assert.That(hud.Q<Button>("health-" + i), Is.Not.Null, "health-" + i);
            Assert.That(cast.style.backgroundColor.value.a, Is.EqualTo(0f).Within(0.001f));
            var verdict = hud.Q<Label>("h-verdict");
            Assert.That(verdict.style.unityFontDefinition.value.font, Is.SameAs(title));

            var openNotes = typeof(AscentHud).GetMethod("OnInfo", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(openNotes, Is.Not.Null);
            openNotes.Invoke(ascent, null);
            Assert.That(help.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(help.style.position.value, Is.EqualTo(Position.Absolute));
            Assert.That(help.parent, Is.EqualTo(hud));
            Assert.That(help.style.bottom.value.value, Is.GreaterThan(40f));
            Assert.That(help.style.height.value.value, Is.InRange(48f, 96f));

            float wordRoom = 335f - 32f;
            var parts = VehicleHealth.Read(new VehicleHealthInput
            {
                Fuel01 = 1,
                QLimitPa = 40000,
                GLimit = 5,
                HasFairing = true,
                QPa = 1000,
            });
            float cell = (335f - 16f - 4f * 4f) / 5f;
            for (int i = 0; i < parts.Length; i++)
            {
                Assert.That(FlightHudLayout.TextWidth(LongestWord(parts[i].Detail), 13f), Is.LessThan(wordRoom), parts[i].Detail);
                Assert.That(FlightHudLayout.TextWidth(parts[i].Mark, 11f), Is.LessThan(cell), parts[i].Mark);
            }
            Assert.That(FlightHudLayout.TrajectoryAlpha, Is.EqualTo(0f));
            float clockBox = 160f;
            float clock = FlightHudLayout.ClockFontPt(clockBox, FlightHudLayout.HeaderPt);
            Assert.That(FlightHudLayout.TextWidth("T-00:00.0", clock), Is.LessThanOrEqualTo(clockBox - 1.5f));
            Assert.That(FlightHudLayout.TextWidth("T+ 00.0", clock), Is.LessThanOrEqualTo(clockBox - 1.5f));
            Assert.That(FlightHudLayout.InfoViewPt(700f, 0f, false), Is.InRange(48f, 96f));

            var call = new Label { text = "FAIRING SEP  T+999.9  999.9km  9.99km/s" };
            FlightDusk.Callout(call, 1f, false);
            Assert.That(call.style.whiteSpace.value, Is.EqualTo(WhiteSpace.Normal));
            Assert.That(call.style.height.keyword, Is.EqualTo(StyleKeyword.Auto));
            Assert.That(call.style.unityFontDefinition.value.font, Is.SameAs(body));

            FlightEntry.Release(ui);
            Assert.That(design.ClassListContains("hidden"), Is.False);
            Assert.That(sheet.style.display.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(hud.ClassListContains("hidden"), Is.True);
            Assert.That(chip.ClassListContains("hidden"), Is.True);
            Assert.That(host.style.display.value, Is.EqualTo(DisplayStyle.None));

            host.style.display = DisplayStyle.Flex;
            design.style.display = DisplayStyle.Flex;
            hud.RemoveFromClassList("hidden");
            FlightEntry.Show(ui);
            Assert.That(host.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(design.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(hud.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        static string LongestWord(string text)
        {
            string best = "";
            if (string.IsNullOrEmpty(text)) return best;
            foreach (var word in text.Split(' '))
                if (word.Length > best.Length) best = word;
            return best;
        }
    }
}
