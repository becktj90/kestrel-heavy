// Real Home captures for Trevor, one PNG per device size and Home mode, from the shipping Boot scene (AppShell, the
// shell PanelSettings, ScreenStub's Home) after 60 frames, rendered like ScreenCaptureTests: every camera and every UI
// Toolkit panel into one RenderTexture during normal frames (WaitForEndOfFrame never fires in batchmode).
// Each shot also runs Kestrel.UiChecks.UiTreeChecks on the live panel (text bounds, overlaps, screen and safe area).
// The PNG is written first, so a failing size still uploads its picture.
// Output: $KH_CAP_OUT/home or <project>/CaptureOut/home/home-<device>-<mode>.png (the home-captures artifact).
// Web sizes take the landscape web panel (WebPanelScale.ForceWide) and the wide Home frame (MainMenuOverlay.ForceWide);
// the editor has no page EXIT button, so the gear keeps the corner it has on iOS.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kestrel.Core.Features;
using Kestrel.Core.Ui;
using Kestrel.Game.Shell;
using Kestrel.Kit;
using Kestrel.UiChecks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Kestrel.Capture.Tests
{
    public class HomeCaptureTests
    {
        const int Frames = 60;

        struct Device { public string Name; public int W, H; public bool Web; }

        // Pixel sizes: iPhone SE (375x667 @2x), iPhone 15 (390x844 @3x), 15 Pro Max (430x932 @3x), iPad 10th gen portrait
        // (820x1180 @2x), and the landscape web viewports from the brief plus Arcade's 1280x800 play-through.
        static readonly Device[] Devices =
        {
            new Device { Name = "se-375x667", W = 750, H = 1334 },
            new Device { Name = "iphone-390x844", W = 1170, H = 2532 },
            new Device { Name = "promax-430x932", W = 1290, H = 2796 },
            new Device { Name = "ipad-820x1180", W = 1640, H = 2360 },
            new Device { Name = "web-1280x720", W = 1280, H = 720, Web = true },
            new Device { Name = "web-1280x800", W = 1280, H = 800, Web = true },
            new Device { Name = "web-1366x768", W = 1366, H = 768, Web = true },
            new Device { Name = "web-1920x1080", W = 1920, H = 1080, Web = true },
        };

        static readonly (string Name, HomeShow Show, bool Save)[] Modes =
        {
            ("starter-continue", HomeShow.None, true), ("starter-launch", HomeShow.None, false), ("full-continue", HomeShow.Full, true),
        };

        FeatureFlags savedFlags;
        bool savedLoaded, savedIntro;
#if UNITY_EDITOR
        uint priorWidth, priorHeight;
#endif

        [UnitySetUp]
        public IEnumerator Remember()
        {
            savedFlags = FeatureFlags.Active;
            savedLoaded = HomeFlagsBoot.Loaded;
            savedIntro = MainMenuOverlay.IntroPlayed;
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
            MainMenuOverlay.IntroPlayed = savedIntro;
            MainMenuOverlay.ForceWide = null;
            WebPanelScale.ForceWide = null;
            var ps = Resources.Load<PanelSettings>("UI/KH_ShellPanelSettings");
            if (ps != null) WebPanelScale.Apply(ps, false);
#if UNITY_EDITOR
            if (priorWidth > 0 && priorHeight > 0)
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(priorWidth, priorHeight, "Previous capture viewport");
#endif
            // Leave no shell behind. A surviving AppShell (DontDestroyOnLoad) makes the next test's Boot load destroy its own
            // fresh shell as a duplicate, while the old shell's Home view is torn down with the old scene (ScreenStub.OnDisable):
            // the next test then sees SceneFlow at Home over an empty root (MenuPlayWalkTests NRE on home-codex, GameCI 39813959626).
            if (AppShell.Instance != null) UnityEngine.Object.Destroy(AppShell.Instance.gameObject);
            yield return null;
        }

        static string OutDir()
        {
            var d = Environment.GetEnvironmentVariable("KH_CAP_OUT");
            if (string.IsNullOrEmpty(d)) d = Path.Combine(Path.GetDirectoryName(Application.dataPath), "CaptureOut");
            d = Path.Combine(d, "home");
            Directory.CreateDirectory(d);
            return d;
        }

        [UnityTest]
        [Timeout(1800000)]
        public IEnumerator HomeAtEverySizeAndMode()
        {
#if !UNITY_EDITOR
            Assert.Ignore("Device captures need the editor play-mode viewport.");
            yield break;
#else
            // No GPU (-nographics): nothing to render. Skip with a warning, never an error log (it fails every later test).
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Debug.LogWarning("[Capture] no graphics device; Home captures skipped");
                Assert.Ignore("No graphics device for Home captures.");
            }
            var problems = new List<string>();
            foreach (var d in Devices)
                foreach (var m in Modes)
                {
                    var shot = Shot(d, m.Name, m.Show, m.Save, problems);
                    while (shot.MoveNext()) yield return shot.Current;
                }
            Assert.That(problems, Is.Empty, UiTreeChecks.Report(problems, 120));
#endif
        }

        static IEnumerator Shot(Device d, string mode, HomeShow show, bool save, List<string> problems)
        {
            string name = "home-" + d.Name + "-" + mode;
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution((uint)d.W, (uint)d.H, "Kestrel " + name);
            for (int i = 0; i < Frames && (Screen.width != d.W || Screen.height != d.H); i++) yield return null;
            Assert.AreEqual(d.W, Screen.width, name + " width");
            Assert.AreEqual(d.H, Screen.height, name + " height");
#endif
            HomeFlagsBoot.Loaded = true;   // keep this mode's flags; Home must not reload data/features.json
            FeatureFlags.Active = HomeFeatures.With(show);
            MainMenuOverlay.ForceWide = d.Web ? true : (bool?)null;
            WebPanelScale.ForceWide = d.Web;
            MainMenuOverlay.IntroPlayed = true;   // the settled Home, not a frame of the staggered intro

            var shell = AppShell.Instance;
            if (shell != null)
            {
                UnityEngine.Object.Destroy(shell.gameObject);
                yield return null;
            }
            CaptureCamera.Clear();
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            for (int i = 0; i < Frames; i++) yield return null;

            shell = AppShell.Instance;
            Assert.IsNotNull(shell, name + " shell");
            var root = shell.Root;
            var home = root.Q(UiIds.ScreenHome);
            Assert.IsNotNull(home, name + " " + UiIds.ScreenHome);
            // Save present or not, whatever this runner's disk holds: the same call ScreenStub makes after reading the save.
            HomeResumeBar.Mount(home, 72, save, () => { });   // the same root ScreenStub mounts on
            for (int i = 0; i < 8; i++) yield return null;

            // Render first, so a failing size still uploads its picture.
            var rt = new RenderTexture(d.W, d.H, 24, RenderTextureFormat.ARGB32);
            var cams = Camera.allCameras.Where(c => c.enabled && c.targetTexture == null).OrderBy(c => c.depth).ToArray();
            foreach (var c in cams) c.targetTexture = rt;
            var panels = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None)
                .Select(x => x.panelSettings).Where(p => p != null).Distinct().ToArray();
            var oldTargets = panels.Select(p => p.targetTexture).ToArray();
            var oldClear = panels.Select(p => p.clearColor).ToArray();
            foreach (var p in panels) { p.targetTexture = rt; p.clearColor = false; }
            for (int i = 0; i < 3; i++) yield return null;
            foreach (var c in cams) c.targetTexture = null;
            for (int i = 0; i < panels.Length; i++) { panels[i].targetTexture = oldTargets[i]; panels[i].clearColor = oldClear[i]; }
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(d.W, d.H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, d.W, d.H), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            rt.Release();
            var path = Path.Combine(OutDir(), name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            int distinct = tex.GetPixels32().Where((_, i) => i % 97 == 0).Select(p => (p.r << 16) | (p.g << 8) | p.b).Distinct().Count();
            Debug.Log("[Capture] " + name + ": " + path + " " + d.W + "x" + d.H + " distinctColours=" + distinct);
            UnityEngine.Object.Destroy(tex);
            UnityEngine.Object.Destroy(rt);
            for (int i = 0; i < 2; i++) yield return null;
            if (distinct <= 8) problems.Add(name + ": capture is flat colour (" + distinct + " colours)");

            // The live panel, as drawn: every label inside its box, nothing on anything else, nothing off screen or out of the safe area.
            var safe = shell.Safe;
            problems.AddRange(UiTreeChecks.Problems(root, root.worldBound, safe != null ? safe.worldBound : root.worldBound, name,
                e => e.name == UiIds.HomeSettings && d.Web));
            // One primary CTA, and it reads CONTINUE exactly when a save is present.
            int primaries = 0;
            home.Query(className: "kh-mm-launch").ForEach(_ => primaries++);
            if (primaries != 1) problems.Add(name + ": " + primaries + " primary CTAs");
            if (MainMenuOverlay.CtaIsContinue(home) != save) problems.Add(name + ": CONTINUE " + (save ? "missing" : "shown without a save"));
            if (root.Q(HomeResumeBar.ContinueName) != null) problems.Add(name + ": a second CONTINUE outside the CTA (F9)");
        }
    }
}
