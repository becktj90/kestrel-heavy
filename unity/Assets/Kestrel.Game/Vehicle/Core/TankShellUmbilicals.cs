// Attach points on a tank shell's outer mould line. Stations and azimuths are fictional; the radius at each
// station comes from the shell profile, so designer diameter and barrel length move the plates with the tank.
// Vehicle frame: +Y up, +Z toward the tower. Pad code consumes the AttachPoints and does not read this type.
using System;
using System.Collections.Generic;
using Kestrel.Kit.Contracts;

namespace Kestrel.Vehicle
{
    public static class TankShellUmbilicals
    {
        /// <summary>Tank-local origin (aft reference) for a barrel whose bottom is at <paramref name="barrelBottomY"/> in vehicle space.</summary>
        public static Vec3 TankOrigin(float barrelBottomY, float radius, float x, float z)
        {
            float dome = TankShellSpec.Default(Math.Max(radius, 0.05f), 1f, 1).DomeHeight;
            return new Vec3(x, barrelBottomY - dome, z);
        }

        /// <summary>Outer radius and meridian normal at a tank-local height. False when y is outside the shell.</summary>
        public static bool TrySkin(TankShellSpec spec, float yLocal, out float radius, out float nr, out float ny)
        {
            radius = spec.Radius; nr = 1f; ny = 0f;
            var layout = TankLayout.Build(spec);
            var prof = TankProfile.Build(spec, layout, TankLod.For(2));
            if (prof.Count == 0) return false;
            if (yLocal < prof[0].Y - 1e-3f || yLocal > prof[prof.Count - 1].Y + 1e-3f) return false;
            for (int i = 1; i < prof.Count; i++)
            {
                var a = prof[i - 1]; var b = prof[i];
                if (yLocal > b.Y + 1e-5f) continue;
                if (Math.Abs(b.Y - a.Y) < 1e-5f)
                {
                    var p = a.NR >= b.NR ? a : b;
                    radius = p.R; nr = p.NR; ny = p.NY; return true;
                }
                float u = (yLocal - a.Y) / (b.Y - a.Y);
                if (u < 0f) u = 0f; else if (u > 1f) u = 1f;
                radius = a.R + (b.R - a.R) * u;
                nr = a.NR + (b.NR - a.NR) * u;
                ny = a.NY + (b.NY - a.NY) * u;
                float n = (float)Math.Sqrt(nr * nr + ny * ny);
                if (n > 1e-6f) { nr /= n; ny /= n; }
                return true;
            }
            var last = prof[prof.Count - 1];
            radius = last.R; nr = last.NR; ny = last.NY; return true;
        }

        /// <summary>
        /// Plates for one tank. <paramref name="index"/> keeps strap-on ids distinct from the core (<c>s0.fuel.0</c> vs <c>s0.fuel.1</c>).
        /// <paramref name="propellantRelease"/> is SwingArmRetract on upper stages, TailServiceMastDrop on a core first stage,
        /// LanyardPull on a strap-on. Solids omit fuel, oxidizer, vent and purge.
        /// </summary>
        public static List<AttachPoint> ForStage(TankShellSpec spec, int stage, int index, Vec3 tankOrigin,
            bool liquid, bool gimbal, bool purge, ReleaseStyle propellantRelease)
        {
            var list = new List<AttachPoint>();
            float scale = DiameterScale(spec.Radius * 2f);
            void Plate(UmbilicalType type, ReleaseStyle rel, float frac, float azDeg, float radiusM)
            {
                float y = spec.BarrelLength < 0.05f
                    ? (spec.ShellBottom + spec.ShellTop) * 0.5f
                    : spec.BarrelBottom + spec.BarrelLength * frac;
                if (!TrySkin(spec, y, out float r, out float nr, out float ny)) return;
                float az = azDeg * (float)(Math.PI / 180.0);
                float s = (float)Math.Sin(az), c = (float)Math.Cos(az);
                var n = Norm(nr * s, ny, nr * c);
                var up = Math.Abs(n.Y) > 0.85f ? new Vec3(0, 0, 1) : new Vec3(0, 1, 0);
                list.Add(new AttachPoint
                {
                    Id = UmbilicalIds.Make(stage, type, index),
                    Type = type, Stage = stage, Release = rel,
                    PlateRadius = Math.Max(0.08f, radiusM),
                    Pose = new LocalPose(new Vec3(tankOrigin.X + r * s, tankOrigin.Y + y, tankOrigin.Z + r * c), ContractMath.LookRotation(n, up)),
                });
            }
            if (liquid)
            {
                Plate(UmbilicalType.Fuel, propellantRelease, 0.28f, 7f, 0.22f * scale);
                Plate(UmbilicalType.Oxidizer, propellantRelease, 0.62f, -9f, 0.22f * scale);
                Plate(UmbilicalType.Vent, ReleaseStyle.Hinged, 0.48f, 3f, 0.10f * scale);
            }
            if (liquid && purge) Plate(UmbilicalType.Purge, ReleaseStyle.Hinged, 0.36f, -16f, 0.10f * scale);
            if (gimbal) Plate(UmbilicalType.Hydraulic, ReleaseStyle.Hinged, 0.18f, 14f, 0.10f * scale);
            Plate(UmbilicalType.PowerData, ReleaseStyle.LanyardPull, 0.84f, -5f, 0.12f * scale);
            return list;
        }

        /// <summary>0.6–1.6 from a 3.7 m reference diameter, so a small designer rocket gets smaller plates.</summary>
        public static float DiameterScale(float diameter)
        {
            float t = diameter / 3.7f;
            if (t < 0.6f) return 0.6f;
            if (t > 1.6f) return 1.6f;
            return t;
        }

        static Vec3 Norm(float x, float y, float z)
        {
            float n = (float)Math.Sqrt(x * x + y * y + z * z);
            return n < 1e-6f ? new Vec3(0, 0, 1) : new Vec3(x / n, y / n, z / n);
        }
    }
}
