using System;
using System.Collections.Generic;

namespace Kestrel.Vehicle
{
    /// <summary>One point of the half-section: radius R at height Y, outward 2D normal (NR, NY), arc length V (m).</summary>
    public readonly struct ProfilePoint
    {
        public readonly float R, Y, NR, NY, V;
        public ProfilePoint(float r, float y, float nr, float ny, float v) { R = r; Y = y; NR = nr; NY = ny; V = v; }
    }

    public static class TankProfile
    {
        /// <summary>Aft pole → aft dome → barrel (rings at each circumferential weld) → fwd dome → fwd pole.
        /// Ellipsoidal domes; poles have R = 0 exactly.</summary>
        public static List<ProfilePoint> Build(TankShellSpec s, TankLayout layout, TankLod lod)
        {
            float R = s.Radius, h = s.DomeHeight;
            int ds = Math.Max(2, lod.DomeSamples);
            var raw = new List<(float r, float y, float nr, float ny)>();
            void Dome(float yc, bool aft)
            {
                for (int i = 0; i <= ds; i++)
                {
                    // phi: aft from pi (pole) to pi/2 (equator); fwd from pi/2 to 0 (pole).
                    double t = (double)i / ds;
                    double phi = aft ? Math.PI - t * Math.PI / 2 : Math.PI / 2 - t * Math.PI / 2;
                    float r = (float)(R * Math.Sin(phi)), y = (float)(yc + h * Math.Cos(phi));
                    if (i == (aft ? 0 : ds)) r = 0f;
                    double nr = Math.Sin(phi) / R, ny = Math.Cos(phi) / h, nl = Math.Sqrt(nr * nr + ny * ny);
                    raw.Add((r, y, (float)(nr / nl), (float)(ny / nl)));
                }
            }
            Dome(s.BarrelBottom, aft: true);
            // Barrel: interior circumferential weld heights become rings (equator already emitted by the domes).
            foreach (var w in layout.CircWelds)
                if (w.Y > s.BarrelBottom + 1e-4f && w.Y < s.BarrelTop - 1e-4f) raw.Add((R, w.Y, 1, 0));
            var fwd = raw.Count;
            Dome(s.BarrelTop, aft: false);
            if (s.BarrelLength <= 1e-4f) raw.RemoveAt(fwd);   // two-dome tank: the equator is shared

            var pts = new List<ProfilePoint>(raw.Count + 1);
            float v = 0;
            for (int i = 0; i < raw.Count; i++)
            {
                var p = raw[i];
                if (i > 0) { var q = raw[i - 1]; float dr = p.r - q.r, dy = p.y - q.y; v += (float)Math.Sqrt(dr * dr + dy * dy); }
                pts.Add(new ProfilePoint(p.r, p.y, p.nr, p.ny, v));
            }
            return pts;
        }
    }
}
