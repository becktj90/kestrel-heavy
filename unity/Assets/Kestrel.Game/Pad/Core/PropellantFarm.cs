// Propellant farm. The pad's tanks follow the vehicle: kerolox gets an RP-1 farm and LOX tanks,
// methalox gets methane and LOX spheres, hydrolox gets a hydrogen sphere with a vent stack,
// and a solid gets no cryo plumbing. Layout only — beams the pad already draws.
// Standoff and the plume keep-out follow docs/PAD_DETAIL_GRAMMAR.md §3–§4. No real facility.
using System;
using System.Collections.Generic;
using Kestrel.Kit;
using Kestrel.Kit.Contracts;

namespace Kestrel.Pad
{
    /// <summary>Which propellant families a vehicle loads. Parsed from catalog ids, not from a scene.</summary>
    public sealed class FarmPlan
    {
        public bool Kerolox, Methalox, Hydrolox, Solid, Storable;
        public bool Lox => Kerolox || Methalox || Hydrolox;
        public bool CryoPlumbing => Kerolox || Methalox || Hydrolox;
        public bool AnyLiquid => CryoPlumbing || Storable;
        public int Key => (Kerolox ? 1 : 0) | (Methalox ? 2 : 0) | (Hydrolox ? 4 : 0) | (Solid ? 8 : 0) | (Storable ? 16 : 0);

        public static FarmPlan FromIds(IEnumerable<string> ids)
        {
            var p = new FarmPlan();
            if (ids == null) return p;
            foreach (var raw in ids)
            {
                var id = (raw ?? "").Trim().ToLowerInvariant();
                if (id.Length == 0) continue;
                if (id.IndexOf("kerolox", StringComparison.Ordinal) >= 0 || id.IndexOf("rp-1", StringComparison.Ordinal) >= 0 || id.IndexOf("rp1", StringComparison.Ordinal) >= 0)
                    p.Kerolox = true;
                else if (id.IndexOf("methalox", StringComparison.Ordinal) >= 0 || id.IndexOf("methane", StringComparison.Ordinal) >= 0)
                    p.Methalox = true;
                else if (id.IndexOf("hydrolox", StringComparison.Ordinal) >= 0 || id.IndexOf("hydrogen", StringComparison.Ordinal) >= 0 || id.IndexOf("lh2", StringComparison.Ordinal) >= 0)
                    p.Hydrolox = true;
                else if (id.IndexOf("solid", StringComparison.Ordinal) >= 0 || id.IndexOf("apcp", StringComparison.Ordinal) >= 0 || id.IndexOf("htpb", StringComparison.Ordinal) >= 0)
                    p.Solid = true;
                else p.Storable = true;
            }
            return p;
        }
    }

    public static class PropellantFarm
    {
        public const string RootId = "pad.farm";

