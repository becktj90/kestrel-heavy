// Orbit, pinch-zoom and double-tap reframe for the Hangar. Sits on the design camera.
// SliceApp assigns the vehicle and the exploded view; the designer toggle lives in Slice.uxml.
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

        /// <summary>A single tap on the 3D view (not on the panel), in screen pixels. The Hangar picks a part with it.</summary>
        public event System.Action<Vector2> Tapped;
        /// <summary>A finger that moves less than this (points) and lifts within TapSeconds is a tap, not an orbit.</summary>
        public const float TapSlopPt = 4f, TapSeconds = 0.35f;
        static float TapSlopPx => TapSlopPt * DesignFraming.PixelsPerPoint(Screen.width, Screen.height, Screen.dpi);

        float focusAim = float.NaN, focusAimTarget = float.NaN, focusZoomTarget = float.NaN;
        Vector2 tapStart; float tapStartTime; bool tapCandidate;

        public float YawDeg => orbit.YawDeg;
        /// <summary>Aim height in metres while a part is focused, NaN at rest.</summary>
        public float FocusAim => focusAim;
        public bool Focused => !float.IsNaN(focusAimTarget);
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

        public void Reframe() { ClearFocus(); orbit.BeginReframe(ReduceMotion); }

        /// <summary>Glide the aim to a part's centre and zoom so the part fills about half the view.</summary>
        public void Focus(float aimY, float partHeight)
        {
            if (float.IsNaN(aimY) || float.IsInfinity(aimY)) { ClearFocus(); return; }
            if (float.IsNaN(focusAim)) focusAim = height * DesignFraming.AimHeight;
            focusAimTarget = aimY;
            focusZoomTarget = HangarFocus.ZoomFor(height, partHeight);
            orbit.Reframing = false;
            if (ReduceMotion) { focusAim = focusAimTarget; orbit.Zoom = focusZoomTarget; }
        }

        /// <summary>Back to the whole-rocket aim (zoom is left where the player put it until Reframe).</summary>
        public void ClearFocus()
        {
            if (float.IsNaN(focusAimTarget) && float.IsNaN(focusAim)) return;
            focusAimTarget = float.NaN; focusZoomTarget = float.NaN;
            if (ReduceMotion) focusAim = float.NaN;
        }

        void StepFocus(float dt)
        {
            bool reduce = ReduceMotion;
            if (!float.IsNaN(focusAimTarget))
            {
                focusAim = HangarFocus.Approach(focusAim, focusAimTarget, dt, reduce);
                if (!float.IsNaN(focusZoomTarget) && !pinching)
                {
                    orbit.Zoom = HangarFocus.Approach(orbit.Zoom > 0f ? orbit.Zoom : 1f, focusZoomTarget, dt, reduce);
                    if (orbit.Zoom == focusZoomTarget) focusZoomTarget = float.NaN;
                }
            }
            else if (!float.IsNaN(focusAim))
            {
                float rest = height * DesignFraming.AimHeight;
                focusAim = HangarFocus.Approach(focusAim, rest, dt, reduce);
                if (focusAim == rest) focusAim = float.NaN;
            }
        }
        public void Drag(float dxPx, float dyPx, float dt) => orbit.Drag(dxPx, dyPx, dt, ReduceMotion);
        public void Pinch(float scale) { focusZoomTarget = float.NaN; orbit.Pinch(scale); }

        /// <summary>Vehicle-space exploded view. Assigned by the designer. Not a child of this camera.</summary>
        public HangarExplodedView ExplodedView;
        bool exploded;
        public bool Exploded => exploded;

        public void SetExploded(bool on)
        {
            exploded = on;
            if (ExplodedView != null) ExplodedView.SetExploded(on, ReduceMotion);
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

        /// <summary>A tap: short and still. Slop is in pixels at the caller's scale.</summary>
        public static bool IsTap(Vector2 down, Vector2 up, float seconds, float slopPx)
            => seconds >= 0f && seconds <= TapSeconds && Vector2.Distance(down, up) <= slopPx;

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
            StepFocus(dt);
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
            HangarOrbit.PoseAt(height, k, orbit.YawDeg, orbit.PitchDeg, orbit.Zoom, focusAim, out var pos, out var aim);
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
                tapCandidate = false;
                if (BlocksGesture(t.position)) { dragging = false; dragFinger = int.MinValue; return; }
                if (t.tapCount >= 2) { Reframe(); dragging = false; dragFinger = int.MinValue; return; }
                dragging = true; dragFinger = t.fingerId;
                tapCandidate = true; tapStart = t.position; tapStartTime = Time.unscaledTime;
                return;
            }
            if (dragging && t.fingerId == dragFinger && (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary))
            {
                if (tapCandidate && Vector2.Distance(t.position, tapStart) > TapSlopPx) tapCandidate = false;
                if (!tapCandidate) Drag(t.deltaPosition.x, t.deltaPosition.y, dt);
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                bool tap = tapCandidate && t.phase == TouchPhase.Ended && t.tapCount < 2 && IsTap(tapStart, t.position, Time.unscaledTime - tapStartTime, TapSlopPx);
                dragging = false; dragFinger = int.MinValue; tapCandidate = false;
                if (tap) Tapped?.Invoke(t.position);
            }
        }

#if UNITY_EDITOR
        void ReadMouse(float dt)
        {
            if (Input.touchCount > 0) return;
            var m = Input.mousePosition;
            if (Input.GetMouseButtonDown(0)) { mouseDown = !BlocksGesture(m); prevMouse = m; dragging = mouseDown; tapCandidate = mouseDown; tapStart = m; tapStartTime = Time.unscaledTime; return; }
            if (Input.GetMouseButtonUp(0) && mouseDown && tapCandidate && IsTap(tapStart, m, Time.unscaledTime - tapStartTime, TapSlopPx)) { mouseDown = false; dragging = false; tapCandidate = false; Tapped?.Invoke(m); return; }
            if (Input.GetMouseButton(0) && mouseDown && tapCandidate && Vector2.Distance(m, tapStart) > TapSlopPx) tapCandidate = false;
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
