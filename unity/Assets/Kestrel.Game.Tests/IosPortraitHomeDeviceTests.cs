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

        [SetUp]
        public void Column()
        {
            MainMenuOverlay.ForceWide = false;
            savedFlags = FeatureFlags.Active;
            HomeFlagsBoot.Loaded = true;
        }

        [TearDown]
        public void Reset()
        {
            MainMenuOverlay.ForceWide = null;
            FeatureFlags.Active = savedFlags;
        }

        [Test]
        public void HomeReappliesItsExtrasWhenAFlagChanges()
        {
            UseFlags(HomeShow.None);
            var s = new HomePhoneScreen { W = 390f, H = 844f, Top = 47f, Bottom = 34f, Left = 8f, Right = 8f, PerPoint = 1f };
            using (var surface = DeviceSurface.Open(s.W, s.H))
            {
                var shell = Shell(surface.Stage, s, out _, out var host);
                var overlay = Home(shell, host, false);
                surface.Pump(6);
                Assert.That(overlay.Q("mm-hint").resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "Thrum off");
                FeatureFlags.Active.SetOverride(FeatureKeys.HomeThrum, true);
                FeatureFlags.Active.SetOverride(FeatureKeys.HomeCodexTile, true);
                surface.Pump(6);
                Assert.That(overlay.Q("mm-hint").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "Thrum back on Changed");
                Assert.That(overlay.Q("home-codex").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "Codex back on Changed");
                Assert.That(shell.Q("home-setting-codex").resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "Settings Codex row goes with the tile");
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
                    foreach (var (w, h) in new[] { (1280f, 720f), (1366f, 768f), (1920f, 1080f) })
                    {
                        UseFlags(mode.Show);
                        MainMenuOverlay.ForceWide = true;
                        string n = w + "x" + h + " " + mode.Name + (save ? " save" : " no save");
                        var s = new HomePhoneScreen { W = w, H = h, PerPoint = 1f };
                        using (var surface = DeviceSurface.Open(w, h))
                        {
                            var shell = Shell(surface.Stage, s, out var safe, out var host);
                            var overlay = Home(shell, host, save);
                            surface.Pump(8);
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
            var key = overlay.Q<Button>("home-launch");
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
            foreach (var id in new[] { "mm-title", "mm-stats", "home-launch", "home-design", "home-missions", "home-codex", "mm-hint" })
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
            using (var surface = DeviceSurface.Open(s.W, s.H))
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

                // 5. Tiles: glyph, name and lamp side by side; the caption off the bottom edge. Starter: MISSIONS spans the row.
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
                    if (parts.Count > 0) Assert.That(Math.Abs(parts[0].worldBound.xMin - head.worldBound.xMin), Is.LessThanOrEqualTo(2f * k), n + " " + it.Id + " glyph at the start");
                    Assert.That(name.worldBound.xMin - parts[0].worldBound.xMax, Is.LessThanOrEqualTo(12f * k), n + " " + it.Id + " name next to its glyph");
                    AssertWords(name, n + " " + it.Id + " name");
                    AssertWords(cap, n + " " + it.Id + " caption");
                    Inside(cap.worldBound, tile.worldBound, n + " " + it.Id + " caption");
                    Assert.That(cap.worldBound.yMax, Is.LessThanOrEqualTo(tile.worldBound.yMax - 1f * k), n + " " + it.Id + " caption off the bottom edge");
                }
                if (!show.Codex)
                {
                    var missions = overlay.Q("home-missions");
                    var launch = overlay.Q("home-launch");
                    Assert.That(missions.worldBound.width, Is.GreaterThanOrEqualTo(launch.worldBound.width - 2f * k), n + " MISSIONS spans the row");
                }

                // 6. Thrum: the name over his words (left-aligned), and the shown words fit the strip.
                if (show.Thrum)
                {
                    var strip = overlay.Q("mm-hint");
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
                var first = show.Stats ? stats : overlay.Q("home-launch");
                Assert.That(shell.worldBound.yMin + clear + pop, Is.LessThanOrEqualTo(first.worldBound.yMin + 0.5f), n + " pop above the first control");
            }
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

        sealed class DeviceSurface : IDisposable
        {
            EditorWindow window;
            GameObject go;
            PanelSettings settings;
            RenderTexture texture;
            public VisualElement Stage { get; private set; }

            public static DeviceSurface Open(float w, float h)
            {
                var surface = new DeviceSurface();
                surface.TryWindow(w, h);
                surface.Pump(4);
                if (!surface.Sized(w, h))
                {
                    surface.CloseWindow();
                    surface.TryDocument(w, h);
                    surface.Pump(6);
                }
                if (!surface.Sized(w, h))
                    Assert.Fail("panel did not take the device size " + w + "x" + h);
                return surface;
            }

            public void Pump(int passes)
            {
                for (int i = 0; i < passes; i++)
                {
                    if (Stage != null && Stage.panel != null)
                    {
                        var tree = Stage.panel.visualTree;
                        if (tree != null && Stage.resolvedStyle.width > 2f)
                        {
                            tree.style.width = Stage.resolvedStyle.width;
                            tree.style.height = Stage.resolvedStyle.height;
                        }
                    }
                    if (settings != null)
                    {
                        var apply = typeof(PanelSettings).GetMethod("ApplyPanelSettings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (apply != null) apply.Invoke(settings, null);
                    }
                    var utility = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility");
                    var update = utility != null ? utility.GetMethod("UpdatePanels", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null;
                    if (update != null && update.GetParameters().Length == 0) update.Invoke(null, null);
                    if (window != null) window.Repaint();
                    var panel = Stage != null ? Stage.panel : null;
                    if (panel != null)
                    {
                        var validate = panel.GetType().GetMethod("ValidateLayout", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (validate != null && validate.GetParameters().Length == 0) validate.Invoke(panel, null);
                    }
                }
            }

            bool Sized(float w, float h)
            {
                if (Stage == null || Stage.panel == null) return false;
                var bound = Stage.worldBound;
                if (bound.width < 8f || bound.height < 8f) return false;
                float sx = bound.width / w, sy = bound.height / h;
                return sx >= 0.8f && sy >= 0.8f && sx <= 3.5f && sy <= 3.5f && Math.Abs(sx - sy) <= 0.15f;
            }

            void TryWindow(float w, float h)
            {
                try
                {
                    window = ScriptableObject.CreateInstance<IosHomeDeviceWindow>();
                    window.position = new Rect(0f, 0f, w, h);
                    window.minSize = new Vector2(w, h);
                    window.maxSize = new Vector2(w, h);
                    window.ShowUtility();
                    var root = window.rootVisualElement;
                    if (root == null) return;
                    Stage = MakeStage(root, w, h);
                }
                catch (Exception) { }
            }

            void TryDocument(float w, float h)
            {
                try
                {
                    texture = new RenderTexture(Mathf.Max(8, (int)w), Mathf.Max(8, (int)h), 0, RenderTextureFormat.ARGB32);
                    texture.Create();
                    settings = ScriptableObject.CreateInstance<PanelSettings>();
                    settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                    settings.scale = 1f;
                    settings.targetTexture = texture;
                    go = new GameObject("kh-ios-home-device");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    var doc = go.AddComponent<UIDocument>();
                    doc.panelSettings = settings;
                    var root = doc.rootVisualElement;
                    if (root == null) return;
                    Stage = MakeStage(root, w, h);
                }
                catch (Exception) { }
            }

            static VisualElement MakeStage(VisualElement panelRoot, float w, float h)
            {
                var tree = panelRoot.panel != null ? panelRoot.panel.visualTree : null;
                if (tree != null)
                {
                    tree.style.width = w;
                    tree.style.height = h;
                }
                panelRoot.style.width = w;
                panelRoot.style.height = h;
                var stage = new VisualElement { name = "stage", pickingMode = PickingMode.Ignore };
                stage.style.position = Position.Absolute;
                stage.style.left = 0f;
                stage.style.top = 0f;
                stage.style.width = w;
                stage.style.height = h;
                stage.style.overflow = Overflow.Hidden;
                panelRoot.Add(stage);
                return stage;
            }

            void CloseWindow()
            {
                if (window == null) return;
                window.Close();
                window = null;
                Stage = null;
            }

            public void Dispose()
            {
                CloseWindow();
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
                if (settings != null) UnityEngine.Object.DestroyImmediate(settings);
                if (texture != null)
                {
                    texture.Release();
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        sealed class IosHomeDeviceWindow : EditorWindow { }
    }
}
