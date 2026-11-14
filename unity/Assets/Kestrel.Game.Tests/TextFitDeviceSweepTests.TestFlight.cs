// Test Flight (DECISIONS #102) strings in the text-fit sweep, worst-case values, at 335 portrait and the landscape
// frames (UX spec: "Add every string with worst-case values to text-fit sweep at 335 px AND web landscape").
using Kestrel.Core.Features;
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
using Kestrel.Game.Shell;
using Kestrel.Slice;
using Kestrel.Slice.DesignerV3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public partial class TextFitDeviceSweepTests
    {
        static readonly Frame[] TestFlightFrames =
        {
            new Frame("335 portrait", 335f, 720f),
            new Frame("335 landscape", 720f, 335f),
            new Frame("390 landscape", 844f, 390f),
            new Frame("web landscape", 1280f, 720f),
        };

        /// <summary>Every card the classifier can show, each line at its longest real value.</summary>
        static TestFlightCard[] WorstCards()
        {
            return new[]
            {
                new TestFlightCard { Outcome = TestOutcome.Orbit, Title = TestFlightCopy.OrbitTitle, Kid = TestFlightCopy.OrbitKid,
                    Engineer = TestFlightCopy.OrbitEngineer.Replace("{apogee}", "35,786").Replace("{perigee}", "35,786").Replace("{dvLeft}", "9,999") },
                new TestFlightCard { Outcome = TestOutcome.OutOfFuel, Kind = "prop-depletion", Title = TestFlightCopy.FuelTitle, Kid = TestFlightCopy.FuelKid, Hint = TestFlightCopy.FuelHint,
                    Engineer = TestFlightCopy.FuelEngineer.Replace("{alt}", "199").Replace("{speed}", "7,799").Replace("{short}", "7,999") },
                new TestFlightCard { Outcome = TestOutcome.NoLiftoff, Kind = "no-liftoff", Title = TestFlightCopy.LiftTitle, Kid = TestFlightCopy.LiftKid, Hint = TestFlightCopy.LiftHint,
                    Engineer = TestFlightCopy.LiftEngineer.Replace("{twr}", "0.99") },
                new TestFlightCard { Outcome = TestOutcome.MaxQBreakup, Kind = "structural-maxq", Title = TestFlightCopy.MaxQTitle, Kid = TestFlightCopy.MaxQKid, Hint = TestFlightCopy.MaxQHint,
                    Engineer = TestFlightCopy.MaxQEngineer.Replace("{q}", "999.9").Replace("{alt}", "199").Replace("{limit}", "999.9") },
                new TestFlightCard { Outcome = TestOutcome.LostControl, Kind = "tumble", Title = TestFlightCopy.SpinTitle, Kid = TestFlightCopy.SpinKid, Hint = TestFlightCopy.SpinHint,
                    Engineer = TestFlightCopy.SpinEngineer.Replace("{angle}", "180").Replace("{alt}", "199") },
                new TestFlightCard { Outcome = TestOutcome.FellShort, Kind = "fell-short", Title = TestFlightCopy.ShortTitle, Kid = TestFlightCopy.ShortKid, Hint = TestFlightCopy.ShortHint,
                    Engineer = TestFlightCopy.ShortEngineer.Replace("{alt}", "9,999").Replace("{speed}", "9,999").Replace("{needed}", "9,999") },
                // Physics ruling on #508: escape, past the Hill sphere, and suborbital.
                new TestFlightCard { Outcome = TestOutcome.Escape, Title = TestFlightCopy.EscapeTitle, Kid = TestFlightCopy.EscapeKid,
                    Engineer = TestFlightCopy.EscapeEngineer.Replace("{vinf}", "99,999") },
                new TestFlightCard { Outcome = TestOutcome.Orbit, Title = TestFlightCopy.OrbitTitle, Kid = TestFlightCopy.OrbitKid,
                    Engineer = TestFlightCopy.OrbitEngineer.Replace("{apogee}", "9,999,999").Replace("{perigee}", "35,786").Replace("{dvLeft}", "9,999") + ". " + TestFlightCopy.HillNote },
                new TestFlightCard { Outcome = TestOutcome.Suborbital, Kind = "fell-short", Title = TestFlightCopy.SubTitle, Kid = TestFlightCopy.SubKid, Hint = TestFlightCopy.SubHint,
                    Engineer = TestFlightCopy.SubEngineer.Replace("{apogee}", "99,999").Replace("{perigee}", "-6,371") },
            };
        }

        [Test]
        public void TestFlightCardsFitAt335AndLandscape()
        {
            foreach (var frame in TestFlightFrames)
            foreach (var model in WorstCards())
            {
                using (var surface = DeviceSurface.Open(frame.W, frame.H))
                {
                    var debrief = new FlightDebrief { TestCard = model };
                    var card = ReportView.Build(null, SampleLog(), ReportLayout.Solve(frame.W, frame.H, 0, 0, 0, 0, true), true, null, debrief, new ReportActions());
                    surface.Stage.Add(card);
                    ReportCardChrome.Present(surface.Stage, card, 1f, 0f, 0f);
                    surface.Pump(6);
                    Settle(surface, card);
                    string where = frame.Name + " " + model.Outcome;
                    AssertFits(card, where);
                    foreach (var id in new[] { UiIds.TestCardEyebrow, UiIds.TestCardTitle, UiIds.TestCardKid, UiIds.TestCardEngineer })
                        AssertStringInside(card.Q<Label>(id), where + " " + id);
                    if (!string.IsNullOrEmpty(model.Hint)) AssertStringInside(card.Q<Label>(UiIds.TestCardHint), where + " hint");
                    else Assert.That(card.Q<Label>(UiIds.TestCardHint), Is.Null, where + " orbit has no Try: line");
                    var retry = card.Q<Button>(UiIds.TestRetry);
                    var fresh = card.Q<Button>(UiIds.TestNewDesign);
                    var home = card.Q<Button>(UiIds.TestHome);
                    foreach (var b in new[] { retry, fresh, home })
                    {
                        Assert.That(b, Is.Not.Null, where);
                        Assert.That(b.worldBound.height, Is.GreaterThanOrEqualTo(44f), where + " " + b.name + " target");
                        Assert.That(b.worldBound.yMax, Is.LessThanOrEqualTo(surface.Stage.worldBound.yMax + 0.5f), where + " " + b.name + " on screen");
                    }
                    // Tweak & Retry full width; New Design and Home share the row under it.
                    Assert.That(retry.worldBound.width, Is.GreaterThan(fresh.worldBound.width * 1.6f), where);
                    Assert.That(Mathf.Abs(fresh.worldBound.y - home.worldBound.y), Is.LessThan(1f), where);
                    Assert.That(fresh.worldBound.y, Is.GreaterThan(retry.worldBound.y), where);
                    Assert.That(card.Q<Button>(UiIds.ReportNext), Is.Null, where + " a test has no Next mission");
                    Assert.That(card.Q(UiIds.ReportStarRow), Is.Null, where + " a test has no stars");
                }
            }
        }

        [Test]
        public void TestFlightHomeTileFitsAt335AndLandscape()
        {
            bool was = HomeFeatures.TestFlightTile;
            HomeFeatures.TestFlightTile = true;
            try
            {
                foreach (var frame in TestFlightFrames)
                {
                    MainMenuOverlay.ForceWide = frame.Landscape;
                    using (var surface = DeviceSurface.Open(frame.W, frame.H))
                    {
                        int pressed = 0;
                        var shell = Column(surface.Stage, out var host);
                        MainMenuOverlay.Build(shell, host, () => { }, () => { }, () => { }, true, testFlight: () => pressed++);
                        surface.Pump(8);
                        Settle(surface, shell);
                        AssertFits(shell, frame.Name + " home with Test Flight");
                        var tile = shell.Q<Button>(UiIds.HomeTestFlight);
                        Assert.That(tile, Is.Not.Null, frame.Name);
                        Assert.That(tile.resolvedStyle.display, Is.Not.EqualTo(DisplayStyle.None), frame.Name);
                        Assert.That(tile.worldBound.height, Is.GreaterThanOrEqualTo(44f), frame.Name + " target");
                        var name = tile.Q<Label>(className: "kh-mm-tile-name");
                        var cap = tile.Q<Label>(className: "kh-mm-tile-caption");
                        Assert.That(name.text, Is.EqualTo(TestFlightCopy.HomeLabel));
                        Assert.That(cap.text, Is.EqualTo(TestFlightCopy.HomeSub));
                        AssertStringInside(name, frame.Name + " Test Flight name");
                        AssertStringInside(cap, frame.Name + " Test Flight caption");
                        var launch = shell.Q<Button>("home-primary");
                        Assert.That(tile.worldBound.y, Is.GreaterThan(launch.worldBound.y), frame.Name + " under LAUNCH");
                        Click(tile);
                        Assert.That(pressed, Is.EqualTo(1), frame.Name + " first tap opens Test Flight");
                    }
                }
            }
            finally { HomeFeatures.TestFlightTile = was; MainMenuOverlay.ForceWide = null; }
        }

        [Test]
        public void TestFlightDesignerBandAndStarterTestItFit()
        {
            GameSession.Carry(MissionContext.Test());
            try
            {
                foreach (var frame in TestFlightFrames)
                {
                    using (var surface = DeviceSurface.Open(frame.W, frame.H))
                    {
                        var band = TestFlightBand.For(new HangarUi { Upp = 1f });
                        Assert.That(band, Is.Not.Null, frame.Name);
                        var column = MountInColumn(surface.Stage, band);
                        band.style.width = Mathf.Min(frame.W, 335f);
                        surface.Pump(4);
                        Settle(surface, column);
                        AssertFits(column, frame.Name + " designer band");
                        AssertStringInside(band.Q<Label>(UiIds.TestFlightEyebrow), frame.Name + " eyebrow");
                        AssertStringInside(band.Q<Label>(UiIds.TestFlightRules), frame.Name + " rules");

                        // "Test it" in its share of a 335-wide Starter CTA row (16 pt sides).
                        surface.Stage.Clear();
                        var box = StarterPanel.TestItBox(new HudBox { X = 16f, Y = 0f, W = 335f - 32f, H = 56f });
                        var b = new Button { name = UiIds.StarterTestIt, text = TestFlightCopy.TestIt };
                        b.style.position = Position.Absolute;
                        b.style.width = box.W; b.style.height = box.H;
                        b.style.fontSize = StarterPanel.TestItMaxPt;
                        surface.Stage.Add(b);
                        surface.Pump(3);
                        Assert.That(box.W, Is.GreaterThanOrEqualTo(44f));
                        AssertStringInside(b, frame.Name + " Test it");
                    }
                }
            }
            finally { GameSession.Clear(); }
        }
    }
}
