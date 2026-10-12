// Hangar warnings. Every issue Validate.Engineering raises (and the budget check, which lives here because
// money is a Hangar rule, not an engineering one) becomes an Advisory with three layers of text:
// a short headline, a kid sentence, and the exact engineer message behind an expand.
// Blocks stop Launch; warnings and notes never do. Kid wording: DesignRules' own kid text when it has one,
// else the table below (the U1 table from DESIGNER_SPEC §6 plus the newer codes).
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;

namespace Kestrel.Core.Hangar
{
    public enum AdvisorySeverity { Block = 0, Warn = 1, Note = 2 }

    public sealed class Advisory
    {
        public string code, tab;
        public AdvisorySeverity severity;
        /// <summary>Two or three words for the chip ("Too heavy", "Over budget").</summary>
        public string headline;
        /// <summary>One plain sentence a kid can act on.</summary>
        public string kid;
        /// <summary>The exact engineering message, shown when the row is expanded or in the Engineer layer.</summary>
        public string engineer;
        /// <summary>Part to ring in the stack list and the 3D view.</summary>
        public PartRef part;
    }

    public static class KidWords
    {
        public const string Fallback = "Something on this rocket needs a fix. Try Make it fly.";
        public const string DvMarginThin = "You'll just make it. A little more fuel gives room to spare.";

        public static readonly IReadOnlyDictionary<string, string> Table = new Dictionary<string, string>
        {
            ["zero-engines"] = "A stage or booster has no engines. Give it at least one.",
            ["zero-propellant"] = "A stage or booster has no fuel. Add some fuel.",
            ["schema"] = Experience.KidSchema,
            ["math"] = "We couldn't work this rocket out. Try Make it fly.",
            ["twr-low"] = "Too heavy to lift off. Add engines to the bottom stage or use less fuel.",
            ["engines-dont-fit"] = "Too many engines for this width. Use fewer or make it wider.",
            ["top-heavy"] = "One stage is much wider than the one under it. Make it narrower.",
            ["fairing-narrow"] = "The nose is too thin for the stage under it. Make it as wide as the stage.",
            ["payload-doesnt-fit"] = "The cargo doesn't fit in the nose. Make the nose longer or wider.",
            ["twr-marginal"] = "Slow off the pad. A bit more push helps.",
            ["weak-upper"] = "One of the upper stages pushes gently. A stronger engine helps.",
            ["weak-upper-dv"] = "The top stage does little work. Give it more fuel.",
            ["dv-margin"] = "Not quite enough speed for this trip. More fuel or a lighter cargo.",
            ["landing-hardware"] = "Saved landing fuel but no landing legs or grid fins. Add both, or skip landing.",
            ["heavy-fairing"] = "The nose weighs more than the cargo. Try a lighter nose.",
            ["unstable"] = "Your rocket would wobble or flip. Add fins, or put heavy parts higher up.",
            ["overstable"] = "Very steady, maybe too steady to steer well. Try smaller fins.",
            ["maxq-high"] = "The air pushes really hard on this rocket. Try a pointy nose or less push low down.",
            ["over-budget"] = "This rocket costs more than your budget. Try fewer engines or a smaller rocket.",
            ["near-budget"] = "Nearly at your budget. Keep an eye on the cost.",
        };

        public static readonly IReadOnlyDictionary<string, string> Headlines = new Dictionary<string, string>
        {
            ["zero-engines"] = "No engines", ["zero-propellant"] = "No fuel", ["schema"] = "Can't build", ["math"] = "Can't work it out",
            ["twr-low"] = "Too heavy to lift off", ["twr-marginal"] = "Slow liftoff", ["engines-dont-fit"] = "Engines don't fit",
            ["top-heavy"] = "Top-heavy", ["fairing-narrow"] = "Nose too thin", ["payload-doesnt-fit"] = "Cargo doesn't fit",
            ["weak-upper"] = "Weak upper stage", ["weak-upper-dv"] = "Top stage too small", ["dv-margin"] = "Not enough speed",
            ["landing-hardware"] = "Missing landing gear", ["heavy-fairing"] = "Heavy nose", ["unstable"] = "Wobbly",
            ["overstable"] = "Too stiff", ["maxq-high"] = "Hard air push", ["over-budget"] = "Over budget", ["near-budget"] = "Near budget",
            ["nozzle-unknown"] = "Unknown nozzle", ["cluster-count"] = "Engine layout", ["cluster-overhang"] = "Engines stick out",
            ["interstage-shape"] = "Odd joint", ["vac-engine-pad"] = "Space engine on the pad", ["engine-out"] = "No spare push",
            ["cryo-composite"] = "Tank too cold", ["rcs-for-landing"] = "Needs steering jets", ["payload-g"] = "Too bumpy",
        };

        /// <summary>Kid sentence for an issue: its own kid text, else the table, else the fallback.</summary>
        public static string For(Issue issue)
        {
            if (issue == null) return "";
            if (issue.code == "dv-margin" && issue.msg != null && issue.msg.StartsWith("Only", StringComparison.Ordinal)) return DvMarginThin;
            if (!string.IsNullOrEmpty(issue.kid)) return issue.kid;
            return issue.code != null && Table.TryGetValue(issue.code, out var w) ? w : Fallback;
        }

