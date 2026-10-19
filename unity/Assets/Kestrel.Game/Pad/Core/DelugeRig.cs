// Deluge. A water tower, a feed line, and rain-bird nozzles along the trench.
// Fires at ignition (the particle view). A solid on a rail has none (pad rule P4).
// A tower still has the water system when the vehicle is a solid: sound suppression, no cryo plumbing.
using System;
using Kestrel.Kit;
using Kestrel.Kit.Contracts;

namespace Kestrel.Pad
{
    public sealed class DelugeSpec
    {
        public bool Active;
        public float TowerX, TowerZ, TowerHeight;
        public float BearingDeg;
        public float ExitDiameter;
        public int Nozzles;
        public readonly float[] X = new float[PadVfxState.MaxNozzles];
        public readonly float[] Y = new float[PadVfxState.MaxNozzles];
        public readonly float[] Z = new float[PadVfxState.MaxNozzles];
        public readonly float[] DirX = new float[PadVfxState.MaxNozzles];
        public readonly float[] DirZ = new float[PadVfxState.MaxNozzles];
    }

    public static class DelugeRig
    {
        public const string RootId = "pad.deluge";

        public static bool Needed(PadLayout L)
        {
            if (L == null) return false;
            bool liquid = L.Propellant != null && L.Propellant.AnyLiquid;
            if (L.Archetype == PadArchetype.Rail) return liquid;
            return true;
        }

        public static void Apply(PadLayout L)
        {
            if (L.Graph.Contains(RootId)) return;
            var spec = new DelugeSpec();
            L.Deluge = spec;
            if (!Needed(L) || L.Trench == null) return;
            spec.Active = true;
            spec.ExitDiameter = L.Trench.ClusterDiameter;
            spec.BearingDeg = L.Trench.BearingDeg;
            float hv = Math.Max(12f, L.Fit.VehicleHeight);
            float reach = Math.Max(L.DeckRadius + 12f, Math.Min(hv * 1.6f, 160f));
            // Off the trench axis, so the tower is not in an exhaust lane.
            float bearing = PropellantFarm.ClearBearing(L, L.Trench.BearingDeg + 90f, reach);
            float rad = bearing * (float)(Math.PI / 180.0);
            float tx = (float)Math.Sin(rad) * reach, tz = (float)Math.Cos(rad) * reach;
            float tankR = Math.Max(1.4f, Math.Min(hv * 0.045f, 4.5f));
            float legH = Math.Max(8f, Math.Min(hv * 0.28f, 28f));
            spec.TowerX = tx; spec.TowerZ = tz; spec.TowerHeight = legH + tankR;

            var root = L.Graph.Add(RootId, ElementCategory.Structure, TowerLayout.SiteId, "deluge", "water");
            root.DisplayName = "Water tower";
            root.Center = new Vec3(tx, legH * 0.5f, tz);
            root.Size = new Vec3(tankR * 3f, legH + tankR * 2f, tankR * 3f);

            for (int i = 0; i < 4; i++)
            {
                float a = (i + 0.5f) * (float)(Math.PI / 2.0);
                float lx = tx + (float)Math.Cos(a) * tankR * 0.85f;
                float lz = tz + (float)Math.Sin(a) * tankR * 0.85f;
                PadLattice.Beam(L, RootId, "leg" + i, new Vec3(lx, 0f, lz), new Vec3(lx, legH, lz), MaterialRole.Galvanized, "Leg", "water-tower");
                int j = (i + 1) % 4;
                float a2 = (j + 0.5f) * (float)(Math.PI / 2.0);
                float mx = tx + (float)Math.Cos(a2) * tankR * 0.85f;
                float mz = tz + (float)Math.Sin(a2) * tankR * 0.85f;
                PadLattice.Beam(L, RootId, "x" + i, new Vec3(lx, legH * 0.25f, lz), new Vec3(mx, legH * 0.75f, mz), MaterialRole.Galvanized, "Brace", "water-tower");
            }
            PadLattice.Beam(L, RootId, "riser", new Vec3(tx, 0f, tz), new Vec3(tx, legH, tz), MaterialRole.Galvanized, "Riser", "water");
            float cy = legH + tankR * 0.15f;
            PadLattice.Ring(L, RootId, "tank", tx, cy, tz, tankR, 1, 6, MaterialRole.Galvanized, "Tank");
            PadLattice.Ring(L, RootId, "tank2", tx, cy + tankR * 0.7f, tz, tankR * 0.75f, 1, 6, MaterialRole.Galvanized, "Tank");

            // Feed stays on the tower's ray so the pipe cannot cross the vehicle or the plume wedge.
            float keep = L.Fit.VehicleRadius + 7f;
            float hx = (float)Math.Sin(rad) * keep, hz = (float)Math.Cos(rad) * keep;
            PadLattice.Beam(L, RootId, "feed", new Vec3(tx, 1.6f, tz), new Vec3(hx, 1.6f, hz), MaterialRole.Galvanized, "Pipe", "water");

            int n = L.Archetype == PadArchetype.Rail ? 6 : 10;
            if (n > PadVfxState.MaxNozzles) n = PadVfxState.MaxNozzles;
            float ring = Math.Max(keep, L.DeckRadius * 0.8f);
            spec.Nozzles = 0;
            for (int i = 0; i < 24 && spec.Nozzles < n; i++)
            {
                float nb = L.Trench.BearingDeg + 40f + i * (300f / 24f);
                float nr = nb * (float)(Math.PI / 180.0);
                float x = (float)Math.Sin(nr) * ring;
                float z = (float)Math.Cos(nr) * ring;
                if (TrenchGrammar.InPlumeSector(L.Trench, x, z, L.Fit.VehicleHeight)) continue;
                if (!PropellantFarm.Free(L, x, z)) continue;
                float dx = -x, dz = -z;
                float len = (float)Math.Sqrt(dx * dx + dz * dz);
                if (len < 0.01f) continue;
                dx /= len; dz /= len;
                int k = spec.Nozzles++;
                spec.X[k] = x; spec.Y[k] = 1.3f; spec.Z[k] = z; spec.DirX[k] = dx; spec.DirZ[k] = dz;
                PadLattice.Beam(L, RootId, "noz" + k, new Vec3(x, 0.6f, z), new Vec3(x + dx * 1.1f, 1.6f, z + dz * 1.1f), MaterialRole.Galvanized, "Nozzle", "rain-bird");
            }
        }
    }
}