        public static void Apply(PadLayout L)
        {
            var plan = L.Propellant;
            if (plan == null || !plan.CryoPlumbing || L.Graph.Contains(RootId)) return;
            var root = L.Graph.Add(RootId, ElementCategory.Structure, TowerLayout.SiteId, "farm", "propellant");
            root.DisplayName = "Propellant farm";
            root.Center = new Vec3(0, 4f, 0);
            float hv = Math.Max(12f, L.Fit.VehicleHeight);
            float reach = Math.Max(L.DeckRadius + 18f, Math.Min(hv * 2.4f, 200f));
            var rng = new PadRng(L.Seed ^ unchecked((int)0xF4A11C0));
            float fuelBr = ClearBearing(L, rng.Range(0f, 360f), reach);
            float oxBr = ClearBearing(L, fuelBr + 180f, reach * 0.92f);
            bool loxSphere = plan.Methalox || (plan.Hydrolox && !plan.Kerolox);
            if (plan.Kerolox)
            {
                HorizontalFarm(L, "rp1", "RP-1 farm", fuelBr, reach, hv * 0.07f, 3, MaterialRole.PaintedSteel);
                VerticalTanks(L, "lox", "LOX tanks", oxBr, reach * 0.92f, hv * 0.09f, 2, MaterialRole.Insulation);
            }
            if (plan.Methalox)
            {
                float br = plan.Kerolox ? ClearBearing(L, fuelBr + 40f, reach * 1.08f) : fuelBr;
                Sphere(L, "ch4", "Methane sphere", br, reach * (plan.Kerolox ? 1.08f : 1f), hv * 0.11f, MaterialRole.Insulation, false);
                if (!plan.Kerolox) Sphere(L, "lox", "LOX sphere", oxBr, reach * 0.92f, hv * 0.09f, MaterialRole.Insulation, false);
            }
            if (plan.Hydrolox)
            {
                float br = ClearBearing(L, fuelBr + (plan.Methalox ? -36f : 0f), reach * 1.15f);
                if (!plan.Kerolox && !plan.Methalox) br = fuelBr;
                Sphere(L, "lh2", "Hydrogen sphere", br, reach * 1.05f, hv * 0.14f, MaterialRole.Insulation, true);
                if (loxSphere) Sphere(L, "lox", "LOX sphere", oxBr, reach * 0.9f, hv * 0.09f, MaterialRole.Insulation, false);
            }
        }

        static void HorizontalFarm(PadLayout L, string id, string label, float bearing, float reach, float radius, int count, MaterialRole mat)
        {
            var node = L.Graph.Add(RootId + "." + id, ElementCategory.Tank, RootId, "fuel", "kerosene", id);
            node.DisplayName = label;
            float r = Math.Max(1.6f, Math.Min(radius, 6f));
            float len = r * 3.2f;
            float along = (float)Math.Sin(bearing * Math.PI / 180.0), across = (float)Math.Cos(bearing * Math.PI / 180.0);
            float ox = along * reach, oz = across * reach;
            node.Center = new Vec3(ox, r + 0.6f, oz);
            node.Size = new Vec3(len, r * 2f, r * 2f);
            for (int i = 0; i < count; i++)
            {
                float side = (i - (count - 1) * 0.5f) * (r * 2.4f);
                float cx = ox + across * side, cz = oz - along * side;
                Cylinder(L, RootId + "." + id, id + i, cx, r + 0.5f, cz, r, len, 0, mat);
                PadLattice.Beam(L, RootId + "." + id, id + i + "a", new Vec3(cx - len * 0.3f, 0f, cz), new Vec3(cx - len * 0.3f, r * 0.5f, cz), MaterialRole.Galvanized, "Leg", "saddle");
                PadLattice.Beam(L, RootId + "." + id, id + i + "b", new Vec3(cx + len * 0.3f, 0f, cz), new Vec3(cx + len * 0.3f, r * 0.5f, cz), MaterialRole.Galvanized, "Leg", "saddle");
            }
            Pipe(L, RootId + "." + id, id + "p", ox, r, oz, bearing);
        }

        static void VerticalTanks(PadLayout L, string id, string label, float bearing, float reach, float radius, int count, MaterialRole mat)
        {
            var node = L.Graph.Add(RootId + "." + id, ElementCategory.Tank, RootId, "oxidizer", "cryo", id);
            node.DisplayName = label;
            float r = Math.Max(1.8f, Math.Min(radius, 7f));
            float h = r * 2.6f;
            float along = (float)Math.Sin(bearing * Math.PI / 180.0), across = (float)Math.Cos(bearing * Math.PI / 180.0);
            float ox = along * reach, oz = across * reach;
            node.Center = new Vec3(ox, h * 0.5f, oz);
            node.Size = new Vec3(r * 4f, h, r * 4f);
            for (int i = 0; i < count; i++)
            {
                float side = (i - (count - 1) * 0.5f) * (r * 2.6f);
                float cx = ox + across * side, cz = oz - along * side;
                Cylinder(L, RootId + "." + id, id + i, cx, 0.4f + h * 0.5f, cz, r, h, 1, mat);
            }
            Pipe(L, RootId + "." + id, id + "p", ox, 1.2f, oz, bearing);
        }

