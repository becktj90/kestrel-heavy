// Orbit, pinch-zoom and double-tap reframe for the Hangar. Sits on the design camera.
// UX calls SetVehicle / SetBottomVisible / ReduceMotion. SliceApp and Slice.uxml stay untouched.
// On iPad the projection shifts left by the 400 pt right-hand panel (DesignFraming.SideVisibleFraction).
using System.Collections.Generic;
using UnityEngine;

namespace Kestrel.Slice
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class HangarCamera : MonoBehaviour
    {
        public const string ReduceMotionPref = "kh.a11y.reduceMotion";
        public const string ReduceFlashingPref = "kh.a11y.reduceFlashing";

        HangarOrbit orbit = HangarOrbit.Rest();
        readonly List<Vector3> fit = new List<Vector3>(64);
        bool hasFit;
        float height = 70f;
        float bottomVisible = 1f;
        bool reduceSet, reduceForced;
        bool dragging, pinching, havePinchMid;
        int dragFinger = int.MinValue;
        float pinchDist;
        Vector2 pinchMid;
        Camera cam;
#if UNITY_EDITOR
        Vector3 prevMouse;
        bool mouseDown;
#endif

        public float YawDeg => orbit.YawDeg;
        public float PitchDeg => orbit.PitchDeg;
        public float Zoom => orbit.Zoom > 0f ? orbit.Zoom : 1f;
        public float VehicleHeight => height;

        /// <summary>True when motion should be instant: the OS Reduce Motion flag or the in-game Reduce Flashing
        /// toggle, whichever UX writes here. Unset, the two PlayerPrefs keys are read instead. There is no
        /// native iOS bridge in this assembly.</summary>
        public bool ReduceMotion
        {
            get => reduceSet ? reduceForced
                : PlayerPrefs.GetInt(ReduceMotionPref, 0) != 0 || PlayerPrefs.GetInt(ReduceFlashingPref, 0) != 0;
            set { reduceSet = true; reduceForced = value; if (value) orbit.Step(0.0001f, true); }
        }

        public void SetBottomVisible(float visibleFraction)
        {
            bottomVisible = DesignFraming.SafeVisible(visibleFraction);
            ApplyPose();
        }

        /// <summary>Frame this vehicle. Height drives aim and distance. fitPoints are the same subject points
        /// DesignFraming fits (vehicle, tower, deck); null frames the height alone.</summary>
        public void SetVehicle(float heightMeters, IReadOnlyList<Vector3> fitPoints)
        {
            height = DesignFraming.SafeHeight(heightMeters);
            fit.Clear();
            hasFit = fitPoints != null && fitPoints.Count > 0;
            if (hasFit) for (int i = 0; i < fitPoints.Count; i++) fit.Add(fitPoints[i]);
            ApplyPose();
        }

        public void Reframe() => orbit.BeginReframe(ReduceMotion);
        public void Drag(float dxPx, float dyPx, float dt) => orbit.Drag(dxPx, dyPx, dt, ReduceMotion);
        public void Pinch(float scale) => orbit.Pinch(scale);

        /// <summary>Assigned by UX, or a <see cref="HangarExplodedView"/> on this camera's children.
        /// <see cref="SetExploded"/> is the only call UX needs.</summary>
        public HangarExplodedView ExplodedView;
        bool exploded;
        public bool Exploded => exploded;

        public void SetExploded(bool on)
        {
            exploded = on;
            var view = ExplodedView != null ? ExplodedView : GetComponentInChildren<HangarExplodedView>(true);
            if (view != null) view.SetExploded(on, ReduceMotion);
        }

        public bool BlocksGesture(Vector2 screenPx)
        {
            float w = cam != null && cam.pixelWidth > 0 ? cam.pixelWidth : Screen.width;
            float h = cam != null && cam.pixelHeight > 0 ? cam.pixelHeight : Screen.height;
            return BlocksGesture(screenPx, w, h, Screen.dpi, bottomVisible);
        }

        /// <summary>Bottom sheet, and on iPad the 400 pt panel on the right. Touches there belong to UX.</summary>
        public static bool BlocksGesture(Vector2 screenPx, float screenW, float screenH, float dpi, float bottomVisibleFraction)
        {
            if (!(screenW > 0f) || !(screenH > 0f)) return false;
            if (DesignFraming.HasSidePanel(screenW, screenH))
            {
                float panelPx = DesignFraming.SidePanelPoints * DesignFraming.PixelsPerPoint(screenW, screenH, dpi);
                if (screenPx.x >= screenW - panelPx) return true;
            }
            float f = DesignFraming.SafeVisible(bottomVisibleFraction);
            return f < 1f && screenPx.y <= screenH * (1f - f);
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (float.IsNaN(dt) || dt < 0f) dt = 0f;
            if (dt > 0.05f) dt = 0.05f;
            ReadTouches(dt);
#if UNITY_EDITOR
            ReadMouse(dt);
#endif
            if (!dragging && !pinching) orbit.Step(dt, ReduceMotion);
            ApplyPose();
        }

        public void ApplyPose()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) return;
            int w = cam.pixelWidth > 0 ? cam.pixelWidth : Screen.width;
            int h = cam.pixelHeight > 0 ? cam.pixelHeight : Screen.height;
            if (!DesignFraming.ValidViewport(w, h)) return;
            float side = DesignFraming.SideVisibleFraction(w, h, Screen.dpi);
            float fov = cam.fieldOfView > 1f ? cam.fieldOfView : 50f;
            float k = DesignFraming.FitDistanceScale(hasFit ? fit : null, height, (float)w / h, fov, bottomVisible, side);
            HangarOrbit.Pose(height, k, orbit.YawDeg, orbit.PitchDeg, orbit.Zoom, out var pos, out var aim);
            if (!DesignFraming.IsFinite(pos) || (pos - aim).sqrMagnitude < 1e-4f) return;
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(aim - pos));
            cam.ResetProjectionMatrix();
            var proj = DesignFraming.ShiftedProjection(cam.projectionMatrix, bottomVisible, side);
            if (DesignFraming.IsFinite(proj)) cam.projectionMatrix = proj;
        }

        void ReadTouches(float dt)
        {
            int n = Input.touchCount;
            if (n <= 0) { dragging = false; pinching = false; havePinchMid = false; dragFinger = int.MinValue; return; }
            if (n >= 2)
            {
                var a = Input.GetTouch(0);
                var b = Input.GetTouch(1);
                if (BlocksGesture(a.position) && BlocksGesture(b.position)) { pinching = false; return; }
                float dist = Vector2.Distance(a.position, b.position);
                var mid = (a.position + b.position) * 0.5f;
                if (!pinching) { pinching = true; pinchDist = dist; pinchMid = mid; havePinchMid = true; dragging = false; }
                else
                {
                    if (pinchDist > 1f && dist > 1f) Pinch(dist / pinchDist);
                    if (havePinchMid) Drag(mid.x - pinchMid.x, mid.y - pinchMid.y, dt);
                    pinchDist = dist; pinchMid = mid;
                }
                return;
            }
            pinching = false; havePinchMid = false;
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                if (BlocksGesture(t.position)) { dragging = false; dragFinger = int.MinValue; return; }
                if (t.tapCount >= 2) { Reframe(); dragging = false; dragFinger = int.MinValue; return; }
                dragging = true; dragFinger = t.fingerId;
                return;
            }
            if (dragging && t.fingerId == dragFinger && (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary))
                Drag(t.deltaPosition.x, t.deltaPosition.y, dt);
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            { dragging = false; dragFinger = int.MinValue; }
        }

#if UNITY_EDITOR
        void ReadMouse(float dt)
        {
            if (Input.touchCount > 0) return;
            var m = Input.mousePosition;
            if (Input.GetMouseButtonDown(0)) { mouseDown = !BlocksGesture(m); prevMouse = m; dragging = mouseDown; return; }
            if (Input.GetMouseButton(0) && mouseDown)
            {
                Drag(m.x - prevMouse.x, m.y - prevMouse.y, dt);
                prevMouse = m; dragging = true; return;
            }
            if (Input.GetMouseButtonUp(0)) { mouseDown = false; dragging = false; }
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f && !BlocksGesture(m)) Pinch(Mathf.Pow(1.08f, scroll));
        }
#endif
    }
}
