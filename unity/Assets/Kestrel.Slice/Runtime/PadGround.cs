// Ground, deck and horizon for the slice pad (P0 fix 1: "pad deck floats in blue sky after liftoff").
//
// Root causes (main b514d53):
//  * ChaseCamera.DesiredOffset puts the camera at -up * 0.6 h, i.e. ~42 m BELOW the vehicle pivot, which
//    at liftoff is the deck top (y = 0). The ground is Unity's single-sided Plane primitive (SliceApp.cs:41),
//    so from below it is back-face culled: no ground, no horizon, only the solid-colour sky clear
//    (SliceApp.cs:35), and we see the dark underside of the deck cylinder.
//  * The ground plane was only 4 km square (SliceApp.cs:42) and anchored at pad x = 0 (SliceApp.cs:44), so
//    once the vehicle is a few km up / downrange the plane shrinks to a dot and there is no horizon.
//  * The deck was a fixed 15 m radius disc (SliceApp.cs:46) while the D1 tower spans z = StandOff +/- Width/2
//    (TowerLayout.cs:27,61-62), ~20 m from the axis: the back legs stood off the deck, and with the ground
//    culled they hung into the sky. Ground (y = 0) was also coplanar with the deck top (z-fighting).
using Kestrel.Pad;
using UnityEngine;

namespace Kestrel.Slice
{
    public static class PadGround
    {
        /// <summary>Deck is a plinth from GroundY up to 0 (vehicle pivot / tower base); ground sits under it.</summary>
        public const float GroundY = -1.0f;
        /// <summary>Ground plane edge length (m). Unity's Plane primitive is 10 m, so scale = Size / 10.</summary>
        public const float Size = 200000f;
        public const float DeckMargin = 2f, MinDeckRadius = 15f;
        /// <summary>Camera must stay this far above the ground so the single-sided plane is always seen from above.</summary>
        public const float MinCameraClearance = 3f;
        public static readonly Color GroundColor = new Color(0.30f, 0.36f, 0.28f);
        public static readonly Color HazeColor = new Color(0.66f, 0.76f, 0.86f);

        /// <summary>Deck radius covering the generated layout's deck and every member's footprint (pad-local XZ).</summary>
        public static float DeckRadius(PadLayout L)
        {
            float r = MinDeckRadius;
            if (L == null) return r;
            r = Mathf.Max(r, L.DeckRadius);
            foreach (var m in L.Members)
            {
                if (Mathf.Min(m.A.Y, m.B.Y) > 0.5f) continue;   // only members that touch the deck
                r = Mathf.Max(r, Mathf.Sqrt(m.A.X * m.A.X + m.A.Z * m.A.Z) + DeckMargin);
                r = Mathf.Max(r, Mathf.Sqrt(m.B.X * m.B.X + m.B.Z * m.B.Z) + DeckMargin);
            }
            return r;
        }

        /// <summary>Keep the camera above the ground plane (world y of the ground = groundWorldY).</summary>
        public static Vector3 ClampAboveGround(Vector3 cam, float groundWorldY) => ClampAboveGround(cam, groundWorldY, MinCameraClearance);

        /// <summary>Same with the pose's own floor (FlightPose.Clearance): the Below pad hold sits LowClearance
        /// over the ground anchor, under the deck top, still above the apron (Coastline.Relief keeps it under GroundY).</summary>
        public static Vector3 ClampAboveGround(Vector3 cam, float groundWorldY, float clearance)
        {
            float c = Mathf.Max(LowCameraClearance, clearance);
            return cam.y < groundWorldY + c ? new Vector3(cam.x, groundWorldY + c, cam.z) : cam;
        }

        /// <summary>Lowest floor any pose may ask for (FlightViewPose.LowClearance).</summary>
        public const float LowCameraClearance = 0.6f;

        /// <summary>Linear haze so the ground's far edge melts into the sky (a horizon) instead of a hard line.</summary>
        public static void ApplyHaze(float farClip)
        {
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = HazeColor;
            RenderSettings.fogStartDistance = farClip * 0.08f; RenderSettings.fogEndDistance = farClip * 0.5f;
        }
    }
}
