// Designer tiers copy (docs/DESIGNER_TIERS_SPEC.md, delivery step 2): data/designer-tiers.json holds every word the
// tier switch, the Kid Why? cards, the speed bar and the X-ray view show. Pure: the host passes the file text (checkout
// or Resources/KestrelData/designer-tiers) and sets Active; with no copy the Starter screen hides those extras.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Features;
using Kestrel.Core.Json;

namespace Kestrel.Core.Designer
{
    /// <summary>One Kid Why? card: the row it explains, its Codex lesson, a pictogram id and one or two plain sentences.</summary>
    public sealed class StarterWhyCopy
    {
        public StarterKnob Knob;
        public string Row, Codex, Icon, Text;
        /// <summary>Optional: the card when the mission locks the row (KH-10 / KH-19 boosters), and when the top
        /// stage burns hydrogen (Stages). They carry the state hints the hint line used to show.</summary>
        public string Locked, Hydrogen;

        /// <summary>Every text this card can show.</summary>
        public IEnumerable<string> Texts()
        {
            yield return Text;
            if (!string.IsNullOrEmpty(Locked)) yield return Locked;
            if (!string.IsNullOrEmpty(Hydrogen)) yield return Hydrogen;
        }
    }

    public sealed class XrayPartCopy
    {
        public string Title = "", Line = "";
    }

    public sealed class DesignerTierText
    {
        public const string FileName = "designer-tiers";
        /// <summary>Kid wording: at most this many sentences and characters in a Why? card.</summary>
        public const int WhyMaxSentences = 2, WhyMaxChars = 140;

        /// <summary>The copy the game reads. The host sets it at boot; null hides the tier extras.</summary>
        public static DesignerTierText Active;

        readonly Dictionary<ExperienceTier, string> tiers = new Dictionary<ExperienceTier, string>();
        readonly List<StarterWhyCopy> why = new List<StarterWhyCopy>();
        readonly Dictionary<string, XrayPartCopy> parts = new Dictionary<string, XrayPartCopy>();
        readonly Dictionary<string, string> numbers = new Dictionary<string, string>();
        readonly Dictionary<string, string> events = new Dictionary<string, string>();

        public string SwitchHint = "";
        public string WhyButton = "?", WhySpoken = "";
        public string SpeedOrbit = "", SpeedHop = "", SpeedEscape = "", SpeedEnough = "", SpeedShort = "", SpokenEnough = "", SpokenShort = "";
        public string XrayButton = "", XrayButtonHint = "", XrayTitle = "", XrayClose = "", XrayCloseHint = "", XrayPick = "", XrayPlan = "";

        public IReadOnlyList<StarterWhyCopy> Why => why;
        public IReadOnlyDictionary<string, XrayPartCopy> Parts => parts;
        public IReadOnlyDictionary<string, string> Events => events;

        public string TierLabel(ExperienceTier t) => tiers.TryGetValue(t, out var s) ? s : "";

        public StarterWhyCopy WhyFor(StarterKnob k)
        {
            foreach (var w in why) if (w.Knob == k) return w;
            return null;
        }

        /// <summary>The card's text for this rocket: the locked card on a locked boosters row, the hydrogen card on a
        /// hydrogen top stage, else the plain card. Empty when the row has no card.</summary>
        public string WhyText(StarterKnob k, StarterRocket r)
        {
            var w = WhyFor(k);
            if (w == null) return "";
            if (k == StarterKnob.Boosters && r != null && r.BoostersLocked && !string.IsNullOrEmpty(w.Locked)) return w.Locked;
            if (k == StarterKnob.Stages && StarterScreen.HydrogenAuto(r) && !string.IsNullOrEmpty(w.Hydrogen)) return w.Hydrogen;
            return w.Text;
        }

        public XrayPartCopy Part(string key) => key != null && parts.TryGetValue(key, out var p) ? p : new XrayPartCopy();
        public string Numbers(string key) => key != null && numbers.TryGetValue(key, out var s) ? s : "";
        public string Event(string key) => key != null && events.TryGetValue(key, out var s) ? s : "";

        /// <summary>The spoken name of the Why? button on a row ("Why does Stages matter?").</summary>
        public string WhySpokenFor(string rowLabel) => (WhySpoken ?? "").Replace("{row}", rowLabel ?? "");

        public static StarterKnob KnobOf(string row)
        {
            switch (row)
            {
                case "engine": return StarterKnob.Engine;
                case "stages": return StarterKnob.Stages;
                case "fuel": return StarterKnob.FuelNotch;
                case "boosters": return StarterKnob.Boosters;
                default: return StarterKnob.None;
            }
        }

