// Starter preview camera (TestFlight build 204: the rocket was a sliver on a distant pad, so knob changes were
// invisible). In Starter mode SliceApp turns HangarCamera off and this frames the hangar-built vehicle alone in
// the sheet-free preview box (StarterFrame.Rocket), solved by Kestrel.Core.Designer.StarterPreviewFraming from the
// vehicle bounds, Camera.fieldOfView and the screen aspect. Every rebuild or layout change sets a new target and
// the pose eases to it (StarterPreviewFraming.Step, ~0.4 s, no overshoot); Reduce Motion snaps. Only the transform
// and the projection's principal point change: no viewport rect, no post effects, nothing that flashes, and only
// APIs that also run on WebGL.
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using UnityEngine;

namespace Kestrel.Slice
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class StarterPreviewCamera : MonoBehaviour
    {
        Camera cam;
        StarterPreviewSubject subject;
        HudBox box;
        float panelW, panelH;
        bool haveSubject, haveBox, dirty = true;
        StarterPreviewPose target, now;

        /// <summary>Snap instead of easing (Reduce Motion / Reduce Flashing, read from HangarCamera by SliceApp).</summary>
        public bool ReduceMotion;
        public StarterPreviewPose Target => target;
        public StarterPreviewPose Pose => now;
        public StarterPreviewSubject Subject => subject;
        public HudBox Box => box;

        /// <summary>The built vehicle: deck (bell exits) at yMin, nose at yMax, widest radius (core, strap-ons, legs).</summary>
        public void SetVehicle(float yMin, float yMax, float radius)
        {
            var s = new StarterPreviewSubject(yMin, yMax, radius);
            if (!s.Usable) return;
            if (haveSubject && System.Math.Abs(s.YMin - subject.YMin) < 1e-4 && System.Math.Abs(s.YMax - subject.YMax) < 1e-4 && System.Math.Abs(s.Radius - subject.Radius) < 1e-4) return;
            subject = s; haveSubject = true; dirty = true;
        }

        /// <summary>The preview box in points on a <paramref name="widthPt"/> x <paramref name="heightPt"/> screen.</summary>
        public void SetPreview(HudBox rocketBox, float widthPt, float heightPt)
        {
            if (!rocketBox.Live || !(widthPt > 1f) || !(heightPt > 1f)) return;
            if (haveBox && Same(rocketBox, box) && Mathf.Abs(widthPt - panelW) < 0.01f && Mathf.Abs(heightPt - panelH) < 0.01f) return;
            box = rocketBox; panelW = widthPt; panelH = heightPt; haveBox = true; dirty = true;
        }

        static bool Same(HudBox a, HudBox b) => Mathf.Abs(a.X - b.X) < 0.01f && Mathf.Abs(a.Y - b.Y) < 0.01f && Mathf.Abs(a.W - b.W) < 0.01f && Mathf.Abs(a.H - b.H) < 0.01f;

        /// <summary>Jump to the target now (entering Starter, tests).</summary>
        public void Snap()
        {
            Solve();
            if (target.Valid) now = target;
            Apply();
        }

        void Solve()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null || !haveSubject || !haveBox) return;
            int w = cam.pixelWidth > 0 ? cam.pixelWidth : Screen.width, h = cam.pixelHeight > 0 ? cam.pixelHeight : Screen.height;
            if (!DesignFraming.ValidViewport(w, h)) return;
            float fov = cam.fieldOfView > 1f ? cam.fieldOfView : 50f;
            var t = StarterPreviewFraming.Solve(subject, box, panelW, panelH, (double)w / h, fov);
            if (t.Valid) target = t;
            dirty = false;
        }

        void OnEnable() { now = default; dirty = true; }

        void OnDisable()
        {
            now = default;
            if (cam != null) cam.ResetProjectionMatrix();
        }

        void LateUpdate()
        {
            if (dirty || !target.Valid) Solve();
            float dt = Time.unscaledDeltaTime;
            if (float.IsNaN(dt) || dt < 0f) dt = 0f;
            now = StarterPreviewFraming.Step(now, target, dt, ReduceMotion);
            Apply();
        }

        /// <summary>Put the camera on the current (eased) pose.</summary>
        public void Apply()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null || !now.Valid) return;
            var pos = new Vector3((float)now.PosX, (float)now.PosY, (float)now.PosZ);
            var aim = new Vector3((float)now.AimX, (float)now.AimY, (float)now.AimZ);
            if (!DesignFraming.IsFinite(pos) || (aim - pos).sqrMagnitude < 1e-4f) return;
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(aim - pos, Vector3.up));
            cam.ResetProjectionMatrix();
            var p = cam.projectionMatrix;
            p.m02 -= (float)now.ShiftX;   // DesignFraming.ShiftedProjection convention: the principal point moves to the box centre
            p.m12 -= (float)now.ShiftY;
            if (DesignFraming.IsFinite(p)) cam.projectionMatrix = p;
        }
    }
}