        static void Sphere(PadLayout L, string id, string label, float bearing, float reach, float radius, MaterialRole mat, bool vent)
        {
            if (L.Graph.Contains(RootId + "." + id)) return;
            var node = L.Graph.Add(RootId + "." + id, ElementCategory.Tank, RootId, vent ? "fuel" : "cryo", "sphere", id);
            node.DisplayName = label;
            float r = Math.Max(2.2f, Math.Min(radius, 9f));
            float along = (float)Math.Sin(bearing * Math.PI / 180.0), across = (float)Math.Cos(bearing * Math.PI / 180.0);
            float cx = along * reach, cz = across * reach;
            float cy = r + 1.6f;
            node.Center = new Vec3(cx, cy, cz);
            node.Size = new Vec3(r * 2f, r * 2f + (vent ? r : 0f), r * 2f);
            const int n = 6;
            for (int k = 0; k < 3; k++)
            {
                float t = 0.35f + k * 0.3f;
                float yy = cy - r + 2f * r * t;
                float rr = r * (float)Math.Sin(t * Math.PI);
                PadLattice.Ring(L, RootId + "." + id, id + "h" + k, cx, yy, cz, Math.Max(0.4f, rr), 1, n, mat, "Ring");
            }
            for (int m = 0; m < 4; m++)
            {
                float a = m * (float)(Math.PI / 2.0);
                float px = (float)Math.Cos(a), pz = (float)Math.Sin(a);
                var prev = new Vec3(cx, cy - r, cz);
                for (int s = 1; s <= 3; s++)
                {
                    float t = s / 3f * (float)Math.PI;
                    var p = new Vec3(cx + px * r * (float)Math.Sin(t), cy - r * (float)Math.Cos(t), cz + pz * r * (float)Math.Sin(t));
                    PadLattice.Beam(L, RootId + "." + id, id + "m" + m + "s" + s, prev, p, mat, "Meridian", "sphere");
                    prev = p;
                }
            }
            for (int leg = 0; leg < 4; leg++)
            {
                float a = (leg + 0.5f) * (float)(Math.PI / 2.0);
                float lx = cx + (float)Math.Cos(a) * r * 0.55f, lz = cz + (float)Math.Sin(a) * r * 0.55f;
                PadLattice.Beam(L, RootId + "." + id, id + "leg" + leg, new Vec3(lx, 0f, lz), new Vec3(lx, cy - r * 0.7f, lz), MaterialRole.Galvanized, "Leg", "skirt");
            }
            if (vent)
            {
                float top = cy + r;
                float stack = Math.Max(6f, Math.Min(r * 1.8f, 18f));
                PadLattice.Beam(L, RootId + "." + id, id + "vent", new Vec3(cx, top, cz), new Vec3(cx, top + stack, cz), MaterialRole.Insulation, "Vent stack", "vent");
                PadLattice.Ring(L, RootId + "." + id, id + "ventcap", cx, top + stack, cz, 0.8f, 1, 5, MaterialRole.Galvanized, "Vent");
                var v = L.Graph.Get(RootId + "." + id);
                v.DisplayName = "Hydrogen sphere";
            }
            Pipe(L, RootId + "." + id, id + "p", cx, 1.2f, cz, bearing);
        }

        static void Cylinder(PadLayout L, string parent, string id, float cx, float cy, float cz, float radius, float length, int axis, MaterialRole mat)
        {
            const int n = 6;
            float half = length * 0.5f;
            for (int k = 0; k < 2; k++)
            {
                float o = k == 0 ? -half : half;
                float x = cx, y = cy, z = cz;
                if (axis == 0) x += o; else y += o;
                PadLattice.Ring(L, parent, id + "e" + k, x, y, z, radius, axis, n, mat, "Ring");
            }
            for (int s = 0; s < n; s++)
            {
                float a = s * (float)(Math.PI * 2.0 / n);
                float c = (float)Math.Cos(a) * radius, sn = (float)Math.Sin(a) * radius;
                Vec3 A, B;
                if (axis == 0) { A = new Vec3(cx - half, cy + c, cz + sn); B = new Vec3(cx + half, cy + c, cz + sn); }
                else { A = new Vec3(cx + c, cy - half, cz + sn); B = new Vec3(cx + c, cy + half, cz + sn); }
                PadLattice.Beam(L, parent, id + "l" + s, A, B, mat, "Stave", "tank");
            }
        }

