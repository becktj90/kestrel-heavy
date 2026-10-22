// Copies an engine-free bell mesh into a Unity mesh. The material is the instanced KH_Metal asset
// so the player keeps INSTANCING_ON. No per-object material, so the bells stay one batch; the fuel cue
// (BellFinish) is one shared tinted clone per fuel, so each fuel's bells are still one batch.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Vehicle
{
    public static class BellBuilder
    {
        public const float HeroLodBias = 1.5f;

        public static Material Material() => VehicleLook.Metal();

        static readonly System.Collections.Generic.Dictionary<string, Material> Finishes = new System.Collections.Generic.Dictionary<string, Material>();

        /// <summary>The fuel-cue bell material for a BellFinish key: one shared, instanced clone of KH_Metal per key
        /// (same shader and keywords, so the player keeps the INSTANCING_ON variant), tinted by BellFinish.
        /// An empty or unknown key is the shared KH_Metal itself.</summary>
        public static Material Material(string finish)
        {
            var metal = Material();
            if (metal == null || string.IsNullOrEmpty(finish) || !BellFinish.TryTint(finish, out var t)) return metal;
            if (Finishes.TryGetValue(finish, out var m) && m != null && m.shader == metal.shader) return m;
            m = new Material(metal) { name = "KH_Metal_" + finish, enableInstancing = true, hideFlags = HideFlags.DontSave };
            var c = new Color(t.R, t.G, t.B, 1f);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", t.Metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", t.Smoothness);
            Finishes[finish] = m;
            return m;
        }

        public static Mesh Build(BellContour contour, int lod)
        {
            var data = BellMesh.Build(contour, lod);
            int n = data.VertexCount;
            var mesh = new Mesh { name = "KH_Bell_" + contour.Spec.EngineId };
            mesh.indexFormat = n > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            var v = new List<Vector3>(n);
            var normals = new List<Vector3>(n);
            var tangents = new List<Vector4>(n);
            var uv = new List<Vector2>(n);
            for (int i = 0; i < n; i++)
            {
                v.Add(new Vector3(data.Positions[i * 3], data.Positions[i * 3 + 1], data.Positions[i * 3 + 2]));
                normals.Add(new Vector3(data.Normals[i * 3], data.Normals[i * 3 + 1], data.Normals[i * 3 + 2]));
                tangents.Add(new Vector4(data.Tangents[i * 4], data.Tangents[i * 4 + 1], data.Tangents[i * 4 + 2], data.Tangents[i * 4 + 3]));
                uv.Add(new Vector2(data.Uv[i * 2], data.Uv[i * 2 + 1]));
            }
            mesh.SetVertices(v);
            mesh.SetNormals(normals);
            mesh.SetTangents(tangents);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(data.Triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
