using Kestrel.Game.Audio;

namespace Kestrel.Game.Guide
{
    /// <summary>Flight bus → subtitle box. Mission control, host and co-host clips become lines in the same stack.
    /// G9 forwards events here. Text shows even when the narration bus is muted.</summary>
    public sealed class NarrationSubtitleSink : IFlightEventSink
    {
        public GuideStage Stage;

        public void OnEvent(string id)
        {
            if (Stage == null || string.IsNullOrEmpty(id)) return;
            if (id == "flight.begin" || id == "flight.end")
            {
                Stage.BeginFlight();
                return;
            }
            Stage.OfferNarration(id);
        }

        public void SetFlightState(float alt, float q, float throttle) { }
    }

    public sealed class DeviceGuideVoice : IGuideVoice
    {
        public void Announce(string text) => GuideNative.Announce(text);
        public void Speak(string text) => GuideNative.Speak(text);
    }
}
