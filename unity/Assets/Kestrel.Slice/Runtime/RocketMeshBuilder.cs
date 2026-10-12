// Procedural rocket mesh from a design, using lathe (surface-of-revolution) geometry.
// Dimensions come from the same core math as physics: tank length from DesignMath.SizeTank,
// diameters from the design, fairing from design.fairing*, engine bells from catalog exit diameters.
// Mesh is in metres, +Y up, base of the first stage at y = 0.
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Vehicle;
using UnityEngine;

namespace Kestrel.Slice
{
    public static class RocketMeshBuilder
    {
        const int Segments = 32;

        /// <summary>Lathe a (radius, y) profile around +Y. Profile runs bottom -> top.</summary>
        public static void Lathe(List<Vector2> profile, Vector3 offset, List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris)
        {
            int ring = Segments + 1;
            int start = verts.Count;
            float total = 0; var cum = new float[profile.Count];
            for (int i = 1; i < profile.Count; i++) { total += Vector2.Distance(profile[i], profile[i - 1]); cum[i] = total; }
            for (int i = 0; i < profile.Count; i++)
            {
                // Profile normal: perpendicular to the local tangent (r, y) in the meridian plane.
                Vector2 a = profile[Mathf.Max(0, i - 1)], b = profile[Mathf.Min(profile.Count - 1, i + 1)];
                Vector2 tan = (b - a).normalized; Vector2 n2 = new Vector2(tan.y, -tan.x);
                for (int s = 0; s <= Segments; s++)
                {
                    float ang = s * Mathf.PI * 2 / Segments, c = Mathf.Cos(ang), sn = Mathf.Sin(ang);
                    verts.Add(offset + new Vector3(profile[i].x * c, profile[i].y, profile[i].x * sn));
                    normals.Add(new Vector3(n2.x * c, n2.y, n2.x * sn).normalized);
                    uvs.Add(new Vector2((float)s / Segments, total > 0 ? cum[i] / total : 0));
                }
            }
            for (int i = 0; i < profile.Count - 1; i++)
                for (int s = 0; s < Segments; s++)
                {
                    int a0 = start + i * ring + s, a1 = a0 + 1, b0 = a0 + ring, b1 = b0 + 1;
                    tris.Add(a0); tris.Add(b0); tris.Add(a1);
                    tris.Add(a1); tris.Add(b0); tris.Add(b1);
                }
        }

        /// <summary>Tank barrel placement on the vehicle (vehicle-local): the plain lathe body is the silhouette, and
        /// SliceApp overlays a Kestrel.Vehicle TankShellView per tank for welds, stiffener rings and the isogrid normal map.</summary>
        public readonly struct TankSlot { public readonly Vector3 Offset; public readonly float Radius, BarrelBottom, BarrelLength; public readonly int Stage; public readonly bool StrapOn;
            public TankSlot(Vector3 off, float r, float y0, float len, int stage, bool strap) { Offset = off; Radius = r; BarrelBottom = y0; BarrelLength = len; Stage = stage; StrapOn = strap; } }
        public sealed class Result { public Mesh mesh; public float height; public float stage1Top; public Vector3[] nozzleExits; public float exitDiameter; public List<TankSlot> tanks = new List<TankSlot>(); }

