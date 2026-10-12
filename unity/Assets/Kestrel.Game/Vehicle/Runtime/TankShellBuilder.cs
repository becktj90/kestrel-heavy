// Builds V2 tank shell meshes from Kestrel.Vehicle.Core data using the shared kit (Kestrel.Kit):
// KitMesh.Tube for weld beads and stiffener rings, MaterialLibrary for materials.
// Output: one Mesh per LOD with ALWAYS 3 submeshes (0 shell, 1 welds, 2 stiffeners; a submesh may be empty),
// matching TankShellBuilder.Materials(). Assembled by hand: Mesh.CombineMeshes drops empty inputs, which collapsed
// LOD meshes to 1 submesh in #106.
using System.Collections.Generic;
using Kestrel.Kit;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Vehicle
{
    public static class TankShellBuilder
    {
        public const int SubmeshShell = 0, SubmeshWelds = 1, SubmeshStiffeners = 2, SubmeshCount = 3;
        public const float WeldBeadRadius = 0.012f;    // m, proud of the skin
        public const float StiffenerRadius = 0.025f;   // m, external hat ring stand-in

        /// <summary>Shell + seams + rings for one LOD. cutFrom/cutTo (radians) remove a sector for cutaway/Lab.</summary>
        public static Mesh Build(TankShellSpec spec, int lod, float cutFrom = 0, float cutTo = 0)
        {
            var layout = TankLayout.Build(spec);
            var s = layout.Fitted;
            var tl = TankLod.For(lod);
            var acc = new Accumulator();

            acc.AddCore(LatheMesh.Build(TankProfile.Build(s, layout, tl), tl.RadialSegments, cutFrom, cutTo));

            acc.Begin(SubmeshWelds);
            float bead = s.Radius + WeldBeadRadius * 0.5f;
            if (tl.CircWelds && layout.CircWelds.Count > 0)
            {
                // One shared ring mesh per (radius, sides), copied per weld height (same tris, one Tube build).
                var ring = KitMesh.Tube(TankShellBuilder.Circle(0, bead, tl.RadialSegments), WeldBeadRadius, tl.WeldSides, "KH_WeldRing");
                foreach (var w in layout.CircWelds) acc.Add(ring, w.Y);
                Kill(ring);
            }
            if (tl.DomeWelds) foreach (var w in layout.DomeWelds)
            {
                var ring = KitMesh.Tube(Circle(w.Y, w.R + WeldBeadRadius * 0.5f, tl.RadialSegments), WeldBeadRadius, tl.WeldSides, "KH_WeldDome");
                acc.Add(ring, 0); Kill(ring);
            }
            if (tl.LongWelds) foreach (var sm in layout.LongWelds)
            {
                var d = new Vector3(Mathf.Sin(sm.Azimuth), 0, Mathf.Cos(sm.Azimuth)) * bead;
                var seam = KitMesh.Tube(new[] { d + Vector3.up * sm.Y0, d + Vector3.up * sm.Y1 }, WeldBeadRadius, tl.WeldSides, "KH_WeldLong");
                acc.Add(seam, 0); Kill(seam);
            }

            acc.Begin(SubmeshStiffeners);
            if (tl.Stiffeners && layout.Stiffeners.Count > 0)
            {
                var ring = KitMesh.Tube(Circle(0, s.Radius + StiffenerRadius * 0.5f, tl.RadialSegments), StiffenerRadius, tl.StiffenerSides, "KH_Stiffener");
                foreach (var r in layout.Stiffeners) acc.Add(ring, r.Y);
                Kill(ring);
            }
            return acc.ToMesh($"KH_Tank_L{lod}");
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
            p[segments] = p[0];
            return p;
        }

        internal static void Kill(Object o) { if (o == null) return; if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o); }

        /// <summary>Concatenates geometry into fixed submeshes. Tube parts get a default tangent (no normal map on them).</summary>
        sealed class Accumulator
        {
            readonly List<Vector3> _v = new List<Vector3>(), _n = new List<Vector3>();
            readonly List<Vector4> _t = new List<Vector4>(); readonly List<Vector2> _uv = new List<Vector2>();
            readonly List<int>[] _tri = { new List<int>(), new List<int>(), new List<int>() };
            int _sub;
            readonly List<Vector3> _tmpV = new List<Vector3>(), _tmpN = new List<Vector3>(); readonly List<Vector2> _tmpUv = new List<Vector2>(); readonly List<int> _tmpT = new List<int>();

            public void Begin(int submesh) => _sub = submesh;

            public void AddCore(MeshData d)
            {
                int b = _v.Count;
                for (int i = 0; i < d.VertexCount; i++)
                {
                    _v.Add(new Vector3(d.Positions[i * 3], d.Positions[i * 3 + 1], d.Positions[i * 3 + 2]));
                    _n.Add(new Vector3(d.Normals[i * 3], d.Normals[i * 3 + 1], d.Normals[i * 3 + 2]));
                    _t.Add(new Vector4(d.Tangents[i * 4], d.Tangents[i * 4 + 1], d.Tangents[i * 4 + 2], d.Tangents[i * 4 + 3]));
                    _uv.Add(new Vector2(d.Uv[i * 2], d.Uv[i * 2 + 1]));
                }
                foreach (var i in d.Triangles) _tri[SubmeshShell].Add(b + i);
            }

            public void Add(Mesh m, float dy)
            {
                m.GetVertices(_tmpV); m.GetNormals(_tmpN); m.GetUVs(0, _tmpUv); m.GetTriangles(_tmpT, 0);
                int b = _v.Count;
                for (int i = 0; i < _tmpV.Count; i++)
                {
                    _v.Add(_tmpV[i] + new Vector3(0, dy, 0)); _n.Add(_tmpN[i]); _t.Add(new Vector4(1, 0, 0, 1));
                    _uv.Add(i < _tmpUv.Count ? _tmpUv[i] : Vector2.zero);
                }
                foreach (var i in _tmpT) _tri[_sub].Add(b + i);
            }

            public Mesh ToMesh(string name)
            {
                var m = new Mesh { name = name };
                if (_v.Count > 65535) m.indexFormat = IndexFormat.UInt32;
                m.SetVertices(_v); m.SetNormals(_n); m.SetTangents(_t); m.SetUVs(0, _uv);
                m.subMeshCount = SubmeshCount;
                for (int s = 0; s < SubmeshCount; s++) m.SetTriangles(_tri[s], s, calculateBounds: false);
                m.RecalculateBounds();
                return m;
            }
        }
    }
}
