// Skip to next stage (DECISIONS #78): the HUD wiring, checked as source (the HUD itself is UI Toolkit, so
// dotnet cannot drive it — the same pattern as AscentHudRailWiringTests). The sim proof lives in
// StageSkipTests and StageSkipRecoveryTests.
using System.IO;
using Kestrel.Core.Ui;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StageSkipHudTests
    {
        static string Hud() => File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Slice", "Runtime", "Ascent", "AscentHud.cs"));
        static string Driver() => File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Slice", "Runtime", "FlightDriver.cs"));
        static string App() => File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Slice", "Runtime", "SliceApp.cs"));

        [Test]
        public void ButtonLivesInTheCruiseRow_NextToAutoAssist()
        {
            string src = Hud();
            StringAssert.Contains("UiIds.FlightStageSkip", src, "the button has a harness id");
            StringAssert.Contains("stageSkip.text = \"Skip to next stage\"", src, "the button is named for the decision");
            StringAssert.Contains("assistRow.Add(stageSkip)", src, "it sits in the cruise row, next to Auto-assist");
            StringAssert.Contains("assistRow.style.flexDirection = FlexDirection.Row;", src,
                "the row lays its two buttons side by side: LayoutRail pins it to the frame's single Assist seat, so a stacked (Column) row would overflow the seat onto the nudge row (EditMode DockStackNeverOverlaps, PR #451)");
            StringAssert.Contains("stageSkip.style.flexGrow = 2;", src,
                "the split is 1:2, not 1:1: the long label needs two thirds of the seat to stay on one line at 335 px");
            StringAssert.Contains("StyleButton(stageSkip)", src, "it is styled like the other dock buttons");
            StringAssert.Contains("stageSkip.style.display = DisplayStyle.None; // Preview has no flight to skip.", src,
                "device-test previews never offer a skip");
        }

        [Test]
        public void PresentShowsItOnlyOnTheCruiseRow_AndTogglesItWhileSkipping()
        {
            string src = Hud();
            StringAssert.Contains("bool show = !prompt && flight.Running && !flight.Lost && target != StageSkipTarget.None", src,
                "never over a prompt, never after a failure, never with no next event");
            StringAssert.Contains("&& (skipping || (!flight.Held && Kestrel.Kit.CinematicEvents.PresentationScale == 1));", src,
                "hidden in slow-mo or while held, but never hidden mid-skip so the tap-to-cancel stays");
            StringAssert.Contains("stageSkip.text = skipping ? \"Skipping…\" : \"Skip to next stage\";", src,
                "while skipping the button is the tap-to-cancel");
            StringAssert.Contains("flight.StartSkip();", src, "tap starts the skip");
            StringAssert.Contains("flight.CancelSkip();", src, "tap again cancels it");
        }

        [Test]
        public void DriverSkipsTheSameSim_InBoundedFrames()
        {
            string src = Driver();
            StringAssert.Contains("StageSkip.Advance(directed, checks, StageSkip.MaxStepsPerFrame)", src,
                "the pump steps the directed flight with its played checks");
            StringAssert.Contains("SkipAdvancedSimS = Sim.T - before;", src, "the skipped seconds are published for the booster");
            StringAssert.Contains("clock = Sim.T;", src, "a skipped frame banks no debt");
            StringAssert.Contains("if (on) CancelSkip();", src, "pausing cancels a skip");
        }

        [Test]
        public void SliceAppKeepsTheBoosterInLockstep_AndHaltsAtItsBurns()
        {
            string src = App();
            StringAssert.Contains("recovery.SkipAdvance(flight.SkipAdvancedSimS)", src, "the booster advances by the skipped seconds");
            StringAssert.Contains("flight.HaltSkip(reached)", src, "its next burn stops the ascent skip");
        }

        [Test]
        public void SkipSamplesTelemetryIntoTheDebriefLog()
        {
            // Area C, bug 3: the frame sampler starves while the sim fast-forwards, so the pump hands the
            // FailureHost's sampler one sample per 0.1 sim-seconds from the stepped state.
            string driver = Driver();
            StringAssert.Contains("public Action<AscentSample> SkipSampler { get; set; }", driver,
                "the driver holds the skip telemetry sink");
            StringAssert.Contains("StageSkip.Advance(directed, checks, StageSkip.MaxStepsPerFrame, SkipSampler)", driver,
                "the pump samples through the sink");
            string app = App();
            StringAssert.Contains("flight.SkipSampler = ", app, "SliceApp wires the sink when the rocket builds");
            StringAssert.Contains("host.SampleSkip", app, "the sink is the FailureHost's, so skip samples land in the debrief log");
        }

        [Test]
        public void StageSkipIdIsInTheFlightControls()
        {
            Assert.That(UiIds.FlightStageSkip, Is.EqualTo("flight-stage-skip"));
            CollectionAssert.Contains(UiIds.FlightControls, UiIds.FlightStageSkip);
        }
    }
}
