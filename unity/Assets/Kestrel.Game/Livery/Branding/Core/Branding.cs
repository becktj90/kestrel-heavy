// Template branding editor (A2, #142): name + mark (1 of 12 original glyphs + 1 of 6 frames) + colours from a
// 16-colour palette. Engine-free. Glyph/frame art: unity/Assets/Art/Vehicle/Branding (SVG source, 64x64 viewBox).
// Guards: real-brand name filter + blocked mark/colour combos that resemble well-known agency-style marks.
// Final trademark sign-off is human: Chief of Staff signs the checklist, Trevor has the final say (TRADEMARK_REVIEW.md).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Kestrel.Livery.Branding
{
    [Serializable]
    public struct BrandingV1
    {
        public const string Schema = "branding_v1";
        public string Name;          // livery.name, 1..20 chars
        public string Glyph, Frame;  // ids from BrandingCatalog
        public int GlyphColour, FrameColour, Primary, Accent;   // palette indices 0..15
        public string SerialClass;   // A..Z, serial = KH-<class>-<nnn> (nnn from Fleet, Gameplay Dev)
    }

    public static class BrandingCatalog
    {
        public static readonly string[] Glyphs =
        {
            "glyph_chevron_up", "glyph_star_four", "glyph_comet", "glyph_peak", "glyph_wave", "glyph_spark",
            "glyph_arrow_split", "glyph_hex_cell", "glyph_kestrel_wing", "glyph_planet_band", "glyph_triple_bar", "glyph_orbit_dot"
        };
        public static readonly string[] Frames = { "frame_none", "frame_roundel", "frame_shield", "frame_hex", "frame_diamond", "frame_banner" };

        /// <summary>Order is the save format: never reorder, only append (and then bump the schema).</summary>
        public static readonly string[] Palette =
        {
            "#E8ECEF", "#1E3A5F", "#14161A", "#F2C21A", "#C83C32", "#D27A3A", "#2B7A5A", "#50DCF0",
            "#5A5E66", "#9EA3A8", "#7A5AA0", "#B58A5A", "#3A6EA5", "#8FBF4A", "#E07AA0", "#F5E6C8"
        };
        public const int MaxNameLength = 20, SvgViewBox = 64, SdfSize = 256;

        public static string Serial(string serialClass, int airframe) => FormattableString.Invariant($"KH-{serialClass}-{airframe:000}");
    }

    public static class BrandingValidator
    {
        /// <summary>Problems as codes for UX copy ("name_length", "name_brand", "mark_lookalike", ...). Empty = OK.</summary>
        public static List<string> Validate(BrandingV1 b)
        {
            var e = new List<string>();
            var n = b.Name ?? "";
            if (n.Trim().Length < 1 || n.Length > BrandingCatalog.MaxNameLength) e.Add("name_length");
            foreach (var c in n) if (!(char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '.' || c == '\'')) { e.Add("name_chars"); break; }
            if (NameGuard.IsBlocked(n)) e.Add("name_brand");
            if (Array.IndexOf(BrandingCatalog.Glyphs, b.Glyph) < 0) e.Add("glyph_unknown");
            if (Array.IndexOf(BrandingCatalog.Frames, b.Frame) < 0) e.Add("frame_unknown");
            foreach (var i in new[] { b.GlyphColour, b.FrameColour, b.Primary, b.Accent })
                if (i < 0 || i >= BrandingCatalog.Palette.Length) { e.Add("colour_index"); break; }
            if (e.Contains("colour_index")) return e;
            if (b.Primary == b.Accent) e.Add("colours_same");
            if (Contrast(BrandingCatalog.Palette[b.GlyphColour], BrandingCatalog.Palette[b.Primary]) < 2.0) e.Add("glyph_contrast");
            if (MarkGuard.IsLookalike(b)) e.Add("mark_lookalike");
            if (b.SerialClass == null || b.SerialClass.Length != 1 || b.SerialClass[0] < 'A' || b.SerialClass[0] > 'Z') e.Add("serial_class");
            return e;
        }

        /// <summary>WCAG contrast ratio between two #RRGGBB colours.</summary>
        public static double Contrast(string a, string b)
        {
            double L(string h)
            {
                double Ch(int i) { double c = int.Parse(h.Substring(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0; return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4); }
                return 0.2126 * Ch(1) + 0.7152 * Ch(3) + 0.0722 * Ch(5);
            }
            double x = L(a), y = L(b); return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
        }
    }

    /// <summary>Blocks names that match or closely imitate real space agencies, launch companies, vehicles, pads and
    /// well-known brands. The list is for filtering only; none of these names appear anywhere in the game.</summary>
    public static class NameGuard
    {
        public static readonly string[] Deny =
        {
            "nasa", "esa", "jaxa", "isro", "roscosmos", "cnsa", "csa", "kari", "dlr", "cnes", "ukspaceagency",
            "spacex", "blueorigin", "newglenn", "newshepard", "jacklyn", "lc36", "lc39", "slc40", "capecanaveral", "kennedyspacecenter", "vandenberg", "baikonur", "kourou", "wallops", "bocachica", "starbase",
            "ula", "unitedlaunchalliance", "rocketlab", "arianespace", "ariane", "boeing", "lockheed", "lockheedmartin", "northrop", "northropgrumman", "virgingalactic", "virginorbit", "firefly", "relativity", "relativityspace", "astra", "abl", "sierraspace", "axiom", "landspace", "galacticenergy", "isar", "rfa", "skyroot", "gilmour",
            "falcon", "falcon9", "falconheavy", "starship", "superheavy", "dragon", "vulcan", "atlasv", "deltaiv", "electron", "neutron", "soyuz", "proton", "angara", "longmarch", "vega", "h2a", "h3", "pslv", "gslv", "saturnv", "apollo", "artemis", "orion", "sls", "gemini", "antares", "minotaur", "pegasus", "terran", "alpha", "dreamchaser", "starliner", "crewdragon", "cygnus", "tianzhou", "shenzhou",
            "tesla", "apple", "google", "microsoft", "amazon", "nike", "cocacola", "pepsi", "redbull", "disney", "lego", "nintendo", "xai", "grok"
        };

        /// <summary>Lower-case, fold common look-alike characters, drop spaces/punctuation.</summary>
        public static string Normalise(string s)
        {
            var b = new StringBuilder();
            foreach (var ch in (s ?? "").ToLowerInvariant().Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
                char c = ch switch { '0' => 'o', '1' => 'i', '3' => 'e', '4' => 'a', '5' => 's', '7' => 't', '8' => 'b', '@' => 'a', '$' => 's', '!' => 'i', _ => ch };
                if (char.IsLetter(c)) b.Append(c);
                else if (char.IsDigit(ch)) b.Append(ch);   // keep digits that were not folded (e.g. "9" in falcon9)
            }
            return b.ToString();
        }

        public static bool IsBlocked(string name)
        {
            var raw = (name ?? "").ToLowerInvariant();
            var n = Normalise(name);
            if (n.Length == 0) return false;
            var words = new List<string>();
            foreach (var w in raw.Split(new[] { ' ', '-', '.', '\'' }, StringSplitOptions.RemoveEmptyEntries)) words.Add(Normalise(w));
            // Digit-preserving variant for ids like lc36 / falcon9.
            var nd = new StringBuilder(); foreach (var c in raw) if (char.IsLetterOrDigit(c)) nd.Append(c);
            foreach (var d in Deny)
            {
                if (n == d || nd.ToString() == d) return true;
                if (d.Length >= 5 && (n.Contains(d) || nd.ToString().Contains(d))) return true;   // "BlueOriginX", "My SpaceX Co"
                foreach (var w in words) if (w == d) return true;                                    // whole-word short names: "NASA Labs"
                int tol = d.Length >= 8 ? 2 : d.Length >= 5 ? 1 : 0;
                if (tol > 0 && Math.Abs(n.Length - d.Length) <= tol && Levenshtein(n, d) <= tol) return true;   // "Blu Origin", "SpaceY"
                if (tol > 0) foreach (var w in words) if (Math.Abs(w.Length - d.Length) <= tol && Levenshtein(w, d) <= tol) return true;
            }
            return false;
        }

        public static int Levenshtein(string a, string b)
        {
            var p = new int[b.Length + 1]; var q = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) p[j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                q[0] = i;
                for (int j = 1; j <= b.Length; j++) q[j] = Math.Min(Math.Min(q[j - 1] + 1, p[j] + 1), p[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
                var t = p; p = q; q = t;
            }
            return p[b.Length];
        }
    }

    /// <summary>Blocks template combinations that read like well-known agency-style marks (e.g. a blue roundel with an
    /// orbit and a red accent, or a feather/wing in a roundel in national-flag colours). Rules are abstract: they
    /// describe shape + colour families, not any logo file.</summary>
    public static class MarkGuard
    {
        static bool Blue(int i) => i == 1 || i == 12;          // navy, mid blue
        static bool Red(int i) => i == 4;
        static bool White(int i) => i == 0 || i == 15;

        public static bool IsLookalike(BrandingV1 b)
        {
            bool roundel = b.Frame == "frame_roundel";
            // Blue disc-like roundel + orbit/planet glyph + red or white accent: classic agency-roundel family.
            if (roundel && (b.Glyph == "glyph_orbit_dot" || b.Glyph == "glyph_planet_band") && Blue(b.Primary) && (Red(b.Accent) || Red(b.GlyphColour)))
                return true;
            // Wing/chevron inside a roundel in blue + red + white (airline/agency roundel family).
            if (roundel && (b.Glyph == "glyph_kestrel_wing" || b.Glyph == "glyph_chevron_up") && Blue(b.FrameColour) && Red(b.GlyphColour) && White(b.Primary))
                return true;
            // Upward arrow/chevron alone (no frame) in red on white or white on red: common space-company swoosh family.
            if (b.Frame == "frame_none" && (b.Glyph == "glyph_arrow_split" || b.Glyph == "glyph_chevron_up") && ((Red(b.GlyphColour) && White(b.Primary)) || (White(b.GlyphColour) && Red(b.Primary))))
                return true;
            return false;
        }
    }
}
