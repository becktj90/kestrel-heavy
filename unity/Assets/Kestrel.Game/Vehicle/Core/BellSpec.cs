// V1a engine bells (docs/VEHICLE_DETAIL_V1_SPEC.md §2). Engine-free: the shape comes from the
// catalog expansion ratio and cycle, never from a shipped mesh or a real engine's outline.
using System;

namespace Kestrel.Vehicle
{
    public enum EngineCycleKind
    {
        GasGenerator,
        StagedCombustion,
        FullFlow,
        Expander,
        PressureFed,
        Solid,
        Hybrid
    }

    /// <summary>Inputs for one bell mesh. Slot index is not here: every engine of a type on a vehicle shares one mesh.</summary>
    public struct BellSpec
    {
        public string EngineId;
        public EngineCycleKind Cycle;
        public float ExpansionRatio;
        public float ExitDiameter;   // m, exit plane
        public ulong VehicleSeed;

        public float ExitRadius => ExitDiameter * 0.5f;
        public bool PumpFed => Cycle != EngineCycleKind.PressureFed && Cycle != EngineCycleKind.Solid && Cycle != EngineCycleKind.Hybrid;
        public bool SolidLike => Cycle == EngineCycleKind.Solid || Cycle == EngineCycleKind.Hybrid;

        public static bool TryParseCycle(string cycle, out EngineCycleKind kind)
        {
            switch (cycle)
            {
                case "gas-generator": kind = EngineCycleKind.GasGenerator; return true;
                case "staged-combustion": kind = EngineCycleKind.StagedCombustion; return true;
                case "full-flow": kind = EngineCycleKind.FullFlow; return true;
                case "expander": kind = EngineCycleKind.Expander; return true;
                case "pressure-fed": kind = EngineCycleKind.PressureFed; return true;
                case "solid": kind = EngineCycleKind.Solid; return true;
                case "hybrid": kind = EngineCycleKind.Hybrid; return true;
                default: kind = EngineCycleKind.GasGenerator; return false;
            }
        }

        /// <summary>Problems that mean the contour must not be built. Empty means usable.</summary>
        public System.Collections.Generic.List<string> Validate()
        {
            var e = new System.Collections.Generic.List<string>();
            if (!(ExpansionRatio > 1.5f && ExpansionRatio < 2000f)) e.Add("ExpansionRatio must be in (1.5, 2000)");
            if (!(ExitDiameter > 0.05f && ExitDiameter < 20f)) e.Add("ExitDiameter must be in (0.05, 20) m");
            return e;
        }
    }

    /// <summary>Where one bell sits on the vehicle. Filled by the slice from the design; the cluster turns it into an instance.</summary>
    public struct BellMount
    {
        public string EngineId, Cycle;
        public float ExpansionRatio, ExitDiameter;
        public float X, Y, Z;
        public int Stage, Slot;
        public ulong Seed;
        /// <summary>Aft-skirt radius (m). Bay parts are placed inside it. 0 means the caller has no skirt yet.</summary>
        public float SkinRadius;
        public bool Gimbal;
    }

    /// <summary>Per-engine placement. Roll and the rest gimbal live on the instance, so the shared mesh stays one batch.</summary>
    public struct BellInstance
    {
        public BellSpec Spec;
        public int Stage;
        public int Slot;
        public float RollDeg;
        public float GimbalXDeg;
        public float GimbalZDeg;
    }

    /// <summary>How many samples a bell LOD keeps. Matches the spec: 32/16/8 around, 24/12/6 along.</summary>
    public static class BellLod
    {
        public static int Radial(int lod) => lod <= 0 ? 32 : lod == 1 ? 16 : 8;
        public static int Axial(int lod) => lod <= 0 ? 24 : lod == 1 ? 12 : 6;
        /// <summary>Visible engine-bay triangles for the whole vehicle (audit V1 share).</summary>
        public static int TriangleCap(int quality) => quality <= 0 ? 35000 : quality == 1 ? 80000 : 130000;
        public static int DrawCallCap(int quality) => quality <= 0 ? 20 : quality == 1 ? 32 : 40;
    }
}
