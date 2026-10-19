// iOS portrait Home (TestFlight 156/158 on Trevor's iPhone), with real UI Toolkit rects. The shell panel is a
// 375x667 reference matched to height, so a 390x844 or 430x932 iPhone lays Home out in a ~308x667 panel, and
// an 820x1180 iPad portrait in ~464x667. Each size also runs at its literal point size (portrait web).
// A save is present (CONTINUE shows), credits show, and the record is a long career. Checked: no top-level
// block on another or outside the safe column, CONTINUE has the Dusk look in its own slot, one primary CTA,
// the stats card holds its two lines, DESIGN FIRST and the tile captions are not clipped, tile names clear
// their glyph and lamp, Thrum's name sits over his words, and the guide pop's rest clears the gear and title.
using System;
using System.Collections.Generic;
using System.Reflection;
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
        }

        [SetUp]
        public void Column() { MainMenuOverlay.ForceWide = false; }

        [TearDown]
        public void Reset()
        {
            MainMenuOverlay.ForceWide = null;
        }

        [Test]
        public void PortraitHomeHasNoOverlapsAndEveryWordFits()
        {
            foreach (var size in Sizes())
            {
                var s = size.Screen;
                string n = size.Name;
                using (var surface = DeviceSurface.Open(s.W, s.H))
                {
                    var shell = Shell(surface.Stage, s, out var safe, out var host);
                    var overlay = MainMenuOverlay.Build(shell, host, () => { }, () => { }, () => { }, true);
                    MainMenuOverlay.SetMissionLine(overlay, "KH-3 · Tiny Orbit");
                    MainMenuOverlay.SetRecord(overlay, LongRecord);
                    HomeResumeBar.Mount(overlay, 72, true, () => { });
                    PlaceGear(shell, s);
                    surface.Pump(8);
                    TextFit.Apply(shell);
                    surface.Pump(4);
                    PlaceGear(shell, s);
                    surface.Pump(3);

                    var stage = surface.Stage.worldBound;
                    float k = stage.width / s.W;   // world units per panel unit
                    var safeRect = safe.worldBound;

                    // 1. Top-level blocks: none on another, all inside the safe column.
                    var bar = overlay.Q(HomeResumeBar.BarName);
                    var gear = shell.Q("home-settings");
                    Assert.That(bar, Is.Not.Null, n);
                    Assert.That(gear, Is.Not.Null, n);
                    var ids = new[] { "mm-title", "mm-stats", "home-launch", "home-design", "home-missions", "home-codex", "mm-hint" };
                    var blocks = new List<(string, VisualElement)> { ("credits band", bar), ("gear", gear) };
                    foreach (var id in ids)
                    {
                        var e = overlay.Q(id);
                        Assert.That(e, Is.Not.Null, n + " " + id);
                        Assert.That(e.worldBound.width, Is.GreaterThan(1f), n + " " + id + " laid out");
                        blocks.Add((id, e));
                    }
                    for (int a = 0; a < blocks.Count; a++)
                        for (int b = a + 1; b < blocks.Count; b++)
                            Assert.That(Overlap(blocks[a].Item2.worldBound, blocks[b].Item2.worldBound), Is.False,
                                n + ": " + blocks[a].Item1 + " " + blocks[a].Item2.worldBound + " on " + blocks[b].Item1 + " " + blocks[b].Item2.worldBound);
                    foreach (var (id, e) in blocks)
                        if (id != "gear") Inside(e.worldBound, safeRect, n + " " + id);
                    Assert.That(gear.worldBound.yMin, Is.GreaterThanOrEqualTo(safeRect.yMin - 0.5f), n + ": gear under the status bar");
                    foreach (var id in new[] { "mm-eyebrow", "mm-kestrel", "mm-heavy" })
                    {
                        var e = overlay.Q(id);
                        Assert.That(Overlap(bar.worldBound, e.worldBound), Is.False, n + " credits on " + id);
                        Assert.That(Overlap(gear.worldBound, e.worldBound), Is.False, n + " gear on " + id);
                        AssertWords(e as TextElement, n + " " + id);
                    }

                    // 2. CONTINUE: Dusk glass, its own slot beside the chip, words fit.
                    var chip = bar.Q<Label>(HomeResumeBar.ChipName);
                    var cont = bar.Q<Button>(HomeResumeBar.ContinueName);
                    Assert.That(cont, Is.Not.Null, n + " CONTINUE with a save");
                    Assert.That(cont.ClassListContains(HomeResumeBar.ContinueClass), n + " CONTINUE Dusk class");
                    var bg = cont.resolvedStyle.backgroundColor;
                    Assert.That(bg.r < 0.2f && bg.g < 0.2f && bg.b < 0.25f, n + " CONTINUE is dark glass, not the grey system button: " + bg);
                    Assert.That(cont.resolvedStyle.borderTopWidth, Is.GreaterThanOrEqualTo(1f), n);
                    Assert.That(cont.worldBound.height, Is.GreaterThanOrEqualTo(44f * k - 0.5f), n + " 44 pt target");
                    Assert.That(Overlap(chip.worldBound, cont.worldBound), Is.False, n + " chip on CONTINUE");
                    Inside(cont.worldBound, bar.worldBound, n + " CONTINUE in the band");
                    Assert.That(cont.resolvedStyle.fontSize, Is.GreaterThanOrEqualTo(14f), n + " CONTINUE size");
                    Assert.That(chip.resolvedStyle.fontSize, Is.GreaterThanOrEqualTo(14f), n + " credits size");
                    AssertWords(cont, n + " CONTINUE");
                    AssertWords(chip, n + " credits");
                    int primary = 0;
                    overlay.Query(className: "kh-mm-launch").ForEach(_ => primary++);
                    Assert.That(primary, Is.EqualTo(1), n + " one primary CTA");

                    // 3. Stats: two lines inside the card, the record on one line.
                    var stats = overlay.Q("mm-stats");
                    foreach (var id in new[] { "mm-fuel", "mm-thrust", "mm-record" })
                    {
                        var e = overlay.Q<Label>(id);
                        Inside(e.worldBound, stats.worldBound, n + " " + id + " inside the stats card");
                    }
                    var record = overlay.Q<Label>("mm-record");
                    Assert.That(record.text, Does.Contain("Kc").And.Contain("888.88M"), n + " compact Kc");
                    var one = record.MeasureTextSize(record.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);
                    Assert.That(one.x, Is.LessThanOrEqualTo(record.contentRect.width + TextFit.Slack), n + " record on one line: " + record.text);
                    Assert.That(overlay.Q("mm-fuel").worldBound.yMax, Is.LessThanOrEqualTo(record.worldBound.yMin + 1f), n + " pills over the record");

                    // 4. LAUNCH and DESIGN FIRST: label and caption inside their buttons.
                    foreach (var (card, cls) in new[] { ("home-launch", "kh-mm-launch-caption"), ("home-launch", "kh-mm-launch-label"), ("home-design", "kh-mm-design-label"), ("home-design", "kh-mm-design-caption") })
                    {
                        var box = overlay.Q(card);
                        var e = box.Q<Label>(className: cls);
                        Inside(e.worldBound, box.worldBound, n + " " + cls);
                        AssertWords(e, n + " " + cls);
                    }

                    // 5. Tiles: glyph, name and lamp side by side; the caption off the bottom edge.
                    foreach (var it in MainMenuCopy.Tiles)
                    {
                        var tile = overlay.Q("home-" + it.Id);
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

                    // 6. Thrum: the name over his words (left-aligned), and the shown words fit the strip.
                    var strip = overlay.Q("mm-hint");
                    var who = strip.Q<Label>("mm-thrum-name");
                    var line = strip.Q<Label>("mm-hint-line");
                    Inside(line.worldBound, strip.worldBound, n + " Thrum line");
                    AssertWords(line, n + " Thrum line");
                    if (who != null && who.resolvedStyle.display != DisplayStyle.None)
                    {
                        var nameText = who.MeasureTextSize(who.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);
                        float drawnLeft = who.contentRect.xMin + who.worldBound.xMin;
                        Assert.That(who.resolvedStyle.unityTextAlign, Is.EqualTo(TextAnchor.UpperLeft), n + " Thrum name left-aligned, not floating mid-strip");
                        Assert.That(Math.Abs(drawnLeft - line.worldBound.xMin), Is.LessThanOrEqualTo(2f * k), n + " Thrum name over his words");
                        Assert.That(nameText.x, Is.LessThanOrEqualTo(who.contentRect.width + TextFit.Slack), n);
                    }

                    // 7. Guide pop: its rest clears the gear and the wordmark, and stays above the stats card.
                    float clear = MainMenuOverlay.PopClearTop(overlay, shell);
                    Assert.That(clear + shell.worldBound.yMin, Is.GreaterThanOrEqualTo(gear.worldBound.yMax - 0.5f), n + " pop under the gear");
                    Assert.That(clear + shell.worldBound.yMin, Is.GreaterThanOrEqualTo(overlay.Q("mm-title").worldBound.yMax - 0.5f), n + " pop under the title");
                    float pop = (HomePhoneLayout.GuidePopPt + HomePhoneLayout.GuideMarginPt) * s.PerPoint * k;
                    Assert.That(shell.worldBound.yMin + clear + pop, Is.LessThanOrEqualTo(stats.worldBound.yMin + 0.5f), n + " pop above the stats card");
                }
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
