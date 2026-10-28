// The pad art a designer or pad camera must keep in frame (TestFlight 208, KH-1 First Hop: "no yellow crown, no base
// plates, no pad circle"): the tower crown, the yellow base plates under the legs and the painted pad circle, as
// plain points. Engine-free. The Starter preview camera (StarterPreviewFraming, Kestrel.Core) frames these and the
// EditMode/dotnet frame test checks the same points, so the camera and the test share one definition.
// Pad placement (PadTower.Fit) also lives here so the yaw that points the pad's +Z at the tower is one formula.
using System;
using System.Collections.Generic;

namespace Kestrel.Pad
{
    /// <summary>Which pad feature a <see cref="ShowPoint"/> belongs to. Values match Kestrel.Core.Designer.PadFeature.</summary>
    public enum ShowKind { TowerTop = 0, BasePlate = 1, PadCircle = 2 }

    public struct ShowPoint
    {
        public ShowKind Kind; public float X, Y, Z;
        public ShowPoint(ShowKind kind, float x, float y, float z) { Kind = kind; X = x; Y = y; Z = z; }
    }

    public static class PadShowcase
    {
        /// <summary>Tower stand-off from the vehicle skin (PadTower.Fit): mast width, clearance, how far behind.</summary>
        public const float MastWidth = 4f, Clearance = 6f, Behind = 6f, Overtop = 1.08f;
        /// <summary>Samples around the pad circle.</summary>
        public const int CircleSamples = 24;
        /// <summary>Half the depth of a base plate (the beam's flange; the plate runs the member's length, 2.2 m in TowerLayout.BuildDetail).</summary>
        public const float PlateHalfDepth = 0.45f;

        /// <summary>Tower direction (x, z) and minimum height for a rocket of height h and max half-width r (metres).</summary>
        public static (float x, float z, float height) Fit(float h, float r) =>
            (-(r + Clearance + MastWidth * 0.5f), Behind, Math.Max(20f, h * Overtop));

        /// <summary>Degrees about +Y that point the pad's +Z at <see cref="Fit"/> (Unity: local +Z maps to (sin yaw, cos yaw)).</summary>
        public static float YawDeg(float h, float r)
        {
            var (x, z, _) = Fit(h, r);
            return (float)(Math.Atan2(x, z) * 180.0 / Math.PI);
        }

        /// <summary>Radius of the painted pad circle (TrenchMeshBuilder.PadCircle): <see cref="TrenchMeshBuilder.CircleFactor"/> x the detail radius.</summary>
        public static float CircleRadius(PadLayout L) =>
            Math.Min(Math.Max(8f, TrenchGrammar.SurfaceRadius(L)), TrenchMeshBuilder.DetailRadiusCap) * TrenchMeshBuilder.CircleFactor;

        /// <summary>Crown points (cap posts, ring corners and the spike tip), base plate corners and the pad circle, in the
        /// pad's own frame (+Z toward the tower). Empty when the pad has no tower.</summary>
        public static List<ShowPoint> Points(PadLayout L)
        {
            var pts = new List<ShowPoint>(64);
            if (L == null) return pts;
            foreach (var m in L.Members)
            {
                if (m.Id == null) continue;
                if (m.Id.StartsWith("tower.cap.", StringComparison.Ordinal))
                {
                    pts.Add(new ShowPoint(ShowKind.TowerTop, m.A.X, m.A.Y, m.A.Z));
                    pts.Add(new ShowPoint(ShowKind.TowerTop, m.B.X, m.B.Y, m.B.Z));
                }
                else if (m.Id.StartsWith("tower.base.plate", StringComparison.Ordinal))
                {
                    // The plate is the member's beam widened by the flange depth: both ends, both sides.
                    foreach (var e in new[] { m.A, m.B })
                        foreach (float dz in new[] { -PlateHalfDepth, PlateHalfDepth })
                            pts.Add(new ShowPoint(ShowKind.BasePlate, e.X, e.Y, e.Z + dz));
                }
            }
            float r = CircleRadius(L);
            for (int i = 0; i < CircleSamples; i++)
            {
                double a = 2.0 * Math.PI * i / CircleSamples;
                pts.Add(new ShowPoint(ShowKind.PadCircle, r * (float)Math.Cos(a), TrenchMeshBuilder.CircleY, r * (float)Math.Sin(a)));
            }
            return pts;
        }
    }
}
