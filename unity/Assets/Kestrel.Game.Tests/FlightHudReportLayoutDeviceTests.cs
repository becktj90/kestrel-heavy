// Flight HUD, engine-out prompt, pause menu and post-flight report at real UI Toolkit rects (NAV_BUGS_LIVE L1-L4),
// plus the Hangar tab row (L8). Sizes: iPhone SE 375x667, 335x720, 390x844, 430x932, iPad 820x1180 and 1024x1366
// portrait, and the web panels for 1280x720, 1280x800, 1366x768, 1920x1080 (WebPanelScale, laid out from the CSS
// viewport as AscentHud reads it). Long values (1234.5 km, 11.20 km/s, a long stage banner) and the engine-out call.
// Checks: every shown label and button keeps its text inside its own box (TextFit.Problems) and inside its parent,
// siblings do not overlap, nothing leaves the screen or the safe area, every control is a 44 pt target that receives
// the tap at its centre, the pause card is opaque and on top, and the report shows every exit with no scrolling,
// with all six of #311's exits (Back to designer, Missions and Home in #311's row wrapper). Each size is captured as a real PNG (UiCapture, #304's helper, CaptureOut/ui -> CI artifact "ui-captures").
// Batchmode: no LogError and no throw for a missing GPU, theme or font; the capture test ignores itself with a
// warning when no offscreen panel can be sized. TearDown destroys every GameObject, panel and texture.
using System;
using System.Collections.Generic;
using System.Reflection;
using Kestrel.Core;
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
using Kestrel.Game.Shell;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class FlightHudReportLayoutDeviceTests
    {
        struct Size
        {
            public string Name;
            public float CssW, CssH, PanelW, PanelH;
            public bool Wide;
            public float SafeTop, SafeBottom;
        }

        static IEnumerable<Size> Sizes()
        {
            yield return new Size { Name = "375x667 SE", CssW = 375f, CssH = 667f, PanelW = 375f, PanelH = 667f, SafeTop = 20f };
            yield return new Size { Name = "335x720", CssW = 335f, CssH = 720f, PanelW = 335f, PanelH = 720f, SafeTop = 20f };
            yield return new Size { Name = "390x844", CssW = 390f, CssH = 844f, PanelW = 390f, PanelH = 844f, SafeTop = 47f, SafeBottom = 34f };
            yield return new Size { Name = "430x932", CssW = 430f, CssH = 932f, PanelW = 430f, PanelH = 932f, SafeTop = 59f, SafeBottom = 34f };
            yield return new Size { Name = "820x1180 iPad", CssW = 820f, CssH = 1180f, PanelW = 820f, PanelH = 1180f, SafeTop = 24f, SafeBottom = 20f };
            yield return new Size { Name = "1024x1366 iPad", CssW = 1024f, CssH = 1366f, PanelW = 1024f, PanelH = 1366f, SafeTop = 24f, SafeBottom = 20f };
            foreach (var (w, h) in new[] { (1280, 720), (1280, 800), (1366, 768), (1920, 1080) })
            {
                WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
                float s = WebLandscapeType.PanelScale(w, h, rw, rh, m);
                yield return new Size { Name = w + "x" + h, CssW = w, CssH = h, PanelW = Mathf.Round(w / s), PanelH = Mathf.Round(h / s), Wide = true };
            }
        }

        const string LongAlt = "1234.5 km";
        const string LongSpeed = "11.20 km/s";
        const string LongVerdict = "Climbing through max-Q on the pitch program, booster burning hot";

        static AscentPrompt EngineOut() => new AscentPrompt
        {
            Call = AscentCall.EngineOut,
            Eyebrow = "ANOMALY · ENGINE 1 (seeded, fair)",
            Quote = "Engine 1 pressure drop. Shut it down?",
            Detail = "0 of 1 still burning · TWR after 0.00 · this design cannot hold an engine-out",
            Primary = "SHUT DOWN ENGINE 1 (will not hold)",
            Alternate = "RIDE IT OUT (risk overpressure)",
            ShowAlternate = true,
            DangerAlternate = true,
        };

        readonly List<GameObject> made = new List<GameObject>();
        readonly List<AscentHud> huds = new List<AscentHud>();
        readonly List<string> temp = new List<string>();
        Action savedEscape;

        [SetUp]
        public void Remember() => savedEscape = ReportCardChrome.Escape;

        [TearDown]
        public void CleanUp()
        {
            foreach (var h in huds)
            {
                try { h.Close(); }
                catch (Exception e) { Debug.LogWarning("[hud-layout test] close: " + e.Message); }
            }
            huds.Clear();
            AscentHud.ForceWide = null;
            AscentHud.ForceCssW = AscentHud.ForceCssH = 0f;
            ReportCardChrome.Escape = savedEscape;
            foreach (var go in made) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            made.Clear();
            foreach (var dir in temp)
            {
                try { if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true); }
                catch (Exception e) { Debug.LogWarning("[hud-layout test] temp: " + e.Message); }
            }
            temp.Clear();
        }

        // ---- L1: flight HUD, cruise, long values --------------------------------------------------

        [Test]
        public void FlightHudKeepsLongReadingsInsideTheirCellsAndNothingOverlaps()
        {
            foreach (var size in Sizes())
            using (var surface = DeviceSurface.Open(size.PanelW, size.PanelH))
            {
                var (hud, chrome, ascent) = Flight(surface, size, null);
                string at = size.Name + " cruise";
                Assert.That(hud.Q<Label>("h-verdict").text, Is.EqualTo(LongVerdict), at);
                CheckHud(hud, surface, size, at);
                // Strip keys sit below the top of the screen, not clipped at y=0 (L1 "ALT/SPEED clipped at top").
                var strip = hud.Q("ascent-telemetry");
                Assert.That(strip, Is.Not.Null, at);
                foreach (var l in strip.Query<Label>().ToList())
                {
                    if (!Shown(l)) continue;
                    Assert.That(l.worldBound.yMin, Is.GreaterThanOrEqualTo(strip.worldBound.yMin - 0.5f), at + " " + l.text + " top");
                    Assert.That(l.worldBound.yMax, Is.LessThanOrEqualTo(strip.worldBound.yMax + 0.5f), at + " " + l.text + " bottom");
                }
                // The three health tabs (Eng/Tnk/Hull) are equal columns that do not overlap.
                var tabs = new List<VisualElement>(hud.Q("ascent-health").Query<Button>().ToList().FindAll(Shown));
                Assert.That(tabs.Count, Is.GreaterThanOrEqualTo(3), at + " health tabs");
                NoOverlap(tabs, at + " health tabs");
                // Each tab is a 44 pt target: the row is the 44 pt band, and a tap at the top or bottom edge of the
                // band, in a tab's column, lands on that tab or the band (which routes it to the tab).
                var row = hud.Q(AscentHud.HealthRowName);
                Assert.That(row, Is.Not.Null, at);
                float unit = size.PanelW / size.CssW;
                Assert.That(row.worldBound.height, Is.GreaterThanOrEqualTo(44f * unit - 1f), at + " health hit band");
                var columns = new List<Rect>();
                foreach (var t in tabs) columns.Add(t.layout);
                for (int i = 0; i < tabs.Count; i++)
                {
                    foreach (float y in new[] { row.worldBound.yMin + 1.5f, row.worldBound.center.y, row.worldBound.yMax - 1.5f })
                    {
                        var hit = hud.panel.Pick(new Vector2(tabs[i].worldBound.center.x, y));
                        bool ok = false;
                        for (var v = hit; v != null; v = v.parent) if (v == tabs[i] || v == row) ok = true;
                        Assert.That(ok, Is.True, at + " tab " + i + " band y " + y + " (got " + (hit != null ? Name(hit) : "null") + ")");
                    }
                    Assert.That(AscentHud.HealthTabAt(tabs[i].layout.center.x, columns), Is.EqualTo(i), at + " band routes to tab " + i);
                }
            }
        }

        // ---- L2: engine-out ---------------------------------------------------------------------

        [Test]
        public void EngineOutQuoteIsOneLineAndItsButtonsDoNotOverlapTheDock()
        {
            foreach (var size in Sizes())
            using (var surface = DeviceSurface.Open(size.PanelW, size.PanelH))
            {
                var (hud, chrome, ascent) = Flight(surface, size, EngineOut());
                string at = size.Name + " engine-out";
                var primary = hud.Q<Button>(UiIds.FlightPrimary);
                var alternate = hud.Q<Button>(UiIds.FlightAlternate);
                Assert.That(primary, Is.Not.Null, at);
                Assert.That(alternate, Is.Not.Null, at);
                Assert.That(Shown(primary) && Shown(alternate), Is.True, at + " both calls shown");
                Assert.That(primary.text, Does.StartWith("SHUT DOWN"), at);
                Assert.That(alternate.text, Does.StartWith("RIDE IT OUT"), at);
                Assert.That(Overlaps(primary.worldBound, alternate.worldBound), Is.False, at + " SHUT DOWN vs RIDE IT OUT");
                var dock = hud.Q("ascent-actions");
                var dockItems = new List<VisualElement>();
                foreach (var e in dock.Query<VisualElement>().ToList())
                    if (e is TextElement && Shown(e)) dockItems.Add(e);
                NoOverlap(dockItems, at + " dock");
                var quote = FindByText(dock, "pressure drop");
                Assert.That(quote, Is.Not.Null, at + " quote shown");
                var medal = FindByText(dock, "Medal timing");
                if (medal != null && Shown(medal))
                    Assert.That(Overlaps(quote.worldBound, medal.worldBound), Is.False, at + " quote over medal line");
                float line = quote.resolvedStyle.fontSize * 1.6f;
                Assert.That(quote.worldBound.height, Is.LessThanOrEqualTo(line + 2f), at + " quote is one line: " + quote.worldBound);
                CheckHud(hud, surface, size, at);
            }
        }

        // ---- L3: pause --------------------------------------------------------------------------

        [Test]
        public void PauseCardIsOpaqueOnTopAndItsButtonsDoNotOverlap()
        {
            foreach (var size in Sizes())
            using (var surface = DeviceSurface.Open(size.PanelW, size.PanelH))
            {
                var (hud, chrome, ascent) = Flight(surface, size, EngineOut());
                string at = size.Name + " pause";
                chrome.Show(true);
                surface.Pump(4);
                chrome.Layout(1f);
                ascent.Layout(1f);
                surface.Pump(6);
                var menu = hud.Q(UiIds.ScreenPause);
                Assert.That(menu, Is.Not.Null, at);
                Assert.That(menu.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), at + " shown");
                Assert.That(menu.parent.IndexOf(menu), Is.EqualTo(menu.parent.childCount - 1), at + " paints above the HUD and the dock prompt");
                Assert.That(menu.resolvedStyle.backgroundColor.a, Is.GreaterThanOrEqualTo(0.95f), at + " backdrop is opaque");
                var card = menu.Q(FlightChrome.PauseCardName);
                Assert.That(card, Is.Not.Null, at);
                Assert.That(card.resolvedStyle.backgroundColor.a, Is.EqualTo(1f).Within(0.001f), at + " card is opaque");
                var ids = new[] { UiIds.PauseResume, UiIds.PauseRestart, UiIds.PauseExitHome, UiIds.PauseMusic, UiIds.PauseSfx, UiIds.PauseNarration };
                var buttons = new List<VisualElement>();
                float unit = size.PanelW / size.CssW;
                foreach (var id in ids)
                {
                    var b = menu.Q<Button>(id);
                    Assert.That(b, Is.Not.Null, at + " " + id);
                    Assert.That(Shown(b), Is.True, at + " " + id);
                    Assert.That(b.worldBound.height, Is.GreaterThanOrEqualTo(44f * unit - 1f), at + " " + id + " target");
                    Inside(b.worldBound, card.worldBound, at + " " + id + " in the card");
                    OnScreen(b.worldBound, surface.Stage.worldBound, at + " " + id);
                    var hit = hud.panel.Pick(b.worldBound.center);
                    bool onIt = false;
                    for (var v = hit; v != null; v = v.parent) if (v == b) onIt = true;
                    Assert.That(onIt, Is.True, at + " " + id + " receives the tap (got " + (hit != null ? hit.name : "null") + ")");
                    buttons.Add(b);
                }
                NoOverlap(buttons, at);
                Assert.That(TextFit.Problems(menu), Is.Empty, at + " text fits");
                // Nothing from the dock (Auto-assist, the prompt) is hit-testable through the menu.
                var assist = hud.Q<Button>(UiIds.FlightAssist);
                if (assist != null && Shown(assist))
                {
                    var hit = hud.panel.Pick(assist.worldBound.center);
                    for (var v = hit; v != null; v = v.parent)
                        Assert.That(v, Is.Not.SameAs(assist), at + " Auto-assist shows through");
                }
                chrome.Show(false);
                surface.Pump(2);
                Assert.That(menu.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), at + " resume hides it");
            }
        }

        // ---- L4: report -------------------------------------------------------------------------

        /// <summary>
        /// #311's six exits (Back to designer added; Missions and Home nested in one NextMission.ExitRowId row). The footer
        /// grid lifts them back into its cells and all six fit at SE 375x667 (#311 alone had report-home at y 645 + 48
        /// on a 667 screen).
        /// </summary>
        [Test]
        public void ReportFitsWithoutScrolling_AllSixExitsIncludingBackToDesigner()
        {
            Assert.That(NextMission.DesignerId, Is.EqualTo(UiIds.ReportDesigner), "one id for Back to designer");
            CheckReport();
        }

        void CheckReport()
        {
            foreach (var size in Sizes())
            using (var surface = DeviceSurface.Open(size.PanelW, size.PanelH))
            {
                int fix = 0, retry = 0, next = 0, missions = 0, home = 0, designer = 0;
                var card = Report(surface, size, new ReportActions
                {
                    Again = () => retry++,
                    Next = () => next++,
                    Home = () => home++,
                    Missions = () => missions++,
                    Fix = () => fix++,
                    Designer = () => designer++,
                });
                string at = size.Name + " report";
                Assert.That(card.name, Is.EqualTo("screen-report"), at);
                Assert.That(card.ClassListContains("post-flight"), Is.True, at + " keeps the USS class");
                var stage = surface.Stage.worldBound;
                float unit = stage.width / size.PanelW;
                var safe = Rect.MinMaxRect(stage.xMin, stage.yMin + size.SafeTop * unit, stage.xMax, stage.yMax - size.SafeBottom * unit);
                var ids = new List<string>(UiIds.ReportExits);
                var foot = card.Q(UiIds.ReportFoot);
                Assert.That(foot, Is.Not.Null, at);
                foreach (var id in ids)
                {
                    var b = card.Q<Button>(id);
                    Assert.That(b, Is.Not.Null, at + " " + id);
                    Assert.That(Shown(b), Is.True, at + " " + id + " shown");
                    Assert.That(b.parent, Is.SameAs(foot), at + " " + id + " is a grid cell");
                    Assert.That(b.worldBound.height, Is.GreaterThanOrEqualTo(44f * unit - 1f), at + " " + id + " target height");
                    Assert.That(b.worldBound.width, Is.GreaterThanOrEqualTo(44f * unit - 1f), at + " " + id + " target width");
                    Inside(b.worldBound, safe, at + " " + id + " inside the safe area");
                    Inside(b.worldBound, foot.worldBound, at + " " + id + " inside the footer");
                    var hit = card.panel.Pick(b.worldBound.center);
                    bool onIt = false;
                    for (var v = hit; v != null; v = v.parent) if (v == b) onIt = true;
                    Assert.That(onIt, Is.True, at + " " + id + " receives the tap");
                }
                // The primary exit (first in the footer) is full width; the rest share the grid.
                var first = card.Q<Button>(UiIds.ReportFix);
                Assert.That(foot.IndexOf(first), Is.EqualTo(0), at + " Fix it leads");
                float contentW = foot.worldBound.width - foot.resolvedStyle.paddingLeft - foot.resolvedStyle.paddingRight;
                Assert.That(first.worldBound.width, Is.EqualTo(contentW).Within(2f), at + " primary is full width");
                var exits = new List<VisualElement>();
                foreach (var b in foot.Query<Button>().ToList()) if (Shown(b)) exits.Add(b);
                Assert.That(exits.Count, Is.EqualTo(ids.Count), at + " no stray exits");
                NoOverlap(exits, at + " exits");
                Assert.That(foot.worldBound.yMax, Is.LessThanOrEqualTo(stage.yMax + 0.5f), at + " footer on screen");

                // No scrolling: the body's content is no taller than its viewport, and no scroller shows.
                var body = card.Q<ScrollView>(UiIds.ReportBody);
                Assert.That(body, Is.Not.Null, at);
                float content = 0f;
                foreach (var child in body.contentContainer.Children())
                    if (Shown(child)) content = Mathf.Max(content, child.worldBound.yMax);
                Assert.That(content, Is.LessThanOrEqualTo(body.contentViewport.worldBound.yMax + 1f), at + " body fits its viewport");
                Assert.That(body.contentViewport.worldBound.yMax, Is.LessThanOrEqualTo(foot.worldBound.yMin + 1f), at + " body ends above the exits");
                Assert.That(body.verticalScrollerVisibility, Is.EqualTo(ScrollerVisibility.Hidden), at + " no scroll bar");

                // Text: inside its own box, a margin off the side edges, below the safe top, siblings apart.
                Assert.That(TextFit.Problems(card), Is.Empty, at + " text fits");
                var lines = new List<VisualElement>();
                foreach (var l in body.Query<Label>().ToList())
                {
                    if (!Shown(l) || l.name == "plot-label") continue;
                    Assert.That(l.worldBound.xMin - stage.xMin, Is.GreaterThanOrEqualTo(ReportFit.SidePt * unit - 1f), at + " " + l.name + " left margin");
                    Assert.That(stage.xMax - l.worldBound.xMax, Is.GreaterThanOrEqualTo(ReportFit.SidePt * unit - 1f), at + " " + l.name + " right margin");
                    Inside(l.worldBound, safe, at + " " + l.name);
                    Assert.That(l.resolvedStyle.fontSize / unit, Is.GreaterThanOrEqualTo(14f - 0.1f), at + " " + l.name + " size");
                    lines.Add(l);
                }
                NoOverlap(lines, at + " lines");
                Assert.That(lines.Count, Is.GreaterThanOrEqualTo(4), at + " cause, numbers, kid and stars are on the card");
                Assert.That(card.Query<Label>().ToList().FindAll(l => Shown(l) && string.IsNullOrEmpty(l.text)), Is.Empty, at + " no empty gap lines");

                foreach (var id in ids) Click(card.Q<Button>(id));
                Assert.That(new[] { fix, retry, next, missions, home, designer }, Is.EqualTo(new[] { 1, 1, 1, 1, 1, 1 }), at + " each exit routes once");
                ReportCardChrome.Dismiss(surface.Stage);
            }
        }

        // ---- L8: Hangar tab row ------------------------------------------------------------------

        [Test]
        public void HangarTabsFitTheirColumnsIncludingDesigns()
        {
            string root = Kestrel.Core.Data.Catalog.FindRepoRoot(System.IO.Directory.GetCurrentDirectory());
            var catalog = Kestrel.Core.Data.Catalog.LoadFromRepo(root);
            var data = Kestrel.Core.Hangar.HangarData.LoadFromRepo(root);
            // The Hangar sheet is a side column on the web panels and full width on phones.
            foreach (float w in new[] { 300f, 335f, 375f, 390f, 430f, 820f })
            using (var surface = DeviceSurface.Open(w, 700f))
            {
                string at = "hangar tabs " + w;
                surface.Stage.Clear();
                var body = new VisualElement { name = "test-hangar-body" };
                body.style.position = Position.Absolute;
                body.style.left = 0; body.style.top = 0;
                body.style.width = w; body.style.height = 700f;
                surface.Stage.Add(body);
                var session = new Kestrel.Core.Hangar.HangarSession(catalog, data, Kestrel.Core.Hangar.HangarEditor.ForkTemplate(data.Template("workhorse")));
                string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "kh-hud-tabs-" + Guid.NewGuid().ToString("N"));
                System.IO.Directory.CreateDirectory(dir);
                temp.Add(dir);
                long clock = 0;
                var library = new Kestrel.Core.Hangar.DesignLibrary(new Kestrel.Core.Save.NativeStore(dir, () => ++clock, () => 0.42));
                new HangarPanel(session, library, body, 1f);
                surface.Pump(8);
                surface.Pump(4);
                var tabs = new List<VisualElement>();
                foreach (var b in body.Query<Button>().ToList())
                    if (Shown(b) && b.tooltip != null && b.tooltip.EndsWith(" tab", StringComparison.Ordinal)) tabs.Add(b);
                Assert.That(tabs.Count, Is.EqualTo(5), at + " five tabs");
                var designs = tabs.Find(t => ((Button)t).tooltip == "Designs tab");
                Assert.That(designs, Is.Not.Null, at + " Designs tab");
                NoOverlap(tabs, at);
                foreach (var t in tabs)
                {
                    Assert.That(t.worldBound.height, Is.GreaterThanOrEqualTo(44f - 1f), at + " " + Name(t) + " height");
                    Assert.That(t.worldBound.width, Is.GreaterThanOrEqualTo(44f - 1f), at + " " + Name(t) + " width");
                    Inside(t.worldBound, surface.Stage.worldBound, at + " " + Name(t));
                    var hit = body.panel.Pick(t.worldBound.center);
                    bool onIt = false;
                    for (var v = hit; v != null; v = v.parent) if (v == t) onIt = true;
                    Assert.That(onIt, Is.True, at + " " + Name(t) + " receives the tap");
                    var bt = (Button)t;
                    var size = bt.MeasureTextSize(bt.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);
                    float room = t.resolvedStyle.width - t.resolvedStyle.paddingLeft - t.resolvedStyle.paddingRight;
                    Assert.That(size.x, Is.LessThanOrEqualTo(room + 1f), at + " \"" + bt.text + "\" fits (" + size.x + " in " + room + ")");
                    Assert.That(bt.resolvedStyle.fontSize, Is.GreaterThanOrEqualTo(10f - 0.01f), at + " " + bt.text + " face floor");
                }
                Assert.That(TextFit.Problems(tabs[0].parent), Is.Empty, at + " tab row text fits");
            }
        }

        // ---- PNGs -------------------------------------------------------------------------------

        [Test]
        public void CapturesFlightEngineOutPauseAndReportAtEverySize()
        {
            foreach (var size in Sizes())
            {
                DeviceSurface surface = null;
                try
                {
                    surface = DeviceSurface.TryOffscreen(size.PanelW, size.PanelH, 2f);
                    if (surface == null)
                    {
                        Debug.LogWarning("[hud-layout test] no offscreen panel at " + size.Name + "; captures skipped");
                        Assert.Ignore("offscreen UI Toolkit panel unavailable (no GPU?)");
                    }
                    string slug = "hud-layout-" + size.Name.Replace(" ", "-");
                    surface.Stage.style.backgroundColor = new Color(0.16f, 0.24f, 0.38f, 1f);
                    var (hud, chrome, ascent) = Flight(surface, size, null);
                    var paths = new List<string> { UiCapture.Save(surface.Texture, slug + "-flight") };
                    ascent.Preview(LongAlt, LongSpeed, LongVerdict, EngineOut());
                    surface.Pump(6);
                    paths.Add(UiCapture.Save(surface.Texture, slug + "-engine-out"));
                    chrome.Show(true);
                    chrome.Layout(1f);
                    surface.Pump(6);
                    paths.Add(UiCapture.Save(surface.Texture, slug + "-pause"));
                    chrome.Show(false);
                    ascent.Close();
                    huds.Remove(ascent);
                    Report(surface, size, new ReportActions());
                    paths.Add(UiCapture.Save(surface.Texture, slug + "-report"));
                    foreach (var p in paths) Assert.That(System.IO.File.Exists(p), Is.True, p);
                }
                finally
                {
                    surface?.Dispose();
                }
            }
        }

        // ---- builders -----------------------------------------------------------------------------

        (VisualElement hud, FlightChrome chrome, AscentHud ascent) Flight(DeviceSurface surface, Size size, AscentPrompt? call)
        {
            surface.Stage.Clear();
            var hud = new VisualElement { name = UiIds.ScreenFlight };
            hud.style.position = Position.Absolute;
            hud.style.left = 0; hud.style.top = 0;
            hud.style.width = size.PanelW;
            hud.style.height = size.PanelH;
            hud.style.flexDirection = FlexDirection.Column;
            var band = new VisualElement { name = "hud-band" };
            var top = new VisualElement();
            top.AddToClassList("hud-top");
            top.style.flexDirection = FlexDirection.Row;
            top.Add(new Label { name = "h-t", text = "T+ 1:12.4" });
            top.Add(new Label { name = "h-stage", text = "S1" });
            band.Add(top);
            hud.Add(band);
            var open = new VisualElement { name = "test-open-column", pickingMode = PickingMode.Ignore };
            open.style.flexGrow = 1;
            hud.Add(open);
            var cast = new VisualElement { name = "webcast" };
            cast.Add(new Label { name = "h-verdict", text = LongVerdict });
            hud.Add(cast);
            surface.Stage.Add(hud);

            var go = new GameObject("kh-hud-layout-device") { hideFlags = HideFlags.HideAndDontSave };
            made.Add(go);
            var chrome = go.AddComponent<FlightChrome>();
            chrome.Bind(hud, _ => { }, () => { }, () => { }, _ => { }, FlightViewMode.Auto);
            chrome.Layout(1f);
            AscentHud.ForceWide = size.Wide;
            AscentHud.ForceCssW = size.CssW;
            AscentHud.ForceCssH = size.CssH;
            var ascent = new AscentHud();
            huds.Add(ascent);
            ascent.Open(hud, 1f);
            surface.Pump(4);
            ascent.Preview(LongAlt, LongSpeed, LongVerdict, call);
            surface.Pump(6);
            chrome.Layout(1f);
            ascent.Layout(1f);
            surface.Pump(6);
            return (hud, chrome, ascent);
        }

        static VisualElement Report(DeviceSurface surface, Size size, ReportActions actions)
        {
            var frame = ReportLayout.Solve(size.PanelW, size.PanelH, size.SafeTop, size.SafeBottom, 0, 0, true);
            var failure = new FailureEvent
            {
                kind = "structural-maxq",
                t = 41,
                kid = "The air pushed harder than the rocket could take, right where it was going fastest.",
                engineer = "Dynamic pressure crossed the airframe limit 3 s after the throttle bucket should have started.",
            };
            var log = new TelemetryLog();
            log.Push(0, 0, 0, 0, 1, 1000, 0, 0, 0);
            log.Push(20, 4000, 250, 18000, 2, 850, 10, 4, 2);
            log.Push(41, 12000, 520, 38000, 3, 700, 20, 8, 4);
            log.Mark(41, "structural-maxq", true);
            var facts = new FlightFacts { Failure = failure, Passed = false, Seed = 7, ApoKm = 41.2, GoalKm = 100, PeakG = 6.8 };
            var debrief = FlightDebrief.Compose(null, MissionContext.Free(), facts, NarrationPool.Empty, 100000, null);
            var card = ReportView.Build(failure, log, frame, true, null, debrief, actions);
            ReportCardChrome.Present(surface.Stage, card, 1f, size.SafeTop, size.SafeBottom);
            surface.Pump(8);
            ReportCardChrome.Dress(card, 1f, size.SafeTop, size.SafeBottom);
            surface.Pump(6);
            return card;
        }

        // ---- checks -------------------------------------------------------------------------------

        static void CheckHud(VisualElement hud, DeviceSurface surface, Size size, string at)
        {
            var screen = surface.Stage.worldBound;
            Assert.That(TextFit.Problems(hud), Is.Empty, at + " text fits its own box");
            var texts = new List<VisualElement>();
            foreach (var t in hud.Query<TextElement>().ToList())
            {
                if (!Shown(t) || string.IsNullOrWhiteSpace(t.text)) continue;
                if (InPauseMenu(t)) continue;
                OnScreen(t.worldBound, screen, at + " " + Name(t));
                var parent = t.parent;
                if (parent != null && parent != hud && Shown(parent) && !(parent is TemplateContainer))
                    Inside(t.worldBound, parent.worldBound, at + " " + Name(t) + " inside " + Name(parent));
            }
            // Regions of the HUD: the strip, the health tabs, the stages banner, the dock and the tools row.
            var regions = new List<VisualElement>();
            foreach (var n in new[] { "ascent-telemetry", "ascent-health", "ascent-actions", UiIds.FlightFollow, UiIds.FlightPause, UiIds.FlightCamera })
            {
                var e = hud.Q(n);
                if (e != null && Shown(e)) regions.Add(e);
            }
            NoOverlap(regions, at + " regions");
            // Siblings inside each region.
            foreach (var r in regions)
                foreach (var container in r.Query<VisualElement>().ToList())
                {
                    if (!Shown(container)) continue;
                    var kids = new List<VisualElement>();
                    foreach (var k in container.Children())
                        if (Shown(k) && (k is TextElement || k.childCount > 0)) kids.Add(k);
                    NoOverlap(kids, at + " in " + Name(container));
                }
            // Every shown control outside the pause menu is a 44 pt target that takes the tap at its centre.
            // The health tabs draw 28 pt inside their 44 pt band (checked in the L1 test), so their height is the band's.
            float unit = size.PanelW / size.CssW;
            foreach (var b in hud.Query<Button>().ToList())
            {
                if (!Shown(b) || InPauseMenu(b)) continue;
                var r = b.worldBound;
                bool tab = b.parent != null && b.parent.name == AscentHud.HealthRowName;
                float h = tab ? b.parent.worldBound.height : r.height;
                Assert.That(h, Is.GreaterThanOrEqualTo(44f * unit - 1f), at + " " + Name(b) + " target height " + r);
                Assert.That(r.width, Is.GreaterThanOrEqualTo(44f * unit - 1f), at + " " + Name(b) + " target width " + r);
                var hit = hud.panel.Pick(r.center);
                bool onIt = false;
                for (var v = hit; v != null; v = v.parent) if (v == b) onIt = true;
                Assert.That(onIt, Is.True, at + " " + Name(b) + " receives the tap (got " + (hit != null ? Name(hit) : "null") + ")");
            }
            var verdict = hud.Q<Label>("h-verdict");
            if (verdict != null && Shown(verdict))
            {
                OnScreen(verdict.worldBound, screen, at + " stage banner");
                Assert.That(TextFit.Problems(verdict), Is.Empty, at + " stage banner text");
                // #322: the whole banner (its own row on narrow phones) clears the strip, the health tabs, the dock,
                // the tools row and the open picker's seat.
                foreach (var r in regions)
                    if (!Contains(r, verdict))
                        Assert.That(Overlaps(verdict.worldBound, r.worldBound), Is.False, at + " stage banner " + verdict.worldBound + " over " + Name(r) + " " + r.worldBound);
                var picker = hud.Q(UiIds.FlightCameraPicker);
                if (picker != null && Shown(picker))
                    Assert.That(Overlaps(verdict.worldBound, picker.worldBound), Is.False, at + " stage banner over the camera picker");
            }
        }

        static bool InPauseMenu(VisualElement e)
        {
            for (var v = e; v != null; v = v.parent) if (v.name == UiIds.ScreenPause) return true;
            return false;
        }

        static Label FindByText(VisualElement root, string part)
        {
            foreach (var l in root.Query<Label>().ToList())
                if (Shown(l) && !string.IsNullOrEmpty(l.text) && l.text.IndexOf(part, StringComparison.Ordinal) >= 0) return l;
            return null;
        }

        static string Name(VisualElement e) => string.IsNullOrEmpty(e.name) ? e.GetType().Name + (e is TextElement t ? " \"" + t.text + "\"" : "") : e.name;

        static void NoOverlap(IList<VisualElement> items, string at)
        {
            for (int i = 0; i < items.Count; i++)
                for (int j = i + 1; j < items.Count; j++)
                {
                    if (Contains(items[i], items[j]) || Contains(items[j], items[i])) continue;
                    Assert.That(Overlaps(items[i].worldBound, items[j].worldBound), Is.False,
                        at + ": " + Name(items[i]) + " " + items[i].worldBound + " overlaps " + Name(items[j]) + " " + items[j].worldBound);
                }
        }

        static bool Contains(VisualElement a, VisualElement b)
        {
            for (var v = b; v != null; v = v.parent) if (v == a) return true;
            return false;
        }

        static void Inside(Rect inner, Rect outer, string at)
        {
            Assert.That(inner.xMin >= outer.xMin - 1f && inner.yMin >= outer.yMin - 1f && inner.xMax <= outer.xMax + 1f && inner.yMax <= outer.yMax + 1f,
                Is.True, at + ": " + inner + " outside " + outer);
        }

        static void OnScreen(Rect r, Rect screen, string at) => Inside(r, screen, at + " on screen");

        static bool Shown(VisualElement e)
        {
            for (var v = e; v != null; v = v.parent)
                if (v.resolvedStyle.display == DisplayStyle.None || v.resolvedStyle.visibility == Visibility.Hidden) return false;
            var r = e.worldBound;
            return r.width > 0.5f && r.height > 0.5f && !float.IsNaN(r.width);
        }

        static bool Overlaps(Rect a, Rect b) =>
            a.xMin < b.xMax - 0.5f && a.xMax > b.xMin + 0.5f && a.yMin < b.yMax - 0.5f && a.yMax > b.yMin + 0.5f;

        static void Click(Button button)
        {
            Assert.That(button, Is.Not.Null);
            using (var ev = NavigationSubmitEvent.GetPooled())
            {
                ev.target = button;
                button.SendEvent(ev);
            }
        }

        // Same surface as FlightCameraPickerDeviceTests (#304): an EditorWindow, else an offscreen UIDocument.
        // Failures to create one degrade (warning / null), never LogError.
        sealed class DeviceSurface : IDisposable
        {
            EditorWindow window;
            GameObject go;
            PanelSettings settings;
            RenderTexture texture;
            public VisualElement Stage { get; private set; }
            public RenderTexture Texture => texture;

            public static DeviceSurface TryOffscreen(float w, float h, float scale)
            {
                var surface = new DeviceSurface();
                surface.TryDocument(w, h, scale);
                surface.Pump(6);
                if (surface.Sized(w, h) && surface.texture != null) return surface;
                surface.Dispose();
                return null;
            }

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
                {
                    surface.Dispose();
                    Assert.Fail("panel did not take the device size " + w + "x" + h);
                }
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
                    window = ScriptableObject.CreateInstance<HudLayoutDeviceWindow>();
                    window.position = new Rect(0f, 0f, w, h);
                    window.minSize = new Vector2(w, h);
                    window.maxSize = new Vector2(w, h);
                    window.ShowUtility();
                    var root = window.rootVisualElement;
                    if (root == null) return;
                    Stage = MakeStage(root, w, h);
                }
                catch (Exception e) { Debug.LogWarning("[hud-layout test] window: " + e.Message); }
            }

            void TryDocument(float w, float h, float scale = 1f)
            {
                try
                {
                    texture = new RenderTexture(Mathf.Max(8, (int)(w * scale)), Mathf.Max(8, (int)(h * scale)), 0, RenderTextureFormat.ARGB32);
                    texture.Create();
                    settings = ScriptableObject.CreateInstance<PanelSettings>();
                    settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                    settings.scale = scale;
                    var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss");
                    if (theme != null) settings.themeStyleSheet = theme;
                    else Debug.LogWarning("[hud-layout test] no runtime theme; capture text may use the fallback face");
                    settings.targetTexture = texture;
                    go = new GameObject("kh-hud-layout-surface") { hideFlags = HideFlags.HideAndDontSave };
                    var doc = go.AddComponent<UIDocument>();
                    doc.panelSettings = settings;
                    var root = doc.rootVisualElement;
                    if (root == null) return;
                    Stage = MakeStage(root, w, h);
                }
                catch (Exception e) { Debug.LogWarning("[hud-layout test] offscreen panel: " + e.Message); }
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
                go = null;
                if (settings != null) UnityEngine.Object.DestroyImmediate(settings);
                settings = null;
                if (texture != null)
                {
                    texture.Release();
                    UnityEngine.Object.DestroyImmediate(texture);
                    texture = null;
                }
            }
        }

        sealed class HudLayoutDeviceWindow : EditorWindow { }
    }
}
