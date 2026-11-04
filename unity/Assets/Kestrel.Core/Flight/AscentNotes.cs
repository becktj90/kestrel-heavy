// The flight notes (the "i" sheet). TestFlight t432 (#332): the notes ran two long sentences each and were cut
// mid-line in a box over the Follow / Cam / Pause row. Short lines now; the sheet has a fixed height and scrolls.
namespace Kestrel.Core.Flight
{
    public static class AscentNotes
    {
        public const string EngineerDetail = "Nudge shifts the planned pitch. Amber shows the shift.";
        /// <summary>DECISIONS #80: the nudge row before the rocket clears the tower.</summary>
        public const string NudgeGated = "Nudge opens once the rocket clears the tower.";
        public const string AssistDetail = "Auto-assist flies the timed calls. Tap one for gold.";
        public const string Caption = "Green: perfect window. Cyan: the plan. Amber: your nudge.";
        /// <summary>Longest note line, characters: one or two lines at 13 pt on a 320 pt phone.</summary>
        public const int MaxChars = 60;
        /// <summary>DECISIONS #80: HUD beats when the design's own max-Q throttle law starts and ends (no call to press).</summary>
        public const string MaxQThrottleDown = "Max-Q, throttling down";
        public const string MaxQThrough = "Through max-Q";
        public static readonly string[] All = { EngineerDetail, NudgeGated, AssistDetail, Caption };
        public static readonly string[] Beats = { MaxQThrottleDown, MaxQThrough };
    }
}
