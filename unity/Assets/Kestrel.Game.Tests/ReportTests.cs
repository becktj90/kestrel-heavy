// Post-flight card (§3): kid line, engineer line, Fix it deep link, plots, portrait from SE to iPad.
using Kestrel.Core.Flight;
using Kestrel.Slice;
using NUnit.Framework;
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
            Assert.That(names, Is.EqualTo(new[] { "failure-icon", "failure-kid", "failure-engineer", "failure-plots", "failure-fix" }));
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
            root.Add(new VisualElement { name = "hud" });
            var engines = new Button { name = "engines" };
            root.Add(engines);
            var prop = new Button { name = "prop" };
            root.Add(prop);
            FixField.Apply(root, FixField.For("engine-out"));
            Assert.That(engines.ClassListContains("fix-target"), Is.True);
            Assert.That(root.Q("design").ClassListContains("hidden"), Is.False);
            Assert.That(root.Q("hud").ClassListContains("hidden"), Is.True);
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
            Assert.That(plots[0].FailureX, Is.EqualTo(0.5f).Within(1e-4));
            Assert.That(plots[0].X[0], Is.EqualTo(0f).Within(1e-4));
            Assert.That(plots[0].X[2], Is.EqualTo(1f).Within(1e-4));
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

        static void Expect(string kind, string tab, string field)
        {
            var link = FixField.For(kind);
            Assert.That(link.Tab, Is.EqualTo(tab), kind);
            Assert.That(link.Field, Is.EqualTo(field), kind);
            Assert.That(link.Uri, Is.EqualTo("kestrel://design?tab=" + tab + "&field=" + field));
            Assert.That(link.VoiceOver, Does.StartWith("Fix it."));
            Assert.That(ReportView.Shapes(kind), Is.GreaterThanOrEqualTo(2), kind);
        }
    }
}
