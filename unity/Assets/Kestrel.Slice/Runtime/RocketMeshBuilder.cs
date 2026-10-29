// Procedural rocket mesh from a design, using lathe (surface-of-revolution) geometry.
// Dimensions come from the same core math as physics: tank length from DesignMath.SizeTank,
// diameters from the design, fairing from design.fairing*, engine bells from catalog exit diameters.
// Mesh is in metres, +Y up, base of the first stage at y = 0.
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Vehicle;
using Kestrel.Vehicle;
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
        /// <summary>Frustum between two stages. The body lathe still draws the cone; StrutCluster draws stringers and joint rings proud of it.</summary>
        public readonly struct InterstageSlot { public readonly float Y0, Y1, R0, R1;
            public InterstageSlot(float y0, float y1, float r0, float r1) { Y0 = y0; Y1 = y1; R0 = r0; R1 = r1; } }
        /// <summary>Fairing cylinder + nose. The lathe still draws the ogive; FairingLayout draws seams and rings proud of it.</summary>
        public readonly struct FairingSlot { public readonly float BaseY, Radius, Length, NoseFraction, JointRadius; public readonly bool Conic, Present;
            /// <summary>Radius is the barrel (the data fairing diameter / 2, the widest point). JointRadius is the base, equal to the stage under it.</summary>
            public FairingSlot(float baseY, float radius, float length, float noseFraction, bool conic, bool present, float jointRadius = -1f)
            { BaseY = baseY; Radius = radius; Length = length; NoseFraction = noseFraction; Conic = conic; Present = present; JointRadius = jointRadius > 0f ? jointRadius : radius; } }
        /// <summary>One engine bell. The mesh is drawn by Kestrel.Vehicle.BellCluster, not lathed into the body.</summary>
        public readonly struct EngineSlot
        {
            public readonly Vector3 Exit; public readonly float ExitDiameter, Expansion, GimbalDeg, StageRadius;
            public readonly string EngineId, Cycle; public readonly int Stage, Slot; public readonly bool StrapOn;
            public EngineSlot(Vector3 exit, float exitDiameter, float expansion, float gimbal, string id, string cycle, int stage, int slot, bool strap, float stageRadius)
            { Exit = exit; ExitDiameter = exitDiameter; Expansion = expansion; GimbalDeg = gimbal; EngineId = id; Cycle = cycle; Stage = stage; Slot = slot; StrapOn = strap; StageRadius = stageRadius; }

            /// <summary>The fuel this bell burns (its stage's or the strap-ons' propellant), for the BellFinish fuel cue.</summary>
            public string Propellant(Design d)
            {
                if (d == null) return null;
                if (StrapOn) return d.strapOns != null ? d.strapOns.propellant : null;
                return d.stages != null && Stage >= 0 && Stage < d.stages.Count ? d.stages[Stage].propellant : null;
            }
        }
        public sealed class Result { public Mesh mesh; public float height; public float stage1Top; public float stage1AftY; public Vector3[] nozzleExits; public float exitDiameter; public List<TankSlot> tanks = new List<TankSlot>(); public List<EngineSlot> engines = new List<EngineSlot>(); public List<InterstageSlot> interstages = new List<InterstageSlot>(); public FairingSlot fairing; }

        /// <summary>Axial room the bell needs, from the catalog expansion ratio. Exit diameter is the packed cluster size.</summary>
        public static float BellHeight(Engine e, float stageDiameter, int count) => BellHeight(e, stageDiameter, count, null);

        /// <summary>Bell room for the chosen nozzle (null or "default" = the catalog nozzle).</summary>
        public static float BellHeight(Engine e, float stageDiameter, int count, string nozzle)
        {
            if (e == null) return 0.8f;
            NozzleOf(e, nozzle, out float nozzleD, out float eps);
            var spots = new List<Vector2>();
            float re = EngineSpots(stageDiameter, nozzleD, Mathf.Max(1, count), spots);
            float exitDiameter = Mathf.Max(0.1f, re * 2f);
            if (!BellSpec.TryParseCycle(e.cycle, out var cycle)) return Mathf.Clamp(exitDiameter * 1.6f, 0.4f, 4f);
            var spec = new BellSpec
            {
                EngineId = string.IsNullOrEmpty(e.id) ? "engine" : e.id,
                Cycle = cycle,
                ExpansionRatio = Mathf.Max(2f, eps),
                ExitDiameter = exitDiameter,
                VehicleSeed = 1
            };
            if (spec.Validate().Count > 0) return Mathf.Clamp(exitDiameter * 1.6f, 0.4f, 4f);
            return Mathf.Max(0.2f, BellProfile.Build(spec, 1).Length);
        }

        /// <summary>Exit diameter and expansion ratio of the selected nozzle variant, else the engine's own.</summary>
        public static void NozzleOf(Engine e, string nozzle, out float exitDiameter, out float expansion)
        {
            exitDiameter = (float)e.diameter; expansion = (float)e.expansionRatio;
            if (string.IsNullOrEmpty(nozzle) || nozzle == "default" || e.nozzles == null) return;
            foreach (var n in e.nozzles)
            {
                if (n == null || n.id != nozzle) continue;
                if (n.exitDiameter > 0) exitDiameter = (float)n.exitDiameter;
                if (n.expansionRatio > 0) expansion = (float)n.expansionRatio;
                return;
            }
        }

        /// <summary>Nose share of the fairing length, by aero.nose.</summary>
        public static float NoseFraction(string nose) => Kestrel.Vehicle.FairingShoulder.NoseFraction(nose);

        /// <summary>Nose radius at t (0 = shoulder, 1 = tip). Von Karman is the LD-Haack series (C = 0).</summary>
        public static float NoseRadius(string nose, float rf, float t) => Kestrel.Vehicle.FairingShoulder.NoseRadius(nose, rf, t);

        /// <summary>Radius (m) of the stage the fairing sits on; the fairing's base is exactly this wide (Trevor's rule). The fairing's own radius when there is no stage or no fairing.</summary>
        public static float JointRadius(Design d)
        {
            float rf = (float)d.fairingDiameter / 2;
            if (d.stages == null || d.stages.Count == 0 || rf <= 0.05f) return rf;
            return Mathf.Max(0.05f, (float)d.stages[d.stages.Count - 1].diameter / 2);
        }

        /// <summary>The fairing lathe profile at vehicle height y: base at the stage radius, smooth shoulder to the barrel, nose to the tip.</summary>
        public static List<Vector2> FairingProfile(Design d, float y)
        {
            var p = new List<Vector2>();
            foreach (var st in Kestrel.Vehicle.FairingShoulder.Profile(JointRadius(d), (float)d.fairingDiameter / 2, (float)d.fairingLength, d.nose))
                p.Add(new Vector2(st.R, y + st.Y));
            return p;
        }

        /// <summary>The payload as drawn in the exploded view: a capsule at the cargo's real diameter (held to the barrel so a cargo that
        /// does not fit does not poke through the skin; Validate already blocks that design). Falls back to the old 0.55 x fairing radius
        /// when the design has no known cargo.</summary>
        /// <summary>Height of #375's shoulder cone above the fairing joint (0 when the fairing is flush with the stage).</summary>
        public static float PayloadShoulderHeight(Design d) =>
            Kestrel.Vehicle.FairingShoulder.Height(JointRadius(d), (float)d.fairingDiameter / 2, (float)d.fairingLength, d.nose);

        public static float PayloadRadius(Catalog c, Design d, float fairingRadius)
        {
            var spec = Kestrel.Core.Vehicle.PayloadFit.SpecFor(c, d);
            return (float)Kestrel.Core.Designer.FairingFlare.CargoDrawRadius(spec != null ? spec.diameter : 0, fairingRadius);
        }

        public static Result Build(Catalog c, Design d)
        {
            var verts = new List<Vector3>(); var normals = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            float y = 0, stage1Top = 0, stage1Aft = 0; var exits = new List<Vector3>(); float exitD = 0; var slots = new List<TankSlot>(); var bells = new List<EngineSlot>(); var spans = new List<InterstageSlot>(); FairingSlot fairing = default;

            for (int i = 0; i < d.stages.Count; i++)
            {
                var s = d.stages[i]; var e = c.EngineById[s.engine];
                float r = (float)s.diameter / 2;
                float tank = (float)DesignMath.SizeTank(c, s.propellant, s.propMass, s.diameter).length;
                float bell = BellHeight(e, (float)s.diameter, s.engineCount, s.nozzle);
                float thrustSection = 0.6f * r + (i == 0 ? 0 : bell * 0.7f); // upper-stage bells nest inside the interstage
                if (i == 0) AddEngines(c, s, new Vector3(0, 0, 0), i, false, 0, exits, ref exitD, bells);
                float baseY = y + (i == 0 ? bell : 0);
                if (i == 0) stage1Aft = baseY;
                var prof = new List<Vector2> { new Vector2(0, baseY), new Vector2(r, baseY), new Vector2(r, baseY + thrustSection + tank) };
                slots.Add(new TankSlot(Vector3.zero, r, baseY + thrustSection, tank, i, false));
                y = baseY + thrustSection + tank;
                bool last = i == d.stages.Count - 1;
                if (!last)
                {
                    float rn = (float)d.stages[i + 1].diameter / 2, inter = 1.2f * Mathf.Max(r, rn);
                    spans.Add(new InterstageSlot(y, y + inter, r, rn));
                    prof.Add(new Vector2(rn, y + inter)); y += inter; prof.Add(new Vector2(0, y));
                }
                else prof.Add(new Vector2(0, y));
                Lathe(prof, Vector3.zero, verts, normals, uvs, tris);
                if (i == 0) stage1Top = y;
            }
            // Fairing: cylinder + ogive-ish nose (length from design; nose fraction from aero.nose).
            {
                float rf = (float)d.fairingDiameter / 2, L = (float)d.fairingLength;
                float noseFrac = NoseFraction(d.nose);
                fairing = new FairingSlot(y, rf, L, noseFrac, d.nose == "conic", L > 0.2f && rf > 0.05f, JointRadius(d));
                var prof = FairingProfile(d, y);
                Lathe(prof, Vector3.zero, verts, normals, uvs, tris);
                y += L;
            }
            // Strap-ons: lathed cylinders with a conic nose, spaced around the core.
            if (d.strapOns != null && d.strapOns.count > 0)
            {
                var so = d.strapOns; var e = c.EngineById[so.engine];
                float r = (float)so.diameter / 2, core = (float)d.stages[0].diameter / 2;
                float bell = BellHeight(e, (float)so.diameter, so.engineCount, so.nozzle);
                float tank = (float)DesignMath.SizeTank(c, so.propellant, so.propMass, so.diameter).length;
                for (int k = 0; k < so.count; k++)
                {
                    float ang = k * Mathf.PI * 2 / so.count;
                    var off = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * (core + r + 0.1f);
                    var prof = new List<Vector2> { new Vector2(0, bell), new Vector2(r, bell), new Vector2(r, bell + tank), new Vector2(0, bell + tank + 2.2f * r) };
                    Lathe(prof, off, verts, normals, uvs, tris);
                    slots.Add(new TankSlot(off, r, bell, tank, 0, true));
                    AddEngines(c, so, off, 0, true, (k + 1) * 16, exits, ref exitD, bells);
                }
            }
            var mesh = new Mesh { name = "Rocket_" + d.id, indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(verts); mesh.SetNormals(normals); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return new Result { mesh = mesh, height = y, stage1Top = stage1Top, stage1AftY = stage1Aft, nozzleExits = exits.ToArray(), exitDiameter = exitD, tanks = slots, engines = bells, interstages = spans, fairing = fairing };
        }

        /// <summary>One Hangar part as its own mesh (vehicle-local, same stations as <see cref="Build"/>).
        /// Boosters are one part with every strap-on in it; Offset is zero for parts on the axis.</summary>
        public sealed class PartMesh
        {
            public Kestrel.Core.Hangar.PartRef Part;
            public Mesh Mesh;
            /// <summary>Axial span (metres above the stage-1 engine exits) and the widest radius.</summary>
            public float Y0, Y1, Radius;
            /// <summary>Optional engine mesh for this part. Null now: BellCluster draws every bell, instanced from the
            /// flight mesh's engine slots, so the parts carry none and nothing is drawn twice.</summary>
            public Mesh Engines;
            /// <summary>Boosters only: one copy of Mesh/Engines per strap-on, at these axis offsets. Null for every other part.</summary>
            public Vector3[] Instances;
            public Vector3 Center => new Vector3(0, (Y0 + Y1) * 0.5f, 0);
        }

        /// <summary>
        /// The same rocket split into Hangar parts: each stage body, each interstage, the fairing, the payload and the
        /// booster set. Engine bells are not here: BellCluster draws them on the assembled rocket.
        /// Stations match <see cref="Build"/>, so the parts reassemble into exactly the flight silhouette.
        /// </summary>
        public static List<PartMesh> BuildParts(Catalog c, Design d)
        {
            var parts = new List<PartMesh>();
            float y = 0;
            for (int i = 0; i < d.stages.Count; i++)
            {
                var s = d.stages[i]; var e = c.EngineById[s.engine];
                float r = (float)s.diameter / 2;
                float tank = (float)DesignMath.SizeTank(c, s.propellant, s.propMass, s.diameter).length;
                float bell = BellHeight(e, (float)s.diameter, s.engineCount, s.nozzle);
                float thrustSection = 0.6f * r + (i == 0 ? 0 : bell * 0.7f);
                float baseY = y + (i == 0 ? bell : 0);
                var g = new Geo();
                float top = baseY + thrustSection + tank;
                Lathe(new List<Vector2> { new Vector2(0, baseY), new Vector2(r, baseY), new Vector2(r, top), new Vector2(0, top) }, Vector3.zero, g.v, g.n, g.uv, g.t);
                parts.Add(new PartMesh { Part = Kestrel.Core.Hangar.PartRef.Stage(i), Mesh = g.ToMesh($"Stage{i + 1}"), Y0 = i == 0 ? 0 : baseY - bell * 0.7f, Y1 = top, Radius = r });
                y = top;
                if (i < d.stages.Count - 1)
                {
                    float rn = (float)d.stages[i + 1].diameter / 2, inter = 1.2f * Mathf.Max(r, rn);
                    var ig = new Geo();
                    Lathe(new List<Vector2> { new Vector2(r, y), new Vector2(rn, y + inter) }, Vector3.zero, ig.v, ig.n, ig.uv, ig.t);
                    parts.Add(new PartMesh { Part = Kestrel.Core.Hangar.PartRef.Interstage(i + 1), Mesh = ig.ToMesh($"Interstage{i + 1}"), Y0 = y, Y1 = y + inter, Radius = Mathf.Max(r, rn) });
                    y += inter;
                }
            }
            {
                float rf = (float)d.fairingDiameter / 2, L = (float)d.fairingLength;
                float cyl = L * (1 - NoseFraction(d.nose));
                var prof = FairingProfile(d, y);
                var g = new Geo();
                Lathe(prof, Vector3.zero, g.v, g.n, g.uv, g.t);
                parts.Add(new PartMesh { Part = Kestrel.Core.Hangar.PartRef.Fairing, Mesh = g.ToMesh("Fairing"), Y0 = y, Y1 = y + L, Radius = rf });
                // Payload: a capsule inside the fairing barrel at the cargo's real diameter (seen when the view explodes).
                // Its base sits above the shoulder cone (FairingShoulder, the same geometry the fairing mesh is lathed from), so a cargo
                // wider than the stage never crosses the cone; when the shoulder pushes it up, it is held to the barrel's top.
                float pr = PayloadRadius(c, d, rf), ph = Mathf.Max(0.5f, cyl * 0.6f), py = y + 0.15f * cyl;
                float shoulder = PayloadShoulderHeight(d);
                if (shoulder > 0.15f * cyl) { py = y + shoulder; ph = Mathf.Min(ph, cyl - shoulder); }
                var pg = new Geo();
                Lathe(new List<Vector2> { new Vector2(0, py), new Vector2(pr, py), new Vector2(pr, py + ph * 0.75f), new Vector2(pr * 0.5f, py + ph), new Vector2(0, py + ph) }, Vector3.zero, pg.v, pg.n, pg.uv, pg.t);
                parts.Add(new PartMesh { Part = Kestrel.Core.Hangar.PartRef.Payload, Mesh = pg.ToMesh("Payload"), Y0 = py, Y1 = py + ph, Radius = pr });
            }
            if (d.strapOns != null && d.strapOns.count > 0)
            {
                var so = d.strapOns; var e = c.EngineById[so.engine];
                float r = (float)so.diameter / 2, core = (float)d.stages[0].diameter / 2;
                float bell = BellHeight(e, (float)so.diameter, so.engineCount, so.nozzle);
                float tank = (float)DesignMath.SizeTank(c, so.propellant, so.propMass, so.diameter).length;
                var g = new Geo();
                Lathe(new List<Vector2> { new Vector2(0, bell), new Vector2(r, bell), new Vector2(r, bell + tank), new Vector2(0, bell + tank + 2.2f * r) }, Vector3.zero, g.v, g.n, g.uv, g.t);
                var at = new Vector3[so.count];
                for (int k = 0; k < so.count; k++)
                {
                    float ang = k * Mathf.PI * 2 / so.count;
                    at[k] = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * (core + r + 0.1f);
                }
                parts.Add(new PartMesh { Part = Kestrel.Core.Hangar.PartRef.Boosters, Mesh = g.ToMesh("Booster"), Instances = at, Y0 = 0, Y1 = bell + tank + 2.2f * r, Radius = r });
            }
            return parts;
        }

        sealed class Geo
        {
            public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>(); public readonly List<Vector2> uv = new List<Vector2>(); public readonly List<int> t = new List<int>();
            public Mesh ToMesh(string name)
            {
                var m = new Mesh { name = name, indexFormat = v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
                m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds();
                return m;
            }
        }

        /// <summary>Engine bell centres in the stage's XZ plane. One centre engine (odd counts) plus concentric rings.
        /// Returns the drawn exit radius. Same packer the assembled lathe uses.</summary>
        public static float EngineSpots(float stageDiameter, float engineDiameter, int count, List<Vector2> spots)
        {
            spots.Clear();
            if (count <= 0) return 0f;
            float stageR = stageDiameter / 2f;
            float re = Mathf.Min(engineDiameter / 2f, stageR / Mathf.Max(1f, Mathf.Sqrt(count)) * 0.95f);
            int left = count;
            if (left % 2 == 1 || left == 1) { spots.Add(Vector2.zero); left--; }
            float ringR = re * 2.1f;
            int guard = 0;
            while (left > 0 && guard++ < 64)
            {
                int cap = Mathf.Max(1, Mathf.FloorToInt(2f * Mathf.PI * ringR / (re * 2.1f)));
                int k = Mathf.Min(cap, left);
                for (int j = 0; j < k; j++) { float a = j * Mathf.PI * 2f / k; spots.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * ringR); }
                left -= k; ringR += re * 2.1f;
            }
            return re;
        }

        /// <summary>Records bell centres. The contour is drawn by BellCluster so each engine type keeps its own shape.</summary>
        static void AddEngines(Catalog c, StagePart s, Vector3 origin, int stage, bool strap, int slotBase, List<Vector3> exits, ref float exitD, List<EngineSlot> slots)
        {
            var e = c.EngineById[s.engine];
            NozzleOf(e, s.nozzle, out float nozzleD, out float eps);
            var spots = new List<Vector2>();
            float re = EngineSpots((float)s.diameter, nozzleD, s.engineCount, spots);
            exitD = Mathf.Max(exitD, re * 2);
            for (int n = 0; n < spots.Count; n++)
            {
                var p = spots[n];
                var off = origin + new Vector3(p.x, 0, p.y);
                exits.Add(off);
                slots.Add(new EngineSlot(off, re * 2f, eps, (float)e.gimbal, e.id, e.cycle, stage, slotBase + n, strap, (float)s.diameter * 0.5f));
            }
        }
    }
}