        /// <summary>A short run toward the pad that stops outside the vehicle. Cryo lines stay on their own side.</summary>
        static void Pipe(PadLayout L, string parent, string id, float x, float y, float z, float bearing)
        {
            float keep = L.Fit.VehicleRadius + 8f;
            float along = (float)Math.Sin(bearing * Math.PI / 180.0), across = (float)Math.Cos(bearing * Math.PI / 180.0);
            float endR = Math.Max(keep, 14f);
            var a = new Vec3(x, y, z);
            var b = new Vec3(along * (endR + 10f), 1.4f, across * (endR + 10f));
            var c = new Vec3(along * endR, 1.1f, across * endR);
            PadLattice.Beam(L, parent, id + "a", a, b, MaterialRole.Insulation, "Pipe", "cryo-line");
            PadLattice.Beam(L, parent, id + "b", b, c, MaterialRole.Insulation, "Pipe", "cryo-line");
        }

        public static float ClearBearing(PadLayout L, float startDeg, float reach)
        {
            for (int i = 0; i < 24; i++)
            {
                float b = startDeg + i * 15f;
                float rad = b * (float)(Math.PI / 180.0);
                float x = (float)Math.Sin(rad) * reach, z = (float)Math.Cos(rad) * reach;
                float inner = Math.Max(L.Fit.VehicleRadius + 8f, 14f);
                float xi = (float)Math.Sin(rad) * inner, zi = (float)Math.Cos(rad) * inner;
                if (Free(L, x, z) && Free(L, xi, zi)) return b;
            }
            return startDeg;
        }

        public static bool Free(PadLayout L, float x, float z)
        {
            float keep = L.Fit.VehicleRadius + 6f;
            if (x * x + z * z < keep * keep) return false;
            if (L.Trench != null && TrenchGrammar.InPlumeSector(L.Trench, x, z, L.Fit.VehicleHeight)) return false;
            if (L.Tower != null)
            {
                float dx = x, dz = z - L.Tower.StandOff;
                if (Math.Abs(dx) < L.Tower.Width * 0.7f && Math.Abs(dz) < L.Tower.Width * 0.7f) return false;
            }
            return true;
        }
    }

    /// <summary>Shared beam/ring writer. Members reuse kit materials the tower already batches.</summary>
    static class PadLattice
    {
        public static void Beam(PadLayout L, string parent, string id, Vec3 a, Vec3 b, MaterialRole mat, string label, string tag)
        {
            string full = parent + "." + id;
            if (L.Graph.Contains(full)) return;
            PadGenerator.AddMember(L, parent, full, MemberKind.Brace, a, b, mat, tag);
            var e = L.Graph.Get(full);
            if (e != null) e.DisplayName = label;
        }

        public static void Ring(PadLayout L, string parent, string prefix, float cx, float cy, float cz, float radius, int axis, int n, MaterialRole mat, string label)
        {
            Vec3 At(int i)
            {
                float a = i * (float)(Math.PI * 2.0 / n);
                float c = (float)Math.Cos(a) * radius, s = (float)Math.Sin(a) * radius;
                if (axis == 0) return new Vec3(cx, cy + c, cz + s);
                if (axis == 2) return new Vec3(cx + c, cy + s, cz);
                return new Vec3(cx + c, cy, cz + s);
            }
            for (int i = 0; i < n; i++)
                Beam(L, parent, prefix + "s" + i, At(i), At(i + 1), mat, label, "ring");
        }
    }
}
