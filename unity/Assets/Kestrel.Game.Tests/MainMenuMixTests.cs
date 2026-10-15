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
                var tag = b.Q<Label>("soon");
                Assert.That(tag, Is.Not.Null, id + " needs a SOON tag");
                Assert.That(tag.parent.ClassListContains("kh-mm-tile-head"), id + ": SOON sits in the title row, not over the caption");
                Assert.That(b.Q(className: "kh-mm-tile-caption").parent, Is.SameAs(b));
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
            string dir = "Assets/Art/Fonts/Resources/KHFonts/";
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
    
        [Test]
        public void ControlsStartBelowTheThreeDHeroOnEveryDevice()
        {
            foreach (var (w, h) in new[] { (375f, 667f), (440f, 956f), (1032f, 1376f) })
            {
                float need = MainMenuCopy.MinControlsHeight(MainMenuCopy.IsNarrow(w));
                Assert.That(1f - need / h, Is.GreaterThanOrEqualTo(MainMenuCopy.HeroBottomFrac - 0.01f), w + "x" + h + ": controls would cover the rocket/telemetry");
            }
        }

        [Test]
        public void NarrowClassAppliesBelow500AndTargetsStayLarge()
        {
            Assert.That(MainMenuCopy.IsNarrow(375f)); Assert.That(MainMenuCopy.IsNarrow(440f)); Assert.That(MainMenuCopy.IsNarrow(1032f), Is.False);
            string uss = File.ReadAllText(Uss);
            foreach (var m in Regex.Matches(uss, @"\.kh-mm-narrow \.(?:kh-mm-launch|kh-button\.kh-mm-tile) \{[^}]*min-height:\s*(\d+)px").Cast<Match>())
                Assert.That(int.Parse(m.Groups[1].Value), Is.GreaterThanOrEqualTo(44));
            StringAssert.DoesNotContain("height: 88px; height", uss);
        }

        [Test]
        public void OverlayDoesNotDimTheTelemetryAndFontsLoadFromResources()
        {
            string uss = File.ReadAllText(Uss);
            var m = Regex.Match(uss, @"\.kh-mm \.kh-home-grain \{ opacity: ([0-9.]+);");
            Assert.That(m.Success && float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) <= 0.3f, "grain stays light over the 3D text");
            Assert.That(File.Exists("Assets/Art/Fonts/Resources/KHFonts/Fraunces-Black.ttf"));
            Assert.That(MainMenuFonts.Dir, Is.EqualTo("KHFonts/"));
        }

        // ---- Responsive type (MenuTypeScale): points, insets are typical safe areas. ----
        static readonly (string name, float w, float h, float top, float bottom)[] Devices =
        {
            ("SE", 375f, 667f, 20f, 0f), ("iPhone", 390f, 844f, 47f, 34f), ("ProMax", 440f, 956f, 62f, 34f),
            ("iPad13", 1032f, 1376f, 24f, 20f), ("iPadSplit", 507f, 1194f, 24f, 20f),
        };

        [Test]
        public void MinimumSizesHoldOnEveryDevice()
        {
            foreach (var d in Devices)
            {
                var z = MenuTypeScale.Compute(d.w, d.h, d.top, d.bottom);
                Assert.That(z.TileCaption, Is.GreaterThanOrEqualTo(MenuTypeScale.CaptionFloor), d.name);
                Assert.That(z.LaunchCaption, Is.GreaterThanOrEqualTo(MenuTypeScale.CaptionFloor), d.name);
                Assert.That(z.Soon, Is.GreaterThanOrEqualTo(MenuTypeScale.Soon.Min), d.name);
                Assert.That(z.Hint, Is.GreaterThanOrEqualTo(MenuTypeScale.Hint.Min), d.name);
                Assert.That(z.TileName, Is.GreaterThanOrEqualTo(MenuTypeScale.TileName.Min), d.name);
                Assert.That(z.Title, Is.InRange(MenuTypeScale.Title.Min, MenuTypeScale.Title.Max), d.name);
                Assert.That(z.Tagline, Is.GreaterThanOrEqualTo(MenuTypeScale.Tagline.Min), d.name);
            }
        }

        [Test]
        public void EveryElementFitsItsContainer()
        {
            foreach (var d in Devices)
            {
                var z = MenuTypeScale.Compute(d.w, d.h, d.top, d.bottom);
                float col = d.w - 2f * MenuTypeScale.SideMargin;
                Assert.That(MenuTypeScale.TextW("KESTREL", z.Title), Is.LessThanOrEqualTo(col), d.name + " title");
                Assert.That(MenuTypeScale.TextW("HEAVY", z.Tagline), Is.LessThanOrEqualTo(col * 0.5f), d.name + " tagline leaves room for the rules");
                Assert.That(MenuTypeScale.TextW("LAUNCH", z.LaunchLabel), Is.LessThanOrEqualTo(z.LaunchCaptionW), d.name + " launch label");
                Assert.That(MenuTypeScale.TextW(MainMenuCopy.TapHint, z.Hint), Is.LessThanOrEqualTo(col), d.name + " hint on one line");
                foreach (var t in MainMenuCopy.Tiles)
                {
                    float n = MenuTypeScale.FitName(z, t.Label, !t.Live);
                    Assert.That(MenuTypeScale.NameFits(z, t.Label, !t.Live, n), d.name + " " + t.Label + " name beside its lamp/tag");
                    // Caption: wraps to the lines the tile height was derived from, never more.
                    float lines = MenuTypeScale.TextW(t.Caption, z.TileCaption) * 1.08f / z.TileInner;
                    Assert.That(lines, Is.LessThanOrEqualTo(z.TileCaptionLines + 0.001f), d.name + " " + t.Id + " caption lines");
                }
                // Tile height is derived from the scaled type, and still a >= 44 target.
                float need = 2f * MenuTypeScale.TileBorder + 2f * z.Pad + System.Math.Max(24f, z.TileName * 1.25f) + 4f + z.TileCaptionLines * z.TileCaption * 1.3f;
                Assert.That(z.TileH, Is.GreaterThanOrEqualTo(need - 0.01f), d.name);
                Assert.That(z.TileH, Is.GreaterThanOrEqualTo(MenuTypeScale.HitMin), d.name);
                Assert.That(z.LaunchH, Is.GreaterThanOrEqualTo(MenuTypeScale.HitMin), d.name);
            }
        }

        [Test]
        public void ControlsAndTitleLeaveTheHeroVisible()
        {
            foreach (var d in Devices)
            {
                var z = MenuTypeScale.Compute(d.w, d.h, d.top, d.bottom);
                Assert.That(1f - z.ControlsH / d.h, Is.GreaterThanOrEqualTo(MainMenuCopy.HeroBottomFrac), d.name + ": controls cover the rocket/telemetry");
                float block = 40f + z.Title * 1.2f + z.Tagline * 1.3f;
                Assert.That(block / d.h, Is.LessThanOrEqualTo(0.25f), d.name + ": title block reaches the rocket nose");
            }
        }

        [Test]
        public void ScalingIsMonotonicInWidthAndHeight()
        {
            float[] ws = { 320f, 375f, 390f, 440f, 600f, 834f, 1032f };
            for (int i = 1; i < ws.Length; i++)
            {
                var a = MenuTypeScale.Compute(ws[i - 1], 1200f); var b = MenuTypeScale.Compute(ws[i], 1200f);   // height large so width limits
                Assert.That(b.Scale, Is.GreaterThanOrEqualTo(a.Scale));
                Assert.That(b.TileCaption, Is.GreaterThanOrEqualTo(a.TileCaption)); Assert.That(b.TileName, Is.GreaterThanOrEqualTo(a.TileName));
                Assert.That(b.LaunchLabel, Is.GreaterThanOrEqualTo(a.LaunchLabel)); Assert.That(b.Soon, Is.GreaterThanOrEqualTo(a.Soon));
            }
            float[] hs = { 568f, 667f, 812f, 844f, 932f, 1194f };
            for (int i = 1; i < hs.Length; i++)
            {
                var a = MenuTypeScale.Compute(1200f, hs[i - 1]); var b = MenuTypeScale.Compute(1200f, hs[i]);   // width large so height limits
                Assert.That(b.Scale, Is.GreaterThanOrEqualTo(a.Scale));
                Assert.That(b.TileCaption, Is.GreaterThanOrEqualTo(a.TileCaption)); Assert.That(b.LaunchCaption, Is.GreaterThanOrEqualTo(a.LaunchCaption));
            }
            Assert.That(MenuTypeScale.Compute(390f, 844f, 47f, 34f).Scale, Is.LessThan(MenuTypeScale.Compute(390f, 844f).Scale), "insets shrink the usable height");
        }

        [Test]
        public void ScaleFormulaMatchesTheSpec()
        {
            Assert.That(MenuTypeScale.Scale(390f, 844f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(MenuTypeScale.TileCaption.At(1f), Is.EqualTo(13f));
            Assert.That(MenuTypeScale.TileCaption.At(0.1f), Is.EqualTo(11f), "floor");
            Assert.That(MenuTypeScale.TileCaption.At(9f), Is.EqualTo(18f), "cap");
        }

        [Test]
        public void ApplyScaleSetsInlineSizesOnTheBuiltOverlay()
        {
            var root = Build(false, out _, out _, out _);
            var z = MenuTypeScale.Compute(390f, 844f, 47f, 34f);
            MainMenuOverlay.ApplyScale(root, z);
            Assert.That(root.Q<Label>("mm-kestrel").style.fontSize.value.value, Is.EqualTo(z.Title).Within(0.01f));
            Assert.That(root.Q<Button>("home-hangar").style.minHeight.value.value, Is.EqualTo(z.TileH).Within(0.01f));
            Assert.That(root.Q<Button>("home-missions").Q(className: "kh-mm-soon").style.fontSize.value.value, Is.EqualTo(z.Soon).Within(0.01f));
        }
    }
}
