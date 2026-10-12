// Sagging cable and hose bundles between a tower anchor (or arm tip) and a vehicle plate.
// The curve is the kit catenary; strand count, slack and colour are generic and fictional.
using Kestrel.Kit;
using Kestrel.Kit.Contracts;

namespace Kestrel.Pad
{
    public struct HoseStrand
    {
        public float Radius, Slack, Lateral;
        public MaterialRole Material;
    }

    public static class HoseBundle
    {
        public static HoseStrand[] For(UmbilicalType t) => t switch
        {
            UmbilicalType.Fuel => new[] { S(0.055f, 1.12f, 0f, MaterialRole.Insulation), S(0.045f, 1.16f, 0.14f, MaterialRole.Insulation), S(0.028f, 1.22f, -0.12f, MaterialRole.RubberHose) },
            UmbilicalType.Oxidizer => new[] { S(0.060f, 1.10f, 0.06f, MaterialRole.Insulation), S(0.050f, 1.15f, -0.10f, MaterialRole.Insulation) },
            UmbilicalType.PowerData => new[] { S(0.020f, 1.18f, -0.08f, MaterialRole.RubberHose), S(0.020f, 1.22f, 0f, MaterialRole.RubberHose), S(0.020f, 1.26f, 0.08f, MaterialRole.RubberHose) },
            UmbilicalType.Hydraulic => new[] { S(0.030f, 1.14f, -0.06f, MaterialRole.RubberHose), S(0.030f, 1.18f, 0.06f, MaterialRole.RubberHose) },
            _ => new[] { S(0.035f, 1.20f, 0f, MaterialRole.RubberHose) },
        };

        static HoseStrand S(float radius, float slack, float lateral, MaterialRole mat) =>
            new HoseStrand { Radius = radius, Slack = slack, Lateral = lateral, Material = mat };

        /// <summary>n+1 points of one strand. <paramref name="lateral"/> shifts the whole strand sideways so a bundle reads as several lines.</summary>
        public static void Sample(Vec3 a, Vec3 b, float slack, float lateral, int n, Vec3[] dst)
        {
            if (n < 1) throw new System.ArgumentOutOfRangeException(nameof(n));
            float dx = b.X - a.X, dz = b.Z - a.Z, w = (float)System.Math.Sqrt(dx * dx + dz * dz);
            float ix = w > 1e-5f ? dx / w : 1f, iz = w > 1e-5f ? dz / w : 0f;
            float lx = -iz * lateral, lz = ix * lateral;
            var a2 = new Vec3(a.X + lx, a.Y, a.Z + lz);
            var b2 = new Vec3(b.X + lx, b.Y, b.Z + lz);
            var hs = new double[n + 1]; var vs = new double[n + 1];
            float chord = (float)System.Math.Sqrt(w * w + (b.Y - a.Y) * (b.Y - a.Y));
            Catenary.Sample2D(0, a2.Y, w, b2.Y, chord * System.Math.Max(1.0001f, slack), n, hs, vs);
            for (int i = 0; i <= n; i++)
                dst[i] = new Vec3(a2.X + ix * (float)hs[i], (float)vs[i], a2.Z + iz * (float)hs[i]);
            dst[0] = a2; dst[n] = b2;
        }

        public static float MinY(Vec3[] pts, int count)
        {
            float m = float.MaxValue;
            for (int i = 0; i < count; i++) if (pts[i].Y < m) m = pts[i].Y;
            return m;
        }
    }
}
