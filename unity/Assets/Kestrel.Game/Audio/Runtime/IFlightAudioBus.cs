namespace Kestrel.Game.Audio
{
    /// <summary>Implemented by the flight driver. <see cref="AudioDirector.Attach"/> is the only wiring call.</summary>
    public interface IFlightAudioBus
    {
        IFlightEventSink Sink { get; set; }
    }
}
