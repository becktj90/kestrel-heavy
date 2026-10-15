// Subtitle-box model. The G7 guide engine (Kestrel.Core/Guide) owns the script, triggers,
// seen-state and save. This assembly is the box: it describes a beat and the settings the
// box reads. Names are the fictional hangar keeper and the existing narration roles.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Kestrel.Game.Guide
{
    public enum GuidePriority { Chatter = 0, Failure = 1, Decision = 2, Safety = 3 }

    public enum SubtitleSpeed { Slow, Normal, Fast, Instant }

    public enum SubtitleSize { Normal, Large, ExtraLarge }

    /// <summary>Settings → Tips from Thrum. Decisions and safety still show when tips are off,
    /// because those are calls the player has to answer.</summary>
    public enum TipsMode { On, WarningsOnly, Off }

    public enum GuidePlace { Designer, Ascent }

    public enum GuideSpeaker { Thrum, MissionControl, Host, Cohost }

    public sealed class GuideSettings
    {
        public SubtitleSpeed Speed = SubtitleSpeed.Normal;
        public SubtitleSize Size = SubtitleSize.Normal;
        public TipsMode Tips = TipsMode.On;
        public bool ReduceMotion;
        public bool ReadAloud;
        public bool SubtitlesOn = true;
        public bool Engineer;
        /// <summary>Sound off. The blip stays quiet. Text still shows.</summary>
        public bool Muted;
        public string StepId = "";
    }

    public sealed class GuideCard
    {
        public string Kid = "";
        public string Eng;
    }

    public sealed class GuideBeat
    {
        public string Id = "";
        public GuideSpeaker Speaker;
        public GuidePriority Priority;
        public bool Critical;
        public bool Decision;
        public string StepId = "";
        public GuideCard[] Cards = Array.Empty<GuideCard>();
        public Dictionary<string, string> Tokens;
        /// <summary>Extra time the line should stay, such as a narration clip's length.</summary>
        public float MinHold;
        /// <summary>True when the beat came from the guide engine rather than a narration clip.</summary>
        public bool FromEngine = true;
        /// <summary>Bundled clip under assets/narration/guide. Empty keeps the line text-only.</summary>
        public string VoiceClip;
    }

    public static class GuideSpeakers
    {
        public static string Tag(GuideSpeaker speaker)
        {
            switch (speaker)
            {
                case GuideSpeaker.MissionControl: return "MISSION CONTROL";
                case GuideSpeaker.Host: return "HOST";
                case GuideSpeaker.Cohost: return "CO-HOST";
                default: return "MABEL THRUM";
            }
        }

        public static string Role(GuideSpeaker speaker)
            => speaker == GuideSpeaker.Thrum ? "HANGAR KEEPER" : "";

        /// <summary>Spoken form. VoiceOver should not shout the name tag.</summary>
        public static string Spoken(GuideSpeaker speaker)
        {
            switch (speaker)
            {
                case GuideSpeaker.MissionControl: return "Mission control";
                case GuideSpeaker.Host: return "Host";
                case GuideSpeaker.Cohost: return "Co-host";
                default: return "Thrum, hangar keeper";
            }
        }

        public static bool HasPortrait(GuideSpeaker speaker) => speaker == GuideSpeaker.Thrum;
    }

    public static class GuideTokens
    {
        public static string Fill(string text, IReadOnlyDictionary<string, string> tokens)
        {
            if (string.IsNullOrEmpty(text) || tokens == null || tokens.Count == 0) return text ?? "";
            string s = text;
            foreach (var pair in tokens)
            {
                if (string.IsNullOrEmpty(pair.Key)) continue;
                s = s.Replace("{" + pair.Key + "}", pair.Value ?? "");
            }
            return s;
        }
    }

    public static class GuidePages
    {
        public static string[] Of(GuideBeat beat, bool engineer)
        {
            var list = new List<string>();
            if (beat == null || beat.Cards == null) return Array.Empty<string>();
            for (int i = 0; i < beat.Cards.Length; i++)
            {
                var card = beat.Cards[i];
                if (card == null) continue;
                string kid = GuideTokens.Fill(card.Kid, beat.Tokens);
                if (!string.IsNullOrEmpty(kid)) list.Add(kid);
                if (engineer && !string.IsNullOrEmpty(card.Eng))
                    list.Add(GuideTokens.Fill(card.Eng, beat.Tokens));
            }
            return list.ToArray();
        }
    }

    /// <summary>Ink, cream and amber from the subtitle spec. Cream on ink is the 12:1 pair.</summary>
    public static class GuideColors
    {
        public const int Ink = 0x1B1F2A;
        public const int Cream = 0xFAF0D7;
        public const int Amber = 0xE8B15A;
        public const int Shadow = 0x0A0C12;
        public const float MinContrast = 12f;

        public static int Hex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return 0;
            if (hex[0] == '#') hex = hex.Substring(1);
            return int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        public static float Contrast(int fg, int bg)
        {
            float hi = Lum(fg);
            float lo = Lum(bg);
            if (hi < lo) { float t = hi; hi = lo; lo = t; }
            return (hi + 0.05f) / (lo + 0.05f);
        }

        static float Lum(int rgb)
        {
            float r = Chan((rgb >> 16) & 255);
            float g = Chan((rgb >> 8) & 255);
            float b = Chan(rgb & 255);
            return 0.2126f * r + 0.7152f * g + 0.0722f * b;
        }

        static float Chan(int c)
        {
            float s = c / 255f;
            if (s <= 0.04045f) return s / 12.92f;
            return (float)Math.Pow((s + 0.055) / 1.055, 2.4);
        }
    }

    public static class GuidePacing
    {
        public const float SlowCps = 25f;
        public const float NormalCps = 40f;
        public const float FastCps = 70f;
        public const float BlipSeconds = 0.04f;
        public const int BlipEvery = 3;
        public const float MinReadSeconds = 2f;
        public const float ReadBaseSeconds = 1.2f;
        public const float ReadCharsPerSecond = 18f;
        public const float MinOnScreenSeconds = 2.5f;
        public const float LongPressSeconds = 0.5f;
        public const float MinFontPt = 15f;
        public const float LargeFontPt = 19f;
        public const float ExtraLargeFontPt = 23f;
        public const float SkipMinPt = 44f;
        public const float MaxHeightPt = 168f;
        public const float CompactHeightPt = 96f;
        public const float ThumbZonePt = 230f;
        public const float PortraitPt = 96f;
        public const float PortraitCompactPt = 52f;
        public const float SideMarginPt = 8f;
        public const float MaxColumnPt = 600f;
        public const float StackGapPt = 6f;
        public const float ShadowPt = 5f;
        public const float BorderPt = 3f;
        public const float PadPt = 6f;
        public const int MaxBoxes = 2;
        public const float AscentHeaderPt = 48f;

        public static float Cps(SubtitleSpeed speed)
        {
            switch (speed)
            {
                case SubtitleSpeed.Slow: return SlowCps;
                case SubtitleSpeed.Fast: return FastCps;
                case SubtitleSpeed.Instant: return 0f;
                default: return NormalCps;
            }
        }

        public static bool ShowWhole(SubtitleSpeed speed, bool reduceMotion)
            => reduceMotion || speed == SubtitleSpeed.Instant;

        public static float FontPt(SubtitleSize size)
        {
            switch (size)
            {
                case SubtitleSize.Large: return LargeFontPt;
                case SubtitleSize.ExtraLarge: return ExtraLargeFontPt;
                default: return MinFontPt;
            }
        }

        public static float HeightCap(bool compact, SubtitleSize size)
            => (compact ? CompactHeightPt : MaxHeightPt) * FontPt(size) / MinFontPt;

        /// <summary>chars/18 + 1.2, and at least 2 seconds.</summary>
        public static float ReadingSeconds(int chars)
        {
            if (chars < 0) chars = 0;
            float t = chars / ReadCharsPerSecond + ReadBaseSeconds;
            return t < MinReadSeconds ? MinReadSeconds : t;
        }

        /// <summary>On screen at least 2.5s, and at least the reading time and any clip length.</summary>
        public static float HoldSeconds(int chars, float extra)
        {
            float read = ReadingSeconds(chars);
            float hold = read > MinOnScreenSeconds ? read : MinOnScreenSeconds;
            return extra > hold ? extra : hold;
        }
    }

    static class GuideMath
    {
        public static float Max(float a, float b) => a > b ? a : b;
        public static float Min(float a, float b) => a < b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;
    }
}
