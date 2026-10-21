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

        static void LooksUpFromThePad(SliceApp app, string name, List<string> problems)
        {
            var cam = app.CameraForCapture; var rocket = app.VehicleForCapture;
            if (cam == null || rocket == null) { problems.Add(name + ": no camera or vehicle"); return; }
            float baseY = rocket.position.y;
            var r = rocket.GetComponentsInChildren<Renderer>().Where(x => x.enabled).ToArray();
            if (r.Length > 0) baseY = r.Min(x => x.bounds.min.y);
            var c = cam.transform;
            if (c.position.y >= baseY) problems.Add(name + ": camera y " + c.position.y.ToString("0.00") + " is not below the rocket base " + baseY.ToString("0.00"));
            float pitch = Mathf.Asin(Mathf.Clamp(c.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            if (pitch < 10f) problems.Add(name + ": camera pitch " + pitch.ToString("0.0") + " deg is not looking up");
        }
    }
}
