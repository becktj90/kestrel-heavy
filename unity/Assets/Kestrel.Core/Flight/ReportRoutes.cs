// Report exits. Next mission, one more launch, Back to designer, Missions, and Home.
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
        public const string DesignerLabel = "Back to designer";

        public const string NextTip = "Next mission. Opens its designer.";

        /// <summary>Playthrough 2026-11-11: on a card where Next lands on the Missions list (the next mission is still
        /// locked, or there is none), the button says so. Wording is Kestrel UX's to change (half-width at 335 pt).</summary>
        public const string NextLockedLabel = "Missions";
        public const string NextLockedTip = "The next mission is still locked. Opens the mission list.";
        public const string AgainTip = "One more launch. Same mission, same seed.";
        public const string MissionsTip = "Back to the mission list.";
        public const string HomeTip = "Exit to Home.";
        public const string DesignerTip = "Back to the designer. Same rocket, same mission.";

        public static ulong SeedOf(MissionContext mission) => mission != null ? mission.Seed : 0;

        public static RouteResult Next(Campaign campaign) => GameSession.Apply(RouteId.ReportNext, campaign);

        /// <summary>
        /// True when Next mission would land on the Missions list instead of a mission (SceneFlow.ReportNext and
        /// GameSession.Apply(ReportNext)): a mission that is not a campaign one, the last mission, or a next mission still
        /// locked (index past cleared + 1) after a miss. A pass unlocks the next mission. Unknown (no session or no
        /// campaign data) is false, so the label stays "Next mission".
        /// </summary>
        public static bool NextLandsOnList(Campaign campaign, MissionContext ctx, int cleared, bool passed)
        {
            if (ctx == null || campaign == null) return false;
            if (ctx.Mode != MissionMode.Campaign) return true;
            var mission = campaign.ById(ctx.MissionId);
            if (mission == null) return false;
            var next = campaign.Mission(mission.Index + 1);
            if (next == null) return true;
            if (passed) return false;
            return next.Index > System.Math.Max(0, cleared) + 1;
        }

        public static string NextLabelFor(Campaign campaign, MissionContext ctx, int cleared, bool passed) =>
            NextLandsOnList(campaign, ctx, cleared, passed) ? NextLockedLabel : NextLabel;

        public static string NextTipFor(Campaign campaign, MissionContext ctx, int cleared, bool passed) =>
            NextLandsOnList(campaign, ctx, cleared, passed) ? NextLockedTip : NextTip;

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

        /// <summary>
        /// NAV-04 / F4: back to the designer with the rocket that just flew. Same screen route as
        /// <see cref="Again"/>: the designer, mission and seed kept. The flight screen does not relaunch;
        /// Launch is the player's.
        /// </summary>
        public static RouteResult BackToDesigner() => Again();

        public static RouteResult Missions() => GameSession.Apply(RouteId.ReportMissions);

        public static RouteResult Home() => GameSession.Apply(RouteId.ReportHome);
    }
}
