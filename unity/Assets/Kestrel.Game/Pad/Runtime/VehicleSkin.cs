// Skin profile for PadFit.SkinRadiusAt from the vehicle's render mesh: for each 1 m height bin, the furthest
// vertex toward the tower (pad +Z) inside a narrow strip around the tower axis. Lets arms end at the real
// skin of a core that is narrower than the overall half-width (side boosters, fairing steps).
using System;
using UnityEngine;

namespace Kestrel.Pad
{
    public static class VehicleSkin
    {
        /// <summary>Changes whenever a different or re-generated mesh is used (instance id + vertex count + bounds).</summary>
        public static long MeshKey(Mesh m)
        {
            if (m == null) return 0;
            unchecked
            {
                long h = m.GetInstanceID(); h = h * 31 + m.vertexCount;
                var b = m.bounds; h = h * 31 + b.size.GetHashCode(); h = h * 31 + b.center.GetHashCode();
                return h == 0 ? 1 : h;
            }
        }

        /// <param name="meshToPad">Mesh space → pad space (pad.worldToLocalMatrix * vehicle.localToWorldMatrix).</param>
        /// <param name="strip">Half-width (m) of the strip around the pad Z axis that counts as "facing the tower".</param>
        public static Func<float, float> FromMesh(Mesh mesh, Matrix4x4 meshToPad, float strip = 0.75f, float bin = 1f)
        {
            if (mesh == null || !mesh.isReadable) return null;
            var v = mesh.vertices; if (v == null || v.Length == 0) return null;
            float yMax = float.MinValue; var p = new Vector3[v.Length];
            for (int i = 0; i < v.Length; i++) { p[i] = meshToPad.MultiplyPoint3x4(v[i]); yMax = Mathf.Max(yMax, p[i].y); }
            int n = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0f, yMax) / bin) + 1);
            var z = new float[n]; for (int i = 0; i < n; i++) z[i] = float.NaN;
            foreach (var q in p)
            {
                if (q.y < 0 || Mathf.Abs(q.x) > strip || q.z <= 0) continue;
                int b = Mathf.Min(n - 1, (int)(q.y / bin));
                if (float.IsNaN(z[b]) || q.z > z[b]) z[b] = q.z;
            }
            return FromTable(z, bin);
        }

        /// <summary>Profile from a per-bin table (NaN = unknown → caller falls back to VehicleRadius). Takes the
        /// max of the bin and its neighbours so an arm never ends inside a flange between samples.</summary>
        public static Func<float, float> FromTable(float[] z, float bin) => y =>
        {
            if (z == null || z.Length == 0 || y < 0) return float.NaN;
            int b = Mathf.Min(z.Length - 1, (int)(y / bin)); float m = float.NaN;
            for (int k = b - 1; k <= b + 1; k++) if (k >= 0 && k < z.Length && !float.IsNaN(z[k])) m = float.IsNaN(m) ? z[k] : Mathf.Max(m, z[k]);
            return m;
        };
    }
}
