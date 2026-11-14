// Test Flight card exits (DECISIONS #102). Tweak & Retry and New Design stay in the flight scene, like the report's
// Fix it: the session moves (GameSession routes TestRetry / TestNewDesign: a mission's "Test it" copy hands the mission
// back), then the designer opens. Home is the report's own Home.
using System;
using Kestrel.Core.Flight;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public static class TestFlightRoutes
    {
        /// <summary>The flight screen's "fresh rocket in the designer" (SliceApp binds it): the starter template in a
        /// free test, the mission's Starter base after a mission's "Test it".</summary>
        public static Action NewDesign { get; private set; }

        public static void Bind(Action newDesign) => NewDesign = newDesign;

        /// <summary>The card's exits for this tree. <paramref name="actions"/> are the report's (already wrapped to
        /// dismiss the card); <paramref name="reveal"/> shows the design sheet again.</summary>
        public static TestCardActions For(VisualElement ui, TestFlightCard card, ReportActions actions, Action reveal)
        {
            return new TestCardActions
            {
                Retry = () =>
                {
                    Leave(ui, reveal);
                    Move(RouteId.TestRetry);
                    // On a failure the designer opens on the part the hint names (FixField); orbit just reopens it.
                    string fix = card != null ? card.FixKind : "";
                    if (!string.IsNullOrEmpty(fix)) FixField.Apply(ui, FixField.For(fix));
                    else PostFlightSource.OpenDesigner?.Invoke();
                },
                NewDesign = () =>
                {
                    Leave(ui, reveal);
                    Move(RouteId.TestNewDesign);
                    if (NewDesign != null) NewDesign();
                    else PostFlightSource.OpenDesigner?.Invoke();
                },
                Home = actions != null ? actions.Home : null,
            };
        }

        static void Leave(VisualElement ui, Action reveal)
        {
            ReportCardChrome.Dismiss(ui);
            reveal?.Invoke();
        }

        /// <summary>The session route; through SceneFlow when the shell is up, so its overlay state follows.</summary>
        static void Move(RouteId id)
        {
            var shell = Kestrel.Game.Shell.AppShell.Instance;
            var flow = shell != null ? shell.Flow : null;
            if (flow != null)
            {
                if (id == RouteId.TestRetry) flow.TestRetry();
                else flow.TestNewDesign();
            }
            else GameSession.Apply(id);
        }
    }
}
