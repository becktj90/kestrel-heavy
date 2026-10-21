// The report exits. One more launch keeps the seed ReportRoutes already holds.
// Back to designer (NAV-04 / F4) reopens the rocket that flew with the same mission.
using System;
using Kestrel.Core.Flight;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public static class NextMission
    {
        /// <summary>UiIds name of the Back to designer exit.</summary>
        public const string DesignerId = "report-back-to-designer";

        public static void AddRoutes(VisualElement parent, ReportActions actions, float tap)
        {
            parent.Add(Exit("report-next", ReportRoutes.NextLabel, ReportRoutes.NextTip, actions != null ? actions.Next : null, tap, "next"));
            parent.Add(Exit("report-retry", ReportRoutes.AgainLabel, ReportRoutes.AgainTip, actions != null ? actions.Again : null, tap, "retry"));
            parent.Add(Exit(DesignerId, ReportRoutes.DesignerLabel, ReportRoutes.DesignerTip, actions != null ? actions.Designer : null, tap, "designer"));
            parent.Add(Exit("report-missions", ReportRoutes.MissionsLabel, ReportRoutes.MissionsTip, actions != null ? actions.Missions : null, tap, "missions"));
            parent.Add(Exit("report-home", ReportRoutes.HomeLabel, ReportRoutes.HomeTip, actions != null ? actions.Home : null, tap, "home"));
        }

        /// <summary>
        /// Shell routes when the app shell is up. Otherwise the session still moves, and relaunch/home
        /// are the flight screen's own callbacks. The seed is not replaced.
        /// </summary>
        public static ReportActions Actions(Action relaunch, Action home, Action designer = null)
        {
            var shell = Kestrel.Game.Shell.AppShell.Instance;
            var flow = shell != null ? shell.Flow : null;
            return new ReportActions
            {
                Next = () =>
                {
                    if (flow != null) flow.ReportNext();
                    else ReportRoutes.Next(Kestrel.Game.Shell.ScreenStub.LoadCampaign());
                },
                Again = () =>
                {
                    ulong seed = ReportRoutes.SeedOf(GameSession.Current);
                    if (flow != null) flow.ReportRetry();
                    else ReportRoutes.Again();
                    if (GameSession.Current != null && GameSession.Current.Seed != seed)
                        GameSession.Current.Seed = seed;
                    relaunch?.Invoke();
                },
                Designer = () =>
                {
                    ulong seed = ReportRoutes.SeedOf(GameSession.Current);
                    if (flow != null) flow.ReportRetry();
                    else ReportRoutes.BackToDesigner();
                    if (GameSession.Current != null && GameSession.Current.Seed != seed)
                        GameSession.Current.Seed = seed;
                    designer?.Invoke();
                },
                Missions = () =>
                {
                    if (flow != null) flow.ExitToMissions();
                    else ReportRoutes.Missions();
                },
                Home = () =>
                {
                    if (flow != null) flow.ExitToHome();
                    else if (home != null) home();
                    else ReportRoutes.Home();
                },
            };
        }

        static Button Exit(string name, string text, string tip, System.Action click, float tap, string role)
        {
            var button = new Button(() => click?.Invoke()) { name = name, text = text, tooltip = tip };
            button.userData = click;
            ReportCardChrome.PaintButton(button, tap, role);
            return button;
        }
    }
}
