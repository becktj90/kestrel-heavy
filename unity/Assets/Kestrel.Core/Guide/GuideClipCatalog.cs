// Beat and card ids from assets/narration/guide/manifest.json.
// A clip file is guide/<cardId>.mp3 (or ogg). Ascent beats are absent; a missing id is not an error.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Json;

namespace Kestrel.Core.Guide
{
    public sealed class GuideClip
    {
        public string Id = "";
        public string Beat = "";
        public string File = "";
        public string Spoken = "";
        public string Text = "";
        public float Duration;
        /// <summary>Unity Resources path, no extension. Narration/guide/first.2.c1</summary>
        public string Resource = "";
        /// <summary>Absolute path when the catalog was loaded from a repo. Empty if the file is not on disk.</summary>
        public string FullPath = "";
    }

    /// <summary>Card id and beat id to a clip. Bad JSON and unknown ids yield nothing.</summary>
    public sealed class GuideClipCatalog
    {
        public static readonly GuideClipCatalog Empty = new GuideClipCatalog();

        readonly Dictionary<string, GuideClip> byCard = new Dictionary<string, GuideClip>();
        readonly Dictionary<string, List<GuideClip>> byBeat = new Dictionary<string, List<GuideClip>>();

        public int Count => byCard.Count;

        public static GuideClipCatalog Load(string repoRoot)
        {
            try
            {
                string path = Path.Combine(repoRoot ?? "", "assets", "narration", "guide", "manifest.json");
                if (!File.Exists(path)) return Empty;
                return Parse(File.ReadAllText(path), repoRoot);
            }
            catch (Exception) { return Empty; }
        }

        public static GuideClipCatalog Parse(string json) => Parse(json, null);

        public static GuideClipCatalog Parse(string json, string repoRoot)
        {
            var catalog = new GuideClipCatalog();
            if (string.IsNullOrEmpty(json)) return catalog;
            JsonObject root;
            try { root = MiniJson.ParseObject(json); }
            catch (Exception) { return catalog; }
            if (root == null) return catalog;
            var clips = root.Arr("clips");
            if (clips == null) return catalog;
            for (int i = 0; i < clips.Count; i++)
            {
                var o = clips[i] as JsonObject;
                if (o == null) continue;
                string id = o.Str("id");
                string file = o.Str("file");
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(file)) continue;
                if (catalog.byCard.ContainsKey(id)) continue;
                string spoken = o.Str("spokenText");
                string text = o.Str("text");
                var clip = new GuideClip
                {
                    Id = id,
                    Beat = o.Str("beat") ?? "",
                    File = file.Replace('\\', '/'),
                    Spoken = string.IsNullOrEmpty(spoken) ? (text ?? "") : spoken,
                    Text = text ?? "",
                    Duration = (float)o.Num("durationSec"),
                    Resource = ResourceFor(file),
                    FullPath = Full(repoRoot, file),
                };
                catalog.byCard[id] = clip;
                if (string.IsNullOrEmpty(clip.Beat)) continue;
                if (!catalog.byBeat.TryGetValue(clip.Beat, out var list))
                {
                    list = new List<GuideClip>();
                    catalog.byBeat[clip.Beat] = list;
                }
                list.Add(clip);
            }
            return catalog;
        }

        public bool TryCard(string cardId, out GuideClip clip)
        {
            clip = null;
            if (string.IsNullOrEmpty(cardId)) return false;
            return byCard.TryGetValue(cardId, out clip);
        }

        /// <summary>The first card of a beat, in manifest order.</summary>
        public bool TryBeat(string beatId, out GuideClip clip)
        {
            clip = null;
            var cards = Cards(beatId);
            if (cards.Count == 0) return false;
            clip = cards[0];
            return true;
        }

        public IReadOnlyList<GuideClip> Cards(string beatId)
        {
            if (string.IsNullOrEmpty(beatId) || !byBeat.TryGetValue(beatId, out var list))
                return Array.Empty<GuideClip>();
            return list;
        }

        /// <summary>Resources.Load key. Strips a trailing audio extension. guide/first.2.c1.mp3 → Narration/guide/first.2.c1</summary>
        public static string ResourceFor(string file)
        {
            if (string.IsNullOrEmpty(file)) return "";
            string rel = file.Replace('\\', '/');
            const string prefix = "assets/narration/";
            if (rel.StartsWith(prefix, StringComparison.Ordinal)) rel = rel.Substring(prefix.Length);
            rel = StripAudio(rel);
            if (rel.StartsWith("Narration/", StringComparison.Ordinal)) return rel;
            return "Narration/" + rel;
        }

        static string Full(string repoRoot, string file)
        {
            if (string.IsNullOrEmpty(repoRoot) || string.IsNullOrEmpty(file)) return "";
            string rel = file.Replace('\\', '/');
            const string prefix = "assets/narration/";
            if (rel.StartsWith(prefix, StringComparison.Ordinal)) rel = rel.Substring(prefix.Length);
            return Path.Combine(repoRoot, "assets", "narration", rel.Replace('/', Path.DirectorySeparatorChar));
        }

        static string StripAudio(string rel)
        {
            if (rel.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)) return rel.Substring(0, rel.Length - 4);
            if (rel.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)) return rel.Substring(0, rel.Length - 4);
            if (rel.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) return rel.Substring(0, rel.Length - 4);
            return rel;
        }
    }
}
