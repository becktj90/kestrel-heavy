// Designer orbit around the vehicle. Pure state: HangarCamera applies it to the camera.
// Rest pose matches DesignFraming.CameraPosition (zoom 1, yaw 0, pitch 0). Yaw is a full turn
// clamped at ±180°, pitch stays off the floor and off a top-down view, zoom is a magnification.
using UnityEngine;

namespace Kestrel.Slice
{
    public static class HangarMotion
    {
        public const float ZoomMin = 0.6f;
        public const float ZoomMax = 3f;
        public const float YawLimitDeg = 180f;
        /// <summary>Added to the rest elevation. Negative looks up from below the aim point; positive looks down.</summary>
        public const float PitchMinDeg = -12f;
        public const float PitchMaxDeg = 58f;
        public const float ReframeSeconds = 0.4f;
        /// <summary>Exploded-view ease, same length as the part separation (<see cref="HangarExplode.Seconds"/>).</summary>
        public const float ExplodedSeconds = 0.4f;
        /// <summary>Magnification while parts are separated. 1 is the assembled frame; higher is closer.</summary>
        public const float ExplodedZoom = 1.8f;
        /// <summary>Look slightly down the stack so the gaps between parts read.</summary>
        public const float ExplodedPitchDeg = 12f;
        /// <summary>Exponential decay rate of a flick, per second. About 0.2 s to fall to 1/e.</summary>
        public const float InertiaDamping = 5f;
        public const float DegreesPerPixel = 0.35f;
        public const float VelocityStop = 2f;
    }

    /// <summary>Focus on one part: aim height and the zoom that makes it fill about half the view.</summary>
    public static class HangarFocus
    {
        /// <summary>Share of the framed height the focused part should fill.</summary>
        public const float Fill = 0.5f;
        /// <summary>Exponential approach rate of the aim and zoom, per second (about 0.25 s to settle).</summary>
        public const float Rate = 12f;

        /// <summary>Zoom for a part of <paramref name="partHeight"/> on a vehicle framed at <paramref name="vehicleHeight"/>.</summary>
        public static float ZoomFor(float vehicleHeight, float partHeight)
        {
            vehicleHeight = DesignFraming.SafeHeight(vehicleHeight);
            if (!(partHeight > 0.1f) || float.IsNaN(partHeight) || float.IsInfinity(partHeight)) return 1f;
            return HangarOrbit.ClampZoom(Fill * vehicleHeight / partHeight);
        }

        /// <summary>One smoothing step toward a target (instant when Reduce Motion is on or dt is bad).</summary>
        public static float Approach(float current, float target, float dt, bool reduceMotion)
        {
            if (reduceMotion || float.IsNaN(current) || !(dt > 0f) || float.IsNaN(dt)) return target;
            float k = 1f - Mathf.Exp(-Rate * dt);
            float v = current + (target - current) * k;
            return Mathf.Abs(target - v) < 1e-3f ? target : v;
        }
    }

    /// <summary>Yaw, pitch and zoom plus flick velocity. Zoom 0 means "not set" and is treated as 1.</summary>
    public struct HangarOrbit
    {
        public float YawDeg, PitchDeg, Zoom, YawVel, PitchVel;
        public bool Reframing;
        /// <summary>True while the camera is easing toward the exploded pose or back to the pose it left.</summary>
        public bool ExplodeFraming;
        float fromYaw, fromPitch, fromZoom, reframeElapsed;
        bool explodedOn;
        float savedYaw, savedPitch, savedZoom;
        float exFromYaw, exFromPitch, exFromZoom, exToYaw, exToPitch, exToZoom, exElapsed;

        public static HangarOrbit Rest() { var o = new HangarOrbit(); o.Zoom = 1f; return o; }

        public static float ClampYaw(float deg)
        {
            if (float.IsNaN(deg) || float.IsInfinity(deg)) return 0f;
            return Mathf.Clamp(deg, -HangarMotion.YawLimitDeg, HangarMotion.YawLimitDeg);
        }

        public static float ClampPitch(float deg)
        {
            if (float.IsNaN(deg) || float.IsInfinity(deg)) return 0f;
            return Mathf.Clamp(deg, HangarMotion.PitchMinDeg, HangarMotion.PitchMaxDeg);
        }

        public static float ClampZoom(float zoom)
        {
            if (!(zoom > 0f) || float.IsNaN(zoom) || float.IsInfinity(zoom)) return 1f;
            return Mathf.Clamp(zoom, HangarMotion.ZoomMin, HangarMotion.ZoomMax);
        }

