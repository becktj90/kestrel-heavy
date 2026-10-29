using System.Collections.Generic;
using Kestrel.Core.Flight.Narration;
using Kestrel.Core.Json;

namespace Kestrel.Game.Audio
{
    public readonly struct NarrationLine
    {
        public readonly string EventId;
        public readonly string Resource;
        public readonly string Relative;
        public readonly string Channel;
        public readonly float Duration;
        /// <summary>The clip's fact tags and layer. Null for a line built by hand (tests), which is never picked by facts.</summary>
        public readonly NarrationTake Take;

        public NarrationLine(string eventId, string resource, string relative, string channel, float duration, NarrationTake take = null)
        {
            EventId = eventId; Resource = resource; Relative = relative; Channel = channel; Duration = duration; Take = take;
        }

        public bool IsColor => Take != null && Take.IsColor;
    }

    /// <summary>
    /// Clips from assets/narration/manifest.json, keyed by the manifest "event" id.
    /// Several takes share an event; playback walks them in file order.
    /// </summary>
    public sealed class NarrationCatalog
    {
        public static readonly NarrationCatalog Empty = new NarrationCatalog();

        readonly Dictionary<string, List<NarrationLine>> byEvent = new Dictionary<string, List<NarrationLine>>();
        readonly Dictionary<string, List<NarrationTake>> takesByEvent = new Dictionary<string, List<NarrationTake>>();
        readonly List<NarrationLine> all = new List<NarrationLine>();

        public int EventCount => byEvent.Count;
        public int ClipCount => all.Count;
        public IReadOnlyList<NarrationLine> All => all;

        public static NarrationCatalog Parse(string json)
        {
            var cat = new NarrationCatalog();
            if (string.IsNullOrEmpty(json)) return cat;
            var root = MiniJson.ParseObject(json);
            var clips = root?.Arr("clips");
            if (clips == null) return cat;
            for (int i = 0; i < clips.Count; i++)
            {
                var o = clips[i] as JsonObject;
                if (o == null) continue;
                var ev = o.Str("event");
                var file = o.Str("file");
                if (string.IsNullOrEmpty(ev) || string.IsNullOrEmpty(file)) continue;
                var relative = file.Replace('\\', '/');
                var bare = relative.EndsWith(".mp3") ? relative.Substring(0, relative.Length - 4) : relative;
                var take = NarrationTake.FromJson(o);
                var line = new NarrationLine(ev, "Narration/" + bare, relative, o.Str("channel"), (float)o.Num("durationSec"), take);
                if (!cat.byEvent.TryGetValue(ev, out var list))
                {
                    list = new List<NarrationLine>();
                    cat.byEvent[ev] = list;
                    cat.takesByEvent[ev] = new List<NarrationTake>();
                }
                list.Add(line);
                cat.takesByEvent[ev].Add(take);
                cat.all.Add(line);
            }
            return cat;
        }

        public int Count(string eventId) => byEvent.TryGetValue(eventId, out var list) ? list.Count : 0;

        public bool TryLine(string eventId, int index, out NarrationLine line)
        {
            line = default;
            if (string.IsNullOrEmpty(eventId) || !byEvent.TryGetValue(eventId, out var list) || list.Count == 0)
                return false;
            if (index < 0) index = 0;
            line = list[index % list.Count];
            return true;
        }

        /// <summary>
        /// The lines of an event that are true for the facts, most specific first (see <see cref="NarrationPicker"/>).
        /// Null facts leave only the neutral lines. Empty when the event has none.
        /// </summary>
        public List<NarrationLine> Eligible(string eventId, NarrationFacts facts)
        {
            var result = new List<NarrationLine>();
            if (string.IsNullOrEmpty(eventId) || !byEvent.TryGetValue(eventId, out var lines)) return result;
            var order = NarrationPicker.Order(takesByEvent[eventId], facts);
            for (int i = 0; i < order.Count; i++) result.Add(lines[order[i]]);
            return result;
        }

        public float Duration(string resource)
        {
            for (int i = 0; i < all.Count; i++)
                if (all[i].Resource == resource) return all[i].Duration;
            return 0f;
        }
    }
}
