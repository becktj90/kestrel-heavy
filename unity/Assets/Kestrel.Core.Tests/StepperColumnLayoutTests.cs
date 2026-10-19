// Web designer overlap audit (beckify.com 50ae4f3, Trevor's HP laptop). Guided steps 1-3 with a
// warning showing, at 1280x720, 1366x768 and 1920x1080 landscape web and 335 portrait: no sibling
// rects intersect, every label fits its box, Fix it / Back / Parts keep their words, and the
// "N things to fix" pill has its own band under Home.
using System.Collections.Generic;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StepperColumnLayoutTests
    {
        // Design panel padding (SliceApp: 12 pt each side) and the "▼ Hopper" handle (56 pt) above the stepper.
        const float PanelPad = 12f, Handle = 56f, PanelBottom = 8f;

        struct Column { public string Name; public float W, H; public WebDesignerFrame Frame; }

        static IEnumerable<Column> Columns()
        {
            foreach (var (w, h) in new[] { (1280, 720), (1366, 768), (1920, 1080) })
            {
                WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
                float s = WebLandscapeType.PanelScale(w, h, rw, rh, m);
                float pw = w / s, ph = h / s;
                var f = WebDesignerLayout.Place(pw, ph, 0, 0, 0, 0);
                yield return new Column
                {
                    Name = $"{w}x{h}",
                    W = f.Parts.W - 2f * PanelPad,
                    H = f.Parts.H - Handle - PanelBottom,
                    Frame = f,
                };
            }
            // 335 portrait: the guided stepper owns the sheet (StepperLayout.Solve), 8 pt SE insets, under the handle.
            yield return new Column { Name = "335 portrait", W = 335f - 2f * 8f, H = 720f - Handle - 2f * PanelBottom };
        }

        // Steps 1-3 with their warning copy showing (the shipped Hopper case from the screenshots).
        static readonly (string title, string check, string extra)[] Steps =
        {
            ("Mission goal", "!  Only 3% dv margin for this trip. A little more propellant gives breathing room.", "Pick the job: payload and orbit."),
            ("Propellant & engines", "!  This rocket runs out of fuel before the trip is done. Add fuel or another stage.", "Your rocket ran out of fuel at T+0 s, short of the target."),
            ("Tanks & stages", "!  Not quite enough speed for this trip. Add fuel or a lighter cargo.", "Only 3% dv margin for First Hop. A little more propellant gives breathing room."),
        };

        [Test]
        public void GuidedStepsOneToThreeNeverOverlap()
        {
            foreach (var col in Columns())
                foreach (var step in Steps)
                    foreach (bool confirm in new[] { false, true })
                    {
                        var f = StepperColumnLayout.Solve(col.W, col.H, step.title, step.check.Length, step.extra.Length, confirm, nextAnyway: true);
                        string at = col.Name + " " + step.title + (confirm ? " +confirm" : "");
                        var stack = new List<(string, HudBox)> { ("count", f.Count), ("title", f.Title), ("tabs", f.Tabs), ("tiles", f.Tiles), ("scroll", f.Scroll), ("nav", f.Nav) };
                        if (!f.CheckInScroll) stack.Add(("check", f.Check));
                        if (confirm) stack.Add(("confirm", f.Confirm));
                        NoOverlap(stack, at);
                        foreach (var (n, b) in stack) Inside(b, col.W, col.H, at + " " + n);
                        if (f.CheckInScroll) Inside(f.Check, f.Scroll, at + " check in the scroller");
                        // The controls keep a usable scroller: 120 pt, or two 44 pt rows while the confirm card is up.
                        float least = col.Name.Contains("portrait") ? 44f : confirm ? 88f : StepperColumnLayout.MinScrollPt - 0.5f;
                        Assert.That(f.Scroll.H, Is.GreaterThanOrEqualTo(least), at + " scroll room");

                        NoOverlap(new List<(string, HudBox)> { ("simple", f.Simple), ("engineer", f.Engineer), ("all parts", f.AllParts) }, at + " tabs");
                        foreach (var b in new[] { f.Simple, f.Engineer, f.AllParts }) Inside(b, f.Tabs, at + " tab row");
                        NoOverlap(new List<(string, HudBox)> { ("back", f.Back), ("next anyway", f.NextAnyway), ("fix it", f.Forward) }, at + " nav");
                        foreach (var b in new[] { f.Back, f.NextAnyway, f.Forward }) Inside(b, f.Nav, at + " nav row");

                        var tiles = new List<(string, HudBox)>();
                        for (int i = 0; i < f.TileBoxes.Count; i++) tiles.Add(("tile " + (i + 1), f.TileBoxes[i]));
                        NoOverlap(tiles, at);
                        foreach (var (n, b) in tiles)
                        {
                            Inside(b, f.Tiles, at + " " + n);
                            Fits(StepperText.Segment, f.TilePt, b.W - 2f * f.TilePad - StepperColumnLayout.TileBorder, at + " " + n + " label");
                        }
                        Assert.That(f.TilePt, Is.GreaterThanOrEqualTo(13f), at + " tile type");

                        Fits(StepperText.Simple, 16f, f.Simple.W - 24f, at + " Simple");
                        Fits(StepperText.Engineer, 16f, f.Engineer.W - 24f, at + " Engineer");
                        Fits(StepperText.AllParts, 16f, f.AllParts.W - 24f, at + " All parts");
                        Fits(StepperText.Back, 16f, f.Back.W - 24f, at + " Back");
                        Fits(StepperText.NextAnyway, 16f, f.NextAnyway.W - 24f, at + " Next anyway");
                        Fits(StepperText.FixIt, 16f, f.Forward.W - 24f, at + " Fix it");
                        Fits(StepperText.Count, 13f, f.Count.W, at + " STEP N OF 7");
                        Assert.That(f.TitleLines * 22f * 1.3f, Is.LessThanOrEqualTo(f.Title.H + 0.5f), at + " title rows");
                        foreach (var b in new[] { f.Back, f.NextAnyway, f.Forward, f.Simple, f.Engineer, f.AllParts })
                            Assert.That(b.H, Is.GreaterThanOrEqualTo(44f), at + " 44 pt target");
                    }
        }

        [Test]
        public void ShortUltrawidePanelsKeepTheStackInsideTheColumn()
        {
            // 3440x1440 (and 2560x1080) scale to a short, wide panel (~1484x621). With the confirm card up the
            // scroller can drop under one 44 pt row. StepperShell honours that height rather than raising it,
            // so every sticky block, the scroller included, must still end inside the column.
            foreach (var (w, h) in new[] { (3440, 1440), (2560, 1080), (1280, 720) })
            {
                WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
                float s = WebLandscapeType.PanelScale(w, h, rw, rh, m);
                var frame = WebDesignerLayout.Place(w / s, h / s, 0, 0, 0, 0);
                float cw = frame.Parts.W - 2f * PanelPad, ch = frame.Parts.H - Handle - PanelBottom;
                foreach (var step in Steps)
                    foreach (bool confirm in new[] { false, true })
                    {
                        var f = StepperColumnLayout.Solve(cw, ch, step.title, step.check.Length, step.extra.Length, confirm, nextAnyway: true);
                        string at = $"{w}x{h} ({cw:0}x{ch:0}) {step.title}" + (confirm ? " +confirm" : "");
                        var stack = new List<(string, HudBox)> { ("count", f.Count), ("title", f.Title), ("tabs", f.Tabs), ("tiles", f.Tiles), ("scroll", f.Scroll), ("nav", f.Nav) };
                        if (!f.CheckInScroll) stack.Add(("check", f.Check));
                        if (confirm) stack.Add(("confirm", f.Confirm));
                        NoOverlap(stack, at);
                        foreach (var (n, b) in stack) Inside(b, cw, ch, at + " " + n);
                        Assert.That(f.Scroll.H, Is.GreaterThanOrEqualTo(0f), at + " scroll");
                        // Sum of the sticky stack equals the column: nothing to push the nav out.
                        Assert.That(f.Nav.Bottom, Is.LessThanOrEqualTo(ch + 0.5f), at + " nav in the column");
                    }
            }
        }

        [Test]
        public void TheWideColumnHoldsTheHeaderTabsAndNavOnOneRowEach()
        {
            foreach (var col in Columns())
            {
                if (col.Name.Contains("portrait")) continue;
                var f = StepperColumnLayout.Solve(col.W, col.H, "Tanks & stages", 70, 80, false, true);
                Assert.That(f.TabsOwnRow, col.Name + " Simple | Engineer | All parts on one row under the title");
                Assert.That(f.NavWraps, Is.False, col.Name + " Back, Next anyway and Fix it on one row");
                Assert.That(f.TilesPerRow, Is.GreaterThanOrEqualTo(3), col.Name);
                Assert.That(f.Title.Y, Is.GreaterThanOrEqualTo(f.Count.Bottom), col.Name + " title under STEP N OF 7");
                Assert.That(f.Tabs.Y, Is.GreaterThanOrEqualTo(f.Title.Bottom), col.Name + " tabs under the title");
                Assert.That(f.Tiles.Y, Is.GreaterThanOrEqualTo(f.Tabs.Bottom), col.Name + " tiles under the tabs");
            }
        }

        [Test]
        public void ThePillHasItsOwnBandEvenWithALongMissionTitle()
        {
            foreach (var col in Columns())
            {
                if (col.Frame == null) continue;
                var f = col.Frame;
                float pillW = StepperText.Width(StepperText.Pill, 15f) + 24f;
                Assert.That(f.Issue.W, Is.GreaterThanOrEqualTo(pillW), col.Name + " pill width");
                Assert.That(f.Issue.H, Is.GreaterThanOrEqualTo(32f), col.Name);
                NoOverlap(new List<(string, HudBox)> { ("home", f.Home), ("pill", f.Issue), ("parts", f.Parts), ("exit", f.Exit) }, col.Name);
                // A long mission name wraps inside the panel's own title row, under the pill band.
                string title = "Kestrel Heavy demo mission to a very high polar orbit";
                int lines = StepperText.Lines(title.Length, 20f, f.Parts.W - 2f * PanelPad);
                Assert.That(lines * 20f * 1.3f, Is.LessThan(f.Parts.H * 0.25f), col.Name + " long title row");
                Assert.That(f.Parts.Y, Is.GreaterThanOrEqualTo(f.Issue.Bottom), col.Name + " title starts under the pill");
            }
            // Portrait web: the pill sits under Home, above the sheet.
            var phone = WebDesignerLayout.Place(335f, 720f, 0, 0, 0, 0);
            Assert.That(phone.Issue.Bottom, Is.LessThanOrEqualTo(phone.RightPanel.Y), "portrait pill above the sheet");
        }

        [Test]
        public void ReadoutColumnKeepsThumbAndLaunchWords()
        {
            foreach (var col in Columns())
            {
                if (col.Frame == null) continue;
                float inner = col.Frame.RightPanel.W - 24f;
                // Thumb row at 13 pt with 12 pt padding: each button keeps its word on one line.
                foreach (var adv in new[] { StepperText.Undo, StepperText.Redo, StepperText.Parts, StepperText.Hangar, StepperText.Designer })
                    Assert.That(StepperColumnLayout.Btn(adv, 13f, 44f), Is.LessThanOrEqualTo(inner), col.Name);
                // Free-designer launch row: Guided steps, Launch and Fix it wrap as whole buttons; the reason has its own row.
                foreach (var adv in new[] { StepperText.GuidedSteps, StepperText.Launch, StepperText.FixIt })
                    Assert.That(StepperColumnLayout.Btn(adv, 16f, 72f), Is.LessThanOrEqualTo(inner), col.Name);
                Assert.That(StepperText.Lines(80, 13f, inner), Is.LessThanOrEqualTo(6), col.Name + " reason wraps by words, not letters");
            }
        }

        [Test]
        public void HomeCreditsChipHasItsOwnCorner()
        {
            // "0 credits" drew on KESTREL (390 portrait) and on the stats card (1366x768). It now sits in the
            // top-left band, clear of the gear and EXIT, and the wide home keeps that band empty.
            foreach (var (w, h) in new[] { (335f, 720f), (390f, 844f), (1280f, 720f), (1366f, 768f), (1920f, 1080f) })
            {
                var credits = HomeCornerLayout.Credits(w, 0f, true);
                HomeCornerLayout.Place(w, 0f, true, out _, out var gear, out var exit);
                string at = w + "x" + h;
                Assert.That(credits.Overlaps(gear), Is.False, at + " gear");
                Assert.That(credits.Overlaps(exit), Is.False, at + " exit");
                Assert.That(credits.H, Is.GreaterThanOrEqualTo(44f), at);
                // "99,999 credits" at the 11 pt floor of HomeResumeBar.Fit, plus CONTINUE.
                Assert.That(credits.W, Is.GreaterThanOrEqualTo(StepperText.Width(14 * StepperText.BodyChar, 11f)), at + " chip text");
                if (w <= h) continue;
                WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
                float s = WebLandscapeType.PanelScale(w, h, rw, rh, m);
                var home = WebHomeLayout.Place(w / s, h / s, 0, 0, 0, 0);
                var c = new HudBox { X = credits.X, Y = credits.Y, W = credits.W, H = credits.H };
                foreach (var (n, b) in new[] { ("title", home.Title), ("stats", home.Stats), ("launch", home.Launch), ("menu", home.Menu), ("rocket", home.Rocket) })
                    NoOverlap(new List<(string, HudBox)> { ("credits", c), (n, b) }, at + " wide home");
            }
        }

        static void Fits(float advance, float pt, float box, string where) =>
            Assert.That(StepperText.Width(advance, pt), Is.LessThanOrEqualTo(box + 0.5f), where + " does not fit");

        static void Inside(HudBox b, float w, float h, string where)
        {
            Assert.That(b.X, Is.GreaterThanOrEqualTo(-0.5f), where);
            Assert.That(b.Y, Is.GreaterThanOrEqualTo(-0.5f), where);
            Assert.That(b.Right, Is.LessThanOrEqualTo(w + 0.5f), where);
            Assert.That(b.Bottom, Is.LessThanOrEqualTo(h + 0.5f), where);
        }

        static void Inside(HudBox b, HudBox outer, string where)
        {
            Assert.That(b.X, Is.GreaterThanOrEqualTo(outer.X - 0.5f), where);
            Assert.That(b.Y, Is.GreaterThanOrEqualTo(outer.Y - 0.5f), where);
            Assert.That(b.Right, Is.LessThanOrEqualTo(outer.Right + 0.5f), where);
            Assert.That(b.Bottom, Is.LessThanOrEqualTo(outer.Bottom + 0.5f), where);
        }

        static void NoOverlap(List<(string, HudBox)> boxes, string where)
        {
            for (int i = 0; i < boxes.Count; i++)
                for (int j = i + 1; j < boxes.Count; j++)
                {
                    var a = boxes[i].Item2; var b = boxes[j].Item2;
                    if (!a.Live || !b.Live) continue;
                    bool hit = a.X < b.Right - 0.5f && b.X < a.Right - 0.5f && a.Y < b.Bottom - 0.5f && b.Y < a.Bottom - 0.5f;
                    Assert.That(hit, Is.False, $"{where}: {boxes[i].Item1} {Show(a)} overlaps {boxes[j].Item1} {Show(b)}");
                }
        }

        static string Show(HudBox b) => $"[{b.X:0},{b.Y:0} {b.W:0}x{b.H:0}]";
    }
}
