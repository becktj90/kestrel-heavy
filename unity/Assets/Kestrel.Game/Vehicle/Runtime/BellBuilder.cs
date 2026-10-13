// Copies an engine-free bell mesh into a Unity mesh. The material is the kit's Galvanized role
// (stand-in until Graphics Dev adds a nozzle role). No per-object material, so the bells instance.
using System.Collections.Generic;
using Kestrel.Kit;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Vehicle
{
    public static class BellBuilder
    {
        public const float HeroLodBias = 1.5f;

        public static Material Material() => MaterialLibrary.Get(MaterialRole.Galvanized);

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
