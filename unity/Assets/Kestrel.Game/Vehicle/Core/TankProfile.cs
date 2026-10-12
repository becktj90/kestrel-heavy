using System;
using System.Collections.Generic;

namespace Kestrel.Vehicle
{
    /// <summary>One point of the half-section: radius R at height Y, outward 2D normal (NR, NY), arc length V (m).
    /// Two consecutive points may share a position (a crease with two normals); LatheMesh emits no triangles there.</summary>
    public readonly struct ProfilePoint
    {
        public readonly float R, Y, NR, NY, V;
        public ProfilePoint(float r, float y, float nr, float ny, float v) { R = r; Y = y; NR = nr; NY = ny; V = v; }
    }

    public static class TankProfile
    {
        /// <summary>Aft end → barrel (a ring at every circumferential weld) → fwd end.
        /// Flat end: pole → rim disc exactly at the barrel end (nothing beyond it).
        /// Ellipsoidal end: the dome (radius DomeRadius &lt; Radius) meets the barrel through a flat inward lip at the
        /// barrel end, so no dome point ever exceeds the dome radius or the barrel (no bulge, no overshoot).</summary>
        public static List<ProfilePoint> Build(TankShellSpec s, TankLayout layout, TankLod lod)
        {
            float R = s.Radius, Rd = s.DomeRadius, h = s.DomeRise;
            int ds = Math.Max(2, lod.DomeSamples);
            var raw = new List<(float r, float y, float nr, float ny)>();

            void Dome(float yc, bool aft)
            {
                for (int i = 0; i <= ds; i++)
                {
                    double t = (double)i / ds;
                    double phi = aft ? Math.PI - t * Math.PI / 2 : Math.PI / 2 - t * Math.PI / 2;
                    float r = (float)(Rd * Math.Sin(phi)), y = (float)(yc + h * Math.Cos(phi));
                    if (i == (aft ? 0 : ds)) r = 0f;
                    if (i == (aft ? ds : 0)) { r = Rd; y = yc; }   // exact equator, no float overshoot
                    double nr = Math.Sin(phi) / Rd, ny = Math.Cos(phi) / h, nl = Math.Sqrt(nr * nr + ny * ny);
                    raw.Add((r, y, (float)(nr / nl), (float)(ny / nl)));
                }
            }

            float yb = s.BarrelBottom, yt = s.BarrelTop;
            // Aft end
            if (s.AftDome == DomeStyle.Flat) { raw.Add((0, yb, 0, -1)); raw.Add((R, yb, 0, -1)); }
            else { Dome(yb, aft: true); raw.Add((Rd, yb, 0, -1)); raw.Add((R, yb, 0, -1)); }   // dome, then lip out to the barrel
            raw.Add((R, yb, 1, 0));                                                                // crease into the barrel
            foreach (var w in layout.CircWelds)
                if (w.Y > yb + 1e-4f && w.Y < yt - 1e-4f) raw.Add((R, w.Y, 1, 0));
            if (yt > yb + 1e-4f) raw.Add((R, yt, 1, 0));
            // Fwd end
            raw.Add((R, yt, 0, 1));
            if (s.FwdDome == DomeStyle.Flat) raw.Add((0, yt, 0, 1));
            else { raw.Add((Rd, yt, 0, 1)); Dome(yt, aft: false); }

            var pts = new List<ProfilePoint>(raw.Count);
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
