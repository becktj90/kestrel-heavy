// Subtitles for the mission-control, host and co-host clips already in the narration manifest.
// Text is the source of truth. Audio can be muted and the line still shows.
using System.Collections.Generic;
using Kestrel.Core.Json;

namespace Kestrel.Game.Guide
{
    public sealed class NarrationSubtitles
    {
        public readonly struct Clip
        {
            public readonly string EventId, Channel, Text, File;
            public readonly float Duration;
            public Clip(string eventId, string channel, string text, string file, float duration)
            {
                EventId = eventId; Channel = channel; Text = text; File = file; Duration = duration;
            }
        }

        readonly Dictionary<string, List<Clip>> byEvent = new Dictionary<string, List<Clip>>();
        readonly Dictionary<string, int> cursors = new Dictionary<string, int>();
        readonly List<Clip> all = new List<Clip>();

        public int ClipCount => all.Count;
        public IReadOnlyList<Clip> All => all;

        public static NarrationSubtitles Parse(string json)
        {
            var book = new NarrationSubtitles();
            if (string.IsNullOrEmpty(json)) return book;
            var root = MiniJson.ParseObject(json);
            var clips = root?.Arr("clips");
            if (clips == null) return book;
            for (int i = 0; i < clips.Count; i++)
            {
                var o = clips[i] as JsonObject;
                if (o == null) continue;
                string channel = o.Str("channel");
                if (!IsSubtitleChannel(channel)) continue;
                string text = o.Str("text");
                string ev = o.Str("event");
                string file = o.Str("file");
                if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(ev)) continue;
                var clip = new Clip(ev, channel, text, file ?? "", (float)o.Num("durationSec"));
                if (!book.byEvent.TryGetValue(ev, out var list))
                {
                    list = new List<Clip>();
                    book.byEvent[ev] = list;
                }
                list.Add(clip);
                book.all.Add(clip);
            }
            return book;
        }

        public static bool IsSubtitleChannel(string channel)
            => channel == "mc" || channel == "host" || channel == "cohost";

        public static GuideSpeaker Speaker(string channel)
        {
            switch (channel)
            {
                case "host": return GuideSpeaker.Host;
                case "cohost": return GuideSpeaker.Cohost;
                default: return GuideSpeaker.MissionControl;
            }
        }

        public static GuidePriority PriorityFor(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return GuidePriority.Chatter;
            if (eventId.StartsWith("abort") || eventId == "health-lethal") return GuidePriority.Safety;
            if (eventId == "struct-warn" || eventId == "fuel-low" || eventId == "tip" || eventId == "splash" || eventId == "sep-contact")
                return GuidePriority.Failure;
            return GuidePriority.Chatter;
        }

        public bool TryNext(string eventId, out GuideBeat beat)
        {
            beat = null;
            if (string.IsNullOrEmpty(eventId) || !byEvent.TryGetValue(eventId, out var list) || list.Count == 0)
                return false;
            cursors.TryGetValue(eventId, out int cursor);
            var clip = list[cursor % list.Count];
            cursors[eventId] = cursor + 1;
            bool safety = PriorityFor(eventId) == GuidePriority.Safety;
            beat = new GuideBeat
            {
                Id = "narr:" + (string.IsNullOrEmpty(clip.File) ? eventId + ":" + cursor : clip.File),
                Speaker = Speaker(clip.Channel),
                Priority = PriorityFor(eventId),
                Critical = safety,
                MinHold = clip.Duration,
                FromEngine = false,
                Cards = new[] { new GuideCard { Kid = clip.Text } },
            };
            return true;
        }

        public void Reset() => cursors.Clear();
    }
}
