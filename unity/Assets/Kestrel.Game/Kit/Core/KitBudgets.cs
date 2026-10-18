// Shared detail kit budgets + LOD policy (engine-free). Quality levels match Kestrel.Game.Quality:
// 0 = Low, 1 = Medium, 2 = High. Frame targets: 120 Hz on High, 60 Hz otherwise.
using System;

namespace Kestrel.Kit
{
    public enum MaterialRole { PaintedSteel, Galvanized, Concrete, RubberHose, Insulation, WarningStripes, SafetyYellowSteel, Glass, Refractory }

    public static class KitBudgets
    {
        public const int Low = 0, Medium = 1, High = 2;
        /// <summary>Environment art (pad + kit + environment textures/meshes held in memory or in the IPA): 8 MB.</summary>
        public const long EnvironmentArtBytes = 8L * 1024 * 1024;

        public static int TargetHz(int q) => q >= High ? 120 : 60;
        /// <summary>Draw calls the pad scene may spend (vehicle, plume, UI excluded).</summary>
        public static int PadDrawCalls(int q) => Pick(q, 60, 110, 160);
        /// <summary>Visible pad triangles per frame.</summary>
        public static int PadTriangles(int q) => Pick(q, 150_000, 300_000, 500_000);
        /// <summary>Realtime (per-pixel) lights the pad may add on top of the sun. Everything else is emissive-only.</summary>
        public static int PadRealtimeLights(int q) => Pick(q, 0, 2, 5);
        /// <summary>Part of PadRealtimeLights reserved for the throttle-driven plume point light (High only); fixtures get the rest.</summary>
        public static int PadPlumeLights(int q) => Pick(q, 0, 0, 1);
        /// <summary>Of those, how many may cast shadows.</summary>
        public static int PadShadowedLights(int q) => Pick(q, 0, 0, 1);
        /// <summary>Max instances per RenderMeshInstanced batch (Unity caps at 1023 for the matrix path).</summary>
        public const int MaxInstancesPerBatch = 1023;

        internal static int Pick(int q, int lo, int mid, int hi) => q <= Low ? lo : q == Medium ? mid : hi;
    }

    /// <summary>Distance-based LOD. Size classes so small props drop out long before towers do.</summary>
    public enum DetailSize { Prop, Small, Medium, Large }   // bolt/valve, pipe run/hose, arm/tank, tower/trench

    public static class LodPolicy
    {
        // [quality][size] -> max distance (m) for LOD0, LOD1, and culling. Portrait FOV ~ 50 deg vertical.
        static readonly float[,,] T =
        {
            { { 15, 40, 80 },  { 30, 80, 200 },  { 60, 180, 600 },  { 150, 600, 4000 } },   // Low
            { { 25, 60, 120 }, { 50, 140, 350 }, { 100, 300, 900 }, { 250, 900, 6000 } },   // Medium
            { { 40, 100, 200 },{ 80, 220, 500 }, { 160, 450, 1400 },{ 400, 1400, 9000 } },  // High
        };

        public static float Lod0Max(int q, DetailSize s) => T[Clamp(q), (int)s, 0];
        public static float Lod1Max(int q, DetailSize s) => T[Clamp(q), (int)s, 1];
        public static float CullDistance(int q, DetailSize s) => T[Clamp(q), (int)s, 2];

        /// <summary>0, 1, 2 (lowest LOD), or -1 (culled). Distances scale by lodBias (>1 keeps detail further).</summary>
        public static int Select(int q, DetailSize s, float distance, float lodBias = 1f)
        {
            float d = distance / Math.Max(0.01f, lodBias);
            if (d <= Lod0Max(q, s)) return 0;
            if (d <= Lod1Max(q, s)) return 1;
            return d <= CullDistance(q, s) ? 2 : -1;
        }

        /// <summary>Radial segments for pipes/hoses at a given LOD.</summary>
        public static int RadialSegments(int lod) => lod <= 0 ? 12 : lod == 1 ? 6 : 3;

        static int Clamp(int q) => q < 0 ? 0 : q > 2 ? 2 : q;
    }

    /// <summary>Budget tracker. Pad systems register what they will draw; tests and a debug overlay read it.</summary>
    public sealed class BudgetLedger
    {
        public int DrawCalls { get; private set; }
        public long Triangles { get; private set; }
        public long ArtBytes { get; private set; }
        public void Add(int drawCalls, long triangles, long artBytes = 0) { DrawCalls += drawCalls; Triangles += triangles; ArtBytes += artBytes; }
        public void Reset() { DrawCalls = 0; Triangles = 0; ArtBytes = 0; }
        public bool Within(int q) => DrawCalls <= KitBudgets.PadDrawCalls(q) && Triangles <= KitBudgets.PadTriangles(q) && ArtBytes <= KitBudgets.EnvironmentArtBytes;
        public string Report(int q) => $"draws {DrawCalls}/{KitBudgets.PadDrawCalls(q)}, tris {Triangles}/{KitBudgets.PadTriangles(q)}, art {ArtBytes / 1024} KB/{KitBudgets.EnvironmentArtBytes / 1024} KB";
    }
}
