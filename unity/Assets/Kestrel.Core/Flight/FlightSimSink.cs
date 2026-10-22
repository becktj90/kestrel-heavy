// Observer hooks for flight VFX and audio. The sim calls these and never reads them back.
// A null sink and any sink leave the step, the event list, and the result identical,
// including a sink that throws: every call goes through GuardedFlightSink.
using System;

namespace Kestrel.Core.Flight
{
    /// <summary>Planar position or velocity. Metres, or metres per second. The ascent frame is Earth-centred; recovery uses the pad frame.</summary>
    public readonly struct FlightVec
    {
        public readonly double X, Y;
        public FlightVec(double x, double y) { X = x; Y = y; }
    }

    /// <summary>Where a booster meets the surface. Deck is Haven or the pad. RTLS off the pad is ground. A barge miss is sea.</summary>
    public enum FlightSurface
    {
        Deck = 0,
        Ground = 1,
        Sea = 2,
    }

    public static class FlightSurfaces
    {
        public static FlightSurface Choose(double missM, double deckRadiusM, string mode)
        {
            if (!double.IsNaN(missM) && Math.Abs(missM) <= deckRadiusM) return FlightSurface.Deck;
            if (mode == "rtls") return FlightSurface.Ground;
            return FlightSurface.Sea;
        }
    }

    /// <summary>Engine-failure picture. Overpressure is the chamber rupture. The other engine kinds sputter.</summary>
    public static class FlightEngineMode
    {
        public const string Sputter = "sputter";
        public const string Overpressure = "overpressure";

        public static bool IsEngine(string kind)
            => kind == "engine-out" || kind == "bad-mixture" || kind == "engine-overpressure";

        public static string Of(string kind)
            => kind == "engine-overpressure" ? Overpressure : Sputter;
    }

    /// <summary>
    /// Flight-sim observer. Payloads match the flight-event VFX audit.
    /// Implementations must not write sim state. Calls arrive on the sim thread, in sim-time order.
    /// </summary>
    /// <remarks>
    /// <para><b>Event order (contract).</b> One ascent emits, in this order:</para>
    /// <list type="number">
    /// <item><see cref="OnIgnition"/>(1, 0), on the first tick, before anything else.</item>
    /// <item><see cref="OnLiftoff"/>, once, when altitude first passes 1 m.</item>
    /// <item><see cref="OnMaxQ"/>, at most once (see its remarks). On a normal ascent it comes before the first separation;
    /// a vehicle whose first stage burns out before q has fallen 2% gets it after that separation instead, still in sim-time order.</item>
    /// <item>For each staging: <see cref="OnStageSeparation"/>(k) immediately followed by <see cref="OnIgnition"/>(k + 1), same tick.</item>
    /// <item><see cref="OnFairingSeparation"/>, at most once, always after <see cref="OnMaxQ"/>.</item>
    /// <item><see cref="OnFailure"/>, at most once, immediately followed by <see cref="OnEngineFailure"/> when the kind is an engine kind.</item>
    /// <item><see cref="OnImpact"/>, at most once, and it is the last ascent call. A failure is followed by its impact when the debris coasts down.</item>
    /// <item>Recovery (<see cref="RecoverySim"/>, a separate run after the ascent): <see cref="OnRecoveryPhase"/> for the first phase and every change,
    /// then <see cref="OnTouchdown"/> and, for a hard landing, <see cref="OnCrash"/>.</item>
    /// </list>
    /// <para>Steps 3 to 6 interleave by sim time. A clean orbit emits no failure or impact.</para>
    /// <para><b>Exceptions.</b> The sims call every hook through <see cref="GuardedFlightSink"/>. A hook that throws is caught,
    /// logged once per hook type for that sink, and the run carries on bit-identical. Sinks should still not throw.</para>
    /// </remarks>
    public interface IFlightSimSink
    {
        /// <summary>Sim ignition, including stage 1 before liftoff. <paramref name="stage"/> is 1-based. <paramref name="t"/> is sim seconds.</summary>
        void OnIgnition(int stage, double t);

        /// <summary>Altitude has passed 1 m. <paramref name="padPos"/> is the pad in the ascent frame.</summary>
        void OnLiftoff(double t, FlightVec padPos);

        /// <summary>
        /// First dynamic-pressure peak, pascals, and the sim time of that peak. Emitted once, from the ascent, not from a presenter.
        /// </summary>
        /// <remarks>
        /// <b>First peak, not the run's maximum.</b> It fires on the tick q drops below 98% of the running maximum
        /// (<see cref="Physics.AscentSim.MaxQConfirm"/>) and reports that maximum, so VFX and narration land a few seconds
        /// after the real peak, during the flight. If a later peak is higher (a hot second burn low in the air),
        /// <c>SimResult.maxQ</c> and <c>maxQTime</c> report that higher one while this hook keeps the first.
        /// If the run ends before q has fallen 2%, it fires once from <c>Finish</c> with the running maximum.
        /// <b>Vacuum flights emit no max-Q:</b> with <c>SimOptions.vacuum</c> (or above 300 km) q is 0, so there is no peak,
        /// and because the fairing gate waits for max-Q, they emit no fairing separation either.
        /// </remarks>
        void OnMaxQ(double q, double t);

        /// <summary>A stage has separated. <paramref name="stage"/> is the 1-based index of the stage that left. Position and velocity are metres and metres per second.</summary>
        void OnStageSeparation(int stage, FlightVec pos, FlightVec vel);

