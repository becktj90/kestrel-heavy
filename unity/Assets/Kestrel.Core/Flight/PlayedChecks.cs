// #314: the checks that end a played flight, in one place. The live pad (FlightDriver), the designer's prediction
// (ReferenceAscent, ReferenceJob) and the launch predictor (LaunchPredictor) all run these, for every design,
// v1 included, so a design that breaks up on the pad never predicts a clean flight.
// After each AscentSim.Tick: a recorded monitor failure ends the flight; otherwise an impact, a Δv-short burnout,
// or (for an arcade design with no strict monitor) a PlayedBreakup load or g breaks it up.
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    public sealed class PlayedChecks
    {
        readonly bool arcade;
        readonly double qLimPa, liftoffTwr, dvMargin, hopTargetAltM;
        readonly int mecoStage;
        bool shortJudged;

        /// <summary>True once the core stage has burned out.</summary>
        public bool Meco { get; private set; }
        public bool Arcade => arcade;
        public double DvMargin => dvMargin;
        public int MecoStage => mecoStage;

        /// <summary>Build right after <paramref name="sim"/> is constructed from <paramref name="stages"/> and <paramref name="opts"/>.</summary>
        public PlayedChecks(Catalog c, Design d, IList<SimStage> stages, SimOptions opts, AscentSim sim)
        {
            arcade = PlayedBreakup.Arcade(d);
            qLimPa = FailureMonitor.StructuralQLimPa(c, d);
            liftoffTwr = stages != null && stages.Count > 0 && sim != null && sim.Y[4] > 1 ? stages[0].thrustSL / (sim.Y[4] * Atmosphere.G0) : 0;
            double ideal = Headless.StackDeltaV(stages, opts != null ? opts.payload : 0).total;
            var req = Stack.RequiredDeltaV(c, d != null ? d.payloadOrbit : null);
            dvMargin = PlayedBreakup.Margin(ideal, req.required);
            hopTargetAltM = req.orbit != null && req.orbit.periKm < 0 && req.orbit.apoKm > 0 ? req.orbit.apoKm * 1000 : double.NaN;
            var serial = Stack.SerialStages(c, d).stages;
            mecoStage = serial.FindIndex(s => s.role == "core" && s.index == 0) + 1;
        }

        /// <summary>Read the events this tick added (from index <paramref name="before"/>). True on the tick the core burns out.</summary>
        public bool NoteEvents(AscentSim sim, int before)
        {
            if (Meco || sim == null) return false;
            for (int i = before; i < sim.Events.Count; i++)
            {
                var ev = sim.Events[i];
                if (ev.type == "burnout" && ev.stage == mecoStage) { Meco = true; return true; }
            }
            return false;
        }

        /// <summary>True when this step ends the flight in a breakup. The sim is broken up and Done.</summary>
        public bool StopForBreakup(AscentSim sim)
        {
            if (sim == null) return false;
            if (Has(sim, "failure")) { sim.BreakUp(null); return true; }
            FailureEvent ev = null;
            if (Has(sim, "impact")) ev = PlayedBreakup.Impact(sim.T);
            else if (Meco && !shortJudged && JudgeShort(sim)) ev = PlayedBreakup.Short(sim.T, dvMargin);
            else if (arcade) ev = PlayedBreakup.At(Sample(sim));
            if (ev == null) return false;
            sim.BreakUp(ev);
            return true;
        }

        public PlayedBreakup.Sample Sample(AscentSim sim)
        {
            double mass = sim.Y[4];
            double x = sim.Y[0], y = sim.Y[1];
            return new PlayedBreakup.Sample
            {
                T = sim.T,
                AltM = Math.Sqrt(x * x + y * y) - Gravity.R_EARTH,
                QPa = sim.LastQ,
                AccelMps2 = mass > 1 ? sim.LastThrust / mass : 0,
                QLimPa = qLimPa,
                LiftoffTwr = liftoffTwr,
                Thrusting = sim.Thrusting,
            };
        }

        /// <summary>One tick and its checks. False when the flight is over (done or broken up).</summary>
        public bool Tick(AscentSim sim)
        {
            int before = sim.Events.Count;
            bool more = sim.Tick();
            NoteEvents(sim, before);
            if (StopForBreakup(sim)) return false;
            return more;
        }

        /// <summary>
        /// Fly <paramref name="stages"/> to the end under the played checks: the same RK4 steps as
        /// <see cref="Headless.Simulate"/>, stopped where play would break the vehicle up.
        /// </summary>
        public static SimResult Fly(Catalog c, Design d, IList<SimStage> stages, SimOptions opts)
        {
            var sim = new AscentSim(stages, opts ?? new SimOptions());
            var checks = new PlayedChecks(c, d, stages, opts, sim);
            while (checks.Tick(sim)) { }
            return sim.Finish();
        }

        /// <summary>
        /// A hop whose burnout state already coasts (in vacuum) to its target altitude is not short of speed. The Δv
        /// budget pads a hop with <see cref="MissionScore.ApogeeLossMps"/>, which is most of a 10 km hop's budget, so
        /// the budget alone broke up a hop that flies past its target (the m1 golden winner: budget −6.2 %, flown 13.2 km).
        /// </summary>
        /// <summary>Judged once, at core burnout: a hop's coast that drag bends below the target later is a miss, not a breakup.</summary>
        bool JudgeShort(AscentSim sim)
        {
            shortJudged = true;
            return dvMargin < 0 && !HopReaches(sim);
        }

        bool HopReaches(AscentSim sim)
        {
            if (double.IsNaN(hopTargetAltM)) return false;
            return Orbit.Planar(sim.Y[0], sim.Y[1], sim.Y[2], sim.Y[3]).apoAlt >= hopTargetAltM;
        }

        static bool Has(AscentSim sim, string type)
        {
            var ev = sim.Events;
            for (int i = ev.Count - 1; i >= 0; i--)
                if (ev[i].type == type) return true;
            return false;
        }
    }
}
