// The flight notes (the "i" sheet). TestFlight t432 (#333): the notes ran two long sentences each and were cut
// mid-line in a box over the Follow / Cam / Pause row. Short lines now; the sheet has a fixed height and scrolls.
namespace Kestrel.Core.Flight
{
    public static class AscentNotes
    {
        public const string EngineerDetail = "Nudge shifts the planned pitch. Amber shows the shift.";
        public const string AssistDetail = "Auto-assist flies the timed calls. Tap one for gold.";
        public const string Caption = "Green: perfect window. Cyan: the plan. Amber: your nudge.";
        /// <summary>Longest note line, characters: one or two lines at 13 pt on a 320 pt phone.</summary>
        public const int MaxChars = 60;
        public static readonly string[] All = { EngineerDetail, AssistDetail, Caption };
    }
}
