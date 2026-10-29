// Chase-camera smoothing in VEHICLE-RELATIVE space.
//
// The previous version lerped the camera's absolute world position toward a target next to the
// vehicle. That breaks with the floating origin in FlightDriver: every rebase (each 2 km) teleports the
// vehicle back near (0,0) but left the camera where it was, so after T+~25 s the camera sits up to
// ~2 km from the rocket looking down at the 4 km ground plane far below (the "flat dark quad" at max-Q
// is that ground plane, with the pad/rocket as the grey dot). Even without rebases, an absolute lerp
// with rate k lags a vehicle moving at v by ~v/k (806 m/s / 4 = ~200 m at T+46 s).
//
// Smoothing only the OFFSET from the vehicle makes the camera immune to both: rebases and vehicle
// speed cancel out, only changes in the desired framing (pitch-over) are smoothed.
using UnityEngine;

namespace Kestrel.Slice
{
    public static class ChaseCamera
    {
        // Lateral standoff, in vehicle heights. Was (0.8, 1.6), which filled a portrait frame with the
        // tank wall once the HUD covered the nose and the tail. 2.5× that distance keeps the whole
        // stack in the middle of the frame with sky around it. Follow still returns this offset unchanged.
        public const float StandoffX = 2.0f;
        public const float StandoffZ = 4.0f;

        /// <summary>Desired camera offset from the vehicle pivot for a rocket of height h with axis 'up'.</summary>
        public static Vector3 DesiredOffset(Vector3 up, float h) => -up * h * 0.6f + new Vector3(h * StandoffX, 0, -h * StandoffZ);

        // ---- Early-ascent horizon hold (P0-1 follow-up) ---------------------------------------------------
        // Root cause of "frame is all sky by T+2.2 s (iPhone) / T+3.7 s (iPad)": the offset above puts the
        // camera 0.6 h BELOW the pivot and LookAt aims at +0.5 h, i.e. ~32 deg nose-up. Vertical half-FOV is
        // 25 deg, so as soon as the vehicle clears the ground clamp the horizon drops below the bottom edge.
        // Fix: during early ascent the camera rides level with the pivot (smaller look-up angle) and the pitch
        // is clamped so the horizon sits in the lower HorizonMin..HorizonMax of the portrait frame; the hold
        // blends out between HoldFullAlt and HoldEndAlt.
        public const float HorizonMin = 0.15f, HorizonMax = 0.35f;     // viewport y (0 = bottom)
        // Held through the 13 km chase view. Above that the nose-up offset blends back in.
        public const float HoldFullAlt = 13000f, HoldEndAlt = 20000f;   // m
        public const float EarthRadius = 6371000f;

        /// <summary>1 while the horizon hold is fully active, 0 once blended out (smoothstep).</summary>
        public static float HoldWeight(float altitude) => (float)Kestrel.Core.Flight.FlightViewPose.HoldWeight(altitude);

        /// <summary>Altitude-aware offset: level with the pivot while the hold is active.</summary>
        public static Vector3 DesiredOffset(Vector3 up, float h, float altitude) =>
            -up * h * Mathf.Lerp(0.6f, 0.05f, HoldWeight(altitude)) + new Vector3(h * StandoffX, 0, -h * StandoffZ);

        /// <summary>Angle (rad) of the visible horizon below the local horizontal at a camera altitude.</summary>
        public static float HorizonDip(float camAltitude) =>
            Mathf.Acos(Mathf.Clamp(EarthRadius / (EarthRadius + Mathf.Max(0f, camAltitude)), -1f, 1f));

        /// <summary>Viewport y of the horizon for a roll-free camera with this pitch (rad, + = nose up).</summary>
        public static float HorizonViewportY(float pitch, float vFovDeg, float camAltitude) =>
            0.5f - Mathf.Tan(pitch + HorizonDip(camAltitude)) / (2f * Mathf.Tan(0.5f * vFovDeg * Mathf.Deg2Rad));

        /// <summary>Camera rotation looking at target, pitch clamped so the horizon stays in the lower band.</summary>
        public static Quaternion Aim(Vector3 camPos, Vector3 target, float vFovDeg, float altitude, float camAltitude)
        {
            var d = target - camPos;
            float horiz = new Vector2(d.x, d.z).magnitude;
            float pitch = Mathf.Atan2(d.y, Mathf.Max(1e-4f, horiz));
            float w = HoldWeight(altitude);
            if (w > 0f)
            {
                float t2 = 2f * Mathf.Tan(0.5f * vFovDeg * Mathf.Deg2Rad), dip = HorizonDip(camAltitude);
                // y = 0.5 - tan(pitch + dip) / t2  =>  pitch = atan((0.5 - y) * t2) - dip
                float pMin = Mathf.Atan((0.5f - HorizonMax) * t2) - dip;
                float pMax = Mathf.Atan((0.5f - HorizonMin) * t2) - dip;
                pitch = Mathf.Lerp(pitch, Mathf.Clamp(pitch, pMin, pMax), w);
            }
            float yaw = Mathf.Atan2(d.x, d.z);
            return Quaternion.Euler(-pitch * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg, 0f);
        }

