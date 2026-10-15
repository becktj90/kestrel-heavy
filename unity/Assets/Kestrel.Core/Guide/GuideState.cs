// kh.save.v1.guide. Tips, subtitle speed and size, voice, and which lines already played.
// A missing file uses defaults. A corrupt file is kept as .bak and the defaults return.
// Voice starts ON only when pre-generated clips are on disk. Text does not depend on voice.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Json;

namespace Kestrel.Core.Guide
{
    public enum GuideTips
    {
        On = 0,
        Warnings = 1,
        Off = 2,
    }

    public enum GuideSpeed
    {
        Slow = 0,
        Normal = 1,
        Fast = 2,
        Instant = 3,
    }

    public enum GuideSize
    {
        Normal = 0,
        Large = 1,
        ExtraLarge = 2,
    }

    public sealed class GuideSettings
    {
        public const double SlowCharsPerSecond = 25;
        public const double NormalCharsPerSecond = 40;
        public const double FastCharsPerSecond = 70;
        public const double MinPointSize = 15;

        public GuideTips Tips = GuideTips.On;
        public GuideSpeed Speed = GuideSpeed.Normal;
        public GuideSize Size = GuideSize.Normal;
        public bool VoiceOn;
        public bool VoiceAvailable;
        public bool ReduceMotion;

        /// <summary>0 means the whole line appears at once (Instant, or Reduce Motion).</summary>
        public double CharsPerSecond
        {
            get
            {
                if (ReduceMotion || Speed == GuideSpeed.Instant) return 0;
                if (Speed == GuideSpeed.Slow) return SlowCharsPerSecond;
                if (Speed == GuideSpeed.Fast) return FastCharsPerSecond;
                return NormalCharsPerSecond;
            }
        }
    }

    public sealed class GuideState
    {
        public const string SaveKey = "kh.save.v1.guide";

        public bool FirstRunDone;
        public readonly List<string> SeenLines = new List<string>();
        public GuideSettings Settings = new GuideSettings();
        readonly HashSet<string> seen = new HashSet<string>();

        public static GuideState Fresh(bool clipsPresent, bool hasDesigns)
        {
            var state = new GuideState();
            state.Settings.VoiceAvailable = clipsPresent;
            state.Settings.VoiceOn = clipsPresent;
            state.FirstRunDone = hasDesigns;
            return state;
        }

        /// <summary>Safety and decisions always play. Warnings adds failures. Off hides chatter and failure color.</summary>
        public bool Allows(GuideLine line)
        {
            if (line == null) return false;
            if (line.Priority == GuidePriority.Chatter && Settings.Tips != GuideTips.On) return false;
            if (line.Priority == GuidePriority.Failure && Settings.Tips == GuideTips.Off) return false;
            if (line.Once && !line.Safety && seen.Contains(line.Id)) return false;
            return true;
        }

        public bool HasSeen(string id) => id != null && seen.Contains(id);

        public void MarkSeen(string id)
        {
            if (string.IsNullOrEmpty(id) || !seen.Add(id)) return;
            SeenLines.Add(id);
        }

        public string ToJson()
        {
            var data = DataObject();
            var env = new JsonObject();
            env.Add("schema", 1d);
            env.Add("kind", "guide");
            env.Add("id", "guide");
            env.Add("name", "guide");
            env.Add("data", data);
            return Kestrel.Core.Save.JsonWriter.Stringify(env);
        }

        public JsonObject DataObject()
        {
            var data = new JsonObject();
            data.Add("firstRunDone", FirstRunDone);
            var ids = new List<object>();
            for (int i = 0; i < SeenLines.Count; i++) ids.Add(SeenLines[i]);
            data.Add("seenLines", ids);
            data.Add("tipsOn", TipsName(Settings.Tips));
            data.Add("subtitleSpeed", SpeedName(Settings.Speed));
            data.Add("subtitleSize", SizeName(Settings.Size));
            data.Add("voiceOn", Settings.VoiceOn);
            return data;
        }

        public static GuideState Parse(string json, bool clipsPresent)
        {
            var root = MiniJson.ParseObject(json);
            var data = root.Str("kind") == "guide" && root.Obj("data") != null ? root.Obj("data") : root;
            var state = Fresh(clipsPresent, false);
            if (data.ContainsKey("firstRunDone")) state.FirstRunDone = data.Bool("firstRunDone");
            var seen = data.Arr("seenLines");
            if (seen != null)
            {
                for (int i = 0; i < seen.Count; i++)
                    if (seen[i] is string id) state.MarkSeen(id);
            }
            if (data.ContainsKey("tipsOn")) state.Settings.Tips = ReadTips(data["tipsOn"]);
            if (data.ContainsKey("subtitleSpeed")) state.Settings.Speed = ReadSpeed(data["subtitleSpeed"]);
            if (data.ContainsKey("subtitleSize")) state.Settings.Size = ReadSize(data["subtitleSize"]);
            state.Settings.VoiceAvailable = clipsPresent;
            if (data.ContainsKey("voiceOn") && data["voiceOn"] is bool voice) state.Settings.VoiceOn = voice;
            else state.Settings.VoiceOn = clipsPresent;
            return state;
        }

