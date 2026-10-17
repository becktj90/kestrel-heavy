// Player-visible copy. Raw timeline ids, mission ids, and dotted or snake_case keys stay in the data.
// Anything drawn on a screen passes through here first.
using System.Text.RegularExpressions;

namespace Kestrel.Core
{
    public static class PlayerText
    {
        static readonly Regex TimelineId = new Regex(@"\btl\.[a-z0-9_.]+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex MissionId = new Regex(@"\bkh-m[a-z0-9-]*\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex KindPrefix = new Regex(
            @"\b(structural-maxq|engine-overpressure|engine-out|bad-mixture|prop-depletion|separation-collision|cryo-material|leg-collapse|recovery-fuel|recovery-fast|recovery-tilt|recovery-miss|overg)\s*:\s*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex Snake = new Regex(@"\b[a-z][a-z0-9]*_[a-z0-9_]+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex DotKey = new Regex(@"\b[a-z][a-z0-9]*\.[a-z][a-z0-9_.]*\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex Spaces = new Regex(@"[ \t]{2,}", RegexOptions.Compiled);

        public static bool Leak(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            if (TimelineId.IsMatch(text) || MissionId.IsMatch(text) || KindPrefix.IsMatch(text)) return true;
            if (Snake.IsMatch(text) || DotKey.IsMatch(text)) return true;
            return false;
        }

        public static string ForPlayer(string text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            string s = text;
            s = s.Replace("g_lim", "the limit");
            s = s.Replace("q_lim", "the limit");
            s = s.Replace("v_leg", "the leg limit");
            s = KindPrefix.Replace(s, "");
            s = TimelineId.Replace(s, "");
            s = MissionId.Replace(s, "this mission");
            s = s.Replace("payload.orbit", "the orbit");
            s = s.Replace("engineParams.pc", "chamber pressure");
            s = s.Replace("legs.kind", "the legs");
            s = Snake.Replace(s, m => m.Value.Replace('_', ' '));
            s = DotKey.Replace(s, m => m.Value.Replace('.', ' '));
            s = Spaces.Replace(s, " ");
            return s.Trim();
        }
    }
}
