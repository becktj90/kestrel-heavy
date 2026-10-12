// EditMode tests for the retro Home (mix-v2): layout keeps the rocket clear of the title and the console on
// SE / Pro Max / iPad portrait, the token palette matches HomeArt, console hit targets are >= 44px and
// only Hangar + LAUNCH are live. Not run locally (no Unity on the authoring box).
using System.IO;
using System.Linq;
using Kestrel.Game.Shell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class HomeRetroTests
    {
        static readonly (string name, float w, float h)[] Devices = { ("SE", 375, 667), ("ProMax", 430, 932), ("iPad", 834, 1194) };

        [Test]
        public void RocketStaysBetweenTitleAndConsole()
        {
            foreach (var (n, w, h) in Devices)
            {
                float title = 0.33f * h, console = 0.70f * h;
                var L = HomeArt.Compute(w, h, title, console);
                Assert.That(L.RocketTop, Is.GreaterThanOrEqualTo(title), n);
                Assert.That(L.RocketBase, Is.LessThanOrEqualTo(console), n);
                Assert.That(L.RocketUnit, Is.GreaterThan(10f), n + ": art too small to read");
                Assert.That(L.TowerX, Is.GreaterThanOrEqualTo(0f), n);
                Assert.That(L.SunC.x - L.SunR, Is.GreaterThanOrEqualTo(0f), n);
                Assert.That(L.SunC.x + L.SunR, Is.LessThanOrEqualTo(w), n);
            }
        }

        [Test]
        public void DegenerateRectsDoNotThrowOrGoNegative()
        {
            var L = HomeArt.Compute(0, 0, 0, 0);
            Assert.That(L.RocketUnit, Is.GreaterThanOrEqualTo(0f));
            Assert.That(HomeArt.Compute(375, 667, 600, 500).RocketUnit, Is.EqualTo(0f));   // console above title
        }

        [Test]
        public void StripesStayInsideTheSun()
        {
            var L = HomeArt.Compute(430, 932, 300, 650);
            var bands = HomeArt.StripeBands(L);
            Assert.That(bands.Count, Is.InRange(4, HomeArt.Stripes));
            foreach (var (y0, y1) in bands)
            {
                Assert.That(y1, Is.GreaterThan(y0));
                foreach (var pt in HomeArt.CircleBand(L.SunC, L.SunR, y0, y1))
                    Assert.That(Vector2.Distance(pt, L.SunC), Is.LessThanOrEqualTo(L.SunR + 0.01f));
            }
        }

        [Test]
        public void ArtIsDeterministic()
        {
            CollectionAssert.AreEqual(HomeArt.Stars(), HomeArt.Stars());
            CollectionAssert.AreEqual(HomeArt.GrainRgba(), HomeArt.GrainRgba());
            Assert.That(HomeArt.GrainRgba().Length, Is.EqualTo(HomeArt.GrainSize * HomeArt.GrainSize * 4));
        }

        [Test]
        public void TokensMatchHomeArtPalette()
        {
            string uss = File.ReadAllText("Assets/Kestrel.Game/Shell/UI/KH_Tokens.uss");
            StringAssert.Contains("--kh-home-navy: #" + ColorUtility.ToHtmlStringRGB(HomeArt.Navy).ToLowerInvariant(), uss);
            StringAssert.Contains("--kh-home-cream: #" + ColorUtility.ToHtmlStringRGB(HomeArt.Cream).ToLowerInvariant(), uss);
            StringAssert.Contains("--kh-home-orange: #" + ColorUtility.ToHtmlStringRGB(HomeArt.SunOuter).ToLowerInvariant(), uss);
            StringAssert.Contains("--kh-home-console: #" + ColorUtility.ToHtmlStringRGB(HomeArt.Console).ToLowerInvariant(), uss);
        }

        [Test]
        public void ConsoleWiresLaunchAndHangarOnlyAndLabelsEverything()
        {
            int launched = 0, hangar = 0;
            var c = ScreenStub.BuildConsole(() => launched++, () => hangar++);
            var buttons = c.Query<Button>().ToList();
            Assert.That(buttons.Count, Is.EqualTo(5));
            foreach (var b in buttons)
            {
                Assert.That(b.text, Is.Not.Empty);
                Assert.That(b.tooltip, Is.Not.Empty, b.name + " needs a spoken label");
            }
            Assert.That(c.Q<Button>("home-launch").enabledSelf, Is.True);
            Assert.That(c.Q<Button>("home-hangar").enabledSelf, Is.True);
            foreach (var id in new[] { "missions", "codex", "settings" })
            {
                Assert.That(c.Q<Button>("home-" + id).enabledSelf, Is.False, id + " has no route yet: disabled stub");
                Assert.That(c.Q<Button>("home-" + id).Q(className: "kh-home-lamp--off"), Is.Not.Null);
            }
            Assert.That(c.Q<MissionPatch>("kh1-patch"), Is.Not.Null);
        }

        [Test]
        public void ConsoleHitTargetsAreAtLeast44px()
        {
            string uss = File.ReadAllText("Assets/Kestrel.Game/Shell/UI/KH_Tokens.uss");
            foreach (var sel in new[] { ".kh-button.kh-home-launch", ".kh-button.kh-home-console-button" })
            {
                int i = uss.IndexOf(sel + " {"); Assert.That(i, Is.GreaterThanOrEqualTo(0), sel);
                string rule = uss.Substring(i, uss.IndexOf('}', i) - i);
                var m = System.Text.RegularExpressions.Regex.Match(rule, @"min-height:\s*(\d+)px");
                Assert.That(m.Success && int.Parse(m.Groups[1].Value) >= 44, sel);
            }
        }
    }
}