        public static string Headline(string code) => code != null && Headlines.TryGetValue(code, out var h) ? h : "Check this";
    }

    public static class Advisories
    {
        /// <summary>Blocks first, then warnings, then notes. Over budget is a warning, near budget a note.</summary>
        public static List<Advisory> From(EngineeringReport report, DataSheet sheet, JsonObject design)
            => From(report, sheet, design?.Arr("stages")?.Count ?? 0, design);

        public static List<Advisory> From(EngineeringReport report, DataSheet sheet, int stageCount, JsonObject design = null)
        {
            var o = new List<Advisory>();
            if (report != null)
            {
                foreach (var b in report.blocks) o.Add(Make(b, AdvisorySeverity.Block, stageCount, design));
                foreach (var w in report.warnings) o.Add(Make(w, AdvisorySeverity.Warn, stageCount, design));
            }
            if (sheet != null && sheet.budget_Kc > 0 && (sheet.overBudget || sheet.nearBudget))
            {
                string code = sheet.overBudget ? "over-budget" : "near-budget";
                double over = sheet.cost_Kc - sheet.budget_Kc;
                o.Add(new Advisory
                {
                    code = code, tab = "stages", severity = sheet.overBudget ? AdvisorySeverity.Warn : AdvisorySeverity.Note,
                    headline = KidWords.Headline(code), kid = KidWords.Table[code],
                    engineer = sheet.overBudget
                        ? $"Recurring cost {Kc(sheet.cost_Kc)} Kc is {Kc(over)} Kc ({Pct(over / sheet.budget_Kc)}) over the {Kc(sheet.budget_Kc)} Kc budget for {sheet.orbitLabel}."
                        : $"Recurring cost {Kc(sheet.cost_Kc)} Kc is {Pct(sheet.cost_Kc / sheet.budget_Kc)} of the {Kc(sheet.budget_Kc)} Kc budget for {sheet.orbitLabel}.",
                    part = PartRef.Stage(0),
                });
            }
            o.Sort((a, b) => a.severity.CompareTo(b.severity));
            return o;
        }

        static Advisory Make(Issue i, AdvisorySeverity sev, int stageCount, JsonObject design)
            => new Advisory { code = i.code, tab = i.tab, severity = sev, headline = KidWords.Headline(i.code), kid = KidWords.For(i), engineer = i.msg, part = PartFor(i, stageCount, design) };

        /// <summary>Per-part rules that DesignRules/Validate report for the booster set with a null index.</summary>
        static readonly HashSet<string> PartRules = new HashSet<string> { "nozzle-unknown", "cluster-count", "cluster-overhang", "vac-engine-pad" };

        /// <summary>
        /// Which part an issue points at, from its web tab id and stage index. Issues with no index that come from a
        /// per-part rule belong to the boosters (stages always carry an index); cryo tanks and missing engines or fuel
        /// are found in the design when it is given.
        /// </summary>
        public static PartRef PartFor(Issue i, int stageCount, JsonObject design = null)
        {
            int top = Math.Max(0, stageCount - 1);
            if (i.index == null)
            {
                if (PartRules.Contains(i.code) && HangarParts.BoosterCount(design) > 0) return PartRef.Boosters;
                var found = Find(i.code, design);
                if (found.HasValue) return found.Value;
            }
            int idx = i.index ?? 0;
            switch (i.code)
            {
                case "fairing-narrow": case "heavy-fairing": case "payload-doesnt-fit": return PartRef.Fairing;
                case "payload-g": return PartRef.Payload;
                case "weak-upper-dv": return PartRef.Stage(top);
                case "interstage-shape": return idx >= 1 ? PartRef.Interstage(idx) : PartRef.Stage(idx);
            }
            switch (i.tab)
            {
                case "strapons": return PartRef.Boosters;
                case "payload": return PartRef.Payload;
                case "stages": case "engines": case "propellants": return PartRef.Stage(Math.Min(idx, top));
                default: return PartRef.Stage(0);
            }
        }

        /// <summary>First part that breaks a part rule reported without an index (stages first, then boosters).</summary>
        static PartRef? Find(string code, JsonObject design)
        {
            if (design == null) return null;
            Func<JsonObject, bool> bad;
            switch (code)
            {
                case "cryo-composite": bad = s => s.Str("material") == "composite" && s.Str("propellant") == "hydrolox"; break;
                case "zero-engines": bad = s => !(s.Num("engineCount") >= 1); break;
                case "zero-propellant": bad = s => !(s.Num("propMass") > 0); break;
                default: return null;
            }
            var stages = design.Arr("stages");
            if (stages != null)
                for (int k = 0; k < stages.Count; k++)
                    if (stages[k] is JsonObject s && bad(s)) return PartRef.Stage(k);
            var so = design.Obj("strapOns");
            if (so != null && so.Num("count") > 0 && bad(so)) return PartRef.Boosters;
            return null;
        }

        static string Kc(double v) => Math.Round(v).ToString("N0", CultureInfo.InvariantCulture);
        static string Pct(double f) => Math.Round(f * 100).ToString(CultureInfo.InvariantCulture) + "%";
    }
}
