// LANDSCAPE_WEB_BRIEF acceptance in panel points: 1280x720, 1920x1080, 2560x1440 and an ultrawide.
// With the 1280x720 / match 0.5 reference every 16:9 monitor is a 1280x720 panel, so the caps hold
// at every size and nothing reads as a billboard. B-10's launch gate rides here too.
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class WebLandscapeShellTests
    {
        static readonly (int w, int h)[] Monitors = { (1280, 720), (1920, 1080), (2560, 1440), (3440, 1440), (1366, 768), (1024, 768) };

        static void Panel(int w, int h, out float pw, out float ph)
        {
            WebLandscapeType.PanelReference(true, out float rw, out float rh, out float m);
            float s = WebLandscapeType.PanelScale(w, h, rw, rh, m);
            pw = w / s; ph = h / s;
        }

        /// <summary>
        /// PlayMode HomeCaptureTests (every web size): a 48 px KESTREL drew 60 px tall in a row budgeted at 1.05 em and
        /// ran over HEAVY. The title box holds the eyebrow, the wordmark's drawn line, the gap and HEAVY's line.
        /// </summary>
        [Test]
        public void WideTitleBoxHoldsTheWordmarksDrawnLine()
        {
            Assert.That(WebHomeLayout.WordmarkLine, Is.GreaterThanOrEqualTo(1.25f), "CI: Fraunces Black 48 px measured 59-60 px");
            foreach (var (w, h) in Monitors)
            {
                Panel(w, h, out float pw, out float ph);
                foreach (bool save in new[] { false, true })
                {
                    var f = WebHomeLayout.Place(pw, ph, 0f, 0f, 0f, 0f, Kestrel.Core.Features.HomeShow.None, save);
                    if (!f.Wide || f.Type == null) continue;
                    string at = $"{w}x{h} save={save}";
                    float block = WebHomeLayout.TitleBlock(f.Type.Title, f.Type.Tagline);
                    Assert.That(block, Is.EqualTo(f.Type.Title * WebHomeLayout.WordmarkLine + WebHomeLayout.TitleGap
                        + f.Type.Tagline * WebHomeLayout.Line).Within(0.01f), at);
                    Assert.That(block + WebHomeLayout.TitleBrow, Is.LessThanOrEqualTo(f.Title.H + 0.6f), at + " title rows fit the box");
                    Assert.IsNull(WebHomeLayout.TextFitError(f), at);
                }
            }
        }

        [Test]
        public void SixteenByNineMonitorsAreAllTheSamePanel()
        {
            foreach (var (w, h) in new[] { (1280, 720), (1920, 1080), (2560, 1440) })
            {
                Panel(w, h, out float pw, out float ph);
                Assert.That(pw, Is.EqualTo(1280f).Within(0.5f), $"{w}x{h}");
                Assert.That(ph, Is.EqualTo(720f).Within(0.5f), $"{w}x{h}");
            }
        }

        [Test]
        public void ThePhoneReferenceWasTheInflation()
        {
            // The old 375x667 match-height reference: 1.6x at 1080p, 2.2x at 1440p.
            Assert.That(WebLandscapeType.PanelScale(1920, 1080, 375, 667, 1f), Is.EqualTo(1.62f).Within(0.01f));
            Assert.That(WebLandscapeType.PanelScale(2560, 1440, 375, 667, 1f), Is.EqualTo(2.16f).Within(0.01f));
            WebLandscapeType.PanelReference(false, out float rw, out float rh, out float m);
            Assert.That((rw, rh, m), Is.EqualTo((375f, 667f, 1f)), "portrait keeps the phone column");
        }

        [Test]
        public void HomeTypeStaysUnderTheCaps()
        {
            foreach (var (w, h) in Monitors)
            {
                Panel(w, h, out float pw, out float ph);
                var f = WebHomeLayout.Place(pw, ph, 0, 0, 0, 0);
                string at = $"{w}x{h} -> {pw:0}x{ph:0}";
                Assert.That(f.Wide, at);
                var t = f.Type;
                Assert.That(t.Title, Is.LessThanOrEqualTo(WebLandscapeType.WordmarkCap + 0.01f), at);
                Assert.That(t.Title, Is.LessThanOrEqualTo(System.Math.Max(WebLandscapeType.WordmarkMin, ph * 0.12f) + 0.01f), at);
                Assert.That(t.Launch, Is.LessThanOrEqualTo(WebLandscapeType.CtaCap + 0.01f), at);
                foreach (var s in new[] { t.Design, t.TileName })
                    Assert.That(s, Is.LessThanOrEqualTo(WebLandscapeType.TileCap + 0.01f), at);
                foreach (var s in new[] { t.LaunchCaption, t.DesignCaption, t.TileCaption, t.Soon, t.Stat, t.Hint })
                    Assert.That(s, Is.LessThanOrEqualTo(WebLandscapeType.BodyCap + 0.01f), at);
                Assert.That(WebHomeLayout.TextFitError(f), Is.Null, at);
                InBand(pw, at, f.Rocket, f.Menu, f.Stats, f.Launch, f.Design, f.Missions);
            }
        }

        [Test]
        public void DesignerIsThreePanesInTheBand()
        {
            foreach (var (w, h) in Monitors)
            {
                Panel(w, h, out float pw, out float ph);
                var f = WebDesignerLayout.Place(pw, ph, 0, 0, 0, 0);
                string at = $"{w}x{h} -> {pw:0}x{ph:0}";
                Assert.That(f.Parts.Right, Is.LessThanOrEqualTo(f.Rocket.X), at);
                Assert.That(f.Rocket.Right, Is.LessThanOrEqualTo(f.RightPanel.X), at);
                float band = System.Math.Min(pw, WebLandscapeType.BandMax);
                Assert.That(f.Rocket.W, Is.GreaterThanOrEqualTo(band * WebLandscapeType.RocketMinFraction), at);
                foreach (var col in new[] { f.Parts, f.RightPanel })
                    Assert.That(col.W, Is.LessThanOrEqualTo(WebLandscapeType.SideCap), at);
                if (pw >= WebLandscapeType.Tight)
                {
                    // The stepper column is the wide one; the readouts only hold gauges and Launch.
                    Assert.That(f.Parts.W, Is.InRange(WebLandscapeType.PartsMin, WebLandscapeType.SideCap), at);
                    Assert.That(f.RightPanel.W, Is.InRange(WebLandscapeType.SideMin, WebLandscapeType.ReadoutMax), at);
                    Assert.That(f.Parts.W, Is.GreaterThanOrEqualTo(f.RightPanel.W), at);
                }
                Assert.That(f.Issue.Y, Is.GreaterThanOrEqualTo(f.Home.Bottom), at + " pill under Home");
                Assert.That(f.Issue.Bottom, Is.LessThanOrEqualTo(f.Parts.Y), at + " pill above the panel");
                Assert.That(f.Launch.H, Is.GreaterThanOrEqualTo(44f), at);
                InBand(pw, at, f.Parts, f.Rocket, f.RightPanel, f.Launch);
            }
        }

        [Test]
        public void BandCentresAndCaps()
        {
            WebLandscapeType.Band(0, 1600, out float l, out float r);
            Assert.That((l, r), Is.EqualTo((160f, 1440f)));
            WebLandscapeType.Band(10, 900, out l, out r);
            Assert.That((l, r), Is.EqualTo((10f, 900f)));
            Assert.That(WebLandscapeType.Cap(30, 12, 16), Is.EqualTo(16f));
            Assert.That(WebLandscapeType.Cap(11, 12, 16), Is.EqualTo(11f), "a box too small for the floor keeps the fitted size");
            Assert.That(WebLandscapeType.Hud(56), Is.EqualTo(18f), "no 56 px HUD on wide web");
        }

        static void InBand(float pw, string at, params HudBox[] boxes)
        {
            WebLandscapeType.Band(0, pw, out float l, out float r);
            foreach (var b in boxes)
            {
                if (!b.Live) continue;
                Assert.That(b.X, Is.GreaterThanOrEqualTo(l - 0.5f), at);
                Assert.That(b.Right, Is.LessThanOrEqualTo(r + 0.5f), at);
            }
        }

        // ---- B-10 -------------------------------------------------------------------------------

        static EngineeringReport Report(QuickReadout r = null) =>
            new EngineeringReport { ok = true, readout = r ?? Good() };

        static QuickReadout Good() => new QuickReadout { margin = 0.12, maxQStale = false, fairingFits = true, risks = new List<FlightRisk>() };

        [Test]
        public void GoodDesignIsReady()
        {
            var g = LaunchGate.Evaluate(Report(), null);
            Assert.That(g.Ready);
            Assert.That(g.Line, Is.EqualTo(Experience.Ready));
        }

        [Test]
        public void StaleAscentIsCheckingNotReady()
        {
            var r = Good(); r.maxQStale = true;
            var g = LaunchGate.Evaluate(Report(r), null);
            Assert.That(g.state, Is.EqualTo(LaunchGateState.Checking));
            Assert.That(g.Ready, Is.False);
            Assert.That(g.Line, Is.EqualTo(LaunchGate.Checking));
        }

        [Test]
        public void BlocksComeFirstWithTheirTab()
        {
            var rep = Report();
            rep.blocks.Add(new Issue { code = "x", tab = "engines", kid = "Add an engine.", msg = "no engine" });
            var g = LaunchGate.Evaluate(rep, null);
            Assert.That(g.state, Is.EqualTo(LaunchGateState.Blocked));
            Assert.That(g.reason, Is.EqualTo("Add an engine."));
            Assert.That(g.tab, Is.EqualTo("engines"));
            Assert.That(g.Line, Is.Not.EqualTo(Experience.Ready));
        }

        [Test]
        public void DeterministicRedBlocksSeededRedDoesNot()
        {
            var r = Good();
            r.risks.Add(new FlightRisk { kind = "seeded", level = "red", odds = 0.2, kid = "Small chance." });
            Assert.That(LaunchGate.Evaluate(Report(r), null).Ready, "a fair seeded event is not a certain failure");
            r.risks.Add(new FlightRisk { kind = "maxq", level = "red", odds = -1, tab = "stages", kid = "Too much push at max-Q." });
            var g = LaunchGate.Evaluate(Report(r), null);
            Assert.That(g.Ready, Is.False);
            Assert.That(g.tab, Is.EqualTo("stages"));
        }

        [Test]
        public void FitAndSpeedFailuresSendYouToTheirTab()
        {
            var r = Good(); r.fairingFits = false;
            Assert.That(LaunchGate.Evaluate(Report(r), null).tab, Is.EqualTo("payload"));
            r = Good(); r.margin = -0.05;
            Assert.That(LaunchGate.Evaluate(Report(r), null).tab, Is.EqualTo("tanks"));
            r = Good(); r.predictedFailure = new FailureEvent { kind = "breakup", kid = "It breaks up." };
            Assert.That(LaunchGate.Evaluate(Report(r), null).reason, Is.EqualTo("It breaks up."));
        }

        [Test]
        public void FreshReadoutBeatsTheReportCopy()
        {
            var stale = Good(); stale.maxQStale = true;
            Assert.That(LaunchGate.Evaluate(Report(stale), Good()).Ready, "the gate reads the readout it is handed");
        }
    }
}
