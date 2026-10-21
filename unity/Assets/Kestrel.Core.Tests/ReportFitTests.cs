// ReportFit (L4) and FlightHudLayout.QuoteLine (L2): the pure sizing behind the post-flight card and the
// one-line prompt quote. The device tests in Kestrel.Game.Tests check the same at real UI Toolkit rects.
using System.Collections.Generic;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class ReportFitTests
    {
        static readonly (float w, float h)[] Sizes =
        {
            (375f, 667f), (335f, 667f), (335f, 720f), (390f, 844f), (430f, 932f), (820f, 1180f), (1024f, 1366f),
            (1280f, 720f), (1280f, 800f), (1366f, 768f), (1920f, 1080f),
        };

        [Test]
        public void ShortLandscapePanelsAreCappedToTheMinimumCardHeight()
        {
            // 1280x800 web panel at the phone ratio: 800 px / 2.4 upp = 333 pt tall, too short for the exits.
            float upp = ReportFit.Units(2.4f, 1280f, 800f);
            Assert.That(800f / upp, Is.GreaterThanOrEqualTo(ReportFit.MinHeightPt - 0.01f));
            Assert.That(ReportFit.Units(1f, 390f, 844f), Is.EqualTo(1f), "a phone keeps its scale");
        }

        [Test]
        public void ContentIsCappedWithSideMarginsAndTheGridFitsTheWidth()
        {
            foreach (var (w, h) in Sizes)
            {
                float cw = ReportFit.ContentWidth(w);
                float side = ReportFit.SideMargin(w);
                Assert.That(cw, Is.LessThanOrEqualTo(ReportFit.MaxContentPt), w + " cap");
                Assert.That(side, Is.GreaterThanOrEqualTo(ReportFit.SidePt), w + " margin, not on the edge");
                Assert.That(cw + 2f * side, Is.LessThanOrEqualTo(w + 0.01f), w + " fits");
                foreach (int n in new[] { 5, 6 })
                {
                    int cols = ReportFit.GridColumns(cw, n);
                    float bw = ReportFit.ButtonWidth(cw, cols);
                    Assert.That(cols * bw + (cols - 1) * ReportFit.GapPt, Is.EqualTo(cw).Within(0.01f), w + " grid");
                    Assert.That(bw, Is.GreaterThanOrEqualTo(120f), w + " a button holds 'Back to designer' in two lines");
                }
            }
            Assert.That(ReportFit.GridColumns(ReportFit.ContentWidth(1280f), 6), Is.EqualTo(3), "wide: primary over 3 + 2");
            Assert.That(ReportFit.FootRows(ReportFit.ContentWidth(1280f), 6), Is.EqualTo(3));
            Assert.That(ReportFit.GridColumns(ReportFit.ContentWidth(375f), 6), Is.EqualTo(2), "phone: primary over 2 + 2 + 1");
            Assert.That(ReportFit.FootRows(ReportFit.ContentWidth(375f), 6), Is.EqualTo(4));
            Assert.That(ReportFit.FootRows(ReportFit.ContentWidth(375f), 5), Is.EqualTo(3), "main's five: primary over 2 + 2");
        }

        [Test]
        public void TheBodyAndEveryExitFitWithoutScrollingAtEverySize()
        {
            var lines = new List<ReportFit.Line>
            {
                new ReportFit.Line { Name = "failure-cause", Text = "Structural failure at max-Q: the fairing folded", FontPt = 22f },
                new ReportFit.Line { Name = "failure-numbers", Text = "Apogee 41.2 km of 100 km · peak 6.8 g · max-Q 38 kPa", FontPt = 16f },
                new ReportFit.Line { Name = "failure-kid", Text = "The air pushed harder than the rocket could take, right where it was going fastest.", FontPt = 16f },
                new ReportFit.Line { Name = "failure-engineer", Text = "Dynamic pressure crossed the airframe limit 3 s after the throttle bucket should have started.", FontPt = 16f, DropOrder = 5 },
                new ReportFit.Line { Name = "narrator-kid", Text = "Next time, throttle down before the rumble gets loud and the rocket will ride it out.", FontPt = 16f, DropOrder = 2 },
                new ReportFit.Line { Name = "narrator-eng", Text = "Pitch program and throttle bucket: hold q under the limit through transonic.", FontPt = 16f, DropOrder = 1 },
                new ReportFit.Line { Name = "report-stars", Text = "1 of 3 stars", FontPt = 22f },
                new ReportFit.Line { Name = "report-medals", Text = "Medal timing: Silver · 1 perfect · 0 good · 0 late", FontPt = 16f },
                new ReportFit.Line { Name = "report-kc", Text = "+120 Kc for the attempt", FontPt = 16f, DropOrder = 6 },
            };
            foreach (var (w, h) in Sizes)
            {
                float upp = ReportFit.Units(1f, w, h);
                float wPt = w / upp, hPt = h / upp;
                float cw = ReportFit.ContentWidth(wPt);
                int rows = ReportFit.FootRows(cw, 6);
                float room = hPt - ReportFit.FootPt(rows, 34f) - 47f - 4f;
                var plan = ReportFit.Body(lines, true, cw, room);
                Assert.That(plan.Fits, Is.True, w + "x" + h + " body " + plan.BodyPt + " room " + room);
                Assert.That(plan.Scale * 16f, Is.GreaterThanOrEqualTo(14f - 0.01f), w + "x" + h + " body floor");
                Assert.That(plan.Dropped.Contains("failure-cause") || plan.Dropped.Contains("report-stars"), Is.False, w + "x" + h + " keeps the cause and stars");
            }
        }

        [Test]
        public void GridCellsArePrimaryFullWidthThenNonOverlapping44PtCellsInsideTheFooter()
        {
            foreach (var (w, h) in Sizes)
            foreach (int n in new[] { 1, 2, 5, 6, 7 })
            {
                float upp = ReportFit.Units(1f, w, h);
                float cw = ReportFit.ContentWidth(w / upp);
                int rows = ReportFit.FootRows(cw, n);
                var cells = new List<ReportFit.Cell>();
                for (int i = 0; i < n; i++) cells.Add(ReportFit.GridCell(cw, n, i));
                string at = w + "x" + h + " n=" + n;
                Assert.That(cells[0].W, Is.EqualTo(cw).Within(0.01f), at + " primary full width");
                Assert.That(cells[0].Row, Is.EqualTo(0), at);
                for (int i = 0; i < n; i++)
                {
                    var c = cells[i];
                    Assert.That(c.W, Is.GreaterThanOrEqualTo(44f), at + " #" + i + " width");
                    Assert.That(c.X, Is.GreaterThanOrEqualTo(-0.01f), at + " #" + i + " left");
                    Assert.That(c.X + c.W, Is.LessThanOrEqualTo(cw + 0.01f), at + " #" + i + " right");
                    Assert.That(c.Row, Is.LessThan(rows), at + " #" + i + " row");
                    Assert.That(c.Y + ReportFit.HitPt, Is.LessThanOrEqualTo(rows * ReportFit.HitPt + (rows - 1) * ReportFit.GapPt + 0.01f), at + " #" + i + " bottom");
                    for (int j = i + 1; j < n; j++)
                    {
                        var d = cells[j];
                        bool apart = c.X + c.W <= d.X + 0.01f || d.X + d.W <= c.X + 0.01f || c.Y + ReportFit.HitPt <= d.Y + 0.01f || d.Y + ReportFit.HitPt <= c.Y + 0.01f;
                        Assert.That(apart, Is.True, at + " #" + i + " vs #" + j);
                    }
                }
            }
        }

        [Test]
        public void SixExitsFitAnSeScreenWithRoomForTheBody()
        {
            // #311's footer failed on SE: report-home's bottom at y 645 + 48 on a 667 screen.
            float cw = ReportFit.ContentWidth(375f);
            float foot = ReportFit.FootPt(ReportFit.FootRows(cw, 6), 0f);
            Assert.That(foot, Is.LessThanOrEqualTo(667f * 0.4f), "footer " + foot + " pt");
            var last = ReportFit.GridCell(cw, 6, 5);
            Assert.That(667f - foot + 8f + last.Y + ReportFit.HitPt, Is.LessThanOrEqualTo(667f - 12f + 0.01f), "last exit clears the bottom pad");
        }

        [Test]
        public void QuoteLineStepsDownThenCutsAtAWord()
        {
            const string q = "Engine 1 pressure drop. Shut it down?";
            var fit = FlightHudLayout.QuoteLine(q, 400f, FlightHudLayout.QuoteFontPt, FlightHudLayout.QuoteMinFontPt);
            Assert.That(fit.Text, Is.EqualTo(q));
            Assert.That(fit.FontPt, Is.EqualTo(FlightHudLayout.QuoteFontPt));
            var tight = FlightHudLayout.QuoteLine(q, 260f, FlightHudLayout.QuoteFontPt, FlightHudLayout.QuoteMinFontPt);
            Assert.That(tight.FontPt, Is.LessThan(FlightHudLayout.QuoteFontPt));
            Assert.That(FlightHudLayout.TextWidth(tight.Text, tight.FontPt), Is.LessThanOrEqualTo(258f));
            var cut = FlightHudLayout.QuoteLine(q, 120f, FlightHudLayout.QuoteFontPt, FlightHudLayout.QuoteMinFontPt);
            Assert.That(cut.Cut, Is.True);
            Assert.That(cut.FontPt, Is.EqualTo(FlightHudLayout.QuoteMinFontPt));
            Assert.That(cut.Text, Does.EndWith("…"));
            Assert.That(FlightHudLayout.TextWidth(cut.Text, cut.FontPt), Is.LessThanOrEqualTo(118f));
        }
    }
}
