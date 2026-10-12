// Uploads an engine-free PadMeshData into a Unity Mesh. One mesh per kit material.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Pad
{
    public static class PadMeshUpload
    {
        public static Mesh Upload(PadMeshData src, string name)
        {
            if (src == null || src.VertexCount < 3 || src.TriangleCount < 1) return null;
            var mesh = new Mesh { name = string.IsNullOrEmpty(name) ? "KH_Pad" : name };
            if (src.VertexCount > 65000) mesh.indexFormat = IndexFormat.UInt32;
            int n = src.VertexCount;
            var v = new List<Vector3>(n);
            var normals = new List<Vector3>(n);
            var uv = new List<Vector2>(n);
            for (int i = 0; i < n; i++)
            {
                v.Add(new Vector3(src.Positions[i * 3], src.Positions[i * 3 + 1], src.Positions[i * 3 + 2]));
                normals.Add(new Vector3(src.Normals[i * 3], src.Normals[i * 3 + 1], src.Normals[i * 3 + 2]));
                uv.Add(new Vector2(src.Uv[i * 2], src.Uv[i * 2 + 1]));
            }
            mesh.SetVertices(v);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(src.Triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
