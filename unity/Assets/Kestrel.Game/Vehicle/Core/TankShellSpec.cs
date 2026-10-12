// V2 tank shells: engine-free description, layout and lathe geometry (docs/VEHICLE_DETAIL_V1_SPEC.md §9, audit V2).
// Units: metres. Tank-local frame: origin at the aft dome pole on the centre line, +Y up the stack,
// azimuth 0 = +Z (toward the pad tower, same as Kestrel.Kit.Contracts), azimuth grows toward +X.
// No real vehicle dimensions: everything comes from the player's Design (radius, barrel length) and seeds.
using System;
using System.Collections.Generic;

namespace Kestrel.Vehicle
{
    /// <summary>How a tank end closes. Flat = skirted end flush with the barrel (nothing below/above the barrel end);
    /// Ellipsoidal = dome inset by DomeInset so it can never stand proud of the barrel or the body it overlays.</summary>
    public enum DomeStyle { Ellipsoidal, Flat }

    [Serializable]
    public struct TankShellSpec
    {
        public float Radius;          // m, barrel outer radius
        public float BarrelLength;    // m, cylindrical section (0 = sphere-ish tank of two domes)
        public float DomeRatio;       // dome height / radius (ellipsoidal). 0.707 is a common generic choice.
        public float PanelLength;     // m, max barrel section length between circumferential welds
        public float MaxPanelWidth;   // m, max skin panel width between longitudinal welds
        public float StiffenerPitch;  // m, target spacing of stiffener rings inside a barrel section
        public float WeldClearance;   // m, keep stiffener rings this far from any weld
        public float IsogridCell;     // m, isogrid triangle edge length (normal map + cutaway ribs)
        public ulong Seed;            // per serial / design hash
        public DomeStyle AftDome, FwdDome;
        public float DomeInset;       // fraction of Radius the dome equator sits inside the barrel (lip step), >= 0.005

        public static TankShellSpec Default(float radius, float barrelLength, ulong seed) => new TankShellSpec
        {
            Radius = radius, BarrelLength = barrelLength, DomeRatio = 0.707f, PanelLength = 2.4f, MaxPanelWidth = 4.0f,
            StiffenerPitch = 0.8f, WeldClearance = 0.15f, IsogridCell = 0.30f, Seed = seed,
            // Aft end flat: on the vehicle the aft dome would hang into the engine bay (the #106 bulge). Fwd dome inset.
            AftDome = DomeStyle.Flat, FwdDome = DomeStyle.Ellipsoidal, DomeInset = 0.01f
        };

        /// <summary>Frame offset: the barrel always starts at Y = DomeHeight (callers place the tank with it),
        /// whatever the aft style. A Flat aft end has nothing below BarrelBottom.</summary>
        public float DomeHeight => DomeRatio * Radius;
        public float DomeRadius => Radius * (1 - DomeInset);
        public float DomeRise => DomeRatio * DomeRadius;
        public float BarrelBottom => DomeHeight;
        public float BarrelTop => DomeHeight + BarrelLength;
        public float ShellBottom => AftDome == DomeStyle.Flat ? BarrelBottom : BarrelBottom - DomeRise;
        public float ShellTop => FwdDome == DomeStyle.Flat ? BarrelTop : BarrelTop + DomeRise;
        public float TotalHeight => ShellTop - ShellBottom;

        public string Key => FormattableString.Invariant($"{Radius:R}|{BarrelLength:R}|{DomeRatio:R}|{PanelLength:R}|{MaxPanelWidth:R}|{StiffenerPitch:R}|{WeldClearance:R}|{IsogridCell:R}|{Seed}|{AftDome}|{FwdDome}|{DomeInset:R}");

        public List<string> Validate()
        {
            var e = new List<string>();
            if (!(Radius > 0.05f)) e.Add("Radius must be > 0.05 m");
            if (!(BarrelLength >= 0)) e.Add("BarrelLength must be >= 0");
            if (!(DomeRatio >= 0.3f && DomeRatio <= 1.0f)) e.Add("DomeRatio must be in [0.3, 1]");
            if (!(PanelLength > 0.2f)) e.Add("PanelLength must be > 0.2 m");
            if (!(MaxPanelWidth > 0.2f)) e.Add("MaxPanelWidth must be > 0.2 m");
            if (!(StiffenerPitch > 0.05f)) e.Add("StiffenerPitch must be > 0.05 m");
            if (!(WeldClearance >= 0)) e.Add("WeldClearance must be >= 0");
            if (!(IsogridCell > 0.02f)) e.Add("IsogridCell must be > 0.02 m");
            if (!(DomeInset >= 0.005f && DomeInset <= 0.1f)) e.Add("DomeInset must be in [0.005, 0.1]");
            return e;
        }
    }

    /// <summary>Per-LOD tessellation. LOD index matches Kestrel.Kit.LodPolicy (0 = closest).</summary>
    public readonly struct TankLod
    {
        public readonly int RadialSegments, DomeSamples, WeldSides, StiffenerSides;
        public readonly bool CircWelds, LongWelds, DomeWelds, Stiffeners;
        public TankLod(int radial, int dome, bool circ, bool lng, bool domeW, bool stiff, int weldSides, int stiffSides)
        { RadialSegments = radial; DomeSamples = dome; CircWelds = circ; LongWelds = lng; DomeWelds = domeW; Stiffeners = stiff; WeldSides = weldSides; StiffenerSides = stiffSides; }

        /// <summary>LOD0 = full hero detail; LOD1 keeps circumferential seams + rings; LOD2 is a bare lathe
        /// (seams/isogrid carried by the normal map only).</summary>
        /// Weld beads are 12 mm and rings 25 mm: 4 / 3 tube sides read as round at chase distance (12 was the 2x overshoot in #106).
        public static TankLod For(int lod) => lod <= 0 ? new TankLod(48, 12, true, true, true, true, 4, 4)
                                            : lod == 1 ? new TankLod(24, 6, true, false, false, true, 3, 3)
                                            : new TankLod(12, 3, false, false, false, false, 3, 3);
    }

    /// <summary>Deterministic splitmix64; System.Random is not used so seeds are stable across runtimes.</summary>
    public static class VehicleRng
    {
        public static ulong Mix(ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            return x ^ (x >> 31);
        }
        /// <summary>Uniform [0, 1) from (seed, stream).</summary>
        public static double Unit(ulong seed, ulong stream) => (Mix(seed ^ Mix(stream)) >> 11) * (1.0 / (1UL << 53));
    }
}
