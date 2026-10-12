using System;

namespace Kestrel.Site
{
    public sealed class SiteMesh
    {
        public float[] Positions = Array.Empty<float>();
        public float[] Normals = Array.Empty<float>();
        public float[] Colors = Array.Empty<float>();
        public int[] Indices = Array.Empty<int>();
        public int VertexCount => Positions.Length / 3;
        public int TriangleCount => Indices.Length / 3;

        public bool ContainsXZ(float x, float z)
        {
            var p = Positions; var ix = Indices;
            for (int i = 0; i + 2 < ix.Length; i += 3)
            {
                int a = ix[i] * 3, b = ix[i + 1] * 3, c = ix[i + 2] * 3;
                if (InTri(x, z, p[a], p[a + 2], p[b], p[b + 2], p[c], p[c + 2])) return true;
            }
            return false;
        }

        public float AverageNormalY()
        {
            if (Normals.Length < 3) return 0f;
            double s = 0; int n = 0;
            for (int i = 1; i < Normals.Length; i += 3) { s += Normals[i]; n++; }
            return n == 0 ? 0f : (float)(s / n);
        }

        static bool InTri(float x, float z, float ax, float az, float bx, float bz, float cx, float cz)
        {
            float c1 = (x - ax) * (bz - az) - (z - az) * (bx - ax);
            float c2 = (x - bx) * (cz - bz) - (z - bz) * (cx - bx);
            float c3 = (x - cx) * (az - cz) - (z - cz) * (ax - cx);
            const float e = 2f;
            bool neg = c1 <= e && c2 <= e && c3 <= e;
            bool pos = c1 >= -e && c2 >= -e && c3 >= -e;
            return neg || pos;
        }
    }
}