        public void Save(string directory)
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, SaveKey + ".json");
            File.WriteAllText(path, ToJson());
        }

        public static GuideState Load(string directory, bool clipsPresent, bool hasDesigns)
        {
            string path = Path.Combine(directory ?? "", SaveKey + ".json");
            if (!File.Exists(path)) return Fresh(clipsPresent, hasDesigns);
            string raw;
            try { raw = File.ReadAllText(path); }
            catch (IOException) { return Fresh(clipsPresent, hasDesigns); }
            try
            {
                var state = Parse(raw, clipsPresent);
                state.Settings.VoiceAvailable = clipsPresent;
                return state;
            }
            catch (Exception)
            {
                KeepBak(path, raw);
                return Fresh(clipsPresent, hasDesigns);
            }
        }

        static void KeepBak(string path, string raw)
        {
            try
            {
                File.WriteAllText(path + ".bak", raw ?? "");
                File.Delete(path);
            }
            catch (IOException) { }
        }

        public static string TipsName(GuideTips tips)
        {
            if (tips == GuideTips.Warnings) return "warnings";
            if (tips == GuideTips.Off) return "off";
            return "on";
        }

        public static string SpeedName(GuideSpeed speed)
        {
            if (speed == GuideSpeed.Slow) return "slow";
            if (speed == GuideSpeed.Fast) return "fast";
            if (speed == GuideSpeed.Instant) return "instant";
            return "normal";
        }

        public static string SizeName(GuideSize size)
        {
            if (size == GuideSize.Large) return "large";
            if (size == GuideSize.ExtraLarge) return "extra-large";
            return "normal";
        }

        static GuideTips ReadTips(object value)
        {
            if (value is bool b) return b ? GuideTips.On : GuideTips.Off;
            string s = value as string;
            if (s == "warnings") return GuideTips.Warnings;
            if (s == "off" || s == "false") return GuideTips.Off;
            return GuideTips.On;
        }

        static GuideSpeed ReadSpeed(object value)
        {
            if (value is double n)
            {
                if (n == 0) return GuideSpeed.Instant;
                if (Math.Abs(n - GuideSettings.SlowCharsPerSecond) < 0.1) return GuideSpeed.Slow;
                if (Math.Abs(n - GuideSettings.FastCharsPerSecond) < 0.1) return GuideSpeed.Fast;
                return GuideSpeed.Normal;
            }
            string s = value as string;
            if (s == "slow") return GuideSpeed.Slow;
            if (s == "fast") return GuideSpeed.Fast;
            if (s == "instant") return GuideSpeed.Instant;
            return GuideSpeed.Normal;
        }

        static GuideSize ReadSize(object value)
        {
            string s = value as string;
            if (s == "large") return GuideSize.Large;
            if (s == "extra-large" || s == "extralarge" || s == "xl") return GuideSize.ExtraLarge;
            return GuideSize.Normal;
        }
    }

    /// <summary>Pre-generated clips under assets/narration/guide. Text stays the source of truth.</summary>
    public static class GuideVoice
    {
        public const string RelativeDir = "assets/narration/guide";

        public static bool ClipsPresent(string repoRoot)
        {
            string dir = Dir(repoRoot);
            if (!Directory.Exists(dir)) return false;
            foreach (var file in Directory.GetFiles(dir))
            {
                string ext = Path.GetExtension(file);
                if (ext == ".mp3" || ext == ".wav" || ext == ".ogg") return true;
            }
            string manifest = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifest)) return false;
            try
            {
                var root = MiniJson.ParseObject(File.ReadAllText(manifest));
                var clips = root.Arr("clips");
                return clips != null && clips.Count > 0;
            }
            catch (FormatException) { return false; }
        }

        public static string ClipFor(string repoRoot, string lineId)
        {
            if (string.IsNullOrEmpty(repoRoot) || string.IsNullOrEmpty(lineId)) return null;
            string dir = Dir(repoRoot);
            string[] ext = { ".mp3", ".wav", ".ogg" };
            for (int i = 0; i < ext.Length; i++)
            {
                string path = Path.Combine(dir, lineId + ext[i]);
                if (File.Exists(path)) return path;
            }
            return null;
        }

        static string Dir(string repoRoot) => Path.Combine(repoRoot ?? "", "assets", "narration", "guide");
    }
}
