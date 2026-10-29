using Kestrel.Core.Flight.Narration;

namespace Kestrel.Game.Audio
{
    /// <summary>
    /// A sink that also gets what the narration needs and <see cref="IFlightEventSink"/> cannot carry: which
    /// stack entry an event belongs to, the rocket's facts, the timeline's calls, and the flight clock.
    /// A sink that is not one of these keeps getting plain ids, so sound effects and fakes are unchanged.
    /// </summary>
    public interface IFlightStageSink : IFlightEventSink
    {
        /// <summary>Once per launch, before flight.begin: the facts to pick takes by, and when the calls are expected (null = unknown).</summary>
        void BeginNarration(NarrationFacts facts, CallSchedule calls);
        /// <summary>A sim event with the sim stack entry it belongs to (1-based, 0 when the event has none).</summary>
        void OnStagedEvent(string type, int stage);
        /// <summary>Sim seconds since ignition and altitude in metres, each frame the sim runs.</summary>
        void SetFlightClock(double seconds, double altM);
    }
}
