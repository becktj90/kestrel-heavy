// Report exits. Next mission, one more launch, Missions, and Home.
// One more launch keeps the mission and the seed. Nothing here rolls a new seed,
// starts a clock, or clears a best.
namespace Kestrel.Core.Flight
{
    public static class ReportRoutes
    {
        public const string NextLabel = "Next mission";
        public const string AgainLabel = "One more launch";
        public const string MissionsLabel = "Missions";
        public const string HomeLabel = "Home";

        public const string NextTip = "Next mission. Opens the list.";
        public const string AgainTip = "One more launch. Same mission, same seed.";
        public const string MissionsTip = "Back to the mission list.";
        public const string HomeTip = "Exit to Home.";

        public static ulong SeedOf(MissionContext mission) => mission != null ? mission.Seed : 0;

        public static RouteResult Next(Campaign campaign) => GameSession.Apply(RouteId.ReportNext, campaign);

        /// <summary>Same mission, same seed. Unlocks and bests stay.</summary>
        public static RouteResult Again()
        {
            ulong seed = SeedOf(GameSession.Current);
            string id = GameSession.Current != null ? GameSession.Current.MissionId : null;
            var route = GameSession.Apply(RouteId.ReportRetry);
            if (route.Session != null && route.Session.Seed != seed)
                route.Session.Seed = seed;
            if (route.Session != null && id != null && route.Session.MissionId != id && !route.Session.IsFree)
                route.Session.MissionId = id;
            return route;
        }

        public static RouteResult Missions() => GameSession.Apply(RouteId.ReportMissions);

        public static RouteResult Home() => GameSession.Apply(RouteId.ReportHome);
    }
}
