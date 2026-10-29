// Shared by CountdownCaptureTests and FlightPhaseCaptureTests: boot the shipping flow to the count, set a device
// viewport, and render every camera plus every UI Toolkit panel into one RenderTexture during normal frames
// (WaitForEndOfFrame never fires in batchmode), exactly like ScreenCaptureTests and HomeCaptureTests.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kestrel.Core.Ui;
using Kestrel.Game.Shell;
using Kestrel.Slice;
using Kestrel.UiChecks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Kestrel.Capture.Tests
{
    public static class FlightCapture
    {
        public struct Device { public string Name; public int W, H; }

        // iPhone SE (375x667 @2x), iPhone 15 (390x844 @3x), 15 Pro Max (430x932 @3x): HomeCaptureTests' phone names.
        public static readonly Device[] Phones =
        {
            new Device { Name = "se-375x667", W = 750, H = 1334 },
            new Device { Name = "iphone-390x844", W = 1170, H = 2532 },
            new Device { Name = "promax-430x932", W = 1290, H = 2796 },
        };

        public static string OutDir(string sub)
        {
            var d = Environment.GetEnvironmentVariable("KH_CAP_OUT");
            if (string.IsNullOrEmpty(d)) d = Path.Combine(Path.GetDirectoryName(Application.dataPath), "CaptureOut");
            d = Path.Combine(d, sub);
            Directory.CreateDirectory(d);
            return d;
        }

        /// <summary>No GPU (-nographics): skip with a warning, never an error log (it fails every later test).</summary>
        public static void RequireGraphics(string what)
        {
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            Debug.LogWarning("[Capture] no graphics device; " + what + " skipped");
            Assert.Ignore("No graphics device for " + what + ".");
        }

        public static IEnumerator Viewport(Device d, string name)
        {
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution((uint)d.W, (uint)d.H, "Kestrel " + name);
            for (int i = 0; i < 60 && (Screen.width != d.W || Screen.height != d.H); i++) yield return null;
            Assert.AreEqual(d.W, Screen.width, name + " width");
            Assert.AreEqual(d.H, Screen.height, name + " height");
#endif
            yield return null;
        }

        /// <summary>
        /// Boot -> Home LAUNCH -> briefing -> To the pad (FlightStartPlayTests' path); ends with the count on screen.
        /// <paramref name="cleared"/> &lt; 0 launches the save's next mission (a fresh save flies KH-1 First Hop, whose
        /// starter cuts off near T+26 and ends the flight). A count &gt;= 0 takes Home LAUNCH's own route as if that many
        /// missions were cleared (HomeRoutes.Launch: the same starter pick and briefing), without touching the save.
        /// </summary>
        public static IEnumerator BootToCount(Action<SliceApp> got, int cleared = -1)
        {
            if (AppShell.Instance != null) { UnityEngine.Object.Destroy(AppShell.Instance.gameObject); yield return null; }
            SceneManager.LoadScene(SceneFlow.BootScene, LoadSceneMode.Single);
            SceneFlow flow = null;
            for (int i = 0; i < 600 && (flow == null || flow.Busy || flow.Current != AppScreen.Home); i++)
            {
                if (flow == null) flow = UnityEngine.Object.FindAnyObjectByType<SceneFlow>();
                yield return null;
            }
            Assert.IsNotNull(flow, "Boot must own a scene flow");
            if (cleared < 0) flow.HomePressed("launch");
            else flow.Present(LaunchAfter(cleared));
            for (int i = 0; i < 600 && (flow.Busy || flow.Overlay != "Briefing"); i++) yield return null;
            Assert.AreEqual("Briefing", flow.Overlay, "LAUNCH opens the briefing");
            flow.BuildFromBriefing();
            SliceApp app = null;
            for (int i = 0; i < 600 && (flow.Busy || flow.Current != AppScreen.Design || app == null); i++)
            {
                app = UnityEngine.Object.FindAnyObjectByType<SliceApp>();
                yield return null;
            }
            Assert.IsNotNull(app, "slice app");
            if (!app.FlightHudVisible) app.PressLaunch();
            for (int i = 0; i < 600 && !app.CountingForCapture; i++) yield return null;
            Assert.IsTrue(app.CountingForCapture, "the count is on screen");
            got(app);
        }

        /// <summary>SceneFlow.HomeLaunchRoute with a given cleared count: the next mission's starter armed, its briefing up.</summary>
        public static Kestrel.Core.Flight.RouteResult LaunchAfter(int cleared)
        {
            var campaign = ScreenStub.LoadCampaign();
            Assert.IsNotNull(campaign, "campaign");
            var mission = Kestrel.Core.Flight.HomeRoutes.NextMission(campaign, cleared);
            Assert.IsNotNull(mission, "the mission after " + cleared + " cleared");
            Kestrel.Core.Flight.MissionBriefing briefing = null;
            string root = ScreenStub.RepoRoot();
            if (root != null)
                briefing = Kestrel.Core.Flight.MissionBriefing.Load(root, mission.Index, 0, Kestrel.Core.Flight.MissionContext.DefaultSeed(campaign));
            return Kestrel.Core.Flight.HomeRoutes.Launch(campaign, cleared, ScreenStub.LoadCatalog(), ScreenStub.LoadHangar()?.templates, briefing);
        }

        /// <summary>Writes &lt;dir&gt;/&lt;name&gt;.png and returns the sampled distinct colour count.</summary>
        public static IEnumerator Png(Device d, string dir, string name, Action<int> distinctColours)
        {
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
            var path = Path.Combine(dir, name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            int distinct = tex.GetPixels32().Where((_, i) => i % 97 == 0).Select(p => (p.r << 16) | (p.g << 8) | p.b).Distinct().Count();
            Debug.Log("[Capture] " + name + ": " + path + " " + d.W + "x" + d.H + " distinctColours=" + distinct);
            UnityEngine.Object.Destroy(tex);
            UnityEngine.Object.Destroy(rt);
            yield return null;
            distinctColours(distinct);
        }

        /// <summary>The flight screen as drawn: labels fit, nothing overlaps, nothing leaves the screen or the safe area.</summary>
        public static void HudChecks(SliceApp app, string name, List<string> problems)
        {
            var ui = app.UiForCapture;
            if (ui == null) { problems.Add(name + ": no UI root"); return; }
            var hud = ui.Q(UiIds.ScreenFlight) ?? ui;
            var screen = ui.worldBound;
            var s = Screen.safeArea;
            float k = Screen.width > 0 ? screen.width / Screen.width : 1f;
            var safe = new Rect(s.x * k, (Screen.height - s.yMax) * k, s.width * k, s.height * k);
            problems.AddRange(UiTreeChecks.Problems(hud, screen, safe, name));
        }

        /// <summary>a lies inside b, in whole panel pixels.</summary>
        public static bool Inside(Rect a, Rect b) =>
            Mathf.Round(a.xMin) >= Mathf.Round(b.xMin) && Mathf.Round(a.yMin) >= Mathf.Round(b.yMin)
            && Mathf.Round(a.xMax) <= Mathf.Round(b.xMax) && Mathf.Round(a.yMax) <= Mathf.Round(b.yMax);

        public static bool Shown(VisualElement e) =>
            e != null && e.resolvedStyle.display != DisplayStyle.None && e.resolvedStyle.visibility == Visibility.Visible
            && e.worldBound.width > 0 && e.worldBound.height > 0 && !e.ClassListContains("hidden");
    }
}
