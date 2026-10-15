// One registration from the designer, the ascent, and the post-flight card.
using System;
using System.Collections.Generic;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Game.Audio;
using Kestrel.Game.Guide;
using UnityEngine;

namespace Kestrel.Slice
{
    public static class GuideBinder
    {
        static AscentCall tracked;
        static int seenPerfect, seenLate;
        static bool reported;

        public static void Designer(int step, StepperReport report, bool engineer)
        {
            try
            {
                var engine = GuideHost.EnsureEngine();
                if (engine == null) return;
                engine.SetEngineer(engineer);
                Attach(GuidePlace.Designer, false);
                GuideHooks.Designer(engine, step, report, engineer);
                GuideHost.Save(engine);
            }
            catch (Exception e) { Debug.LogWarning("[Thrum] step skipped: " + e.Message); }
        }

        public static void Listen(FlightDriver flight)
        {
            if (flight == null) return;
            try
            {
                GuideHost.EnsureEngine();
                Attach(GuidePlace.Ascent, true);
                if (flight.Sink is GuideFlightTap) return;
                reported = false;
                flight.Sink = new GuideFlightTap { Flight = flight, Next = flight.Sink };
            }
            catch (Exception e) { Debug.LogWarning("[Thrum] ascent skipped: " + e.Message); }
        }

        public static void Prompts(FlightDriver flight)
        {
            if (flight == null || flight.Decisions == null) return;
            try
            {
                var engine = GuideHost.EnsureEngine();
                if (engine == null) return;
                var director = flight.Decisions;
                engine.SetEngineer(director.Engineer);
                var notes = new List<GuideCallNote>();
                var open = director.OpenPrompts();
                AscentCall shown = open.Length > 0 ? open[0].Call : default;
                if (shown != default) notes.Add(Note(director, shown, true, ""));
                var score = director.Score();
                if (shown == default && tracked != default && score != null)
                {
                    string grade = score.Perfect > seenPerfect ? "Perfect" : score.Late > seenLate ? "Late" : "";
                    if (grade.Length > 0) notes.Add(Note(director, tracked, false, grade));
                    tracked = default;
                }
                else if (shown != default) tracked = shown;
                if (score != null) { seenPerfect = score.Perfect; seenLate = score.Late; }
                GuideHooks.Ascent(engine, notes, director.AutoAssist, director.Engineer);
            }
            catch (Exception e) { Debug.LogWarning("[Thrum] call skipped: " + e.Message); }
        }

        public static void Debrief(FlightDriver flight, FlightDebrief debrief)
        {
            if (debrief == null || reported) return;
            try
            {
                var engine = GuideHost.EnsureEngine();
                if (engine == null) return;
                reported = true;
                Attach(GuidePlace.Ascent, true);
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

        static GuideCallNote Note(AscentDirector director, AscentCall call, bool open, string grade)
        {
            var vehicle = director.Vehicle;
            return new GuideCallNote
            {
                Call = call.ToString(),
                Open = open,
                Grade = grade,
                QKpa = director.LiveQ > 0 ? director.LiveQ / 1000.0 : double.NaN,
                QLimKpa = vehicle != null && vehicle.QLimPa > 0 ? vehicle.QLimPa / 1000.0 : double.NaN,
                Flux = vehicle != null && vehicle.MaxHeatFlux > 0 ? vehicle.MaxHeatFlux : double.NaN,
                EnginesLeft = vehicle != null ? Math.Max(0, vehicle.Engines - 1) : 0,
            };
        }

        sealed class GuideFlightTap : IFlightEventSink
        {
            public FlightDriver Flight;
            public IFlightEventSink Next;

            public void OnEvent(string id)
            {
                try
                {
                    var engine = GuideSession.Engine;
                    if (engine != null && !string.IsNullOrEmpty(id) && Want(id))
                    {
                        if (Flight != null && Flight.Decisions != null) engine.SetEngineer(Flight.Decisions.Engineer);
                        GuideHooks.FlightBus(engine, id, Tokens(id));
                    }
                }
                catch (Exception e) { Debug.LogWarning("[Thrum] bus skipped: " + e.Message); }
                if (Next != null) Next.OnEvent(id);
            }

            public void SetFlightState(float alt, float q, float throttle)
            {
                if (Next != null) Next.SetFlightState(alt, q, throttle);
            }

            bool Want(string id)
            {
                if (id != "cutoff") return true;
                var sim = Flight != null ? Flight.Sim : null;
                if (sim == null || sim.Events == null) return false;
                for (int i = sim.Events.Count - 1; i >= 0; i--)
                {
                    var ev = sim.Events[i];
                    if (ev == null || ev.type != "cutoff") continue;
                    return ev.stage.HasValue && ev.stage.Value > 1;
                }
                return false;
            }

            Dictionary<string, string> Tokens(string id)
            {
                var tokens = new Dictionary<string, string>();
                var sim = Flight != null ? Flight.Sim : null;
                if (id == "orbit" && sim != null && sim.Events != null)
                {
                    for (int i = sim.Events.Count - 1; i >= 0; i--)
                    {
                        var ev = sim.Events[i];
                        if (ev == null || ev.type != "orbit") continue;
                        if (ev.apoAlt.HasValue) tokens["apo"] = GuideHooks.Num(ev.apoAlt.Value / 1000.0, "0");
                        if (ev.periAlt.HasValue) tokens["peri"] = GuideHooks.Num(ev.periAlt.Value / 1000.0, "0");
                        break;
                    }
                }
                if (id == "fairing" && Flight != null && Flight.Decisions != null && Flight.Decisions.Vehicle != null)
                {
                    double flux = Flight.Decisions.Vehicle.MaxHeatFlux;
                    if (flux > 0) tokens["flux"] = GuideHooks.Num(flux, "0");
                }
                if ((id == "failure" || id.StartsWith("abort", StringComparison.Ordinal)) && Flight != null && Flight.Result != null && Flight.Result.failure != null)
                {
                    var ev = Flight.Result.failure;
                    tokens["kind"] = ev.kind ?? "";
                    if (ev.kind == "structural-maxq")
                    {
                        tokens["q"] = GuideHooks.Num(ev.value / 1000.0, "0");
                        tokens["qlim"] = GuideHooks.Num(ev.limit / 1000.0, "0");
                    }
                    else if (ev.kind == "prop-depletion")
                        tokens["dv"] = GuideHooks.Num(Math.Abs(ev.value), "0");
                }
                return tokens;
            }
        }
    }
}
