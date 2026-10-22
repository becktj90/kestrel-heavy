// Which elements clip their children (overflow: hidden) when that comes from a style sheet rather than inline style.
// Unity 6.3 has no public computed overflow: IResolvedStyle has no overflow member, and IStyle.overflow (element.style)
// reads only the inline value. So UiTreeChecks.ClipsChildren reads the inline value first and, when it is unset, matches
// the element against this list, which mirrors every "overflow: hidden" rule in the repo's .uss files plus Unity's own
// ScrollView viewport (unity-scroll-view__content-viewport, overflow: hidden in the default theme).
//
// Missing a rule here makes the checks stricter (more overlap reports), never weaker. A stale rule would hide real
// overlaps, so Kestrel.Core.Tests (dotnet, UiClipRulesUssTests) parses every .uss and fails if this list and the sheets
// differ in either direction. Update the list in the same change as the .uss.
//
// Supported selector grammar: compounds separated by whitespace (descendant combinator); a compound is one or more
// ".class" and at most one "#name". Plain C#, no UnityEngine, so the dotnet tests can compile it.
using System;
using System.Collections.Generic;

namespace Kestrel.UiChecks
{
    public static class UiClipRules
    {
        public const string ScrollViewport = "unity-scroll-view__content-viewport";

        /// <summary>Selectors of overflow: hidden. Order does not matter.</summary>
        public static readonly string[] HiddenSelectors =
        {
            "." + ScrollViewport,                     // Unity default theme
            // Kestrel.Game/Campaign/Resources/UI/MissionBoard.uss
            ".mission-act-name",
            // Kestrel.Game/Shell/UI/KH_Tokens.uss
            ".kh-mm-launch",
            ".kh-mm-tile-name",
            ".kh-mm-tile-caption",
            ".kh-mm-hint",
            ".kh-mm-thrum",
            ".kh-mm-wide .kh-mm-title-card",
            ".kh-mm-wide .kh-mm-launch",
            ".kh-mm-wide .kh-mm-design",
            ".kh-mm-wide .kh-button.kh-mm-tile",
            ".kh-mm-wide .kh-mm-stats",
            ".kh-mm-wide .kh-mm-hint",
            ".kh-mm-wide .kh-mm-thrum",
            ".kh-mm-wide .kh-mm-kestrel",
            ".kh-mm-wide .kh-mm-heavy",
            ".kh-mm-wide .kh-mm-launch-label",
            ".kh-mm-wide .kh-mm-design-label",
            ".kh-mm-wide .kh-mm-tile-name",
            ".kh-mm-wide .kh-mm-launch-caption",
            ".kh-mm-wide .kh-mm-design-caption",
            ".kh-mm-wide .kh-mm-tile-caption",
            // Resources/UI/GuideBox.uss
            ".guide-panel",
            ".guide-body",
            ".guide-body-clip",
            // UI Toolkit/DesignerV3/Stepper.uss
            "#stepper-rocket",
        };

        /// <summary>Selectors of overflow: visible. An element matching one of these and a hidden one is treated as not
        /// clipping (the stricter reading), since specificity is not modelled.</summary>
        public static readonly string[] VisibleSelectors =
        {
            ".guide-box",        // Resources/UI/GuideBox.uss
            ".launch-row",       // Resources/UI/Slice.uss
        };

        public sealed class Compound
        {
            public string Name;                                   // null: any
            public readonly List<string> Classes = new List<string>();
        }

        /// <summary>Right-most compound last. False when the selector uses grammar this matcher does not model.</summary>
        public static bool TryParse(string selector, out List<Compound> compounds)
        {
            compounds = new List<Compound>();
            if (string.IsNullOrWhiteSpace(selector)) return false;
            foreach (var part in selector.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var c = new Compound();
                int i = 0;
                while (i < part.Length)
                {
                    char k = part[i];
                    if (k != '.' && k != '#') return false;
                    int j = i + 1;
                    while (j < part.Length && (char.IsLetterOrDigit(part[j]) || part[j] == '-' || part[j] == '_')) j++;
                    if (j == i + 1) return false;
                    string id = part.Substring(i + 1, j - i - 1);
                    if (k == '.') c.Classes.Add(id);
                    else { if (c.Name != null) return false; c.Name = id; }
                    i = j;
                }
                compounds.Add(c);
            }
            return compounds.Count > 0;
        }

        /// <summary>Matches a parsed selector. <paramref name="self"/> and <paramref name="parentOf"/> are generic so this
        /// file stays free of UnityEngine.</summary>
        public static bool Matches<T>(List<Compound> sel, T self, Func<T, T> parentOf, Func<T, string> nameOf,
                                      Func<T, string, bool> hasClass) where T : class
        {
            int k = sel.Count - 1;
            if (!One(sel[k], self, nameOf, hasClass)) return false;
            k--;
            for (var p = parentOf(self); p != null && k >= 0; p = parentOf(p))
                if (One(sel[k], p, nameOf, hasClass)) k--;
            return k < 0;
        }

        static bool One<T>(Compound c, T e, Func<T, string> nameOf, Func<T, string, bool> hasClass)
        {
            if (c.Name != null && nameOf(e) != c.Name) return false;
            foreach (var cls in c.Classes) if (!hasClass(e, cls)) return false;
            return true;
        }

        static List<List<Compound>> _hidden, _visible;
        public static List<List<Compound>> Hidden => _hidden ??= ParseAll(HiddenSelectors);
        public static List<List<Compound>> Visible => _visible ??= ParseAll(VisibleSelectors);

        static List<List<Compound>> ParseAll(string[] selectors)
        {
            var list = new List<List<Compound>>();
            foreach (var s in selectors)
                if (TryParse(s, out var c)) list.Add(c);     // UiClipRulesUssTests fails on any that does not parse
            return list;
        }
    }
}
