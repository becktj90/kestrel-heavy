using System;
using System.Collections.Generic;

namespace Kestrel.Vehicle
{
    /// <summary>A circumferential feature (weld bead or stiffener ring) at height Y with ring radius R.</summary>
    public readonly struct Ring { public readonly float Y, R; public Ring(float y, float r) { Y = y; R = r; } }

    /// <summary>A longitudinal weld from Y0 to Y1 at an azimuth (radians, 0 = +Z, toward +X).</summary>
    public readonly struct Seam { public readonly float Azimuth, Y0, Y1; public Seam(float a, float y0, float y1) { Azimuth = a; Y0 = y0; Y1 = y1; } }

    /// <summary>Where welds and stiffeners go. Pure function of the spec (seeded), shared by every LOD.</summary>
    public sealed class TankLayout
    {
        public readonly List<Ring> CircWelds = new List<Ring>();    // barrel joints incl. both dome-to-barrel joints
        public readonly List<Ring> DomeWelds = new List<Ring>();    // one cap ring per dome
        public readonly List<Seam> LongWelds = new List<Seam>();
        public readonly List<Ring> Stiffeners = new List<Ring>();
        public int BarrelSections { get; private set; }

        public const float DomeCapRadiusFraction = 0.35f;

        /// <summary>Hero cap: LOD0 triangles per tank (shell + welds + rings). Build() widens stiffener pitch, then panel
        /// length, until the tank fits, so long tanks keep their seams and some rings instead of blowing the budget.</summary>
        public const int Lod0TriangleCap = 14_000;

        /// <summary>Spec actually used after budget fitting (StiffenerPitch / PanelLength may be wider than requested).</summary>
        public TankShellSpec Fitted { get; private set; }

        public static TankLayout Build(TankShellSpec s)
        {
            var errs = s.Validate();
            if (errs.Count > 0) throw new ArgumentException(string.Join("; ", errs));
            var L = BuildRaw(s);
            for (int guard = 0; guard < 40 && TankBudget.Triangles(L, TankLod.For(0), out _, out _) > Lod0TriangleCap; guard++)
            {
                // Thin rings down to one per panel first, then lengthen panels (fewer seams and rings); never zero rings.
                if (L.Stiffeners.Count > L.BarrelSections) s.StiffenerPitch *= 1.25f;
                else { s.PanelLength *= 1.25f; s.StiffenerPitch = Math.Min(s.StiffenerPitch, s.PanelLength * 0.45f); }
                L = BuildRaw(s);
            }
            return L;
        }

        static TankLayout BuildRaw(TankShellSpec s)
        {
            var L = new TankLayout { Fitted = s };
            float R = s.Radius, y0 = s.BarrelBottom, len = s.BarrelLength;
            int n = len <= 1e-4f ? 0 : Math.Max(1, (int)Math.Ceiling(len / s.PanelLength - 1e-6));
            L.BarrelSections = n;

            if (n == 0) L.CircWelds.Add(new Ring(y0, R));   // equator weld of a two-dome tank
            else for (int k = 0; k <= n; k++) L.CircWelds.Add(new Ring(y0 + len * k / n, R));

            // Dome cap welds (ellipsoidal ends only) where the dome radius is DomeCapRadiusFraction of the dome radius.
            float f = DomeCapRadiusFraction, dy = s.DomeRise * (float)Math.Sqrt(1 - f * f);
            if (s.AftDome == DomeStyle.Ellipsoidal) L.DomeWelds.Add(new Ring(s.BarrelBottom - dy, f * s.DomeRadius));
            if (s.FwdDome == DomeStyle.Ellipsoidal) L.DomeWelds.Add(new Ring(s.BarrelTop + dy, f * s.DomeRadius));

            if (n > 0)
            {
                int m = Math.Max(1, (int)Math.Ceiling(2 * Math.PI * R / s.MaxPanelWidth - 1e-6));
                float step = (float)(2 * Math.PI / m);
                float sec = len / n;
                for (int k = 0; k < n; k++)
                {
                    // Staggered: each section's seams start at a seeded offset so they never line up section to section.
                    float off = (float)(VehicleRng.Unit(s.Seed, 0x5EA0UL + (ulong)k) * step);
                    for (int j = 0; j < m; j++)
                        L.LongWelds.Add(new Seam(off + j * step, y0 + sec * k, y0 + sec * (k + 1)));

                    int c = (int)Math.Floor((sec - 2 * s.WeldClearance) / s.StiffenerPitch);
                    if (c > 0 && sec / (c + 1) >= s.WeldClearance)
                        for (int i = 0; i < c; i++) L.Stiffeners.Add(new Ring(y0 + sec * k + sec * (i + 1) / (c + 1), R));
                }
            }
            return L;
        }
    }
}
