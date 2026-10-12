using System;

namespace Kestrel.Vehicle
{
    /// <summary>Triangle estimates for one tank at one LOD. tubeSides = Kestrel.Kit.LodPolicy.RadialSegments(lod)
    /// (passed in so this assembly stays Contracts-only). Must match TankShellBuilder within a few percent.</summary>
    public static class TankBudget
    {
        public const int StiffenerSides = 4;   // low rectangular-ish hat ring

        public static int ShellTriangles(int profilePoints, int poles, int segments)
            => Math.Max(0, (profilePoints - 1) * 2 * segments - poles * segments);

        public static int Triangles(TankShellSpec s, TankLayout layout, TankLod lod, int tubeSides, out int shell, out int hardware)
        {
            var prof = TankProfile.Build(s, layout, lod);
            int S = lod.RadialSegments;
            shell = ShellTriangles(prof.Count, 2, S);
            int ring = S * tubeSides * 2;
            hardware = 0;
            if (lod.CircWelds) hardware += layout.CircWelds.Count * ring;
            if (lod.DomeWelds) hardware += layout.DomeWelds.Count * ring;
            if (lod.LongWelds) hardware += layout.LongWelds.Count * tubeSides * 2;
            if (lod.Stiffeners) hardware += layout.Stiffeners.Count * S * StiffenerSides * 2;
            return shell + hardware;
        }
    }
}
