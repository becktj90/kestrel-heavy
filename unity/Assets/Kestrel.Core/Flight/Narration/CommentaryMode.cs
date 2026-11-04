namespace Kestrel.Core.Flight.Narration
{
    /// <summary>
    /// The player's commentary choice (pref <see cref="Pref"/>): the two-voice color, the calls, or nothing.
    /// Off is the old narration mute.
    /// </summary>
    public enum CommentaryMode
    {
        Full = 0,
        CallsOnly = 1,
        Off = 2,
    }

    public static class Commentary
    {
        public const string Pref = "kh.audio.commentary";

        public static CommentaryMode FromInt(int value)
        {
            switch (value)
            {
                case 1: return CommentaryMode.CallsOnly;
                case 2: return CommentaryMode.Off;
                default: return CommentaryMode.Full;
            }
        }

        /// <summary>The saved mode. A save with no commentary pref reads the old narration mute: muted is Off.</summary>
        public static CommentaryMode Resolve(bool hasPref, int pref, bool legacyMuted)
            => hasPref ? FromInt(pref) : (legacyMuted ? CommentaryMode.Off : CommentaryMode.Full);

        /// <summary>Full, then Calls only, then Off, then back to Full.</summary>
        public static CommentaryMode Next(CommentaryMode mode)
        {
            switch (mode)
            {
                case CommentaryMode.Full: return CommentaryMode.CallsOnly;
                case CommentaryMode.CallsOnly: return CommentaryMode.Off;
                default: return CommentaryMode.Full;
            }
        }

        public static bool AllowsCalls(CommentaryMode mode) => mode != CommentaryMode.Off;
        public static bool AllowsColor(CommentaryMode mode) => mode == CommentaryMode.Full;

        public static string Label(CommentaryMode mode)
        {
            switch (mode)
            {
                case CommentaryMode.CallsOnly: return "Narration calls";
                case CommentaryMode.Off: return "Narration off";
                default: return "Narration on";
            }
        }
    }
}