        public static Result Build(Catalog c, Design d)
        {
            var verts = new List<Vector3>(); var normals = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            float y = 0, stage1Top = 0; var exits = new List<Vector3>(); float exitD = 0; var slots = new List<TankSlot>();

            for (int i = 0; i < d.stages.Count; i++)
            {
                var s = d.stages[i]; var e = c.EngineById[s.engine];
                float r = (float)s.diameter / 2;
                float tank = (float)DesignMath.SizeTank(c, s.propellant, s.propMass, s.diameter).length;
                float bell = Mathf.Clamp((float)e.diameter * 1.6f, 0.4f, 4f);
                float thrustSection = 0.6f * r + (i == 0 ? 0 : bell * 0.7f); // upper-stage bells nest inside the interstage
                if (i == 0) AddEngines(c, s, new Vector3(0, 0, 0), bell, verts, normals, uvs, tris, exits, ref exitD);
                float baseY = y + (i == 0 ? bell : 0);
                var prof = new List<Vector2> { new Vector2(0, baseY), new Vector2(r, baseY), new Vector2(r, baseY + thrustSection + tank) };
                slots.Add(new TankSlot(Vector3.zero, r, baseY + thrustSection, tank, i, false));
                y = baseY + thrustSection + tank;
                bool last = i == d.stages.Count - 1;
                if (!last)
                {
                    float rn = (float)d.stages[i + 1].diameter / 2, inter = 1.2f * Mathf.Max(r, rn);
                    prof.Add(new Vector2(rn, y + inter)); y += inter; prof.Add(new Vector2(0, y));
                }
                else prof.Add(new Vector2(0, y));
                Lathe(prof, Vector3.zero, verts, normals, uvs, tris);
                if (i == 0) stage1Top = y;
            }
            // Fairing: cylinder + ogive-ish nose (length from design; nose fraction from aero.nose).
            {
                float rf = (float)d.fairingDiameter / 2, L = (float)d.fairingLength;
                float noseFrac = d.nose == "blunt" ? 0.25f : d.nose == "conic" ? 0.4f : 0.45f;
                float cyl = L * (1 - noseFrac), nose = L * noseFrac;
                var prof = new List<Vector2> { new Vector2(0, y), new Vector2(rf, y), new Vector2(rf, y + cyl) };
                for (int k = 1; k <= 10; k++)
                {
                    float t = k / 10f; // tangent ogive approximation: r = rf * sqrt(1 - t^2)
                    float rr = d.nose == "conic" ? rf * (1 - t) : rf * Mathf.Sqrt(Mathf.Max(0, 1 - t * t));
                    prof.Add(new Vector2(k == 10 ? 0 : rr, y + cyl + nose * t));
                }
                Lathe(prof, Vector3.zero, verts, normals, uvs, tris);
                y += L;
            }
            // Strap-ons: lathed cylinders with a conic nose, spaced around the core.
            if (d.strapOns != null && d.strapOns.count > 0)
            {
                var so = d.strapOns; var e = c.EngineById[so.engine];
                float r = (float)so.diameter / 2, core = (float)d.stages[0].diameter / 2;
                float bell = Mathf.Clamp((float)e.diameter * 1.6f, 0.4f, 4f);
                float tank = (float)DesignMath.SizeTank(c, so.propellant, so.propMass, so.diameter).length;
                for (int k = 0; k < so.count; k++)
                {
                    float ang = k * Mathf.PI * 2 / so.count;
                    var off = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * (core + r + 0.1f);
                    var prof = new List<Vector2> { new Vector2(0, bell), new Vector2(r, bell), new Vector2(r, bell + tank), new Vector2(0, bell + tank + 2.2f * r) };
                    Lathe(prof, off, verts, normals, uvs, tris);
                    slots.Add(new TankSlot(off, r, bell, tank, 0, true));
                    AddEngines(c, so, off, bell, verts, normals, uvs, tris, exits, ref exitD);
                }
            }
            var mesh = new Mesh { name = "Rocket_" + d.id, indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(verts); mesh.SetNormals(normals); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return new Result { mesh = mesh, height = y, stage1Top = stage1Top, nozzleExits = exits.ToArray(), exitDiameter = exitD, tanks = slots };
        }

        /// <summary>Engine bells: one centre engine plus concentric rings (simple circle packer).</summary>
        static void AddEngines(Catalog c, StagePart s, Vector3 origin, float bell, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t, List<Vector3> exits, ref float exitD)
        {
            var e = c.EngineById[s.engine];
            float stageR = (float)s.diameter / 2;
            float re = Mathf.Min((float)e.diameter / 2, stageR / Mathf.Max(1, Mathf.Sqrt(s.engineCount)) * 0.95f);
            exitD = Mathf.Max(exitD, re * 2);
            var spots = new List<Vector2>();
            int left = s.engineCount;
            if (left % 2 == 1 || left == 1) { spots.Add(Vector2.zero); left--; }
            float ringR = re * 2.1f;
            while (left > 0)
            {
                int cap = Mathf.Max(1, Mathf.FloorToInt(2 * Mathf.PI * ringR / (re * 2.1f)));
                int k = Mathf.Min(cap, left);
                for (int j = 0; j < k; j++) { float a = j * Mathf.PI * 2 / k; spots.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * ringR); }
                left -= k; ringR += re * 2.1f;
            }
            foreach (var p in spots)
            {
                var off = origin + new Vector3(p.x, 0, p.y);
                var prof = new List<Vector2> { new Vector2(re, 0), new Vector2(re * 0.45f, bell * 0.75f), new Vector2(re * 0.3f, bell * 0.85f), new Vector2(re * 0.5f, bell), new Vector2(0, bell) };
                Lathe(prof, off, v, n, uv, t);
                exits.Add(off);
            }
        }
    }
}