        public static string RowOf(StarterKnob k)
        {
            switch (k)
            {
                case StarterKnob.Engine: return "engine";
                case StarterKnob.Stages: return "stages";
                case StarterKnob.FuelNotch: return "fuel";
                case StarterKnob.Boosters: return "boosters";
                default: return null;
            }
        }

        public static DesignerTierText Load(string repoRoot) => Parse(File.ReadAllText(Path.Combine(repoRoot, "data", FileName + ".json")));

        public static DesignerTierText Parse(string json)
        {
            var o = MiniJson.ParseObject(json);
            if (o.Num("version") != 1) throw new FormatException("designer-tiers version must be 1");
            var t = new DesignerTierText();
            var tiers = o.Obj("tiers") ?? throw new FormatException("designer-tiers.tiers missing");
            foreach (var tier in DesignerTiers.Order)
                t.tiers[tier] = Need(tiers, ExperienceSettings.TierId(tier), "tiers");
            t.SwitchHint = o.Str("switchHint") ?? "";

            var why = o.Obj("why") ?? throw new FormatException("designer-tiers.why missing");
            t.WhyButton = Need(why, "button", "why");
            t.WhySpoken = Need(why, "spoken", "why");
            var rows = why.Arr("rows") ?? throw new FormatException("designer-tiers.why.rows missing");
            foreach (var r in rows)
            {
                if (!(r is JsonObject row)) throw new FormatException("designer-tiers.why.rows entry is not an object");
                string id = Need(row, "row", "why.rows");
                var k = KnobOf(id);
                if (k == StarterKnob.None) throw new FormatException("designer-tiers.why.rows: unknown row " + id);
                if (t.WhyFor(k) != null) throw new FormatException("designer-tiers.why.rows: duplicate row " + id);
                t.why.Add(new StarterWhyCopy { Knob = k, Row = id, Codex = Need(row, "codex", id), Icon = Need(row, "icon", id), Text = Need(row, "text", id), Locked = row.Str("locked"), Hydrogen = row.Str("hydrogen") });
            }

            var speed = o.Obj("speed") ?? throw new FormatException("designer-tiers.speed missing");
            t.SpeedOrbit = Need(speed, "orbit", "speed");
            t.SpeedHop = Need(speed, "hop", "speed");
            t.SpeedEscape = Need(speed, "escape", "speed");
            t.SpeedEnough = Need(speed, "enough", "speed");
            t.SpeedShort = Need(speed, "short", "speed");
            t.SpokenEnough = Need(speed, "spokenEnough", "speed");
            t.SpokenShort = Need(speed, "spokenShort", "speed");

            var x = o.Obj("xray") ?? throw new FormatException("designer-tiers.xray missing");
            t.XrayButton = Need(x, "button", "xray");
            t.XrayButtonHint = Need(x, "buttonHint", "xray");
            t.XrayTitle = Need(x, "title", "xray");
            t.XrayClose = Need(x, "close", "xray");
            t.XrayCloseHint = Need(x, "closeHint", "xray");
            t.XrayPick = Need(x, "pick", "xray");
            t.XrayPlan = Need(x, "plan", "xray");
            var parts = x.Obj("parts") ?? throw new FormatException("designer-tiers.xray.parts missing");
            foreach (var k in parts.Order)
                if (parts[k] is JsonObject p) t.parts[k] = new XrayPartCopy { Title = Need(p, "title", k), Line = Need(p, "line", k) };
            var nums = x.Obj("numbers");
            if (nums != null) foreach (var k in nums.Order) if (nums[k] is string s) t.numbers[k] = s;
            var ev = x.Obj("events") ?? throw new FormatException("designer-tiers.xray.events missing");
            foreach (var k in ev.Order) if (ev[k] is string s) t.events[k] = s;
            return t;
        }

        /// <summary>Sentences in a line: runs ending in '.', '!' or '?' (a trailing run without one counts too).</summary>
        public static int Sentences(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            int n = 0;
            bool inRun = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '.' || c == '!' || c == '?')
                {
                    if (inRun) n++;
                    inRun = false;
                }
                else if (!char.IsWhiteSpace(c)) inRun = true;
            }
            if (inRun) n++;
            return n;
        }

        static string Need(JsonObject o, string key, string where)
        {
            string s = o.Str(key);
            if (string.IsNullOrEmpty(s)) throw new FormatException("designer-tiers." + where + "." + key + " missing");
            return s;
        }
    }
}
