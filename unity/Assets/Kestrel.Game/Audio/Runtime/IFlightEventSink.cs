namespace Kestrel.Game.Audio
{
    /// <summary>
    /// Flight → audio. One sink receives timeline ids and the live ascent state.
    /// Altitude is metres, dynamic pressure is pascals, throttle is 0..1.
    /// </summary>
    public interface IFlightEventSink
    {
        void OnEvent(string id);
        void SetFlightState(float alt, float q, float throttle);
    }
}
