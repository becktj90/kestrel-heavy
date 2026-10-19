// Shared setup for the Starter tests: the real catalog, the campaign and each mission's campaign winner.
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;

namespace Kestrel.Core.Tests
{
    static class StarterFixture
    {
        static Catalog catalog;
        static Campaign campaign;

        public static Catalog Catalog => catalog ??= Catalog.LoadFromRepo(Golden.Root);
        public static Campaign Campaign => campaign ??= Campaign.Load(Golden.Root);

        public static void EnsureBases()
        {
            if (StarterBases.Active != null) return;
            StarterBases.Active = StarterBases.Parse(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-winners.json")));
        }

        public static StarterRocket Rocket(string missionId)
        {
            EnsureBases();
            return new StarterRocket(Catalog, Campaign.ById(missionId), StarterBases.For(missionId));
        }
    }
}
