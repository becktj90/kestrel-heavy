using UnityEngine;

namespace Kestrel.Slice
{
    /// <summary>Manual orbit layered on the chase offset. Follow leaves that offset untouched.</summary>
    public struct FlightRig
    {
        public bool Follow;
        public float Yaw;
        public float Pitch;
        public float Zoom;
        public Vector2 Origin;
        public bool Moved;
        public bool Active;
    }

    public static class FlightCameraRig
    {
        public const float ZoomMin = 0.6f;
        public const float ZoomMax = 2.4f;
        public const float PitchMin = -28f;
        public const float PitchMax = 48f;
        public const float YawLimit = 180f;
        public const float SlopPx = 14f;
        public const float YawPerPx = 0.18f;
        public const float PitchPerPx = 0.14f;
        /// <summary>Smallest orbit that reads on screen (deg of yaw or pitch, or zoom off 1 by this much). Below it
        /// the camera is still following: a drag just past the slop no longer leaves "Follow rocket" up with a
        /// Follow tap that changes nothing visible (#348, t463 T+16).</summary>
        public const float VisibleDeg = 6f;
        public const float VisibleZoom = 0.08f;

        /// <summary>The manual orbit moves the view visibly off the chase.</summary>
        public static bool Orbited(FlightRig rig)
        {
            if (rig.Follow) return false;
            float zoom = rig.Zoom > 0f ? rig.Zoom : 1f;
            return Mathf.Abs(rig.Yaw) >= VisibleDeg || Mathf.Abs(rig.Pitch) >= VisibleDeg || Mathf.Abs(zoom - 1f) >= VisibleZoom;
        }

        public static FlightRig Following() => new FlightRig { Follow = true, Zoom = 1f };

        public static FlightRig Begin(FlightRig rig, Vector2 pos)
        {
            rig.Active = true;
            rig.Origin = pos;
            rig.Moved = false;
            return rig;
        }

        public static FlightRig Drag(FlightRig rig, Vector2 pos)
        {
            var delta = pos - rig.Origin;
            if (!rig.Moved && delta.magnitude < SlopPx) return rig;
            rig.Moved = true;
            rig.Follow = false;
            rig.Yaw = Clamp(rig.Yaw + (pos.x - rig.Origin.x) * YawPerPx, -YawLimit, YawLimit);
            rig.Pitch = Clamp(rig.Pitch - (pos.y - rig.Origin.y) * PitchPerPx, PitchMin, PitchMax);
            rig.Origin = pos;
            return rig;
        }

        /// <summary>A lift with no real drag is a tap and resumes chase follow.</summary>
        public static FlightRig End(FlightRig rig, Vector2 pos)
        {
            rig = Drag(rig, pos);
            bool tap = !rig.Moved;
            rig.Active = false;
            // A tap, or a drag too small to read, hands the view back (SliceApp eases the orbit out, no cut).
            return tap || !Orbited(rig) ? Recenter(rig) : rig;
        }

        public static FlightRig Pinch(FlightRig rig, float scale)
        {
            if (!(scale > 0f) || float.IsNaN(scale) || float.IsInfinity(scale)) return rig;
            rig.Follow = false;
            rig.Moved = true;
            float zoom = rig.Zoom > 0f ? rig.Zoom : 1f;
            rig.Zoom = Mathf.Clamp(zoom * scale, ZoomMin, ZoomMax);
            return rig;
        }

        public static FlightRig Recenter(FlightRig rig)
        {
            rig.Follow = true;
            rig.Yaw = 0f;
            rig.Pitch = 0f;
            rig.Zoom = 1f;
            rig.Active = false;
            rig.Moved = false;
            return rig;
        }

        /// <summary>Chase offset, or that offset orbited and zoomed. Follow returns the chase offset unchanged.</summary>
        public static Vector3 Orbit(Vector3 chase, FlightRig rig)
        {
            if (rig.Follow || chase.sqrMagnitude < 1e-8f) return chase;
            float zoom = Mathf.Clamp(rig.Zoom > 0f ? rig.Zoom : 1f, ZoomMin, ZoomMax);
            return Quaternion.Euler(rig.Pitch, rig.Yaw, 0f) * chase / zoom;
        }

        static float Clamp(float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return 0f;
            return Mathf.Clamp(value, min, max);
        }
    }
}
