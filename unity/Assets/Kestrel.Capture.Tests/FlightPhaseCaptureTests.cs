// Real flight-phase captures for Trevor's t432 set: the shipping flow from the count through the ascent, one PNG at
// T+17, T+116 and T+192 (sim seconds) per phone size, Auto camera, no input. Each shot runs UiTreeChecks on the
// flight screen (the dock stack, the health strip, readouts, info panel and control row must not overlap; every label
// fits). The PNG is written first, so a failing shot still uploads its picture.
// The sim steps with the fixed capture frame (0.1 s, FlightDriver's own per-frame cap), so T+192 is about 2000 frames,
// flown once at the SE size; each phase holds the sim clock (FlightDriver.Hold, not the pause sheet) while all three sizes are shot.
// Output: $KH_CAP_OUT/flight-phase or <project>/CaptureOut/flight-phase/flight-<device>-t<sec>.png (flight-phase-captures).
using System.Collections;
using System.Collections.Generic;
using Kestrel.Slice;
using Kestrel.UiChecks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kestrel.Capture.Tests
{
    // CI runs this fixture in its own job (unity.yml PlayModeDevice, -testCategory DeviceCapture).
    [Category("DeviceCapture")]
    public class FlightPhaseCaptureTests
    {
        static readonly int[] Phases = { 17, 116, 192 };
        const int StallFrames = 600;   // 60 s of capture time with no sim progress: something holds the flight
        const int OrbitalCleared = 2;  // Home LAUNCH as if KH-1 and KH-2 were cleared: the KH-3 Tiny Orbit starter

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
            if (Kestrel.Game.Shell.AppShell.Instance != null) Object.Destroy(Kestrel.Game.Shell.AppShell.Instance.gameObject);
#if UNITY_EDITOR
            if (priorWidth > 0 && priorHeight > 0)
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(priorWidth, priorHeight, "Previous capture viewport");
#endif
            yield return null;
        }

        [UnityTest]
        [Timeout(3600000)]
        public IEnumerator AscentAtT17T116T192AtEveryPhoneSize()
        {
#if !UNITY_EDITOR
            Assert.Ignore("Device captures need the editor play-mode viewport.");
            yield break;
#else
            FlightCapture.RequireGraphics("flight-phase captures");
            var problems = new List<string>();
            var dir = FlightCapture.OutDir("flight-phase");
            SliceApp.HoldCountForCapture = false;
            Time.captureDeltaTime = 0.1f;
            // One flight, flown at the smallest phone; at each phase the sim clock holds (FlightDriver.Hold) while every phone
            // size is shot, so CI renders about 2000 small frames instead of three flights at Pro Max resolution.
            var first = FlightCapture.Phones[0];
            var vp = FlightCapture.Viewport(first, "flight-" + first.Name);
            while (vp.MoveNext()) yield return vp.Current;
            SliceApp app = null;
            // KH-3 Tiny Orbit: Home LAUNCH's starter for it burns to orbit (about T+517 headless), so T+192 is still
            // powered ascent. A fresh save would fly KH-1 First Hop, whose 10 km hop cuts off at T+25.7 and ends the flight.
            var boot = FlightCapture.BootToCount(a => app = a, OrbitalCleared);
            while (boot.MoveNext()) yield return boot.Current;
            foreach (int t in Phases)
            {
                double last = app.FlightClockForCapture; int still = 0;
                while (!(app.FlightClockForCapture >= t) && still < StallFrames)
                {
                    yield return null;
                    double now = app.FlightClockForCapture;
                    if (now > last) { last = now; still = 0; } else still++;
                }
                bool reached = app.FlightClockForCapture >= t;
                if (!reached) problems.Add("flight-t" + t.ToString("000") + ": the flight stopped at T+" + app.FlightClockForCapture.ToString("0.0"));
                var driver = Object.FindAnyObjectByType<FlightDriver>();
                if (driver != null) driver.Hold(true);
                foreach (var d in FlightCapture.Phones)
                {
                    string name = "flight-" + d.Name + "-t" + t.ToString("000");
                    var size = FlightCapture.Viewport(d, name);
                    while (size.MoveNext()) yield return size.Current;
                    for (int i = 0; i < 10; i++) yield return null;   // the panel re-lays out at the new size
                    int distinct = 0;
                    var png = FlightCapture.Png(d, dir, name, n => distinct = n);
                    while (png.MoveNext()) yield return png.Current;
                    if (distinct <= 8) problems.Add(name + ": capture is flat colour (" + distinct + " colours)");
                    FlightCapture.HudChecks(app, name, problems);
                }
                var back = FlightCapture.Viewport(first, "flight-" + first.Name);
                while (back.MoveNext()) yield return back.Current;
                if (driver != null) driver.Hold(false);
                if (!reached) break;
            }
            Assert.That(problems, Is.Empty, UiTreeChecks.Report(problems, 120));
#endif
        }
    }
}
