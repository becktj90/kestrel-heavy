// Copies an engine-free bay mesh into a Unity mesh. One mesh per kind, instanced across the cluster.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Vehicle
{
    public static class BayBuilder
    {
        public static Material Material() => VehicleLook.Metal();

        public static Mesh Build(string kind, int lod) => FromData(BayMesh.Build(kind, lod), "KH_Bay_" + kind);

        public static Mesh FromData(MeshData data, string name)
        {
            int n = data.VertexCount;
            var mesh = new Mesh { name = name };
            if (n == 0) return mesh;
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