        /// <summary>
        /// Fairing jettison. One sim event, after max-Q, once altitude reaches <c>SimOptions.fairingJettisonAltM</c>.
        /// Mission flights (<see cref="MissionFlightSetup"/>) set that from the design's jettison altitude (default 110 km).
        /// </summary>
        void OnFairingSeparation(double t, FlightVec pos, FlightVec vel);

        /// <summary>Recovery phase changed. <paramref name="t"/> is recovery sim time.</summary>
        void OnRecoveryPhase(RecoveryPhase phase, double t);

        /// <summary>Touchdown. <paramref name="vz"/> is sink speed (m/s), <paramref name="tilt"/> is degrees from vertical.</summary>
        void OnTouchdown(double vz, double tilt, FlightSurface surface);

        /// <summary>Hard touchdown. <paramref name="kind"/> is the boom kind. <paramref name="energy"/> is kinetic energy, joules.</summary>
        void OnCrash(string kind, FlightVec pos, FlightVec vel, double energy);

        /// <summary>In-flight failure. <paramref name="engineIndex"/> is -1 when the failure is not an engine.</summary>
        void OnFailure(string kind, string cause, int engineIndex, FlightVec pos, FlightVec vel, double t);

        /// <summary>Engine-out, bad mixture, or overpressure. <paramref name="mode"/> is <see cref="FlightEngineMode.Sputter"/> or <see cref="FlightEngineMode.Overpressure"/>.</summary>
        void OnEngineFailure(int engineIndex, string mode);

        /// <summary>Unplanned ground hit. <paramref name="speed"/> is the downward radial speed (m/s).</summary>
        void OnImpact(double speed, FlightVec pos);
    }

    /// <summary>
    /// Wraps a sink so a throwing hook cannot abort or change the sim run. Each hook type logs its first exception
    /// once through <see cref="Log"/>; later throws from the same hook on the same wrapper are swallowed silently.
    /// The happy path does not allocate.
    /// </summary>
    public sealed class GuardedFlightSink : IFlightSimSink
    {
        /// <summary>Where the first exception per hook type goes. Defaults to stderr; a Unity host can point it at Debug.LogWarning.</summary>
        public static Action<string> Log = message => Console.Error.WriteLine(message);

        enum Hook { Ignition, Liftoff, MaxQ, StageSeparation, FairingSeparation, RecoveryPhase, Touchdown, Crash, Failure, EngineFailure, Impact, Count }

        readonly IFlightSimSink inner;
        readonly bool[] logged = new bool[(int)Hook.Count];
        int faults;

        GuardedFlightSink(IFlightSimSink inner) { this.inner = inner; }

        /// <summary>Null stays null. An already guarded sink is returned as is.</summary>
        public static IFlightSimSink Wrap(IFlightSimSink sink)
        {
            if (sink == null || sink is GuardedFlightSink) return sink;
            return new GuardedFlightSink(sink);
        }

        /// <summary>The sink being guarded.</summary>
        public IFlightSimSink Inner => inner;

        /// <summary>How many hook calls threw on this wrapper.</summary>
        public int Faults => faults;

        void Fault(Hook hook, Exception e)
        {
            faults++;
            if (logged[(int)hook]) return;
            logged[(int)hook] = true;
            try { Log?.Invoke("[Kestrel] flight sink " + inner.GetType().Name + ".On" + hook + " threw " + e.GetType().Name + ": " + e.Message + " (ignored; further throws from this hook are not logged)"); }
            catch (Exception) { }
        }

        public void OnIgnition(int stage, double t) { try { inner.OnIgnition(stage, t); } catch (Exception e) { Fault(Hook.Ignition, e); } }
        public void OnLiftoff(double t, FlightVec padPos) { try { inner.OnLiftoff(t, padPos); } catch (Exception e) { Fault(Hook.Liftoff, e); } }
        public void OnMaxQ(double q, double t) { try { inner.OnMaxQ(q, t); } catch (Exception e) { Fault(Hook.MaxQ, e); } }
        public void OnStageSeparation(int stage, FlightVec pos, FlightVec vel) { try { inner.OnStageSeparation(stage, pos, vel); } catch (Exception e) { Fault(Hook.StageSeparation, e); } }
        public void OnFairingSeparation(double t, FlightVec pos, FlightVec vel) { try { inner.OnFairingSeparation(t, pos, vel); } catch (Exception e) { Fault(Hook.FairingSeparation, e); } }
        public void OnRecoveryPhase(RecoveryPhase phase, double t) { try { inner.OnRecoveryPhase(phase, t); } catch (Exception e) { Fault(Hook.RecoveryPhase, e); } }
        public void OnTouchdown(double vz, double tilt, FlightSurface surface) { try { inner.OnTouchdown(vz, tilt, surface); } catch (Exception e) { Fault(Hook.Touchdown, e); } }
        public void OnCrash(string kind, FlightVec pos, FlightVec vel, double energy) { try { inner.OnCrash(kind, pos, vel, energy); } catch (Exception e) { Fault(Hook.Crash, e); } }
        public void OnFailure(string kind, string cause, int engineIndex, FlightVec pos, FlightVec vel, double t) { try { inner.OnFailure(kind, cause, engineIndex, pos, vel, t); } catch (Exception e) { Fault(Hook.Failure, e); } }
        public void OnEngineFailure(int engineIndex, string mode) { try { inner.OnEngineFailure(engineIndex, mode); } catch (Exception e) { Fault(Hook.EngineFailure, e); } }
        public void OnImpact(double speed, FlightVec pos) { try { inner.OnImpact(speed, pos); } catch (Exception e) { Fault(Hook.Impact, e); } }
    }
}
