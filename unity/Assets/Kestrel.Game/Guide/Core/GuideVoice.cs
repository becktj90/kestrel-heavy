using System.Collections.Generic;

namespace Kestrel.Game.Guide
{
    /// <summary>VoiceOver announcement, and the optional system-voice read-aloud.
    /// The box calls Announce once when a line finishes typing. Speak only when Read aloud is on.</summary>
    public interface IGuideVoice
    {
        void Announce(string text);
        void Speak(string text);
    }

    public static class GuideSpeech
    {
        public static string Line(string speaker, string text, int page, int pages)
            => (speaker ?? "Guide") + ". " + (text ?? "") + ". Card " + page + " of " + pages + ".";
    }

    public sealed class RecordingGuideVoice : IGuideVoice
    {
        public readonly List<string> Announcements = new List<string>();
        public readonly List<string> Spoken = new List<string>();

        public void Announce(string text)
        {
            if (!string.IsNullOrEmpty(text)) Announcements.Add(text);
        }

        public void Speak(string text)
        {
            if (!string.IsNullOrEmpty(text)) Spoken.Add(text);
        }
    }
}
