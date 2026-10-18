// Player-facing designer copy. The shipped fonts cover ASCII plus a few punctuation
// marks, so every string that reaches the screen is rewritten onto those glyphs.
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Kestrel.Core.Designer
{
    public static class ShownText
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static bool Covered(char c)
        {
            if (c == '\n' || c == '\r' || c == '\t') return true;
            if (c >= 32 && c <= 126) return true;
            return c == '\u00B7' || c == '\u2013' || c == '\u2014' || c == '\u2019';
        }

        public static bool Covered(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            for (int i = 0; i < text.Length; i++)
                if (!Covered(text[i])) return false;
            return true;
        }

        /// <summary>Idempotent. Missing faces become letters the Fraunces and Space Grotesk files actually contain.</summary>
        public static string Screen(string text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            var sb = new StringBuilder(text.Length + 8);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                char next = i + 1 < text.Length ? text[i + 1] : '\0';
                switch (c)
                {
                    case '\u0394':
                    case '\u03B4':
                        sb.Append(next == 'v' || next == 'V' ? "d" : "d");
                        if (next == 'v' || next == 'V') { sb.Append('v'); i++; }
                        break;
                    case '\u2212':
                        sb.Append('-');
                        break;
                    case '\u00D7':
                    case '\u2715':
                    case '\u2717':
                    case '\u2718':
                        sb.Append('x');
                        break;
                    case '\u2713':
                    case '\u2714':
                        sb.Append("OK");
                        break;
                    case '\u2192':
                    case '\u2190':
                        sb.Append(" to ");
                        break;
                    case '\u2264':
                        sb.Append("<=");
                        break;
                    case '\u2265':
                        sb.Append(">=");
                        break;
                    case '\u00B1':
                        sb.Append("+/-");
                        break;
                    case '\u00F7':
                        sb.Append('/');
                        break;
                    case '\u00B0':
                        sb.Append(" deg");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        public static string CountLabel(int n)
        {
            if (n <= 0) return "";
            if (n == 1) return "1 thing to fix";
            return n.ToString(Inv) + " things to fix";
        }

        /// <summary>One plain sentence. Never empty, never a bare margin number, never a T+0 pad failure.</summary>
        public static string Plain(string code, string kid, string engineer)
        {
            string mapped = Mapped(Norm(code));
            if (mapped == null) mapped = Mapped(Norm(kid));
            if (mapped != null) return mapped;
            if (Usable(kid)) return Screen(kid);
            if (Usable(engineer)) return Screen(engineer);
            return "Something on this rocket needs a fix.";
        }

        public static bool Jargon(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.IndexOf("margin", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool Usable(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            if (Jargon(text)) return false;
            if (text.IndexOf("T+0", System.StringComparison.Ordinal) >= 0) return false;
            if (text.IndexOf("T+", System.StringComparison.Ordinal) >= 0) return false;
            return true;
        }

        public static string Norm(string code)
        {
            if (string.IsNullOrEmpty(code)) return "";
            if (code.StartsWith("risk-", System.StringComparison.Ordinal)) code = code.Substring(5);
            return code;
        }

        static string Mapped(string kind)
        {
            switch (kind)
            {
                case "tumble":
                    return "This rocket can flip in the air. Add fins or ease the turn.";
                case "engine-overpressure":
                    return "An engine is working harder than it is built for. Lower the chamber pressure.";
                case "prop-depletion":
                    return "This rocket runs out of fuel before the trip is done. Add fuel or another stage.";
                case "structural-maxq":
                case "maxq-high":
                    return "The air pushes harder than the tanks can take. Make the walls stronger or ease off at Max-Q.";
                case "overg":
                case "payload-g":
                    return "The climb is too bumpy for the cargo. Ease the g limit.";
                case "twr-low":
                case "zero-engines":
                    return "Too heavy to lift off. Add engines or take off some fuel.";
                case "twr-marginal":
                    return "Slow off the pad. A bit more push helps.";
                case "dv-margin":
                case "weak-upper-dv":
                    return "Not quite enough speed for this trip. Add fuel or a lighter cargo.";
                case "unstable":
                    return "This rocket can wobble or flip. Add fins.";
                case "bad-mixture":
                    return "The fuel mix is off. Put it back near the normal mix.";
                case "payload-doesnt-fit":
                case "fairing-narrow":
                    return "The cargo does not fit in the nose. Make the nose longer or wider.";
                case "cryo-material":
                case "cryo-composite":
                    return "This fuel is too cold for that tank. Pick a tank that can take the cold.";
                default:
                    return null;
            }
        }

        /// <summary>Strings the designer shows. Tests check every character against the font files.</summary>
        public static List<string> Samples()
        {
            var list = new List<string>
            {
                Screen("+1058 m/s dv · -4.52 TWR"),
                Screen("Change: +42 m/s dv · -0.03 TWR"),
                Plain("tumble", "", "tumble margin 0.07"),
                Plain("engine-overpressure", "", "engine-overpressure margin 0.15"),
                Plain("prop-depletion", "Your rocket ran out of fuel at T+0 s, short of the target.", ""),
                Plain("structural-maxq", "Your rocket bent in half at T+0 s.", "Air load past the wall."),
                Plain("maxq-high", "", ""),
                Plain("overg", "", "Peak 30 g is past the cargo rating."),
                CountLabel(0),
                CountLabel(1),
                CountLabel(4),
                "Before: 1058 m/s dv, push 0.62, 4 to fix",
                "After: 1400 m/s dv, push 1.30, 0 to fix",
                "Set the trip to a 10 km hop.",
                "Welcome to the hangar. We will build a rocket that reaches a 10 km hop.",
                "Max-Q",
                "$516M / $12M",
                "-",
                "+",
                "OK",
                "x",
                "Fix it",
                "Launch",
                "Parts",
                "Hangar",
                "Undo",
                "Redo",
                "Nothing to change.",
                "Strength 1.40 to 2.00.",
            };
            return list;
        }
    }
}
