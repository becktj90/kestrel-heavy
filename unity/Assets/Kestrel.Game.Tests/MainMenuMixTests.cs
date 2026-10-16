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

        static VisualElement Build(bool reduce, out int launched, out int designed, out VisualElement full)
        {
            int l = 0, d = 0;
            full = new VisualElement(); var safe = new VisualElement();
            var root = MainMenuOverlay.Build(full, safe, () => l++, () => d++, () => { }, reduce);
            launched = l; designed = d;
            return root;
        }

        [Test]
        public void CopyIsHonestAndHasNoFakeData()
        {
            var all = MainMenuCopy.Launch.Caption + MainMenuCopy.TapHint + string.Join(" ", MainMenuCopy.Tiles.Select(t => t.Caption + t.Label));
            StringAssert.DoesNotContain("Next:", MainMenuCopy.LaunchCaption);
            StringAssert.DoesNotContain("NEW PARTS", all.ToUpperInvariant());
            Assert.That(Regex.IsMatch(all, @"T[\u2013\-]\d"), Is.False, "no countdown");
        }

        [Test]
        public void LaunchDesignAndMissionsAreLive()
        {
            Assert.That(MainMenuCopy.Launch.Live);
            Assert.That(MainMenuCopy.Design.Live);
            Assert.That(MainMenuCopy.Tiles.Where(t => t.Live).Select(t => t.Id), Is.EqualTo(new[] { "missions" }));
            Assert.That(MainMenuCopy.Tiles.Length, Is.EqualTo(2));
            Assert.That(MainMenuCopy.Tiles.Any(t => t.Id == "hangar"), Is.False);
        }

        [Test]
        public void OverlayBuildsControlsWithSpokenLabelsAndSoonTags()
        {
            var root = Build(false, out _, out _, out var full);
            string[] ids = { "home-launch", "home-design", "home-missions", "home-codex" };
            foreach (var id in ids)
            {
                var b = root.Q<Button>(id);
                Assert.That(b, Is.Not.Null, id);
                Assert.That(b.tooltip, Is.Not.Empty, id + " needs a spoken label");
            }
            var gear = full.Q<Button>("home-settings");
            Assert.That(gear, Is.Not.Null);
            Assert.That(gear.tooltip, Is.Not.Empty);
            Assert.That(root.Q<Button>("home-launch").enabledSelf);
            Assert.That(root.Q<Button>("home-design").enabledSelf);
            Assert.That(root.Q<Button>("home-missions").enabledSelf);
            Assert.That(root.Q("home-hangar"), Is.Null);
            var codex = root.Q<Button>("home-codex");
            Assert.That(codex.enabledSelf, Is.False);
            var tag = codex.Q<Label>("soon");
            Assert.That(tag, Is.Not.Null);
            Assert.That(tag.parent.ClassListContains("kh-mm-tile-head"));
            Assert.That(codex.Q(className: "kh-mm-tile-caption").parent, Is.SameAs(codex));
            Assert.That(root.Query<Label>("soon").ToList().Count, Is.EqualTo(1));
            Assert.That(root.Query<VisualElement>("lamp").ToList().Count, Is.EqualTo(3));
            Assert.That(full.Q("home-settings-sheet").style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(root.Q("mm-stats"), Is.Not.Null);
            Assert.That(root.Q<Label>("mm-fuel").text, Is.EqualTo(MainMenuCopy.FuelStat));
            Assert.That(root.Q<Label>("mm-thrust").text, Is.EqualTo(MainMenuCopy.ThrustStat));
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
            string[] steps = { "launch", "design", "missions", "codex" };
            for (int i = 1; i < steps.Length; i++)
                Assert.That(MainMenuCopy.IntroDelayMs(steps[i]) - MainMenuCopy.IntroDelayMs(steps[i - 1]), Is.EqualTo(70));
            Assert.That(MainMenuCopy.IntroDelayMs("codex"), Is.LessThan(1500));
        }

        [Test]
        public void HitTargetsAreAtLeast44px()
        {
            string uss = File.ReadAllText(Uss);
            foreach (var sel in new[] { ".kh-mm-launch", ".kh-mm-design", ".kh-mm-gear", ".kh-button.kh-mm-tile" })
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
                bool replaced = a.Clickable || a.Id == "title" || a.Id == "subtitle" || a.Id == "tel-l" || a.Id == "tel-r";
                Assert.That(hidden, Is.EqualTo(replaced), a.Id);
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
        public void NarrowClassAppliesBelow500AndTargetsStayLarge()
        {
            Assert.That(MainMenuCopy.IsNarrow(375f)); Assert.That(MainMenuCopy.IsNarrow(440f)); Assert.That(MainMenuCopy.IsNarrow(1032f), Is.False);
            string uss = File.ReadAllText(Uss);
            foreach (var m in Regex.Matches(uss, @"\.kh-mm-narrow \.(?:kh-mm-launch|kh-mm-design|kh-mm-gear|kh-button\.kh-mm-tile) \{[^}]*min-height:\s*(\d+)px").Cast<Match>())
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
                float need = 2f * MenuTypeScale.TileBorder + 2f * z.Pad + System.Math.Max(22f, z.TileName * MenuTypeScale.LineH) + 3f + z.TileCaptionLines * z.TileCaption * MenuTypeScale.LineH;
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
                // The controls end above the bottom film-reel bar (which already spans the bottom safe inset); they start ControlsH above that.
                float bar = MainMenuCopy.BarHeight(d.h, d.bottom);
                Assert.That(z.BottomReserve, Is.EqualTo(bar).Within(0.01f), d.name);
                Assert.That(z.ControlsTop, Is.EqualTo(d.h - bar - z.ControlsH).Within(0.01f), d.name);
                Assert.That(z.ControlsTop + z.ControlsH, Is.LessThanOrEqualTo(d.h - bar + 0.01f), d.name + ": the 2x2 tiles run under the bottom reel bar");
                Assert.That(z.HintBand + d.bottom, Is.GreaterThanOrEqualTo(bar - 0.01f), d.name + ": root padding-bottom plus safe inset clears the bar");
                // After the lens-shift lift, the rocket base (and so the telemetry panels above it) ends above the LAUNCH bar.
                Assert.That(MenuTypeScale.HeroOverlapPx(z, d.h), Is.LessThanOrEqualTo(-MenuTypeScale.HeroGapPx + 0.5f), d.name + ": LAUNCH bar cuts the rocket base or telemetry");
                // ...and the nose stays below the title block.
                float noseY = (MenuTypeScale.HeroTopFrac - z.HeroLift) * d.h;
                Assert.That(noseY, Is.GreaterThanOrEqualTo(z.TitleBottom + MenuTypeScale.HeroGapPx - 0.5f), d.name + ": title block reaches the rocket nose");
            }
        }

        [Test]
        public void ControlsBlockIsCompactOnPhones()
        {
            foreach (var d in Devices.Where(x => x.w < 500f))
            {
                var z = MenuTypeScale.Compute(d.w, d.h, d.top, d.bottom);
                Assert.That(z.ControlsH / (d.h - d.top - d.bottom), Is.LessThanOrEqualTo(0.42f), d.name + ": controls take too much of the usable height");
                Assert.That(z.LaunchCaptionLines, Is.EqualTo(1), d.name + ": LAUNCH caption is one line");
            }
        }

        [Test]
        public void HeroLiftIsZeroWhenThereIsRoomAndCappedByTheTitle()
        {
            Assert.That(MenuTypeScale.HeroLift(844f, 190f, 700f), Is.EqualTo(0f));
            float lift = MenuTypeScale.HeroLift(844f, 190f, 300f);            // controls far too high
            Assert.That(lift * 844f, Is.EqualTo(MenuTypeScale.HeroTopFrac * 844f - MenuTypeScale.HeroGapPx - 190f).Within(0.01f), "capped so the nose clears the title");
            float some = MenuTypeScale.HeroLift(844f, 190f, 0.53f * 844f);    // base touches the controls: lift the gap
            Assert.That(some * 844f, Is.EqualTo(MenuTypeScale.HeroGapPx).Within(0.01f));
            Assert.That(MenuTypeScale.HeroLift(0f, 0f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void TileNamesNeverEllipsizeAtAnySize()
        {
            // Names keep their scaled size only when it fits beside the SOON tag / lamp; otherwise they shrink, never below Min.
            foreach (var d in Devices)
            {
                var z = MenuTypeScale.Compute(d.w, d.h, d.top, d.bottom);
                foreach (var t in MainMenuCopy.Tiles)
                {
                    float n = MenuTypeScale.FitName(z, t.Label, !t.Live);
                    Assert.That(n, Is.InRange(MenuTypeScale.TileName.Min, z.TileName + 0.001f), d.name + " " + t.Label);
                    Assert.That(MenuTypeScale.NameFits(z, t.Label, !t.Live, n), d.name + " " + t.Label + " fits with " + MenuTypeScale.NameSlack + " px to spare");
                    float spare = z.TileInner - (t.Live ? MenuTypeScale.LampSlot : MenuTypeScale.TextW(MainMenuCopy.SoonTag, z.Soon) + MenuTypeScale.TagPad) - MenuTypeScale.TextW(t.Label, n);
                    Assert.That(spare, Is.GreaterThanOrEqualTo(MenuTypeScale.NameSlack - 0.01f), d.name + " " + t.Label);
                }
            }
        }

        [Test]
        public void TelemetryIsCrispOnDarkPanelsWithWideEnoughText()
        {
            var hud = MainMenuLayout.Style(MenuTextRole.Hud);
            Assert.That(hud.OutlineWidth, Is.EqualTo(0f)); Assert.That(hud.GlowOuter, Is.EqualTo(0f)); Assert.That(hud.GlowPower, Is.EqualTo(0f)); Assert.That(hud.FaceDilate, Is.EqualTo(0f));
            Assert.That(MainMenuMaterials.InkFor(MenuTextRole.Hud, true), Is.EqualTo(MainMenuMaterials.HudInk));
            Assert.That(MainMenuMaterials.HudInk.r + MainMenuMaterials.HudInk.g + MainMenuMaterials.HudInk.b, Is.EqualTo(3f), "pure white ink");
            var panel = MainMenuMaterials.HudPanel;
            Assert.That(panel.a, Is.EqualTo(1f), "opaque face, not emissive cyan glass");
            Assert.That(panel.r + panel.g + panel.b, Is.LessThan(0.3f), "dark face for contrast");
            // Accent palette is untouched (Trevor has not approved a recolour).
            Assert.That(MainMenuMaterials.Cyan, Is.EqualTo(new UnityEngine.Color(0.25f, 0.95f, 1f, 1f)));
            Assert.That(MainMenuText.PixelSize, Is.LessThanOrEqualTo(64), "atlas near on-screen size so small text does not shimmer");
            foreach (var id in new[] { "tel-l", "tel-r" })
            {
                var a = MainMenuLayout.Find(id);
                Assert.That(a.WidthFrac, Is.GreaterThanOrEqualTo(0.28f), id + " wide enough for 14-char lines at phone size");
                var box = MainMenuLayout.RestBox(a);
                Assert.That(box.X0, Is.GreaterThanOrEqualTo(0.06f), id + " keeps >=6% side margin (SE: ~19 px)"); Assert.That(box.X1, Is.LessThanOrEqualTo(0.94f));
            }
            Assert.That(MainMenuText.CoverageModeFor(UnityEngine.TextureFormat.Alpha8), Is.EqualTo(0f));
            Assert.That(MainMenuText.CoverageModeFor(UnityEngine.TextureFormat.RGBA32), Is.EqualTo(0f));
            Assert.That(MainMenuText.CoverageModeFor(UnityEngine.TextureFormat.R8), Is.EqualTo(1f));
            var fontSrc = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath, "Resources/Shaders/KH_Font.shader"));
            Assert.That(fontSrc, Does.Contain("_CoverageMode"));
            Assert.That(fontSrc, Does.Not.Contain("0.999h"), "no per-pixel alpha/rgb guess: it hollowed glyph bodies");
            // Every telemetry line is laid out at the same glyph size (as wide as the longest one).
            var verts = new System.Collections.Generic.List<UnityEngine.Vector3>(); var uvs = new System.Collections.Generic.List<UnityEngine.Vector2>(); var tris = new System.Collections.Generic.List<int>();
            var g = new System.Collections.Generic.List<MainMenuText.GlyphStamp>();
            for (int i = 0; i < 4; i++) g.Add(new MainMenuText.GlyphStamp(0f, 10f, 0f, 14f, 12f, default, default, default, default));
            Assert.That(MainMenuText.Layout(g, 1f, 1f, 0f, verts, uvs, tris, 96f), Is.True);
            float shortW = verts.Max(v => v.x) - verts.Min(v => v.x);
            Assert.That(shortW, Is.LessThan(0.6f), "a 4-glyph line must not stretch to the full width");
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
            Assert.That(MenuTypeScale.TileCaption.At(9f), Is.EqualTo(18f), "cap = 18 per the type-scale table");
        }

        [Test]
        public void ApplyScaleSetsInlineSizesOnTheBuiltOverlay()
        {
            var root = Build(false, out _, out _, out _);
            var z = MenuTypeScale.Compute(390f, 844f, 47f, 34f);
            MainMenuOverlay.ApplyScale(root, z);
            Assert.That(root.Q<Label>("mm-kestrel").style.fontSize.value.value, Is.EqualTo(z.Title).Within(0.01f));
            Assert.That(root.Q<Button>("home-missions").style.minHeight.value.value, Is.EqualTo(z.TileH).Within(0.01f));
            Assert.That(root.Q<Button>("home-missions").style.height.value.value, Is.EqualTo(z.TileH).Within(0.01f), "exact height, so the block is what the maths says");
            Assert.That(root.Q<Button>("home-launch").style.height.value.value, Is.EqualTo(z.LaunchH).Within(0.01f));
            Assert.That(root.Q<Button>("home-design").style.height.value.value, Is.EqualTo(z.DesignH).Within(0.01f));
            Assert.That(root.Q("mm-stats").style.minHeight.value.value, Is.GreaterThanOrEqualTo(36f));
            Assert.That(root.Q<Label>("mm-fuel").style.fontSize.value.value, Is.GreaterThanOrEqualTo(13f));
            foreach (var it in MainMenuCopy.Tiles)
            {
                var name = root.Q<Button>("home-" + it.Id).Q(className: "kh-mm-tile-name");
                Assert.That(name.style.textOverflow.value, Is.EqualTo(TextOverflow.Clip), it.Id + ": no ellipsis");
                Assert.That(name.style.flexShrink.value, Is.EqualTo(0f), it.Id);
                Assert.That(name.style.fontSize.value.value, Is.EqualTo(MenuTypeScale.FitName(z, it.Label, !it.Live)).Within(0.01f), it.Id);
            }
            Assert.That(root.Q<Button>("home-codex").Q(className: "kh-mm-soon").style.fontSize.value.value, Is.EqualTo(z.Soon).Within(0.01f));
        }
    }
}
