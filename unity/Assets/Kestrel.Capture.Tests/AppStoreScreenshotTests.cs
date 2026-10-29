// App Store screenshot frames, rendered by CI at the exact store sizes and uploaded as the "appstore-screenshots"
// artifact (workflow: .github/workflows/appstore-screenshots.yml, a separate non-required job).
//   iPhone 6.9"  1320x2868        iPhone 6.3"  1206x2622        iPad 13"  2064x2752
// Scenes: the Starter screen, the Designer, the held countdown, the ascent and the booster landing, from the shipping flow
// (Boot -> Home LAUNCH -> briefing -> Starter -> count -> ascent -> landing for the KH-7 starter, whose first stage lands on Haven).
// Reuses the existing harness: FlightCapture.Png renders every camera and UI Toolkit panel into a RenderTexture of exactly
// W x H (the same path HomeCaptureTests, CountdownCaptureTests and FlightPhaseCaptureTests use), after the play-mode viewport
// has been set to the same size (PlayModeWindow.SetCustomRenderingResolution) so the layout is the one a device draws.
// The only assertions are about the picture: the PNG is exactly the requested size, is not blank, and is not magenta.
// Output: $KH_CAP_OUT/appstore or <project>/CaptureOut/appstore/<scene>-<device>.png.
// Opt-in: the tests skip unless Unity was started with -khAppStoreShots, so the required PlayMode jobs only report them as skipped.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Features;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
using Kestrel.Game.Shell;
using Kestrel.Kit;
using Kestrel.Slice;
using Kestrel.UiChecks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Kestrel.Capture.Tests
{
    [Category("AppStoreShots")]
    public class AppStoreScreenshotTests
    {
        public const string Flag = "-khAppStoreShots";
        public const string Folder = "appstore";
        const int KhSevenCleared = 6;      // Home LAUNCH as if KH-1..KH-6 were cleared: the KH-7 starter, a planned booster landing on Haven
        const int StallFrames = 600;
        const int MaxFlightFrames = 9000;  // 900 s of sim at the 0.1 s capture step

        /// <summary>The three App Store screenshot sizes this job produces (pixels, portrait).</summary>
        public static readonly FlightCapture.Device[] StoreSizes =
        {
            new FlightCapture.Device { Name = "iphone-1320x2868", W = 1320, H = 2868 },
            new FlightCapture.Device { Name = "iphone-1206x2622", W = 1206, H = 2622 },
            new FlightCapture.Device { Name = "ipad-2064x2752", W = 2064, H = 2752 },
        };

        // Flown at the smallest phone size so the ~2000 sim frames are cheap on the CPU-rendered runner; each moment then holds
        // while it is shot at every store size.
        static readonly FlightCapture.Device Flown = FlightCapture.Phones[0];

#if UNITY_EDITOR
        uint priorWidth, priorHeight;
#endif

        FeatureFlags savedFlags;
        bool savedLoaded;

        static bool Wanted() => Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0;

        [UnitySetUp]
        public IEnumerator Remember()
        {
            savedFlags = FeatureFlags.Active;
            savedLoaded = HomeFlagsBoot.Loaded;
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.GetRenderingResolution(out priorWidth, out priorHeight);
#endif
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            FeatureFlags.Active = savedFlags;
            HomeFlagsBoot.Loaded = savedLoaded;
            SliceApp.HoldCountForCapture = false;
            RecoveryShow.HoldForCapture = false;
            Time.captureDeltaTime = 0f;
            if (AppShell.Instance != null) UnityEngine.Object.Destroy(AppShell.Instance.gameObject);
#if UNITY_EDITOR
            if (priorWidth > 0 && priorHeight > 0)
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(priorWidth, priorHeight, "Previous capture viewport");
#endif
            yield return null;
        }

        static string Dir => FlightCapture.OutDir(Folder);

#if UNITY_EDITOR
        /// <summary>Set the play-mode viewport to the device size. A viewport that will not take the size is reported, not asserted:
        /// the PNG is rendered into a W x H texture either way, so its size is exact.</summary>
        static IEnumerator Resize(FlightCapture.Device d, string name, List<string> problems)
        {
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution((uint)d.W, (uint)d.H, "Kestrel " + name);
            for (int i = 0; i < 60 && (Screen.width != d.W || Screen.height != d.H); i++) yield return null;
            if (Screen.width != d.W || Screen.height != d.H)
                problems.Add(name + ": the game view is " + Screen.width + "x" + Screen.height + ", wanted " + d.W + "x" + d.H);
            yield return null;
        }

        /// <summary>Render the scene as it stands into a W x H PNG and check the file.</summary>
        static IEnumerator Shoot(FlightCapture.Device d, string scene, List<string> problems)
        {
            string name = scene + "-" + d.Name;
            for (int i = 0; i < 10; i++) yield return null;   // the panel re-lays out at the new size
            int distinct = 0;
            var png = FlightCapture.Png(d, Dir, name, n => distinct = n);
            while (png.MoveNext()) yield return png.Current;
            Verify(Path.Combine(Dir, name + ".png"), d, name, problems);
        }

        static IEnumerator ShootAll(string scene, List<string> problems)
        {
            foreach (var d in StoreSizes)
            {
                var size = Resize(d, scene + "-" + d.Name, problems);
                while (size.MoveNext()) yield return size.Current;
                var shot = Shoot(d, scene, problems);
                while (shot.MoveNext()) yield return shot.Current;
            }
            var back = Resize(Flown, "flown", problems);
            while (back.MoveNext()) yield return back.Current;
        }
#endif

        /// <summary>The only gate: the PNG on disk is exactly W x H, is not (nearly) one colour, and is not magenta.</summary>
        public static void Verify(string path, FlightCapture.Device d, string name, List<string> problems)
        {
            if (!File.Exists(path)) { problems.Add(name + ": no PNG written"); return; }
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length < 33 || bytes[1] != (byte)'P' || bytes[2] != (byte)'N' || bytes[3] != (byte)'G') { problems.Add(name + ": not a PNG"); return; }
            int w = bytes[16] << 24 | bytes[17] << 16 | bytes[18] << 8 | bytes[19];
            int h = bytes[20] << 24 | bytes[21] << 16 | bytes[22] << 8 | bytes[23];
            if (w != d.W || h != d.H) problems.Add(name + ": PNG is " + w + "x" + h + ", wanted " + d.W + "x" + d.H);
            var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                if (!ImageConversion.LoadImage(tex, bytes, false)) { problems.Add(name + ": PNG does not decode"); return; }
                if (tex.width != d.W || tex.height != d.H) problems.Add(name + ": decoded size " + tex.width + "x" + tex.height);
                var px = tex.GetPixels32();
                var seen = new HashSet<int>();
                int magenta = 0, sampled = 0;
                for (int i = 0; i < px.Length; i += 7)
                {
                    sampled++;
                    var p = px[i];
                    if (seen.Count < 64) seen.Add(p.r << 16 | p.g << 8 | p.b);
                    if (p.r >= 200 && p.b >= 200 && p.g <= 60) magenta++;
                }
                float share = sampled > 0 ? magenta / (float)sampled : 0f;
                TestContext.WriteLine("appstore frame " + name + ": " + w + "x" + h + " colours>=" + seen.Count + " magenta " + (share * 100f).ToString("0.00") + "%");
                if (seen.Count <= 8) problems.Add(name + ": blank (" + seen.Count + " colours)");
                if (share > 0.01f) problems.Add(name + ": " + (share * 100f).ToString("0.0") + "% magenta pixels (a missing material or shader)");
            }
            finally { UnityEngine.Object.DestroyImmediate(tex); }
        }

        [UnityTest]
        [Timeout(1800000)]
        public IEnumerator DesignerAtEveryStoreSize()
        {
#if !UNITY_EDITOR
            Assert.Ignore("Store captures need the editor play-mode viewport.");
            yield break;
#else
            if (!Wanted()) { Assert.Ignore("App Store captures run only with " + Flag); yield break; }
            FlightCapture.RequireGraphics("App Store designer captures");
            var problems = new List<string>();
            PadHandoff.Clear();
            GameSession.Clear();
            int prevEngineer = PlayerPrefs.GetInt(Kestrel.Slice.DesignerV3.DesignerV3Panel.EngineerPref, 0);
            foreach (var d in StoreSizes)
            {
                string name = "designer-" + d.Name;
                var size = Resize(d, name, problems);
                while (size.MoveNext()) yield return size.Current;
                CaptureCamera.Clear();
                SceneManager.LoadScene("Design", LoadSceneMode.Single);
                for (int i = 0; i < 60; i++) yield return null;
                var app = UnityEngine.Object.FindAnyObjectByType<SliceApp>();
                if (app == null) { problems.Add(name + ": no SliceApp in the Design scene"); continue; }
                app.ShowDesigner(true);
                if (app.Designer == null) { problems.Add(name + ": the designer panel did not open"); continue; }
                app.Designer.SetEngineer(true);
                app.Designer.FinishReference();
                for (int i = 0; i < 5; i++) yield return null;
                var shot = Shoot(d, "designer", problems);
                while (shot.MoveNext()) yield return shot.Current;
                CaptureCamera.Clear();
            }
            PlayerPrefs.SetInt(Kestrel.Slice.DesignerV3.DesignerV3Panel.EngineerPref, prevEngineer);
            Assert.That(problems, Is.Empty, UiTreeChecks.Report(problems, 120));
#endif
        }

        [UnityTest]
        [Timeout(3600000)]
        public IEnumerator StarterCountdownFlightAndLandingAtEveryStoreSize()
        {
#if !UNITY_EDITOR
            Assert.Ignore("Store captures need the editor play-mode viewport.");
            yield break;
#else
            if (!Wanted()) { Assert.Ignore("App Store captures run only with " + Flag); yield break; }
            FlightCapture.RequireGraphics("App Store flight captures");
            var problems = new List<string>();
            // The App Store shows the app as shipped: FullHomeFlagsFixture turns every home.* flag on for this assembly, so load
            // data/features.json the way Boot does (tier Starter: the Starter screen and the campaign-winner design).
            HomeFlagsBoot.Loaded = false;
            FeatureFlags.Active = new FeatureFlags();
            HomeFlagsBoot.Load();
            if (!StarterScreen.UseStarter(FeatureFlags.Active)) problems.Add("starter: data/features.json did not load as the Starter tier");
            SliceApp.HoldCountForCapture = true;
            Time.captureDeltaTime = 1f / 60f;
            var vp = Resize(Flown, "flown", problems);
            while (vp.MoveNext()) yield return vp.Current;

            // Boot -> Home LAUNCH (KH-7's route) -> briefing -> build: the Starter screen is up on the pad.
            SliceApp app = null;
            var boot = BootToStarter(a => app = a, problems);
            while (boot.MoveNext()) yield return boot.Current;
            if (app == null) { Assert.That(problems, Is.Empty, UiTreeChecks.Report(problems, 120)); yield break; }
            for (int i = 0; i < 30; i++) yield return null;
            if (app.UiForCapture == null || !app.UiForCapture.ClassListContains("starter-mode")) problems.Add("starter: the Starter screen is not up (no starter-mode)");
            var starter = ShootAll("starter", problems);
            while (starter.MoveNext()) yield return starter.Current;

            // Launch: the held count.
            if (!app.FlightHudVisible) app.PressLaunch();
            for (int i = 0; i < 600 && !app.CountingForCapture; i++) yield return null;
            if (!app.CountingForCapture) problems.Add("countdown: the count never came up");
            for (int i = 0; i < 150; i++) yield return null;   // FlightViewPose smoothing settles in about 1.5 s
            var count = ShootAll("countdown", problems);
            while (count.MoveNext()) yield return count.Current;

            // The ascent, at T+60.
            SliceApp.HoldCountForCapture = false;
            Time.captureDeltaTime = 0.1f;
            var driver = UnityEngine.Object.FindAnyObjectByType<FlightDriver>();
            bool reached = false;
            double last = app.FlightClockForCapture; int still = 0;
            for (int f = 0; f < MaxFlightFrames && still < StallFrames; f++)
            {
                yield return null;
                double now = app.FlightClockForCapture;
                if (now >= 60) { reached = true; break; }
                if (now > last) { last = now; still = 0; } else still++;
            }
            if (!reached) problems.Add("flight: the flight stopped at T+" + app.FlightClockForCapture.ToString("0.0") + " before T+60 (driver running: " + (driver != null && driver.Running) + ", hud: " + app.FlightHudVisible + ")");
            if (app.FlightClockForCapture >= 5)   // still shot where it stands, so the artifact shows what the flight looked like
            {
                if (driver != null) driver.Hold(true);
                var fl = ShootAll("flight", problems);
                while (fl.MoveNext()) yield return fl.Current;
                if (driver != null) driver.Hold(false);
            }

            // The booster landing: the first stage lets go, boosts back, and comes down on its legs. Shot in the landing burn.
            bool landing = false;
            if (reached)
            {
                last = app.FlightClockForCapture; still = 0;
                for (int f = 0; f < MaxFlightFrames && still < StallFrames; f++)
                {
                    yield return null;
                    var rec = app.RecoveryForCapture;
                    if (rec != null && rec.StartedForCapture && rec.PhaseForCapture >= RecoveryPhase.Landing) { landing = true; break; }
                    if (driver != null && !driver.Running && (rec == null || !rec.StartedForCapture)) break;
                    double now = app.FlightClockForCapture;
                    if (now > last) { last = now; still = 0; } else still++;
                }
                if (landing)
                {
                    // A couple of seconds into the burn, unless it has already touched down.
                    for (int i = 0; i < 20 && app.RecoveryForCapture.PhaseForCapture == RecoveryPhase.Landing; i++) yield return null;
                    RecoveryShow.HoldForCapture = true;
                    if (driver != null) driver.Hold(true);
                    var ld = ShootAll("landing", problems);
                    while (ld.MoveNext()) yield return ld.Current;
                    if (driver != null) driver.Hold(false);
                    RecoveryShow.HoldForCapture = false;
                }
                else problems.Add("landing: the booster never reached its landing burn (flight clock T+" + app.FlightClockForCapture.ToString("0.0") + ")");
            }
            Assert.That(problems, Is.Empty, UiTreeChecks.Report(problems, 120));
#endif
        }

