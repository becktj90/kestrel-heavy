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

        /// <summary>Exponentially smooth the current offset toward the desired one (frame-rate independent).</summary>
        public static Vector3 StepOffset(Vector3 current, Vector3 desired, float rate, float dt) =>
            Vector3.Lerp(current, desired, 1 - Mathf.Exp(-rate * dt));
    }
}
