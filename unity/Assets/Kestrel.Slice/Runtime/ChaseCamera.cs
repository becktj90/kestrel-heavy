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
        /// <summary>Desired camera offset from the vehicle pivot for a rocket of height h with axis 'up'.</summary>
        public static Vector3 DesiredOffset(Vector3 up, float h) => -up * h * 0.6f + new Vector3(h * 0.8f, 0, -h * 1.6f);

        /// <summary>Exponentially smooth the current offset toward the desired one (frame-rate independent).</summary>
        public static Vector3 StepOffset(Vector3 current, Vector3 desired, float rate, float dt) =>
            Vector3.Lerp(current, desired, 1 - Mathf.Exp(-rate * dt));
    }
}
