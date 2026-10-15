// Thrum on the designer and the post-flight report. She stays off the ascent.
// Anomaly calls stay on the HUD; this type does not offer them.
using System;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Game.Guide;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public static class GuideBinder
    {
        static bool reported;

        public static void Designer(int step, StepperReport report, bool engineer)
        {
            try
            {
                if (InFlight()) return;
                var engine = GuideHost.EnsureEngine();
                if (engine == null) return;
                engine.SetEngineer(engineer);
                Attach(GuidePlace.Designer, false);
                GuideHooks.Designer(engine, step, report, engineer);
                GuideHost.Save(engine);
            }
            catch (Exception e) { Debug.LogWarning("[Thrum] step skipped: " + e.Message); }
        }

        /// <summary>Flight start. Clears anything she queued and hides the portrait. Does not listen to the bus.</summary>
        public static void Listen(FlightDriver flight)
        {
            if (flight == null) return;
            try
            {
                reported = false;
                var engine = GuideHost.EnsureEngine();
                if (engine != null) engine.BeginFlight();
                Hide();
            }
            catch (Exception e) { Debug.LogWarning("[Thrum] ascent skipped: " + e.Message); }
        }

        /// <summary>The HUD already shows the open call. Thrum does not speak over it.</summary>
        public static void Prompts(FlightDriver flight)
        {
            if (flight == null) return;
        }

        public static void Debrief(FlightDriver flight, FlightDebrief debrief)
        {
            if (debrief == null || reported) return;
            try
            {
                var engine = GuideHost.EnsureEngine();
                if (engine == null) return;
                reported = true;
                Attach(GuidePlace.Menu, false);
                AscentFlightScore score = null;
                if (flight != null && flight.Decisions != null)
                {
                    engine.SetEngineer(flight.Decisions.Engineer);
                    try { score = flight.Decisions.Score(); }
                    catch (Exception) { score = null; }
                }
                engine.ShowReport(GuideHooks.FromFlight(debrief, flight != null ? flight.Result : null, GameSession.Current, score));
                GuideHost.Save(engine);
            }
            catch (Exception e) { Debug.LogWarning("[Thrum] report skipped: " + e.Message); }
        }

        static void Attach(GuidePlace place, bool compact)
        {
            var doc = UnityEngine.Object.FindAnyObjectByType<UIDocument>();
            if (doc != null) GuideHost.Open(doc, place, compact);
        }

        static void Hide()
        {
            var doc = UnityEngine.Object.FindAnyObjectByType<UIDocument>();
            if (doc == null) return;
            var driver = doc.GetComponent<GuideBoxDriver>();
            if (driver == null) return;
            driver.Place = GuidePlace.Ascent;
            if (driver.Stage != null) driver.Stage.BeginFlight();
            driver.Silence();
        }

        static bool InFlight()
        {
            var flight = UnityEngine.Object.FindAnyObjectByType<FlightDriver>();
            return flight != null && flight.Running && flight.Result == null;
        }
    }
}
