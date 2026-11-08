// Post-flight card (§3): kid line, engineer line, Fix it deep link, plots, portrait from SE to iPad.
using Kestrel.Core.Ui;
using Kestrel.Core.Flight;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class ReportTests
    {
        static readonly object[] Devices =
        {
            new object[] { "iPhone SE", 375.0, 667.0, 20.0, 0.0, 0.0, 0.0 },
            new object[] { "iPhone Pro Max", 430.0, 932.0, 59.0, 34.0, 0.0, 0.0 },
            new object[] { "iPad", 1024.0, 1366.0, 24.0, 20.0, 0.0, 0.0 },
        };

        [TestCaseSource(nameof(Devices))]
        public void SheetFitsPortraitAndKeepsTheFailureVisible(string name, double w, double h, double top, double bottom, double left, double right)
        {
            var frame = ReportLayout.Solve(w, h, top, bottom, left, right, false);
            Assert.That(frame.Portrait, Is.True, name);
            Assert.That(frame.Fix.H, Is.GreaterThanOrEqualTo(ReportLayout.MinTargetPt), name);
            Assert.That(frame.Fix.W, Is.GreaterThanOrEqualTo(ReportLayout.MinTargetPt), name);
            Assert.That(frame.Icon.W, Is.GreaterThanOrEqualTo(ReportLayout.MinTargetPt), name);
            Assert.That(frame.Card.X, Is.GreaterThanOrEqualTo(left), name);
            Assert.That(frame.Card.Right, Is.LessThanOrEqualTo(w - right + 0.01), name);
            Assert.That(frame.Card.Y, Is.GreaterThanOrEqualTo(top - 0.01), name);
            Assert.That(frame.Card.Bottom, Is.LessThanOrEqualTo(h - bottom + 0.01), name);
            Assert.That(frame.Fix.Bottom, Is.LessThanOrEqualTo(frame.Card.Bottom + 0.01), name);
            Assert.That(frame.VisualBottom - frame.VisualTop, Is.GreaterThanOrEqualTo(h * 0.28), name + " leaves the failure on screen");
            Assert.That(ReportLayout.PanelAlpha, Is.EqualTo(1));
            Assert.That(ReportLayout.CoversDesignerStats(frame, h), Is.True, name + " designer stats stay behind the card");
            Assert.That(frame.RevealSeconds, Is.EqualTo(ReportLayout.RevealSeconds));
            var still = ReportLayout.Solve(w, h, top, bottom, left, right, true);
            Assert.That(still.RevealSeconds, Is.EqualTo(0));
        }

        [Test]
        public void CardUsesTheFailureSentencesAndFixLink()
        {
            var hit = new FailureHit { on = true, kind = FailureKind.StructuralMaxQ, cause = 0, value = 41000, limit = 33000, engine = -1, stage = 1 };
            var ev = FailureModel.Explain(hit, 62, 41000);
            Assert.That(ev.kid, Does.Contain("T+62"));
            Assert.That(ev.kid, Does.Contain("41 kPa"));
            Assert.That(ev.kid, Does.Contain("33 kPa"));
            var log = new TelemetryLog();
            log.Push(0, 0, 0, 0, 1, 1000, 0, 0, 0);
            log.Push(62, 8000, 400, 41000, 3, 700, 20, 8, 4);
            log.Mark(62, ev.kind, true);
            var frame = ReportLayout.Solve(375, 667, 20, 0, 0, 0, false);
            var card = ReportView.Build(ev, log, frame, false, null);
            Assert.That(card.Q<Label>("failure-kid").text, Is.EqualTo(ev.kid));
            Assert.That(card.Q<Label>("failure-engineer").text, Is.EqualTo(ev.engineer));
            var names = new System.Collections.Generic.List<string>();
            foreach (var child in card.Children()) names.Add(child.name);
            Assert.That(names, Is.EqualTo(new[] { "report-plate", "failure-icon", "failure-kid", "failure-engineer", "failure-plots", "report-actions" }));
            // NAV-05: with no debrief the card still has every exit, not only Fix it.
            foreach (var id in Kestrel.Core.Ui.UiIds.ReportExits)
                Assert.That(card.Q("report-actions").Q<Button>(id), Is.Not.Null, id);
            Assert.That(card.style.backgroundColor.value.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(card.Q("report-plate").style.backgroundColor.value.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(card.Q("failure-icon").childCount, Is.GreaterThanOrEqualTo(2));
            var fix = card.Q<Button>("failure-fix");
            Assert.That(fix.text, Is.EqualTo("Fix it"));
            var link = FixField.For("structural-maxq");
            Assert.That(fix.tooltip, Is.EqualTo(link.VoiceOver));
            Assert.That(fix.userData, Is.EqualTo(link.Uri));
            Assert.That(link.Uri, Is.EqualTo("kestrel://design?tab=structure&field=safetyFactor"));
            Assert.That(frame.Fix.H, Is.GreaterThanOrEqualTo(44));
        }

        [Test]
        public void FixLinkMatchesTheRiskFields()
        {
            Expect("structural-maxq", "structure", "safetyFactor");
            Expect("overg", "guidance", "gLimit");
            Expect("tumble", "aero", "fins");
            Expect("engine-overpressure", "engines", "engineParams.pc");
            Expect("bad-mixture", "propellants", "mixtureOffsetPct");
            Expect("thermal", "aero", "nose");
            Expect("cryo-material", "tanks", "material");
            Expect("prop-depletion", "propellants", "propMass");
            Expect("separation-collision", "stages", "sepImpulse");
            Expect("leg-collapse", "recovery", "legs.kind");
            Expect("engine-out", "engines", "engineCount");
        }

        [Test]
        public void FixJumpFocusesTheDesignerControl()
        {
            var root = new VisualElement();
            root.Add(new VisualElement { name = "design" });
            root.Add(new VisualElement { name = UiIds.ScreenFlight });
            var engines = new Button { name = "engines" };
            root.Add(engines);
            var prop = new Button { name = "prop" };
            root.Add(prop);
            FixField.Apply(root, FixField.For("engine-out"));
            Assert.That(engines.ClassListContains("fix-target"), Is.True);
            Assert.That(root.Q("design").ClassListContains("hidden"), Is.False);
            Assert.That(root.Q(UiIds.ScreenFlight).ClassListContains("hidden"), Is.True);
            Assert.That(FixField.Pending.Field, Is.EqualTo("engineCount"));
            FixField.Apply(root, FixField.For("prop-depletion"));
            Assert.That(prop.ClassListContains("fix-target"), Is.True);
            Assert.That(prop.tooltip, Does.Contain("Propellant"));
        }

        [Test]
        public void PlotsMarkTheFailureOnTheNearestSample()
        {
            var log = new TelemetryLog();
            log.Push(0, 0, 0, 1000, 1, 100, 0, 0, 0);
            log.Push(10, 5000, 200, 20000, 2, 80, 10, 4, 2);
            log.Push(20, 9000, 400, 5000, 1.5, 60, 30, 9, 5);
            log.Mark(9, "max-q", false);
            log.Mark(9, "structural-maxq", true);
            var plots = ReportPlot.Build(log);
            Assert.That(plots.Length, Is.EqualTo(5));
            Assert.That(plots[0].Channel, Is.EqualTo("alt"));
            Assert.That(plots[4].Channel, Is.EqualTo("losses"));
            Assert.That(plots[0].FailureX, Is.EqualTo(0.5f * (1f - ReportPlot.EdgeInset)).Within(1e-4));
            Assert.That(plots[0].X[0], Is.EqualTo(0f).Within(1e-4));
            Assert.That(plots[0].X[2], Is.EqualTo(1f - ReportPlot.EdgeInset).Within(1e-4));
            Assert.That(plots[0].Y[0], Is.EqualTo(0f).Within(1e-4));
            Assert.That(plots[0].Y[2], Is.EqualTo(1f).Within(1e-4));
            Assert.That(plots[4].Y[2], Is.EqualTo(1f).Within(1e-4), "loss total grows");
            Assert.That(plots[0].MarkerX.Length, Is.EqualTo(2));
            var frame = ReportLayout.Solve(375, 667, 20, 0, 0, 0, true);
            var card = ReportView.Build(new FailureEvent { kind = "structural-maxq", kid = "Bent.", engineer = "q high." }, log, frame, true, null);
            Assert.That(card.ClassListContains("reduce-motion"), Is.True);
            Assert.That(card.Q("plot-alt").Q("failure-marker"), Is.Not.Null);
            Assert.That(card.Q("failure-plots").tooltip, Does.Contain("Altitude"));
        }

        [Test]
        public void PlotBarsAndMarkersStayInsideThePlot()
        {
            // Area C, bug 2: a bar at X = 1 sat entirely outside its plot (left:100%, 2 pt wide, overflow
            // hidden) — and with it the losses panel's only tall bar, since the final loss totals are stamped
            // onto the newest sample only. Every plotted X is held back by ReportPlot.EdgeInset, so at the
            // narrowest tested plot (335 pt screen: 55 pt plots) even the 4 pt failure mark stays inside.
            var log = new TelemetryLog();
            log.Push(0, 0, 0, 0, 1, 1000, 0, 0, 0);
            log.Push(5, 2000, 100, 8000, 1.5, 900, 5, 2, 1);
            log.Push(10, 5000, 200, 20000, 2, 800, 10, 4, 2);
            log.Push(15, 8000, 300, 12000, 2.5, 700, 20, 6, 3);
            log.Push(20, 9000, 400, 5000, 1.5, 600, 30, 9, 5);
            log.Mark(0, "liftoff", false);
            log.Mark(20, "structural-maxq", true);
            var plots = ReportPlot.Build(log);
            float maxX = 1f - ReportPlot.EdgeInset;
            const float narrowestPlotPt = 55f; // 335 pt screen: (335 - 16 - 24 - 5 * 4) / 5
            foreach (var p in plots)
            {
                foreach (float x in p.X)
                {
                    Assert.That(x, Is.GreaterThanOrEqualTo(0f).And.LessThanOrEqualTo(maxX), p.Channel + " bar X " + x);
                    Assert.That(x * narrowestPlotPt + 2f, Is.LessThanOrEqualTo(narrowestPlotPt + 0.01f), p.Channel + " bar inside");
                }
                foreach (float x in p.MarkerX)
                {
                    Assert.That(x, Is.GreaterThanOrEqualTo(0f).And.LessThanOrEqualTo(maxX), p.Channel + " marker X " + x);
                    Assert.That(x * narrowestPlotPt + 2f, Is.LessThanOrEqualTo(narrowestPlotPt + 0.01f), p.Channel + " marker inside");
                }
                Assert.That(p.FailureX, Is.GreaterThanOrEqualTo(0f).And.LessThanOrEqualTo(maxX), p.Channel + " failure X");
                Assert.That(p.FailureX * narrowestPlotPt + 4f, Is.LessThanOrEqualTo(narrowestPlotPt + 0.01f), p.Channel + " failure mark inside");
            }
            Assert.That(plots[0].X[plots[0].X.Length - 1], Is.EqualTo(maxX).Within(1e-4), "the last sample still reaches the edge");
            Assert.That(plots[0].MarkerX[1], Is.EqualTo(maxX).Within(1e-4), "the last mark still reaches the edge");

            // The rendered bars carry the same inset: every bar/marker/failure left is within the plot bounds.
            var frame = ReportLayout.Solve(335, 720, 20, 0, 0, 0, true);
            var card = ReportView.Build(new FailureEvent { kind = "structural-maxq", kid = "Bent.", engineer = "q high." }, log, frame, true, null);
            foreach (var plot in card.Q("failure-plots").Children())
            {
                foreach (var bar in plot.Query<VisualElement>("bar").ToList())
                {
                    Assert.That(bar.style.left.value.unit, Is.EqualTo(LengthUnit.Percent), plot.name + " bar left is a percent");
                    Assert.That(bar.style.left.value.value, Is.GreaterThanOrEqualTo(0f).And.LessThanOrEqualTo(maxX * 100f), plot.name + " bar left");
                }
                foreach (var mark in plot.Query<VisualElement>("marker").ToList())
                    Assert.That(mark.style.left.value.value, Is.GreaterThanOrEqualTo(0f).And.LessThanOrEqualTo(maxX * 100f), plot.name + " marker left");
                var fail = plot.Q<VisualElement>("failure-marker");
                if (fail != null)
                    Assert.That(fail.style.left.value.value, Is.GreaterThanOrEqualTo(0f).And.LessThanOrEqualTo(maxX * 100f), plot.name + " failure left");
            }

            // Area C, bug 1: the captions are the short forms that fit a 55 pt plot at 14 pt
            // ("Dynamic pressure" rendered as "Dynamic p", "Delta-v losses" as "Delta-v los").
            Assert.That(card.Q("plot-alt").Q<Label>("plot-label").text, Is.EqualTo("Altitude"));
            Assert.That(card.Q("plot-vel").Q<Label>("plot-label").text, Is.EqualTo("Speed"));
            Assert.That(card.Q("plot-q").Q<Label>("plot-label").text, Is.EqualTo("Dyn. q"));
            Assert.That(card.Q("plot-g").Q<Label>("plot-label").text, Is.EqualTo("g load"));
            Assert.That(card.Q("plot-losses").Q<Label>("plot-label").text, Is.EqualTo("Δv loss"));
            // The full names stay in the tooltips.
            Assert.That(card.Q("plot-q").tooltip, Does.StartWith("Dynamic pressure"));
            Assert.That(card.Q("plot-losses").tooltip, Does.StartWith("Delta-v losses"));
        }

        [Test]
        public void SkipSamplesLandInTheLogAtTheFrameCadence()
        {
            // Area C, bug 3 (host side): skip-time samples land in the same log at the frame sampler's
            // 0.1 s cadence, with the same kinematic g-load — no fabricated fills.
            var go = new UnityEngine.GameObject("skip-host");
            var host = go.AddComponent<Kestrel.Game.Failure.FailureHost>();
            try
            {
                host.SampleSkip(new AscentSample { T = 0, AltM = 0, SpeedMps = 0, QPa = 0, MassKg = 10000 });
                host.SampleSkip(new AscentSample { T = 0.05, AltM = 10, SpeedMps = 50, QPa = 100, MassKg = 9900 });
                host.SampleSkip(new AscentSample { T = 0.1, AltM = 40, SpeedMps = 100, QPa = 400, MassKg = 9800 });
                host.SampleSkip(new AscentSample { T = 0.2, AltM = 160, SpeedMps = 200, QPa = 900, MassKg = 9600 });
                host.SampleSkip(new AscentSample { T = 0.3, AltM = 360, SpeedMps = 300, QPa = 1600, MassKg = 9400 });
                var log = host.Log;
                Assert.That(log.Count, Is.EqualTo(4), "the 0.05 sample is gated out by the 0.1 s gate");
                Assert.That(log[0].T, Is.EqualTo(0).Within(1e-9));
                Assert.That(log[1].T, Is.EqualTo(0.1).Within(1e-9));
                Assert.That(log[2].T, Is.EqualTo(0.2).Within(1e-9));
                Assert.That(log[3].T, Is.EqualTo(0.3).Within(1e-9));
                Assert.That(log[3].Alt, Is.EqualTo(360).Within(1e-9));
                Assert.That(log[3].V, Is.EqualTo(300).Within(1e-9));
                Assert.That(log[3].Q, Is.EqualTo(1600).Within(1e-9));
                Assert.That(log[3].Mass, Is.EqualTo(9400).Within(1e-9));
                Assert.That(log[3].G, Is.EqualTo((300 - 200) / 0.1 / 9.80665).Within(1e-6), "kinematic g over the 0.1 s gap");
                Assert.That(log[3].LossTotal, Is.EqualTo(0), "losses stay the host's own until the result lands");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void DebriefCardShowsTheCauseNumbersStarsAndExits()
        {
            var hit = new FailureHit { on = true, kind = FailureKind.StructuralMaxQ, cause = 0, value = 41000, limit = 33000, engine = -1, stage = 1 };
            var ev = FailureModel.Explain(hit, 62, 41000);
            var facts = new FlightFacts { Failure = ev, Passed = false, Seed = 2 };
            var debrief = FlightDebrief.Compose(null, MissionContext.Free(), facts, NarrationPool.Empty, 100000, null);
            Assert.AreEqual("Structural breakup", debrief.CauseTitle);
            Assert.That(debrief.Numbers, Does.Contain("41 kPa"));
            Assert.That(debrief.Numbers, Does.Contain("33 kPa"));
            var log = new TelemetryLog();
            log.Push(0, 0, 0, 0, 1, 1000, 0, 0, 0);
            var frame = ReportLayout.Solve(375, 667, 20, 0, 0, 0, true);
            var card = ReportView.Build(ev, log, frame, true, null, debrief, null);
            Assert.That(card.Q<Label>("failure-cause").text, Is.EqualTo("Structural breakup"));
            Assert.That(card.Q<Label>("failure-numbers").text, Does.Contain("41 kPa"));
            Assert.That(card.Q<Label>("failure-kid").text, Is.EqualTo(ev.kid));
            Assert.That(card.Q<Label>("failure-engineer").text, Is.EqualTo(ev.engineer));
            Assert.That(card.Q<Label>("report-stars").text.ToLowerInvariant(), Does.Contain("star"));
            Assert.That(card.Q<Button>("report-retry").text, Is.EqualTo("One more launch"));
            Assert.That(card.Q<Button>("report-retry").tooltip, Does.Contain("same seed"));
            Assert.IsNotNull(card.Q<Button>("report-next"));
            Assert.IsNotNull(card.Q<Button>("report-missions"));
            Assert.IsNotNull(card.Q<Button>("report-home"));
            Assert.That(card.Q<Button>("failure-fix").style.minHeight.value.value, Is.GreaterThanOrEqualTo(44f));
            Assert.IsTrue(card.ClassListContains("portrait"));
            Assert.That(card.style.backgroundColor.value.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(card.Q("report-plate"), Is.Not.Null);
            Assert.That(card.Q("report-scroll").style.backgroundColor.value.a, Is.EqualTo(1f).Within(0.001f));
        }

        /// <summary>#416 item 3: a pass opens on MISSION COMPLETE, stars and "+2.0M Kc", leads with a green Next mission,
        /// and has no Fix it. Every other exit stays, as a secondary button.</summary>
        [Test]
        public void PassCardCelebratesAndLeadsWithNextMission()
        {
            var card = ReportView.Build(null, new TelemetryLog(), ReportLayout.Solve(390, 844, 20, 0, 0, 0, true), true, null, PassDebrief(), new ReportActions());
            Assert.IsTrue(card.ClassListContains(UiIds.ReportPassClass));
            Assert.That(card.Q<Label>(UiIds.ReportHero).text, Is.EqualTo("MISSION COMPLETE"));
            Assert.That(card.Q<Label>(UiIds.ReportKcHeadline).text, Is.EqualTo("+2.0M Kc"));
            var stars = card.Q(UiIds.ReportStarRow);
            Assert.That(stars, Is.Not.Null);
            Assert.That(stars.childCount, Is.EqualTo(3));
            Assert.That(stars.tooltip, Is.EqualTo("2 of 3 stars"));
            Assert.That(card.Q<Label>("report-kc").text, Does.Contain("Reward 2.0M Kc").And.Contain("Bank 4.0M Kc").And.Not.Contain("2000000"));
            Assert.That(card.Q<Button>(UiIds.ReportFix), Is.Null, "no Fix it on a pass");
            var foot = card.Q(UiIds.ReportFoot);
            var first = foot.Q<Button>();
            Assert.That(first.name, Is.EqualTo(UiIds.ReportNext), "Next mission leads");
            Assert.That(ReportCardChrome.PassRole(UiIds.ReportNext), Is.EqualTo("primary"));
            Color go = first.style.backgroundColor.value;
            Assert.That(go.g, Is.GreaterThan(go.r + 0.2f), "green primary");
            foreach (var id in new[] { UiIds.ReportRetry, UiIds.ReportDesigner, UiIds.ReportMissions, UiIds.ReportHome })
            {
                var b = card.Q<Button>(id);
                Assert.That(b, Is.Not.Null, id);
                Assert.That(ReportCardChrome.PassRole(id), Is.EqualTo("home"), id + " secondary");
                Assert.That(b.style.backgroundColor.value, Is.Not.EqualTo(go), id);
            }
            ReportCardChrome.Dress(card, 1f, 20f, 0f);
            Assert.That(card.Q<Button>(UiIds.ReportNext).style.backgroundColor.value, Is.EqualTo(ReportCardChrome.Go), "Dress keeps the green primary");
            Assert.That(card.Q<Label>(UiIds.ReportHero).style.color.value, Is.EqualTo(ReportCardChrome.Win));
        }

        [Test]
        public void FailCardStillLeadsWithFixIt()
        {
            var debrief = PassDebrief();
            debrief.Passed = false;
            debrief.KcHeadline = "";
            var card = ReportView.Build(null, new TelemetryLog(), ReportLayout.Solve(390, 844, 20, 0, 0, 0, true), true, null, debrief, new ReportActions());
            Assert.IsFalse(card.ClassListContains(UiIds.ReportPassClass));
            Assert.That(card.Q(UiIds.ReportHero), Is.Null);
            Assert.That(card.Q(UiIds.ReportStarRow), Is.Null);
            Assert.That(card.Q(UiIds.ReportFoot).Q<Button>().name, Is.EqualTo(UiIds.ReportFix));
            var withFailure = ReportView.Build(new FailureEvent { kind = "overg", kid = "Too many g.", engineer = "8 g." }, new TelemetryLog(),
                ReportLayout.Solve(390, 844, 20, 0, 0, 0, true), true, null, PassDebrief(), new ReportActions());
            Assert.That(withFailure.Q<Button>(UiIds.ReportFix), Is.Not.Null, "a failure event always keeps Fix it");
        }

        internal static FlightDebrief PassDebrief()
        {
            return new FlightDebrief
            {
                Passed = true,
                Kind = "pass",
                CauseTitle = "She flew",
                Numbers = "Apogee 10 km. Goal 10 km.",
                Stars = 2,
                StarsLine = "2 stars.",
                MedalsLine = "Bronze for timing.",
                KcLine = "Reward 2.0M Kc. Hardware 4.8K Kc. Bank 4.0M Kc.",
                KcHeadline = "+2.0M Kc",
            };
        }

        static void Expect(string kind, string tab, string field)
        {
            var link = FixField.For(kind);
            Assert.That(link.Tab, Is.EqualTo(tab), kind);
            Assert.That(link.Field, Is.EqualTo(field), kind);
            Assert.That(link.Uri, Is.EqualTo("kestrel://design?tab=" + tab + "&field=" + field));
            Assert.That(link.VoiceOver, Does.StartWith("Fix it."));
            Assert.That(ReportView.Shapes(kind), Is.GreaterThanOrEqualTo(2), kind);
        }

        /// <summary>QA #417: a destroyed listener threw first and the live one never ran (Fix it -> empty sky).</summary>
        [Test]
        public void FixItReachesEveryLiveListener_EvenWhenOneThrowsOrIsDestroyed()
        {
            var dead = new UnityEngine.GameObject("dead").AddComponent<FixListener>();
            var live = new UnityEngine.GameObject("live").AddComponent<FixListener>();
            System.Action<FixField> thrower = _ => throw new System.InvalidOperationException("dead app");
            FixField.OnApply += dead.Hear;
            FixField.OnApply += thrower;
            FixField.OnApply += live.Hear;
            try
            {
                UnityEngine.Object.DestroyImmediate(dead.gameObject);
                UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Exception, new System.Text.RegularExpressions.Regex("dead app"));
                FixField.Apply(new VisualElement(), FixField.For("engine-out"));
                Assert.That(live.Heard, Is.EqualTo(1), "the live listener still routes to the designer");
            }
            finally
            {
                FixField.OnApply -= thrower;
                FixField.OnApply -= live.Hear;
                UnityEngine.Object.DestroyImmediate(live.gameObject);
            }
        }

        sealed class FixListener : UnityEngine.MonoBehaviour
        {
            public int Heard;
            public void Hear(FixField link) { var p = transform.position; if (link != null && p.x < float.PositiveInfinity) Heard++; }
        }
    }
}