#if UNITY_EDITOR
        /// <summary>FlightCapture.BootToCount's first half: Boot -> Home LAUNCH for KH-7 -> briefing -> build, ending on the Starter screen.</summary>
        static IEnumerator BootToStarter(Action<SliceApp> got, List<string> problems)
        {
            if (AppShell.Instance != null) { UnityEngine.Object.Destroy(AppShell.Instance.gameObject); yield return null; }
            SceneManager.LoadScene(SceneFlow.BootScene, LoadSceneMode.Single);
            SceneFlow flow = null;
            for (int i = 0; i < 600 && (flow == null || flow.Busy || flow.Current != AppScreen.Home); i++)
            {
                if (flow == null) flow = UnityEngine.Object.FindAnyObjectByType<SceneFlow>();
                yield return null;
            }
            if (flow == null || flow.Current != AppScreen.Home) { problems.Add("starter: Boot never settled on Home"); yield break; }
            flow.Present(FlightCapture.LaunchAfter(KhSevenCleared));
            for (int i = 0; i < 600 && (flow.Busy || flow.Overlay != "Briefing"); i++) yield return null;
            if (flow.Overlay != "Briefing") { problems.Add("starter: LAUNCH did not open the briefing"); yield break; }
            flow.BuildFromBriefing();
            SliceApp app = null;
            for (int i = 0; i < 600 && (flow.Busy || flow.Current != AppScreen.Design || app == null); i++)
            {
                app = UnityEngine.Object.FindAnyObjectByType<SliceApp>();
                yield return null;
            }
            if (app == null) { problems.Add("starter: no SliceApp after the briefing"); yield break; }
            got(app);
        }
#endif
    }
}
