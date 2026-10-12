using Kestrel.Kit;

namespace Kestrel.Site
{
    /// <summary>Environment share of the frame. Two draws: one combined land mesh, one water mesh. No textures.</summary>
    public static class SiteBudget
    {
        public const int DrawCalls = 2;
        public const int MaxTrisHigh = 48000;
        public const int MaxTrisLow = 18000;

        public static int Triangles(CoastalSite site, int lod) => site.Land(lod).TriangleCount + site.Water(lod).TriangleCount;

        public static void Estimate(CoastalSite site, int lod, BudgetLedger ledger) =>
            ledger.Add(DrawCalls, Triangles(site, lod), 0);
    }
}
