// DECISIONS #79: the player-flown landing burn's HUD, shot from the shipping flow at SE, Pro Max and iPad. Boot -> Home
// LAUNCH for the KH-7 starter (a planned landing on Haven) -> count -> ascent -> separation; the autopilot flies boostback
// and entry, and at the hand-over the landing layer takes the band between the top strip and the tools row. Three
// moments are held (RecoveryShow.HoldForCapture) and shot at every size: the hand-over (YOUR BURN, LIGHT IN ...), the
// burn low over the deck, and the grade splash. Each shot runs UiTreeChecks (labels fit, nothing overlaps, nothing
// leaves the screen or the safe area), TextFit.Problems on the layer, and the 44 pt touch-target check on the lean pads
// and the throttle slider. The burn is flown by a scripted pilot through RecoveryShow.PilotForCapture (the same
// ArcadeInput the slider and pads produce); RecoveryShow.PlayerBurnForCapture stands in for Auto-assist off.
// Output: $KH_CAP_OUT/landing-hud or <project>/CaptureOut/landing-hud/landing-<moment>-<device>.png.
using System;
using System.Collections;
using System.Collections.Generic;
using Kestrel.Core.Flight;
using Kestrel.Game.Shell;
using Kestrel.Slice;
using Kestrel.UiChecks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Kestrel.Capture.Tests
{
    // CI runs this fixture in its own job (unity.yml PlayModeDevice, -testCategory DeviceCapture).
    [Category("DeviceCapture")]
    public class LandingHudCaptureTests
    {
        const int KhSevenCleared = 6;      // Home LAUNCH as if KH-1..KH-6 were cleared: the KH-7 starter, which lands on Haven
        const int MaxFrames = 12000;       // 1200 s of sim at the 0.1 s capture step
        const int StallFrames = 600;
        const float MinTouchPt = 44f;

        static readonly FlightCapture.Device[] Sizes =
        {
            FlightCapture.Phones[0],                                                   // iPhone SE 375x667
            FlightCapture.Phones[2],                                                   // iPhone 15 Pro Max 430x932
            new FlightCapture.Device { Name = "ipad-1024x1366", W = 2048, H = 2732 },  // iPad Pro 12.9"
        };

#if UNITY_EDITOR
        uint priorWidth, priorHeight;
#endif

        [UnitySetUp]
        public IEnumerator Remember()
        {
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.GetRenderingResolution(out priorWidth, out priorHeight);
#endif
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            RecoveryShow.HoldForCapture = false;
            RecoveryShow.PlayerBurnForCapture = false;
            RecoveryShow.PilotForCapture = null;
            SliceApp.HoldCountForCapture = false;
            Time.captureDeltaTime = 0f;
            if (AppShell.Instance != null) UnityEngine.Object.Destroy(AppShell.Instance.gameObject);
#if UNITY_EDITOR
            if (priorWidth > 0 && priorHeight > 0)
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(priorWidth, priorHeight, "Previous capture viewport");
#endif
            yield return null;
        }

        /// <summary>The good pilot of ArcadeLandingTests, reading only the HUD readout.</summary>
        static ArcadeInput Pilot(ArcadeLandingSession s)
        {
            var r = s.Readout;
            var input = s.Input;
            const double g = 9.81;
            if (!(r.Throttle > 0.005) && r.Cue != LightCue.LightNow && r.Cue != LightCue.Late) return ArcadeInput.Idle;
            double aMax = Math.Max(1, (r.TwMax - 1) * g);
            bool high = r.AltM > 20;
            double vT = high ? Math.Sqrt(2 * 0.75 * aMax * (r.AltM - 12)) + 1.0 : 0.8 + (r.AltM / 20) * 1.5;
            double tw = 1 + ((high ? 0.75 * aMax : 0) + 1.5 * (r.SinkMps - vT)) / g;
            input.Throttle = s.SliderForTw(Math.Max(0.1, Math.Min(r.TwMax, tw)));
            double tGo = r.SinkMps > 2 ? Math.Max(3, r.AltM / r.SinkMps) : 6;
            double cap = r.AltM > 150 ? 12 : r.AltM > 40 ? 4 : 1.5;
            double want = Math.Max(-cap, Math.Min(cap, -r.OffsetM / tGo));
            double lim = r.AltM > 60 ? 1 : r.AltM > 15 ? 0.35 : 0.15;
            input.Steer = Math.Max(-lim, Math.Min(lim, (want - r.DriftMps) / 4.0));
            return input;
        }

        [UnityTest]
        [Timeout(3600000)]
        public IEnumerator LandingBurnHud_FitsAtSeProMaxAndIpad()
        {
#if !UNITY_EDITOR
            Assert.Ignore("Device captures need the editor play-mode viewport.");
            yield break;
#else
            FlightCapture.RequireGraphics("landing HUD captures");
            var problems = new List<string>();
            var dir = FlightCapture.OutDir("landing-hud");
            RecoveryShow.PlayerBurnForCapture = true;
            RecoveryShow.PilotForCapture = Pilot;
            SliceApp.HoldCountForCapture = false;
            Time.captureDeltaTime = 0.1f;
            var first = Sizes[0];
            var vp = FlightCapture.Viewport(first, "landing-" + first.Name);
            while (vp.MoveNext()) yield return vp.Current;
            SliceApp app = null;
            var boot = FlightCapture.BootToCount(a => app = a, KhSevenCleared);
            while (boot.MoveNext()) yield return boot.Current;

            // Fly to the hand-over: separation, boostback, entry, then the player's burn.
            RecoveryShow rec = null;
            double last = 0; int still = 0;
            for (int f = 0; f < MaxFrames && still < StallFrames; f++)
            {
                yield return null;
                rec = app.RecoveryForCapture;
                if (rec != null && rec.PlayerBurn) break;
                double now = app.FlightClockForCapture + (rec != null && rec.StartedForCapture && rec.ArcadeForCapture != null ? rec.ArcadeForCapture.Landing.Body.T : 0);
                if (now > last) { last = now; still = 0; } else still++;
            }
            if (rec == null || !rec.PlayerBurn)
            {
                Assert.Fail("the player's landing burn never started (flight T+" + app.FlightClockForCapture.ToString("0.0") + ", started "
                    + (rec != null && rec.StartedForCapture) + ", phase " + (rec != null ? rec.PhaseForCapture.ToString() : "-") + ")");
                yield break;
            }
            var arcade = rec.ArcadeForCapture;
            TestContext.WriteLine("hand-over at " + arcade.HandoverAltM.ToString("0") + " m, cue " + arcade.Readout.Cue);
            var shot = ShootAll(app, rec, "handover", dir, problems);
            while (shot.MoveNext()) yield return shot.Current;

            // Low over the deck with the engine lit.
            for (int f = 0; f < MaxFrames && !arcade.Landing.Touched && !(arcade.Readout.AltM < 60 && arcade.Readout.Throttle > 0.005); f++) yield return null;
            if (!arcade.Landing.Touched)
            {
                TestContext.WriteLine("burn at " + arcade.Readout.AltM.ToString("0") + " m, sink " + arcade.Readout.SinkMps.ToString("0.0") + ", T/W " + arcade.Readout.Tw.ToString("0.00"));
                shot = ShootAll(app, rec, "burn", dir, problems);
                while (shot.MoveNext()) yield return shot.Current;
            }
            else problems.Add("burn: touched down before the low-burn shot");

            // The grade splash.
            for (int f = 0; f < MaxFrames && !arcade.Landing.Touched; f++) yield return null;
            for (int f = 0; f < 6; f++) yield return null;
            if (arcade.Landing.Touched && rec.PlayerBurn)
            {
                var v = arcade.Landing.Verdict;
                TestContext.WriteLine("touchdown " + (v.Cause ?? "clean") + " " + v.GradeName + " sink " + v.Sink.ToString("0.00") + " miss " + v.MissM.ToString("0.0")
                    + ", " + (arcade.Score != null ? arcade.Score.Points.ToString() : "-") + " points");
                if (!MissionScore.IsGood(v.GradeName)) problems.Add("splash: the scripted pilot landed " + v.GradeName + " (" + v.Cause + ")");
                shot = ShootAll(app, rec, "splash", dir, problems);
                while (shot.MoveNext()) yield return shot.Current;
            }
            else problems.Add("splash: no touchdown with the layer up");
            Assert.That(problems, Is.Empty, UiTreeChecks.Report(problems, 120));
#endif
        }

#if UNITY_EDITOR
        static IEnumerator ShootAll(SliceApp app, RecoveryShow rec, string moment, string dir, List<string> problems)
        {
            RecoveryShow.HoldForCapture = true;
            foreach (var d in Sizes)
            {
                string name = "landing-" + moment + "-" + d.Name;
                var size = FlightCapture.Viewport(d, name);
                while (size.MoveNext()) yield return size.Current;
                for (int i = 0; i < 10; i++) yield return null;   // the panel and the layer re-lay out at the new size
                int distinct = 0;
                var png = FlightCapture.Png(d, dir, name, n => distinct = n);
                while (png.MoveNext()) yield return png.Current;
                if (distinct <= 8) problems.Add(name + ": capture is flat colour (" + distinct + " colours)");
                FlightCapture.HudChecks(app, name, problems);
                var layer = rec.LayerForCapture;
                if (layer == null || !UiTreeChecks.Visible(layer)) { problems.Add(name + ": the landing layer is not shown"); continue; }
                foreach (var p in TextFit.Problems(layer)) problems.Add(name + ": " + p);
                if (moment != "splash") TouchTargets(rec, name, problems);
            }
            var back = FlightCapture.Viewport(Sizes[0], "landing-" + Sizes[0].Name);
            while (back.MoveNext()) yield return back.Current;
            for (int i = 0; i < 4; i++) yield return null;
            RecoveryShow.HoldForCapture = false;
        }

        /// <summary>The lean pads and the throttle slider are shown and at least 44 pt each way.</summary>
        static void TouchTargets(RecoveryShow rec, string name, List<string> problems)
        {
            var app = UnityEngine.Object.FindAnyObjectByType<SliceApp>();
            var m = typeof(SliceApp).GetMethod("UnitsPerPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float upp = m != null && app != null ? (float)m.Invoke(app, null) : 1f;
            if (!(upp > 0f)) upp = 1f;
            foreach (var t in rec.TouchTargetsForCapture)
            {
                if (t == null || !UiTreeChecks.Visible(t)) { problems.Add(name + ": touch target " + (t != null ? t.name : "?") + " is not shown"); continue; }
                var b = t.worldBound;
                float w = b.width / upp, h = b.height / upp;
                if (w < MinTouchPt - 0.5f || h < MinTouchPt - 0.5f)
                    problems.Add(name + ": " + t.name + " is " + w.ToString("0") + "x" + h.ToString("0") + " pt, under " + MinTouchPt + " pt");
            }
        }
#endif
    }
}
