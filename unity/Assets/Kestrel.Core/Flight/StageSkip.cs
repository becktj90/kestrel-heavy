// "Skip to next stage" (DECISIONS #78, Trevor 11-06): the flight HUD gets a button that fast-forwards the same
// sim to the next flight event (Max-Q, MECO, staging, SECO, entry burn, landing burn). No physics change, no
// dropped steps: the pump below runs DirectedFlight.Step — the same stepper the pad, the batch and the predictor
// fly — one fixed 1/120 s step at a time, with the same PlayedChecks after each step, so the state at the event
// is bit-identical to playing in real time (StageSkipTests.SkippedFlightMatchesRealTime). Real time stays the
// default: the button is opt-in per press, and the skip runs in bounded per-frame chunks (MaxStepsPerFrame) so
// the app stays responsive while a minutes-long burn passes in under a second of wall time.
//
// Stops, in priority order on each step: a failure (PlayedChecks.StopForBreakup, the same check the pad loop
// runs — the skip never passes a failure, it stops and the normal failure playback takes over, #405 untouched),
// the next flight event, an open call the player must decide (auto-assist calls resolve inside the pump at
// their real-time grades, so a skip with auto-assist on flies straight through them), or the end of the flight.
//
// The recovery side (entry burn, landing burn) lives on BoosterLanding.AdvanceSkip: the booster is stepped by
// the same fixed recovery steps, in lockstep with the ascent's skipped seconds, and stops at its next burn
// phase (Entry, Landing) or touchdown. SliceApp halts the ascent skip when the booster hits one, so the first
// event on either timeline wins. An expendable booster with no landing fuel has no burn to skip to, so it
// offers no recovery target and just rides along (its attempt still flies, ungraded, and Conclude settles it
// when the ascent ends).
using System;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Flight
{
    /// <summary>The flight events a skip can land on (DECISIONS #78).</summary>
    public enum StageSkipTarget
    {
        None = 0,
        MaxQ = 1,
        Meco = 2,
        Staging = 3,
        Seco = 4,
        EntryBurn = 5,
        LandingBurn = 6,
    }

    /// <summary>Why a skip run stopped.</summary>
    public enum StageSkipStop
    {
        None = 0,
        /// <summary>Reached the next flight event.</summary>
        Target = 1,
        /// <summary>The vehicle failed mid-skip: the failure plays, nothing is skipped past it.</summary>
        Failure = 2,
        /// <summary>A call opened that the player must decide (auto-assist off).</summary>
        Call = 3,
        /// <summary>The flight ended (cutoff, burnout, impact) with no failure.</summary>
        Complete = 4,
        /// <summary>The per-frame step budget ran out; the skip continues next frame.</summary>
        Budget = 5,
    }

    /// <summary>One <see cref="StageSkip.Advance"/> run: what it skipped to, why it stopped, and how far it flew.</summary>
    public sealed class StageSkipRun
    {
        /// <summary>The event the run was skipping to when it started (the HUD label).</summary>
        public StageSkipTarget Target;
        /// <summary>The event that actually fired, when <see cref="Stop"/> is <see cref="StageSkipStop.Target"/>.</summary>
        public StageSkipTarget Reached;
        public StageSkipStop Stop;
        /// <summary>Physics steps run.</summary>
        public int Steps;
        /// <summary>Sim seconds advanced.</summary>
        public double SimSeconds;
        /// <summary>True when the core burned out (PlayedChecks.Meco) during this run: the driver reports MECO.</summary>
        public bool Meco;
    }

    public static class StageSkip
    {
        /// <summary>
        /// Ascent steps per frame while skipping: 1024 fixed 1/120 s steps is ~8.5 sim seconds a frame, so a
        /// minutes-long burn passes in well under a second of wall time. A step costs microseconds (CoastWarp),
        /// so this stays inside a frame; the skip resumes next frame when the budget runs out.
        /// </summary>
        public const int MaxStepsPerFrame = 1024;

        /// <summary>Telemetry cadence for a skip (seconds of sim time between samples): the same 0.1 s the frame sampler gates on.</summary>
        public const double SampleIntervalS = 0.1;

        /// <summary>The HUD copy for a target: "Max-Q", "MECO", "Staging", "SECO", "Entry burn", "Landing burn".</summary>
        public static string Label(StageSkipTarget target)
        {
            switch (target)
            {
                case StageSkipTarget.MaxQ: return "Max-Q";
                case StageSkipTarget.Meco: return "MECO";
                case StageSkipTarget.Staging: return "Staging";
                case StageSkipTarget.Seco: return "SECO";
                case StageSkipTarget.EntryBurn: return "Entry burn";
                case StageSkipTarget.LandingBurn: return "Landing burn";
                default: return "";
            }
        }

        /// <summary>
        /// The next ascent event for this sim, in flight order. None once the flight is over. The label is a
        /// prediction, not a promise: the pump stops at the first target event that actually fires (a burnout and
        /// its separation land on the same step), and at a failure or an open call before any of them.
        /// </summary>
        public static StageSkipTarget NextAscentTarget(AscentSim sim, PlayedChecks checks)
        {
            if (sim == null || sim.Done) return StageSkipTarget.None;
            if (!sim.MaxQPassed) return StageSkipTarget.MaxQ;
            // MECO is the core stage's burnout (PlayedChecks.MecoStage, the serial core), and the core is not
            // always the stage burning now: a booster stage can burn out and separate first, and that staging
            // is the next event, not MECO (m10: stage 1 separates at T+107, the core follows).
            if (checks != null)
            {
                if (!checks.Meco && checks.MecoStage == sim.StageIdx + 1) return StageSkipTarget.Meco;
            }
            else if (!HasEvent(sim, "burnout")) return StageSkipTarget.Meco;
            if (CountEvents(sim, "separation") < sim.StageCount - 1) return StageSkipTarget.Staging;
            return StageSkipTarget.Seco;
        }

        /// <summary>
        /// The next recovery burn this booster can still reach: the entry burn while it has the propellant for one
        /// (EntryWanted needs prop above the landing reserve), else the landing burn while it has any propellant
        /// left. None when the booster has not started, has touched down, is already in its landing burn, or has
        /// no burn left to fly (an expendable stage with no landing fuel). The booster still rides along with an
        /// ascent skip when this is None; it just has no burn of its own to stop at.
        /// </summary>
        public static StageSkipTarget NextRecoveryTarget(BoosterLanding landing)
        {
            if (landing == null || !landing.Armed || !landing.Started || landing.Touched) return StageSkipTarget.None;
            var sample = landing.Sample();
            if (sample.Phase == RecoveryPhase.Down || sample.Phase == RecoveryPhase.Landing) return StageSkipTarget.None;
            if (sample.Phase == RecoveryPhase.Entry || sample.Phase == RecoveryPhase.Descent)
                return sample.Prop > 0 ? StageSkipTarget.LandingBurn : StageSkipTarget.None;
            double floor = RecoverySim.LandingReserveKg(landing.Craft);
            return sample.Prop > floor * 1.2 + 300 ? StageSkipTarget.EntryBurn : StageSkipTarget.None;
        }

        /// <summary>
        /// Fast-forward <paramref name="flight"/> by up to <paramref name="maxSteps"/> fixed steps, stopping at
        /// the next flight event, a failure, an open call the player must decide, or the end of the flight. The
        /// per-step order is the pad loop's (FlightDriver.Update): DirectedFlight.Step, then the played checks.
        /// The director is never bypassed: its calls arm, open and (under auto-assist) resolve inside the steps,
        /// at the same grades as real time.
        /// <para>
        /// <paramref name="onSample"/> (optional) gets one <see cref="AscentSample"/> per 0.1 sim-seconds of
        /// skipped time, taken from the real stepped state right after the step that crossed the boundary. The
        /// frame sampler only runs once per rendered frame, so without this a minutes-long skip starves the
        /// debrief log to one sample per frame (~5 bars for a mostly-skipped flight). The cadence matches the
        /// frame sampler's 0.1 s gate and no sample is fabricated: each one is the sim's own state at that time.
        /// </para>
        /// </summary>
        public static StageSkipRun Advance(DirectedFlight flight, PlayedChecks checks, int maxSteps, Action<AscentSample> onSample = null)
        {
            if (flight == null) throw new ArgumentNullException(nameof(flight));
            var sim = flight.Sim;
            var run = new StageSkipRun { Target = NextAscentTarget(sim, checks) };
            if (sim == null || sim.Done)
            {
                run.Stop = StageSkipStop.Complete;
                return run;
            }
            if (maxSteps < 1) maxSteps = 1;
            double startT = sim.T;
            // One sample per 0.1 sim-seconds from the stepped state (see the doc on Advance). The grid is
            // anchored to absolute sim time (the next 0.1 s multiple), not to this call's start, so a skip
            // split across frame chunks keeps one continuous grid — the same grid a real-time run samples on.
            // The fixed 1/120 s step can cross at most one boundary per step.
            double nextSampleT = Math.Ceiling(startT / SampleIntervalS) * SampleIntervalS;
            bool maxQBefore = sim.MaxQPassed;
            bool mecoBefore = checks != null && checks.Meco;
            int sepsBefore = CountEvents(sim, "separation");
            int cutoffsBefore = CountEvents(sim, "cutoff");
            int openBefore = flight.Director != null ? flight.Director.OpenCount : 0;
            bool autoAssist = flight.Director != null && flight.Director.AutoAssist;
            while (run.Steps < maxSteps && !sim.Done)
            {
                int before = sim.Events.Count;
                flight.Step();
                run.Steps++;
                if (onSample != null && sim.T >= nextSampleT)
                {
                    onSample(flight.Sample());
                    nextSampleT += SampleIntervalS;
                }
                bool mecoNow = false;
                if (checks != null)
                {
                    if (checks.NoteEvents(sim, before)) mecoNow = true;
                    if (checks.StopForBreakup(sim)) { run.Stop = StageSkipStop.Failure; break; }
                }
                else if (HasEventSince(sim, before, "failure"))
                {
                    run.Stop = StageSkipStop.Failure;
                    break;
                }
                if (mecoNow) run.Meco = true;
                // The next flight event, in flight order. A burnout and its separation share one step, so the
                // first predicate that fires names the stop; the rest have already happened by then.
                if (!maxQBefore && sim.MaxQPassed) { run.Stop = StageSkipStop.Target; run.Reached = StageSkipTarget.MaxQ; break; }
                if (!mecoBefore && (mecoNow || (checks != null && checks.Meco))) { run.Stop = StageSkipStop.Target; run.Reached = StageSkipTarget.Meco; break; }
                if (CountEvents(sim, "separation") > sepsBefore) { run.Stop = StageSkipStop.Target; run.Reached = StageSkipTarget.Staging; break; }
                if (CountEvents(sim, "cutoff") > cutoffsBefore) { run.Stop = StageSkipStop.Target; run.Reached = StageSkipTarget.Seco; break; }
                if (FinalBurnoutSince(sim, before)) { run.Stop = StageSkipStop.Target; run.Reached = StageSkipTarget.Seco; break; }
                // A call the player must decide stops the skip so the decision — and its medal timing — is not
                // flown past. Auto-assist resolves its calls inside the steps above, so those never stop a skip.
                if (!autoAssist && flight.Director != null && flight.Director.OpenCount > openBefore)
                {
                    run.Stop = StageSkipStop.Call;
                    break;
                }
                if (sim.Done) { run.Stop = StageSkipStop.Complete; break; }
            }
            if (run.Stop == StageSkipStop.None) run.Stop = StageSkipStop.Budget;
            run.SimSeconds = sim.T - startT;
            return run;
        }

        /// <summary>True when the last stage burned out (ran dry) since event <paramref name="before"/>: SECO without a cutoff.</summary>
        static bool FinalBurnoutSince(AscentSim sim, int before)
        {
            var ev = sim.Events;
            for (int i = before; i < ev.Count; i++)
                if (ev[i].type == "burnout" && ev[i].stage == sim.StageCount) return true;
            return false;
        }

        static bool HasEvent(AscentSim sim, string type)
        {
            var ev = sim.Events;
            for (int i = 0; i < ev.Count; i++)
                if (ev[i].type == type) return true;
            return false;
        }

        static bool HasEventSince(AscentSim sim, int before, string type)
        {
            var ev = sim.Events;
            for (int i = before; i < ev.Count; i++)
                if (ev[i].type == type) return true;
            return false;
        }

        static int CountEvents(AscentSim sim, string type)
        {
            int n = 0;
            var ev = sim.Events;
            for (int i = 0; i < ev.Count; i++)
                if (ev[i].type == type) n++;
            return n;
        }
    }
}
