// Home at real UI Toolkit rects: iOS portrait (TestFlight 156/158 on Trevor's iPhone), portrait web, and the wide web
// Home. The iOS shell panel is a 375x667 reference matched to height, so a 390x844 or 430x932 iPhone lays Home out in a
// ~308x667 panel, and an 820x1180 iPad portrait in ~464x667; each also runs at its literal point size (portrait web),
// plus 335x667 web portrait. Wide web: 1280x720, 1366x768, 1920x1080.
// Every size runs with and without a save, on the Starter Home (home.* flags off: ONE CTA, MISSIONS, the gear) and on
// the full Home (all home.* flags on). Checked: no visible block on another or outside the safe column, exactly one
// primary CTA (CONTINUE with a save, else LAUNCH with the mission line) and no second CONTINUE, hidden extras take no
// room, the stats card holds its two lines, DESIGN FIRST and tile captions are not clipped, tile names clear their glyph
// and lamp, Thrum's name sits over his words, and the guide pop's rest clears the gear and title.
using System;
using System.Collections.Generic;
using System.Reflection;
using Kestrel.Core.Features;
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
using Kestrel.UiChecks;
using Kestrel.Game.Shell;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class IosPortraitHomeDeviceTests
    {
        const string Tokens = "Assets/Kestrel.Game/Shell/UI/KH_Tokens.uss";
        const string LongRecord = "20 flights cleared · 888.88M Kc · 20 patches";
        const string Mission = "KH-3 · Tiny Orbit";

        struct Size { public string Name; public HomePhoneScreen Screen; }

        static IEnumerable<Size> Sizes()
        {
            foreach (var (name, w, h, top, bottom) in new[]
            {
                ("375x667 SE", 375f, 667f, 20f, 0f),
                ("390x844", 390f, 844f, 47f, 34f),
                ("430x932", 430f, 932f, 59f, 34f),
                ("820x1180 iPad", 820f, 1180f, 24f, 20f),
            })
            {
                yield return new Size { Name = name + " (iOS shell panel)", Screen = HomePhoneLayout.Device(w, h, top, bottom) };
                if (h > 667f)
                    yield return new Size
                    {
                        Name = name + " (points)",
                        Screen = new HomePhoneScreen { W = w, H = h, Top = top, Bottom = bottom, Left = 8f, Right = 8f, PerPoint = 1f },
                    };
            }
            yield return new Size
            {
                Name = "335x667 web portrait",
                Screen = new HomePhoneScreen { W = 335f, H = 667f, Top = 0f, Bottom = 0f, Left = 8f, Right = 8f, PerPoint = 1f },
            };
        }

        /// <summary>Starter is data/features.json as shipped (all five home.* flags off); full turns them on.</summary>
        static readonly (string Name, HomeShow Show)[] Modes = { ("starter", HomeShow.None), ("full", HomeShow.Full) };

        static void UseFlags(HomeShow show) => FeatureFlags.Active = HomeFeatures.With(show);

        FeatureFlags savedFlags;
        bool savedIntro;

        [SetUp]
        public void Column()
        {
            MainMenuOverlay.ForceWide = false;
            savedFlags = FeatureFlags.Active;
            HomeFlagsBoot.Loaded = true;
            savedIntro = MainMenuOverlay.IntroPlayed;
        }

        [TearDown]
        public void Reset()
        {
            MainMenuOverlay.ForceWide = null;
            FeatureFlags.Active = savedFlags;
            MainMenuOverlay.IntroPlayed = savedIntro;
        }

        [Test]
        public void HomeReappliesItsExtrasWhenAFlagChanges()
        {
            UseFlags(HomeShow.None);
            var s = new HomePhoneScreen { W = 390f, H = 844f, Top = 47f, Bottom = 34f, Left = 8f, Right = 8f, PerPoint = 1f };
            using (var surface = UiDeviceSurface.Open(s.W, s.H))
            {
                var shell = Shell(surface.Stage, s, out _, out var host);
                var overlay = Home(shell, host, false);
                surface.Pump(6);
                Assert.That(overlay.Q("home-thrum").resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "Thrum off");
                FeatureFlags.Active.SetOverride(FeatureKeys.HomeThrum, true);
                FeatureFlags.Active.SetOverride(FeatureKeys.HomeCodexTile, true);
                surface.Pump(6);
                Assert.That(overlay.Q("home-thrum").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "Thrum back on Changed");
                Assert.That(overlay.Q("home-codex").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "Codex back on Changed");
                Assert.That(shell.Q("settings-codex").resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "Settings Codex row goes with the tile");
                var blocks = Visible(overlay, shell, "after Changed");
                NoOverlaps(blocks, "after Changed");
            }
        }

        [Test]
        public void PortraitHomeHasNoOverlapsAndEveryWordFits()
        {
            foreach (var mode in Modes)
                foreach (bool save in new[] { false, true })
                    foreach (var size in Sizes())
                    {
                        UseFlags(mode.Show);
                        Portrait(size, mode.Show, save, size.Name + " " + mode.Name + (save ? " save" : " no save"));
                    }
        }

        [Test]
        public void WideWebHomeHasOneCtaAndNoOverlaps()
        {
            foreach (var mode in Modes)
                foreach (bool save in new[] { false, true })
                    foreach (var (w, h) in WideSizes)
                    {
                        UseFlags(mode.Show);
                        MainMenuOverlay.ForceWide = true;
                        string n = w + "x" + h + " " + mode.Name + (save ? " save" : " no save");
                        var s = new HomePhoneScreen { W = w, H = h, PerPoint = 1f };
                        using (var surface = UiDeviceSurface.Open(w, h))
                        {
                            var shell = Shell(surface.Stage, s, out var safe, out var host);
                            var overlay = Home(shell, host, save);
                            surface.Pump(8);
                            PlaceWideGear(shell, w, h);
                            surface.Pump(3);
                            TextFit.Apply(shell);
                            surface.Pump(4);
                            Assert.That(overlay.ClassListContains("kh-mm-wide"), n + " wide frame");
                            CheckCta(overlay, shell, save, n);
                            CheckHidden(overlay, mode.Show, n);
                            var blocks = Visible(overlay, shell, n);
                            NoOverlaps(blocks, n);
                            foreach (var (id, e) in blocks)
                                if (id != "gear") Inside(e.worldBound, safe.worldBound, n + " " + id);
                            var missions = overlay.Q("home-missions");
                            AssertWords(missions.Q<Label>(className: "kh-mm-tile-name"), n + " MISSIONS name");
                        }
                    }
        }

        static VisualElement Home(VisualElement shell, VisualElement host, bool save)
        {
            var overlay = MainMenuOverlay.Build(shell, host, () => { }, () => { }, () => { }, true);
            MainMenuOverlay.SetMissionLine(overlay, Mission);
            MainMenuOverlay.SetRecord(overlay, LongRecord);
            MainMenuOverlay.SetMissionsFlown(overlay, 2, 20);
            HomeResumeBar.Mount(overlay, 72, save, () => { });
            return overlay;
        }

        /// <summary>One CTA: CONTINUE with a save (and no second CONTINUE), else LAUNCH over the mission line.</summary>
        static void CheckCta(VisualElement overlay, VisualElement shell, bool save, string n)
        {
            int primary = 0;
            overlay.Query(className: "kh-mm-launch").ForEach(_ => primary++);
            Assert.That(primary, Is.EqualTo(1), n + " one primary CTA");
            var key = overlay.Q<Button>("home-primary");
            var label = key.Q<Label>("mm-launch-label");
            var cap = key.Q<Label>("mm-launch-caption");
            // STARTER_COPY.md: CONTINUE over "KH-3 · Tiny Orbit"; else "LAUNCH KH-3 · Tiny Orbit", or "LAUNCH KH-3" over the name.
            if (save)
            {
                Assert.That(label.text, Is.EqualTo(HomePhoneLayout.Continue), n + " CTA word");
                Assert.That(cap.text, Is.EqualTo(Mission), n + " CONTINUE sub-line");
            }
            else if (label.text == "LAUNCH " + Mission)
                Assert.That(cap.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), n + " no sub-line under the whole label");
            else
            {
                Assert.That(label.text, Is.EqualTo("LAUNCH KH-3"), n + " short CTA");
                Assert.That(cap.text, Is.EqualTo("Tiny Orbit"), n + " name as the sub-line");
            }
            Assert.That(MainMenuOverlay.CtaIsContinue(overlay), Is.EqualTo(save), n);
            Assert.That(shell.Q(HomeResumeBar.ContinueName), Is.Null, n + " no second CONTINUE in the credits bar");
            Assert.That(key.worldBound.height, Is.GreaterThanOrEqualTo(40f), n + " CTA a full touch target");
            Inside(label.worldBound, key.worldBound, n + " CTA label");
            if (cap.resolvedStyle.display != DisplayStyle.None) Inside(cap.worldBound, key.worldBound, n + " CTA caption");
            AssertWords(label, n + " CTA label");
            AssertWords(cap, n + " CTA caption");
            var missions = overlay.Q("home-missions");
            Assert.That(missions, Is.Not.Null, n + " MISSIONS");
            Assert.That(missions.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), n + " MISSIONS shown");
            Assert.That(missions.worldBound.height, Is.GreaterThanOrEqualTo(40f), n + " MISSIONS a full touch target");
            Assert.That(missions.Q<Label>(className: "kh-mm-tile-caption").text, Is.EqualTo("2 of 20 flown"), n + " MISSIONS sub-line");
            var gear = shell.Q<Button>("home-settings");
            Assert.That(gear, Is.Not.Null, n + " Settings gear");
            Assert.That(gear.tooltip, Is.EqualTo("Settings"), n + " gear VoiceOver label");
        }

        static void CheckHidden(VisualElement overlay, HomeShow show, string n)
        {
            foreach (var (id, on) in new[] { (MainMenuOverlay.DesignId, show.DesignFirst), (MainMenuOverlay.CodexId, show.Codex), (MainMenuOverlay.ThrumId, show.Thrum), (MainMenuOverlay.StatsId, show.Stats) })
            {
                var e = overlay.Q(id);
                Assert.That(e, Is.Not.Null, n + " " + id + " in the tree");
                Assert.That(e.resolvedStyle.display, Is.EqualTo(on ? DisplayStyle.Flex : DisplayStyle.None), n + " " + id + " display");
                if (!on) Assert.That(e.layout.height, Is.LessThan(0.5f), n + " " + id + " hidden takes no height");
            }
            var bar = overlay.Q(HomeResumeBar.BarName);
            Assert.That(bar, Is.Not.Null, n + " credits bar in the tree");
            Assert.That(bar.resolvedStyle.display, Is.EqualTo(show.Credits ? DisplayStyle.Flex : DisplayStyle.None), n + " credits chip display");
        }

        /// <summary>The visible top-level blocks (hidden ones are skipped).</summary>
        static List<(string, VisualElement)> Visible(VisualElement overlay, VisualElement shell, string n)
        {
            var blocks = new List<(string, VisualElement)>();
            var bar = overlay.Q(HomeResumeBar.BarName);
            if (bar != null && bar.resolvedStyle.display != DisplayStyle.None) blocks.Add(("credits band", bar));
            var gear = shell.Q("home-settings");
            Assert.That(gear, Is.Not.Null, n);
            blocks.Add(("gear", gear));
            foreach (var id in new[] { "mm-title", "mm-stats", "home-primary", "home-design", "home-missions", "home-codex", "home-thrum" })
            {
                var e = overlay.Q(id);
                Assert.That(e, Is.Not.Null, n + " " + id);
                if (e.resolvedStyle.display == DisplayStyle.None) continue;
                Assert.That(e.worldBound.width, Is.GreaterThan(1f), n + " " + id + " laid out");
                blocks.Add((id, e));
            }
            return blocks;
        }

        static void NoOverlaps(List<(string, VisualElement)> blocks, string n)
        {
            for (int a = 0; a < blocks.Count; a++)
                for (int b = a + 1; b < blocks.Count; b++)
                    Assert.That(Overlap(blocks[a].Item2.worldBound, blocks[b].Item2.worldBound), Is.False,
                        n + ": " + blocks[a].Item1 + " " + blocks[a].Item2.worldBound + " on " + blocks[b].Item1 + " " + blocks[b].Item2.worldBound);
        }

        static void Portrait(Size size, HomeShow show, bool save, string n)
        {
            var s = size.Screen;
            using (var surface = UiDeviceSurface.Open(s.W, s.H))
            {
                var shell = Shell(surface.Stage, s, out var safe, out var host);
                var overlay = Home(shell, host, save);
                PlaceGear(shell, s);
                surface.Pump(8);
                TextFit.Apply(shell);
                surface.Pump(4);
                PlaceGear(shell, s);
                surface.Pump(3);

                var stage = surface.Stage.worldBound;
                float k = stage.width / s.W;   // world units per panel unit
                var safeRect = safe.worldBound;

                // 1. Visible top-level blocks: none on another, all inside the safe column; hidden extras take no room.
                CheckHidden(overlay, show, n);
                var gear = shell.Q("home-settings");
                var bar = overlay.Q(HomeResumeBar.BarName);
                if (bar != null && bar.resolvedStyle.display == DisplayStyle.None) bar = null;
                var blocks = Visible(overlay, shell, n);
                NoOverlaps(blocks, n);
                foreach (var (id, e) in blocks)
                    if (id != "gear") Inside(e.worldBound, safeRect, n + " " + id);
                Assert.That(gear.worldBound.yMin, Is.GreaterThanOrEqualTo(safeRect.yMin - 0.5f), n + ": gear under the status bar");
                foreach (var id in new[] { "mm-eyebrow", "mm-kestrel", "mm-heavy" })
                {
                    var e = overlay.Q(id);
                    if (bar != null) Assert.That(Overlap(bar.worldBound, e.worldBound), Is.False, n + " credits on " + id);
                    Assert.That(Overlap(gear.worldBound, e.worldBound), Is.False, n + " gear on " + id);
                    AssertWords(e as TextElement, n + " " + id);
                }

                // 2. One primary CTA (CONTINUE or LAUNCH), MISSIONS and the gear.
                CheckCta(overlay, shell, save, n);
                if (bar != null)
                {
                    var chip = bar.Q<Label>(HomeResumeBar.ChipName);
                    Assert.That(chip.resolvedStyle.fontSize, Is.GreaterThanOrEqualTo(14f), n + " credits size");
                    AssertWords(chip, n + " credits");
                }

                // 3. Stats: two lines inside the card, the record on one line.
                var stats = overlay.Q("mm-stats");
                if (show.Stats)
                {
                    foreach (var id in new[] { "mm-fuel", "mm-thrust", "mm-record" })
                        Inside(overlay.Q<Label>(id).worldBound, stats.worldBound, n + " " + id + " inside the stats card");
                    var record = overlay.Q<Label>("mm-record");
                    Assert.That(record.text, Does.Contain("Kc").And.Contain("888.88M"), n + " compact Kc");
                    var one = record.MeasureTextSize(record.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);
                    Assert.That(one.x, Is.LessThanOrEqualTo(record.contentRect.width + TextFit.Slack), n + " record on one line: " + record.text);
                    Assert.That(overlay.Q("mm-fuel").worldBound.yMax, Is.LessThanOrEqualTo(record.worldBound.yMin + 1f), n + " pills over the record");
                }

                // 4. DESIGN FIRST: label and caption inside the button.
                if (show.DesignFirst)
                    foreach (var cls in new[] { "kh-mm-design-label", "kh-mm-design-caption" })
                    {
                        var box = overlay.Q("home-design");
                        var e = box.Q<Label>(className: cls);
                        Inside(e.worldBound, box.worldBound, n + " " + cls);
                        AssertWords(e, n + " " + cls);
                    }

                // 5. Tiles: name, glyph and lamp side by side; the caption off the bottom edge. Starter: MISSIONS spans the row.
                foreach (var it in MainMenuCopy.Tiles)
                {
                    var tile = overlay.Q("home-" + it.Id);
                    if (tile.resolvedStyle.display == DisplayStyle.None) continue;
                    var head = tile.Q(className: "kh-mm-tile-head");
                    var name = tile.Q<Label>(className: "kh-mm-tile-name");
                    var cap = tile.Q<Label>(className: "kh-mm-tile-caption");
                    var parts = new List<VisualElement>();
                    foreach (var c in head.Children()) if (c.resolvedStyle.display != DisplayStyle.None && c.worldBound.width > 0.5f) parts.Add(c);
                    for (int a = 0; a < parts.Count; a++)
                        for (int b = a + 1; b < parts.Count; b++)
                            Assert.That(Overlap(parts[a].worldBound, parts[b].worldBound), Is.False, n + " " + it.Id + " head: " + parts[a].name + "/" + parts[b].GetType().Name);
                    // The name leads (flush with the caption under it); the glyph trails it, then the lamp.
                    Assert.That(Math.Abs(name.worldBound.xMin - head.worldBound.xMin), Is.LessThanOrEqualTo(2f * k), n + " " + it.Id + " name at the start");
                    Assert.That(Math.Abs(cap.worldBound.xMin - name.worldBound.xMin), Is.LessThanOrEqualTo(1.5f * k), n + " " + it.Id + " caption under the name");
                    AssertWords(name, n + " " + it.Id + " name");
                    AssertWords(cap, n + " " + it.Id + " caption");
                    Inside(cap.worldBound, tile.worldBound, n + " " + it.Id + " caption");
                    Assert.That(cap.worldBound.yMax, Is.LessThanOrEqualTo(tile.worldBound.yMax - 1f * k), n + " " + it.Id + " caption off the bottom edge");
                }
                if (!show.Codex)
                {
                    var missions = overlay.Q("home-missions");
                    var launch = overlay.Q("home-primary");
                    Assert.That(missions.worldBound.width, Is.GreaterThanOrEqualTo(launch.worldBound.width - 2f * k), n + " MISSIONS spans the row");
                }

                // 6. Thrum: the name over his words (left-aligned), and the shown words fit the strip.
                if (show.Thrum)
                {
                    var strip = overlay.Q("home-thrum");
                    var who = strip.Q<Label>("mm-thrum-name");
                    var line = strip.Q<Label>("mm-hint-line");
                    Inside(line.worldBound, strip.worldBound, n + " Thrum line");
                    AssertWords(line, n + " Thrum line");
                    if (who != null && who.resolvedStyle.display != DisplayStyle.None)
                    {
                        var nameText = who.MeasureTextSize(who.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);
                        float drawnLeft = who.contentRect.xMin + who.worldBound.xMin;
                        Assert.That(who.style.unityTextAlign.value, Is.EqualTo(TextAnchor.UpperLeft), n + " Thrum name left-aligned, not floating mid-strip");
                        Assert.That(Math.Abs(drawnLeft - line.worldBound.xMin), Is.LessThanOrEqualTo(2f * k), n + " Thrum name over his words");
                        Assert.That(nameText.x, Is.LessThanOrEqualTo(who.contentRect.width + TextFit.Slack), n);
                    }
                }

                // 7. Guide pop: its rest clears the gear and the wordmark, and stays above the first control.
                float clear = MainMenuOverlay.PopClearTop(overlay, shell);
                Assert.That(clear + shell.worldBound.yMin, Is.GreaterThanOrEqualTo(gear.worldBound.yMax - 0.5f), n + " pop under the gear");
                Assert.That(clear + shell.worldBound.yMin, Is.GreaterThanOrEqualTo(overlay.Q("mm-title").worldBound.yMax - 0.5f), n + " pop under the title");
                float pop = (HomePhoneLayout.GuidePopPt + HomePhoneLayout.GuideMarginPt) * s.PerPoint * k;
                var first = show.Stats ? stats : overlay.Q("home-primary");
                Assert.That(shell.worldBound.yMin + clear + pop, Is.LessThanOrEqualTo(first.worldBound.yMin + 0.5f), n + " pop above the first control");
            }
        }

        // ---- real tree: every label's text, every overlap, the screen and the safe area -------------------------

        /// <summary>Landscape web viewports (1280x800 from Arcade's live play-through).</summary>
        static readonly (float, float)[] WideSizes = { (1280f, 720f), (1280f, 800f), (1366f, 768f), (1920f, 1080f) };

        /// <summary>The panel a landscape web viewport really lays out in: WebPanelScale's 1280x720 reference, matched halfway.</summary>
        static HomePhoneScreen WebPanel(float w, float h)
        {
            WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
            float k = WebLandscapeType.PanelScale(w, h, rw, rh, m);
            return new HomePhoneScreen { W = Mathf.Round(w / k), H = Mathf.Round(h / k), PerPoint = 1f };
        }

        static readonly (string Name, HomeShow Show, bool Save)[] TreeModes =
        {
            ("starter save", HomeShow.None, true), ("starter no save", HomeShow.None, false), ("full save", HomeShow.Full, true),
        };

        /// <summary>
        /// The whole Home tree (the real MainMenuOverlay with KH_Tokens.uss and the runtime theme, in a panel at each device
        /// size, after layout and TextFit) goes through UiTreeChecks: every visible label's measured text inside its own
        /// content rect and its parent's box, no two visible leaves on each other, nothing off the screen or outside the safe
        /// area. Then the Settings sheet the gear opens, the same way. All sizes and modes are collected before the assert,
        /// so one CI run lists every fault.
        /// </summary>
        [Test]
        public void RealTreeEveryLabelFitsNothingOverlapsNothingLeaves()
        {
            var problems = new List<string>();
            foreach (var mode in TreeModes)
            {
                foreach (var size in Sizes())
                    problems.AddRange(RealTree(size.Screen, false, mode.Show, mode.Save, size.Name + " " + mode.Name));
                foreach (var (w, h) in WideSizes)
                    problems.AddRange(RealTree(WebPanel(w, h), true, mode.Show, mode.Save, "web " + w + "x" + h + " " + mode.Name));
            }
            Assert.That(problems, Is.Empty, UiTreeChecks.Report(problems));
        }

        static List<string> RealTree(HomePhoneScreen s, bool wide, HomeShow show, bool save, string n)
        {
            var problems = new List<string>();
            UseFlags(show);
            MainMenuOverlay.ForceWide = wide;
            using (var surface = UiDeviceSurface.Open(s.W, s.H))
            {
                var shell = Shell(surface.Stage, s, out var safe, out var host);
                var overlay = Home(shell, host, save);
                if (wide) PlaceWideGear(shell, s.W, s.H); else PlaceGear(shell, s);
                surface.Pump(8);
                TextFit.Apply(shell);
                surface.Pump(4);
                if (wide) PlaceWideGear(shell, s.W, s.H); else PlaceGear(shell, s);
                surface.Pump(3);
                var screen = surface.Stage.worldBound;
                problems.AddRange(UiTreeChecks.Problems(shell, screen, safe.worldBound, n));
                if (overlay.Q(UiIds.HomePrimary) == null) problems.Add(n + ": no " + UiIds.HomePrimary);

                // Settings: open it from the gear, check the sheet alone (it sits over a dimmed Home), then Close.
                var gear = shell.Q<Button>(UiIds.HomeSettings);
                Click(gear);
                surface.Pump(6);
                var sheet = shell.Q(UiIds.ScreenSettings);
                if (sheet == null || sheet.resolvedStyle.display == DisplayStyle.None) problems.Add(n + ": the gear did not open " + UiIds.ScreenSettings);
                else
                {
                    TextFit.Apply(sheet);
                    surface.Pump(3);
                    problems.AddRange(UiTreeChecks.Problems(sheet, screen, safe.worldBound, n + " settings", e => true));
                    var close = sheet.Q<Button>(UiIds.SettingsClose);
                    if (close == null || !UiTreeChecks.Visible(close)) problems.Add(n + ": settings has no visible " + UiIds.SettingsClose);
                    var list = sheet.Q<ScrollView>("settings-list");
                    if (list != null && list.horizontalScroller != null && UiTreeChecks.Visible(list.horizontalScroller))
                        problems.Add(n + ": settings shows a horizontal scrollbar");
                    Click(close);
                    surface.Pump(4);
                    if (sheet.resolvedStyle.display != DisplayStyle.None) problems.Add(n + ": " + UiIds.SettingsClose + " did not close the sheet");
                    var scrim = shell.Q(MainMenuOverlay.SettingsScrimName);
                    if (scrim != null && scrim.resolvedStyle.display != DisplayStyle.None) problems.Add(n + ": the settings scrim stayed up");
                }
            }
            return problems;
        }

        static void Click(Button b)
        {
            if (b == null) return;
            using (var e = NavigationSubmitEvent.GetPooled())
            {
                e.target = b;
                b.SendEvent(e);
            }
        }

        /// <summary>Nav harness contract: every Home and Settings id in UiIds names one element of the real tree.</summary>
        [Test]
        public void EveryHomeAndSettingsUiIdIsInTheTree()
        {
            UseFlags(HomeShow.Full);
            var s = new HomePhoneScreen { W = 390f, H = 844f, Top = 47f, Bottom = 34f, Left = 8f, Right = 8f, PerPoint = 1f };
            using (var surface = UiDeviceSurface.Open(s.W, s.H))
            {
                var shell = Shell(surface.Stage, s, out _, out var host);
                Home(shell, host, true);
                surface.Pump(6);
                foreach (var id in new[] { UiIds.ScreenHome, UiIds.ScreenSettings })
                    Assert.That(shell.Q(id), Is.Not.Null, id);
                foreach (var id in UiIds.HomeControls)
                    Assert.That(shell.Q<Button>(id), Is.Not.Null, id);
                foreach (var id in UiIds.SettingsControls)
                    Assert.That(shell.Q<Button>(id), Is.Not.Null, id);
                Assert.That(shell.Q<Button>(MainMenuOverlay.LipName), Is.Null, "the lip is paint, not a control");
                var lip = shell.Q(MainMenuOverlay.LipName);
                var key = shell.Q(UiIds.HomePrimary);
                Assert.That(lip, Is.Not.Null, "LAUNCH/CONTINUE lip");
                Assert.That(lip.parent, Is.SameAs(key), "the lip lives inside the button");
                Assert.That(key.resolvedStyle.borderBottomWidth, Is.LessThanOrEqualTo(1.01f), "no 8 px bottom border sticking out");
                var lb = lip.worldBound; var kb = key.worldBound;
                Assert.That(lb.xMin >= kb.xMin - 0.5f && lb.xMax <= kb.xMax + 0.5f && lb.yMax <= kb.yMax + 0.5f && lb.yMin >= kb.yMin - 0.5f,
                    "lip " + lb + " inside the button " + kb);
                // The guide pop is not part of Home and nothing reads like a placeholder.
                shell.Query<Label>().ForEach(l => Assert.That(l.text ?? "", Does.Not.Contain("placeholder").IgnoreCase.And.Not.EqualTo("pop"), l.name));
            }
        }

        /// <summary>Tile captions start under the tile name (same left edge), not under the glyph.</summary>
        [Test]
        public void TileNameAndCaptionShareTheirLeftEdge()
        {
            foreach (var mode in Modes)
                foreach (var size in Sizes())
                {
                    UseFlags(mode.Show);
                    var s = size.Screen;
                    using (var surface = UiDeviceSurface.Open(s.W, s.H))
                    {
                        var shell = Shell(surface.Stage, s, out _, out var host);
                        var overlay = Home(shell, host, false);
                        surface.Pump(8);
                        TextFit.Apply(shell);
                        surface.Pump(3);
                        foreach (var it in MainMenuCopy.Tiles)
                        {
                            var tile = overlay.Q("home-" + it.Id);
                            if (tile.resolvedStyle.display == DisplayStyle.None) continue;
                            var name = tile.Q<Label>(className: "kh-mm-tile-name");
                            var cap = tile.Q<Label>(className: "kh-mm-tile-caption");
                            float a = UiTreeChecks.TextRect(name).xMin, b = UiTreeChecks.TextRect(cap).xMin;
                            Assert.That(Math.Abs(a - b), Is.LessThanOrEqualTo(1.5f), size.Name + " " + mode.Name + " " + it.Id + " name x " + a + " caption x " + b);
                        }
                    }
                }
        }

        /// <summary>Starter Home (home.thrum off) keeps Thrum's corner pop off Home; full Home lets it show.</summary>
        [Test]
        public void GuidePopStaysOffStarterHome()
        {
            var s = new HomePhoneScreen { W = 390f, H = 844f, Top = 47f, Bottom = 34f, Left = 8f, Right = 8f, PerPoint = 1f };
            foreach (var mode in Modes)
            {
                UseFlags(mode.Show);
                using (var surface = UiDeviceSurface.Open(s.W, s.H))
                {
                    var shell = Shell(surface.Stage, s, out _, out var host);
                    Home(shell, host, false);
                    surface.Pump(4);
                    Assert.That(Kestrel.Game.Guide.GuideBoxDriver.HideOnMenu, Is.Not.Null, mode.Name);
                    Assert.That(Kestrel.Game.Guide.GuideBoxDriver.HideOnMenu(), Is.EqualTo(!mode.Show.Thrum), mode.Name);
                }
            }
        }

        /// <summary>F8: coming back to Home (not the first Home of the session) shows every control on the first frame.</summary>
        [Test]
        public void ReturningHomeIsWholeOnItsFirstFrame()
        {
            UseFlags(HomeShow.None);
            MainMenuOverlay.IntroPlayed = true;
            var s = new HomePhoneScreen { W = 390f, H = 844f, Top = 47f, Bottom = 34f, Left = 8f, Right = 8f, PerPoint = 1f };
            using (var surface = UiDeviceSurface.Open(s.W, s.H))
            {
                var shell = Shell(surface.Stage, s, out _, out var host);
                var overlay = MainMenuOverlay.Build(shell, host, () => { }, () => { }, () => { }, false);
                Assert.That(overlay.ClassListContains(MainMenuOverlay.InstantClass), "no intro on a return");
                foreach (var id in new[] { "mm-title", UiIds.HomePrimary, UiIds.HomeMissions })
                    Assert.That(overlay.Q(id).ClassListContains("kh-mm-in"), id + " shown at once");
                Assert.That(shell.Q(UiIds.HomeSettings).ClassListContains("kh-mm-in"), "gear shown at once");
            }
        }

        /// <summary>Wide web: the gear where ScreenStub puts it on WebGL (WebHomeLayout's frame, CSS = panel here).</summary>
        static void PlaceWideGear(VisualElement shell, float w, float h)
        {
            var gear = shell.Q(UiIds.HomeSettings);
            if (gear == null) return;
            var frame = WebHomeLayout.Place(w, h, 0f, 0f, 0f, 0f);
            gear.style.position = Position.Absolute;
            gear.style.left = frame.Gear.X; gear.style.top = frame.Gear.Y; gear.style.width = frame.Gear.W; gear.style.height = frame.Gear.H;
            gear.style.right = StyleKeyword.Auto;
        }

        // ---- helpers ------------------------------------------------------------------------------

        static VisualElement Shell(VisualElement stage, HomePhoneScreen s, out VisualElement safe, out VisualElement host)
        {
            var shell = new VisualElement { name = "shell", pickingMode = PickingMode.Ignore };
            shell.AddToClassList("kh-root");
            shell.style.position = Position.Absolute;
            shell.style.left = 0f; shell.style.right = 0f; shell.style.top = 0f; shell.style.bottom = 0f;
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(Tokens);
            if (sheet != null) shell.styleSheets.Add(sheet);
            stage.Add(shell);
            // SafeAreaRoot: the content rect as absolute offsets (not padding).
            safe = new VisualElement { name = "safe", pickingMode = PickingMode.Ignore };
            safe.AddToClassList("kh-safe");
            safe.style.position = Position.Absolute;
            safe.style.left = s.Left; safe.style.right = s.Right; safe.style.top = s.Top; safe.style.bottom = s.Bottom;
            shell.Add(safe);
            host = new VisualElement { name = "home-host", pickingMode = PickingMode.Ignore };
            host.AddToClassList("kh-screen");
            host.AddToClassList("kh-home");
            ScreenStub.FillHost(host);
            safe.Add(host);
            return shell;
        }

        /// <summary>ScreenStub's iOS gear placement: the top-right corner at the safe top.</summary>
        static void PlaceGear(VisualElement shell, HomePhoneScreen s)
        {
            var gear = shell.Q("home-settings");
            if (gear == null) return;
            HomeCornerLayout.Place(s.W, s.Top, false, out _, out var box, out _);
            gear.style.position = Position.Absolute;
            gear.style.left = box.X; gear.style.top = box.Y; gear.style.width = box.W; gear.style.height = box.H;
            gear.style.right = StyleKeyword.Auto;
        }

        static bool Overlap(Rect a, Rect b) => a.xMax > b.xMin + 0.5f && b.xMax > a.xMin + 0.5f && a.yMax > b.yMin + 0.5f && b.yMax > a.yMin + 0.5f;

        static void Inside(Rect inner, Rect outer, string what)
        {
            Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin - 0.5f), what + " left " + inner + " in " + outer);
            Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax + 0.5f), what + " right " + inner + " in " + outer);
            Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin - 0.5f), what + " top " + inner + " in " + outer);
            Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax + 0.5f), what + " bottom " + inner + " in " + outer);
        }

        static void AssertWords(TextElement e, string where)
        {
            if (e == null || string.IsNullOrEmpty(e.text)) return;
            float w = e.contentRect.width;
            if (float.IsNaN(w) || w < 1f) w = e.layout.width;
            float h = e.contentRect.height;
            if (float.IsNaN(h) || h < 1f) h = e.layout.height;
            var one = e.MeasureTextSize(e.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);
            if (e.resolvedStyle.whiteSpace == WhiteSpace.NoWrap)
            {
                Assert.That(one.x, Is.LessThanOrEqualTo(w + TextFit.Slack), where + " need " + one.x.ToString("0") + " box " + w.ToString("0"));
                return;
            }
            // Wrapping copy: the longest word fits the box, and the wrapped text fits its height.
            float longest = 0f;
            foreach (var word in e.text.Split(' '))
            {
                if (word.Length == 0) continue;
                longest = Mathf.Max(longest, e.MeasureTextSize(word, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x);
            }
            Assert.That(longest, Is.LessThanOrEqualTo(w + TextFit.Slack), where + " a word is wider than its box (" + longest.ToString("0") + " > " + w.ToString("0") + ")");
            var need = e.MeasureTextSize(e.text, w, VisualElement.MeasureMode.AtMost, 0f, VisualElement.MeasureMode.Undefined);
            Assert.That(need.y, Is.LessThanOrEqualTo(h + TextFit.Slack), where + " tall need " + need.y.ToString("0") + " box " + h.ToString("0"));
        }

    }
}
