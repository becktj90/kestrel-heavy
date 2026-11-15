// DECISIONS #100 liftoff juice captures: the hold-down release gauge on the count card, held in the green, at iPhone SE,
// 15 Pro Max and iPad (10th gen portrait). One boot (KH-1, the fresh-save flight), one hold, three viewports: kept small on
// purpose, so the long CountdownCaptureTests stays under its 30 min limit. Every shot asserts on the live scene:
//   - UiTreeChecks on the flight screen (labels fit, no overlaps, nothing off screen or out of the safe area);
//   - TextFit on the clamp block (its status, T/W and the Release label fit their boxes);
//   - the Release button is at least 44 pt tall and wide, and the needle sits inside the gauge.
// The PNG is written before the checks, so a failing shot still uploads its picture.
// Output: $KH_CAP_OUT/liftoff or <project>/CaptureOut/liftoff/liftoff-<device>-<shot>.png.
using System.Collections;
using System.Collections.Generic;
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
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
    public class LiftoffCaptureTests
    {
        const int WaitFrames = 1200;   // 20 s at the fixed 1/60 step: the count opens at T-12 on a fresh save

        struct Shot { public FlightCapture.Device Device; public float Dpr; }

        static readonly Shot[] Shots =
        {
            new Shot { Device = new FlightCapture.Device { Name = "se-375x667", W = 750, H = 1334 }, Dpr = 2f },
            new Shot { Device = new FlightCapture.Device { Name = "promax-430x932", W = 1290, H = 2796 }, Dpr = 3f },
            new Shot { Device = new FlightCapture.Device { Name = "ipad-820x1180", W = 1640, H = 2360 }, Dpr = 2f },
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
            SliceApp.HoldCountForCapture = false;
            Time.captureDeltaTime = 0f;
            if (AppShell.Instance != null) Object.Destroy(AppShell.Instance.gameObject);
#if UNITY_EDITOR
            if (priorWidth > 0 && priorHeight > 0)
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(priorWidth, priorHeight, "Previous capture viewport");
#endif
            yield return null;
        }

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator ClampGaugeInTheGreen_AtSeProMaxAndIpad()
        {
#if !UNITY_EDITOR
            Assert.Ignore("Device captures need the editor play-mode viewport.");
            yield break;
#else
            FlightCapture.RequireGraphics("liftoff captures");
            var problems = new List<string>();
            var dir = FlightCapture.OutDir("liftoff");
            SliceApp.HoldCountForCapture = false;
            Time.captureDeltaTime = 1f / 60f;
            var first = Shots[0].Device;
            var vp = FlightCapture.Viewport(first, "liftoff-" + first.Name);
            while (vp.MoveNext()) yield return vp.Current;
            SliceApp app = null;
            var boot = FlightCapture.BootToCount(a => app = a);
            while (boot.MoveNext()) yield return boot.Current;
            Assert.IsNotNull(app.ClampForCapture, "a count arms the hold-down release");
            var clamp = app.ClampForCapture;
            // Into the green, then hold the count (and the clamp clock with it) for every viewport.
            for (int i = 0; i < WaitFrames && app.CountingForCapture && !(app.ClampClockForCapture >= clamp.GreenStart + 0.2); i++) yield return null;
            SliceApp.HoldCountForCapture = true;
            Assert.IsTrue(app.CountingForCapture, "still on the count");
            Assert.IsTrue(clamp.InGreen(app.ClampClockForCapture), "the needle is in the green at t=" + app.ClampClockForCapture.ToString("0.00"));
            foreach (var shot in Shots)
            {
                var d = shot.Device;
                string name = "liftoff-" + d.Name + "-clamp-green";
                var size = FlightCapture.Viewport(d, name);
                while (size.MoveNext()) yield return size.Current;
                for (int i = 0; i < 12; i++) yield return null;   // the panel and the card re-lay out at the new size
                int distinct = 0;
                var png = FlightCapture.Png(d, dir, name, n => distinct = n);
                while (png.MoveNext()) yield return png.Current;
                if (distinct <= 8) problems.Add(name + ": capture is flat colour (" + distinct + " colours)");
                FlightCapture.HudChecks(app, name, problems);
                ClampBlock(app, shot, name, problems);
            }
            Assert.That(problems, Is.Empty, UiTreeChecks.Report(problems, 120));
#endif
        }

        static void ClampBlock(SliceApp app, Shot shot, string name, List<string> problems)
        {
            var ui = app.UiForCapture;
            var block = ui?.Q(UiIds.FlightClamp);
            if (!FlightCapture.Shown(block)) { problems.Add(name + ": the clamp block is not shown"); return; }
            var button = block.Q<Button>(UiIds.FlightClampRelease);
            if (!FlightCapture.Shown(button)) { problems.Add(name + ": no Release button"); return; }
            // Panel units per point: the panel width over the screen width in points.
            float points = Screen.width / shot.Dpr;
            float upp = points > 1f ? ui.worldBound.width / points : 1f;
            var b = button.worldBound;
            if (b.height + 0.5f < FlightHudLayout.HitPt * upp) problems.Add(name + ": Release is " + (b.height / upp).ToString("0.0") + " pt tall (< 44)");
            if (b.width + 0.5f < FlightHudLayout.HitPt * upp) problems.Add(name + ": Release is " + (b.width / upp).ToString("0.0") + " pt wide (< 44)");
            if (!button.enabledSelf) problems.Add(name + ": Release is disabled in the green");
            var card = ui.Q("webcast");
            if (card != null && !FlightCapture.Inside(block.worldBound, card.worldBound))
                problems.Add(name + ": the clamp block " + block.worldBound + " leaves the count card " + card.worldBound);
            var gauge = block.Q(UiIds.FlightClampGauge);
            var needle = gauge?.Q(UiIds.FlightClampGauge + "-needle");
            if (gauge == null || needle == null) problems.Add(name + ": no gauge");
            else if (!FlightCapture.Inside(needle.worldBound, gauge.worldBound))
                problems.Add(name + ": the needle " + needle.worldBound + " leaves the gauge " + gauge.worldBound);
            var status = block.Q<Label>(UiIds.FlightClampStatus);
            if (status == null || string.IsNullOrEmpty(status.text)) problems.Add(name + ": no status line");
            foreach (var p in TextFit.Problems(block)) problems.Add(name + ": " + p);
        }
    }
}
