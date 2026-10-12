using System;
using System.Collections.Generic;

namespace Kestrel.Vehicle
{
    /// <summary>Equilateral-triangle isogrid. Normal views use a tiling normal map generated from Height();
    /// real rib geometry is only built for cutaway and the Detail Lab (approved direction, audit rec 7).</summary>
    public static class Isogrid
    {
        static readonly double S60 = Math.Sqrt(3) / 2;

        /// <summary>Row spacing of an isogrid with edge length cell.</summary>
        public static double RowSpacing(double cell) => cell * S60;

        /// <summary>Distance (same units as u, v) to the nearest rib centre line of the three line families.</summary>
        public static double RibDistance(double u, double v, double cell)
        {
            double s = RowSpacing(cell), best = double.MaxValue;
            // Line normals for the 0, 60 and 120 degree families.
            double[,] n = { { 0, 1 }, { -S60, 0.5 }, { S60, 0.5 } };
            for (int k = 0; k < 3; k++)
            {
                double d = u * n[k, 0] + v * n[k, 1];
                double m = d - Math.Floor(d / s) * s;
                best = Math.Min(best, Math.Min(m, s - m));
            }
            return best;
        }

        /// <summary>Rib height 0..1: 1 on a rib, 0 in a pocket, smooth bevel of width bevel.</summary>
        public static double Height(double u, double v, double cell, double ribWidth, double bevel)
        {
            double d = RibDistance(u, v, cell), a = ribWidth * 0.5, b = a + Math.Max(1e-6, bevel);
            if (d <= a) return 1; if (d >= b) return 0;
            double t = (d - a) / (b - a); return 1 - t * t * (3 - 2 * t);
        }

        /// <summary>One tile of the pattern: width = cell, height = 2 * row spacing (sqrt 3 * cell). Tiles seamlessly.
        /// Returns RGBA32 bytes for a linear normal map encoded so both RGB (mobile) and AG (desktop) unpacking work:
        /// R = x, G = y, B = z, A = 1 (AG path computes a *= r).</summary>
        public static byte[] NormalTileRgba(int width, int height, double ribFraction = 0.12, double bevelFraction = 0.06, double strength = 2.0)
        {
            const double cell = 1.0; double tileH = 2 * RowSpacing(cell);
            var px = new byte[width * height * 4];
            double H(int x, int y)
            {
                x = ((x % width) + width) % width; y = ((y % height) + height) % height;
                return Height((x + 0.5) / width * cell, (y + 0.5) / height * tileH, cell, ribFraction, bevelFraction);
            }
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                double dx = (H(x + 1, y) - H(x - 1, y)) * 0.5 * strength, dy = (H(x, y + 1) - H(x, y - 1)) * 0.5 * strength;
                double nx = -dx, ny = -dy, nz = 1, l = Math.Sqrt(nx * nx + ny * ny + nz * nz);
                int o = (y * width + x) * 4;
                px[o] = Enc(nx / l); px[o + 1] = Enc(ny / l); px[o + 2] = Enc(nz / l); px[o + 3] = 255;
            }
            return px;
        }
        static byte Enc(double c) => (byte)Math.Max(0, Math.Min(255, Math.Round((c * 0.5 + 0.5) * 255)));

        /// <summary>Rib edges (in barrel surface coords: u metres around from azimuth a0, v metres up from y0) for
        /// a cutaway sector. Each edge has length cell. Returns false (and an empty list) if over maxEdges.</summary>
        public static bool CutawayEdges(double width, double height, double cell, int maxEdges, List<(double u0, double v0, double u1, double v1)> edges)
        {
            edges.Clear();
            double s = RowSpacing(cell);
            int rows = (int)Math.Floor(height / s + 1e-9), cols = (int)Math.Floor(width / cell + 1e-9);
            (double, double) Node(int i, int j) => (i * cell + ((j & 1) == 1 ? cell / 2 : 0), j * s);
            bool In((double u, double v) p) => p.u >= -1e-9 && p.u <= width + 1e-9 && p.v >= -1e-9 && p.v <= height + 1e-9;
            void Try((double u, double v) a, (double u, double v) b)
            { if (In(a) && In(b)) edges.Add((a.u, a.v, b.u, b.v)); }
            for (int j = 0; j <= rows; j++)
                for (int i = -1; i <= cols + 1; i++)
                {
                    var a = Node(i, j);
                    Try(a, Node(i + 1, j));
                    if (j < rows)
                    {
                        int ul = (j & 1) == 0 ? i - 1 : i, ur = ul + 1;
                        Try(a, Node(ul, j + 1)); Try(a, Node(ur, j + 1));
                    }
                    if (edges.Count > maxEdges) { edges.Clear(); return false; }
                }
            return true;
        }
    }
}
