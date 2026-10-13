// Fairing seams, the base separation ring, the shoulder, and nose breaks. The lathe ogive stays
// the silhouette; these tubes sit just outside it. Height of the vehicle does not change.
using System;
using System.Collections.Generic;

namespace Kestrel.Vehicle
{
    public static class FairingLayout
    {
        public const string Ring = "fairing-ring";
        public const string Seam = "fairing-seam";

        /// <summary>Local radius of the nose at fraction t along it (0 at the shoulder, 1 at the tip).</summary>
        public static float SectionRadius(float radius, float t, bool conic)
        {
            if (t <= 0f) return radius;
            if (t >= 1f) return 0f;
            if (conic) return radius * (1f - t);
            return radius * (float)Math.Sqrt(Math.Max(0.0, 1.0 - t * t));
        }

        public static List<StrutPose> Build(float baseY, float radius, float length, float noseFraction, bool conic)
        {
            var list = new List<StrutPose>();
            if (!(radius > 0.05f) || !(length > 0.3f)) return list;
            float noseFrac = noseFraction < 0.05f ? 0.05f : noseFraction > 0.9f ? 0.9f : noseFraction;
            float cyl = length * (1f - noseFrac);
            float nose = length * noseFrac;
            const float proud = 0.03f;
            const float band = 0.055f;
            list.Add(Band(baseY + band * 0.5f, radius + proud, band));
            float shoulderY = baseY + cyl;
            list.Add(Band(shoulderY, radius + proud, band));
            if (cyl > 0.8f)
            {
                list.Add(Band(baseY + cyl * 0.33f, radius + proud, 0.028f));
                list.Add(Band(baseY + cyl * 0.66f, radius + proud, 0.028f));
            }
            float[] cuts = { 0.35f, 0.65f };
            for (int i = 0; i < cuts.Length; i++)
            {
                float rr = SectionRadius(radius, cuts[i], conic);
                if (rr < 0.08f) continue;
                list.Add(Band(shoulderY + nose * cuts[i], rr + proud, 0.028f));
            }
            for (int side = 0; side < 2; side++)
            {
                double a = side == 0 ? 0.35 : Math.PI + 0.35;
                float sa = (float)Math.Sin(a), ca = (float)Math.Cos(a);
                float r0 = radius + proud + 0.012f;
                Span(list, r0 * sa, baseY, r0 * ca, r0 * sa, shoulderY, r0 * ca, 0.012f);
                const float tip = 0.72f;
                float r1 = SectionRadius(radius, tip, conic) + proud;
                if (r1 > 0.06f)
                    Span(list, r0 * sa, shoulderY, r0 * ca, r1 * sa, shoulderY + nose * tip, r1 * ca, 0.012f);
            }
            return list;
        }

        static StrutPose Band(float y, float radius, float height)
        {
            return new StrutPose(Ring, 0f, y, 0f, radius, height, 0f, 1f, 0f);
        }

        static void Span(List<StrutPose> list, float x0, float y0, float z0, float x1, float y1, float z1, float radius)
        {
            float dx = x1 - x0, dy = y1 - y0, dz = z1 - z0;
            float len = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (len < 1e-3f) return;
            list.Add(new StrutPose(Seam, (x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f, radius, len, dx / len, dy / len, dz / len));
        }
    }
}