        /// <summary>Clear of the HUD's top block: the nose sits this far (viewport) under the strip's bottom edge.</summary>
        public const float HudMargin = 0.03f;

        /// <summary>
        /// Raise a roll-free camera's pitch just enough that <paramref name="point"/> (the stack's nose) projects at or below
        /// viewport y <paramref name="maxY"/> (0 = bottom). The horizon hold above only bounds where the horizon sits, so on a
        /// 4:3 screen it could leave the nose under the telemetry strip (#381 iPad flight frame at T+60). The pitch never
        /// rises past the point where <paramref name="floorPoint"/> (the stack's base) would drop under viewport y
        /// <paramref name="minY"/>. maxY outside (0.5, 1) leaves the rotation unchanged.
        /// </summary>
        public static Quaternion KeepBelow(Quaternion rot, Vector3 camPos, Vector3 point, Vector3 floorPoint, float vFovDeg, float maxY, float minY = 0.05f)
        {
            if (!(maxY > 0.5f && maxY < 1f)) return rot;
            var fwd = rot * Vector3.forward;
            var flat = new Vector3(fwd.x, 0f, fwd.z);
            if (flat.sqrMagnitude < 1e-8f) return rot;
            flat.Normalize();
            float pitch = Mathf.Asin(Mathf.Clamp(fwd.y, -1f, 1f));
            float t2 = 2f * Mathf.Tan(0.5f * vFovDeg * Mathf.Deg2Rad);
            float Elevation(Vector3 p)
            {
                var d = p - camPos;
                return Mathf.Atan2(d.y, Mathf.Max(1e-4f, Vector3.Dot(d, flat)));
            }
            // y = 0.5 + tan(e - pitch) / t2 for a point in the camera's vertical plane.
            float need = Elevation(point) - Mathf.Atan((maxY - 0.5f) * t2);
            if (need <= pitch) return rot;
            float cap = Elevation(floorPoint) - Mathf.Atan((Mathf.Clamp(minY, 0f, 0.49f) - 0.5f) * t2);
            float raised = Mathf.Max(pitch, Mathf.Min(need, cap));
            float yaw = Mathf.Atan2(flat.x, flat.z);
            return Quaternion.Euler(-raised * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg, 0f);
        }

        /// <summary>
        /// Move a camera along its own back and up axes, rotation unchanged (so a held horizon stays put), just far enough
        /// that every point projects inside the viewport band y in [<paramref name="yMin"/>, <paramref name="yMax"/>] and
        /// |x - 0.5| &lt;= <paramref name="xHalf"/>. It only ever backs away (widens), never closes in, and moves up or down
        /// as little as it can. The booster follow used to centre the booster with no HUD inset, so its engines, flame
        /// and the deck sat behind the bottom control row (#381 landing frames; worst on a 4:3 iPad). Returns
        /// <paramref name="camPos"/> unchanged for an empty or degenerate band.
        /// </summary>
        public static Vector3 FitBand(Quaternion rot, Vector3 camPos, Vector3[] points, int count, float vFovDeg, float aspect,
            float yMin, float yMax, float xHalf = 0.45f)
        {
            if (points == null || count <= 0 || !(yMax - yMin > 0.1f) || !(aspect > 0.05f) || !(vFovDeg > 1f)) return camPos;
            if (count > points.Length) count = points.Length;
            var f = rot * Vector3.forward; var u = rot * Vector3.up; var r = rot * Vector3.right;
            float t2 = 2f * Mathf.Tan(0.5f * vFovDeg * Mathf.Deg2Rad);
            float a = (yMax - 0.5f) * t2, b = (yMin - 0.5f) * t2, h = xHalf * t2 * aspect;
            // Shifting the camera by -f*dz + u*dy maps a point (x, y, z) in camera space to (x, y - dy, z + dz).
            // y - dy <= a (z + dz) and y - dy >= b (z + dz) for all points  <=>  dz (a - b) >= max(y - a z) - min(y - b z).
            float hiA = float.NegativeInfinity, loB = float.PositiveInfinity, dz = 0f;
            for (int i = 0; i < count; i++)
            {
                var d = points[i] - camPos;
                float x = Vector3.Dot(d, r), y = Vector3.Dot(d, u), z = Vector3.Dot(d, f);
                if (float.IsNaN(x + y + z)) return camPos;
                hiA = Mathf.Max(hiA, y - a * z);
                loB = Mathf.Min(loB, y - b * z);
                if (h > 1e-4f) dz = Mathf.Max(dz, Mathf.Abs(x) / h - z);
            }
            dz = Mathf.Max(dz, (hiA - loB) / (a - b));
            if (!(dz < 1e5f)) return camPos;
            // dy in [hiA - a dz, loB - b dz]: the smallest move up or down that fits.
            float lo = hiA - a * dz, hi = loB - b * dz;
            float dy = lo > 0f ? lo : hi < 0f ? hi : 0f;
            return camPos - f * dz + u * dy;
        }

        /// <summary>Exponentially smooth the current offset toward the desired one (frame-rate independent).</summary>
        public static Vector3 StepOffset(Vector3 current, Vector3 desired, float rate, float dt) =>
            Vector3.Lerp(current, desired, 1 - Mathf.Exp(-rate * dt));
    }
}