        /// <summary>One-finger orbit. Screen +x yaws, screen +y (up) raises the camera. A flick stores deg/s
        /// unless Reduce Motion is on, in which case the pose tracks the finger and then stops.</summary>
        public void Drag(float dxPx, float dyPx, float dt, bool reduceMotion)
        {
            if (dxPx == 0f && dyPx == 0f) return;
            Reframing = false;
            ExplodeFraming = false;
            float yaw = dxPx * HangarMotion.DegreesPerPixel;
            float pitch = dyPx * HangarMotion.DegreesPerPixel;
            YawDeg = ClampYaw(YawDeg + yaw);
            PitchDeg = ClampPitch(PitchDeg + pitch);
            if (reduceMotion || !(dt > 1e-4f) || float.IsNaN(dt)) { YawVel = PitchVel = 0f; return; }
            YawVel = yaw / dt;
            PitchVel = pitch / dt;
            if (YawDeg >= HangarMotion.YawLimitDeg && YawVel > 0f) YawVel = 0f;
            if (YawDeg <= -HangarMotion.YawLimitDeg && YawVel < 0f) YawVel = 0f;
            if (PitchDeg >= HangarMotion.PitchMaxDeg && PitchVel > 0f) PitchVel = 0f;
            if (PitchDeg <= HangarMotion.PitchMinDeg && PitchVel < 0f) PitchVel = 0f;
        }

        /// <summary>Pinch scale multiplies magnification. 1 keeps the framed distance; 3 is three times closer.</summary>
        public void Pinch(float scale)
        {
            if (!(scale > 0f) || float.IsNaN(scale) || float.IsInfinity(scale)) return;
            Reframing = false;
            ExplodeFraming = false;
            Zoom = ClampZoom(ClampZoom(Zoom) * scale);
        }

        /// <summary>Ease to a closer pose while the exploded view is open, and back to the pose captured
        /// when it opened. The same call twice does not recapture that pose. Reduce Motion snaps.</summary>
        public void BeginExploded(bool on, bool reduceMotion)
        {
            if (on == explodedOn) return;
            explodedOn = on;
            YawVel = PitchVel = 0f;
            Reframing = false;
            float yaw = ClampYaw(YawDeg), pitch = ClampPitch(PitchDeg), zoom = ClampZoom(Zoom);
            float toYaw, toPitch, toZoom;
            if (on)
            {
                savedYaw = yaw; savedPitch = pitch; savedZoom = zoom;
                toYaw = yaw;
                toPitch = HangarMotion.ExplodedPitchDeg;
                toZoom = HangarMotion.ExplodedZoom;
            }
            else
            {
                toYaw = savedYaw;
                toPitch = savedPitch;
                toZoom = savedZoom > 0f ? savedZoom : 1f;
            }
            if (reduceMotion)
            {
                YawDeg = toYaw; PitchDeg = toPitch; Zoom = toZoom;
                ExplodeFraming = false;
                return;
            }
            exFromYaw = yaw; exFromPitch = pitch; exFromZoom = zoom;
            exToYaw = toYaw; exToPitch = toPitch; exToZoom = toZoom;
            exElapsed = 0f;
            ExplodeFraming = true;
        }

        public void BeginReframe(bool reduceMotion)
        {
            YawVel = PitchVel = 0f;
            ExplodeFraming = false;
            if (reduceMotion)
            {
                if (explodedOn) SnapExplodedFrame();
                else SnapRest();
                return;
            }
            fromYaw = YawDeg; fromPitch = PitchDeg; fromZoom = ClampZoom(Zoom);
            reframeElapsed = 0f;
            Reframing = true;
        }

