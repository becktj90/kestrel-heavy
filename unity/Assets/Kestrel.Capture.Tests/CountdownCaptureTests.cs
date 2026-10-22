// Real countdown captures for Trevor (TestFlight 202, "Cam: Below"): the shipping flow held on the count, one PNG per
// phone size and camera mode, each after the view has settled. Every shot also asserts, on the live scene:
//   - UiTreeChecks on the flight screen (labels fit, no overlaps, nothing off screen or out of the safe area);
//   - the COUNT tag sits inside its parent row and never under the guide pop;
//   - every umbilical's pad end is on pad structure (UmbilicalRig.StructureGap <= PadGenerator.RailSkinGap) and the
//     tower drew this frame (PadRenderer.Ledger);
//   - in Below, the camera is under the rocket's base and pitched up.
// The PNG is written before the checks, so a failing shot still uploads its picture.
// Output: $KH_CAP_OUT/countdown or <project>/CaptureOut/countdown/countdown-<device>-<mode>.png (countdown-captures).
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kestrel.Core.Flight;
using Kestrel.Pad;
using Kestrel.Slice;
using Kestrel.UiChecks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Kestrel.Capture.Tests
{
    public class CountdownCaptureTests
    {
        const int SettleFrames = 150;   // FlightViewPose smoothing settles in about 1.5 s at the fixed 1/60 step

        static readonly (string Name, FlightViewMode Mode)[] Modes =
        {
            ("auto", FlightViewMode.Auto), ("above", FlightViewMode.Above), ("below", FlightViewMode.Below),
            ("chase", FlightViewMode.Chase), ("pad", FlightViewMode.Pad),
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
            if (Kestrel.Game.Shell.AppShell.Instance != null) Object.Destroy(Kestrel.Game.Shell.AppShell.Instance.gameObject);
#if UNITY_EDITOR
            if (priorWidth > 0 && priorHeight > 0)
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(priorWidth, priorHeight, "Previous capture viewport");
#endif
            yield return null;
        }

        [UnityTest]
        [Timeout(1800000)]
        public IEnumerator CountdownInEveryCameraAtEveryPhoneSize()
        {
#if !UNITY_EDITOR
            Assert.Ignore("Device captures need the editor play-mode viewport.");
            yield break;
#else
            FlightCapture.RequireGraphics("countdown captures");
            var problems = new List<string>();
            var dir = FlightCapture.OutDir("countdown");
            SliceApp.HoldCountForCapture = true;   // the count stays on screen for all five views
            Time.captureDeltaTime = 1f / 60f;
            foreach (var d in FlightCapture.Phones)
            {
                var vp = FlightCapture.Viewport(d, "countdown-" + d.Name);
                while (vp.MoveNext()) yield return vp.Current;
                SliceApp app = null;
                var boot = FlightCapture.BootToCount(a => app = a);
                while (boot.MoveNext()) yield return boot.Current;
                foreach (var m in Modes)
                {
                    string name = "countdown-" + d.Name + "-" + m.Name;
                    app.ChooseViewForCapture(m.Mode);
                    for (int i = 0; i < SettleFrames; i++) yield return null;
                    if (app.ViewForCapture != m.Mode) problems.Add(name + ": view is " + app.ViewForCapture);
                    if (!app.CountingForCapture) problems.Add(name + ": the count left the screen");
                    int distinct = 0;
                    var png = FlightCapture.Png(d, dir, name, n => distinct = n);
                    while (png.MoveNext()) yield return png.Current;
                    if (distinct <= 8) problems.Add(name + ": capture is flat colour (" + distinct + " colours)");
                    FlightCapture.HudChecks(app, name, problems);
                    CountTag(app, name, problems);
                    PadAttached(app, name, problems);
                    if (m.Mode == FlightViewMode.Below) LooksUpFromThePad(app, name, problems);
                }
            }
            Assert.That(problems, Is.Empty, UiTreeChecks.Report(problems, 120));
#endif
        }

        static void CountTag(SliceApp app, string name, List<string> problems)
        {
            var tag = app.UiForCapture?.Q<Label>("h-stage");
            if (!FlightCapture.Shown(tag)) { problems.Add(name + ": COUNT tag not shown"); return; }
            if (tag.text != CountdownStack.TagText) problems.Add(name + ": tag reads \"" + tag.text + "\"");
            var parent = tag.hierarchy.parent;
            if (parent != null && !FlightCapture.Inside(tag.worldBound, parent.worldBound))
                problems.Add(name + ": COUNT " + tag.worldBound + " leaves its parent " + UiTreeChecks.Describe(parent) + " " + parent.worldBound);
            foreach (var doc in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                var pop = doc.rootVisualElement?.Q("guide-pop");
                if (FlightCapture.Shown(pop) && pop.worldBound.Overlaps(tag.worldBound))
                    problems.Add(name + ": the guide pop " + pop.worldBound + " covers COUNT " + tag.worldBound);
            }
        }

        static void PadAttached(SliceApp app, string name, List<string> problems)
        {
            var pad = app.PadForCapture;
            if (pad == null || pad.Layout == null) { problems.Add(name + ": no pad renderer"); return; }
            if (!pad.gameObject.activeInHierarchy) problems.Add(name + ": the pad is inactive");
            if (pad.Ledger.DrawCalls <= 0) problems.Add(name + ": the tower drew nothing (" + pad.Ledger.Report(pad.Quality) + ")");
            foreach (var link in pad.Layout.Links)
            {
                float gap = UmbilicalRig.StructureGap(pad.Layout, UmbilicalRig.PadEnd(link), out string on);
                if (gap > PadGenerator.RailSkinGap)
                    problems.Add(name + ": umbilical " + link.AttachId + " hangs " + gap.ToString("0.00") + " m from " + on);
            }
        }

        /// <summary>#348's Below spec, live: the rocket base is the vehicle root (the stack bottom), not the lowest
        /// renderer under it (#332 read -12.28 m from the trench plume). The camera stands 0.6-1.5 m over the deck top,
        /// looks up >= 15 deg, and the whole stack is inside the viewport with >= 3% margin, >= 35% of its height.
        /// The pure checks gate in EditMode (FlightViewPoseTests, PadBelowFramingTests, CountdownBelowDeviceTests).</summary>
        static void LooksUpFromThePad(SliceApp app, string name, List<string> problems)
        {
            var cam = app.CameraForCapture; var rocket = app.VehicleForCapture;
            if (cam == null || rocket == null) { problems.Add(name + ": no camera or vehicle"); return; }
            var c = cam.transform;
            float over = c.position.y - app.DeckTopForCapture;
            if (over < 0.6f - 0.05f || over > 1.5f + 0.05f)
                problems.Add(name + ": camera " + over.ToString("0.00") + " m over the deck top (want 0.6-1.5)");
            if (c.position.y >= rocket.position.y + app.VehicleHeightForCapture * 0.5f)
                problems.Add(name + ": camera y " + c.position.y.ToString("0.00") + " is not under the stack's middle (base " + rocket.position.y.ToString("0.00") + ")");
            float pitch = Mathf.Asin(Mathf.Clamp(c.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            if (pitch < 15f) problems.Add(name + ": camera pitch " + pitch.ToString("0.0") + " deg is not looking up (want >= 15)");
            float h = app.VehicleHeightForCapture, r = Mathf.Max(0.1f, app.VehicleRadiusForCapture);
            float x0 = 1f, x1 = 0f, y0 = 1f, y1 = 0f;
            for (int i = 0; i <= 8; i++)
            for (int k = 0; k < 24; k++)
            {
                float a = k * Mathf.PI * 2f / 24f;
                var w = rocket.position + rocket.up * (h * i / 8f) + rocket.right * (Mathf.Cos(a) * r) + rocket.forward * (Mathf.Sin(a) * r);
                var v = cam.WorldToViewportPoint(w);
                if (v.z <= cam.nearClipPlane) { problems.Add(name + ": stack point behind the near plane"); return; }
                x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x); y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y);
            }
            if (x0 < 0.03f || x1 > 0.97f || y0 < 0.03f || y1 > 0.97f)
                problems.Add(name + ": stack spans viewport x " + x0.ToString("0.000") + "-" + x1.ToString("0.000") + " y " + y0.ToString("0.000") + "-" + y1.ToString("0.000") + " (3% margin)");
            if (y1 - y0 < 0.35f) problems.Add(name + ": stack fills " + (y1 - y0).ToString("0.00") + " of the height (want >= 0.35)");
        }
    }
}
