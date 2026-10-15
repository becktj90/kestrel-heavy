// EditMode tests for the main-menu "mix" overlay. Not run locally (no Unity on the authoring box).
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Kestrel.Game.Shell;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class MainMenuMixTests
    {
        const string Uss = "Assets/Kestrel.Game/Shell/UI/KH_Tokens.uss";

        static VisualElement Build(bool reduce, out int launched, out int hangar, out VisualElement full)
        {
            int l = 0, h = 0;
            full = new VisualElement(); var safe = new VisualElement();
            var root = MainMenuOverlay.Build(full, safe, () => l++, () => h++, reduce);
            launched = l; hangar = h;
            return root;
        }

        [Test]
        public void CopyIsHonestAndHasNoFakeData()
        {
            var all = MainMenuCopy.Launch.Caption + MainMenuCopy.TapHint + string.Join(" ", MainMenuCopy.Tiles.Select(t => t.Caption + t.Label));
            StringAssert.DoesNotContain("Next:", MainMenuCopy.LaunchCaption, "no start-mission route exists yet");
            StringAssert.DoesNotContain("NEW PARTS", all.ToUpperInvariant());
            Assert.That(Regex.IsMatch(all, @"T[\u2013\-]\d"), Is.False, "no countdown");
        }

        [Test]
        public void OnlyHangarAndLaunchAreLive()
        {
            Assert.That(MainMenuCopy.Launch.Live);
            Assert.That(MainMenuCopy.Tiles.Where(t => t.Live).Select(t => t.Id), Is.EqualTo(new[] { "hangar" }));
            Assert.That(MainMenuCopy.Tiles.Length, Is.EqualTo(4));
        }

        [Test]
        public void OverlayBuildsControlsWithSpokenLabelsAndSoonTags()
        {
            var root = Build(false, out _, out _, out var full);
            string[] ids = { "home-launch", "home-hangar", "home-missions", "home-codex", "home-settings" };
            foreach (var id in ids)
            {
                var b = root.Q<Button>(id);
                Assert.That(b, Is.Not.Null, id);
                Assert.That(b.tooltip, Is.Not.Empty, id + " needs a spoken label");
            }
            Assert.That(root.Q<Button>("home-launch").enabledSelf);
            Assert.That(root.Q<Button>("home-hangar").enabledSelf);
            foreach (var id in new[] { "missions", "codex", "settings" })
            {
                var b = root.Q<Button>("home-" + id);
                Assert.That(b.enabledSelf, Is.False, id);
                Assert.That(b.Q<Label>("soon"), Is.Not.Null, id + " needs a SOON tag");
            }
            Assert.That(root.Query<Label>("soon").ToList().Count, Is.EqualTo(3));
            Assert.That(root.Query<VisualElement>("lamp").ToList().Count, Is.EqualTo(2));
            Assert.That(full.Q("mm-bar-top"), Is.Not.Null); Assert.That(full.Q("mm-bar-bottom"), Is.Not.Null);
            // Decorative elements must not steal taps from the 3D scene or the buttons.
            Assert.That(root.pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(full.Q("mm-full").pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(root.Q<Label>("mm-hint").text, Is.EqualTo(MainMenuCopy.TapHint));
        }

        [Test]
        public void ReduceMotionShowsEverythingAtOnceWithNoTransitions()
        {
            var root = Build(true, out _, out _, out _);
            Assert.That(root.ClassListContains("kh-mm-reduce"));
            foreach (var el in root.Query(className: "kh-mm-anim").ToList())
                Assert.That(el.ClassListContains("kh-mm-in"), "reduce motion: no intro delay");
            StringAssert.Contains(".kh-mm-reduce .kh-mm-anim { transition-duration: 0s; }", File.ReadAllText(Uss));
            var live = Build(false, out _, out _, out _);
            Assert.That(live.ClassListContains("kh-mm-reduce"), Is.False);
        }

        [Test]
        public void IntroOrderFollowsTheMotionSpec()
        {
            Assert.That(MainMenuCopy.IntroDelayMs("title"), Is.LessThan(MainMenuCopy.IntroDelayMs("launch")));
            int prev = MainMenuCopy.IntroDelayMs("launch");
            foreach (var t in MainMenuCopy.Tiles) { int d = MainMenuCopy.IntroDelayMs(t.Id); Assert.That(d - prev, Is.EqualTo(70)); prev = d; }
            Assert.That(prev, Is.LessThan(1500));
        }

        [Test]
        public void HitTargetsAreAtLeast44px()
        {
            string uss = File.ReadAllText(Uss);
            foreach (var sel in new[] { ".kh-mm-launch", ".kh-button.kh-mm-tile" })
            {
                int i = uss.IndexOf(sel + " {"); Assert.That(i, Is.GreaterThanOrEqualTo(0), sel);
                string rule = uss.Substring(i, uss.IndexOf('}', i) - i);
                var m = Regex.Match(rule, @"min-height:\s*(\d+)px");
                Assert.That(m.Success && int.Parse(m.Groups[1].Value) >= 44, sel);
            }
        }

        [Test]
        public void TitleFitStaysInsideBoundsAndTheColumn()
        {
            Assert.That(MainMenuCopy.FitTitle(0, 500), Is.EqualTo(MainMenuCopy.TitleMin));
            foreach (float avail in new[] { 200f, 340f, 400f, 900f })
            {
                float size = MainMenuCopy.FitTitle(avail, 480f);   // KESTREL is about 480 px wide at 100 px
                Assert.That(size, Is.InRange(MainMenuCopy.TitleMin, MainMenuCopy.TitleMax));
                if (size > MainMenuCopy.TitleMin) Assert.That(480f * size / 100f, Is.LessThanOrEqualTo(avail + 0.01f));
            }
        }

        [Test]
        public void BarsClearTheSafeInsetsOnEveryDevice()
        {
            foreach (var (h, inset) in new[] { (667f, 20f), (956f, 62f), (1376f, 24f) })
                Assert.That(MainMenuCopy.BarHeight(h, inset), Is.GreaterThanOrEqualTo(inset + 24f));
        }

        [Test]
        public void ThreeDSceneKeepsRocketHangarAndTelemetryOnly()
        {
            foreach (var a in MainMenuLayout.Anchors())
            {
                bool hidden = MainMenuLayout.ReplacedByOverlay(a);
                if (a.Clickable || a.Id == "title" || a.Id == "subtitle") Assert.That(hidden, a.Id);
                else Assert.That(hidden, Is.False, a.Id + " (3D telemetry/KH-1) must stay");
            }
            Assert.That(MainMenuLayout.Anchors().Any(a => a.Id == "kh1" && !MainMenuLayout.ReplacedByOverlay(a)));
        }

        [Test]
        public void FontsShipSubsetAndUssPointsAtTheirGuids()
        {
            string dir = "Assets/Art/Fonts/";
            long total = 0; string uss = File.ReadAllText(Uss);
            foreach (var n in new[] { "Fraunces-Black", "Fraunces-Bold", "SpaceGrotesk-Medium", "SpaceGrotesk-Bold" })
            {
                Assert.That(File.Exists(dir + n + ".ttf"), n);
                total += new FileInfo(dir + n + ".ttf").Length;
                var guid = Regex.Match(File.ReadAllText(dir + n + ".ttf.meta"), @"guid:\s*([0-9a-f]{32})").Groups[1].Value;
                StringAssert.Contains("guid=" + guid, uss, n + " guid must match its meta");
            }
            Assert.That(total, Is.LessThan(120 * 1024), "font IPA budget");
        }
    }
}
