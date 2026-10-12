using System;

namespace Kestrel.Vehicle
{
    /// <summary>Exact triangle counts for one tank at one LOD, from the same numbers TankShellBuilder uses
    /// (KitMesh.Tube: (pathPoints - 1) * sides * 2; circle paths have RadialSegments + 1 points).</summary>
    public static class TankBudget
    {
        public static int TubeTriangles(int pathPoints, int sides) => Math.Max(0, pathPoints - 1) * Math.Max(3, sides) * 2;

        public static int Triangles(TankShellSpec s, TankLod lod, out int shell, out int hardware)
            => Triangles(TankLayout.Build(s), lod, out shell, out hardware);

        public static int Triangles(TankLayout layout, TankLod lod, out int shell, out int hardware)
        {
            var s = layout.Fitted;
            shell = LatheMesh.CountTriangles(TankProfile.Build(s, layout, lod), lod.RadialSegments);
            int S = lod.RadialSegments, weldRing = TubeTriangles(S + 1, lod.WeldSides);
            hardware = 0;
            if (lod.CircWelds) hardware += layout.CircWelds.Count * weldRing;
            if (lod.DomeWelds) hardware += layout.DomeWelds.Count * weldRing;
            if (lod.LongWelds) hardware += layout.LongWelds.Count * TubeTriangles(2, lod.WeldSides);
            if (lod.Stiffeners) hardware += layout.Stiffeners.Count * TubeTriangles(S + 1, lod.StiffenerSides);
            return shell + hardware;
        }
    }
}
