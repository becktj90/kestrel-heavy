// Builds V2 tank shell meshes from Kestrel.Vehicle.Core data using the shared kit (Kestrel.Kit):
// KitMesh.Tube for weld beads and stiffener rings, MaterialLibrary for materials, LodPolicy for tube sides.
// Output: one Mesh per LOD with 3 submeshes (0 shell, 1 welds, 2 stiffeners), matching TankShellBuilder.Materials().
using System.Collections.Generic;
using Kestrel.Kit;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Vehicle
{
    public static class TankShellBuilder
    {
        public const int SubmeshShell = 0, SubmeshWelds = 1, SubmeshStiffeners = 2;
        public const float WeldBeadRadius = 0.012f;    // m, proud of the skin
        public const float StiffenerRadius = 0.025f;   // m, external hat ring stand-in

        /// <summary>Shell + seams + rings for one LOD. cutFrom/cutTo (radians) remove a sector for cutaway/Lab.</summary>
        public static Mesh Build(TankShellSpec spec, int lod, float cutFrom = 0, float cutTo = 0)
        {
            var layout = TankLayout.Build(spec);
            var tl = TankLod.For(lod);
            int sides = LodPolicy.RadialSegments(lod);
            var shell = ToMesh(LatheMesh.Build(TankProfile.Build(spec, layout, tl), tl.RadialSegments, cutFrom, cutTo), "KH_TankShell");

            var welds = new List<Mesh>(); var rings = new List<Mesh>();
            float bead = spec.Radius + WeldBeadRadius * 0.5f;
            if (tl.CircWelds) foreach (var w in layout.CircWelds) welds.Add(KitMesh.Tube(Circle(w.Y, bead, tl.RadialSegments), WeldBeadRadius, sides, "KH_WeldCirc"));
            if (tl.DomeWelds) foreach (var w in layout.DomeWelds) welds.Add(KitMesh.Tube(Circle(w.Y, w.R + WeldBeadRadius * 0.5f, tl.RadialSegments), WeldBeadRadius, sides, "KH_WeldDome"));
            if (tl.LongWelds) foreach (var s in layout.LongWelds)
            {
                var d = new Vector3(Mathf.Sin(s.Azimuth), 0, Mathf.Cos(s.Azimuth)) * bead;
                welds.Add(KitMesh.Tube(new[] { d + Vector3.up * s.Y0, d + Vector3.up * s.Y1 }, WeldBeadRadius, sides, "KH_WeldLong"));
            }
            if (tl.Stiffeners) foreach (var r in layout.Stiffeners)
                rings.Add(KitMesh.Tube(Circle(r.Y, spec.Radius + StiffenerRadius * 0.5f, tl.RadialSegments), StiffenerRadius, TankBudget.StiffenerSides, "KH_Stiffener"));

            var mesh = new Mesh { name = $"KH_Tank_L{lod}" };
            var weldsM = Merge(welds, "KH_Welds"); var ringsM = Merge(rings, "KH_Stiffeners");
            long verts = shell.vertexCount + weldsM.vertexCount + ringsM.vertexCount;
            if (verts > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.CombineMeshes(new[]
            {
                new CombineInstance { mesh = shell, transform = Matrix4x4.identity },
                new CombineInstance { mesh = weldsM, transform = Matrix4x4.identity },
                new CombineInstance { mesh = ringsM, transform = Matrix4x4.identity },
            }, mergeSubMeshes: false, useMatrices: false);
            mesh.RecalculateBounds();
            Object.DestroyImmediate(shell); Object.DestroyImmediate(weldsM); Object.DestroyImmediate(ringsM);
            foreach (var m in welds) Object.DestroyImmediate(m);
            foreach (var m in rings) Object.DestroyImmediate(m);
            return mesh;
        }

        /// <summary>Shared materials in submesh order. Shell = kit PaintedSteel + isogrid normal (VehicleMaterials);
        /// welds = kit Galvanized; stiffeners = kit PaintedSteel. No per-object materials.</summary>
        public static Material[] Materials() => new[]
        {
            VehicleMaterials.ShellIsogrid(), MaterialLibrary.Get(MaterialRole.Galvanized), MaterialLibrary.Get(MaterialRole.PaintedSteel)
        };

        /// <summary>Closed circle path (first point repeated) at height y, azimuth 0 = +Z.</summary>
        public static Vector3[] Circle(float y, float r, int segments)
        {
            var p = new Vector3[segments + 1];
            for (int j = 0; j <= segments; j++) { float a = 2 * Mathf.PI * j / segments; p[j] = new Vector3(r * Mathf.Sin(a), y, r * Mathf.Cos(a)); }
            return p;
        }

        internal static Mesh ToMesh(MeshData d, string name)
        {
            int n = d.VertexCount;
            var v = new List<Vector3>(n); var nn = new List<Vector3>(n); var t = new List<Vector4>(n); var uv = new List<Vector2>(n);
            for (int i = 0; i < n; i++)
            {
                v.Add(new Vector3(d.Positions[i * 3], d.Positions[i * 3 + 1], d.Positions[i * 3 + 2]));
                nn.Add(new Vector3(d.Normals[i * 3], d.Normals[i * 3 + 1], d.Normals[i * 3 + 2]));
                t.Add(new Vector4(d.Tangents[i * 4], d.Tangents[i * 4 + 1], d.Tangents[i * 4 + 2], d.Tangents[i * 4 + 3]));
                uv.Add(new Vector2(d.Uv[i * 2], d.Uv[i * 2 + 1]));
            }
            var m = new Mesh { name = name };
            if (n > 65535) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(v); m.SetNormals(nn); m.SetTangents(t); m.SetUVs(0, uv);
            m.SetTriangles(d.Triangles, 0); m.RecalculateBounds();
            return m;
        }

        static Mesh Merge(List<Mesh> parts, string name)
        {
            var m = new Mesh { name = name };
            if (parts.Count == 0) return m;
            long verts = 0; foreach (var p in parts) verts += p.vertexCount;
            if (verts > 65535) m.indexFormat = IndexFormat.UInt32;
            var ci = new CombineInstance[parts.Count];
            for (int i = 0; i < parts.Count; i++) ci[i] = new CombineInstance { mesh = parts[i], transform = Matrix4x4.identity };
            m.CombineMeshes(ci, mergeSubMeshes: true, useMatrices: false);
            return m;
        }
    }
}