        public void Step(float dt, bool reduceMotion)
        {
            if (reduceMotion)
            {
                YawVel = PitchVel = 0f;
                if (Reframing)
                {
                    if (explodedOn) SnapExplodedFrame();
                    else SnapRest();
                }
                else if (ExplodeFraming)
                {
                    YawDeg = exToYaw; PitchDeg = exToPitch; Zoom = exToZoom;
                    ExplodeFraming = false;
                }
                return;
            }
            if (!(dt > 0f) || float.IsNaN(dt)) return;
            if (Reframing)
            {
                reframeElapsed += dt;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(reframeElapsed / HangarMotion.ReframeSeconds));
                float ty = 0f;
                float tp = explodedOn ? HangarMotion.ExplodedPitchDeg : 0f;
                float tz = explodedOn ? HangarMotion.ExplodedZoom : 1f;
                YawDeg = Mathf.Lerp(fromYaw, ty, u);
                PitchDeg = Mathf.Lerp(fromPitch, tp, u);
                Zoom = Mathf.Lerp(fromZoom, tz, u);
                YawVel = PitchVel = 0f;
                if (reframeElapsed >= HangarMotion.ReframeSeconds)
                {
                    if (explodedOn) SnapExplodedFrame();
                    else SnapRest();
                }
                return;
            }
            if (ExplodeFraming)
            {
                exElapsed += dt;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(exElapsed / HangarMotion.ExplodedSeconds));
                YawDeg = Mathf.Lerp(exFromYaw, exToYaw, u);
                PitchDeg = Mathf.Lerp(exFromPitch, exToPitch, u);
                Zoom = Mathf.Lerp(exFromZoom, exToZoom, u);
                YawVel = PitchVel = 0f;
                if (exElapsed >= HangarMotion.ExplodedSeconds)
                {
                    YawDeg = exToYaw; PitchDeg = exToPitch; Zoom = exToZoom;
                    ExplodeFraming = false;
                }
                return;
            }
            YawDeg = ClampYaw(YawDeg + YawVel * dt);
            PitchDeg = ClampPitch(PitchDeg + PitchVel * dt);
            if (YawDeg >= HangarMotion.YawLimitDeg && YawVel > 0f) YawVel = 0f;
            if (YawDeg <= -HangarMotion.YawLimitDeg && YawVel < 0f) YawVel = 0f;
            if (PitchDeg >= HangarMotion.PitchMaxDeg && PitchVel > 0f) PitchVel = 0f;
            if (PitchDeg <= HangarMotion.PitchMinDeg && PitchVel < 0f) PitchVel = 0f;
            float keep = Mathf.Exp(-HangarMotion.InertiaDamping * dt);
            YawVel *= keep; PitchVel *= keep;
            if (Mathf.Abs(YawVel) < HangarMotion.VelocityStop) YawVel = 0f;
            if (Mathf.Abs(PitchVel) < HangarMotion.VelocityStop) PitchVel = 0f;
        }

        void SnapRest() { YawDeg = PitchDeg = YawVel = PitchVel = 0f; Zoom = 1f; Reframing = false; ExplodeFraming = false; }

        /// <summary>Double-tap while exploded: front-on and close, so the separated parts fill the frame.</summary>
        void SnapExplodedFrame()
        {
            YawDeg = YawVel = PitchVel = 0f;
            PitchDeg = HangarMotion.ExplodedPitchDeg;
            Zoom = HangarMotion.ExplodedZoom;
            Reframing = false;
            ExplodeFraming = false;
        }

        /// <summary>World pose. Distance tracks vehicle height and the framing scale, then zoom.</summary>
        public static void Pose(float height, float distanceScale, float yawDeg, float pitchDeg, float zoom, out Vector3 position, out Vector3 aim)
            => PoseAt(height, distanceScale, yawDeg, pitchDeg, zoom, float.NaN, out position, out aim);

        /// <summary>Same pose, but aimed at <paramref name="aimY"/> metres up the axis (a focused part). NaN aims at the rest point.
        /// The camera keeps the rest offset, so focusing slides the view along the rocket rather than swinging it.</summary>
        public static void PoseAt(float height, float distanceScale, float yawDeg, float pitchDeg, float zoom, float aimY, out Vector3 position, out Vector3 aim)
        {
            height = DesignFraming.SafeHeight(height);
            if (!(distanceScale > 0.05f) || float.IsNaN(distanceScale) || float.IsInfinity(distanceScale)) distanceScale = 1f;
            zoom = ClampZoom(zoom);
            yawDeg = ClampYaw(yawDeg);
            pitchDeg = ClampPitch(pitchDeg);
            float restY = height * DesignFraming.AimHeight;
            aim = new Vector3(0f, float.IsNaN(aimY) || float.IsInfinity(aimY) ? restY : Mathf.Clamp(aimY, 0f, height * 1.2f), 0f);
            var rest = new Vector3(height * 0.9f, height * (0.45f - DesignFraming.AimHeight), -height * 1.4f) * (distanceScale / zoom);
            position = aim + Offset(rest, yawDeg, pitchDeg);
        }

        /// <summary>Yaw around +Y, then pitch about the camera's right so a positive pitch raises it.</summary>
        public static Vector3 Offset(Vector3 rest, float yawDeg, float pitchDeg)
        {
            float yaw = yawDeg * Mathf.Deg2Rad;
            float c = Mathf.Cos(yaw), s = Mathf.Sin(yaw);
            var v = new Vector3(rest.x * c + rest.z * s, rest.y, -rest.x * s + rest.z * c);
            if (Mathf.Abs(pitchDeg) < 1e-4f) return v;
            var right = Vector3.Cross(v, Vector3.up);
            float m = right.magnitude;
            if (m < 1e-6f) return v;
            right /= m;
            float rad = pitchDeg * Mathf.Deg2Rad;
            float cp = Mathf.Cos(rad), sp = Mathf.Sin(rad);
            var crossed = Vector3.Cross(right, v);
            float dot = Vector3.Dot(right, v);
            return v * cp + crossed * sp + right * (dot * (1f - cp));
        }
    }
}
