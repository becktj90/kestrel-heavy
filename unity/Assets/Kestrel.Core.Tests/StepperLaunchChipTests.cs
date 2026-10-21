// F3 (NAV_BUGS_LIVE): the "Launch" step chip in the stepper did nothing. On the Review step the chip that reads
// "Launch" is the current step, and PaintSegmentButton disabled every current chip; its click only re-opened the
// step it was already on. Now the current Review chip launches through the same Forward action as the Launch
// button (confirm on red risks, the designer's launch gate behind the bridge).
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StepperLaunchChipTests
    {
        [SetUp]
        public void Fresh() => ReferenceAscent.Reset();

        static StepperProgress At(DesignerStep step, params DesignerStep[] visited)
        {
            var p = StepperProgress.FreshGuided();
            p.Current = step;
            p.Visited.Add(step);
            foreach (var v in visited) p.Visited.Add(v);
            return p;
        }

        StepperReport Workhorse() => StepperModel.Evaluate(Golden.Catalog, HangarFixtures.Template("workhorse"));

        [Test]
        public void OnReviewTheLaunchChipLaunches()
        {
            var report = Workhorse();
            Assert.IsTrue(report.CanLaunch);
            var review = At(DesignerStep.Review);
            Assert.AreEqual(StepMark.Current, StepperModel.Mark(review, report, DesignerStep.Review));
            Assert.AreEqual(SegmentTap.Launch, StepperModel.Tap(review, report, DesignerStep.Review));
            Assert.IsTrue(StepperModel.Forward(review, report).Launches, "the chip and the Launch button share Forward");
        }

        [Test]
        public void WithARedRiskTheLaunchChipAsksFirstLikeTheButton()
        {
            var report = Workhorse();
            report.HasRedRisk = true;
            report.NeedsLaunchConfirm = StepperLaunch.NeedsConfirm(true, false);
            report.CanLaunch = StepperLaunch.CanLaunch(true, false);
            var review = At(DesignerStep.Review);
            var forward = StepperModel.Forward(review, report);
            Assert.IsTrue(forward.NeedsConfirm);
            Assert.IsFalse(forward.Launches);
            Assert.AreEqual(SegmentTap.Launch, StepperModel.Tap(review, report, DesignerStep.Review), "the chip arms the same confirm");
        }

        [Test]
        public void OtherChipsOpenTheirStepOrDoNothing()
        {
            var report = Workhorse();
            var tanks = At(DesignerStep.Tanks, DesignerStep.Mission, DesignerStep.Engines, DesignerStep.Review);
            Assert.AreEqual(SegmentTap.None, StepperModel.Tap(tanks, report, DesignerStep.Tanks), "the current step");
            Assert.AreEqual(SegmentTap.Open, StepperModel.Tap(tanks, report, DesignerStep.Mission));
            Assert.AreEqual(SegmentTap.Open, StepperModel.Tap(tanks, report, DesignerStep.Review), "a visited Launch chip opens Review");
            var fresh = StepperProgress.FreshGuided();
            Assert.AreEqual(SegmentTap.None, StepperModel.Tap(fresh, report, DesignerStep.Review), "Review stays locked until reached");
            var all = At(DesignerStep.Review);
            all.ShowAllParts();
            Assert.AreEqual(SegmentTap.None, StepperModel.Tap(all, report, DesignerStep.Review), "all parts has no guided chips");
            Assert.AreEqual(SegmentTap.None, StepperModel.Tap(null, report, DesignerStep.Review));
        }

        /// <summary>Fails on main: the shell disabled the current chip and wired every chip to PressJump.</summary>
        [Test]
        public void TheShellWiresTheChipToTap()
        {
            string shell = File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Slice", "Runtime", "DesignerV3", "Stepper", "StepperShell.cs"));
            Assert.That(shell, Does.Not.Contain("button.SetEnabled(!locked && mark != StepMark.Current)"), "the current Launch chip is not disabled");
            StringAssert.Contains("new Button(() => PressSegment(step))", shell);
            StringAssert.Contains("button.SetEnabled(!locked && tap != SegmentTap.None)", shell);
            StringAssert.Contains("case SegmentTap.Launch: return PressForward();", shell);
        }
    }
}
